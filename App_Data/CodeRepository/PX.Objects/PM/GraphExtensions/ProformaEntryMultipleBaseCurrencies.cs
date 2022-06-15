using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using System;


namespace PX.Objects.PM
{
	public class ProformaEntryMultipleBaseCurrencies : PXGraphExtension<ProformaEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<PMProforma.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<PMProforma.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			PXFieldState customerBaseCuryID = e.Cache.GetValueExt<ProformaMultipleBaseCurrenciesRestriction.customerBaseCuryID>(e.Row) as PXFieldState;
			if (customerBaseCuryID?.Value != null
				&& branch.BaseCuryID != customerBaseCuryID.ToString())
			{
				e.NewValue = branch.BranchCD;
				BAccountR customer = PXSelectorAttribute.Select<PMProforma.customerID>(e.Cache, e.Row) as BAccountR;
				throw new PXSetPropertyException(AR.Messages.BranchCustomerDifferentBaseCury, PXOrgAccess.GetCD(customer.COrgBAccountID), customer.AcctCD);
			}
		}
	}
}
