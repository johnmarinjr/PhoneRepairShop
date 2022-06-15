using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System.Collections.Generic;

namespace PX.Objects.PM.MaterialManagement
{
    public class StockInitMaint : PXGraph<StockInitMaint>
    {
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDefault]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<PMSiteStatus.taskID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDefault]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<PMLocationStatus.taskID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDefault]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<PMLotSerialStatus.taskID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBDate()]
		protected virtual void _(Events.CacheAttached<PMLotSerialStatus.receiptDate> e) { }

		public PXSelect<PMLocationStatus> locationstatus;
		public PXSelect<PMLotSerialStatus> lotserialstatus;
		public PXSelect<PMSiteStatus> sitestatus;
		public PXSelect<PMSiteSummaryStatus> sitesummarystatus;
		public PXSetup<INSetup> insetup;
		public PXSelect<PMSetup> pmsetup;

		public virtual void InitStock()
        {
			InitSiteStatus();
			InitSiteSummaryStatus();
			InitLocationStatus();
			InitLotSerialStatus();
			PMSetup setup = pmsetup.Select();
			if (setup != null) 
			{
				setup.StockInitRequired = false;
				pmsetup.Update(setup);
			}
			Actions.PressSave();
		}

		private void InitSiteStatus()
		{
			Dictionary<SiteStatusKey, PMSiteStatus> projectStatus = GetSiteStatusForProjects();
			Dictionary<SiteStatusKey, PMSiteStatus> nonProjectStatus = GetSiteStatusForNonProjects();

			var select = new PXSelectJoin<INSiteStatus, 
				InnerJoin<InventoryItem, On<INSiteStatus.inventoryID, Equal<InventoryItem.inventoryID>>>,
				Where<INSiteStatus.siteID,
				NotEqual<Required<INSiteStatus.siteID>>,
				And<InventoryItem.stkItem, Equal<True>>>>(this);

			foreach (INSiteStatus item in select.Select(insetup.Current.TransitSiteID))
			{
				SiteStatusKey key = new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value);

				PMSiteStatus nonProjectItem = null;
				nonProjectStatus.TryGetValue(key, out nonProjectItem);

				PMSiteStatus projectItem = null;
				projectStatus.TryGetValue(key, out projectItem);

				PMSiteStatus sum = GetSum(projectItem, nonProjectItem);

				IStatus delta = PXCache<INSiteStatus>.CreateCopy(item);
				if (sum != null)
					delta.Subtract(sum);

				if (!delta.IsZero())
				{
					if (nonProjectItem == null)
					{
						nonProjectItem = new PMSiteStatus();
						nonProjectItem.InventoryID = key.InventoryID;
						nonProjectItem.SubItemID = key.SubItemID;
						nonProjectItem.SiteID = key.SiteID;
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
						nonProjectItem = sitestatus.Insert(nonProjectItem);
					}
					else
					{
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
					}

					nonProjectItem.Add(delta);
					sitestatus.Update(nonProjectItem);
				}
			}
		}

		private void InitSiteSummaryStatus()
		{
			Dictionary<SiteStatusKey, PMSiteSummaryStatus> projectStatus = GetSiteSummaryStatusForProjects();
			Dictionary<SiteStatusKey, PMSiteSummaryStatus> nonProjectStatus = GetSiteSummaryStatusForNonProjects();

			var select = new PXSelectJoin<INSiteStatus,
				InnerJoin<InventoryItem, On<INSiteStatus.inventoryID, Equal<InventoryItem.inventoryID>>>,
				Where<INSiteStatus.siteID,
				NotEqual<Required<INSiteStatus.siteID>>,
				And<InventoryItem.stkItem, Equal<True>>>>(this);

			foreach (INSiteStatus item in select.Select(insetup.Current.TransitSiteID))
			{
				SiteStatusKey key = new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value);

				PMSiteSummaryStatus nonProjectItem = null;
				nonProjectStatus.TryGetValue(key, out nonProjectItem);

				PMSiteSummaryStatus projectItem = null;
				projectStatus.TryGetValue(key, out projectItem);

				PMSiteSummaryStatus sum = GetSum(projectItem, nonProjectItem);

				IStatus delta = PXCache<INSiteStatus>.CreateCopy(item);
				if (sum != null)
					delta.Subtract(sum);

				if (!delta.IsZero())
				{
					if (nonProjectItem == null)
					{
						nonProjectItem = new PMSiteSummaryStatus();
						nonProjectItem.InventoryID = key.InventoryID;
						nonProjectItem.SubItemID = key.SubItemID;
						nonProjectItem.SiteID = key.SiteID;
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem = sitesummarystatus.Insert(nonProjectItem);
					}
					else
					{
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
					}

					nonProjectItem.Add(delta);
					sitesummarystatus.Update(nonProjectItem);
				}
			}
		}

		private void InitLocationStatus()
		{
			Dictionary<LocationStatusKey, PMLocationStatus> projectStatus = GetLocationStatusForProjects();
			Dictionary<LocationStatusKey, PMLocationStatus> nonProjectStatus = GetLocationStatusForNonProject();

			var select = new PXSelectJoin<INLocationStatus,
				InnerJoin<INLocation, On<INLocationStatus.locationID, Equal<INLocation.locationID>>,
				InnerJoin<InventoryItem, On<INLocationStatus.inventoryID, Equal<InventoryItem.inventoryID>>>>,
				Where<InventoryItem.stkItem, Equal<True>>>(this);

			foreach (INLocationStatus item in select.Select())
			{
				LocationStatusKey key = new LocationStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value);

				PMLocationStatus nonProjectItem = null;
				nonProjectStatus.TryGetValue(key, out nonProjectItem);

				PMLocationStatus projectItem = null;
				projectStatus.TryGetValue(key, out projectItem);

				PMLocationStatus sum = GetSum(projectItem, nonProjectItem);

				IStatus delta = PXCache<INLocationStatus>.CreateCopy(item);
				if (sum != null)
					delta.Subtract(sum);

				if (!delta.IsZero())
				{
					if (nonProjectItem == null)
					{
						nonProjectItem = new PMLocationStatus();
						nonProjectItem.InventoryID = key.InventoryID;
						nonProjectItem.SubItemID = key.SubItemID;
						nonProjectItem.SiteID = key.SiteID;
						nonProjectItem.LocationID = key.LocationID;
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
						nonProjectItem = locationstatus.Insert(nonProjectItem);
					}
					else
					{
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
					}

					nonProjectItem.Add(delta);
					locationstatus.Update(nonProjectItem);
				}
			}
		}

		private void InitLotSerialStatus()
		{
			Dictionary<LotSerialStatusKey, PMLotSerialStatus> projectStatus = GetLotSerialStatusForProjects();
			Dictionary<LotSerialStatusKey, PMLotSerialStatus> nonProjectStatus = GetLotSerialStatusForNonProjects();
			var select = new PXSelectJoin<INLotSerialStatus,
				InnerJoin<INLocation, On<INLotSerialStatus.locationID, Equal<INLocation.locationID>>,
				InnerJoin<InventoryItem, On<INLotSerialStatus.inventoryID, Equal<InventoryItem.inventoryID>>>>,
				Where<InventoryItem.stkItem, Equal<True>>>(this);

			foreach (INLotSerialStatus item in select.Select())
			{
				LotSerialStatusKey key = new LotSerialStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value, item.LotSerialNbr);

				PMLotSerialStatus nonProjectItem = null;
				nonProjectStatus.TryGetValue(key, out nonProjectItem);

				PMLotSerialStatus projectItem = null;
				projectStatus.TryGetValue(key, out projectItem);

				PMLotSerialStatus sum = GetSum(projectItem, nonProjectItem);

				IStatus delta = PXCache<INLotSerialStatus>.CreateCopy(item);
				if (sum != null)
					delta.Subtract(sum);

				if (!delta.IsZero())
				{
					if (nonProjectItem == null)
					{
						nonProjectItem = new PMLotSerialStatus();
						nonProjectItem.InventoryID = key.InventoryID;
						nonProjectItem.SubItemID = key.SubItemID;
						nonProjectItem.SiteID = key.SiteID;
						nonProjectItem.LocationID = key.LocationID;
						nonProjectItem.LotSerialNbr = key.LotSerialNbr;
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
						nonProjectItem.LotSerTrack = item.LotSerTrack;
						nonProjectItem.ReceiptDate = item.ReceiptDate;
						nonProjectItem = lotserialstatus.Insert(nonProjectItem);
					}
					else
					{
						nonProjectItem.ProjectID = ProjectDefaultAttribute.NonProject();
						nonProjectItem.TaskID = 0;
					}

					nonProjectItem.Add(delta);
					lotserialstatus.Update(nonProjectItem);
				}
			}
		}

		private T GetSum<T>(T x, T y) where T : class, IStatus, IBqlTable, new()
		{
			if (x == null && y == null)
				return null;

			if (x != null && y != null)
			{
				var result = PXCache<T>.CreateCopy(x);
				result.Add(y);
				return result;
			}
			else if (x != null)
			{
				return x;
			}
			else
			{
				return y;
			}
		}

		private Dictionary<SiteStatusKey, PMSiteStatus> GetSiteStatusForProjects()
		{
			var select = new PXSelectGroupBy<PMSiteStatus,
				Where<PMSiteStatus.taskID, NotEqual<int0>>,
				Aggregate<
						Sum<PMSiteStatus.qtyOnHand,
						Sum<PMSiteStatus.qtyAvail,
						Sum<PMSiteStatus.qtyNotAvail,
						Sum<PMSiteStatus.qtyExpired,
						Sum<PMSiteStatus.qtyHardAvail,
						Sum<PMSiteStatus.qtyActual,
						Sum<PMSiteStatus.qtyFSSrvOrdBooked,
						Sum<PMSiteStatus.qtyFSSrvOrdAllocated,
						Sum<PMSiteStatus.qtyFSSrvOrdPrepared,
						Sum<PMSiteStatus.qtySOBackOrdered,
						Sum<PMSiteStatus.qtySOPrepared,
						Sum<PMSiteStatus.qtySOBooked,
						Sum<PMSiteStatus.qtySOShipped,
						Sum<PMSiteStatus.qtySOShipping,
						Sum<PMSiteStatus.qtyINIssues,
						Sum<PMSiteStatus.qtyINReceipts,
						Sum<PMSiteStatus.qtyInTransit,
						Sum<PMSiteStatus.qtyInTransitToSO,
						Sum<PMSiteStatus.qtyPOReceipts,
						Sum<PMSiteStatus.qtyPOPrepared,
						Sum<PMSiteStatus.qtyPOOrders,
						Sum<PMSiteStatus.qtyFixedFSSrvOrd,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrd,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMSiteStatus.qtySOFixed,
						Sum<PMSiteStatus.qtyPOFixedOrders,
						Sum<PMSiteStatus.qtyPOFixedPrepared,
						Sum<PMSiteStatus.qtyPOFixedReceipts,
						Sum<PMSiteStatus.qtySODropShip,
						Sum<PMSiteStatus.qtyPODropShipOrders,
						Sum<PMSiteStatus.qtyPODropShipPrepared,
						Sum<PMSiteStatus.qtyPODropShipReceipts,
						Sum<PMSiteStatus.qtyINAssemblySupply,
						Sum<PMSiteStatus.qtyINAssemblyDemand,
						Sum<PMSiteStatus.qtyInTransitToProduction,
						Sum<PMSiteStatus.qtyProductionSupplyPrepared,
						Sum<PMSiteStatus.qtyProductionSupply,
						Sum<PMSiteStatus.qtyPOFixedProductionPrepared,
						Sum<PMSiteStatus.qtyPOFixedProductionOrders,
						Sum<PMSiteStatus.qtyProductionDemandPrepared,
						Sum<PMSiteStatus.qtyProductionDemand,
						Sum<PMSiteStatus.qtyProductionAllocated,
						Sum<PMSiteStatus.qtySOFixedProduction,
						Sum<PMSiteStatus.qtyProdFixedPurchase,
						Sum<PMSiteStatus.qtyProdFixedProduction,
						Sum<PMSiteStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMSiteStatus.qtyProdFixedProdOrders,
						Sum<PMSiteStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMSiteStatus.qtyProdFixedSalesOrders,

						GroupBy<PMSiteStatus.inventoryID,
						GroupBy<PMSiteStatus.subItemID,
						GroupBy<PMSiteStatus.siteID>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<SiteStatusKey, PMSiteStatus> result = new Dictionary<SiteStatusKey, PMSiteStatus>();
			foreach (PMSiteStatus item in select.Select())
			{
				result.Add(new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value), item);
			}

			return result;
		}

		private Dictionary<SiteStatusKey, PMSiteSummaryStatus> GetSiteSummaryStatusForProjects()
		{
			var select = new PXSelectGroupBy<PMSiteSummaryStatus,
				Where<PMSiteSummaryStatus.projectID, NotEqual<Required<PMSiteSummaryStatus.projectID>>>,
				Aggregate<
						Sum<PMSiteSummaryStatus.qtyOnHand,
						Sum<PMSiteSummaryStatus.qtyAvail,
						Sum<PMSiteSummaryStatus.qtyNotAvail,
						Sum<PMSiteSummaryStatus.qtyExpired,
						Sum<PMSiteSummaryStatus.qtyHardAvail,
						Sum<PMSiteSummaryStatus.qtyActual,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdBooked,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdAllocated,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdPrepared,
						Sum<PMSiteSummaryStatus.qtySOBackOrdered,
						Sum<PMSiteSummaryStatus.qtySOPrepared,
						Sum<PMSiteSummaryStatus.qtySOBooked,
						Sum<PMSiteSummaryStatus.qtySOShipped,
						Sum<PMSiteSummaryStatus.qtySOShipping,
						Sum<PMSiteSummaryStatus.qtyINIssues,
						Sum<PMSiteSummaryStatus.qtyINReceipts,
						Sum<PMSiteSummaryStatus.qtyInTransit,
						Sum<PMSiteSummaryStatus.qtyInTransitToSO,
						Sum<PMSiteSummaryStatus.qtyPOReceipts,
						Sum<PMSiteSummaryStatus.qtyPOPrepared,
						Sum<PMSiteSummaryStatus.qtyPOOrders,
						Sum<PMSiteSummaryStatus.qtyFixedFSSrvOrd,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrd,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMSiteSummaryStatus.qtySOFixed,
						Sum<PMSiteSummaryStatus.qtyPOFixedOrders,
						Sum<PMSiteSummaryStatus.qtyPOFixedPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedReceipts,
						Sum<PMSiteSummaryStatus.qtySODropShip,
						Sum<PMSiteSummaryStatus.qtyPODropShipOrders,
						Sum<PMSiteSummaryStatus.qtyPODropShipPrepared,
						Sum<PMSiteSummaryStatus.qtyPODropShipReceipts,
						Sum<PMSiteSummaryStatus.qtyINAssemblySupply,
						Sum<PMSiteSummaryStatus.qtyINAssemblyDemand,
						Sum<PMSiteSummaryStatus.qtyInTransitToProduction,
						Sum<PMSiteSummaryStatus.qtyProductionSupplyPrepared,
						Sum<PMSiteSummaryStatus.qtyProductionSupply,
						Sum<PMSiteSummaryStatus.qtyPOFixedProductionPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedProductionOrders,
						Sum<PMSiteSummaryStatus.qtyProductionDemandPrepared,
						Sum<PMSiteSummaryStatus.qtyProductionDemand,
						Sum<PMSiteSummaryStatus.qtyProductionAllocated,
						Sum<PMSiteSummaryStatus.qtySOFixedProduction,
						Sum<PMSiteSummaryStatus.qtyProdFixedPurchase,
						Sum<PMSiteSummaryStatus.qtyProdFixedProduction,
						Sum<PMSiteSummaryStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMSiteSummaryStatus.qtyProdFixedProdOrders,
						Sum<PMSiteSummaryStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMSiteSummaryStatus.qtyProdFixedSalesOrders,

						GroupBy<PMSiteSummaryStatus.inventoryID,
						GroupBy<PMSiteSummaryStatus.subItemID,
						GroupBy<PMSiteSummaryStatus.siteID>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<SiteStatusKey, PMSiteSummaryStatus> result = new Dictionary<SiteStatusKey, PMSiteSummaryStatus>();
			foreach (PMSiteSummaryStatus item in select.Select(ProjectDefaultAttribute.NonProject()))
			{
				result.Add(new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value), item);
			}

			return result;
		}

		private Dictionary<LocationStatusKey, PMLocationStatus> GetLocationStatusForProjects()
		{
			var select = new PXSelectGroupBy<PMLocationStatus,
				Where<PMLocationStatus.taskID, NotEqual<int0>>,
				Aggregate<
						Sum<PMLocationStatus.qtyOnHand,
						Sum<PMLocationStatus.qtyAvail,
						Sum<PMLocationStatus.qtyNotAvail,
						Sum<PMLocationStatus.qtyExpired,
						Sum<PMLocationStatus.qtyHardAvail,
						Sum<PMLocationStatus.qtyActual,
						Sum<PMLocationStatus.qtyFSSrvOrdBooked,
						Sum<PMLocationStatus.qtyFSSrvOrdAllocated,
						Sum<PMLocationStatus.qtyFSSrvOrdPrepared,
						Sum<PMLocationStatus.qtySOBackOrdered,
						Sum<PMLocationStatus.qtySOPrepared,
						Sum<PMLocationStatus.qtySOBooked,
						Sum<PMLocationStatus.qtySOShipped,
						Sum<PMLocationStatus.qtySOShipping,
						Sum<PMLocationStatus.qtyINIssues,
						Sum<PMLocationStatus.qtyINReceipts,
						Sum<PMLocationStatus.qtyInTransit,
						Sum<PMLocationStatus.qtyInTransitToSO,
						Sum<PMLocationStatus.qtyPOReceipts,
						Sum<PMLocationStatus.qtyPOPrepared,
						Sum<PMLocationStatus.qtyPOOrders,
						Sum<PMLocationStatus.qtyFixedFSSrvOrd,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrd,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMLocationStatus.qtySOFixed,
						Sum<PMLocationStatus.qtyPOFixedOrders,
						Sum<PMLocationStatus.qtyPOFixedPrepared,
						Sum<PMLocationStatus.qtyPOFixedReceipts,
						Sum<PMLocationStatus.qtySODropShip,
						Sum<PMLocationStatus.qtyPODropShipOrders,
						Sum<PMLocationStatus.qtyPODropShipPrepared,
						Sum<PMLocationStatus.qtyPODropShipReceipts,
						Sum<PMLocationStatus.qtyINAssemblySupply,
						Sum<PMLocationStatus.qtyINAssemblyDemand,
						Sum<PMLocationStatus.qtyInTransitToProduction,
						Sum<PMLocationStatus.qtyProductionSupplyPrepared,
						Sum<PMLocationStatus.qtyProductionSupply,
						Sum<PMLocationStatus.qtyPOFixedProductionPrepared,
						Sum<PMLocationStatus.qtyPOFixedProductionOrders,
						Sum<PMLocationStatus.qtyProductionDemandPrepared,
						Sum<PMLocationStatus.qtyProductionDemand,
						Sum<PMLocationStatus.qtyProductionAllocated,
						Sum<PMLocationStatus.qtySOFixedProduction,
						Sum<PMLocationStatus.qtyProdFixedPurchase,
						Sum<PMLocationStatus.qtyProdFixedProduction,
						Sum<PMLocationStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMLocationStatus.qtyProdFixedProdOrders,
						Sum<PMLocationStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMLocationStatus.qtyProdFixedSalesOrders,

						GroupBy<PMLocationStatus.inventoryID,
						GroupBy<PMLocationStatus.subItemID,
						GroupBy<PMLocationStatus.siteID,
						GroupBy<PMLocationStatus.locationID>>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<LocationStatusKey, PMLocationStatus> result = new Dictionary<LocationStatusKey, PMLocationStatus>();
			foreach (PMLocationStatus item in select.Select())
			{
				result.Add(new LocationStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value), item);
			}

			return result;
		}

		private Dictionary<LotSerialStatusKey, PMLotSerialStatus> GetLotSerialStatusForProjects()
		{
			var select = new PXSelectGroupBy<PMLotSerialStatus,
				Where<PMLotSerialStatus.taskID, NotEqual<int0>>,
				Aggregate<
						Sum<PMLotSerialStatus.qtyOnHand,
						Sum<PMLotSerialStatus.qtyAvail,
						Sum<PMLotSerialStatus.qtyNotAvail,
						Sum<PMLotSerialStatus.qtyExpired,
						Sum<PMLotSerialStatus.qtyHardAvail,
						Sum<PMLotSerialStatus.qtyActual,
						Sum<PMLotSerialStatus.qtyFSSrvOrdBooked,
						Sum<PMLotSerialStatus.qtyFSSrvOrdAllocated,
						Sum<PMLotSerialStatus.qtyFSSrvOrdPrepared,
						Sum<PMLotSerialStatus.qtySOBackOrdered,
						Sum<PMLotSerialStatus.qtySOPrepared,
						Sum<PMLotSerialStatus.qtySOBooked,
						Sum<PMLotSerialStatus.qtySOShipped,
						Sum<PMLotSerialStatus.qtySOShipping,
						Sum<PMLotSerialStatus.qtyINIssues,
						Sum<PMLotSerialStatus.qtyINReceipts,
						Sum<PMLotSerialStatus.qtyInTransit,
						Sum<PMLotSerialStatus.qtyInTransitToSO,
						Sum<PMLotSerialStatus.qtyPOReceipts,
						Sum<PMLotSerialStatus.qtyPOPrepared,
						Sum<PMLotSerialStatus.qtyPOOrders,
						Sum<PMLotSerialStatus.qtyFixedFSSrvOrd,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrd,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMLotSerialStatus.qtySOFixed,
						Sum<PMLotSerialStatus.qtyPOFixedOrders,
						Sum<PMLotSerialStatus.qtyPOFixedPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedReceipts,
						Sum<PMLotSerialStatus.qtySODropShip,
						Sum<PMLotSerialStatus.qtyPODropShipOrders,
						Sum<PMLotSerialStatus.qtyPODropShipPrepared,
						Sum<PMLotSerialStatus.qtyPODropShipReceipts,
						Sum<PMLotSerialStatus.qtyINAssemblySupply,
						Sum<PMLotSerialStatus.qtyINAssemblyDemand,
						Sum<PMLotSerialStatus.qtyInTransitToProduction,
						Sum<PMLotSerialStatus.qtyProductionSupplyPrepared,
						Sum<PMLotSerialStatus.qtyProductionSupply,
						Sum<PMLotSerialStatus.qtyPOFixedProductionPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedProductionOrders,
						Sum<PMLotSerialStatus.qtyProductionDemandPrepared,
						Sum<PMLotSerialStatus.qtyProductionDemand,
						Sum<PMLotSerialStatus.qtyProductionAllocated,
						Sum<PMLotSerialStatus.qtySOFixedProduction,
						Sum<PMLotSerialStatus.qtyProdFixedPurchase,
						Sum<PMLotSerialStatus.qtyProdFixedProduction,
						Sum<PMLotSerialStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMLotSerialStatus.qtyProdFixedProdOrders,
						Sum<PMLotSerialStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMLotSerialStatus.qtyProdFixedSalesOrders,

						GroupBy<PMLotSerialStatus.inventoryID,
						GroupBy<PMLotSerialStatus.subItemID,
						GroupBy<PMLotSerialStatus.siteID,
						GroupBy<PMLotSerialStatus.locationID,
						GroupBy<PMLotSerialStatus.lotSerialNbr>>>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<LotSerialStatusKey, PMLotSerialStatus> result = new Dictionary<LotSerialStatusKey, PMLotSerialStatus>();
			foreach (PMLotSerialStatus item in select.Select())
			{
				result.Add(new LotSerialStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value, item.LotSerialNbr), item);
			}

			return result;
		}

		private Dictionary<SiteStatusKey, PMSiteStatus> GetSiteStatusForNonProjects()
		{
			var select = new PXSelectGroupBy<PMSiteStatus,
				Where<PMSiteStatus.taskID, Equal<int0>>,
				Aggregate<
						Sum<PMSiteStatus.qtyOnHand,
						Sum<PMSiteStatus.qtyAvail,
						Sum<PMSiteStatus.qtyNotAvail,
						Sum<PMSiteStatus.qtyExpired,
						Sum<PMSiteStatus.qtyHardAvail,
						Sum<PMSiteStatus.qtyActual,
						Sum<PMSiteStatus.qtyFSSrvOrdBooked,
						Sum<PMSiteStatus.qtyFSSrvOrdAllocated,
						Sum<PMSiteStatus.qtyFSSrvOrdPrepared,
						Sum<PMSiteStatus.qtySOBackOrdered,
						Sum<PMSiteStatus.qtySOPrepared,
						Sum<PMSiteStatus.qtySOBooked,
						Sum<PMSiteStatus.qtySOShipped,
						Sum<PMSiteStatus.qtySOShipping,
						Sum<PMSiteStatus.qtyINIssues,
						Sum<PMSiteStatus.qtyINReceipts,
						Sum<PMSiteStatus.qtyInTransit,
						Sum<PMSiteStatus.qtyInTransitToSO,
						Sum<PMSiteStatus.qtyPOReceipts,
						Sum<PMSiteStatus.qtyPOPrepared,
						Sum<PMSiteStatus.qtyPOOrders,
						Sum<PMSiteStatus.qtyFixedFSSrvOrd,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrd,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMSiteStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMSiteStatus.qtySOFixed,
						Sum<PMSiteStatus.qtyPOFixedOrders,
						Sum<PMSiteStatus.qtyPOFixedPrepared,
						Sum<PMSiteStatus.qtyPOFixedReceipts,
						Sum<PMSiteStatus.qtySODropShip,
						Sum<PMSiteStatus.qtyPODropShipOrders,
						Sum<PMSiteStatus.qtyPODropShipPrepared,
						Sum<PMSiteStatus.qtyPODropShipReceipts,
						Sum<PMSiteStatus.qtyINAssemblySupply,
						Sum<PMSiteStatus.qtyINAssemblyDemand,
						Sum<PMSiteStatus.qtyInTransitToProduction,
						Sum<PMSiteStatus.qtyProductionSupplyPrepared,
						Sum<PMSiteStatus.qtyProductionSupply,
						Sum<PMSiteStatus.qtyPOFixedProductionPrepared,
						Sum<PMSiteStatus.qtyPOFixedProductionOrders,
						Sum<PMSiteStatus.qtyProductionDemandPrepared,
						Sum<PMSiteStatus.qtyProductionDemand,
						Sum<PMSiteStatus.qtyProductionAllocated,
						Sum<PMSiteStatus.qtySOFixedProduction,
						Sum<PMSiteStatus.qtyProdFixedPurchase,
						Sum<PMSiteStatus.qtyProdFixedProduction,
						Sum<PMSiteStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMSiteStatus.qtyProdFixedProdOrders,
						Sum<PMSiteStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMSiteStatus.qtyProdFixedSalesOrders,

						GroupBy<PMSiteStatus.inventoryID,
						GroupBy<PMSiteStatus.subItemID,
						GroupBy<PMSiteStatus.siteID>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<SiteStatusKey, PMSiteStatus> result = new Dictionary<SiteStatusKey, PMSiteStatus>();
			foreach (PMSiteStatus item in select.Select())
			{
				result.Add(new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value), item);
			}

			return result;
		}

		private Dictionary<SiteStatusKey, PMSiteSummaryStatus> GetSiteSummaryStatusForNonProjects()
		{
			var select = new PXSelectGroupBy<PMSiteSummaryStatus,
				Where<PMSiteSummaryStatus.projectID, Equal<Required<PMSiteSummaryStatus.projectID>>>,
				Aggregate<
						Sum<PMSiteSummaryStatus.qtyOnHand,
						Sum<PMSiteSummaryStatus.qtyAvail,
						Sum<PMSiteSummaryStatus.qtyNotAvail,
						Sum<PMSiteSummaryStatus.qtyExpired,
						Sum<PMSiteSummaryStatus.qtyHardAvail,
						Sum<PMSiteSummaryStatus.qtyActual,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdBooked,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdAllocated,
						Sum<PMSiteSummaryStatus.qtyFSSrvOrdPrepared,
						Sum<PMSiteSummaryStatus.qtySOBackOrdered,
						Sum<PMSiteSummaryStatus.qtySOPrepared,
						Sum<PMSiteSummaryStatus.qtySOBooked,
						Sum<PMSiteSummaryStatus.qtySOShipped,
						Sum<PMSiteSummaryStatus.qtySOShipping,
						Sum<PMSiteSummaryStatus.qtyINIssues,
						Sum<PMSiteSummaryStatus.qtyINReceipts,
						Sum<PMSiteSummaryStatus.qtyInTransit,
						Sum<PMSiteSummaryStatus.qtyInTransitToSO,
						Sum<PMSiteSummaryStatus.qtyPOReceipts,
						Sum<PMSiteSummaryStatus.qtyPOPrepared,
						Sum<PMSiteSummaryStatus.qtyPOOrders,
						Sum<PMSiteSummaryStatus.qtyFixedFSSrvOrd,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrd,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMSiteSummaryStatus.qtySOFixed,
						Sum<PMSiteSummaryStatus.qtyPOFixedOrders,
						Sum<PMSiteSummaryStatus.qtyPOFixedPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedReceipts,
						Sum<PMSiteSummaryStatus.qtySODropShip,
						Sum<PMSiteSummaryStatus.qtyPODropShipOrders,
						Sum<PMSiteSummaryStatus.qtyPODropShipPrepared,
						Sum<PMSiteSummaryStatus.qtyPODropShipReceipts,
						Sum<PMSiteSummaryStatus.qtyINAssemblySupply,
						Sum<PMSiteSummaryStatus.qtyINAssemblyDemand,
						Sum<PMSiteSummaryStatus.qtyInTransitToProduction,
						Sum<PMSiteSummaryStatus.qtyProductionSupplyPrepared,
						Sum<PMSiteSummaryStatus.qtyProductionSupply,
						Sum<PMSiteSummaryStatus.qtyPOFixedProductionPrepared,
						Sum<PMSiteSummaryStatus.qtyPOFixedProductionOrders,
						Sum<PMSiteSummaryStatus.qtyProductionDemandPrepared,
						Sum<PMSiteSummaryStatus.qtyProductionDemand,
						Sum<PMSiteSummaryStatus.qtyProductionAllocated,
						Sum<PMSiteSummaryStatus.qtySOFixedProduction,
						Sum<PMSiteSummaryStatus.qtyProdFixedPurchase,
						Sum<PMSiteSummaryStatus.qtyProdFixedProduction,
						Sum<PMSiteSummaryStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMSiteSummaryStatus.qtyProdFixedProdOrders,
						Sum<PMSiteSummaryStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMSiteSummaryStatus.qtyProdFixedSalesOrders,

						GroupBy<PMSiteSummaryStatus.inventoryID,
						GroupBy<PMSiteSummaryStatus.subItemID,
						GroupBy<PMSiteSummaryStatus.siteID>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<SiteStatusKey, PMSiteSummaryStatus> result = new Dictionary<SiteStatusKey, PMSiteSummaryStatus>();
			foreach (PMSiteSummaryStatus item in select.Select(ProjectDefaultAttribute.NonProject()))
			{
				result.Add(new SiteStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value), item);
			}

			return result;
		}

		private Dictionary<LocationStatusKey, PMLocationStatus> GetLocationStatusForNonProject()
		{
			var select = new PXSelectGroupBy<PMLocationStatus,
				Where<PMLocationStatus.taskID, Equal<int0>>,
				Aggregate<
						Sum<PMLocationStatus.qtyOnHand,
						Sum<PMLocationStatus.qtyAvail,
						Sum<PMLocationStatus.qtyNotAvail,
						Sum<PMLocationStatus.qtyExpired,
						Sum<PMLocationStatus.qtyHardAvail,
						Sum<PMLocationStatus.qtyActual,
						Sum<PMLocationStatus.qtyFSSrvOrdBooked,
						Sum<PMLocationStatus.qtyFSSrvOrdAllocated,
						Sum<PMLocationStatus.qtyFSSrvOrdPrepared,
						Sum<PMLocationStatus.qtySOBackOrdered,
						Sum<PMLocationStatus.qtySOPrepared,
						Sum<PMLocationStatus.qtySOBooked,
						Sum<PMLocationStatus.qtySOShipped,
						Sum<PMLocationStatus.qtySOShipping,
						Sum<PMLocationStatus.qtyINIssues,
						Sum<PMLocationStatus.qtyINReceipts,
						Sum<PMLocationStatus.qtyInTransit,
						Sum<PMLocationStatus.qtyInTransitToSO,
						Sum<PMLocationStatus.qtyPOReceipts,
						Sum<PMLocationStatus.qtyPOPrepared,
						Sum<PMLocationStatus.qtyPOOrders,
						Sum<PMLocationStatus.qtyFixedFSSrvOrd,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrd,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMLocationStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMLocationStatus.qtySOFixed,
						Sum<PMLocationStatus.qtyPOFixedOrders,
						Sum<PMLocationStatus.qtyPOFixedPrepared,
						Sum<PMLocationStatus.qtyPOFixedReceipts,
						Sum<PMLocationStatus.qtySODropShip,
						Sum<PMLocationStatus.qtyPODropShipOrders,
						Sum<PMLocationStatus.qtyPODropShipPrepared,
						Sum<PMLocationStatus.qtyPODropShipReceipts,
						Sum<PMLocationStatus.qtyINAssemblySupply,
						Sum<PMLocationStatus.qtyINAssemblyDemand,
						Sum<PMLocationStatus.qtyInTransitToProduction,
						Sum<PMLocationStatus.qtyProductionSupplyPrepared,
						Sum<PMLocationStatus.qtyProductionSupply,
						Sum<PMLocationStatus.qtyPOFixedProductionPrepared,
						Sum<PMLocationStatus.qtyPOFixedProductionOrders,
						Sum<PMLocationStatus.qtyProductionDemandPrepared,
						Sum<PMLocationStatus.qtyProductionDemand,
						Sum<PMLocationStatus.qtyProductionAllocated,
						Sum<PMLocationStatus.qtySOFixedProduction,
						Sum<PMLocationStatus.qtyProdFixedPurchase,
						Sum<PMLocationStatus.qtyProdFixedProduction,
						Sum<PMLocationStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMLocationStatus.qtyProdFixedProdOrders,
						Sum<PMLocationStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMLocationStatus.qtyProdFixedSalesOrders,

						GroupBy<PMLocationStatus.inventoryID,
						GroupBy<PMLocationStatus.subItemID,
						GroupBy<PMLocationStatus.siteID,
						GroupBy<PMLocationStatus.locationID>>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<LocationStatusKey, PMLocationStatus> result = new Dictionary<LocationStatusKey, PMLocationStatus>();
			foreach (PMLocationStatus item in select.Select())
			{
				result.Add(new LocationStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value), item);
			}

			return result;
		}

		private Dictionary<LotSerialStatusKey, PMLotSerialStatus> GetLotSerialStatusForNonProjects()
		{
			var select = new PXSelectGroupBy<PMLotSerialStatus,
				Where<PMLotSerialStatus.taskID, Equal<int0>>,
				Aggregate<
						Sum<PMLotSerialStatus.qtyOnHand,
						Sum<PMLotSerialStatus.qtyAvail,
						Sum<PMLotSerialStatus.qtyNotAvail,
						Sum<PMLotSerialStatus.qtyExpired,
						Sum<PMLotSerialStatus.qtyHardAvail,
						Sum<PMLotSerialStatus.qtyActual,
						Sum<PMLotSerialStatus.qtyFSSrvOrdBooked,
						Sum<PMLotSerialStatus.qtyFSSrvOrdAllocated,
						Sum<PMLotSerialStatus.qtyFSSrvOrdPrepared,
						Sum<PMLotSerialStatus.qtySOBackOrdered,
						Sum<PMLotSerialStatus.qtySOPrepared,
						Sum<PMLotSerialStatus.qtySOBooked,
						Sum<PMLotSerialStatus.qtySOShipped,
						Sum<PMLotSerialStatus.qtySOShipping,
						Sum<PMLotSerialStatus.qtyINIssues,
						Sum<PMLotSerialStatus.qtyINReceipts,
						Sum<PMLotSerialStatus.qtyInTransit,
						Sum<PMLotSerialStatus.qtyInTransitToSO,
						Sum<PMLotSerialStatus.qtyPOReceipts,
						Sum<PMLotSerialStatus.qtyPOPrepared,
						Sum<PMLotSerialStatus.qtyPOOrders,
						Sum<PMLotSerialStatus.qtyFixedFSSrvOrd,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrd,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrdPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedFSSrvOrdReceipts,
						Sum<PMLotSerialStatus.qtySOFixed,
						Sum<PMLotSerialStatus.qtyPOFixedOrders,
						Sum<PMLotSerialStatus.qtyPOFixedPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedReceipts,
						Sum<PMLotSerialStatus.qtySODropShip,
						Sum<PMLotSerialStatus.qtyPODropShipOrders,
						Sum<PMLotSerialStatus.qtyPODropShipPrepared,
						Sum<PMLotSerialStatus.qtyPODropShipReceipts,
						Sum<PMLotSerialStatus.qtyINAssemblySupply,
						Sum<PMLotSerialStatus.qtyINAssemblyDemand,
						Sum<PMLotSerialStatus.qtyInTransitToProduction,
						Sum<PMLotSerialStatus.qtyProductionSupplyPrepared,
						Sum<PMLotSerialStatus.qtyProductionSupply,
						Sum<PMLotSerialStatus.qtyPOFixedProductionPrepared,
						Sum<PMLotSerialStatus.qtyPOFixedProductionOrders,
						Sum<PMLotSerialStatus.qtyProductionDemandPrepared,
						Sum<PMLotSerialStatus.qtyProductionDemand,
						Sum<PMLotSerialStatus.qtyProductionAllocated,
						Sum<PMLotSerialStatus.qtySOFixedProduction,
						Sum<PMLotSerialStatus.qtyProdFixedPurchase,
						Sum<PMLotSerialStatus.qtyProdFixedProduction,
						Sum<PMLotSerialStatus.qtyProdFixedProdOrdersPrepared,
						Sum<PMLotSerialStatus.qtyProdFixedProdOrders,
						Sum<PMLotSerialStatus.qtyProdFixedSalesOrdersPrepared,
						Sum<PMLotSerialStatus.qtyProdFixedSalesOrders,

						GroupBy<PMLotSerialStatus.inventoryID,
						GroupBy<PMLotSerialStatus.subItemID,
						GroupBy<PMLotSerialStatus.siteID,
						GroupBy<PMLotSerialStatus.locationID,
						GroupBy<PMLotSerialStatus.lotSerialNbr>>>>>
						>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>(this);

			Dictionary<LotSerialStatusKey, PMLotSerialStatus> result = new Dictionary<LotSerialStatusKey, PMLotSerialStatus>();
			foreach (PMLotSerialStatus item in select.Select())
			{
				result.Add(new LotSerialStatusKey(item.InventoryID.Value, item.SubItemID.Value, item.SiteID.Value, item.LocationID.Value, item.LotSerialNbr), item);
			}

			return result;
		}

		private struct LocationStatusKey
		{
			public readonly int InventoryID;
			public readonly int SubItemID;
			public readonly int SiteID;
			public readonly int LocationID;

			public LocationStatusKey(int inventoryID, int subItemID, int siteID, int locationID)
			{
				InventoryID = inventoryID;
				SubItemID = subItemID;
				LocationID = locationID;
				SiteID = siteID;
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;
					hash = hash * 23 + InventoryID.GetHashCode();
					hash = hash * 23 + SubItemID.GetHashCode();
					hash = hash * 23 + SiteID.GetHashCode();
					hash = hash * 23 + LocationID.GetHashCode();
					return hash;
				}
			}
		}

		private struct LotSerialStatusKey
		{
			public readonly int InventoryID;
			public readonly int SubItemID;
			public readonly int SiteID;
			public readonly int LocationID;
			public readonly string LotSerialNbr;

			public LotSerialStatusKey(int inventoryID, int subItemID, int siteID, int locationID, string lotSerialNbr)
			{
				InventoryID = inventoryID;
				SubItemID = subItemID;
				LocationID = locationID;
				SiteID = siteID;
				LotSerialNbr = lotSerialNbr;
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;
					hash = hash * 23 + InventoryID.GetHashCode();
					hash = hash * 23 + SubItemID.GetHashCode();
					hash = hash * 23 + SiteID.GetHashCode();
					hash = hash * 23 + LocationID.GetHashCode();
					hash = hash * 23 + LotSerialNbr.GetHashCode();
					return hash;
				}
			}
		}

		private struct SiteStatusKey
		{
			public readonly int InventoryID;
			public readonly int SubItemID;
			public readonly int SiteID;

			public SiteStatusKey(int inventoryID, int subItemID, int siteID)
			{
				InventoryID = inventoryID;
				SubItemID = subItemID;
				SiteID = siteID;
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;
					hash = hash * 23 + InventoryID.GetHashCode();
					hash = hash * 23 + SubItemID.GetHashCode();
					hash = hash * 23 + SiteID.GetHashCode();
					return hash;
				}
			}
		}
	}
}
