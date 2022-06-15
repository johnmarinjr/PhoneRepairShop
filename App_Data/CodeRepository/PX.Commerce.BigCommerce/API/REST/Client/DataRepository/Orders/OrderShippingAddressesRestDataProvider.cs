using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderShippingAddressesRestDataProvider : RestDataProviderV2, IChildRestDataProvider<OrdersShippingAddressData>
    {
        protected override string GetListUrl { get; }   = "v2/orders/{parent_id}/shipping_addresses";
        protected override string GetSingleUrl { get; } = "v2/orders/{parent_id}/shipping_addresses/{id}";

        public OrderShippingAddressesRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public virtual IEnumerable<OrdersShippingAddressData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersShippingAddressData>(null, segments);
        }

		public virtual OrdersShippingAddressData GetByID(string parentId, string id)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<OrdersShippingAddressData>(segments);
        }

		public virtual OrdersShippingAddressData Create(OrdersShippingAddressData entity, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return Create(entity, segments);
        }

		public virtual OrdersShippingAddressData Update(OrdersShippingAddressData entity, string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return Update(entity, segments);
        }

		public virtual bool Delete(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return base.Delete(segments);
        }
    }
}
