using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using System;
using System.Collections.Generic;

namespace PX.Objects.SO.GraphExtensions.ARPaymentEntryExt
{
	public class AffectedSOOrdersWithCreditLimitExt : AffectedSOOrdersWithCreditLimitExtBase<ARPaymentEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		private HashSet<SOOrder> ordersChangedDuringPersist;

		public override void Persist(Action basePersist)
		{
			ordersChangedDuringPersist = new HashSet<SOOrder>(Base.Caches<SOOrder>().GetComparer());
			base.Persist(basePersist);
		}

		protected virtual void _(Events.RowUpdated<SOOrder> args)
		{
			if (ordersChangedDuringPersist != null && args.Row.IsFullyPaid != args.OldRow.IsFullyPaid)
				ordersChangedDuringPersist.Add(args.Row);
		}

		protected override IEnumerable<SOOrder> GetLatelyAffectedEntities() => ordersChangedDuringPersist;
		protected override void OnProcessed(SOOrderEntry foreignGraph) => ordersChangedDuringPersist = null;
	}
}
