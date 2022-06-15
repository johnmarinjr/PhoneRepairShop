using PX.Data;
using System.Collections.Generic;

namespace PX.Objects.AM.Attributes
{
	/// <summary>
	/// Production Schedule Status
	/// </summary>
	public class ProductionScheduleStatus
	{
        /// <summary>
        /// A production order that has not been finite scheduled
        /// Unscheduled = U    (Default value)
        /// </summary>
        public const string Unscheduled = "U";
        /// <summary>
        /// A production order that has been finite scheduled and could be rescheduled if rough cut is run again
        /// Scheduled = S
        /// </summary>
        public const string Scheduled = "S";
        /// <summary>
        /// A production order that has been scheduled and should not be rescheduled when rough cut is run again
        /// Firmed = F
        /// </summary>
        public const string Firm = "F";

		private static readonly char Separator = ',';

		public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute()
                : base(
                    new string[] { 
                        Unscheduled,
                        Scheduled, 
                        Firm},
                    new string[] {
                        Messages.Unscheduled,
                        Messages.Scheduled,
                        Messages.Firm}) 
            {
                //MultiSelect = true;
            }
        }
		
        public static string MaxStatus(string status1, string status2)
        {
            var rank1 = StatusAsRank(status1);
            var rank2 = StatusAsRank(status2);

            if(rank1 > rank2)
            {
                return status1;
            }

            return rank1 >= rank2 ? status1 : status2;
        }

        private static int StatusAsRank(string status)
        {
            switch (status)
            {
                case Unscheduled:
                    return 1;
                case Scheduled:
                    return 2;
                case Firm:
                    return 3;
            }

            return 0;
        }


		public static string[] Parse(string value)
		{
			return value?.Split(Separator) ?? new string[0];
		}

        public class unscheduled : PX.Data.BQL.BqlString.Constant<unscheduled>
        {
            public unscheduled() : base(Unscheduled) { }
        }

        public class scheduled : PX.Data.BQL.BqlString.Constant<scheduled>
        {
            public scheduled() : base(Scheduled) { }
        }

        public class firmed : PX.Data.BQL.BqlString.Constant<firmed>
        {
            public firmed() : base(Firm) { }
        }
	}
}