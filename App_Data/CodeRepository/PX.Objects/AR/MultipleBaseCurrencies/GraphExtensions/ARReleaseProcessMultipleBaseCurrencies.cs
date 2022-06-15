using System;
using System.Linq;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.AR
{
	public class ARReleaseProcessMultipleBaseCurrencies : PXGraphExtension<ARReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate void PerformBasicReleaseChecksDelegate(PXGraph selectGraph, ARRegister document);
		[PXOverride]
		public void PerformBasicReleaseChecks(PXGraph selectGraph, ARRegister document, PerformBasicReleaseChecksDelegate baseMethod)
		{
			baseMethod(selectGraph, document);

			var SelectCustomer = SelectFrom<ARRegister>
					.InnerJoin<Branch>.On<ARRegister.branchID.IsEqual<Branch.branchID>>
					.InnerJoin<Customer>.On<ARRegister.customerID.IsEqual<Customer.bAccountID>>
				.Where<ARRegister.docType.IsEqual<@P.AsString>
					.And<ARRegister.refNbr.IsEqual<@P.AsString>>
					.And<Branch.baseCuryID.IsNotEqual<Customer.baseCuryID>>
					.And<Customer.baseCuryID.IsNotNull>>
				.View.SelectSingleBound(Base, null, new object[] { document.DocType, document.RefNbr }).RowCast<Customer>();

			if (SelectCustomer.Any())
			{
				Customer customer = SelectCustomer.First();
				throw new PX.Objects.Common.Exceptions.ReleaseException(Messages.BranchCustomerDifferentBaseCuryReleased, PXOrgAccess.GetCD(customer?.COrgBAccountID), customer?.AcctCD);
			}
		}
	}
}
