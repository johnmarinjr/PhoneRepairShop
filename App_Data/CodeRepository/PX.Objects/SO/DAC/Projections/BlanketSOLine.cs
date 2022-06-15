using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO.DAC.Projections
{
	[PXCacheName(Messages.BlanketSOLine)]
	[PXProjection(typeof(Select<SOLine, Where<SOLine.behavior, Equal<SOBehavior.bL>>>), Persistent = true)]
	public class BlanketSOLine : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BlanketSOLine>.By<orderType, orderNbr, lineNbr>
		{
			public static BlanketSOLine Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr) => FindBy(graph, orderType, orderNbr, lineNbr);
		}
		public static class FK
		{
			public class BlanketOrder : BlanketSOOrder.PK.ForeignKeyOf<BlanketSOLine>.By<orderType, orderNbr> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(BqlField = typeof(SOLine.branchID))]
		[PXDefault]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region OrderType
		public abstract class orderType : Data.BQL.BqlString.Field<orderType> { }
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(SOLine.orderType))]
		[PXDefault]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : Data.BQL.BqlString.Field<orderNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(SOLine.orderNbr))]
		[PXDefault]
		[PXParent(typeof(FK.BlanketOrder))]
		public virtual string OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(SOLine.lineNbr))]
		[PXDefault]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion

		#region Behavior
		public abstract class behavior : Data.BQL.BqlString.Field<behavior> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLine.behavior))]
		[PXDefault]
		public virtual string Behavior
		{
			get;
			set;
		}
		#endregion
		#region Operation
		public abstract class operation : Data.BQL.BqlString.Field<operation> { }
		[PXDBString(1, IsFixed = true, InputMask = ">a", BqlField = typeof(SOLine.operation))]
		[PXDefault]
		public virtual string Operation
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID>
		{
		}
		[PXDBInt(BqlField = typeof(SOLine.inventoryID))]
		[PXDefault]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : Data.BQL.BqlInt.Field<subItemID> { }
		[PXDBInt(BqlField = typeof(SOLine.subItemID))]
		[PXDefault]
		public virtual int? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }
		[PXDBString(256, IsUnicode = true, BqlField = typeof(SOLine.tranDesc))]
		public virtual String TranDesc
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : Data.BQL.BqlString.Field<uOM> { }
		[INUnit(typeof(inventoryID), BqlField = typeof(SOLine.uOM))]
		[PXDefault]
		public virtual string UOM
		{
			get;
			set;
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : Data.BQL.BqlDecimal.Field<orderQty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseOrderQty), BqlField = typeof(SOLine.orderQty))]
		[PXDefault]
		public virtual decimal? OrderQty
		{
			get;
			set;
		}
		#endregion
		#region BaseOrderQty
		public abstract class baseOrderQty : Data.BQL.BqlDecimal.Field<baseOrderQty> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLine.baseOrderQty))]
		[PXDefault]
		public virtual decimal? BaseOrderQty
		{
			get;
			set;
		}
		#endregion
		#region RequestDate
		public abstract class requestDate : Data.BQL.BqlDateTime.Field<requestDate> { }
		[PXDBDate(BqlField = typeof(SOLine.requestDate))]
		[PXDefault]
		public virtual DateTime? RequestDate
		{
			get;
			set;
		}
		#endregion
		#region TaxCategoryID
		public abstract class taxCategoryID : Data.BQL.BqlString.Field<taxCategoryID> { }
		[PXDBString(TX.TaxCategory.taxCategoryID.Length, IsUnicode = true, BqlField = typeof(SOLine.taxCategoryID))]
		public virtual string TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : Data.BQL.BqlInt.Field<projectID> { }
		[PXDBInt(BqlField = typeof(SOLine.projectID))]
		public virtual int? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region TaskID
		public abstract class taskID : Data.BQL.BqlInt.Field<taskID> { }
		[PXDBInt(BqlField = typeof(SOLine.taskID))]
		public virtual int? TaskID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : Data.BQL.BqlInt.Field<costCodeID> { }
		[PXDBInt(BqlField = typeof(SOLine.costCodeID))]
		public virtual int? CostCodeID
		{
			get;
			set;
		}
		#endregion
		#region ShipComplete
		public abstract class shipComplete : Data.BQL.BqlString.Field<shipComplete> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOLine.shipComplete))]
		[PXDefault]
		public virtual string ShipComplete
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong(BqlField = typeof(SOLine.curyInfoID))]
		[CurrencyInfo]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryUnitPrice
		public abstract class curyUnitPrice : Data.BQL.BqlDecimal.Field<curyUnitPrice> { }
		[PXDBCurrency(typeof(Search<CS.CommonSetup.decPlPrcCst>), typeof(curyInfoID), typeof(unitPrice), BqlField = typeof(SOLine.curyUnitPrice))]
		[PXDefault]
		public virtual decimal? CuryUnitPrice
		{
			get;
			set;
		}
		#endregion
		#region UnitPrice
		public abstract class unitPrice : Data.BQL.BqlDecimal.Field<unitPrice> { }
		[PXDBPriceCost(BqlField = typeof(SOLine.unitPrice))]
		[PXDefault]
		public virtual decimal? UnitPrice
		{
			get;
			set;
		}
		#endregion
		#region CuryExtPrice
		public abstract class curyExtPrice : Data.BQL.BqlDecimal.Field<curyExtPrice> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(extPrice), BqlField = typeof(SOLine.curyExtPrice))]
		[PXDefault]
		public virtual decimal? CuryExtPrice
		{
			get;
			set;
		}
		#endregion
		#region ExtPrice
		public abstract class extPrice : Data.BQL.BqlDecimal.Field<extPrice> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.extPrice))]
		[PXDefault]
		public virtual decimal? ExtPrice
		{
			get;
			set;
		}
		#endregion
		#region DiscPct
		public abstract class discPct : Data.BQL.BqlDecimal.Field<discPct> { }
		[PXDBDecimal(6, MinValue = -100, MaxValue = 100, BqlField = typeof(SOLine.discPct))]
		[PXDefault]
		public virtual decimal? DiscPct
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscAmt
		public abstract class curyDiscAmt : Data.BQL.BqlDecimal.Field<curyDiscAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(discAmt), BqlField = typeof(SOLine.curyDiscAmt))]
		[PXDefault]
		public virtual decimal? CuryDiscAmt
		{
			get;
			set;
		}
		#endregion
		#region DiscAmt
		public abstract class discAmt : Data.BQL.BqlDecimal.Field<discAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.discAmt))]
		[PXDefault]
		public virtual decimal? DiscAmt
		{
			get;
			set;
		}
		#endregion
		#region IsFree
		public abstract class isFree : Data.BQL.BqlBool.Field<isFree> { }
		[PXDBBool(BqlField = typeof(SOLine.isFree))]
		[PXDefault]
		public virtual bool? IsFree
		{
			get;
			set;
		}
		#endregion
		#region ManualDisc
		public abstract class manualDisc : PX.Data.BQL.BqlBool.Field<manualDisc> { }
		[PXDBBool(BqlField = typeof(SOLine.manualDisc))]
		[PXDefault(false)]
		public virtual Boolean? ManualDisc
		{
			get;
			set;
		}
		#endregion
		#region AutomaticDiscountsDisabled
		public abstract class automaticDiscountsDisabled : PX.Data.BQL.BqlBool.Field<automaticDiscountsDisabled> { }
		[PXDBBool(BqlField = typeof(SOLine.automaticDiscountsDisabled))]
		[PXDefault(false)]
		public virtual Boolean? AutomaticDiscountsDisabled
		{
			get;
			set;
		}
		#endregion
		#region DiscountID
		public abstract class discountID : PX.Data.BQL.BqlString.Field<discountID> { }
		[PXDBString(10, IsUnicode = true, BqlField = typeof(SOLine.discountID))]
		public virtual String DiscountID
		{
			get;
			set;
		}
		#endregion
		#region LineType
		public abstract class lineType : Data.BQL.BqlString.Field<lineType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLine.lineType))]
		[PXDefault]
		public virtual string LineType
		{
			get;
			set;
		}
		#endregion
		#region Completed
		public abstract class completed : Data.BQL.BqlBool.Field<completed> { }
		[PXDBBool(BqlField = typeof(SOLine.completed))]
		[PXDefault]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region CuryLineAmt
		public abstract class curyLineAmt : Data.BQL.BqlDecimal.Field<curyLineAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(lineAmt), BqlField = typeof(SOLine.curyLineAmt))]
		[PXDefault]
		public virtual decimal? CuryLineAmt
		{
			get;
			set;
		}
		#endregion
		#region LineAmt
		public abstract class lineAmt : Data.BQL.BqlDecimal.Field<lineAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.lineAmt))]
		[PXDefault]
		public virtual decimal? LineAmt
		{
			get;
			set;
		}
		#endregion
		#region GroupDiscountRate
		public abstract class groupDiscountRate : Data.BQL.BqlDecimal.Field<groupDiscountRate> { }
		[PXDBDecimal(18, BqlField = typeof(SOLine.groupDiscountRate))]
		[PXDefault]
		public virtual decimal? GroupDiscountRate
		{
			get;
			set;
		}
		#endregion
		#region DocumentDiscountRate
		public abstract class documentDiscountRate : Data.BQL.BqlDecimal.Field<documentDiscountRate> { }
		[PXDBDecimal(18, BqlField = typeof(SOLine.documentDiscountRate))]
		[PXDefault]
		public virtual decimal? DocumentDiscountRate
		{
			get;
			set;
		}
		#endregion
		#region SalesPersonID
		public abstract class salesPersonID : Data.BQL.BqlInt.Field<salesPersonID> { }
		[PXDBInt(BqlField = typeof(SOLine.salesPersonID))]
		public virtual int? SalesPersonID
		{
			get;
			set;
		}
		#endregion

		#region POCreate
		public abstract class pOCreate : Data.BQL.BqlBool.Field<pOCreate> { }
		[PXDBBool(BqlField = typeof(SOLine.pOCreate))]
		[PXDefault]
		public virtual bool? POCreate
		{
			get;
			set;
		}
		#endregion
		#region POSource
		public abstract class pOSource : Data.BQL.BqlString.Field<pOSource> { }
		[PXDBString(BqlField = typeof(SOLine.pOSource))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string POSource
		{
			get;
			set;
		}
		#endregion
		#region POCreated
		public abstract class pOCreated : Data.BQL.BqlBool.Field<pOCreated> { }
		[PXDBBool(BqlField = typeof(SOLine.pOCreated))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? POCreated
		{
			get;
			set;
		}
		#endregion
		#region VendorID
		public abstract class vendorID : Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt(BqlField = typeof(SOLine.vendorID))]
		public virtual int? VendorID
		{
			get;
			set;
		}
		#endregion

		#region QtyOnOrders
		public abstract class qtyOnOrders : Data.BQL.BqlDecimal.Field<qtyOnOrders> { }
		[PXDBQuantity(typeof(uOM), typeof(baseQtyOnOrders), BqlField = typeof(SOLine.qtyOnOrders))]
		[PXDefault]
		[PXUnboundFormula(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>>, qtyOnOrders>, decimal0>), typeof(SumCalc<BlanketSOOrder.qtyOnOrders>))]
		public virtual decimal? QtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region BaseQtyOnOrders
		public abstract class baseQtyOnOrders : Data.BQL.BqlDecimal.Field<baseQtyOnOrders> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLine.baseQtyOnOrders))]
		[PXDefault]
		public virtual decimal? BaseQtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region CustomerOrderNbr
		public abstract class customerOrderNbr : Data.BQL.BqlString.Field<customerOrderNbr> { }
		[PXDBString(40, IsUnicode = true, BqlField = typeof(SOLine.customerOrderNbr))]
		public virtual string CustomerOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region SchedOrderDate
		public abstract class schedOrderDate : Data.BQL.BqlDateTime.Field<schedOrderDate> { }
		[PXDBDate(BqlField = typeof(SOLine.schedOrderDate))]
		public virtual DateTime? SchedOrderDate
		{
			get;
			set;
		}
		#endregion
		#region SchedShipDate
		public abstract class schedShipDate : Data.BQL.BqlDateTime.Field<schedShipDate> { }
		[PXDBDate(BqlField = typeof(SOLine.schedShipDate))]
		public virtual DateTime? SchedShipDate
		{
			get;
			set;
		}
		#endregion
		#region TaxZoneID
		public abstract class taxZoneID : Data.BQL.BqlString.Field<taxZoneID> { }
		[PXDBString(10, IsUnicode = true, BqlField = typeof(SOLine.taxZoneID))]
		public virtual string TaxZoneID
		{
			get;
			set;
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[PXDBInt(BqlField = typeof(SOLine.customerID))]
		[PXDefault()]
		public virtual Int32? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region CustomerLocationID
		public abstract class customerLocationID : Data.BQL.BqlInt.Field<customerLocationID> { }
		[PXDBInt(BqlField = typeof(SOLine.customerLocationID))]
		[PXDefault]
		public virtual int? CustomerLocationID
		{
			get;
			set;
		}
		#endregion
		#region ShipVia
		public abstract class shipVia : Data.BQL.BqlString.Field<shipVia> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLine.shipVia))]
		public virtual string ShipVia
		{
			get;
			set;
		}
		#endregion
		#region FOBPoint
		public abstract class fOBPoint : Data.BQL.BqlString.Field<fOBPoint> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLine.fOBPoint))]
		public virtual string FOBPoint
		{
			get;
			set;
		}
		#endregion
		#region ShipTermsID
		public abstract class shipTermsID : Data.BQL.BqlString.Field<shipTermsID>
		{
		}
		[PXDBString(10, IsUnicode = true, BqlField = typeof(SOLine.shipTermsID))]
		public virtual string ShipTermsID
		{
			get;
			set;
		}
		#endregion
		#region ShipZoneID
		public abstract class shipZoneID : Data.BQL.BqlString.Field<shipZoneID> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLine.shipZoneID))]
		public virtual string ShipZoneID
		{
			get;
			set;
		}
		#endregion
		#region BlanketOpenQty
		public abstract class blanketOpenQty : Data.BQL.BqlDecimal.Field<blanketOpenQty> { }
		[PXQuantity]
		[PXDBCalced(typeof(Switch<Case<Where<SOLine.lineType, NotEqual<SOLineType.miscCharge>, And<SOLine.completed, Equal<False>>>, Sub<SOLine.orderQty, SOLine.qtyOnOrders>>, decimal0>), typeof(decimal))]
		[PXFormula(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>, And<completed, Equal<False>>>, Sub<orderQty, qtyOnOrders>>, decimal0>), typeof(SumCalc<BlanketSOOrder.blanketOpenQty>))]
		[PXDefault]
		public virtual decimal? BlanketOpenQty
		{
			get;
			set;
		}
		#endregion
		#region ChildLineCntr
		public abstract class childLineCntr : Data.BQL.BqlInt.Field<childLineCntr> { }
		[PXFormula(null, typeof(SumCalc<BlanketSOOrder.childLineCntr>))]
		[PXDBInt(BqlField = typeof(SOLine.childLineCntr))]
		[PXDefault]
		public virtual int? ChildLineCntr
		{
			get;
			set;
		}
		#endregion
		#region OpenChildLineCntr
		public abstract class openChildLineCntr : Data.BQL.BqlInt.Field<openChildLineCntr> { }
		[PXDBInt(BqlField = typeof(SOLine.openChildLineCntr))]
		[PXDefault]
		public virtual int? OpenChildLineCntr
		{
			get;
			set;
		}
		#endregion

		#region OpenLine
		public abstract class openLine : Data.BQL.BqlBool.Field<openLine> { }
		[PXDBBool(BqlField = typeof(SOLine.openLine))]
		[PXFormula(typeof(Switch<Case<Where<completed, NotEqual<True>, And<orderQty, Greater<decimal0>>>, True>, False>),
			typeof(OpenLineCalc<BlanketSOOrder.openLineCntr>))]
		public virtual bool? OpenLine
		{
			get;
			set;
		}
		#endregion
		#region ShippedQty
		public abstract class shippedQty : Data.BQL.BqlDecimal.Field<shippedQty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseShippedQty), MinValue = 0, BqlField = typeof(SOLine.shippedQty))]
		[PXDefault]
		public virtual decimal? ShippedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseShippedQty
		public abstract class baseShippedQty : Data.BQL.BqlDecimal.Field<baseShippedQty> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLine.baseShippedQty))]
		[PXDefault]
		public virtual decimal? BaseShippedQty
		{
			get;
			set;
		}
		#endregion
		#region ClosedQty
		public abstract class closedQty : Data.BQL.BqlDecimal.Field<closedQty> { }
		[PXDBCalced(typeof(Sub<SOLine.orderQty, SOLine.openQty>), typeof(decimal))]
		[PXQuantity(typeof(uOM), typeof(baseClosedQty))]
		[PXDefault]
		public virtual decimal? ClosedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseClosedQty
		public abstract class baseClosedQty : Data.BQL.BqlDecimal.Field<baseClosedQty> { }
		[PXDBCalced(typeof(Sub<SOLine.baseOrderQty, SOLine.baseOpenQty>), typeof(decimal))]
		[PXQuantity]
		[PXDefault]
		public virtual decimal? BaseClosedQty
		{
			get;
			set;
		}
		#endregion
		#region OpenQty
		public abstract class openQty : Data.BQL.BqlDecimal.Field<openQty> { }
		[PXDBQuantity(typeof(uOM), typeof(baseOpenQty), MinValue = 0, BqlField = typeof(SOLine.openQty))]
		[PXFormula(typeof(Switch<
			Case<Where<completed, NotEqual<True>>,
				Sub<orderQty, closedQty>>,
				decimal0>))]
		[PXUnboundFormula(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>>, openQty>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.openOrderQty>))]
		[PXDefault]
		public virtual decimal? OpenQty
		{
			get;
			set;
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : Data.BQL.BqlDecimal.Field<baseOpenQty> { }
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLine.baseOpenQty))]
		[PXDefault]
		public virtual decimal? BaseOpenQty
		{
			get;
			set;
		}
		#endregion
		#region CuryOpenAmt
		public abstract class curyOpenAmt : Data.BQL.BqlDecimal.Field<curyOpenAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(openAmt), BqlField = typeof(SOLine.curyOpenAmt))]
		[PXFormula(typeof(openQty.When<lineType.IsNotEqual<SOLineType.miscCharge>>.Else<decimal0>
			.Multiply<curyLineAmt.Divide<orderQty.When<orderQty.IsNotEqual<decimal0>>.Else<decimal1>>>))]
		[PXDefault]
		public virtual decimal? CuryOpenAmt
		{
			get;
			set;
		}
		#endregion
		#region OpenAmt
		public abstract class openAmt : Data.BQL.BqlDecimal.Field<openAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.openAmt))]
		[PXDefault]
		public virtual decimal? OpenAmt
		{
			get;
			set;
		}
		#endregion
		#region OrigCuryOpenAmt
		public abstract class origCuryOpenAmt : Data.BQL.BqlDecimal.Field<origCuryOpenAmt> { }
		[PXDecimal]
		[PXDBCalced(typeof(SOLine.curyOpenAmt), typeof(decimal))]
		[PXDefault]
		public virtual decimal? OrigCuryOpenAmt
		{
			get;
			set;
		}
		#endregion

		#region BilledQty
		public abstract class billedQty : Data.BQL.BqlDecimal.Field<billedQty> { }
		[PXDBDecimal(6, BqlField = typeof(SOLine.billedQty))]
		[PXDefault]
		public virtual decimal? BilledQty
		{
			get;
			set;
		}
		#endregion
		#region BaseBilledQty
		public abstract class baseBilledQty : Data.BQL.BqlDecimal.Field<baseBilledQty> { }
		[PXDBBaseQuantity(typeof(uOM), typeof(billedQty), BqlField = typeof(SOLine.baseBilledQty))]
		[PXDefault]
		public virtual decimal? BaseBilledQty
		{
			get;
			set;
		}
		#endregion
		#region UnbilledQty
		public abstract class unbilledQty : Data.BQL.BqlDecimal.Field<unbilledQty> { }
		[PXDBQuantity(BqlField = typeof(SOLine.unbilledQty))]
		[PXDefault]
		public virtual decimal? UnbilledQty
		{
			get;
			set;
		}
		#endregion
		#region BaseUnbilledQty
		public abstract class baseUnbilledQty : Data.BQL.BqlDecimal.Field<baseUnbilledQty> { }
		[PXDBBaseQuantity(typeof(uOM), typeof(unbilledQty), BqlField = typeof(SOLine.baseUnbilledQty))]
		[PXDefault]
		public virtual decimal? BaseUnbilledQty
		{
			get;
			set;
		}
		#endregion
		#region CuryBilledAmt
		public abstract class curyBilledAmt : Data.BQL.BqlDecimal.Field<curyBilledAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(billedAmt), BqlField = typeof(SOLine.curyBilledAmt))]
		[PXFormula(typeof(Mult<Mult<billedQty, curyUnitPrice>, Sub<decimal1, Div<discPct, decimal100>>>))]
		[PXDefault]
		public virtual decimal? CuryBilledAmt
		{
			get;
			set;
		}
		#endregion
		#region BilledAmt
		public abstract class billedAmt : Data.BQL.BqlDecimal.Field<billedAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.billedAmt))]
		[PXDefault]
		public virtual decimal? BilledAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryUnbilledAmt
		public abstract class curyUnbilledAmt : Data.BQL.BqlDecimal.Field<curyUnbilledAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(unbilledAmt), BqlField = typeof(SOLine.curyUnbilledAmt))]
		[PXFormula(typeof(Mult<Mult<unbilledQty, curyUnitPrice>, Sub<decimal1, Div<discPct, decimal100>>>))]
		[PXDefault]
		public virtual decimal? CuryUnbilledAmt
		{
			get;
			set;
		}
		#endregion
		#region UnbilledAmt
		public abstract class unbilledAmt : Data.BQL.BqlDecimal.Field<unbilledAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOLine.unbilledAmt))]
		[PXDefault]
		public virtual decimal? UnbilledAmt
		{
			get;
			set;
		}
		#endregion

		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXNote(BqlField = typeof(SOLine.noteID))]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp(BqlField = typeof(SOLine.Tstamp), RecordComesFirst = true)]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(SOLine.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(SOLine.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(SOLine.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}
}
