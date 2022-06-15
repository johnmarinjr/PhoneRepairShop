﻿using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{ 
	[JsonObject(Description = "Product Video")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	class ProductVideoData : IEntityResponse<ProductsVideo>
	{
		[JsonProperty("data")]
		public ProductsVideo Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }		
	}
}
