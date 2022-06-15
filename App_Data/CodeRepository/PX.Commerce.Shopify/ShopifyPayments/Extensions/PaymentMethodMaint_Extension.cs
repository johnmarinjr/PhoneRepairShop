using PX.Data;
using PX.Objects.CA;

namespace PX.Commerce.Shopify.ShopifyPayments.Extensions
{
    public class PaymentMethodMaint_Extension : PXGraphExtension<PaymentMethodMaint>
    {
        #region Event Handlers
        public virtual void _(Events.FieldDefaulting<CCProcessingCenterPmntMethod, CCProcessingCenterPmntMethod.fundHoldPeriod> e)
        {
            if (e.Row == null || e.Row.ProcessingCenterID == null)
            {
                return;
            }

            CCProcessingCenter ccProcessingCenter = (CCProcessingCenter)PXSelectorAttribute.Select<CCProcessingCenterPmntMethod.processingCenterID>(e.Cache, e.Row);

            if (ExtensionHelper.IsShopifyPaymentsPlugin(ccProcessingCenter))
            {
                e.NewValue = ShopifyPluginHelper.AuthorizationValidPeriodDays;
                e.Cancel = true;
            }
        }

        public virtual void _(Events.FieldUpdated<CCProcessingCenterPmntMethod, CCProcessingCenterPmntMethod.processingCenterID> e)
        {
            if (e.Row == null || e.Row.ProcessingCenterID == null)
            {
                return;
            }

            CCProcessingCenter ccProcessingCenter = (CCProcessingCenter)PXSelectorAttribute.Select<CCProcessingCenterPmntMethod.processingCenterID>(e.Cache, e.Row);

            if (ExtensionHelper.IsShopifyPaymentsPlugin(ccProcessingCenter))
            {
                e.Cache.SetDefaultExt<CCProcessingCenterPmntMethod.fundHoldPeriod>(e.Row);
                e.Cache.SetValuePending<CCProcessingCenterPmntMethod.fundHoldPeriod>(e.Row, PXCache.NotSetValue);
            }
        }

        public virtual void _(Events.RowUpdated<CCProcessingCenterPmntMethod> e)
        {
            if (e.Row == null || e.Row.ProcessingCenterID == null)
            {
                return;
            }

            CCProcessingCenter ccProcessingCenter = (CCProcessingCenter)PXSelectorAttribute.Select<CCProcessingCenterPmntMethod.processingCenterID>(e.Cache, e.Row);

            if (ExtensionHelper.IsShopifyPaymentsPlugin(ccProcessingCenter))
            {
                e.Cache.SetDefaultExt<CCProcessingCenterPmntMethod.fundHoldPeriod>(e.Row);
            }
        }

        public virtual void _(Events.RowSelected<CCProcessingCenterPmntMethod> e)
        {
            if (e.Row == null)
            {
                return;
            }

            bool enableFundHoldPeriod = true;

            if (e.Row.ProcessingCenterID != null)
            {
                CCProcessingCenter ccProcessingCenter = (CCProcessingCenter)PXSelectorAttribute.Select<CCProcessingCenterPmntMethod.processingCenterID>(e.Cache, e.Row);

                enableFundHoldPeriod = ExtensionHelper.IsShopifyPaymentsPlugin(ccProcessingCenter) == false;
            }

            PXUIFieldAttribute.SetEnabled<CCProcessingCenterPmntMethod.fundHoldPeriod>(e.Cache, e.Row, enableFundHoldPeriod);
        }
        #endregion
    }
}
