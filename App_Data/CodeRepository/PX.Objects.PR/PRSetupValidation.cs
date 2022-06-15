using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.PR
{
	public abstract class PRSetupValidation<TGraph> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		public PXSetup<PRSetup> Setup;

		public override void Initialize()
		{
			base.Initialize();

			if (Setup.Current != null && PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>())
			{
				if (string.IsNullOrEmpty(Setup.Current.PTOExpenseAcctDefault)
					|| string.IsNullOrEmpty(Setup.Current.PTOLiabilityAcctDefault)
					|| string.IsNullOrEmpty(Setup.Current.PTOAssetAcctDefault))
				{
					throw new PXSetupNotEnteredException<PRSetup>();
				}

				if (PXAccess.FeatureInstalled<FeaturesSet.subAccount>())
				{
					if (string.IsNullOrEmpty(Setup.Current.PTOExpenseSubMask)
					|| string.IsNullOrEmpty(Setup.Current.PTOLiabilitySubMask)
					|| string.IsNullOrEmpty(Setup.Current.PTOAssetSubMask))
					{
						throw new PXSetupNotEnteredException<PRSetup>();
					}
				}
			}
		}
	}
}
