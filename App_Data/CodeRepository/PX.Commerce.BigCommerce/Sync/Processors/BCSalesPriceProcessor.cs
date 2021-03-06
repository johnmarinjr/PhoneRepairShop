using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.BigCommerce.API.REST.Filters;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.AR;
using PX.Objects.IN;
using Serilog.Context;

namespace PX.Commerce.BigCommerce
{
	public class BCSalesPriceEnityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Price;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };

		public MappedBaseSalesPrice Price;
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.SalesPrice, BCCaptions.SalesPrice,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		ExternTypes = new Type[] { },
		LocalTypes = new Type[] { },
		AcumaticaPrimaryType = typeof(InventoryItem),
		URL = "products/{0}/edit",
		RequiresOneOf = new string[] { BCEntitiesAttribute.StockItem + "." + BCEntitiesAttribute.NonStockItem + "." + BCEntitiesAttribute.ProductWithVariant }
	)]
	public class BCSalesPriceProcessor : BCProcessorBulkBase<BCSalesPriceProcessor, BCSalesPriceEnityBucket, MappedBaseSalesPrice>, IProcessor
	{
		public BCHelper helper = PXGraph.CreateInstance<BCHelper>();

		protected ProductBulkPricingRestDataProvider productBulkPricingRestDataProvider;
		protected ProductBatchBulkRestDataProvider productBatchBulkRestDataProvider;
		protected ProductRestDataProvider productRestDataProvider;
		protected StoreCurrencyDataProvider storCurrencyDataProvider;
		protected ProductVariantBatchRestDataProvider variantBatchRestDataProvider;
		protected List<Currency> currencies;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());

			productBulkPricingRestDataProvider = new ProductBulkPricingRestDataProvider(client);
			productBatchBulkRestDataProvider = new ProductBatchBulkRestDataProvider(client);
			productRestDataProvider = new ProductRestDataProvider(client);
			storCurrencyDataProvider = new StoreCurrencyDataProvider(client);
			variantBatchRestDataProvider = new ProductVariantBatchRestDataProvider(client);

			helper.Initialize(this);
		}
		#endregion
		#region Common
		public override void NavigateLocal(IConnector connector, ISyncStatus status)
		{
			ARSalesPriceMaint extGraph = PXGraph.CreateInstance<ARSalesPriceMaint>();
			ARSalesPriceFilter filter = extGraph.Filter.Current;
			filter.PriceType = PriceTypes.BasePrice;
			InventoryItem inventory = PXSelect<InventoryItem, Where<InventoryItem.noteID, Equal<Required<InventoryItem.noteID>>>>.Select(this, status?.LocalID);
			filter.InventoryID = inventory.InventoryID;

			throw new PXRedirectRequiredException(extGraph, "Navigation") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}
		#endregion

		#region Import
		public override void FetchBucketsImport()
		{
			throw new NotImplementedException();
		}
		public override List<BCSalesPriceEnityBucket> GetBucketsImport(List<BCSyncStatus> ids)
		{
			throw new NotImplementedException();
		}
		public override void SaveBucketsImport(List<BCSalesPriceEnityBucket> buckets)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Export

		public override void FetchBucketsExport()
		{
			GetBucketsExport(null);
		}

		public override List<BCSalesPriceEnityBucket> GetBucketsExport(List<BCSyncStatus> ids)
		{
			BCBinding binding = GetBinding();
			BCEntityStats entityStats = GetEntityStats();

			List<BCSyncStatus> parentEntities = PXSelect<
				BCSyncStatus,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<Where<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						Or<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						Or<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>>>
						>>>>>
				.Select(this, BCEntitiesAttribute.StockItem, BCEntitiesAttribute.NonStockItem, BCEntitiesAttribute.ProductWithVariant)
				.RowCast<BCSyncStatus>()
				.ToList();


			var details = PXSelectJoin<BCSyncDetail,
					InnerJoin<InventoryItem, On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncDetail.localID>>,
					InnerJoin<BCSyncStatus, On<BCSyncStatus.syncID, Equal<BCSyncDetail.syncID>>>>,
					   Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncDetail.entityType, Equal<Required<BCSyncDetail.entityType>>
						>>>>.Select(this, BCEntitiesAttribute.Variant);

			List<BCSyncStatus> CurrentEntities = PXSelect<
				BCSyncStatus,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
					And<BCSyncStatus.deleted, Equal<Required<BCSyncStatus.deleted>
						>>>>>>
				.Select(this, BCEntitiesAttribute.SalesPrice, false)
				.RowCast<BCSyncStatus>()
				.ToList();

			List<BCSalesPriceEnityBucket> buckets = new List<BCSalesPriceEnityBucket>();
			//get base currency from BC store
			currencies = storCurrencyDataProvider.Get();
			var defaultCurrency = currencies.Where(x => x.Default == true).FirstOrDefault();
			if (defaultCurrency == null) return buckets;
			var baseCurrency = Branch.PK.Find(this, binding.BranchID)?.BaseCuryID.ValueField();

			//BQL version
			List<SalesPriceDetail> salesPriceDetails = new List<SalesPriceDetail>();
			foreach (PXResult<ARSalesPrice, InventoryItem, INSite> item in PXSelectJoin<PX.Objects.AR.ARSalesPrice, InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<ARSalesPrice.inventoryID>>,
				LeftJoin<INSite, On<INSite.siteID, Equal<ARSalesPrice.siteID>>>>,
				Where<ARSalesPrice.priceType, Equal<PriceTypes.basePrice>, And<InventoryItem.itemStatus, NotIn3<INItemStatus.inactive, InventoryItemStatus.unknown, INItemStatus.toDelete>>>>.Select(this))
			{
				ARSalesPrice salesPrice = (ARSalesPrice)item;
				InventoryItem inventoryItem = (InventoryItem)item;
				INSite warehouse = (INSite)item;
				if (salesPrice != null && (salesPrice.CuryID ?? baseCurrency?.Value) == defaultCurrency.CurrencyCode && salesPrice.TaxCalcMode != PX.Objects.TX.TaxCalculationMode.Gross)
				{
					salesPriceDetails.Add(new SalesPriceDetail()
					{
						NoteID = salesPrice.NoteID.ValueField(),
						PriceCode = salesPrice.PriceCode.ValueField(),
						UOM = salesPrice.UOM.ValueField(),
						TAX = salesPrice.TaxID.ValueField(),
						Warehouse = warehouse?.SiteCD?.Trim().ValueField(),
						CurrencyID = (salesPrice.CuryID ?? baseCurrency?.Value).ValueField(),
						Promotion = salesPrice.IsPromotionalPrice.ValueField(),
						PriceType = salesPrice.PriceType.ValueField(),
						InventoryID = inventoryItem.InventoryCD.Trim().ValueField(),
						LastModifiedDateTime = salesPrice.LastModifiedDateTime.ValueField(),
						EffectiveDate = salesPrice.EffectiveDate.ValueField(),
						ExpirationDate = salesPrice.ExpirationDate.ValueField(),
						Description = salesPrice.Description.ValueField(),
						BreakQty = (salesPrice.BreakQty ?? 1).ValueField(),
						Price = salesPrice.SalesPrice.ValueField(),
						TemplateItemID = inventoryItem.TemplateItemID,
						InventoryNoteID = inventoryItem.NoteID,
						BaseUnit = inventoryItem.SalesUnit
					});
				}
			}
			if (salesPriceDetails.Count == 0) return buckets;

			var inventories = salesPriceDetails?.GroupBy(x => new { InventoryID = x.InventoryID.Value, x.InventoryNoteID, x.TemplateItemID })?.ToDictionary(d => d.Key, d => d.ToList());
			if (inventories == null) return buckets;

			foreach (var item in inventories)
			{
				BCSyncStatus parent = null;

				bool isVariant = item.Key.TemplateItemID != null;
				BCSyncDetail detail = null;
				if (isVariant)
				{
					detail = details.FirstOrDefault(x => x.GetItem<BCSyncDetail>().LocalID == item.Key.InventoryNoteID);
					parent = parentEntities.FirstOrDefault(p => p.SyncID == detail?.SyncID);
				}
				else
					parent = parentEntities.FirstOrDefault(p => p.LocalID.Value == item.Key.InventoryNoteID);

				if (parent == null || parent?.ExternID == null || parent.Deleted == true ||
						parent?.Status == BCSyncStatusAttribute.Filtered || parent?.Status == BCSyncStatusAttribute.Invalid || parent?.Status == BCSyncStatusAttribute.Skipped)
				{
					LogWarning(Operation.LogScope(), BCMessages.LogPricesSkippedItemNotSynce, item.Key.InventoryID);
					continue; //if Inventory is not found, skip  
				}

				//for variants consider only breakqty 0 or 1
				if (isVariant)
				{
					bool newItems = false;
					if (CurrentEntities == null || CurrentEntities.Count == 0 || !item.Value.Any(i => CurrentEntities.Any(e => e.LocalID == i.NoteID.Value)))
						newItems = true;
					item.Value.RemoveAll(x => x.BreakQty?.Value > 1);
					if (item.Value.Count == 0 && newItems)
						continue;
				}

				DateTime? maxDateTime = null;
				bool updatedAny = false;
				bool forceSync = false;

				//store in sync status by inventory
				SalesPricesInquiry productsSalesPrice = new SalesPricesInquiry();
				productsSalesPrice.ExternalTemplateID = parent.ExternID.ValueField();
				productsSalesPrice.ExternalInventoryID = isVariant ? detail.ExternID.ValueField() : parent.ExternID.ValueField();
				productsSalesPrice.SalesPriceDetails = new List<SalesPriceDetail>();
				productsSalesPrice.Inventory_ID = item.Key.InventoryID;
				productsSalesPrice.Isvariant = isVariant;
				BCSyncStatus current;
				if (ids != null && ids.Count > 0 && (Operation.PrepareMode == PrepareMode.None))
				{
					var localIds = ids.Select(x => x.LocalID);
					current = ids.FirstOrDefault(s => s.LocalID == item.Key.InventoryNoteID);

					if (!localIds.Contains(item.Key.InventoryNoteID)) continue;
				}
				else
					current = CurrentEntities.FirstOrDefault(s => s.LocalID == item.Key.InventoryNoteID);

				foreach (SalesPriceDetail basePrice in item.Value)
				{
					basePrice.Isvariant = isVariant;
					//skip prices that are expired or are not yet effective
					if ((basePrice.BaseUnit != basePrice.UOM?.Value || basePrice.Warehouse?.Value != null)  ||
						(basePrice.ExpirationDate?.Value != null && ((DateTime)basePrice.ExpirationDate.Value).Date < PX.Common.PXTimeZoneInfo.Now.Date) ||
						(basePrice.EffectiveDate?.Value != null && ((DateTime)basePrice.EffectiveDate.Value).Date > PX.Common.PXTimeZoneInfo.Now.Date))
					{
						continue;
					}

					basePrice.SyncTime = basePrice.LastModifiedDateTime?.Value;
					maxDateTime = maxDateTime ?? basePrice.SyncTime;
					maxDateTime = maxDateTime > basePrice.SyncTime ? maxDateTime : basePrice.SyncTime;

					productsSalesPrice.SalesPriceDetails.Add(basePrice);
					updatedAny = true;

				}
				MappedBaseSalesPrice obj = new MappedBaseSalesPrice(productsSalesPrice, item.Key.InventoryNoteID, maxDateTime, parent.SyncID);

				//get difference
				if (isVariant)
				{
					if (current != null) //meaning sales price was synced for variant before  but is no longer present in ERP/or expired
					{
						if (!productsSalesPrice.SalesPriceDetails.Any(x => x.BreakQty?.Value == 0 || x.BreakQty?.Value == 1))
					{
							forceSync = true;
							obj.Local.Delete = true;
						}
					}
				}
				else
				{
					var breakQtyPrices = productsSalesPrice.SalesPriceDetails.Where(x => x.BreakQty.Value > 1)?.ToList();
					obj.SyncID = current?.SyncID;
					if (obj.SyncID != null)
						EnsureDetails(obj);
					if (obj.Details?.Where(x => x.ExternID != current.ExternID)?.Count() > 0 && (breakQtyPrices == null || breakQtyPrices?.Count() == 0)) forceSync = true; // Lines deletd or no longer valid at acumatica 
					var breakqty0Or1 = obj.Details?.FirstOrDefault(x => x.ExternID == current.ExternID);
					if (current != null && breakqty0Or1 != null)
					{
						if (!productsSalesPrice.SalesPriceDetails.Any(x => x.BreakQty?.Value == 0 || x.BreakQty?.Value == 1))
					{
							forceSync = true; // 0 or 1 breakqty is not in sync
							if (breakQtyPrices == null || breakQtyPrices?.Count() == 0) obj.Local.Delete = true;
						}
						if (productsSalesPrice.SalesPriceDetails.Any(x => (x.BreakQty?.Value == 0 || x.BreakQty?.Value == 1) && x.NoteID.Value != breakqty0Or1.LocalID))
						{ //if price becomes effectve today or on date after last sync need to force sync
							forceSync = true;
						}
					}
					if (obj.Details != null)//lines exist for product but some brekqty line is delted
					{

						if (breakQtyPrices != null)
						{
							if (!breakQtyPrices.All(c => obj.Details.Where(x => x.ExternID != current.ExternID).Any(x => x.LocalID == c.NoteID.Value))) forceSync = true;
							if (!obj.Details.Where(x => x.ExternID != current.ExternID).All(c => breakQtyPrices.Any(x => c.LocalID == x.NoteID.Value))) forceSync = true;
						}
					}
				}
				if (!updatedAny && !forceSync) continue;
				EntityStatus status = EnsureStatus(obj, SyncDirection.Export, conditions: forceSync ? Conditions.Resync : Conditions.Default);
				if (Operation.PrepareMode != PrepareMode.Reconciliation && Operation.PrepareMode != PrepareMode.Full
					&& status != EntityStatus.Pending && Operation.SyncMethod != SyncMode.Force) continue;
				buckets.Add(new BCSalesPriceEnityBucket() { Price = obj });
			}
			// If all sales price in acumatica is deleted for inventory
			if (CurrentEntities == null) return buckets;

			foreach (var entity in CurrentEntities)
			{
				BCSyncStatus parent = null;
				if (inventories.Any(x => x.Key.InventoryNoteID == entity.LocalID)) continue;
				InventoryItem inventory = PXSelect<InventoryItem, Where<InventoryItem.noteID, Equal<Required<InventoryItem.noteID>>>>.Select(this, entity.LocalID);
				bool isVariant = inventory.TemplateItemID != null;
				BCSyncDetail detail = null;
				if (isVariant)
				{
					detail = details.FirstOrDefault(x => x.GetItem<BCSyncDetail>().LocalID == inventory.NoteID);
					parent = parentEntities.FirstOrDefault(p => p.SyncID == detail?.SyncID);
				}
				else
					parent = parentEntities.FirstOrDefault(p => p.LocalID.Value == inventory.NoteID);

				if (parent == null || parent?.ExternID == null || parent?.Deleted == true)
				{
					continue; //if Inventory is not found, skip  
				}

				MappedBaseSalesPrice obj = new MappedBaseSalesPrice(new SalesPricesInquiry()
				{
					SalesPriceDetails = new List<SalesPriceDetail>(),
					ExternalTemplateID = parent.ExternID.ValueField(),
					ExternalInventoryID = isVariant ? detail.ExternID.ValueField() : parent.ExternID.ValueField(),
					Isvariant = isVariant,
					Delete = true,
					Inventory_ID = inventory.InventoryCD
				}, entity.LocalID, entity.LastModifiedDateTime, null);
				EnsureStatus(obj, SyncDirection.Export);
				buckets.Add(new BCSalesPriceEnityBucket() { Price = obj });
			}

			return buckets;
		}

		public override void MapBucketExport(BCSalesPriceEnityBucket bucket, IMappedEntity existing)
		{
			MappedBaseSalesPrice obj = bucket.Price;

			BulkPricingWithSalesPrice product = obj.Extern = new BulkPricingWithSalesPrice();
			SalesPricesInquiry salesPricesInquiry = obj.Local;
			product.Data = new List<ProductsBulkPricingRules>();
			product.SalePrice = 0;
			product.Id = obj.Local.ExternalInventoryID.Value.ToInt();
			if (salesPricesInquiry.SalesPriceDetails.Any(x => x.BreakQty?.Value == 0) && salesPricesInquiry.SalesPriceDetails.Any(x => x.BreakQty?.Value == 1))
			{
				var basePrice = salesPricesInquiry.SalesPriceDetails.FirstOrDefault(x => x.BreakQty?.Value == 0);
				salesPricesInquiry.SalesPriceDetails.Remove(basePrice);
			}
			if (salesPricesInquiry.Isvariant)
			{
				product.Variant = new ProductsVariantData();
				product.Variant.Id = salesPricesInquiry.ExternalInventoryID?.Value.ToInt();
				product.Variant.ProductId = salesPricesInquiry.ExternalTemplateID?.Value.ToInt();
				product.Variant.OptionValues = null;
				product.Variant.SalePrice = 0;

				foreach (var impl in salesPricesInquiry.SalesPriceDetails)
				{
					var price = helper.RoundToStoreSetting(impl.Price.Value);
					product.Variant.SalePrice = price;
				}
			}

			else
			{
				salesPricesInquiry.existingId = productBulkPricingRestDataProvider.GetAll(product.Id.ToString()).Select(x => x.Id.Value)?.ToList();
				if (salesPricesInquiry.SalesPriceDetails?.Count() > 0)
					{
					var prices = salesPricesInquiry.SalesPriceDetails.OrderBy(x => x.BreakQty?.Value)?.ToList();
					for (int i = 0; i < prices.Count(); i++)
					{
						ProductsBulkPricingRules bulkPricingRules = new ProductsBulkPricingRules();
						var impl = prices[i];
						var price = helper.RoundToStoreSetting(impl.Price?.Value);
						if (impl.BreakQty?.Value > 1)
						{
							bulkPricingRules.QuantityMax = Convert.ToInt32((i + 1) >= prices.Count() ? 0 : prices[i + 1].BreakQty.Value - 1);
							bulkPricingRules.Type = BCObjectsConstants.Fixed;
							bulkPricingRules.Amount = price;

							bulkPricingRules.QuantityMin = Convert.ToInt32(impl.BreakQty?.Value);
							product.Data.Add(bulkPricingRules);

						}
						else
						{
							product.SalePrice = bulkPricingRules.Amount = price;
						}

					}
				}
			}

		}

		public override void SaveBucketsExport(List<BCSalesPriceEnityBucket> buckets)
		{

			var bulkPrices = buckets.Where(x => !x.Price.Local.Isvariant).ToList();
			foreach (var price in bulkPrices)
			{
				foreach (var id in price.Price.Local.existingId)
					try
					{
						productBulkPricingRestDataProvider.Delete(id.ToString(), price.Price.Extern.Id.ToString());
					}
					catch { }
			}
			productBatchBulkRestDataProvider.UpdateAll(bulkPrices.Select(x => x.Price.Extern).ToList(), delegate (ItemProcessCallback<BulkPricingWithSalesPrice> callback)
				  {
					  BCSalesPriceEnityBucket obj = bulkPrices[callback.Index];
					  if (callback.IsSuccess)
					  {
						  BulkPricingWithSalesPrice data = callback.Result;
					obj.Price.ClearDetails();
						  obj.Price.ExternID = null;
					if (obj.Price.Local.Delete)
						  {
						Statuses.Delete(BCSyncStatus.PK.Find(this, obj.Price.SyncID));
						  }
						  else
						  {
						if (obj.Price.Local.SalesPriceDetails?.Any(x => x.BreakQty?.Value <= 1) == true)
						{
							var localId = obj.Price.Local.SalesPriceDetails?.FirstOrDefault(x => x.BreakQty?.Value <= 1)?.NoteID?.Value;
							if (!obj.Price.Details.Any(x => x.EntityType == BCEntitiesAttribute.BulkPrice && x.LocalID == localId))
								obj.Price.AddDetail(BCEntitiesAttribute.BulkPrice, localId, data.Id.ToString());
						}
						if (data.Data?.Count() > 0)
						{

							foreach (var price in data.Data)
							{
								var localId = obj.Price.Local.SalesPriceDetails?.FirstOrDefault(x => Convert.ToInt32(x.BreakQty?.Value) == price.QuantityMin)?.NoteID?.Value;
								if (!obj.Price.Details.Any(x => x.EntityType == BCEntitiesAttribute.BulkPrice && x.LocalID == localId))
									obj.Price.AddDetail(BCEntitiesAttribute.BulkPrice, localId, price.Id.ToString());
							}
						}
							  obj.Price.AddExtern(obj.Price.Extern, new object[] { data.Id }.KeyCombine(), data.DateModifiedUT.ToDate());
							  UpdateStatus(obj.Price, BCSyncOperationAttribute.ExternUpdate);
						  }
					  }
					  else
					  {
						  Log(obj.Price.SyncID, SyncDirection.Export, callback.Error);
						  UpdateStatus(obj.Price, BCSyncOperationAttribute.ExternFailed, callback.Error.ToString());

					  }
				  });

			var variantPrices = buckets.Where(x => x.Price.Local.Isvariant).ToList();
			variantBatchRestDataProvider.UpdateAll(variantPrices.Select(x => x.Price.Extern.Variant).ToList(), delegate (ItemProcessCallback<ProductsVariantData> callbackVariant)
			{
				BCSalesPriceEnityBucket obj = variantPrices[callbackVariant.Index];
				if (callbackVariant.IsSuccess)
				{
					ProductsVariantData data = callbackVariant.Result;
					obj.Price.ExternID = null;
					if (obj.Price.Local.Delete)
						Statuses.Delete(BCSyncStatus.PK.Find(this, obj.Price.SyncID));
					else
					{
						obj.Price.AddExtern(obj.Price.Extern, new object[] { data.ProductId, data.Id }.KeyCombine(), data.CalculateHash());
						UpdateStatus(obj.Price, BCSyncOperationAttribute.ExternUpdate);
					}
				}
				else
				{
					Log(obj.Price.SyncID, SyncDirection.Export, callbackVariant.Error);
					UpdateStatus(obj.Price, BCSyncOperationAttribute.ExternFailed, callbackVariant.Error.ToString());
				}
			});
		}
		#endregion
	}
}
