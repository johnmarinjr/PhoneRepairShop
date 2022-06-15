using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.IN.Attributes;

namespace PX.Objects.IN.GraphExtensions.INItemClassMaintExt
{
	public class MultipleBaseCurrencyExt : PXGraphExtension<INItemClassMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictorWithParameters(typeof(Where<INSite.baseCuryID, Equal<Current<AccessInfo.baseCuryID>>>),
			Messages.ItemDefaultSiteBaseCurrencyDiffers,
				typeof(INSite.branchID), typeof(INSite.siteCD), typeof(Current<AccessInfo.branchID>))]
		protected virtual void _(Events.CacheAttached<INItemClassCurySettings.dfltSiteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictorWithParameters(typeof(Where<INSite.baseCuryID, Equal<Current<AccessInfo.baseCuryID>>>),
			Messages.ItemDefaultSiteBaseCurrencyDiffers,
				typeof(INSite.branchID), typeof(INSite.siteCD), typeof(Current<AccessInfo.branchID>))]
		protected virtual void _(Events.CacheAttached<INItemClassRep.replenishmentSourceSiteID> e)
		{
		}
	}
}
