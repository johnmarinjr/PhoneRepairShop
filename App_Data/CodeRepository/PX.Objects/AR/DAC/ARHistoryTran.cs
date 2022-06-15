using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.CM;
using PX.Objects.GL;
using PX.Objects.CS;

namespace PX.Objects.AR
{
    [PXProjection(typeof(Select<ARTranPostGL>), Persistent = false)]
    [PXCacheName("AR History Transaction")]
    public class ARHistoryTran : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<ARTran>.By<docType, refNbr, lineNbr, iD>
        {
            public static ARTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
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

        #region ID

        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        }

        [PXDBInt(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        public virtual int? ID { get; set; }

        #endregion

        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Doc. Type")]
        [ARDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPostGL))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [ARDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion

        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
        {
        }

        [PXDBLong(BqlTable=typeof(ARTranPostGL))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [PXUIField(DisplayName = "Branch")]
        [Branch(BqlTable = typeof(ARTranPostGL))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region CustomerID

        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID>
        {
        }

        [Customer(BqlTable = typeof(ARTranPostGL))]
        public virtual int? CustomerID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(ARTranPostGL))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(ARTranPostGL))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(ARTranPostGL))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [ARTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranRefNbr { get; set; }

        #endregion

        #region ReferenceID

        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPostGL))]
        public virtual int? ReferenceID { get; set; }

        #endregion
        
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool(BqlTable = typeof(ARTranPostGL))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion

        #region PtdSales
        public abstract class ptdSales : PX.Data.BQL.BqlShort.Field<ptdSales>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.debitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>,
                    Data.BQL.Minus<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.CSLR,ARTranPost.tranClass.RCSR>>,
                    Data.BQL.Minus<ARTranPostGL.turnDiscAmt>
                    >>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdSales { get; set; }
        #endregion
        
        #region PtdPayments
        public abstract class ptdPayments : PX.Data.BQL.BqlShort.Field<ptdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.debitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>, 
                    Data.BQL.Minus<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CRMU>>, 
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>, 
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PMTU,ARTranPost.tranClass.RPMU>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMN,ARTranPost.tranClass.PMTN,
                        ARTranPost.tranClass.PPMN,ARTranPost.tranClass.REFN,
                        ARTranPost.tranClass.VRFN>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PPMB,ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMP,ARTranPost.tranClass.PMTP,
                        ARTranPost.tranClass.REFP,ARTranPost.tranClass.VRFP>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR,ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.PPMR,ARTranPost.tranClass.REFR,
                        ARTranPost.tranClass.VRFR,ARTranPost.tranClass.PMTX>>,
                    ARTranPostGL.turnWOAmt.Add<ARTranPostGL.turnDiscAmt>.Subtract<ARTranPostGL.rGOLAmt>>>>>>>>>,  
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.DRMN, ARTranPost.tranClass.SMCN, ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass
                        .IsIn<ARTranPost.tranClass.PPMU,ARTranPost.tranClass.PMTU,
                              ARTranPost.tranClass.RPMU,ARTranPost.tranClass.SMCU>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>, 
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.PMTX>>,
                    ARTranPostGL.turnWOAmt.Add<ARTranPostGL.turnDiscAmt>.Subtract<ARTranPostGL.rGOLAmt>>>>, 
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
                Case<Where<ARTranPostGL.isMigratedRecord.IsEqual<True>
                    .And<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.INVN>>>,
                    ARTranPostGL.creditARAmt,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.CRMN, ARTranPost.tranClass.CRMP,
                        ARTranPost.tranClass.SMBN,ARTranPost.tranClass.CRMU>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR, ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.REFR,ARTranPost.tranClass.VRFR,
                        ARTranPost.tranClass.PPMR>>,
                    Data.BQL.Minus<ARTranPostGL.turnWOAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.SMBR, ARTranPost.tranClass.CRMR>>,
                    ARTranPostGL.turnDiscAmt.Subtract<ARTranPostGL.rGOLAmt>>>>>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? PtdCrAdjustments { get; set; }
        #endregion
        
        #region PtdDiscounts
        public abstract class ptdDiscounts : PX.Data.BQL.BqlShort.Field<ptdDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.rgol>>,
                    Data.BQL.Minus<ARTranPostGL.turnDiscAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? PtdDiscounts { get; set; }
        #endregion
        
        #region PtdItemDiscounts
        public abstract class ptdItemDiscounts : PX.Data.BQL.BqlShort.Field<ptdItemDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(ARTranPostGL.curyTurnItemDiscAmt), typeof(decimal))]
        public virtual decimal? PtdItemDiscounts { get; set; }
        #endregion
        
        #region PtdRGOL
        public abstract class ptdRGOL : PX.Data.BQL.BqlShort.Field<ptdRGOL>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(ARTranPostGL.turnRGOLAmt), typeof(decimal))]
        public virtual decimal? PtdRGOL { get; set; }
        #endregion
        
        #region PtdFinCharges
        public abstract class ptdFinCharges : PX.Data.BQL.BqlShort.Field<ptdFinCharges>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.FCHN>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? PtdFinCharges { get; set; }
        #endregion

        #region PtdDeposits
        public abstract class ptdDeposits : PX.Data.BQL.BqlShort.Field<ptdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.PPMB>>,
                    short2.Multiply<
                        ARTranPostGL.creditARAmt
                        .Subtract<ARTranPostGL.debitARAmt>>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.PPMP,ARTranPost.tranClass.PPMU,
                        ARTranPost.tranClass.REFU,ARTranPost.tranClass.VRFU,
                        ARTranPost.tranClass.SMCU, ARTranPost.tranClass.CRMU,
                        ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.creditARAmt
                        .Subtract<ARTranPostGL.debitARAmt>>>,
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN, 
                            ARTranPost.tranClass.CRMN,ARTranPost.tranClass.DRMN>
                    .And<ARTranPostGL.type.IsEqual<ARTranPost.type.origin>>>,
                    ARTranPostGL.turnRetainageAmt>,
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
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.retainage>>,
                    Data.BQL.Minus<ARTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageReleased { get; set; }
        #endregion

    }
    
    [PXProjection(typeof(Select<ARTranPostGL>), Persistent = false)]
    [PXCacheName("AR History Currency Transaction")]
    public class CuryARHistoryTran : IBqlTable
    {
        #region Keys

        public class PK : PrimaryKeyOf<ARTran>.By<docType, refNbr, lineNbr, iD>
        {
            public static ARTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? id) =>
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

        #region ID

        public abstract class iD : PX.Data.BQL.BqlInt.Field<iD>
        {
        }

        [PXDBInt(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        public virtual int? ID { get; set; }

        #endregion

        #region DocType

        public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Doc. Type")]
        [ARDocType.List()]
        public virtual string DocType { get; set; }

        #endregion

        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        {
        }

        [PXDBString(IsKey = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string RefNbr { get; set; }
        #endregion

        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPostGL))] 
        [PXUIField(DisplayName = "Line Nbr.")]
        public virtual int? LineNbr { get; set; }
        #endregion

        #region SourceDocType

        public abstract class sourceDocType : PX.Data.BQL.BqlString.Field<sourceDocType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Source Doc. Type")]
        [ARDocType.List()]

        public virtual string SourceDocType { get; set; }

        #endregion

        #region SourceRefNbr

        public abstract class sourceRefNbr : PX.Data.BQL.BqlString.Field<sourceRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Source Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        public virtual string SourceRefNbr { get; set; }

        #endregion

        #region CuryID

        public abstract class curyID : PX.Data.BQL.BqlLong.Field<curyID>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))] 
        public virtual string CuryID { get; set; }

        #endregion
        
        #region CuryInfoID

        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
        {
        }

        [PXDBLong(BqlTable = typeof(ARTranPostGL))] 
        public virtual long? CuryInfoID { get; set; }

        #endregion

        #region BranchID

        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
        {
        }
        
        [PXUIField(DisplayName = "Batch")]
        [Branch(BqlTable = typeof(ARTranPostGL))]
        public virtual int? BranchID { get; set; }

        #endregion

        #region CustomerID

        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID>
        {
        }

        [Customer(BqlTable = typeof(ARTranPostGL))]
        public virtual int? CustomerID { get; set; }

        #endregion

        #region AccountID

        public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID>
        {
        }

        [Account(BqlTable = typeof(ARTranPostGL))]
        public virtual int? AccountID { get; set; }

        #endregion

        #region SubID

        public abstract class subID : PX.Data.BQL.BqlInt.Field<subID>
        {
        }

        [SubAccount(BqlTable = typeof(ARTranPostGL))]
        public virtual int? SubID { get; set; }

        #endregion
        
        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [FinPeriodID(BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Application Period")]
        public virtual string FinPeriodID { get; set; }

        #endregion
        
        #region TranPeriodID
        public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }
        [PeriodID(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranPeriodID { get; set; }
        #endregion
        
        #region BatchNbr
        public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr>
        {
        }

        [PXDBString(15, IsUnicode = true, BqlTable = typeof(ARTranPostGL))]
        [PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
            IsMigratedRecordField = typeof(isMigratedRecord),
            BqlTable = typeof(ARTranPostGL))]
        public virtual string BatchNbr { get; set; }

        #endregion

        #region Type

        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        [ARTranPost.type.List]
        public virtual string Type { get; set; }

        #endregion

        #region TranType

        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranType { get; set; }

        #endregion

        #region TranRefNbr

        public abstract class tranRefNbr : PX.Data.BQL.BqlString.Field<tranRefNbr>
        {
        }

        [PXDBString(BqlTable = typeof(ARTranPostGL))]
        public virtual string TranRefNbr { get; set; }

        #endregion

        #region ReferenceID

        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID>
        {
        }

        [PXDBInt(BqlTable = typeof(ARTranPostGL))]
        public virtual int? ReferenceID { get; set; }

        #endregion
        
        #region IsMigratedRecord
        public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        /// <summary>
        /// Specifies (if set to <c>true</c>) that the record has been created 
        /// in migration mode without affecting GL module.
        /// </summary>
        [PXDBBool(BqlTable = typeof(ARTranPostGL))]
        public virtual bool? IsMigratedRecord {get; set; }
        #endregion

        #region CuryPtdSales
        public abstract class curyPtdSales : PX.Data.BQL.BqlShort.Field<curyPtdSales>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.curyDebitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>,
                    Data.BQL.Minus<ARTranPostGL.curyCreditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.CSLR,ARTranPost.tranClass.RCSR>>,
                    Data.BQL.Minus<ARTranPostGL.curyTurnDiscAmt>>>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdSales { get; set; }
        #endregion
        
        #region CuryPtdPayments
        public abstract class curyPtdPayments : PX.Data.BQL.BqlShort.Field<curyPtdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.curyDebitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>, 
                    Data.BQL.Minus<ARTranPostGL.curyCreditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CRMU>>, 
                    ARTranPostGL.curyDebitARAmt.Subtract<ARTranPostGL.curyCreditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PMTU,ARTranPost.tranClass.RPMU>>,
                    ARTranPostGL.curyCreditARAmt.Subtract<ARTranPostGL.curyDebitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMN,ARTranPost.tranClass.PMTN,
                        ARTranPost.tranClass.PPMN,ARTranPost.tranClass.REFN,
                        ARTranPost.tranClass.VRFN>>,
                    ARTranPostGL.curyCreditARAmt.Subtract<ARTranPostGL.curyDebitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PPMB,ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.curyDebitARAmt.Subtract<ARTranPostGL.curyCreditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMP,ARTranPost.tranClass.PMTP,
                        ARTranPost.tranClass.REFP,ARTranPost.tranClass.VRFP>>,
                    ARTranPostGL.curyCreditARAmt.Subtract<ARTranPostGL.curyDebitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR,ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.PPMR,ARTranPost.tranClass.REFR,
                        ARTranPost.tranClass.VRFR>>,
                    ARTranPostGL.curyTurnWOAmt.Add<ARTranPostGL.curyTurnDiscAmt>>>>>>>>>,  
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.DRMN, ARTranPost.tranClass.SMCN, ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.curyDebitARAmt.Subtract<ARTranPostGL.curyCreditARAmt>,
                Case<Where<ARTranPostGL.tranClass
                        .IsIn<ARTranPost.tranClass.PPMU,ARTranPost.tranClass.PMTU,
                              ARTranPost.tranClass.RPMU,ARTranPost.tranClass.SMCU>>,
                    ARTranPostGL.curyCreditARAmt.Subtract<ARTranPostGL.curyDebitARAmt>>>, 
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
                Case<Where<ARTranPostGL.isMigratedRecord.IsEqual<True>
                    .And<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.INVN>>>,
                    ARTranPostGL.curyCreditARAmt,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.CRMN, ARTranPost.tranClass.CRMP,
                        ARTranPost.tranClass.SMBN,ARTranPost.tranClass.CRMU>>,
                    ARTranPostGL.curyCreditARAmt.Subtract<ARTranPostGL.curyDebitARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR, ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.REFR,ARTranPost.tranClass.VRFR,
                        ARTranPost.tranClass.PPMR>>,
                    Data.BQL.Minus<ARTranPostGL.curyTurnWOAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.SMBR, ARTranPost.tranClass.CRMR>>,
                    ARTranPostGL.curyTurnDiscAmt>>>>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdCrAdjustments { get; set; }
        #endregion
        
        #region CuryPtdDiscounts
        public abstract class curyPtdDiscounts : PX.Data.BQL.BqlShort.Field<curyPtdDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.rgol>>,
                    Data.BQL.Minus<ARTranPostGL.curyTurnDiscAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdDiscounts { get; set; }
        #endregion
        
        #region CuryPtdItemDiscounts
        public abstract class curyPtdItemDiscounts : PX.Data.BQL.BqlShort.Field<curyPtdItemDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(ARTranPostGL.turnItemDiscAmt), typeof(decimal))]
        public virtual decimal? CuryPtdItemDiscounts { get; set; }
        #endregion
        
        #region CuryPtdFinCharges
        public abstract class curyPtdFinCharges : PX.Data.BQL.BqlShort.Field<curyPtdFinCharges>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.FCHN>>,
                    ARTranPostGL.curyDebitARAmt.Subtract<ARTranPostGL.curyCreditARAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdFinCharges { get; set; }
        #endregion

        #region CuryPtdDeposits
        public abstract class curyPtdDeposits : PX.Data.BQL.BqlShort.Field<curyPtdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.PPMB>>,
                    short2.Multiply<
                        ARTranPostGL.creditARAmt
                        .Subtract<ARTranPostGL.debitARAmt>>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.PPMP, ARTranPost.tranClass.PPMU, 
                        ARTranPost.tranClass.REFU,ARTranPost.tranClass.VRFU,
                        ARTranPost.tranClass.SMCU, ARTranPost.tranClass.CRMU,
                        ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.curyCreditARAmt
                    .Subtract<ARTranPostGL.curyDebitARAmt>>>,
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN, 
                            ARTranPost.tranClass.CRMN,ARTranPost.tranClass.DRMN>
                    .And<ARTranPostGL.type.IsEqual<ARTranPost.type.origin>>>,
                    ARTranPostGL.turnRetainageAmt>,
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
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.retainage>>,
                    Data.BQL.Minus<ARTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? CuryPtdRetainageReleased { get; set; }
        #endregion
        
        #region PtdSales
        public abstract class ptdSales : PX.Data.BQL.BqlShort.Field<ptdSales>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.debitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>,
                    Data.BQL.Minus<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.CSLR,ARTranPost.tranClass.RCSR>>,
                    Data.BQL.Minus<ARTranPostGL.turnDiscAmt>>>>,
        decimal0>), typeof(decimal))]
        public virtual decimal? PtdSales { get; set; }
        #endregion
        
        #region PtdPayments
        public abstract class ptdPayments : PX.Data.BQL.BqlShort.Field<ptdPayments>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CSLN>>,
                    ARTranPostGL.debitARAmt,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.RCSN>>, 
                    Data.BQL.Minus<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.CRMU>>, 
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PMTU,ARTranPost.tranClass.RPMU>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMN,ARTranPost.tranClass.PMTN,
                        ARTranPost.tranClass.PPMN,ARTranPost.tranClass.REFN,
                        ARTranPost.tranClass.VRFN>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.PPMB,ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMP,ARTranPost.tranClass.PMTP,
                        ARTranPost.tranClass.REFP,ARTranPost.tranClass.VRFP>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,  
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR,ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.PPMR,ARTranPost.tranClass.REFR,
                        ARTranPost.tranClass.VRFR,ARTranPost.tranClass.PMTX>>,
                    ARTranPostGL.turnWOAmt.Add<ARTranPostGL.turnDiscAmt>.Subtract<ARTranPostGL.rGOLAmt>>>>>>>>>,  
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.DRMN, ARTranPost.tranClass.SMCN, ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>,
                Case<Where<ARTranPostGL.tranClass
                        .IsIn<ARTranPost.tranClass.PPMU,ARTranPost.tranClass.PMTU,
                              ARTranPost.tranClass.RPMU,ARTranPost.tranClass.SMCU>>,
                    ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.PMTX>>,
                    ARTranPostGL.turnWOAmt.Add<ARTranPostGL.turnDiscAmt>.Subtract<ARTranPostGL.rGOLAmt>>>>,     
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
                Case<Where<ARTranPostGL.isMigratedRecord.IsEqual<True>
                    .And<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.INVN>>>,
                    ARTranPostGL.creditARAmt,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.CRMN, ARTranPost.tranClass.CRMP,
                        ARTranPost.tranClass.SMBN,ARTranPost.tranClass.CRMU>>,
                        ARTranPostGL.creditARAmt.Subtract<ARTranPostGL.debitARAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.RPMR, ARTranPost.tranClass.PMTR,
                        ARTranPost.tranClass.REFR,ARTranPost.tranClass.VRFR,
                        ARTranPost.tranClass.PPMR>>,
                    Data.BQL.Minus<ARTranPostGL.turnWOAmt>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                        ARTranPost.tranClass.SMBR, ARTranPost.tranClass.CRMR>>,
                    ARTranPostGL.turnDiscAmt.Subtract<ARTranPostGL.rGOLAmt>>>>>,
                    decimal0>), typeof(decimal))]
        public virtual decimal? PtdCrAdjustments { get; set; }
        #endregion
        
        #region PtdDiscounts
        public abstract class ptdDiscounts : PX.Data.BQL.BqlShort.Field<ptdDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.rgol>>,
                    Data.BQL.Minus<ARTranPostGL.turnDiscAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? PtdDiscounts { get; set; }
        #endregion
        
        #region PtdItemDiscounts
        public abstract class ptdItemDiscounts : PX.Data.BQL.BqlShort.Field<ptdItemDiscounts>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(ARTranPostGL.turnItemDiscAmt), typeof(decimal))]
        public virtual decimal? PtdItemDiscounts { get; set; }
        #endregion
        
        #region PtdRGOL
        public abstract class ptdRGOL : PX.Data.BQL.BqlShort.Field<ptdRGOL>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(ARTranPostGL.turnRGOLAmt), typeof(decimal))]
        public virtual decimal? PtdRGOL { get; set; }
        #endregion 
        
        #region PtdFinCharges
        public abstract class ptdFinCharges : PX.Data.BQL.BqlShort.Field<ptdFinCharges>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.FCHN>>,
                    ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>>,
            decimal0>), typeof(decimal))]
        public virtual decimal? PtdFinCharges { get; set; }
        #endregion

        #region PtdDeposits
        public abstract class ptdDeposits : PX.Data.BQL.BqlShort.Field<ptdDeposits>
        {
        }

        [PXDecimal(4)]
        [PXDBCalced(typeof(
            Switch<
                Case<Where<ARTranPostGL.tranClass.IsEqual<ARTranPost.tranClass.PPMB>>,
                    short2.Multiply<
                        ARTranPostGL.creditARAmt
                        .Subtract<ARTranPostGL.debitARAmt>>,
                Case<Where<ARTranPostGL.tranClass.IsIn<
                            ARTranPost.tranClass.PPMP, ARTranPost.tranClass.PPMU, 
                            ARTranPost.tranClass.REFU,ARTranPost.tranClass.VRFU,
                            ARTranPost.tranClass.SMCU, ARTranPost.tranClass.CRMU,
                            ARTranPost.tranClass.SMCB>>,
                    ARTranPostGL.creditARAmt
                    .Subtract<ARTranPostGL.debitARAmt>>>,
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
                Case<Where<ARTranPostGL.tranClass.IsIn<ARTranPost.tranClass.INVN, ARTranPost.tranClass.CSLN,
                                ARTranPost.tranClass.CRMN, ARTranPost.tranClass.DRMN>
                    .And<ARTranPostGL.type.IsEqual<ARTranPost.type.origin>>>,
                    ARTranPostGL.turnRetainageAmt>,
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
                Case<Where<ARTranPostGL.type.IsEqual<ARTranPost.type.retainage>>,
                    Data.BQL.Minus<ARTranPostGL.turnRetainageAmt>>,
                decimal0>), typeof(decimal))]
        public virtual decimal? PtdRetainageReleased { get; set; }
        #endregion

    }
}
