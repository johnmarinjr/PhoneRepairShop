using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class CanadianPTOValidatePaycheckTotalsExt : PXGraphExtension<PRValidatePaycheckTotals>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>();
		}

		#region Data views
		public SelectFrom<PRPaymentPTOBank>
			.Where<PRPaymentPTOBank.FK.Payment.SameAsCurrent
				.And<PRPaymentPTOBank.createFinancialTransaction.IsEqual<True>>
				.And<PRPaymentPTOBank.isActive.IsEqual<True>>>.View FinancialPaymentPTOBanks;

		public SelectFrom<PRPTODetail>
			.Where<PRPTODetail.FK.Payment.SameAsCurrent>.View PTODetails;

		public SelectFrom<PRPTODetail>
			.LeftJoin<PRPaymentPTOBank>.On<PRPaymentPTOBank.FK.Payment.SameAsCurrent
				.And<PRPaymentPTOBank.bankID.IsEqual<PRPTODetail.bankID>>>
			.Where<PRPTODetail.FK.Payment.SameAsCurrent
				.And<PRPaymentPTOBank.bankID.IsNull>>.View PTODetailsWithMissingSummary;
		#endregion Data views

		#region Base graph overrides
		public delegate void DoAdditionalValidationsDelegate(bool tryCorrectTotalsOnDiscrepancy);
		[PXOverride]
		public virtual void DoAdditionalValidations(bool tryCorrectTotalsOnDiscrepancy, DoAdditionalValidationsDelegate baseMethod)
		{
			baseMethod(tryCorrectTotalsOnDiscrepancy);
			ValidatePTODetails(tryCorrectTotalsOnDiscrepancy);
		}
		#endregion Base graph overrides

		#region Helpers
		protected virtual void ValidatePTODetails(bool tryCorrectTotalsOnDiscrepancy)
		{
			Dictionary<string, IEnumerable<PRPTODetail>> ptoDetails = PTODetails.Select().FirstTableItems
				.GroupBy(x => x.BankID)
				.ToDictionary(k => k.Key, v => v.AsEnumerable());
			foreach (IGrouping<string, PRPaymentPTOBank> paymentBankGroup in FinancialPaymentPTOBanks.Select().FirstTableItems.GroupBy(x => x.BankID))
			{
				decimal detailAmount = 0;
				if (ptoDetails.ContainsKey(paymentBankGroup.Key))
				{
					detailAmount = ptoDetails[paymentBankGroup.Key].Sum(x => x.Amount.GetValueOrDefault());
				}

				decimal summaryAmount = paymentBankGroup.Sum(x => x.AccrualAmount.GetValueOrDefault());
				if (summaryAmount != detailAmount)
				{
					if (paymentBankGroup.Count() == 1 && tryCorrectTotalsOnDiscrepancy)
					{
						paymentBankGroup.First().AccrualAmount = detailAmount;
						PXTrace.WriteWarning(
							PXMessages.LocalizeFormat(Messages.SummaryPTODoesntMatch, paymentBankGroup.Key, summaryAmount, detailAmount) 
								+ PXMessages.LocalizeNoPrefix(Messages.UpdatingFromDetailsWarning));
						FinancialPaymentPTOBanks.Update(paymentBankGroup.First());
					}
					else
					{
						throw new PXException(Messages.SummaryPTODoesntMatch, paymentBankGroup.Key, summaryAmount, detailAmount);
					}
				}
			}

			IEnumerable<PRPTODetail> detailsMissingSummary = PTODetailsWithMissingSummary.Select().FirstTableItems;
			if (detailsMissingSummary.Any())
			{
				throw new PXException(Messages.SummaryPTOMissing, detailsMissingSummary.First().BankID);
			}
		}
		#endregion Helpers
	}
}
