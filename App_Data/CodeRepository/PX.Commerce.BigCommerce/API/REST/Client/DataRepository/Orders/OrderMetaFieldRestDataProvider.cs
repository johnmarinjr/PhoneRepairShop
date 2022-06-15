using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class OrderMetaFieldRestDataProvider : RestDataProviderV3
	{
		protected override string GetListUrl { get; } = "/v3/orders/{parent_id}/metafields";

		protected override string GetSingleUrl { get; } = "/v3/orders/{parent_id}/metafields/{id}";

		public OrderMetaFieldRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public OrdersMetaFieldData Create(OrdersMetaFieldData entity, string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return base.Create(entity, segments);
		}

		public IEnumerable<OrdersMetaFieldData> GetAll(IFilter filter, string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);

			return base.GetAll<OrdersMetaFieldData, OrdersMetaFieldList>(filter, segments);
		}

		public OrdersMetaFieldData Update(OrdersMetaFieldData entity, string id, string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return base.Update(entity, segments);
		}
	}
}
