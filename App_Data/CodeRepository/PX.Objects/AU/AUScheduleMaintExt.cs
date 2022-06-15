using System;
using PX.Data;
using PX.SM;

namespace PX.Objects.AU
{
	public class AUScheduleMaintExt : PXGraphExtension<AUScheduleMaint>
	{
		protected void AUSchedule_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e, PXRowUpdated baseHandler)
		{
			try
			{
				baseHandler(sender, e);
			}
			catch (PXFinPeriodDoesNotExist)
			{
				AUSchedule schedule = (AUSchedule)e.Row;
				sender.RaiseExceptionHandling<AUSchedule.nextRunDate>(schedule, schedule.NextRunDate,
					new PXSetPropertyException(PXMessages.Localize(Common.Messages.NotOpenedFinPeriod)));

				schedule.NextRunTime = schedule.NextRunDate.Value.Date.Add(new TimeSpan(schedule.NextRunTime.Value.Hour, schedule.NextRunTime.Value.Minute, 0));
				Base.ScheduleCurrentScreen.Current.ScreenID = schedule.ScreenID;
			}
		}
	}
}
