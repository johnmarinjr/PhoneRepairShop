using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.GL;
using APQuickCheck = PX.Objects.AP.Standalone.APQuickCheck;

namespace PX.Objects.AP
{
	public sealed class APQuickCheckEntryMultipleBaseCurrencies : PXGraphExtension<APQuickCheckEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected void _(Events.FieldVerifying<APQuickCheck.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<APQuickCheck.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			BAccountR vendor = PXSelectorAttribute.Select<APQuickCheck.vendorID>(e.Cache, e.Row) as BAccountR;

            if (branch != null && vendor != null && vendor.BaseCuryID != null
                && branch.BaseCuryID != vendor.BaseCuryID)
            {
                e.NewValue = branch.BranchCD;
                throw new PXSetPropertyException(Messages.BranchVendorDifferentBaseCury, PXOrgAccess.GetCD(vendor.VOrgBAccountID), vendor.AcctCD);
            }
        }
	}
}
