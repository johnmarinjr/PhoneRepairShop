using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.Localizations.CA.PO
{
	public sealed class POLandedCostDocExt : PXCacheExtension<POLandedCostDoc>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(POLandedCostDoc.billDate), 
            typeof(POLandedCostDoc.dueDate), 
            typeof(POLandedCostDoc.discDate), 
            typeof(POLandedCostDoc.curyLineTotal), 
            typeof(POLandedCostDoc.curyDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
