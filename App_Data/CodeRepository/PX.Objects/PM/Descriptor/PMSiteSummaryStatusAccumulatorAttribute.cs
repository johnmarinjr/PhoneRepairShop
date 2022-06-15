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
    public class PMSiteSummaryStatusAccumulatorAttribute : StatusAccumulatorAttribute
    {
        public PMSiteSummaryStatusAccumulatorAttribute()
        {
            base._SingleRecord = true;
        }

        protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
        {
            if (!base.PrepareInsert(sender, row, columns))
            {
                return false;
            }

            PMSiteSummaryStatusAccum bal = (PMSiteSummaryStatusAccum)row;

            columns.Update<PMSiteSummaryStatusAccum.qtyOnHand>(bal.QtyOnHand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyAvail>(bal.QtyAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyHardAvail>(bal.QtyHardAvail, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyActual>(bal.QtyActual, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyINIssues>(bal.QtyINIssues, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyINReceipts>(bal.QtyINReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyInTransit>(bal.QtyInTransit, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyPOReceipts>(bal.QtyPOReceipts, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyPOPrepared>(bal.QtyPOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMSiteSummaryStatusAccum.qtyFSSrvOrdPrepared>(bal.QtyFSSrvOrdPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyFSSrvOrdBooked>(bal.QtyFSSrvOrdBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyFSSrvOrdAllocated>(bal.QtyFSSrvOrdAllocated, PXDataFieldAssign.AssignBehavior.Summarize);

            columns.Update<PMSiteSummaryStatusAccum.qtyPOOrders>(bal.QtyPOOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtySOPrepared>(bal.QtySOPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtySOBooked>(bal.QtySOBooked, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtySOShipped>(bal.QtySOShipped, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtySOShipping>(bal.QtySOShipping, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyINAssemblyDemand>(bal.QtyINAssemblyDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyINAssemblySupply>(bal.QtyINAssemblySupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyInTransitToProduction>(bal.QtyInTransitToProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProductionSupplyPrepared>(bal.QtyProductionSupplyPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProductionSupply>(bal.QtyProductionSupply, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyPOFixedProductionPrepared>(bal.QtyPOFixedProductionPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyPOFixedProductionOrders>(bal.QtyPOFixedProductionOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProductionDemandPrepared>(bal.QtyProductionDemandPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProductionDemand>(bal.QtyProductionDemand, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProductionAllocated>(bal.QtyProductionAllocated, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtySOFixedProduction>(bal.QtySOFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedPurchase>(bal.QtyProdFixedPurchase, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedProduction>(bal.QtyProdFixedProduction, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedProdOrdersPrepared>(bal.QtyProdFixedProdOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedProdOrders>(bal.QtyProdFixedProdOrders, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedSalesOrdersPrepared>(bal.QtyProdFixedSalesOrdersPrepared, PXDataFieldAssign.AssignBehavior.Summarize);
            columns.Update<PMSiteSummaryStatusAccum.qtyProdFixedSalesOrders>(bal.QtyProdFixedSalesOrders, PXDataFieldAssign.AssignBehavior.Summarize);

            //only in release process updates onhand.
            if (bal.NegQty == false && bal.SkipQtyValidation != true && bal.QtyOnHand < 0m)
            {
                columns.Restrict<PMSiteSummaryStatusAccum.qtyOnHand>(PXComp.GE, -bal.QtyOnHand);
            }
            else if (bal.NegAvailQty == false && bal.SkipQtyValidation != true && bal.QtyHardAvail < 0m)
            {
                columns.Restrict<PMSiteSummaryStatusAccum.qtyHardAvail>(PXComp.GE, -bal.QtyHardAvail);
            }
            if (bal.NegQty == false && bal.SkipQtyValidation != true && bal.QtyActual < 0m)
            {
                columns.Restrict<PMSiteSummaryStatusAccum.qtyActual>(PXComp.GE, -bal.QtyActual);
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
                object inventoryID = sender.GetValue<PMSiteSummaryStatusAccum.inventoryID>(row);
                object subItemID = sender.GetValue<PMSiteSummaryStatusAccum.subItemID>(row);
                object siteID = sender.GetValue<PMSiteSummaryStatusAccum.siteID>(row);
                object projectID = sender.GetValue<PMSiteSummaryStatusAccum.projectID>(row);
               
                PMSiteSummaryStatusAccum item = PMSiteSummaryStatusAccum.PK.Find(sender.Graph, (int?)inventoryID, (int?)subItemID, (int?)siteID, (int?)projectID);

                item = (PMSiteSummaryStatusAccum)this.Aggregate(sender, item, row);

                PMSiteSummaryStatusAccum bal = (PMSiteSummaryStatusAccum)row;
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
                    PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.inventoryID>(sender, row),
                    PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.subItemID>(sender, row),
                    PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.siteID>(sender, row));
                }

                throw;
            }
        }

        public override void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
            {
                PMSiteSummaryStatusAccum bal = (PMSiteSummaryStatusAccum)e.Row;
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
                        PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.inventoryID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.subItemID>(sender, e.Row),
                        PXForeignSelectorAttribute.GetValueExt<PMSiteSummaryStatusAccum.siteID>(sender, e.Row));
                }
            }

            base.RowPersisted(sender, e);
        }
    }
}
