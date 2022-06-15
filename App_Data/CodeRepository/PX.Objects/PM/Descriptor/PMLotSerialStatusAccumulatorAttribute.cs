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
    public class PMLotSerialStatusAccumAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMLotSerialStatusAccumAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMLotSerialStatusAccum bal = (PMLotSerialStatusAccum)row;

            columns.Update<PMLotSerialStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMLotSerialStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMLotSerialStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.lotSerTrack>(bal.LotSerTrack, PXDataFieldAssign.AssignBehavior.Initialize);
            columns.Update<PMLotSerialStatusAccum.receiptDate>(bal.ReceiptDate, PXDataFieldAssign.AssignBehavior.Initialize);
            columns.Update<PMLotSerialStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLotSerialStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            //only in release process updates onhand.
            if (bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMLotSerialStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }

            if (sender.GetStatus(row) == PXEntryStatus.Inserted && IsZero((PMLotSerialStatusAccum)row))
            {
                sender.SetStatus(row, PXEntryStatus.InsertedDeleted);
                return false;
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
                object inventoryID = sender.GetValue<PMLotSerialStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMLotSerialStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMLotSerialStatusAccum.siteID>(row);
                object locationID = sender.GetValue<PMLotSerialStatusAccum.locationID>(row);
                object lotSerialNbr = sender.GetValue<PMLotSerialStatusAccum.lotSerialNbr>(row);
                object projectID = sender.GetValue<PMLotSerialStatusAccum.projectID>(row);
                object taskID = sender.GetValue<PMLotSerialStatusAccum.taskID>(row);

                PMLotSerialStatusAccum item = PMLotSerialStatusAccum.PK.Find(sender.Graph, (int)inventoryID, (int?)subItemID, (int?)siteID, (int?)locationID, (string)lotSerialNbr, (int?)projectID, (int?)taskID);

                item = (PMLotSerialStatusAccum)this.Aggregate(sender, item, row);

                PMLotSerialStatusAccum bal = (PMLotSerialStatusAccum)row;

                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    if (item.QtyOnHand < 0m)
                    {
                        message = IN.Messages.StatusCheck_QtyLotSerialOnHandNegative;
                    }
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.inventoryID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.subItemID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.siteID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.locationID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.lotSerialNbr>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.projectID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.taskID>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMLotSerialStatusAccum bal = (PMLotSerialStatusAccum)e.Row;
                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyLotSerialOnHandNegative;
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.subItemID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.siteID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.locationID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.lotSerialNbr>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.projectID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLotSerialStatusAccum.taskID>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }
    }
}
