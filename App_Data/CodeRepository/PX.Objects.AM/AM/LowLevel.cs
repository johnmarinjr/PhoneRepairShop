using System;
using System.Collections.Generic;
using PX.Objects.AM.Attributes;
using PX.Data;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.CS;
using PX.Objects.IN;
using System.Linq;

namespace PX.Objects.AM
{
    public class LowLevel : PXGraph<LowLevel>
    {
        // 2021R2 swap primary with LowLevelInventoryItem
        public PXSelect<InventoryItem> InventoryItemRecs;
        public PXSetup<AMBSetup> BomSetup;

        // MAIN CACHE USED FOR UPDATING LOWLEVEL VALUE
        [PXHidden]
        public PXSelect<LowLevelInventoryItem> LowLevelInventoryItemRecs;

        //Required as a workaround to AEF InventoryItemExt updates - Acumatica case 031594
        public PXSetup<INSetup> InvSetup;
        public PXSetup<CommonSetup> CSetup;
        
        /// <summary>
        /// Number of levels found
        /// </summary>
        public int CurrentMaxLowLevel;
        /// <summary>
        /// Was the process skipped (no boms changed from last run)
        /// </summary>
        public bool ProcessLevelsSkipped;
        public const int MaxLowLevel = 25;
        protected const int MaxNumberOfErrors = 50;
        private List<int> _updateErrorItemsList;
        private int _currentNumberOfErrors;

        /// <summary>
        /// Keeps track of all item low levels to call one DB update at the end of set all
        /// </summary>
        private Dictionary<int, int> _lowLevelDictionary;


        public LowLevel()
        {
            _updateErrorItemsList = new List<int>();
            _lowLevelDictionary = new Dictionary<int, int>();
            CurrentMaxLowLevel = 0;
            ProcessLevelsSkipped = false;

            InventoryItemRecs.AllowDelete = false;
            InventoryItemRecs.AllowInsert = false;

            LowLevelInventoryItemRecs.AllowDelete = false;
            LowLevelInventoryItemRecs.AllowInsert = false;
        }

        public static LowLevel Construct()
        {
            return CreateInstance<LowLevel>();
        }

        /// <summary>
        /// Determine if BOM data has changed. If not then no need to recalc low levels
        /// </summary>
        /// <param name="graph">calling graph</param>
        /// <param name="fromDateTime">Date and time to check from for bom changes</param>
        /// <returns></returns>
        public static bool BomDataChanged(PXGraph graph, DateTime? fromDateTime)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (fromDateTime == null)
            {
                return true;
            }

            var bomItemAdded = (AMBomItem)PXSelect<AMBomItem,
                Where<AMBomItem.createdDateTime, GreaterEqual<Required<AMBomItem.createdDateTime>>>>
               .SelectWindowed(graph, 0, 1, fromDateTime);

            if (bomItemAdded?.BOMID != null)
            {
                return true;
            }
            // Joining AMBomItem as ECC reuses BOM Matl tables and changes to ECC should not impact low level logic
            var bomMatlAddedUpdated = (AMBomMatl)PXSelectJoin<
                    AMBomMatl,
                    InnerJoin<AMBomItem, On<AMBomMatl.bOMID, Equal<AMBomItem.bOMID>>>,
                    Where<AMBomMatl.lastModifiedDateTime, GreaterEqual<Required<AMBomMatl.lastModifiedDateTime>>>>
                .SelectWindowed(graph, 0, 1, fromDateTime);

            return bomMatlAddedUpdated?.BOMID != null;
        }

        #region Dictionary Methods

        protected void UpdateLowLevelDictionary(int? key, int? value)
        {
            if (key.GetValueOrDefault() == 0 || value.GetValueOrDefault() == 0)
            {
                return;
            }

            if (_lowLevelDictionary.ContainsKey(key.GetValueOrDefault()))
            {
                _lowLevelDictionary.Remove(key.GetValueOrDefault());
            }

            _lowLevelDictionary.Add(key.GetValueOrDefault(), value.GetValueOrDefault());
        }

        protected int GetLowLevelDictionaryValue(int? key)
        {
            return _lowLevelDictionary.TryGetValue(key.GetValueOrDefault(), out var lowLevelReturn) ? lowLevelReturn : 0;
        }
        #endregion

		protected virtual bool UpdateInventoryItem(int inventoryID, int lowLevel)
		{
			return PXDatabase.Update<InventoryItem>(
                new PXDataFieldAssign<InventoryItemExt.aMLowLevel>(PXDbType.Int, lowLevel),
				new PXDataFieldRestrict<INLotSerialStatus.inventoryID>(PXDbType.Int, 4, inventoryID, PXComp.EQ));
		}

		[Obsolete("Use UpdateInventoryItem(int, int)")]
        protected virtual void UpdateInventoryItem(LowLevelInventoryItem row, bool persistRow)
        {
            if (row?.InventoryID == null)
            {
                return;
            }

            var currentItemCd = row.InventoryCD ?? string.Empty;
            var currentItemId = row.InventoryID ?? 0;

            try
            {
                if (persistRow)
                {
                    LowLevelInventoryItemRecs.Cache.Persist(row, PXDBOperation.Update);
                }
                else
                {
                    LowLevelInventoryItemRecs.Cache.Update(row);
                }
            }
            catch (PXUnitConversionException unitException)
            {
                //These occur in the standard demo database and have nothing to do with this process.
                _currentNumberOfErrors++;

                if (currentItemId != 0 && !_updateErrorItemsList.Contains(currentItemId))
                {
                    _updateErrorItemsList.Add(currentItemId);

                    var msg = Messages.GetLocal(Messages.LowLevelUnableToUpdateItem, currentItemCd, unitException.Message);
                    PXTrace.WriteWarning(msg);
#if DEBUG
                    AMDebug.TraceWriteMethodName(msg);
#endif
                }
            }
            catch (PXOuterException e)
            {
                _currentNumberOfErrors++;

                PXTraceHelper.PxTraceOuterException(e, PXTraceHelper.ErrorLevel.Warning);
            }
            catch (Exception e)
            {
                _currentNumberOfErrors++;

                if(currentItemId != 0 && !_updateErrorItemsList.Contains(currentItemId))
                {
                    _updateErrorItemsList.Add(currentItemId);
                }

                if (string.IsNullOrWhiteSpace(currentItemCd))
                {
                    currentItemCd = $"ID:{currentItemId}";
                }

                var msg = Messages.GetLocal(Messages.LowLevelUnableToUpdateItem, currentItemCd, e.Message);
                PXTrace.WriteWarning(msg);
#if DEBUG
                AMDebug.TraceWriteMethodName(msg);
#endif
            }

            CheckForMaxErrorsReached();
        }

        protected virtual void CheckForMaxErrorsReached()
        {
            if (_currentNumberOfErrors >= MaxNumberOfErrors)
            {
                throw new PXException(Messages.LowLevelMaxErrorsReceived);
            }
        }

		[Obsolete("Use PersistDictionary()")]
        protected virtual void PersistDictionary(bool persistEachRow)
		{
		}

        protected virtual void PersistDictionary()
        {
			foreach(var kv in _lowLevelDictionary)
			{
				var inventoryID = kv.Key;
				var newLowLevel = kv.Value;

				if (newLowLevel >= MaxLowLevel)
                {
					var item = (BomInventoryItemSimple)this.Caches[typeof(BomInventoryItemSimple)].Locate(new BomInventoryItemSimple { InventoryID = inventoryID });
					if(item != null)
					{
						// to help in troubleshooting items related to circular reference
						PXTrace.WriteInformation(Messages.GetLocal(Messages.LowLevelMaxLevelReachedForItem, item.InventoryCD.TrimIfNotNullEmpty(), item.AMLowLevel.GetValueOrDefault()));
					}
                }

				UpdateInventoryItem(inventoryID, newLowLevel);
			}
        }

        /// <summary>
        /// Persist with a retry for each row vs first attempt at mass update.
        /// This exists due to various customer item table error that exist before this process runs preventing the update from occurring.
        /// </summary>
        protected virtual void PersistDictionaryWithRetry()
        {
            int retryCount = 1;
            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    PersistDictionary();
                    retry = retryCount;
                }
                catch
                {
                    if (retry >= retryCount)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Sets Low Level for all Inventory Id's
        /// </summary>
        public virtual void SetAll()
        {
            var lastLowLevelDateTime = BomSetup?.Current?.LastLowLevelCompletedDateTime;
            if (BomDataChanged(this, lastLowLevelDateTime))
            {
                ProcessAllLevels();
                ProcessLevelsSkipped = false;
                return;
            }
            ProcessLevelsSkipped = true;
            CurrentMaxLowLevel = BomSetup?.Current?.LastMaxLowLevel ?? 0;
            PXTrace.WriteInformation($"No bom changes found from {lastLowLevelDateTime}. Low level process skipped");
        }

        protected virtual void ResetAllLowLevels()
        {
            PXDatabase.Update<InventoryItem>(
                new PXDataFieldAssign<InventoryItemExt.aMLowLevel>(PXDbType.Int, 0));
        }

		[Obsolete]
        protected virtual List<BomInventoryItem> GetInventory()
        {
            return PXSelect<BomInventoryItem>.Select(this).ToFirstTableList();
        }

		protected virtual List<BomInventoryItemSimple> GetInventorySimple()
        {
            return PXSelect<BomInventoryItemSimple>.Select(this).ToFirstTableList();
        }

        protected virtual void ProcessAllLevels()
        {
            _updateErrorItemsList = new List<int>();
            _lowLevelDictionary = new Dictionary<int, int>();

            CurrentMaxLowLevel = 0;
            _currentNumberOfErrors = 0;

			// We could get rid of this reset if then on delete of either AMBomMatl or AMBomItem we reset the last low level data and made those related items set back to InventoryItem.AMLowLevel to zero.
            ResetAllLowLevels();

			var inventoryItemParents = GetInventorySimple();

			if (inventoryItemParents == null || inventoryItemParents.Count == 0)
            {
                return;
            }

			var bomMatlBomItemQueryResults = new Dictionary<int, int[]>();

			// First loop pings the database for results where we will then store the results for each further loop to calculate each lower level
			foreach (var inventoryItemParent in inventoryItemParents)
            {
				this.Caches[typeof(BomInventoryItemSimple)].Hold(inventoryItemParent);

                int currentlevel = GetLowLevelDictionaryValue(inventoryItemParent.InventoryID);
				var currentMaterial = new HashSet<int>();

				// uses index: AMBomItem_InventoryID_Status
				foreach (LowLevelBomMatlBomItem row in PXSelectReadonly<LowLevelBomMatlBomItem,
					Where<LowLevelBomMatlBomItem.itemInventoryID, Equal<Required<LowLevelBomMatlBomItem.itemInventoryID>>>>
					.Select(this, inventoryItemParent.InventoryID))
                {
					currentMaterial.Add(row.MatlInventoryID.GetValueOrDefault());
					ProcessBomMaterialLowLevel(row.MatlInventoryID, currentlevel);
                }

				if (currentMaterial.Count > 0)
                {
					bomMatlBomItemQueryResults.Add(inventoryItemParent.InventoryID.GetValueOrDefault(), currentMaterial.ToArray());
                }
            }

			var lowLevel = 1;
			var hasMoreLevels = true;
            while (hasMoreLevels)
            {
                foreach (var inventoryItemParent in inventoryItemParents)
                {
                    int currentlevel = GetLowLevelDictionaryValue(inventoryItemParent.InventoryID);
                    if (currentlevel >= MaxLowLevel)
                    {
                        continue;
                    }

					bomMatlBomItemQueryResults.TryGetValue(inventoryItemParent.InventoryID.GetValueOrDefault(), out var bomMatlInventoryIDs);
					if (bomMatlInventoryIDs == null)
					{
						continue;
					}

					foreach (var matlInventoryID in bomMatlInventoryIDs)
                    {
						ProcessBomMaterialLowLevel(matlInventoryID, currentlevel);
                    }
                }

                lowLevel++;
                if (lowLevel > CurrentMaxLowLevel || lowLevel >= MaxLowLevel)
                {
                    //Either no more levels to process or the max has been reached
                    hasMoreLevels = false;
                }
            }

            PersistDictionaryWithRetry();

            if (CurrentMaxLowLevel >= MaxLowLevel)
            {
                PXTrace.WriteError(Messages.GetLocal(Messages.LowLevelMaxLevelReached, MaxLowLevel));
            }

            UpdateBomSetup();

            Clear();
        }

		private void ProcessBomMaterialLowLevel(int? matlInventoryID, int currentlevel)
		{
			var childLowLevel = GetLowLevelDictionaryValue(matlInventoryID);
            if (childLowLevel <= currentlevel)
            {
                childLowLevel = currentlevel + 1;

                if (childLowLevel > CurrentMaxLowLevel)
                {
                    CurrentMaxLowLevel = childLowLevel;
                }

                UpdateLowLevelDictionary(matlInventoryID, childLowLevel);
            }
		}

        protected virtual void UpdateBomSetup()
        {
            if (BomSetup?.Current == null)
            {
                BomSetup?.Select();
            }

            var setup = BomSetup?.Current;
            if (setup == null)
            {
                return;
            }

            setup.LastLowLevelCompletedDateTime = Common.Dates.Now;
            setup.LastMaxLowLevel = CurrentMaxLowLevel;

            BomSetup.Cache.PersistUpdated(setup);
        }

		[Obsolete]
        [PXProjection(typeof(Select<AMBomMatl>), Persistent = false)]
        [Serializable]
        [PXHidden]
        public class LowLevelBomMatl : IBqlTable
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            [BomID(BqlField = typeof(AMBomMatl.bOMID))]
            public virtual String BOMID { get; set; }
            #endregion
            #region InventoryID
            public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
            [PXDBInt(BqlField = typeof(AMBomMatl.inventoryID))]
            [PXUIField(DisplayName = "Inventory ID")]
            public virtual Int32? InventoryID { get; set; }
            #endregion
        }

		[Obsolete]
        [PXProjection(typeof(Select<AMBomItem>), Persistent = false)]
        [Serializable]
        [PXHidden]
        public class LowLevelBomItem : IBqlTable
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            [BomID(BqlField = typeof(AMBomItem.bOMID))]
            public virtual String BOMID { get; set; }
            #endregion
            #region InventoryID
            public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
            [PXDBInt(BqlField = typeof(AMBomItem.inventoryID))]
            [PXUIField(DisplayName = "Inventory ID")]
            public virtual Int32? InventoryID { get; set; }
            #endregion
        }

		[PXProjection(typeof(Select2<AMBomMatl,
			InnerJoin<AMBomItem, On<AMBomMatl.bOMID, Equal<AMBomItem.bOMID>>>,
			Where<AMBomItem.status, NotEqual<AMBomStatus.archived>>>), Persistent = false)]
        [Serializable]
        [PXHidden]
        public class LowLevelBomMatlBomItem : IBqlTable
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            [BomID(BqlField = typeof(AMBomMatl.bOMID))]
            public virtual String BOMID { get; set; }
            #endregion
            #region MatlInventoryID
            public abstract class matlInventoryID : PX.Data.BQL.BqlInt.Field<matlInventoryID> { }
            [PXDBInt(BqlField = typeof(AMBomMatl.inventoryID))]
            [PXUIField(DisplayName = "Inventory ID")]
            public virtual Int32? MatlInventoryID { get; set; }
            #endregion

			#region ItemInventoryID
            public abstract class itemInventoryID : PX.Data.BQL.BqlInt.Field<itemInventoryID> { }
            [PXDBInt(BqlField = typeof(AMBomItem.inventoryID))]
            [PXUIField(DisplayName = "Inventory ID")]
            public virtual Int32? ItemInventoryID { get; set; }
            #endregion
        }

        /// <summary>
        /// PXProjection for <see cref="InventoryItem"/> only including the low level field to update
        /// </summary>
        [PXProjection(typeof(Select<InventoryItem>), Persistent = true)]
        [Serializable]
        [PXHidden]
        public class LowLevelInventoryItem : IBqlTable
        {
            #region InventoryID
            public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

            [PXDBInt(BqlField = typeof(InventoryItem.inventoryID), IsKey = true)]
            [PXUIField(DisplayName = "Inventory ID", Enabled = false)]
            public virtual Int32? InventoryID { get; set; }

            #endregion
            #region InventoryCD
            public abstract class inventoryCD : PX.Data.BQL.BqlString.Field<inventoryCD> { }

            [PXDBString(InputMask = "", IsUnicode = true, BqlField = typeof(InventoryItem.inventoryCD))]
            [PXUIField(DisplayName = "Inventory CD", Enabled = false)]
            public virtual String InventoryCD { get; set; }
            #endregion

            #region AMLowLevel
            public abstract class aMLowLevel : PX.Data.BQL.BqlInt.Field<aMLowLevel> { }

            [PXDBInt(BqlField = typeof(InventoryItemExt.aMLowLevel))]
            [PXUIField(DisplayName = "Low Level")]
            public Int32? AMLowLevel { get; set; }
            #endregion
        }
    }
}
