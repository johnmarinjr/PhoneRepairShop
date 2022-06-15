using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.GL;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	[PXCacheName("AR Document transaction")]
	public class ARTranPost : IBqlTable
    {
	    #region Keys
		public class PK : PrimaryKeyOf<ARTran>.By<docType, refNbr, lineNbr, iD>
		{
			public static ARTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) => FindBy(graph, docType, refNbr, lineNbr, id);
		}

		public static class FK
		{
			public class Document : AR.ARRegister.PK.ForeignKeyOf<ARTran>.By<docType, refNbr> { }
			public class Invoice : AR.ARInvoice.PK.ForeignKeyOf<ARTran>.By<docType, refNbr> { }
			public class Payment : AR.ARPayment.PK.ForeignKeyOf<ARTran>.By<docType, refNbr> { }
			public class CashSale : Standalone.ARCashSale.PK.ForeignKeyOf<ARTran>.By<docType, refNbr> { }
			public class SOInvoice : SO.SOInvoice.PK.ForeignKeyOf<ARTran>.By<docType, refNbr> { }

			public class Branch : GL.Branch.PK.ForeignKeyOf<ARTran>.By<branchID> { }
			public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<ARTran>.By<curyInfoID> { }
			public class Customer : AR.Customer.PK.ForeignKeyOf<ARTran>.By<customerID> { }

			public class Account : GL.Account.PK.ForeignKeyOf<ARTran>.By<accountID> { }
			public class Subaccount : GL.Sub.PK.ForeignKeyOf<ARTran>.By<subID> { }
		}
		#endregion
		#region BranchID
        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

        [Branch(typeof(ARRegister.branchID))]
        public virtual Int32? BranchID { get; set; }
        #endregion
        #region DocType
        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }
        // Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Identiry was add on purpose]
        [PXDBString(IsKey = true)] 
        [PXUIField(DisplayName = "Doc. Type")]

        public virtual string DocType { get; set; }
        #endregion
        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }
        // Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Identiry was add on purpose]
        [PXDBString(IsKey = true)] 
        [PXParent(typeof(Select<ARRegister, Where<ARRegister.docType, Equal<Current<ARTranPost.docType>>, And<ARRegister.refNbr, Equal<Current<ARTranPost.refNbr>>>>>))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion
        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt] 
        [PXDefault(0)]
        [PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.PaymentsByLines))]
        public virtual int? LineNbr { get; set; }
        #endregion
        #region ID
        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        }
        // Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Identiry was add on purpose]
        [PXDBIdentity(IsKey = true)] 
        public virtual int? ID { get; set; }
        #endregion
        #region AdjNbr
        public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr>
        {
        }
        [PXDBInt()]
        public virtual int? AdjNbr { get; set; }
        #endregion
        #region RefNoteID
        public new abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }
        [PXDBGuidAttribute]
        public virtual Guid? RefNoteID
        {
	        get;
	        set;
        }
        #endregion
        #region SourceDocType
        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }
        [PXDBString(3, IsFixed = true)]
        [ARDocType.List()]
        [PXUIField(DisplayName = "Source Doc. Type")]
        public virtual string SourceDocType { get; set; }

        #endregion
        #region SourceRefNbr
        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }
        [PXDBString(15, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion
        #region DocDate
        public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
        {
        }
        [PXDBDate()]
        [PXDefault(typeof(ARRegister.docDate))]
        [PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? DocDate { get; set; }
        #endregion
        #region CustomerID
        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
        [Customer()]
        [PXDefault]
        public virtual Int32? CustomerID { get; set; }

        #endregion
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(
	        branchSourceType: typeof(branchID),
	        masterFinPeriodIDType: typeof(tranPeriodID))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID]
        public virtual string TranPeriodID { get; set; }
        #endregion
        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
        {
        }
        [PXDBLong()]
        [CurrencyInfo(typeof(ARRegister.curyInfoID))]
        public virtual long? CuryInfoID { get; set; }

        #endregion
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
	        IsMigratedRecordField = typeof(isMigratedRecord))]
        [PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]

        public virtual string BatchNbr { get; set; }

        #endregion
        #region AccountID
        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(typeof(branchID), ValidateValue = false, DisplayName = "Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
        public virtual int? AccountID { get; set; }

        #endregion
        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(typeof(accountID), typeof(branchID),  true, ValidateValue = false, DisplayName = "Subaccount", Visibility = PXUIVisibility.Visible)]
        public virtual int? SubID { get; set; }

        #endregion
        #region CuryAmt
        public abstract class curyAmt : PX.Data.BQL.BqlDecimal.Field<curyAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(amt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? CuryAmt { get; set; }

        #endregion
        #region CuryPPDAmt
        public abstract class curyPPDAmt : PX.Data.BQL.BqlDecimal.Field<curyPPDAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(ppdAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryPPDAmt { get; set; }

        #endregion
        #region CuryDiscAmt
        public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(discAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryDiscAmt { get; set; }

        #endregion
        #region CuryRetainageAmt

        public abstract class curyRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(retainageAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? CuryRetainageAmt { get; set; }

        #endregion
        #region CuryWOAmt

        public abstract class curyWOAmt : PX.Data.BQL.BqlDecimal.Field<curyWOAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(wOAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Write-Off Amount")]
        public virtual decimal? CuryWOAmt { get; set; }

        #endregion
        #region CuryItemDiscAmt

        public abstract class curyItemDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyItemDiscAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(itemDiscAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
       
        public virtual decimal? CuryItemDiscAmt { get; set; }

        #endregion
        #region Amt

        public abstract class amt : PX.Data.BQL.BqlDecimal.Field<amt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? Amt { get; set; }

        #endregion
        #region PPDAmt
        public abstract class ppdAmt : PX.Data.BQL.BqlDecimal.Field<ppdAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? PPDAmt { get; set; }

        #endregion
        #region DiscAmt

        public abstract class discAmt : PX.Data.BQL.BqlDecimal.Field<discAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? DiscAmt { get; set; }

        #endregion
        #region RetainageAmt

        public abstract class retainageAmt : PX.Data.BQL.BqlDecimal.Field<retainageAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? RetainageAmt { get; set; }

        #endregion
        #region WOAmt

        public abstract class wOAmt : PX.Data.BQL.BqlDecimal.Field<wOAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? WOAmt { get; set; }

        #endregion
        #region ItemDiscAmt

        public abstract class itemDiscAmt : PX.Data.BQL.BqlDecimal.Field<itemDiscAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? ItemDiscAmt { get; set; }

        #endregion
        #region RGOLAmt

        public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? RGOLAmt { get; set; }

        #endregion
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool()]
        public virtual bool? IsMigratedRecord
        {
	        get;
	        set;
        }
        #endregion
        #region IsVoidPrepayment
        public abstract class isVoidPrepayment : PX.Data.BQL.BqlBool.Field<isVoidPrepayment> { }

        [PXBool()]
        public virtual bool? IsVoidPrepayment
        {
	        get;
	        set;
        }
        #endregion
        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
	        public const string Origin = "S";
	        public const string Application = "D";
	        public const string Adjustment = "G";
	        public const string Retainage = "F";
	        public const string RetainageReverse = "U";
	        public const string RGOL = "R";
	        public const string Rounding = "X";
	        public const string Voided = "V";
	        public const string Installment = "I";

	        public class origin : PX.Data.BQL.BqlString.Constant<origin> {
		        public origin() : base(Origin) {}
	        }
	        public class application : PX.Data.BQL.BqlString.Constant<application>
	        {
		        public application() : base(Application) {}
	        }
	        public class adjustment : PX.Data.BQL.BqlString.Constant<adjustment>
	        {
		        public adjustment() : base(Adjustment) {}
	        }
	        public class retainage : PX.Data.BQL.BqlString.Constant<retainage>
	        {
		        public retainage() : base(Retainage) {}
	        }
	        public class retainageReverse : PX.Data.BQL.BqlString.Constant<retainageReverse> {
		        public retainageReverse() : base(RetainageReverse) {}
	        }
	        public class rgol : PX.Data.BQL.BqlString.Constant<rgol>
	        {
		        public rgol() : base(RGOL) {}
	        }
	        public class rounding : PX.Data.BQL.BqlString.Constant<rounding>
	        {
		        public rounding() : base(Rounding) {}
	        }
	        public class @void : PX.Data.BQL.BqlString.Constant<@void>
	        {
		        public @void() : base(Voided) {}
	        }
	        public class installment : PX.Data.BQL.BqlString.Constant<installment>
	        {
		        public installment() : base(Installment) {}
	        }
	        public class ListAttribute : PXStringListAttribute
	        {
		        public ListAttribute()
			        : base(new[] {Origin, Application, Adjustment, Retainage, RetainageReverse, Voided, Installment, RGOL, Rounding},
				        new[]
				        {
					        Messages.Origin, Messages.Application, Messages.Adjusted, Messages.Retainage, Messages.RetainageReverse, Messages.Voided,
					        Messages.Installment, Messages.RGOL, Messages.Rounding
				        }
			        )
		        {
		        }
	        }
        }

        [PXDBString()]
        [type.List]
        [PXUIField(DisplayName = "Transaction type")]
        public virtual string Type { get; set; }

        #endregion
        #region TranType
        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }
        [PXDBString()] 
        [PXUIField(DisplayName = "Tran. Type")]
        public virtual string TranType { get; set; }
        #endregion
        #region TranRefNbr
        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }
        [PXDBString()] 
        [PXUIField(DisplayName = "Tran. Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string TranRefNbr { get; set; }
        #endregion
        #region ReferenceID
        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }
        [Customer()]
        public virtual int? ReferenceID { get; set; }
        #endregion
        #region BalanceSign
        public abstract class balanceSign : PX.Data.BQL.BqlShort.Field<balanceSign> { }
        [PXDBShort]
		[PXFormula(typeof(Switch<Case<
			Where<docType.IsIn<ARDocType.refund,ARDocType.voidRefund,ARDocType.invoice, ARDocType.debitMemo, 
							   ARDocType.finCharge,ARDocType.cashSale,ARDocType.smallCreditWO>>,
				short1>, 
			shortMinus1>))]
        public virtual short? BalanceSign { get; set; }
        #endregion
        #region GLSign
        public abstract class glSign : PX.Data.BQL.BqlShort.Field<glSign> { }
        [PXDBShort]
        [PXFormula(typeof(Switch<
	        Case<Where<type.IsEqual<type.@void>
			        .And<tranType.IsEqual<ARDocType.voidRefund>>>,
		        shortMinus1,
		     Case<Where<type.IsEqual<type.retainageReverse>
			        .And<docType.IsIn<ARDocType.invoice, ARDocType.debitMemo>>>,
		        shortMinus1,   
	        Case<Where<type.IsEqual<type.retainageReverse>>,
		        short1,
		     Case<Where<type.IsEqual<type.retainage>
				        .And<tranType.IsEqual<ARDocType.creditMemo>>>,
			        shortMinus1,
			  Case<Where<type.IsEqual<type.retainage>
							.And<tranType.IsIn<ARDocType.invoice, ARDocType.debitMemo>>>,
           		  short1,      
	        Case<Where<type.IsNotIn<type.adjustment, type.rgol>
			        .And<docType.IsIn<ARDocType.refund,ARDocType.voidRefund,ARDocType.invoice, ARDocType.debitMemo, 
						ARDocType.finCharge,ARDocType.cashSale,ARDocType.smallCreditWO>>>, 
		        short1,
	        Case<Where<type.IsEqual<type.adjustment>
			        .And<sourceDocType.IsIn<ARDocType.refund,ARDocType.voidRefund,ARDocType.invoice, ARDocType.debitMemo, 
				        ARDocType.finCharge,ARDocType.cashSale,ARDocType.smallCreditWO>>>, 
		        short1,
		     Case<Where<type.IsEqual<type.rgol>
					.And<docType.IsEqual<ARDocType.cashReturn>>>,
			    short1,    
		    Case<Where<type.IsEqual<type.@void>
					.And<docType.IsEqual<ARDocType.smallBalanceWO>>>,
			    shortMinus1,
			Case<Where<type.IsEqual<type.@void>>,
				short1>>>>>>>>>>, 
	        shortMinus1>))]
        public virtual short? GLSign { get; set; }
        #endregion
        #region TranClass
        public abstract class tranClass : PX.Data.BQL.BqlString.Field<tranClass>
        {
            public class CRMN : PX.Data.BQL.BqlString.Constant<CRMN> { public CRMN() : base(nameof(CRMN)) {} }
            public class CRMP : PX.Data.BQL.BqlString.Constant<CRMP> { public CRMP() : base(nameof(CRMP)) {} }
            public class CRMR : PX.Data.BQL.BqlString.Constant<CRMR> { public CRMR() : base(nameof(CRMR)) {} }
            public class CRMU : PX.Data.BQL.BqlString.Constant<CRMU> { public CRMU() : base(nameof(CRMU)) {} }
            public class CRMX : PX.Data.BQL.BqlString.Constant<CRMX> { public CRMX() : base(nameof(CRMX)) {} }
            public class DRMN : PX.Data.BQL.BqlString.Constant<DRMN> { public DRMN() : base(nameof(DRMN)) {} }
            public class FCHN : PX.Data.BQL.BqlString.Constant<FCHN> { public FCHN() : base(nameof(FCHN)) {} }
            public class INVN : PX.Data.BQL.BqlString.Constant<INVN> { public INVN() : base(nameof(INVN)) {} }
            public class PMTN : PX.Data.BQL.BqlString.Constant<PMTN> { public PMTN() : base(nameof(PMTN)) {} }
            public class PMTP : PX.Data.BQL.BqlString.Constant<PMTP> { public PMTP() : base(nameof(PMTP)) {} }
            public class PMTR : PX.Data.BQL.BqlString.Constant<PMTR> { public PMTR() : base(nameof(PMTR)) {} }
            public class PMTU : PX.Data.BQL.BqlString.Constant<PMTU> { public PMTU() : base(nameof(PMTU)) {} }
            public class PMTX : PX.Data.BQL.BqlString.Constant<PMTX> { public PMTX() : base(nameof(PMTX)) {} }
            public class PPMB : PX.Data.BQL.BqlString.Constant<PPMB> { public PPMB() : base(nameof(PPMB)) {} }
            public class PPMN : PX.Data.BQL.BqlString.Constant<PPMN> { public PPMN() : base(nameof(PPMN)) {} }
            public class PPMP : PX.Data.BQL.BqlString.Constant<PPMP> { public PPMP() : base(nameof(PPMP)) {} }
            public class PPMR : PX.Data.BQL.BqlString.Constant<PPMR> { public PPMR() : base(nameof(PPMR)) {} }
            public class PPMU : PX.Data.BQL.BqlString.Constant<PPMU> { public PPMU() : base(nameof(PPMU)) {} }
            public class REFN : PX.Data.BQL.BqlString.Constant<REFN> { public REFN() : base(nameof(REFN)) {} }
            public class REFP : PX.Data.BQL.BqlString.Constant<REFP> { public REFP() : base(nameof(REFP)) {} }
            public class REFU : PX.Data.BQL.BqlString.Constant<REFU> { public REFU() : base(nameof(REFU)) {} }
            public class REFR : PX.Data.BQL.BqlString.Constant<REFR> { public REFR() : base(nameof(REFR)) {} }
            public class RPMN : PX.Data.BQL.BqlString.Constant<RPMN> { public RPMN() : base(nameof(RPMN)) {} }
            public class RPMP : PX.Data.BQL.BqlString.Constant<RPMP> { public RPMP() : base(nameof(RPMP)) {} }
            public class RPMR : PX.Data.BQL.BqlString.Constant<RPMR> { public RPMR() : base(nameof(RPMR)) {} }
            public class RPMU : PX.Data.BQL.BqlString.Constant<RPMU> { public RPMU() : base(nameof(RPMU)) {} }
            public class SMBN : PX.Data.BQL.BqlString.Constant<SMBN> { public SMBN() : base(nameof(SMBN)) {} }
            public class SMBP : PX.Data.BQL.BqlString.Constant<SMBP> { public SMBP() : base(nameof(SMBP)) {} }
            public class SMBR : PX.Data.BQL.BqlString.Constant<SMBR> { public SMBR() : base(nameof(SMBR)) {} }
            public class SMBU : PX.Data.BQL.BqlString.Constant<SMBU> { public SMBU() : base(nameof(SMBU)) {} }
            public class SMCB : PX.Data.BQL.BqlString.Constant<SMCB> { public SMCB() : base(nameof(SMCB)) {} }
            public class SMCN : PX.Data.BQL.BqlString.Constant<SMCN> { public SMCN() : base(nameof(SMCN)) {} }
            public class SMCP : PX.Data.BQL.BqlString.Constant<SMCP> { public SMCP() : base(nameof(SMCP)) {} }
            public class SMCU : PX.Data.BQL.BqlString.Constant<SMCU> { public SMCU() : base(nameof(SMCU)) {} }
            public class VRFN : PX.Data.BQL.BqlString.Constant<VRFN> { public VRFN() : base(nameof(VRFN)) {} }
            public class VRFP : PX.Data.BQL.BqlString.Constant<VRFP> { public VRFP() : base(nameof(VRFP)) {} }
            public class VRFR : PX.Data.BQL.BqlString.Constant<VRFR> { public VRFR() : base(nameof(VRFR)) {} }
            public class VRFU : PX.Data.BQL.BqlString.Constant<VRFU> { public VRFU() : base(nameof(VRFU)) {} }

            public class CSLN : PX.Data.BQL.BqlString.Constant<CSLN> { public CSLN() : base(nameof(CSLN)) {} }
            public class CSLR : PX.Data.BQL.BqlString.Constant<CSLR> { public CSLR() : base(nameof(CSLR)) {} }
            public class RCSN : PX.Data.BQL.BqlString.Constant<RCSN> { public RCSN() : base(nameof(RCSN)) {} }
            public class RCSR : PX.Data.BQL.BqlString.Constant<RCSR> { public RCSR() : base(nameof(RCSR)) {} }

        }
        [PXDBString]
        [PXFormula(typeof(Add<
	        Switch<
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>
			        .And<isMigratedRecord.IsEqual<True>>>, ARDocType.creditMemo,
			     Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>
					   .And<ARTranPost.docType.IsEqual<ARDocType.smallCreditWO>
						.And<ARTranPost.sourceDocType.IsEqual<ARDocType.voidPayment>
						.And<ARTranPost.isVoidPrepayment.IsEqual<True>>>>>, 
				     ARDocType.smallCreditWO, 
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>
				        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.smallCreditWO>
					     .And<ARTranPost.docType.IsEqual<ARDocType.voidPayment>
						  .And<ARTranPost.isVoidPrepayment.IsEqual<True>>>>>, 
			        ARDocType.prepayment,       
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>
			        .And<Not<ARTranPost.docType.IsEqual<ARDocType.smallCreditWO>.And<ARTranPost.adjNbr.IsEqual<int_1>>>>>, 
                    ARTranPost.sourceDocType,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>
			        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.smallCreditWO>.And<ARTranPost.adjNbr.IsEqual<int_1>>>>, 
                    ARTranPost.sourceDocType,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rgol>
			        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.smallCreditWO>.And<ARTranPost.adjNbr.IsEqual<int_1>>>>, 
                    ARTranPost.docType>>>>>>, docType>,
	        Switch<
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>
			        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.smallCreditWO>>
			        .And<ARTranPost.docType.IsEqual<ARDocType.prepayment>>>,
			        GLTran.tranClass.writeoff,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>
				        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.smallCreditWO>>
				        .And<ARTranPost.adjNbr.IsEqual<int_1>>>,
			        GLTran.tranClass.normal,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>>,
			        GLTran.tranClass.payment,    
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>
				        .And<ARTranPost.docType.IsEqual<ARDocType.smallCreditWO>>
				        .And<ARTranPost.adjNbr.IsEqual<int_1>>>,
			        GLTran.tranClass.payment,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>
				        .And<ARTranPost.docType.IsEqual<ARDocType.smallCreditWO>>
				        .And<ARTranPost.sourceDocType.IsEqual<ARDocType.creditMemo>>>,
			        Space,
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rgol>>,
			        GLTran.tranClass.rgol,    
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rounding>>,
			        GLTran.tranClass.rounding,    
		        Case<Where<ARTranPost.type.IsIn<ARTranPost.type.origin,ARTranPost.type.@void>
			        .And<ARTranPost.docType.IsEqual<ARDocType.prepayment>>>,
			        GLTran.tranClass.payment,    
		        Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.@void>
				        .And<ARTranPost.docType.IsEqual<ARDocType.smallCreditWO>>>,
			        GLTran.tranClass.@void,  
		        Case<Where<ARTranPost.docType.IsIn<ARDocType.invoice, ARDocType.debitMemo, ARDocType.creditMemo, ARDocType.finCharge,ARDocType.cashSale, ARDocType.cashReturn>>,
			        GLTran.tranClass.normal,
		        Case<Where<ARTranPost.docType.IsIn<ARDocType.payment, ARDocType.voidPayment, ARDocType.refund, ARDocType.voidRefund>>,
			        GLTran.tranClass.payment,
		        Case<Where<ARTranPost.docType.IsIn<ARDocType.smallBalanceWO, ARDocType.smallCreditWO, ARDocType.prepayment>>,
			        GLTran.tranClass.charge>>>>>>>>>>>>>
        >))]
        public virtual string TranClass { get; set; }
        #endregion
        
    }
}