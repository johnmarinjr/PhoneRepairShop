using PX.Commerce.Core;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class OrderRefundsRestDataProvider : RestDataProviderV3, IChildReadOnlyRestDataProvider<OrderRefund>
	{
		private const string id_string = "id";
		private const string parent_id_string = "parent_id";

		protected override string GetListUrl { get; } = "v3/orders/{parent_id}/payment_actions/refunds";
		protected override string GetSingleUrl => string.Empty; //Not implemented on Big Commerce

		public OrderRefundsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}


		public virtual OrderRefund GetByID(string id, string parentId)
		{
			foreach (OrderRefund refund in GetAll(parentId))
			{
				if (refund.Id == id.ToInt()) return refund;
			}
			return null;
		}

		public virtual IEnumerable<OrderRefund> GetAll(string externID)
		{
			var segments = MakeParentUrlSegments(externID);
			return base.GetAll<OrderRefund, OrderRefundsList>(null, segments);
		}
	}
}
