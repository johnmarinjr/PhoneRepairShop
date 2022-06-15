using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using static PX.Objects.FA.TransferProcess;

namespace PX.Objects.FA
{
	public sealed class TransferFilterMultipleBaseCurrencies : PXCacheExtension<TransferFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchTo
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(
			typeof(Where<Branch.baseCuryID, EqualBaseCuryID<Current2<TransferFilter.branchFrom>>,
				Or<Current2<TransferFilter.branchFrom>, IsNull>>),
			Messages.DestinationBranchBaseCurrencyDiffersFromSourceBranch,
			typeof(Branch.baseCuryID),
			typeof(Branch.branchCD),
			typeof(TransferFilter.branchFrom))]
		public int? BranchTo
		{
			get;
			set;
		}
		#endregion

		#region BranchFrom
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(
			typeof(Where<Branch.baseCuryID, EqualBaseCuryID<Current2<TransferFilter.branchTo>>,
				Or<Current2<TransferFilter.branchTo>, IsNull>>),
			Messages.SourceBranchBaseCurrencyDiffersFromDestinationBranch,
			typeof(Branch.baseCuryID),
			typeof(Branch.branchCD),
			typeof(TransferFilter.branchTo))]
		public int? BranchFrom
		{
			get;
			set;
		}
		#endregion
	}
}
