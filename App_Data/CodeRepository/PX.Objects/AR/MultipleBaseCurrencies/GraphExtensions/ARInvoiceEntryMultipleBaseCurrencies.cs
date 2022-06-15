using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using System;


namespace PX.Objects.AR
{
	public class ARInvoiceEntryMultipleBaseCurrencies : PXGraphExtension<ARInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<ARInvoice.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<ARInvoice.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			string customerBaseCuryID = (string)PXFormulaAttribute.Evaluate<ARInvoiceMultipleBaseCurrenciesRestriction.customerBaseCuryID>(e.Cache, e.Row);

			if (customerBaseCuryID != null && branch != null
				&& branch.BaseCuryID != customerBaseCuryID)
			{
				e.NewValue = branch.BranchCD;
				BAccountR customer = PXSelectorAttribute.Select<ARInvoice.customerID>(e.Cache, e.Row) as BAccountR;
				throw new PXSetPropertyException(Messages.BranchCustomerDifferentBaseCury, PXOrgAccess.GetCD(customer.COrgBAccountID), customer.AcctCD);
			}
		}

		protected virtual void _(Events.RowUpdated<ARInvoice> e)
		{
			Branch branch = PXSelectorAttribute.Select<ARInvoice.branchID>(e.Cache, e.Row, e.Row.BranchID) as Branch;
			PXFieldState customerBaseCuryID = e.Cache.GetValueExt<ARInvoiceMultipleBaseCurrenciesRestriction.customerBaseCuryID>(e.Row) as PXFieldState;
			if (customerBaseCuryID?.Value != null && branch != null
				&& branch.BaseCuryID != customerBaseCuryID.ToString())
			{
				e.Row.BranchID = null;
			}
		}
	}
}
