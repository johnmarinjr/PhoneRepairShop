using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Common;
using PX.Data.BQL;
using PX.Objects.SO;
using PX.Objects.AR;
using PX.Objects.GL;
using PX.Objects.Common;
using PX.Objects.PO;
using PX.Data.BQL.Fluent;
using PX.Api.ContractBased.Models;
using System.Reflection;
using PX.Objects.IN;

namespace PX.Commerce.Shopify
{
	public class SPShipmentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Shipment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary }.Concat(Orders).ToArray();

		public MappedShipment Shipment;
		public List<MappedOrder> Orders = new List<MappedOrder>();
	}

	public class SPShipmentsRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region Shipments
			return base.Restrict<MappedShipment>(mapped, delegate (MappedShipment obj)
			{
				if (obj.Local != null)
				{
					if (obj.Local.Confirmed?.Value == false)
					{
						return new FilterResult(FilterStatus.Invalid,
								PXMessages.Localize(BCMessages.LogShipmentSkippedNotConfirmed));
					}

					if (obj.Local.OrderNoteIds != null)
					{
						BCBindingExt binding = processor.GetBindingExt<BCBindingExt>();

						Boolean anyFound = false;
						foreach (var orderNoeId in obj.Local?.OrderNoteIds)
						{
							if (processor.SelectStatus(BCEntitiesAttribute.Order, orderNoeId) == null) continue;

							anyFound = true;
						}
						if (!anyFound)
						{
							return new FilterResult(FilterStatus.Ignore,
								PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogShipmentSkippedNoOrder, obj.Local.ShipmentNumber?.Value ?? obj.Local.SyncID.ToString()));
						}
					}
				}

				return null;
			});
			#endregion
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}
	}

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.Shipment, BCCaptions.Shipment,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		ExternTypes = new Type[] { typeof(ShipmentData) },
		LocalTypes = new Type[] { typeof(BCShipments) },
		GIScreenID = BCConstants.GenericInquiryShipmentDetails,
		GIResult = typeof(BCShipmentsResult),
		AcumaticaPrimaryType = typeof(PX.Objects.SO.SOShipment),
		AcumaticaPrimarySelect = typeof(PX.Objects.SO.SOShipment.shipmentNbr),
		URL = "orders/{0}",
		Requires = new string[] { BCEntitiesAttribute.Order }
	)]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.ShipmentLine, EntityName = BCCaptions.ShipmentLine, AcumaticaType = typeof(PX.Objects.SO.SOShipLine))]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.ShipmentBoxLine, EntityName = BCCaptions.ShipmentLineBox, AcumaticaType = typeof(PX.Objects.SO.SOPackageDetailEx))]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Shipments" }, PushDestination = BCConstants.PushNotificationDestination)]
	public class SPShipmentProcessor : BCProcessorSingleBase<SPShipmentProcessor, SPShipmentEntityBucket, MappedShipment>, IProcessor
	{
		public SPHelper helper = PXGraph.CreateInstance<SPHelper>();

		protected OrderRestDataProvider orderDataProvider;
		protected FulfillmentRestDataProvider fulfillmentDataProvider;
		protected IEnumerable<InventoryLocationData> inventoryLocations;
		protected List<BCShippingMappings> shippingMappings;
		protected BCBinding currentBinding;
		protected BCBindingExt currentBindingExt;
		protected BCBindingShopify currentShopifySettings;
		private long? defaultLocationId;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			currentBinding = GetBinding();
			currentBindingExt = GetBindingExt<BCBindingExt>();
			currentShopifySettings = GetBindingExt<BCBindingShopify>();

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			orderDataProvider = new OrderRestDataProvider(client);
			fulfillmentDataProvider = new FulfillmentRestDataProvider(client);

			shippingMappings = PXSelectReadonly<BCShippingMappings,
				Where<BCShippingMappings.bindingID, Equal<Required<BCShippingMappings.bindingID>>>>
				.Select(this, Operation.Binding).Select(x => x.GetItem<BCShippingMappings>()).ToList();
			inventoryLocations = ConnectorHelper.GetConnector(currentBinding.ConnectorType)?.GetExternalInfo<InventoryLocationData>(BCObjectsConstants.BCInventoryLocation, currentBinding.BindingID)?.Where(x => x.Active == true);
			if (inventoryLocations == null || inventoryLocations.Count() == 0)
			{
				throw new PXException(ShopifyMessages.InventoryLocationNotFound);
			}
			else
				defaultLocationId = inventoryLocations.First().Id;
		}
		#endregion

		public override void NavigateLocal(IConnector connector, ISyncStatus status)
		{
			SOOrderShipment orderShipment = PXSelect<SOOrderShipment, Where<SOOrderShipment.shippingRefNoteID, Equal<Required<SOOrderShipment.shippingRefNoteID>>>>.Select(this, status?.LocalID);
			if (orderShipment.ShipmentType == SOShipmentType.DropShip)//dropshipment
			{
				POReceiptEntry extGraph = PXGraph.CreateInstance<POReceiptEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}
			if (orderShipment.ShipmentType == SOShipmentType.Issue && orderShipment.ShipmentNoteID == null) //Invoice
			{
				ARInvoiceEntry extGraph = PXGraph.CreateInstance<ARInvoiceEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}
			else//shipment
			{
				SOShipmentEntry extGraph = PXGraph.CreateInstance<SOShipmentEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}

		}

		public override bool ControlModification(IMappedEntity mapped, BCSyncStatus status, string operation)
		{
			if (mapped is MappedShipment)
			{
				MappedShipment obj = mapped as MappedShipment;
				if (!obj.IsNew && obj.Local != null && obj.Local?.ExternalShipmentUpdated?.Value == true) // Update status only if order is unmodified
				{
					return false;
				}
			}
			return base.ControlModification(mapped, status, operation);
		}

		#region Pull
		public override MappedShipment PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			BCShipments giResult = (new BCShipments()
			{
				ShippingNoteID = localID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null) return null;
			MapFilterFields(giResult?.Results, giResult);
			GetOrderShipment(giResult);
			if (giResult.Shipment == null && giResult.POReceipt == null) return null;
			MappedShipment obj = new MappedShipment(giResult, giResult.ShippingNoteID.Value, giResult.LastModified?.Value);
			return obj;


		}
		public override MappedShipment PullEntity(String externID, String externalInfo)
		{
			FulfillmentData data = fulfillmentDataProvider.GetByID(externID.KeySplit(0), externID.KeySplit(1));
			if (data == null) return null;

			MappedShipment obj = new MappedShipment(new ShipmentData() { FulfillmentDataList = new List<FulfillmentData>() { data } }, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateModifiedAt.ToDate(false), data.CalculateHash());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForImport(SPShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			bucket.Shipment = bucket.Shipment.Set(new ShipmentData(), syncstatus.ExternID, syncstatus.ExternTS);

			return EntityStatus.None;
		}

		public override void MapBucketImport(SPShipmentEntityBucket bucket, IMappedEntity existing)
		{
		}
		public override void SaveBucketImport(SPShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			var giResult = cbapi.GetGIResult<BCShipmentsResult>(new BCShipments()
			{
				BindingID = currentBinding.BindingID.ValueField(),
				LastModified = minDateTime?.ValueField()
			}, BCConstants.GenericInquiryShipment);

			foreach (var result in giResult)
				{
				if (result.NoteID?.Value == null)
					continue;

				BCShipments bCShipments = new BCShipments() { ShippingNoteID = result.NoteID, LastModified = result.LastModifiedDateTime, ExternalShipmentUpdated = result.ExternalShipmentUpdated };
				MappedShipment obj = new MappedShipment(bCShipments, bCShipments.ShippingNoteID.Value, bCShipments.LastModified.Value);
					EntityStatus status = EnsureStatus(obj, SyncDirection.Export);
				}
			}

		protected virtual void MapFilterFields(List<BCShipmentsResult> results, BCShipments impl)
		{
			impl.OrderNoteIds = new List<Guid?>();
			foreach (var result in results)
			{
				impl.ShippingNoteID = result.NoteID;
				impl.VendorRef = result.InvoiceNbr;
				impl.ShipmentNumber = result.ShipmentNumber;
				impl.ShipmentType = result.ShipmentType;
				impl.LastModified = result.LastModifiedDateTime;
				impl.Confirmed = result.Confirmed;
				impl.ExternalShipmentUpdated = result.ExternalShipmentUpdated;
				impl.OrderNoteIds.Add(result.OrderNoteID.Value);
			}
		}

		public override EntityStatus GetBucketForExport(SPShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			SOOrderShipments impl = new SOOrderShipments();
			BCShipments giResult = new BCShipments()
			{
				ShippingNoteID = syncstatus.LocalID.ValueField()
			};
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();
			if (giResult?.Results == null || giResult?.Results?.Any() != true) return EntityStatus.None;

			MapFilterFields(giResult?.Results, giResult);
			if (giResult?.ShipmentType.Value == SOShipmentType.DropShip)
			{
				return GetDropShipment(bucket, giResult);
			}
			else if (giResult?.ShipmentType.Value == SOShipmentType.Invoice)
			{
				return GetInvoice(bucket, giResult);
			}
			else
			{
				return GetShipment(bucket, giResult);
			}
		}


		public override void MapBucketExport(SPShipmentEntityBucket bucket, IMappedEntity existing)
		{
			MappedShipment obj = bucket.Shipment;
			if (obj.Local?.Confirmed?.Value == false) throw new PXException(BCMessages.ShipmentNotConfirmed);
			List<BCLocations> locationMappings = new List<BCLocations>();
			if (currentBindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse)
			{
				foreach (PXResult<BCLocations, INSite, INLocation> result in PXSelectJoin<BCLocations,
					InnerJoin<INSite, On<INSite.siteID, Equal<BCLocations.siteID>>,
					LeftJoin<INLocation, On<INLocation.siteID, Equal<BCLocations.siteID>, And<BCLocations.locationID, Equal<INLocation.locationID>>>>>,
					Where<BCLocations.bindingID, Equal<Required<BCLocations.bindingID>>, And<BCLocations.mappingDirection, Equal<BCMappingDirectionAttribute.export>>>,
					OrderBy<Desc<BCLocations.mappingDirection>>>.Select(this, currentBinding.BindingID))
				{
					var bl = (BCLocations)result;
					var site = (INSite)result;
					var iNLocation = (INLocation)result;
					bl.SiteCD = site.SiteCD.Trim();
					bl.LocationCD = bl.LocationID == null ? null : iNLocation.LocationCD.Trim();
					locationMappings.Add(bl);
				}
			}
			if (obj.Local.ShipmentType.Value == SOShipmentType.DropShip)
			{
				PurchaseReceipt impl = obj.Local.POReceipt;
				MapDropShipment(bucket, obj, impl, locationMappings);
			}
			else if (obj.Local.ShipmentType.Value == SOShipmentType.Issue)
			{
				Shipment impl = obj.Local.Shipment;
				MapShipment(bucket, obj, impl, locationMappings);
			}
			else
			{
				Shipment impl = obj.Local.Shipment;
				MapInvoice(bucket, obj, impl, locationMappings);
			}

			ValidateShipments(bucket, obj);
		}

		public override CustomField GetLocalCustomField(SPShipmentEntityBucket bucket, string viewName, string fieldName)
		{
			MappedShipment obj = bucket.Shipment;
			BCShipments impl = obj.Local;
			if (impl?.Results?.Count() > 0)
				return impl.Results[0].Custom?.Where(x => x.ViewName == viewName && x.FieldName == fieldName).FirstOrDefault();
			else return null;
		}

		public void ValidateShipments(SPShipmentEntityBucket bucket, MappedShipment obj)
                {
			ShipmentData shipment = obj.Extern;
			//Validate all Shipments:
			//1. generate the removal list for these shipping lines have been removed from Shipment and no more shipping lines from the same order
			//2. match the eixsting external Shipment with current FulfillmentData
			//2. Validate the Shipping Qty, ensure "Shipping Qty" + "Shipped Qty in SPC" should be less or equal to order quantity.
			if (shipment.FulfillmentDataList.Any())
                    {
				var existingDetailsForShipment = obj.Details.Where(d => d.EntityType == BCEntitiesAttribute.ShipmentLine || d.EntityType == BCEntitiesAttribute.ShipmentBoxLine);
				var existingDetailsForRemovedOrders = existingDetailsForShipment.Where(x => shipment.FulfillmentDataList.Any(osd => osd.OrderLocalID == x.LocalID) == false);
				//if detail existed but no more order in the Shipment, should delete the Shipment and update order status.
				//That means the Shipment exported to BC before, and then Shipment in AC changed, it removed previous Shipping items, and no more Shipping items represent to the order.
				//Scenario:
				//1. Shipment has itemA from orderA and itemB from orderB, sync the Shipment to SPC and it created sync details for both itemA and itemB.
				//2. Changed the Shipment, removed itemA and kept itemB in the Shipment, sync the Shipment again. System should cancel the itemA Shipment record in Shopify
				foreach (var detail in existingDetailsForRemovedOrders)
                    {
					if (detail.ExternID.HasParent() && !string.IsNullOrEmpty(detail.ExternID.KeySplit(1)))
                        {
						shipment.ExternShipmentsToRemove[detail.ExternID.KeySplit(1)] = detail.ExternID.KeySplit(0);
                        }
                    }

				//Group the shipmentData by Order, and then generate the removal list for the same existing shipments,
				//validate the Shipping Qty, ensure "Shipping Qty" + "Shipped Qty in SPC" should be less or equal to order quantity.
				foreach (var shipmentDataByOrder in shipment.FulfillmentDataList.GroupBy(x => new { x.OrderId, x.OrderLocalID }))
			{
					MappedOrder mappedOrder = bucket.Orders.FirstOrDefault(x => x.LocalID == shipmentDataByOrder.Key.OrderLocalID);

					string orderID = shipmentDataByOrder.Key.OrderId.ToString();

					//Get SyncDetails for current order
					var existingDetailsForOrder = existingDetailsForShipment.Where(x => x.LocalID == shipmentDataByOrder.Key.OrderLocalID);

					//Get extern Order, OrderShipments and OrderProducts from Shopify
					OrderData externOrder = externOrder = orderDataProvider.GetByID(orderID, false, false, false);
					List<FulfillmentData> existingFulfillments = externOrder.Fulfillments.Where(i => i.Status == FulfillmentStatus.Success).ToList();

					//qtyByItem for all items of order quantities and qtyUsedOnOrder is for the amount of objects processed by external shipments, refunds and used to prevent overshipping
					Dictionary<long, int> qtyByItem = externOrder.LineItems.Where(i => i.RequiresShipping == true).ToDictionary(x => (long)x.Id, x => x.Quantity ?? 0);
					Dictionary<long, int> qtyUsedOnOrder = qtyByItem.ToDictionary(item => (long)item.Key, item =>
						externOrder.Fulfillments.Where(i => i.Status == FulfillmentStatus.Success).SelectMany(i => i.LineItems).Where(i => i.Id == item.Key).Sum(i => i.Quantity ?? 0) +
						externOrder.Refunds.SelectMany(i => i.RefundLineItems).Where(i => i.LineItemId == item.Key).Sum(i => i.Quantity ?? 0));

					//If the order status in external order has been Cancelled or Refunded, we should not create Shipment and throw error
					if (externOrder?.CancelledAt != null || externOrder?.FinancialStatus == OrderFinancialStatus.Refunded)
						throw new PXException(BCMessages.InvalidOrderStatusforShipment, externOrder?.Id, externOrder?.CancelledAt != null ? OrderStatus.Cancelled.ToString() : OrderFinancialStatus.Refunded.ToString());

					//Get all fulfillments that matching the externID in syncDetails
					var existingFulfillmentsForCurrent = existingFulfillments.Where(x => x != null && existingDetailsForOrder.Any(d => string.Equals(d.ExternID, $"{x.OrderId};{x.Id}", StringComparison.OrdinalIgnoreCase)));
					//If there is only one matching record, just need to assign existing fulfillmentID to current one. This should cover most of cases
					if(existingFulfillmentsForCurrent?.Count() == 1 && shipmentDataByOrder.Count() == 1)
				{
						var oneShipment = shipmentDataByOrder.First();
						//if line items and quantity matched, only need to update
						if (existingFulfillmentsForCurrent.First().LineItems.All(i => oneShipment.LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)) &&
									oneShipment.LineItems.All(i => existingFulfillmentsForCurrent.First().LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)))
					{
							//Don't send duplicated message to customer
							oneShipment.NotifyCustomer = false;
							oneShipment.Id = existingFulfillmentsForCurrent.First().Id;
				}
				else
				{
							//Must cancel fulfillment because changing number of lines/item quantities impossible
							shipment.ExternShipmentsToRemove[existingFulfillmentsForCurrent.First().Id.ToString()] = orderID;

							//reduce the existing shipment qty and add current shipment qty to qtyUsedOnOrder dictionary in case to compare total qty later
							qtyByItem.Keys.ForEach(id => qtyUsedOnOrder[id] = qtyUsedOnOrder[id] - (existingFulfillmentsForCurrent.First()?.LineItems?.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0)
														+ (oneShipment.LineItems.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0));
				}
						existingFulfillments.Remove(existingFulfillmentsForCurrent.First());
			}
					else 
			{
						//Validate Shipments, ensure all matching shipments link to existing shipment id and update data only. 
						foreach (var oneShipment in shipmentDataByOrder)
				{
							//find the matching fulfillment by tracking number
							FulfillmentData matchingFulfillment = existingFulfillments?.FirstOrDefault(i => !String.IsNullOrEmpty(i.TrackingNumbers.FirstOrDefault()) &&
								string.Equals(i.TrackingNumbers.FirstOrDefault(), oneShipment.TrackingNumbers.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
							//If the fulfillment is not matched by lines and quantities, but with tracking number, we cannot modify it externally and need to cancel it first
							if (matchingFulfillment != null)
			{
								existingFulfillments.Remove(matchingFulfillment);
								if (matchingFulfillment.LineItems.All(i => oneShipment.LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)) &&
									oneShipment.LineItems.All(i => matchingFulfillment.LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)))
				{
									oneShipment.NotifyCustomer = false;
									oneShipment.Id = matchingFulfillment.Id;
									continue;
			}
								//Must cancel fulfillment because changing number of lines/item quantities impossible
								shipment.ExternShipmentsToRemove[matchingFulfillment.Id.ToString()] = orderID;

								//reduce the existing shipment qty and add current shipment qty to qtyUsedOnOrder dictionary in case to compare total qty later
								qtyByItem.Keys.ForEach(id => qtyUsedOnOrder[id] = qtyUsedOnOrder[id] - (matchingFulfillment?.LineItems?.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0)
															+ (oneShipment.LineItems.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0));
		}
							else
		{
								//for items that cannot match by Tracking number, compare itemID and quantity to find the matching shipment
								var matchingFulfillments = existingFulfillments?.Where(i =>
									i.LineItems.All(x => oneShipment.LineItems.Any(item => item.Id == x.Id && item.Quantity == x.Quantity && x.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)) &&
									oneShipment.LineItems.All(x => i.LineItems.Any(item => item.Id == x.Id && item.Quantity == x.Quantity && item.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)));
								if (matchingFulfillments != null && matchingFulfillments.Count() > 1 && existingDetailsForOrder != null)
			{
									List<FulfillmentData> matches = new List<FulfillmentData>();
									//Find the relationship between sync detail record and external shipment
									foreach (var detail in existingDetailsForOrder)
				{
										matches.AddRange(matchingFulfillments.Where(mf => String.Equals(detail.ExternID, $"{mf.OrderId};{mf.Id}", StringComparison.OrdinalIgnoreCase))?.ToList() ?? new List<FulfillmentData>());
									}
									matchingFulfillments = matches;
								}
								if (matchingFulfillments.Any())
								{
									//for matching shipment, link it to existing one and only update.
									oneShipment.Id = matchingFulfillments.FirstOrDefault()?.Id;
									oneShipment.NotifyCustomer = false;
									existingFulfillments.Remove(matchingFulfillments.FirstOrDefault());
				}
				else
				{
									//If no matching shipment, add shipping qty to qtyUsedOnOrder dictionary in case to compare total qty later
									qtyByItem.Keys.ForEach(id => qtyUsedOnOrder[id] = qtyUsedOnOrder[id] + (oneShipment.LineItems.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0));
								}
				}
			}
		}

					//verify that after exporting we will not exceed item quantity on all fulfillments and also predict if order will be entirely fulfilled
					qtyUsedOnOrder.ForEach(x =>
				{
						if (x.Value > qtyByItem[x.Key])
							throw new PXException(BCMessages.ShipmentCannotBeExported, externOrder.LineItems.FirstOrDefault(i => i.Id == x.Key)?.Sku);
					});
				}
				}
			}

		public override void SaveBucketExport(SPShipmentEntityBucket bucket, IMappedEntity existing, String operation)
				{
			MappedShipment obj = bucket.Shipment;

			StringBuilder key = new StringBuilder();

			if (obj.Extern.FulfillmentDataList.Any() == false)
							{
				SetInvalidStatus(obj,SPConnector.NAME);
				return;
						}
			
			obj.ClearDetails();

			//Based on the validation result, cancel some shipments from Shopify
			foreach (var shipmentToRemove in obj.Extern.ExternShipmentsToRemove)
			{
				CancelFullfillment(bucket, shipmentToRemove.Value, shipmentToRemove.Key);
					}

			//Create all shipments for given order
			try
					{
				foreach (var shipmentDataByOrder in obj.Extern.FulfillmentDataList.GroupBy(x => new { x.OrderId, x.OrderLocalID }))
						{
					MappedOrder mappedOrder = bucket.Orders.FirstOrDefault(x => x.LocalID == shipmentDataByOrder.Key.OrderLocalID);
					DateTime? lastModifiedOrderAt = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

					foreach (var shipmentData in shipmentDataByOrder)
							{
						FulfillmentData data = SaveFullfillment(shipmentData);
						if (lastModifiedOrderAt < data.DateModifiedAt)
							lastModifiedOrderAt = (DateTime)data.DateModifiedAt;

						obj.With(_ => { _.ExternID = null; return _; }).AddExtern(obj.Extern, new object[] { data.OrderId, data.Id }.KeyCombine(), data.DateModifiedAt.ToDate());
						obj.AddDetail(shipmentData.ShipmentType, shipmentData.OrderLocalID, new object[] { data.OrderId, data.Id }.KeyCombine());

						//Concat all externID together and then show in the externId field to user
						key.Append(key.Length > 0 ? "|" + obj.ExternID : obj.ExternID);
							}

					//Get orderData to check the fulfillment status
					OrderData orderData = orderDataProvider.GetByID(shipmentDataByOrder.Key.OrderId.ToString(), false, false, false);
					if (orderData.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)
						lastModifiedOrderAt = orderData.DateModifiedAt;

					//Update order lastModifiedDate info in sync record
					mappedOrder.AddExtern(null, mappedOrder.ExternID, lastModifiedOrderAt.ToDate(false));
					UpdateStatus(mappedOrder, null);
				}

				obj.ExternID = key.ToString().TrimExternID();
				UpdateStatus(obj, operation);
						}
			catch(Exception ex)
						{
				UpdateStatus(obj, BCSyncOperationAttribute.ExternFailed, ex.InnerException?.Message ?? ex.Message);
						}

			#region Reset externalShipmentUpdated flag
			List<PXDataFieldParam> fieldParams = new List<PXDataFieldParam>();
			fieldParams.Add(new PXDataFieldAssign(typeof(BCSOShipmentExt.externalShipmentUpdated).Name, PXDbType.Bit, true));
			fieldParams.Add(new PXDataFieldRestrict(typeof(PX.Objects.SO.SOShipment.noteID).Name, PXDbType.UniqueIdentifier, obj.LocalID));
			PXDatabase.Update<PX.Objects.SO.SOShipment>(fieldParams.ToArray());
			#endregion
					}

		private void CancelFullfillment(SPShipmentEntityBucket bucket, String orderID, String fulfillmentID)
				   {
			if (!string.IsNullOrEmpty(orderID) && !string.IsNullOrEmpty(fulfillmentID))
			{
				try
				{
					fulfillmentDataProvider.CancelFulfillment(orderID, fulfillmentID);
				}
				catch (Exception ex)
                {
					Log(bucket?.Primary?.SyncID, SyncDirection.Export, ex);
				}
			}
		}

		private FulfillmentData SaveFullfillment(FulfillmentData ordersShipmentData)
		{
			if (ordersShipmentData.Id != null)
				return fulfillmentDataProvider.Update(ordersShipmentData, ordersShipmentData.OrderId.ToString(), ordersShipmentData.Id.ToString());
			else
				return fulfillmentDataProvider.Create(ordersShipmentData, ordersShipmentData.OrderId.ToString());
		}

		#region ShipmentGetSection

		protected virtual void GetOrderShipment(BCShipments bCShipments)
		{
			if (bCShipments.ShipmentType?.Value == SOShipmentType.DropShip)
				GetDropShipmentByShipmentNbr(bCShipments);
			else if (bCShipments.ShipmentType.Value == SOShipmentType.Invoice)
				GetInvoiceByShipmentNbr(bCShipments);
			else
				bCShipments.Shipment = cbapi.GetByID<Shipment>(bCShipments.ShippingNoteID.Value,
					new Shipment()
					{
						ReturnBehavior = ReturnBehavior.OnlySpecified,
						Details = new List<ShipmentDetail>() { new ShipmentDetail() },
						Packages = new List<ShipmentPackage>() { new ShipmentPackage() },
						Orders = new List<ShipmentOrderDetail>() { new ShipmentOrderDetail() },
					});

		}

		protected virtual void GetInvoiceByShipmentNbr(BCShipments bCShipment)
		{
			bCShipment.Shipment = new Shipment();
			bCShipment.Shipment.Details = new List<ShipmentDetail>();

			foreach (PXResult<ARTran, SOOrder> item in PXSelectJoin<ARTran,
				InnerJoin<SOOrder, On<ARTran.sOOrderType, Equal<SOOrder.orderType>, And<ARTran.sOOrderNbr, Equal<SOOrder.orderNbr>>>>,
				Where<ARTran.refNbr, Equal<Required<ARTran.refNbr>>>>
			.Select(this, bCShipment.ShipmentNumber.Value))
			{
				ARTran line = item.GetItem<ARTran>();
				ShipmentDetail detail = new ShipmentDetail();
				detail.OrderNbr = line.SOOrderNbr.ValueField();
				detail.OrderLineNbr = line.SOOrderLineNbr.ValueField();
				detail.OrderType = line.SOOrderType.ValueField();
				bCShipment.Shipment.Details.Add(detail);
			}
		}

		protected virtual void GetDropShipmentByShipmentNbr(BCShipments bCShipments)
		{
			bCShipments.POReceipt = new PurchaseReceipt();
			bCShipments.POReceipt.ShipmentNbr = bCShipments.ShipmentNumber;
			bCShipments.POReceipt.VendorRef = bCShipments.VendorRef;
			bCShipments.POReceipt.Details = new List<PurchaseReceiptDetail>();

			foreach (PXResult<SOLineSplit, POOrder, SOOrder> item in PXSelectJoin<SOLineSplit,
				InnerJoin<POOrder, On<POOrder.orderNbr, Equal<SOLineSplit.pONbr>>,
				InnerJoin<SOOrder, On<SOLineSplit.orderNbr, Equal<SOOrder.orderNbr>>>>,
				Where<SOLineSplit.pOReceiptNbr, Equal<Required<SOLineSplit.pOReceiptNbr>>>>
			.Select(this, bCShipments.ShipmentNumber.Value))
			{
				SOLineSplit lineSplit = item.GetItem<SOLineSplit>();
				SOOrder line = item.GetItem<SOOrder>();
				POOrder poOrder = item.GetItem<POOrder>();
				PurchaseReceiptDetail detail = new PurchaseReceiptDetail();
				detail.SOOrderNbr = lineSplit.OrderNbr.ValueField();
				detail.SOLineNbr = lineSplit.LineNbr.ValueField();
				detail.SOOrderType = lineSplit.OrderType.ValueField();
				detail.ReceiptQty = lineSplit.ShippedQty.ValueField();
				detail.ShipVia = poOrder.ShipVia.ValueField();
				detail.SONoteID = line.NoteID.ValueField();
				bCShipments.POReceipt.Details.Add(detail);
			}
		}

		protected virtual EntityStatus GetDropShipment(SPShipmentEntityBucket bucket, BCShipments bCShipments)
		{
			if (bCShipments.ShipmentNumber == null) return EntityStatus.None;
			GetDropShipmentByShipmentNbr(bCShipments);
			if (bCShipments.POReceipt == null || bCShipments.POReceipt?.Details?.Count == 0)
				return EntityStatus.None;

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipments, bCShipments.ShippingNoteID.Value, bCShipments.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<PurchaseReceiptDetail> lines = bCShipments.POReceipt.Details
				.GroupBy(r => new { OrderType = r.SOOrderType.Value, OrderNbr = r.SOOrderNbr.Value })
				.Select(r => r.First());
			foreach (PurchaseReceiptDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.SOOrderType.Value.SearchField(), OrderNbr = line.SOOrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipments.POReceipt.ShipmentNbr.Value);

				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);

				bucket.Orders.Add(orderObj);
			}
			return status;
		}
		protected virtual EntityStatus GetShipment(SPShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShippingNoteID == null || bCShipment.ShippingNoteID.Value == Guid.Empty) return EntityStatus.None;
			bCShipment.Shipment = cbapi.GetByID<Shipment>(bCShipment.ShippingNoteID.Value,
					new Shipment()
					{
						ReturnBehavior = ReturnBehavior.OnlySpecified,
						Details = new List<ShipmentDetail>() { new ShipmentDetail() },
						Packages = new List<ShipmentPackage>() { new ShipmentPackage() },
						Orders = new List<ShipmentOrderDetail>() { new ShipmentOrderDetail() },
					});
			if (bCShipment.Shipment == null || bCShipment.Shipment?.Details?.Count == 0)
				return EntityStatus.None;

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipment, bCShipment.ShippingNoteID.Value, bCShipment.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<ShipmentDetail> lines = bCShipment.Shipment.Details
				.GroupBy(r => new { OrderType = r.OrderType.Value, OrderNbr = r.OrderNbr.Value })
				.Select(r => r.First());
			foreach (ShipmentDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.OrderType.Value.SearchField(), OrderNbr = line.OrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipment.Shipment.ShipmentNbr.Value);
				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);

				bucket.Orders.Add(orderObj);
			}
			return status;
		}
		protected virtual EntityStatus GetInvoice(SPShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShipmentNumber == null) return EntityStatus.None;
			GetInvoiceByShipmentNbr(bCShipment);
			if (bCShipment.Shipment?.Details?.Count == 0) return EntityStatus.None;

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipment, bCShipment.ShippingNoteID.Value, bCShipment.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<ShipmentDetail> lines = bCShipment.Shipment.Details
				.GroupBy(r => new { OrderType = r.OrderType.Value, OrderNbr = r.OrderNbr.Value })
				.Select(r => r.First());
			foreach (ShipmentDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.OrderType.Value.SearchField(), OrderNbr = line.OrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipment.Shipment.ShipmentNbr.Value);
				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);

				bucket.Orders.Add(orderObj);
			}
			return status;
		}

		#endregion

		#region ShipmentMappingSection

		protected virtual void MapDropShipment(SPShipmentEntityBucket bucket, MappedShipment obj, PurchaseReceipt impl, List<BCLocations> locationMappings)
		{
			ShipmentData shipment = obj.Extern = new ShipmentData();

			foreach (MappedOrder order in bucket.Orders)
			{
				FulfillmentData shipmentData = new FulfillmentData();
			shipmentData.LineItems = new List<OrderLineItem>();

				shipmentData.OrderId = order.ExternID.ToLong();
				shipmentData.OrderLocalID = order.LocalID;
			shipmentData.LocationId = defaultLocationId;
				shipmentData.ShipmentType = BCEntitiesAttribute.ShipmentLine;

			var shipvia = impl.Details.FirstOrDefault(x => !string.IsNullOrEmpty(x.ShipVia?.Value))?.ShipVia?.Value ?? string.Empty;
			shipmentData.TrackingCompany = GetCarrierName(shipvia);
			shipmentData.TrackingNumbers = new List<string>() { impl.VendorRef?.Value };

				foreach (PurchaseReceiptDetail line in impl.Details ?? new List<PurchaseReceiptDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.SOOrderType.Value && order.Local.OrderNbr.Value == line.SOOrderNbr.Value && d.LineNbr.Value == line.SOLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) continue;

					OrderLineItem shipItem = new OrderLineItem();
					shipItem.Id = lineInfo.ExternID.ToLong();
					shipItem.Quantity = (int)line.ReceiptQty.Value;
					shipItem.OrderId = order.ExternID.ToLong();

					shipmentData.LineItems.Add(shipItem);
				}

				//Add to Shipment only if ShipmentItems have value
				if (shipmentData.LineItems.Any())
					shipment.FulfillmentDataList.Add(shipmentData);
			}
		}

		protected virtual void MapInvoice(SPShipmentEntityBucket bucket, MappedShipment obj, Shipment impl, List<BCLocations> locationMappings)
		{
			ShipmentData shipment = obj.Extern = new ShipmentData();

			foreach (MappedOrder order in bucket.Orders)
			{
				FulfillmentData shipmentData = new FulfillmentData();
				shipmentData.LineItems = new List<OrderLineItem>();

				shipmentData.OrderId = order.ExternID.ToLong();
				shipmentData.OrderLocalID = order.LocalID;
				shipmentData.ShipmentType = BCEntitiesAttribute.ShipmentLine;
				shipmentData.LocationId = GetMappedExternalLocation(locationMappings, impl.WarehouseID.Value, impl.Details.FirstOrDefault()?.LocationID.Value);

				foreach (ShipmentDetail line in impl.Details ?? new List<ShipmentDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) continue;

					OrderLineItem shipItem = new OrderLineItem();
						shipItem.Id = lineInfo.ExternID.ToLong();
						shipItem.Quantity = (int)line.ShippedQty.Value;
						shipItem.OrderId = order.ExternID.ToLong();
						shipmentData.LineItems.Add(shipItem);
				}

				//Add to Shipment only if ShipmentItems have value
				if (shipmentData.LineItems.Any())
					shipment.FulfillmentDataList.Add(shipmentData);
			}
		}

		protected virtual void MapShipment(SPShipmentEntityBucket bucket, MappedShipment obj, Shipment impl, List<BCLocations> locationMappings)
		{
			ShipmentData shipment = obj.Extern = new ShipmentData();

			//Get Package Details, there is only InventoryID in SOShipLineSplitPackage, in case to compare InventoryCD field with Shipping line item, get InventoryCD from InventoryItem and save it in the Tuple.
			List<Tuple<SOShipLineSplitPackage, string>> PackageDetails = new List<Tuple<SOShipLineSplitPackage, string>>();
			foreach (PXResult<SOShipLineSplitPackage, InventoryItem> item in PXSelectJoin<SOShipLineSplitPackage,
				InnerJoin<InventoryItem, On<SOShipLineSplitPackage.inventoryID, Equal<InventoryItem.inventoryID>>>,
				Where<SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>>>.
				Select(this, impl.ShipmentNbr?.Value))
			{
				PackageDetails.Add(Tuple.Create(item.GetItem<SOShipLineSplitPackage>(), item.GetItem<InventoryItem>().InventoryCD.Trim()));
			}

			var packages = new List<ShipmentPackage>();

			foreach (ShipmentPackage package in impl.Packages ?? new List<ShipmentPackage>())
				{
				//Check the Package whether has shipping items in it
				var detail = PackageDetails.Where(x => x.Item1.PackageLineNbr == package.LineNbr?.Value && x.Item1.PackedQty != 0);

				if (string.IsNullOrEmpty(package.TrackingNbr?.Value))
					continue; // if tracking number is empty then ignore

				//If there is not content in the package, that means it's a empty package, we ship emptybox as well.
				//If it's not empty, add detail item info to ShipmentLineNbr, in case to compare with Shipping lines later
				if (detail.Any())
					{
					package.ShipmentLineNbr.AddRange(detail.Select(x => new Tuple<int?, decimal?>(x.Item1.ShipmentLineNbr, x.Item1.PackedQty)));
				}
				packages.Add(package);
			}

			foreach (MappedOrder order in bucket.Orders)
			{
				List<FulfillmentData> shipmentsPerOrder = new List<FulfillmentData>();

				//get all line items for the current order in this Shipment
				foreach (ShipmentDetail line in impl.Details ?? new List<ShipmentDetail>())
					{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					//if no data found in sync detail, try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null)
						lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value);
					if (lineInfo == null)
						continue;// if order line not present in external system then just skip

					//get all packages with current line items or empty packages with Tracking number
					var matchingPackages = packages.Where(x => !string.IsNullOrEmpty(x.TrackingNbr?.Value) && (x.ShipmentLineNbr.Any() == false || x.ShipmentLineNbr.Any(s => s.Item1 == line.LineNbr.Value)));
					//get the matching shopify locationID or default one
					var locationId = GetMappedExternalLocation(locationMappings, impl.WarehouseID.Value, line?.LocationID?.Value);

					FulfillmentData shipmentData = shipmentsPerOrder.FirstOrDefault(x => string.Equals(x.OrderId.ToString(), order.ExternID) && x.LocationId == locationId);
					if (shipmentData == null)
					{
						//if shipmentData doesn't exist in current order, create a new one and fill up values
						shipmentData = new FulfillmentData();
						shipmentData.LineItems = new List<OrderLineItem>();
						shipmentData.LocationId = locationId;
						shipmentData.TrackingCompany = GetCarrierName(impl.ShipVia?.Value ?? string.Empty);
						shipmentData.ShipmentType = BCEntitiesAttribute.ShipmentLine;
						shipmentData.OrderId = order.ExternID?.ToLong();
						shipmentData.OrderLocalID = order.LocalID;
						shipmentData.TrackingNumbers = new List<string>();
						shipmentsPerOrder.Add(shipmentData);
					}

					//Compare tracking number, if it doesn't include in the list, add it.
					foreach(var onePackage in matchingPackages)
					{
						if (!shipmentData.TrackingNumbers.Contains(onePackage.TrackingNbr?.Value))
							shipmentData.TrackingNumbers.Add(onePackage.TrackingNbr.Value);
					}

						OrderLineItem shipItem = new OrderLineItem();
						shipItem.Id = lineInfo.ExternID.ToLong();
						shipItem.OrderId = order.ExternID.ToLong();
					shipItem.Quantity = (int)(line.ShippedQty.Value ?? 0); //Use rest ShippedQty of line item
						shipmentData.LineItems.Add(shipItem);
					}

				//Add to Fulfillment list if there is a shipment for current order
				if(shipmentsPerOrder.Any())
					shipment.FulfillmentDataList.AddRange(shipmentsPerOrder);
			}
		}

		#endregion

		protected virtual string GetCarrierName(string shipVia)
		{
			string company = null;
			if (!string.IsNullOrEmpty(shipVia))
			{
				PX.Objects.CS.Carrier carrierData = SelectFrom<PX.Objects.CS.Carrier>.Where<PX.Objects.CS.Carrier.carrierID.IsEqual<@P.AsString>>.View.Select(this, shipVia);
				if (!string.IsNullOrEmpty(carrierData?.CarrierPluginID))
				{
					company = carrierData?.CarrierPluginID;
				}
				else
					company = shipVia;
				company = helper.GetSubstituteExternByLocal(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.Carriers), company, company);
			}

			return company;
		}

		protected virtual DetailInfo MatchOrderLineFromExtern(string externalOrderId, string identifyKey)
		{
			DetailInfo lineInfo = null;
			if (string.IsNullOrEmpty(externalOrderId) || string.IsNullOrEmpty(identifyKey))
				return lineInfo;
			var orderLineDetails = orderDataProvider.GetByID(externalOrderId, includedMetafields: false, includedTransactions: false, includedCustomer: false, includedOrderRisk: false)?.LineItems;
			var matchedLine = orderLineDetails?.FirstOrDefault(x => string.Equals(x?.Sku, identifyKey, StringComparison.OrdinalIgnoreCase));
			if (matchedLine != null && matchedLine?.Id.HasValue == true)
			{
				lineInfo = new DetailInfo(BCEntitiesAttribute.OrderLine, null, matchedLine.Id.ToString());
			}
			return lineInfo;
		}

		protected virtual long? GetMappedExternalLocation(List<BCLocations> locationMappings, string siteCD, string locationCD)
		{
			if (locationMappings?.Count == 0 || string.IsNullOrEmpty(siteCD))
				return defaultLocationId;
			var matchedItem = locationMappings.FirstOrDefault(l => !string.IsNullOrEmpty(l.ExternalLocationID) && string.Equals(l.SiteCD, siteCD, StringComparison.OrdinalIgnoreCase) && (l.LocationID == null || (l.LocationID != null && string.Equals(l.LocationCD, locationCD, StringComparison.OrdinalIgnoreCase))));
			if (matchedItem != null)
				return inventoryLocations.Any(x => x.Id?.ToString() == matchedItem.ExternalLocationID) ? matchedItem.ExternalLocationID?.ToLong() : defaultLocationId;
			else
				return defaultLocationId;
		}
		#endregion
	}
}
