using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CS;
using System;

namespace PX.Objects.SO.GraphExtensions.ARPaymentEntryExt
{
	public class Blanket : PXGraphExtension<OrdersToApplyTab, ARPaymentEntry.MultiCurrency, ARPaymentEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		#region Events

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUnboundFormula(
			typeof(IIf<Where<SOAdjust.voided, Equal<False>>, SOAdjust.curyAdjdTransferredToChildrenAmt, decimal0>),
			typeof(SumCalc<SOOrder.curyTransferredToChildrenPaymentTotal>),
			ForceAggregateRecalculation = true)]
		protected virtual void _(Events.CacheAttached<SOAdjust.curyAdjdTransferredToChildrenAmt> eventArgs)
		{
		}

		protected virtual void _(Events.RowSelected<SOAdjust> e)
		{
			if (e.Row == null)
				return;

			if (Base2.IsApplicationToBlanketOrderWithChild(e.Row))
			{
				PXUIFieldAttribute.SetEnabled<SOAdjust.curyAdjgAmt>(e.Cache, e.Row, false);

				e.Cache.RaiseExceptionHandling<SOAdjust.curyDocBal>(e.Row, 0m,
					new PXSetPropertyException(Messages.CannotApplyPaymentToBlanketOrderWithChild, PXErrorLevel.Warning));
			}
		}

		protected virtual void _(Events.RowUpdated<SOAdjust> e)
		{
			if (e.Row.CuryAdjgAmt != e.OldRow.CuryAdjgAmt && e.Row.BlanketNbr != null &&
				e.Row.CuryAdjgBilledAmt == e.OldRow.CuryAdjgBilledAmt)
			{
				UpdateBlanketSOAdjust(e.Row, e.OldRow);
			}
		}

		protected virtual void _(Events.RowDeleted<SOAdjust> e)
		{
			if (Base2.IsApplicationToBlanketOrderWithChild(e.Row))
				ClearLinksToBlanketSOAdjust(e.Row);
		}

		#endregion // Events

		#region Methods
		#region TransferredToChildrenAmt calculation

		protected virtual void UpdateBlanketSOAdjust(SOAdjust adjustment, SOAdjust oldAdjustment)
		{
			var difference = GetBlanketSOAdjustDifference(adjustment, oldAdjustment);
			if (difference.CuryAdjg == 0)
				return;

			SOAdjust blanketAdjustment = GetBlanketSOAdjust(adjustment);
			CalculateBlanketTransferredAmount(difference, blanketAdjustment);

			blanketAdjustment = Base2.SOAdjustments.Update(blanketAdjustment);
			Base2.SOAdjustments.View.RequestRefresh();
		}

		protected virtual (decimal CuryAdjg, decimal Adj, decimal CuryAdjd) GetBlanketSOAdjustDifference(SOAdjust adjustment, SOAdjust oldAdjustment)
		{
			return (
				(adjustment.CuryAdjgAmt ?? 0m) - (oldAdjustment.CuryAdjgAmt ?? 0m),
				(adjustment.AdjAmt ?? 0m) - (oldAdjustment.AdjAmt ?? 0m),
				(adjustment.CuryAdjdAmt ?? 0m) - (oldAdjustment.CuryAdjdAmt ?? 0m));
		}

		protected virtual SOAdjust GetBlanketSOAdjust(SOAdjust adjustment)
		{
			SOAdjust blanketAdjustment = SelectFrom<SOAdjust>
				.Where<SOAdjust.recordID.IsEqual<SOAdjust.blanketRecordID.FromCurrent>
					.And<SOAdjust.adjdOrderType.IsEqual<SOAdjust.blanketType.FromCurrent>>
					.And<SOAdjust.adjdOrderNbr.IsEqual<SOAdjust.blanketNbr.FromCurrent>>
					.And<SOAdjust.adjgDocType.IsEqual<SOAdjust.adjgDocType.FromCurrent>>
					.And<SOAdjust.adjgRefNbr.IsEqual<SOAdjust.adjgRefNbr.FromCurrent>>>
				.View.SelectSingleBound(Base, new object[] { adjustment });

			if (blanketAdjustment == null)
			{
				throw new Common.Exceptions.RowNotFoundException(Base2.SOAdjustments.Cache,
					adjustment.RecordID, adjustment.BlanketType, adjustment.BlanketNbr, adjustment.AdjgDocType, adjustment.AdjgRefNbr);
			}

			return blanketAdjustment;
		}

		protected virtual void CalculateBlanketTransferredAmount((decimal CuryAdjg, decimal Adj, decimal CuryAdjd) difference, SOAdjust blanketAdjustment)
		{
			blanketAdjustment.CuryAdjgAmt = Math.Max(blanketAdjustment.CuryAdjgAmt - difference.CuryAdjg ?? 0m, 0m);

			blanketAdjustment.CuryAdjgTransferredToChildrenAmt =
				Math.Max(blanketAdjustment.CuryAdjgTransferredToChildrenAmt + difference.CuryAdjg ?? 0m, 0m);

			blanketAdjustment.AdjTransferredToChildrenAmt =
				Math.Max(blanketAdjustment.AdjTransferredToChildrenAmt + difference.Adj ?? 0m, 0m);

			blanketAdjustment.CuryAdjdTransferredToChildrenAmt =
				Math.Max(blanketAdjustment.CuryAdjdTransferredToChildrenAmt + difference.CuryAdjd ?? 0m, 0m);
		}

		#endregion // TransferredToChildrenAmt calculation

		protected virtual void ClearLinksToBlanketSOAdjust(SOAdjust blanketSOAdjust)
		{
			foreach(SOAdjust childAdjustment in SelectFrom<SOAdjust>
				.Where<SOAdjust.blanketRecordID.IsEqual<SOAdjust.recordID.FromCurrent>
					.And<SOAdjust.blanketType.IsEqual<SOAdjust.adjdOrderType.FromCurrent>>
					.And<SOAdjust.blanketNbr.IsEqual<SOAdjust.adjdOrderNbr.FromCurrent>>
					.And<SOAdjust.adjgDocType.IsEqual<SOAdjust.adjgDocType.FromCurrent>>
					.And<SOAdjust.adjgRefNbr.IsEqual<SOAdjust.adjgRefNbr.FromCurrent>>>
				.View.SelectMultiBound(Base, new object[] { blanketSOAdjust }))
			{
				childAdjustment.BlanketRecordID = null;
				childAdjustment.BlanketType = null;
				childAdjustment.BlanketNbr = null;
				Base2.SOAdjustments.Update(childAdjustment);
			}
		}

		#endregion // Methods
	}
}
