using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.CS;
using PX.SM;

using OrdersToApplyTab = PX.Objects.SO.GraphExtensions.ARPaymentEntryExt.OrdersToApplyTab;

namespace PX.Commerce.Objects
{
	public class BCARPaymentEntryExt : PXGraphExtension<OrdersToApplyTab, ARPaymentEntry.MultiCurrency, ARPaymentEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void ARPayment_LastModifiedDateTime_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCConditionalRemoveBaseAttribute(typeof(PXRestrictorAttribute))]
		public void SOAdjust_AdjdOrderNbr_CacheAttached(PXCache sender) { }

		/// <summary>
		/// Overrides <see cref="OrdersToApplyTab._(Events.FieldVerifying{SOAdjust, SOAdjust.adjdOrderNbr})"/>
		/// </summary>
		protected virtual void _(Events.FieldVerifying<SOAdjust, SOAdjust.adjdOrderNbr> e, PXFieldVerifying baseHandler)
		{
			
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null && e.NewValue != null)
			{
				SOOrder order = PXSelect<SOOrder, Where<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>.Select(Base, e.NewValue.ToString());
				if (order != null && order.Status == SOOrderStatus.Cancelled)
					return;
			}

			baseHandler?.Invoke(e.Cache, e.Args);
		}
		protected virtual void SOOrder_Cancelled_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying baseHandler)
		{
			SOOrder order = (SOOrder)e.Row;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null && order != null)
			{
				return;
			}
			baseHandler.Invoke(sender, e);
		}

	}
}
