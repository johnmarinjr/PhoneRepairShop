using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.AP
{
	public class APPaymentEntryMultipleBaseCurrencies : PXGraphExtension<APPaymentEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<APPayment.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<APPayment.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			string vendorBaseCuryID = (string)PXFormulaAttribute.Evaluate<APPaymentMultipleBaseCurrenciesRestriction.vendorBaseCuryID>(e.Cache, e.Row);

			if (vendorBaseCuryID != null && branch != null
				&& branch.BaseCuryID != vendorBaseCuryID)
			{
				e.NewValue = branch.BranchCD;
				BAccountR vendor = PXSelectorAttribute.Select<APPayment.vendorID>(e.Cache, e.Row) as BAccountR;
				throw new PXSetPropertyException(Messages.BranchVendorDifferentBaseCury, PXOrgAccess.GetCD(vendor.VOrgBAccountID), vendor.AcctCD);
			}
		}

		protected virtual void _(Events.RowUpdated<APPayment> e)
		{
			Branch branch = PXSelectorAttribute.Select<APPayment.branchID>(e.Cache, e.Row, e.Row.BranchID) as Branch;
			PXFieldState vendorBaseCuryID = e.Cache.GetValueExt<APPaymentMultipleBaseCurrenciesRestriction.vendorBaseCuryID>(e.Row) as PXFieldState;
			if (vendorBaseCuryID?.Value != null && branch != null
				&& branch.BaseCuryID != vendorBaseCuryID.ToString())
			{
				e.Row.BranchID = null;
			}
		}
	}
}
