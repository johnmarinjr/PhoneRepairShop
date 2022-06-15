using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public class AssetSummaryMultipleBaseCurrencies : PXGraphExtension<AssetSummary>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate BqlCommand GetSelectCommandDelegate(AssetFilter filter);

		[PXOverride]
		public virtual BqlCommand GetSelectCommand(AssetFilter filter, GetSelectCommandDelegate baseDelegate)
		{
			BqlCommand query = baseDelegate(filter);
			if (filter.OrganizationID == null)
			{
				query = query.WhereAnd<Where<Branch.organizationID, Equal<Current2<AssetFilter.organizationID>>, And<MatchWithBranch<Branch.branchID>>>>();
			}
			return query;
		}
	}
}
