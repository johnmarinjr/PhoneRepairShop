using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PR
{
	public class PTOAccrualMethod
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { Percentage, TotalHoursPerYear },
				new string[] { Messages.Percentage, Messages.TotalHoursPerYear })
			{ }
		}

		public class percentage : PX.Data.BQL.BqlString.Constant<percentage>
		{
			public percentage() : base(Percentage) { }
		}

		public class totalHoursPerYear : PX.Data.BQL.BqlString.Constant<totalHoursPerYear>
		{
			public totalHoursPerYear() : base(TotalHoursPerYear) { }
		}

		public const string Percentage = "PCT";
		public const string TotalHoursPerYear = "HPY";
	}
}
