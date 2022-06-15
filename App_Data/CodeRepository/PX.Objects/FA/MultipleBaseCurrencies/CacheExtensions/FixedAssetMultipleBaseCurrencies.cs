using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public sealed class FixedAssetMultipleBaseCurrencies : PXCacheExtension<FixedAsset>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[BaseCurrency(typeof(FixedAsset.branchID))]
		[PXUIField(DisplayName = "Currency")]
		public string BaseCuryID { get; set; }

	}
}
