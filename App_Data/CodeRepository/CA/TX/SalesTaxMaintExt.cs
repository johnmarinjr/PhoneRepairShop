using PX.Data;
using PX.Objects.CS;
using PX.Objects.TX;
using System;

namespace PX.Objects.Localizations.CA.TX
{
	public class SalesTaxMaintExt : PXGraphExtension<SalesTaxMaint>
	{
        #region IsActive

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }

        #endregion

        [PXOverride]
        public bool IsPrintingSettingsTabVisible(Tax tax,Func<Tax,bool> baseDelegate)
		{
            return true;
		}
    }
}
