using PX.Data;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// MRP Forecast Intervals
    /// </summary>
    public class ForecastInterval
    {
        /// <summary>
        /// OneTime = O
        /// </summary>
        public const string OneTime = "O";
        /// <summary>
        /// Weekly = W
        /// </summary>
        public const string Weekly = "W";
        /// <summary>
        /// Monthly = M
        /// </summary>
        public const string Monthly = "M";
        /// <summary>
        /// Yearly = Y
        /// </summary>
        public const string Yearly = "Y";

        /// <summary>
        /// Description/labels for identifiers
        /// </summary>
        public static class Desc
        {
            public static string OneTime => Messages.GetLocal(Messages.OneTime);
            public static string Weekly => Messages.GetLocal(Messages.Weekly);
            public static string Monthly => Messages.GetLocal(Messages.Monthly);
            public static string Yearly => Messages.GetLocal(Messages.Yearly);
        }

        public class oneTime : PX.Data.BQL.BqlString.Constant<oneTime>
        {
            public oneTime() : base(OneTime) { ;}
        }
        public class weekly : PX.Data.BQL.BqlString.Constant<weekly>
        {
            public weekly() : base(Weekly) { ;}
        }
        public class monthly : PX.Data.BQL.BqlString.Constant<monthly>
        {
            public monthly() : base(Monthly) { ;}
        }
        public class yearly : PX.Data.BQL.BqlString.Constant<yearly>
        {
            public yearly() : base(Yearly) { ;}
        }

        public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute()
                : base(
                    new string[] { 
                        OneTime, 
                        Weekly, 
                        Monthly, 
                        Yearly },
                    new string[] {
                        Messages.OneTime, 
                        Messages.Weekly, 
                        Messages.Monthly, 
                        Messages.Yearly  }) { }
        }

		public static bool IsIntervalKey(string key)
		{
			switch (key)
			{
				case OneTime:
				case Weekly:
				case Monthly:
				case Yearly:
					return true;
			}

			return false;
		}

		public static string ToIntervalKey(string label)
		{
			if(string.IsNullOrWhiteSpace(label))
			{
				return null;
			}

			var upperValue = label.ToUpper();
			if(upperValue == Desc.OneTime.ToUpper() || upperValue == OneTime)
			{
				return OneTime;
			}

			if(upperValue == Desc.Weekly.ToUpper() || upperValue == Weekly)
			{
				return Weekly;
			}

			if(upperValue == Desc.Monthly.ToUpper() || upperValue == Monthly)
			{
				return Monthly;
			}

			if(upperValue == Desc.Yearly.ToUpper() || upperValue == Yearly)
			{
				return Yearly;
			}

			return null;
		}

        /// <summary>
        /// Get end date based on the begin date and the interval type
        /// </summary>
        /// <param name="interval">Forecast interval type</param>
        /// <param name="beginDate">Begin date</param>
        /// <returns>Calculated end date</returns>
        public static System.DateTime GetEndDate(string interval, System.DateTime beginDate)
        {
            switch (interval)
            {
                case Yearly:
                    return beginDate.AddYears(1).AddDays(-1);
                case Monthly:
                    return beginDate.AddMonths(1).AddDays(-1);
                case Weekly:
                    return beginDate.AddDays(6);
            }

            return beginDate;
        }
    }
}
