using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Overrides.INDocumentRelease;
using PX.Objects.IN.PhysicalInventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class PMLocationStatusAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMLocationStatusAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMLocationStatusAccum bal = (PMLocationStatusAccum)row;

            columns.Update<PMLocationStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMLocationStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMLocationStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMLocationStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            if (bal.QtyOnHand >= 0m)
            {
                bal.NegQty = true;
            }

            //only in release process updates onhand.
            if (bal.NegQty == false && bal.SkipQtyValidation != true && bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMLocationStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }

            if (!_InternalCall &&
                (bal.QtyOnHand < 0m || bal.QtySOShipped < 0m ||
                bal.QtyOnHand > 0m || bal.QtySOShipped > 0m))
            {
                if (this.CreateLocksInspector(bal.SiteID.Value)
                    .IsInventoryLocationLocked(bal.InventoryID, bal.LocationID, bal.RelatedPIID))
                {
                    throw new PXException(IN.Messages.PICountInProgressDuringRelease,
                                          PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.inventoryID>(sender, bal),
                                          PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.siteID>(sender, bal),
                                          PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.locationID>(sender, bal));
                }
            }

            if (sender.GetStatus(row) == PXEntryStatus.Inserted && IsZero((PMLocationStatusAccum)row))
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
                object inventoryID = sender.GetValue<PMLocationStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMLocationStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMLocationStatusAccum.siteID>(row);
                object locationID = sender.GetValue<PMLocationStatusAccum.locationID>(row);
                object projectID = sender.GetValue<PMLocationStatusAccum.projectID>(row);
                object taskID = sender.GetValue<PMLocationStatusAccum.taskID>(row);

                PMLocationStatusAccum item = PMLocationStatusAccum.PK.Find(sender.Graph, (int?)inventoryID, (int?)subItemID, (int?)siteID, (int?)locationID, (int?) projectID, (int?) taskID);

                item = (PMLocationStatusAccum)this.Aggregate(sender, item, row);

                PMLocationStatusAccum bal = (PMLocationStatusAccum)row;

                string message = null;
                //only in release process updates onhand.
                if (bal.NegQty == false && bal.QtyOnHand < 0m)
                {
                    if (item.QtyOnHand < 0m)
                    {
                        message = IN.Messages.StatusCheck_QtyLocationOnHandNegative;
                    }
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.inventoryID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.subItemID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.siteID>(sender, row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.locationID>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMLocationStatusAccum bal = (PMLocationStatusAccum)e.Row;
                string message = null;
                //only in release process updates onhand.
                if (bal.NegQty == false && bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyLocationOnHandNegative;
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.subItemID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.siteID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMLocationStatusAccum.locationID>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }

        protected virtual PILocksInspector CreateLocksInspector(int siteID)
        {
            return new PILocksInspector(siteID);
        }
    }
}
