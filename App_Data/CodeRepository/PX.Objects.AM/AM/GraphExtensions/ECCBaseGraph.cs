using PX.Data;
using PX.Objects.AM.Attributes;
using PX.Objects.Common;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AM
{
	public abstract class ECCBaseGraph<TGraph, TPrimary> : PXGraphExtension<TGraph>
		where TPrimary : class, IBqlTable, IECCItem, new()
		where TGraph : PXGraph, new()
	{
		[PXHidden]
        public PXSelect<INItemSite> ItemSiteRecord;

		public override void Initialize()
		{
			base.Initialize();

			Base.RowUpdated.AddHandler<TPrimary>(PrimaryRowUpdated);
			Base.RowSelected.AddHandler<TPrimary>(PrimaryRowSelected);
			Base.FieldVerifying.AddHandler(typeof(TPrimary), nameof(IECCItem.RevisionID), ECCRevisionIDFieldVerifying);
		}

		protected abstract string ECCRev { get; }

		public PXAction<TPrimary> BOMCompare;
        [PXUIField(DisplayName = "Compare BOM", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable bOMCompare(PXAdapter adapter)
        {
            var item = (TPrimary)Base.Caches<TPrimary>().Current;
            if (item != null)
            {
                var graph = PXGraph.CreateInstance<BOMCompareInq>();

				if(item is AMECRItem)
				{
					graph.Filter.Current.IDType1 = BOMCompareInq.IDTypes.ECR;
					graph.Filter.Current.ECRID1 = item.ID;
				}

				if(item is AMECOItem)
				{
					graph.Filter.Current.IDType1 = BOMCompareInq.IDTypes.ECO;
					graph.Filter.Current.ECOID1 = item.ID;
				}

				// required to set bomid also for the compare screen to function
				graph.Filter.Current.BOMID1 = item.ID;
                graph.Filter.Current.RevisionID1 = ECCRev;

                graph.Filter.Current.IDType2 = BOMCompareInq.IDTypes.BOM;
                graph.Filter.Current.BOMID2 = item.BOMID;
                graph.Filter.Current.RevisionID2 = item.BOMRevisionID;
                throw new PXRedirectRequiredException(graph, Messages.BOMCompare);
            }

            return adapter.Get();
        }

		protected virtual void PrimaryRowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var item = (TPrimary)e.Row;
            if (item == null)
            {
                return;
            }

			BOMCompare.SetEnabled(!sender.IsRowInserted(item));
        }

		protected virtual void ECCRevisionIDFieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            var row = (TPrimary)e.Row;
            if(row == null || e.NewValue == null)
            {
                return;
            }
            var bomItem = AMBomItem.PK.Find(Base, row.BOMID, (string)e.NewValue);
            if (bomItem != null)
            {
                e.Cancel = true;
                throw new PXSetPropertyException(Messages.GetLocal(Messages.BomRevisionExists), PXErrorLevel.Error, row.BOMID, e.NewValue);
            }
        }

		protected virtual void PrimaryRowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
        {
            var row = (TPrimary) e.Row;
            if (row?.BOMID == null || row.BOMRevisionID == null)
            {
                return;
            }

			var oldRow = (TPrimary) e.OldRow;
			if (row.BOMID == oldRow?.BOMID && row.BOMRevisionID == oldRow?.BOMRevisionID)
            {
                return;
            }

            CopyBomToECC(cache, row, AMBomItem.PK.Find(Base, row.BOMID, row.BOMRevisionID));
        }

		protected virtual void CopyBomToECC(PXCache cache, TPrimary item, AMBomItem sourceBOM)
        {
            if (sourceBOM?.BOMID == null || item == null)
            {
                return;
            }

            DeleteAllCurrentECCDetail(item);

			cache.SetValueExt(item, nameof(item.RevisionID), AutoNumberHelper.NextNumber(GetMaxRevForBOM(sourceBOM.BOMID)));
			cache.SetValueExt(item, nameof(item.InventoryID), sourceBOM.InventoryID);
            if (sourceBOM.SubItemID != null)
            {
				cache.SetValueExt(item, nameof(item.SubItemID), sourceBOM.SubItemID);
            }
			cache.SetValueExt(item, nameof(item.SiteID), sourceBOM.SiteID);
			cache.SetValueExt(item, nameof(item.Descr), sourceBOM.Descr);
			cache.SetValueExt(item, nameof(item.BOMID), sourceBOM.BOMID);
			cache.SetValueExt(item, nameof(item.BOMRevisionID), sourceBOM.RevisionID);

            PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomItem>(), sourceBOM, cache, item);

            CopyBomOper(sourceBOM, item.ID, ECCRev, true);
            CopyBomMatl(sourceBOM, item.ID, ECCRev, true, item.SiteID);
            CopyBomStep(sourceBOM, item.ID, ECCRev, true);
            CopyBomRef(sourceBOM, item.ID, ECCRev);
            CopyBomTool(sourceBOM, item.ID, ECCRev, true);
            CopyBomOvhd(sourceBOM, item.ID, ECCRev, true);
            CopyBomAttributes(sourceBOM, item.ID, ECCRev);
        }

		protected virtual void DeleteAllCurrentECCDetail(TPrimary item)
        {
            foreach (AMBomAttribute bomAttribute in PXSelect<AMBomAttribute,
                Where<AMBomAttribute.bOMID, Equal<Required<AMBomAttribute.bOMID>>,
                    And<AMBomAttribute.revisionID, Equal<Required<AMBomAttribute.revisionID>>>>
            >.Select(Base, item.ID, ECCRev))
            {
                Base.Caches<AMBomAttribute>().Delete(bomAttribute);
            }

            foreach (AMBomOper bomOper in PXSelect<AMBomOper,
                Where<AMBomOper.bOMID, Equal<Required<AMBomOper.bOMID>>,
                    And<AMBomOper.revisionID, Equal<Required<AMBomOper.revisionID>>>>
            >.Select(Base, item.ID, ECCRev))
            {
                Base.Caches<AMBomOper>().Delete(bomOper);
            }
        }

        protected virtual void CopyBomOper(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
        {
            using (new DisableSelectorValidationScope(Base.Caches<AMBomOper>()))
            {
                var fromRows = PXSelect<AMBomOper,
                Where<AMBomOper.bOMID, Equal<Required<AMBomOper.bOMID>>,
                    And<AMBomOper.revisionID, Equal<Required<AMBomOper.revisionID>>>>
                    >.Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID);

                foreach (AMBomOper fromRow in fromRows)
                {
                    var toRow = PXCache<AMBomOper>.CreateCopy(fromRow);
                    toRow.BOMID = newBOMID;
                    toRow.RevisionID = newRevisionID;
                    toRow.NoteID = null;
                    toRow = (AMBomOper)Base.Caches<AMBomOper>().Insert(toRow);
                    toRow.RowStatus = AMRowStatus.Unchanged;
                    toRow = (AMBomOper)Base.Caches<AMBomOper>().Update(toRow);

                    if (copyNotes)
                    {
                        PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomOper>(), fromRow, Base.Caches<AMBomOper>(), toRow);
                        Base.Caches<AMBomOper>().Update(toRow);
                    }
                }
            }
        }

        protected virtual void CopyBomMatl(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes, int? defaultSiteID)
        {
            foreach (PXResult<AMBomMatl, InventoryItem, AMBomItem, INItemSite> result in PXSelectJoin<
                AMBomMatl,
                InnerJoin<InventoryItem,
                    On<AMBomMatl.inventoryID, Equal<InventoryItem.inventoryID>>,
                InnerJoin<AMBomItem,
                    On<AMBomMatl.bOMID, Equal<AMBomItem.bOMID>,
                    And<AMBomMatl.revisionID, Equal<AMBomItem.revisionID>>>,
                LeftJoin<INItemSite,
                    On<AMBomMatl.inventoryID, Equal<INItemSite.inventoryID>,
                    And<AMBomItem.siteID, Equal<INItemSite.siteID>>>>>>,
                Where<AMBomMatl.bOMID, Equal<Required<AMBomMatl.bOMID>>,
                    And<AMBomMatl.revisionID, Equal<Required<AMBomMatl.revisionID>>,
                    And<Where<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.inactive>,
                        And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.markedForDeletion>>>>>>,
                OrderBy<
                    Asc<AMBomMatl.sortOrder,
                    Asc<AMBomMatl.lineID>>>
                >
                .Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID))
            {
                var fromRow = (AMBomMatl)result;
                var inventoryItem = (InventoryItem)result;

                if (fromRow == null || inventoryItem == null ||
                    fromRow.ExpDate.GetValueOrDefault(Common.Dates.BeginOfTimeDate) != Common.Dates.BeginOfTimeDate
                    && fromRow.ExpDate.GetValueOrDefault() < Base.Accessinfo.BusinessDate.GetValueOrDefault())
                {
                    //no point in copying expired material
                    continue;
                }

                var toRow = PXCache<AMBomMatl>.CreateCopy(fromRow);
                toRow.BOMID = newBOMID;
                toRow.RevisionID = newRevisionID;
                toRow.NoteID = null;

                if (toRow.CompBOMID != null && !IsValidBom(toRow.CompBOMID, toRow.CompBOMRevisionID))
                {
                    toRow.CompBOMID = null;
                    toRow.CompBOMRevisionID = null;
                }

                try
                {
                    toRow = (AMBomMatl)Base.Caches<AMBomMatl>().Insert(toRow);
                    toRow.RowStatus = AMRowStatus.Unchanged;
                    toRow = (AMBomMatl)Base.Caches<AMBomMatl>().Update(toRow);

                    // The result uses the bom siteid, so if material has a site id we still want to call DefaultItemSite
                    var materialItemSite = (INItemSite)result;
                    if (toRow.SiteID != null || materialItemSite == null)
                    {
                        DefaultItemSite(toRow.InventoryID, toRow.SiteID ?? defaultSiteID);
                    }

                    if (copyNotes)
                    {
                        PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomMatl>(), fromRow, Base.Caches<AMBomMatl>(), toRow);
                        Base.Caches<AMBomMatl>().Update(toRow);
                    }
                }
                catch (Exception exception)
                {
                    PXTrace.WriteError(
                            Messages.GetLocal(Messages.UnableToCopyMaterialFromToBomID),
                            inventoryItem?.InventoryCD.TrimIfNotNullEmpty(),
                            fromRow?.BOMID,
                            fromRow?.RevisionID,
                            toRow?.BOMID,
                            toRow?.RevisionID,
                            exception.Message);
                    throw;
                }
            }
        }
        protected virtual void CopyBomStep(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
        {
            var fromRows = PXSelect<AMBomStep,
                Where<AMBomStep.bOMID, Equal<Required<AMBomStep.bOMID>>,
                    And<AMBomStep.revisionID, Equal<Required<AMBomStep.revisionID>>
                    >>>.Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID);

            foreach (AMBomStep fromRow in fromRows)
            {
                var toRow = PXCache<AMBomStep>.CreateCopy(fromRow);
                toRow.BOMID = newBOMID;
                toRow.RevisionID = newRevisionID;
                toRow.NoteID = null;
                toRow = (AMBomStep)Base.Caches<AMBomStep>().Insert(toRow);
                toRow.RowStatus = AMRowStatus.Unchanged;
                toRow = (AMBomStep)Base.Caches<AMBomStep>().Update(toRow);

                if (copyNotes)
                {
                    PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomStep>(), fromRow, Base.Caches<AMBomStep>(), toRow);
                    Base.Caches<AMBomStep>().Update(toRow);
                }
            }
        }
        protected virtual void CopyBomRef(AMBomItem sourceBOM, string newBOMID, string newRevisionID)
        {
            var fromRows = PXSelect<AMBomRef,
                Where<AMBomRef.bOMID, Equal<Required<AMBomRef.bOMID>>,
                    And<AMBomRef.revisionID, Equal<Required<AMBomRef.revisionID>>
                    >>>.Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID);

            foreach (AMBomRef fromRow in fromRows)
            {
                var toRow = PXCache<AMBomRef>.CreateCopy(fromRow);
                toRow.BOMID = newBOMID;
                toRow.RevisionID = newRevisionID;
                toRow.NoteID = null;
                toRow = (AMBomRef)Base.Caches<AMBomRef>().Insert(toRow);
                toRow.RowStatus = AMRowStatus.Unchanged;
                Base.Caches<AMBomRef>().Update(toRow);
            }
        }

        protected virtual void CopyBomTool(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
        {
            var fromRows = PXSelectJoin<AMBomTool,
                InnerJoin<AMToolMst, On<AMBomTool.toolID, Equal<AMToolMst.toolID>>>,
                Where<AMBomTool.bOMID, Equal<Required<AMBomTool.bOMID>>,
                    And<AMBomTool.revisionID, Equal<Required<AMBomTool.revisionID>>
                    >>>.Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID);

            foreach (AMBomTool fromRow in fromRows)
            {
                var toRow = PXCache<AMBomTool>.CreateCopy(fromRow);
                toRow.BOMID = newBOMID;
                toRow.RevisionID = newRevisionID;
                toRow.NoteID = null;
                toRow = (AMBomTool)Base.Caches<AMBomTool>().Insert(toRow);
                toRow.RowStatus = AMRowStatus.Unchanged;
                toRow = (AMBomTool)Base.Caches<AMBomTool>().Update(toRow);

                if (copyNotes)
                {
                    PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomTool>(), fromRow, Base.Caches<AMBomTool>(), toRow);
                    Base.Caches<AMBomTool>().Update(toRow);
                }
            }
        }

        protected virtual void CopyBomOvhd(AMBomItem sourceBOM, string newBOMID, string newRevisionID, bool copyNotes)
        {
            var fromRows = PXSelectJoin<AMBomOvhd,
                InnerJoin<AMOverhead, On<AMBomOvhd.ovhdID, Equal<AMOverhead.ovhdID>>>,
                Where<AMBomOvhd.bOMID, Equal<Required<AMBomOvhd.bOMID>>,
                    And<AMBomOvhd.revisionID, Equal<Required<AMBomOvhd.revisionID>>
                    >>>.Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID);

            foreach (AMBomOvhd fromRow in fromRows)
            {
                var toRow = PXCache<AMBomOvhd>.CreateCopy(fromRow);
                toRow.BOMID = newBOMID;
                toRow.RevisionID = newRevisionID;
                toRow.NoteID = null;
                toRow = (AMBomOvhd)Base.Caches<AMBomOvhd>().Insert(toRow);
                toRow.RowStatus = AMRowStatus.Unchanged;
                toRow = (AMBomOvhd)Base.Caches<AMBomOvhd>().Update(toRow);

                if (copyNotes)
                {
                    PXNoteAttribute.CopyNoteAndFiles(Base.Caches<AMBomOvhd>(), fromRow, Base.Caches<AMBomOvhd>(), toRow);
                    Base.Caches<AMBomOvhd>().Update(toRow);
                }
            }
        }

        protected virtual void CopyBomAttributes(AMBomItem sourceBOM, string newBOMID, string newRevisionID)
        {
            Base.FieldVerifying.AddHandler<AMBomAttribute.operationID>((sender, e) => { e.Cancel = true; });

            foreach (PXResult<AMBomAttribute, AMBomOper> result in PXSelectJoin<AMBomAttribute,
                    LeftJoin<AMBomOper, On<AMBomAttribute.bOMID, Equal<AMBomOper.bOMID>,
                            And<AMBomAttribute.revisionID, Equal<AMBomOper.revisionID>,
                        And<AMBomAttribute.operationID, Equal<AMBomOper.operationID>>>>>,
                Where<AMBomAttribute.bOMID, Equal<Required<AMBomAttribute.bOMID>>,
                    And<AMBomAttribute.revisionID, Equal<Required<AMBomAttribute.revisionID>>>>>
                .Select(Base, sourceBOM.BOMID, sourceBOM.RevisionID))
            {
                var fromBomAttribute = (AMBomAttribute)result;
                var fromBomAttOper = (AMBomOper)result;

                int? newOperationId = null;
                if (fromBomAttOper?.OperationCD != null)
                {
                    var newOperation = FindInsertedBomOperByCd(fromBomAttOper.OperationCD);
                    if (newOperation?.OperationID == null)
                    {
                        continue;
                    }

                    newOperationId = newOperation.OperationID;
                }

                var newBomAtt = PXCache<AMBomAttribute>.CreateCopy(fromBomAttribute);
                newBomAtt.BOMID = newBOMID;
                newBomAtt.RevisionID = newRevisionID;
                newBomAtt.OperationID = newOperationId;

                var insertedAttribute = (AMBomAttribute)Base.Caches<AMBomAttribute>().Insert(newBomAtt);
                if (insertedAttribute != null)
                {
                    insertedAttribute.RowStatus = AMRowStatus.Unchanged;
                    Base.Caches<AMBomAttribute>().Update(insertedAttribute);
                    continue;
                }

                PXTrace.WriteWarning($"Unable to copy {Common.Cache.GetCacheName(typeof(AMBomAttribute))} from ({fromBomAttribute.BOMID};{fromBomAttribute.RevisionID};{fromBomAttribute.LineNbr})");
#if DEBUG
                AMDebug.TraceWriteMethodName($"Unable to copy {Common.Cache.GetCacheName(typeof(AMBomAttribute))} from ({fromBomAttribute.BOMID};{fromBomAttribute.RevisionID};{fromBomAttribute.LineNbr})");
#endif
            }
        }

        private AMBomOper FindInsertedBomOperByCd(string operationCd)
        {
			return Base.Caches<AMBomOper>()?.Inserted.RowCast<AMBomOper>()
                ?.Where(r => r.OperationCD == operationCd)
                ?.First();
        }

		public virtual string GetMaxRevForBOM(string bomid)
        {
            List<string> list = new List<string>();

            AMBomItem item = PXSelectGroupBy<AMBomItem, Where<AMBomItem.bOMID, Equal<Required<AMBomItem.bOMID>>>,
                Aggregate<Max<AMBomItem.revisionID>>>.Select(Base, bomid);
            list.Add(item != null ? item.RevisionID : "");

            AMECRItem ecr = PXSelectGroupBy<AMECRItem, Where<AMECRItem.bOMID, Equal<Required<AMECRItem.bOMID>>>,
                Aggregate<Max<AMECRItem.revisionID>>>.Select(Base, bomid);
            list.Add(ecr != null ? ecr.RevisionID : "");

            AMECOItem eco = PXSelectGroupBy<AMECOItem, Where<AMECOItem.bOMID, Equal<Required<AMECOItem.bOMID>>>,
            Aggregate<Max<AMECOItem.revisionID>>>.Select(Base, bomid);
            list.Add(eco != null ? eco.RevisionID : "");

            return list.Max();
        }

		protected bool IsValidBom(string bomId, string revisionId)
        {
            if (string.IsNullOrWhiteSpace(bomId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(revisionId))
            {
                return (AMBomItem)PXSelect<
                    AMBomItem,
                    Where<AMBomItem.bOMID, Equal<Required<AMBomItem.bOMID>>,
                        And<AMBomItem.status, NotEqual<AMBomStatus.archived>>>>
                    .SelectWindowed(Base, 0, 1, bomId) != null;
            }

            return (AMBomItem)PXSelect<AMBomItem,
                    Where<AMBomItem.bOMID, Equal<Required<AMBomItem.bOMID>>,
                        And<AMBomItem.revisionID, Equal<Required<AMBomItem.revisionID>>,
                        And<AMBomItem.status, NotEqual<AMBomStatus.archived>>>>>
                .SelectWindowed(Base, 0, 1, bomId, revisionId) != null;
        }

        /// <summary>
        /// Create an INItemSite record if one doesn't exist for the bom item/site
        /// </summary>
        protected virtual void DefaultItemSite(int? inventoryID, int? siteID)
        {
            if (inventoryID == null || siteID == null || !InventoryHelper.MultiWarehousesFeatureEnabled)
            {
                return;
            }

            if (InventoryHelper.MakeItemSiteByItem(Base, inventoryID, siteID, out var inItemSite))
            {
                INItemSite itemSite = ItemSiteRecord.Locate(inItemSite);
                if (itemSite == null)
                {
                    ItemSiteRecord.Insert(inItemSite);
                }
            }
        }
	}
}
