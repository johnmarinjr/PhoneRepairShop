using Newtonsoft.Json;
using PX.Commerce.BigCommerce.API.WebDAV;
using PX.Commerce.Core;

namespace PX.Commerce.BigCommerce.API.REST
{
	[JsonObject(Description = "Product Tax Class")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ProductsTaxData : BCAPIEntity
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}
