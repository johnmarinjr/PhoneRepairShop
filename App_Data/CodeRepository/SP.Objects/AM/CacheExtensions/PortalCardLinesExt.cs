using System;
using PX.Data;
using SP.Objects.IN;

namespace SP.Objects.AM
{
    /// <summary>
    /// Manufacturing extension to B2B Portal Cart items (PortalCardLines)
    /// </summary>
    [Serializable]
    public sealed class PortalCardLinesExt : PXCacheExtension<PortalCardLines>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.manufacturingProductConfigurator>();
        }

        #region AMConfigurationID
        public abstract class aMConfigurationID : PX.Data.IBqlField
        {
        }
        [PXDBString(IsUnicode = true)]
        [PXUIField(DisplayName = "Configuration ID", Enabled = false, Visible = false)]
        public string AMConfigurationID
        {
            get;
            set;
        }

        #endregion

        #region AMIsConfigurable
        public abstract class aMIsConfigurable : PX.Data.IBqlField
        {
        }
        [PXBool]
        [PXUIField(DisplayName = "Configurable", Enabled = false)]
        [PXDependsOnFields(typeof(PortalCardLinesExt.aMConfigurationID))]
        public bool? AMIsConfigurable
        {
            get
            {
                return !string.IsNullOrEmpty(AMConfigurationID);
            }
        }
        #endregion
    }
}