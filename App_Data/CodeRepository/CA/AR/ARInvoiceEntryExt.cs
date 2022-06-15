using PX.Data;
using PX.Objects.CS;
using PX.Objects.AR;

namespace PX.Objects.Localizations.CA.AR
{
	public class ARInvoiceEntryExt : PXGraphExtension<ARInvoiceEntry>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        #region Cache Attached

        [PXRemoveBaseAttribute(typeof(PX.Objects.CS.TermsAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [Terms(typeof(ARInvoice.docDate),
            typeof(ARInvoice.dueDate),
            typeof(ARInvoice.discDate),
            typeof(ARInvoiceExt.curyDocTotalWithoutTax),
            typeof(ARInvoice.curyOrigDiscAmt))]
        protected virtual void ARInvoice_TermsID_CacheAttached(PXCache sender)
        {
        }

        #endregion
    }
}