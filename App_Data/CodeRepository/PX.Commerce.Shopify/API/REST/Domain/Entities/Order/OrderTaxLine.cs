using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[JsonObject(Description = "Order Tax Line")]
	[Description(ShopifyCaptions.TaxLine)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrderTaxLine : BCAPIEntity
	{
		/// <summary>
		/// The name of the tax.
		/// </summary>
		[JsonProperty("title")]
		[CommerceDescription(ShopifyCaptions.TaxName, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String TaxName { get; set; }

		/// <summary>
		/// The amount added to the order for this tax in the shop currency.
		/// </summary>
		[JsonProperty("price")]
		[CommerceDescription(ShopifyCaptions.TaxLineAmount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? TaxAmount { get; set; }

		[CommerceDescription(ShopifyCaptions.TaxAmountPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? TaxAmountPresentment { get => TaxPriceSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The tax rate applied to the order to calculate the tax price.
		/// </summary>
		[JsonProperty("rate")]
		[CommerceDescription(ShopifyCaptions.TaxRate, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? TaxRate { get; set; }

		/// <summary>
		/// The amount added to the order for this tax in shop and presentment currencies.
		/// </summary>
		[JsonProperty("price_set")]
		public PriceSet TaxPriceSet { get; set; }
	}

}
