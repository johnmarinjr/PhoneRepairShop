using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.AP
{
    [PXCacheName("AP Document transaction")]
	public class APTranPost : IBqlTable
    {
	    #region Keys
		public class PK : PrimaryKeyOf<APTranPost>.By<docType, refNbr, lineNbr, iD>
		{
			public static APTranPost Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) => FindBy(graph, docType, refNbr, lineNbr, id);
		}

		public static class FK
		{
			public class Document : AP.APRegister.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
			public class Invoice : AP.APInvoice.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
			public class Payment : AP.APPayment.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
			public class QuickCheck : Standalone.APQuickCheck.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
			public class SOInvoice : SO.SOInvoice.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }

			public class Branch : GL.Branch.PK.ForeignKeyOf<APTran>.By<branchID> { }
			public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<APTran>.By<curyInfoID> { }
			public class Vendor : AP.Vendor.PK.ForeignKeyOf<APTran>.By<vendorID> { }

			public class Account : GL.Account.PK.ForeignKeyOf<APTran>.By<accountID> { }
			public class Subaccount : GL.Sub.PK.ForeignKeyOf<APTran>.By<subID> { }
		}
		#endregion
		
        #region BranchID
        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

        [Branch(typeof(APRegister.branchID))]
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
        [PXParent(typeof(Select<APRegister, Where<APRegister.docType, Equal<Current<docType>>, And<APRegister.refNbr, Equal<Current<refNbr>>>>>))]
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
        [APDocType.List()]
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
        [PXDefault(typeof(APRegister.docDate))]
        [PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? DocDate { get; set; }
        #endregion
        
        #region VendorID
        public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
        [Vendor()]
        [PXDefault]
        public virtual Int32? VendorID { get; set; }

        #endregion

        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID]
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
        [CurrencyInfo(typeof(APRegister.curyInfoID))]
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName="Batch Number", Visibility=PXUIVisibility.Visible, Visible=true, Enabled=false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
	        IsMigratedRecordField = typeof(isMigratedRecord))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region AccountID
        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(typeof(branchID), DisplayName = "Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(typeof(accountID), typeof(branchID), true, DisplayName = "Subaccount", Visibility = PXUIVisibility.Visible)]
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

        #region CuryWhTaxAmt

        public abstract class curyWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyWhTaxAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(whTaxAmt), BaseCalc = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "With. Tax")]

        public virtual decimal? CuryWhTaxAmt { get; set; }

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

        #region WhTaxAmt

        public abstract class whTaxAmt : PX.Data.BQL.BqlDecimal.Field<whTaxAmt>
        {
        }

        [PXDBBaseCury]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? WhTaxAmt { get; set; }

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
        
        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
	        public const string Origin = "S";
	        public const string Application = "D";
	        public const string Adjustment = "G";
	        public const string Retainage = "F";
	        public const string RetainageReverse = "U";
	        public const string Installment = "I";
	        public const string Voided = "V";
	        public const string RGOL = "R";
	        
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
	        public class rgol : PX.Data.BQL.BqlString.Constant<rgol>
	        {
		        public rgol() : base(RGOL) {}
	        }
	        public class retainage : PX.Data.BQL.BqlString.Constant<retainage>
	        {
		        public retainage() : base(Retainage) {}
	        }
	        public class retainageReverse : PX.Data.BQL.BqlString.Constant<retainageReverse> {
		        public retainageReverse() : base(RetainageReverse) {}
	        }

	        public class voided : PX.Data.BQL.BqlString.Constant<voided>
	        {
		        public voided() : base(Voided) {}
	        }
	        public class installment : PX.Data.BQL.BqlString.Constant<installment>
	        {
		        public installment() : base(Installment) {}
	        }
	        public class ListAttribute : PXStringListAttribute
	        {
		        public ListAttribute()
			        : base( new [] {Origin, Application, Adjustment, Retainage, RetainageReverse, Voided },
				        new []{Messages.Origin, Messages.Application, Messages.Adjustment, Messages.Retainage, Messages.RetainageReverse, Messages.Voided }
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
       
        #region BalanceSign
        public abstract class balanceSign : PX.Data.BQL.BqlShort.Field<balanceSign> { }
        [PXDBShort]
		[PXFormula(typeof(Switch<Case<
			Where<docType.IsIn<APDocType.refund,APDocType.voidRefund,APDocType.invoice, 
				APDocType.creditAdj,APDocType.quickCheck>>,
				short1>, 
			shortMinus1>))]
        public virtual short? BalanceSign { get; set; }
        #endregion
        
        #region GLSign
        public abstract class glSign : PX.Data.BQL.BqlShort.Field<glSign> { }
        [PXDBShort]
        [PXFormula(typeof(Switch<
	        Case<Where<type.IsEqual<type.voided>>, 
		        short1,
		     Case<Where<type.IsEqual<type.retainageReverse>
			        .And<docType.IsIn<APDocType.invoice, APDocType.creditAdj>>>,
		        shortMinus1,   
	        Case<Where<type.IsEqual<type.retainageReverse>>,
		        short1,
		     Case<Where<type.IsEqual<type.retainage>
				        .And<tranType.IsEqual<APDocType.debitAdj>>>,
			     shortMinus1,
			  Case<Where<type.IsEqual<type.retainage>
							.And<tranType.IsIn<APDocType.invoice, APDocType.creditAdj>>>,
				  short1,
           Case<Where<type.IsEqual<type.application>
				    .And<docType.IsEqual<APDocType.prepayment>
					.And<sourceDocType.IsIn<APDocType.prepayment, APDocType.check, APDocType.voidCheck>>>>, 
			    short1,
			  Case<Where<type.IsEqual<type.adjustment>
					.And<sourceDocType.IsEqual<APDocType.prepayment>
					.And<docType.IsIn<APDocType.prepayment,APDocType.check, APDocType.voidCheck>>>>, 
				short1,
	        Case<Where<type.IsNotEqual<type.adjustment>
			        .And<docType.IsIn<APDocType.refund,APDocType.voidRefund,APDocType.invoice, APDocType.creditAdj, 
						APDocType.quickCheck>>>, 
		        short1,
	        Case<Where<type.IsEqual<type.adjustment>
			        .And<sourceDocType.IsIn<APDocType.refund,APDocType.voidRefund,APDocType.invoice, APDocType.creditAdj, 
				        APDocType.quickCheck>>>, 
		        short1>>>>>>>>>,
		    shortMinus1>))]
        public virtual short? GLSign { get; set; }
        #endregion
        
        #region IsVoidPrepayment
        public abstract class isVoidPrepayment : PX.Data.BQL.BqlBool.Field<isVoidPrepayment> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXBool()]
        public virtual bool? IsVoidPrepayment
        {
	        get;
	        set;
        }
        #endregion
       
        #region TranClass
        public abstract class tranClass : PX.Data.BQL.BqlString.Field<tranClass>
        {
	        public class ACRN : PX.Data.BQL.BqlString.Constant<ADRN> { public ACRN() : base(nameof(ACRN)) {} }
	        public class ADRN : PX.Data.BQL.BqlString.Constant<ADRN> { public ADRN() : base(nameof(ADRN)) {} }
	        public class ADRP : PX.Data.BQL.BqlString.Constant<ADRP> { public ADRP() : base(nameof(ADRP)) {} }
	        public class ADRR : PX.Data.BQL.BqlString.Constant<ADRR> { public ADRR() : base(nameof(ADRR)) {} }
	        public class ADRU : PX.Data.BQL.BqlString.Constant<ADRU> { public ADRU() : base(nameof(ADRU)) {} }
	        public class INVN : PX.Data.BQL.BqlString.Constant<INVN> { public INVN() : base(nameof(INVN)) {} }
            public class PPMB : PX.Data.BQL.BqlString.Constant<PPMB> { public PPMB() : base(nameof(PPMB)) {} }
            public class PPMN : PX.Data.BQL.BqlString.Constant<PPMN> { public PPMN() : base(nameof(PPMN)) {} }
            public class PPMP : PX.Data.BQL.BqlString.Constant<PPMP> { public PPMP() : base(nameof(PPMP)) {} }
            public class PPMR : PX.Data.BQL.BqlString.Constant<PPMR> { public PPMR() : base(nameof(PPMR)) {} }
            public class PPMU : PX.Data.BQL.BqlString.Constant<PPMU> { public PPMU() : base(nameof(PPMU)) {} }
            public class REFN : PX.Data.BQL.BqlString.Constant<REFN> { public REFN() : base(nameof(REFN)) {} }
            public class REFP : PX.Data.BQL.BqlString.Constant<REFP> { public REFP() : base(nameof(REFP)) {} }
            public class REFU : PX.Data.BQL.BqlString.Constant<REFU> { public REFU() : base(nameof(REFU)) {} }
            public class REFR : PX.Data.BQL.BqlString.Constant<REFR> { public REFR() : base(nameof(REFR)) {} }
            public class VRFN : PX.Data.BQL.BqlString.Constant<VRFN> { public VRFN() : base(nameof(VRFN)) {} }
            public class VRFP : PX.Data.BQL.BqlString.Constant<VRFP> { public VRFP() : base(nameof(VRFP)) {} }
            public class VRFU : PX.Data.BQL.BqlString.Constant<VRFU> { public VRFU() : base(nameof(VRFU)) {} }
            public class VRFR : PX.Data.BQL.BqlString.Constant<VRFR> { public VRFR() : base(nameof(VRFR)) {} }
            public class VCKN : PX.Data.BQL.BqlString.Constant<VCKN> { public VCKN() : base(nameof(VCKN)) {} }
            public class VCKP : PX.Data.BQL.BqlString.Constant<VCKP> { public VCKP() : base(nameof(VCKP)) {} }
            public class VCKR : PX.Data.BQL.BqlString.Constant<VCKR> { public VCKR() : base(nameof(VCKR)) {} }
            public class VCKU : PX.Data.BQL.BqlString.Constant<VCKU> { public VCKU() : base(nameof(VCKU)) {} }
            public class CHKN : PX.Data.BQL.BqlString.Constant<CHKN> { public CHKN() : base(nameof(CHKN)) {} }
            public class CHKP : PX.Data.BQL.BqlString.Constant<CHKP> { public CHKP() : base(nameof(CHKP)) {} }
            public class CHKU : PX.Data.BQL.BqlString.Constant<CHKU> { public CHKU() : base(nameof(CHKU)) {} }
            public class CHKR : PX.Data.BQL.BqlString.Constant<CHKR> { public CHKR() : base(nameof(CHKR)) {} }
            public class QCKN : PX.Data.BQL.BqlString.Constant<QCKN> { public QCKN() : base(nameof(QCKN)) {} }
            public class QCKR : PX.Data.BQL.BqlString.Constant<QCKR> { public QCKR() : base(nameof(QCKR)) {} }
            public class VQCN : PX.Data.BQL.BqlString.Constant<VQCN> { public VQCN() : base(nameof(VQCN)) {} }
            public class VQCR : PX.Data.BQL.BqlString.Constant<VQCR> { public VQCR() : base(nameof(VQCR)) {} }

        }
        [PXDBString]
        [PXFormula(typeof(Add<
	        Switch<
		        Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
				        .And<APTranPost.isMigratedRecord.IsEqual<True>>>,
			        APDocType.debitAdj,
		        Case<Where<APTranPost.type.IsIn<APTranPost.type.application, APTranPost.type.rgol>>,
			        APTranPost.sourceDocType>>,
		        docType>,
	        Switch<
		        Case<Where<APTranPost.type.IsIn<APTranPost.type.origin, APTranPost.type.voided, APTranPost.type.adjustment>
				    .And<APTranPost.isMigratedRecord.IsEqual<False>>
			        .And<APTranPost.docType.IsEqual<APDocType.voidCheck>>
			        .And<APTranPost.isVoidPrepayment.IsEqual<True>>>,
			        GLTran.tranClass.charge,
			    Case<Where<APTranPost.type.IsEqual<APTranPost.type.adjustment>>,
				    GLTran.tranClass.payment,
				Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>>,
				    GLTran.tranClass.rgol,    
				Case<Where<APTranPost.docType.IsIn<APDocType.invoice, APDocType.debitAdj, APDocType.creditAdj, APDocType.quickCheck,APDocType.voidQuickCheck>>,
			        GLTran.tranClass.normal,
		        Case<Where<APTranPost.docType.IsIn<APDocType.check, APDocType.voidCheck, APDocType.refund, APDocType.voidRefund>>,
			        GLTran.tranClass.payment,
		        Case<Where<APTranPost.docType.IsEqual<APDocType.prepayment>>,
			        GLTran.tranClass.charge>>>>>>>
        >))]
        public virtual string TranClass { get; set; }
        #endregion
    }
}