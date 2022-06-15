using PX.CCProcessingBase.Interfaces.V2;
using PX.Commerce.Shopify;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.Shopify.ShopifyPayments
{
    public static class ShopifyPluginHelper
    {
		// Shopify Payments provides an authorization period of 7 days.
		// Source:
		// https://help.shopify.com/en/manual/payments/payment-authorization
		public const int AuthorizationValidPeriodDays = 7;


		internal static class SettingsKeys
		{
			public const string Key_StoreName = "STORENAME";
			public const string Descr_StoreName = ShopifyPluginMessages.APIPluginParameter_StoreName;

			public class Const_StoreName : PX.Data.BQL.BqlString.Constant<Const_StoreName>
			{
				public Const_StoreName() : base(Key_StoreName) { }
			}
		}

		public static IEnumerable<SettingsDetail> GetDefaultSettings()
        {
			yield return new SettingsDetail
			{
				DetailID = SettingsKeys.Key_StoreName,
				Descr = SettingsKeys.Descr_StoreName,
				ControlType = SettingsControlType.Text,
			};
		}
	}
}
