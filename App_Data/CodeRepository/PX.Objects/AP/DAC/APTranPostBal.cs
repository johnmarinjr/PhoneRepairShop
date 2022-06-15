using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AP
{
	[PXProjection(typeof(Select<APTranPost>), Persistent = false)]
    [PXHidden]
    public class APTranPostBal : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<APTranPostBal>.By<iD>
        {
            public static APTranPostBal Find(PXGraph graph, int? id) =>
                FindBy(graph, id);
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
        
        #region ID

        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        } 

        [PXDBInt(IsKey = true, BqlTable = typeof(APTranPost))]
        public virtual int? ID { get; set; }

        #endregion
        
        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Doc. Type")]
        [APDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPost))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(APTranPost))] 
        [PXUIField(DisplayName = "Line Nbr.", FieldClass = nameof(FeaturesSet.PaymentsByLines))]
        public virtual int? LineNbr { get; set; }
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
        
        #region ApplicationDate
        public abstract class applicationDate : PX.Data.BQL.BqlDateTime.Field<applicationDate>
        {
        }
        [PXDBDate(BqlField = typeof(APTranPost.docDate))]
        [PXUIField(DisplayName = "Application Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? ApplicationDate { get; set; }
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

        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summAPy>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summAPy>
        [PXDBBool(BqlTable = typeof(APTranPost))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion

        #region RefNoteID
        public new abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> 
        {
            public class NoteAttribute : PXNoteAttribute
            {
                public NoteAttribute()
                {
                    BqlTable = typeof(APAdjust);
                }

                protected override bool IsVirtualTable(Type table)
                {
                    return false;
                }
            }
        }
        [refNoteID.Note(BqlTable = typeof(APTranPost))]
        public virtual Guid? RefNoteID
        {
            get;
            set;
        }
        #endregion
        
        #region DocDate
        public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
        {
        }
        [PXDBDate( BqlTable = typeof(APTranPost))]
        [PXDefault(typeof(APRegister.docDate))]
        [PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? DocDate { get; set; }
        #endregion
        
        #region CuryAmt
        public abstract class curyAmt : PX.Data.BQL.BqlDecimal.Field<curyAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(amt), BaseCalc = false, BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? CuryAmt { get; set; }

        #endregion
        
        #region CuryPPDAmt
        public abstract class curyPPDAmt : PX.Data.BQL.BqlDecimal.Field<curyPPDAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(ppdAmt), BaseCalc = false, BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryPPDAmt { get; set; }

        #endregion

        #region CuryDiscAmt
        public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(discAmt), BaseCalc = false, BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryDiscAmt { get; set; }

        #endregion

        #region CuryRetainageAmt

        public abstract class curyRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(retainageAmt), BaseCalc = false, BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? CuryRetainageAmt { get; set; }

        #endregion

        #region CuryWhTaxAmt

        public abstract class curyWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyWhTaxAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(whTaxAmt), BaseCalc = false, BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "With. Tax")]

        public virtual decimal? CuryWhTaxAmt { get; set; }

        #endregion
        
        #region Amt

        public abstract class amt : PX.Data.BQL.BqlDecimal.Field<amt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? Amt { get; set; }

        #endregion

        #region PPDAmt
        public abstract class ppdAmt : PX.Data.BQL.BqlDecimal.Field<ppdAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? PPDAmt { get; set; }

        #endregion

        
        #region DiscAmt

        public abstract class discAmt : PX.Data.BQL.BqlDecimal.Field<discAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? DiscAmt { get; set; }

        #endregion

        #region RetainageAmt

        public abstract class retainageAmt : PX.Data.BQL.BqlDecimal.Field<retainageAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? RetainageAmt { get; set; }

        #endregion

        #region WhTaxAmt

        public abstract class whTaxAmt : PX.Data.BQL.BqlDecimal.Field<whTaxAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? WhTaxAmt { get; set; }

        #endregion
        
        #region RGOLAmt

        public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(APTranPost))]
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
            Data.Round<
            Mult<
                Switch<Case<Where<Standalone.APRegister.paymentsByLinesAllowed.IsEqual<True>>,
                        APTran.curyTranBal>, 
                    Standalone.APRegister.curyDocBal>,                
            Switch<Case<Where<Standalone.APRegister.curyID.IsEqual<CM.CurrencyInfo2.curyID>>,
                decimal1>,
                Div<CurrencyInfo.curyRate, CM.CurrencyInfo2.curyRate>>>,
            CM.CurrencyInfo2.curyPrecision>), 
            typeof(decimal))]
        public virtual decimal? CuryBalanceAmt { get; set; }

        #endregion

        #region BalanceAmt
        public abstract class balanceAmt : PX.Data.BQL.BqlDecimal.Field<balanceAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<Standalone.APRegister.paymentsByLinesAllowed.IsEqual<True>>,
                    APTran.tranBal>, 
                Standalone.APRegister.docBal>), typeof(decimal))]
        public virtual decimal? BalanceAmt { get; set; }

        #endregion
        
        #region CuryDiscBal
        public abstract class curyDiscBalanceAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscBalanceAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(discBalanceAmt), BaseCalc = false)]
        [PXUIField(DisplayName = "Cash Discount Balance")]
        [PXDBCalced(typeof(
            Data.Round<
                Mult<Standalone.APRegister.curyDiscBal,
                    Switch<Case<Where<Standalone.APRegister.curyID.IsEqual<CM.CurrencyInfo2.curyID>>, decimal1>,
                    Div<CurrencyInfo.curyRate, CM.CurrencyInfo2.curyRate>>>,
                CM.CurrencyInfo2.curyPrecision>), 
            typeof(decimal))]
        public virtual decimal? CuryDiscBalanceAmt { get; set; }

        #endregion
        
        #region DiscBalanceAmt
        public abstract class discBalanceAmt : PX.Data.BQL.BqlDecimal.Field<discBalanceAmt>
        {
        }

        [PXBaseCury]
        [PXDBCalced(typeof(Standalone.APRegister.discBal), typeof(decimal))]
        public virtual decimal? DiscBalanceAmt { get; set; }

        #endregion
    }
}