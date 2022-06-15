using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.DR;
using PX.TM;

namespace PX.Objects.IN
{
	[PXCacheName(Messages.InventoryItemCurySetting, CacheGlobal = true)]
	public class InventoryItemCurySettings : IBqlTable
    {
        #region Keys
		public class PK : PrimaryKeyOf<InventoryItemCurySettings>.By<inventoryID, curyID>
		{
			public static InventoryItemCurySettings Find(PXGraph graph, int? inventoryID, string curyID) => FindBy(graph, inventoryID, curyID);
			public static InventoryItemCurySettings FindDirty(PXGraph graph, int? inventoryID, string curyID)
				=> (InventoryItemCurySettings)PXSelect<InventoryItemCurySettings, 
						Where<inventoryID, Equal<Required<inventoryID>>,
						And<curyID, Equal<Required<curyID>>>>>
					.SelectWindowed(graph, 0, 1, inventoryID, curyID);
		}
		public static class FK
		{
			public class Inventory : IN.InventoryItem.PK.ForeignKeyOf<InventoryItemCurySettings>.By<inventoryID> { }
			public class Currency : CM.Currency.PK.ForeignKeyOf<InventoryItemCurySettings>.By<curyID> { }
			public class DefaultSite : INSite.PK.ForeignKeyOf<InventoryItemCurySettings>.By<dfltSiteID> { }
			public class DefaultShipLocation : INLocation.PK.ForeignKeyOf<InventoryItemCurySettings>.By<dfltShipLocationID> { }
			public class DefaultReceiptLocation : INLocation.PK.ForeignKeyOf<InventoryItemCurySettings>.By<dfltReceiptLocationID> { }
			public class PreferredVendor : AP.Vendor.PK.ForeignKeyOf<InventoryItem>.By<preferredVendorID> { }
			public class PreferredVendorLocation : Location.PK.ForeignKeyOf<InventoryItem>.By<preferredVendorID, preferredVendorLocationID> { }
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXParent(typeof(FK.Inventory))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region Currency
		[PXDBString(IsUnicode = true, IsKey=true)]
		[PXUIField(DisplayName = "Currency", Enabled = true)]
		[PXSelector(typeof(Search<CurrencyList.curyID>))]
		public virtual string CuryID { get; set; }
		public abstract class curyID : BqlString.Field<curyID> { }
		#endregion
		#region LastStdCost
		public abstract class lastStdCost : PX.Data.BQL.BqlDecimal.Field<lastStdCost> { }

		/// <summary>
		/// The standard cost assigned to the item before the current standard cost was set.
		/// </summary>
		[PXDBPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[CurySymbol]
		[PXUIField(DisplayName = "Last Cost", Enabled = false)]
		public virtual Decimal? LastStdCost { get; set; }

		#endregion
		#region PendingStdCost
		public abstract class pendingStdCost : PX.Data.BQL.BqlDecimal.Field<pendingStdCost> { }

		/// <summary>
		/// The standard cost to be assigned to the item when the costs are updated.
		/// </summary>
		[PXDBPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[CurySymbol]
		[PXUIField(DisplayName = "Pending Cost")]
		public virtual Decimal? PendingStdCost { get; set; }

		#endregion
		#region PendingStdCostDate
		public abstract class pendingStdCostDate : PX.Data.BQL.BqlDateTime.Field<pendingStdCostDate> { }

		/// <summary>
		/// The date when the <see cref="PendingStdCost">Pending Cost</see> becomes effective.
		/// </summary>
		[PXDBDate()]
		[PXUIField(DisplayName = "Pending Cost Date")]
		[PXFormula(typeof(Switch<Case<Where<InventoryItemCurySettings.pendingStdCost, NotEqual<CS.decimal0>>, Current<AccessInfo.businessDate>>, InventoryItemCurySettings.pendingStdCostDate>))]
		public virtual DateTime? PendingStdCostDate { get; set; }

		#endregion
		#region StdCost
		public abstract class stdCost : PX.Data.BQL.BqlDecimal.Field<stdCost> { }

		/// <summary>
		/// The current standard cost of the item.
		/// </summary>
		[PXDBPriceCost()]
		[CurySymbol]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Current Cost", Enabled = false)]
		public virtual Decimal? StdCost { get; set; }

		#endregion
		#region StdCostDate
		public abstract class stdCostDate : PX.Data.BQL.BqlDateTime.Field<stdCostDate> { }

		/// <summary>
		/// The date when the <see cref="StdCost">Current Cost</see> became effective.
		/// </summary>
		[PXDBDate()]
		[PXUIField(DisplayName = "Effective Date", Enabled = false)]
		public virtual DateTime? StdCostDate { get; set; }

		#endregion
		#region BasePrice
		public abstract class basePrice : PX.Data.BQL.BqlDecimal.Field<basePrice> { }

		/// <summary>
		/// The price used as the default price, if there are no other prices defined for this item in any price list in the Accounts Receivable module.
		/// </summary>
		[PXDBPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[CurySymbol]
		[PXUIField(DisplayName = "Default Price", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? BasePrice { get; set; }
		#endregion
		#region RecPrice
		public abstract class recPrice : PX.Data.BQL.BqlDecimal.Field<recPrice> { }
		protected Decimal? _RecPrice;

		/// <summary>
		/// The manufacturer's suggested retail price of the item.
		/// </summary>
		[PXDBPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[CurySymbol]
		[PXUIField(DisplayName = "MSRP")]
		public virtual Decimal? RecPrice
		{
			get
			{
				return this._RecPrice;
			}
			set
			{
				this._RecPrice = value;
			}
		}
		#endregion
		#region DfltSiteID
		/// <summary>
		/// The default <see cref="INSite">Warehouse</see> used to store the items of this kind.
		/// Applicable only for Stock Items (see <see cref="InventoryItem.StkItem"/>) and when the <see cref="FeaturesSet.Warehouse">Warehouses</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INSite.SiteID"/> field.
		/// Defaults to the <see cref="INItemClassCurySettings.DfltSiteID">Default Warehouse</see> specified for the <see cref="InventoryItem.ItemClassID">Class of the item</see>.
		/// </value>
		[IN.Site(DisplayName = "Default Warehouse", DescriptionField = typeof(INSite.descr))]
		[PXForeignReference(typeof(FK.DefaultSite))]
		public virtual Int32? DfltSiteID { get; set; }
		public abstract class dfltSiteID : BqlInt.Field<dfltSiteID> { }
		#endregion
		#region DfltShipLocationID
		/// <summary>
		/// The <see cref="INLocation">Location of warehouse</see> used by default to issue items of this kind.
		/// Applicable only for Stock Items (see <see cref="InventoryItem.StkItem"/>) when the <see cref="FeaturesSet.WarehouseLocation">Warehouse Locations</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INLocation.LocationID"/> field.
		/// </value>
		[Location(typeof(dfltSiteID), DisplayName = "Default Issue From", KeepEntry = false, ResetEntry = false, DescriptionField = typeof(INLocation.descr))]
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		public virtual Int32? DfltShipLocationID { get; set; }
		public abstract class dfltShipLocationID : BqlInt.Field<dfltShipLocationID> { }
		#endregion
		#region DfltReceiptLocationID
		/// <summary>
		/// The <see cref="INLocation">Location of warehouse</see> used by default to receive items of this kind.
		/// Applicable only for Stock Items (see <see cref="InventoryItem.StkItem"/>) when the <see cref="FeaturesSet.WarehouseLocation">Warehouse Locations</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INLocation.LocationID"/> field.
		/// </value>
		[Location(typeof(dfltSiteID), DisplayName = "Default Receipt To", KeepEntry = false, ResetEntry = false, DescriptionField = typeof(INLocation.descr))]
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		public virtual Int32? DfltReceiptLocationID { get; set; }
		public abstract class dfltReceiptLocationID : BqlInt.Field<dfltReceiptLocationID> { }
		#endregion
		#region PreferredVendorID
		/// <summary>
		/// Preferred (default) <see cref="AP.Vendor">Vendor</see> for purchases of this item. 
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="BAccount.BAccountID"/> field.
		/// </value>
		[AP.VendorNonEmployeeActive(DisplayName = "Preferred Vendor", Required = false, DescriptionField = typeof(AP.Vendor.acctName))]
		public virtual Int32? PreferredVendorID { get; set; }
		public abstract class preferredVendorID : BqlInt.Field<preferredVendorID> { }
		#endregion
		#region PreferredVendorLocationID
		/// <summary>
		/// The <see cref="Location"/> of the <see cref="PreferredVendorID">Preferred (default) Vendor</see>.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Location.LocationID"/> field.
		/// </value>
		[LocationID(typeof(Where<Location.bAccountID.IsEqual<preferredVendorID.FromCurrent>>), DescriptionField = typeof(Location.descr), DisplayName = "Preferred Location")]
		public virtual Int32? PreferredVendorLocationID { get; set; }
		public abstract class preferredVendorLocationID : BqlInt.Field<preferredVendorLocationID> { }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }

		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID()]
		public virtual string CreatedByScreenID { get; set; }

		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime()]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? CreatedDateTime { get; set; }

		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }

		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }

		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime()]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? LastModifiedDateTime { get; set; }

		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion

    }
}
