using PX.Data;
using PX.Objects.AP;
using PX.Objects.Common.Formula;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.GL;

namespace PX.Objects.PO
{
	public sealed class POLandedCostDocVisibilityRestriction : PXCacheExtension<POLandedCostDoc>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(IsDetail = false)]
		[PXFormula(typeof(Switch<
			Case<Where<IsCopyPasteContext, Equal<True>, And<Current2<POLandedCostDoc.branchID>, IsNotNull>>, Current2<POLandedCostDoc.branchID>,
			Case<Where<POLandedCostDoc.vendorLocationID, IsNotNull,
					And<Selector<POLandedCostDoc.vendorLocationID, Location.vBranchID>, IsNotNull>>,
				Selector<POLandedCostDoc.vendorLocationID, Location.vBranchID>,
				Case<Where<POLandedCostDoc.vendorID, IsNotNull,
						And<Not<Selector<POLandedCostDoc.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<POLandedCostDoc.branchID>>>>>,
					Null,
					Case<Where<Current2<POLandedCostDoc.branchID>, IsNotNull>,
						Current2<POLandedCostDoc.branchID>>>>>,
			Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(POLandedCostDoc.branchID), ResetVendor = false)]
		public int? VendorID { get; set; }
		#endregion
	}
}