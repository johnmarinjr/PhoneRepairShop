

using PX.Data;

namespace PX.Objects.AM.Attributes
{
	/// <summary>
	/// Actions for the Rough Cut Planning processing screen (<see cref="APSRoughCutProcess"/>)
	/// </summary>
	public class APSRoughCutProcessActions
	{
        public const string Schedule = "S";
		public const string ScheduleAndFirm = "A";
		public const string Firm = "F";
		public const string UndoFirm = "U";

		public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute()
                : base(new string[]{ Schedule, ScheduleAndFirm, Firm, UndoFirm }
                , new string[]{ Messages.Schedule, Messages.ScheduleAndFirm, Messages.Firm, Messages.UndoFirm }) { }
        }
	}
}
