using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    [CommerceDescription(BigCommerceCaptions.OrderProducts)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrdersProductData : BCAPIEntity
	{
		/// <summary>
		/// The ID of the product.
		/// </summary>
		[JsonProperty("id")]
        public virtual int? Id { get; set; }

        /// <summary>
        /// The SKU of the product.
        /// 
        /// [string(250)]
        /// </summary>
        [JsonProperty("sku")]
		[CommerceDescription(BigCommerceCaptions.SKU,mappingStatus:FieldMappingStatus.Import)]
		public virtual string Sku { get; set; }

        /// <summary>
        /// The name of the product.
        /// 
        /// [string(250)]
        /// </summary>
        [JsonProperty("name")]
		[CommerceDescription(BigCommerceCaptions.ProductName, mappingStatus: FieldMappingStatus.Import)]
		public virtual string ProductName { get; set; }

        /// <summary>
        /// The type of product that is one of the following values:
        ///
        /// physical
        /// digital
        /// giftcertificate
        /// </summary>
        [JsonProperty("type")]
        public virtual OrdersProductsType ProductType { get; set; }

        /// <summary>
        /// The base price of the product as entered by the store owner. 
        /// May be inc or ex tax depending on the "Prices Entered With Tax?" tax setting.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("base_price")]
		[CommerceDescription(BigCommerceCaptions.BasePrice, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal BasePrice { get; set; }

        /// <summary>
        /// The price of the product excluding tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("price_ex_tax", DefaultValueHandling = DefaultValueHandling.Include)]
		[CommerceDescription(BigCommerceCaptions.PriceExcludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal PriceExcludingTax { get; set; }

        /// <summary>
        /// The price of the product include tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("price_inc_tax", DefaultValueHandling = DefaultValueHandling.Include)]
		[CommerceDescription(BigCommerceCaptions.PriceIncludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal PriceIncludingTax { get; set; }

        /// <summary>
        /// The amount of tax on the product price.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("price_tax")]
		[CommerceDescription(BigCommerceCaptions.PriceTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal PriceTax { get; set; }

        /// <summary>
        /// The total base price (base_price * quantity).
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("base_total")]
		[CommerceDescription(BigCommerceCaptions.BaseTotal, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal BaseTotal { get; set; }

        /// <summary>
        /// The total price (price * quantity) excluding tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("total_ex_tax")]
		[CommerceDescription(BigCommerceCaptions.TotalExcludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal TotalExcludingTax { get; set; }

        /// <summary>
        /// The total price (price * quantity) including tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>

        [JsonProperty("total_inc_tax")]
		[CommerceDescription(BigCommerceCaptions.TotalIncludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal TotalIncludingTax { get; set; }

        /// <summary>
        /// The tax on the total price.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("total_tax")]
		[CommerceDescription(BigCommerceCaptions.TotalTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal TotalTax { get; set; }

        /// <summary>
        /// The weight of the product.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("weight")]
		[CommerceDescription(BigCommerceCaptions.Weight, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal Weight { get; set; }

        /// <summary>
        /// The quantity of the product that was purchased.
        /// </summary>
        [JsonProperty("quantity", DefaultValueHandling = DefaultValueHandling.Include)]
		[CommerceDescription(BigCommerceCaptions.Quantity, mappingStatus: FieldMappingStatus.Import)]
		public virtual int Quantity { get; set; }

        /// <summary>
        /// The ID of the order that this product is associated with.
        /// </summary>
        [JsonProperty("order_id")]
        public virtual int OrderId { get; set; }

        /// <summary>
        /// The ID of the actual product this order product refers to.
        /// </summary>
        [JsonProperty("product_id")]
        public virtual int ProductId { get; set; }

		/// <summary>
		/// The ID of the actual product variant this order product refers to.
		/// </summary>
		[JsonProperty("variant_id")]
		public virtual int VariandId { get; set; }

        /// <summary>
        /// The base cost price of the product as entered by the store owner. 
        /// May be inc or ex tax depending on the "Prices Entered With Tax?" tax setting.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("base_cost_price")]
		[CommerceDescription(BigCommerceCaptions.BaseCostPrice, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal BaseCostPrice { get; set; }

        /// <summary>
        /// The cost price of the product excluding tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("cost_price_inc_tax")]
		[CommerceDescription(BigCommerceCaptions.CostPriceIncludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal CostPriceIncludingTax { get; set; }

        /// <summary>
        /// The cost price of the product including tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("cost_price_ex_tax")]
		[CommerceDescription(BigCommerceCaptions.CostPriceExcludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal CostPriceExcludingTax { get; set; }

        /// <summary>
        /// The amount of tax on the cost price.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("cost_price_tax")]
		[CommerceDescription(BigCommerceCaptions.CostPriceTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal CostPriceTax { get; set; }

        /// <summary>
        /// Indicates if this product has been refunded.
        /// </summary>
        [JsonProperty("is_refunded")]
		[CommerceDescription(BigCommerceCaptions.IsRefunded, mappingStatus: FieldMappingStatus.Import)]
		public virtual bool IsRefunded { get; set; }

        /// <summary>
        /// The Qty that was refunded by the customer.
        /// </summary>
        [JsonProperty("quantity_refunded")]
		[CommerceDescription(BigCommerceCaptions.QuantityRefund, mappingStatus: FieldMappingStatus.Import)]
		public virtual int QuantityRefund { get; set; }

        /// <summary>
        /// The amount that was refunded to the customer.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("refund_amount")]
		[CommerceDescription(BigCommerceCaptions.RefundAmount, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal RefundAmount { get; set; }

        /// <summary>
        /// The ID of the return request submitted for this product.
        /// </summary>
        [JsonProperty("return_id")]
        public virtual int ReturnId { get; set; }

        /// <summary>
        /// The name of the wrapping applied to the product.
        /// 
        /// [string(100)]
        /// </summary>
        [JsonProperty("wrapping_name")]
		[CommerceDescription(BigCommerceCaptions.WrappingName, mappingStatus: FieldMappingStatus.Import)]
		public virtual string WrappingName { get; set; }

        /// <summary>
        /// The base wrapping cost as entered by the store owner. 
        /// May be inc or ex tax depending on the "Prices Entered With Tax?" tax setting.
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("base_wrapping_cost")]
		[CommerceDescription(BigCommerceCaptions.BaseWrappingCost, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal BaseWrappingCost { get; set; }

        /// <summary>
        /// The wrapping cost excluding tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("wrapping_cost_ex_tax")]
		[CommerceDescription(BigCommerceCaptions.WrappingCostExcludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal WrappingCostExcludingTax { get; set; }

        /// <summary>
        /// The wrapping cost including tax.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("wrapping_cost_inc_tax")]
		[CommerceDescription(BigCommerceCaptions.WrappingCostIncludingTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal WrappingCostIncludingTax { get; set; }

        /// <summary>
        /// The amount of tax on the wrapping cost.
        /// 
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("wrapping_cost_tax")]
		[CommerceDescription(BigCommerceCaptions.WrappingCostTax, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal WrappingCostTax { get; set; }

        /// <summary>
        /// The message left by the customer for the wrapping.
        /// 
        /// [text]
        /// </summary>
        [JsonProperty("wrapping_message")]
		[CommerceDescription(BigCommerceCaptions.WrappingMessage, mappingStatus: FieldMappingStatus.Import)]
		public virtual string WrappingMessage { get; set; }

        /// <summary>
        /// The quantity of the product that has been shipped at this time.
        /// </summary>
        [JsonProperty("quantity_shipped")]
		[CommerceDescription(BigCommerceCaptions.QuantityShipped, mappingStatus: FieldMappingStatus.Import)]
		public virtual int QuantityShipped { get; set; }

        /// <summary>
        /// The name of the event date defined for the product.
        /// 
        /// [string(255)]
        /// </summary>
        [JsonProperty("event_name")]
		[CommerceDescription(BigCommerceCaptions.EventName, mappingStatus: FieldMappingStatus.Import)]
		public virtual string EventName { get; set; }

        [JsonProperty("event_date")]
        public virtual string EventDateUT { get; set; }

        /// <summary>
        /// The fixed shipping cost for the product as defined by the store owner. 
        /// May be inc or ex tax depending on the "Prices Entered With Tax?" tax setting.
        /// [decimal(20,4)]
        /// </summary>
        [JsonProperty("fixed_shipping_cost")]
		[CommerceDescription(BigCommerceCaptions.FixedShippingCost, mappingStatus: FieldMappingStatus.Import)]
		public virtual decimal FixedShippingCost { get; set; }

        /// <summary>
        /// The ID of the shipping address that this product is associated with.
        /// </summary>
        [JsonProperty("order_address_id")]
        public virtual int OrderAddressId { get; set; }

        /// <summary>
        /// eBay's item ID for this product when it was listed on eBay.
        /// 
        /// [string(19)]
        /// </summary>
        [JsonProperty("ebay_item_id")]
        public virtual string EbayItemId { get; set; }

        /// <summary>
        /// A list of discounts (such as from Coupons or Discount Rules) applied to this product. 
        /// See the Product Discounts section below for a definition of this object.
        /// </summary>
        [JsonProperty("applied_discounts")]
        public virtual IList<OrdersProductsDiscount> AppliedDiscounts { get; set; }

        /// <summary>
        /// eBay's transaction ID for this product when purchased on eBay.
        /// [string(19)]
        /// </summary>
        [JsonProperty("ebay_transaction_id")]
        public virtual string EbayTransactionId { get; set; }

        /// <summary>
        /// The ID of the option set applied to this product.
        /// </summary>
        [JsonIgnore]
        public virtual int OptionSetId { get; set; }

		[JsonProperty("option_set_id")]
		public virtual string OptionSetId_InString
        {
            get
            {
                if (OptionSetId == -1)
                    return "NULL";
                else
                    return OptionSetId.ToString();
            }
            set
            {
                int tmpInt;
                bool wasParsed = int.TryParse(value, out tmpInt);
                if (wasParsed)
                    OptionSetId = tmpInt;
                else
                    OptionSetId = -1;
            }
        }

		public bool ShouldSerializeOptionSetId_InString()
		{
			return false;
		}

		/// <summary>
		/// The ID of order product if this product was bundled with another product.
		/// </summary>
		[JsonIgnore]
        public virtual int ParentOrderProductId { get; set; }

		[JsonProperty("parent_order_product_id")]
		public virtual string ParentOrderProductId_InString
        {
            get
            {
                if (ParentOrderProductId == -1)
                    return "NULL";
                else
                    return ParentOrderProductId.ToString();
            }
            set
            {
                int tmpInt;
                bool wasParsed = int.TryParse(value, out tmpInt);
                if (wasParsed)
                    ParentOrderProductId = tmpInt;
                else
                    ParentOrderProductId = -1;
            }
        }

		public bool ShouldSerializeParentOrderProductId_InString()
		{
			return false;
		}

		/// <summary>
		/// Indicates if this product was bundled with another product.
		/// </summary>
		[JsonProperty("is_bundled_product")]
		[CommerceDescription(BigCommerceCaptions.IsBundledProduct, mappingStatus: FieldMappingStatus.Import)]
		public virtual bool IsBundledProduct { get; set; }

        /// <summary>
        /// The bin picking number for the product.
        /// 
        /// [string(255)]
        /// </summary>
        [JsonProperty("bin_picking_number")]
		[CommerceDescription(BigCommerceCaptions.BinPickingNumber)]
		public virtual string BinPickingNumber { get; set; }

        /// <summary>
        /// The Product Options chosen/configured by the customer when adding the product to their cart. 
        /// See the Product Options section below for a definition of this object.
        /// See https://developer.bigcommerce.com/display/API/Order+Products
        /// </summary>
        [JsonProperty("product_options")]
		[CommerceDescription(BigCommerceCaptions.OrdersProductsOption, mappingStatus: FieldMappingStatus.Import)]
        [BCExternCustomField(BCConstants.BCProductOptions)]
        public virtual IList<OrdersProductsOption> ProductOptions { get; set; }

        /// <summary>
        /// The Configurable Fields chosen/configured by the customer when adding the product to their cart. 
        /// See the Configurable Fields section below for a definition of this object.
        /// See https://developer.bigcommerce.com/display/API/Order+Products
        /// </summary>
        [JsonProperty("configurable_fields")]
		[Description(BigCommerceCaptions.OrdersProductsConfigurableField)]
		public virtual IList<OrdersProductsConfigurableField> ConfigurableFields { get; set; }
    }
}
