using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderCouponsRestDataProvider : RestDataProviderV2, IChildRestDataProvider<OrdersCouponData>
    {
        protected override string GetListUrl { get; } = "v2/orders/{parent_id}/coupons";
        protected override string GetSingleUrl { get; } = "v2/orders/{parent_id}/coupons/{id}";

        public OrderCouponsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public virtual IEnumerable<OrdersCouponData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersCouponData>(null, segments);
        }

		public virtual OrdersCouponData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<OrdersCouponData>(segments);
        }

		public virtual OrdersCouponData Create(OrdersCouponData entity, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return Create(entity, segments);
        }

		public virtual OrdersCouponData Update(OrdersCouponData entity, string id, string parentId)
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
