using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using static PX.Objects.AR.ARStatementPrint;

namespace PX.Objects.AR
{
	public sealed class ARStatementPrintMultipleBaseCurrencies : PXGraphExtension<ARStatementPrint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected void _(Events.FieldVerifying<PrintParameters.statementCycleId> e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>()
					&& Base.ARSetup.Current.PrepareStatements.Equals(AR.ARSetup.prepareStatements.ConsolidatedForAllCompanies))
			{
				e.Cache.RaiseExceptionHandling<PrintParameters.statementCycleId>(e.Row, e.NewValue, new PXSetPropertyException(Messages.StatementsCannotBePrinted));
			}
		}
	}
}
