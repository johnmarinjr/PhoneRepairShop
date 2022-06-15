using System;
using System.Collections.Generic;
using System.Text;
using PX.Objects.AM.GraphExtensions;
using PX.Objects.AM.Attributes;
using PX.Common;
using PX.Data;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using VendorLocation = PX.Objects.CR.Standalone.Location;
using PX.Data.BQL.Fluent;

namespace PX.Objects.AM
{
    /// <summary>
    /// Cache inventory and related values used throughout the MRP process
    /// </summary>
    public class MRPProcessCache
    {
        public MrpItemDictionary MrpItems;
        public DetailSupply DetailSupply;
        public CalendarHelper PurchaseCalendar;
        public readonly int MaxLowLevel;

        public MRPProcessCache(int maxLowLevel)
        {
            MrpItems = new MrpItemDictionary();
            DetailSupply = new DetailSupply();
            MaxLowLevel = maxLowLevel;
        }

        /// <summary>
        /// Only necessary when sub item is enabled. load sub item level replenishment settings from POVendorInventory and INItemSiteReplenishment
        /// </summary>
        /// <param name="mrpEngine">Calling MRP Graph</param>
        /// <param name="itemSite">base projection for inventory and item warehouse</param>
        /// <param name="defaultSubItemID">Sub item ID already used to get default POVendorInventory record to exclude in next POVendorInventory records lookup</param>
        /// <param name="useSafetyStock">Indicator to use safety stock or reorder point as the Manufacturing safety stock value</param>
        /// <returns></returns>
        private static Dictionary<int, ReplenishmentSourceCache> BuildItemSiteReplenishmentSourceDictionary(MRPEngine mrpEngine, MRPInventory itemSite, int? defaultSubItemID, bool useSafetyStock)
        {
            var subItemRepSourceDictionary = new Dictionary<int, ReplenishmentSourceCache>();

            if (mrpEngine == null || itemSite?.SiteID == null)
            {
                return subItemRepSourceDictionary;
            }

            // Vendor inventory per sub item
            if (itemSite.SitePreferredVendorID != null)
            {
                foreach (POVendorInventory poVendorInventory in PXSelect<POVendorInventory,
                    Where<POVendorInventory.vendorID, Equal<Required<POVendorInventory.vendorID>>,
                        And<POVendorInventory.inventoryID, Equal<Required<POVendorInventory.inventoryID>>,
                            And<POVendorInventory.subItemID, NotEqual<Required<POVendorInventory.subItemID>>>>>>.Select(
                            mrpEngine, itemSite.SitePreferredVendorID, itemSite.InventoryID, defaultSubItemID.GetValueOrDefault()))
                {

                    //Not ideal as a sub query but for now only for those subitem customers
                    var bomIDManager = new PrimaryBomIDManager(mrpEngine)
                    {
                        BOMIDType = PrimaryBomIDManager.BomIDType.Planning
                    };
                    var bomID = bomIDManager.GetItemSitePrimary(itemSite.InventoryID, itemSite.SiteID, poVendorInventory.SubItemID);
                    var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomID);

                    var replenishmentCache = new ReplenishmentSourceCache
                    {
                        InventoryID = poVendorInventory.InventoryID.GetValueOrDefault(),
                        SiteID = itemSite.SiteID.GetValueOrDefault(),
                        SubItemID = poVendorInventory.SubItemID.GetValueOrDefault(),
                        ReplenishmentSource = itemSite.ReplenishmentSource,
                        UseSafetyStock = useSafetyStock,
                        SafetyStock = itemSite.SafetyStock.GetValueOrDefault(),
                        ReorderPoint = itemSite.MinQty.GetValueOrDefault(),
                        LeadTime = poVendorInventory.VLeadTime.GetValueOrDefault() + poVendorInventory.AddLeadTimeDays.GetValueOrDefault(),
                        LotSize = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory, 
                            poVendorInventory.LotSize.GetValueOrDefault()),
                        MinOrderQty = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory,
                            poVendorInventory.MinOrdQty.GetValueOrDefault()),
                        MaxOrderQty = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory,
                            poVendorInventory.MaxOrdQty.GetValueOrDefault()),
                        BomID = bomItem?.BOMID,
                        BomRevisionID = bomItem?.RevisionID,
                        BomStartDate = bomItem?.EffStartDate,
                        BomEndDate = bomItem?.EffEndDate
                    };

                    if (!subItemRepSourceDictionary.ContainsKey(replenishmentCache.SubItemID))
                    {
                        subItemRepSourceDictionary.Add(replenishmentCache.SubItemID, replenishmentCache);
                    }
                }
            }

            // Item warehouse replenishment values per sub item
            if (itemSite.SubItemOverride ?? false)
            {
                foreach (INItemSiteReplenishment inItemSiteReplenishment in PXSelect<INItemSiteReplenishment,
                    Where<INItemSiteReplenishment.siteID, Equal<Required<INItemSiteReplenishment.siteID>>,
                        And<INItemSiteReplenishment.inventoryID, Equal<Required<INItemSiteReplenishment.inventoryID>>>>>
                    .Select(mrpEngine, itemSite.SiteID, itemSite.InventoryID))
                {
                    ReplenishmentSourceCache replenishmentCache = null;
                    if (
                        subItemRepSourceDictionary.TryGetValue(
                            inItemSiteReplenishment.SubItemID.GetValueOrDefault(), out replenishmentCache))
                    {
                        replenishmentCache.UseSafetyStock = useSafetyStock;
                        replenishmentCache.SafetyStock = inItemSiteReplenishment.SafetyStock.GetValueOrDefault();
                        replenishmentCache.ReorderPoint = inItemSiteReplenishment.MinQty.GetValueOrDefault();

                        subItemRepSourceDictionary.Remove(replenishmentCache.SubItemID);
                    }

                    if (replenishmentCache == null)
                    {
                        replenishmentCache = new ReplenishmentSourceCache
                        {
                            InventoryID = inItemSiteReplenishment.InventoryID.GetValueOrDefault(),
                            SiteID = inItemSiteReplenishment.SiteID.GetValueOrDefault(),
                            SubItemID = inItemSiteReplenishment.SubItemID.GetValueOrDefault(),
                            ReplenishmentSource = itemSite.ReplenishmentSource,
                            UseSafetyStock = useSafetyStock,
                            SafetyStock = inItemSiteReplenishment.SafetyStock.GetValueOrDefault(),
                            ReorderPoint = inItemSiteReplenishment.MinQty.GetValueOrDefault()
                        };
                    }

                    if (string.IsNullOrWhiteSpace(replenishmentCache.BomID))
                    {
                        //Not ideal as a sub query but for now only for those subitem customers
                        var bomIDManager = new PrimaryBomIDManager(mrpEngine)
                        {
                            BOMIDType = PrimaryBomIDManager.BomIDType.Planning
                        };
                        var bomId = bomIDManager.GetItemSitePrimary(itemSite.InventoryID, itemSite.SiteID, replenishmentCache.SubItemID);
                        var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomId);
                        replenishmentCache.BomID = bomItem?.BOMID;
                        replenishmentCache.BomRevisionID = bomItem?.RevisionID;
                        replenishmentCache.BomStartDate = bomItem?.EffStartDate;
                        replenishmentCache.BomEndDate = bomItem?.EffEndDate;
                    }

                    if (!subItemRepSourceDictionary.ContainsKey(replenishmentCache.SubItemID))
                    {
                        subItemRepSourceDictionary.Add(replenishmentCache.SubItemID, replenishmentCache);
                    }
                }
            }

            foreach (AMSubItemDefault amSubItemDefault in PXSelect<AMSubItemDefault,
                    Where<AMSubItemDefault.siteID, Equal<Required<AMSubItemDefault.siteID>>,
                        And<AMSubItemDefault.inventoryID, Equal<Required<AMSubItemDefault.inventoryID>>>>>
                    .Select(mrpEngine, itemSite.SiteID, itemSite.InventoryID))
            {
                ReplenishmentSourceCache replenishmentCache = null;
                if (
                    subItemRepSourceDictionary.TryGetValue(
                        amSubItemDefault.SubItemID.GetValueOrDefault(), out replenishmentCache))
                {
                    if (string.IsNullOrWhiteSpace(replenishmentCache.BomID))
                    {
                        var bomIDManager = new PrimaryBomIDManager(mrpEngine)
                        {
                            BOMIDType = PrimaryBomIDManager.BomIDType.Planning
                        };
                        var bomId = bomIDManager.GetItemSitePrimary(itemSite.InventoryID, itemSite.SiteID, replenishmentCache.SubItemID);
                        var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomId);
                        replenishmentCache.BomID = bomItem?.BOMID;
                        replenishmentCache.BomRevisionID = bomItem?.RevisionID;
                    }

                    subItemRepSourceDictionary.Remove(replenishmentCache.SubItemID);
                }

                if (replenishmentCache == null)
                {
                    var bomIDManager = new PrimaryBomIDManager(mrpEngine)
                    {
                        BOMIDType = PrimaryBomIDManager.BomIDType.Planning
                    };
                    var bomId = bomIDManager.GetItemSitePrimary(itemSite.InventoryID, itemSite.SiteID, amSubItemDefault.SubItemID, true);
                    var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomId);

                    replenishmentCache = new ReplenishmentSourceCache
                    {
                        InventoryID = amSubItemDefault.InventoryID.GetValueOrDefault(),
                        SiteID = amSubItemDefault.SiteID.GetValueOrDefault(),
                        SubItemID = amSubItemDefault.SubItemID.GetValueOrDefault(),
                        ReplenishmentSource = itemSite.ReplenishmentSource,
                        UseSafetyStock = useSafetyStock,
                        SafetyStock = itemSite.SafetyStock.GetValueOrDefault(),
                        ReorderPoint = itemSite.MinQty.GetValueOrDefault(),
                        LeadTime = 0,
                        LotSize = 0,
                        MinOrderQty = 0,
                        MaxOrderQty = 0,
                        BomID = bomItem?.BOMID,
                        BomRevisionID = bomItem?.RevisionID,
                        BomStartDate = bomItem?.EffStartDate,
                        BomEndDate = bomItem?.EffStartDate
                    };
                }

                if (string.IsNullOrWhiteSpace(replenishmentCache.BomID))
                {
                    //Not ideal as a sub query but for now only for those subitem customers
                    var bomIDManager = new PrimaryBomIDManager(mrpEngine)
                    {
                        BOMIDType = PrimaryBomIDManager.BomIDType.Planning
                    };
                    var bomId = bomIDManager.GetItemSitePrimary(itemSite.InventoryID, itemSite.SiteID, replenishmentCache.SubItemID, true);
                    var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomId);
                    replenishmentCache.BomID = bomItem?.BOMID;
                    replenishmentCache.BomRevisionID = bomItem?.RevisionID;
                    replenishmentCache.BomStartDate = bomItem?.EffStartDate;
                    replenishmentCache.BomEndDate = bomItem?.EffEndDate;
                }

                if (!subItemRepSourceDictionary.ContainsKey(replenishmentCache.SubItemID))
                {
                    subItemRepSourceDictionary.Add(replenishmentCache.SubItemID, replenishmentCache);
                }
            }

            return subItemRepSourceDictionary;
        }

        /// <summary>
        /// Cache item replenishment source values
        /// This is necessary because the INItemRep is multiple per item. This defines it 1 to 1 for first item replenishment and per sub item
        /// </summary>
        private static Dictionary<int, Dictionary<int, ReplenishmentSourceCache>> BuildItemReplenishmentSourceDictionary(MRPEngine mrpEngine, bool useSafetyStock)
        {
            var itemRepSourceDictionary = new Dictionary<int, Dictionary<int, ReplenishmentSourceCache>>();
            var subItemRepSourceDictionary = new Dictionary<int, ReplenishmentSourceCache>();

            int previousItem = 0;
            string previousReplenishmentClass = string.Empty;

#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
            PXResultset<INItemRep> resultset = PXSelectJoin<INItemRep,
                InnerJoin<InventoryItem, On<INItemRep.inventoryID, Equal<InventoryItem.inventoryID>,
                        And<INItemRep.replenishmentSource, Equal<InventoryItemExt.aMReplenishmentSource>>>,
                    LeftJoin<INSubItemRep, On<INItemRep.inventoryID, Equal<INSubItemRep.inventoryID>,
                        And<INItemRep.replenishmentClassID, Equal<INSubItemRep.replenishmentClassID>>>>>,
                Where<InventoryItem.stkItem, Equal<boolTrue>,
                    And2<Where<InventoryItemExt.aMMRPItem, Equal<True>,
                        Or<InventoryItemExt.aMMRPItem, IsNull>>,
                        And<Where<INItemRep.terminationDate, Greater<Current<AccessInfo.businessDate>>,
                            Or<INItemRep.terminationDate, IsNull>>>>>,
                OrderBy<Asc<INItemRep.inventoryID>>>.Select(mrpEngine);

            int lastRow = resultset.Count;
            int counter = 0;
            foreach (PXResult<INItemRep, InventoryItem, INSubItemRep> result in resultset)
            {
                counter++;

                var itemRep = (INItemRep) result;
                var subItemRep = (INSubItemRep) result;
                var inventoryItem = (InventoryItem) result;

                if (itemRep == null || itemRep.InventoryID == null)
                {
                    continue;
                }

                var inventoryExt = inventoryItem.GetExtension<InventoryItemExt>();

                // Find the first item rep record per item/sub item and ignore all others
                if (counter > 1
                    && (( previousItem != itemRep.InventoryID ) 
                    || ( previousItem == itemRep.InventoryID && previousReplenishmentClass != itemRep.ReplenishmentClassID )))
                {
                    if (!itemRepSourceDictionary.ContainsKey(previousItem) && subItemRepSourceDictionary.Count == 0)
                    {
                        itemRepSourceDictionary.Add(previousItem, subItemRepSourceDictionary);
                    }

                    subItemRepSourceDictionary = new Dictionary<int, ReplenishmentSourceCache>();

                    if (previousItem == itemRep.InventoryID &&
                        (subItemRep.InventoryID == null || subItemRep.SubItemID == null))
                    {
                        previousItem = itemRep.InventoryID ?? 0;
                        previousReplenishmentClass = itemRep.ReplenishmentClassID;
                        continue;
                    }
                }

                var replenishmentCache = new ReplenishmentSourceCache()
                {
                    InventoryID = itemRep.InventoryID.GetValueOrDefault(),
                    SiteID = 0,
                    SubItemID = 0,
                    ReplenishmentSource = itemRep.ReplenishmentSource,
                    UseSafetyStock = useSafetyStock,
                    SafetyStock = inventoryExt != null && inventoryExt.AMSafetyStockOverride.GetValueOrDefault()
                        ? inventoryExt.AMSafetyStock.GetValueOrDefault()
                        : itemRep.SafetyStock.GetValueOrDefault(),
                    ReorderPoint = inventoryExt != null && inventoryExt.AMMinQtyOverride.GetValueOrDefault()
                            ? inventoryExt.AMMinQty.GetValueOrDefault()
                            : itemRep.MinQty.GetValueOrDefault()
                };

                if (subItemRep != null && subItemRep.InventoryID != null && subItemRep.SubItemID != null)
                {
                    //sub item replenishment
                    replenishmentCache.SubItemID = subItemRep.SubItemID.GetValueOrDefault();
                    replenishmentCache.UseSafetyStock = useSafetyStock;
                    replenishmentCache.SafetyStock = subItemRep.SafetyStock.GetValueOrDefault();
                    replenishmentCache.ReorderPoint = subItemRep.MinQty.GetValueOrDefault();
                }

                if (!subItemRepSourceDictionary.ContainsKey(replenishmentCache.SubItemID))
                {
                    subItemRepSourceDictionary.Add(replenishmentCache.SubItemID, replenishmentCache);
                }

                previousItem = itemRep.InventoryID.GetValueOrDefault();
                previousReplenishmentClass = itemRep.ReplenishmentClassID;

                if (counter == lastRow && !itemRepSourceDictionary.ContainsKey(previousItem))
                {
                    itemRepSourceDictionary.Add(previousItem, subItemRepSourceDictionary);
                }
            }
#if DEBUG
            sw.Stop();
            PXTraceHelper.WriteTimespan(sw.Elapsed, "MRPProcessCache.BuildItemReplenishmentSourceDictionary");
#endif

            return itemRepSourceDictionary;
        }

		/// <summary>
		/// Determines which POVendorInventory record to use
		/// </summary>
		/// <param name="defaultVendorInventory">The item default vendor record</param>
		/// <param name="itemSite">Item warehouse record</param>
		/// <param name="siteVendorInventory">item warehouse referenced vendor inventory record</param>
		/// <returns>POVendorInventory record to use for MRP Processing for the given item warehouse</returns>
		[Obsolete]
		protected static POVendorInventory UseVendorInventory(POVendorInventory defaultVendorInventory, INItemSite itemSite, POVendorInventory siteVendorInventory)
		{
			return UseVendorInventory(defaultVendorInventory, siteVendorInventory, itemSite?.PreferredVendorOverride == true);
		}

		/// <summary>
		/// Determines which POVendorInventory record to use
		/// </summary>
		/// <param name="defaultVendorInventory">The item default vendor record</param>
		/// <param name="siteVendorInventory">item warehouse referenced vendor inventory record</param>
		/// <param name="sitePreferredVendorOverride">Is item warehouse record have override preferred vendor</param>
		/// <returns>POVendorInventory record to use for MRP Processing for the given item warehouse</returns>
		protected static POVendorInventory UseVendorInventory(POVendorInventory defaultVendorInventory, POVendorInventory siteVendorInventory, bool sitePreferredVendorOverride)
		{
			POVendorInventory defaultVI = defaultVendorInventory != null &&
												 (defaultVendorInventory.VendorID.GetValueOrDefault() == 0 || !defaultVendorInventory.Active.GetValueOrDefault())
				? null
				: defaultVendorInventory;

			if (sitePreferredVendorOverride
				&& siteVendorInventory?.VendorID != null
				&& siteVendorInventory.Active.GetValueOrDefault())
			{
				return siteVendorInventory;
			}

			return defaultVI;
		}

		/// <summary>
		/// Create a dictionary of all item and itemsite values cached for use during the MRP process and reduce nested/repeat queries
		/// </summary>
		public static MrpItemDictionary BuildMrpItemDictionary(MRPEngine mrpEngine, bool subItemEnabled)
        {
            var itemDictionary = new MrpItemDictionary {SubItemEnabled = subItemEnabled, UseSafetyStock = mrpEngine.Setup.Current.StockingMethod == AMRPSetup.MRPStockingMethod.SafetyStock};

            bool useSafetyStock = mrpEngine.Setup.Current.StockingMethod == AMRPSetup.MRPStockingMethod.SafetyStock;

            //  cache replenishment sources per item
            Dictionary<int, Dictionary<int, ReplenishmentSourceCache>> itemReplenishmentSourceDictionary = BuildItemReplenishmentSourceDictionary(mrpEngine, useSafetyStock);

            //Query sourced from replaced stored procedure "AMMInvGet"
            InventoryCache inventoryCache = null;

            LoadItemSiteDetails(mrpEngine, ref itemDictionary, ref inventoryCache, itemReplenishmentSourceDictionary, subItemEnabled);
            LoadItemWithoutItemSiteDetails(mrpEngine, ref itemDictionary, ref inventoryCache, itemReplenishmentSourceDictionary, subItemEnabled);

            //this covers the last record
            itemDictionary.Add(inventoryCache);

#if DEBUG
            AMDebug.TraceWriteMethodName(string.Format("{0} Inventory Items added to cache", itemDictionary.Count()));
#endif
            return itemDictionary;
        }

        private static IMRPCacheBomItem SelectCachebomItem(IMRPCacheBomItem b1, IMRPCacheBomItem b2)
        {
            return b1?.BOMID == null ? b2 : b1;
        }

        /// <summary>
        /// Load items with item site details
        /// </summary>
        private static void LoadItemSiteDetails(MRPEngine mrpEngine, ref MrpItemDictionary itemDictionary, ref InventoryCache inventoryCache,
            Dictionary<int, Dictionary<int, ReplenishmentSourceCache>> itemReplenishmentSourceDictionary, bool subItemEnabled)
        {
#if DEBUG
            var sb = new StringBuilder();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var isFirst = true;
#endif
            foreach (PXResult<INItemSiteProjection, BomItemActiveRevision, BomItemActiveRevision2, BomItemActiveRevision3, BomItemActiveRevision4> result in
            SelectFrom<INItemSiteProjection>.                
                LeftJoin<BomItemActiveRevision>.On<INItemSiteProjection.siteAMBOMID.IsEqual<BomItemActiveRevision.bOMID>>.
                LeftJoin<BomItemActiveRevision2>.On<INItemSiteProjection.siteAMPlanningBOMID.IsEqual<BomItemActiveRevision2.bOMID>>.
                LeftJoin<BomItemActiveRevision3>.On<INItemSiteProjection.itemAMBOMID.IsEqual<BomItemActiveRevision3.bOMID>>.
                LeftJoin<BomItemActiveRevision4>.On<INItemSiteProjection.itemAMPlanningBOMID.IsEqual<BomItemActiveRevision4.bOMID>>.
                OrderBy<INItemSiteProjection.inventoryID.Asc, INItemSiteProjection.siteID.Asc>.
                View.Select(mrpEngine))
            {
#if DEBUG
                if (isFirst)
                {
                    sb.Append(PXTraceHelper.CreateTimespanMessage(sw.Elapsed, "MRPProcessCache.BuildMrpItemDictionary (Executing Query: Load Item Site Details) "));
                    isFirst = false;
                }
#endif
				var itemSiteProj = result.GetItem<INItemSiteProjection>();
				if (itemSiteProj?.InventoryID == null)
				{
					continue;
				}

				var poVendorInventoryDefault = new POVendorInventory()
				{
					InventoryID = itemSiteProj.InventoryID,
					VendorID = itemSiteProj.ItemPreferredVendorID,
					Active = itemSiteProj.VendorActive,
					SubItemID = itemSiteProj.VendorSubItemID,
					VLeadTime = itemSiteProj.VendorVLeadTime,
					AddLeadTimeDays = itemSiteProj.AddLeadTimeDays,
					LotSize = itemSiteProj.LotSize,
					MinOrdQty = itemSiteProj.MinOrdQty,
					MaxOrdQty = itemSiteProj.MaxOrdQty
				};

				POVendorInventory poVendorInventorySite = itemSiteProj.SitePreferredVendorID != null
					&& itemSiteProj.SitePreferredVendorID.GetValueOrDefault() == itemSiteProj.ItemPreferredVendorID.GetValueOrDefault()
					&& itemSiteProj.SitePreferredVendorLocationID.GetValueOrDefault() == itemSiteProj.ItemPreferredVendorLocationID.GetValueOrDefault() ? poVendorInventoryDefault : null;
				if (poVendorInventorySite == null)
                {
                    //handle itemsite with a different default vendor
                    poVendorInventorySite = GetPOVendorInventory(mrpEngine, itemSiteProj.InventoryID, itemSiteProj.SitePreferredVendorID, itemSiteProj.SitePreferredVendorLocationID, itemSiteProj.DefaultSubItemID);
				}

                try
                {
                    // switching from planning to regular default bom
                    var itemSiteBom = SelectCachebomItem((BomItemActiveRevision)result, (BomItemActiveRevision2)result);
                    var inventoryBom = SelectCachebomItem((BomItemActiveRevision3)result, (BomItemActiveRevision4)result);

                    BuildDictionary(mrpEngine, ref itemDictionary, ref inventoryCache, itemReplenishmentSourceDictionary,
                        subItemEnabled, itemSiteProj, inventoryBom, itemSiteBom, UseVendorInventory(poVendorInventoryDefault, poVendorInventorySite, itemSiteProj.PreferredVendorOverride.GetValueOrDefault()));
                }
                catch (Exception e)
                {
                    INSite site = PXSelect<INSite, Where<INSite.siteID, Equal<Required<INSite.siteID>>>>.Select(mrpEngine, itemSiteProj.SiteID);

                    var errMsg = AM.Messages.GetLocal(AM.Messages.MRPErrorCachingItemWithWarehouse,
                        itemSiteProj.InventoryCD.TrimIfNotNullEmpty(),
                        site?.SiteCD.TrimIfNotNullEmpty());
                    throw new MRPRegenException(errMsg, e);
                }
            }

#if DEBUG
            sb.Append(PXTraceHelper.CreateTimespanMessage(sw.Elapsed, "Process Complete"));
            AMDebug.TraceWriteMethodName(sb.ToString());
#endif
        }

        /// <summary>
        /// Load items without item site details
        /// </summary>
        private static void LoadItemWithoutItemSiteDetails(MRPEngine mrpEngine, ref MrpItemDictionary itemDictionary, ref InventoryCache inventoryCache,
            Dictionary<int, Dictionary<int, ReplenishmentSourceCache>> itemReplenishmentSourceDictionary, bool subItemEnabled)
        {
#if DEBUG
            var sb = new StringBuilder();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isFirst = true;
#endif
			 foreach (PXResult<InventoryProjection, BomItemActiveRevision, BomItemActiveRevision2> result in
				SelectFrom<InventoryProjection>.                
					LeftJoin<BomItemActiveRevision>.
						On<InventoryProjection.itemAMBOMID.IsEqual<BomItemActiveRevision.bOMID>>.
					LeftJoin<BomItemActiveRevision2>.
						On<InventoryProjection.itemAMPlanningBOMID.IsEqual<BomItemActiveRevision2.bOMID>>.
					OrderBy<INItemSiteProjection.inventoryID.Asc, INItemSiteProjection.siteID.Asc>.
					View.Select(mrpEngine))
            {
#if DEBUG
                if (isFirst)
                {
                    sb.Append(PXTraceHelper.CreateTimespanMessage(sw.Elapsed, "MRPProcessCache.BuildMrpItemDictionary (Executing Query: Items without item warehouse details) "));
                    isFirst = false;
                }
#endif
                var inventory = result.GetItem<InventoryProjection>();
				if (inventory?.InventoryID == null)
				{
					continue;
				}

                try
                {
                    // switching from planning to regular default bom
                    var inventoryBomRev = SelectCachebomItem((BomItemActiveRevision)result, (BomItemActiveRevision2)result);

                    BuildDictionary(mrpEngine, ref itemDictionary, ref inventoryCache, itemReplenishmentSourceDictionary,
                        subItemEnabled, inventory, inventoryBomRev, null, null);
                }
                catch (Exception e)
                {
                    var errMsg = AM.Messages.GetLocal(AM.Messages.MRPErrorCachingItemWithoutWarehouse, inventory.InventoryCD.TrimIfNotNullEmpty());
                    throw new MRPRegenException(errMsg, e);
                }
            }

#if DEBUG
            sb.Append(PXTraceHelper.CreateTimespanMessage(sw.Elapsed, "Process Complete"));
            AMDebug.TraceWriteMethodName(sb.ToString());
#endif
        }

        private static POVendorInventory GetPOVendorInventory(MRPEngine mrpEngine, int? inventoryID, int? preferredVendorID, int? preferredVendorLocationID, int? subItemID)
        {
            if (inventoryID == null || preferredVendorID == null)
            {
                return null;
            }

            return PXSelect<POVendorInventory, 
                Where<POVendorInventory.inventoryID, Equal<Required<POVendorInventory.inventoryID>>,
                    And<POVendorInventory.vendorID, Equal<Required<POVendorInventory.vendorID>>,
                    And<POVendorInventory.vendorLocationID, Equal<Required<POVendorInventory.vendorLocationID>>,
                    And<IsNull<POVendorInventory.subItemID, int0>, Equal<Required<POVendorInventory.subItemID>>>>>>
                        >.SelectWindowed(mrpEngine, 0, 1, 
                        inventoryID, preferredVendorID, preferredVendorLocationID, subItemID.GetValueOrDefault());
        }

		private static void BuildDictionary(MRPEngine mrpEngine, ref MrpItemDictionary itemDictionary,
            ref InventoryCache inventoryCache,
            Dictionary<int, Dictionary<int, ReplenishmentSourceCache>> itemReplishmentSourceDictionary,
            bool subItemEnabled, MRPInventory inventory, IMRPCacheBomItem inventoryItemBom, IMRPCacheBomItem inItemSiteBom, POVendorInventory poVendorInventory)
        {
            var useSafetyStock = mrpEngine.Setup.Current.StockingMethod == AMRPSetup.MRPStockingMethod.SafetyStock;
            
            if (inventory?.InventoryID == null)
            {
                return;
            }

            if (inventoryCache == null || inventoryCache.InventoryID != inventory.InventoryID.GetValueOrDefault())
            {
                // -----------------------
                //  New item 
                //      add the last item to the dictionary and create a new item cache record.
                itemDictionary.Add(inventoryCache);
                // -----------------------

                inventoryCache = SetInventoryCache(inventory);
                inventoryCache.BomRevisionID = inventoryItemBom?.RevisionID;
                inventoryCache.BomStartDate = inventoryItemBom?.EffStartDate;
                inventoryCache.BomEndDate = inventoryItemBom?.EffEndDate;
                if (inventoryCache.BomRevisionID == null)
                {
                    // no bom revision most likely indicates no active rev for bomid, so lets remove to prevent issues later
                    inventoryCache.BomID = null;
                }

                Dictionary<int, ReplenishmentSourceCache> repCache = null;
                if (itemReplishmentSourceDictionary.TryGetValue(inventoryCache.InventoryID, out repCache))
                {
                    inventoryCache = SetInventoryCache(repCache, inventoryCache);
                }
                if (string.IsNullOrWhiteSpace(inventoryCache.ReplenishmentSource))
                {
                    // Default as None. If a source is not manufactured it is treated as purchased
                    inventoryCache.ReplenishmentSource = INReplenishmentSource.None;
                }
            }

            if (inventory.SiteID == null)
            {
#if DEBUG
                var dm = $"inventoryItem [{inventoryCache.InventoryID}] {inventory.InventoryCD} <no warehouse> add to cache";
                if (inventoryCache.ItemStatus == INItemStatus.Inactive ||
                    inventoryCache.ItemStatus == INItemStatus.ToDelete)
                {
                    dm = $"{dm} [Inactive/ToDelete status]";
                }
                AMDebug.TraceWriteMethodName(dm);
#endif
                // we are processing an item without any warehouse detail
                return;
            }

            if (inventoryCache.ItemSiteCacheDictionary.ContainsKey(inventory.SiteID.GetValueOrDefault()))
            {
                //it is possible to have multiple POVendorInventory records for the same item (different Purchase Units)
                // If we make it here we already processed the first record
                return;
            }

            var itemSiteCache = SetItemSiteCache(inventory);
            itemSiteCache.DefaultSubItemID = inventoryCache.DefaultSubItemID;
            itemSiteCache.BomRevisionID = inItemSiteBom?.RevisionID;
            itemSiteCache.BomStartDate = inItemSiteBom?.EffStartDate;
            itemSiteCache.BomEndDate = inItemSiteBom?.EffEndDate;
            if (itemSiteCache.BomRevisionID == null)
            {
                // no bom revision most likely indicates no active rev for bomid, so lets remove to prevent issues later
                itemSiteCache.BomID = null;
            }

            //Replenishment Source
            if (string.IsNullOrWhiteSpace(itemSiteCache.ReplenishmentSource) &&
                !string.IsNullOrWhiteSpace(inventoryCache.ReplenishmentSource))
            {
                itemSiteCache.ReplenishmentSource = inventoryCache.ReplenishmentSource;
            }

            //Sub Item Override set when each subitem
            if (subItemEnabled)
            {
                //Not ideal as a sub query but for now only for those subitem customers
                var bomId = new PrimaryBomIDManager(mrpEngine).GetItemSitePrimary(inventory.InventoryID, inventory.SiteID, itemSiteCache.DefaultSubItemID);

                var subItemDefault = poVendorInventory == null
                    ? inventoryCache.DefaultSubItemID
                    : poVendorInventory.SubItemID;
                // this loads subitem level replenishments
                var itemSiteReplishmentSourceDictionary = BuildItemSiteReplenishmentSourceDictionary(mrpEngine,
                    inventory, subItemDefault, useSafetyStock);
                itemSiteCache = SetItemSiteCache(itemSiteReplishmentSourceDictionary, itemSiteCache, inventoryCache.DefaultSubItemID);

                if (!string.IsNullOrWhiteSpace(bomId))
                {
                    var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(mrpEngine, bomId);
                    itemSiteCache.BomID = bomItem?.BOMID;
                    itemSiteCache.BomRevisionID = bomItem?.RevisionID;
                    itemSiteCache.BomStartDate = bomItem?.EffStartDate;
                    itemSiteCache.BomEndDate = bomItem?.EffEndDate;
                }
            }

            itemSiteCache.UseSafetyStock = useSafetyStock;
            itemSiteCache.SafetyStock = inventory.SafetyStockOverride.GetValueOrDefault()
                ? inventory.SafetyStock.GetValueOrDefault()
                : (inventory.SafetyStock.GetValueOrDefault() != 0
                    ? inventory.SafetyStock.GetValueOrDefault()
                    : inventoryCache.SafetyStock);
            itemSiteCache.ReorderPoint = inventory.MinQtyOverride.GetValueOrDefault()
                ? inventory.MinQty.GetValueOrDefault()
                : (inventory.MinQty.GetValueOrDefault() != 0
                    ? inventory.MinQty.GetValueOrDefault()
                    : inventoryCache.SafetyStock);

            //non manufactured values (assumed source other than manufactured is a type of purchased)
            if (itemSiteCache.ReplenishmentSource != INReplenishmentSource.Manufactured)
            {
                if (inventory.BAccountID != null)
                {
                    // Make sure we at least get the vendor lead time if no specific vendor inventory record exists
                    itemSiteCache.LeadTime = inventory.VendorVLeadTime.GetValueOrDefault();
                }

                //if a primary vendor is found use its values for the following.
                if (poVendorInventory?.InventoryID != null)
                {
                    itemSiteCache.LeadTime = poVendorInventory.VLeadTime.GetValueOrDefault() + poVendorInventory.AddLeadTimeDays.GetValueOrDefault();
                    itemSiteCache.LotSize = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory,
                        poVendorInventory.LotSize.GetValueOrDefault());
                    itemSiteCache.MinOrderQty = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory,
                        poVendorInventory.MinOrdQty.GetValueOrDefault());
                    itemSiteCache.MaxOrderQty = PX.Objects.AM.InventoryHelper.ConvertToBaseUnits(mrpEngine, poVendorInventory,
                        poVendorInventory.MaxOrdQty.GetValueOrDefault());
                }

                if (inventoryCache.ReplenishmentSource == itemSiteCache.ReplenishmentSource)
                {
                    //  set inventory Cache to have the max/min values when not manufactured.
                    //  If the inventory values are used its a better starting point before getting to the itemsite values
                    if (itemSiteCache.LeadTime > inventoryCache.LeadTime)
                    {
                        inventoryCache.LeadTime = itemSiteCache.LeadTime;
                    }

                    if (itemSiteCache.MaxOrderQty > inventoryCache.MaxOrderQty)
                    {
                        inventoryCache.MaxOrderQty = itemSiteCache.MaxOrderQty;
                    }

                    if (itemSiteCache.MinOrderQty < inventoryCache.MinOrderQty)
                    {
                        inventoryCache.MinOrderQty = itemSiteCache.MinOrderQty;
                    }
                }
            }

            //Product Manager
            if (itemSiteCache.ProductManagerID == null && inventoryCache.ProductManagerID != null)
            {
                itemSiteCache.ProductManagerID = inventoryCache.ProductManagerID;
            }

#if DEBUG
            if (inventoryCache.ItemStatus == INItemStatus.Inactive ||
                inventoryCache.ItemStatus == INItemStatus.ToDelete)
            {
				var debugMsg = string.Format("[Inactive/ToDelete status] inventoryItem [{0}] {1} warehouse [{2}] add to cache", inventoryCache.InventoryID, inventory.InventoryCD, itemSiteCache.SiteID);
                AMDebug.TraceWriteMethodName(debugMsg);
            }
#endif
            inventoryCache.ItemSiteCacheDictionary.Add(itemSiteCache.SiteID, itemSiteCache);
        }

        /// <summary>
        /// Map InventoryItem to InventoryCache
        /// </summary>
        private static InventoryCache SetInventoryCache(MRPInventory inventory, InventoryCache inventoryCache = null)
        {
            if (inventoryCache == null)
            {
                inventoryCache = new InventoryCache();
            }

            if (inventory?.InventoryID == null)
            {
                return null;
            }

            inventoryCache.InventoryID = inventory.InventoryID.GetValueOrDefault();
            inventoryCache.BaseUnit = inventory.BaseUnit;
            inventoryCache.ItemStatus = inventory.ItemStatus;
            inventoryCache.ProductManagerID = inventory.ProductManagerID;
            inventoryCache.IsKitItem = inventory.KitItem.GetValueOrDefault();
            inventoryCache.DefaultSubItemID = inventory.DefaultSubItemID;
            inventoryCache.ItemClassID = inventory.ItemClassID;
            inventoryCache.BomID = inventory.ItemAMPlanningBOMID ?? inventory.ItemAMBOMID;
            inventoryCache.LowLevel = inventory.AMLowLevel.GetValueOrDefault();
            inventoryCache.LeadTime = inventory.ItemAMMFGLeadTime.GetValueOrDefault();
            inventoryCache.MinOrderQty = inventory.ItemAMMinOrdQty.GetValueOrDefault();
            inventoryCache.MaxOrderQty = inventory.ItemAMMaxOrdQty.GetValueOrDefault();
            inventoryCache.LotSize = inventory.ItemAMLotSize.GetValueOrDefault();
            inventoryCache.QtyRoundUp = inventory.AMQtyRoundUp.GetValueOrDefault();
            inventoryCache.GroupWindow = inventory.AMGroupWindow.GetValueOrDefault();

            return inventoryCache;
        }

        /// <summary>
        /// Map ReplenishmentSourceCache to InventoryCache
        /// </summary>
        private static InventoryCache SetInventoryCache(Dictionary<int, ReplenishmentSourceCache> replenishmentSourceCache, InventoryCache inventoryCache)
        {
            if (replenishmentSourceCache == null || inventoryCache == null)
            {
                return inventoryCache;
            }

            ReplenishmentSourceCache repCache = null;
            if (replenishmentSourceCache.TryGetValue(0, out repCache))
            {
                inventoryCache.ReplenishmentSource = repCache.ReplenishmentSource;
                inventoryCache.SafetyStock = repCache.SafetyStock;
                inventoryCache.ReorderPoint = repCache.ReorderPoint;
            }
            else if (replenishmentSourceCache.Count > 0)
            {
                //get any first for source
                var e = replenishmentSourceCache.GetEnumerator();
                e.MoveNext();
                inventoryCache.ReplenishmentSource = e.Current.Value.ReplenishmentSource;
                inventoryCache.SafetyStock = 0;
                inventoryCache.ReorderPoint = 0;
            }

            inventoryCache.ReplenishmentSourceDictionary = replenishmentSourceCache;

            return inventoryCache;
        }

        private static ItemSiteCache SetItemSiteCache(Dictionary<int, ReplenishmentSourceCache> replenishmentSourceCache, ItemSiteCache itemSiteCache, int? defaultSubItemId = null)
        {
            if (replenishmentSourceCache == null || itemSiteCache == null)
            {
                return itemSiteCache;
            }

            itemSiteCache.DefaultSubItemID = defaultSubItemId;

            ReplenishmentSourceCache repCache = null;
            if (replenishmentSourceCache.TryGetValue(defaultSubItemId.GetValueOrDefault(), out repCache))
            {
                itemSiteCache.DefaultSubItemID = repCache.SubItemID;
                itemSiteCache.LeadTime = repCache.LeadTime;
                itemSiteCache.MinOrderQty = repCache.MinOrderQty;
                itemSiteCache.MaxOrderQty = repCache.MaxOrderQty;
                itemSiteCache.LotSize = repCache.LotSize;
            }

            itemSiteCache.ReplenishmentSourceDictionary = replenishmentSourceCache;

            return itemSiteCache;
        }

        /// <summary>
        /// Map INItemSite to ItemSiteCache
        /// </summary>
        private static ItemSiteCache SetItemSiteCache(MRPInventory inItemSite, ItemSiteCache itemSiteCache = null)
        {
            if (itemSiteCache == null)
            {
                itemSiteCache = new ItemSiteCache();
            }

            if (inItemSite?.InventoryID == null || inItemSite.SiteID == null)
            {
                return null;
            }

            itemSiteCache.InventoryID = inItemSite.InventoryID.GetValueOrDefault();
            itemSiteCache.SiteID = inItemSite.SiteID.GetValueOrDefault();
            itemSiteCache.ReplenishmentSource = inItemSite.ReplenishmentSource;
            itemSiteCache.ReplenishmentClassID = inItemSite.ReplenishmentClassID;
            itemSiteCache.PreferredVendorID = inItemSite.SitePreferredVendorID;
            itemSiteCache.ProductManagerID = inItemSite.ProductManagerID;
            itemSiteCache.BomID = inItemSite.SiteAMPlanningBOMID ?? inItemSite.SiteAMBOMID;
            itemSiteCache.LeadTime = inItemSite.SiteAMMFGLeadTime.GetValueOrDefault();
            itemSiteCache.MinOrderQty = inItemSite.SiteAMMinOrdQty.GetValueOrDefault();
            itemSiteCache.MaxOrderQty = inItemSite.SiteAMMaxOrdQty.GetValueOrDefault();
            itemSiteCache.LotSize = inItemSite.SiteAMLotSize.GetValueOrDefault();
            itemSiteCache.GroupWindow = inItemSite.SiteAMGroupWindow.GetValueOrDefault();

            return itemSiteCache;
        }

        public abstract class InventoryCacheBase
        {
            /// <summary>
            /// KEY
            /// </summary>
            public int InventoryID { get; set; }

            public int? DefaultSubItemID { get; set; }
            public string ItemStatus { get; set; }
            public int LowLevel { get; set; }
            public string BaseUnit { get; set; }
            public string BomID { get; set; }
            public string BomRevisionID { get; set; }
            public DateTime? BomStartDate { get; set; }
            public DateTime? BomEndDate { get; set; }
            public int LeadTime { get; set; }
            public decimal MinOrderQty { get; set; }
            public decimal MaxOrderQty { get; set; }
            public decimal LotSize { get; set; }
            public decimal SafetyStock { get; set; }
            public decimal ReorderPoint { get; set; }
            public int? ProductManagerID { get; set; }
            public string ReplenishmentSource { get; set; }
            public bool IsKitItem { get; set; }
            public bool QtyRoundUp { get; set; }
            public bool IsManufacturedItem { get { return ReplenishmentSource == INReplenishmentSource.Manufactured && !string.IsNullOrWhiteSpace(BomID); } }
            public bool HasReplenishmenSource { get { return ReplenishmentSource != INReplenishmentSource.None; } }
            public int? ItemClassID { get; set; }
			public int GroupWindow { get; set; }
		}

        public class InventoryCache : InventoryCacheBase
        {
            public Dictionary<int, ItemSiteCache> ItemSiteCacheDictionary;
            public Dictionary<int, ReplenishmentSourceCache> ReplenishmentSourceDictionary; 

            public InventoryCache()
            {
                ItemSiteCacheDictionary = new Dictionary<int, ItemSiteCache>();
                ReplenishmentSourceDictionary = new Dictionary<int, ReplenishmentSourceCache>();
            }
        }

        public class ItemSiteCache
        {
            /// <summary>
            /// KEY
            /// </summary>
            public int SiteID { get; set; }
            public int InventoryID { get; set; }
            public int? PreferredVendorID { get; set; }
            public string ReplenishmentClassID { get; set; }
            public decimal LotSize { get; set; }
            public int? DefaultSubItemID { get; set; }
            public string BomID { get; set; }
            public string BomRevisionID { get; set; }
            public DateTime? BomStartDate { get; set; }
            public DateTime? BomEndDate { get; set; }
            public int LeadTime { get; set; }
			public int GroupWindow { get; set; }
			public decimal MinOrderQty { get; set; }
            public decimal MaxOrderQty { get; set; }
            /// <summary>
            /// Indicates if use stock level is for safetystock (true) or reorderpoint (false)
            /// </summary>
            public bool UseSafetyStock { get; set; }
            public decimal SafetyStock { get; set; }
            public decimal ReorderPoint { get; set; }
            public int? ProductManagerID { get; set; }
            public string ReplenishmentSource { get; set; }
            public bool IsManufacturedItem => ReplenishmentSource.TrimIfNotNull() == INReplenishmentSource.Manufactured;
            public bool HasReplenishmentSource => ReplenishmentSource != INReplenishmentSource.None;

            public Dictionary<int, ReplenishmentSourceCache> ReplenishmentSourceDictionary; 

            public ItemSiteCache()
            {
                ReplenishmentSourceDictionary = new Dictionary<int, ReplenishmentSourceCache>();
            }

            public bool IsDateBetweenBomDates(DateTime? date)
            {
                return BomStartDate == null ||
                       date != null && BomStartDate <= date && Common.Dates.IsDateNull(BomEndDate) ||
                       date.BetweenInclusive(BomStartDate, BomEndDate);
            }
        }

        public class ReplenishmentSourceCache
        {
            public int InventoryID;
            public int SiteID;
            public int SubItemID;
            public string ReplenishmentSource;
            public string BomID;
            public string BomRevisionID;
            public DateTime? BomStartDate;
            public DateTime? BomEndDate;
            public bool UseSafetyStock;
            public decimal SafetyStock;
            public decimal ReorderPoint;
            public int LeadTime;
            public decimal MinOrderQty;
            public decimal MaxOrderQty;
            public decimal LotSize;
        }

        [Serializable]
        public sealed class MrpItemDictionary : Dictionary<int, InventoryCache>
        {
            private InventoryCache _currentInventoryCache;
            private ItemSiteCache _currentItemSiteCache;
            private ReplenishmentSourceCache _currentReplenishmentSourceCache;
            public int? LastLoadInventoryID { get; private set; }
            public int? LastLoadSiteID { get; private set; }
            public bool SubItemEnabled;
            public bool UseSafetyStock;

            public void Add(InventoryCache inventoryCache)
            {
                if (inventoryCache != null && inventoryCache.InventoryID > 0)
                {
                    if (this.ContainsKey(inventoryCache.InventoryID))
                    {
                        AMDebug.TraceWriteMethodName(string.Format("Attempt to add existing key {0}",inventoryCache.InventoryID));
                        return;
                    }

                    this.Add(inventoryCache.InventoryID, inventoryCache);
                }
            }

            public static InventoryCache GetInventoryCache(MrpItemDictionary dictionary, int? inventoryID)
            {
                if (dictionary == null
                    || !dictionary.Any_()
                    || (inventoryID ?? 0) <= 0)
                {
                    return null;
                }

                InventoryCache inventoryCache = null;
                if (dictionary.TryGetValue(inventoryID ?? 0, out inventoryCache))
                {
                    return inventoryCache;
                }

                return null;
            }

            public static ItemSiteCache GetItemSiteCache(InventoryCache inventoryCache, int? siteID)
            {
                if (inventoryCache == null
                    || !inventoryCache.ItemSiteCacheDictionary.Any_()
                    || (siteID ?? 0) <= 0)
                {
                    return null;
                }

                ItemSiteCache itemSiteCache = null;
                if (inventoryCache.ItemSiteCacheDictionary.TryGetValue(siteID ?? 0, out itemSiteCache))
                {
                    return itemSiteCache;
                }

                return null;
            }

            public ItemSiteCache GetCurrentItemSiteCache(int? inventoryID, int? siteID = null)
            {
                if (!LoadCurrent(inventoryID, siteID))
                {
                    return null;
                }

                return _currentItemSiteCache;
            }

            public ItemCache GetCurrentItemCache(int? inventoryID, int? siteID = null, int? subItemID = null)
            {
                if (!LoadCurrent(inventoryID, siteID))
                {
                    return null;
                }

                var itemCache = new ItemCache
                {
                    InventoryID = this._currentItemSiteCache.InventoryID,
                    SiteID = this._currentItemSiteCache.SiteID,
                    DefaultSubItemID = this._currentInventoryCache.DefaultSubItemID,
                    BaseUnit = this._currentInventoryCache.BaseUnit,
                    BomID = this._currentItemSiteCache.BomID,
                    BomRevisionID = this._currentItemSiteCache.BomRevisionID,
                    BomStartDate = this._currentItemSiteCache.BomStartDate,
                    BomEndDate = this._currentItemSiteCache.BomEndDate,
                    IsKitItem = this._currentInventoryCache.IsKitItem,
                    ItemStatus = this._currentInventoryCache.ItemStatus,
                    InvalidItemStatus = PX.Objects.AM.InventoryHelper.IsInvalidItemStatus(_currentInventoryCache.ItemStatus),
                    LeadTime = this._currentItemSiteCache.LeadTime,
                    LotSize = this._currentItemSiteCache.LotSize,
                    LowLevel = this._currentInventoryCache.LowLevel,
                    MinOrderQty = this._currentItemSiteCache.MinOrderQty,
                    MaxOrderQty = this._currentItemSiteCache.MaxOrderQty,
                    UseSafetyStock = this._currentItemSiteCache.UseSafetyStock,
                    SafetyStock = this._currentItemSiteCache.SafetyStock,
                    ReorderPoint = this._currentItemSiteCache.ReorderPoint,
                    PreferredVendorID = this._currentItemSiteCache.PreferredVendorID,
                    ReplenishmentClassID = this._currentItemSiteCache.ReplenishmentClassID,
                    ProductManagerID = this._currentItemSiteCache.ProductManagerID,
                    ReplenishmentSource = this._currentItemSiteCache.ReplenishmentSource,
                    QtyRoundUp = this._currentInventoryCache.QtyRoundUp,
                    IemClassID = this._currentInventoryCache.ItemClassID,
                };

				if (itemCache.ReplenishmentSource == INReplenishmentSource.Manufactured && string.IsNullOrWhiteSpace(itemCache.BomID))
				{
					itemCache.BomID = this._currentInventoryCache.BomID;
					itemCache.BomRevisionID = this._currentInventoryCache.BomRevisionID;
				}

				if (SubItemEnabled && LoadCurrentSubItem(subItemID ?? itemCache.DefaultSubItemID))
                {
                    // Sub Item specific replenishment settings
                    itemCache.ReplenishmentSource = _currentReplenishmentSourceCache.ReplenishmentSource;
                    itemCache.SafetyStock = _currentReplenishmentSourceCache.SafetyStock;
                    itemCache.ReorderPoint = _currentReplenishmentSourceCache.ReorderPoint;
                    itemCache.LeadTime = _currentReplenishmentSourceCache.LeadTime == 0 ? itemCache.LeadTime : _currentReplenishmentSourceCache.LeadTime;
                    itemCache.MinOrderQty = _currentReplenishmentSourceCache.MinOrderQty == 0 ? itemCache.MinOrderQty : _currentReplenishmentSourceCache.MinOrderQty;
                    itemCache.MaxOrderQty = _currentReplenishmentSourceCache.MaxOrderQty == 0 ? itemCache.MaxOrderQty : _currentReplenishmentSourceCache.MaxOrderQty;
                    itemCache.LotSize = _currentReplenishmentSourceCache.LotSize == 0 ? itemCache.LotSize : _currentReplenishmentSourceCache.LotSize;
                    itemCache.BomID = _currentReplenishmentSourceCache.BomID;
                    itemCache.BomRevisionID = _currentReplenishmentSourceCache.BomRevisionID;
                    itemCache.BomStartDate = _currentReplenishmentSourceCache.BomStartDate;
                    itemCache.BomEndDate = _currentReplenishmentSourceCache.BomEndDate;
                }

                return itemCache;
            }

            private bool LoadCurrentSubItem(int? subItemID)
            {
                //pull site level first
                if ((_currentReplenishmentSourceCache == null 
                    || _currentReplenishmentSourceCache.InventoryID != _currentItemSiteCache.InventoryID 
                    || _currentReplenishmentSourceCache.SiteID != _currentItemSiteCache.SiteID
                    || _currentReplenishmentSourceCache.SubItemID != subItemID.GetValueOrDefault())
                    && _currentItemSiteCache.ReplenishmentSourceDictionary.Count > 0)
                {
                    _currentReplenishmentSourceCache = null;

                    if (_currentItemSiteCache.ReplenishmentSourceDictionary.TryGetValue(
                            subItemID.GetValueOrDefault(), out _currentReplenishmentSourceCache))
                    {
                        return true;
                    }
                }

                // pull generic item level
                if ((_currentReplenishmentSourceCache == null
                    || _currentReplenishmentSourceCache.InventoryID != _currentInventoryCache.InventoryID
                    || _currentReplenishmentSourceCache.SubItemID != subItemID.GetValueOrDefault())
                    && _currentInventoryCache.ReplenishmentSourceDictionary.Count > 0)
                {
                    _currentReplenishmentSourceCache = null;

                    if (_currentInventoryCache.ReplenishmentSourceDictionary.TryGetValue(
                            subItemID.GetValueOrDefault(), out _currentReplenishmentSourceCache))
                    {
                        return true;
                    }
                }

                return _currentReplenishmentSourceCache != null && _currentReplenishmentSourceCache.InventoryID == _currentInventoryCache.InventoryID;
            }

            private bool LoadCurrent(int? inventoryID, int? siteID = null)
            {
                LastLoadInventoryID = inventoryID;
                LastLoadSiteID = siteID;

                if (inventoryID.GetValueOrDefault() <= 0)
                {
                    _currentInventoryCache = null;
                    _currentItemSiteCache = null;
                    _currentReplenishmentSourceCache = null;
                    return false;
                }

                bool foundItem = true;

                if (_currentInventoryCache == null
                    || _currentInventoryCache.InventoryID != inventoryID)
                {
                    _currentInventoryCache = null;
                    _currentItemSiteCache = null;
                    _currentReplenishmentSourceCache = null;

                    InventoryCache inventoryCache = null;
                    foundItem = this.TryGetValue(inventoryID.GetValueOrDefault(), out inventoryCache);

                    if (foundItem)
                    {
                        _currentInventoryCache = inventoryCache;
                    }
                }

                //either no item cache found or the siteid is invalid/null - done
                if (_currentInventoryCache == null || (siteID ?? 0) <= 0)
                {
                    _currentItemSiteCache = null;
                    return foundItem;
                }

                if (_currentItemSiteCache == null
                    || _currentItemSiteCache.InventoryID != inventoryID
                    || _currentItemSiteCache.SiteID != siteID)
                {
                    ItemSiteCache itemSiteCache = null;
                    _currentItemSiteCache = null;
                    if (_currentInventoryCache.ItemSiteCacheDictionary.TryGetValue(siteID ?? 0, out itemSiteCache))
                    {
                        _currentItemSiteCache = itemSiteCache;
                    }
                    else
                    {
                        //default filler using the item specific settings
                        //  This might happen if there is no INItemSite records yet for the item
                        _currentItemSiteCache = MakeItemSiteCacheFromInventoryCache(_currentInventoryCache);
                        _currentItemSiteCache.SiteID = siteID.GetValueOrDefault(-1);
                        _currentItemSiteCache.UseSafetyStock = this.UseSafetyStock;
                    }
                }

                return foundItem;
            }

            private static ItemSiteCache MakeItemSiteCacheFromInventoryCache(InventoryCache inventoryCache)
            {
                var itemSiteCache = new ItemSiteCache();

                if (inventoryCache == null)
                {
                    return itemSiteCache;
                }

                itemSiteCache.InventoryID = inventoryCache.InventoryID;
                itemSiteCache.DefaultSubItemID = inventoryCache.DefaultSubItemID;
                itemSiteCache.LeadTime = inventoryCache.LeadTime;
                itemSiteCache.ProductManagerID = inventoryCache.ProductManagerID;
                itemSiteCache.BomID = inventoryCache.BomID;
                itemSiteCache.BomRevisionID = inventoryCache.BomRevisionID;
                itemSiteCache.BomStartDate = inventoryCache.BomStartDate;
                itemSiteCache.BomEndDate = inventoryCache.BomEndDate;
                itemSiteCache.SafetyStock = inventoryCache.SafetyStock;
                itemSiteCache.MinOrderQty = inventoryCache.MinOrderQty;
                itemSiteCache.MaxOrderQty = inventoryCache.MaxOrderQty;
                itemSiteCache.LotSize = inventoryCache.LotSize;
                itemSiteCache.ReplenishmentSource = inventoryCache.ReplenishmentSource;

                return itemSiteCache;
            }
        }

        public class ItemCache : ItemSiteCache
        {
            public string ItemStatus { get; set; }
            public bool InvalidItemStatus { get; set; }
            public int LowLevel { get; set; }
            public string BaseUnit { get; set; }
            public bool IsKitItem { get; set; }
            public bool QtyRoundUp { get; set; }
            public int? IemClassID { get; set; }
        }

        /// <summary>
        /// Determine/Load the purchase calendar (Purchase Calendar is Optional)
        /// </summary>
        /// <param name="mrpEngineGraph">Calling MRP Engine Graph</param>
        /// <returns>True if the purchase calendar was loaded</returns>
        public virtual bool LoadPurchaseCalendar(MRPEngine mrpEngineGraph)
        {
            if (mrpEngineGraph.Setup.Current != null
                && !string.IsNullOrWhiteSpace(mrpEngineGraph.Setup.Current.PurchaseCalendarID))
            {
                if (PurchaseCalendar == null
                    || PurchaseCalendar.CurrentCalendarId != mrpEngineGraph.Setup.Current.PurchaseCalendarID)
                {
                    try
                    {
                        PurchaseCalendar = new CalendarHelper(mrpEngineGraph, mrpEngineGraph.Setup.Current.PurchaseCalendarID);
                        //purchase calendar calculates date back from a plan date (to calculate action date)
                        PurchaseCalendar.CalendarReadDirection = ReadDirection.Backward;
                    }
                    catch (Exception exception)
                    {
                        if (exception is InvalidWorkCalendarException)
                        {
                            //if the purchase calendar is no longer valid -> avoid repeat exceptions for each action date calculation
                            mrpEngineGraph.Setup.Current.PurchaseCalendarID = null;
                        }

                        return false;
                    }
                }

                return PurchaseCalendar != null && !string.IsNullOrWhiteSpace(PurchaseCalendar.CurrentCalendarId);
            }

            return false;
        }

        internal interface IMRPCacheBomItem
        {
            string BOMID { get; set; }
            string RevisionID { get; set; }
            DateTime? EffStartDate { get; set; }
            DateTime? EffEndDate { get; set; }
        }

        [PXHidden]
        [Serializable]
        [PXProjection(typeof(Select2<AMBomItem,
            InnerJoin<AMBomItemActiveAggregate,
                On<AMBomItem.bOMID, Equal<AMBomItemActiveAggregate.bOMID>,
                    And<AMBomItem.revisionID, Equal<AMBomItemActiveAggregate.revisionID>>>>>), Persistent = false)]
        public class BomItemActiveRevision : IBqlTable, IMRPCacheBomItem
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            protected string _BOMID;
            [BomID(IsKey = true, BqlField = typeof(AMBomItem.bOMID))]
            public virtual string BOMID
            {
                get
                {
                    return this._BOMID;
                }
                set
                {
                    this._BOMID = value;
                }
            }
            #endregion
            #region RevisionID
            public abstract class revisionID : PX.Data.BQL.BqlString.Field<revisionID> { }
            protected string _RevisionID;
            [RevisionIDField(BqlField = typeof(AMBomItem.revisionID))]
            public virtual string RevisionID
            {
                get
                {
                    return this._RevisionID;
                }
                set
                {
                    this._RevisionID = value;
                }
            }
            #endregion
            #region EffStartDate
            public abstract class effStartDate : PX.Data.BQL.BqlDateTime.Field<effStartDate> { }

            protected DateTime? _EffStartDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effStartDate))]
            [PXUIField(DisplayName = "Start Date")]
            public virtual DateTime? EffStartDate
            {
                get
                {
                    return this._EffStartDate;
                }
                set
                {
                    this._EffStartDate = value;
                }
            }
            #endregion
            #region EffEndDate
            public abstract class effEndDate : PX.Data.BQL.BqlDateTime.Field<effEndDate> { }

            protected DateTime? _EffEndDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effEndDate))]
            [PXUIField(DisplayName = "End Date")]
            public virtual DateTime? EffEndDate
            {
                get
                {
                    return this._EffEndDate;
                }
                set
                {
                    this._EffEndDate = value;
                }
            }
            #endregion
        }

        [PXHidden]
        [Serializable]
        [PXProjection(typeof(Select2<AMBomItem,
            InnerJoin<AMBomItemActiveAggregate,
                On<AMBomItem.bOMID, Equal<AMBomItemActiveAggregate.bOMID>,
                    And<AMBomItem.revisionID, Equal<AMBomItemActiveAggregate.revisionID>>>>>), Persistent = false)]
        public class BomItemActiveRevision2 : IBqlTable, IMRPCacheBomItem
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            protected string _BOMID;
            [BomID(IsKey = true, BqlField = typeof(AMBomItem.bOMID))]
            public virtual string BOMID
            {
                get
                {
                    return this._BOMID;
                }
                set
                {
                    this._BOMID = value;
                }
            }
            #endregion
            #region RevisionID
            public abstract class revisionID : PX.Data.BQL.BqlString.Field<revisionID> { }
            protected string _RevisionID;
            [RevisionIDField(BqlField = typeof(AMBomItem.revisionID))]
            public virtual string RevisionID
            {
                get
                {
                    return this._RevisionID;
                }
                set
                {
                    this._RevisionID = value;
                }
            }
            #endregion
            #region EffStartDate
            public abstract class effStartDate : PX.Data.BQL.BqlDateTime.Field<effStartDate> { }

            protected DateTime? _EffStartDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effStartDate))]
            [PXUIField(DisplayName = "Start Date")]
            public virtual DateTime? EffStartDate
            {
                get
                {
                    return this._EffStartDate;
                }
                set
                {
                    this._EffStartDate = value;
                }
            }
            #endregion
            #region EffEndDate
            public abstract class effEndDate : PX.Data.BQL.BqlDateTime.Field<effEndDate> { }

            protected DateTime? _EffEndDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effEndDate))]
            [PXUIField(DisplayName = "End Date")]
            public virtual DateTime? EffEndDate
            {
                get
                {
                    return this._EffEndDate;
                }
                set
                {
                    this._EffEndDate = value;
                }
            }
            #endregion
        }

        [PXHidden]
        [Serializable]
        [PXProjection(typeof(Select2<AMBomItem,
            InnerJoin<AMBomItemActiveAggregate,
                On<AMBomItem.bOMID, Equal<AMBomItemActiveAggregate.bOMID>,
                    And<AMBomItem.revisionID, Equal<AMBomItemActiveAggregate.revisionID>>>>>), Persistent = false)]
        public class BomItemActiveRevision3 : IBqlTable, IMRPCacheBomItem
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            protected string _BOMID;
            [BomID(IsKey = true, BqlField = typeof(AMBomItem.bOMID))]
            public virtual string BOMID
            {
                get
                {
                    return this._BOMID;
                }
                set
                {
                    this._BOMID = value;
                }
            }
            #endregion
            #region RevisionID
            public abstract class revisionID : PX.Data.BQL.BqlString.Field<revisionID> { }
            protected string _RevisionID;
            [RevisionIDField(BqlField = typeof(AMBomItem.revisionID))]
            public virtual string RevisionID
            {
                get
                {
                    return this._RevisionID;
                }
                set
                {
                    this._RevisionID = value;
                }
            }
            #endregion
            #region EffStartDate
            public abstract class effStartDate : PX.Data.BQL.BqlDateTime.Field<effStartDate> { }

            protected DateTime? _EffStartDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effStartDate))]
            [PXUIField(DisplayName = "Start Date")]
            public virtual DateTime? EffStartDate
            {
                get
                {
                    return this._EffStartDate;
                }
                set
                {
                    this._EffStartDate = value;
                }
            }
            #endregion
            #region EffEndDate
            public abstract class effEndDate : PX.Data.BQL.BqlDateTime.Field<effEndDate> { }

            protected DateTime? _EffEndDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effEndDate))]
            [PXUIField(DisplayName = "End Date")]
            public virtual DateTime? EffEndDate
            {
                get
                {
                    return this._EffEndDate;
                }
                set
                {
                    this._EffEndDate = value;
                }
            }
            #endregion
        }

        [PXHidden]
        [Serializable]
        [PXProjection(typeof(Select2<AMBomItem,
            InnerJoin<AMBomItemActiveAggregate,
                On<AMBomItem.bOMID, Equal<AMBomItemActiveAggregate.bOMID>,
                    And<AMBomItem.revisionID, Equal<AMBomItemActiveAggregate.revisionID>>>>>), Persistent = false)]
        public class BomItemActiveRevision4 : IBqlTable, IMRPCacheBomItem
        {
            #region BOMID
            public abstract class bOMID : PX.Data.BQL.BqlString.Field<bOMID> { }
            protected string _BOMID;
            [BomID(IsKey = true, BqlField = typeof(AMBomItem.bOMID))]
            public virtual string BOMID
            {
                get
                {
                    return this._BOMID;
                }
                set
                {
                    this._BOMID = value;
                }
            }
            #endregion
            #region RevisionID
            public abstract class revisionID : PX.Data.BQL.BqlString.Field<revisionID> { }
            protected string _RevisionID;
            [RevisionIDField(BqlField = typeof(AMBomItem.revisionID))]
            public virtual string RevisionID
            {
                get
                {
                    return this._RevisionID;
                }
                set
                {
                    this._RevisionID = value;
                }
            }
            #endregion
            #region EffStartDate
            public abstract class effStartDate : PX.Data.BQL.BqlDateTime.Field<effStartDate> { }

            protected DateTime? _EffStartDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effStartDate))]
            [PXUIField(DisplayName = "Start Date")]
            public virtual DateTime? EffStartDate
            {
                get
                {
                    return this._EffStartDate;
                }
                set
                {
                    this._EffStartDate = value;
                }
            }
            #endregion
            #region EffEndDate
            public abstract class effEndDate : PX.Data.BQL.BqlDateTime.Field<effEndDate> { }

            protected DateTime? _EffEndDate;
            [PXDBDate(BqlField = typeof(AMBomItem.effEndDate))]
            [PXUIField(DisplayName = "End Date")]
            public virtual DateTime? EffEndDate
            {
                get
                {
                    return this._EffEndDate;
                }
                set
                {
                    this._EffEndDate = value;
                }
            }
            #endregion
        }

		[PXHidden]
		[Serializable]
		[PXProjection(typeof(SelectFrom<INItemSite>.
				InnerJoin<InventoryItem>.On<INItemSite.inventoryID.IsEqual<InventoryItem.inventoryID>>.
				LeftJoin<POVendorInventory>.On<InventoryItem.inventoryID.IsEqual<POVendorInventory.inventoryID>.
					And<InventoryItem.preferredVendorID.IsEqual<POVendorInventory.vendorID>>.
					And<InventoryItem.preferredVendorLocationID.IsEqual<POVendorInventory.vendorLocationID>>.
					And<Where<InventoryItem.defaultSubItemID.IfNullThen<int0>.IsEqual<POVendorInventory.subItemID.IfNullThen<int0>>.
						Or<Not<FeatureInstalled<FeaturesSet.subItem>>>>>>.
				LeftJoin<VendorLocation>.On<InventoryItem.preferredVendorID.IsEqual<VendorLocation.bAccountID>.
					And<InventoryItem.preferredVendorLocationID.IsEqual<VendorLocation.locationID>>>.				
				Where<InventoryItem.stkItem.IsEqual<True>.
					And<Where<InventoryItemExt.aMMRPItem.IsEqual<True>.
						Or<InventoryItemExt.aMMRPItem.IsNull>>>>))]
		public class INItemSiteProjection : MRPInventory, IBqlTable
		{
			// INItemSite  ----------------------------------------------

			#region InventoryID (INItemSite.inventoryID) KEY
			public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
			/// <summary>
			/// INItemSite.inventoryID
			/// </summary>
			[PXDBInt(IsKey = true, BqlField = typeof(INItemSite.inventoryID))]
			public override int? InventoryID { get; set; }
			#endregion
			#region SiteID (INItemSite.siteID) KEY
			public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
			/// <summary>
			/// INItemSite.siteID
			/// </summary>
			[PXDBInt(IsKey = true, BqlField = typeof(INItemSite.siteID))]
			public override int? SiteID { get; set; }
			#endregion
			#region PreferredVendorOverride (INItemSite.preferredVendorOverride)
			public abstract class preferredVendorOverride : PX.Data.BQL.BqlBool.Field<preferredVendorOverride> { }
			/// <summary>
			/// INItemSite.preferredVendorOverride
			/// </summary>
			[PXDBBool(BqlField = typeof(INItemSite.preferredVendorOverride))]
			public override bool? PreferredVendorOverride { get; set; }
			#endregion
			#region SitePreferredVendorID (INItemSite.preferredVendorID)
			public abstract class sitePreferredVendorID : PX.Data.BQL.BqlInt.Field<sitePreferredVendorID> { }
			/// <summary>
			/// INItemSite.preferredVendorID
			/// </summary>
			[PXDBInt(BqlField = typeof(INItemSite.preferredVendorID))]
			public override int? SitePreferredVendorID { get; set; }
			#endregion
			#region SitePreferredVendorLocationID (INItemSite.preferredVendorLocationID)
			public abstract class sitePreferredVendorLocationID : PX.Data.BQL.BqlInt.Field<sitePreferredVendorLocationID> { }
			/// <summary>
			/// INItemSite.preferredVendorLocationID
			/// </summary>
			[PXDBInt(BqlField = typeof(INItemSite.preferredVendorLocationID))]
			public override int? SitePreferredVendorLocationID { get; set; }
			#endregion
			#region ReplenishmentSource (INItemSite.replenishmentSource)
			public abstract class replenishmentSource : PX.Data.BQL.BqlString.Field<replenishmentSource> { }
			/// <summary>
			/// INItemSite.replenishmentSource
			/// </summary>
			[PXDBString(BqlField = typeof(INItemSite.replenishmentSource))]
			public override string ReplenishmentSource { get; set; }
			#endregion
			#region ReplenishmentClassID (INItemSite.replenishmentClassID)
			public abstract class replenishmentClassID : PX.Data.BQL.BqlString.Field<replenishmentClassID> { }
			/// <summary>
			/// INItemSite.replenishmentClassID
			/// </summary>
			[PXDBString(BqlField = typeof(INItemSite.replenishmentClassID))]
			public override string ReplenishmentClassID { get; set; }
			#endregion
			#region ProductManagerID (INItemSite.productManagerID)
			public abstract class productManagerID : PX.Data.BQL.BqlInt.Field<productManagerID> { }
			/// <summary>
			/// INItemSite.productManagerID
			/// </summary>
			[PXDBInt(BqlField = typeof(INItemSite.productManagerID))]
			public override int? ProductManagerID { get; set; }
			#endregion
			#region SafetyStock (INItemSite.safetyStock)
			public abstract class safetyStock : PX.Data.BQL.BqlDecimal.Field<safetyStock> { }
			/// <summary>
			/// INItemSite.safetyStock
			/// </summary>
			[PXDBDecimal(BqlField = typeof(INItemSite.safetyStock))]
			public override decimal? SafetyStock { get; set; }
			#endregion
			#region SafetyStockOverride (INItemSite.safetyStockOverride)
			public abstract class safetyStockOverride : PX.Data.BQL.BqlBool.Field<safetyStockOverride> { }
			/// <summary>
			/// INItemSite.safetyStockOverride
			/// </summary>
			[PXDBBool(BqlField = typeof(INItemSite.safetyStockOverride))]
			public override bool? SafetyStockOverride { get; set; }
			#endregion
			#region MinQty (INItemSite.minQty)
			public abstract class minQty : PX.Data.BQL.BqlDecimal.Field<minQty> { }
			/// <summary>
			/// INItemSite.minQty
			/// </summary>
			[PXDBDecimal(BqlField = typeof(INItemSite.minQty))]
			public override decimal? MinQty { get; set; }
			#endregion
			#region MinQtyOverride (INItemSite.minQtyOverride)
			public abstract class minQtyOverride : PX.Data.BQL.BqlBool.Field<minQtyOverride> { }
			/// <summary>
			/// INItemSite.minQtyOverride
			/// </summary>
			[PXDBBool(BqlField = typeof(INItemSite.minQtyOverride))]
			public override bool? MinQtyOverride { get; set; }
			#endregion
			#region SubItemOverride (INItemSite.subItemOverride)
			public abstract class subItemOverride : PX.Data.BQL.BqlBool.Field<subItemOverride> { }
			/// <summary>
			/// INItemSite.subItemOverride
			/// </summary>
			[PXDBBool(BqlField = typeof(INItemSite.subItemOverride))]
			public override bool? SubItemOverride { get; set; }
			#endregion


			// INItemSiteExt --------------------------------------------

			#region SiteAMBOMID (INItemSiteExt.aMBOMID)
			public abstract class siteAMBOMID : PX.Data.BQL.BqlString.Field<siteAMBOMID> { }
			/// <summary>
			/// INItemSiteExt.aMBOMID
			/// </summary>
			[PXDBString(BqlField = typeof(INItemSiteExt.aMBOMID))]
			public override string SiteAMBOMID { get; set; }
			#endregion
			#region SiteAMPlanningBOMID (INItemSiteExt.aMPlanningBOMID)
			public abstract class siteAMPlanningBOMID : PX.Data.BQL.BqlString.Field<siteAMPlanningBOMID> { }
			/// <summary>
			/// INItemSiteExt.aMPlanningBOMID
			/// </summary>
			[PXDBString(BqlField = typeof(INItemSiteExt.aMPlanningBOMID))]
			public override string SiteAMPlanningBOMID { get; set; }
			#endregion
			#region SiteAMMFGLeadTime (INItemSiteExt.aMMFGLeadTime)
			/// <summary>
			/// INItemSiteExt.AMMFGLeadTime
			/// </summary>
			public abstract class siteAMMFGLeadTime : PX.Data.BQL.BqlInt.Field<siteAMMFGLeadTime> { }
			[PXDBInt(BqlField = typeof(INItemSiteExt.aMMFGLeadTime))]
			[PXUIField(DisplayName = "MFG Lead Time")]
			public override Int32? SiteAMMFGLeadTime { get; set; }
			#endregion
			#region SiteAMLotSize (INItemSiteExt.aMLotSize)
			public abstract class siteAMLotSize : PX.Data.BQL.BqlDecimal.Field<siteAMLotSize> { }
			/// <summary>
			/// INItemSiteExt.AMLotSize
			/// </summary>
			[PXDBQuantity(BqlField = typeof(INItemSiteExt.aMLotSize))]
			[PXUIField(DisplayName = "Lot Size")]
			public override Decimal? SiteAMLotSize { get; set; }
			#endregion
			#region SiteAMMaxOrdQty (INItemSiteExt.aMMaxOrdQty)
			/// <summary>
			/// INItemSiteExt.AMMaxOrdQty
			/// </summary>
			public abstract class siteAMMaxOrdQty : PX.Data.BQL.BqlDecimal.Field<siteAMMaxOrdQty> { }
			[PXDBQuantity(BqlField = typeof(INItemSiteExt.aMMaxOrdQty))]
			[PXUIField(DisplayName = "Max Order Qty")]
			public  override Decimal? SiteAMMaxOrdQty { get; set; }
			#endregion
			#region SiteAMMinOrdQty (INItemSiteExt.aMMinOrdQty)
			/// <summary>
			/// INItemSiteExt.AMMinOrdQty
			/// </summary>
			public abstract class siteAMMinOrdQty : PX.Data.BQL.BqlDecimal.Field<siteAMMinOrdQty> { }
			[PXDBQuantity(BqlField = typeof(INItemSiteExt.aMMinOrdQty))]
			[PXUIField(DisplayName = "Min Order Qty")]
			public override Decimal? SiteAMMinOrdQty { get; set; }
			#endregion
			#region SiteAMGroupWindow (InventoryItemExt.AMGroupWindow)
			public abstract class siteAMGroupWindow : PX.Data.BQL.BqlInt.Field<siteAMGroupWindow> { }
			/// <summary>
			/// InventoryItemExt.AMGroupWindow
			/// </summary>
			[PXDBInt(BqlField = typeof(INItemSiteExt.aMGroupWindow))]
			[PXUIField(DisplayName = "Days Supply")]
			public override Int32? SiteAMGroupWindow { get; set; }
			#endregion

			// InventoryItem  -------------------------------------------
			// as defined in  MRPInventory


			// InventoryItemExt  ----------------------------------------
			// as defined in  MRPInventory


			// POVendorInventory ----------------------------------------
			#region VendorActive (POVendorInventory.active)
			public abstract class vendorActive : PX.Data.BQL.BqlBool.Field<vendorActive> { }
			/// <summary>
			/// POVendorInventory.active
			/// </summary>
			[PXDBBool(BqlField = typeof(POVendorInventory.active))]
			public override bool? VendorActive { get; set; }
			#endregion
			#region VendorSubItemID (POVendorInventory.subItemID)
			public abstract class vendorSubItemID : PX.Data.BQL.BqlInt.Field<vendorSubItemID> { }
			/// <summary>
			/// POVendorInventory.subItemID
			/// </summary>
			[PXDBInt(BqlField = typeof(POVendorInventory.subItemID))]
			public override int? VendorSubItemID { get; set; }
			#endregion
			#region VendorVLeadTime (POVendorInventory.vLeadTime)
			public abstract class vendorVLeadTime : PX.Data.BQL.BqlShort.Field<vendorVLeadTime> { }
			/// <summary>
			/// POVendorInventory.vLeadTime
			/// </summary>
			[PXShort]
			[PXUIField(DisplayName = "Vendor Lead Time (Days)", Enabled=false)]
			[PXDBScalar(typeof(Search<PX.Objects.CR.Standalone.Location.vLeadTime, 
				Where<PX.Objects.CR.Standalone.Location.bAccountID, Equal<POVendorInventory.vendorID>,
				  And<PX.Objects.CR.Standalone.Location.locationID, Equal<POVendorInventory.vendorLocationID>>>>))]
			public override short? VendorVLeadTime { get; set; }
			#endregion
			#region AddLeadTimeDays (POVendorInventory.addLeadTimeDays)
			public abstract class addLeadTimeDays : PX.Data.BQL.BqlShort.Field<addLeadTimeDays> { }
			/// <summary>
			/// POVendorInventory.addLeadTimeDays
			/// </summary>
			[PXDBShort(BqlField = typeof(POVendorInventory.addLeadTimeDays))]
			public override short? AddLeadTimeDays { get; set; }
			#endregion
			#region LotSize (POVendorInventory.lotSize)
			public abstract class lotSize : PX.Data.BQL.BqlDecimal.Field<lotSize> { }
			/// <summary>
			/// POVendorInventory.lotSize
			/// </summary>
			[PXDBDecimal(BqlField = typeof(POVendorInventory.lotSize))]
			public override decimal? LotSize { get; set; }
			#endregion
			#region MinOrdQty (POVendorInventory.minOrdQty)
			public abstract class minOrdQty : PX.Data.BQL.BqlDecimal.Field<minOrdQty> { }
			/// <summary>
			/// POVendorInventory.minOrdQty
			/// </summary>
			[PXDBDecimal(BqlField = typeof(POVendorInventory.minOrdQty))]
			public override decimal? MinOrdQty { get; set; }
			#endregion
			#region MaxOrdQty (POVendorInventory.maxOrdQty)
			public abstract class maxOrdQty : PX.Data.BQL.BqlDecimal.Field<maxOrdQty> { }
			/// <summary>
			/// POVendorInventory.maxOrdQty
			/// </summary>
			[PXDBDecimal(BqlField = typeof(POVendorInventory.maxOrdQty))]
			public override decimal? MaxOrdQty { get; set; }
			#endregion


			// VendorLocation ------------------------------------------
			// as defined in  MRPInventory
		}

		[PXHidden]
		[Serializable]
		[PXProjection(typeof(SelectFrom<InventoryItem>.
			LeftJoin<INItemSite>.
				On<InventoryItem.inventoryID.IsEqual<INItemSite.inventoryID>>.
			LeftJoin<VendorLocation>.
				On<InventoryItem.preferredVendorID.IsEqual<VendorLocation.bAccountID>.
					And<InventoryItem.preferredVendorLocationID.IsEqual<VendorLocation.locationID>>>.
			Where<INItemSite.siteID.IsNull.
				And<InventoryItem.stkItem.IsEqual<True>.
				And<Where<InventoryItemExt.aMMRPItem.IsEqual<True>.
					Or<InventoryItemExt.aMMRPItem.IsNull>>>>>.
			OrderBy<InventoryItem.inventoryID.Asc>))]
		public class InventoryProjection : MRPInventory, IBqlTable
		{
			#region InventoryID (InventoryItem.InventoryID) KEY
			public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
			/// <summary>
			/// InventoryItem.InventoryID
			/// </summary>
			[PXDBInt(IsKey = true, BqlField = typeof(InventoryItem.inventoryID))]
			public override int? InventoryID { get; set; }
			#endregion
		}

		public abstract class MRPInventory
		{
			public virtual int? InventoryID { get; set; }

			// INItemSite  ----------------------------------------------

			/// <summary>
			/// INItemSite.SiteID
			/// </summary>
			public virtual int? SiteID { get; set; }
			/// <summary>
			/// INItemSite.PreferredVendorOverride
			/// </summary>
			public virtual bool? PreferredVendorOverride { get; set; }
			/// <summary>
			/// INItemSite.PreferredVendorID
			/// </summary>
			public virtual int? SitePreferredVendorID { get; set; }
			/// <summary>
			/// INItemSite.PreferredVendorLocationID
			/// </summary>
			public virtual int? SitePreferredVendorLocationID { get; set; }
			/// <summary>
			/// INItemSite.ReplenishmentSource
			/// </summary>
			public virtual string ReplenishmentSource { get; set; }
			/// <summary>
			/// INItemSite.ReplenishmentClassID
			/// </summary>
			public virtual string ReplenishmentClassID { get; set; }
			/// <summary>
			/// INItemSite.ProductManagerID
			/// </summary>
			public virtual int? ProductManagerID { get; set; }
			/// <summary>
			/// INItemSite.SafetyStock
			/// </summary>
			public virtual decimal? SafetyStock { get; set; }
			/// <summary>
			/// INItemSite.SafetyStockOverride
			/// </summary>
			public virtual bool? SafetyStockOverride { get; set; }
			/// <summary>
			/// INItemSite.MinQty
			/// </summary>
			public virtual decimal? MinQty { get; set; }
			/// <summary>
			/// INItemSite.MinQtyOverride
			/// </summary>
			public virtual bool? MinQtyOverride { get; set; }
			/// <summary>
			/// INItemSite.SubItemOverride
			/// </summary>
			public virtual bool? SubItemOverride { get; set; }


			// INItemSiteExt --------------------------------------------

			public virtual string SiteAMBOMID { get; set; }
			/// <summary>
			/// INItemSiteExt.AMPlanningBOMID
			/// </summary>
			public virtual string SiteAMPlanningBOMID { get; set; }
			/// <summary>
			/// INItemSiteExt.AMMFGLeadTime
			/// </summary>
			public virtual Int32? SiteAMMFGLeadTime { get; set; }
			/// <summary>
			/// INItemSiteExt.AMLotSize
			/// </summary>
			public virtual Decimal? SiteAMLotSize { get; set; }
			/// <summary>
			/// INItemSiteExt.AMMaxOrdQty
			/// </summary>
			public virtual Decimal? SiteAMMaxOrdQty { get; set; }
			/// <summary>
			/// INItemSiteExt.AMMinOrdQty
			/// </summary>
			public virtual Decimal? SiteAMMinOrdQty { get; set; }
			/// <summary>
			/// INItemSiteExt.AMGroupWindow
			/// </summary>
			public virtual Int32? SiteAMGroupWindow { get; set; }



			// InventoryItem  -------------------------------------------
			#region InventoryCD (InventoryItem.inventoryCD)
			public abstract class inventoryCD : PX.Data.BQL.BqlString.Field<inventoryCD> { }
			/// <summary>
			/// InventoryItem.inventoryCD
			/// </summary>
			[PXDBString(BqlField = typeof(InventoryItem.inventoryCD))]
			public virtual string InventoryCD { get; set; }
			#endregion
			#region StkItem (InventoryItem.stkItem)
			public abstract class stkItem : PX.Data.BQL.BqlBool.Field<stkItem> { }
			/// <summary>
			/// InventoryItem.stkItem
			/// </summary>
			[PXDBBool(BqlField = typeof(InventoryItem.stkItem))]
			public virtual bool? StkItem { get; set; }
			#endregion
			#region ItemPreferredVendorID (InventoryItem.preferredVendorID)
			public abstract class itemPreferredVendorID : PX.Data.BQL.BqlInt.Field<itemPreferredVendorID> { }
			/// <summary>
			/// InventoryItem.preferredVendorID
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItem.preferredVendorID))]
			public virtual int? ItemPreferredVendorID { get; set; }
			#endregion
			#region ItemPreferredVendorLocationID (InventoryItem.preferredVendorLocationID)
			public abstract class itemPreferredVendorLocationID : PX.Data.BQL.BqlInt.Field<itemPreferredVendorLocationID> { }
			/// <summary>
			/// InventoryItem.preferredVendorLocationID
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItem.preferredVendorLocationID))]
			public virtual int? ItemPreferredVendorLocationID { get; set; }
			#endregion
			#region DefaultSubItemID (InventoryItem.defaultSubItemID)
			public abstract class defaultSubItemID : PX.Data.BQL.BqlInt.Field<defaultSubItemID> { }
			/// <summary>
			/// InventoryItem.defaultSubItemID
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItem.defaultSubItemID))]
			public virtual int? DefaultSubItemID { get; set; }
			#endregion
			#region BaseUnit (InventoryItem.baseUnit)
			public abstract class baseUnit : PX.Data.BQL.BqlString.Field<baseUnit> { }
			/// <summary>
			/// InventoryItem.baseUnit
			/// </summary>
			[PXDBString(BqlField = typeof(InventoryItem.baseUnit))]
			public virtual string BaseUnit { get; set; }
			#endregion
			#region ItemStatus (InventoryItem.itemStatus)
			public abstract class itemStatus : PX.Data.BQL.BqlString.Field<itemStatus> { }
			/// <summary>
			/// InventoryItem.itemStatus
			/// </summary>
			[PXDBString(BqlField = typeof(InventoryItem.itemStatus))]
			public virtual string ItemStatus { get; set; }
			#endregion
			#region KitItem (InventoryItem.kitItem)
			public abstract class kitItem : PX.Data.BQL.BqlBool.Field<kitItem> { }
			/// <summary>
			/// InventoryItem.kitItem
			/// </summary>
			[PXDBBool(BqlField = typeof(InventoryItem.kitItem))]
			public virtual bool? KitItem { get; set; }
			#endregion
			#region ItemClassID (InventoryItem.itemClassID)
			public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
			/// <summary>
			/// InventoryItem.itemClassID
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItem.itemClassID))]
			public virtual int? ItemClassID { get; set; }
			#endregion


			// InventoryItemExt  ----------------------------------------

			#region ItemAMBOMID (InventoryItemExt.aMBOMID)
			public abstract class itemAMBOMID : PX.Data.BQL.BqlString.Field<itemAMBOMID> { }
			/// <summary>
			/// InventoryItemExt.aMBOMID
			/// </summary>
			[PXDBString(BqlField = typeof(InventoryItemExt.aMBOMID))]
			public virtual string ItemAMBOMID { get; set; }
			#endregion
			#region ItemAMPlanningBOMID (InventoryItemExt.aMPlanningBOMID)
			public abstract class itemAMPlanningBOMID : PX.Data.BQL.BqlString.Field<itemAMPlanningBOMID> { }
			/// <summary>
			/// InventoryItemExt.aMPlanningBOMID
			/// </summary>
			[PXDBString(BqlField = typeof(InventoryItemExt.aMPlanningBOMID))]
			public virtual string ItemAMPlanningBOMID { get; set; }
			#endregion
			#region AMMRPItem (InventoryItemExt.aMMRPItem)
			public abstract class aMMRPItem : PX.Data.BQL.BqlBool.Field<aMMRPItem> { }
			/// <summary>
			/// InventoryItemExt.aMMRPItem
			/// </summary>
			[PXDBBool(BqlField = typeof(InventoryItemExt.aMMRPItem))]
			public virtual bool? AMMRPItem { get; set; }
			#endregion
			#region AMLowLevel (InventoryItemExt.aMLowLevel)
			public abstract class aMLowLevel : PX.Data.BQL.BqlInt.Field<aMLowLevel> { }
			/// <summary>
			/// InventoryItemExt.aMLowLevel
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItemExt.aMLowLevel))]
			[PXUIField(DisplayName = "Low Level", Visibility = PXUIVisibility.Invisible, Visible = false, Enabled = false)]
			[PXDefault(TypeCode.Int32, "0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual Int32? AMLowLevel { get; set; }
			#endregion
			#region ItemAMMFGLeadTime (InventoryItemExt.aMMFGLeadTime)
			public abstract class itemAMMFGLeadTime : PX.Data.BQL.BqlInt.Field<itemAMMFGLeadTime> { }
			/// <summary>
			/// InventoryItemExt.aMMFGLeadTime
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItemExt.aMMFGLeadTime))]
			[PXUIField(DisplayName = "MFG Lead Time")]
			public virtual Int32? ItemAMMFGLeadTime { get; set; }
			#endregion
			#region ItemAMMinOrdQty (InventoryItemExt.aMMinOrdQty)
			/// <summary>
			/// InventoryItemExt.aMMinOrdQty
			/// </summary>
			public abstract class itemAMMinOrdQty : PX.Data.BQL.BqlDecimal.Field<itemAMMinOrdQty> { }
			[PXDBQuantity(BqlField = typeof(InventoryItemExt.aMMinOrdQty))]
			[PXUIField(DisplayName="Min Order Qty")]
			public virtual Decimal? ItemAMMinOrdQty { get; set; }
			#endregion
			#region ItemAMMaxOrdQty (InventoryItemExt.aMMaxOrdQty)
			/// <summary>
			/// InventoryItemExt.aMMaxOrdQty
			/// </summary>
			public abstract class itemAMMaxOrdQty : PX.Data.BQL.BqlDecimal.Field<itemAMMaxOrdQty> { }
			[PXDBQuantity(BqlField = typeof(InventoryItemExt.aMMaxOrdQty))]
			[PXUIField(DisplayName="Max Order Qty")]
			public virtual Decimal? ItemAMMaxOrdQty { get; set; }
			#endregion
			#region ItemAMLotSize (InventoryItemExt.aMLotSize)
			/// <summary>
			/// InventoryItemExt.aMLotSize
			/// </summary>
			public abstract class itemAMLotSize : PX.Data.BQL.BqlDecimal.Field<itemAMLotSize> { }
			[PXDBQuantity(BqlField = typeof(InventoryItemExt.aMLotSize))]
			[PXUIField(DisplayName = "Lot Size")]
			public virtual Decimal? ItemAMLotSize { get; set; }
			#endregion
			#region AMQtyRoundUp (InventoryItemExt.aMQtyRoundUp)
			/// <summary>
			/// InventoryItemExt.aMQtyRoundUp
			/// </summary>
			public abstract class aMQtyRoundUp : PX.Data.BQL.BqlBool.Field<aMQtyRoundUp> { }
			[PXDBBool(BqlField = typeof(InventoryItemExt.aMQtyRoundUp))]
			[PXUIField(DisplayName = "Quantity Round Up")]
			public virtual Boolean? AMQtyRoundUp { get; set; }
			#endregion
			#region AMReplenishmentSourceOverride (InventoryItemExt.AMReplenishmentSourceOverride)
			public abstract class aMReplenishmentSourceOverride : PX.Data.BQL.BqlBool.Field<aMReplenishmentSourceOverride> { }
			/// <summary>
			/// InventoryItemExt.AMReplenishmentSourceOverride
			/// </summary>
			[PXDBBool(BqlField = typeof(InventoryItemExt.aMReplenishmentSourceOverride))]
			public virtual Boolean? AMReplenishmentSourceOverride { get; set; }
			#endregion
			#region AMReplenishmentSource (InventoryItemExt.AMReplenishmentSource)
			public abstract class aMReplenishmentSource : PX.Data.BQL.BqlString.Field<aMReplenishmentSource> { }
			/// <summary>
			/// InventoryItemExt.AMReplenishmentSource
			/// </summary>
			[PXDBString(1, IsFixed = true, BqlField = typeof(InventoryItemExt.aMReplenishmentSource))]
			[PXUIField(DisplayName = "Source")]
			public virtual string AMReplenishmentSource { get; set; }
			#endregion
			#region AMGroupWindow (InventoryItemExt.AMGroupWindow)
			public abstract class aMGroupWindow : PX.Data.BQL.BqlInt.Field<aMGroupWindow> { }
			/// <summary>
			/// InventoryItemExt.AMGroupWindow
			/// </summary>
			[PXDBInt(BqlField = typeof(InventoryItemExt.aMGroupWindow))]
			[PXUIField(DisplayName = "Days Supply")]
			public virtual Int32? AMGroupWindow { get; set; }
			#endregion

			// POVendorInventory ----------------------------------------
			/// <summary>
			/// POVendorInventory.Active
			/// </summary>
			public virtual bool? VendorActive { get; set; }
			/// <summary>
			/// POVendorInventory.SubItemID
			/// </summary>
			public virtual int? VendorSubItemID { get; set; }
			/// <summary>
			/// POVendorInventory.VLeadTime
			/// </summary>
			public virtual short? VendorVLeadTime { get; set; }
			/// <summary>
			/// POVendorInventory.AddLeadTimeDays
			/// </summary>
			public virtual short? AddLeadTimeDays { get; set; }
			/// <summary>
			/// POVendorInventory.LotSize
			/// </summary>
			public virtual decimal? LotSize { get; set; }
			/// <summary>
			/// POVendorInventory.MinOrdQty
			/// </summary>
			public virtual decimal? MinOrdQty { get; set; }
			/// <summary>
			/// POVendorInventory.MaxOrdQty
			/// </summary>
			public virtual decimal? MaxOrdQty { get; set; }


			// VendorLocation ------------------------------------------
			#region BAccountID (VendorLocation.bAccountID)
			public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
			/// <summary>
			/// VendorLocation.bAccountID
			/// </summary>
			[PXDBInt(BqlField = typeof(VendorLocation.bAccountID))]
			public virtual int? BAccountID { get; set; }
			#endregion
			#region LocVLeadTime (VendorLocation.vLeadTime)
			public abstract class locVLeadTime : PX.Data.BQL.BqlShort.Field<locVLeadTime> { }
			/// <summary>
			/// VendorLocation.vLeadTime
			/// </summary>
			[PXDBShort(BqlField = typeof(VendorLocation.vLeadTime))]
			public virtual short? LocVLeadTime { get; set; }
			#endregion
		}
	}
}
