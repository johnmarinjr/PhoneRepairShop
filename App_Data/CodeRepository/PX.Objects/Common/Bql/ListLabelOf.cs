using System;
using System.Collections.Generic;
using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.Common.Bql
{
	public class ListLabelOf<TStringField> : BqlFunction<ListLabelOf<TStringField>.Evaluator, IBqlString>
		where TStringField : IBqlField, PX.Common.IImplement<IBqlString>
	{
		public class Evaluator : BqlFormulaEvaluator, IBqlOperand
		{
			public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> parameters)
			{
				return PXStringListAttribute.GetLocalizedLabel<TStringField>(cache, item);
			}
		}
	}
}
