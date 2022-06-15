using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class Meta
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }
    }
}
