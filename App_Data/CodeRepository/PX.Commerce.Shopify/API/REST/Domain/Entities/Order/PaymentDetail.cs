using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	/// <summary>
	/// Information about the credit card used for the transaction.
	/// </summary>
	[JsonObject(Description = "Payment Detail")]
	[CommerceDescription(ShopifyCaptions.PaymentDetail, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PaymentDetail : BCAPIEntity
	{
		/// <summary>
		/// avs_result_code: The response code from the address verification system. The code is a single letter; see this chart for the codes and their definitions.
		/// </summary>
		[JsonProperty("avs_result_code", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.AvsResultCode, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string AvsResultCode { get; set; }

		/// <summary>
		/// credit_card_bin: The issuer identification number (IIN), formerly known as bank identification number (BIN) of the customer's credit card. 
		/// This is made up of the first few digits of the credit card number.
		/// </summary>
		[JsonProperty("credit_card_bin", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CreditCardBin, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String CreditCardBin { get; set; }

		/// <summary>
		/// credit_card_company: The name of the company that issued the customer's credit card.
		/// </summary>
		[JsonProperty("credit_card_company")]
		[CommerceDescription(ShopifyCaptions.CreditCardCompany, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string CreditCardCompany { get; set; }

		/// <summary>
		/// credit_card_number: The customer's credit card number, with most of the leading digits redacted.
		/// </summary>
		[JsonProperty("credit_card_number", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CreditCardNumber, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string CreditCardNumber { get; set; }

		/// <summary>
		///  cvv_result_code: The response code from the credit card company indicating whether the customer entered the card security code, or card verification value, correctly. The code is a single letter or empty string; 
		/// </summary>
		[JsonProperty("cvv_result_code", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.CvvResultCode, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string CvvResultCode { get; set; }
	}

}
