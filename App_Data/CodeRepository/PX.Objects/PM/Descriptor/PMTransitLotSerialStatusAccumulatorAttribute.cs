using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Overrides.INDocumentRelease;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class PMTransitLotSerialStatusAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMTransitLotSerialStatusAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMTransitLotSerialStatusAccum bal = (PMTransitLotSerialStatusAccum)row;

            columns.Update<PMTransitLotSerialStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMTransitLotSerialStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMTransitLotSerialStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.lotSerTrack>(bal.LotSerTrack, PXDataFieldAssign.AssignBehavior.Initialize);
            columns.Update<PMTransitLotSerialStatusAccum.receiptDate>(bal.ReceiptDate, PXDataFieldAssign.AssignBehavior.Initialize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLotSerialStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            //only in release process updates onhand.
            if (bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMTransitLotSerialStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }

            return true;
        }

        public override bool PersistInserted(PXCache sender, object row)
        {
            try
            {
                return base.PersistInserted(sender, row);
            }
            catch (PXLockViolationException)
            {
                object inventoryID = sender.GetValue<PMTransitLotSerialStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMTransitLotSerialStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMTransitLotSerialStatusAccum.siteID>(row);
                object locationID = sender.GetValue<PMTransitLotSerialStatusAccum.locationID>(row);
                object lotSerialNbr = sender.GetValue<PMTransitLotSerialStatusAccum.lotSerialNbr>(row);
                object projectID = sender.GetValue<PMTransitLotSerialStatusAccum.projectID>(row);
                object taskID = sender.GetValue<PMTransitLotSerialStatusAccum.taskID>(row);

                PMTransitLotSerialStatusAccum item = PMTransitLotSerialStatusAccum.PK.Find(sender.Graph, (int?)inventoryID, (int?)subItemID, (int?)siteID, (int?)locationID, (string)lotSerialNbr, (int?) projectID, (int?) taskID);

                item = (PMTransitLotSerialStatusAccum)this.Aggregate(sender, item, row);

                PMTransitLotSerialStatusAccum bal = (PMTransitLotSerialStatusAccum)row;

                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    if (item.QtyOnHand < 0m)
                    {
                        message = IN.Messages.StatusCheck_QtyTransitLotSerialOnHandNegative;
                    }
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.inventoryID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.subItemID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.lotSerialNbr>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMTransitLotSerialStatusAccum bal = (PMTransitLotSerialStatusAccum)e.Row;
                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyTransitLotSerialOnHandNegative;
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.subItemID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLotSerialStatusAccum.lotSerialNbr>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }
    }


}
