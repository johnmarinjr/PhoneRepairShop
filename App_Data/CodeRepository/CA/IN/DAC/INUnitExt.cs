using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.Localizations.CA.CS;

namespace PX.Objects.Localizations.CA.IN
{
    // Override the primary graph to set the default page to 10CS2001 
    [PXPrimaryGraph(typeof(UnitOfMeasureMaint))]
    public sealed class INUnitExt : PXCacheExtension<INUnit>
    {
        #region IsActive
        
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }
        
        #endregion
        
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [MultilingualUnitOfMeasure]
        public string FromUnit
        {
            get;
            set;
        }
    }
}
