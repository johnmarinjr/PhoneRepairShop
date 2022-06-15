using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

using System;

namespace PX.Objects.FA
{
	public sealed class FAAccrualTranMultipleBaseCurrencies : PXCacheExtension<FAAccrualTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(
			typeof(Where<Branch.baseCuryID.IsEqual<GLTranFilter.branchBaseCuryID.FromCurrent>>),
			Messages.BaseCurrencyDiffersFromTransactionBranch,
			typeof(Branch.baseCuryID),
			typeof(Branch.branchCD),
			typeof(GLTranFilter.branchBaseCuryID),
			typeof(GLTranFilter.branchID)
			)]
		public int? BranchID
		{
			get;
			set;
		}
		#endregion
	}
}
