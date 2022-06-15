using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderTaxesRestDataProvider : RestDataProviderV2, IChildRestDataProvider<OrdersTaxData>
    {
        protected override string GetListUrl { get; } = "v2/orders/{parent_id}/taxes?details=true";
        protected override string GetSingleUrl { get; } = "v2/orders/{parent_id}/taxes/{id}?details=true";

        public OrderTaxesRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public virtual IEnumerable<OrdersTaxData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersTaxData>(null, segments);
        }

		public virtual OrdersTaxData GetByID(string id, string parentId)
        {
            var segments = MakeUrlSegments(id, parentId);
            return GetByID<OrdersTaxData>(segments);
        }

		public virtual OrdersTaxData Create(OrdersTaxData entity, string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return Create(entity, segments);
        }

		public virtual OrdersTaxData Update(OrdersTaxData entity, string id, string parentId)
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
