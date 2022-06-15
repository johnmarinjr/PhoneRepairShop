using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.Objects.SO.DAC.Projections;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.OpenBlanketSOLineSplit)]
	[PXVirtual]
	public class OpenBlanketSOLineSplit : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<OpenBlanketSOLineSplit>.By<orderType, orderNbr, lineNbr, splitLineNbr>
		{
			public static OpenBlanketSOLineSplit Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr, int? splitLineNbr)
				=> FindBy(graph, orderType, orderNbr, lineNbr, splitLineNbr);
		}
		public static class FK
		{
			public class BlanketOrderLine : BlanketSOLine.PK.ForeignKeyOf<OpenBlanketSOLineSplit>.By<orderType, orderNbr, lineNbr> { }

			public class BlanketOrderLineSplit : BlanketSOLineSplit.PK.ForeignKeyOf<OpenBlanketSOLineSplit>.By<orderType, orderNbr, lineNbr, splitLineNbr> { }
		}
		#endregion
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected
		{
			get;
			set;
		}
		#endregion
		#region OrderType
		public abstract class orderType : Data.BQL.BqlString.Field<orderType> { }
		[PXString(2, IsKey = true, IsFixed = true)]
		[PXUIField(DisplayName = "Order Type", Visible = true, Enabled = false)]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : Data.BQL.BqlString.Field<orderNbr> { }
		[PXString(15, IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual string OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		[PXInt(IsKey = true)]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region SplitLineNbr
		public abstract class splitLineNbr : Data.BQL.BqlInt.Field<splitLineNbr> { }
		[PXInt(IsKey = true)]
		public virtual int? SplitLineNbr
		{
			get;
			set;
		}
		#endregion

		#region InventoryID
		public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID> { }
		[Inventory(Enabled = false, Visible = true)]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : Data.BQL.BqlInt.Field<subItemID> { }
		[SubItem(Enabled = false)]
		public virtual int? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }
		[PXString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Enabled = false)]
		public virtual String TranDesc
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : Data.BQL.BqlInt.Field<siteID> { }
		[Site(Enabled = false)]
		[PXUIField(DisplayName = "Warehouse", Enabled = false, Visible = false)]
		public virtual int? SiteID
		{
			get;
			set;
		}
		#endregion
		#region CustomerOrderNbr
		public abstract class customerOrderNbr : Data.BQL.BqlString.Field<customerOrderNbr> { }
		[PXString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Customer Order Nbr.", Enabled = false)]
		public virtual string CustomerOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region SchedOrderDate
		public abstract class schedOrderDate : Data.BQL.BqlDateTime.Field<schedOrderDate> { }
		[PXDate()]
		[PXUIField(DisplayName = "Sched. Order Date", Enabled = false)]
		public virtual DateTime? SchedOrderDate
		{
			get;
			set;
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[PXInt()]
		public virtual Int32? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region CustomerLocationID
		public abstract class customerLocationID : Data.BQL.BqlInt.Field<customerLocationID> { }
		[LocationActive(DescriptionField = typeof(Location.descr),
			DisplayName = "Ship-To Location", Enabled = false)]
		public virtual int? CustomerLocationID
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : Data.BQL.BqlString.Field<uOM> { }
		[INUnit(typeof(inventoryID), Enabled = false)]
		public virtual string UOM
		{
			get;
			set;
		}
		#endregion
		#region BlanketOpenQty
		public abstract class blanketOpenQty : Data.BQL.BqlDecimal.Field<blanketOpenQty> { }
		[PXQuantity]
		[PXUIField(DisplayName = "Blanket Open Qty.", Enabled = false)]
		public virtual decimal? BlanketOpenQty
		{
			get;
			set;
		}
		#endregion
		#region TaxZoneID
		public abstract class taxZoneID : Data.BQL.BqlString.Field<taxZoneID> { }
		[PXString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Zone", Enabled = false)]
		public virtual string TaxZoneID
		{
			get;
			set;
		}
		#endregion
	}
}
