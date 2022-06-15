using PX.Data;
using PX.Data.SQLTree;
using PX.Objects.CS;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class MatchPRCountry<CountryIDField> : Data.BQL.BqlChainableConditionLite<MatchPRCountry<CountryIDField>>, IBqlUnary
			where CountryIDField : IBqlOperand, IBqlField
	{
		/// <exclude/>
		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			result = Verify(cache, item);
		}

		public static bool Verify(PXCache cache, object item)
		{
			string payrollCountry = PRCountryAttribute.GetPayrollCountry();
			return payrollCountry.Equals(cache.GetValue<CountryIDField>(item));
		}

		/// <exclude/>
		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			if (graph == null || !info.BuildExpression || !info.Tables.Contains(typeof(CountryIDField).DeclaringType))
			{
				return true;
			}

			SQLExpression fieldExpression = BqlCommand.GetSingleExpression(typeof(CountryIDField), graph, info.Tables, selection, BqlCommand.FieldPlace.Condition);

			exp = fieldExpression.EQ(PRCountryAttribute.GetPayrollCountry());
			return true;
		}
	}
}
