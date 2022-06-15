using PX.Data;

namespace PX.Objects.PR
{
	public class SettlementBalanceType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { Pay, Keep, Discard},
				new string[] { Messages.Pay, Messages.Keep, Messages.Discard})
			{ }
		}

		public class pay : PX.Data.BQL.BqlString.Constant<pay>
		{
			public pay() : base(Pay) { }
		}

		public class keep : PX.Data.BQL.BqlString.Constant<keep>
		{
			public keep() : base(Keep) { }
		}

		public class discard : PX.Data.BQL.BqlString.Constant<discard>
		{
			public discard() : base(Discard) { }
		}

		public const string Pay = "PAY";
		public const string Keep = "KEE";
		public const string Discard = "DIS";
	}
}
