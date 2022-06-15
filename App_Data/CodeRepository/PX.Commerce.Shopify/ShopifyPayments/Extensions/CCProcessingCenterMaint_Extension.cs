using PX.Commerce.Core;
using PX.Data;
using PX.Objects.CA;
using System;
using System.Collections.Generic;
using System.Linq;
using static PX.Commerce.Shopify.ShopifyPayments.ShopifyPluginHelper;

namespace PX.Commerce.Shopify.ShopifyPayments.Extensions
{
	public class CCProcessingCenterMaint_Extension : PXGraphExtension<CCProcessingCenterMaint>
	{
		protected PXSetPropertyException _ProcessingTypeNameException = null;

		#region CacheAttached
		#region CCProcessingCenter.isExternalAuthorizationOnly
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<CCProcessingCenter.processingTypeName>))]
		protected void _(Events.CacheAttached<CCProcessingCenter.isExternalAuthorizationOnly> e) { }
		#endregion
		#region CCProcessingCenter.allowUnlinkedRefund
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<CCProcessingCenter.processingTypeName>))]
		protected void _(Events.CacheAttached<CCProcessingCenter.allowUnlinkedRefund> e) { }
		#endregion
		#endregion

		#region Event Handlers
		public virtual void _(Events.FieldDefaulting<CCProcessingCenter, CCProcessingCenter.isExternalAuthorizationOnly> e)
		{
			e.NewValue = ExtensionHelper.IsShopifyPaymentsPlugin(e.Row);
			e.Cancel = true;
		}

		public virtual void _(Events.FieldDefaulting<CCProcessingCenter, CCProcessingCenter.allowUnlinkedRefund> e)
		{
			if (ExtensionHelper.IsShopifyPaymentsPlugin(e.Row) == false)
			{
				return;
			}

			e.NewValue = false;
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldVerifying<CCProcessingCenter, CCProcessingCenter.processingTypeName> e)
		{
			if (ExtensionHelper.IsShopifyPaymentsPlugin((string)e.NewValue) == false)
			{
				return;
			}

			if (CommerceFeaturesHelper.ShopifyConnector == false)
			{
				_ProcessingTypeNameException = new PXSetPropertyException(ShopifyPluginMessages.TheXPluginRequiresTheXFeatureEnabled,
													ShopifyPluginMessages.APIPluginDisplayName,
													ShopifyPluginMessages.ShopifyConnectorFeatureDisplayName);
				throw _ProcessingTypeNameException;
			}
		}

		protected virtual void _(Events.FieldUpdated<CCProcessingCenter, CCProcessingCenter.processingTypeName> e, PXFieldUpdated baseHandler)
		{
			baseHandler?.Invoke(e.Cache, e.Args);

			if (e.Row == null || ExtensionHelper.IsShopifyPaymentsPlugin(e.Row) == false)
			{
				return;
			}

			if (e.Row.UseAcceptPaymentForm == true)
			{
				e.Cache.RaiseExceptionHandling<CCProcessingCenter.useAcceptPaymentForm>(e.Row, null, null);
				e.Cache.SetValueExt<CCProcessingCenter.useAcceptPaymentForm>(e.Row, false);
			}
		}

		protected virtual void _(Events.RowSelected<CCProcessingCenter> e, PXRowSelected baseHandler)
		{
			if (baseHandler == null)
			{
				return;
			}

			baseHandler(e.Cache, e.Args);

			if (e.Row == null)
			{
				return;
			}

			if (_ProcessingTypeNameException != null)
			{
				e.Cache.RaiseExceptionHandling<CCProcessingCenter.processingTypeName>(e.Row, e.Row.ProcessingTypeName,
					_ProcessingTypeNameException);
			}

			bool isShopifyPaymentsPlugin = ExtensionHelper.IsShopifyPaymentsPlugin(e.Row);
			PXUIFieldAttribute.SetEnabled<CCProcessingCenter.allowUnlinkedRefund>(e.Cache, e.Row, isShopifyPaymentsPlugin == false);
			PXUIFieldAttribute.SetEnabled<CCProcessingCenter.useAcceptPaymentForm>(e.Cache, e.Row, isShopifyPaymentsPlugin == false);

			if (isShopifyPaymentsPlugin == true)
			{
				e.Cache.RaiseExceptionHandling<CCProcessingCenter.allowUnlinkedRefund>(e.Row, e.Row.AllowUnlinkedRefund, null);
			}
		}

		protected virtual void _(Events.RowPersisting<CCProcessingCenter> e)
		{
			if (ExtensionHelper.IsShopifyPaymentsPlugin(e.Row) == false)
			{
				return;
			}

			if (e.Row.UseAcceptPaymentForm == true)
			{
				e.Cache.RaiseExceptionHandling<CCProcessingCenter.useAcceptPaymentForm>(e.Row, null,
					new PXSetPropertyException(ShopifyPluginMessages.DoNotUseAcceptPaymentFormWarning, PXErrorLevel.Warning));
				e.Row.UseAcceptPaymentForm = false;
			}
		}
		
		public virtual void _(Events.RowPersisting<CCProcessingCenterDetail> e)
		{
			if (e.Row.DetailID != ShopifyPluginHelper.SettingsKeys.Key_StoreName
				|| Base.ProcessingCenter.Current == null
				|| ExtensionHelper.IsShopifyPaymentsPlugin(Base.ProcessingCenter.Current) == false)
			{
				return;
			}

			string store = e.Row.Value;
			if (string.IsNullOrEmpty(store))
			{
				e.Cache.RaiseExceptionHandling<CCProcessingCenterDetail.value>(e.Row, e.Row.Value,
					new PXSetPropertyException(ShopifyPluginMessages.StoreName_CannotBeEmpty, PXErrorLevel.Error));
			}
			else
			{
				BCBindingShopify bindingShopify = PXSelectJoin<BCBindingShopify,
					InnerJoin<BCBinding, On<BCBinding.bindingID, Equal<BCBindingShopify.bindingID>>>,
					Where<BCBinding.bindingID, Equal<Required<BCBinding.bindingID>>>>.Select(Base, store);
				if(bindingShopify == null)
				{
					e.Cache.RaiseExceptionHandling<CCProcessingCenterDetail.value>(e.Row, e.Row.Value,
						new PXSetPropertyException(ShopifyPluginMessages.StoreName_CannotBeFound, PXErrorLevel.Error));
				}
			}
		}

		public virtual void _(Events.FieldSelecting<CCProcessingCenterDetail, CCProcessingCenterDetail.value> e)
		{
			if (e.Row?.DetailID != ShopifyPluginHelper.SettingsKeys.Key_StoreName
				|| Base.ProcessingCenter.Current == null
				|| ExtensionHelper.IsShopifyPaymentsPlugin(Base.ProcessingCenter.Current) == false)
			{
				return;
			}

			Dictionary<string,string> dict = PX.Commerce.Core.ConnectorHelper.GetConnectorBindings(SPConnector.TYPE).ToDictionary(s => s.BindingID.ToString(), s => s.BindingName);

			e.ReturnState = PXStringState.CreateInstance(e.ReturnState, null, null, null, null, null, null,
				dict.Keys.ToArray(), dict.Values.ToArray(), null, null);
		}
		#endregion
	}
}
