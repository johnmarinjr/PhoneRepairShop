using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN
{
	[PXProjection(typeof(Select2<InventoryItem,
		CrossJoin<Currency,
		LeftJoin<INItemCost, On2<FK.InventoryItem, And<INItemCost.FK.Currency>>,
		LeftJoin<InventoryItemCurySettings, On2<InventoryItemCurySettings.FK.Inventory, And<InventoryItemCurySettings.FK.Currency>>,
		LeftJoin<INSite, On<InventoryItemCurySettings.FK.DefaultSite>>>>>>
		), Persistent = false)]
	[PXCacheName(Messages.ItemCostStatistics)]
	public class INItemCost : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INItemCost>.By<inventoryID, curyID>
		{
			public static INItemCost Find(PXGraph graph, int? inventoryID, string baseCuryID) => FindBy(graph, inventoryID, baseCuryID);
		}
		public static class FK
		{
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INItemCost>.By<inventoryID> { }
			public class InventoryItemCurySettings : IN.InventoryItemCurySettings.PK.ForeignKeyOf<INItemCost>.By<inventoryID, curyID> { }
			public class Currency : CM.Currency.PK.ForeignKeyOf<INItemCost>.By<curyID> { }
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true, BqlField = typeof(InventoryItem.inventoryID))]
		[PXDefault(typeof(InventoryItem.inventoryID))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsKey = true, IsUnicode = true, BqlField = typeof(Currency.curyID))]
		[PXDefault(typeof(AccessInfo.baseCuryID))]
		public string CuryID
		{
			get;
			set;
		}
		#endregion
		#region LastCost
		public abstract class lastCost : PX.Data.BQL.BqlDecimal.Field<lastCost> { }
		[PXDBPriceCost(BqlField = typeof(INItemCost.lastCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Last Cost", Enabled = false)]
		[CurySymbol]
		public virtual Decimal? LastCost
		{
			get;
			set;
		}
		#endregion
		#region LastCostDate
		public abstract class lastCostDate : PX.Data.BQL.BqlDateTime.Field<lastCostDate> { }
		[PXDBLastChangeDateTime(typeof(lastCost), BqlField = typeof(INItemCost.lastCostDate))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual DateTime? LastCostDate
		{
			get;
			set;
		}
		#endregion
		#region TotalCost
		public abstract class totalCost : PX.Data.BQL.BqlDecimal.Field<totalCost> { }
		[PXDBBaseCury(BqlField = typeof(INItemCost.totalCost))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? TotalCost
		{
			get;
			set;
		}
		#endregion
		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity(BqlField = typeof(INItemCost.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region AvgCost
		public abstract class avgCost : PX.Data.BQL.BqlDecimal.Field<avgCost> { }
		[PXPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Average Cost", Enabled = false)]
		[PXDBPriceCostCalced(typeof(Switch<Case<Where<INItemCost.qtyOnHand, Equal<decimal0>>, decimal0>, Div<INItemCost.totalCost, INItemCost.qtyOnHand>>), typeof(Decimal), CastToScale = 9, CastToPrecision = 25)]
		[CurySymbol]
		public virtual Decimal? AvgCost
		{
			get;
			set;
		}
		#endregion
		#region MinCost
		public abstract class minCost : PX.Data.BQL.BqlDecimal.Field<minCost> { }
		[PXDBPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Min. Cost", Enabled = false)]
		[CurySymbol]
		public virtual Decimal? MinCost
		{
			get;
			set;
		}
		#endregion
		#region MaxCost
		public abstract class maxCost : PX.Data.BQL.BqlDecimal.Field<maxCost> { }
		[PXDBPriceCost(BqlField = typeof(INItemCost.maxCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Max. Cost", Enabled = false)]
		[CurySymbol]
		public virtual Decimal? MaxCost
		{
			get;
			set;
		}
		#endregion
		#region TranUnitCost
		public abstract class tranUnitCost : PX.Data.BQL.BqlDecimal.Field<tranUnitCost> { }
		[PXDBCalced(typeof(Switch<Case<Where<InventoryItem.valMethod, Equal<INValMethod.standard>>, InventoryItemCurySettings.stdCost,
								Case<Where<InventoryItem.valMethod, Equal<INValMethod.average>, And<INSite.avgDefaultCost, Equal<INSite.avgDefaultCost.lastCost>,
										Or<InventoryItem.valMethod, Equal<INValMethod.fIFO>, And<INSite.fIFODefaultCost, Equal<INSite.avgDefaultCost.lastCost>,
										Or<InventoryItem.valMethod, Equal<INValMethod.specific>>>>>>,
										INItemCost.lastCost>>,
								Switch<Case<Where<INItemCost.qtyOnHand, Equal<decimal0>>, decimal0,
									Case<Where<Div<INItemCost.totalCost, INItemCost.qtyOnHand>, Less<decimal0>>, INItemCost.lastCost>>,
									Div<INItemCost.totalCost, INItemCost.qtyOnHand>>>), typeof(Decimal))]
		public virtual Decimal? TranUnitCost
		{
			get;
			set;
		}
		#endregion
	}
}
