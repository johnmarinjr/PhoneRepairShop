using PX.Common;
using PX.Data;
using PX.Data.BQL;
using System;

namespace PX.Objects.IN.RelatedItems
{
	public class InventoryRelation
	{
		[Flags]
        public enum RelationType: int
        {
			None = 0,
			Substitute = 1,
			CrossSell = 2,
			Other = 4,
			UpSell = 8,

			CrossSellAndOther = CrossSell | Other,
			All = Substitute | CrossSell | Other | UpSell
		}

		public const string CrossSell = "CSELL";
		public const string UpSell = "USELL";
		public const string Substitute = "SUBST";
		public const string Other = "OTHER";
		public const string All = "ALL__";

		[PXLocalizable]
		public static class Desc
		{
			public const string CrossSell = "Cross-Sell";
			public const string UpSell = "Up-Sell";
			public const string Substitute = "Substitute";
			public const string Other = "Other";
			public const string All = "All";
		}

		public class ListAttribute: PXStringListAttribute
		{
			private static (string Value, string Label)[] _values =
				new[]
				{
					(CrossSell, Desc.CrossSell),
					(UpSell, Desc.UpSell),
					(Substitute, Desc.Substitute),
					(Other, Desc.Other)
				};
			public ListAttribute()
				: base(_values)
			{ }

			protected ListAttribute(params (string Value, string Label)[] additionalValues)
				: base(_values.Append(additionalValues))
			{ }

			public class WithAllAttribute : ListAttribute
			{
				public WithAllAttribute() : base((All, Desc.All)) { }
			}
		}

		public class crossSell : BqlString.Constant<crossSell> { public crossSell() : base(CrossSell) { } };
		public class upSell : BqlString.Constant<upSell> { public upSell() : base(UpSell) { } };
		public class substitute : BqlString.Constant<substitute> { public substitute() : base(Substitute) { } };
		public class other : BqlString.Constant<other> { public other() : base(Other) { } };
		public class all : BqlString.Constant<all> { public all() : base(All) { } };
	}
}
