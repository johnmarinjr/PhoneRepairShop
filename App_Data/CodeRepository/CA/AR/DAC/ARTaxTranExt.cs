using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.AR
{
	public sealed class ARTaxTranExt : PXCacheExtension<ARTaxTran>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        #region CuryTotalTaxAmount

        public abstract class curyTotalTaxAmount : BqlDecimal.Field<curyTotalTaxAmount>
        {
        }

        [PXDecimal(2)]
        [PXFormula(typeof(
            Add<ARTaxTran.curyTaxAmt, ARTaxTran.curyExpenseAmt>))]
        public decimal? CuryTotalTaxAmount
        {
            get;
            set;
        }

        #endregion

    }
}
