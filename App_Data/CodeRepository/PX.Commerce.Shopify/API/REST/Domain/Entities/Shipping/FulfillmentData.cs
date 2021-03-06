using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[CommerceDescription(ShopifyCaptions.ShipmentData, FieldFilterStatus.Filterable, FieldMappingStatus.Skipped)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ShipmentData : BCAPIEntity
	{
		public ShipmentData()
		{
			FulfillmentDataList = new List<FulfillmentData>();
		}

		[CommerceDescription(ShopifyCaptions.Fulfillment, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public List<FulfillmentData> FulfillmentDataList { get; set; }

		/// <summary>
		/// Existing extern shipments need to remove before creating new shipment.
		/// </summary>
		public Dictionary<string, string> ExternShipmentsToRemove { get; set; } = new Dictionary<string, string>();
	}
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FulfillmentResponse : IEntityResponse<FulfillmentData>
	{
		[JsonProperty("fulfillment")]
		public FulfillmentData Data { get; set; }
	}

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FulfillmentsResponse : IEntitiesResponse<FulfillmentData>
	{
		[JsonProperty("fulfillments")]
		public IEnumerable<FulfillmentData> Data { get; set; }
	}

	[JsonObject(Description = "Fulfillment")]
	[CommerceDescription(ShopifyCaptions.Fulfillment, FieldFilterStatus.Filterable, FieldMappingStatus.Export)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FulfillmentData : BCAPIEntity
	{
		public FulfillmentData()
		{

		}
		public FulfillmentData Clone(bool? DeepClone = false)
		{
			FulfillmentData copyObj = (FulfillmentData)this.MemberwiseClone();
			List<OrderLineItem> newItems = new List<OrderLineItem>();
			if (DeepClone == true)
			{
				foreach (OrderLineItem item in this.LineItems)
				{
					newItems.Add(item.Clone());
				}
				copyObj.LineItems = newItems;
			}
			return copyObj;
		}

		/// <summary>
		/// The date and time when the fulfillment was created. The API returns this value in ISO 8601 format.
		/// </summary>
		[JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.DateCreated)]
		[ShouldNotSerialize]
		public DateTime? DateCreatedAt { get; set; }

		/// <summary>
		/// The ID for the fulfillment.
		/// </summary>
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Id)]
		public long? Id { get; set; }

		/// <summary>
		/// A historical record of each item in the fulfillment
		/// </summary>
		[JsonProperty("line_items", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.LineItem, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public List<OrderLineItem> LineItems { get; set; }

		/// <summary>
		/// The unique identifier of the location that the fulfillment should be processed for. 
		/// </summary>
		[JsonProperty("location_id", NullValueHandling = NullValueHandling.Ignore)]
		public long? LocationId { get; set; }

		/// <summary>
		/// The uniquely identifying fulfillment name, consisting of two parts separated by a .. 
		/// The first part represents the order name and the second part represents the fulfillment number. 
		/// The fulfillment number automatically increments depending on how many fulfillments are in an order (e.g. #1001.1, #1001.2).
		/// </summary>
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Name, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string Name { get; set; }

		/// <summary>
		/// Whether the customer should be notified. If set to true, then an email will be sent when the fulfillment is created or updated. 
		/// For orders that were initially created using the API, the default value is false. For all other orders, the default value is true.
		/// </summary>
		[JsonProperty("notify_customer", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.NotifyCustomer, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public bool? NotifyCustomer { get; set; }

		/// <summary>
		/// The unique numeric identifier for the order.
		/// </summary>
		[JsonProperty("order_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.OrderId, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public long? OrderId { get; set; }


		/// <summary>
		/// A text field that provides information about the receipt
		/// </summary>
		[JsonProperty("receipt", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Receipt, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public FulfillmentReceipt Receipt { get; set; }

		/// <summary>
		/// The type of service used.
		/// </summary>
		[JsonProperty("service", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Service, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string Service { get; set; }

		/// <summary>
		/// The current shipment status of the fulfillment.
		/// </summary>
		[JsonProperty("shipment_status", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ShipmentStatus, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public ShipmentStatus? ShipmentStatus { get; set; }

		/// <summary>
		/// The status of the fulfillment.
		/// </summary>
		[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Status, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public FulfillmentStatus? Status { get; set; }

		/// <summary>
		/// The name of the tracking company.
		/// </summary>
		[JsonProperty("tracking_company", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingCompany, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string TrackingCompany { get; set; }

		/// <summary>
		/// The tracking info.
		/// </summary>
		[JsonProperty("tracking_info", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingInfo, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public TrackingInfo TrackingInfo{ get; set; }

		/// <summary>
		/// A list of tracking numbers, provided by the shipping company.
		/// </summary>
		[JsonProperty("tracking_numbers", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingNumbers, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public List<string> TrackingNumbers { get; set; }

		/// <summary>
		/// The URLs of tracking pages for the fulfillment.
		/// </summary>
		[JsonProperty("tracking_urls", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingUrls, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		public List<string> TrackingUrls { get; set; }

		/// <summary>
		/// The date and time (ISO 8601 format) when the fulfillment was last modified..
		/// </summary>
		[JsonProperty("updated_at")]
		[ShouldNotSerialize]
		public DateTime? DateModifiedAt { get; set; }

		/// <summary>
		/// The name of the inventory management service.
		/// </summary>
		[JsonProperty("variant_inventory_management", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.InventoryManagement, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
		[ShouldNotSerialize]
		public string InventoryManagement { get; set; }

		[JsonIgnore]
		public virtual Guid? OrderLocalID { get; set; }

		[JsonIgnore]
		public virtual String ShipmentType { get; set; }
	}

	public class FulfillmentReceipt
	{
		/// <summary>
		/// Whether the fulfillment was a testcase.
		/// </summary>
		[JsonProperty("testcase")]
		[CommerceDescription(ShopifyCaptions.TestCase, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public bool? TestCase { get; set; }

		/// <summary>
		/// authorization: The authorization code.
		/// </summary>
		[JsonProperty("authorization")]
		[CommerceDescription(ShopifyCaptions.Authorization, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Authorization { get; set; }
	}

	public class TrackingInfo
	{
		/// <summary>
		/// Tracking Number
		/// </summary>
		[JsonProperty("number", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingNumber, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string Number { get; set; }

		/// <summary>
		/// Tracking Url
		/// </summary>
		[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingUrl, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string URL { get; set; }

		/// <summary>
		/// Tracking Company
		/// </summary>
		[JsonProperty("company", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TrackingCompany, FieldFilterStatus.Skipped, FieldMappingStatus.Export)]
		public string Company { get; set; }
	}
}
