using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.Common.Bql;
using PX.Objects.CS;

namespace PX.Objects.IN.GraphExtensions.INReplenishmentCreateExt
{
	public class MultipleBaseCurrencyExt : PXGraphExtension<INReplenishmentCreate>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<INSite.baseCuryID, EqualSiteBaseCuryID<Current2<INReplenishmentFilter.replenishmentSiteID>>>),
			Messages.ReplenishmentSiteDiffers, typeof(INSite.branchID), typeof(INSite.siteCD))]
		protected virtual void _(Events.CacheAttached<INReplenishmentItem.replenishmentSourceSiteID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Vendor.baseCuryID, EqualSiteBaseCuryID<Current2<INReplenishmentFilter.replenishmentSiteID>>,
			Or<Vendor.baseCuryID, IsNull>>),
			Messages.ReplenishmentVendorDiffers, typeof(Vendor.acctCD))]
		protected virtual void _(Events.CacheAttached<INReplenishmentItem.preferredVendorID> eventArgs)
		{
		}

	}
}
