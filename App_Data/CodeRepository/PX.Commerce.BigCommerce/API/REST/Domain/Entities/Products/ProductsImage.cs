using System.Collections.Generic;
using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
    [JsonObject(Description = "Product Image")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	class ProductsImage : IEntityResponse<ProductsImageData>
    {
        [JsonProperty("data")]
        public ProductsImageData Data { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsImageList : IEntitiesResponse<ProductsImageData>
    {
        private List<ProductsImageData> _data;

        [JsonProperty("data")]
        public List<ProductsImageData> Data
        {
            get => _data ?? (_data = new List<ProductsImageData>());
            set => _data = value;
        }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
}
