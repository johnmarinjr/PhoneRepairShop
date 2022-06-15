using PX.CCProcessingBase.Attributes;
using PX.CCProcessingBase.Interfaces.V2;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.ShopifyPayments
{
    [PXDisplayTypeName(ShopifyPluginMessages.APIPluginDisplayName)]
    public class ShopifyPaymentsProcessingPlugin : ICCProcessingPlugin
    {
		public class Const_PluginName : PX.Data.BQL.BqlString.Constant<Const_PluginName>
		{
			public Const_PluginName() : base(typeof(ShopifyPaymentsProcessingPlugin).FullName) { }
		}

		public IEnumerable<SettingsDetail> ExportSettings()
        {
            return ShopifyPluginHelper.GetDefaultSettings();
        }

		public string ValidateSettings(SettingsValue setting)
		{
			return ShopifyValidator.Validate(setting);
		}

		public void TestCredentials(IEnumerable<SettingsValue> settingValues)
        {
			var authenticateTestProcessor = new ShopifyAuthenticateTestProcessor(settingValues);
			authenticateTestProcessor.TestCredentials();
		}

		public T CreateProcessor<T>(IEnumerable<SettingsValue> settingValues)
			where T : class
		{
			if (typeof(T) == typeof(ICCProfileProcessor))
			{
				return null;
				//return new AuthnetProfileProcessor(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCHostedFormProcessor))
			{
				return null;
				//return new AuthnetHostedFormProcessor(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCHostedPaymentFormProcessor))
			{
				return new ShopifyHostedPaymentFormProcessor(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCTransactionProcessor))
			{
				return new ShopifyTransactionProcessor(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCTransactionGetter))
			{
				return new ShopifyTransactionGetter(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCProfileCreator))
			{
				return new ShopifyProfileCreator(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCWebhookResolver))
			{
				return null;
				//return new AuthnetWebhookResolver() as T;
			}
			if (typeof(T) == typeof(ICCWebhookProcessor))
			{
				return null;
				//return new AuthnetWebhookProcessor(settingValues) as T;
			}
			if (typeof(T) == typeof(ICCTranStatusGetter))
			{
				return null;
				//return new AuthnetTranStatusGetter() as T;
			}
			if (typeof(T) == typeof(ICCHostedPaymentFormResponseParser))
			{
				return new ShopifyHostedPaymentFormResponseParser() as T;
			}
			return null;
		}
	}
}
