using System;
using System.Collections.Generic;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Commerce.Shopify;
using PX.Commerce.Shopify.API.REST;
using PX.Data;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public class ShopifyAuthenticateTestProcessor : ShopifyProcessor
	{
		public ShopifyAuthenticateTestProcessor(IEnumerable<SettingsValue> settingValues) : base(settingValues)
		{
		}

		public void TestCredentials()
		{
			try
			{
				StoreRestDataProvider restClient = new StoreRestDataProvider(ShopifyRestClient);
				StoreData store = null;

				try
				{
					store = restClient.Get();
				}
				catch (NullReferenceException)
                {
					// Do nothing
                }

				if (store == null || store.Id == null)
					throw new PXException(ShopifyMessages.TestConnectionStoreNotFound);
			}
			catch (Exception ex)
			{
				ErrorHandler(ex);
				throw;
			}
		}
	}
}
