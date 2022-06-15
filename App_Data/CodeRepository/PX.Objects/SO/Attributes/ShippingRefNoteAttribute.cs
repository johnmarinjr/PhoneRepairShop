using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.PO;
using System;

namespace PX.Objects.SO.Attributes
{
	public class ShippingRefNoteAttribute : PXGuidAttribute
	{
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			if (sender.Graph.PrimaryItemType == null)
				return;

			Type cacheType = sender.GetItemType();

			PXButtonDelegate delgate = delegate (PXAdapter adapter)
			{
				PXCache cache = adapter.View.Graph.Caches[cacheType];
				if (cache.Current != null)
				{
					var helper = new EntityHelper(cache.Graph);
					object val = cache.GetValueExt(cache.Current, _FieldName);
					var state = val as PXRefNoteBaseAttribute.PXLinkState;
					if (state != null)
					{
						helper.NavigateToRow(state.target.FullName, state.keys, PXRedirectHelper.WindowMode.NewWindow);
					}
					else
					{
						helper.NavigateToRow((Guid?)cache.GetValue(cache.Current, _FieldName), PXRedirectHelper.WindowMode.NewWindow);
					}
				}

				return adapter.Get();
			};

			string actionName = $"{ cacheType.Name }~{ _FieldName }~Link";
			PXNamedAction.AddHiddenAction(sender.Graph, sender.Graph.PrimaryItemType, actionName, delgate);
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			GetTargetTypeAndKeys(sender, e.Row, out Type targetType, out object[] targetKeys);

			if (targetType != null)
			{
				e.ReturnValue = PXRefNoteBaseAttribute.GetEntityRowID(sender.Graph.Caches[targetType], targetKeys, ", ");
			}
			e.ReturnState = PXRefNoteBaseAttribute.PXLinkState.CreateInstance(e.ReturnState, targetType, targetKeys);
		}

		public static void GetTargetTypeAndKeys(PXCache cache, object row, out Type targetType, out object[] targetKeys)
		{
			targetType = null;
			targetKeys = null;

			var shipmentType = (string)cache.GetValue<SOOrderShipment.shipmentType>(row);
			var shipmentNbr = (string)cache.GetValue<SOOrderShipment.shipmentNbr>(row);
			var operation = (string)cache.GetValue<SOOrderShipment.operation>(row);
			var invoiceType = (string)cache.GetValue<SOOrderShipment.invoiceType>(row);
			var invoiceNbr = (string)cache.GetValue<SOOrderShipment.invoiceNbr>(row);

			if (row != null && shipmentType.IsIn(INDocType.Issue, INDocType.Transfer) && !string.Equals(shipmentNbr, Constants.NoShipmentNbr))
			{
				targetType = typeof(SOShipment);
				targetKeys = new object[] { shipmentNbr };
			}
			else if (row != null && shipmentType == INDocType.DropShip && !string.IsNullOrEmpty(shipmentNbr))
			{
				targetType = typeof(POReceipt);
				string receiptType = (operation == SOOperation.Receipt) ? POReceiptType.POReturn : POReceiptType.POReceipt;
				targetKeys = new object[] { receiptType, shipmentNbr };
			}
			else if (row != null
				&& (shipmentType == INDocType.Issue && string.Equals(shipmentNbr, Constants.NoShipmentNbr) || shipmentType == INDocType.Invoice)
				&& !string.IsNullOrEmpty(invoiceType) && !string.IsNullOrEmpty(invoiceNbr))
			{
				targetType = typeof(ARInvoice);
				targetKeys = new object[] { invoiceType, invoiceNbr };
			}
		}

		public override void CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.External) == PXDBOperation.External)
				return;

			base.CommandPreparing(sender, e);
		}
	}
}
