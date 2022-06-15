using PX.Data;
using PX.Objects.IN;
using System;
using System.Linq;
using PX.Objects.AM.Attributes;
using PX.Common;
using PX.Objects.CS;
using System.Collections;
using System.Collections.Generic;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;
using PX.Objects.EP;

namespace PX.Objects.AM
{
    /// <summary>
    /// Engineering Change Request graph
    /// Main graph for managing a Engineering Change Request (ECR)
    /// </summary>
    public class ECRMaint : PXGraph<ECRMaint, AMECRItem>
    {
        [PXViewName(Messages.ECRItem)]
        public PXSelect<AMECRItem> Documents;

        [PXImport(typeof(AMECRItem))]
        public PXSelect<AMBomOper,
            Where<AMBomOper.bOMID, Equal<Current<AMECRItem.eCRID>>,
                And<AMBomOper.revisionID, Equal<AMECRItem.eCRRev>>>,
            OrderBy<Asc<AMBomOper.operationCD>>> BomOperRecords;

        [PXImport(typeof(AMECRItem))]
        [PXCopyPasteHiddenFields]
        public AMOrderedMatlSelect<AMECRItem, AMBomMatl,
            Where<AMBomMatl.bOMID, Equal<Current<AMBomOper.bOMID>>,
                And<AMBomMatl.revisionID, Equal<Current<AMBomOper.revisionID>>,
                And<AMBomMatl.operationID, Equal<Current<AMBomOper.operationID>>>>>,
            OrderBy<Asc<AMBomMatl.sortOrder, Asc<AMBomMatl.lineID>>>> BomMatlRecords;

        [PXImport(typeof(AMECRItem))]
        public PXSelect<AMBomStep,
            Where<AMBomStep.bOMID, Equal<Current<AMBomOper.bOMID>>,
                And<AMBomStep.revisionID, Equal<Current<AMBomOper.revisionID>>,
                And<AMBomStep.operationID, Equal<Current<AMBomOper.operationID>>>>>> BomStepRecords;

        [PXImport(typeof(AMECRItem))]
        public PXSelectJoin<AMBomTool,
            InnerJoin<AMToolMst, On<AMBomTool.toolID, Equal<AMToolMst.toolID>>>,
            Where<AMBomTool.bOMID, Equal<Current<AMBomOper.bOMID>>,
                And<AMBomTool.revisionID, Equal<Current<AMBomOper.revisionID>>,
                And<AMBomTool.operationID, Equal<Current<AMBomOper.operationID>>>>>> BomToolRecords;

        [PXImport(typeof(AMECRItem))]
        public PXSelectJoin<AMBomOvhd,
            InnerJoin<AMOverhead, On<AMBomOvhd.ovhdID, Equal<AMOverhead.ovhdID>>>,
            Where<AMBomOvhd.bOMID, Equal<Current<AMBomOper.bOMID>>,
                And<AMBomOvhd.revisionID, Equal<Current<AMBomOper.revisionID>>,
                And<AMBomOvhd.operationID, Equal<Current<AMBomOper.operationID>>>>>> BomOvhdRecords;

        public PXSelect<AMBomRef,
            Where<AMBomRef.bOMID, Equal<Current<AMBomMatl.bOMID>>,
                And<AMBomRef.revisionID, Equal<Current<AMBomMatl.revisionID>>,
                And<AMBomRef.operationID, Equal<Current<AMBomMatl.operationID>>,
                And<AMBomRef.matlLineID, Equal<Current<AMBomMatl.lineID>>>>>>> BomRefRecords;

        public PXSetup<AMBSetup> ambsetup;
        public PXSetup<AMPSetup> ProdSetup;

        public PXSelect<AMECRItem, Where<AMECRItem.eCRID, Equal<Current<AMECRItem.eCRID>>>> CurrentDocument;

        [PXHidden]
        public PXSelect<
            AMBomAttribute,
            Where<AMBomAttribute.bOMID, Equal<Current<AMECRItem.eCRID>>,
                And<AMBomAttribute.revisionID, Equal<AMECRItem.eCRRev>>>> BomAttributes;

        [PXHidden]
        public PXSelect<AMBomOper,
            Where<AMBomOper.bOMID, Equal<Current<AMBomOper.bOMID>>,
                And<AMBomOper.revisionID, Equal<Current<AMBomOper.revisionID>>,
                And<AMBomOper.operationID, Equal<Current<AMBomOper.operationID>>>>>> OutsideProcessingOperationSelected;

        public ECRMaint()
        {
            var bomSetup = ambsetup.Current;
            if (string.IsNullOrWhiteSpace(bomSetup?.ECRNumberingID))
            {
                throw new BOMSetupNotEnteredException();
            }
        }

        public PXSelect<AMECRSetupApproval> SetupApproval;

        [PXViewName(PX.Objects.EP.Messages.Approval)]
        public PX.Objects.EP.EPApprovalAutomation<AMECRItem, AMECRItem.approved, AMECRItem.rejected, AMECRItem.hold, AMECRSetupApproval> Approval;

        #region CACHE ATTACHED

        [PXDBBool]
        [PXDefault(false, typeof(Search<AMWC.bflushMatl, Where<AMWC.wcID, Equal<Current<AMBomOper.wcID>>>>))]
        [PXUIField(DisplayName = "Backflush")]
		protected virtual void _(Events.CacheAttached<AMBomMatl.bFlush> e) { }

        [BomID(DisplayName = "Comp BOM ID")]
        [BOMIDSelector(typeof(Search2<AMBomItemActive.bOMID,
            LeftJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<AMBomItemActive.inventoryID>>>,
            Where<AMBomItemActive.inventoryID, Equal<Current<AMBomMatl.inventoryID>>>>))]
		protected virtual void _(Events.CacheAttached<AMBomMatl.compBOMID> e) { }

        [OperationIDField(IsKey = true, Visible = false, Enabled = false, DisplayName = "Operation DB ID")]
        [PXLineNbr(typeof(AMECRItem.lineCntrOperation))]
		protected virtual void _(Events.CacheAttached<AMBomOper.operationID> e)
        {
#if DEBUG
            //Cache attached to change display name so we can provide the user with a way to see the DB ID if needed 
#endif
        }

        [BomID(IsKey = true, Visible = false, Enabled = false)]
        [BOMIDSelector(ValidateValue = false)]
        [PXDBDefault(typeof(AMECRItem.eCRID))]
        [PXParent(typeof(Select<AMECRItem, Where<AMECRItem.eCRID, Equal<Current<AMBomOper.bOMID>>,
            And<AMECRItem.eCRRev, Equal<Current<AMBomOper.revisionID>>>>>))]
		protected virtual void _(Events.CacheAttached<AMBomOper.bOMID> e) { }

        [BomID(IsKey = true, Visible = false, Enabled = false)]
        [PXDBDefault(typeof(AMECRItem.eCRID))]
        [PXParent(typeof(Select<AMECRItem, Where<AMECRItem.eCRID, Equal<Current<AMBomAttribute.bOMID>>,
            And<AMECRItem.eCRRev, Equal<Current<AMBomAttribute.revisionID>>>>>))]
		protected virtual void _(Events.CacheAttached<AMBomAttribute.bOMID> e) { }

        [PXDBInt(IsKey = true)]
        [PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, Enabled = false)]
        [PXLineNbr(typeof(AMECRItem.lineCntrAttribute))]
		protected virtual void _(Events.CacheAttached<AMBomAttribute.lineNbr> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomAttribute.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomMatl.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomOper.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomOvhd.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomRef.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomStep.rowStatus> e) { }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [AMRowStatusEvent(typeof(AMECRItem))]
        [PXDefault]
		protected virtual void _(Events.CacheAttached<AMBomTool.rowStatus> e) { }

        [PXDBDate]
        [PXDefault(typeof(AMECRItem.requestDate), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<EPApproval.docDate> e) { }

        [PXDBInt]
        [PXDefault(typeof(AMECRItem.requestor), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<EPApproval.bAccountID> e) { }

        protected virtual void EPApproval_Details_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            var ecr = Documents.Current;
            if (ecr != null)
            {
                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<AMECRItem.inventoryID>(Documents.Cache, ecr);
                if(item != null)
                {
                    e.NewValue = ECRMaint.BOMRevItemDisplay(ecr.BOMID, ecr.BOMRevisionID, item.InventoryCD);
                }
            }
        }

        protected virtual void EPApproval_Descr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            var ecr = Documents.Current;
            if (ecr != null)
            {
                e.NewValue = ecr.Descr;
            }
        }

        #endregion

        #region EP Approval Actions
        public PXInitializeState<AMECRItem> initializeState;
        public PXAction<AMECRItem> hold;
        [PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select)]
        [PXButton]
        protected virtual IEnumerable Hold(PXAdapter adapter) => adapter.Get();

        public PXAction<AMECRItem> approve;
        [PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public IEnumerable Approve(PXAdapter adapter) => adapter.Get();

        public PXAction<AMECRItem> reject;
        [PXUIField(DisplayName = "Reject",  MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public IEnumerable Reject(PXAdapter adapter) => adapter.Get();


        public PXAction<AMECRItem> submit;
        [PXUIField(DisplayName = "Submit", MapEnableRights = PXCacheRights.Select)]
        [PXButton]
        public IEnumerable Submit(PXAdapter adapter) => adapter.Get();

        public PXAction<AMECRItem> CreateECO;
        [PXUIField(DisplayName = "Create ECO", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXButton]
        public virtual IEnumerable createECO(PXAdapter adapter)
        {
            var currentItem = Documents.Current;
            if (currentItem?.RevisionID == null)
            {
                return adapter.Get();
            }

            var ecoGraph = CreateInstance<ECOMaint>();

            var newEco = ecoGraph.Documents.Insert();
            if (newEco == null)
            {
                return adapter.Get();
            }
            ecoGraph.CopyECRtoECO(ecoGraph.Documents.Cache, newEco, currentItem);
            ecoGraph.UpdateECRStatus(currentItem, AMECRStatus.Completed);

            PXRedirectHelper.TryRedirect(ecoGraph, PXRedirectHelper.WindowMode.NewWindow);

            return adapter.Get();
        }
        #endregion

        
        protected virtual void AMECRItem_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            Approval.AllowSelect = ambsetup?.Current?.ECRRequestApproval == true;

            var item = (AMECRItem)e.Row;
            if (item == null)
            {
                return;
            }

			EnableECRItemFields(sender, item);
            // When inserted we want to disable because updated row status is not possible as no save yet
            var holdEnabled = item.Hold.GetValueOrDefault() && !sender.IsRowInserted(item);
            EnableOperCache(holdEnabled);
            EnableOperChildCache(holdEnabled);
        }

        protected virtual void EnableOperCache(bool enabled)
        {
            BomOperRecords.AllowInsert = enabled;
            BomOperRecords.AllowUpdate = enabled;
            BomOperRecords.AllowDelete = enabled;
        }

        protected virtual void EnableOperChildCache(bool enabled)
        {
            BomMatlRecords.AllowInsert = enabled;
            BomMatlRecords.AllowUpdate = enabled;
            BomMatlRecords.AllowDelete = enabled;

            BomStepRecords.AllowInsert = enabled;
            BomStepRecords.AllowUpdate = enabled;
            BomStepRecords.AllowDelete = enabled;

            BomOvhdRecords.AllowInsert = enabled;
            BomOvhdRecords.AllowUpdate = enabled;
            BomOvhdRecords.AllowDelete = enabled;

            BomToolRecords.AllowInsert = enabled;
            BomToolRecords.AllowUpdate = enabled;
            BomToolRecords.AllowDelete = enabled;

            BomRefRecords.AllowInsert = enabled;
            BomRefRecords.AllowUpdate = enabled;
            BomRefRecords.AllowDelete = enabled;

			BomAttributes.AllowInsert = enabled;
			BomAttributes.AllowUpdate = enabled;
			BomAttributes.AllowDelete = enabled;

		}

        #region BOM Oper Processes

        protected virtual AMWC GetCurrentWorkcenter()
        {
            AMWC workCenter = PXSelect<AMWC, Where<AMWC.wcID, Equal<Current<AMBomOper.wcID>>>>.Select(this);

            if (this.Caches<AMWC>() != null)
            {
                this.Caches<AMWC>().Current = workCenter;
            }

            return workCenter;
        }

        protected virtual void AMBomOper_WcID_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            SetWorkCenterFields(cache, (AMBomOper)e.Row);
        }

        protected virtual void AMBomOper_RevisionID_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            e.NewValue = AMECRItem.ECRRev;
        }

        protected virtual void AMBomOper_WcID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            SetWorkCenterFields(cache, (AMBomOper)e.Row);
        }

        protected virtual void SetWorkCenterFields(PXCache cache, AMBomOper bomOper)
        {
            if (cache == null || bomOper == null)
            {
                return;
            }

            var amWC = GetCurrentWorkcenter();

            if (amWC == null)
            {
                return;
            }

            bool isInsert = cache.GetStatus(bomOper) == PXEntryStatus.Inserted;

            if (string.IsNullOrWhiteSpace(bomOper.Descr) || isInsert)
            {
                cache.SetValueExt<AMBomOper.descr>(bomOper, amWC.Descr);
            }

            if (!bomOper.BFlush.GetValueOrDefault() || isInsert)
            {
                cache.SetValueExt<AMBomOper.bFlush>(bomOper, amWC.BflushLbr.GetValueOrDefault());
            }

            // Set the Scrap Action from Work Center
            cache.SetValueExt<AMBomOper.scrapAction>(bomOper, amWC.ScrapAction);
            cache.SetValueExt<AMBomOper.outsideProcess>(bomOper, amWC.OutsideFlg.GetValueOrDefault());
        }

        protected virtual void AMBomOper_RowDeleting(PXCache cache, PXRowDeletingEventArgs e)
        {
            var row = (AMBomOper)e.Row;
            if (row == null || Documents.Cache.IsCurrentRowDeleted())
            {
                return;
            }

            AMBomAttribute bomOperAttribute = PXSelect<AMBomAttribute,
                Where<AMBomAttribute.bOMID, Equal<Required<AMBomAttribute.bOMID>>,
                And<AMBomAttribute.revisionID, Equal<Required<AMBomAttribute.revisionID>>,
                And<AMBomAttribute.operationID, Equal<Required<AMBomAttribute.operationID>>>>
                >>.Select(this, row.BOMID, row.RevisionID, row.OperationID);

            if (bomOperAttribute != null)
            {
                e.Cancel |= BomOperRecords.Ask(Messages.ConfirmDeleteTitle,
                                Messages.GetLocal(Messages.ConfirmOperationDeleteWhenAttributesExist),
                                MessageButtons.YesNo) != WebDialogResult.Yes;
            }

            if (e.Cancel)
            {
                return;
            }

            DeleteBomOperationAttributes(row);
        }

        protected virtual void DeleteBomOperationAttributes(AMBomOper row)
        {
            foreach (AMBomAttribute bomOperAttribute in PXSelect<AMBomAttribute,
                Where<AMBomAttribute.bOMID, Equal<Required<AMBomAttribute.bOMID>>,
                    And<AMBomAttribute.revisionID, Equal<Required<AMBomAttribute.revisionID>>,
                    And<AMBomAttribute.operationID, Equal<Required<AMBomAttribute.operationID>>
                    >>>>.Select(this, row.BOMID, row.RevisionID, row.OperationID))
            {
                BomAttributes.Delete(bomOperAttribute);
            }
        }

        #endregion

        #region BOM Matl Processes

        protected virtual void CompBOMIDFieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            e.Cancel = true;
        }

        protected virtual void AMBomMatl_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var row = (AMBomMatl)e.Row;
            if (row == null)
            {
                return;
            }

            PXUIFieldAttribute.SetEnabled<AMBomMatl.subItemID>(sender, e.Row, row.IsStockItem.GetValueOrDefault());
            PXUIFieldAttribute.SetEnabled<AMBomMatl.subcontractSource>(sender, e.Row, row.MaterialType == AMMaterialType.Subcontract);

            if (IsImport || IsContractBasedAPI)
            {
                return;
            }

            var isMatlExpired = row.ExpDate > Common.Current.BusinessDate(this) || Common.Dates.IsDateNull(row.ExpDate);
            if (!isMatlExpired)
            {
                sender.RaiseExceptionHandling<AMBomMatl.inventoryID>(row, row.InventoryID,
                    new PXSetPropertyException(Messages.MaterialExpiredOnBom, PXErrorLevel.Warning, row.BOMID, row.RevisionID));
            }
        }

        protected virtual void AMBomMatl_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            var matl = (AMBomMatl)e.Row;
            if (matl == null)
            {
                return;
            }

            var subItemFeatureEnabled = InventoryHelper.SubItemFeatureEnabled;

            // Require SUBITEMID when the item is a stock item
            if (subItemFeatureEnabled && matl.InventoryID != null && matl.IsStockItem.GetValueOrDefault() && matl.SubItemID == null)
            {
                cache.RaiseExceptionHandling<AMBomMatl.subItemID>(
                        matl,
                        matl.SubItemID,
                        new PXSetPropertyException(Messages.SubItemIDRequiredForStockItem, PXErrorLevel.Error));
            }

            //  PREVENT A USER FROM ADDING THE MATERIAL ITEM TO ITSELF
            //      More in depth prevention can be added down the road
            if (Documents.Current != null && matl.InventoryID.GetValueOrDefault() != 0)
            {
                if (matl.InventoryID == Documents.Current.InventoryID)
                {
                    if (subItemFeatureEnabled
                        && matl.IsStockItem.GetValueOrDefault()
                        && Documents.Current.SubItemID != null
                        && matl.SubItemID.GetValueOrDefault() != Documents.Current.SubItemID.GetValueOrDefault())
                    {
                        //this should allow different sub items to be consumed on the same BOM as the item being built
                        return;
                    }

                    cache.RaiseExceptionHandling<AMBomMatl.inventoryID>(
                        matl,
                        matl.InventoryID,
                        new PXSetPropertyException(Messages.BomMatlCircularRefAttempt, PXErrorLevel.Error));
                }
            }
        }

        protected virtual void AMBomMatl_SubItemID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            var amBomMatl = (AMBomMatl)e.Row;
            if (amBomMatl == null || Documents.Current == null
                || e.NewValue == null || amBomMatl.InventoryID == null
                || !InventoryHelper.SubItemFeatureEnabled)
            {
                return;
            }

            int? subItemID = Convert.ToInt32(e.NewValue ?? 0);
            if (amBomMatl.InventoryID == Documents.Current.InventoryID
                && (Documents.Current.SubItemID == null
                || Documents.Current.SubItemID.GetValueOrDefault() == subItemID))
            {
                e.NewValue = null;
                e.Cancel = true;
                throw new PXSetPropertyException(Messages.BomMatlCircularRefAttempt, PXErrorLevel.Error);
            }

            InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(this, amBomMatl.InventoryID);
            if (item == null)
            {
                return;
            }
            CheckDuplicateEntry(e, amBomMatl, item, subItemID);
        }

        protected virtual void AMBomMatl_InventoryID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            var amBomMatl = (AMBomMatl)e.Row;
            if (amBomMatl == null || Documents.Current == null
                || e.NewValue == null || InventoryHelper.SubItemFeatureEnabled)
            {
                return;
            }

            int? inventoryID = Convert.ToInt32(e.NewValue);
            InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(this, inventoryID);

            if (item == null)
            {
                return;
            }

            //  PREVENT A USER FROM ADDING THE MATERIAL ITEM TO ITSELF
            //      More in depth prevention can be added down the road
            if (inventoryID == Documents.Current.InventoryID)
            {
                e.NewValue = item.InventoryCD;
                e.Cancel = true;
                throw new PXSetPropertyException(Messages.BomMatlCircularRefAttempt, PXErrorLevel.Error);
            }
            CheckDuplicateEntry(e, amBomMatl, item, amBomMatl.SubItemID);
        }

        protected virtual void AMBomMatl_InventoryID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            var amBomMatl = (AMBomMatl)e.Row;
            if (amBomMatl == null)
            {
                return;
            }

            if (Documents.Current != null && amBomMatl.InventoryID.GetValueOrDefault() != 0)
            {
                cache.SetDefaultExt<AMBomMatl.descr>(e.Row);
                cache.SetDefaultExt<AMBomMatl.subItemID>(e.Row);
                cache.SetDefaultExt<AMBomMatl.uOM>(e.Row);
                cache.SetDefaultExt<AMBomMatl.unitCost>(e.Row);
            }
        }

        protected virtual void DefaultUnitCost(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            object MatlUnitCost;
            sender.RaiseFieldDefaulting<AMBomMatl.unitCost>(e.Row, out MatlUnitCost);

            if (MatlUnitCost != null && (decimal)MatlUnitCost != 0m)
            {
                decimal? matlUnitCost = INUnitAttribute.ConvertToBase<AMBomMatl.inventoryID>(sender, e.Row, ((AMBomMatl)e.Row).UOM, (decimal)MatlUnitCost, INPrecision.UNITCOST);
                sender.SetValueExt<AMBomMatl.unitCost>(e.Row, matlUnitCost);
            }

        }

        protected virtual void AMBomMatl_UOM_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            DefaultUnitCost(sender, e);
        }

        protected virtual void AMBomAttribute_RevisionID_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            e.NewValue = AMECRItem.ECRRev;
        }

        protected virtual void AMBomAttribute_AttributeID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var row = (AMBomAttribute)e.Row;
            if (row == null)
            {
                return;
            }

            var item = (CSAttribute)PXSelectorAttribute.Select<AMBomAttribute.attributeID>(sender, row);
            if (item == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(row.Label))
            {
                sender.SetValueExt<AMBomAttribute.label>(row, item.AttributeID);
            }
            if (string.IsNullOrWhiteSpace(row.Descr))
            {
                sender.SetValueExt<AMBomAttribute.descr>(row, item.Description);
            }
        }

        /// <summary>
        /// Checks for duplicate item in a BOM
        /// </summary>
        /// <param name="e">Calling Field Verifying event args</param>
        /// <param name="matlRow">source material row to check against</param>
        /// <param name="inventoryItem">Inventory item row of newly entered inventory ID (from field verifying)</param>
        /// <returns>True if the row can be added, false otherwise</returns>
        protected virtual void CheckDuplicateEntry(PXFieldVerifyingEventArgs e, AMBomMatl matlRow, InventoryItem inventoryItem)
        {
            CheckDuplicateEntry(e, matlRow, inventoryItem, null);
        }

        /// <summary>
        /// Checks for duplicate item in a BOM
        /// </summary>
        /// <param name="e">Calling Field Verifying event args</param>
        /// <param name="matlRow">source material row to check against</param>
        /// <param name="inventoryItem">Inventory item row of newly entered inventory ID (from field verifying)</param>
        /// <param name="subItemID">SUbItemID</param>
        /// <returns>True if the row can be added, false otherwise</returns>
        protected virtual void CheckDuplicateEntry(PXFieldVerifyingEventArgs e, AMBomMatl matlRow, InventoryItem inventoryItem, int? subItemID)
        {
            AMDebug.TraceWriteMethodName();

            if (matlRow == null || this.ambsetup.Current == null || inventoryItem == null)
            {
                return;
            }

            AMBSetup bomSetup = this.ambsetup.Current;

            //If pages running as import treat warnings the same as allow
            if (IsImport && bomSetup.DupInvBOM.Trim() == SetupMessage.WarningMsg)
            {
                bomSetup.DupInvBOM = SetupMessage.AllowMsg;
            }
            if (IsImport && bomSetup.DupInvOper.Trim() == SetupMessage.WarningMsg)
            {
                bomSetup.DupInvOper = SetupMessage.AllowMsg;
            }

            if (bomSetup.DupInvBOM.Trim() == SetupMessage.AllowMsg
                && bomSetup.DupInvOper.Trim() == SetupMessage.AllowMsg)
            {
                // both allow = nothing to validate
                return;
            }

            AMBomMatl dupBomMatl = null;
            AMBomMatl dupOperMatl = null;

            foreach (AMBomMatl duplicateAMBomMatl in PXSelect<AMBomMatl,
                Where<AMBomMatl.bOMID, Equal<Required<AMBomMatl.bOMID>>,
                    And<AMBomMatl.revisionID, Equal<Required<AMBomMatl.revisionID>>,
                    And<AMBomMatl.inventoryID, Equal<Required<AMBomMatl.inventoryID>>
                    >>>>.Select(this, matlRow.BOMID, matlRow.RevisionID, inventoryItem.InventoryID))
            {
                if (subItemID != null && duplicateAMBomMatl.SubItemID.GetValueOrDefault() != subItemID.GetValueOrDefault() && InventoryHelper.SubItemFeatureEnabled)
                {
                    continue;
                }
                if (duplicateAMBomMatl.OperationID.Equals(matlRow.OperationID) && duplicateAMBomMatl.LineID != matlRow.LineID && dupOperMatl == null)
                {
                    dupOperMatl = duplicateAMBomMatl;
                }

                if (!duplicateAMBomMatl.OperationID.Equals(matlRow.OperationID) && dupBomMatl == null)
                {
                    dupBomMatl = duplicateAMBomMatl;
                }

                if (dupOperMatl != null && dupBomMatl != null)
                {
                    break;
                }
            }

            var skipBomCheck = false;
            if (dupOperMatl != null && bomSetup.DupInvOper.Trim() != SetupMessage.AllowMsg)
            {
                DuplicateEntryMessage(e, dupOperMatl, inventoryItem, bomSetup.DupInvOper.Trim());
                skipBomCheck = true;
            }

            if (dupBomMatl != null && !skipBomCheck && bomSetup.DupInvBOM.Trim() != SetupMessage.AllowMsg)
            {
                DuplicateEntryMessage(e, dupBomMatl, inventoryItem, bomSetup.DupInvBOM.Trim());
            }
        }

        /// <summary>
        /// Builds and creates the warning/error message related to duplicates items on a BOM
        /// </summary>
        /// <param name="e">Calling Field Verifying event args</param>
        /// <param name="duplicateAMBomMatl">The found duplicate AMBomMatl row</param>
        /// <param name="inventoryItem">Inventory item row of newly entered inventory ID (from field verifying)</param>
        /// <param name="setupCheck">BOM Setup duplicate setup option indicating warning or error</param>
        protected virtual void DuplicateEntryMessage(PXFieldVerifyingEventArgs e, AMBomMatl duplicateAMBomMatl, InventoryItem inventoryItem, string setupCheck)
        {
            if (duplicateAMBomMatl == null ||
                duplicateAMBomMatl.InventoryID == null ||
                inventoryItem == null ||
                string.IsNullOrWhiteSpace(setupCheck))
            {
                return;
            }

            var operBomValue = (AMBomOper)PXSelect<AMBomOper, Where<AMBomOper.bOMID, Equal<Required<AMBomOper.bOMID>>,
                            And<AMBomOper.operationID, Equal<Required<AMBomOper.operationID>>>>>
                            .Select(this, duplicateAMBomMatl.BOMID, duplicateAMBomMatl.OperationID);

            var userMessage = Messages.GetLocal(Messages.EcrMatlDupItems, operBomValue?.OperationCD, operBomValue?.BOMID);            

            switch (setupCheck)
            {
                case SetupMessage.WarningMsg:
                    WebDialogResult response = BomMatlRecords.Ask(
                        Messages.Warning,
                        $"{userMessage} {Messages.GetLocal(Messages.Continue)}?",
                        MessageButtons.YesNo);

                    if (response != WebDialogResult.Yes)
                    {
                        e.NewValue = inventoryItem.InventoryCD;
                        e.Cancel = true;
                        throw new PXSetPropertyException(userMessage, PXErrorLevel.Error);
                    }
                    break;
                case SetupMessage.ErrorMsg:
                    e.NewValue = inventoryItem.InventoryCD;
                    e.Cancel = true;
                    throw new PXSetPropertyException(userMessage, PXErrorLevel.Error);
            }
        }

        #endregion

        protected virtual void AMBomTool_ToolID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            var row = (AMBomTool)e.Row;
            if (row == null)
            {
                return;
            }

            var toolMst = (AMToolMst)PXSelectorAttribute.Select<AMBomTool.toolID>(cache, row);

            row.Descr = toolMst?.Descr;
            row.UnitCost = toolMst?.UnitCost ?? 0m;
        }


		#region Button - Copy Bom
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomToECR(PXCache ecrItemCache, AMECRItem ecr, AMBomItem sourceBOM)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void DeleteAllCurrentECRDetail(AMECRItem ecrItem)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomOper(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomMatl(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomStep(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomRef(AMBomItem sourceBOM, string newBOMID, string newRevisionID)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomTool(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomOvhd(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected virtual void CopyBomAttributes(AMBomItem sourceBOM, string newBOMID, string newRevisionID)
		{
			throw new NotImplementedException();
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2 + " Moved to ECCBaseGraph")]
		protected bool IsValidBom(string bomId, string revisionId)
		{
			throw new NotImplementedException();
		}
		#endregion

		protected virtual void EnableECRItemFields(PXCache cache, AMECRItem item)
        {

			if (!cache.IsRowInserted(item))
			{
				PXUIFieldAttribute.SetEnabled<AMECRItem.bOMID>(cache, item, false);
				PXUIFieldAttribute.SetEnabled<AMECRItem.bOMRevisionID>(cache, item, false);
			}

			if (item == null || item.Hold.GetValueOrDefault())
            {
                return;
            }

            bool isCompleted = (item.Status == AMECRStatus.Completed);

            PXUIFieldAttribute.SetEnabled(cache, item, false);
            PXUIFieldAttribute.SetEnabled<AMECRItem.eCRID>(cache, item, true);
        }

        //We get field name cannot be empty but no indication to which DAC, so we add this for improved error reporting
        public override int Persist(Type cacheType, PXDBOperation operation)
        {
            try
            {
				return base.Persist(cacheType, operation);
            }
            catch (Exception e)
            {
                PXTrace.WriteError($"Persist; cacheType = {cacheType.Name}; operation = {Enum.GetName(typeof(PXDBOperation), operation)}; {e.Message}");
#if DEBUG
                AMDebug.TraceWriteMethodName($"Persist; cacheType = {cacheType.Name}; operation = {Enum.GetName(typeof(PXDBOperation), operation)}; {e.Message}");
#endif
                throw;
            }
        }

        public static string BOMRevItemDisplay(string bomid, string rev, string invtid)
        {
            var display = "";
            if (!string.IsNullOrEmpty(bomid))
            {
                display += bomid.Trim();
                if (!string.IsNullOrEmpty(rev))
                {
                    display += " - " + rev.Trim();
                }
                if (!string.IsNullOrEmpty(invtid))
                {
                    display += ", " + invtid.Trim();
                }
            }
            return display;
        }
    }

	public class ECRMaintECCExt : ECCBaseGraph<ECRMaint, AMECRItem>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.manufacturingECC>();

		protected override string ECCRev => AMECRItem.ECRRev;

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<ECRMaint, AMECRItem>();
			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add<ECRMaintECCExt>(g => g.BOMCompare, c => c.WithCategory(PredefinedCategory.Inquiries));
					});
			});
		}
	}
}
