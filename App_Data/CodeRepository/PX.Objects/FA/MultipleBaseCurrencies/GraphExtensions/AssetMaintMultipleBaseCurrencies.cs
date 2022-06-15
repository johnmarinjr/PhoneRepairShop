using System;
using System.Collections;
using System.Linq;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public abstract class FAAccrualTranMultipleBaseCurrenciesBase<TGraphExtension, TGraph> : PXGraphExtension<TGraphExtension, TGraph>
		where TGraph : PXGraph
		where TGraphExtension: PXGraphExtension<TGraph>
	{
		protected virtual BqlCommand ModifySelectCommand(GLTranFilter filter, BqlCommand query)
		{
			if (filter.BranchID != null)
			{
				query = BqlCommand.AppendJoin<LeftJoin<Branch, On<FAAccrualTran.gLTranBranchID, Equal<Branch.branchID>>>>(query);
				query = query.WhereAnd<Where<Branch.baseCuryID.IsEqual<GLTranFilter.branchBaseCuryID.FromCurrent>>>();
			}
			return query;
		}
	}

	public class AssetMaintMultipleBaseCurrencies : FAAccrualTranMultipleBaseCurrenciesBase<AssetMaint.AdditionsViewExtension, AssetMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<
			Branch.baseCuryID, Equal<Current<FixedAsset.baseCuryID>>,
			Or<
				IsNull<Current<FixedAsset.isAcquired>, False>, Equal<False>,
				And<Current<FixedAsset.splittedFrom>, IsNull,
				And<
					NotExists<Select<FATran,
						Where<FATran.assetID, Equal<Current<FixedAsset.assetID>>,
							And<FATran.tranType, Equal<FATran.tranType.reconcilliationPlus>>>>>>>
				>>),
			Messages.BaseCurrencyDifferentFromAssetBranch,
			typeof(Branch.branchCD))]
		protected virtual void _(Events.CacheAttached<FALocationHistory.locationID> e)
		{
		}

		protected virtual void _(Events.FieldVerifying<FALocationHistory.locationID> e)
		{
			if (e.NewValue == null)
				return;

			FALocationHistory location = (FALocationHistory)e.Row;
			Branch branch = PXSelectorAttribute.Select<FALocationHistory.locationID>(e.Cache, location, (int)e.NewValue) as Branch;
			FixedAsset asset = (FixedAsset)PXSelect<FixedAsset,
				Where<FixedAsset.assetID, Equal<Required<FixedAsset.assetID>>>>.Select(e.Cache.Graph, location.AssetID);

			if (asset.BaseCuryID != null && branch != null && branch.BaseCuryID != asset.BaseCuryID)
			{
				PXResultset<FATran> tran = PXSelect<FATran,
					Where<FATran.assetID, Equal<Required<FATran.assetID>>,
						And<FATran.tranType, Equal<FATran.tranType.reconcilliationPlus>>>>
						.SelectSingleBound(e.Cache.Graph, null, asset.AssetID);

				if (tran.Any())
				{
					e.NewValue = branch.BranchCD;
					throw new PXSetPropertyException(Messages.BaseCurrencyDifferentFromAssetBranch, branch.BranchCD);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<FALocationHistory, FALocationHistory.locationID> e)
		{
			if (e.Row == null)
				return;

			FADetails currentDetails = Base.AssetDetails.Current;

			if (currentDetails != null) {
				currentDetails.BaseCuryID = PXAccess.GetBranch(e.Row.LocationID).BaseCuryID;
			}
		}

		public delegate IEnumerable ViewDelegate();

		[PXOverride]
		public IEnumerable additions(ViewDelegate baseDelegate)
		{
			int? oldBranchID = Base.GLTrnFilter.Current.BranchID;
			Base.GLTrnFilter.Current.BranchID = Base.Asset.Current.BranchID;
			Base.GLTrnFilter.Cache.RaiseFieldUpdated<GLTranFilter.branchID>(Base.GLTrnFilter.Current, oldBranchID); // to synchronize GLTranFilter.BranchBaseCuryID from GLTranFilter.BranchID 
			return baseDelegate();
		}

		public delegate BqlCommand GetSelectCommandDelegate(GLTranFilter filter);

		[PXOverride]
		public virtual BqlCommand GetSelectCommand(GLTranFilter filter, GetSelectCommandDelegate baseDelegate)
		{
			BqlCommand query = baseDelegate(filter);
			return ModifySelectCommand(filter, query);
		}
	}
}
