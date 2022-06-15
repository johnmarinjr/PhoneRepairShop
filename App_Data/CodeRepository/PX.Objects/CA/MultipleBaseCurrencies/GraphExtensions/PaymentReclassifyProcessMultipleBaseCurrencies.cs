using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.CA
{
	public class PaymentReclassifyProcessMultipleBaseCurrencies : PXGraphExtension<PaymentReclassifyProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldUpdated<PaymentReclassifyProcess.Filter.branchID> e)
		{
			PaymentReclassifyProcess.Filter row = e.Row as PaymentReclassifyProcess.Filter;
			if (row == null) return;

			Branch currentBranch = PXSelectorAttribute.Select<PaymentReclassifyProcess.Filter.branchID>(e.Cache, row) as Branch;
			PXFieldState accFieldState = e.Cache.GetValueExt<PaymentReclassifyProcess.Filter.accountID>(e.Row) as PXFieldState;

			if (accFieldState == null) return;
			CashAccount currentCashAccount = PXSelectorAttribute.Select<PaymentReclassifyProcess.Filter.accountID>(e.Cache, row, accFieldState.Value) as CashAccount;

			if (currentCashAccount != null && (currentBranch?.BaseCuryID != currentCashAccount.BaseCuryID || currentCashAccount.RestrictVisibilityWithBranch == true))
			{
				e.Cache.SetValue<PaymentReclassifyProcess.Filter.accountID>(row, null);
				e.Cache.SetValueExt<PaymentReclassifyProcess.Filter.accountID>(row, null);
				e.Cache.SetValuePending<PaymentReclassifyProcess.Filter.accountID>(row, null);
				e.Cache.RaiseExceptionHandling<PaymentReclassifyProcess.Filter.accountID>(row, null, null);
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<BAccountR.baseCuryID, EqualBaseCuryID<Current<PaymentReclassifyProcess.Filter.branchID>>>), "")]
		protected virtual void _(Events.CacheAttached<CASplitExt.referenceID> e)
		{
		}

		protected virtual void _(Events.RowSelected<CASplitExt> e)
		{
			CASplitExt row = e.Row as CASplitExt;

			if (row != null && row.ReferenceID != null)
			{
				BAccountR bAccount = PXSelectorAttribute.Select<CASplitExt.referenceID>(e.Cache, row) as BAccountR;

				if (bAccount == null) 
				{
					bAccount = PXSelectReadonly<BAccountR,Where<BAccountR.bAccountID, Equal<Required<CASplitExt.referenceID>>>>.Select(this.Base, row.ReferenceID);
					
					var newValue = bAccount == null ? (object)row.ReferenceID : (object)bAccount.AcctCD;
					e.Cache.RaiseExceptionHandling<CASplitExt.referenceID>(row, newValue, 
						new PXSetPropertyException(Messages.FieldCanNotBeFound, PXUIFieldAttribute.GetDisplayName<CASplitExt.referenceID>(e.Cache)));
				}
			}
		}
	}
}