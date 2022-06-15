using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Objects;

namespace PX.Commerce.Shopify.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PaymentMethod : IPaymentMethod
	{

		[JsonProperty("name")]
		public string Name { get; set; }

		public string Currency { get; set; }

		public bool CreatePaymentfromOrder { get; set; }
	}

	public class PaymentsRefundAttribute : BCAPIEntity
	{
		/// <summary>
		/// The current status of the refund. Valid values: pending, failure, success, and error.
		/// </summary>
		[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
		public string Status { get; set; }

		/// <summary>
		/// A unique number associated with the transaction that can be used to track the refund. 
		/// </summary>
		[JsonProperty("acquirer_reference_number", NullValueHandling = NullValueHandling.Ignore)]
		public string AcquirerReferenceNumber { get; set; }
	}
}
