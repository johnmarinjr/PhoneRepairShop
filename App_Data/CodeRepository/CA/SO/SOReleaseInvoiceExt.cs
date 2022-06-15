using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.SO
{
	public class SOReleaseInvoiceExt : PXGraphExtension<SOReleaseInvoice>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        #region Cache Attached

        [PXRemoveBaseAttribute(typeof(PX.Objects.SO.SOInvoiceTermsAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [CanadaSOInvoiceTerms(typeof(ARInvoice.docDate),
            typeof(ARInvoice.dueDate),
            typeof(ARInvoice.discDate),
            typeof(ARInvoice.docType),
            typeof(AR.ARInvoiceExt.curyDocTotalWithoutTax),
            typeof(AR.ARInvoiceExt.curyDocBalWithoutTax),
            typeof(ARInvoice.curyOrigDiscAmt))]
        protected virtual void ARInvoice_TermsID_CacheAttached(PXCache sender)
        {
        }

        #endregion
    }
}
