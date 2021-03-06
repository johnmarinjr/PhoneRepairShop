using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;
using PX.Objects.Common;

namespace PX.Objects.AR
{
	public sealed class ARPaymentVisibilityRestriction : PXCacheExtension<ARPayment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}
		#region BranchID
		/// <summary>
		/// The identifier of the <see cref="Branch">branch</see> to which the document belongs.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Branch.BranchID"/> field.
		/// </value>
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
			Case<Where<PendingValue<ARPayment.branchID>, IsPending>, Null,
			Case<Where<ARPayment.customerLocationID, IsNotNull,
					And<Selector<ARPayment.customerLocationID, Location.cBranchID>, IsNotNull>>,
				Selector<ARPayment.customerLocationID, Location.cBranchID>,
			Case<Where<ARPayment.customerID, IsNotNull,
					And<Not<Selector<ARPayment.customerID, Customer.cOrgBAccountID>, RestrictByBranch<Current2<ARPayment.branchID>>>>>,
				Null,
			Case<Where<Current2<ARPayment.branchID>, IsNotNull>,
				Current2<ARPayment.branchID>>>>>,
			Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion


		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(branchID: typeof(ARPayment.branchID))]
		public int? CustomerID { get; set; }
		#endregion
	}
}
