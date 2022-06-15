using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	public sealed class ARWriteOffFilterVisibilityRestriction : PXCacheExtension<ARWriteOffFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByOrganization(orgBAccountID: typeof(ARWriteOffFilter.orgBAccountID))]
		public int? CustomerID { get; set; }
		#endregion
	}
}
