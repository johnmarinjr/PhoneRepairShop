using PX.Data;
using PX.Data.BQL;
using PX.Objects.AP.Standalone;
using PX.Objects.CS;
using PX.Objects.Localizations.CA.CS;

namespace PX.Objects.Localizations.CA.AP.Standalone
{
    public sealed class APQuickCheckExt : PXCacheExtension<APQuickCheck>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        #region CuryDocBalWithoutTax

        public abstract class curyDocBalWithoutTax : BqlDecimal.Field<curyDocBalWithoutTax> { }

        [PXDecimal(2)]
        public decimal? CuryDocBalWithoutTax
        {
            get;
            set;
        }

        #endregion

        #region CuryDocBal

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [CanadaCashDiscount(typeof(APQuickCheck.curyDocBal), typeof(APQuickCheck.curyTaxTotal), typeof(APQuickCheckExt.curyDocBalWithoutTax))]
        public decimal? CuryDocBal { get; set; }

        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(
            typeof(APQuickCheck.docDate),
            null,
            null,
            typeof(APQuickCheckExt.curyDocBalWithoutTax),
            typeof(APQuickCheck.curyOrigDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
