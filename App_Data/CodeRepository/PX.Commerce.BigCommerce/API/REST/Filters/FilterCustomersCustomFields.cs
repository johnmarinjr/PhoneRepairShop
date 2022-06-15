using System;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FilterCustomersCustomFields : Filter
    {
        [Description("customer_id")]
        public int? CustomerId { get; set; }
        
        [Description("address_id")]
        public int? AddressId { get; set; }

        [Description("field_name")]
        public string FieldName { get; set; }

        [Description("field_type")]
        public string FieldType { get; set; }
    }
}
