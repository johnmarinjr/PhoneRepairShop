using PX.Data;
using PX.Data.BQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.EP
{
	public class EPShiftCodeType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new string[] { Amount, Percent },
				new string[] { Messages.Amount, Messages.Percent })
			{ }
		}

		public class amount : BqlString.Constant<amount>
		{
			public amount() : base(Amount) { }
		}

		public class percent : BqlString.Constant<percent>
		{
			public percent() : base(Percent) { }
		}

		public const string Amount = "AMT";
		public const string Percent = "PCT";
	}
}