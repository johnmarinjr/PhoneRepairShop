using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PR
{
	public class EmploymentPeriods
	{
		public EmploymentPeriods(HashSet<DateTime> employmentDates, bool employedForEntireBatchPeriod)
		{
			EmploymentDates = employmentDates;
			EmployedForEntireBatchPeriod = employedForEntireBatchPeriod;
		}

		public HashSet<DateTime> EmploymentDates { get; }
		public bool EmployedForEntireBatchPeriod { get; }

		public bool IsEmployedOnDate(DateTime date)
		{
			return EmployedForEntireBatchPeriod || EmploymentDates.Contains(date.Date);
		}

		public static EmploymentPeriods GetEmploymentPeriods(PXGraph graph, int? currentEmployeeID, DateTime batchStartDate, DateTime batchEndDate)
		{
			HashSet<DateTime> employmentDates = new HashSet<DateTime>();

			PXResultset<EPEmployeePosition> employeePositionsWithinBatchPeriod =
				SelectFrom<EPEmployeePosition>.
				Where<EPEmployeePosition.employeeID.IsEqual<P.AsInt>.
					And<EPEmployeePosition.startDate.IsLessEqual<P.AsDateTime>>.
					And<EPEmployeePosition.endDate.IsNull.
						Or<EPEmployeePosition.endDate.IsGreaterEqual<P.AsDateTime>>>>.
				OrderBy<EPEmployeePosition.startDate.Desc>.View.
				Select(graph, currentEmployeeID, batchEndDate, batchStartDate);

			foreach (EPEmployeePosition position in employeePositionsWithinBatchPeriod)
			{
				if (position.StartDate <= batchStartDate &&
					(position.EndDate == null || position.EndDate >= batchEndDate))
				{
					return new EmploymentPeriods(new HashSet<DateTime>(), true);
				}

				DateTime startDate = position.StartDate == null || position.StartDate <= batchStartDate
					? batchStartDate
					: position.StartDate.Value;
				DateTime endDate = position.EndDate == null || position.EndDate >= batchEndDate
					? batchEndDate
					: position.EndDate.Value;
				
				for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
				{
					employmentDates.Add(date.Date);
				}
			}

			for (DateTime date = batchStartDate; date <= batchEndDate; date = date.AddDays(1))
			{
				if (!employmentDates.Contains(date))
				{
					return new EmploymentPeriods(employmentDates, false);
				}
			}

			return new EmploymentPeriods(new HashSet<DateTime>(), true);
		}
	}
}
