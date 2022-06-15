using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	/// <summary>
	/// Order adjustments are generated to account for refunded shipping costs and differences between calculated and actual refund amounts. 
	/// </summary>
	[JsonObject(Description = "Order Adjustment")]
	[Description(ShopifyCaptions.OrderAdjustment)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrderAdjustment : BCAPIEntity
	{
		/// <summary>
		/// [READ-ONLY] The unique identifier for the order adjustment.
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Id, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? Id { get; set; }

		/// <summary>
		/// [READ-ONLY] The unique identifier for the order that the order adjustment is associated with.
		[JsonProperty("order_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.OrderId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? OrderId { get; set; }

		/// <summary>
		/// [READ-ONLY] The unique identifier for the refund that the order adjustment is associated with.
		[JsonProperty("refund_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.RefundId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? RefundId { get; set; }

		/// <summary>
		/// The value of the discrepancy between the calculated refund and the actual refund. If the kind property's value is shipping_refund, then amount returns the value of shipping charges refunded to the customer.
		/// </summary>
		[JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Amount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? Amount { get; set; }

		[CommerceDescription(ShopifyCaptions.AmountPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? AmountPresentment { get => AmountSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The taxes that are added to amount, such as applicable shipping taxes added to a shipping refund.
		/// </summary>
		[JsonProperty("tax_amount", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TaxAmount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? TaxAmount { get; set; }

		[CommerceDescription(ShopifyCaptions.TaxAmountPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? TaxAmountPresentment { get => TaxAmountSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The order adjustment type. Valid values: shipping_refund and refund_discrepancy.
		/// </summary>
		[JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Kind, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public OrderAdjustmentType Kind { get; set; }

		/// <summary>
		/// The reason for the order adjustment. To set this value, include discrepancy_reason when you create a refund.
		/// </summary>
		[JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Reason, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String Reason { get; set; }

		/// <summary>
		/// The amount of the order adjustment in shop and presentment currencies.
		/// </summary>
		[JsonProperty("amount_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet AmountSet { get; set; }

		/// <summary>
		/// The amount of the order adjustment in shop and presentment currencies.
		/// </summary>
		[JsonProperty("tax_amount_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet TaxAmountSet { get; set; }
	}
}
