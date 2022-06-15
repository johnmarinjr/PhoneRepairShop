using PX.Data;
using System;

namespace PX.Objects.CR
{
	#region RemindAtListAttribute

	public class RemindAtListAttribute : PXStringListAttribute, IPXRowSelectedSubscriber
	{
		public Type IsAllDay { get; set; }

		#region List 
		public const string AtTheTimeOfEvent = "ATEV";
		public const string Before5minutes = "B05m";
		public const string Before15minutes = "B15m";
		public const string Before30minutes = "B30m";
		public const string Before1hour = "B01h";
		public const string Before2hours = "B02h";
		public const string Before1day = "B01d";
		public const string Before3days = "B03d";
		public const string Before1week = "B07d";
		public const string DateTimeFromExchange = "EXCH";

		protected string[] ValuesForMinutes = {
					AtTheTimeOfEvent,
					Before5minutes,
					Before15minutes,
					Before30minutes,
					Before1hour,
					Before2hours,
					Before1day,
					Before3days,
					Before1week};

		protected string[] LabelsForMinutes = {
					Messages.AtTheTimeOfEvent,
					Messages.Before5minutes,
					Messages.Before15minutes,
					Messages.Before30minutes,
					Messages.Before1hour,
					Messages.Before2hours,
					Messages.Before1day,
					Messages.Before3days,
					Messages.Before1week };

		protected string[] ValuesForDays = {
					AtTheTimeOfEvent,
					Before1day,
					Before3days,
					Before1week };

		protected string[] LabelsForDays = {
					Messages.AtTheTimeOfEvent,
					Messages.Before1day,
					Messages.Before3days,
					Messages.Before1week };

		public class before15minutes : PX.Data.BQL.BqlString.Constant<before15minutes>
		{
			public before15minutes() : base(Before15minutes) { }
		}
		public class before1day : PX.Data.BQL.BqlString.Constant<before1day>
		{
			public before1day() : base(Before1day) { }
		}
		#endregion

		#region Events

		public virtual void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (IsAllDay != null)
			{
				bool? result = null;
				object value = null;

				BqlFormula.Verify(sender, e.Row, PXFormulaAttribute.InitFormula(BqlCommand.Compose(typeof(Current<>), IsAllDay)), ref result, ref value);

				if (value is bool boolValue && boolValue)
				{
					SetList(sender, e.Row, _FieldName, ValuesForDays, LabelsForDays);
					return;
				}
			}
			SetList(sender, e.Row, _FieldName, ValuesForMinutes, LabelsForMinutes);
		}

		#endregion

		#region Functions
		public static TimeSpan GetRemindAtTimeSpan(string remindAt)
		{
			var timeSpan = new TimeSpan();
			switch (remindAt)
			{
				case AtTheTimeOfEvent:
					break;
				case Before5minutes:
					timeSpan = new TimeSpan(0, -5, 0);
					break;
				case Before15minutes:
					timeSpan = new TimeSpan(0, -15, 0);
					break;
				case Before30minutes:
					timeSpan = new TimeSpan(0, -30, 0);
					break;
				case Before1hour:
					timeSpan = new TimeSpan(-1, 0, 0);
					break;
				case Before2hours:
					timeSpan = new TimeSpan(-2, 0, 0);
					break;
				case Before1day:
					timeSpan = new TimeSpan(-1, 0, 0, 0);
					break;
				case Before3days:
					timeSpan = new TimeSpan(-3, 0, 0, 0);
					break;
				case Before1week:
					timeSpan = new TimeSpan(-7, 0, 0, 0);
					break;
				default:
					break;
			}
			return timeSpan;
		}
		#endregion
	}

	#endregion
}