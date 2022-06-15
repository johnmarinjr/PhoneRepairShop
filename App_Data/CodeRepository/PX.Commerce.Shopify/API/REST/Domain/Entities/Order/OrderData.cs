﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrderResponse : IEntityResponse<OrderData>
	{
		[JsonProperty("order")]
		public OrderData Data { get; set; }
	}

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrdersResponse : IEntitiesResponse<OrderData>
	{
		[JsonProperty("orders")]
		public IEnumerable<OrderData> Data { get; set; }
	}

	[JsonObject(Description = "Order")]
	[CommerceDescription(ShopifyCaptions.OrderData, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrderData : BCAPIEntity
	{
		public OrderData()
		{
			LineItems = new List<OrderLineItem>();
		}
		/// <summary>
		/// The ID of the app that created the order.
		/// </summary>
		[JsonProperty("app_id", NullValueHandling = NullValueHandling.Ignore)]
		public long? AppId { get; set; }

		/// <summary>
		/// The mailing address associated with the payment method. This address is an optional field that won't be available on orders that do not require a payment method. 
		/// </summary>
		[JsonProperty("billing_address", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.BillingAddress, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public OrderAddressData BillingAddress { get; set; }

		/// <summary>
		/// [READ-ONLY] The IP address of the browser used by the customer when they placed the order.
		/// </summary>
		[JsonProperty("browser_ip")]
		[ShouldNotSerialize]
		public string BrowserIP { get; set; }

		/// <summary>
		/// Whether the customer consented to receive email updates from the shop.
		/// </summary>
		[JsonProperty("buyer_accepts_marketing", NullValueHandling = NullValueHandling.Ignore)]
		public bool? BuyerAcceptsMarketing { get; set; }

		/// <summary>
		/// The reason why the order was canceled. Valid values:
		/// customer: The customer canceled the order.
		/// fraud: The order was fraudulent.
		/// inventory: Items in the order were not in inventory.
		/// declined: The payment was declined.
		/// other: A reason not in this list.
		/// </summary>
		[JsonProperty("cancel_reason", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CancelReason, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public OrderCancelReason? CancelReason { get; set; }

		/// <summary>
		/// [READ-ONLY] The date and time ( ISO 8601 format) when the order was canceled.
		/// </summary>
		[JsonProperty("cancelled_at")]
		[ShouldNotSerialize]
		[CommerceDescription(ShopifyCaptions.DateCanceled, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public DateTime? CancelledAt { get; set; }

		/// <summary>
		/// [READ-ONLY] The ID of the cart that's associated with the order.
		/// </summary>
		[JsonProperty("cart_token")]
		[ShouldNotSerialize]
		public string CartToken { get; set; }

		/// <summary>
		/// [READ-ONLY] Information about the browser that the customer used when they placed their order.
		/// </summary>
		[JsonProperty("client_details")]
		[ShouldNotSerialize]
		public OrderClientDetails ClientDetails { get; set; }

		/// <summary>
		/// [READ-ONLY] The date and time (ISO 8601 format) when the order was closed.
		/// </summary>
		[JsonProperty("closed_at")]
		[ShouldNotSerialize]
		public DateTime? ClosedAt { get; set; }

		/// <summary>
		/// [READ-ONLY] The autogenerated date and time (ISO 8601 format) when the order was created in Shopify. The value for this property cannot be changed.
		/// </summary>
		[JsonProperty("created_at")]
		[CommerceDescription(ShopifyCaptions.DateCreated, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public DateTime? DateCreatedAt { get; set; }

		/// <summary>
		/// The three-letter code (ISO 4217 format) for the shop currency.
		/// </summary>
		[JsonProperty("currency", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Currency, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public string Currency { get; set; }

		/// <summary>
		/// The current total discounts on the order in the shop currency. The value of this field reflects order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_discounts", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CurrentTotalDiscounts, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalDiscounts { get; set; }

		[CommerceDescription(ShopifyCaptions.CurrentTotalDiscountsPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalDiscountsPresentment { get => CurrentTotalDiscountsSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The current total discounts on the order in shop and presentment currencies. The amount values associated with this field reflect order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_discounts_set", NullValueHandling = NullValueHandling.Ignore)]
		[ShouldNotSerialize]
		public PriceSet CurrentTotalDiscountsSet { get; set; }

		[CommerceDescription(ShopifyCaptions.CurrentTotalDutiesPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalDutiesPresentment { get => CurrentTotalDutiesSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The current total duties charged on the order in shop and presentment currencies. The amount values associated with this field reflect order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_duties_set", NullValueHandling = NullValueHandling.Ignore)]
		[ShouldNotSerialize]
		public PriceSet CurrentTotalDutiesSet { get; set; }

		/// <summary>
		/// The current total price of the order in the shop currency. The value of this field reflects order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CurrentTotalPrice, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalPrice { get; set; }

		[CommerceDescription(ShopifyCaptions.CurrentTotalPricePresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalPricePresentment { get => CurrentTotalPriceSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The current total price of the order in shop and presentment currencies. The amount values associated with this field reflect order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_price_set", NullValueHandling = NullValueHandling.Ignore)]
		[ShouldNotSerialize]
		public PriceSet CurrentTotalPriceSet { get; set; }

		/// <summary>
		/// The current subtotal price of the order in the shop currency. The value of this field reflects order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_subtotal_price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CurrentSubTotalPrice, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentSubTotalPrice { get; set; }

		[CommerceDescription(ShopifyCaptions.CurrentSubTotalPricePresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentSubTotalPricePresentment { get => CurrentSubTotalPriceSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The current subtotal price of the order in shop and presentment currencies. The amount values associated with this field reflect order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_subtotal_price_set", NullValueHandling = NullValueHandling.Ignore)]
		[ShouldNotSerialize]
		public PriceSet CurrentSubTotalPriceSet { get; set; }

		/// <summary>
		/// The current total taxes charged on the order in the shop currency. The value of this field reflects order edits, returns, or refunds.
		/// </summary>
		[JsonProperty("current_total_tax", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CurrentTotalTax, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalTax { get; set; }

		[CommerceDescription(ShopifyCaptions.CurrentTotalTaxPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? CurrentTotalTaxPresentment { get => CurrentTotalTaxSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The current total taxes charged on the order in shop and presentment currencies. The amount values associated with this field reflect order edits, returns, and refunds.
		/// </summary>
		[JsonProperty("current_total_tax_set", NullValueHandling = NullValueHandling.Ignore)]
		[ShouldNotSerialize]
		public PriceSet CurrentTotalTaxSet { get; set; }

		/// <summary>
		/// Information about the customer. 
		/// The order might not have a customer and apps should not depend on the existence of a customer object. 
		/// This value might be null if the order was created through Shopify POS.
		/// </summary>
		[JsonProperty("customer", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Customer, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public CustomerData Customer { get; set; }

		/// <summary>
		/// [READ-ONLY] The two or three-letter language code, optionally followed by a region modifier.
		/// </summary>
		[JsonProperty("customer_locale")]
		[ShouldNotSerialize]
		public string CustomerLocale { get; set; }

		/// <summary>
		/// [READ-ONLY] An ordered list of stacked discount applications.
		/// The discount_applications property includes 3 types: discount_code, manual, and script. All 3 types share a common structure and have some type specific attributes.
		/// </summary>
		[JsonProperty("discount_applications")]
		[ShouldNotSerialize]
		public List<OrderDiscountApplications> DiscountApplications { get; set; }

		/// <summary>
		/// A list of discounts applied to the order.
		/// </summary>
		[JsonProperty("discount_codes", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Discount, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public List<OrderDiscountCodes> DiscountCodes { get; set; }

		/// <summary>
		/// The customer's email address.
		/// </summary>
		[PIIData]
		[JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Email, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Email { get; set; }

		/// <summary>
		/// Whether taxes on the order are estimated.
		/// This property returns false when taxes on the order are finalized and aren't subject to any changes.
		/// </summary>
		[JsonProperty("estimated_taxes", NullValueHandling = NullValueHandling.Ignore)]
		public bool? EstimatedTaxes { get; set; }

		/// <summary>
		/// The status of payments associated with the order. Can only be set when the order is created.
		/// </summary>
		[JsonProperty("financial_status", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.FinancialStatus, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public OrderFinancialStatus? FinancialStatus { get; set; }

		/// <summary>
		/// A list of fulfillments associated with the order. 
		/// </summary>
		[JsonProperty("fulfillments", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Fulfillment, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public List<FulfillmentData> Fulfillments { get; set; }

		/// <summary>
		/// The order's status in terms of fulfilled line items.
		/// </summary>
		[JsonProperty("fulfillment_status", NullValueHandling = NullValueHandling.Include)]
		[CommerceDescription(ShopifyCaptions.FulfillmentStatus, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public OrderFulfillmentStatus? FulfillmentStatus { get; set; }

		/// <summary>
		/// The ID of the order, used for API purposes. This is different from the order_number property, which is the ID used by the shop owner and customer.
		/// </summary>
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.OrderId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public long? Id { get; set; }

		/// <summary>
		/// The behaviour to use when updating inventory. (default: bypass)
		/// </summary>
		[JsonProperty("inventory_behaviour", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.InventoryBehaviour)]
		public OrderInventoryBehaviour? InventoryBehaviour { get; set; } = OrderInventoryBehaviour.Bypass;

		/// <summary>
		/// [READ-ONLY] The URL for the page where the buyer landed when they entered the shop.
		/// </summary>
		[JsonProperty("landing_site")]
		[CommerceDescription(ShopifyCaptions.LandingSite, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public String LandingSite { get; set; }

		/// <summary>
		/// A list of line item objects, each containing information about an item in the order.
		/// </summary>
		[JsonProperty("line_items", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.LineItem, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public List<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

		/// <summary>
		/// The ID of the physical location where the order was processed. 
		/// </summary>
		[JsonProperty("location_id", NullValueHandling = NullValueHandling.Ignore)]
		public long? LocationId { get; set; }

		/// <summary>
		/// The order name, generated by combining the order_number property with the order prefix and suffix that are set in the merchant's general settings. 
		/// This is different from the id property, which is the ID of the order used by the API. 
		/// This field can also be set by the API to be any string value.
		/// </summary>
		[PIIData]
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Name, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Name { get; set; }

		/// <summary>
		/// An optional note that a shop owner can attach to the order.
		/// </summary>
		[JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Note, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String Note { get; set; }

		/// <summary>
		/// Extra information that is added to the order. Appears in the Additional details section of an order details page. 
		/// Each array entry must contain a hash with name and value keys.
		/// </summary>
		[JsonProperty("note_attributes", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.NoteAttribute, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public List<NameValuePair> NoteAttributes { get; set; }

		/// <summary>
		/// [READ-ONLY] The order's position in the shop's count of orders. Numbers are sequential and start at 1.
		/// </summary>
		[JsonProperty("number")]
		[ShouldNotSerialize]
		public int? Number { get; set; }

		/// <summary>
		/// [READ-ONLY] The order 's position in the shop's count of orders starting at 1001. Order numbers are sequential and start at 1001.
		/// </summary>
		[JsonProperty("order_number")]
		[CommerceDescription(ShopifyCaptions.OrderNumber, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public string OrderNumber { get; set; }

		[CommerceDescription(ShopifyCaptions.OriginalTotalDutiesPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public decimal? OriginalTotalDutiesPresentment { get => OriginalTotalDutiesSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The original total duties charged on the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("original_total_duties_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet OriginalTotalDutiesSet { get; set; }

		/// <summary>
		/// The customer's phone number for receiving SMS notifications.
		/// </summary>
		[PIIData]
		[JsonProperty("phone", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Phone, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String Phone { get; set; }

		/// <summary>
		/// The list of payment gateways used for the order.
		/// </summary>
		[JsonProperty("payment_gateway_names", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Gateway, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped )]
		[ShouldNotSerialize]
		public List<string> PaymentGatewayNames { get; set; }

		/// <summary>
		/// The terms and conditions under which a payment should be processed.
		/// </summary>
		[JsonProperty("payment_terms", NullValueHandling = NullValueHandling.Ignore)]
		public PaymentTerm PaymentTerms { get; set; }

		/// <summary>
		/// The presentment currency that was used to display prices to the customer.
		/// </summary>
		[JsonProperty("presentment_currency", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.PresentmentCurrency, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String CurrencyPresentment { get; set; }

		/// <summary>
		/// The date and time (ISO 8601 format) when an order was processed. 
		/// This value is the date that appears on your orders and that's used in the analytic reports. 
		/// By default, it matches the created_at value. 
		/// If you're importing orders from an app or another platform, then you can set processed_at to a date and time in the past to match when the original order was created.
		/// </summary>
		[JsonProperty("processed_at", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ProcessedAt, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public DateTime? ProcessedAt { get; set; }

		/// <summary>
		/// How the payment was processed.
		/// </summary>
		[JsonProperty("processing_method")]
		[CommerceDescription(ShopifyCaptions.ProcessingMethod, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public string ProcessingMethod { get; set; }

		/// <summary>
		/// The website where the customer clicked a link to the shop.
		/// </summary>
		[JsonProperty("referring_site", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ReferringSite, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String ReferringSite { get; set; }

		/// <summary>
		/// A list of refunds applied to the order.
		/// </summary>
		[JsonProperty("refunds")]
		[CommerceDescription(ShopifyCaptions.Refund, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public List<OrderRefund> Refunds { get; set; }

		/// <summary>
		/// Whether to send an order confirmation to the customer.
		/// When send_receipt is set to false, then you need to disable the Storefront API from the private app's page in the Shopify admin.
		/// </summary>
		[JsonProperty("send_receipt", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.SendReceipt, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public bool? SendReceipt { get; set; }

		/// <summary>
		/// Whether to send a shipping confirmation to the customer.
		/// </summary>
		[JsonProperty("send_fulfillment_receipt", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.SendFulfillmentReceipt, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public bool? SendFulfillmentReceipt { get; set; }

		/// <summary>
		/// The mailing address to where the order will be shipped. This address is optional and will not be available on orders that do not require shipping. 
		/// </summary>
		[JsonProperty("shipping_address", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ShippingAddress, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public OrderAddressData ShippingAddress { get; set; }

		/// <summary>
		/// An array of objects, each of which details a shipping method used.
		/// </summary>
		[JsonProperty("shipping_lines", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ShippingLine, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public List<OrderShippingLine> ShippingLines { get; set; }

		/// <summary>
		/// Where the order originated. Can be set only during order creation, and is not writeable afterwards. 
		/// Values for Shopify channels are protected and cannot be assigned by other API clients: web, pos, shopify_draft_order, iphone, and android. 
		/// Orders created via the API can be assigned any other string of your choice. 
		/// If unspecified, then new orders are assigned the value of your app's ID.
		/// </summary>
		[JsonProperty("source_name", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.SourceName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String SourceName { get; set; }

		/// <summary>
		/// The price of the order in the shop currency after discounts but before shipping, taxes, and tips.
		/// </summary>
		[JsonProperty("subtotal_price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Subtotal, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? SubTotal { get; set; }

		[CommerceDescription(ShopifyCaptions.SubtotalPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[ShouldNotSerialize]
		public decimal? SubTotalPresentment { get => SubTotalSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The subtotal of the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("subtotal_price_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet SubTotalSet { get; set; }

		/// <summary>
		/// Tags attached to the order, formatted as a string of comma-separated values. 
		/// Tags are additional short descriptors, commonly used for filtering and searching. Each individual tag is limited to 40 characters in length.
		/// </summary>
		[JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Tags, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public String Tags { get; set; }

		/// <summary>
		/// An array of tax line objects, each of which details a tax applicable to the order.
		/// </summary>
		[JsonProperty("tax_lines", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TaxLine, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public List<OrderTaxLine> TaxLines { get; set; }

		/// <summary>
		/// Whether taxes are included in the order subtotal.
		/// </summary>
		[JsonProperty("taxes_included", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TaxesIncluded, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public bool? TaxesIncluded { get; set; }

		/// <summary>
		/// Whether this is a test order.
		/// </summary>
		[JsonProperty("test", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TestCase, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public bool? IsTestOrder { get; set; }

		/// <summary>
		/// A unique token for the order.
		/// </summary>
		[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Token, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public String Token { get; set; }

		/// <summary>
		/// The total discounts applied to the price of the order in the shop currency.
		/// </summary>
		[JsonProperty("total_discounts", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalDiscount, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? TotalDiscount { get; set; }

		[CommerceDescription(ShopifyCaptions.TotalDiscountPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[ShouldNotSerialize]
		public decimal? TotalDiscountPresentment { get => TotalDiscountSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The total discounts applied to the price of the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("total_discounts_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet TotalDiscountSet { get; set; }

		/// <summary>
		/// The sum of all line item prices in the shop currency.
		/// </summary>
		[JsonProperty("total_line_items_price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ItemsTotal, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? ItemsTotal { get; set; }

		[CommerceDescription(ShopifyCaptions.ItemsTotalPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[ShouldNotSerialize]
		public decimal? ItemsTotalPresentment { get => ItemsTotalSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The total discounts applied to the price of the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("total_line_items_price_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet ItemsTotalSet { get; set; }

		/// <summary>
		/// The sum of all line item prices, discounts, shipping, taxes, and tips in the shop currency. Must be positive.
		/// </summary>
		[JsonProperty("total_price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.OrderTotal, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? OrderTotal { get; set; }

		[CommerceDescription(ShopifyCaptions.OrderTotalPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[ShouldNotSerialize]
		public decimal? OrderTotalPresentment { get => OrderTotalSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The total price of the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("total_price_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet OrderTotalSet { get; set; }

		/// <summary>
		/// The sum of all the taxes applied to the order in th shop currency. Must be positive.
		/// </summary>
		[JsonProperty("total_tax", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalTax, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? TotalTax { get; set; }

		[CommerceDescription(ShopifyCaptions.TotalTaxPresentment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[ShouldNotSerialize]
		public decimal? TotalTaxPresentment { get => TotalTaxSet?.PresentmentMoney?.Amount; }

		/// <summary>
		/// The total tax applied to the order in shop and presentment currencies.
		/// </summary>
		[JsonProperty("total_tax_set", NullValueHandling = NullValueHandling.Ignore)]
		public PriceSet TotalTaxSet { get; set; }

		/// <summary>
		/// The sum of all the tips in the order in the shop currency.
		/// </summary>
		[JsonProperty("total_tip_received", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalTips, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? TotalTips { get; set; }

		/// <summary>
		/// The sum of all line item weights in grams.
		/// </summary>
		[JsonProperty("total_weight", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalWeight, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public decimal? TotalWeightInGrams { get; set; }

        /// <summary>
        /// The transactions of the order
        /// </summary>
        [JsonProperty("transactions", NullValueHandling = NullValueHandling.Ignore)]
        [CommerceDescription(ShopifyCaptions.OrdersTransaction, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public List<OrderTransaction> Transactions { get; set; }

		/// <summary>
		/// The date and time (ISO 8601 format) when the order was last modified.
		/// Filtering orders by updated_at is not an effective method for fetching orders because its value can change when no visible fields of an order have been updated. 
		/// Use the Webhook and Event APIs to subscribe to order events instead.
		/// </summary>
		[JsonProperty("updated_at")]
		[ShouldNotSerialize]
		[CommerceDescription(ShopifyCaptions.DateModified, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public DateTime? DateModifiedAt { get; set; }

		/// <summary>
		/// The ID of the user logged into Shopify POS who processed the order, if applicable.
		/// </summary>
		[JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.UserId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public long? UserId { get; set; }

		/// <summary>
		/// The URL pointing to the order status web page, if applicable.
		/// </summary>
		[JsonProperty("order_status_url", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.OrderStatusURL, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		[ShouldNotSerialize]
		public String OrderStatusURL { get; set; }

		/// <summary>
		/// Attaches additional metadata to a shop's resources:
		///key(required) : An identifier for the metafield(maximum of 30 characters).
		///namespace(required): A container for a set of metadata(maximum of 20 characters). Namespaces help distinguish between metadata that you created and metadata created by another individual with a similar namespace.
		///value (required): Information to be stored as metadata.
		///value_type(required): The value type.Valid values: string and integer.
		///description(optional): Additional information about the metafield.
		/// </summary>
		[JsonProperty("metafields", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Metafields, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
        [BCExternCustomField(BCConstants.MetaFields)]
		public List<MetafieldData> Metafields { get; set; }

		/// <summary>
		/// The risks of the order
		/// </summary>
		[ShouldNotSerialize]
		[CommerceDescription(ShopifyCaptions.OrderRisk, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public List<OrderRisk> OrderRisks { get; set; }

		public override string CalculateHash()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(this.Id ?? 0);
			sb.Append(this.Email ?? string.Empty);
			sb.Append(this.Phone ?? string.Empty);
			sb.Append(this.OrderTotal ?? 0.00m);
			sb.Append(this.TotalTax ?? 0.00m);
			foreach (var item in this.LineItems ?? new List<OrderLineItem>())
			{
				sb.Append(item.Id ?? 0);
				sb.Append(item.Quantity ?? 0);
			}
			sb.Append(this.ShippingAddress?.Name ?? string.Empty);
			sb.Append(this.ShippingAddress?.Address1 ?? string.Empty);
			sb.Append(this.ShippingAddress?.PostalCode ?? string.Empty);

			if (sb.Length <= 0) return null;
			byte[] hash = PX.Data.Update.PXCriptoHelper.CalculateSHA(sb.ToString());
			String hashcode = String.Concat(hash.Select(b => b.ToString("X2")));
			return hashcode;
		}
	}

	public class NameValuePair
	{
		[JsonProperty("name")]
		[CommerceDescription(ShopifyCaptions.Name, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string Name { get; set; }

		[JsonProperty("value")]
		[CommerceDescription(ShopifyCaptions.Value, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string Value { get; set; }
	}
}
