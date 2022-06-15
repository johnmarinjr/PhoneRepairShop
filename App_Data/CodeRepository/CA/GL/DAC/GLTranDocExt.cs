using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.Localizations.CA.GL
{
	public sealed class GLTranDocExt : PXCacheExtension<GLTranDoc>
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
            Sub<GLTranDoc.curyTranTotal, GLTranDoc.curyTaxAmt>))]
        public decimal? CuryDocTotalWithoutTax
        {
            get;
            set;
        }

        #endregion

        #region TermsID

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [Terms(typeof(GLTranDoc.tranDate), 
            typeof(GLTranDoc.dueDate), 
            typeof(GLTranDoc.discDate), 
            typeof(GLTranDocExt.curyDocTotalWithoutTax), 
            typeof(GLTranDoc.curyDiscAmt))]
        public string TermsID
        {
            get;
            set;
        }

        #endregion
    }
}
