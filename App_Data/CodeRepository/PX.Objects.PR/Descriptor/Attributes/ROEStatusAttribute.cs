using PX.Data;

namespace PX.Objects.PR
{
	public class ROEStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { Open, Exported, Submitted, NeedsAmendment, Amended },
				new string[] { Messages.OpenROE, Messages.Exported, Messages.Submitted, Messages.NeedsAmendment, Messages.Amended })
			{ }
		}

		public class open : PX.Data.BQL.BqlString.Constant<open>
		{
			public open() : base(Open)
			{
			}
		}

		public class exported : PX.Data.BQL.BqlString.Constant<exported>
		{
			public exported() : base(Exported)
			{
			}
		}

		public class submitted : PX.Data.BQL.BqlString.Constant<submitted>
		{
			public submitted() : base(Submitted)
			{
			}
		}

		public class needsAmendment : PX.Data.BQL.BqlString.Constant<needsAmendment>
		{
			public needsAmendment() : base(NeedsAmendment)
			{
			}
		}

		public class amended : PX.Data.BQL.BqlString.Constant<amended>
		{
			public amended() : base(Amended)
			{
			}
		}

		public const string Open = "OPN";
		public const string Exported = "EXP";
		public const string Submitted = "SMT";
		public const string NeedsAmendment = "NAM";
		public const string Amended = "AMD";
	}
}
