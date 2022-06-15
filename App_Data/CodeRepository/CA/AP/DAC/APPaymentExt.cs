using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.AP
{
	public sealed class APPaymentExt : PXCacheExtension<APPayment>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        [PXRemoveBaseAttribute(typeof(PX.Objects.AP.ToWordsAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [FrenchToWords(typeof(APPayment.curyOrigDocAmt))]
        public string AmountToWords
        {
            get;
            set;
        }
    }
}
