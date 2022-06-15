using System;
using System.Linq;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.Extensions.CustomerCreditHold;
using PX.SM;

namespace PX.Objects.SO.GraphExtensions
{
	/// <summary>A mapped generic graph extension that defines the SO credit helper functionality.</summary>
	public abstract class SOOrderCustomerCreditExtension<TGraph> : CustomerCreditExtension<
			TGraph,
			SOOrder,
			SOOrder.customerID,
			SOOrder.creditHold,
			SOOrder.completed,
			SOOrder.status>
		where TGraph : PXGraph
	{
		#region Implementation

		protected override bool? GetReleasedValue(PXCache sender, SOOrder row)
		{
			return row?.Cancelled == true || row?.Completed == true;
		}

		protected override bool? GetHoldValue(PXCache sender, SOOrder row)
		{
			return (row?.Hold == true || row?.CreditHold == true || row?.InclCustOpenOrders == false);
		}

		protected override bool? GetCreditCheckError(PXCache sender, SOOrder row)
		{
			return SOOrderType.PK.Find(sender.Graph, row.OrderType)?.CreditHoldEntry ?? false;
		}

		private bool IsLongOperationProcessing(PXGraph graph) => PXLongOperation.Exists(graph);
		public override void Verify(PXCache sender, SOOrder Row, EventArgs e)
		{
			if (!IsLongOperationProcessing(sender.Graph) || VerifyOnLongRun(sender, Row, e))
				base.Verify(sender, Row, e);
		}

		public virtual bool VerifyOnLongRun(PXCache sender, SOOrder Row, EventArgs e)
		{
			var rowUpdatedArgs = e as PXRowUpdatedEventArgs;
			if (rowUpdatedArgs == null)
				return false;

			return GetHoldValue(sender, Row) != GetHoldValue(sender, (SOOrder)rowUpdatedArgs.OldRow);
		}

		protected virtual void _(Events.RowInserted<SOOrder> e)
		{
			if (e.Row != null)
				UpdateARBalances(e.Cache, e.Row, null);
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.UpdateCustomerBalances(PXCache, SOOrder, SOOrder)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateCustomerBalances(PXCache cache, SOOrder row, SOOrder oldRow,
			Action<PXCache, SOOrder, SOOrder> baseMethod)
		{
			if (row == null || oldRow == null)
				return;

			UpdateARBalances(cache, row, oldRow);
		}

		protected override void _(Events.RowUpdated<SOOrder> e)
		{
			if (_InternalCall == false)
				base._(e);
		}

		protected virtual void _(Events.RowDeleted<SOOrder> e)
		{
			if (e.Row != null)
				UpdateARBalances(e.Cache, null, e.Row);
		}

		protected override decimal? GetDocumentBalance(PXCache cache, SOOrder row)
		{
			decimal? DocumentBal = base.GetDocumentBalance(cache, row);

			if (DocumentBal > 0m && IsFullAmountApproved(row))
			{
				DocumentBal = 0m;
			}

			return DocumentBal;
		}

		protected override bool IsPutOnCreditHoldAllowed(PXCache cache, SOOrder row)
		{
			return (row.ApprovedCreditByPayment == true) ? (row.IsFullyPaid != true) :
				base.IsPutOnCreditHoldAllowed(cache, row);
		}

		protected override void PlaceOnHold(PXCache cache, SOOrder order, bool onAdminHold)
		{
			SOOrder.Events.Select(e => e.CreditLimitViolated).FireOn(Base, order);
			base.PlaceOnHold(cache, order, false);

			cache.SetValue<SOOrder.approvedCredit>(order, false);
			cache.SetValue<SOOrder.approvedCreditAmt>(order, 0m);
		}

		protected override void ApplyCreditVerificationResult(PXCache sender, SOOrder row, CreditVerificationResult res, PXCache arbalancescache)
		{
			if (row.IsFullyPaid == true)
			{
				row.SatisfyCreditLimitByPayment(Base);
			}
			else
			{
				base.ApplyCreditVerificationResult(sender, row, res, arbalancescache);
			}
		}

		public override void UpdateARBalances(PXCache cache, SOOrder newRow, SOOrder oldRow)
		{
			if (oldRow == null || newRow == null ||
				!cache.ObjectsEqualBy<BqlFields.FilledWith<
					SOOrder.unbilledOrderTotal,
					SOOrder.openOrderTotal,
					SOOrder.inclCustOpenOrders,
					SOOrder.hold,
					SOOrder.creditHold,
					SOOrder.customerID,
					SOOrder.customerLocationID,
					SOOrder.aRDocType,
					SOOrder.branchID,
					SOOrder.shipmentCntr,
					SOOrder.cancelled>>(oldRow, newRow))
			{
				if (oldRow != null)
					ARReleaseProcess.UpdateARBalances(cache.Graph, oldRow, -oldRow.UnbilledOrderTotal, -oldRow.OpenOrderTotal);

				if (newRow != null)
					ARReleaseProcess.UpdateARBalances(cache.Graph, newRow, newRow.UnbilledOrderTotal, newRow.OpenOrderTotal);
			}
		}

		protected virtual bool IsFullAmountApproved(SOOrder row)
			=> row.ApprovedCredit == true && row.ApprovedCreditAmt >= row.OrderTotal;

		protected override bool IsCreditVerificationEnabled()
		{
			var transitions = WorkflowService.GetPossibleTransition(Base, SOOrderStatus.CreditHold).ToList();
			return transitions.Any(t => t.ActionName == nameof(SOOrderEntry.OnCreditLimitViolated));
		}

		#endregion
	}
}
