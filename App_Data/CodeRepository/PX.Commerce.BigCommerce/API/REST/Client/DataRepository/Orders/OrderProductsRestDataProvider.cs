using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderProductsRestDataProvider : RestDataProviderV2, IChildRestDataProvider<OrdersProductData>
    {
        protected override string GetListUrl { get; }   = "v2/orders/{parent_id}/products";
        protected override string GetSingleUrl { get; } = "v2/orders/{parent_id}/products/{id}";

        public OrderProductsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

		public virtual IEnumerable<OrdersProductData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersProductData>(null, segments);
        }

		public virtual OrdersProductData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<OrdersProductData>(segments);
        }

		public virtual OrdersProductData Create(OrdersProductData entity, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return Create(entity, segments);
        }

		public virtual OrdersProductData Update(OrdersProductData entity, string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return Update(entity, segments);
        }

		public virtual bool Delete(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return Delete(segments);
        }
    }
}
