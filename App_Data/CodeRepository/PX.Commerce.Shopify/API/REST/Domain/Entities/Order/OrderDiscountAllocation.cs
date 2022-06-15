using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[JsonObject(Description = "Order Discount Allocation")]
	[CommerceDescription(ShopifyCaptions.DiscountAllocation, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrderDiscountAllocation : BCAPIEntity
	{
		/// <summary>
		/// The discount amount allocated to the line in the shop currency.
		/// </summary>
		[JsonProperty("amount")]
		[CommerceDescription(ShopifyCaptions.DiscountAmount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? DiscountAmount { get; set; }

		[CommerceDescription(ShopifyCaptions.DiscountAmountPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? DiscountAmountPresentment { get => DiscountPriceSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The index of the associated discount application in the order's discount_applications list.
		/// </summary>
		[JsonProperty("discount_application_index")]
		public int? DiscountApplicationIndex { get; set; }

		/// <summary>
		/// The discount amount allocated to the line item in shop and presentment currencies.
		/// </summary>
		[JsonProperty("amount_set")]
		public PriceSet DiscountPriceSet { get; set; }
	}

}
