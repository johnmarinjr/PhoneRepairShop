using PX.Data;
using PX.Objects.GL;
using PX.Objects.IN;
using static PX.Objects.IN.INReleaseProcess;
using System;
using PX.Objects.PM.GraphExtensions;

namespace PX.Objects.PM.MaterialManagement
{
    public class INReleaseProcessMaterialExt : PXGraphExtension<INReleaseProcessExt, INReleaseProcess>
    {
        #region DAC Cache Attached overrides

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [INTranSplitForProjectPlanID(typeof(INRegister.noteID), typeof(INRegister.hold), typeof(INRegister.transferType))]
        protected virtual void _(Events.CacheAttached<INTranSplit.planID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INTranCost.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INTranSplit.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INCostStatusTransitLineSummary.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INCostStatusSummary.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ReadOnlyCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.OversoldCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.UnmanagedCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.AverageCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.StandardCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.FIFOCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.SpecificCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.SpecificTransitCostStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INItemCostHist.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ItemCostHist.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INReceiptStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ReceiptStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ReadOnlyReceiptStatus.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INItemSalesHist.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ItemSalesHist.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<INItemCustSalesHist.costSiteID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBChildIdentity(typeof(PMCostCenter.costCenterID))]
        protected virtual void _(Events.CacheAttached<IN.Overrides.INDocumentRelease.ItemCustSalesHist.costSiteID> e) { } 
        #endregion

        public PXSelect<PMSiteStatusAccum> projectsitestatus;
        public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
        public PXSelect<PMLocationStatusAccum> projectlocationstatus;
        public PXSelect<PMTransitLocationStatusAccum> projecttransitlocationstatus;
        public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;
        public PXSelect<PMTransitLotSerialStatusAccum> projecttransitlotserialstatus;

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
        }

        [PXOverride]
        public virtual void UpdateSplitDestinationLocation(INTran tran, INTranSplit split, int? value, Action<INTran, INTranSplit, int?> baseMethod)
        {
            baseMethod(tran, split, value);

            if (split.SkipCostUpdate == true)
            {
                if (tran.CostCenterID != null || tran.ToCostCenterID != null)
                {
                    split.SkipCostUpdate = false;
                    Base.intransplit.Cache.MarkUpdated(split);
                }
            }
        }

        [PXOverride]
        public virtual void UpdateCrossReference(INTran tran, INTranSplit split, InventoryItem item, INLocation whseloc, Action<INTran, INTranSplit, InventoryItem, INLocation> baseMethod)
        {
            baseMethod(tran, split, item, whseloc);

            if (tran.CostCenterID != null)
                split.CostSiteID = tran.CostCenterID;
        }

        [PXOverride]
        public virtual void UpdateSiteStatus(INTran tran, INTranSplit split, INLocation whseloc, Action<INTran, INTranSplit, INLocation> baseMethod)
        {
            baseMethod(tran, split, whseloc);

            if (split.InvtMult == 0)
                return;

            ProjectLocationKey key = GetStatusKey(tran, split);

            PMSiteStatusAccum item = new PMSiteStatusAccum();
            item.InventoryID = split.InventoryID;
            item.SubItemID = split.SubItemID;
            item.SiteID = key.SiteID;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;

            item = projectsitestatus.Insert(item);

            item.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyAvail += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            item.QtyHardAvail += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            item.QtyActual += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            item.QtyNotAvail += whseloc.InclQtyAvail == true ? 0m : (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.SkipQtyValidation = split.SkipQtyValidation;

            PMSiteSummaryStatusAccum itemSummary = new PMSiteSummaryStatusAccum();
            itemSummary.InventoryID = split.InventoryID;
            itemSummary.SubItemID = split.SubItemID;
            itemSummary.SiteID = key.SiteID;
            itemSummary.ProjectID = key.ProjectID;

            itemSummary = projectsummarysitestatus.Insert(itemSummary);

            itemSummary.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
            itemSummary.QtyAvail += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            itemSummary.QtyHardAvail += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            itemSummary.QtyActual += whseloc.InclQtyAvail == true ? (decimal)split.InvtMult * (decimal)split.BaseQty : 0m;
            itemSummary.QtyNotAvail += whseloc.InclQtyAvail == true ? 0m : (decimal)split.InvtMult * (decimal)split.BaseQty;
            itemSummary.SkipQtyValidation = split.SkipQtyValidation;
        }

        [PXOverride]
        public virtual void UpdateLocationStatus(INTran tran, INTranSplit split, Action<INTran, INTranSplit> baseMethod)
        {
            baseMethod(tran, split);

            if (split.InvtMult == 0)
                return;

            ProjectLocationKey key = GetStatusKey(tran, split);

            PMLocationStatusAccum item = new PMLocationStatusAccum();
            item.InventoryID = split.InventoryID;
            item.SubItemID = split.SubItemID;
            item.SiteID = split.SiteID;
            item.LocationID = key.LocationID;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;
            item.RelatedPIID = Base.inregister.Current.PIID;

            item = projectlocationstatus.Insert(item);

            item.NegQty = (split.TranType == INTranType.Adjustment) ? false : item.NegQty;
            item.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyHardAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyActual += (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.SkipQtyValidation = split.SkipQtyValidation;
        }

        [PXOverride]
        public virtual IN.Overrides.INDocumentRelease.TransitLocationStatus TwoStepTransferNonLot(INTran tran, INTranSplit split,
            Func<INTran, INTranSplit, IN.Overrides.INDocumentRelease.TransitLocationStatus> baseMethod)
        {
            IN.Overrides.INDocumentRelease.TransitLocationStatus tranitem = baseMethod(tran, split);
            PMTransitLocationStatusAccum item = AccumulateFromTransitLocationStatus(tran, split, tranitem);

            item.QtyOnHand -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyAvail -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyHardAvail -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            item.QtyActual -= (decimal)split.InvtMult * (decimal)split.BaseQty;

            return tranitem;
        }

        private PMTransitLocationStatusAccum AccumulateFromTransitLocationStatus(INTran tran, INTranSplit split, PX.Objects.IN.Overrides.INDocumentRelease.TransitLocationStatus lstritem)
        {            
            ProjectLocationKey key = GetStatusKey(tran, split);

            if (Base.IsIngoingTransfer(tran))
            {
                INTran origTransfer = INTran.PK.Find(Base, tran.OrigDocType, tran.OrigRefNbr, tran.OrigLineNbr);
                if (origTransfer != null)
                {
                    key = GetStatusKey(origTransfer, split);
                }
            }


            PMTransitLocationStatusAccum item = new PMTransitLocationStatusAccum();
            item.InventoryID = lstritem.InventoryID;
            item.SubItemID = lstritem.SubItemID;
            item.SiteID = lstritem.SiteID;
            item.LocationID = lstritem.LocationID;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;
           
            item = (PMTransitLocationStatusAccum)projecttransitlocationstatus.Cache.Insert(item);
            return item;
        }


        [PXOverride]
        public virtual void ReceiveLot(INTran tran, INTranSplit split, InventoryItem item, INLotSerClass lsclass,
            Action<INTran, INTranSplit, InventoryItem, INLotSerClass> baseMethod)
        {
            baseMethod(tran, split, item, lsclass);

            PMLotSerialStatusAccum lsitem;
            if (split.InvtMult == (short)1 && !Base.IsOneStepTransfer())
            {
                if (lsclass.LotSerTrack != INLotSerTrack.NotNumbered &&
                    (lsclass.LotSerAssign == INLotSerAssign.WhenReceived)
                )
                {
                    lsitem = AccumulatedLotSerialStatus(tran, split, lsclass);
                    lsitem.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
                    lsitem.QtyAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
                    lsitem.QtyHardAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
                    lsitem.QtyActual += (decimal)split.InvtMult * (decimal)split.BaseQty;

                    return;
                }
            }
        }

        [PXOverride]
        public virtual void IssueLot(INTran tran, INTranSplit split, InventoryItem item, INLotSerClass lsclass,
            Action<INTran, INTranSplit, InventoryItem, INLotSerClass> baseMethod)
        {
            baseMethod(tran, split, item, lsclass);

            PMLotSerialStatusAccum lsitem;
            if (split.InvtMult == -1)
            {
                //for when used serial numbers numbers will mark processed numbers with trandate
                if (INLotSerialNbrAttribute.IsTrackSerial(lsclass, tran.TranType, tran.InvtMult) ||
                    lsclass.LotSerTrack != INLotSerTrack.NotNumbered && lsclass.LotSerAssign == INLotSerAssign.WhenReceived)
                {
                    lsitem = AccumulatedLotSerialStatus(tran, split, lsclass);

                    if (lsclass.LotSerAssign == INLotSerAssign.WhenReceived)
                    {
                        lsitem.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
                        lsitem.QtyAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
                        lsitem.QtyHardAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
                        lsitem.QtyActual += (decimal)split.InvtMult * (decimal)split.BaseQty;
                    }

                    return;
                }
            }
        }

       
        [PXOverride]
        public virtual PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus OneStepTransferLot(INTran tran, INTranSplit split, InventoryItem item, INLotSerClass lsclass,
            Func<INTran, INTranSplit, InventoryItem, INLotSerClass, PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus> baseMethod)
        {
            PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus lsitem = baseMethod(tran, split, item, lsclass);

            PMLotSerialStatusAccum pmlsitem = AccumulatedLotSerialStatus(tran, split, lsclass);
            pmlsitem.ReceiptDate = lsitem.ReceiptDate;
          
            pmlsitem.QtyOnHand += (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyHardAvail += (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyActual += (decimal)split.InvtMult * (decimal)split.BaseQty;

            return lsitem;
        }
                
        [PXOverride]
        public virtual IN.Overrides.INDocumentRelease.TransitLotSerialStatus AccumulatedTransitLotSerialStatus(INTran tran, INTranSplit split, INLotSerClass lsclass, INTransitLine tl, 
            Func<INTran, INTranSplit, INLotSerClass, INTransitLine, IN.Overrides.INDocumentRelease.TransitLotSerialStatus> baseMethod)
        {
            PX.Objects.IN.Overrides.INDocumentRelease.TransitLotSerialStatus lstritem = baseMethod(tran, split, lsclass, tl);
            
            PMTransitLotSerialStatusAccum pmlsitem = AccumulateFromTransitLotSerialStatus(tran, split, lstritem);
            
            pmlsitem.QtyOnHand -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyAvail -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyHardAvail -= (decimal)split.InvtMult * (decimal)split.BaseQty;
            pmlsitem.QtyActual -= (decimal)split.InvtMult * (decimal)split.BaseQty;

            return lstritem;
        }

        private PMTransitLotSerialStatusAccum AccumulateFromTransitLotSerialStatus(INTran tran, INTranSplit split, PX.Objects.IN.Overrides.INDocumentRelease.TransitLotSerialStatus lstritem)
        {
            ProjectLocationKey key = GetStatusKey(tran, split);
            
            if (Base.IsIngoingTransfer(tran))
            {
                INTran origTransfer = INTran.PK.Find(Base, tran.OrigDocType, tran.OrigRefNbr, tran.OrigLineNbr);
                if (origTransfer != null)
                {
                    key = GetStatusKey(origTransfer, split);
                }
            }

            PMTransitLotSerialStatusAccum item = new PMTransitLotSerialStatusAccum();
            item.InventoryID = lstritem.InventoryID;
            item.SubItemID = lstritem.SubItemID;
            item.SiteID = lstritem.SiteID;
            item.LocationID = lstritem.LocationID;
            item.LotSerialNbr = lstritem.LotSerialNbr;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;
            
            item = (PMTransitLotSerialStatusAccum)projecttransitlotserialstatus.Cache.Insert(item);
            item.ReceiptDate = lstritem.ReceiptDate;
            item.ExpireDate = lstritem.ExpireDate;
            item.LotSerTrack = lstritem.LotSerTrack;

            return item;
        }


        [PXOverride]
        public virtual IN.Overrides.INDocumentRelease.LotSerialStatus TwoStepTransferLotComplement(INTran tran, INTranSplit split, InventoryItem item, INLotSerClass lsclass, DateTime receiptDate,
            Func<INTran, INTranSplit, InventoryItem, INLotSerClass, DateTime, IN.Overrides.INDocumentRelease.LotSerialStatus> baseMethod)
        {
            IN.Overrides.INDocumentRelease.LotSerialStatus complementlsitem = baseMethod(tran, split, item, lsclass, receiptDate);
            
            AccumulateFromComplement(tran, split, complementlsitem);

            return complementlsitem;
        }

        private PMLotSerialStatusAccum AccumulateFromComplement(INTran tran, INTranSplit split, PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus complementlsitem)
        {
            ProjectLocationKey key = GetStatusKey(tran, split);

            PMLotSerialStatusAccum item = new PMLotSerialStatusAccum();
            item.InventoryID = complementlsitem.InventoryID;
            item.SubItemID = complementlsitem.SubItemID;
            item.SiteID = complementlsitem.SiteID;
            item.LocationID = complementlsitem.LocationID;
            item.LotSerialNbr = complementlsitem.LotSerialNbr;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;
            item = (PMLotSerialStatusAccum)projectlotserialstatus.Cache.Insert(item);

            item.ReceiptDate = complementlsitem.ReceiptDate;
            item.ExpireDate = complementlsitem.ExpireDate;
            item.LotSerTrack = complementlsitem.LotSerTrack;
           
            return item;
        }


        [PXOverride]
        public virtual GLTran InsertGLCostsDebit(JournalEntry je, GLTran tran, GLTranInsertionContext context, Func<JournalEntry, GLTran, GLTranInsertionContext, GLTran> baseMethod)
        {
            if (!ProjectDefaultAttribute.IsNonProject(context.INTran.ProjectID) && IsProjectAccount(tran.AccountID))
            {
                string accountingMode = GetAccountingMode(context.INTran.ProjectID);
                if (accountingMode == ProjectAccountingModes.Linked)
                {
                    WriteLinkedCostsDebitTarget(tran, context);
                }
                else
                {
                    WriteValuatedCostsDebitTarget(tran, context);
                }
            }
            if (context.INTran.CostCodeID != null)
                tran.CostCodeID = context.INTran.CostCodeID;

            return baseMethod(je, tran, context);
        }

        [PXOverride]
        public virtual INCostStatus AccumulatedCostStatus(INTran tran, INTranSplit split, InventoryItem item, Func<INTran, INTranSplit, InventoryItem, INCostStatus> baseMethod)
        {
            INCostStatus result = baseMethod(tran, split, item);

            if (item.ValMethod == INValMethod.Standard &&
                tran.TranType != INTranType.NegativeCostAdjustment && 
                tran.CostCenterID == result.CostSiteID)
            {
                INItemSite itemsite = INReleaseProcess.SelectItemSite(Base, tran.InventoryID, tran.SiteID);

                if (itemsite != null)
                {
                    result.UnitCost = itemsite.StdCost;
                }
            }

            return result;
        }

        public virtual PMLotSerialStatusAccum AccumulatedLotSerialStatus(INTran tran, INTranSplit split, INLotSerClass lsclass)
        {
            ProjectLocationKey key = GetStatusKey(tran, split);

            PMLotSerialStatusAccum item = new PMLotSerialStatusAccum();
            item.InventoryID = split.InventoryID;
            item.SubItemID = split.SubItemID;
            item.SiteID = key.SiteID;
            item.LocationID = key.LocationID;
            item.LotSerialNbr = split.LotSerialNbr;
            item.ProjectID = key.ProjectID;
            item.TaskID = key.ProjectTaskID;

            item = (PMLotSerialStatusAccum)projectlotserialstatus.Cache.Insert(item);
            if (item.ExpireDate == null)
            {
                item.ExpireDate = split.ExpireDate;
            }
            if (item.ReceiptDate == null)
            {
                item.ReceiptDate = split.TranDate;
            }
            item.LotSerTrack = lsclass.LotSerTrack;

            return item;
        }

        private ProjectLocationKey GetStatusKey(INTran tran, INTranSplit split)
        {
            int projectID = tran.ProjectID.GetValueOrDefault(ProjectDefaultAttribute.NonProject().GetValueOrDefault());
            int taskID = tran.TaskID.GetValueOrDefault();
            if ( INTranSplitForProjectPlanIDAttribute.IsLinkedProject(Base, tran.ProjectID) )
            {
                projectID = ProjectDefaultAttribute.NonProject().GetValueOrDefault();
                taskID = 0;
            }
            
            return new ProjectLocationKey(tran.SiteID.Value, split.LocationID.Value, projectID, taskID);
        }

        private void WriteLinkedCostsDebitTarget(GLTran tran, GLTranInsertionContext context)
        {
            int? locProjectID;
            int? locTaskID = null;
            if (context.Location != null && context.Location.ProjectID != null)//can be null if Adjustment
            {
                locProjectID = context.Location.ProjectID;
                locTaskID = context.Location.TaskID;

                if (locTaskID == null)//Location with ProjectTask WildCard
                {
                    if (context.Location.ProjectID == context.INTran.ProjectID)
                    {
                        locTaskID = context.INTran.TaskID;
                    }
                    else
                    {
                        //substitute with any task from the project.
                        PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
                            And<PMTask.visibleInIN, Equal<True>, And<PMTask.isActive, Equal<True>>>>>.Select(Base, context.Location.ProjectID);
                        if (task != null)
                        {
                            locTaskID = task.TaskID;
                        }
                    }
                }

            }
            else
            {
                locProjectID = PM.ProjectDefaultAttribute.NonProject();
            }

            if (context.TranCost.TranType == INTranType.Adjustment || context.TranCost.TranType == INTranType.Transfer)
            {
                tran.ProjectID = locProjectID;
                tran.TaskID = locTaskID;
            }
            else
            {
                tran.ProjectID = context.INTran.ProjectID ?? locProjectID;
                tran.TaskID = context.INTran.TaskID ?? locTaskID;
            }
            if (context.INTran.CostCodeID != null)
                tran.CostCodeID = context.INTran.CostCodeID;
        }

        private void WriteValuatedCostsDebitTarget(GLTran tran, GLTranInsertionContext context)
        {
            tran.ProjectID = context.INTran.ProjectID;
            tran.TaskID = context.INTran.TaskID;
            if (context.INTran.CostCodeID != null)
                tran.CostCodeID = context.INTran.CostCodeID;
        }

        private bool IsProjectAccount(int? accountID)
        {
            Account account = Account.PK.Find(Base, accountID);
            return account?.AccountGroupID != null;
        }

        
        private string GetAccountingMode(int? projectID)
        {
            if (projectID != null)
            {
                PMProject project = PMProject.PK.Find(Base, projectID);
                if (project != null && project.NonProject != true)
                {
                    return project.AccountingMode;
                }
            }

            return ProjectAccountingModes.Valuated;
        }
               
        [System.Diagnostics.DebuggerDisplay("{SiteID}.{ProjectID}.{ProjectTaskID}.{CostCodeID}")]
        public struct ProjectCostSiteKey
        {
            public readonly int SiteID;
            public readonly int LocationID;
            public readonly int ProjectID;
            public readonly int ProjectTaskID;
            public readonly int CostCodeID;

            public ProjectCostSiteKey(int siteID, int locationID, int projectID, int projectTaskID, int costCodeID)
            {
                LocationID = locationID;
                SiteID = siteID;
                ProjectID = projectID;
                ProjectTaskID = projectTaskID;
                CostCodeID = costCodeID;
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    hash = hash * 23 + SiteID.GetHashCode();
                    hash = hash * 23 + LocationID.GetHashCode();
                    hash = hash * 23 + ProjectID.GetHashCode();
                    hash = hash * 23 + ProjectTaskID.GetHashCode();
                    hash = hash * 23 + CostCodeID.GetHashCode();
                    return hash;
                }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{SiteID}.{LocationID}.{ProjectID}.{ProjectTaskID}")]
        public struct ProjectLocationKey
        {
            public readonly int SiteID;
            public readonly int LocationID;
            public readonly int ProjectID;
            public readonly int ProjectTaskID;
            
            public ProjectLocationKey(int siteID, int locationID, int projectID, int projectTaskID)
            {
                SiteID = siteID;
                LocationID = locationID;
                ProjectID = projectID;
                ProjectTaskID = projectTaskID;
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    hash = hash * 23 + SiteID.GetHashCode();
                    hash = hash * 23 + LocationID.GetHashCode();
                    hash = hash * 23 + ProjectID.GetHashCode();
                    hash = hash * 23 + ProjectTaskID.GetHashCode();
                    
                    return hash;
                }
            }
        }
    }

}
