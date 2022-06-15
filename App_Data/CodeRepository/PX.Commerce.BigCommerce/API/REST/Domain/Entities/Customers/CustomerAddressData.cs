using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	[JsonObject(Description = "Customer address list (BigCommerce API v3 response)")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class CustomerAddressList : IEntitiesResponse<CustomerAddressData>
	{
		public CustomerAddressList()
		{
			Data = new List<CustomerAddressData>();
		}

		[JsonProperty("data")]
		public List<CustomerAddressData> Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }

	}

	[JsonObject(Description = "Customer -> Customer Address")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[CommerceDescription(BigCommerceCaptions.CustomerAddressData, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
	public class CustomerAddressData : BCAPIEntity
	{
        [JsonProperty("id")]
		[CommerceDescription(BigCommerceCaptions.ID, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual int? Id { get; set; }

        [JsonProperty("customer_id")]
		[CommerceDescription(BigCommerceCaptions.CustomerId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual int? CustomerId { get; set; }

		[PIIData]
		[JsonProperty("first_name")]
		[CommerceDescription(BigCommerceCaptions.FirstName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired()]
		public virtual string FirstName { get; set; }

		[PIIData]
		[JsonProperty("last_name")]
		[CommerceDescription(BigCommerceCaptions.LastName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public virtual string LastName { get; set; }

		[PIIData]
		[JsonProperty("company")]
		[CommerceDescription(BigCommerceCaptions.CompanyName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public virtual string Company { get; set; }

		[PIIData]
		[JsonProperty("address1")]
		[CommerceDescription(BigCommerceCaptions.AddressLine1, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
		public string Address1 { get; set; }

		[PIIData]
		[JsonProperty("address2")]
		[CommerceDescription(BigCommerceCaptions.AddressLine2, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Address2 { get; set; }

		[PIIData]
		[JsonProperty("city")]
		[CommerceDescription(BigCommerceCaptions.City, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
		public virtual string City { get; set; }

        [JsonProperty("state_or_province")]
		[CommerceDescription(BigCommerceCaptions.State, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
		public virtual string State { get; set; }

		[PIIData]
		[JsonProperty("postal_code")]
		[CommerceDescription(BigCommerceCaptions.PostalCode, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
		public virtual string PostalCode { get; set; }

		[JsonProperty("country")]
		[CommerceDescription(BigCommerceCaptions.Country, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired()]
		public virtual string Country { get; set; }

        [JsonProperty("country_code")]
		[CommerceDescription(BigCommerceCaptions.CountryIso2, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public virtual string CountryCode { get; set; }


		[JsonProperty("address_type")]
		[CommerceDescription(BigCommerceCaptions.AddressType, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public virtual string AddressType { get; set; }

		[PIIData]
		[JsonProperty("phone")]
		[CommerceDescription(BigCommerceCaptions.PhoneNumber, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
		public virtual string Phone { get; set; }

        [JsonProperty("form_fields")]
        [CommerceDescription(BigCommerceCaptions.FormFields)]
		[BCExternCustomField(BCConstants.FormFields)]
		public IList<CustomerFormFieldData> FormFields { get; set; }
    }
}
