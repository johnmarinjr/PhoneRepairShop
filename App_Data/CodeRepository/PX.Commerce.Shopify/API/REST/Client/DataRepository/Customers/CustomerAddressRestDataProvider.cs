using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
	public class CustomerAddressRestDataProvider : RestDataProviderBase, IChildRestDataProvider<CustomerAddressData>
	{
		protected override string GetListUrl { get; } = "customers/{parent_id}/addresses.json";
		protected override string GetSingleUrl { get; } = "customers/{parent_id}/addresses/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();

		public CustomerAddressRestDataProvider(IShopifyRestClient restClient) : base()
		{
			ShopifyRestClient = restClient;
		}

		public virtual CustomerAddressData Create(CustomerAddressData entity, string customerId)
		{
			var segments = MakeParentUrlSegments(customerId);
			return Create<CustomerAddressData, CustomerAddressResponse>(entity, segments);
		}

		public virtual CustomerAddressData Update(CustomerAddressData entity, string customerId, string addressId)
		{
			var segments = MakeUrlSegments(addressId, customerId);
			return Update<CustomerAddressData, CustomerAddressResponse>(entity, segments);
		}

		public virtual bool Delete(string customerId, string addressId)
		{
			var segments = MakeUrlSegments(addressId, customerId);
			return Delete(segments);
		}

		public virtual IEnumerable<CustomerAddressData> GetAll(string customerId, IFilter filter = null)
		{
			var segments = MakeParentUrlSegments(customerId);
			return GetAll<CustomerAddressData, CustomerAddressesResponse>(filter, segments);
		}

		public virtual CustomerAddressData GetByID(string customerId, string addressId)
		{
			var segments = MakeUrlSegments(addressId, customerId);
			return GetByID<CustomerAddressData, CustomerAddressResponse>(segments);
		}

		public virtual IEnumerable<CustomerAddressData> GetAllWithoutParent(IFilter filter = null)
		{
			throw new NotImplementedException();
		}
	}
}
