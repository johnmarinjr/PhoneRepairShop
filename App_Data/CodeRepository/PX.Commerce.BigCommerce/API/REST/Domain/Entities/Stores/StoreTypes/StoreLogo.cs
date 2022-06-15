using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class StoreLogo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("mobile_url")]
        public bool MobileUrl { get; set; }
    }

}
