using PX.CCProcessingBase.Interfaces.V2;
using PX.Data;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public class ShopifyHostedPaymentFormResponseParser : ICCHostedPaymentFormResponseParser
	{
		public HostedFormResponse Parse(string input)
		{
			throw new PXException(ShopifyPluginMessages.TheMethodXIsNotImplementedInTheX, nameof(ICCHostedPaymentFormResponseParser) + "." + nameof(Parse), ShopifyPluginMessages.APIPluginDisplayName);
		}
	}
}
