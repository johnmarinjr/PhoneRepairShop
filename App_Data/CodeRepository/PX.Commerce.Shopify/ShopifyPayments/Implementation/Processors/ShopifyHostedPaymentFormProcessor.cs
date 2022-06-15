using System.Collections.Generic;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Data;
using ProcessingInput = PX.CCProcessingBase.Interfaces.V2.ProcessingInput;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public class ShopifyHostedPaymentFormProcessor : ShopifyProcessor, ICCHostedPaymentFormProcessor
	{
		public ShopifyHostedPaymentFormProcessor(IEnumerable<SettingsValue> settingValues) : base(settingValues)
		{
		}

        public HostedFormData GetDataForPaymentForm(ProcessingInput inputData)
        {
            throw new PXException(ShopifyPluginMessages.TheMethodXIsNotImplementedInTheX, nameof(ICCHostedPaymentFormProcessor) + "." + nameof(GetDataForPaymentForm), ShopifyPluginMessages.APIPluginDisplayName);
        }
    }
}
