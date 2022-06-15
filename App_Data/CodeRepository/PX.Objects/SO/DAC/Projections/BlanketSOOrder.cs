using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.IN;

namespace PX.Objects.SO.DAC.Projections
{
	[PXCacheName(Messages.BlanketSOOrder)]
	[PXProjection(typeof(Select<SOOrder, Where<SOOrder.behavior, Equal<SOBehavior.bL>>>), Persistent = true)]
	public class BlanketSOOrder : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BlanketSOOrder>.By<orderType, orderNbr>
		{
			public static BlanketSOOrder Find(PXGraph graph, string orderType, string orderNbr) => FindBy(graph, orderType, orderNbr);
		}
		#endregion

		#region OrderType
		public abstract class orderType : Data.BQL.BqlString.Field<orderType> { }
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(SOOrder.orderType))]
		[PXDefault]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : Data.BQL.BqlString.Field<orderNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(SOOrder.orderNbr))]
		[PXDefault]
		public virtual string OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region Hold
		public abstract class hold : BqlBool.Field<hold> { }
		[PXDBBool(BqlField = typeof(SOOrder.hold))]
		[PXDefault]
		public virtual bool? Hold
		{
			get;
			set;
		}
		#endregion
		#region Approved
		public abstract class approved : BqlBool.Field<approved> { }
		[PXDBBool(BqlField = typeof(SOOrder.approved))]
		[PXDefault]
		public virtual bool? Approved
		{
			get;
			set;
		}
		#endregion
		#region Completed
		public abstract class completed : BqlBool.Field<completed> { }
		[PXDBBool(BqlField = typeof(SOOrder.completed))]
		[PXDefault]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlField = typeof(SOOrder.curyID))]
		[PXDefault]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : BqlLong.Field<curyInfoID> { }
		[PXDBLong(BqlField = typeof(SOOrder.curyInfoID))]
		[CurrencyInfo]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region OrderDate
		public abstract class orderDate : BqlDateTime.Field<orderDate> { }
		[PXDBDate(BqlField = typeof(SOOrder.orderDate))]
		[PXDefault]
		public virtual DateTime? OrderDate
		{
			get;
			set;
		}
		#endregion
		#region TaxCalcMode
		public abstract class taxCalcMode : BqlString.Field<taxCalcMode> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOOrder.taxCalcMode))]
		[PXDefault]
		public virtual string TaxCalcMode
		{
			get;
			set;
		}
		#endregion

		#region ExpireDate
		public abstract class expireDate : BqlDateTime.Field<expireDate> { }
		[PXDBDate(BqlField = typeof(SOOrder.expireDate))]
		public virtual DateTime? ExpireDate
		{
			get;
			set;
		}
		#endregion
		#region IsExpired
		public abstract class isExpired : BqlBool.Field<isExpired> { }
		[PXDBBool(BqlField = typeof(SOOrder.isExpired))]
		[PXDefault]
		public virtual bool? IsExpired
		{
			get;
			set;
		}
		#endregion
		#region QtyOnOrders
		public abstract class qtyOnOrders : BqlDecimal.Field<qtyOnOrders> { }
		[PXDBQuantity(BqlField = typeof(SOOrder.qtyOnOrders))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region BlanketOpenQty
		public abstract class blanketOpenQty : BqlDecimal.Field<blanketOpenQty> { }
		[PXDBQuantity(BqlField = typeof(SOOrder.blanketOpenQty))]
		[PXDefault]
		public virtual decimal? BlanketOpenQty
		{
			get;
			set;
		}
		#endregion
		#region CuryOpenOrderTotal
		public abstract class curyOpenOrderTotal : BqlDecimal.Field<curyOpenOrderTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(openOrderTotal), BqlField = typeof(SOOrder.curyOpenOrderTotal))]
		[PXDefault]
		public virtual decimal? CuryOpenOrderTotal
		{
			get;
			set;
		}
		#endregion
		#region OpenOrderTotal
		public abstract class openOrderTotal : BqlDecimal.Field<openOrderTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.openOrderTotal))]
		[PXDefault]
		public virtual decimal? OpenOrderTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryOpenLineTotal
		public abstract class curyOpenLineTotal : BqlDecimal.Field<curyOpenLineTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(openLineTotal), BqlField = typeof(SOOrder.curyOpenLineTotal))]
		[PXDefault]
		public virtual decimal? CuryOpenLineTotal
		{
			get;
			set;
		}
		#endregion
		#region OpenLineTotal
		public abstract class openLineTotal : BqlDecimal.Field<openLineTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.openLineTotal))]
		[PXDefault]
		public virtual decimal? OpenLineTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryOpenTaxTotal
		public abstract class curyOpenTaxTotal : BqlDecimal.Field<curyOpenTaxTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(openTaxTotal), BqlField = typeof(SOOrder.curyOpenTaxTotal))]
		[PXDefault]
		public virtual decimal? CuryOpenTaxTotal
		{
			get;
			set;
		}
		#endregion
		#region OpenTaxTotal
		public abstract class openTaxTotal : BqlDecimal.Field<openTaxTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.openTaxTotal))]
		[PXDefault]
		public virtual decimal? OpenTaxTotal
		{
			get;
			set;
		}
		#endregion
		#region OpenOrderQty
		public abstract class openOrderQty : BqlDecimal.Field<openOrderQty> { }
		[PXDBQuantity(BqlField = typeof(SOOrder.openOrderQty))]
		[PXDefault]
		public virtual decimal? OpenOrderQty
		{
			get;
			set;
		}
		#endregion
		#region UnbilledOrderQty
		public abstract class unbilledOrderQty : BqlDecimal.Field<unbilledOrderQty> { }
		[PXDBQuantity(BqlField = typeof(SOOrder.unbilledOrderQty))]
		[PXDefault]
		public virtual decimal? UnbilledOrderQty
		{
			get;
			set;
		}
		#endregion
		#region CuryUnbilledOrderTotal
		public abstract class curyUnbilledOrderTotal : BqlDecimal.Field<curyUnbilledOrderTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(unbilledOrderTotal), BqlField = typeof(SOOrder.curyUnbilledOrderTotal))]
		[PXDefault]
		public virtual decimal? CuryUnbilledOrderTotal
		{
			get;
			set;
		}
		#endregion
		#region UnbilledOrderTotal
		public abstract class unbilledOrderTotal : BqlDecimal.Field<unbilledOrderTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.unbilledOrderTotal))]
		[PXDefault]
		public virtual decimal? UnbilledOrderTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryUnbilledLineTotal
		public abstract class curyUnbilledLineTotal : BqlDecimal.Field<curyUnbilledLineTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(unbilledLineTotal), BqlField = typeof(SOOrder.curyUnbilledLineTotal))]
		[PXDefault]
		public virtual decimal? CuryUnbilledLineTotal
		{
			get;
			set;
		}
		#endregion
		#region UnbilledLineTotal
		public abstract class unbilledLineTotal : BqlDecimal.Field<unbilledLineTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.unbilledLineTotal))]
		[PXDefault]
		public virtual decimal? UnbilledLineTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryUnbilledTaxTotal
		public abstract class curyUnbilledTaxTotal : BqlDecimal.Field<curyUnbilledTaxTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(unbilledTaxTotal), BqlField = typeof(SOOrder.curyUnbilledTaxTotal))]
		[PXDefault]
		public virtual decimal? CuryUnbilledTaxTotal
		{
			get;
			set;
		}
		#endregion
		#region UnbilledTaxTotal
		public abstract class unbilledTaxTotal : BqlDecimal.Field<unbilledTaxTotal> { }
		[PXDBDecimal(4, BqlField = typeof(SOOrder.unbilledTaxTotal))]
		[PXDefault]
		public virtual decimal? UnbilledTaxTotal
		{
			get;
			set;
		}
		#endregion
		#region MinSchedOrderDate
		public abstract class minSchedOrderDate : BqlDateTime.Field<minSchedOrderDate> { }
		[PXDBDate(BqlField = typeof(SOOrder.minSchedOrderDate))]
		public virtual DateTime? MinSchedOrderDate
		{
			get;
			set;
		}
		#endregion

		#region OpenLineCntr
		public abstract class openLineCntr : BqlInt.Field<openLineCntr> { }
		[PXDBInt(BqlField = typeof(SOOrder.openLineCntr))]
		[PXDefault]
		public virtual int? OpenLineCntr
		{
			get;
			set;
		}
		#endregion
		#region OrigOpenLineCntr
		public abstract class origOpenLineCntr : BqlInt.Field<origOpenLineCntr> { }
		[PXInt]
		[PXDBCalced(typeof(SOOrder.openLineCntr), typeof(int))]
		[PXDefault]
		public virtual int? OrigOpenLineCntr
		{
			get;
			set;
		}
		#endregion
		#region CuryUnreleasedPaymentAmt
		public abstract class curyUnreleasedPaymentAmt : Data.BQL.BqlDecimal.Field<curyUnreleasedPaymentAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(unreleasedPaymentAmt), BqlField = typeof(SOOrder.curyUnreleasedPaymentAmt))]
		[PXDefault]
		public virtual decimal? CuryUnreleasedPaymentAmt
		{
			get;
			set;
		}
		#endregion
		#region UnreleasedPaymentAmt
		public abstract class unreleasedPaymentAmt : Data.BQL.BqlDecimal.Field<unreleasedPaymentAmt> { }
		[PXDBBaseCury(BqlField = typeof(SOOrder.unreleasedPaymentAmt))]
		[PXDefault]
		public virtual decimal? UnreleasedPaymentAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryCCAuthorizedAmt
		public abstract class curyCCAuthorizedAmt : Data.BQL.BqlDecimal.Field<curyCCAuthorizedAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(cCAuthorizedAmt), BqlField = typeof(SOOrder.curyCCAuthorizedAmt))]
		[PXDefault]
		public virtual decimal? CuryCCAuthorizedAmt
		{
			get;
			set;
		}
		#endregion
		#region CCAuthorizedAmt
		public abstract class cCAuthorizedAmt : Data.BQL.BqlDecimal.Field<cCAuthorizedAmt> { }
		[PXDBBaseCury(BqlField = typeof(SOOrder.cCAuthorizedAmt))]
		[PXDefault]
		public virtual decimal? CCAuthorizedAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryPaidAmt
		public abstract class curyPaidAmt : Data.BQL.BqlDecimal.Field<curyPaidAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(paidAmt), BqlField = typeof(SOOrder.curyPaidAmt))]
		[PXDefault]
		public virtual decimal? CuryPaidAmt
		{
			get;
			set;
		}
		#endregion
		#region PaidAmt
		public abstract class paidAmt : Data.BQL.BqlDecimal.Field<paidAmt> { }
		[PXDBBaseCury(BqlField = typeof(SOOrder.paidAmt))]
		[PXDefault]
		public virtual decimal? PaidAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryPaymentTotal
		public abstract class curyPaymentTotal : PX.Data.BQL.BqlDecimal.Field<curyPaymentTotal> { }
		[PXDBCurrency(typeof(BlanketSOOrder.curyInfoID), typeof(BlanketSOOrder.paymentTotal), BqlField = typeof(SOOrder.curyPaymentTotal))]
		[PXDefault]
		public virtual Decimal? CuryPaymentTotal
		{
			get;
			set;
		}
		#endregion

		#region PaymentTotal
		public abstract class paymentTotal : PX.Data.BQL.BqlDecimal.Field<paymentTotal> { }
		[PXDBBaseCury(BqlField = typeof(SOOrder.paymentTotal))]
		[PXDefault]
		public virtual Decimal? PaymentTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryPaymentOverall
		public abstract class curyPaymentOverall : PX.Data.BQL.BqlDecimal.Field<curyPaymentOverall> { }
		[PXDBCurrency(typeof(BlanketSOOrder.curyInfoID), typeof(BlanketSOOrder.paymentOverall), BqlField = typeof(SOOrder.curyPaymentOverall))]
		[PXDefault]
		public virtual decimal? CuryPaymentOverall
		{
			get;
			set;
		}
		#endregion
		#region PaymentOverall
		public abstract class paymentOverall : PX.Data.BQL.BqlDecimal.Field<paymentOverall> { }
		[PXDBBaseCury(BqlField = typeof(SOOrder.paymentOverall))]
		[PXDefault]
		public virtual decimal? PaymentOverall
		{
			get;
			set;
		}
		#endregion
		#region CuryTransferredToChildrenPaymentTotal
		public abstract class curyTransferredToChildrenPaymentTotal : Data.BQL.BqlDecimal.Field<curyTransferredToChildrenPaymentTotal> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(transferredToChildrenPaymentTotal), BqlField = typeof(SOOrder.curyTransferredToChildrenPaymentTotal))]
		[PXDefault]
		public virtual decimal? CuryTransferredToChildrenPaymentTotal
		{
			get;
			set;
		}
		#endregion
		#region TransferredToChildrenPaymentTotal
		public abstract class transferredToChildrenPaymentTotal : Data.BQL.BqlDecimal.Field<transferredToChildrenPaymentTotal> { }
		[PXDBBaseCury(BqlField = typeof(transferredToChildrenPaymentTotal))]
		[PXDefault]
		public virtual decimal? TransferredToChildrenPaymentTotal
		{
			get;
			set;
		}
		#endregion

		#region ShipmentCntr
		public abstract class shipmentCntr : BqlInt.Field<shipmentCntr> { }
		[PXDBInt(BqlField = typeof(SOOrder.shipmentCntr))]
		[PXDefault]
		public virtual int? ShipmentCntr
		{
			get;
			set;
		}
		#endregion
		#region ShipmentCntrUpdated
		public abstract class shipmentCntrUpdated : BqlBool.Field<shipmentCntrUpdated> { }
		[PXBool]
		public virtual bool? ShipmentCntrUpdated
		{
			get;
			set;
		}
		#endregion
		#region ChildLineCntr
		public abstract class childLineCntr : Data.BQL.BqlInt.Field<childLineCntr> { }
		[PXDBInt(BqlField = typeof(SOOrder.childLineCntr))]
		[PXDefault]
		public virtual int? ChildLineCntr
		{
			get;
			set;
		}
		#endregion

		#region NoteID
		public abstract class noteID : BqlGuid.Field<noteID> { }
		[PXDBGuid(BqlField = typeof(SOOrder.noteID))]
		[PXDefault]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp(BqlField = typeof(SOOrder.Tstamp), RecordComesFirst = true)]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(SOOrder.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(SOOrder.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(SOOrder.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}
}
