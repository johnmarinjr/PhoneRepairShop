using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using System;


namespace PX.Objects.AR
{
	public class ARPaymentEntryMultipleBaseCurrencies : PXGraphExtension<ARPaymentEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<ARPayment.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<ARPayment.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			string customerBaseCuryID = (string)PXFormulaAttribute.Evaluate<ARPaymentMultipleBaseCurrenciesRestriction.customerBaseCuryID>(e.Cache, e.Row);

			if (customerBaseCuryID != null && branch != null
				&& branch.BaseCuryID != customerBaseCuryID)
			{
				e.NewValue = branch.BranchCD;
				BAccountR customer = PXSelectorAttribute.Select<ARPayment.customerID>(e.Cache, e.Row) as BAccountR;
				throw new PXSetPropertyException(Messages.BranchCustomerDifferentBaseCury, PXOrgAccess.GetCD(customer.COrgBAccountID), customer.AcctCD);
			}
		}

		protected virtual void _(Events.RowUpdated<ARPayment> e)
		{
			Branch branch = PXSelectorAttribute.Select<ARPayment.branchID>(e.Cache, e.Row, e.Row.BranchID) as Branch;
			PXFieldState customerBaseCuryID = e.Cache.GetValueExt<ARPaymentMultipleBaseCurrenciesRestriction.customerBaseCuryID>(e.Row) as PXFieldState;
			if (customerBaseCuryID?.Value != null && branch != null
				&& branch.BaseCuryID != customerBaseCuryID.ToString())
			{
				e.Row.BranchID = null;
			}
		}
	}
}
