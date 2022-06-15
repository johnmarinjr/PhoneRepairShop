using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.AR
{
	public sealed class ARInvoiceExt : PXCacheExtension<PX.Objects.AR.ARInvoice>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion

        #region CuryDocTotalWithoutTax

        public abstract class curyDocTotalWithoutTax : BqlDecimal.Field<curyDocTotalWithoutTax> { }

        [PXDecimal(2)]
        [PXFormula(typeof(
            Sub<ARInvoice.curyOrigDocAmt, ARInvoice.curyTaxTotal>))]
        public decimal? CuryDocTotalWithoutTax
        {
            get;
            set;
        }

        #endregion

        #region CuryDocBalWithoutTax

        public abstract class curyDocBalWithoutTax : BqlDecimal.Field<curyDocBalWithoutTax> { }

        [PXDecimal(2)]
        [PXFormula(typeof(
            Sub<ARInvoice.curyDocBal, ARInvoice.curyTaxTotal>))]
        public decimal? CuryDocBalWithoutTax
        {
            get;
            set;
        }

        #endregion

        #region CuryDocTotalWithoutTax

        public abstract class printCuryDocTotalWithoutTax : BqlDecimal.Field<printCuryDocTotalWithoutTax> { }

        /// <summary>
        /// This field is for calculating the correct document total without taxes and without the
        /// cash discount.  It is for printing only.
        ///  
        /// CuryDocBal has the right amount but only until the invoice is released or until payments are
        /// applied so it cannot be used reliably.  When it's a cash sale, CuryDocBal's value drops to 
        /// zero soon as the invoice is released.
        /// 
        /// Instead of using CuryDocBal we compute the amount we need by using the following logic:
        /// 
        ///     CuryOrigDocAmt - CuryTaxTotal
        /// 
        ///     if cash sale or cash return and cash discount is not zero
        ///         + CuryOrigDiscAmt
        /// 
        /// </summary>
        [PXDecimal(2)]
        public decimal? PrintCuryDocTotalWithoutTax
        {
            [PXDependsOnFields(
                typeof(ARInvoice.docType), 
                typeof(ARInvoice.curyOrigDocAmt), 
                typeof(ARInvoice.curyTaxTotal), 
                typeof(ARInvoice.curyOrigDiscAmt))]
            get
            {
                decimal? result = Base.CuryOrigDocAmt - Base.CuryTaxTotal;

                if ((Base.DocType == ARDocType.CashSale || Base.DocType == ARDocType.CashReturn) &&
                    Base.CuryOrigDiscAmt.HasValue && Base.CuryOrigDiscAmt.Value != 0m)
                {
                    result += Base.CuryOrigDiscAmt;
                }

                return result;
            }
        }

        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(ARInvoice.docDate),
            typeof(ARInvoice.dueDate),
            typeof(ARInvoice.discDate),
            typeof(ARInvoiceExt.curyDocTotalWithoutTax),
            typeof(ARInvoice.curyOrigDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
