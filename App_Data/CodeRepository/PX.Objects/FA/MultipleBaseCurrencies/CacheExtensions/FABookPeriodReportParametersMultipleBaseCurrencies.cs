using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL.Attributes;

namespace PX.Objects.FA
{
	public sealed class FABookPeriodReportParametersMultipleBaseCurrencies : PXCacheExtension<FABookPeriodReportParameters>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(
			typeof(OrganizationTreeAttribute),
			nameof(OrganizationTreeAttribute.PersistingCheck),
			PXPersistingCheck.Null)]
		[PXCustomizeBaseAttribute(
			typeof(OrganizationTreeAttribute),
			nameof(OrganizationTreeAttribute.Required),
			true)]
		public int? OrgBAccountID { get; set; }
	}
}
