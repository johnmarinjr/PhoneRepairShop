using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Common;
using PX.Data.BQL;
using PX.Objects.SO;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.PO;
using PX.Objects.IN;
using PX.Api.ContractBased.Models;
using Serilog.Context;
using static PX.Objects.SO.SOShipmentEntry;
using PX.Objects.CS;

namespace PX.Commerce.BigCommerce
{
	public class BCShipmentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Shipment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary }.Concat(Orders).ToArray();

		public MappedShipment Shipment;
		public List<MappedOrder> Orders = new List<MappedOrder>();
	}

	public class BCShipmentsRestrictor : BCBaseRestrictor, IRestrictor
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
					if (obj.Local?.OrderNoteIds != null)
					{
						BCBindingExt binding = processor.GetBindingExt<BCBindingExt>();

						Boolean anyFound = false;
						foreach (var orderNoteID in obj.Local?.OrderNoteIds)
						{
							if (processor.SelectStatus(BCEntitiesAttribute.Order, orderNoteID) == null) continue;

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

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.Shipment, BCCaptions.Shipment,
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
		URL = "orders?keywords={0}&searchDeletedOrders=no",
		Requires = new string[] { BCEntitiesAttribute.Order }
	)]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.ShipmentLine, EntityName = BCCaptions.ShipmentLine, AcumaticaType = typeof(PX.Objects.SO.SOShipLine))]
	[BCProcessorDetail(EntityType = BCEntitiesAttribute.ShipmentBoxLine, EntityName = BCCaptions.ShipmentLineBox, AcumaticaType = typeof(PX.Objects.SO.SOPackageDetailEx))]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Shipments" }, PushDestination = BCConstants.PushNotificationDestination)]
	public class BCShipmentProcessor : BCProcessorSingleBase<BCShipmentProcessor, BCShipmentEntityBucket, MappedShipment>, IProcessor
	{
		protected OrderRestDataProvider orderDataProvider;
		protected IChildRestDataProvider<OrdersShipmentData> orderShipmentRestDataProvider;
		protected IChildRestDataProvider<OrdersProductData> orderProductsRestDataProvider;
		protected IChildRestDataProvider<OrdersShippingAddressData> orderShippingAddressesRestDataProvider;

		protected List<BCShippingMappings> shippingMappings;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());

			orderDataProvider = new OrderRestDataProvider(client);
			orderShipmentRestDataProvider = new OrderShipmentsRestDataProvider(client);
			orderProductsRestDataProvider = new OrderProductsRestDataProvider(client);
			orderShippingAddressesRestDataProvider = new OrderShippingAddressesRestDataProvider(client);

			shippingMappings = PXSelectReadonly<BCShippingMappings,
				Where<BCShippingMappings.bindingID, Equal<Required<BCShippingMappings.bindingID>>>>
				.Select(this, Operation.Binding).Select(x => x.GetItem<BCShippingMappings>()).ToList();
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
				if (!obj.IsNew && obj.Local.ExternalShipmentUpdated?.Value == true) //mark as pending only if there is change in shipment
				{
					return false;
				}
			}
			return base.ControlModification(mapped, status, operation);
		}


		#region Pull
		public override MappedShipment PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			BCShipments giResult = (new BCShipments()
			{
				ShippingNoteID = localID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null) return null;
			MapFilterFields(binding, giResult?.Results, giResult);
			GetOrderShipment(giResult);
			if (giResult.Shipment == null && giResult.POReceipt == null) return null;
			MappedShipment obj = new MappedShipment(giResult, giResult.ShippingNoteID.Value, giResult.LastModified.Value);
			return obj;


		}
		public override MappedShipment PullEntity(String externID, String externalInfo)
		{
			OrdersShipmentData data = orderShipmentRestDataProvider.GetByID(externID.KeySplit(1), externID.KeySplit(0));
			if (data == null) return null;

			MappedShipment obj = new MappedShipment(new ShipmentData() { OrdersShipmentDataList = new List<OrdersShipmentData>() { data } }, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate(), data.CalculateHash());

			return obj;
		}
		#endregion

		#region Import
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForImport(BCShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			bucket.Shipment = bucket.Shipment.Set(new ShipmentData(), syncstatus.ExternID, syncstatus.ExternTS);

			return EntityStatus.None;
		}
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void MapBucketImport(BCShipmentEntityBucket bucket, IMappedEntity existing)
		{
		}
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override void SaveBucketImport(BCShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && binding.SyncOrdersFrom != null && minDateTime < binding.SyncOrdersFrom) ? binding.SyncOrdersFrom : minDateTime;
			IEnumerable<BCShipmentsResult> giResult = cbapi.GetGIResult<BCShipmentsResult>(new BCShipments()
			{
				BindingID = GetBinding().BindingID.ValueField(),
				LastModified = minDate?.ValueField()
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

		protected virtual void MapFilterFields(BCBindingExt binding, List<BCShipmentsResult> results, BCShipments impl)
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

		public override EntityStatus GetBucketForExport(BCShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			SOOrderShipments impl = new SOOrderShipments();

			BCShipments giResult = (new BCShipments()
			{
				ShippingNoteID = syncstatus.LocalID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null || giResult?.Results?.Any() != true) return EntityStatus.None;

			MapFilterFields(binding, giResult?.Results, giResult);

			if (giResult.ShipmentType.Value == SOShipmentType.DropShip)
			{
				return GetDropShipment(bucket, giResult);
			}
			else if (giResult.ShipmentType.Value == SOShipmentType.Invoice)
			{

				return GetInvoice(bucket, giResult);
			}
			else
			{
				return GetShipment(bucket, giResult);
			}

		}

		public override void MapBucketExport(BCShipmentEntityBucket bucket, IMappedEntity existing)
		{
			MappedShipment obj = bucket.Shipment;
			if (obj.Local?.Confirmed?.Value == false) throw new PXException(BCMessages.ShipmentNotConfirmed);
			if (obj.Local.ShipmentType.Value == SOShipmentType.DropShip)
			{
				PurchaseReceipt impl = obj.Local.POReceipt;
				MapDropShipment(bucket, obj, impl);
			}
			else if (obj.Local.ShipmentType.Value == SOShipmentType.Issue)
			{
				Shipment impl = obj.Local.Shipment;
				MapShipment(bucket, obj, impl);
			}
			else
			{
				Shipment impl = obj.Local.Shipment;
				MapInvoice(bucket, obj, impl);
			}

			ValidateShipments(bucket, obj);
		}

		public override CustomField GetLocalCustomField(BCShipmentEntityBucket bucket, string viewName, string fieldName)
		{
			MappedShipment obj = bucket.Shipment;
			BCShipments impl = obj.Local;
			if (impl?.Results?.Count() > 0)
				return impl.Results[0].Custom?.Where(x => x.ViewName == viewName && x.FieldName == fieldName).FirstOrDefault();
			else return null;
		}

		public void ValidateShipments(BCShipmentEntityBucket bucket, MappedShipment obj)
		{
			ShipmentData shipment = obj.Extern;
			//Validate all Shipments:
			//1. generate the removal list for the same existing shipments
			//2. generate the removal list for these shipping lines have been removed from Shipment and no more shipping lines from the same order, we should delete the external Shipment and rollback the order shipping status to "AwaitingFulfillment"
			//3. Validate the Shipping Qty, ensure "Shipping Qty" + "Shipped Qty in BC" should be less or equal to order quantity.
			if (shipment.OrdersShipmentDataList.Any())
			{
				var existingDetailsForShipment = obj.Details.Where(d => d.EntityType == BCEntitiesAttribute.ShipmentLine || d.EntityType == BCEntitiesAttribute.ShipmentBoxLine);
				var existingDetailsForRemovedOrders = existingDetailsForShipment.Where(x => shipment.OrdersShipmentDataList.Any(osd => osd.OrderLocalID == x.LocalID) == false);
				//if detail existed but no more order in the Shipment, should delete the Shipment and update order status.
				//That means the Shipment exported to BC before, and then Shipment in AC changed, it removed previous Shipping items, and no more Shipping items represent to the order.
				//Scenario:
				//1. Shipment has itemA from orderA and itemB from orderB, sync the Shipment to BC and it created sync details for both itemA and itemB.
				//2. Changed the Shipment, removed itemA and kept itemB in the Shipment, sync the Shipment again. System should delete the itemA Shipment record in BC and rollback orderA status to "AwaitingFulfillment"
				foreach (var detail in existingDetailsForRemovedOrders)
				{
					if (detail.ExternID.HasParent() && !string.IsNullOrEmpty(detail.ExternID.KeySplit(1)))
					{
						shipment.ExternOrdersToUpdate[detail.ExternID.KeySplit(1)] = detail.ExternID.KeySplit(0);
					}
				}

				//Group the shipmentData by Order, and then generate the removal list for the same existing shipments,
				//validate the Shipping Qty, ensure "Shipping Qty" + "Shipped Qty in BC" should be less or equal to order quantity.
				foreach (var shipmentDataByOrder in shipment.OrdersShipmentDataList.GroupBy(x => new { x.OrderId, x.OrderLocalID }))
				{
					MappedOrder mappedOrder = bucket.Orders.FirstOrDefault(x => x.LocalID == shipmentDataByOrder.Key.OrderLocalID);

					string orderID = shipmentDataByOrder.Key.OrderId.ToString();

					//Get SyncDetails for current order
					var existingDetailsForOrder = existingDetailsForShipment.Where(x => x.LocalID == shipmentDataByOrder.Key.OrderLocalID);

					//Get extern Order, OrderShipments and OrderProducts from BC
					OrderData externOrder = orderDataProvider.GetByID(orderID);
					List<OrdersShipmentData> existingShipments = orderShipmentRestDataProvider.GetAll(orderID).ToList();
					List<OrdersProductData> orderProducts = orderProductsRestDataProvider.GetAll(orderID).ToList();

					//If the order status in external order has been Cancelled or Refunded, we should not create Shipment and throw error
					if (externOrder?.StatusId == OrderStatuses.Cancelled.GetHashCode() || externOrder?.StatusId == OrderStatuses.Refunded.GetHashCode())
						throw new PXException(BCMessages.InvalidOrderStatusforShipment, orderID, externOrder?.Status);

					//Check order whether have a shipping address, if no shipping address, the shipment cannot be created
					DetailInfo addressInfo = mappedOrder.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderAddress && d.LocalID == mappedOrder.LocalID);
					int orderAddressId;
					if (addressInfo != null)
						orderAddressId = addressInfo.ExternID.ToInt().Value;
					else
					{
						//If not found, try to get from BC
						List<OrdersShippingAddressData> addressesList = orderShippingAddressesRestDataProvider.GetAll(orderID).ToList();
						if (addressesList?.Any() == true)
							orderAddressId = addressesList.FirstOrDefault().Id.Value;
						else
							throw new PXException(BCMessages.ShippingTypeNotValid, obj.Local?.Shipment?.ShipmentNbr.Value);
					}

					//If there is existing shipment sync details for current order, we should remove them in BC and use the new data to create again.
					if (existingDetailsForOrder.Any())
					{
						foreach (var detail in existingDetailsForOrder)
						{
							var externShipmentID = detail.ExternID.HasParent() ? detail.ExternID.KeySplit(1) : null;
							var existingShipment = existingShipments?.FirstOrDefault(x => x != null && x.Id.ToString() == externShipmentID);
							//if shipment has been created in BC before, we should remove it later and then use new data to create again
							if (existingShipment != null)
							{
								shipment.ExternShipmentsToRemove[existingShipment.Id.ToString()] = orderID;
							}
						}
					}

					//Validate Shipments again, ensure all matching shipment but not in sync details should be removed correctly before pushing to BC.
					foreach (var oneShipment in shipmentDataByOrder)
					{
						oneShipment.OrderAddressId = orderAddressId;
						if (!string.IsNullOrEmpty(oneShipment.TrackingNumber))
						{
							//Try to get the existing extern Shipment with matching TrackingNumber but not in the sync detail list
							var existingShipment = existingShipments?.FirstOrDefault(x => x != null && string.Equals(x.TrackingNumber, oneShipment.TrackingNumber, StringComparison.OrdinalIgnoreCase) &&
								shipment.ExternShipmentsToRemove.ContainsKey(x.Id.ToString()) == false);
							if (existingShipment != null)
							{
								//Add to remove list
								shipment.ExternShipmentsToRemove[existingShipment.Id.ToString()] = orderID;
							}
						}
					}

					//Validate the Shipping Qty, ensure "Shipping Qty" + "Shipped Qty in BC" should be less or equal to order quantity.
					foreach (var orderProduct in orderProducts)
					{
						var remainShipmentQty = existingShipments?.Where(x => x != null && shipment.ExternShipmentsToRemove.ContainsKey(x.Id.ToString()) == false).SelectMany(x => x.ShipmentItems)?.Where(s => s.OrderProductId == orderProduct.Id).Sum(q => q.Quantity) ?? 0;
						var toShipQty = shipmentDataByOrder.SelectMany(x => x.ShipmentItems).Where(s => s.OrderProductId == orderProduct.Id).Sum(p => p.Quantity);
						if (toShipQty + remainShipmentQty > orderProduct.Quantity)
							throw new PXException(BCMessages.ShipmentCannotBeExported, orderProduct.Sku);
					}
				}
			}
		}

		public override void SaveBucketExport(BCShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedShipment obj = bucket.Shipment;

			StringBuilder key = new StringBuilder();

			if(obj.Extern.OrdersShipmentDataList.Any() == false)
			{
				SetInvalidStatus(obj, BCConnector.NAME);
				return;
			}
			
			obj.ClearDetails();

			//Delete all shipments for given BCSyncDetails
			foreach (var shipmentItem in obj.Extern.ExternShipmentsToRemove)
			{
				orderShipmentRestDataProvider.Delete(shipmentItem.Key, shipmentItem.Value);
			}

			//if order is removed from shipment then delete the shipment from BC and change status back to Awaiting Full fillment
			foreach (var orderToUpdate in obj.Extern.ExternOrdersToUpdate)
			{
				OrderStatus orderStatus = new OrderStatus();
				orderStatus.StatusId = OrderStatuses.AwaitingFulfillment.GetHashCode();
				orderShipmentRestDataProvider.Delete(orderToUpdate.Key, orderToUpdate.Value);
				orderStatus = orderDataProvider.Update(orderStatus, orderToUpdate.Value);
			}

			//Create all shipments for given order
			foreach (OrdersShipmentData shipmentData in obj.Extern.OrdersShipmentDataList)
			{
				MappedOrder mappedOrder = bucket.Orders.FirstOrDefault(x => x.LocalID == shipmentData.OrderLocalID);
				OrdersShipmentData data = orderShipmentRestDataProvider.Create(shipmentData, shipmentData.OrderId.ToString());

				obj.With(_ => { _.ExternID = null; return _; }).AddExtern(obj.Extern, new object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate());
				obj.AddDetail(shipmentData.ShipmentType, shipmentData.OrderLocalID, new object[] { data.OrderId, data.Id }.KeyCombine());

				//Concat all externID together and then show in the externId field to user
				key.Append(key.Length > 0 ? "|" + obj.ExternID : obj.ExternID);

				OrderStatus orderStatus = new OrderStatus();
				if (obj.Local.ShipmentType.Value == SOShipmentType.Invoice)
					orderStatus.StatusId = (int)OrderStatuses.Completed;
				else
					orderStatus.StatusId = BCSalesOrderProcessor.ConvertStatus(mappedOrder?.Local.Status?.Value).GetHashCode();
				orderStatus = orderDataProvider.Update(orderStatus, shipmentData.OrderId.ToString());

				mappedOrder.AddExtern(null, orderStatus.Id?.ToString(), orderStatus.DateModifiedUT.ToDate());
				UpdateStatus(mappedOrder, null);
			}

			obj.ExternID = key.ToString().TrimExternID();
			UpdateStatus(obj, operation);

			#region Reset externalShipmentUpdated flag
			List<PXDataFieldParam> fieldParams = new List<PXDataFieldParam>();
			fieldParams.Add(new PXDataFieldAssign(typeof(BCSOShipmentExt.externalShipmentUpdated).Name, PXDbType.Bit, true));
			fieldParams.Add(new PXDataFieldRestrict(typeof(PX.Objects.SO.SOShipment.noteID).Name, PXDbType.UniqueIdentifier, obj.LocalID));
			PXDatabase.Update<PX.Objects.SO.SOShipment>(fieldParams.ToArray());
			#endregion
		}

		#region ShipmentGetSection
		protected virtual void GetOrderShipment(BCShipments bCShipments)
		{
			if (bCShipments.ShipmentType.Value == SOShipmentType.DropShip)
				GetDropShipmentByShipmentNbr(bCShipments);
			else if (bCShipments.ShipmentType.Value == SOShipmentType.Invoice)
				GetInvoiceByShipmentNbr(bCShipments);
			else
				bCShipments.Shipment = cbapi.GetByID<Shipment>(bCShipments.ShippingNoteID.Value,
					new Shipment() {
						ReturnBehavior = ReturnBehavior.OnlySpecified,
						Details = new List<ShipmentDetail>() { new ShipmentDetail()},
						Packages = new List<ShipmentPackage>() { new ShipmentPackage()},
						Orders = new List<ShipmentOrderDetail>() { new ShipmentOrderDetail()},
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
		protected virtual EntityStatus GetDropShipment(BCShipmentEntityBucket bucket, BCShipments bCShipments)
		{
			if (bCShipments.ShipmentNumber == null) return EntityStatus.None;
			GetDropShipmentByShipmentNbr(bCShipments);
			if (bCShipments.POReceipt == null) return EntityStatus.None;

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
		protected virtual EntityStatus GetShipment(BCShipmentEntityBucket bucket, BCShipments bCShipment)
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
			if (bCShipment.Shipment == null) return EntityStatus.None;

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
		protected virtual EntityStatus GetInvoice(BCShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShipmentNumber == null) return EntityStatus.None;


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

        protected virtual void MapDropShipment(BCShipmentEntityBucket bucket, MappedShipment obj, PurchaseReceipt impl)
		{
			ShipmentData shipment = obj.Extern = new ShipmentData();
			
			foreach (MappedOrder order in bucket.Orders)
			{
				OrdersShipmentData shipmentData = new OrdersShipmentData();
				shipmentData.ShippingProvider = string.Empty;
				shipmentData.TrackingNumber = impl.VendorRef?.Value;
				shipmentData.ShipmentType = BCEntitiesAttribute.ShipmentLine;
				shipmentData.ShippingMethod = GetShippingMethod(impl.Details.FirstOrDefault(x => !string.IsNullOrEmpty(x.ShipVia?.Value))?.ShipVia?.Value);

				shipmentData.OrderId = order.ExternID?.ToInt();
				shipmentData.OrderLocalID = order.LocalID;

				foreach (PurchaseReceiptDetail line in impl.Details ?? new List<PurchaseReceiptDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.SOOrderType.Value && order.Local.OrderNbr.Value == line.SOOrderNbr.Value && d.LineNbr.Value == line.SOLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => (d.EntityType == BCEntitiesAttribute.OrderLine || d.EntityType == BCEntitiesAttribute.GiftWrapOrderLine) && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo?.EntityType == BCEntitiesAttribute.GiftWrapOrderLine) continue;// skip Gift wrap line
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) continue;// if order line not present in external system then just skip 


					OrdersShipmentItem shipItem = new OrdersShipmentItem();
					shipItem.OrderProductId = lineInfo.ExternID.ToInt();
					shipItem.Quantity = (int)line.ReceiptQty.Value;
					shipItem.OrderID = order.ExternID;

					shipmentData.ShipmentItems.Add(shipItem);
				}
				//Add to Shipment only if ShipmentItems have value
				if(shipmentData.ShipmentItems.Any())
					shipment.OrdersShipmentDataList.Add(shipmentData);
			}
		}

		protected virtual void MapInvoice(BCShipmentEntityBucket bucket, MappedShipment obj, Shipment impl)
		{
			ShipmentData shipment = obj.Extern = new ShipmentData();

			foreach (MappedOrder order in bucket.Orders)
			{
				OrdersShipmentData shipmentData = new OrdersShipmentData();
				shipmentData.ShippingProvider = string.Empty;
				shipmentData.TrackingNumber = string.Empty;
				shipmentData.ShipmentType = BCEntitiesAttribute.ShipmentLine;
				shipmentData.ShippingMethod = GetShippingMethod(impl.ShipVia?.Value);

				shipmentData.OrderId = order.ExternID?.ToInt();
				shipmentData.OrderLocalID = order.LocalID;

				foreach (ShipmentDetail line in impl.Details ?? new List<ShipmentDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) continue;

					OrdersShipmentItem shipItem = new OrdersShipmentItem();
					shipItem.OrderProductId = lineInfo.ExternID.ToInt();
					shipItem.Quantity = (int)line.ShippedQty.Value;
					shipItem.OrderID = order.ExternID;

					shipmentData.ShipmentItems.Add(shipItem);
				}

				shipment.OrdersShipmentDataList.Add(shipmentData);
			}
		}

		protected virtual void MapShipment(BCShipmentEntityBucket bucket, MappedShipment obj, Shipment impl)
		{
			var bindingExt = GetBindingExt<BCBindingBigCommerce>();
			ShipmentData shipment = obj.Extern = new ShipmentData();

			var shipvia = GetShippingMethod(impl.ShipVia?.Value);

			//Get Package Details, there is only InventoryID in SOShipLineSplitPackage, in case to compare InventoryCD field with Shipping line item, get InventoryCD from InventoryItem and save it in a Tuple.
			List<Tuple<SOShipLineSplitPackage, string>> PackageDetails = new List<Tuple<SOShipLineSplitPackage, string>>();
			foreach(PXResult< SOShipLineSplitPackage, InventoryItem> item in PXSelectJoin<SOShipLineSplitPackage,
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
				//get all line items for the current order in this Shipment
				Dictionary<ShipmentDetail, DetailInfo> ShippingLineDetails = new Dictionary<ShipmentDetail, DetailInfo>();
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

					ShippingLineDetails[line] = lineInfo;
				}

				if (!ShippingLineDetails.Any()) continue;

				//Lookup by packages first
				foreach (var onePackage in packages)
				{
					//Get the line items in the package
					var shippingLinesInPackage = ShippingLineDetails.Keys.Where(x => onePackage.ShipmentLineNbr.Any() && onePackage.ShipmentLineNbr.Select(y => y.Item1).Contains(x.LineNbr?.Value));
					//if no lines in the package and package is not emptybox, that means the package is not for this order, we should skip it.
					if (onePackage.ShipmentLineNbr.Any() && !shippingLinesInPackage.Any()) continue;

					OrdersShipmentData shipmentDataByPackage = new OrdersShipmentData();
					shipmentDataByPackage.ShippingProvider = string.Empty;
					shipmentDataByPackage.ShippingMethod = shipvia;
					shipmentDataByPackage.TrackingNumber = onePackage.TrackingNbr?.Value;
					shipmentDataByPackage.ShipmentType = BCEntitiesAttribute.ShipmentBoxLine;
					shipmentDataByPackage.OrderId = order.ExternID?.ToInt();
					shipmentDataByPackage.OrderLocalID = order.LocalID;

					foreach (ShipmentDetail line in shippingLinesInPackage)
					{
						//Get the SyncDetail for current line
						DetailInfo lineInfo = ShippingLineDetails[line];

						//Non-stock kits
						if(PackageDetails.Any(x => x.Item1.PackageLineNbr == onePackage.LineNbr.Value && x.Item1.ShipmentLineNbr == line.LineNbr.Value && x.Item2 != line.InventoryID.Value.Trim()) == true)
						{
							//Skip shipping line if its ShippingQty is 0, because the non-stock kit has shipped in other package
							if (line.ShippedQty.Value == 0) continue;

							OrdersShipmentItem shipItem = new OrdersShipmentItem();
							shipItem.OrderProductId = lineInfo.ExternID.ToInt();
							shipItem.OrderID = order.ExternID;
							shipItem.Quantity = (int)(line.ShippedQty.Value ?? 0); //Use ShippedQty of line itme instead of packedQty of non-stock kits
							shipmentDataByPackage.ShipmentItems.Add(shipItem);

							line.ShippedQty = 0m.ValueField();//Reduce the ShippedQty if it has used.
						}
						else //normal items
						{
							int shippingQty = (int)(onePackage?.ShipmentLineNbr.Where(x => x.Item1 == line.LineNbr?.Value)?.Sum(x => x.Item2) ?? 0); //Qty should use the actual packedQty
							if (shippingQty == 0) continue; //Skip shipping line if its ShippingQty is 0

							OrdersShipmentItem shipItem = new OrdersShipmentItem();
							shipItem.OrderProductId = lineInfo.ExternID.ToInt();
							shipItem.OrderID = order.ExternID;
							shipItem.Quantity = shippingQty; //Qty should use the actual packedQty
							shipmentDataByPackage.ShipmentItems.Add(shipItem);

							line.ShippedQty = (line.ShippedQty.Value - shippingQty).ValueField();//Reduce the ShippedQty if it has used.
						}
					}

					shipment.OrdersShipmentDataList.Add(shipmentDataByPackage);
				}

				//if shipping lines still have ShippedQty, that means there is no package for them. Put them in emptybox or virtual package without tracking number
				var restShippingLines = ShippingLineDetails.Keys.Where(x => x.ShippedQty?.Value > 0);
				if (restShippingLines.Any())
				{
					var trackingNumber = impl.Packages?.FirstOrDefault(p => p.ShipmentLineNbr.Any() == false && !string.IsNullOrEmpty(p.TrackingNbr?.Value))?.TrackingNbr.Value;//If no emptybox, tracking number should be empty
					OrdersShipmentData shipmentDataForRestLines;
					//If the shipment for emptybox has been created, put all rest items to this shipment, otherwise create a new one
					if (!string.IsNullOrEmpty(trackingNumber) && shipment.OrdersShipmentDataList.Any(x => x.TrackingNumber == trackingNumber && x.OrderLocalID == order.LocalID))
					{
						shipmentDataForRestLines = shipment.OrdersShipmentDataList.First(x => x.TrackingNumber == trackingNumber && x.OrderLocalID == order.LocalID);
					}
					else
					{
						shipmentDataForRestLines = new OrdersShipmentData();
						shipmentDataForRestLines.ShippingProvider = string.Empty;
						shipmentDataForRestLines.TrackingNumber = trackingNumber;
						shipmentDataForRestLines.ShippingMethod = shipvia;
						shipmentDataForRestLines.ShipmentType = BCEntitiesAttribute.ShipmentLine;
						shipmentDataForRestLines.OrderId = order.ExternID?.ToInt();
						shipmentDataForRestLines.OrderLocalID = order.LocalID;
						shipment.OrdersShipmentDataList.Add(shipmentDataForRestLines);
					}

					foreach(ShipmentDetail line in restShippingLines)
					{
						//Get the SyncDetail for current line
						DetailInfo lineInfo = ShippingLineDetails[line];

						OrdersShipmentItem shipItem = new OrdersShipmentItem();
						shipItem.OrderProductId = lineInfo.ExternID.ToInt();
						shipItem.OrderID = order.ExternID;
						shipItem.Quantity = (int)(line.ShippedQty.Value ?? 0); //Use rest ShippedQty of line item
						shipmentDataForRestLines.ShipmentItems.Add(shipItem);
					}
				}
			}
		}

		protected DetailInfo MatchOrderLineFromExtern(string externalOrderId, string identifyKey)
		{
			DetailInfo lineInfo = null;
			if (string.IsNullOrEmpty(externalOrderId) || string.IsNullOrEmpty(identifyKey))
				return lineInfo;
			var orderLineDetails = orderProductsRestDataProvider.GetAll(externalOrderId).ToList();
			var matchedLine = orderLineDetails?.FirstOrDefault(x => string.Equals(x?.Sku, identifyKey, StringComparison.OrdinalIgnoreCase));
			if (matchedLine != null && matchedLine?.Id.HasValue == true)
			{
				lineInfo = new DetailInfo(BCEntitiesAttribute.OrderLine, null, matchedLine.Id.ToString());
			}
			return lineInfo;
		}

		protected string GetShippingMethod(String shipVia)
		{
			var retShipVia = shipVia ?? string.Empty;
			if (!string.IsNullOrEmpty(retShipVia))
			{
				var shippingmethods = shippingMappings.Where(x => string.Equals(x.CarrierID,retShipVia, StringComparison.OrdinalIgnoreCase))?.ToList();
				if (shippingmethods?.Count == 1)
					retShipVia = shippingmethods.FirstOrDefault().ShippingMethod;
			}

			return retShipVia;
		}
		#endregion

        #endregion
    }
}
