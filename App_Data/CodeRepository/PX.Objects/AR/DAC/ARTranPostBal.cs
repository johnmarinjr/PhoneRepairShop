using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AR
{
    [PXProjection(typeof(Select<ARTranPost>), Persistent = false)]
    [PXHidden]
    public class ARTranPostBal : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<ARTranPostBal>.By<iD>
        {
            public static ARTranPostBal Find(PXGraph graph, int? id)  =>
                FindBy(graph, id);
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
        
        #region ID
        
        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        } 

        [PXDBInt(IsKey = true, BqlTable = typeof(ARTranPost))]
        public virtual int? ID { get; set; }

        #endregion
        
        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Doc. Type")]
        [ARDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString( BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPost))]
        [PXUIField(DisplayName = "Line Nbr.", FieldClass = nameof(FeaturesSet.PaymentsByLines))]
        public virtual int? LineNbr { get; set; }
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
        
        #region ApplicationDate
        public abstract class applicationDate : PX.Data.BQL.BqlDateTime.Field<applicationDate>
        {
        }
        [PXDBDate(BqlField = typeof(ARTranPost.docDate))]
        [PXUIField(DisplayName = "Application Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? ApplicationDate { get; set; }
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
        [PXDBString(15, IsUnicode = true, BqlTable = typeof(ARTranPost))]
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

        #region RefNoteID
        public new abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> 
        {
            public class NoteAttribute : PXNoteAttribute
            {
                public NoteAttribute()
                {
                    BqlTable = typeof(ARAdjust);
                }

                protected override bool IsVirtualTable(Type table)
                {
                    return false;
                }
            }
        }
        [refNoteID.Note(BqlTable = typeof(ARTranPost))]
        public virtual Guid? RefNoteID
        {
            get;
            set;
        }
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

        #region CuryAmt

        public abstract class curyAmt : PX.Data.BQL.BqlDecimal.Field<curyAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(amt), BaseCalc = false, BqlTable = typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? CuryAmt { get; set; }

        #endregion
        
        #region CuryPPDAmt
        public abstract class curyPPDAmt : PX.Data.BQL.BqlDecimal.Field<curyPPDAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(ppdAmt), BaseCalc = false, BqlTable = typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryPPDAmt { get; set; }

        #endregion
        #region CuryDiscAmt
        public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(discAmt), BaseCalc = false, BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Cash Discount Taken")]
        public virtual decimal? CuryDiscAmt { get; set; }

        #endregion
        #region CuryRetainageAmt

        public abstract class curyRetainageAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(retainageAmt), BaseCalc = false, BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? CuryRetainageAmt { get; set; }

        #endregion
        #region CuryWOAmt

        public abstract class curyWOAmt : PX.Data.BQL.BqlDecimal.Field<curyWOAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(wOAmt), BaseCalc = false, BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Write-Off Amount")]
        public virtual decimal? CuryWOAmt { get; set; }

        #endregion
        #region CuryItemDiscAmt

        public abstract class curyItemDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyItemDiscAmt>
        {
        }

        [PXDBCurrency(typeof(curyInfoID), typeof(itemDiscAmt), BaseCalc = false, BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
       
        public virtual decimal? CuryItemDiscAmt { get; set; }

        #endregion
        #region Amt

        public abstract class amt : PX.Data.BQL.BqlDecimal.Field<amt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? Amt { get; set; }

        #endregion
        #region PPDAmt
        public abstract class ppdAmt : PX.Data.BQL.BqlDecimal.Field<ppdAmt>
        {
        }

        [PXDBBaseCury(BqlTable = typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? PPDAmt { get; set; }

        #endregion
        #region DiscAmt

        public abstract class discAmt : PX.Data.BQL.BqlDecimal.Field<discAmt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? DiscAmt { get; set; }

        #endregion
        #region RetainageAmt

        public abstract class retainageAmt : PX.Data.BQL.BqlDecimal.Field<retainageAmt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? RetainageAmt { get; set; }

        #endregion
        #region WOAmt

        public abstract class wOAmt : PX.Data.BQL.BqlDecimal.Field<wOAmt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? WOAmt { get; set; }

        #endregion
        #region ItemDiscAmt

        public abstract class itemDiscAmt : PX.Data.BQL.BqlDecimal.Field<itemDiscAmt>
        {
        }

        [PXDBBaseCury(BqlTable=typeof(ARTranPost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? ItemDiscAmt { get; set; }

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
            Data.Round<
            Mult<
                Switch<Case<Where<Standalone.ARRegisterAlias.paymentsByLinesAllowed.IsEqual<True>>,
                        ARTran.curyTranBal>, 
                    Standalone.ARRegisterAlias.curyDocBal>,
            Switch<Case<Where<Standalone.ARRegisterAlias.curyID.IsEqual<CM.CurrencyInfo2.curyID>>,
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
                Case<Where<Standalone.ARRegisterAlias.paymentsByLinesAllowed.IsEqual<True>>,
                    ARTran.tranBal>, 
                Standalone.ARRegisterAlias.docBal>), typeof(decimal))]
        public virtual decimal? BalanceAmt { get; set; }

        #endregion
        
        #region CuryDiscBal
        public abstract class curyDiscBalanceAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscBalanceAmt>
        {
        }

        [PXCurrency(typeof(curyInfoID), typeof(discBalanceAmt), BaseCalc = false)]
        [PXUIField(DisplayName = "Cash Discount Balance")]
        [PXDBCalced(typeof(            
            PX.Data.Round<
                Mult<
                    Switch<Case<Where<Standalone.ARRegisterAlias.paymentsByLinesAllowed, Equal<True>>, ARTran.curyCashDiscBal>,
                        Standalone.ARRegisterAlias.curyDiscBal>,
                    Switch<Case<Where<Standalone.ARRegisterAlias.curyID.IsEqual<CM.CurrencyInfo2.curyID>>, decimal1>,
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
        [PXDBCalced(typeof(Switch<Case<Where<Standalone.ARRegisterAlias.paymentsByLinesAllowed, Equal<True>>, ARTran.curyCashDiscBal>,    
                                       Standalone.ARRegisterAlias.discBal>), typeof(decimal))]
        public virtual decimal? DiscBalanceAmt { get; set; }

        #endregion
        
    }
}