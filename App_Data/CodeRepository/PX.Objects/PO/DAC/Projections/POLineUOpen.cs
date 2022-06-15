using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;

namespace PX.Objects.PO
{
	[PXProjection(typeof(Select<POLine>), Persistent = true)]
	[Serializable]
	public partial class POLineUOpen : IBqlTable, IItemPlanMaster, ISortOrder, ICommitmentSource
	{
		#region Keys
		public class PK : PrimaryKeyOf<POLineUOpen>.By<orderType, orderNbr, lineNbr>
		{
			public static POLineUOpen Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr) => FindBy(graph, orderType, orderNbr, lineNbr);
		}
		public static class FK
		{
			public class Order : POOrder.PK.ForeignKeyOf<POLineUOpen>.By<orderType, orderNbr> { }
			public class BlanketOrder : POOrder.PK.ForeignKeyOf<POLineUOpen>.By<pOType, pONbr> { }
			public class BlanketOrderLine : POLineUOpen.PK.ForeignKeyOf<POLineUOpen>.By<pOType, pONbr, pOLineNbr> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<POLineUOpen>.By<inventoryID> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		protected Int32? _BranchID;
		[Branch(typeof(POOrder.branchID), BqlField = typeof(POLine.branchID))]
		public virtual Int32? BranchID
		{
			get
			{
				return this._BranchID;
			}
			set
			{
				this._BranchID = value;
			}
		}
		#endregion
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		protected String _OrderType;
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(POLine.orderType))]
		[PXDefault()]
		[PXUIField(DisplayName = "Order Type", Visibility = PXUIVisibility.Visible, Visible = false)]
		public virtual String OrderType
		{
			get
			{
				return this._OrderType;
			}
			set
			{
				this._OrderType = value;
			}
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		protected String _OrderNbr;

		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POLine.orderNbr))]
		[PXDefault()]
		[PXParent(typeof(FK.Order))]
		[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.Invisible, Visible = false)]
		public virtual String OrderNbr
		{
			get
			{
				return this._OrderNbr;
			}
			set
			{
				this._OrderNbr = value;
			}
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;
		[PXDBInt(IsKey = true, BqlField = typeof(POLine.lineNbr))]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
		public virtual Int32? LineNbr
		{
			get
			{
				return this._LineNbr;
			}
			set
			{
				this._LineNbr = value;
			}
		}
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		protected Int32? _SortOrder;
		[PXDBInt(BqlField = typeof(POLine.sortOrder))]
		public virtual Int32? SortOrder
		{
			get
			{
				return this._SortOrder;
			}
			set
			{
				this._SortOrder = value;
			}
		}
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }
		protected String _LineType;
		[PXDBString(2, IsFixed = true, BqlField = typeof(POLine.lineType))]
		[PXUIField(DisplayName = "Line Type")]
		public virtual String LineType
		{
			get
			{
				return this._LineType;
			}
			set
			{
				this._LineType = value;
			}
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		protected Int64? _CuryInfoID;
		[PXDBLong(BqlField = typeof(POLine.curyInfoID))]
		public virtual Int64? CuryInfoID
		{
			get
			{
				return this._CuryInfoID;
			}
			set
			{
				this._CuryInfoID = value;
			}
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;
		[INUnit(typeof(POLineUOpen.inventoryID), DisplayName = "UOM", BqlField = typeof(POLine.uOM))]
		public virtual String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region PlanID
		public abstract class planID : PX.Data.BQL.BqlLong.Field<planID> { }
		protected Int64? _PlanID;
		[PXDBLong(BqlField = typeof(POLine.planID), IsImmutable = true)]
		public virtual Int64? PlanID
		{
			get
			{
				return this._PlanID;
			}
			set
			{
				this._PlanID = value;
			}
		}
		#endregion
		#region ClearPlanID
		public abstract class clearPlanID : Data.BQL.BqlBool.Field<clearPlanID> { }
		[PXBool]
		public virtual bool? ClearPlanID
		{
			get;
			set;
		}
		#endregion
		#region Completed
		public abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }
		[PXDBBool(BqlField = typeof(POLine.completed))]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<lineType, NotEqual<POLineType.description>, And<completed, Equal<False>>>, int1>, int0>),
			typeof(SumCalc<POOrder.linesToCompleteCntr>))]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region Closed
		public abstract class closed : PX.Data.BQL.BqlBool.Field<closed> { }
		[PXDBBool(BqlField = typeof(POLine.closed))]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<lineType, NotEqual<POLineType.description>, And<closed, Equal<False>>>, int1>, int0>),
			typeof(SumCalc<POOrder.linesToCloseCntr>))]
		public virtual bool? Closed
		{
			get;
			set;
		}
		#endregion
		#region Cancelled
		public abstract class cancelled : PX.Data.BQL.BqlBool.Field<cancelled> { }
		protected Boolean? _Cancelled;
		[PXDBBool(BqlField = typeof(POLine.cancelled))]
		[PXDefault(false)]
		public virtual Boolean? Cancelled
		{
			get
			{
				return this._Cancelled;
			}
			set
			{
				this._Cancelled = value;
			}
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }
		protected Decimal? _OrderQty;
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseOrderQty), HandleEmptyKey = true, MinValue = 0, BqlField = typeof(POLine.orderQty))]
		public virtual Decimal? OrderQty
		{
			get
			{
				return this._OrderQty;
			}
			set
			{
				this._OrderQty = value;
			}
		}
		#endregion
		#region BaseOrderQty
		public abstract class baseOrderQty : PX.Data.BQL.BqlDecimal.Field<baseOrderQty> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.baseOrderQty))]
		public virtual Decimal? BaseOrderQty
		{
			get;
			set;
		}
		#endregion
		#region OrigOrderQty
		public abstract class origOrderQty : PX.Data.BQL.BqlDecimal.Field<origOrderQty> { }
		[PXDBDecimal(BqlField = typeof(POLine.origOrderQty))]
		public virtual Decimal? OrigOrderQty
		{
			get;
			set;
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }
		protected Decimal? _ReceivedQty;
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseReceivedQty), HandleEmptyKey = true, BqlField = typeof(POLine.receivedQty))]
		public virtual Decimal? ReceivedQty
		{
			get
			{
				return this._ReceivedQty;
			}
			set
			{
				this._ReceivedQty = value;
			}
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }
		protected Decimal? _BaseReceivedQty;
		[PXDBDecimal(6, BqlField = typeof(POLine.baseReceivedQty))]
		public virtual Decimal? BaseReceivedQty
		{
			get
			{
				return this._BaseReceivedQty;
			}
			set
			{
				this._BaseReceivedQty = value;
			}
		}
		#endregion

		#region CompletedQty
		public abstract class completedQty : PX.Data.BQL.BqlDecimal.Field<completedQty> { }
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseCompletedQty), HandleEmptyKey = true, MinValue = 0, BqlField = typeof(POLine.completedQty))]
		public virtual decimal? CompletedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseCompletedQty
		public abstract class baseCompletedQty : PX.Data.BQL.BqlDecimal.Field<baseCompletedQty> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.baseCompletedQty))]
		public virtual decimal? BaseCompletedQty
		{
			get;
			set;
		}
		#endregion
		#region BilledQty
		public abstract class billedQty : PX.Data.BQL.BqlDecimal.Field<billedQty> { }
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseBilledQty), HandleEmptyKey = true, MinValue = 0, BqlField = typeof(POLine.billedQty))]
		public virtual decimal? BilledQty
		{
			get;
			set;
		}
		#endregion
		#region BaseBilledQty
		public abstract class baseBilledQty : PX.Data.BQL.BqlDecimal.Field<baseBilledQty> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.baseBilledQty))]
		public virtual decimal? BaseBilledQty
		{
			get;
			set;
		}
		#endregion
		#region CuryBilledAmt
		public abstract class curyBilledAmt : PX.Data.BQL.BqlDecimal.Field<curyBilledAmt> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.billedAmt), BqlField = typeof(POLine.curyBilledAmt))]
		public virtual decimal? CuryBilledAmt
		{
			get;
			set;
		}
		#endregion
		#region BilledAmt
		public abstract class billedAmt : PX.Data.BQL.BqlDecimal.Field<billedAmt> { }
		[PXDBDecimal(4, BqlField = typeof(POLine.billedAmt))]
		public virtual decimal? BilledAmt
		{
			get;
			set;
		}
		#endregion
		#region OpenQty
		public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseOpenQty), HandleEmptyKey = true, BqlField = typeof(POLine.openQty))]
		[PXFormula(typeof(Switch<Case<Where<POLineUOpen.completed, Equal<True>, Or<POLineUOpen.cancelled, Equal<True>>>, decimal0>,
			Maximum<Sub<POLineUOpen.orderQty, POLineUOpen.completedQty>, decimal0>>),
			typeof(SumCalc<POOrder.openOrderQty>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Open Qty.", Enabled = false)]
		public virtual Decimal? OpenQty
		{
			get;
			set;
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : PX.Data.BQL.BqlDecimal.Field<baseOpenQty> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.baseOpenQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseOpenQty
		{
			get;
			set;
		}
		#endregion
		#region UnbilledQty
		public abstract class unbilledQty : PX.Data.BQL.BqlDecimal.Field<unbilledQty> { }
		[PXDBQuantity(typeof(POLineUOpen.uOM), typeof(POLineUOpen.baseUnbilledQty), HandleEmptyKey = true, BqlField = typeof(POLine.unbilledQty))]
		[PXFormula(typeof(Switch<Case<Where<POLineUOpen.closed, Equal<True>, Or<POLineUOpen.cancelled, Equal<True>>>, decimal0>,
			Maximum<Sub<Maximum<POLineUOpen.orderQty, POLineUOpen.completedQty>, POLineUOpen.billedQty>, decimal0>>),
			typeof(SumCalc<POOrder.unbilledOrderQty>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unbilled Qty.", Enabled = false)]
		public virtual Decimal? UnbilledQty
		{
			get;
			set;
		}
		#endregion
		#region BaseUnbilledQty
		public abstract class baseUnbilledQty : PX.Data.BQL.BqlDecimal.Field<baseUnbilledQty> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.baseUnbilledQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseUnbilledQty
		{
			get;
			set;
		}
		#endregion
		#region OrigExtCost
		public abstract class origExtCost : PX.Data.BQL.BqlDecimal.Field<origExtCost> { }

		[PXDBDecimal(BqlField = typeof(POLine.origExtCost))]
		public virtual Decimal? OrigExtCost
		{
			get;
			set;
		}
		#endregion
		#region CuryExtCost
		public abstract class curyExtCost : PX.Data.BQL.BqlDecimal.Field<curyExtCost> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.extCost), BqlField = typeof(POLine.curyExtCost))]
		public virtual decimal? CuryExtCost
		{
			get;
			set;
		}
		#endregion
		#region ExtCost
		public abstract class extCost : PX.Data.BQL.BqlDecimal.Field<extCost> { }
		protected Decimal? _ExtCost;
		[PXDBBaseCury(BqlField = typeof(POLine.extCost))]
		public virtual Decimal? ExtCost
		{
			get
			{
				return this._ExtCost;
			}
			set
			{
				this._ExtCost = value;
			}
		}
		#endregion
		#region CuryUnbilledAmt
		public abstract class curyUnbilledAmt : PX.Data.BQL.BqlDecimal.Field<curyUnbilledAmt> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.unbilledAmt), BqlField = typeof(POLine.curyUnbilledAmt))]
		[PXFormula(typeof(Switch<Case<Where<POLineUOpen.closed, Equal<True>, Or<POLineUOpen.cancelled, Equal<True>>>, decimal0,
			Case<Where<POLineUOpen.curyLineAmt, GreaterEqual<decimal0>>,
				Maximum<Sub<Sub<POLineUOpen.curyLineAmt, POLineUOpen.curyDiscAmt>, POLineUOpen.curyBilledAmt>, decimal0>>>,
			Minimum<Sub<Sub<POLineUOpen.curyLineAmt, POLineUOpen.curyDiscAmt>, POLineUOpen.curyBilledAmt>, decimal0>>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryUnbilledAmt
		{
			get;
			set;
		}
		#endregion
		#region UnbilledAmt
		public abstract class unbilledAmt : PX.Data.BQL.BqlDecimal.Field<unbilledAmt> { }
		[PXDBDecimal(4, BqlField = typeof(POLine.unbilledAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? UnbilledAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryUnitCost
		public abstract class curyUnitCost : PX.Data.BQL.BqlDecimal.Field<curyUnitCost> { }
		protected Decimal? _CuryUnitCost;

		[PXDBDecimal(4, BqlField = typeof(POLine.curyUnitCost))]
		[PXUIField(DisplayName = "Unit Cost", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryUnitCost
		{
			get
			{
				return this._CuryUnitCost;
			}
			set
			{
				this._CuryUnitCost = value;
			}
		}
		#endregion
		#region GroupDiscountRate
		public abstract class groupDiscountRate : PX.Data.BQL.BqlDecimal.Field<groupDiscountRate> { }
		protected Decimal? _GroupDiscountRate;
		[PXDBDecimal(18, BqlField = typeof(POLine.groupDiscountRate))]
		[PXDefault(TypeCode.Decimal, "1.0")]
		public virtual Decimal? GroupDiscountRate
		{
			get
			{
				return this._GroupDiscountRate;
			}
			set
			{
				this._GroupDiscountRate = value;
			}
		}
		#endregion
		#region DocumentDiscountRate
		public abstract class documentDiscountRate : PX.Data.BQL.BqlDecimal.Field<documentDiscountRate> { }
		protected Decimal? _DocumentDiscountRate;
		[PXDBDecimal(18, BqlField = typeof(POLine.documentDiscountRate))]
		[PXDefault(TypeCode.Decimal, "1.0")]
		public virtual Decimal? DocumentDiscountRate
		{
			get
			{
				return this._DocumentDiscountRate;
			}
			set
			{
				this._DocumentDiscountRate = value;
			}
		}
		#endregion
		#region TaxCategoryID
		public abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }
		protected String _TaxCategoryID;
		[PXDBString(TX.TaxCategory.taxCategoryID.Length, IsUnicode = true, BqlField = typeof(POLine.taxCategoryID))]
		[PXUIField(DisplayName = "Tax Category", Visibility = PXUIVisibility.Visible)]
		[POUnbilledTaxR(typeof(POOrder), typeof(POTax), typeof(POTaxTran),
			   //Per Unit Tax settings
			   Inventory = typeof(POLineUOpen.inventoryID), UOM = typeof(POLineUOpen.uOM), LineQty = typeof(POLineUOpen.unbilledQty))]
		public virtual String TaxCategoryID
		{
			get
			{
				return this._TaxCategoryID;
			}
			set
			{
				this._TaxCategoryID = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[PXDBInt(BqlField = typeof(POLine.inventoryID))]
		public virtual Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region PromisedDate
		public abstract class promisedDate : PX.Data.BQL.BqlDateTime.Field<promisedDate> { }
		protected DateTime? _PromisedDate;
		[PXDBDate(BqlField = typeof(POLine.promisedDate))]
		public virtual DateTime? PromisedDate
		{
			get
			{
				return this._PromisedDate;
			}
			set
			{
				this._PromisedDate = value;
			}
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		protected Int32? _SubItemID;
		[PXDBInt(BqlField = typeof(POLine.subItemID))]
		public virtual Int32? SubItemID
		{
			get
			{
				return this._SubItemID;
			}
			set
			{
				this._SubItemID = value;
			}
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		protected Int32? _SiteID;
		[PXDBInt(BqlField = typeof(POLine.siteID))]
		public virtual Int32? SiteID
		{
			get
			{
				return this._SiteID;
			}
			set
			{
				this._SiteID = value;
			}
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		protected Int32? _VendorID;
		[PXDBInt(BqlField = typeof(POLine.vendorID))]
		public virtual Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region RequestedDate
		public abstract class requestedDate : PX.Data.BQL.BqlDateTime.Field<requestedDate> { }
		protected DateTime? _RequestedDate;
		[PXDBDate(BqlField = typeof(POLine.requestedDate))]
		public virtual DateTime? RequestedDate
		{
			get
			{
				return this._RequestedDate;
			}
			set
			{
				this._RequestedDate = value;
			}
		}
		#endregion
		#region ExpenseAcctID
		public abstract class expenseAcctID : PX.Data.BQL.BqlInt.Field<expenseAcctID> { }
		protected Int32? _ExpenseAcctID;
		[Account(typeof(POLineUOpen.branchID), BqlField = typeof(POLine.expenseAcctID))]
		public virtual Int32? ExpenseAcctID
		{
			get
			{
				return this._ExpenseAcctID;
			}
			set
			{
				this._ExpenseAcctID = value;
			}
		}
		#endregion
		#region ExpenseSubID
		public abstract class expenseSubID : PX.Data.BQL.BqlInt.Field<expenseSubID> { }
		[PXDBInt(BqlField = typeof(POLine.expenseSubID))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? ExpenseSubID
		{
			get;
			set;
		}
		#endregion
		#region POAccrualAcctID
		public abstract class pOAccrualAcctID : PX.Data.BQL.BqlInt.Field<pOAccrualAcctID> { }
		[Account(typeof(POLineUOpen.branchID), BqlField = typeof(POLine.pOAccrualAcctID))]
		public virtual int? POAccrualAcctID
		{
			get;
			set;
		}
		#endregion
		#region POAccrualSubID
		public abstract class pOAccrualSubID : PX.Data.BQL.BqlInt.Field<pOAccrualSubID> { }
		[SubAccount(typeof(POLineUOpen.pOAccrualAcctID), typeof(POLineUOpen.branchID), BqlField = typeof(POLine.pOAccrualSubID))]
		public virtual int? POAccrualSubID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;
		[PXDBInt(BqlField = typeof(POLine.projectID))]
		public virtual Int32? ProjectID
		{
			get
			{
				return this._ProjectID;
			}
			set
			{
				this._ProjectID = value;
			}
		}
		#endregion
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		protected Int32? _TaskID;
		[PXDBInt(BqlField = typeof(POLine.taskID))]
		public virtual Int32? TaskID
		{
			get
			{
				return this._TaskID;
			}
			set
			{
				this._TaskID = value;
			}
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		protected Int32? _CostCodeID;
		[PXDBInt(BqlField = typeof(POLine.costCodeID))]
		public virtual Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region CommitmentID
		public abstract class commitmentID : PX.Data.BQL.BqlGuid.Field<commitmentID> { }
		protected Guid? _CommitmentID;
		[POCommitment]
		[PXDBGuid(BqlField = typeof(POLine.commitmentID))]
		public virtual Guid? CommitmentID
		{
			get
			{
				return this._CommitmentID;
			}
			set
			{
				this._CommitmentID = value;
			}
		}
		#endregion
		#region CuryLineAmt
		public abstract class curyLineAmt : PX.Data.BQL.BqlDecimal.Field<curyLineAmt> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.lineAmt), BqlField = typeof(POLine.curyLineAmt))]
		public virtual decimal? CuryLineAmt
		{
			get;
			set;
		}
		#endregion
		#region LineAmt
		public abstract class lineAmt : PX.Data.BQL.BqlDecimal.Field<lineAmt> { }
		[PXDBBaseCury(BqlField = typeof(POLine.lineAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? LineAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscAmt
		public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.discAmt), BqlField = typeof(POLine.curyDiscAmt))]
		public virtual decimal? CuryDiscAmt
		{
			get;
			set;
		}
		#endregion
		#region DiscAmt
		public abstract class discAmt : PX.Data.BQL.BqlDecimal.Field<discAmt> { }
		[PXDBBaseCury(BqlField = typeof(POLine.discAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? DiscAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryRetainageAmt
		public abstract class curyRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageAmt> { }
		[PXDBCurrency(typeof(POLineUOpen.curyInfoID), typeof(POLineUOpen.retainageAmt), BqlField = typeof(POLine.curyRetainageAmt))]
		public virtual decimal? CuryRetainageAmt
		{
			get;
			set;
		}
		#endregion
		#region RetainageAmt
		public abstract class retainageAmt : PX.Data.BQL.BqlDecimal.Field<retainageAmt> { }
		[PXDBBaseCury(BqlField = typeof(POLine.retainageAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? RetainageAmt
		{
			get;
			set;
		}
		#endregion
		#region ReqPrepaidQty
		public abstract class reqPrepaidQty : Data.BQL.BqlDecimal.Field<reqPrepaidQty>
		{
		}
		[PXDBQuantity(typeof(uOM), typeof(baseReqPrepaidQty), HandleEmptyKey = true, MinValue = 0, BqlField = typeof(POLine.reqPrepaidQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? ReqPrepaidQty
		{
			get;
			set;
		}
		#endregion
		#region BaseReqPrepaidQty
		public abstract class baseReqPrepaidQty : Data.BQL.BqlDecimal.Field<baseReqPrepaidQty>
		{
		}
		[PXDBDecimal(6, BqlField = typeof(POLine.baseReqPrepaidQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? BaseReqPrepaidQty
		{
			get;
			set;
		}
		#endregion
		#region CuryReqPrepaidAmt
		public abstract class curyReqPrepaidAmt : Data.BQL.BqlDecimal.Field<curyReqPrepaidAmt>
		{
		}
		[PXDBCurrency(typeof(curyInfoID), typeof(reqPrepaidAmt), BqlField = typeof(POLine.curyReqPrepaidAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryReqPrepaidAmt
		{
			get;
			set;
		}
		#endregion
		#region ReqPrepaidAmt
		public abstract class reqPrepaidAmt : Data.BQL.BqlDecimal.Field<reqPrepaidAmt>
		{
		}
		[PXDBDecimal(4, BqlField = typeof(POLine.reqPrepaidAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? ReqPrepaidAmt
		{
			get;
			set;
		}
		#endregion

		#region AllowComplete
		public abstract class allowComplete : PX.Data.BQL.BqlBool.Field<allowComplete> { }
		protected Boolean? _AllowComplete;
		[PXDBBool(BqlField = typeof(POLine.allowComplete))]
		[PXUIField(DisplayName = "Allow Complete", Visibility = PXUIVisibility.Visible)]
		[PXDefault(false)]
		public virtual Boolean? AllowComplete
		{
			get
			{
				return this._AllowComplete;
			}
			set
			{
				this._AllowComplete = value;
			}
		}
		#endregion
		#region CompletePOLine
		public abstract class completePOLine : PX.Data.BQL.BqlString.Field<completePOLine> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(POLine.completePOLine))]
		[PXDefault]
		[CompletePOLineTypes.List]
		public virtual String CompletePOLine
		{
			get;
			set;
		}
		#endregion
		#region RcptQtyThreshold
		public abstract class rcptQtyThreshold : PX.Data.BQL.BqlDecimal.Field<rcptQtyThreshold> { }
		[PXDBDecimal(2, BqlField = typeof(POLine.rcptQtyThreshold))]
		public virtual Decimal? RcptQtyThreshold
		{
			get;
			set;
		}
		#endregion
		#region POAccrualType
		public abstract class pOAccrualType : PX.Data.BQL.BqlString.Field<pOAccrualType> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(POLine.pOAccrualType))]
		[POAccrualType.List]
		public virtual string POAccrualType
		{
			get;
			set;
		}
		#endregion

		#region POType
		public abstract class pOType : PX.Data.BQL.BqlString.Field<pOType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(POLine.pOType))]
		[POOrderType.List()]
		public virtual String POType
		{
			get;
			set;
		}
		#endregion
		#region PONbr
		public abstract class pONbr : PX.Data.BQL.BqlString.Field<pONbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(POLine.pONbr))]
		public virtual String PONbr
		{
			get;
			set;
		}
		#endregion
		#region POLineNbr
		public abstract class pOLineNbr : PX.Data.BQL.BqlInt.Field<pOLineNbr> { }
		[PXDBInt(BqlField = typeof(POLine.pOLineNbr))]
		public virtual Int32? POLineNbr
		{
			get;
			set;
		}
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp(BqlField = typeof(POLine.Tstamp), RecordComesFirst = true)]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(POLine.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(POLine.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(POLine.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion

		public POLine ToPOLine()
		{
			return new POLine
			{
				OrderType = this.OrderType,
				OrderNbr = this.OrderNbr,
				LineNbr = this.LineNbr,
				CuryInfoID = this.CuryInfoID,
			};
		}
	}
}
