using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR.Standalone;
using PX.Objects.CS;
using PX.Objects.Localizations.CA.CS;

namespace PX.Objects.Localizations.CA.AR.Standalone
{
	public sealed class ARCashSaleExt : PXCacheExtension<ARCashSale>
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
        [CanadaCashDiscount(typeof(ARCashSale.curyDocBal), typeof(ARCashSale.curyTaxTotal), typeof(ARCashSaleExt.curyDocBalWithoutTax))]
        public decimal? CuryDocBal { get; set; }
        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(ARCashSale.docDate),
            null,
            null,
            typeof(ARCashSaleExt.curyDocBalWithoutTax),
            typeof(ARCashSale.curyOrigDiscAmt))]
        public string TermsID { get; set; }

        #endregion
    }
}
