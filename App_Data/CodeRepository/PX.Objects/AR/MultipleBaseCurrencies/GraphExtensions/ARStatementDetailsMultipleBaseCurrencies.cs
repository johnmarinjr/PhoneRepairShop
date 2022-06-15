using System;
using System.Collections;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using static PX.Objects.AR.ARStatementDetails;

namespace PX.Objects.AR
{
	public sealed class ARStatementDetailsMultipleBaseCurrencies : PXGraphExtension<ARStatementDetails>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate IEnumerable PrintReportDelegate(PXAdapter adapter);
		[PXOverride]
		public IEnumerable PrintReport(PXAdapter adapter, PrintReportDelegate baseMethod)
		{
			if (Base.Details.Current != null)
			{
				DetailsResult res = Base.Details.Current;

				Customer customer = PXSelect<Customer,
						Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
						.Select(Base, res.CustomerId);

				if (PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>()
					&& Base.ARSetup.Current.PrepareStatements.Equals(AR.ARSetup.prepareStatements.ConsolidatedForAllCompanies)
					&& customer.BaseCuryID == null)
				{
					throw new PXException(Messages.StatementsCannotBePrinted);
				}
			}

			return baseMethod(adapter);
		}
	}
}
