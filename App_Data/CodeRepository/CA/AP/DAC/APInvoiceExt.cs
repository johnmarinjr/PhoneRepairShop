using PX.Data;
using PX.Data.BQL;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.AP
{
	public sealed class APInvoiceExt : PXCacheExtension<APInvoice>
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
            Sub<APInvoice.curyOrigDocAmt, APInvoice.curyTaxTotal>))]
        public decimal? CuryDocTotalWithoutTax
        {
            get;
            set;
        }
        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(APInvoice.docDate),
            typeof(APInvoice.dueDate),
            typeof(APInvoice.discDate),
            typeof(APInvoiceExt.curyDocTotalWithoutTax),
            typeof(APInvoice.curyOrigDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
