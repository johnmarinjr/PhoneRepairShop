using PX.Commerce.Core.Model;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class ProductsOptionRestDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsOptionData>
    {
        protected override string GetListUrl { get; } = "v3/catalog/products/{parent_id}/options";
        protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/options/{id}";

        public ProductsOptionRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

		#region IChildRestDataProvider
		public virtual ProductsOptionData Create(ProductsOptionData productsOptionData, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            var productsOption = new ProductsOption { Data = productsOptionData };
            return Create<ProductsOptionData, ProductsOption>(productsOption, segments).Data;
        }

		public virtual ProductsOptionData Update(ProductsOptionData productsOptionData, string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            var productsOption = new ProductsOption { Data = productsOptionData };

            return Update<ProductsOptionData, ProductsOption>(productsOption, segments).Data;
        }

		public virtual bool Delete(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return base.Delete(segments);
        }

        public virtual ProductsOptionData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<ProductsOptionData, ProductsOption>(segments).Data;
        }

		public virtual IEnumerable<ProductsOptionData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<ProductsOptionData, ProductsOptionList>(null, segments);
        }
        #endregion
    }
}
