using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
    [JsonObject(Description = "Product -> ProductsCustomField")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsCustomFieldData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("id")]
        public int? Id { get; set; }
    }
}
