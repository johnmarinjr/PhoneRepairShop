using System;
using System.Collections.Generic;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core.Model;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class ProductVariantRestDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsVariantData>
    {
        protected override string GetListUrl { get; }   = "v3/catalog/products/{parent_id}/variants";
        protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/variants/{id}";
        
        public ProductVariantRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public virtual ProductsVariantData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<ProductsVariantData, ProductsVariant>(segments).Data;
        }

		public virtual IEnumerable<ProductsVariantData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<ProductsVariantData, ProductVariantList>(null, segments);
        }

		public virtual ProductsVariantData Create(ProductsVariantData productsVariantData, string parentId)
        {
            var productsVariant = new ProductsVariant { Data = productsVariantData };
            var segments = MakeParentUrlSegments(parentId);
            return Create<ProductsVariantData, ProductsVariant>(productsVariant, segments).Data;
        }

		public virtual ProductsVariantData Update(ProductsVariantData productsVariantData, string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            var productVariant = new ProductsVariant {Data = productsVariantData};
            return Update<ProductsVariantData, ProductsVariant>(productVariant, segments).Data;
        }

		public virtual bool Delete(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return base.Delete(segments);
        }
    }
	public class ProductVariantBatchRestDataProvider : ProductVariantRestDataProvider
	{
		protected override string GetListUrl { get; } = "v3/catalog/variants";
		public ProductVariantBatchRestDataProvider(IBigCommerceRestClient restClient) : base(restClient)
		{
		}

		public virtual void UpdateAll(List<ProductsVariantData> productDatas, Action<ItemProcessCallback<ProductsVariantData>> callback)
		{
			var product = new ProductVariantList { Data = productDatas };
			UpdateAll<ProductsVariantData, ProductVariantList>(product, new UrlSegments(), callback);
		}
	}
}
