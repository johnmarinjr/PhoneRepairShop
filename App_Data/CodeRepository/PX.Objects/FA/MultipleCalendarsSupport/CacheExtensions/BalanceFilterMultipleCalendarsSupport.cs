using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.FA
{
	public sealed class BalanceFilterMultipleCalendarsSupport : PXCacheExtension<BalanceFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleCalendarsSupport>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault]
		public int? OrgBAccountID { get; set; }
	}
}
