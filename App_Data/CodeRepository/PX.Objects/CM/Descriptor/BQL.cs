using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.SQLTree;
using Selection = PX.Data.BqlCommand.Selection;

namespace PX.Objects.CM
{
	public class IsBaseCurrency : IBqlComparison, IBqlCreator, IBqlVerifier
	{
		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			result = CurrencyCollection.IsBaseCurrency((string)value);
		}

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, Selection selection)
		{
			if (graph != null && info.BuildExpression)
			{
				var baseCurrencies = CurrencyCollection.GetBaseCurrencies();
				var list = baseCurrencies.Select(_ => new SQLConst(_));
				exp = exp.In(list);
			}
			return true;
		}
	}
}
