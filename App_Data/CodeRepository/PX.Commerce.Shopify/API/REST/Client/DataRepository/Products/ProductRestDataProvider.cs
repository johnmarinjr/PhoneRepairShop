using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.Shopify.API.REST
{
	public class ProductRestDataProvider : RestDataProviderBase, IParentRestDataProvider<ProductData>
	{
		protected override string GetListUrl { get; } = "products.json";
		protected override string GetSingleUrl { get; } = "products/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();
		private string GetMetafieldsUrl { get; } = "products/{id}/metafields.json";
		private string GetVariantMetafieldsUrl { get; } = "products/{parent_id}/variants/{id}/metafields.json";

		public ProductRestDataProvider(IShopifyRestClient restClient) : base()
		{
			ShopifyRestClient = restClient;
		}

		public virtual ProductData Create(ProductData entity)
		{
			return base.Create<ProductData, ProductResponse>(entity);
		}

		public virtual ProductData Update(ProductData entity) => Update(entity, entity.Id.ToString());
		public virtual ProductData Update(ProductData entity, string productId)
		{
			var segments = MakeUrlSegments(productId);
			return base.Update<ProductData, ProductResponse>(entity, segments);
		}

		public virtual bool Delete(ProductData entity, string productId) => Delete(productId);

		public virtual bool Delete(string productId)
		{
			var segments = MakeUrlSegments(productId);
			return Delete(segments);
		}

		public virtual IEnumerable<ProductData> GetAll(IFilter filter = null)
		{
			return GetAll<ProductData, ProductsResponse>(filter);
		}

		public virtual ProductData GetByID(string productId) => GetByID(productId, false);

		public virtual ProductData GetByID(string productId, bool includedMetafields = false)
		{
			var segments = MakeUrlSegments(productId);
			var entity = base.GetByID<ProductData, ProductResponse>(segments);
			if (entity != null && includedMetafields == true)
			{
				entity.Metafields = GetMetafieldsForProduct(productId);
				foreach (var variant in entity.Variants)
				{
					variant.VariantMetafields = GetMetafieldsForProductVariant(productId, variant.Id?.ToString());
				}
			}
			return entity;
		}

		public virtual List<MetafieldData> GetMetafieldsForProduct(string productId)
		{
			var request = BuildRequest(GetMetafieldsUrl, nameof(GetMetafieldsForProduct), MakeUrlSegments(productId), null);
			return ShopifyRestClient.GetAll<MetafieldData, MetafieldsResponse>(request).ToList();
		}

		public virtual List<MetafieldData> GetMetafieldsForProductVariant(string productId, string variantId)
		{
			var request = BuildRequest(GetVariantMetafieldsUrl, nameof(GetMetafieldsForProductVariant), MakeUrlSegments(variantId, productId), null);
			return ShopifyRestClient.GetAll<MetafieldData, MetafieldsResponse>(request).ToList();
		}
	}
}
