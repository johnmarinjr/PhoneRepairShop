using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO.GraphExtensions.ARReleaseProcessExt
{
	public class AffectedSOOrdersWithCreditLimitExt : AffectedSOOrdersWithCreditLimitExtBase<ARReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		public override void Persist(Action basePersist)
		{
			IEnumerable<SOOrder> affectedEntities = GetAffectedEntities();
			IEnumerable<SOOrder> lateAffectedEntities = GetLatelyAffectedEntities();

			base.Persist(basePersist);

			if (lateAffectedEntities != null || affectedEntities.Any())
			{
				var typesOfDirtyCaches = Base.FindImplementation<AffectedSOOrdersWithCreditLimitExt>().
					ProcessAffectedEntities(lateAffectedEntities == null ? affectedEntities : lateAffectedEntities.Union(affectedEntities, Base.Caches<SOOrder>().GetComparer()));

				ClearCaches(Base, typesOfDirtyCaches);
			}
		}
	}
}
