using System.Collections.Generic;
using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsOptionValue : IEntityResponse<ProductOptionValueData>
    {
        [JsonProperty("data")]
        public ProductOptionValueData Data { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsOptionValueList :IEntitiesResponse<ProductOptionValueData>
    {
        private List<ProductOptionValueData> _data;

        [JsonProperty("data")]
        public List<ProductOptionValueData> Data
        {
            get => _data ?? (_data = new List<ProductOptionValueData>());
            set => _data = value;
        }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
}
