using System.Collections.Generic;
using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
    [JsonObject(Description = "Product Tax")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsTax : IEntityResponse<ProductsTaxData>
    {
        [JsonProperty("data")]
        public ProductsTaxData Data { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
}
