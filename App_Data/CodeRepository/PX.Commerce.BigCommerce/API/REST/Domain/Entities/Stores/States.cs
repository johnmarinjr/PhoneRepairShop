using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class States
	{
		[JsonProperty("state_abbreviation")]
		public string StateID { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("country_id")]
		public int CountryID { get; set; }
	}
}
