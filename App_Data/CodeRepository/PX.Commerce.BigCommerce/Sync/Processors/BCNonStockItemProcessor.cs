using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.Common;
using Serilog.Context;
using PX.Objects.IN.RelatedItems;
using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;

namespace PX.Commerce.BigCommerce
{
	public class BCNonStockItemEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };
		public override IMappedEntity[] PreProcessors { get => Categories.ToArray(); }

		public MappedNonStockItem Product;

		public List<MappedCategory> Categories = new List<MappedCategory>();

	}

	public class BCNonStockItemRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region NonStockItems
			return base.Restrict<MappedNonStockItem>(mapped, delegate (MappedNonStockItem obj)
			{
				BCBindingExt bindingExt = processor.GetBindingExt<BCBindingExt>();

				if (obj.Local != null && obj.Local.TemplateItemID?.Value != null)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedVariant, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
				}

				if (obj.Local != null && obj.Local.ExportToExternal?.Value == false)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogItemNoExport, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
				}

				if (bindingExt.GiftCertificateItemID != null && obj.Local?.InventoryID?.Value != null)
				{
					PX.Objects.IN.InventoryItem giftCertificate = bindingExt.GiftCertificateItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)processor, bindingExt.GiftCertificateItemID) : null;
					if (giftCertificate != null && obj.Local?.InventoryID?.Value.Trim() == giftCertificate?.InventoryCD?.Trim())
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedGift, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}

				if (bindingExt.GiftWrappingItemID != null && obj.Local?.InventoryID?.Value != null)
				{
					PX.Objects.IN.InventoryItem giftWrapItem = bindingExt.GiftWrappingItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)processor, bindingExt.GiftWrappingItemID) : null;
					if (giftWrapItem != null && obj.Local?.InventoryID?.Value.Trim() == giftWrapItem?.InventoryCD?.Trim())
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedGiftWrapItem, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}

				if (bindingExt.RefundAmountItemID != null && obj.Local?.InventoryID?.Value != null)
				{
					PX.Objects.IN.InventoryItem refundItem = bindingExt.RefundAmountItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)processor, bindingExt.RefundAmountItemID) : null;
					if (refundItem != null && obj.Local?.InventoryID?.Value.Trim() == refundItem?.InventoryCD?.Trim())
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedRefund, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}

				return null;
			});
			#endregion
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.NonStockItem, BCCaptions.NonStockItem,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.NonStockItemMaint),
		ExternTypes = new Type[] { typeof(ProductData) },
		LocalTypes = new Type[] { typeof(NonStockItem) },
		AcumaticaPrimaryType = typeof(PX.Objects.IN.InventoryItem),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.IN.InventoryItem.inventoryCD, Where<PX.Objects.IN.InventoryItem.stkItem, Equal<False>>>),
		URL = "products/{0}/edit",
		Requires = new string[] { }
	)]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.ProductVideo, EntityName = BCCaptions.ProductVideo, AcumaticaType = typeof(BCInventoryFileUrls))]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.RelatedItem, EntityName = BCCaptions.RelatedItem, AcumaticaType = typeof(PX.Objects.IN.InventoryItem))]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-NonStocks" }, PushDestination = BCConstants.PushNotificationDestination,
		WebHookType = typeof(WebHookProduct),
		WebHooks = new String[]
		{
			"store/product/created",
			"store/product/updated",
			"store/product/deleted"
		})]
	[BCProcessorExternCustomField(BCConstants.CustomFields, BigCommerceCaptions.CustomFields, nameof(ProductData.CustomFields), typeof(ProductData))]
	public class BCNonStockItemProcessor : BCProductProcessor<BCStockItemProcessor, BCNonStockItemEntityBucket, MappedNonStockItem>, IProcessor
	{
		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
		}
		#endregion

		#region Common
		public override MappedNonStockItem PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			NonStockItem impl = cbapi.GetByID(localID,
				new NonStockItem()
				{
					ReturnBehavior = ReturnBehavior.OnlySpecified,
					Attributes = new List<AttributeValue>() { new AttributeValue() },
					Categories = new List<CategoryStockItem>() { new CategoryStockItem() },
					CrossReferences = new List<InventoryItemCrossReference>() { new InventoryItemCrossReference() },
					VendorDetails = new List<NonStockItemVendorDetail>() { new NonStockItemVendorDetail() },
					FileUrls = new List<InventoryFileUrls>() { new InventoryFileUrls() },

				});
			if (impl == null) return null;

			MappedNonStockItem obj = new MappedNonStockItem(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedNonStockItem PullEntity(String externID, String externalInfo)
		{
			ProductData data = productDataProvider.GetByID(externID);
			if (data == null) return null;

			MappedNonStockItem obj = new MappedNonStockItem(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			FilterProducts filter = new FilterProducts
			{
				Type = ProductTypes.digital.ToString(),
				MinDateModified = minDateTime == null ? null : minDateTime,
				MaxDateModified = maxDateTime == null ? null : maxDateTime
			};

			IEnumerable<ProductData> datas = productDataProvider.GetAll(filter);

			foreach (ProductData data in datas)
			{
				BCNonStockItemEntityBucket bucket = CreateBucket();

				MappedNonStockItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
				EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
			}
		}
		public override EntityStatus GetBucketForImport(BCNonStockItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			FilterProducts filter = new FilterProducts { Include = "images,modifiers" };
			ProductData data = productDataProvider.GetByID(syncstatus.ExternID, filter);
			if (data == null) return EntityStatus.None;

			MappedNonStockItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			return status;
		}

		public override void MapBucketImport(BCNonStockItemEntityBucket bucket, IMappedEntity existing)
		{
			MappedNonStockItem obj = bucket.Product;

			ProductData data = obj.Extern;
			NonStockItem impl = obj.Local = new NonStockItem();
			StringValue tax = obj.Extern?.TaxClassId != null ? helper.GetSubstituteLocalByExtern(
					BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.TaxClasses),
					taxClasses?.Find(i => i.Id == obj.Extern?.TaxClassId)?.Name, "").ValueField() :
					obj.Local.TaxCategory;

			impl.Custom = GetCustomFieldsForImport();

			//Product
			impl.InventoryID = GetEntityKey(PX.Objects.IN.InventoryAttribute.DimensionName, data.Name).ValueField();
			impl.Description = data.Name.ValueField();
			impl.ItemClass = obj.LocalID == null || existing?.Local == null ? PX.Objects.IN.INItemClass.PK.Find(this, GetBindingExt<BCBindingExt>().NonStockItemClassID)?.ItemClassCD.ValueField() : null;
			impl.CurySpecificPrice = data.Price.ValueField();
			impl.TaxCategory = tax;

			if (GetEntity(BCEntitiesAttribute.SalesCategory)?.IsActive == true)
			{
				if (data.Categories != null) impl.Categories = new List<CategoryStockItem>();
				foreach (int cat in data.Categories ?? new List<int>())
				{
					PX.Objects.IN.INCategory incategory = PXSelectJoin<PX.Objects.IN.INCategory,
					LeftJoin<BCSyncStatus, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>.Select(this, BCEntitiesAttribute.SalesCategory, cat);

					if (incategory == null || incategory.CategoryID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, data.Name);

					impl.Categories.Add(new CategoryStockItem() { CategoryID = incategory.CategoryID.ValueField() });
				}
			}
		}
		public override void SaveBucketImport(BCNonStockItemEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedNonStockItem obj = bucket.Product;

			if (existing?.Local != null) obj.Local.InventoryID = ((NonStockItem)existing.Local).InventoryID.Value.SearchField();

			NonStockItem impl = cbapi.Put<NonStockItem>(obj.Local, obj.LocalID);

			bucket.Product.AddLocal(impl, impl.SyncID, impl.SyncTime);
			UpdateStatus(obj, operation);
		}
		#endregion

		#region Export
		public override IEnumerable<MappedNonStockItem> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			List<ProductData> datas = PullSimilar(((NonStockItem)entity)?.Description?.Value, ((NonStockItem)entity)?.InventoryID?.Value, out uniqueField);
			return datas == null ? null : datas.Select(data => new MappedNonStockItem(data, data.Id.ToString(), data.DateModifiedUT.ToDate()));
		}

		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			NonStockItem item = new NonStockItem()
			{
				InventoryID = new StringReturn(),
				TemplateItemID = new StringReturn(),
				ExportToExternal = new BooleanReturn()
			};
			IEnumerable<NonStockItem> impls = cbapi.GetAll<NonStockItem>(item, minDateTime, maxDateTime, filters);

			if (impls != null)
			{
				int countNum = 0;
				List<IMappedEntity> mappedList = new List<IMappedEntity>();

				foreach (NonStockItem impl in impls)
				{
					IMappedEntity obj = new MappedNonStockItem(impl, impl.SyncID, impl.SyncTime);
					mappedList.Add(obj);
					countNum++;
					if (countNum % BatchFetchCount == 0)
					{
						ProcessMappedListForExport(ref mappedList);
					}
				}
				if (mappedList.Any())
				{
					ProcessMappedListForExport(ref mappedList);
				}
			}
		}
		public override EntityStatus GetBucketForExport(BCNonStockItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			NonStockItem impl = cbapi.GetByID(syncstatus.LocalID,
				new NonStockItem()
				{
					ReturnBehavior = ReturnBehavior.OnlySpecified,
					Attributes = new List<AttributeValue>() { new AttributeValue() },
					Categories = new List<CategoryStockItem>() { new CategoryStockItem() },
					CrossReferences = new List<InventoryItemCrossReference>() { new InventoryItemCrossReference() },
					VendorDetails = new List<NonStockItemVendorDetail>() { new NonStockItemVendorDetail()},
					FileUrls= new List<InventoryFileUrls>() { new InventoryFileUrls()},

				}, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

			MappedNonStockItem obj = bucket.Product = bucket.Product.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			if (GetEntity(BCEntitiesAttribute.SalesCategory)?.IsActive == true)
			{
				if (obj.Local.Categories != null)
				{
					foreach (CategoryStockItem item in obj.Local.Categories)
					{
						BCSyncStatus result = PXSelectJoin<BCSyncStatus,
							InnerJoin<PX.Objects.IN.INCategory, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
							Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
								And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
								And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
								And<PX.Objects.IN.INCategory.categoryID, Equal<Required<PX.Objects.IN.INCategory.categoryID>>>>>>>
							.Select(this, BCEntitiesAttribute.SalesCategory, item.CategoryID.Value);
						if (result != null && result.ExternID != null && result.LocalID != null) continue;

						BCItemSalesCategory implCat = cbapi.Get<BCItemSalesCategory>(new BCItemSalesCategory() { CategoryID = new IntSearch() { Value = item.CategoryID.Value } });
						if (implCat == null) continue;

						MappedCategory mappedCategory = new MappedCategory(implCat, implCat.SyncID, implCat.SyncTime);
						EntityStatus mappedCategoryStatus = EnsureStatus(mappedCategory, SyncDirection.Export);
						if (mappedCategoryStatus == EntityStatus.Deleted)
							throw new PXException(BCMessages.CategoryIsDeletedForItem, item.CategoryID.Value, impl.Description.Value);
						if (mappedCategoryStatus == EntityStatus.Pending)
							bucket.Categories.Add(mappedCategory);
					}
				}
			}
			return status;
		}

		public override void MapBucketExport(BCNonStockItemEntityBucket bucket, IMappedEntity existing)
		{
			MappedNonStockItem obj = bucket.Product;

			NonStockItem impl = obj.Local;

			//Existing item and store Availability Policies
			ProductData existingData = existing?.Extern as ProductData;
			string storeAvailability = BCItemAvailabilities.Convert(GetBindingExt<BCBindingExt>().Availability);
			string currentAvail = obj.Extern?.Availability;

			ProductData data = obj.Extern = new ProductData();

			//Inventory Item
			data.Name = impl.Description?.Value;
			data.Description = helper.ClearHTMLContent(impl.Content?.Value);
			data.Type = impl.RequireShipment?.Value == true ? ProductsType.Physical.ToEnumMemberAttrValue() : ProductsType.Digital.ToEnumMemberAttrValue();
			data.Price = helper.RoundToStoreSetting(impl.CurySpecificPrice?.Value);
			data.Weight = impl.DimensionWeight.Value;
			data.CostPrice = impl.CurrentStdCost.Value;
			data.RetailPrice = helper.RoundToStoreSetting(impl.CurySpecificMSRP?.Value);
			data.Sku = impl.InventoryID?.Value;
			data.TaxClassId = taxClasses?.Find(i => string.Equals(i.Name, helper.GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxCategorySubstitutionListID, impl.TaxCategory?.Value, String.Empty)))?.Id;

			//custom field mapping
			data.PageTitle = impl.PageTitle?.Value;
			data.MetaDescription = impl.MetaDescription?.Value;
			data.MetaKeywords = impl.MetaKeywords?.Value != null ? impl.MetaKeywords?.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;
			data.SearchKeywords = impl.SearchKeywords?.Value;
			var vendor = impl.VendorDetails?.FirstOrDefault(v => v.Default?.Value == true);
			if (vendor != null)
				data.MPN = impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == BCCaptions.VendorPartNumber && x.VendorOrCustomer?.Value == vendor.VendorID?.Value)?.AlternateID?.Value;
			if (!string.IsNullOrWhiteSpace(impl.SalesUnit?.Value))
				data.Upc = (impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == BCCaptions.Barcode && x.UOM?.Value == impl.SalesUnit?.Value) ??
					impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == BCCaptions.Barcode && string.IsNullOrWhiteSpace(x.UOM?.Value)))?.AlternateID?.Value;
			MapCustomUrl(existing, impl.CustomURL?.Value, data);
			data.RelatedProducts = MapRelatedItems(obj);

			string visibility = impl?.Visibility?.Value;
			if (visibility == null || visibility == BCCaptions.StoreDefault) visibility = BCItemVisibility.Convert(GetBindingExt<BCBindingExt>().Visibility);
			switch (visibility)
			{
				case BCCaptions.Visible:
					{
					data.IsVisible = true;
						data.IsFeatured = false;
					break;
					}
				case BCCaptions.Featured:
					{
						data.IsVisible = true;
						data.IsFeatured = true;
						break;
					}
				case BCCaptions.Invisible:
				default:
					{
						data.IsFeatured = false;
						data.IsVisible = false;
						break;
					}
			}
			Boolean isItemActive = !(impl.ItemStatus?.Value == PX.Objects.IN.Messages.Inactive || impl.ItemStatus?.Value == PX.Objects.IN.Messages.ToDelete || impl.ItemStatus?.Value == PX.Objects.IN.Messages.NoSales);
			string availability = impl?.Availability?.Value;
			if (availability == null || availability == BCCaptions.StoreDefault) availability = storeAvailability;
			if (availability != BCCaptions.DoNotUpdate)
			{
				data.Availability = "disabled";
				data.InventoryTracking = "none";
				if (isItemActive)
				{
					switch (availability)
					{
						case BCCaptions.AvailableTrack:
							{
								data.Availability = "available";
								data.InventoryTracking = "product";
								break;
							}
						case BCCaptions.AvailableSkip:
							{
								data.Availability = "available";
								break;
							}
						case BCCaptions.PreOrder:
							{
								data.Availability = "preorder";
								break;
							}
						case BCCaptions.Disabled:
							{
								data.Availability = "disabled";
								break;
							}
					}
				}
			}
			else data.Availability = currentAvail;

			if (GetEntity(BCEntitiesAttribute.SalesCategory)?.IsActive == true)
			{
				if (data.Categories == null) data.Categories = new List<int>();

				foreach (PXResult<PX.Objects.IN.INCategory, PX.Objects.IN.INItemCategory, PX.Objects.IN.InventoryItem, BCSyncStatus> result in PXSelectJoin<PX.Objects.IN.INCategory,
					InnerJoin<PX.Objects.IN.INItemCategory, On<PX.Objects.IN.INItemCategory.categoryID, Equal<PX.Objects.IN.INCategory.categoryID>>,
					InnerJoin<PX.Objects.IN.InventoryItem, On<PX.Objects.IN.InventoryItem.inventoryID, Equal<PX.Objects.IN.INItemCategory.inventoryID>>,
					LeftJoin<BCSyncStatus, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<PX.Objects.IN.InventoryItem.noteID, Equal<Required<PX.Objects.IN.InventoryItem.noteID>>>>>>>
							.Select(this, BCEntitiesAttribute.SalesCategory, obj.LocalID))
				{
					BCSyncStatus status = result.GetItem<BCSyncStatus>();
					if (status == null || status.ExternID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, impl.Description.Value);

					data.Categories.Add(status.ExternID.ToInt().Value);
				}
				if ((data.Categories ?? Enumerable.Empty<int>()).Empty_())
				{
					String categories = GetBindingExt<BCBindingExt>().NonStockSalesCategoriesIDs;
					if (!String.IsNullOrEmpty(categories))
					{
						Int32?[] categoriesArray = categories.Split(',').Select(c => { return Int32.TryParse(c, out Int32 i) ? (int?)i : null; }).Where(i => i != null).ToArray();

						foreach (BCSyncStatus status in PXSelectJoin<BCSyncStatus,
							LeftJoin<PX.Objects.IN.INCategory, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
							Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
								And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
								And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
								And<PX.Objects.IN.INCategory.categoryID, In<Required<PX.Objects.IN.INCategory.categoryID>>>>>>>
								.Select(this, BCEntitiesAttribute.SalesCategory, categoriesArray))
						{
							if (status == null || status.ExternID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, impl.Description.Value);

							data.Categories.Add(status.ExternID.ToInt().Value);
						}
					}
				}
			}
		}

		public override object GetAttribute(BCNonStockItemEntityBucket bucket, string attributeID)
		{
			MappedNonStockItem obj = bucket.Product;
			NonStockItem impl = obj.Local;
			return impl.Attributes?.Where(x => string.Equals(x?.AttributeDescription?.Value, attributeID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

		}
		public override void AddAttributeValue(BCNonStockItemEntityBucket bucket, string attributeID, object attributeValue)
		{
			MappedNonStockItem obj = bucket.Product;
			NonStockItem impl = obj.Local;
			impl.Attributes = impl.Attributes ?? new List<AttributeValue>();
			AttributeValue attribute = new AttributeValue();
			attribute.AttributeID = new StringValue() { Value = attributeID };
			attribute.Value = new StringValue() { Value = attributeValue?.ToString() };
			attribute.ValueDescription = new StringValue() { Value = attributeValue?.ToString() };
			impl.Attributes.Add(attribute);
		}

		public override void SaveBucketExport(BCNonStockItemEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedNonStockItem obj = bucket.Product;

			ProductData data = null;

			obj.Extern.CustomFieldsData = ExportCustomFields(obj, obj.Extern.CustomFields, data);

			if (obj.ExternID == null)
				data = productDataProvider.Create(obj.Extern);
			else
				data = productDataProvider.Update(obj.Extern, obj.ExternID.ToInt().Value);

			obj.AddExtern(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());

			SaveImages(obj, obj.Local?.FileUrls);
			SaveVideos(obj, obj.Local?.FileUrls);

			UpdateStatus(obj, operation);
			if (data != null)
				UpdateRelatedItems(obj);
		}
		#endregion
	}
}
