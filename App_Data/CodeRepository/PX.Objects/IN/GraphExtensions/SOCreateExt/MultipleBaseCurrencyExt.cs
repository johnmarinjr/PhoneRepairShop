using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Bql;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOCreateFilter = PX.Objects.SO.SOCreate.SOCreateFilter;
using SOFixedDemand = PX.Objects.SO.SOCreate.SOFixedDemand;

namespace PX.Objects.IN.GraphExtensions.SOCreateExt
{
	public class MultipleBaseCurrencyExt : PXGraphExtension<SOCreate>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<SOCreateFilter.sourceSiteID>, IsNull,
			Or<INSite.baseCuryID, EqualSiteBaseCuryID<Current2<SOCreateFilter.sourceSiteID>>>>),
			Messages.ReplenishmentSiteDiffers, typeof(INSite.branchID), typeof(INSite.siteCD))]
		protected virtual void _(Events.CacheAttached<SOCreateFilter.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<SOCreateFilter.siteID>, IsNull,
			Or<INSite.baseCuryID, EqualSiteBaseCuryID<Current2<SOCreateFilter.siteID>>>>),
			Messages.ReplenishmentSiteDiffers, typeof(INSite.branchID), typeof(INSite.siteCD))]
		protected virtual void _(Events.CacheAttached<SOCreateFilter.sourceSiteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<SOFixedDemand.siteID>, IsNull,
			Or<INSite.baseCuryID, EqualSiteBaseCuryID<Current2<SOFixedDemand.siteID>>>>),
			Messages.ReplenishmentSiteDiffers, typeof(INSite.branchID), typeof(INSite.siteCD))]
		protected virtual void _(Events.CacheAttached<SOFixedDemand.sourceSiteID> e)
		{
		}

	}
}
