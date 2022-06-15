using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO.Attributes
{
	public class SOShipmentStatusVerifierAttribute : PXEventSubscriberAttribute, IPXRowPersistingSubscriber
	{
		[PXLocalizable]
		public static class AttributeMessages
		{
			public const string InvalidShipmentStatus = "The {0} for the {1} {2} cannot be updated. Please contact support service.";
			public const string EntityFieldValues = "The {0} {1} has the following field values: {2}.";
		}

		public virtual void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				var shipment = (SOShipment)e.Row;

				if (!VerifyStatus(sender, shipment))
				{
					LogError(sender, shipment);
					ThrowInvalidStatusException(sender, e, shipment);
				}
			}
		}

		protected virtual bool VerifyStatus(PXCache cache, SOShipment shipment)
		{
			switch(shipment.Status)
			{
				case SOShipmentStatus.Hold:
					return VerifyHold(cache, shipment);

				case SOShipmentStatus.Open:
					return VerifyOpen(cache, shipment);

				case SOShipmentStatus.Confirmed:
					return VerifyConfirmed(cache, shipment);

				case SOShipmentStatus.PartiallyInvoiced:
					return VerifyPartiallyInvoiced(cache, shipment);

				case SOShipmentStatus.Invoiced:
					return VerifyInvoiced(cache, shipment);

				case SOShipmentStatus.Completed:
					return VerifyCompleted(cache, shipment);

				default:
					return VerifyUnknown(cache, shipment);
			}
		}

		protected virtual bool VerifyHold(PXCache cache, SOShipment shipment)
		{
			return shipment.Hold == true &&
				shipment.Confirmed != true &&
				shipment.BilledOrderCntr == 0 &&
				shipment.ReleasedOrderCntr == 0 &&
				shipment.Released != true;
		}

		protected virtual bool VerifyOpen(PXCache cache, SOShipment shipment)
		{
			return shipment.Hold != true &&
				shipment.Confirmed != true &&
				shipment.BilledOrderCntr == 0 &&
				shipment.ReleasedOrderCntr == 0 &&
				shipment.Released != true;
		}

		protected virtual bool VerifyConfirmed(PXCache cache, SOShipment shipment)
		{
			return shipment.Hold != true &&
				shipment.Confirmed == true &&
				shipment.BilledOrderCntr == 0 &&
				shipment.ReleasedOrderCntr == 0 &&
				(shipment.UnbilledOrderCntr > 0 ||
					(shipment.UnbilledOrderCntr == 0 && shipment.Released != true));
		}

		protected virtual bool VerifyPartiallyInvoiced(PXCache cache, SOShipment shipment)
		{
			return shipment.Hold != true &&
				shipment.Confirmed == true &&
				shipment.UnbilledOrderCntr > 0 &&
				(shipment.BilledOrderCntr > 0 || shipment.ReleasedOrderCntr > 0);
		}

		protected virtual bool VerifyInvoiced(PXCache cache, SOShipment shipment)
		{
			return shipment.Hold != true &&
				shipment.Confirmed == true &&
				shipment.UnbilledOrderCntr == 0 &&
				shipment.BilledOrderCntr > 0;
		}

		protected virtual bool VerifyCompleted(PXCache cache, SOShipment shipment)
		{
			bool flagsAndBilledUnbilled =
				shipment.Hold != true &&
				shipment.Confirmed == true &&
				shipment.UnbilledOrderCntr == 0 &&
				shipment.BilledOrderCntr == 0;

			if (!flagsAndBilledUnbilled)
				return false;

			if (shipment.ReleasedOrderCntr > 0 || shipment.Released == true)
				return true;

			return IsNonStockTransfer(cache, shipment);
		}

		protected virtual bool IsNonStockTransfer(PXCache cache, SOShipment shipment)
		{
			var soorderShipmentCache = cache.Graph.Caches[typeof(SOOrderShipment)];

			return PXParentAttribute.SelectChildren(soorderShipmentCache, shipment, typeof(SOShipment))
				.Cast<SOOrderShipment>().All(s => s.CreateINDoc != true);
		}

		protected virtual bool VerifyUnknown(PXCache cache, SOShipment shipment)
		{
			return true;
		}

		protected virtual void LogError(PXCache sender, SOShipment shipment)
		{
			string fieldValues = GetFieldValues(sender, shipment, GetLogFieldList());
			PXTrace.WriteError(AttributeMessages.EntityFieldValues, shipment.ShipmentNbr, sender.DisplayName, fieldValues);
		}

		protected virtual List<Type> GetLogFieldList()
		{
			return new List<Type>()
			{
				typeof(SOShipment.status),
				typeof(SOShipment.hold),
				typeof(SOShipment.confirmed),
				typeof(SOShipment.billedOrderCntr),
				typeof(SOShipment.releasedOrderCntr),
				typeof(SOShipment.unbilledOrderCntr),
				typeof(SOShipment.released)
			};
		}

		protected virtual string GetFieldValues(PXCache cache, SOShipment shipment, IEnumerable<Type> field)
		{
			var fieldValues = field.Select(f => $"{f.Name}={cache.GetValue(shipment, f.Name)}");
			return String.Join(", ", fieldValues);
		}

		protected virtual void ThrowInvalidStatusException(PXCache sender, PXRowPersistingEventArgs e, SOShipment shipment)
		{
			PXFieldState state = sender.GetStateExt(e.Row, FieldName) as PXFieldState;
			string fieldDescription = state?.DisplayName ?? FieldName;

			throw new PXException(AttributeMessages.InvalidShipmentStatus, fieldDescription, shipment.ShipmentNbr, sender.DisplayName);
		}
	}
}