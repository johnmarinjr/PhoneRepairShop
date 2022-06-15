using System;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.IN;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXCacheName("BigCommerce Inventory Item")]
	public sealed class BCInventoryItem : PXCacheExtension<InventoryItem>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region Visibility
		[PXDBString(1, IsUnicode = true)]
        [PXUIField(DisplayName = "Visibility")]
		[BCItemVisibility.List]
		[PXDefault(BCItemVisibility.StoreDefault)]
        public string Visibility { get; set; }
        public abstract class visibility : PX.Data.BQL.BqlString.Field<visibility> { }
		#endregion
		#region Availability
		[PXDBString(1, IsUnicode = true)]
		[PXUIField(DisplayName = "Availability")]
		[BCItemAvailabilities.ListDef]
		[PXDefault(BCItemAvailabilities.StoreDefault)]
		public string Availability { get; set; }
		public abstract class availability : PX.Data.BQL.BqlString.Field<availability> { }
		#endregion
		#region NotAvailMode
		[PXDBString(1, IsUnicode = true)]
		[PXUIField(DisplayName = "When Qty Unavailable")]
		[BCItemNotAvailModes.ListDef]
		[PXDefault(BCItemNotAvailModes.StoreDefault)]
		[PXUIEnabled(typeof(Where<BCInventoryItem.availability, Equal<BCItemAvailabilities.availableTrack>>))]
		[PXFormula(typeof(Default<BCInventoryItem.availability>))]
		public string NotAvailMode { get; set; }
		public abstract class notAvailMode : PX.Data.BQL.BqlString.Field<notAvailMode> { }
		#endregion
	
        #region CustomURL
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Custom URL")]
        public string CustomURL { get; set; }
        public abstract class customURL : PX.Data.BQL.BqlString.Field<customURL> { }
        #endregion
        #region PageTitle
		[PXDBLocalizableString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Page Title")]
        public string PageTitle { get; set; }
        public abstract class pageTitle : PX.Data.BQL.BqlString.Field<pageTitle> { }
		#endregion
		#region MetaDescription
		[PXDBLocalizableString(1024, IsUnicode = true)]
		[PXUIField(DisplayName = "Meta Description")]
        public string MetaDescription { get; set; }
        public abstract class metaDescription : PX.Data.BQL.BqlString.Field<metaDescription> { }
		#endregion
		#region MetaKeywords
		[PXDBLocalizableString(1024, IsUnicode = true)]
		[PXUIField(DisplayName = "Meta Keywords")]
		public string MetaKeywords { get; set; }
        public abstract class metaKeywords : PX.Data.BQL.BqlString.Field<metaKeywords> { }
		#endregion
		#region SearchKeywords
		[PXDBLocalizableString(1024, IsUnicode = true)]
		[PXUIField(DisplayName = "Search Keywords")]
		public string SearchKeywords { get; set; }
        public abstract class searchKeywords : PX.Data.BQL.BqlString.Field<searchKeywords> { }
		#endregion
		#region ShortDescription
		[PXDBLocalizableString(1024, IsUnicode = true)]
		[PXUIField(DisplayName = "Short Description", Visible = false)]
		public string ShortDescription { get; set; }
		public abstract class shortDescription : PX.Data.BQL.BqlString.Field<shortDescription> { }
		#endregion
	}
}