using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO.DAC.Projections
{
	[PXCacheName(Messages.BlanketSOLineSplit)]
	[PXProjection(typeof(Select<SOLineSplit, Where<SOLineSplit.behavior, Equal<SOBehavior.bL>>>), Persistent = true)]
	public class BlanketSOLineSplit : IBqlTable, IItemPlanMaster
	{
		#region Keys
		public class PK : PrimaryKeyOf<BlanketSOLineSplit>.By<orderType, orderNbr, lineNbr, splitLineNbr>
		{
			public static BlanketSOLineSplit Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr, int? splitLineNbr)
				=> FindBy(graph, orderType, orderNbr, lineNbr, splitLineNbr);
		}
		public static class FK
		{
			public class Order : SOOrder.PK.ForeignKeyOf<BlanketSOLineSplit>.By<orderType, orderNbr> { }
			public class BlanketOrder : BlanketSOOrder.PK.ForeignKeyOf<BlanketSOLineSplit>.By<orderType, orderNbr> { }
			public class BlanketOrderLine : BlanketSOLine.PK.ForeignKeyOf<BlanketSOLineSplit>.By<orderType, orderNbr, lineNbr> { }
		}
		#endregion

		#region OrderType
		public abstract class orderType : Data.BQL.BqlString.Field<orderType> { }
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(SOLineSplit.orderType))]
		[PXDefault]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : Data.BQL.BqlString.Field<orderNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(SOLineSplit.orderNbr))]
		[PXDefault]
		public virtual string OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(SOLineSplit.lineNbr))]
		[PXDefault]
		[PXParent(typeof(FK.BlanketOrderLine))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region SplitLineNbr
		public abstract class splitLineNbr : Data.BQL.BqlInt.Field<splitLineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(SOLineSplit.splitLineNbr))]
		[PXDefault]
		public virtual int? SplitLineNbr
		{
			get;
			set;
		}
		#endregion

		#region InventoryID
		public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID>
		{
		}
		[PXDBInt(BqlField = typeof(SOLineSplit.inventoryID))]
		[PXDefault]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region LineType
		public abstract class lineType : Data.BQL.BqlString.Field<lineType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLineSplit.lineType))]
		[PXDefault]
		public virtual string LineType
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : Data.BQL.BqlInt.Field<siteID> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.siteID))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? SiteID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : Data.BQL.BqlInt.Field<locationID> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.locationID))]
		public virtual int? LocationID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : Data.BQL.BqlInt.Field<subItemID> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.subItemID))]
		[PXDefault]
		public virtual int? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region ShipDate
		public abstract class shipDate : PX.Data.BQL.BqlDateTime.Field<shipDate> { }
		[PXDBDate(BqlField = typeof(SOLineSplit.shipDate))]
		public virtual DateTime? ShipDate
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : Data.BQL.BqlString.Field<uOM> { }
		[INUnit(typeof(inventoryID), BqlField = typeof(SOLineSplit.uOM))]
		[PXDefault]
		public virtual string UOM
		{
			get;
			set;
		}
		#endregion
		#region Qty
		public abstract class qty : Data.BQL.BqlDecimal.Field<qty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseQty), BqlField = typeof(SOLineSplit.qty))]
		[PXDefault]
		public virtual decimal? Qty
		{
			get;
			set;
		}
		#endregion
		#region BaseQty
		public abstract class baseQty : Data.BQL.BqlDecimal.Field<baseQty> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLineSplit.baseQty))]
		[PXDefault]
		public virtual decimal? BaseQty
		{
			get;
			set;
		}
		#endregion
		#region ToSiteID
		public abstract class toSiteID : Data.BQL.BqlInt.Field<toSiteID> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.toSiteID))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? ToSiteID
		{
			get;
			set;
		}
		#endregion
		#region LotSerialNbr
		public abstract class lotSerialNbr : Data.BQL.BqlString.Field<lotSerialNbr> { }
		[PXDBString(INLotSerialStatus.lotSerialNbr.LENGTH, IsUnicode = true, BqlField = typeof(SOLineSplit.lotSerialNbr))]
		public virtual string LotSerialNbr
		{
			get;
			set;
		}
		#endregion

		#region IsAllocated
		public abstract class isAllocated : Data.BQL.BqlBool.Field<isAllocated> { }
		[PXDBBool(BqlField = typeof(SOLineSplit.isAllocated))]
		[PXDefault]
		public virtual bool? IsAllocated
		{
			get;
			set;
		}
		#endregion
		#region POCreate
		public abstract class pOCreate : Data.BQL.BqlBool.Field<pOCreate> { }
		[PXDBBool(BqlField = typeof(SOLineSplit.pOCreate))]
		[PXDefault]
		public virtual bool? POCreate
		{
			get;
			set;
		}
		#endregion
		#region POType
		public abstract class pOType : Data.BQL.BqlString.Field<pOType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLineSplit.pOType))]
		public virtual string POType
		{
			get;
			set;
		}
		#endregion
		#region PONbr
		public abstract class pONbr : Data.BQL.BqlString.Field<pONbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLineSplit.pONbr))]
		public virtual string PONbr
		{
			get;
			set;
		}
		#endregion
		#region POLineNbr
		public abstract class pOLineNbr : Data.BQL.BqlInt.Field<pOLineNbr> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.pOLineNbr))]
		public virtual int? POLineNbr
		{
			get;
			set;
		}
		#endregion
		#region POReceiptType
		public abstract class pOReceiptType : Data.BQL.BqlString.Field<pOReceiptType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLineSplit.pOReceiptType))]
		public virtual string POReceiptType
		{
			get;
			set;
		}
		#endregion
		#region POReceiptNbr
		public abstract class pOReceiptNbr : Data.BQL.BqlString.Field<pOReceiptNbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLineSplit.pOReceiptNbr))]
		public virtual string POReceiptNbr
		{
			get;
			set;
		}
		#endregion
		#region RefNoteID
		public abstract class refNoteID : Data.BQL.BqlGuid.Field<refNoteID> { }
		[PXDBGuid(BqlField = typeof(SOLineSplit.refNoteID))]
		public virtual Guid? RefNoteID
		{
			get;
			set;
		}
		#endregion
		#region POCompleted
		public abstract class pOCompleted : Data.BQL.BqlBool.Field<pOCompleted> { }
		[PXDBBool(BqlField = typeof(SOLineSplit.pOCompleted))]
		[PXDefault]
		public virtual bool? POCompleted
		{
			get;
			set;
		}
		#endregion
		#region POCancelled
		public abstract class pOCancelled : Data.BQL.BqlBool.Field<pOCancelled> { }
		[PXDBBool(BqlField = typeof(SOLineSplit.pOCancelled))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? POCancelled
		{
			get;
			set;
		}
		#endregion
		#region POSource
		public abstract class pOSource : Data.BQL.BqlString.Field<pOSource> { }
		[PXDBString(BqlField = typeof(SOLineSplit.pOSource))]
		public virtual string POSource
		{
			get;
			set;
		}
		#endregion
		#region VendorID
		public abstract class vendorID : Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.vendorID))]
		public virtual int? VendorID
		{
			get;
			set;
		}
		#endregion

		#region Completed
		public abstract class completed : Data.BQL.BqlBool.Field<completed> { }
		[PXDBBool(BqlField = typeof(SOLineSplit.completed))]
		[PXDefault]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region PlanID
		public abstract class planID : Data.BQL.BqlLong.Field<planID> { }
		[PXDBLong(IsImmutable = true, BqlField = typeof(SOLineSplit.planID))]
		public virtual long? PlanID
		{
			get;
			set;
		}
		#endregion

		#region QtyOnOrders
		public abstract class qtyOnOrders : Data.BQL.BqlDecimal.Field<qtyOnOrders> { }
		[PXDBQuantity(typeof(uOM), typeof(baseQtyOnOrders), BqlField = typeof(SOLineSplit.qtyOnOrders))]
		[PXDefault]
		public virtual decimal? QtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region BaseQtyOnOrders
		public abstract class baseQtyOnOrders : Data.BQL.BqlDecimal.Field<baseQtyOnOrders> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLineSplit.baseQtyOnOrders))]
		[PXDefault]
		public virtual decimal? BaseQtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region CustomerOrderNbr
		public abstract class customerOrderNbr : Data.BQL.BqlString.Field<customerOrderNbr> { }
		[PXDBString(40, IsUnicode = true, BqlField = typeof(SOLineSplit.customerOrderNbr))]
		public virtual string CustomerOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region SchedOrderDate
		public abstract class schedOrderDate : Data.BQL.BqlDateTime.Field<schedOrderDate> { }
		[PXDBDate(BqlField = typeof(SOLineSplit.schedOrderDate))]
		public virtual DateTime? SchedOrderDate
		{
			get;
			set;
		}
		#endregion
		#region SchedShipDate
		public abstract class schedShipDate : Data.BQL.BqlDateTime.Field<schedShipDate> { }
		[PXDBDate(BqlField = typeof(SOLineSplit.schedShipDate))]
		public virtual DateTime? SchedShipDate
		{
			get;
			set;
		}
		#endregion
		#region BlanketOpenQty
		public abstract class blanketOpenQty : Data.BQL.BqlDecimal.Field<blanketOpenQty> { }
		[PXQuantity]
		[PXDBCalced(typeof(Switch<Case<Where<SOLineSplit.lineType, NotEqual<SOLineType.miscCharge>, And<SOLineSplit.completed, Equal<False>>>, Sub<SOLineSplit.qty, Add<SOLineSplit.qtyOnOrders, SOLineSplit.receivedQty>>>, decimal0>), typeof(decimal))]
		[PXFormula(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>, And<completed, Equal<False>>>, Sub<qty, Add<qtyOnOrders, receivedQty>>>, decimal0>))]
		[PXDefault]
		public virtual decimal? BlanketOpenQty
		{
			get;
			set;
		}
		#endregion
		#region ChildLineCntr
		public abstract class childLineCntr : Data.BQL.BqlInt.Field<childLineCntr> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.childLineCntr))]
		[PXDefault]
		public virtual int? ChildLineCntr
		{
			get;
			set;
		}
		#endregion
		#region EffectiveChildLineCntr
		public abstract class effectiveChildLineCntr : Data.BQL.BqlInt.Field<effectiveChildLineCntr> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.effectiveChildLineCntr))]
		[PXDefault]
		public virtual int? EffectiveChildLineCntr
		{
			get;
			set;
		}
		#endregion
		#region OpenChildLineCntr
		public abstract class openChildLineCntr : Data.BQL.BqlInt.Field<openChildLineCntr> { }
		[PXDBInt(BqlField = typeof(SOLineSplit.openChildLineCntr))]
		[PXDefault]
		public virtual int? OpenChildLineCntr
		{
			get;
			set;
		}
		#endregion

		#region ShippedQty
		public abstract class shippedQty : Data.BQL.BqlDecimal.Field<shippedQty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseShippedQty), BqlField = typeof(SOLineSplit.shippedQty))]
		[PXDefault]
		public virtual decimal? ShippedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseShippedQty
		public abstract class baseShippedQty : Data.BQL.BqlDecimal.Field<baseShippedQty> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLineSplit.baseShippedQty))]
		[PXDefault]
		public virtual decimal? BaseShippedQty
		{
			get;
			set;
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : Data.BQL.BqlDecimal.Field<receivedQty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseReceivedQty), BqlField = typeof(SOLineSplit.receivedQty))]
		[PXDefault]
		public virtual decimal? ReceivedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : Data.BQL.BqlDecimal.Field<baseReceivedQty> { }
		[PXDBDecimal(6, BqlField = typeof(SOLineSplit.baseReceivedQty))]
		[PXDefault]
		public virtual decimal? BaseReceivedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseUnreceivedQty
		public abstract class baseUnreceivedQty : Data.BQL.BqlDecimal.Field<baseUnreceivedQty> { }
		[PXDecimal]
		[PXFormula(typeof(Sub<baseQty, baseReceivedQty>))]
		public virtual decimal? BaseUnreceivedQty
		{
			get;
			set;
		}
		#endregion

		#region tstamp
		public abstract class Tstamp : Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp(BqlField = typeof(SOLineSplit.Tstamp), RecordComesFirst = true)]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(SOLineSplit.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(SOLineSplit.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(SOLineSplit.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}
}
