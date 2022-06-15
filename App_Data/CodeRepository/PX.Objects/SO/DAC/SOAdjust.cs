namespace PX.Objects.SO
{
	using System;
	using PX.Data;
	using PX.Data.WorkflowAPI;
	using PX.Data.ReferentialIntegrity.Attributes;
	using PX.Objects.AR;
	using PX.Objects.CA;
    using PX.Objects.CM.Extensions;
	using PX.Objects.Common.Attributes;
	using PX.Objects.CS;
	using PX.Objects.GL;
	using PX.Objects.SO.Interfaces;
	using PX.Objects.Common;
	using PX.Objects.SO.DAC.Projections;

	[PXCacheName(Messages.SOAdjust)]
	public partial class SOAdjust : PX.Data.IBqlTable, IAdjustment, ICreatePaymentAdjust
	{
		#region Keys
		public class PK : PrimaryKeyOf<SOAdjust>.By<recordID, adjdOrderType, adjdOrderNbr, adjgDocType, adjgRefNbr>
		{
			public static SOAdjust Find(PXGraph graph, int recordID, string adjdOrderType, string adjdOrderNbr, string adjgDocType, string adjgRefNbr)
				=> FindBy(graph, recordID, adjdOrderType, adjdOrderNbr, adjgDocType, adjgRefNbr);
		}
		public static class FK
		{
			public class Order : SOOrder.PK.ForeignKeyOf<SOAdjust>.By<adjdOrderType, adjdOrderNbr> { }
			public class Customer : AR.Customer.PK.ForeignKeyOf<SOAdjust>.By<customerID> { }
			public class AdjustingPayment : ARPayment.PK.ForeignKeyOf<SOAdjust>.By<adjgDocType, adjgRefNbr> { }
			public class CurrencyInfoOfAdjustingPayment : CM.CurrencyInfo.PK.ForeignKeyOf<SOAdjust>.By<adjgCuryInfoID> { }

			public class AdjustedOrderType : SOOrderType.PK.ForeignKeyOf<SOAdjust>.By<adjdOrderType> { }
			public class AdjustedOrder : SOOrder.PK.ForeignKeyOf<SOAdjust>.By<adjgDocType, adjgRefNbr> { }
			public class CurrencyInfoOfAdjustedOrder : CM.CurrencyInfo.PK.ForeignKeyOf<SOAdjust>.By<adjdCuryInfoID> { }
			public class OriginalCurrencyInfoOfAdjustedOrder : CM.CurrencyInfo.PK.ForeignKeyOf<SOAdjust>.By<adjdOrigCuryInfoID> { }

			public class PaymentMethod : CA.PaymentMethod.PK.ForeignKeyOf<SOAdjust>.By<paymentMethodID> { }
			public class CustomerPaymentMethod : AR.CustomerPaymentMethod.PK.ForeignKeyOf<SOAdjust>.By<pMInstanceID> { }
			public class CashAccount : GL.Account.PK.ForeignKeyOf<SOAdjust>.By<cashAccountID> { }
			public class ProcessingCenter : CCProcessingCenter.PK.ForeignKeyOf<SOAdjust>.By<processingCenterID> { }

			public class BlanketOrder : BlanketSOOrder.PK.ForeignKeyOf<SOAdjust>.By<blanketType, blanketNbr> { }
			public class BlanketAdjustment : BlanketSOAdjust.PK.ForeignKeyOf<SOAdjust>.By<blanketRecordID, blanketType, blanketNbr, adjgDocType, adjgRefNbr> { }

			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use AdjustingPayment instead.")]
			public class Payment : AdjustingPayment { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		protected Int32? _RecordID;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Consider removing IsKey=true from RecordID field]
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID
		{
			get
			{
				return this._RecordID;
			}
			set
			{
				this._RecordID = value;
			}
		}
		#endregion
		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		protected Boolean? _Hold;
		[PXDBBool()]
		public virtual Boolean? Hold
		{
			get
			{
				return this._Hold;
			}
			set
			{
				this._Hold = value;
			}
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		protected Int32? _CustomerID;
		[PXDBInt()]
		[PXDefault(typeof(ARPayment.customerID))]
		public virtual Int32? CustomerID
		{
			get
			{
				return this._CustomerID;
			}
			set
			{
				this._CustomerID = value;
			}
		}
		#endregion
		#region AdjgDocType
		public abstract class adjgDocType : PX.Data.BQL.BqlString.Field<adjgDocType> { }
		protected string _AdjgDocType;
		public const int AdjgDocTypeLength = 3;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Consider removing IsKey=true from RecordID field]
		[PXDBString(AdjgDocTypeLength, IsKey = true, IsFixed = true, InputMask = "")]
		[ARPaymentType.List()]
		[PXDefault(typeof(ARPayment.docType))]
		[PXUIField(DisplayName = "Doc. Type", Enabled = false, Visible = false)]
		public virtual String AdjgDocType
		{
			get
			{
				return this._AdjgDocType;
			}
			set
			{
				this._AdjgDocType = value;
			}
		}
		#endregion
		#region AdjgRefNbr
		public abstract class adjgRefNbr : PX.Data.BQL.BqlString.Field<adjgRefNbr> { }
		protected string _AdjgRefNbr;
		public const int AdjgRefNbrLength = 15;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Consider removing IsKey=true from RecordID field]
		[PXDBString(AdjgRefNbrLength, IsKey = true, IsUnicode = true)]
		[PXDBDefault(typeof(ARPayment.refNbr), DefaultForUpdate = false)]
		[PXUIField(DisplayName = "Reference Nbr.", Enabled = false, Visible = false)]
		[PXParent(typeof(Select<ARPayment, Where<ARPayment.docType, Equal<Current<SOAdjust.adjgDocType>>, And<ARPayment.refNbr, Equal<Current<SOAdjust.adjgRefNbr>>>>>))]
		[PXParent(typeof(Select<ARPaymentTotals,
			Where<ARPaymentTotals.docType, Equal<Current<SOAdjust.adjgDocType>>,
				And<ARPaymentTotals.refNbr, Equal<Current<SOAdjust.adjgRefNbr>>>>>), ParentCreate = true)]
		public virtual String AdjgRefNbr
		{
			get
			{
				return this._AdjgRefNbr;
			}
			set
			{
				this._AdjgRefNbr = value;
			}
		}
		#endregion
		#region AdjdOrderType
		public abstract class adjdOrderType : PX.Data.BQL.BqlString.Field<adjdOrderType> { }
		protected String _AdjdOrderType;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Consider removing IsKey=true from RecordID field]
		[PXDBString(2, IsKey = true, IsFixed = true, InputMask = ">aa")]
		[PXDefault(typeof(Switch<Case<Where<Current<adjgDocType>, Equal<ARPaymentType.refund>>,
			SOOrderTypeConstants.creditMemo>,
			SOOrderTypeConstants.salesOrder>))]
		[PXUIField(DisplayName = "Order Type")]
		[PXSelector(typeof(Search<SOOrderType.orderType,
			Where<SOOrderType.active, Equal<True>,
				And<Where<Current<adjgDocType>, NotEqual<ARPaymentType.refund>, And<SOOrderType.canHavePayments, Equal<True>,
					Or<Current<adjgDocType>, Equal<ARPaymentType.refund>, And<SOOrderType.canHaveRefunds, Equal<True>>>>>>>>))]
		public virtual String AdjdOrderType
		{
			get
			{
				return this._AdjdOrderType;
			}
			set
			{
				this._AdjdOrderType = value;
			}
		}
		#endregion
		#region AdjdOrderNbr
		public abstract class adjdOrderNbr : PX.Data.BQL.BqlString.Field<adjdOrderNbr> { }
		protected String _AdjdOrderNbr;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Consider removing IsKey=true from RecordID field]
        [PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault()]
		[PXUIField(DisplayName = "Order Nbr.")]
		[PXParent(typeof(FK.Order))]
		[PXUnboundFormula(typeof(Switch<Case<Where<SOAdjust.curyAdjdAmt, Greater<decimal0>>, int1>, int0>), typeof(SumCalc<SOOrder.paymentCntr>))]
		[PXRestrictor(typeof(Where<SOOrder.status, NotIn3<SOOrderStatus.cancelled, SOOrderStatus.pendingApproval, SOOrderStatus.voided>>),
			Messages.DontApprovedDocumentsCannotBeSelected)]
		[PXRestrictor(typeof(Where<SOOrder.hasLegacyCCTran, NotEqual<True>>),
			Messages.CantProcessSOBecauseItHasLegacyCCTran, typeof(SOOrder.orderType), typeof(SOOrder.orderNbr))]
        [PXSelector(typeof(Search2<SOOrder.orderNbr, 
			InnerJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrder.orderType>>,
			LeftJoin<Terms, On<Terms.termsID, Equal<SOOrder.termsID>>>>,
		    Where2<Where<SOOrder.customerID, Equal<Current<customerID>>, Or<SOOrder.customerID, In2<Search<Customer.bAccountID, Where<Customer.consolidatingBAccountID, Equal<Current<customerID>>>>>>>,
			  And<SOOrder.orderType, Equal<Optional<adjdOrderType>>,
			  And<SOOrder.openDoc, Equal<boolTrue>, 
			  And2<Where<SOOrder.behavior, NotEqual<SOBehavior.bL>, Or<
				  Where<SOOrder.hold, NotEqual<True>,
					And<SOOrder.isExpired, NotEqual<True>,
					And<SOOrder.childLineCntr, Equal<int0>>>>>>,
			  And<Where<Terms.termsID, IsNull, Or<Terms.installmentType, NotEqual<TermsInstallmentType.multiple>>>>>>>>>),
				typeof(SOOrder.orderNbr),
				typeof(SOOrder.orderDate),
				typeof(SOOrder.finPeriodID),
				typeof(SOOrder.customerLocationID),
				typeof(SOOrder.curyID),
				typeof(SOOrder.curyOrderTotal),
				typeof(SOOrder.curyOpenOrderTotal),
				typeof(SOOrder.status),
				typeof(SOOrder.dueDate),
				typeof(SOOrder.invoiceNbr),
				typeof(SOOrder.orderDesc),
				Filterable = true)]
		public virtual String AdjdOrderNbr
		{
			get
			{
				return this._AdjdOrderNbr;
			}
			set
			{
				this._AdjdOrderNbr = value;
			}
		}
        #endregion
        #region CuryAdjgAmt
        public abstract class curyAdjgAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgAmt> { }
		protected Decimal? _CuryAdjgAmt;
		[PXDBCurrency(typeof(SOAdjust.adjgCuryInfoID), typeof(SOAdjust.adjAmt))]
		[PXFormula(typeof(Switch<Case<Where<Maximum<Sub<SOAdjust.curyOrigAdjgAmt, SOAdjust.curyAdjgBilledAmt>, decimal0>, Greater<Current<SOAdjust.curyDocBal>>>, Current<SOAdjust.curyDocBal>>, Maximum<Sub<SOAdjust.curyOrigAdjgAmt, SOAdjust.curyAdjgBilledAmt>, decimal0>>), typeof(SumCalc<ARPayment.curySOApplAmt>))]
		[PXUIField(DisplayName = "Applied To Order")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryAdjgAmt
		{
			get
			{
				return this._CuryAdjgAmt;
			}
			set
			{
				this._CuryAdjgAmt = value;
			}
		}
		#endregion
		#region AdjAmt
		public abstract class adjAmt : PX.Data.BQL.BqlDecimal.Field<adjAmt> { }
		protected Decimal? _AdjAmt;
		[PXDBDecimal(4)]
		[PXFormula(typeof(Maximum<Sub<SOAdjust.origAdjAmt, SOAdjust.adjBilledAmt>, decimal0>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? AdjAmt
		{
			get
			{
				return this._AdjAmt;
			}
			set
			{
				this._AdjAmt = value;
			}
		}
		#endregion
		#region CuryAdjdAmt
		public abstract class curyAdjdAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdAmt> { }
		protected Decimal? _CuryAdjdAmt;
		[PXDBDecimal(4)]
		[PXFormula(typeof(Maximum<Sub<SOAdjust.curyOrigAdjdAmt, SOAdjust.curyAdjdBilledAmt>, decimal0>))]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<False>, And<isCCAuthorized, Equal<False>>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<SOOrder.curyUnreleasedPaymentAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<False>, And<isCCAuthorized, Equal<True>>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<SOOrder.curyCCAuthorizedAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<True>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<SOOrder.curyPaidAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<SOOrder.curyPaymentTotal>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(IIf<Where<voided, Equal<False>,
				And<Where<isCCPayment, Equal<False>,
					Or<isCCAuthorized, Equal<True>,
					Or<isCCCaptured, Equal<True>,
					Or<paymentReleased, Equal<True>>>>>>>,
				Add<curyAdjdAmt, curyAdjdBilledAmt>, decimal0>),
			typeof(SumCalc<SOOrder.curyPaymentOverall>),
			ForceAggregateRecalculation = true)]
		[CopyChildLink(typeof(ARPaymentTotals.orderCntr), typeof(curyAdjdAmt),
			new Type[] { typeof(adjdOrderType), typeof(adjdOrderNbr) },
			new Type[] { typeof(ARPaymentTotals.adjdOrderType), typeof(ARPaymentTotals.adjdOrderNbr) })]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryAdjdAmt
		{
			get
			{
				return this._CuryAdjdAmt;
			}
			set
			{
				this._CuryAdjdAmt = value;
			}
		}
		#endregion

		/**/
		#region CuryOrigAdjdAmt
		public abstract class curyOrigAdjdAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigAdjdAmt> { }
		protected Decimal? _CuryOrigAdjdAmt;
		[PXDBCalced(typeof(Add<SOAdjust.curyAdjdAmt, SOAdjust.curyAdjdBilledAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryOrigAdjdAmt
		{
			get
			{
				return this._CuryOrigAdjdAmt;
			}
			set
			{
				this._CuryOrigAdjdAmt = value;
			}
		}
		#endregion
		#region OrigAdjAmt
		public abstract class origAdjAmt : PX.Data.BQL.BqlDecimal.Field<origAdjAmt> { }
		protected Decimal? _OrigAdjAmt;
		[PXDBCalced(typeof(Add<SOAdjust.adjAmt, SOAdjust.adjBilledAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? OrigAdjAmt
		{
			get
			{
				return this._OrigAdjAmt;
			}
			set
			{
				this._OrigAdjAmt = value;
			}
		}
		#endregion
		#region CuryOrigAdjgAmt
		public abstract class curyOrigAdjgAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigAdjgAmt> { }
		protected Decimal? _CuryOrigAdjgAmt;
		[PXDBCalced(typeof(Add<SOAdjust.curyAdjgAmt, SOAdjust.curyAdjgBilledAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryOrigAdjgAmt
		{
			get
			{
				return this._CuryOrigAdjgAmt;
			}
			set
			{
				this._CuryOrigAdjgAmt = value;
			}
		}
		#endregion
		#region CuryAdjdOrigBlanketAmt
		public abstract class curyAdjdOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<SOAdjust.curyAdjdAmt, SOAdjust.curyAdjdTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? CuryAdjdOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion
		#region AdjOrigBlanketAmt
		public abstract class adjOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<adjOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<SOAdjust.adjAmt, SOAdjust.adjTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? AdjOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjgOrigBlanketAmt
		public abstract class curyAdjgOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<SOAdjust.curyAdjgAmt, SOAdjust.curyAdjgTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? CuryAdjgOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion
		/**/


		#region CuryAdjgDiscAmt
		public abstract class curyAdjgDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgDiscAmt> { }
		[PXDecimal(4)]
		public decimal? CuryAdjgDiscAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}
		#endregion
		#region CuryAdjdDiscAmt
		public abstract class curyAdjdDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdDiscAmt> { }
		[PXDecimal(4)]
		public decimal? CuryAdjdDiscAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}
		#endregion
		#region AdjDiscAmt
		public abstract class adjDiscAmt : PX.Data.BQL.BqlDecimal.Field<adjDiscAmt> { }
		[PXDecimal(4)]
		public decimal? AdjDiscAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}
		#endregion
		#region AdjdOrigCuryInfoID
		public abstract class adjdOrigCuryInfoID : PX.Data.BQL.BqlLong.Field<adjdOrigCuryInfoID> { }
		protected Int64? _AdjdOrigCuryInfoID;
		[PXDBLong]
		[PXDefault]
		[CurrencyInfo(typeof(AR.ARInvoice.curyInfoID))]
		public virtual Int64? AdjdOrigCuryInfoID
		{
			get
			{
				return this._AdjdOrigCuryInfoID;
			}
			set
			{
				this._AdjdOrigCuryInfoID = value;
			}
		}
		#endregion
		#region AdjgCuryInfoID
		public abstract class adjgCuryInfoID : PX.Data.BQL.BqlLong.Field<adjgCuryInfoID> { }
		protected Int64? _AdjgCuryInfoID;
		[PXDBLong()]
		[CurrencyInfo(typeof(ARPayment.curyInfoID))]//, CuryIDField = "AdjgCuryID")]
		public virtual Int64? AdjgCuryInfoID
		{
			get
			{
				return this._AdjgCuryInfoID;
			}
			set
			{
				this._AdjgCuryInfoID = value;
			}
		}
		#endregion
		#region AdjdCuryInfoID
		public abstract class adjdCuryInfoID : PX.Data.BQL.BqlLong.Field<adjdCuryInfoID> { }
		protected Int64? _AdjdCuryInfoID;
		[PXDBLong()]
		[PXDefault()]
		[CurrencyInfo(typeof(ARPayment.curyInfoID))] //(ModuleCode = GL.BatchModule.SO, CuryIDField = "AdjdCuryID")]
		public virtual Int64? AdjdCuryInfoID
		{
			get
			{
				return this._AdjdCuryInfoID;
			}
			set
			{
				this._AdjdCuryInfoID = value;
			}
		}
		#endregion
		#region AdjgDocDate
		public abstract class adjgDocDate : PX.Data.BQL.BqlDateTime.Field<adjgDocDate> { }
		protected DateTime? _AdjgDocDate;
		[PXDBDate()]
		[PXUIField(DisplayName = "Application Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? AdjgDocDate
		{
			get
			{
				return this._AdjgDocDate;
			}
			set
			{
				this._AdjgDocDate = value;
			}
		}
		#endregion
		#region AdjdOrderDate
		public abstract class adjdOrderDate : PX.Data.BQL.BqlDateTime.Field<adjdOrderDate> { }
		protected DateTime? _AdjdOrderDate;
		[PXDBDate()]
		[PXDefault()]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.Visible, Enabled = false)]
		public virtual DateTime? AdjdOrderDate
		{
			get
			{
				return this._AdjdOrderDate;
			}
			set
			{
				this._AdjdOrderDate = value;
			}
		}
		#endregion

		#region CuryAdjgBilledAmt
		public abstract class curyAdjgBilledAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgBilledAmt> { }
		protected Decimal? _CuryAdjgBilledAmt;
		[PXDBCurrency(typeof(SOAdjust.adjgCuryInfoID), typeof(SOAdjust.adjBilledAmt), BaseCalc = false)]
		[PXUIField(DisplayName = "Transferred to Invoice", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryAdjgBilledAmt
		{
			get
			{
				return this._CuryAdjgBilledAmt;
			}
			set
			{
				this._CuryAdjgBilledAmt = value;
			}
		}
		#endregion
		#region AdjBilledAmt
		public abstract class adjBilledAmt : PX.Data.BQL.BqlDecimal.Field<adjBilledAmt> { }
		protected Decimal? _AdjBilledAmt;
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? AdjBilledAmt
		{
			get
			{
				return this._AdjBilledAmt;
			}
			set
			{
				this._AdjBilledAmt = value;
			}
		}
		#endregion
		#region CuryAdjdBilledAmt
		public abstract class curyAdjdBilledAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdBilledAmt> { }
		protected Decimal? _CuryAdjdBilledAmt;
		[PXDBCurrency(typeof(SOAdjust.adjdCuryInfoID), typeof(SOAdjust.adjBilledAmt), BaseCalc = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Transferred to Invoice", Enabled = false)]
		[PXUnboundFormula(
			typeof(IIf<Where<voided, Equal<False>>, curyAdjdBilledAmt, decimal0>),
			typeof(SumCalc<SOOrder.curyBilledPaymentTotal>),
			ForceAggregateRecalculation = true)]
		public virtual Decimal? CuryAdjdBilledAmt
		{
			get
			{
				return this._CuryAdjdBilledAmt;
			}
			set
			{
				this._CuryAdjdBilledAmt = value;
			}
		}
		#endregion

		#region CuryAdjgTransferredToChildrenAmt
		public abstract class curyAdjgTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgTransferredToChildrenAmt> { }

		[PXDBCurrency(typeof(SOAdjust.adjgCuryInfoID), typeof(SOAdjust.adjTransferredToChildrenAmt), BaseCalc = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Transferred to Child Orders", Enabled = false)]
		public virtual decimal? CuryAdjgTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion
		#region AdjTransferredToChildrenAmt
		public abstract class adjTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<adjTransferredToChildrenAmt> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? AdjTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjdTransferredToChildrenAmt
		public abstract class curyAdjdTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdTransferredToChildrenAmt> { }

		[PXDBCurrency(typeof(SOAdjust.adjdCuryInfoID), typeof(SOAdjust.adjTransferredToChildrenAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Transferred to Child Orders", Enabled = false, Visible = false)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<SOAdjust.voided, Equal<False>>,
				Add<SOAdjust.curyAdjgAmt, SOAdjust.curyAdjgBilledAmt>>, decimal0>),
			typeof(SumCalc<BlanketSOAdjust.curyAdjgTransferredToChildrenAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<SOAdjust.voided, Equal<False>>,
				Add<SOAdjust.adjAmt, SOAdjust.adjBilledAmt>>, decimal0>),
			typeof(SumCalc<BlanketSOAdjust.adjTransferredToChildrenAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<SOAdjust.voided, Equal<False>>,
				Add<SOAdjust.curyAdjdAmt, SOAdjust.curyAdjdBilledAmt>>, decimal0>),
			typeof(SumCalc<BlanketSOAdjust.curyAdjdTransferredToChildrenAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(IIf<Where<SOAdjust.voided, Equal<False>>, SOAdjust.curyAdjdTransferredToChildrenAmt, decimal0>),
			typeof(SumCalc<SOOrder.curyTransferredToChildrenPaymentTotal>),
			ForceAggregateRecalculation = true)]
		public virtual decimal? CuryAdjdTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion

		#region CuryDocBal
		public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }
		protected Decimal? _CuryDocBal;
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		[PXCurrency(typeof(SOAdjust.adjgCuryInfoID), typeof(SOAdjust.docBal), BaseCalc = false)]
		[PXUIField(DisplayName = "Balance", Visibility = PXUIVisibility.Visible, Enabled = false)]
		public virtual Decimal? CuryDocBal
		{
			get
			{
				return this._CuryDocBal;
			}
			set
			{
				this._CuryDocBal = value;
			}
		}
		#endregion
		#region DocBal
		public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }
		protected Decimal? _DocBal;
		[PXDecimal(4)]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? DocBal
		{
			get
			{
				return this._DocBal;
			}
			set
			{
				this._DocBal = value;
			}
		}
		#endregion

		#region IsCCPayment
		public abstract class isCCPayment : Data.BQL.BqlBool.Field<isCCPayment> { }
		[PXDBBool]
		[DenormalizedFrom(
			new[] { typeof(ARPayment.isCCPayment), typeof(ARPayment.released), typeof(ARPayment.isCCAuthorized), typeof(ARPayment.isCCCaptured), typeof(ARPayment.voided),
					typeof(ARPayment.hold), typeof(ARPayment.adjDate), typeof(ARPayment.paymentMethodID), typeof(ARPayment.cashAccountID), typeof(ARPayment.pMInstanceID),
					typeof(ARPayment.processingCenterID), typeof(ARPayment.extRefNbr), typeof(ARPayment.docDesc), typeof(ARPayment.curyOrigDocAmt), typeof(ARPayment.origDocAmt),
					typeof(ARPayment.syncLock), typeof(ARPayment.syncLockReason) },
			new[] { typeof(isCCPayment), typeof(paymentReleased), typeof(isCCAuthorized), typeof(isCCCaptured), typeof(voided),
					typeof(hold), typeof(adjgDocDate), typeof(paymentMethodID), typeof(cashAccountID), typeof(pMInstanceID),
					typeof(processingCenterID), typeof(extRefNbr), typeof(docDesc), typeof(curyOrigDocAmt), typeof(origDocAmt),
					typeof(syncLock), typeof(syncLockReason) })]
		public virtual bool? IsCCPayment
		{
			get;
			set;
		}
		#endregion
		#region PaymentReleased
		public abstract class paymentReleased : Data.BQL.BqlBool.Field<paymentReleased> { }
		[PXDBBool]
		public virtual bool? PaymentReleased
		{
			get;
			set;
		}
		#endregion
		#region IsCCAuthorized
		public abstract class isCCAuthorized : Data.BQL.BqlBool.Field<isCCAuthorized> { }
		[PXDBBool]
		public virtual bool? IsCCAuthorized
		{
			get;
			set;
		}
		#endregion
		#region IsCCCaptured
		public abstract class isCCCaptured : Data.BQL.BqlBool.Field<isCCCaptured> { }
		[PXDBBool]
		public virtual bool? IsCCCaptured
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		protected Boolean? _Voided;
		[PXDBBool()]
		public virtual Boolean? Voided
		{
			get
			{
				return this._Voided;
			}
			set
			{
				this._Voided = value;
			}
		}
		#endregion

		#region BlanketRecordID
		public abstract class blanketRecordID : PX.Data.BQL.BqlInt.Field<blanketRecordID> { }
		[PXDBInt]
		[PXParent(typeof(FK.BlanketAdjustment))]
		public virtual int? BlanketRecordID
		{
			get;
			set;
		}
		#endregion
		#region BlanketType
		public abstract class blanketType : PX.Data.BQL.BqlString.Field<blanketType> { }
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "Blanket SO Type", Enabled = false)]
		public virtual string BlanketType
		{
			get;
			set;
		}
		#endregion
		#region BlanketNbr
		public abstract class blanketNbr : PX.Data.BQL.BqlString.Field<blanketNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXParent(typeof(FK.BlanketOrder))]
		//[PXUnboundFormula(typeof(Switch<Case<Where<SOAdjust.curyAdjdAmt, Greater<decimal0>>, int1>, int0>), typeof(SumCalc<SOOrder.paymentCntr>))]
		public virtual string BlanketNbr
		{
			get;
			set;
		}
		#endregion

		#region Unbound fields for API
		#region RefTranExtNbr
		/// <summary>
		/// Orig. Transaction number (for customer refunds).
		/// </summary>
		public abstract class refTranExtNbr : Data.BQL.BqlString.Field<refTranExtNbr> { }
		[PXString(50, IsUnicode = true)]
		public virtual string RefTranExtNbr
		{
			get;
			set;
		}
		#endregion

		#region ExternalRef
		/// <summary>
		/// ExternalRef for ecommerce - to track external payment refernce
		/// </summary>
		public abstract class externalRef : Data.BQL.BqlString.Field<externalRef> { }
		[PXString(80, IsUnicode = true)]
		public virtual string ExternalRef
		{
			get;
			set;
		}
		#endregion
		#region Authorize
		/// <summary>
		/// When true - AuthorizeCCPayment action will be called automatically.
		/// </summary>
		public abstract class authorize : Data.BQL.BqlBool.Field<authorize> { }
		[PXBool]
		public virtual bool? Authorize
		{
			get;
			set;
		}
		#endregion
		#region Capture
		/// <summary>
		/// When true - CaptureCCPayment action will be called automatically.
		/// </summary>
		public abstract class capture : Data.BQL.BqlBool.Field<capture> { }
		[PXBool]
		public virtual bool? Capture
		{
			get;
			set;
		}
		#endregion
		#region Refund
		/// <summary>
		/// When true - CreditCCPayment action will be called automatically.
		/// </summary>
		public abstract class refund : Data.BQL.BqlBool.Field<refund> { }
		[PXBool]
		public virtual bool? Refund
		{
			get;
			set;
		}
		#endregion
		#region ValidateCCRefundOrigTransaction
		/// <summary>
		/// When false - the validation of the link between the sales order lines (returns) and the original CC transaction will be skipped.
		/// </summary>
		public abstract class validateCCRefundOrigTransaction : Data.BQL.BqlBool.Field<validateCCRefundOrigTransaction> { }
		[PXDBBool]
		[PXDefault(typeof(Search<SOOrderType.validateCCRefundsOrigTransactions, Where<SOOrderType.orderType, Equal<Current<SOAdjust.adjdOrderType>>>>))]
		public virtual bool? ValidateCCRefundOrigTransaction
		{
			get;
			set;
		}
		#endregion
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[PXNote]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
		#region System Columns
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp(RecordComesFirst = true)]
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
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#endregion

		#region IPayBalance Members

		public decimal? CuryPayDocBal
		{
			get
			{
				return this._CuryDocBal;
			}
			set
			{
				this._CuryDocBal = value;
			}
		}

		public decimal? PayDocBal
		{
			get
			{
				return this._DocBal;
			}
			set
			{
				this._DocBal = value;
			}
		}

		public decimal? CuryPayDiscBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? PayDiscBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? CuryPayWhTaxBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? PayWhTaxBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}
		#endregion

		#region IAdjustment Members

		
		public DateTime? AdjdDocDate
		{
			get
			{
				return this._AdjdOrderDate;
			}
			set
			{
				this._AdjdOrderDate = value;
			}
		}

		public decimal? RGOLAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public bool? Released
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool? ReverseGainLoss
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public decimal? CuryDiscBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? DiscBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? CuryAdjgWhTaxAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? CuryAdjdWhTaxAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? AdjWhTaxAmt
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? CuryWhTaxBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		public decimal? WhTaxBal
		{
			get
			{
				return 0m;
			}
			set
			{
			}
		}

		#endregion

		#region Fields denormalized from ARPayment

		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		protected String _PaymentMethodID;
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Method")]
		[PXSelector(typeof(Search<PaymentMethod.paymentMethodID>), DescriptionField = typeof(PaymentMethod.descr))]
		public virtual String PaymentMethodID
		{
			get
			{
				return this._PaymentMethodID;
			}
			set
			{
				this._PaymentMethodID = value;
			}
		}
		#endregion

		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		protected Int32? _CashAccountID;
		[PXUIField(DisplayName = "Cash Account", Visibility = PXUIVisibility.Visible)]
		[CashAccount(typeof(ARPayment.branchID), typeof(Search<CashAccount.cashAccountID>), Visibility = PXUIVisibility.Visible)]
		public virtual Int32? CashAccountID
		{
			get
			{
				return this._CashAccountID;
			}
			set
			{
				this._CashAccountID = value;
			}
		}
		#endregion

		#region PMInstanceID
		public abstract class pMInstanceID : PX.Data.BQL.BqlInt.Field<pMInstanceID> { }

		[PXDBInt()]
		[PXUIField(DisplayName = "Card/Account No")]
		[PXSelector(typeof(Search<CustomerPaymentMethod.pMInstanceID>), DescriptionField = typeof(CustomerPaymentMethod.descr))]
		public virtual Int32? PMInstanceID
		{
			get;
			set;
		}
		#endregion

		#region ProcessingCenterID
		public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Proc. Center ID")]
		[PXSelector(typeof(Search<CCProcessingCenter.processingCenterID>), DescriptionField = typeof(CCProcessingCenter.name), ValidateValue = false)]
		public virtual string ProcessingCenterID
		{
			get;
			set;
		}
		#endregion

		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		protected String _ExtRefNbr;
		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Ref.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String ExtRefNbr
		{
			get;
			set;
		}
		#endregion

		#region DocDesc
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXDefault(typeof(ARPayment.docDesc), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String DocDesc
		{
			get;
			set;
		}
		#endregion

		#region CuryOrigDocAmt
		public new abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }
		[PXDBCurrency(typeof(SOAdjust.adjgCuryInfoID), typeof(SOAdjust.origDocAmt))]
		[PXUIField(DisplayName = "Payment Amount", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? CuryOrigDocAmt
		{
			get;
			set;
		}
		#endregion

		#region OrigDocAmt
		public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt> { }
		protected Decimal? _OrigDocAmt;
		[PXDBBaseCury()]
		public virtual Decimal? OrigDocAmt
		{
			get
			{
				return this._OrigDocAmt;
			}
			set
			{
				this._OrigDocAmt = value;
			}
		}
		#endregion

		#region SyncLock
		public abstract class syncLock : Data.BQL.BqlBool.Field<syncLock> { }
		[PXUnboundFormula(
			typeof(IIf<Where<isCCPayment, Equal<True>,
				And<syncLock, Equal<True>,
				And<syncLockReason, NotEqual<ARPayment.syncLockReason.newCard>,
				And<curyAdjdAmt, NotEqual<decimal0>>>>>,
					int1, Zero>),
			typeof(SumCalc<SOOrder.paymentsNeedValidationCntr>),
			ForceAggregateRecalculation = true)]
		[PXDBBool]
		public virtual bool? SyncLock
		{
			get;
			set;
		}
		#endregion
		#region SyncLockReason
		public abstract class syncLockReason : PX.Data.BQL.BqlString.Field<syncLockReason> { }
		[PXDBString(1, IsFixed = true, IsUnicode = false)]
		public virtual string SyncLockReason
		{
			get;
			set;
		}
		#endregion


		#region Unbound fields
		#region NewCard
		public abstract class newCard : PX.Data.BQL.BqlBool.Field<newCard> { }

		[PXBool]
		public virtual bool? NewCard
		{
			get;
			set;
		}
		#endregion
		#region SaveCard
		public abstract class saveCard : PX.Data.BQL.BqlBool.Field<saveCard> { }
		[PXBool()]
		[PXUIField(DisplayName = "Save Card")]
		public virtual Boolean? SaveCard
		{
			get;
			set;
		}
		#endregion
		#endregion
		#endregion
	}
}
