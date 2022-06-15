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
    public class PMTransitLocationStatusAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMTransitLocationStatusAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMTransitLocationStatusAccum bal = (PMTransitLocationStatusAccum)row;

            columns.Update<PMTransitLocationStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMTransitLocationStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMTransitLocationStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMTransitLocationStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            //only in release process updates onhand.
            if (bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMTransitLocationStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }

            if (sender.GetStatus(row) == PXEntryStatus.Inserted && IsZero((PMTransitLocationStatusAccum)row))
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
                object inventoryID = sender.GetValue<PMTransitLocationStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMTransitLocationStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMTransitLocationStatusAccum.siteID>(row);
                object locationID = sender.GetValue<PMTransitLocationStatusAccum.locationID>(row);
                object projectID = sender.GetValue<PMTransitLocationStatusAccum.projectID>(row);
                object taskID = sender.GetValue<PMTransitLocationStatusAccum.taskID>(row);

                PMTransitLocationStatusAccum item = PMTransitLocationStatusAccum.PK.Find(sender.Graph, (int?)inventoryID, (int?)subItemID, (int?)siteID, (int?)locationID, (int?)projectID, (int?)taskID);

                item = (PMTransitLocationStatusAccum)this.Aggregate(sender, item, row);

                PMTransitLocationStatusAccum bal = (PMTransitLocationStatusAccum)row;

                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    if (item.QtyOnHand < 0m)
                    {
                        message = IN.Messages.StatusCheck_QtyTransitOnHandNegative;
                    }
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLocationStatusAccum.inventoryID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLocationStatusAccum.subItemID>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMTransitLocationStatusAccum bal = (PMTransitLocationStatusAccum)e.Row;
                string message = null;
                //only in release process updates onhand.
                if (bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyTransitOnHandNegative;
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLocationStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMTransitLocationStatusAccum.subItemID>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }
    }


}
