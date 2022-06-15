using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.IN;
using PX.Objects.Common;
using Serilog.Context;
using System.Reflection;

namespace PX.Commerce.BigCommerce
{
	public class BCAvailabilityEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };

		public MappedAvailability Product;
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.ProductAvailability, BCCaptions.ProductAvailability,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.InventorySummaryEnq),
		ExternTypes = new Type[] { },
		LocalTypes = new Type[] { },
		GIScreenID = BCConstants.GenericInquiryAvailability,
		GIResult = typeof(StorageDetails),
		AcumaticaPrimaryType = typeof(InventoryItem),
		URL = "products/{0}/edit",
		Requires = new string[] { },
		RequiresOneOf = new string[] { BCEntitiesAttribute.StockItem + "." + BCEntitiesAttribute.ProductWithVariant }
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-AvailabilityStockItem", "BC-PUSH-AvailabilityTemplates" }, PushDestination = BCConstants.PushNotificationDestination)]
	public class BCAvailabilityProcessor : AvailabilityProcessorBase<BCStockItemProcessor, BCAvailabilityEntityBucket, MappedAvailability>, IProcessor
	{
		protected ProductRestDataProvider productDataProvider;
		protected ProductVariantBatchRestDataProvider variantBatchRestDataProvider;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			productDataProvider = new ProductRestDataProvider(BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>()));
			variantBatchRestDataProvider = new ProductVariantBatchRestDataProvider(BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>()));
		}
		#endregion

		#region Common
		public override void NavigateLocal(IConnector connector, ISyncStatus status)
		{
			PX.Objects.IN.InventorySummaryEnq extGraph = PXGraph.CreateInstance<PX.Objects.IN.InventorySummaryEnq>();
			InventorySummaryEnqFilter filter = extGraph.Filter.Current;
			InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.noteID, Equal<Required<InventoryItem.noteID>>>>.Select(this, status.LocalID);
			filter.InventoryID = item.InventoryID;

			if (filter.InventoryID != null)
				throw new PXRedirectRequiredException(extGraph, "Navigation") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}
		public override MappedAvailability PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			if (localID == null) return null;
			DateTime? timeStamp = fields.Where(f => f.Key.EndsWith(nameof(BCEntity.LastModifiedDateTime), StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToDate();
			int? parentID = fields.Where(f => f.Key.EndsWith(nameof(BCSyncStatus.SyncID), StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToInt();
			localID = fields.Where(f => f.Key.EndsWith("TemplateItem_noteID", StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToGuid() ?? localID;
			return new MappedAvailability(new StorageDetailsResult(), localID, timeStamp, parentID);
		}
		#endregion

		#region Import
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void FetchBucketsImport()
		{

		}
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override List<BCAvailabilityEntityBucket> GetBucketsImport(List<BCSyncStatus> ids)
		{
			return null;
		}
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void MapBucketImport(BCAvailabilityEntityBucket bucket, IMappedEntity existing)
		{
			throw new NotImplementedException();
		}
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void SaveBucketsImport(List<BCAvailabilityEntityBucket> buckets)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Export

		public override void FetchBucketsExport()
		{
			DateTime? startDate = Operation.PrepareMode == PrepareMode.Incremental ? GetEntityStats()?.LastIncrementalExportDateTime : Operation.StartDate;
			IEnumerable<StorageDetailsResult> results = Enumerable.Empty<StorageDetailsResult>();
			if (GetEntity(BCEntitiesAttribute.StockItem)?.IsActive == true)
			{
				results = results.Concat(FetchStorageDetails(GetBindingExt<BCBindingExt>(), startDate, Operation.EndDate, FetchAvailabilityBaseCommandForStockItem));
			}
			if (GetEntity(BCEntitiesAttribute.ProductWithVariant)?.IsActive == true)
			{
				results = results.Concat(FetchStorageDetails(GetBindingExt<BCBindingExt>(), startDate, Operation.EndDate, FetchAvailabilityBaseCommandForTemplateItem));
			}

			foreach (StorageDetailsResult lineItem in results)
			{
				DateTime? lastModified = new DateTime?[] { lineItem.SiteLastModifiedDate?.Value, lineItem.InventoryLastModifiedDate?.Value }.Where(d => d != null).Select(d => d.Value).Max();
				MappedAvailability obj = new MappedAvailability(lineItem, lineItem.InventoryNoteID.Value, lastModified, lineItem.ParentSyncId.Value);
				EntityStatus status = EnsureStatus(obj, SyncDirection.Export);
				if (status == EntityStatus.Deleted) status = EnsureStatus(obj, SyncDirection.Export, resync: true);
			}
		}

		public override List<BCAvailabilityEntityBucket> GetBucketsExport(List<BCSyncStatus> syncIDs)
		{
			BCEntityStats entityStats = GetEntityStats();
			BCBinding binding = GetBinding();
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			List<BCAvailabilityEntityBucket> buckets = new List<BCAvailabilityEntityBucket>();

			var warehouses = new Dictionary<int, INSite>();
			List<BCLocations> locationMappings = BCLocationSlot.GetBCLocations(bindingExt.BindingID);
			Dictionary<int, Dictionary<int, PX.Objects.IN.INLocation>> siteLocationIDs = BCLocationSlot.GetWarehouseLocations(bindingExt.BindingID);

			if (bindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse)
			{
				warehouses = BCLocationSlot.GetWarehouses(bindingExt.BindingID);
			}
			Boolean anyLocation = locationMappings.Any(x => x.LocationID != null);

			IEnumerable<StorageDetailsResult> response = GetStorageDetailsResults(bindingExt, syncIDs);

			if (response == null || response.Any() == false) return buckets;

			List<StorageDetailsResult> results = new List<StorageDetailsResult>();
			foreach (var detailsGroup in response.GroupBy(r => new { InventoryID = r.InventoryCD?.Value, /*SiteID = r.SiteID?.Value*/ }))
			{
				if (detailsGroup.First().Availability?.Value == BCItemAvailabilities.DoNotUpdate)
					continue;
				StorageDetailsResult result = detailsGroup.First();
				result.SiteLastModifiedDate = detailsGroup.Where(d => d.SiteLastModifiedDate?.Value != null).Select(d => d.SiteLastModifiedDate.Value).Max().ValueField();
				result.LocationLastModifiedDate = detailsGroup.Where(d => d.LocationLastModifiedDate?.Value != null).Select(d => d.LocationLastModifiedDate.Value).Max().ValueField();
				result.SiteOnHand = detailsGroup.Sum(k => k.SiteOnHand?.Value ?? 0m).ValueField();
				result.SiteAvailable = detailsGroup.Sum(k => k.SiteAvailable?.Value ?? 0m).ValueField();
				result.SiteAvailableforIssue = detailsGroup.Sum(k => k.SiteAvailableforIssue?.Value ?? 0m).ValueField();
				result.SiteAvailableforShipping = detailsGroup.Sum(k => k.SiteAvailableforShipping?.Value ?? 0m).ValueField();
				if (bindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse && locationMappings.Any() == false)//if warehouse is specific but nothing is configured in table
				{
					result.LocationOnHand = result.LocationAvailable = result.LocationAvailableforIssue = result.LocationAvailableforShipping = 0m.ValueField();
				}
				else
				{
					if (detailsGroup.Any(i => i.SiteID?.Value != null))
					{
						result.LocationOnHand = anyLocation ? detailsGroup.Where
							(k => warehouses.Count <= 0
							|| (siteLocationIDs.ContainsKey(k.SiteID?.Value ?? 0)
								&& (siteLocationIDs[k.SiteID?.Value ?? 0].Count == 0
								|| (k.LocationID?.Value != null
									&& siteLocationIDs[k.SiteID?.Value ?? 0].ContainsKey(k.LocationID.Value.Value)))))
							.Sum(k => k.LocationOnHand?.Value ?? 0m).ValueField() : null;       
						result.LocationAvailable = anyLocation ? detailsGroup.Where(
							k => warehouses.Count <= 0
							|| (siteLocationIDs.ContainsKey(k.SiteID?.Value ?? 0)
								&& (siteLocationIDs[k.SiteID?.Value ?? 0].Count == 0
								|| (k.LocationID?.Value != null
									&& siteLocationIDs[k.SiteID?.Value ?? 0].ContainsKey(k.LocationID.Value.Value)))))
							.Sum(k => k.LocationAvailable?.Value ?? 0m).ValueField() : null;
						result.LocationAvailableforIssue = anyLocation ? detailsGroup.Where(
							k => warehouses.Count <= 0
							|| (siteLocationIDs.ContainsKey(k.SiteID?.Value ?? 0)
								&& (siteLocationIDs[k.SiteID?.Value ?? 0].Count == 0
								|| (k.LocationID?.Value != null
								&& siteLocationIDs[k.SiteID?.Value ?? 0].ContainsKey(k.LocationID.Value.Value)))))
							.Sum(k => k.LocationAvailableforIssue?.Value ?? 0m).ValueField() : null;
						result.LocationAvailableforShipping = anyLocation ? detailsGroup.Where(
							k => warehouses.Count <= 0
							|| (siteLocationIDs.ContainsKey(k.SiteID?.Value ?? 0)
								&& (siteLocationIDs[k.SiteID?.Value ?? 0].Count == 0
								|| (k.LocationID?.Value != null
								&& siteLocationIDs[k.SiteID?.Value ?? 0].ContainsKey(k.LocationID.Value.Value)))))
							.Sum(k => k.LocationAvailableforShipping?.Value ?? 0m).ValueField() : null;
					}
					else
						result.LocationOnHand = result.LocationAvailable = result.LocationAvailableforIssue = result.LocationAvailableforShipping = null;
				}
				results.Add(result);
			}

			var allVariants = results.Where(x => x.TemplateItemID?.Value != null);

			if (results != null)
			{
				var stockItems = results.Where(x => x.TemplateItemID?.Value == null);
				if (stockItems != null)
				{
					foreach (StorageDetailsResult line in stockItems)
					{
						Guid? noteID = line.InventoryNoteID?.Value;
						DateTime? lastModified;
						if (line.IsTemplate?.Value == true)
						{
							line.VariantDetails = new List<StorageDetailsResult>();
							line.VariantDetails.AddRange(allVariants.Where(x => x.TemplateItemID?.Value == line.InventoryID.Value));
							if (line.VariantDetails.Count() == 0) continue;
							lastModified = line.VariantDetails.Select(x => new DateTime?[] { x.LocationLastModifiedDate?.Value, x.SiteLastModifiedDate?.Value, x.InventoryLastModifiedDate.Value }.Where(d => d != null).Select(d => d.Value).Max()).Max();
						}
						else
						{
							lastModified = new DateTime?[] { line.LocationLastModifiedDate?.Value, line.SiteLastModifiedDate?.Value, line.InventoryLastModifiedDate?.Value }.Where(d => d != null).Select(d => d.Value).Max();
						}

						BCAvailabilityEntityBucket bucket = new BCAvailabilityEntityBucket();
						MappedAvailability obj = bucket.Product = new MappedAvailability(line, noteID, lastModified, line.ParentSyncId.Value);
						EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

						obj.ParentID = line.ParentSyncId.Value;
						if (Operation.PrepareMode != PrepareMode.Reconciliation && Operation.PrepareMode != PrepareMode.Full && status != EntityStatus.Pending && Operation.SyncMethod != SyncMode.Force)
						{
							SynchronizeStatus(bucket.Product, BCSyncOperationAttribute.Reconfiguration);
							Statuses.Cache.Persist(PXDBOperation.Update);
							Statuses.Cache.Persisted(false);
							continue;
						}

						buckets.Add(bucket);
					}
				}
			}

			return buckets;
		}

		public override void MapBucketExport(BCAvailabilityEntityBucket bucket, IMappedEntity existing)
		{
			BCBinding binding = GetBinding();
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();

			MappedAvailability obj = bucket.Product;

			StorageDetailsResult impl = obj.Local;
			ProductQtyData data = obj.Extern = new ProductQtyData();

			data.Id = impl.ProductExternID.Value.ToInt();

			string availability = impl.Availability?.Value;
			string notAvailMode = impl.NotAvailMode?.Value;
			if (availability == BCItemAvailabilities.AvailableTrack)
			{
				data.Availability = "available";

				if (impl.IsTemplate?.Value == true)
				{
					data.InventoryTracking = "variant";
					data.Variants = new List<ProductsVariantData>();
					foreach (var variant in impl.VariantDetails)
					{
						ProductsVariantData variantData = new ProductsVariantData();

						if (variant.VariantExternID.Value != null)
						{
							variantData.Id = variant.VariantExternID.Value.ToInt();
							variantData.ProductId = data.Id;
							variantData.OptionValues = null;
							//Inventory Level
							variantData.InventoryLevel = GetInventoryLevel(bindingExt, variant);
							if (variantData.InventoryLevel < 0)
								variantData.InventoryLevel = 0;
							data.Variants.Add(variantData);

						}
					}
					if (data.Variants.All(x => x.InventoryLevel <= 0))
					{
						switch (notAvailMode)
						{
							case BCItemNotAvailModes.DisableItem:
								data.Availability = "disabled";
								break;
							case BCItemNotAvailModes.PreOrderItem:
								data.Availability = "preorder";
								break;
						}
					}
				}
				else
				{
					data.InventoryTracking = "product";
					//Inventory Level
					data.InventoryLevel = GetInventoryLevel(bindingExt, impl);
					//Not In Stock mode
					if (data.InventoryLevel <= 0)
					{
						data.InventoryLevel = 0;

						switch (notAvailMode)
						{
							case BCItemNotAvailModes.DisableItem:
								data.Availability = "disabled";
								break;
							case BCItemNotAvailModes.PreOrderItem:
								data.Availability = "preorder";
								break;
						}
					}
				}

			}
			else
			{
				data.InventoryTracking = "none";

				switch (availability)
				{
					case BCItemAvailabilities.AvailableSkip: data.Availability = "available"; break;
					case BCItemAvailabilities.PreOrder: data.Availability = "preorder"; break;
					case BCItemAvailabilities.Disabled: data.Availability = "disabled"; break;
				}
			}

			Boolean isItemActive = !(impl.ItemStatus?.Value == InventoryItemStatus.Inactive || impl.ItemStatus?.Value == InventoryItemStatus.MarkedForDeletion || impl.ItemStatus?.Value == InventoryItemStatus.NoSales);
			if (!isItemActive)
			{
				data.Availability = "disabled";
			}

			if (data.Availability == "disabled")
				data.IsPriceHidden = true;

			if (data.Availability == "preorder")
			{//need to assign existing values because syncing PA resets  below field in case of Preorder
				ProductData productdata = productDataProvider.GetByID(data.Id.ToString());
				data.PreorderDate = productdata?.PreorderDate;
				data.IsPreorderOnly = productdata?.IsPreorderOnly;
			}
		}

		public int GetInventoryLevel(BCBindingExt store, StorageDetailsResult detailsResult)
		{
			switch (store.AvailabilityCalcRule)
			{
				case BCAvailabilityLevelsAttribute.Available:
					return (int)(detailsResult.LocationAvailable?.Value ?? detailsResult.SiteAvailable.Value);
				case BCAvailabilityLevelsAttribute.AvailableForShipping:
					return (int)(detailsResult.LocationAvailableforShipping?.Value ?? detailsResult.SiteAvailableforShipping.Value);
				case BCAvailabilityLevelsAttribute.OnHand:
					return (int)(detailsResult.LocationOnHand?.Value ?? detailsResult.SiteOnHand.Value);
				default:
					return 0;
			}
		}

		public override void SaveBucketsExport(List<BCAvailabilityEntityBucket> buckets)
		{
			productDataProvider.UpdateAllQty(buckets.Select(b => b.Product.Extern).ToList(), delegate (ItemProcessCallback<ProductQtyData> callback)
			{
				Exception Error = null;
				BCAvailabilityEntityBucket bucket = buckets[callback.Index];
				if (callback.IsSuccess)
				{
					ProductQtyData data = callback.Result;
					if (bucket.Product.Extern.Variants != null && bucket.Product.Extern.Variants.Count > 0)
					{
						variantBatchRestDataProvider.UpdateAll(bucket.Product.Extern.Variants.ToList(), delegate (ItemProcessCallback<ProductsVariantData> callbackVariant)
						{
							if (!callbackVariant.IsSuccess)
							{
								Error = callbackVariant.Error;
							}
						});
					}
					if (Error == null)
					{
						bucket.Product.AddExtern(data, data.Id?.ToString(), data.DateModified);
						UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternUpdate);
						Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Processed));
					}
					else
					{
						Log(bucket.Product?.SyncID, SyncDirection.Export, Error);
						UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternFailed, Error.ToString());
						Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Error, Error));
					}
				}
				else
				{
					productDataProvider.UpdateAllQty(new List<ProductQtyData>() { bucket.Product.Extern }, delegate (ItemProcessCallback<ProductQtyData> retrycallback)
					{
						if (retrycallback.IsSuccess)
						{
							ProductQtyData data = retrycallback.Result;
							bucket.Product.AddExtern(data, data.Id?.ToString(), data.DateModified);
							UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternUpdate);
							Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Processed));
						}
						else
						{
							if (retrycallback.Error?.ResponceStatusCode == "422") //id not found
							{
								DeleteStatus(BCSyncStatus.PK.Find(this, bucket.Product.ParentID), BCSyncOperationAttribute.NotFound);
								DeleteStatus(bucket.Product, BCSyncOperationAttribute.NotFound);
								Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Deleted));
							}
							else
							{
								Log(bucket.Product?.SyncID, SyncDirection.Export, retrycallback.Error);

								UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternFailed, retrycallback.Error.ToString());
								Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Error, retrycallback.Error));
							}
						}
					});
				}

			});
		}
		#endregion
	}
}
