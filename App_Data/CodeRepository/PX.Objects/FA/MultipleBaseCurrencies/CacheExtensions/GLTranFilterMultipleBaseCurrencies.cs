using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public sealed class GLTranFilterMultipleBaseCurrencies : PXCacheExtension<GLTranFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[BaseCurrency(typeof(GLTranFilter.branchID))]
		public string BranchBaseCuryID
		{
			get;
			set;
		}
	}
}
