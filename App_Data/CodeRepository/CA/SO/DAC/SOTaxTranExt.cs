using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.SO
{
	public sealed class SOTaxTranExt : PXCacheExtension<SOTaxTran>
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
            Add<SOTaxTran.curyTaxAmt, SOTaxTran.curyExpenseAmt>))]
        public decimal? CuryTotalTaxAmount
        {
            get;
            set;
        }

        #endregion
    }
}
