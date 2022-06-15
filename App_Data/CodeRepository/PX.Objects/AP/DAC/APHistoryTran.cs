using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.GL;
using PX.Objects.CS;

namespace PX.Objects.AP
{
    [PXProjection(typeof(Select<APTranPostGL>), Persistent = false)]
    [PXCacheName("AP History Transaction")]
    public class APHistoryTran : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<APHistoryTran>.By<docType, refNbr, lineNbr, iD>
        {
            public static APHistoryTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
                FindBy(graph, docType, refNbr, lineNbr, id);
        }

        public static class FK
        {
            public class Document : AP.APRegister.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class Invoice : AP.APInvoice.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class Payment : AP.APPayment.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class CashSale : Standalone.APQuickCheck.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
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

        [PXDBInt(IsKey = true, BqlTable = typeof(APTranPostGL))]
        public virtual int? ID { get; set; }

        #endregion

        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Doc. Type")]
        [APDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(APTranPostGL))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [APDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion

        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
        {
        }

        [PXDBLong(BqlTable=typeof(APTranPostGL))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [PXUIField(DisplayName = "Branch")]
        [Branch(BqlTable = typeof(APTranPostGL))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region VendorID

        public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
        {
        }

        [Vendor(BqlTable = typeof(APTranPostGL))]
        public virtual int? VendorID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(APTranPostGL))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(APTranPostGL))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(APTranPostGL))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(APTranPostGL))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [APTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        public virtual string TranRefNbr { get; set; }

        #endregion
        
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool(BqlTable = typeof(APTranPostGL))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion

        #region PtdPurchases
        public abstract class ptdPurchases : PX.Data.BQL.BqlShort.Field<ptdPurchases>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>,
                    APTranPostGL.creditAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.debitAPAmt>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdPurchases { get; set; }
        #endregion
        
        #region PtdPayments
        public abstract class ptdPayments : PX.Data.BQL.BqlShort.Field<ptdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.debitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKN,APTranPost.tranClass.CHKN,
                        APTranPost.tranClass.PPMN,APTranPost.tranClass.REFN,
                        APTranPost.tranClass.VRFN>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,  
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKP,APTranPost.tranClass.CHKP,
                        APTranPost.tranClass.REFP,APTranPost.tranClass.VRFP,
                        APTranPost.tranClass.ADRU>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.REFR, APTranPost.tranClass.VRFR>>,
                    Data.BQL.Minus<APTranPostGL.turnRGOLAmt>, 
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.CHKR,APTranPost.tranClass.PPMR,
                        APTranPost.tranClass.VCKR,APTranPost.tranClass.VQCR,
                        APTranPost.tranClass.QCKR,APTranPost.tranClass.VRFR>>,
                    Data.BQL.Minus<APTranPostGL.turnWhTaxAmt.Add<APTranPostGL.turnDiscAmt>.Add<APTranPostGL.rGOLAmt>>
                    >>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdPayments { get; set; }
        #endregion
        
        #region PtdDrAdjustments
        public abstract class ptdDrAdjustments : PX.Data.BQL.BqlShort.Field<ptdDrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.isMigratedRecord.IsEqual<True>
                        .And<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.creditAPAmt.Subtract<APTranPostGL.debitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.ADRP,APTranPost.tranClass.ADRN>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRR>>,
                    Data.BQL.Minus<APTranPostGL.turnWhTaxAmt.Add<APTranPostGL.turnDiscAmt>.Add<APTranPostGL.rGOLAmt>>
                    >>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdDrAdjustments { get; set; }
        #endregion
        
        #region PtdCrAdjustments
        public abstract class ptdCrAdjustments : PX.Data.BQL.BqlShort.Field<ptdCrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ACRN>>,
                    APTranPostGL.creditAPAmt.Subtract<APTranPostGL.debitAPAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdCrAdjustments { get; set; }
        #endregion
        
        #region PtdDiscTaken
        public abstract class ptdDiscTaken : PX.Data.BQL.BqlShort.Field<ptdDiscTaken>
        {
        }
        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnDiscAmt), typeof(decimal))]
        public virtual decimal? PtdDiscTaken { get; set; }
        #endregion
        
        #region PtdWhTax
        public abstract class ptdWhTax : PX.Data.BQL.BqlShort.Field<ptdWhTax>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnWhTaxAmt), typeof(decimal))]
        public virtual decimal? PtdWhTax { get; set; }
        #endregion
        
        #region PtdRGOL
        public abstract class ptdRGOL : PX.Data.BQL.BqlShort.Field<ptdRGOL>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnRGOLAmt), typeof(decimal))]
        public virtual decimal? PtdRGOL { get; set; }
        #endregion
        
        #region PtdDeposits
        public abstract class ptdDeposits : PX.Data.BQL.BqlShort.Field<ptdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.creditAPAmt
                    .Subtract<APTranPostGL.debitAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.PPMP, APTranPost.tranClass.PPMU,
                        APTranPost.tranClass.CHKU,APTranPost.tranClass.VCKU,
                        APTranPost.tranClass.REFU, APTranPost.tranClass.VRFU>>,
                    APTranPostGL.debitAPAmt
                        .Subtract<APTranPostGL.creditAPAmt>
                >>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdDeposits { get; set; }
        #endregion
        
        #region PtdRetainageWithheld
        public abstract class ptdRetainageWithheld : PX.Data.BQL.BqlShort.Field<ptdRetainageWithheld>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.INVN, APTranPost.tranClass.QCKN, APTranPost.tranClass.ADRN>
                    .And<APTranPostGL.type.IsEqual<APTranPost.type.origin>>>,
                    APTranPostGL.turnRetainageAmt>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageWithheld { get; set; }
        #endregion
        
        #region PtdRetainageReleased
        public abstract class ptdRetainageReleased : PX.Data.BQL.BqlShort.Field<ptdRetainageReleased>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.type.IsEqual<APTranPost.type.retainage>>,
                    Data.BQL.Minus<APTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageReleased { get; set; }
        #endregion

    }
    
    [PXProjection(typeof(Select<APTranPostGL>), Persistent = false)]
    [PXCacheName("AP History Transaction")]
    public class CuryAPHistoryTran : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<CuryAPHistoryTran>.By<docType, refNbr, lineNbr, iD>
        {
            public static CuryAPHistoryTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
                FindBy(graph, docType, refNbr, lineNbr, id);
        }

        public static class FK
        {
            public class Document : AP.APRegister.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class Invoice : AP.APInvoice.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class Payment : AP.APPayment.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
            public class CashSale : Standalone.APQuickCheck.PK.ForeignKeyOf<APTran>.By<docType, refNbr> { }
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

        [PXDBInt(IsKey = true, BqlTable = typeof(APTranPostGL))]
        public virtual int? ID { get; set; }

        #endregion

        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Doc. Type")]
        [APDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(APTranPostGL))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [APDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion

        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
        {
        }

        [PXDBLong(BqlTable=typeof(APTranPostGL))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region CuryID

        public abstract class curyID : PX.Data.BQL.BqlLong.Field<curyID>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))] 
        public virtual string CuryID { get; set; }

        #endregion
        
        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [PXUIField(DisplayName = "Branch")]
        [Branch(BqlTable = typeof(APTranPostGL))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region VendorID

        public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
        {
        }

        [Vendor(BqlTable = typeof(APTranPostGL))]
        public virtual int? VendorID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(APTranPostGL))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(APTranPostGL))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(APTranPostGL))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true, BqlTable = typeof(APTranPostGL))]
        [PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(APTranPostGL))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        [APTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(APTranPostGL))]
        public virtual string TranRefNbr { get; set; }

        #endregion
        
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool(BqlTable = typeof(APTranPostGL))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion
        
        #region CuryPtdPurchases
        public abstract class curyPtdPurchases : PX.Data.BQL.BqlShort.Field<curyPtdPurchases>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>,
                    APTranPostGL.curyCreditAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.curyDebitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.curyDebitAPAmt>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdPurchases { get; set; }
        #endregion
        
        #region CuryPtdPayments
        public abstract class curyPtdPayments : PX.Data.BQL.BqlShort.Field<curyPtdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.curyDebitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.curyDebitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKN,APTranPost.tranClass.CHKN,
                        APTranPost.tranClass.PPMN,APTranPost.tranClass.REFN,
                        APTranPost.tranClass.VRFN>>,
                    APTranPostGL.curyDebitAPAmt.Subtract<APTranPostGL.curyCreditAPAmt>,  
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKP,APTranPost.tranClass.CHKP,
                        APTranPost.tranClass.REFP,APTranPost.tranClass.VRFP,
                        APTranPost.tranClass.ADRU>>,
                    APTranPostGL.curyDebitAPAmt.Subtract<APTranPostGL.curyCreditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.CHKR,APTranPost.tranClass.PPMR,
                        APTranPost.tranClass.VCKR,APTranPost.tranClass.VQCR,
                        APTranPost.tranClass.QCKR,APTranPost.tranClass.VRFR>>,
                    Data.BQL.Minus<APTranPostGL.curyTurnWhTaxAmt.Add<APTranPostGL.curyTurnDiscAmt>>
                    >>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdPayments { get; set; }
        #endregion
        
        #region CuryPtdDrAdjustments
        public abstract class curyPtdDrAdjustments : PX.Data.BQL.BqlShort.Field<curyPtdDrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.isMigratedRecord.IsEqual<True>
                        .And<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>>,
                    APTranPostGL.curyDebitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.curyCreditAPAmt.Subtract<APTranPostGL.curyDebitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.ADRP,APTranPost.tranClass.ADRN>>,
                    APTranPostGL.curyDebitAPAmt.Subtract<APTranPostGL.curyCreditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRR>>,
                    Data.BQL.Minus<APTranPostGL.curyTurnWhTaxAmt.Add<APTranPostGL.curyTurnDiscAmt>>
                    >>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdDrAdjustments { get; set; }
        #endregion
        
        #region CuryPtdCrAdjustments
        public abstract class curyPtdCrAdjustments : PX.Data.BQL.BqlShort.Field<curyPtdCrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ACRN>>,
                    APTranPostGL.curyCreditAPAmt.Subtract<APTranPostGL.curyDebitAPAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdCrAdjustments { get; set; }
        #endregion
        
        #region CuryPtdDiscTaken
        public abstract class curyPtdDiscTaken : PX.Data.BQL.BqlShort.Field<curyPtdDiscTaken>
        {
        }
        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.curyTurnDiscAmt), typeof(decimal))]
        public virtual decimal? CuryPtdDiscTaken { get; set; }
        #endregion
        
        #region CuryPtdWhTax
        public abstract class curyPtdWhTax : PX.Data.BQL.BqlShort.Field<curyPtdWhTax>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.curyTurnWhTaxAmt), typeof(decimal))]
        public virtual decimal? CuryPtdWhTax { get; set; }
        #endregion
        
        #region CuryPtdDeposits
        public abstract class curyPtdDeposits : PX.Data.BQL.BqlShort.Field<curyPtdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.curyCreditAPAmt
                    .Subtract<APTranPostGL.curyDebitAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.PPMP, APTranPost.tranClass.PPMU,
                        APTranPost.tranClass.CHKU,APTranPost.tranClass.VCKU,
                        APTranPost.tranClass.REFU, APTranPost.tranClass.VRFU>>,
                    APTranPostGL.curyDebitAPAmt
                        .Subtract<APTranPostGL.curyCreditAPAmt>
                >>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdDeposits { get; set; }
        #endregion
        
        #region CuryPtdRetainageWithheld
        public abstract class curyPtdRetainageWithheld : PX.Data.BQL.BqlShort.Field<curyPtdRetainageWithheld>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.INVN, APTranPost.tranClass.QCKN, APTranPost.tranClass.ADRN>
                    .And<APTranPostGL.type.IsEqual<APTranPost.type.origin>>>,
                    APTranPostGL.turnRetainageAmt>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdRetainageWithheld { get; set; }
        #endregion
        
        #region CuryPtdRetainageReleased
        public abstract class curyPtdRetainageReleased : PX.Data.BQL.BqlShort.Field<curyPtdRetainageReleased>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.type.IsEqual<APTranPost.type.retainage>>,
                    Data.BQL.Minus<APTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdRetainageReleased { get; set; }
        #endregion
        
        #region PtdPurchases
        public abstract class ptdPurchases : PX.Data.BQL.BqlShort.Field<ptdPurchases>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>,
                    APTranPostGL.creditAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.debitAPAmt>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdPurchases { get; set; }
        #endregion
        
        #region PtdPayments
        public abstract class ptdPayments : PX.Data.BQL.BqlShort.Field<ptdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.QCKN>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.VQCN>>,
                    Data.BQL.Minus<APTranPostGL.debitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKN,APTranPost.tranClass.CHKN,
                        APTranPost.tranClass.PPMN,APTranPost.tranClass.REFN,
                        APTranPost.tranClass.VRFN>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,  
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.VCKP,APTranPost.tranClass.CHKP,
                        APTranPost.tranClass.REFP,APTranPost.tranClass.VRFP,
                        APTranPost.tranClass.ADRU>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.REFR, APTranPost.tranClass.VRFR>>,
                    Data.BQL.Minus<APTranPostGL.turnRGOLAmt>, 
                Case<Where<APTranPostGL.tranClass.IsIn<
                        APTranPost.tranClass.CHKR,APTranPost.tranClass.PPMR,
                        APTranPost.tranClass.VCKR,APTranPost.tranClass.VQCR,
                        APTranPost.tranClass.QCKR,APTranPost.tranClass.VRFR>>,
                    Data.BQL.Minus<APTranPostGL.turnWhTaxAmt.Add<APTranPostGL.turnDiscAmt>.Add<APTranPostGL.rGOLAmt>>
                    >>>>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdPayments { get; set; }
        #endregion
        
        #region PtdDrAdjustments
        public abstract class ptdDrAdjustments : PX.Data.BQL.BqlShort.Field<ptdDrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.isMigratedRecord.IsEqual<True>
                        .And<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.INVN>>>,
                    APTranPostGL.debitAPAmt,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.creditAPAmt.Subtract<APTranPostGL.debitAPAmt>,    
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.ADRP,APTranPost.tranClass.ADRN>>,
                    APTranPostGL.debitAPAmt.Subtract<APTranPostGL.creditAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRR>>,
                    Data.BQL.Minus<APTranPostGL.turnWhTaxAmt.Add<APTranPostGL.turnDiscAmt>.Add<APTranPostGL.rGOLAmt>>
                    >>>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdDrAdjustments { get; set; }
        #endregion
        
        #region PtdCrAdjustments
        public abstract class ptdCrAdjustments : PX.Data.BQL.BqlShort.Field<ptdCrAdjustments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ACRN>>,
                    APTranPostGL.creditAPAmt.Subtract<APTranPostGL.debitAPAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdCrAdjustments { get; set; }
        #endregion
        
        #region PtdDiscTaken
        public abstract class ptdDiscTaken : PX.Data.BQL.BqlShort.Field<ptdDiscTaken>
        {
        }
        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnDiscAmt), typeof(decimal))]
        public virtual decimal? PtdDiscTaken { get; set; }
        #endregion
        
        #region PtdWhTax
        public abstract class ptdWhTax : PX.Data.BQL.BqlShort.Field<ptdWhTax>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnWhTaxAmt), typeof(decimal))]
        public virtual decimal? PtdWhTax { get; set; }
        #endregion
        
        #region PtdRGOL
        public abstract class ptdRGOL : PX.Data.BQL.BqlShort.Field<ptdRGOL>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranPostGL.turnRGOLAmt), typeof(decimal))]
        public virtual decimal? PtdRGOL { get; set; }
        #endregion
        
        #region PtdDeposits
        public abstract class ptdDeposits : PX.Data.BQL.BqlShort.Field<ptdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsEqual<APTranPost.tranClass.ADRU>>,
                    APTranPostGL.creditAPAmt
                    .Subtract<APTranPostGL.debitAPAmt>,
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.PPMP, APTranPost.tranClass.PPMU,
                        APTranPost.tranClass.CHKU,APTranPost.tranClass.VCKU,
                        APTranPost.tranClass.REFU, APTranPost.tranClass.VRFU>>,
                    APTranPostGL.debitAPAmt
                        .Subtract<APTranPostGL.creditAPAmt>
                >>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdDeposits { get; set; }
        #endregion
        
        #region PtdRetainageWithheld
        public abstract class ptdRetainageWithheld : PX.Data.BQL.BqlShort.Field<ptdRetainageWithheld>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.tranClass.IsIn<APTranPost.tranClass.INVN, APTranPost.tranClass.QCKN, APTranPost.tranClass.ADRN>
                    .And<APTranPostGL.type.IsEqual<APTranPost.type.origin>>>,
                    APTranPostGL.turnRetainageAmt>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageWithheld { get; set; }
        #endregion
        
        #region PtdRetainageReleased
        public abstract class ptdRetainageReleased : PX.Data.BQL.BqlShort.Field<ptdRetainageReleased>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<APTranPostGL.type.IsEqual<APTranPost.type.retainage>>,
                    Data.BQL.Minus<APTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageReleased { get; set; }
        #endregion
    }
}