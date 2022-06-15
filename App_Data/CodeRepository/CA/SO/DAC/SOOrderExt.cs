using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.Localizations.CA.SO
{
	public sealed class SOOrderExt : PXCacheExtension<SOOrder>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion

        #region CuryOrderTotalWithoutTax

        public abstract class curyOrderTotalWithoutTax : BqlDecimal.Field<curyOrderTotalWithoutTax> { }

        [PXDecimal(2)]
        [PXFormula(typeof(
            Sub<SOOrder.curyOrderTotal, SOOrder.curyTaxTotal>))]
        public decimal? CuryOrderTotalWithoutTax
        {
            get;
            set;
        }

        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(SOOrder.invoiceDate),
            typeof(SOOrder.dueDate),
            typeof(SOOrder.discDate),
            typeof(SOOrderExt.curyOrderTotalWithoutTax),
            typeof(SOOrder.curyTermsDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
