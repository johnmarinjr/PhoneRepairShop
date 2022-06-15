using PX.Data;

namespace PX.Objects.PR
{
	public class PTODisbursingType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { CurrentRate, AverageRate },
				new string[] { Messages.CurrentRate, Messages.AverageRate })
			{ }
		}

		public class currentRate : PX.Data.BQL.BqlString.Constant<currentRate>
		{
			public currentRate() : base(CurrentRate) { }
		}

		public class averageRate : PX.Data.BQL.BqlString.Constant<averageRate>
		{
			public averageRate() : base(AverageRate) { }
		}

		public const string CurrentRate = "C";
		public const string AverageRate = "A";
	}
}
