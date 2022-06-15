using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderShipmentsRestDataProvider : RestDataProviderV2, IChildRestDataProvider<OrdersShipmentData>
    {
        protected override string GetListUrl { get; }   = "v2/orders/{parent_id}/shipments";
        protected override string GetSingleUrl { get; } = "v2/orders/{parent_id}/shipments/{id}";

        public OrderShipmentsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public virtual IEnumerable<OrdersShipmentData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersShipmentData>(null, segments);
        }

		public virtual OrdersShipmentData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<OrdersShipmentData>(segments);
        }

		public virtual OrdersShipmentData Create(OrdersShipmentData entity, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return base.Create(entity, segments);
        }

		public virtual OrdersShipmentData Update(OrdersShipmentData entity, string id, string parentId)
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
