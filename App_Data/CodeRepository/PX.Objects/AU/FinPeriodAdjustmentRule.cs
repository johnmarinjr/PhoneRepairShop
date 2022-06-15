using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.Process;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.SM;

namespace PX.Objects.AU
{
    internal class FinPeriodScheduleAdjustmentRule : ScheduleAdjustmentRuleBase
	{
        private readonly Func<PXGraph> _cacheGraphFactory;

        public override string TypeID { get; } = AUSchedule.Types.ByFinancialPeriod;

		public FinPeriodScheduleAdjustmentRule(Func<PXGraph> cacheGraphFactory)
		{
			_cacheGraphFactory = cacheGraphFactory ?? throw new ArgumentNullException(nameof(cacheGraphFactory));
		}

        public override void AdjustNextDate(AUSchedule schedule)
        {
            var period = GetClosestPeriod(schedule.LastRunDate ?? schedule.NextRunDate);
            if (period == null)
                throw new PXFinPeriodDoesNotExist(
                    PXMessages.LocalizeNoPrefix(Common.Messages.OperationNotCompletedAbsentFinPeriod));

            int numPeriodShifts = schedule.PeriodFrequency.Value;
            for (int i = 0; i < numPeriodShifts && period != null; i++)
                period = GetClosestPeriod(period.EndDate);
            DateTime nextDate = period != null
                ? GetNextDate(schedule, period)
                : schedule.LastRunDate?.AddMonths(numPeriodShifts) ??
                  schedule.NextRunDate.Value.AddMonths(numPeriodShifts);

            schedule.NextRunDate = nextDate;
            if (period == null)
                throw new PXFinPeriodDoesNotExist(
                    PXMessages.LocalizeNoPrefix(Common.Messages.OperationCompletedAbsentFinPeriod), PXErrorLevel.RowInfo);
        }

        protected virtual FinPeriod GetClosestPeriod(DateTime? date)
		{
			FinPeriod period = null;
			try
			{
				period = PXSelect<FinPeriod,
					 Where<FinPeriod.endDate, Greater<Required<FinPeriod.endDate>>>>
                     .SelectWindowed(_cacheGraphFactory(), 0, 1, date);
			}
			catch
			{
			}
            return period;
        }

        private DateTime GetNextDate(AUSchedule schedule, FinPeriod period)
			{
            DateTime target;
				if (schedule.PeriodDateSel == AUSchedule.FinPeriodDaySelTypes.Start)
				{
					target = period.StartDate.Value;
				}
				else if (schedule.PeriodDateSel == AUSchedule.FinPeriodDaySelTypes.End)
				{
					target = period.EndDate.Value.AddDays(-1.0);
				}
				else
				{
					if ((period.EndDate.Value - period.StartDate.Value).TotalDays >= schedule.PeriodFixedDay)
					{
						target = period.StartDate.Value.AddDays((double)(schedule.PeriodFixedDay - 1.0));
					}
					else
					{
						target = period.EndDate.Value.AddDays(-1.0); ;
					}
				}
			return target;
		}
	}

    [Serializable]
    internal class PXFinPeriodDoesNotExist : PXSetPropertyException
    {
        public PXFinPeriodDoesNotExist(string message)
            : base(message) { }

        public PXFinPeriodDoesNotExist(string message, PXErrorLevel errorLevel)
            : base(message, errorLevel) { }

        public PXFinPeriodDoesNotExist(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
