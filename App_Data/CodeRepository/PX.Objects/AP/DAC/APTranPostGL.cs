using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AP
{
	[PXProjection(typeof(Select2<APTranPost, 
        LeftJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<APTranPost.curyInfoID>>>>), Persistent = false)]
    [PXCacheName("AP Document Post GL")]
    public class APTranPostGL : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<APTranPostGL>.By<docType, refNbr, lineNbr, iD>
        {
            public static APTranPostGL Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
                FindBy(graph, docType, refNbr, lineNbr, id);
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
        
        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Doc. Type")]
        [APDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(APTranPost))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region ID

        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        } 

        [PXDBInt(IsKey = true, BqlTable = typeof(APTranPost))]
        public virtual int? ID { get; set; }

        #endregion

        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [APDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion
        
        #region CuryID
        
        public abstract class curyID : PX.Data.BQL.BqlLong.Field<curyID> { }

        [PXDBString(BqlTable = typeof(CurrencyInfo))] 
        public virtual string CuryID { get; set; }

        #endregion
        
        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

        [PXDBLong(BqlTable = typeof(APTranPost))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [Branch(BqlTable = typeof(APTranPost))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region VendorID

        public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
        {
        }

        [Vendor(BqlTable = typeof(APTranPost))]
        public virtual int? VendorID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(APTranPost))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(APTranPost))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(APTranPost))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BalanceSign
        public abstract class balanceSign : PX.Data.BQL.BqlShort.Field<balanceSign> { }
        [PXDBShort(BqlTable=typeof(APTranPost))]
        public virtual short? BalanceSign { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }
        [PXDBString(15, IsUnicode = true,BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Batch Number", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(APTranPost))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        [APTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranClass

        public abstract class tranClass : PX.Data.BQL.BqlString.Field<tranClass>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        public virtual string TranClass { get; set; }

        #endregion
        
        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        public virtual string TranRefNbr { get; set; }

        #endregion

        #region ReferenceID

        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID>
        {
        }

        [PXDBInt(BqlTable = typeof(APTranPost))]
        public virtual int? ReferenceID { get; set; }

        #endregion

        #region RefNoteID
        public new abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }
        [PXDBGuidAttribute(BqlTable = typeof(APTranPost))]
        public virtual Guid? RefNoteID
        {
            get;
            set;
        }
        #endregion
        
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summAPy>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summAPy>
        [PXDBBool(BqlTable = typeof(APTranPost))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion

        #region CuryBalanceAmt

        public abstract class curyBalanceAmt : PX.Data.BQL.BqlDecimal.Field<curyBalanceAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(balanceAmt), BaseCalc = false)]
        [PXUIField(DisplayName = "Balance")]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN,APTranPost.tranClass.VQCN>>>,
                    Data.BQL.Minus<APTranPost.glSign>.Multiply<APTranPost.curyAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>>,
                    Data.BQL.Minus<APTranPost.glSign>.Multiply<APTranPost.curyDiscAmt.Add<APTranPost.curyWhTaxAmt>.Add<APTranPost.curyAmt>>,
                Case<Where<APTranPost.type.IsIn<APTranPost.type.origin, APTranPost.type.adjustment, APTranPost.type.voided, APTranPost.type.installment>>,
                    APTranPost.glSign.Multiply<APTranPost.curyAmt>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryBalanceAmt { get; set; }

        #endregion

        #region BalanceAmt

        public abstract class balanceAmt : PX.Data.BQL.BqlDecimal.Field<balanceAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
            
            Switch<
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                        .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.PPMU,APTranPost.tranClass.CHKU,APTranPost.tranClass.VCKU>>>,
                    Data.BQL.Minus<APTranPost.glSign>.Multiply<APTranPost.amt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN,APTranPost.tranClass.VQCN>>>,
                    Data.BQL.Minus<APTranPost.glSign>.Multiply<APTranPost.amt>.Subtract<APTranPost.rGOLAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>>,
                    Data.BQL.Minus<APTranPost.glSign>.Multiply<APTranPost.discAmt.Add<APTranPost.whTaxAmt>.Add<APTranPost.amt>>.Subtract<APTranPost.rGOLAmt>,
                Case<Where<APTranPost.type.IsIn<APTranPost.type.origin, APTranPost.type.adjustment, APTranPost.type.voided, APTranPost.type.installment>>,
                    APTranPost.glSign.Multiply<APTranPost.amt>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? BalanceAmt { get; set; }

        #endregion

        #region CuryDebitAPAmt

        public abstract class curyDebitAPAmt : PX.Data.BQL.BqlDecimal.Field<curyDebitAPAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(debitAPAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPost.accountID.IsNull>, decimal0,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.voided>>,
                    PX.Data.Minus<APTranPost.curyAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                        .And<APTranPost.glSign.IsEqual<shortMinus1>>
                        .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN, APTranPost.tranClass.VQCN>>>,
                    APTranPost.curyAmt.Add<APTranPost.curyDiscAmt>.Add<APTranPost.curyWhTaxAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                    .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.curyAmt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.adjustment>
                        .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.curyAmt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.curyAmt.Add<APTranPost.curyDiscAmt>.Add<APTranPost.curyWhTaxAmt>>>>>>>,
                decimal0>), typeof(decimal))]
        [PXUIField(DisplayName = "Debit AP Amt.")]
        public virtual decimal? CuryDebitAPAmt { get; set; }

        #endregion

        #region DebitAPAmt

        public abstract class debitAPAmt : PX.Data.BQL.BqlDecimal.Field<debitAPAmt>
        {
        }
        
        [PXBaseCury]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPost.accountID.IsNull>, decimal0,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.voided>>,
                    PX.Data.Minus<APTranPost.amt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                    .And<APTranPost.glSign.IsEqual<shortMinus1>>
                    .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN, APTranPost.tranClass.VQCN>>>,
                    APTranPost.amt.Add<APTranPost.discAmt>.Add<APTranPost.whTaxAmt>.Add<APTranPost.rGOLAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                    .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.amt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.adjustment>
                        .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.amt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.amt.Add<APTranPost.discAmt>.Add<APTranPost.whTaxAmt>.Add<APTranPost.rGOLAmt>>>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? DebitAPAmt { get; set; }

        #endregion

        #region CuryCreditAPAmt

        public abstract class curyCreditAPAmt : PX.Data.BQL.BqlDecimal.Field<curyCreditAPAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(creditAPAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPost.accountID.IsNull>, decimal0,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                        .And<APTranPost.glSign.IsEqual<short1>>
                        .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN, APTranPost.tranClass.VQCN>>>,
                    APTranPost.curyAmt.Add<APTranPost.curyDiscAmt>.Add<APTranPost.curyWhTaxAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                    .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.curyAmt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.adjustment>
                        .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.curyAmt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.curyAmt.Add<APTranPost.curyDiscAmt>.Add<APTranPost.curyWhTaxAmt>>>>>>,
                decimal0>), typeof(decimal))]
        [PXUIField(DisplayName = "Credit AP Amt.")]
        public virtual decimal? CuryCreditAPAmt { get; set; }

        #endregion

        #region CreditAPAmt

        public abstract class creditAPAmt : PX.Data.BQL.BqlDecimal.Field<creditAPAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPost.accountID.IsNull>, decimal0,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                        .And<APTranPost.glSign.IsEqual<short1>>
                        .And<APTranPost.tranClass.IsIn<APTranPost.tranClass.QCKN, APTranPost.tranClass.VQCN>>>,
                    APTranPost.amt
                        .Add<APTranPost.discAmt>
                        .Add<APTranPost.whTaxAmt>
                        .Subtract<APTranPost.rGOLAmt>,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.origin>
                    .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.amt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.adjustment>
                        .And<APTranPost.glSign.IsEqual<short1>>>,
                    APTranPost.amt,
                Case<Where<APTranPost.type.IsEqual<APTranPost.type.application>
                    .And<APTranPost.glSign.IsEqual<shortMinus1>>>,
                    APTranPost.amt
                        .Add<APTranPost.discAmt>
                        .Add<APTranPost.whTaxAmt>
                        .Subtract<APTranPost.rGOLAmt>>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CreditAPAmt { get; set; }

        #endregion

        #region CuryTurnDiscAmt

        public abstract class curyTurnDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnDiscAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnDiscAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
            Switch<Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>>, 
                APTranPost.glSign.Multiply<APTranPost.curyDiscAmt>>, 
                decimal0> ), 
            typeof(decimal))]
        public virtual decimal? CuryTurnDiscAmt { get; set; }

        #endregion

        #region TurnDiscAmt

        public abstract class turnDiscAmt : PX.Data.BQL.BqlDecimal.Field<turnDiscAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
                Switch<Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>>, 
                        APTranPost.glSign.Multiply<APTranPost.discAmt>>, 
                    decimal0> ), 
            typeof(decimal))]
        public virtual decimal? TurnDiscAmt { get; set; }

        #endregion
        
        #region CuryTurnWHTaxAmt

        public abstract class curyTurnWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnWhTaxAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnWhTaxAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
                Switch<Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>
                        .And<APTranPost.tranClass.IsNotIn<APTranPost.tranClass.REFN,APTranPost.tranClass.VRFN>>>, 
                        APTranPost.glSign.Multiply<APTranPost.curyWhTaxAmt>>, 
                    decimal0> ), 
            typeof(decimal))]
        public virtual decimal? CuryTurnWHTaxAmt { get; set; }

        #endregion
        
        #region TurnWHTaxAmt

        public abstract class turnWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<turnWhTaxAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
                Switch<Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>
                        .And<APTranPost.tranClass.IsNotIn<APTranPost.tranClass.REFN,APTranPost.tranClass.VRFN>>>, 
                        APTranPost.glSign.Multiply<APTranPost.whTaxAmt>>, 
                    decimal0> ), 
            typeof(decimal))]
        public virtual decimal? TurnWHTaxAmt { get; set; }

        #endregion
        
        #region CuryTurnRetainageAmt

        public abstract class curyTurnRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnRetainageAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnRetainageAmt), BaseCalc = false)]
        [PXDBCalced(typeof(APTranPost.glSign.Multiply<APTranPost.curyRetainageAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnRetainageAmt { get; set; }

        #endregion
        
        #region TurnRetainageAmt

        public abstract class turnRetainageAmt : PX.Data.BQL.BqlDecimal.Field<turnRetainageAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(APTranPost.glSign.Multiply<APTranPost.retainageAmt>), typeof(decimal))]
        public virtual decimal? TurnRetainageAmt { get; set; }

        #endregion

        #region CuryRetainageReleasedAmt

        public abstract class curyRetainageReleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageReleasedAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(retainageReleasedAmt), BaseCalc = false)]
        [PXDBCalced(typeof(IIf<APTranPost.type.IsEqual<APTranPost.type.retainage>,
            Data.BQL.Minus<APTranPost.balanceSign>.Multiply<APTranPost.glSign.Multiply<APTranPost.curyRetainageAmt>>,
            Zero>), typeof(decimal))]
        public virtual decimal? CuryRetainageReleasedAmt { get; set; }

        #endregion
        
        #region RetainageReleasedAmt

        public abstract class retainageReleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageReleasedAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(IIf<APTranPost.type.IsEqual<APTranPost.type.retainage>,
            Data.BQL.Minus<APTranPost.balanceSign>.Multiply<APTranPost.glSign.Multiply<APTranPost.retainageAmt>>,
            Zero>), typeof(decimal))]
        public virtual decimal? RetainageReleasedAmt { get; set; }

        #endregion
        
        #region CuryRetainageUnreleasedAmt

        public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt), BaseCalc = false)]
        [PXDBCalced(typeof(APTranPost.balanceSign.Multiply<APTranPost.glSign.Multiply<APTranPost.curyRetainageAmt>>), typeof(decimal))]
        public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

        #endregion
        
        #region RetainageUnreleasedAmt

        public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(APTranPost.balanceSign.Multiply<APTranPost.glSign.Multiply<APTranPost.retainageAmt>>), typeof(decimal))]
        public virtual decimal? RetainageUnreleasedAmt { get; set; }

        #endregion

        #region TurnRGOLAmt

        public abstract class turnRGOLAmt : PX.Data.BQL.BqlDecimal.Field<turnRGOLAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
                Switch<Case<Where<APTranPost.type.IsEqual<APTranPost.type.rgol>>, 
                    APTranPost.rGOLAmt>, 
                decimal0>),
            typeof(decimal))]
        public virtual decimal? TurnRGOLAmt { get; set; }

        #endregion
        
        #region RGOLAmt
        public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        public virtual decimal? RGOLAmt { get; set; }
        #endregion
    }
}