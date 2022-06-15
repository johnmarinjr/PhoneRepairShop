using PX.Data;
using PX.Objects.Common;
using System.Collections.Generic;
using PX.Objects.CS;
using PX.Objects.CR;
using System;


namespace PX.Objects.DR
{
	public class ScheduleMaintMultipleBaseCurrencies : PXGraphExtension<ScheduleMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate void PerformReleaseCustomScheduleValidationsDelegate(DRSchedule schedule, IEnumerable<DRScheduleDetail> details);
		[PXOverride]
		public void PerformReleaseCustomScheduleValidations(DRSchedule schedule, IEnumerable<DRScheduleDetail> details, PerformReleaseCustomScheduleValidationsDelegate baseMethod)
		{
			if (schedule.BAccountID == null)
			{
				return;
			}

			var baccount = (BAccount)PXSelectorAttribute.Select<DRSchedule.bAccountID>(Base.Schedule.Cache, schedule);
			if (VisibilityRestriction.IsNotEmpty(baccount.COrgBAccountID) || VisibilityRestriction.IsNotEmpty(baccount.VOrgBAccountID))
			{
				if (schedule.BaseCuryID != baccount.BaseCuryID)
				{
					var restrictVisibilityTo = VisibilityRestriction.IsNotEmpty(baccount.COrgBAccountID) ?
						PXAccess.GetBranchByBAccountID(baccount.COrgBAccountID) :
						PXAccess.GetBranchByBAccountID(baccount.VOrgBAccountID);

					throw new PXException(
						Messages.ScheduleCurrencyDifferentFromRestrictVisibilityTo,
						restrictVisibilityTo.BranchCD,
						baccount.AcctCD);
				}
			}

			foreach(var component in details)
			{
				var componentBranch = PXAccess.GetBranch(component.BranchID);
				if (schedule.BaseCuryID != componentBranch.BaseCuryID)
				{
					throw new PXException(
						Messages.ScheduleCurrencyDifferentFromComponentBranchBaseCurrency,
						componentBranch.BranchCD);
				}
			}

		}
	}
}
