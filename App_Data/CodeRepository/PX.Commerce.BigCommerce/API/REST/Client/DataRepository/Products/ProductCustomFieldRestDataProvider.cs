using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class ProductCustomFieldRestDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsCustomFieldData>
    {
        private const string id_string = "id";

        protected override string GetListUrl { get; } = "v3/catalog/products/{parent_id}/custom-fields";
        protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/custom-fields/{id}";

        public ProductCustomFieldRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

		#region IChildRestDataProvider
		public virtual ProductsCustomFieldData Create(ProductsCustomFieldData productsCustomFieldData, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return Create<ProductsCustomFieldData, ProductsCustomField>(productsCustomFieldData, segments).Data;
        }

		public virtual ProductsCustomFieldData Update(ProductsCustomFieldData productsCustomFieldData, string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);

            return Update<ProductsCustomFieldData, ProductsCustomField>(productsCustomFieldData, segments).Data;
        }

		public virtual bool Delete(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return base.Delete(segments);
        }

        public virtual ProductsCustomFieldData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<ProductsCustomFieldData, ProductsCustomField>(segments).Data;
        }

		public virtual IEnumerable<ProductsCustomFieldData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<ProductsCustomFieldData, ProductsCustomFieldList>(null, segments);
        }
        #endregion
    }
}
