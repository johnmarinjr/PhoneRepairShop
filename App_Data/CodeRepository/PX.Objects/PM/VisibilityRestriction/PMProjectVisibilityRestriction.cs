using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.PM
{
	public sealed class PMProjectVisibilityRestriction : PXCacheExtension<PMProject>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region ContractCD
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[RestrictCustomerByUserBranches(typeof(Customer.cOrgBAccountID), typeof(Or<Current<PMProject.customerID>, IsNull>))]
		public string ContractCD { get; set; }
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches]
		public int? CustomerID { get; set; }
		#endregion
	}
}
