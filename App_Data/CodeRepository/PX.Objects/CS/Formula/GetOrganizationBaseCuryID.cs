using System;
using System.Collections.Generic;
using PX.Data;

namespace PX.Objects.GL
{
	public class GetOrganizationBaseCuryID<OrganizationBAccountID> : BqlFormulaEvaluator<OrganizationBAccountID>, IBqlOperand
		where OrganizationBAccountID : IBqlOperand
	{
		public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> parameters)
		{
			var bAccountID = (int?)parameters[typeof(OrganizationBAccountID)];
			if (bAccountID == null) return null;

			var branch = PXAccess.GetBranchByBAccountID(bAccountID);
			if (branch != null) return branch.BaseCuryID;

			var org = PXAccess.GetOrganizationByBAccountID(bAccountID);
			if (org != null) return org.BaseCuryID;

			return null;
		}
	}
}
