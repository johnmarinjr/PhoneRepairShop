using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.Objects.GL;
using PX.Objects.CS;
using System.Collections.Generic;
using PX.Commerce.Core;
using PX.Commerce.Objects;
using PX.Data.ReferentialIntegrity.Attributes;
using static PX.Commerce.Shopify.SPConnector;
using PX.Commerce.Shopify.API.REST;
using PX.Common;

namespace PX.Commerce.Shopify
{
	[Serializable]
	[PXCacheName("Shopify Settings")]
	public class BCBindingShopify : IBqlTable
	{
		public class PK : PrimaryKeyOf<BCBindingShopify>.By<BCBindingShopify.bindingID>
		{
			public static BCBindingShopify Find(PXGraph graph, int? binding) => FindBy(graph, binding);
		}

		#region BindingID
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(BCBinding.bindingID))]
		[PXUIField(DisplayName = "Store", Visible = false)]
		[PXParent(typeof(Select<BCBinding, Where<BCBinding.bindingID, Equal<Current<BCBindingShopify.bindingID>>>>))]
		public int? BindingID { get; set; }
		public abstract class bindingID : PX.Data.BQL.BqlInt.Field<bindingID> { }
		#endregion

		//Connection
		#region StoreBaseUrl
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Store Admin URL")]
		[PXDefault()]
		public virtual string ShopifyApiBaseUrl { get; set; }
		public abstract class shopifyApiBaseUrl : PX.Data.BQL.BqlString.Field<shopifyApiBaseUrl> { }
		#endregion
		#region ShopifyApiKey
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "API Key")]
		public virtual string ShopifyApiKey { get; set; }
		public abstract class shopifyApiKey : PX.Data.BQL.BqlString.Field<shopifyApiKey> { }
		#endregion
		#region ShopifyApiPassword
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "API Password")]
		public virtual string ShopifyApiPassword { get; set; }
		public abstract class shopifyApiPassword : PX.Data.BQL.BqlString.Field<shopifyApiPassword> { }
		#endregion
		#region ShopifyAccessToken
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Access Token")]
		public virtual string ShopifyAccessToken { get; set; }
		public abstract class shopifyAccessToken : IBqlField { }
		#endregion
		#region StoreSharedSecret
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Shared Secret")]
		[PXDefault()]
		public virtual string StoreSharedSecret { get; set; }
		public abstract class storeSharedSecret : PX.Data.BQL.BqlString.Field<storeSharedSecret> { }
		#endregion
		#region ShopifyPlus
		[PXDBString(2)]
		[PXUIField(DisplayName = "Store Plan")]
		[BCShopifyStorePlanAttribute]
		[PXDefault(BCShopifyStorePlanAttribute.NormalPlan, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string ShopifyStorePlan { get; set; }
		public abstract class shopifyStorePlan : PX.Data.BQL.BqlString.Field<shopifyStorePlan> { }
		#endregion
		#region ShopifyApiVersion
		[PXUIField(DisplayName = "API Version", IsReadOnly = true)]
		[PXString()]
		public virtual string ShopifyApiVersion { get; set; }
		public abstract class shopifyApiVersion : PX.Data.BQL.BqlString.Field<shopifyApiVersion> { }
		#endregion
		#region CombineCategoriesToTags
		[PXDBString(2)]
		[PXUIField(DisplayName = "Sales Category Export")]
		[BCSalesCategoriesExport]
		[PXDefault(BCSalesCategoriesExportAttribute.SyncToProductTags, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string CombineCategoriesToTags { get; set; }
		public abstract class combineCategoriesToTags : PX.Data.BQL.BqlString.Field<combineCategoriesToTags> { }
		#endregion
		#region ShopifySupportCurrencies 
		[PXDBString(200, IsUnicode = true)]
		[PXUIField(DisplayName = "Supported Currencies", IsReadOnly = true)]
		public virtual string ShopifySupportCurrencies { get; set; }
		public abstract class shopifySupportCurrencies : PX.Data.BQL.BqlString.Field<shopifySupportCurrencies> { }
		#endregion
		#region ShopifyStoreUrl
		[PXDBString(200, IsUnicode = true)]
		[PXUIField(DisplayName = "Store URL", IsReadOnly = true)]
		public virtual string ShopifyStoreUrl { get; set; }
		public abstract class shopifyStoreUrl : PX.Data.BQL.BqlString.Field<shopifyStoreUrl> { }
		#endregion
		#region ShopifyStoreTimeZone 
		[PXDBString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Store Time Zone", IsReadOnly = true)]
		public virtual string ShopifyStoreTimeZone { get; set; }
		public abstract class shopifyStoreTimeZone : PX.Data.BQL.BqlString.Field<shopifyStoreTimeZone> { }
        #endregion
        #region ApiDelaySeconds
        [PXDBInt(MinValue = 0)]
        [PXDefault(180, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual int? ApiDelaySeconds { get; set; }
        public abstract class apiDelaySeconds : PX.Data.BQL.BqlInt.Field<apiDelaySeconds> { }
		#endregion
		#region ShopifyPOS
		[PXDBBool()]
		[PXUIField(DisplayName = "Import POS Orders")]
		[PXDefault(false,PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? ShopifyPOS { get; set; }
		public abstract class shopifyPOS : PX.Data.BQL.BqlBool.Field<shopifyPOS> { }
		#endregion
		#region POSDirectOrderType
		[PXDBString(2, IsFixed = true, InputMask = "")]
		[PXUIField(DisplayName = "POS Direct Order Type")]
		[PXSelector(
			typeof(Search<SOOrderType.orderType,
				Where<SOOrderType.active, Equal<True>,
					And<SOOrderType.behavior, Equal<SOBehavior.iN>, And<SOOrderType.aRDocType, Equal<ARDocType.invoice>>>>>),
			DescriptionField = typeof(SOOrderType.descr))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string POSDirectOrderType { get; set; }
		public abstract class pOSDirectOrderType : PX.Data.BQL.BqlString.Field<pOSDirectOrderType> { }
		#endregion
		#region POSShippingOrderType
		[PXDBString(2, IsFixed = true, InputMask = "")]
		[PXUIField(DisplayName = "POS Shipping Order Type")]
		[PXSelector(
			typeof(Search<SOOrderType.orderType,
				Where<SOOrderType.active, Equal<True>,
					And<SOOrderType.behavior, In3<SOBehavior.sO, SOBehavior.tR>, And<SOOrderType.aRDocType, Equal<ARDocType.invoice>>>>>),
			DescriptionField = typeof(SOOrderType.descr))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string POSShippingOrderType { get; set; }
		public abstract class pOSShippingOrderType : PX.Data.BQL.BqlString.Field<pOSShippingOrderType> { }
		#endregion


		#region ApiCallLimit
		[PXInt()]
		public virtual int? ApiCallLimit
		{
			get
			{
				return this.ShopifyStorePlan == BCShopifyStorePlanAttribute.PlusPlan ? ShopifyConstants.ApiCallLimitPlus : ShopifyConstants.ApiCallLimitDefault;
			}
		}
		public abstract class apiCallLimit : PX.Data.BQL.BqlInt.Field<apiCallLimit> { }
		#endregion
	}

	[PXPrimaryGraph(new Type[] { typeof(BCShopifyStoreMaint) },
					new Type[] { typeof(Where<BCBinding.connectorType, Equal<spConnectorType>>),})]
	public sealed class BCBindingShopifyExtension : PXCacheExtension<BCBinding>
	{
		public static bool IsActive() { return true; }
	}
}
