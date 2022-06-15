using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.Commerce.Objects
{
	public class BCSOShipmentEntryExt : PXGraphExtension<SOShipmentEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void SOShipment_LastModifiedDateTime_CacheAttached(PXCache sender) { }

		public delegate IEnumerable ConfirmShipmentActionDelegate(PXAdapter adapter);

		[PXOverride]
		public virtual IEnumerable ConfirmShipmentAction(PXAdapter adapter, ConfirmShipmentActionDelegate handler)
		{
			if (Base.CurrentDocument.Current != null)
			{
				Base.CurrentDocument.SetValueExt<BCSOShipmentExt.externalShipmentUpdated>(Base.CurrentDocument.Current, false);
				Base.CurrentDocument.Update(Base.CurrentDocument.Current);
			}

			handler(adapter);

			return adapter.Get();
		}

		protected virtual void _(PX.Data.Events.RowInserted<SOShipLine> e)
		{
			SOShipLine row = e.Row;
			if (row == null)
				return;
			var soLine = SOLine.PK.Find(Base, row.OrigOrderType, row.OrigOrderNbr, row.OrigLineNbr);
			row.GetExtension<BCSOShipLineExt>().AssociatedOrderLineNbr = soLine?.GetExtension<BCSOLineExt>()?.AssociatedOrderLineNbr;
			row.GetExtension<BCSOShipLineExt>().GiftMessage = soLine?.GetExtension<BCSOLineExt>()?.GiftMessage;
		}

		protected virtual void _(PX.Data.Events.RowPersisting<SOShipment> e)
		{
			SOShipment row = e.Row;
			if (row == null)
				return;

			if (!Base.Transactions.Cache.IsInsertedUpdatedDeleted || e.Operation == PXDBOperation.Delete) return;
			List<PXResult<SOShipLine, SOLine>> giftwrapLines = PXSelectJoin<SOShipLine, InnerJoin<SOLine,
									   On<SOShipLine.origOrderType, Equal<SOLine.orderType>, And<SOShipLine.origOrderNbr, Equal<SOLine.orderNbr>,
									   And<SOShipLine.origLineNbr, Equal<BCSOLineExt.associatedOrderLineNbr>>>>>,
									   Where<SOShipLine.shipmentType, Equal<Required<SOShipLine.shipmentType>>,
									   And<SOShipLine.shipmentNbr, Equal<Required<SOShipLine.shipmentNbr>>>>>
									  .Select(Base, row.ShipmentType, row.ShipmentNbr)
									  .Cast<PXResult<SOShipLine, SOLine>>().ToList();
			if (giftwrapLines?.Count > 0)
			{
				var shipLines = Base.Transactions.Select().RowCast<SOShipLine>().ToList();
				foreach (PXResult<SOShipLine, SOLine> giftwrapLine in giftwrapLines)
				{
					var shipLine = giftwrapLine.GetItem<SOShipLine>();
					var giftWrapSOLine = giftwrapLine.GetItem<SOLine>();
					if (giftWrapSOLine != null && !shipLines.Any(x => x.OrigOrderType == giftWrapSOLine.OrderType && x.OrigOrderNbr == giftWrapSOLine.OrderNbr && x.OrigLineNbr == giftWrapSOLine.LineNbr && x.ShippedQty == shipLine.ShippedQty))
					{
						var inventory = InventoryItem.PK.Find(Base, giftWrapSOLine.InventoryID);
						Base.Transactions.Cache.RaiseExceptionHandling<SOShipLine.lineNbr>(shipLine, shipLine.OrigLineNbr, new PXSetPropertyException(BCMessages.GiftWrapLineMissing, PXErrorLevel.RowWarning, inventory?.InventoryCD, giftWrapSOLine.LineNbr, shipLine.OrigOrderNbr));
					}
					else
					{
						Base.Transactions.Cache.RaiseExceptionHandling<SOShipLine.lineNbr>(shipLine, shipLine.OrigLineNbr, null);

					}
				}
			}
		}
	}
}
