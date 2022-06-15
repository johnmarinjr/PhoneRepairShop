using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select2<ARTranPost, 
        LeftJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARTranPost.curyInfoID>>>>), Persistent = false)]
    [PXCacheName("AR Document Post GL")]
    public class ARTranPostGL : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<ARTranPostGL>.By<docType, refNbr, lineNbr, iD>
        {
            public static ARTranPostGL Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
                FindBy(graph, docType, refNbr, lineNbr, id);
        }

        public static class FK
        {
            public class Document : AR.ARRegister.PK.ForeignKeyOf<ARTran>.By<docType, refNbr>
            {
            }

            public class Invoice : AR.ARInvoice.PK.ForeignKeyOf<ARTran>.By<docType, refNbr>
            {
            }

            public class Payment : AR.ARPayment.PK.ForeignKeyOf<ARTran>.By<docType, refNbr>
            {
            }

            public class CashSale : Standalone.ARCashSale.PK.ForeignKeyOf<ARTran>.By<docType, refNbr>
            {
            }

            public class SOInvoice : SO.SOInvoice.PK.ForeignKeyOf<ARTran>.By<docType, refNbr>
            {
            }

            public class Branch : GL.Branch.PK.ForeignKeyOf<ARTran>.By<branchID>
            {
            }

            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<ARTran>.By<curyInfoID>
            {
            }

            public class Customer : AR.Customer.PK.ForeignKeyOf<ARTran>.By<customerID>
            {
            }

            public class Account : GL.Account.PK.ForeignKeyOf<ARTran>.By<accountID>
            {
            }

            public class Subaccount : GL.Sub.PK.ForeignKeyOf<ARTran>.By<subID>
            {
            }
        }

        #endregion
        
        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Doc. Type")]
        [ARDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPost))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region ID

        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        } 

        [PXDBInt(IsKey = true, BqlTable = typeof(ARTranPost))]
        public virtual int? ID { get; set; }

        #endregion

        #region AdjNbr
        public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr>
        {
        }
        [PXDBInt(BqlTable = typeof(ARTranPost))]
        public virtual int? AdjNbr { get; set; }
        #endregion
        
        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [ARDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
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

        [PXDBLong(BqlTable = typeof(ARTranPost))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [Branch(BqlTable = typeof(ARTranPost))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region DocDate
        public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
        {
        }
        [PXDBDate(BqlTable = typeof(ARTranPost))]
        [PXDefault(typeof(ARRegister.docDate))]
        [PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? DocDate { get; set; }
        #endregion
        
        #region CustomerID

        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID>
        {
        }

        [Customer(BqlTable = typeof(ARTranPost))]
        public virtual int? CustomerID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(ARTranPost))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(ARTranPost))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(ARTranPost))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BalanceSign
        public abstract class balanceSign : PX.Data.BQL.BqlShort.Field<balanceSign> { }
        [PXDBShort(BqlTable=typeof(ARTranPost))]
        public virtual short? BalanceSign { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }
        [PXDBString(15, IsUnicode = true,BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Batch Number", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(ARTranPost))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        [ARTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranClass

        public abstract class tranClass : PX.Data.BQL.BqlString.Field<tranClass>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        public virtual string TranClass { get; set; }

        #endregion

        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        public virtual string TranRefNbr { get; set; }

        #endregion

        #region ReferenceID

        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPost))]
        public virtual int? ReferenceID { get; set; }

        #endregion

        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool(BqlTable = typeof(ARTranPost))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion
        
        #region RGOLAmt

        public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? RGOLAmt { get; set; }

        #endregion

        #region CuryBalanceAmt

        public abstract class curyBalanceAmt : PX.Data.BQL.BqlDecimal.Field<curyBalanceAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(balanceAmt), BaseCalc = false)]
        [PXUIField(DisplayName = "Balance")]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPost.type.IsIn<ARTranPost.type.origin, ARTranPost.type.adjustment, ARTranPost.type.@void, ARTranPost.type.installment>>,
                    ARTranPost.glSign.Multiply<ARTranPost.curyAmt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>>,
                    Data.BQL.Minus<ARTranPost.glSign>.Multiply<ARTranPost.curyDiscAmt.Add<ARTranPost.curyWOAmt>.Add<ARTranPost.curyAmt>>>>,
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
                Case<Where<ARTranPost.type.IsIn<ARTranPost.type.origin, ARTranPost.type.adjustment, ARTranPost.type.@void, ARTranPost.type.installment>>,
                    ARTranPost.glSign.Multiply<ARTranPost.amt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>>,
                    Data.BQL.Minus<ARTranPost.glSign>.Multiply<
                        ARTranPost.amt.Add<ARTranPost.discAmt>.Add<ARTranPost.wOAmt>>.Subtract<ARTranPost.rGOLAmt>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? BalanceAmt { get; set; }

        #endregion

        #region CuryDebitARAmt

        public abstract class curyDebitARAmt : PX.Data.BQL.BqlDecimal.Field<curyDebitARAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(debitARAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPost.accountID.IsNull>, decimal0,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.origin>
                        .And<ARTranPost.glSign.IsEqual<short1>>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    ARTranPost.curyAmt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>.And<ARTranPost.glSign.IsEqual<short1>>>,
                    ARTranPost.curyAmt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>.And<ARTranPost.glSign.IsEqual<shortMinus1>>>,
                    ARTranPost.curyAmt.Add<ARTranPost.curyDiscAmt>.Add<ARTranPost.curyWOAmt>>>>>,
                decimal0>), typeof(decimal))]
        [PXUIField(DisplayName = "Debit AR Amt.")]
        public virtual decimal? CuryDebitARAmt { get; set; }

        #endregion

        #region DebitARAmt

        public abstract class debitARAmt : PX.Data.BQL.BqlDecimal.Field<debitARAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPost.accountID.IsNull>, decimal0,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.origin>
                        .And<ARTranPost.glSign.IsEqual<short1>>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    ARTranPost.amt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>.And<ARTranPost.glSign.IsEqual<short1>>>,
                    ARTranPost.amt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>.And<ARTranPost.glSign.IsEqual<shortMinus1>>>,
                    ARTranPost.amt.Add<ARTranPost.discAmt>.Add<ARTranPost.wOAmt>.Subtract<ARTranPost.rGOLAmt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rounding>.And<ARTranPost.rGOLAmt.IsLess<Zero>>>,
                    ARTranPost.rGOLAmt.Multiply<shortMinus1>
                        >>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? DebitARAmt { get; set; }

        #endregion

        #region CuryCreditARAmt

        public abstract class curyCreditARAmt : PX.Data.BQL.BqlDecimal.Field<curyCreditARAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(creditARAmt), BaseCalc = false)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPost.accountID.IsNull>, decimal0,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.origin>
                        .And<ARTranPost.glSign.IsEqual<shortMinus1>>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    ARTranPost.curyAmt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>.And<ARTranPost.glSign.IsEqual<shortMinus1>>>,
                    ARTranPost.curyAmt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.@void>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    Data.BQL.Minus<ARTranPost.curyAmt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>.And<ARTranPost.glSign.IsEqual<short1>>>,
                    ARTranPost.curyAmt.Add<ARTranPost.curyDiscAmt>.Add<ARTranPost.curyWOAmt>
                >>>>>, 
                decimal0>), typeof(decimal))]
        [PXUIField(DisplayName = "Credit AR Amt.")]
        public virtual decimal? CuryCreditARAmt { get; set; }

        #endregion

        #region CreditARAmt

        public abstract class creditARAmt : PX.Data.BQL.BqlDecimal.Field<creditARAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPost.accountID.IsNull>, decimal0,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.origin>
                        .And<ARTranPost.glSign.IsEqual<shortMinus1>>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    ARTranPost.amt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.adjustment>.And<ARTranPost.glSign.IsEqual<shortMinus1>>>,
                    ARTranPost.amt,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.@void>
                        .And<ARTranPost.docType.IsNotIn<ARDocType.smallCreditWO, ARDocType.smallBalanceWO>>>,
                    Data.BQL.Minus<ARTranPost.amt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.application>.And<ARTranPost.glSign.IsEqual<short1>>>,
                    ARTranPost.amt.Add<ARTranPost.discAmt>.Add<ARTranPost.wOAmt>.Add<ARTranPost.rGOLAmt>,
                Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rounding>.And<ARTranPost.rGOLAmt.IsGreater<Zero>>>,
                    ARTranPost.rGOLAmt
                >>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CreditARAmt { get; set; }

        #endregion

        #region CuryTurnAmt

        public abstract class curyTurnAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.curyAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnAmt { get; set; }

        #endregion

        #region TurnAmt

        public abstract class turnAmt : PX.Data.BQL.BqlDecimal.Field<turnAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.discAmt>), typeof(decimal))]
        public virtual decimal? TurnAmt { get; set; }

        #endregion
        
        #region CuryTurnDiscAmt

        public abstract class curyTurnDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnDiscAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnDiscAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.curyDiscAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnDiscAmt { get; set; }

        #endregion

        #region TurnDiscAmt

        public abstract class turnDiscAmt : PX.Data.BQL.BqlDecimal.Field<turnDiscAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.discAmt>), typeof(decimal))]
        public virtual decimal? TurnDiscAmt { get; set; }

        #endregion
        
        #region CuryTurnItemDiscAmt

        public abstract class curyTurnItemDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnItemDiscAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnItemDiscAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.curyItemDiscAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnItemDiscAmt { get; set; }

        #endregion

        #region TurnItemDiscAmt

        public abstract class turnItemDiscAmt : PX.Data.BQL.BqlDecimal.Field<turnItemDiscAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.itemDiscAmt>), typeof(decimal))]
        public virtual decimal? TurnItemDiscAmt { get; set; }

        #endregion

        #region CuryTurnWOAmt

        public abstract class curyTurnWOAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnWOAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnWOAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.curyWOAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnWOAmt { get; set; }

        #endregion
        
        #region TurnWOAmt

        public abstract class turnWOAmt : PX.Data.BQL.BqlDecimal.Field<turnWOAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.wOAmt>), typeof(decimal))]
        public virtual decimal? TurnWOAmt { get; set; }

        #endregion
        
        #region CuryTurnRetainageAmt

        public abstract class curyTurnRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyTurnRetainageAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(turnRetainageAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.curyRetainageAmt>), typeof(decimal))]
        public virtual decimal? CuryTurnRetainageAmt { get; set; }

        #endregion
        
        #region TurnRetainageAmt

        public abstract class turnRetainageAmt : PX.Data.BQL.BqlDecimal.Field<turnRetainageAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.glSign.Multiply<ARTranPost.retainageAmt>), typeof(decimal))]
        public virtual decimal? TurnRetainageAmt { get; set; }

        #endregion

        #region CuryRetainageReleasedAmt

        public abstract class curyRetainageReleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageReleasedAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(retainageReleasedAmt), BaseCalc = false)]
        [PXDBCalced(typeof(IIf<ARTranPost.type.IsEqual<ARTranPost.type.retainage>,
            Data.BQL.Minus<ARTranPost.balanceSign>.Multiply<ARTranPost.glSign.Multiply<ARTranPost.curyRetainageAmt>>,
            Zero>), typeof(decimal))]
        public virtual decimal? CuryRetainageReleasedAmt { get; set; }

        #endregion
        
        #region RetainageReleasedAmt

        public abstract class retainageReleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageReleasedAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(IIf<ARTranPost.type.IsEqual<ARTranPost.type.retainage>,
            Data.BQL.Minus<ARTranPost.balanceSign>.Multiply<ARTranPost.glSign.Multiply<ARTranPost.retainageAmt>>,
            Zero>), typeof(decimal))]
        public virtual decimal? RetainageReleasedAmt { get; set; }

        #endregion
        
        #region CuryRetainageUnreleasedAmt

        public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt), BaseCalc = false)]
        [PXDBCalced(typeof(ARTranPost.balanceSign.Multiply<ARTranPost.glSign.Multiply<ARTranPost.curyRetainageAmt>>), typeof(decimal))]
        public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

        #endregion
        
        #region RetainageUnreleasedAmt

        public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(ARTranPost.balanceSign.Multiply<ARTranPost.glSign.Multiply<ARTranPost.retainageAmt>>), typeof(decimal))]
        public virtual decimal? RetainageUnreleasedAmt { get; set; }

        #endregion

        #region TurnRGOLAmt

        public abstract class turnRGOLAmt : PX.Data.BQL.BqlDecimal.Field<turnRGOLAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
                Switch<Case<Where<ARTranPost.type.IsEqual<ARTranPost.type.rgol>>, 
                    ARTranPost.rGOLAmt>, 
                decimal0>),
            typeof(decimal))]
        public virtual decimal? TurnRGOLAmt { get; set; }

        #endregion
        
    }
}
