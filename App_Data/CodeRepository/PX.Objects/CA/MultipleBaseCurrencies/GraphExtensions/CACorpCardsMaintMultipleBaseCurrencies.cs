using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;
using PX.Objects.EP.DAC;

namespace PX.Objects.CA
{
	public class CACorpCardsMaintMultipleBaseCurrencies : PXGraphExtension<CACorpCardsMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDimensionSelector("EMPLOYEE",
			typeof(Search2<CR.Standalone.EPEmployee.bAccountID,
						InnerJoin<CashAccount,
							On<CashAccount.cashAccountID, Equal<Current<CACorpCard.cashAccountID>>>>,
						Where<CR.Standalone.EPEmployee.baseCuryID, Equal<CashAccount.baseCuryID>>>),
				typeof(CR.Standalone.EPEmployee.acctCD),
				typeof(CR.Standalone.EPEmployee.bAccountID),
				typeof(CR.Standalone.EPEmployee.acctCD),
				typeof(CR.Standalone.EPEmployee.acctName),
			typeof(CR.Standalone.EPEmployee.departmentID), DescriptionField = typeof(CR.Standalone.EPEmployee.acctName))]
		protected virtual void _(Events.CacheAttached<EPEmployeeCorpCardLink.employeeID> e)
		{
		}
	}
}