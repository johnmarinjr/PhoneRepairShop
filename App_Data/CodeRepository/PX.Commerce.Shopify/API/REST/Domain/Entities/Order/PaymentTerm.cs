using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Shopify.API.REST;

namespace PX.Commerce.Shopify.API
{
	[JsonObject(Description = "payment_terms")]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

	public class PaymentTerm : BCAPIEntity
	{
		/// <summary>
		/// The amount that is owed according to the payment terms
		/// </summary>
		[JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Amount { get; set; }

		/// <summary>
		/// The presentment currency for the payment
		/// </summary>
		[JsonProperty("currency", NullValueHandling = NullValueHandling.Ignore)]
		public string Currency { get; set; }

		/// <summary>
		/// The number of days between the invoice date and due date that is defined in the selected payment terms template.
		/// </summary>
		[JsonProperty("due_in_days", NullValueHandling = NullValueHandling.Ignore)]
		public int? DueInDays { get; set; }

		/// <summary>
		/// The name of the selected payment terms template for the order.
		/// </summary>
		[JsonProperty("payment_terms_name", NullValueHandling = NullValueHandling.Ignore)]
		public string PaymentTermsName { get; set; }

		/// <summary>
		/// The type of selected payment terms template for the order.
		/// </summary>
		[JsonProperty("payment_terms_type", NullValueHandling = NullValueHandling.Ignore)]
		public string PaymentTermsType { get; set; }

		/// <summary>
		/// An array of schedules associated to the payment terms.
		/// </summary>
		[JsonProperty("payment_schedules", NullValueHandling = NullValueHandling.Ignore)]
		public List<PaymentSchedule> PaymentSchedules { get; set; }
	}
}
