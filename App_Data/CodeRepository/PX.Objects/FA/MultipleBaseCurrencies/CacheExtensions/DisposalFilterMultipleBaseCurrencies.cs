using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.FA
{
	public sealed class DisposalFilterMultipleBaseCurrencies : PXCacheExtension<DisposalProcess.DisposalFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault]
		public int? OrgBAccountID { get; set; }
	}
}
