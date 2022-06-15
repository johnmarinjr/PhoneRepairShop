using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class CustomerAddressRestDataProviderV3 : RestDataProviderV3
	{
		protected override string GetListUrl { get; } = "v3/customers/addresses";

		protected override string GetSingleUrl { get; } = "v3/customers/addresses";


		public CustomerAddressRestDataProviderV3(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public virtual IEnumerable<CustomerAddressData> GetAll(IFilter filter = null)
		{
			return GetAll<CustomerAddressData, CustomerAddressList>(filter);
		}

		public virtual CustomerAddressData Create(CustomerAddressData address)
		{
			CustomerAddressList response = Create<CustomerAddressData, CustomerAddressList>(new CustomerAddressData[] { address }.ToList());
			return response?.Data?.FirstOrDefault();
		}

		public virtual CustomerAddressData Update(CustomerAddressData address)
		{
			CustomerAddressList response = Update<CustomerAddressData, CustomerAddressList>(new CustomerAddressData[] { address }.ToList());
			return response?.Data?.FirstOrDefault();
		}
	}
}
