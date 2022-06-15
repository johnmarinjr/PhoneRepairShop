using PX.Common;
using PX.Objects.CR;
using System;

namespace PX.Objects.PR
{
	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
	public static class TimeZoneHelper
	{
		public static TimeSpan GetOffsetAgainstCurrent(PXTimeZoneInfo timeZoneInfo)
		{
			PXTimeZoneInfo currentTimeZone = LocaleInfo.GetTimeZone();
			TimeSpan zoneDiff = new TimeSpan(0);

			if (timeZoneInfo != null && currentTimeZone.Id != timeZoneInfo.Id)
			{
				zoneDiff = timeZoneInfo.UtcOffset - currentTimeZone.BaseUtcOffset;
			}

			return zoneDiff;
		}

		public static DateTime GetAdjustedDate(DateTime date, string timeZoneID)
		{
			PXTimeZoneInfo timeZoneInfo = PXTimeZoneInfo.FindSystemTimeZoneById(timeZoneID);
			TimeSpan timeZoneOffset = GetOffsetAgainstCurrent(timeZoneInfo);

			return date.Date.Add(timeZoneOffset).Date;
		}

		public static DateTime GetTimeZoneAdjustedDate(PMTimeActivity timeActivity)
		{
			return GetAdjustedDate(timeActivity.Date.Value, timeActivity.ReportedInTimeZoneID);
		}
	}
}
