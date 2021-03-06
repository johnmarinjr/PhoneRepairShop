using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class StoreCredit
    {
        [JsonProperty("remaining_balance")]
		[CommerceDescription(BigCommerceCaptions.RemainingBalance, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string RemainingBalance { get; set; }
    }
}
