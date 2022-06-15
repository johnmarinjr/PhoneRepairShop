using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public class AccBalanceByAssetInqMultipleCalendarsSupport : PXGraphExtension<AccBalanceByAssetInq>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleCalendarsSupport>();
		}

		public delegate BqlCommand GetSelectCommandDelegate(AccBalanceByAssetInq.AccBalanceByAssetFilter filter);

		[PXOverride]
		public virtual BqlCommand GetSelectCommand(AccBalanceByAssetInq.AccBalanceByAssetFilter filter, GetSelectCommandDelegate baseDelegate)
		{
			BqlCommand query = baseDelegate(filter);
			if (filter.OrganizationID == null)
			{
				query = query.WhereAnd<Where<Branch.organizationID, Equal<Current2<AccBalanceByAssetInq.AccBalanceByAssetFilter.organizationID>>,
					And<MatchWithBranch<Branch.branchID>>>>();
			}
			return query;
		}
	}
}
