using PX.Commerce.Core.Model;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class CustomerFormFieldRestDataProvider : RestDataProviderV3
    {
        private const string id_string = "id";

        protected override string GetListUrl { get; } = "v3/customers/form-field-values";

        protected override string GetSingleUrl { get; } = "v3/customers/form-field-values";


        public CustomerFormFieldRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

		public virtual CustomerFormFieldData Create(CustomerFormFieldData customersCustomFieldData)
        {
            var newData = Update<CustomerFormFieldData>(customersCustomFieldData, new UrlSegments());
            return newData;
        }

		public virtual CustomerFormFieldData Update(CustomerFormFieldData customersCustomFieldData)
        {
            var updateData = Update<CustomerFormFieldData>(customersCustomFieldData, new UrlSegments());
            return updateData;
        }

		public virtual List<CustomerFormFieldData> UpdateAll(List<CustomerFormFieldData> customersCustomFieldDataList)
		{
			CustomerFormFieldList response = Update<CustomerFormFieldData, CustomerFormFieldList>(customersCustomFieldDataList, new UrlSegments());
			return response?.Data;
		}

		public virtual IEnumerable<CustomerFormFieldData> GetAll()
        {
			return GetAll<CustomerFormFieldData, CustomerFormFieldList>();
        }
    }
}
