using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;


namespace PX.Objects.FA
{
	public sealed class FADetailsMultipleBaseCurrencies : PXCacheExtension<FADetails>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Selector<FixedAsset.branchID.FromCurrent, Branch.baseCuryID>))]
		public string BaseCuryID { get; set; }
		#endregion
	}
}
