using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.SO
{
	public class SOOrderShipmentProcess : PXGraph<SOOrderShipmentProcess, SOOrderShipment>
	{
        public PXSelect<SOOrder> Orders;
        public PXSelect<SOShipment> Shipments;
		public PXSelect<SOMiscLine2,
			Where<SOMiscLine2.orderType, Equal<Required<SOOrder.orderType>>,
				And<SOMiscLine2.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
			MiscLines;
		public PXSelect<ARBalances> Arbalances;

		public PXAction<SOShipment> flow;
		[PXUIField(DisplayName = "Flow")]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntryF)]
		protected virtual IEnumerable Flow(PXAdapter adapter)
		{
			Save.Press();
			return adapter.Get();
		}

		public PXSelectJoin<SOOrderShipment,
			InnerJoin<SOOrder, 
				On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, 
				And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,
			InnerJoin<ARInvoice,
				On<ARInvoice.docType, Equal<SOOrderShipment.invoiceType>,
					And<ARInvoice.refNbr, Equal<SOOrderShipment.invoiceNbr>,
						And<ARInvoice.released, Equal<boolTrue>>>>>>,
			Where<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>, 
				And<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>>>> Items;
		public PXSelect<SOAdjust,
				Where<SOAdjust.adjdOrderType, Equal<Required<SOAdjust.adjdOrderType>>,
					And<SOAdjust.adjdOrderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>>>> Adjustments;

        protected virtual void SOOrder_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            SOOrder doc = (SOOrder)e.Row;
            if (e.Operation == PXDBOperation.Update)
            {
                if (doc.ShipmentCntr < 0 || doc.OpenShipmentCntr < 0 || doc.ShipmentCntr < doc.BilledCntr + doc.ReleasedCntr && doc.Behavior == SOBehavior.SO)
                {
                    throw new Exceptions.InvalidShipmentCountersException();
                }
            } 
        }

		public SOOrderShipmentProcess()
		{
		}

		public virtual void CompleteSOLinesAndSplits(ARRegister ardoc, List<PXResult<SOOrderShipment, SOOrder>> orderShipments)
		{
			if (ardoc.IsCancellation == true || ardoc.IsCorrection == true) return;

			foreach (PXResult<SOOrderShipment, SOOrder> orderShipment in orderShipments)
			{
				SOOrder order = orderShipment;
				SOOrderType orderType = SOOrderType.PK.Find(this, order.OrderType);
				if (orderType.RequireShipping == false)
				{
					PXDatabase.Update<SOLine>(
						new PXDataFieldAssign<SOLine.completed>(true),
						new PXDataFieldRestrict<SOLine.completed>(false),
						new PXDataFieldRestrict<SOLine.orderType>(PXDbType.VarChar, 2, order.OrderType, PXComp.EQ),
						new PXDataFieldRestrict<SOLine.orderNbr>(PXDbType.NVarChar, 15, order.OrderNbr, PXComp.EQ));
					PXDatabase.Update<SOLineSplit>(
						new PXDataFieldAssign<SOLineSplit.completed>(true),
						new PXDataFieldRestrict<SOLineSplit.completed>(false),
						new PXDataFieldRestrict<SOLineSplit.orderType>(PXDbType.VarChar, 2, order.OrderType, PXComp.EQ),
						new PXDataFieldRestrict<SOLineSplit.orderNbr>(PXDbType.NVarChar, 15, order.OrderNbr, PXComp.EQ));
				}
			}
		}

		public virtual List<PXResult<SOOrderShipment, SOOrder>> UpdateOrderShipments(ARRegister arDoc, HashSet<object> processed)
		{
			bool isCancellationInvoice = arDoc.IsCancellation == true;
			bool isCorrectionInvoice = arDoc.IsCorrection == true;
			var boundInvoice = isCancellationInvoice
				? ARInvoice.PK.Find(this, arDoc.OrigDocType, arDoc.OrigRefNbr)
				: arDoc;

			var orderShipments = Items.View.SelectMultiBound(new object[] { boundInvoice })
				.Cast<PXResult<SOOrderShipment, SOOrder>>()
				.ToList();

			var (links, orders) = orderShipments.UnZip(
				r => PXCache<SOOrderShipment>.CreateCopy(r),
				r => r.GetItem<SOOrder>(),
				(ls, rs) => (ls.ToList(), rs.ToList()));

			SOOrderShipment ChangeReleased(SOOrderShipment sosh, bool isReleased)
			{
				sosh.InvoiceReleased = isReleased;
				return Items.Update(sosh);
			}

			if (isCancellationInvoice)
			{
				links =
					links
					.Select(r => ChangeReleased(r, false))
					.ToList();

				var cancelledInvoice = SOInvoice.PK.Find(this, boundInvoice.DocType, boundInvoice.RefNbr);
				SOInvoice.Events
					.Select(e => e.InvoiceCancelled)
					.FireOn(this, cancelledInvoice);

				links =
					links
					.Select(r => r.UnlinkInvoice(this))
					.ToList();

				ARInvoice existingCorrectionInvoice =
					PXSelect<ARInvoice,
					Where<ARInvoice.origDocType.IsEqual<ARInvoice.origDocType.FromCurrent>.
						And<ARInvoice.origRefNbr.IsEqual<ARInvoice.origRefNbr.FromCurrent>>.
						And<ARInvoice.isCorrection.IsEqual<True>>>>
					.SelectSingleBound(this, new[] { arDoc });
				if (existingCorrectionInvoice != null)
				{
					var correctionInvoice = SOInvoice.PK.Find(this, existingCorrectionInvoice.DocType, existingCorrectionInvoice.RefNbr);
					links = 
						links
						.Select(r => r.LinkInvoice(correctionInvoice, this))
						.ToList();
				}
			}
			else
			{
				links =
					links
					.Select(r => ChangeReleased(r, true))
					.ToList();
				// note that SOInvoice.Events.InvoiceReleased will be fired outside
			}

			if (!isCancellationInvoice && !isCorrectionInvoice)
			{
				foreach (var group in orders.GroupBy(order => (order.OrderType, order.OrderNbr)))
				{
					SOOrder order = group.First();
					SOOrderType otype = SOOrderType.PK.Find(this, order.OrderType);
					SOOrder updatedOrder = (SOOrder)Caches[typeof(SOOrder)].Locate(order);

					if ((order.Completed == true || otype.RequireShipping == false) && updatedOrder.ShipmentCntr <= updatedOrder.ReleasedCntr)
					{
						foreach (SOAdjust adj in Adjustments.Select(order.OrderType, order.OrderNbr))
						{
							SOAdjust adjcopy = PXCache<SOAdjust>.CreateCopy(adj);
							adjcopy.CuryAdjdAmt = 0m;
							adjcopy.CuryAdjgAmt = 0m;
							adjcopy.AdjAmt = 0m;
							Adjustments.Update(adjcopy);
						}

						CompleteMiscLines(order);
					}
				}
			}

			if (orderShipments.Any())
				processed.Add(arDoc);

			return orderShipments.ToList();
		}

		protected virtual void CompleteMiscLines(SOOrder order)
		{
			PXRowUpdated updateBalance = (s, e) =>
			{
				if (!(e.Row is SOOrder row))
					return;

				if (e.OldRow is SOOrder oldRow)
				{
					ARReleaseProcess.UpdateARBalances(this, oldRow, -oldRow.UnbilledOrderTotal, -oldRow.OpenOrderTotal);
				}
				ARReleaseProcess.UpdateARBalances(this, row, row.UnbilledOrderTotal, row.OpenOrderTotal);
			};

			RowUpdated.AddHandler<SOOrder>(updateBalance);

			try
			{
				foreach (SOMiscLine2 line in MiscLines.Select(order.OrderType, order.OrderNbr))
				{
					line.CuryUnbilledAmt = 0m;
					line.UnbilledQty = 0m;
					line.Completed = true;
					MiscLines.Update(line);
				}
			}
			finally
			{
				RowUpdated.RemoveHandler<SOOrder>(updateBalance);
			}
		}

		public virtual void OnInvoiceReleased(ARRegister ardoc, List<PXResult<SOOrderShipment, SOOrder>> orderShipments)
        {
			CompleteSOLinesAndSplits(ardoc, orderShipments);
		}
	}
}
