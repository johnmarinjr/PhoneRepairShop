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
    public class PMSiteStatusAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMSiteStatusAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMSiteStatusAccum bal = (PMSiteStatusAccum)row;

            columns.Update<PMSiteStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMSiteStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMSiteStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            //only in release process updates onhand.
            if (bal.NegQty == false && bal.SkipQtyValidation != true && bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMSiteStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }
            else if (bal.NegAvailQty == false && bal.SkipQtyValidation != true && bal.QtyHardAvail < 0m)
            {
                columns.Restrict<PMSiteStatusAccum.qtyHardAvail>(PXComp.GE, -bal.QtyHardAvail);
            }
            if (bal.NegQty == false && bal.SkipQtyValidation != true && bal.QtyActual < 0m)
            {
                columns.Restrict<PMSiteStatusAccum.qtyActual>(PXComp.GE, -bal.QtyActual);
            }

            if (sender.GetStatus(row) == PXEntryStatus.Inserted && IsZero(bal) && bal.PersistEvenZero != true)
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
                object inventoryID = sender.GetValue<PMSiteStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMSiteStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMSiteStatusAccum.siteID>(row);
                object projectID = sender.GetValue<PMLocationStatusAccum.projectID>(row);
                object taskID = sender.GetValue<PMLocationStatusAccum.taskID>(row);

                PMSiteStatusAccum item = PMSiteStatusAccum.PK.Find(sender.Graph, (int?)inventoryID, (int?)subItemID, (int?)siteID, (int?)projectID, (int?)taskID);

                item = (PMSiteStatusAccum)this.Aggregate(sender, item, row);

                PMSiteStatusAccum bal = (PMSiteStatusAccum)row;
                string message = null;
                //only in release process updates onhand.
                if (bal.NegQty == false && bal.QtyOnHand < 0m)
                {
                    if (item.QtyOnHand < 0m)
                    {
                        message = IN.Messages.StatusCheck_QtyOnHandNegative;
                    }
                }
                else if (bal.NegAvailQty == false && bal.QtyHardAvail < 0m)
                {
                    if (item.QtyHardAvail < 0)
                    {
                        message = IN.Messages.StatusCheck_QtyAvailNegative;
                    }
                }
                if (bal.NegQty == false && bal.QtyActual < 0m)
                {
                    if (item.QtyActual < 0)
                    {
                        message = IN.Messages.StatusCheck_QtyActualNegative;
                    }
                }

                if (message != null)
                {
                    throw new PXException(message,
                    PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.inventoryID>(sender, row),
                    PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.subItemID>(sender, row),
                    PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.siteID>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMSiteStatusAccum bal = (PMSiteStatusAccum)e.Row;
                string message = null;
                //only in release process updates onhand.
                if (bal.NegQty == false && bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyOnHandNegative;
                }

                if (bal.NegAvailQty == false && bal.QtyHardAvail < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyAvailNegative;
                }
                else if (bal.NegQty == false && bal.QtyINIssues < 0m && bal.QtyOnHand < 0m)
                {
                    message = IN.Messages.StatusCheck_QtyAvailNegative;
                }

                if (message != null)
                {
                    throw new PXException(message,
                        PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.subItemID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMSiteStatusAccum.siteID>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }
    }
}
