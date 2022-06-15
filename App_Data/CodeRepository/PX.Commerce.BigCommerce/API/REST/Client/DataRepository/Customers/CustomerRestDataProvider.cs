using PX.Commerce.Core.Model;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class CustomerRestDataProviderV3 : RestDataProviderV3
	{
		protected override string GetListUrl { get; } = "v3/customers";

		protected override string GetSingleUrl { get; } = "v3/customers";

		private CustomerAddressRestDataProviderV3 customerAddressDataProviderV3;

		public CustomerRestDataProviderV3(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
			customerAddressDataProviderV3 = new CustomerAddressRestDataProviderV3(restClient);
		}

		public virtual IEnumerable<CustomerData> GetAll(IFilter filter = null)
		{
			return GetAll<CustomerData, CustomerList>(filter);
		}

		public CustomerData GetById(string id, IFilter filter = null)
		{
			var customerFilter = (FilterCustomers)filter;
			if (customerFilter == null) customerFilter = new FilterCustomers { Include = "addresses,formfields" };

			customerFilter.Id = id;

			var result = base.GetAll<CustomerData, CustomerList>(customerFilter).FirstOrDefault();

			// if there are exactly 10 addresses returned included in the customer's data, send a separate request to get all addresses
			// of customer to make sure all addresses are available for further process as there's a limit of max 10 addresses returned
			// with customer's data
			if (result != null && customerFilter.Include.Contains("addresses") && result.AddressCount > 10)
			{
				FilterAddresses addressFilter = new FilterAddresses { Include = "formfields", CustomerId = result.Id.ToString() };
				result.Addresses = customerAddressDataProviderV3.GetAll(addressFilter).ToList();
			}

			return result;
		}

		public virtual CustomerData Create(CustomerData customer)
		{
			CustomerList resonse = Create<CustomerData, CustomerList>(new CustomerData[] { customer }.ToList());
			return resonse?.Data?.FirstOrDefault();
		}

		public virtual CustomerData Update(CustomerData customer)
		{
			CustomerList resonse = Update<CustomerData, CustomerList>(new CustomerData[] { customer }.ToList());
			return resonse?.Data?.FirstOrDefault();
		}
	}
}
