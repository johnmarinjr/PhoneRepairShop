using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class CustomerPriceClassRestDataProvider : RestDataProviderV2
	{
		protected override string GetListUrl  { get; } = "v2/customer_groups";

		protected override string GetSingleUrl { get; } = "v2/customer_groups/{id}";

		public CustomerPriceClassRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public virtual CustomerGroupData Create(CustomerGroupData group)
		{
			var newGroup = Create<CustomerGroupData>(group);
			return newGroup;
		}

		public virtual CustomerGroupData Update(CustomerGroupData group, string id)
		{
			var segments = MakeUrlSegments(id);
			return Update(group, segments);
		}

		public virtual IEnumerable<CustomerGroupData> GetAll(IFilter filter = null)
		{
			return GetAll<CustomerGroupData>(filter);
		}

		public virtual CustomerGroupData GetByID(string id)
		{
			var segments = MakeUrlSegments(id);
			return GetByID<CustomerGroupData>(segments);
		}
	}
}
