using PX.Data;
using PX.Objects.CS;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AR
{
	public sealed class ARCashSaleVisibilityRestriction : PXCacheExtension<Standalone.ARCashSale>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(Standalone.ARCashSale.branchID))]
		public int? CustomerID { get; set; }
		#endregion
	}
}
