using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderRestDataProvider : RestDataProviderV2
    {
        protected override string GetListUrl { get; } = "v2/orders";
        protected override string GetSingleUrl { get; } = "v2/orders/{id}";

		public OrderRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;

		}

		public OrderData Create(OrderData order)
		{
			var newOrder = Create<OrderData>(order);
			return newOrder;
		}

		public virtual OrderData Update(OrderData order, int id)
		{
			var segments = MakeUrlSegments(id.ToString());
			var updated = Update(order, segments);
			return updated;
		}

		public virtual OrderStatus Update(OrderStatus order, string id)
		{
			var segments = MakeUrlSegments(id);
			var updated = Update(order, segments);
			return updated;
		}

		public virtual bool Delete(OrderData order, int id)
        {
            return Delete(id);
        }

        public bool Delete(int id)
        {
            var segments = MakeUrlSegments(id.ToString());
            return base.Delete(segments);
        }

		public virtual List<OrderData> Get(IFilter filter = null)
		{
			return base.Get<OrderData>(filter);
        }

		public virtual IEnumerable<OrderData> GetAll(IFilter filter = null)
		{
			return base.GetAll<OrderData>(filter);
        }

		public virtual OrderData GetByID(string id)
		{
			var segments = MakeUrlSegments(id);
            var orderData = GetByID<OrderData>(segments);

			return orderData;
        }
    }
}
