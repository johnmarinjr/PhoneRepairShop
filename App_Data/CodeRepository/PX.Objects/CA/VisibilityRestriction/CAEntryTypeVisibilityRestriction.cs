using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.AR;
using PX.Objects.AP;

namespace PX.Objects.CA
{
	public sealed class CAEntryTypeVisibilityRestriction : PXCacheExtension<CAEntryType>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region ReferenceID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches(typeof(BAccountR.cOrgBAccountID))]
		[RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
		public int? ReferenceID { get; set; }
		#endregion
	}
}