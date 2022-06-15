using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.FA
{
	public class DisposalProcessMultipleCalendarsSupport : PXGraphExtension<DisposalProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleCalendarsSupport>();
		}

		public delegate BqlCommand GetSelectCommandDelegate(DisposalProcess.DisposalFilter filter);

		[PXOverride]
		public virtual BqlCommand GetSelectCommand(DisposalProcess.DisposalFilter filter, GetSelectCommandDelegate baseDelegate)
		{
			BqlCommand query = baseDelegate(filter);
			if (filter.OrgBAccountID == null)
			{
				query = query.WhereAnd<Where<FixedAsset.branchID, Inside<Current2<DisposalProcess.DisposalFilter.orgBAccountID>>>>();
			}
			return query;
		}
	}
}
