using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
    [PXCacheName(Messages.POAccrualBalanceInquiryResult)]
    [PXProjection(typeof(
        SelectFrom<POAccrualDetail>
            .InnerJoin<POAccrualStatus>
                .On<POAccrualDetail.posted.IsEqual<True>
                .And<POAccrualDetail.branchID.Is<Inside<POAccrualInquiryFilter.orgBAccountID.FromCurrent>>>
                .And<POAccrualInquiryFilter.vendorID.FromCurrent.Value.IsNull
                    .Or<POAccrualDetail.vendorID.IsEqual<POAccrualInquiryFilter.vendorID.FromCurrent.Value>>>
                .And<POAccrualDetail.finPeriodID.IsLessEqual<POAccrualInquiryFilter.finPeriodID.FromCurrent.Value>>
                .And<POAccrualStatus.closedFinPeriodID.IsNull
                    .Or<POAccrualStatus.closedFinPeriodID.IsGreater<POAccrualInquiryFilter.finPeriodID.FromCurrent.Value>>>
                .And<POAccrualStatus.acctID.IsEqual<POAccrualInquiryFilter.acctID.FromCurrent.Value>>
                .And<POAccrualDetail.FK.AccrualStatus>>
            .LeftJoin<Sub>
                .On<POAccrualStatus.FK.Subaccount>
            .LeftJoin<POReceiptLineAccrual>
				.On<POReceiptLineAccrual.pOReceiptType.IsEqual<POAccrualDetail.pOReceiptType>
				.And<POReceiptLineAccrual.pOReceiptNbr.IsEqual<POAccrualDetail.pOReceiptNbr>>
                .And<POReceiptLineAccrual.pOReceiptLineNbr.IsEqual<POAccrualDetail.lineNbr>>>
            .LeftJoin<APTranAccrual>
                .On<APTranAccrual.aPDocType.IsEqual<POAccrualDetail.aPDocType>
                .And<APTranAccrual.aPRefNbr.IsEqual<POAccrualDetail.aPRefNbr>>
                .And<APTranAccrual.aPLineNbr.IsEqual<POAccrualDetail.lineNbr>>>
            .LeftJoin<INTran>
                .On<INTran.docType.IsNotEqual<INDocType.adjustment>
				.And<INTran.pOReceiptType.IsEqual<POAccrualDetail.pOReceiptType>>
				.And<INTran.pOReceiptNbr.IsEqual<POAccrualDetail.pOReceiptNbr>>
                .And<INTran.pOReceiptLineNbr.IsEqual<POAccrualDetail.lineNbr>>>
        .Where<
            Brackets<POAccrualInquiryFilter.subCD.FromCurrent.Value.IsNull.Or<Sub.subCD.IsLike<POAccrualInquiryFilter.subCDWildcard.FromCurrent.Value>>>
            .And<
                Brackets<POAccrualDetail.aPRefNbr.IsNotNull
                    .And<
                        POAccrualDetail.taxAdjPosted.IsNotEqual<True>
                        .Or<POAccrualDetail.pPVAdjPosted.IsNotEqual<True>>
                        .Or<POAccrualDetail.isReversed.IsNotEqual<True>
                            .And<POAccrualDetail.isReversing.IsNotEqual<True>>
                            .And<POAccrualDetail.accruedCostTotal.IsNotEqual<Use<IsNull<APTranAccrual.accruedCost.Add<APTranAccrual.pPVAmt>.Add<APTranAccrual.taxAccruedCost>, decimal0>>.AsDecimal>>>>
                .Or<POAccrualDetail.pOReceiptNbr.IsNotNull
                    .And<POAccrualDetail.accruedCostTotal.IsNotEqual<Use<IsNull<POReceiptLineAccrual.accruedCost.Add<POReceiptLineAccrual.pPVAmt>.Add<POReceiptLineAccrual.taxAccruedCost>, decimal0>>.AsDecimal>>>>>>
        ), Persistent = false)]
    public class POAccrualInquiryResult : IBqlTable
    {
        #region DocumentNoteID
        [PXNote(IsKey = true, BqlField = typeof(POAccrualDetail.documentNoteID))]
        public virtual Guid? DocumentNoteID { get; set; }
        public abstract class documentNoteID : BqlGuid.Field<documentNoteID> { }
        #endregion
        #region LineNbr
        [PXDBInt(IsKey = true, BqlField = typeof(POAccrualDetail.lineNbr))]
        [PXUIField(DisplayName = "Line Nbr.", Visible = false, Visibility = PXUIVisibility.Visible)]
        public virtual int? LineNbr { get; set; }
        public abstract class lineNbr : BqlInt.Field<lineNbr> { }
        #endregion

        #region OrderType
        [PXDBString(2, IsFixed = true, BqlField = typeof(POAccrualStatus.orderType))]
        [PXUIField(DisplayName = "PO Type")]
        [POOrderType.List]
        public virtual string OrderType { get; set; }
        public abstract class orderType : BqlString.Field<orderType> { }
        #endregion
        #region OrderNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(POAccrualStatus.orderNbr))]
        [PXSelector(typeof(Search<POOrder.orderNbr, Where<POOrder.orderType, Equal<Current<orderType>>>>))]
        [PXUIField(DisplayName = "PO Ref. Nbr.")]
        public virtual string OrderNbr { get; set; }
        public abstract class orderNbr : BqlString.Field<orderNbr> { }
        #endregion

        #region POReceiptType
        [PXDBString(2, IsFixed = true, BqlField = typeof(POAccrualDetail.pOReceiptType))]
        public virtual string POReceiptType { get; set; }
        public abstract class pOReceiptType : BqlString.Field<pOReceiptType> { }
        #endregion
        #region POReceiptNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(POAccrualDetail.pOReceiptNbr))]
        public virtual string POReceiptNbr { get; set; }
        public abstract class pOReceiptNbr : BqlString.Field<pOReceiptNbr> { }
        #endregion
        #region APDocType        
        [PXDBString(3, IsFixed = true, BqlField = typeof(POAccrualDetail.aPDocType))]
        [APDocType.List]
        public virtual string APDocType { get; set; }
        public abstract class aPDocType : BqlString.Field<aPDocType> { }
        #endregion
        #region APRefNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(POAccrualDetail.aPRefNbr))]
        public virtual string APRefNbr { get; set; }
        public abstract class aPRefNbr : BqlString.Field<aPRefNbr> { }
        #endregion

        #region IsReversed
        [PXDBBool(BqlField = typeof(POAccrualDetail.isReversed))]
        public virtual bool? IsReversed { get; set; }
        public abstract class isReversed : BqlBool.Field<isReversed> { }
        #endregion
        #region IsReversing
        [PXDBBool(BqlField = typeof(POAccrualDetail.isReversing))]
        public virtual bool? IsReversing { get; set; }
        public abstract class isReversing : BqlBool.Field<isReversing> { }
        #endregion

        #region DocumentType
        [PXString(2, IsFixed = true)]
        [documentType.List]
        [PXFormula(typeof(Switch<
            Case<Where<aPDocType.IsEqual<APDocType.invoice>>,
                documentType.apBill,
            Case<Where<aPDocType.IsEqual<APDocType.debitAdj>>,
                documentType.debitAdj,
            Case<Where<pOReceiptType.IsEqual<POReceiptType.poreceipt>>,
                documentType.poReceipt,
            Case<Where<pOReceiptType.IsEqual<POReceiptType.poreturn>>,
                documentType.poReturn>>>>,
            Null>))]
        [PXUIField(DisplayName = "Document Type")]
        public virtual string DocumentType { get; set; }
        public abstract class documentType : BqlString.Field<documentType>
        {
            public const string Bill = "BL";
            public const string DebitAdj = "DA";
            public const string Receipt = "PR";
            public const string Return = "RT";

            public class apBill : BqlString.Constant<apBill> { public apBill() : base(Bill) { } }
            public class debitAdj : BqlString.Constant<debitAdj> { public debitAdj() : base(DebitAdj) { } }
            public class poReceipt : BqlString.Constant<poReceipt> { public poReceipt() : base(Receipt) { } }
            public class poReturn : BqlString.Constant<poReturn> { public poReturn() : base(Return) { } }

            [PXLocalizable]
            public static class Messages
            {
                public const string Bill = "AP Bill";
                public const string DebitAdj = AP.Messages.DebitAdj;
                public const string Receipt = "PO Receipt";
                public const string Return = "PO Return";
            }

            public class ListAttribute : PXStringListAttribute
            {
                public ListAttribute() : base(
                    (Bill, Messages.Bill),
                    (DebitAdj, Messages.DebitAdj),
                    (Receipt, Messages.Receipt),
                    (Return, Messages.Return))
                { }
            }
        }
        #endregion
        #region DocumentNbr
        [PXString]
        [PXUIField(DisplayName = "Document Number")]
        [PXFormula(typeof(
            aPRefNbr
                .When<aPRefNbr.IsNotNull>
            .Else<pOReceiptNbr>))]
        public virtual string DocumentNbr { get; set; }
        public abstract class documentNbr : BqlString.Field<documentNbr> { }
        #endregion

        #region FinPeriodID
        [FinPeriodID(BqlField = typeof(POAccrualDetail.finPeriodID))]
        [PXUIField(DisplayName = "Post Period")]
        public virtual string FinPeriodID { get; set; }
        public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
        #endregion

        #region DocDate
        [PXDBDate(BqlField = typeof(POAccrualDetail.docDate))]
        [PXUIField(DisplayName = "Document Date")]
        public virtual DateTime? DocDate { get; set; }
        public abstract class docDate : BqlDateTime.Field<docDate> { }
        #endregion

        #region VendorID
        [Vendor(DescriptionField = typeof(Vendor.acctCD), BqlField = typeof(POAccrualDetail.vendorID))]
        public virtual int? VendorID { get; set; }
        public abstract class vendorID : BqlInt.Field<vendorID> { }
        #endregion
        #region VendorName
        [PXString]
        [PXFormula(typeof(Selector<vendorID, Vendor.acctName>))]
        [PXUIField(DisplayName = "Vendor Name")]
        public virtual string VendorName { get; set; }
        public abstract class vendorName : BqlString.Field<vendorName> { }
        #endregion

        #region INDocType
        [PXDBString(1, IsFixed = true, BqlField = typeof(INTran.docType))]
        [INDocType.List]
        [PXUIField(DisplayName = "IN Document Type", Visible = false, Visibility = PXUIVisibility.Visible)]
        public virtual string INDocType { get; set; }
        public abstract class iNDocType : BqlString.Field<iNDocType> { }
        #endregion
        #region INRefNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(INTran.refNbr))]
        [PXUIField(DisplayName = "IN Document Ref. Nbr.")]
        [PXSelector(typeof(Search<INRegister.refNbr, Where<INRegister.docType, Equal<Current<iNDocType>>>>))]
        public virtual string INRefNbr { get; set; }
        public abstract class iNRefNbr : BqlString.Field<iNRefNbr> { }
        #endregion

        #region PPVAdjRefNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(POAccrualDetail.pPVAdjRefNbr))]
        [PXUIField(DisplayName = "PPV Adj. Ref. Nbr.", Visible = false, Visibility = PXUIVisibility.Visible)]
        [PXSelector(typeof(Search<INRegister.refNbr, Where<INRegister.docType, Equal<INDocType.adjustment>>>))]
        public virtual string PPVAdjRefNbr { get; set; }
        public abstract class pPVAdjRefNbr : BqlString.Field<iNRefNbr> { }
        #endregion
        #region PPVAdjPosted
        [PXDBBool(BqlField = typeof(POAccrualDetail.pPVAdjPosted))]
        public virtual bool? PPVAdjPosted { get; set; }
        public abstract class pPVAdjPosted : BqlBool.Field<pPVAdjPosted> { }
        #endregion

        #region TaxAdjRefNbr
        [PXDBString(15, IsUnicode = true, BqlField = typeof(POAccrualDetail.taxAdjRefNbr))]
        [PXUIField(DisplayName = "Tax Adj. Ref. Nbr.", Visible = false, Visibility = PXUIVisibility.Visible)]
        [PXSelector(typeof(Search<INRegister.refNbr, Where<INRegister.docType, Equal<INDocType.adjustment>>>))]
        public virtual string TaxAdjRefNbr { get; set; }
        public abstract class taxAdjRefNbr : BqlString.Field<taxAdjRefNbr> { }
        #endregion
        #region TaxAdjPosted
        [PXDBBool(BqlField = typeof(POAccrualDetail.taxAdjPosted))]
        public virtual bool? TaxAdjPosted { get; set; }
        public abstract class taxAdjPosted : BqlBool.Field<taxAdjPosted> { }
        #endregion

        #region BranchID
        [Branch(BqlField = typeof(POAccrualDetail.branchID), Required = false)]
        public virtual int? BranchID { get; set; }
        public abstract class branchID : BqlInt.Field<branchID> { }
        #endregion
        #region SiteID        
        [Site(BqlField = typeof(POAccrualStatus.siteID))]
        public virtual int? SiteID { get; set; }
        public abstract class siteID : BqlInt.Field<siteID> { }
        #endregion

        #region InventoryID
        [AnyInventory(BqlField = typeof(POAccrualStatus.inventoryID))]
        public virtual int? InventoryID { get; set; }
        public abstract class inventoryID : BqlInt.Field<inventoryID> { }
        #endregion

        #region TranDesc
        [PXDBString(256, IsUnicode = true, BqlField = typeof(POAccrualDetail.tranDesc))]
        [PXUIField(DisplayName = "Description")]
        public virtual string TranDesc { get; set; }
        public abstract class tranDesc : BqlString.Field<tranDesc> { }
        #endregion

        #region AcctID
        [Account(BqlField = typeof(POAccrualStatus.acctID))]
        public virtual int? AcctID { get; set; }
        public abstract class acctID : BqlInt.Field<acctID> { }
        #endregion
        #region SubID
        [SubAccount(BqlField = typeof(POAccrualStatus.subID))]
        public virtual int? SubID { get; set; }
        public abstract class subID : BqlInt.Field<subID> { }
        #endregion

        #region AccruedCost
        [PXDBDecimal(4, BqlField = typeof(POAccrualDetail.accruedCost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? AccruedCost { get; set; }
        public abstract class accruedCost : BqlDecimal.Field<accruedCost> { }
        #endregion
        #region PPVAmt
        [PXDBDecimal(4, BqlField = typeof(POAccrualDetail.pPVAmt))]
        public virtual decimal? PPVAmt { get; set; }
        public abstract class pPVAmt : BqlDecimal.Field<pPVAmt> { }
        #endregion
        #region AccruedCostTotal
        [PXDecimal(4)]
        [PXDBCalced(typeof(POAccrualDetail.accruedCost.Add<POAccrualDetail.pPVAmt>.Add<POAccrualDetail.taxAccruedCost>), typeof(decimal))]
        public virtual decimal? AccruedCostTotal { get; set; }
        public abstract class accruedCostTotal : BqlDecimal.Field<accruedCostTotal> { }
        #endregion
        #region TaxAdjAmt
        [PXDBDecimal(4, BqlField = typeof(POAccrualDetail.taxAdjAmt))]
        public virtual decimal? TaxAdjAmt { get; set; }
        public abstract class taxAdjAmt : BqlDecimal.Field<taxAdjAmt> { }
        #endregion


        #region AccruedByReceiptsCost
        [PXDecimal(4)]
        [PXDBCalced(typeof(IsNull<POReceiptLineAccrual.accruedCost, decimal0>), typeof(decimal))]
        public virtual decimal? AccruedByReceiptsCost { get; set; }
        public abstract class accruedByReceiptsCost : BqlDecimal.Field<accruedByReceiptsCost> { }
        #endregion
        #region AccruedByReceiptsPPVAmt
        [PXDecimal(4)]
        [PXDBCalced(typeof(IsNull<POReceiptLineAccrual.pPVAmt, decimal0>), typeof(decimal))]
        public virtual decimal? AccruedByReceiptsPPVAmt { get; set; }
        public abstract class accruedByReceiptsPPVAmt : BqlDecimal.Field<accruedByReceiptsPPVAmt> { }
        #endregion
        #region AccruedByReceiptsTotal
        [PXDecimal(4)]
        [PXDBCalced(typeof(IsNull<POReceiptLineAccrual.accruedCost.Add<POReceiptLineAccrual.pPVAmt>.Add<POReceiptLineAccrual.taxAccruedCost>, decimal0>), typeof(decimal))]
        public virtual decimal? AccruedByReceiptsTotal { get; set; }
        public abstract class accruedByReceiptsTotal : BqlDecimal.Field<accruedByReceiptsTotal> { }
        #endregion

        #region AccruedByBillsTotal
        [PXDecimal(4)]
        [PXDBCalced(typeof(APTranAccrual.accruedCost.Add<APTranAccrual.pPVAmt>.Add<APTranAccrual.taxAccruedCost>), typeof(decimal))]
        public virtual decimal? AccruedByBillsTotal { get; set; }
        public abstract class accruedByBillsTotal : BqlDecimal.Field<accruedByBillsTotal> { }
        #endregion

        #region UnbilledAmt
        [PXBaseCury]
        [PXFormula(typeof(accruedCost.Subtract<accruedByReceiptsCost>.Subtract<accruedByReceiptsPPVAmt>
            .When<pOReceiptNbr.IsNotNull>
            .Else<decimal0>))]
        [PXUIField(DisplayName = "Unbilled Amount")]
        public virtual decimal? UnbilledAmt { get; set; }
        public abstract class unbilledAmt : BqlDecimal.Field<unbilledAmt> { }
        #endregion
        
        #region NotAdjustedAmt
        [PXBaseCury]
        [PXFormula(typeof(
            Switch<Case<Where<aPRefNbr.IsNotNull>,
                decimal0.When<pPVAdjPosted.IsEqual<True>>.Else<pPVAmt.Multiply<decimal_1>>
                .Add<decimal0
                    .When<taxAdjPosted.IsEqual<True>>
                    .Else<taxAdjAmt>>>,
            decimal0>))]
        [PXUIField(DisplayName = "IN Adjustment Amount Not Released")]
        public virtual decimal? NotAdjustedAmt { get; set; }
        public abstract class notAdjustedAmt : BqlDecimal.Field<notAdjustedAmt> { }
        #endregion

        #region NotReceivedAmt
        [PXBaseCury]
        [PXFormula(typeof(accruedCostTotal.Subtract<Use<IsNull<accruedByBillsTotal, taxAdjAmt>>.AsDecimal>
            .When<aPRefNbr.IsNotNull
                .And<orderType.IsNotEqual<POOrderType.dropShip>>
                .And<isReversed.IsNotEqual<True>>
                .And<isReversing.IsNotEqual<True>>>
            .Else<decimal0>))]
        [PXUIField(DisplayName = "Not Received Amount")]
        public virtual decimal? NotReceivedAmt { get; set; }
        public abstract class notReceivedAmt : BqlDecimal.Field<notReceivedAmt> { }
        #endregion
        #region NotInvoicedAmt
        [PXBaseCury]
        [PXFormula(typeof(accruedCostTotal.Subtract<Use<IsNull<accruedByBillsTotal, taxAdjAmt>>.AsDecimal>
            .When<aPRefNbr.IsNotNull
                .And<orderType.IsEqual<POOrderType.dropShip>>
                .And<isReversed.IsNotEqual<True>>
                .And<isReversing.IsNotEqual<True>>>
            .Else<decimal0>))]
        [PXUIField(DisplayName = "Drop-Ship Amount Not Invoiced")]
        public virtual decimal? NotInvoicedAmt { get; set; }
        public abstract class notInvoicedAmt : BqlDecimal.Field<notInvoicedAmt> { }
        #endregion
        #region AccrualAmt
        [PXBaseCury]
        [PXFormula(typeof(notAdjustedAmt
            .Add<notReceivedAmt>
            .Add<notInvoicedAmt>.
            Subtract<unbilledAmt>))]
        [PXUIField(DisplayName = "PO Accrued Amount")]
        public virtual decimal? AccrualAmt { get; set; }
        public abstract class accrualAmt : BqlDecimal.Field<accrualAmt> { }
        #endregion
    }
}
