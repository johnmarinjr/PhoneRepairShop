using System.Collections.Generic;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Data;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	class ShopifyProfileCreator : ShopifyProcessor, ICCProfileCreator
	{
		public ShopifyProfileCreator(IEnumerable<SettingsValue> settings) : base(settings)
		{
		}

        public TranProfile GetOrCreatePaymentProfileFromTransaction(string transactionId, CreateTranPaymentProfileParams cParams)
        {
            throw new PXException(ShopifyPluginMessages.TheMethodXIsNotImplementedInTheX, nameof(ICCProfileCreator) + "." + nameof(GetOrCreatePaymentProfileFromTransaction), ShopifyPluginMessages.APIPluginDisplayName);
        }
    }
}
