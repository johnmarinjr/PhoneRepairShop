using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class ProductCategoryRestDataProvider : RestDataProviderV3, IParentRestDataProvider<ProductCategoryData>
    {
        private const string id_string = "id";

        protected override string GetListUrl { get; }   = "v3/catalog/categories";
        protected override string GetSingleUrl { get; } = "v3/catalog/categories/{id}";

        public ProductCategoryRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        #region  IParentRestDataProvider  
        public virtual IEnumerable<ProductCategoryData> GetAll(IFilter filter = null)
        {
            return GetAll<ProductCategoryData, ProductCategoryList>(filter);
        }

		public virtual ProductCategoryData GetByID(string id)
        {
            var segments = MakeUrlSegments(id);
            return GetByID<ProductCategoryData, ProductCategory>(segments).Data;
        }

		public virtual bool Delete(ProductCategoryData productCategoryData, int id)
        {
            return Delete(id);
        }

        public virtual bool Delete(int id)
        {
            var segments = MakeUrlSegments(id.ToString());
            return base.Delete(segments);
        }

		public virtual ProductCategoryData Create(ProductCategoryData category)
        {
            var productCategory  = new ProductCategory{Data = category};
            return Create<ProductCategoryData, ProductCategory>(productCategory).Data;
        }

		public virtual ProductCategoryData Update(ProductCategoryData category, int id)
        {
            var segments = MakeUrlSegments(id.ToString());
            return Update<ProductCategoryData, ProductCategory>(category, segments).Data;
        }
        #endregion
    }
}
