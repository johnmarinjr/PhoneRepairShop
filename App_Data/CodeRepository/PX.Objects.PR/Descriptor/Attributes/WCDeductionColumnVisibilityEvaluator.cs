using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Payroll.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.PR
{
	public class WCDeductionColumnVisibilityEvaluator : BqlFormulaEvaluator, IBqlOperand
	{
		public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> parameters)
		{
			return new SelectFrom<PRDeductCode>
				.Where<PRDeductCode.isWorkersCompensation.IsEqual<True>
					.And<PRDeductCode.contribType.IsNotEqual<ContributionTypeListAttribute.employerContribution>>
					.And<MatchPRCountry<PRDeductCode.countryID>>>.View(cache.Graph).SelectSingle() != null;
		}
	}
}
