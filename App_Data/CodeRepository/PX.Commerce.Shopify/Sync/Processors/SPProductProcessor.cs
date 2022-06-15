using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Commerce.Objects.Substitutes;
using PX.Data;
using PX.Objects.IN.RelatedItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
namespace PX.Commerce.Shopify
{
	public abstract class SPProductProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		public SPHelper helper = PXGraph.CreateInstance<SPHelper>();

		protected ProductRestDataProvider productDataProvider;
		protected IChildRestDataProvider<ProductVariantData> productVariantDataProvider;
		protected IChildRestDataProvider<ProductImageData> productImageDataProvider;
		protected IEnumerable<ProductVariantData> ExternProductVariantData = new List<ProductVariantData>();
		protected Dictionary<int, string> SalesCategories;

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			productDataProvider = new ProductRestDataProvider(client);
			productVariantDataProvider = new ProductVariantRestDataProvider(client);
			productImageDataProvider = new ProductImageRestDataProvider(client);

			SalesCategories = new Dictionary<int, string>();
		}

		public virtual void SaveImages(IMappedEntity obj, List<InventoryFileUrls> urls)
		{
			var fileURLs = urls?.Where(x => x.FileType?.Value == BCCaptions.Image && !string.IsNullOrEmpty(x.FileURL?.Value))?.ToList();
			if (fileURLs == null || fileURLs.Count() == 0) return;

			List<ProductImageData> imageList = null;
			foreach (var image in fileURLs)
			{
				ProductImageData productImageData = null;
				try
				{
					if (imageList == null)
						imageList = productImageDataProvider.GetAll(obj.ExternID, new FilterWithFields() { Fields = "id,product_id,src,variant_ids,position" }).ToList();
					if (imageList?.Count > 0)
					{
						productImageData = imageList.FirstOrDefault(x => (x.Metafields != null && x.Metafields.Any(m => string.Equals(m.Key, ShopifyConstants.ProductImage, StringComparison.OrdinalIgnoreCase)
							&& string.Equals(m.Value, image.FileURL.Value, StringComparison.OrdinalIgnoreCase))));
						if (productImageData != null)
						{
							if (obj.Details?.Any(x => x.EntityType == BCEntitiesAttribute.ProductImage && x.LocalID == image.NoteID?.Value) == false)
							{
								obj.AddDetail(BCEntitiesAttribute.ProductImage, image.NoteID.Value, productImageData.Id.ToString());
							}
							continue;
						}
					};
					productImageData = new ProductImageData()
					{
						Src = Uri.EscapeUriString(System.Web.HttpUtility.UrlDecode(image.FileURL.Value)),
						Metafields = new List<MetafieldData>() { new MetafieldData() { Key = ShopifyConstants.ProductImage, Value = image.FileURL.Value, Type = ShopifyConstants.ValueType_SingleString, Namespace = BCObjectsConstants.Namespace_Global } },
					};
					var metafields = productImageData.Metafields;
					productImageData = productImageDataProvider.Create(productImageData, obj.ExternID);
					productImageData.Metafields = metafields;
					if (obj.Details?.Any(x => x.EntityType == BCEntitiesAttribute.ProductImage && x.LocalID == image.NoteID?.Value) == false)
					{
						obj.AddDetail(BCEntitiesAttribute.ProductImage, image.NoteID.Value, productImageData.Id.ToString());
					}
					imageList = imageList ?? new List<ProductImageData>();
					imageList.Add(productImageData);
				}
				catch (Exception ex)
				{
					throw new PXException(ex.Message);
				}
			}
		}

		public virtual void SetProductStatus(ProductData data, string status, string availability, string visibility)
		{
			if (availability != BCCaptions.DoNotUpdate)
			{
				if (status.Equals(PX.Objects.IN.Messages.Inactive) || status.Equals(PX.Objects.IN.Messages.NoSales) || status.Equals(PX.Objects.IN.Messages.ToDelete))
				{
					data.Status = ProductStatus.Draft;
					data.Published = false;
				}
				else
				{
					data.Status = ProductStatus.Active;
					if(visibility == BCCaptions.Invisible || availability == BCCaptions.Disabled)
					{
						data.PublishedScope = PublishedScope.Web;
						data.Published = false;
					}
					else
					{
						data.PublishedScope = PublishedScope.Global;
						data.Published = true;
					}
				}
			}
		}

		public override List<Tuple<string, string>> GetExternCustomFieldList(BCEntity entity, EntityInfo entityInfo, ExternCustomFieldInfo customFieldInfo, PropertyInfo objectPropertyInfo)
		{
			List<Tuple<String, String>> fieldsList = new List<Tuple<String, String>>() { Tuple.Create(ShopifyConstants.MetafieldFormat, ShopifyConstants.MetafieldFormat) };

			return fieldsList;
		}
		public override string ValidateExternCustomField(BCEntity entity, EntityInfo entityInfo, ExternCustomFieldInfo customFieldInfo, string sourceObject, string sourceField, string targetObject, string targetField, EntityOperationType direction)
		{
			//Validate the field format
			if (customFieldInfo.Identifier == BCConstants.MetaFields)
			{
				var fieldStrGroup = direction == EntityOperationType.ImportMapping ? sourceField.Split('.') : targetField.Split('.');
				if (fieldStrGroup.Length == 2)
				{
					var keyFieldName = fieldStrGroup[0].Replace("[", "").Replace("]", "").Replace(" ", "");
					if (!string.IsNullOrWhiteSpace(keyFieldName) && string.Equals(keyFieldName, ShopifyConstants.MetafieldFormat, StringComparison.OrdinalIgnoreCase) == false)
						return null;
				}
				return string.Format(BCMessages.InvalidFilter, "Target", ShopifyConstants.MetafieldFormat);
			}
			return null;
		}

		public override object GetExternCustomFieldValue(TEntityBucket entity, ExternCustomFieldInfo customFieldInfo, object sourceData, string sourceObject, string sourceField, out string displayName)
		{
			displayName = null;
			if (customFieldInfo.Identifier == BCConstants.MetaFields)
			{
				return new List<object>() { sourceData };
			}
			else if(customFieldInfo.Identifier == BCAPICaptions.Matrix)
			{
				if (string.IsNullOrWhiteSpace(sourceField))
				{
					return ((TemplateItems)sourceData)?.Matrix ?? new List<MatrixItems>();
				}
				
				var result = GetPropertyValue(sourceData, sourceField, out displayName);
				displayName = sourceData != null && sourceData is MatrixItems ? $"{BCAPICaptions.Matrix}{BCConstants.Arrow} {((MatrixItems)sourceData)?.InventoryID?.Value}" : displayName;
				return result;
			}
			return null;
		}

		public override void SetExternCustomFieldValue(TEntityBucket entity, ExternCustomFieldInfo customFieldInfo, object targetData, string targetObject, string targetField, string sourceObject, object value, IMappedEntity existing)
		{
			if (value != PXCache.NotSetValue && value != null)
			{
				if (customFieldInfo.Identifier == BCConstants.MetaFields)
				{
					var targetinfo = targetField?.Split('.');
					if (targetinfo.Length == 2)
					{
						var nameSpaceField = targetinfo[0].Replace("[", "").Replace("]", "")?.Trim();
						var keyField = targetinfo[1].Replace("[", "").Replace("]", "")?.Trim();
						ProductData data = (ProductData)entity.Primary.Extern;
						ProductData existingProduct = existing?.Extern as ProductData;
						var newMetaField = new MetafieldData()
						{
							Namespace = nameSpaceField,
							Key = keyField,
							Value = Convert.ToString(value),
							Type = ShopifyConstants.ValueType_MultiString
						};
						if (customFieldInfo.ExternEntityType == typeof(ProductData))
						{
							var metaFieldList = data.Metafields = data.Metafields ?? new List<MetafieldData>();
							if (existingProduct != null && existingProduct.Metafields?.Count > 0)
							{
								var existedMetaField = existingProduct.Metafields.FirstOrDefault(x => string.Equals(x.Namespace, nameSpaceField, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Key, keyField, StringComparison.OrdinalIgnoreCase));
								newMetaField.Id = existedMetaField?.Id;
							}
							var matchedData = metaFieldList.FirstOrDefault(x => string.Equals(x.Namespace, nameSpaceField, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Key, keyField, StringComparison.OrdinalIgnoreCase));
							if (matchedData != null)
							{
								matchedData = newMetaField;
							}
							else
								metaFieldList.Add(newMetaField);
						}
						else if (customFieldInfo.ExternEntityType == typeof(ProductVariantData))
						{
							bool anyFound = false;
							string matrixItemFormat = $"{BCAPICaptions.Matrix}{BCConstants.Arrow}";
							foreach (var variantItem in data.Variants)
							{
								var metaFieldList = variantItem.VariantMetafields = variantItem.VariantMetafields ?? new List<MetafieldData>();
								if (sourceObject.StartsWith(matrixItemFormat))
								{
									if (string.Equals(variantItem.Sku, sourceObject.Substring(matrixItemFormat.Length)?.Trim(), StringComparison.OrdinalIgnoreCase))
										anyFound = true;
									else
										continue;
								}
								if (existingProduct?.Variants?.Count > 0)
								{
									var existedVariant = existingProduct.Variants.FirstOrDefault(x => string.Equals(x.Sku, variantItem.Sku, StringComparison.OrdinalIgnoreCase));
									if (existedVariant != null && existedVariant.VariantMetafields?.Count > 0)
									{
										var existedMetaField = existedVariant.VariantMetafields.FirstOrDefault(x => string.Equals(x.Namespace, nameSpaceField, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Key, keyField, StringComparison.OrdinalIgnoreCase));
										newMetaField.Id = existedMetaField?.Id;
									}
								}
								var matchedData = metaFieldList.FirstOrDefault(x => string.Equals(x.Namespace, nameSpaceField, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Key, keyField, StringComparison.OrdinalIgnoreCase));
								if (matchedData != null)
								{
									matchedData = newMetaField;
								}
								else
									metaFieldList.Add(newMetaField);
								if (anyFound) break;
							}
						}
					}
				}
			}
		}
	}
}
