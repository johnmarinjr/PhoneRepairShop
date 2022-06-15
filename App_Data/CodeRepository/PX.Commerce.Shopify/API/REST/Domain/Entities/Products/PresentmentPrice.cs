using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PresentmentPrice
	{
		/// <summary>
		/// The three-letter code (ISO 4217 format) for one of the shop's enabled presentment currencies.
		/// </summary>
		[JsonProperty("currency_code")]
		[CommerceDescription(ShopifyCaptions.CurrencyCode, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string CurrencyCode { get; set; }

		/// <summary>
		/// The variant's price or compare-at price in the presentment currency.
		/// </summary>
		[JsonProperty("amount")]
		[CommerceDescription(ShopifyCaptions.Amount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal Amount { get; set; }
	}
}
