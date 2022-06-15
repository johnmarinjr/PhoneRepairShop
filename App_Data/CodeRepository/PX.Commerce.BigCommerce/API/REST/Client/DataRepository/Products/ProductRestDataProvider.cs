using PX.Commerce.Core;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Net;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class ProductRestDataProvider : RestDataProviderV3
	{
		private const string id_string = "id";
		protected override string GetListUrl { get; } = "v3/catalog/products";
		//protected override string GetFullListUrl { get; } = "v3/catalog/products?include=variants,images,custom_fields,primary_image,bulk_pricing_rules";
		protected override string GetSingleUrl { get; } = "v3/catalog/products/{id}";

		public ProductRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		#region IParentRestDataProvider
		public virtual IEnumerable<ProductData> GetAll(IFilter filter = null)
		{
			return GetAll<ProductData, ProductList>(filter);
		}

		public virtual ProductData GetByID(string id, IFilter filter = null)
		{
			var segments = MakeUrlSegments(id);
			var result = GetByID<ProductData, Product>(segments, filter);
			return result.Data;
		}

		public virtual ProductData Create(ProductData productData)
		{
				var product = new Product { Data = productData };
				var result = base.Create<ProductData, Product>(product);
				return result.Data;
		}

		public virtual bool Delete(ProductData productData, int id)
		{
			return Delete(id);
		}

		public virtual bool Delete(int id)
		{
			var segments = MakeUrlSegments(id.ToString());
			return Delete(segments);
		}
		public virtual ProductData Update(ProductData productData, int id)
		{
			var segments = MakeUrlSegments(id.ToString());
			var result = Update<ProductData, Product>(productData, segments);
			return result.Data;
		}

		public virtual void UpdateAllQty(List<ProductQtyData> productDatas, Action<ItemProcessCallback<ProductQtyData>> callback)
		{
			var product = new ProductQtyList { Data = productDatas };
			UpdateAll<ProductQtyData, ProductQtyList>(product, new UrlSegments(), callback);
		}
		public virtual void UpdateAllRelations(List<RelatedProductsData> productDatas, Action<ItemProcessCallback<RelatedProductsData>> callback)
		{
			var product = new RelatedProductsList { Data = productDatas };
			UpdateAll<RelatedProductsData, RelatedProductsList>(product, new UrlSegments(), callback);
		}
		#endregion
	}
}
