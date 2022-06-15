using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

using System;

namespace PX.Objects.FA
{
	public class AssetGLTransactionsMultipleBaseCurrencies : FAAccrualTranMultipleBaseCurrenciesBase<AssetGLTransactions.GLTransactionsViewExtension, AssetGLTransactions>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(
			typeof(Where<FixedAsset.baseCuryID, EqualBaseCuryID<Current<FATran.branchID>>>),
			Messages.AssetBaseCurrencyDiffersFromTransactionBranch,
			typeof(FixedAsset.baseCuryID),
			typeof(FixedAsset.assetCD),
			typeof(FATran.branchID))]
		protected virtual void _(Events.CacheAttached<FATran.targetAssetID> e){}

		public delegate BqlCommand GetSelectCommandDelegate(GLTranFilter filter);

		[PXOverride]
		public virtual BqlCommand GetSelectCommand(GLTranFilter filter, GetSelectCommandDelegate baseDelegate)
		{
			BqlCommand query = baseDelegate(filter);
			return ModifySelectCommand(filter, query);
		}
	}
}
