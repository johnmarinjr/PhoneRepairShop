using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.Objects.GL;
using PX.Objects.CS;
using System.Collections.Generic;
using PX.Commerce.BigCommerce;
using PX.Commerce.Core;
using PX.Data.ReferentialIntegrity.Attributes;
using static PX.Commerce.BigCommerce.BCConnector;
using PX.Commerce.Objects;
namespace PX.Commerce.BigCommerce
{
	[Serializable]
	[PXCacheName("BigCommerce Settings")]
	public class BCBindingBigCommerce : IBqlTable
	{
		public class PK : PrimaryKeyOf<BCBindingBigCommerce>.By<BCBindingBigCommerce.bindingID>
		{
			public static BCBindingBigCommerce Find(PXGraph graph, int? binding) => FindBy(graph, binding);
		}

		#region BindingID
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(BCBinding.bindingID))]
		[PXUIField(DisplayName = "Store", Visible = false)]
		[PXParent(typeof(Select<BCBinding, Where<BCBinding.bindingID, Equal<Current<BCBindingBigCommerce.bindingID>>>>))]
		public int? BindingID { get; set; }
		public abstract class bindingID : PX.Data.BQL.BqlInt.Field<bindingID> { }
		#endregion

		//Connection
		#region StoreBaseUrl
		[PXDBString(50, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "API Path")]
		[PXDefault()]
		public virtual string StoreBaseUrl { get; set; }
		public abstract class storeBaseUrl : PX.Data.BQL.BqlString.Field<storeBaseUrl> { }
		#endregion
		#region StoreXAuthClient
		//[PXDBString(50, IsUnicode = true, InputMask = "")]
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Client ID")]
		[PXDefault()]
		public virtual string StoreXAuthClient { get; set; }
		public abstract class storeXAuthClient : PX.Data.BQL.BqlString.Field<storeXAuthClient> { }
		#endregion
		#region StoreXAuthToken
		//[PXDBString(50, IsUnicode = true, InputMask = "")]
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Access Token")]
		[PXDefault()]
		public virtual string StoreXAuthToken { get; set; }
		public abstract class storeXAuthToken : PX.Data.BQL.BqlString.Field<storeXAuthToken> { }
		#endregion

		#region BigCommerceStoreTimeZone 
		[PXDBString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Store Time Zone", IsReadOnly = true)]
		public virtual string BigCommerceStoreTimeZone { get; set; }
		public abstract class bigCommerceStoreTimeZone : PX.Data.BQL.BqlString.Field<bigCommerceStoreTimeZone> { }
		#endregion

		#region StoreWDAVServerUrl
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "WebDAV Path")]
		[PXDefault()]
		public virtual string StoreWDAVServerUrl { get; set; }
		public abstract class storeWDAVServerUrl : PX.Data.BQL.BqlString.Field<storeWDAVServerUrl> { }
		#endregion
		#region StoreWDAVClientUser
		[PXDBString(50, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "WebDAV Username")]
		[PXDefault()]
		public virtual string StoreWDAVClientUser { get; set; }
		public abstract class storeWDAVClientUser : PX.Data.BQL.BqlString.Field<storeWDAVClientUser> { }
		#endregion
		#region StoreWDAVClientPass
		//[PXDBString(50, IsUnicode = true, InputMask = "")]
		[PXRSACryptString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "WebDAV Password")]
		[PXDefault()]
		public virtual string StoreWDAVClientPass { get; set; }
		public abstract class storeWDAVClientPass : PX.Data.BQL.BqlString.Field<storeWDAVClientPass> { }
		#endregion
		#region StoreAdminURL
		[PXDBString(100, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Store Admin Path")]
		[PXDefault()]
		public virtual string StoreAdminUrl { get; set; }
		public abstract class storeAdminUrl : PX.Data.BQL.BqlString.Field<storeAdminUrl> { }
		#endregion
	}

	[PXPrimaryGraph(new Type[] { typeof(BCBigCommerceStoreMaint) },
					new Type[] { typeof(Where<BCBinding.connectorType, Equal<bcConnectorType>>)})]

	public sealed class BCBindingBigCommerceExtension : PXCacheExtension<BCBinding>
	{
		public static bool IsActive() { return true; }
	}
}
