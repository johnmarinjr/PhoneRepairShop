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
	[PXCacheName(Messages.ItemClassCurySettings, CacheGlobal = true)]
	public class INItemClassCurySettings : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INItemClassCurySettings>.By<itemClassID, curyID>
		{
			public static INItemClassCurySettings Find(PXGraph graph, int? itemClassID, string curyID)
				=> FindBy(graph, itemClassID, curyID);
		}
		public static class FK
		{
			public class ItemClass : INItemClass.PK.ForeignKeyOf<INItemClassCurySettings>.By<itemClassID> { }
			public class Currency : CM.Currency.PK.ForeignKeyOf<InventoryItemCurySettings>.By<curyID> { }
			public class DefaultSite : INSite.PK.ForeignKeyOf<InventoryItemCurySettings>.By<dfltSiteID> { }
		}
		#endregion
		#region ItemClassID
		public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(INItemClass.itemClassID))]
		[PXUIField(DisplayName = "Item Class", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXParent(typeof(FK.ItemClass))]
		public virtual Int32? ItemClassID { get; set; }
		#endregion
		#region Currency
		[PXDBString(IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Currency", Enabled = true)]
		[PXSelector(typeof(Search<CurrencyList.curyID>))]
		[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
		public virtual string CuryID { get; set; }
		public abstract class curyID : BqlString.Field<curyID> { }
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

		[PXDBTimestamp()]
		public virtual Byte[] tstamp { get; set; }
		#endregion

	}
}