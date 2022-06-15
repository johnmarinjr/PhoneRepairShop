using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class Resource
    {
        [JsonProperty("url")]
        public virtual string Url { get; set; }

        [JsonProperty("resource")]
        public virtual string ResourceEndPoint { get; set; }        
    }
}
