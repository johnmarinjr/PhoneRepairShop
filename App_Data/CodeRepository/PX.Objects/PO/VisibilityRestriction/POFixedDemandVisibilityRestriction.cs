using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using POCreateFilter = PX.Objects.PO.POCreate.POCreateFilter;

namespace PX.Objects.PO.VisibilityRestriction
{
	public sealed class POFixedDemandVisibilityRestriction : PXCacheExtension<POFixedDemand>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(POCreateFilter.branchID), ResetVendor = false)]
		public int? VendorID { get; set; }
		#endregion
	}
}
