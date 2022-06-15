using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Payroll.Data;
using PX.Payroll.Data.Vertex;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.GL.FinPeriods;

namespace PX.Objects.PR
{
	public class PRPayChecksAndAdjustments : PXGraph<PRPayChecksAndAdjustments, PRPayment>
	{
		private bool _IsTimeActivitiesImportInProgress = false;
		private bool _AllowUpdatePaymentChildrenRecords = true;
		public bool IsVoidCheckInProgress { get; protected set; } = false;
		private readonly Type[] CountryPayrollFeatureSwitches = new Type[] { typeof(FeaturesSet.payrollUS), typeof(FeaturesSet.payrollCAN) };

		public PXCopyPastePaymentAction PXCopyPastePayment;

		#region Views
		[PXCopyPasteHiddenView]
		public PXSetup<PRSetup> PRSetup;
		public class SetupValidation : PRSetupValidation<PRPayChecksAndAdjustments> { }

		[PXCopyPasteHiddenFields(typeof(PRPayment.batchNbr), typeof(PRPayment.payBatchNbr), typeof(PRPayment.paymentBatchNbr), typeof(PRPayment.extRefNbr),
			typeof(PRPayment.status), typeof(PRPayment.hold), typeof(PRPayment.released), typeof(PRPayment.voided), typeof(PRPayment.closed), typeof(PRPayment.liabilityPartiallyPaid), typeof(PRPayment.paid), typeof(PRPayment.calculated),
			typeof(PRPayment.finPeriodID), typeof(PRPayment.startDate), typeof(PRPayment.endDate), typeof(PRPayment.transactionDate), typeof(PRPayment.docDesc),
			typeof(PRPayment.origDocType), typeof(PRPayment.origRefNbr), typeof(PRPayment.caTranID), typeof(PRPayment.noteID))]
		public SelectFrom<PRPayment>
			.Where<PRPayment.docType.IsEqual<PRPayment.docType.AsOptional>>.View Document;

		[PXCopyPasteHiddenView]
		public PXSelect<PRPayment,
				Where<PRPayment.docType,
					Equal<Optional<PRPayment.docType>>,
				And<PRPayment.refNbr,
					Equal<Current<PRPayment.refNbr>>>>> CurrentDocument;
		[PXCopyPasteHiddenView]
		public SelectFrom<PRRecordOfEmployment>
			.Where<PRRecordOfEmployment.origDocType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRRecordOfEmployment.origRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View RecordsOfEmployment;
		[PXCopyPasteHiddenFields(typeof(PREarningDetail.batchNbr), typeof(PRPayment.employeeID))]
		public PXSelect<PREarningDetail,
				Where<PREarningDetail.employeeID,
					Equal<Current<PRPayment.employeeID>>,
				And<PREarningDetail.paymentDocType,
					Equal<Current<PRPayment.docType>>,
				And<PREarningDetail.paymentRefNbr,
					Equal<Current<PRPayment.refNbr>>>>>,
				OrderBy<Asc<PREarningDetail.date, Asc<PREarningDetail.isFringeRateEarning, Asc<PREarningDetail.sortingRecordID, Asc<PREarningDetail.rate>>>>>> Earnings;
		[PXCopyPasteHiddenView]
		public SelectFrom<PMTimeActivityExt>
			.InnerJoin<PREmployee>
				.On<PMTimeActivity.ownerID.IsEqual<PREmployee.defContactID>>
			.InnerJoin<GL.Branch>
				.On<PREmployee.parentBAccountID.IsEqual<GL.Branch.bAccountID>>
			.LeftJoin<PREarningDetail>
				.On<PREarningDetail.sourceType.IsEqual<EarningDetailSourceType.timeActivity>
					.And<PMTimeActivity.noteID.IsEqual<PREarningDetail.sourceNoteID>>>
			.LeftJoin<PRBatch>
				.On<PREarningDetail.FK.PayrollBatch
					.And<PRBatch.status.IsEqual<BatchStatus.hold>
						.Or<PRBatch.status.IsEqual<BatchStatus.balanced>>>>
			.LeftJoin<PRPayment>
				.On<PREarningDetail.FK.Payment
					.And<PRPayment.voided.IsNotEqual<True>>
					.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>
			.Where<PREmployee.bAccountID.IsEqual<PRPayment.employeeID.FromCurrent>
				.And<PMTimeActivity.trackTime.IsEqual<True>>
				.And<PMTimeActivity.isCorrected.IsEqual<False>>
				.And<Where<TimeZoneAdjustedDate<PMTimeActivityExt.date, PMTimeActivityExt.reportedInTimeZoneID>,
					Between<PRPayment.startDate.FromCurrent, PRPayment.endDate.FromCurrent>>>>
			.OrderBy<PREarningDetail.sourceNoteID.Asc, PMTimeActivity.date.Asc>.View TimeActivities;
		[PXCopyPasteHiddenView]
		public PXSelect<PRPaymentEarning,
				Where<PRPaymentEarning.docType,
					Equal<Current<PRPayment.docType>>,
				And<PRPaymentEarning.refNbr,
					Equal<Current<PRPayment.refNbr>>>>> SummaryEarnings;
		public SelectFrom<PRPaymentDeduct>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRPaymentDeduct.codeID>>
			.Where<PRPaymentDeduct.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentDeduct.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View Deductions;
		[PXCopyPasteHiddenView]
		public PXSelect<PRPaymentTax,
				Where<PRPaymentTax.docType,
					Equal<Current<PRPayment.docType>>,
				And<PRPaymentTax.refNbr,
					Equal<Current<PRPayment.refNbr>>>>> Taxes;
		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentTaxSplit>
			.Where<PRPaymentTaxSplit.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentTaxSplit.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>
				.And<PRPaymentTaxSplit.taxID.IsEqual<PRPaymentTax.taxID.FromCurrent>>>
			.OrderBy<PRPaymentTaxSplit.taxID.Asc>.View TaxSplits;
		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentTaxSplit>
			.Where<PRPaymentTaxSplit.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentTaxSplit.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View AllTaxSplits;
		
		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentTaxApplicableAmounts>
			.Where<PRPaymentTaxApplicableAmounts.FK.Payment.SameAsCurrent>.View PaymentTaxApplicableAmounts;

		public SelectFrom<PRPaymentOvertimeRule>.
			InnerJoin<PROvertimeRule>.
				On<PRPaymentOvertimeRule.overtimeRuleID.IsEqual<PROvertimeRule.overtimeRuleID>>.
			Where<PRPaymentOvertimeRule.paymentDocType.IsEqual<PRPayment.docType.FromCurrent>.
				And<PRPaymentOvertimeRule.paymentRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View PaymentOvertimeRules;
		[PXCopyPasteHiddenView]
		public SelectFrom<PRBatchOvertimeRule>.
			Where<PRBatchOvertimeRule.batchNbr.IsEqual<PRPayment.payBatchNbr.FromCurrent>>.View BatchOvertimeRules;
		public PXFilter<ExistingPayment> ExistingPayment;
		public PXFilter<ExistingPayrollBatch> ExistingPayrollBatch;
		public PXFilter<TaxUpdateHelpers.UpdateTaxesWarning> UpdateTaxesPopupView;
		public PXFilter<ImportTimeActivitiesFilter> ImportTimeActivitiesFilter;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRYtdEarnings>
			.InnerJoin<EPEarningType>.On<EPEarningType.typeCD.IsEqual<PRYtdEarnings.typeCD>>
			.InnerJoin<PRLocation>.On<PRLocation.locationID.IsEqual<PRYtdEarnings.locationID>>
			.InnerJoin<Address>.On<Address.addressID.IsEqual<PRLocation.addressID>>
			.Where<PRYtdEarnings.employeeID.IsEqual<PRPayment­.employeeID.FromCurrent>
				.And<PRYtdEarnings.year.IsEqual<P.AsString>>
				.And<Address.countryID.IsEqual<PRPayment.countryID.FromCurrent>>>.View EmployeeYTDEarnings;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRYtdDeductions>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRYtdDeductions.codeID>>
			.Where<PRYtdDeductions.employeeID.IsEqual<PRPayment­.employeeID.FromCurrent>
				.And<PRYtdDeductions.year.IsEqual<P.AsString>>
				.And<PRDeductCode.countryID.IsEqual<PRPayment.countryID.FromCurrent>>>.View EmployeeYTDDeductions;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRYtdTaxes>
			.InnerJoin<PRTaxCode>.On<PRTaxCode.taxID.IsEqual<PRYtdTaxes.taxID>>
			.Where<PRYtdTaxes.employeeID.IsEqual<PRPayment­.employeeID.FromCurrent>
				.And<PRYtdTaxes.year.IsEqual<P.AsString>>
				.And<PRTaxCode.countryID.IsEqual<PRPayment.countryID.FromCurrent>>>.View EmployeeYTDTaxes;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRDeductionDetail>.
			Where<PRDeductionDetail.employeeID.IsEqual<PRPayment.employeeID.FromCurrent>.
				And<PRDeductionDetail.paymentDocType.IsEqual<PRPayment.docType.FromCurrent>.
				And<PRDeductionDetail.paymentRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>>.
			OrderBy<PRDeductionDetail.codeID.Asc>.View DeductionDetails;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRBenefitDetail>.
			Where<PRBenefitDetail.employeeID.IsEqual<PRPayment.employeeID.FromCurrent>.
				And<PRBenefitDetail.paymentDocType.IsEqual<PRPayment.docType.FromCurrent>.
				And<PRBenefitDetail.paymentRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>>.
			OrderBy<PRBenefitDetail.codeID.Asc>.View BenefitDetails;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRTaxDetail>.
			Where<PRTaxDetail.employeeID.IsEqual<PRPayment.employeeID.FromCurrent>.
				And<PRTaxDetail.paymentDocType.IsEqual<PRPayment.docType.FromCurrent>.
				And<PRTaxDetail.paymentRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>>.
			OrderBy<PRTaxDetail.taxID.Asc>.View TaxDetails;

		[PXCopyPasteHiddenView]
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<PRPayment.curyInfoID>>>> CurrencyInfo;

		public SelectFrom<PRPaymentPTOBank>
			.Where<PRPaymentPTOBank.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentPTOBank.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View PaymentPTOBanks;

		[PXCopyPasteHiddenView]
		public SelectFrom<PaymentMethodAccount>
			.Where<PaymentMethodAccount.paymentMethodID.IsEqual<PRPayment.paymentMethodID.FromCurrent>
				.And<PaymentMethodAccount.cashAccountID.IsEqual<PRPayment.cashAccountID.FromCurrent>>>.View PaymentMethodAccount;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRDeductionAndBenefitUnionPackage>
			.InnerJoin<PMUnion>.On<PMUnion.unionID.IsEqual<PRDeductionAndBenefitUnionPackage.unionID>>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRDeductionAndBenefitUnionPackage.deductionAndBenefitCodeID>>
			.Where<PRDeductionAndBenefitUnionPackage.unionID.IsEqual<P.AsString>
				.And<PRDeductionAndBenefitUnionPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>
				.And<PMUnion.isActive.IsEqual<True>>
				.And<PRDeductCode.isActive.IsEqual<True>>
				.And<PRDeductCode.countryID.IsEqual<PRPayment.countryID.FromCurrent>>
				.And<PRDeductionAndBenefitUnionPackage.laborItemID.IsEqual<P.AsInt>
					.Or<PRDeductionAndBenefitUnionPackage.laborItemID.IsNull
						.And<P.AsInt.IsNull
							.Or<P.AsInt.IsNotInSubselect<SearchFor<PRDeductionAndBenefitUnionPackage.laborItemID>
								.Where<PRDeductionAndBenefitUnionPackage.unionID.IsEqual<P.AsString>
									.And<PRDeductionAndBenefitUnionPackage.laborItemID.IsNotNull>
									.And<PRDeductionAndBenefitUnionPackage.deductionAndBenefitCodeID.IsEqual<PRDeductCode.codeID>>
									.And<PRDeductionAndBenefitUnionPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>>>>>>>>
			.OrderBy<PRDeductionAndBenefitUnionPackage.deductionAndBenefitCodeID.Asc, PRDeductionAndBenefitUnionPackage.effectiveDate.Desc>.View EarningUnionDeductions;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRDeductionAndBenefitProjectPackage>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRDeductionAndBenefitProjectPackage.deductionAndBenefitCodeID>>
			.Where<PRDeductionAndBenefitProjectPackage.projectID.IsEqual<P.AsInt>
				.And<PRDeductionAndBenefitProjectPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>
				.And<PRDeductCode.isActive.IsEqual<True>>
				.And<PRDeductCode.countryID.IsEqual<BQLLocationConstants.CountryUS>>
				.And<PRDeductionAndBenefitProjectPackage.laborItemID.IsEqual<P.AsInt>
					.Or<PRDeductionAndBenefitProjectPackage.laborItemID.IsNull
						.And<P.AsInt.IsNull
							.Or<P.AsInt.IsNotInSubselect<SearchFor<PRDeductionAndBenefitProjectPackage.laborItemID>
								.Where<PRDeductionAndBenefitProjectPackage.projectID.IsEqual<P.AsInt>
									.And<PRDeductionAndBenefitProjectPackage.laborItemID.IsNotNull>
									.And<PRDeductionAndBenefitProjectPackage.deductionAndBenefitCodeID.IsEqual<PRDeductCode.codeID>>
									.And<PRDeductionAndBenefitProjectPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>>>>>>>>
			.OrderBy<PRDeductionAndBenefitProjectPackage.deductionAndBenefitCodeID.Asc, PRDeductionAndBenefitProjectPackage.effectiveDate.Desc>.View EarningProjectDeductions;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRDirectDepositSplit>
			.Where<PRDirectDepositSplit.docType.IsEqual<PRPayment.docType.AsOptional>
				.And<PRDirectDepositSplit.refNbr.IsEqual<PRPayment.refNbr.AsOptional>>>.View DirectDepositSplits;

		[PXCopyPasteHiddenView]
		public SelectFrom<PREmployee>.Where<PREmployee.bAccountID.IsEqual<PRPayment.employeeID.FromCurrent>>.View CurrentEmployee;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentWCPremium>
			.InnerJoin<PMWorkCode>.On<PMWorkCode.workCodeID.IsEqual<PRPaymentWCPremium.workCodeID>>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRPaymentWCPremium.deductCodeID>>
			.Where<PRPaymentWCPremium.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>
				.And<PRPaymentWCPremium.docType.IsEqual<PRPayment.docType.FromCurrent>>>.View WCPremiums;

		[PXCopyPasteHiddenView]
		public SelectFrom<PaymentMethod>.Where<PaymentMethod.paymentMethodID.IsEqual<PRPayment.paymentMethodID.AsOptional>>.View PaymentMethod;

		[PXCopyPasteHiddenView]
		public DirectDepositBatchAndDetailsSelect.View DirectDepositBatchAndDetails;
		public class DirectDepositBatchAndDetailsSelect : SelectFrom<CABatchDetail>
			.InnerJoin<PRCABatch>
				.On<PRCABatch.batchNbr.IsEqual<CABatchDetail.batchNbr>>
			.Where<CABatchDetail.origDocType.IsEqual<PRPayment.docType.AsOptional>
			.And<CABatchDetail.origRefNbr.IsEqual<PRPayment.refNbr.AsOptional>>
			.And<CABatchDetail.origModule.IsEqual<BatchModule.modulePR>>>
		{ }

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentProjectPackageDeduct>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRPaymentProjectPackageDeduct.deductCodeID>>
			.LeftJoin<PRDeductionAndBenefitProjectPackage>.On<PRDeductionAndBenefitProjectPackage.deductionAndBenefitCodeID.IsEqual<PRPaymentProjectPackageDeduct.deductCodeID>
				.And<PRDeductionAndBenefitProjectPackage.projectID.IsEqual<PRPaymentProjectPackageDeduct.projectID>>
				.And<PRDeductionAndBenefitProjectPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>
				.And<PRDeductionAndBenefitProjectPackage.laborItemID.IsEqual<PRPaymentProjectPackageDeduct.laborItemID>
					.Or<PRDeductionAndBenefitProjectPackage.laborItemID.IsNull
						.And<PRPaymentProjectPackageDeduct.laborItemID.IsNull>>>>
			.Where<PRPaymentProjectPackageDeduct.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>
				.And<PRPaymentProjectPackageDeduct.docType.IsEqual<PRPayment.docType.FromCurrent>>>.View ProjectPackageDeductions;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentUnionPackageDeduct>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRPaymentUnionPackageDeduct.deductCodeID>>
			.LeftJoin<PRDeductionAndBenefitUnionPackage>.On<PRDeductionAndBenefitUnionPackage.deductionAndBenefitCodeID.IsEqual<PRPaymentUnionPackageDeduct.deductCodeID>
				.And<PRDeductionAndBenefitUnionPackage.unionID.IsEqual<PRPaymentUnionPackageDeduct.unionID>>
				.And<PRDeductionAndBenefitUnionPackage.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>
				.And<PRDeductionAndBenefitUnionPackage.laborItemID.IsEqual<PRPaymentUnionPackageDeduct.laborItemID>
					.Or<PRDeductionAndBenefitUnionPackage.laborItemID.IsNull
						.And<PRPaymentUnionPackageDeduct.laborItemID.IsNull>>>>
			.Where<PRPaymentUnionPackageDeduct.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>
				.And<PRPaymentUnionPackageDeduct.docType.IsEqual<PRPayment.docType.FromCurrent>>>.View UnionPackageDeductions;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentFringeBenefit>
			.Where<PRPaymentFringeBenefit.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentFringeBenefit.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>
			.OrderBy<PRPaymentFringeBenefit.projectID.Asc, PRPaymentFringeBenefit.laborItemID.Asc, PRPaymentFringeBenefit.projectTaskID.Asc>.View PaymentFringeBenefits;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentFringeBenefitDecreasingRate>
			.Where<PRPaymentFringeBenefitDecreasingRate.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentFringeBenefitDecreasingRate.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>
				.And<PRPaymentFringeBenefitDecreasingRate.projectID.IsEqual<PRPaymentFringeBenefit.projectID.FromCurrent>>
				.And<PRPaymentFringeBenefitDecreasingRate.laborItemID.IsEqual<PRPaymentFringeBenefit.laborItemID.FromCurrent>>
				.And<PRPaymentFringeBenefitDecreasingRate.projectTaskID.IsEqual<PRPaymentFringeBenefit.projectTaskID.FromCurrent>
					.Or<PRPaymentFringeBenefitDecreasingRate.projectTaskID.IsNull
						.And<PRPaymentFringeBenefit.projectTaskID.FromCurrent.IsNull>>>>.View PaymentFringeBenefitsDecreasingRate;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentFringeBenefitDecreasingRate>
			.Where<PRPaymentFringeBenefitDecreasingRate.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentFringeBenefitDecreasingRate.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View AllPaymentFringeBenefitsDecreasingRate;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentFringeEarningDecreasingRate>
			.Where<PRPaymentFringeEarningDecreasingRate.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentFringeEarningDecreasingRate.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>
				.And<PRPaymentFringeEarningDecreasingRate.projectID.IsEqual<PRPaymentFringeBenefit.projectID.FromCurrent>>
				.And<PRPaymentFringeEarningDecreasingRate.laborItemID.IsEqual<PRPaymentFringeBenefit.laborItemID.FromCurrent>>
				.And<PRPaymentFringeEarningDecreasingRate.projectTaskID.IsEqual<PRPaymentFringeBenefit.projectTaskID.FromCurrent>
					.Or<PRPaymentFringeEarningDecreasingRate.projectTaskID.IsNull
						.And<PRPaymentFringeBenefit.projectTaskID.FromCurrent.IsNull>>>>.View PaymentFringeEarningsDecreasingRate;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRPaymentFringeEarningDecreasingRate>
			.Where<PRPaymentFringeEarningDecreasingRate.docType.IsEqual<PRPayment.docType.FromCurrent>
				.And<PRPaymentFringeEarningDecreasingRate.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>.View AllPaymentFringeEarningsDecreasingRate;

		[PXCopyPasteHiddenView]
		public SelectFrom<PRTaxUpdateHistory>.View UpdateHistory;

		public SelectFrom<PRPaymentPTOBank>
			.InnerJoin<PRPTOBank>.On<PRPaymentPTOBank.FK.PTOBank>
			.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PRPayment.employeeID.FromCurrent>>
			.LeftJoin<PREmployeePTOBank>.On<PREmployeePTOBank.bankID.IsEqual<PRPaymentPTOBank.bankID>
				.And<PREmployeePTOBank.bAccountID.IsEqual<PREmployee.bAccountID>>>
			.LeftJoin<PREmployeeClassPTOBank>.On<PREmployeeClassPTOBank.bankID.IsEqual<PRPaymentPTOBank.bankID>
				.And<PREmployeeClassPTOBank.employeeClassID.IsEqual<PREmployee.employeeClassID>>>
			.Where<PRPaymentPTOBank.FK.Payment.SameAsCurrent
				.And<PRPaymentPTOBank.isActive.IsEqual<True>>
				.And<PRPTOBank.createFinancialTransaction.IsEqual<True>>>.View FinancialPTOBanks;
		#endregion

		private void UpdatePayrollBatch(string payBatchNumber, int? employeeID)
		{
			PRPayBatchEntry payBatchEntryGraph = CreateInstance<PRPayBatchEntry>();
			payBatchEntryGraph.UpdatePayrollBatch(payBatchNumber, employeeID, true);
		}

		public PRPayChecksAndAdjustments()
		{
			Action.AddMenuAction(Calculate);
			Action.AddMenuAction(ProcessPayment);
			Action.AddMenuAction(Release);
			Action.AddMenuAction(PrintPayStub);
			Action.AddMenuAction(VoidPayment);
			Action.AddMenuAction(CreateROE);

			Deductions.AllowDelete = false;

			DeductionDetails.Cache.AllowInsert = false;
			DeductionDetails.Cache.AllowDelete = false;

			SummaryEarnings.AllowInsert =
			SummaryEarnings.AllowUpdate =
			SummaryEarnings.AllowDelete =

			TaxSplits.AllowInsert =
			TaxSplits.AllowDelete = false;

			PaymentPTOBanks.AllowInsert = false;
			PaymentPTOBanks.AllowDelete = false;

			PaymentOvertimeRules.AllowInsert = false;
			PaymentOvertimeRules.AllowDelete = false;

			DirectDepositSplits.AllowInsert = false;
			DirectDepositSplits.AllowUpdate = false;
			DirectDepositSplits.AllowDelete = false;

			PaymentFringeBenefits.AllowInsert = false;
			PaymentFringeBenefits.AllowUpdate = false;
			PaymentFringeBenefits.AllowDelete = false;
			PaymentFringeBenefitsDecreasingRate.AllowInsert = false;
			PaymentFringeBenefitsDecreasingRate.AllowUpdate = false;
			PaymentFringeBenefitsDecreasingRate.AllowDelete = false;
			PaymentFringeEarningsDecreasingRate.AllowInsert = false;
			PaymentFringeEarningsDecreasingRate.AllowUpdate = false;
			PaymentFringeEarningsDecreasingRate.AllowDelete = false;

			ImportTimeActivities.SetEnabled(false);
			TimeActivities.AllowInsert = false;
			TimeActivities.AllowDelete = false;
			TimeActivities.Cache.Adjust<PXUIFieldAttribute>().
				ForAllFields(field => field.Enabled = false).
				For<PMTimeActivity.selected>(field => field.Enabled = true);

			RecordsOfEmployment.AllowInsert = false;
			RecordsOfEmployment.AllowUpdate = false;
			RecordsOfEmployment.AllowDelete = false;
		}

		public override void CopyPasteGetScript(bool isImportSimple, List<PX.Api.Models.Command> script, List<PX.Api.Models.Container> containers)
		{
			base.CopyPasteGetScript(isImportSimple, script, containers);

			SetScriptCommit(script.Where(command => command.ObjectName == nameof(Earnings)).ToArray());
			SetScriptCommit(script.Where(command => command.ObjectName == nameof(PaymentOvertimeRules)).ToArray());
			SetScriptCommit(script.Where(command => command.ObjectName == nameof(PaymentPTOBanks)).ToArray());
			SetScriptCommit(script.Where(command => command.ObjectName == nameof(Deductions)).ToArray());
		}

		private void SetScriptCommit(PX.Api.Models.Command[] script)
		{
			for (int i = 0; i < script.Length; i++)
			{
				script[i].Commit = i == script.Length - 1;
			}
		}

		#region Data View Delegates
		protected virtual IEnumerable document()
		{
			if (IsCopyPasteContext && Document.Current.DocType == PayrollType.VoidCheck)
			{
				throw new PXException(Messages.CopyPasteIsNotAvailableForVoidPaychecks);
			}

			return null;
		}

		protected virtual IEnumerable earnings()
		{
			PXView query = new PXView(this, false, Earnings.View.BqlSelect);
			Dictionary<int?, PREarningDetail> earningDetails = new Dictionary<int?, PREarningDetail>();
			if (!IsCopyPasteContext)
			{
				earningDetails = query.SelectMulti().ToDictionary(k => ((PREarningDetail)k).RecordID, v => (PREarningDetail)v);
			}
			else
			{
				List<PREarningDetail> overtimeEarningDetails = new List<PREarningDetail>();
				List<PREarningDetail> ptoSplitEarningDetails = new List<PREarningDetail>();
				foreach (PREarningDetail earningDetail in query.SelectMulti())
				{
					if (earningDetail.BaseOvertimeRecordID == null && earningDetail.BasePTORecordID == null)
					{
						PREarningDetail copiedEarningDetail = PXCache<PREarningDetail>.CreateCopy(earningDetail);
						earningDetails[earningDetail.RecordID] = copiedEarningDetail;
					}
					else if (earningDetail.BaseOvertimeRecordID != null)
					{
						overtimeEarningDetails.Add(earningDetail);
					}
					else
					{
						ptoSplitEarningDetails.Add(earningDetail);
					}
				}

				foreach (PREarningDetail overtimeEarningDetail in overtimeEarningDetails)
				{
					if (earningDetails.TryGetValue(overtimeEarningDetail.BaseOvertimeRecordID, out PREarningDetail regularEarningDetail))
					{
						regularEarningDetail.Hours += overtimeEarningDetail.Hours;
					}
				}

				foreach (PREarningDetail ptoSplitEarningDetail in ptoSplitEarningDetails)
				{
					if (earningDetails.TryGetValue(ptoSplitEarningDetail.BasePTORecordID, out PREarningDetail regularEarningDetail))
					{
						regularEarningDetail.Hours += ptoSplitEarningDetail.Hours;
					}
				}
			}

			Dictionary<string, PXResult<PRPaymentPTOBank, PRPTOBank, PREmployee, PREmployeePTOBank, PREmployeeClassPTOBank>> financialPTOBanks = null;
			earningDetails.Values.ForEach(earningDetail => SetEarningFinancialPTOFlags(earningDetail, ref financialPTOBanks));
			return earningDetails.Values.ToArray();
		}

		protected virtual IEnumerable timeActivities()
		{
			IEnumerable result = ImportTimeActivitiesHelper.TimeActivitiesDelegate(this, TimeActivities.View, Earnings.Cache, ImportTimeActivitiesFilter.Current, out bool selectedTimeActivitiesExist);

			AddSelectedTimeActivities.SetEnabled(selectedTimeActivitiesExist);
			AddSelectedTimeActivitiesAndClose.SetEnabled(selectedTimeActivitiesExist);

			return result;
		}

		protected IEnumerable deductions()
		{
			PXView viewSelect = new PXView(this, true, Deductions.View.BqlSelect);
			HashSet<(int?, string)> paymentDeductIDs = new HashSet<(int?, string)>();
			List<PRPaymentDeduct> queryResults = new List<PRPaymentDeduct>();

			foreach (PRPaymentDeduct record in Deductions.Cache.Cached)
			{
				if (record.CodeID != null &&
					!paymentDeductIDs.Contains((record.CodeID, record.Source)) &&
					(Deductions.Cache.GetStatus(record) == PXEntryStatus.Updated || Deductions.Cache.GetStatus(record) == PXEntryStatus.Inserted))
				{
					paymentDeductIDs.Add((record.CodeID, record.Source));
					queryResults.Add(record);
				}
			}

			foreach (PRPaymentDeduct paymentDeduct in viewSelect.SelectMulti()
				.Select(x => (PXResult<PRPaymentDeduct, PRDeductCode>)x)
				.Where(x => !paymentDeductIDs.Contains((((PRPaymentDeduct)x).CodeID, ((PRPaymentDeduct)x).Source))))
			{
				var record = Deductions.Cache.Locate(paymentDeduct) ?? paymentDeduct;
				if (Deductions.Cache.GetStatus(record) != PXEntryStatus.Deleted)
				{
					paymentDeductIDs.Add((paymentDeduct.CodeID, paymentDeduct.Source));
					queryResults.Add(paymentDeduct);
				}
			}

			IEnumerable<PRDeductCode> deductCodes = SelectFrom<PRDeductCode>.View.Select(this).FirstTableItems;
			IEnumerable<PRDeductCode> activeDeductCodes = deductCodes.Where(x => x.IsActive == true);
			PRPayment currentPayment = CurrentDocument.Current;
			foreach (PRPaymentDeduct paymentDeduct in queryResults)
			{
				bool deductionNotActive = paymentDeduct.CodeID != null && !activeDeductCodes.Any(x => x.CodeID == paymentDeduct.CodeID);
				if (paymentDeduct.Source == PaymentDeductionSourceAttribute.WorkCode)
				{
					var stateWCEarningsQuery = new SelectFrom<PREarningDetail>
						.InnerJoin<PRLocation>.On<PRLocation.locationID.IsEqual<PREarningDetail.locationID>>
						.InnerJoin<Address>.On<Address.addressID.IsEqual<PRLocation.addressID>>
						.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<P.AsInt>>
						.InnerJoin<EPEarningType>.On<EPEarningType.typeCD.IsEqual<PREarningDetail.typeCD>>
						.Where<PREarningDetail.paymentRefNbr.IsEqual<PRPayment.refNbr.FromCurrent>
							.And<PREarningDetail.paymentDocType.IsEqual<PRPayment.docType.FromCurrent>>
							.And<PREarningDetail.workCodeID.IsNotNull>
							.And<PRDeductCode.countryID.IsEqual<PRPayment.countryID.FromCurrent>>
							.And<Address.state.IsEqual<PRDeductCode.state>>>.View(this);

					paymentDeduct.IsActive = (stateWCEarningsQuery.Select(paymentDeduct.CodeID).Any_() && !deductionNotActive) ||
						WCPremiums.Select().FirstTableItems.Any(x => x.DeductCodeID == paymentDeduct.CodeID);
					PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.isActive>(Deductions.Cache, paymentDeduct, false);
				}
				else if (currentPayment.Released == false && currentPayment.Paid == false && deductionNotActive)
				{
					if (currentPayment.DocType != PayrollType.Adjustment && currentPayment.DocType != PayrollType.VoidCheck)
					{
						bool oldIsActive = paymentDeduct.IsActive ?? false;
						paymentDeduct.IsActive = false;
						PXUIFieldAttribute.SetEnabled(Deductions.Cache, paymentDeduct, false);
						if (oldIsActive)
						{
							currentPayment.Calculated = false;
							CurrentDocument.Update(currentPayment);
						}
					}
				}
				else
				{
					PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.isActive>(Deductions.Cache, paymentDeduct, true);
				}

				if (currentPayment.Released == false && currentPayment.Paid == false && deductionNotActive)
				{
					Deductions.Cache.RaiseExceptionHandling<PREmployeeDeduct.codeID>(
						paymentDeduct,
						paymentDeduct.CodeID,
						new PXSetPropertyException(Messages.DeductCodeInactive, PXErrorLevel.Warning));
				}

				PRDeductCode deductCode = deductCodes.Where(x => x.CodeID == paymentDeduct.CodeID).FirstOrDefault();
				if (deductCode != null)
				{
					yield return new PXResult<PRPaymentDeduct, PRDeductCode>(paymentDeduct, deductCode);
				}
			}
		}

		protected IEnumerable paymentPTOBanks()
		{
			PXView viewSelect = new PXView(this, true, PaymentPTOBanks.View.BqlSelect);
			List<PRPaymentPTOBank> queryResults = new List<PRPaymentPTOBank>();

			//Retrieves inserted / updated records that aren't persisted yet
			foreach (PRPaymentPTOBank record in PaymentPTOBanks.Cache.Cached)
			{
				if (PaymentPTOBanks.Cache.GetStatus(record) == PXEntryStatus.Updated || PaymentPTOBanks.Cache.GetStatus(record) == PXEntryStatus.Inserted)
				{
					queryResults.Add(record);
				}
			}

			//Retrieve persisted banks that were not already updated/inserted
			foreach (PRPaymentPTOBank bank in viewSelect.SelectMulti())
			{
				var record = PaymentPTOBanks.Cache.Locate(bank) ?? bank;
				if (PaymentPTOBanks.Cache.GetStatus(record) != PXEntryStatus.Deleted && !queryResults.Contains(record))
				{
					queryResults.Add(bank);
				}
			}

			if (CurrentDocument.Current?.DocType == PayrollType.Adjustment)
			{
				foreach (object record in queryResults)
				{
					PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.isActive>(PaymentPTOBanks.Cache, record, true);
					yield return record;
				}
			}
			else
			{
				PXResultset<PREmployeePTOBank> employeeBanks = new SelectFrom<PREmployeePTOBank>
					.Where<PREmployeePTOBank.bAccountID.IsEqual<PRPayment.employeeID.FromCurrent>>.View(this).Select();

				foreach (PRPaymentPTOBank bank in queryResults)
				{
					PREmployeePTOBank employeeBank = employeeBanks.FirstOrDefault(x => ((PREmployeePTOBank)x).BankID == bank.BankID);
					PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.isActive>(PaymentPTOBanks.Cache, bank, employeeBank?.IsActive ?? false);
					yield return bank;
				}
			}
		}

		protected IEnumerable taxSplits()
		{
			if (CurrentDocument.Current.DocType == PayrollType.Adjustment && Taxes.Current != null)
			{
				List<object> existingSplits = new PXView(this, false, TaxSplits.View.BqlSelect).SelectMulti();

				if (CurrentDocument.Current.CountryID == LocationConstants.USCountryCode)
				{
					foreach (int wageType in PRTypeSelectorAttribute.GetAll<PRWage>(LocationConstants.USCountryCode).Where(x => x.HasDescription).Select(x => x.ID))
					{
						if (!existingSplits.Any(x => ((PRPaymentTaxSplit)x).WageType == wageType && ((PRPaymentTaxSplit)x).TaxID == Taxes.Current.TaxID))
						{
							TaxSplits.Update(new PRPaymentTaxSplit()
							{
								TaxID = Taxes.Current.TaxID,
								WageType = wageType
							});
						}
					}
				}
				else
				{
					if (!existingSplits.Any(x => ((PRPaymentTaxSplit)x).WageType == TaxSplitWageType.Others && ((PRPaymentTaxSplit)x).TaxID == Taxes.Current.TaxID))
					{
						TaxSplits.Update(new PRPaymentTaxSplit()
						{
							TaxID = Taxes.Current.TaxID,
							WageType = TaxSplitWageType.Others
						});
					}

					if (!existingSplits.Any(x => ((PRPaymentTaxSplit)x).WageType == TaxSplitWageType.Tips && ((PRPaymentTaxSplit)x).TaxID == Taxes.Current.TaxID))
					{
						TaxSplits.Update(new PRPaymentTaxSplit()
						{
							TaxID = Taxes.Current.TaxID,
							WageType = TaxSplitWageType.Tips
						});
					}
				}
			}

			return null;
		}

		public IEnumerable wCPremiums()
		{
			PXView bqlSelect = new PXView(this, false, WCPremiums.View.BqlSelect);

			foreach (object objResult in bqlSelect.SelectMulti())
			{
				PXResult<PRPaymentWCPremium, PMWorkCode, PRDeductCode> result = objResult as PXResult<PRPaymentWCPremium, PMWorkCode, PRDeductCode>;
				if (result != null)
				{
					PRPaymentWCPremium premium = (PRPaymentWCPremium)result;
					PRDeductCode deductCode = (PRDeductCode)result;

					if (CurrentDocument.Current.Released == false && CurrentDocument.Current.Paid == false &&
						premium.DeductCodeID != null && deductCode.IsActive != true)
					{
						WCPremiums.Cache.RaiseExceptionHandling<PRPaymentWCPremium.deductCodeID>(
							premium,
							premium.DeductCodeID,
							new PXSetPropertyException(Messages.DeductCodeInactive, PXErrorLevel.Warning));
					}

					yield return result;
				}
			}
		}

		public IEnumerable projectPackageDeductions()
		{
			PXView bqlSelect = new PXView(this, false, ProjectPackageDeductions.View.BqlSelect);

			foreach (IGrouping<int?, PXResult<PRPaymentProjectPackageDeduct, PRDeductCode, PRDeductionAndBenefitProjectPackage>> resultGroup in bqlSelect.SelectMulti()
				.Cast<PXResult<PRPaymentProjectPackageDeduct, PRDeductCode, PRDeductionAndBenefitProjectPackage>>()
				.GroupBy(x => ((PRPaymentProjectPackageDeduct)x).RecordID))
			{
				PRPaymentProjectPackageDeduct packageDeduct = resultGroup.First();
				PRDeductCode deductCode = resultGroup.First();
				PRDeductionAndBenefitProjectPackage package = resultGroup.OrderByDescending(x => ((PRDeductionAndBenefitProjectPackage)x).EffectiveDate).First();

				if (CurrentDocument.Current.Released == false && CurrentDocument.Current.Paid == false &&
					packageDeduct.DeductCodeID != null)
				{
					if (deductCode.IsActive != true)
					{
						ProjectPackageDeductions.Cache.RaiseExceptionHandling<PRPaymentProjectPackageDeduct.deductCodeID>(
							packageDeduct,
							packageDeduct.DeductCodeID,
							new PXSetPropertyException(Messages.DeductCodeInactive, PXErrorLevel.Warning));
					}

					if (packageDeduct.ProjectID != null && package.RecordID == null)
					{
						ProjectPackageDeductions.Cache.RaiseExceptionHandling<PRPaymentProjectPackageDeduct.projectID>(
							packageDeduct,
							packageDeduct.ProjectID,
							new PXSetPropertyException(Messages.CantFindProjectPackageDeduct, PXErrorLevel.RowWarning));
					}
				}

				yield return new PXResult<PRPaymentProjectPackageDeduct, PRDeductCode, PRDeductionAndBenefitProjectPackage>(packageDeduct, deductCode, package);
			}
		}

		public IEnumerable unionPackageDeductions()
		{
			PXView bqlSelect = new PXView(this, false, UnionPackageDeductions.View.BqlSelect);

			foreach (IGrouping<int?, PXResult<PRPaymentUnionPackageDeduct, PRDeductCode, PRDeductionAndBenefitUnionPackage>> resultGroup in bqlSelect.SelectMulti()
				.Cast<PXResult<PRPaymentUnionPackageDeduct, PRDeductCode, PRDeductionAndBenefitUnionPackage>>()
				.GroupBy(x => ((PRPaymentUnionPackageDeduct)x).RecordID))
			{
				PRPaymentUnionPackageDeduct packageDeduct = resultGroup.First();
				PRDeductCode deductCode = resultGroup.First();
				PRDeductionAndBenefitUnionPackage package = resultGroup.OrderByDescending(x => ((PRDeductionAndBenefitUnionPackage)x).EffectiveDate).First();

				if (CurrentDocument.Current.Released == false && CurrentDocument.Current.Paid == false &&
					packageDeduct.DeductCodeID != null)
				{
					if (deductCode.IsActive != true)
					{
						UnionPackageDeductions.Cache.RaiseExceptionHandling<PRPaymentUnionPackageDeduct.deductCodeID>(
							packageDeduct,
							packageDeduct.DeductCodeID,
							new PXSetPropertyException(Messages.DeductCodeInactive, PXErrorLevel.Warning));
					}

					if (packageDeduct.UnionID != null && package.RecordID == null)
					{
						UnionPackageDeductions.Cache.RaiseExceptionHandling<PRPaymentUnionPackageDeduct.unionID>(
							packageDeduct,
							packageDeduct.UnionID,
							new PXSetPropertyException(Messages.CantFindUnionPackageDeduct, PXErrorLevel.RowWarning));
					}
				}

				yield return new PXResult<PRPaymentUnionPackageDeduct, PRDeductCode, PRDeductionAndBenefitUnionPackage>(packageDeduct, deductCode, package);
			}
		}
		#endregion Data View Delegates

		#region CacheAttached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PaymentRegularAmount(nameof(Earnings))]
		protected virtual void _(Events.CacheAttached<PRPayment.regularAmount> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(PRPayment.employeeID))]
		protected virtual void PREarningDetail_EmployeeID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(PRPayment.docType))]
		protected virtual void PREarningDetail_PaymentDocType_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(PRPayment.refNbr))]
		protected virtual void PREarningDetail_PaymentRefNbr_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(PRPayment.payBatchNbr), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void PREarningDetail_BatchNbr_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[GL.Branch(
			typeof(Parent<PRPayment.branchID>),
			typeof(SearchFor<Branch.branchID>.Where<Branch.countryID.IsEqual<PRPayment.countryID.FromCurrent>>),
			IsDetail = false)]
		protected virtual void _(Events.CacheAttached<PREarningDetail.branchID> e) { }

		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Deduction Code")]
		[PRUniqueDeductionCodeSelector(typeof(PRDeductCode.codeID), SubstituteKey = typeof(PRDeductCode.codeCD), DescriptionField = typeof(PRDeductCode.description))]
		protected virtual void PRPaymentDeduct_CodeID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXUnboundDefaultAttribute))]
		[PXUnboundDefault(typeof(SelectFrom<PREmployee>
			.Where<PREmployee.bAccountID.IsEqual<PREmployeePTOBank.bAccountID.FromCurrent>>
			.SearchFor<PREmployee.employeeClassID>))]
		protected virtual void _(Events.CacheAttached<PREmployeePTOBank.employeeClassID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDBDecimalAttribute))]
		[PXDBDecimal]
		protected virtual void PREarningDetail_Hours_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDBDecimalAttribute))]
		[PXDBDecimal]
		protected virtual void PREarningDetail_Units_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDBDateAttribute))]
		[DateInPaymentPeriod(typeof(PRPayment), typeof(PRPayment.startDate), typeof(PRPayment.endDate), nameof(Earnings))]
		protected virtual void PREarningDetail_Date_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		[PXSelector(
			typeof(SelectFrom<PRLocation>
				.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PREarningDetail.employeeID.FromCurrent>>
				.SearchFor<PRLocation.locationID>),
			SubstituteKey = typeof(PRLocation.locationCD))]
		[PXFormula(typeof(Default<PREarningDetail.projectID>))]
		[PXRestrictor(
			typeof(Where<PRLocation.isActive.IsEqual<True>
						.Or<PRPayment.docType.FromCurrent.IsNotEqual<PayrollType.regular>>>), Messages.LocationIsInactive, typeof(PRLocation.locationID))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.locationID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), false)]
		protected virtual void _(Events.CacheAttached<PRPaymentTaxSplit.taxID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Deduction Calculation Method")]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXRemoveBaseAttribute(typeof(PXUIRequiredAttribute))]
		[DeductionCalcMethodDisplay]
		protected virtual void _(Events.CacheAttached<PRDeductCode.dedCalcType> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Contribution Calculation Method")]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXRemoveBaseAttribute(typeof(PXUIRequiredAttribute))]
		[BenefitCalcMethodDisplay]
		protected virtual void _(Events.CacheAttached<PRDeductCode.cntCalcType> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		[PXSelector(
			typeof(SearchFor<PRTaxCode.taxID>.Where<PRTaxCode.countryID.IsEqual<PRPaymentTax.paymentCountryID.FromCurrent>>),
			DescriptionField = typeof(PRTaxCode.description),
			SubstituteKey = typeof(PRTaxCode.taxCD))]
		protected virtual void _(Events.CacheAttached<PRPaymentTax.taxID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.ROERefNbr)]
		protected virtual void _(Events.CacheAttached<PRRecordOfEmployment.refNbr> e) { }
		#endregion

		#region Actions
		public PXAction<PRPayment> Action;
		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(MenuAutoOpen = true)]
		public virtual void action() { }

		public PXAction<PRPayment> Calculate;
		[PXUIField(DisplayName = Messages.Calculate, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable calculate(PXAdapter adapter)
		{
			List<PRPayment> list = adapter.Get<PRPayment>().ToList();
			if (list.Count == 0)
			{
				throw new PXException(Messages.Document_Status_Invalid);
			}

			Save.Press();
			CalculatePaymentList(list, false);
			return list;
		}

		public virtual void CalculatePaymentList(List<PRPayment> list, bool isMassProcess)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					PRPayChecksAndAdjustments paychecksGraph = CreateInstance<PRPayChecksAndAdjustments>();
					foreach (PRPayment pmt in list)
					{
						paychecksGraph.Document.Current = pmt;

						// Pull list of union and project deductions in case deductions packages were updated after creation of earning details
						paychecksGraph.Earnings.Select().FirstTableItems.ForEach(x =>
						{
							paychecksGraph.AddUnionDeductions(x, false);
							paychecksGraph.AddProjectDeductions(x, false);
						});

						paychecksGraph.Taxes.Select().ForEach(x => paychecksGraph.Taxes.Delete(x));
						paychecksGraph.PaymentTaxApplicableAmounts.Select().ForEach(x => paychecksGraph.PaymentTaxApplicableAmounts.Delete(x));
						paychecksGraph.DeductionDetails.Select().FirstTableItems.ForEach(x => paychecksGraph.DeductionDetails.Delete(x));
						paychecksGraph.BenefitDetails.Select().FirstTableItems.ForEach(x => paychecksGraph.BenefitDetails.Delete(x));
						paychecksGraph.WCPremiums.Select().FirstTableItems.ForEach(x => paychecksGraph.WCPremiums.Delete(x));
						paychecksGraph.ProjectPackageDeductions.Select().FirstTableItems.ForEach(x => paychecksGraph.ProjectPackageDeductions.Delete(x));
						paychecksGraph.UnionPackageDeductions.Select().FirstTableItems.ForEach(x => paychecksGraph.UnionPackageDeductions.Delete(x));
						paychecksGraph.PaymentFringeBenefits.Select().FirstTableItems.ForEach(x => paychecksGraph.PaymentFringeBenefits.Delete(x));
						paychecksGraph.AllPaymentFringeBenefitsDecreasingRate.Select().FirstTableItems.ForEach(x => paychecksGraph.AllPaymentFringeBenefitsDecreasingRate.Delete(x));
						paychecksGraph.AllPaymentFringeEarningsDecreasingRate.Select().FirstTableItems.ForEach(x => paychecksGraph.AllPaymentFringeEarningsDecreasingRate.Delete(x));

						paychecksGraph.Document.Current = paychecksGraph.Document.Update(pmt);
						paychecksGraph.Actions.PressSave();
					}

					PRCalculationEngine.Run(list, isMassProcess);

					ts.Complete();
				}
			});
		}

		public PXAction<PRPayment> ProcessPayment;
		[PXUIField(DisplayName = "Process Payment", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual void processPayment()
		{
			PRPayment payment = Document.Current;
			var graph = PXGraph.CreateInstance<PRPrintChecks>();
			PrintChecksFilter filterCopy = PXCache<PrintChecksFilter>.CreateCopy(graph.Filter.Current);
			filterCopy.CashAccountID = payment.CashAccountID;
			filterCopy.PaymentMethodID = payment.PaymentMethodID;
			graph.Filter.Cache.Update(filterCopy);

			payment.Selected = true;
			graph.PaymentList.Cache.Update(payment);
			graph.PaymentList.Cache.IsDirty = false;
			throw new PXRedirectRequiredException(graph, "Preview");
		}


		public PXAction<PRPayment> PrintPayStub;
		[PXUIField(DisplayName = Messages.PrintPayStub, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable printPayStub(PXAdapter adapter)
		{
			if (Document.Current != null)
			{
				var parameters = new Dictionary<string, string>();
				parameters["DocType"] = Document.Current.DocType;
				parameters["RefNbr"] = Document.Current.RefNbr;

				throw new PXReportRequiredException(parameters, "PR641000", PXBaseRedirectException.WindowMode.New, Messages.PayCheckReport);
			}

			return adapter.Get();
		}

		public PXAction<PRPayment> VoidPayment;
		[PXUIField(DisplayName = Messages.Void, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public virtual IEnumerable voidPayment(PXAdapter adapter)
		{
			List<PRPayment> paymentList = new List<PRPayment>();

			if (Document.Current != null &&
				Document.Current.Released == true &&
				Document.Current.Voided == false &&
				Document.Current.DocType != PayrollType.VoidCheck)
			{
				PRPayment voidcheck = CurrentDocument.Select(PayrollType.VoidCheck);
				if (voidcheck != null)
				{
					paymentList.Add(voidcheck);
					return paymentList;
				}

				PRPayment doc = PXCache<PRPayment>.CreateCopy(Document.Current);
				try
				{
					IsVoidCheckInProgress = true;
					VoidCheckProc(doc);
				}
				catch (PXSetPropertyException)
				{
					Clear();
					Document.Current = doc;
					throw;
				}
				finally
				{
					IsVoidCheckInProgress = false;
				}

				Actions.PressSave();
				paymentList.Add(Document.Current);
				return paymentList;
			}
			return Document.Select();
		}

		public PXAction<PRPayment> CopySelectedEarningDetailLine;
		[PXUIField(DisplayName = "Copy Selected Entry", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable copySelectedEarningDetailLine(PXAdapter adapter)
		{
			RegularAmountAttribute.EnforceEarningDetailUpdate<PRPayment.regularAmount>(Document.Cache, Document.Current, false);
			EarningDetailHelper.CopySelectedEarningDetailRecord(Earnings.Cache);
			RegularAmountAttribute.EnforceEarningDetailUpdate<PRPayment.regularAmount>(Document.Cache, Document.Current, true);
			return adapter.Get();
		}

		public PXAction<PRPayment> ImportTimeActivities;
		[PXUIField(DisplayName = "Import Time Activities", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable importTimeActivities(PXAdapter adapter)
		{
			PrepareTimeActivitiesPanel(false);

			TimeActivities.AskExt();

			TimeActivities.Cache.Clear();
			return adapter.Get();
		}

		protected virtual void PrepareTimeActivitiesPanel(bool retainFilter)
		{
			bool? showImportedActivities = ImportTimeActivitiesFilter.Current.ShowImportedActivities;

			TimeActivities.Cache.Clear();
			ImportTimeActivitiesFilter.Cache.Clear();

			if (retainFilter)
			{
				ImportTimeActivitiesFilter.Current.ShowImportedActivities = showImportedActivities;
			}
			else
			{
				TimeActivities.View.RequestFiltersReset();
			}

			EmploymentPeriods employmentPeriods = GetEmploymentPeriods(Document.Current.EmployeeID.Value, Document.Current.StartDate.Value, Document.Current.EndDate.Value);

			foreach (PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment> record in TimeActivities.Select())
			{
				bool allowTimeActivitySelection = AllowTimeActivitySelection(record, employmentPeriods, out string errorMessage);
				PMTimeActivityExt timeActivity = record;

				timeActivity.Selected = allowTimeActivitySelection;
				timeActivity.ErrorMessage = errorMessage;
				TimeActivities.Cache.Update(timeActivity);

				if (allowTimeActivitySelection)
				{
					ImportTimeActivitiesFilter.Current.SelectedTimeActivityExist = true;
				}
			}
		}

		protected virtual EmploymentPeriods GetEmploymentPeriods(int? currentEmployeeID, DateTime startDate, DateTime endDate)
		{
			return EmploymentPeriods.GetEmploymentPeriods(this, currentEmployeeID, startDate, endDate);
		}

		protected virtual bool AllowTimeActivitySelection(
			PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment> record,
			EmploymentPeriods employmentPeriods,
			out string errorMessage)
		{
			PMTimeActivityExt timeActivity = record;
			PREarningDetail earningDetail = record;
			PRPayment payment = record;
			PRBatch payrollBatch = record;
			errorMessage = null;

			if (!employmentPeriods.IsEmployedOnDate(timeActivity.Date.Value))
			{
				errorMessage = Messages.EmployeeWasNotEmployed;
				return false;
			}

			if (!ImportTimeActivitiesHelper.CheckTimeActivityForImport(timeActivity, out errorMessage))
			{
				return false;
			}

			if (earningDetail?.PaymentDocType == Document.Current.DocType &&
				earningDetail?.PaymentRefNbr == Document.Current.RefNbr &&
				!string.IsNullOrWhiteSpace(Document.Current.PaymentDocAndRef) ||
				Earnings.Cache.Cached.Cast<PREarningDetail>().FirstOrDefault(x =>
					x.SourceType == EarningDetailSourceType.TimeActivity &&
					x.SourceNoteID == timeActivity.NoteID &&
					Earnings.Cache.GetStatus(x) != PXEntryStatus.Deleted) != null)
			{
				errorMessage = Messages.TimeActivityAlreadyAddedToThisPaycheck;
				return false;
			}
			else if (!string.IsNullOrWhiteSpace(payment?.PaymentDocAndRef))
			{
				errorMessage = string.Format(Messages.TimeActivityAlreadyAddedToPaycheck, payment.PaymentDocAndRef);
				return false;
			}
			else if (!string.IsNullOrWhiteSpace(payrollBatch?.BatchNbr))
			{
				errorMessage = string.Format(Messages.TimeActivityAlreadyAddedToBatch, payrollBatch.BatchNbr);
				return false;
			}

			return true;
		}

		public PXAction<PRPayment> ToggleSelectedTimeActivities;
		[PXUIField(DisplayName = "Toggle Selected")]
		[PXProcessButton]
		public virtual IEnumerable toggleSelectedTimeActivities(PXAdapter adapter)
		{
			ImportTimeActivitiesHelper.ToggleSelectedTimeActivities(TimeActivities.Select(), TimeActivities.Cache, ImportTimeActivitiesFilter.Current);

			return adapter.Get();
		}

		public PXAction<PRPayment> AddSelectedTimeActivities;
		[PXUIField(DisplayName = "Add", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable addSelectedTimeActivities(PXAdapter adapter)
		{
			try
			{
				_IsTimeActivitiesImportInProgress = true;
				AddTimeActivities();
			}
			finally
			{
				_IsTimeActivitiesImportInProgress = false;
				PrepareTimeActivitiesPanel(true);
			}
			return adapter.Get();
		}


		public PXAction<PRPayment> AddSelectedTimeActivitiesAndClose;
		[PXUIField(DisplayName = "Add & Close", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable addSelectedTimeActivitiesAndClose(PXAdapter adapter)
		{
			try
			{
				_IsTimeActivitiesImportInProgress = true;
				AddTimeActivities();
			}
			finally
			{
				_IsTimeActivitiesImportInProgress = false;
			}
			return adapter.Get();
		}

		protected virtual void AddTimeActivities()
		{
			foreach (PMTimeActivityExt timeActivity in TimeActivities.Select().FirstTableItems.Where(item => item.Selected == true))
			{
				// ToDo AC-200348: Copy Shift Code to Earning Detail.

				if (timeActivity.TimeSpent == null || timeActivity.Date == null)
				{
					continue;
				}

				PREarningDetail earningDetail = Earnings.Insert();

				earningDetail.EmployeeID = Document.Current.EmployeeID;
				earningDetail.Date = timeActivity.ActivityDate.Value;
				earningDetail.TypeCD = timeActivity.EarningTypeID;
				earningDetail.Hours = Math.Round((decimal)timeActivity.TimeSpent / 60, 2);

				earningDetail.CostCodeID = timeActivity.CostCodeID;
				earningDetail.WorkCodeID = timeActivity.WorkCodeID;
				earningDetail.CertifiedJob = timeActivity.CertifiedJob ?? false;
				earningDetail.LabourItemID = timeActivity.LabourItemID;

				earningDetail.ProjectID = timeActivity.ProjectID;
				earningDetail.ProjectTaskID = timeActivity.ProjectTaskID;
				earningDetail.UnionID = timeActivity.UnionID;

				earningDetail.SourceType = EarningDetailSourceType.TimeActivity;
				earningDetail.SourceNoteID = timeActivity.NoteID;
				Earnings.Update(earningDetail);

				earningDetail.WorkCodeID = timeActivity.WorkCodeID;
				Earnings.Update(earningDetail);
			}
		}

		public PXAction<PRPayment> ViewTimeActivity;
		[PXUIField(DisplayName = "View Time Activity", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual void viewTimeActivity()
		{
			ImportTimeActivitiesHelper.ViewTimeActivity(this, Earnings.Current);
		}

		public PXAction<PRPayment> ViewOvertimeRules;
		[PXUIField(DisplayName = "Overtime Rules", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewOvertimeRules()
		{
			PaymentOvertimeRules.AskExt();
		}

		public PXAction<PRPayment> RevertOvertimeCalculation;
		[PXUIField(DisplayName = "Revert Overtime Calculations and Close", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable revertOvertimeCalculation(PXAdapter adapter)
		{
			RevertPaymentOvertimeCalculation(this, Document.Current, Earnings.View);

			return adapter.Get();
		}

		public PXAction<PRPayment> Release;
		[PXUIField(DisplayName = Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable release(PXAdapter adapter)
		{
			List<PRPayment> list = adapter.Get<PRPayment>().ToList();
			if (list.Count == 0)
			{
				throw new PXException(Messages.Document_Status_Invalid);
			}

			ReleasePaymentList(list, false);
			return list;
		}

		public virtual void ReleasePaymentList(List<PRPayment> list, bool isMassProcess)
		{
			//Verify records from DB can be released
			var noteIDs = list.Select(x => x.NoteID);
			var paymentsList = SelectFrom<PRPayment>
				.Where<PRPayment.noteID.IsIn<P.AsGuid>>.View.ReadOnly
				.Select(this, noteIDs).FirstTableItems;
			foreach (var payment in paymentsList)
			{
				//Remove released payments to not process them
				var index = list.FindIndex(x => x.NoteID == payment.NoteID);
				if (payment.Released == true)
				{
					list.RemoveAt(index);
					continue;
				}

				if (IsReleaseActionEnabled(payment, PRSetup.Current.UpdateGL ?? false) == false)
				{
					list.RemoveAt(index);
					var exception = new PXException(Messages.PaycheckCantBeReleasedVerifyStatus, payment.PaymentDocAndRef);
					if (isMassProcess)
					{
						PXProcessing.SetError(index, exception);
					}
					else
					{
						if (list.Count > 1)
						{
							throw new PXException(Messages.BulkProcessErrorFormat, payment?.PaymentDocAndRef, exception.Message);
						}
						else
						{
							throw exception;
						}
					}
				}
			}

			PXCache cache = Document.Cache;
			List<PRPayment> notReleasedPayments = new List<PRPayment>();
			foreach (PRPayment pmt in list.Where(x => x.Released != true))
			{
				try
				{
					Document.Current = pmt;
					Persist();
					PRValidatePaycheckTotals validationGraph = CreateInstance<PRValidatePaycheckTotals>();
					if (pmt.DocType != PayrollType.VoidCheck)
					{
					validationGraph.ValidateTotals(pmt, false);
					}

					if (pmt.LaborCostSplitType == null)
					{
						SetCostAssignmentType(pmt);
					}
					DefaultDescription(cache, pmt);
					cache.Update(pmt);
					notReleasedPayments.Add(pmt);
				}
				catch (Exception ex)
				{
					if (isMassProcess)
					{
						PXProcessing.SetError(list.IndexOf(pmt), ex);
					}
					else
					{
						if (list.Count > 1)
						{
							throw new PXException(Messages.BulkProcessErrorFormat, pmt?.PaymentDocAndRef, ex.Message);
						}
						else
						{
							throw;
						}
					}
				}
			}

			Save.Press();
			PXLongOperation.StartOperation(this, delegate()
			{
				PRDocumentProcess.ReleaseDoc(notReleasedPayments, isMassProcess);

				string[] paymentBatchNumbers = notReleasedPayments.Where(x => x.DocType != PayrollType.VoidCheck && !string.IsNullOrEmpty(x.PaymentBatchNbr))
					.Select(payment => payment.PaymentBatchNbr)
					.Distinct().ToArray();

				PRCABatchUpdate batchUpdateGraph = PXGraph.CreateInstance<PRCABatchUpdate>();
				foreach (string paymentBatchNumber in paymentBatchNumbers)
				{
					PRCABatch paymentBatch = SelectFrom<PRCABatch>.Where<PRCABatch.batchNbr.IsEqual<P.AsString>>.View.Select(batchUpdateGraph, paymentBatchNumber).TopFirst;
					if(paymentBatch == null)
					{
						continue;
					}

					batchUpdateGraph.Document.Current = paymentBatch;
					if (batchUpdateGraph.BatchPaymentsDetails.Select().FirstTableItems.Any(payment => payment.Released != true))
					{
						continue;
					}

					paymentBatch.Released = true;
					paymentBatch.DateSeqNbr = CABatchEntry.GetNextDateSeqNbr(batchUpdateGraph, paymentBatch);
					paymentBatch = batchUpdateGraph.Document.Update(paymentBatch);
					batchUpdateGraph.Persist();
				}
			});
		}

		public PXAction<PRPayment> CreateROE;
		[PXUIField(DisplayName = Messages.CreateROE, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable createROE(PXAdapter adapter)
		{
			PRPayment payment = Document.Current;

			if (payment != null &&
				payment.Released == true &&
				payment.Voided == false &&
				payment.DocType == PayrollType.Final)
			{
				PXLongOperation.StartOperation(this, delegate () {
					PRPayChecksAndAdjustments graph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
					graph.Document.Current = payment;

					InsertRecordOfEmployment(graph, payment);
				});
			}

			return adapter.Get();
		}

		public PXAction<PRPayment> ViewRecordsOfEmployment;
		[PXUIField(DisplayName = "View Records of Employment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		protected virtual void viewRecordsOfEmployment()
		{
			PRRecordOfEmployment recordOfEmployment = RecordsOfEmployment.Current;

			if (recordOfEmployment != null)
			{
				PRRecordOfEmploymentMaint graph = PXGraph.CreateInstance<PRRecordOfEmploymentMaint>();
				graph.Document.Current = recordOfEmployment;
				throw new PXRedirectRequiredException(graph, true, Messages.PRRecordOfEmployment);
			}
		}

		public PXAction<PRPayment> ViewDeductionDetails;
		[PXUIField(DisplayName = "Deduction Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewDeductionDetails()
		{
			DeductionDetails.AskExt();
		}

		public PXAction<PRPayment> ViewBenefitDetails;
		[PXUIField(DisplayName = "Benefit Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewBenefitDetails()
		{
			BenefitDetails.AskExt();
		}

		public PXAction<PRPayment> ViewTaxDetails;
		[PXUIField(DisplayName = "Tax Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewTaxDetails()
		{
			TaxDetails.AskExt();
		}

		public PXAction<PRPayment> ViewTaxableWageDetails;
		[PXUIField(DisplayName = "Taxable Wage Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewTaxableWageDetails()
		{
			PaymentTaxApplicableAmounts.AskExt();
		}

		public PXAction<PRPayment> ViewOriginalDocument;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		protected virtual IEnumerable viewOriginalDocument(PXAdapter adapter)
		{
			var document = Document.Search<PRPayment.refNbr>(Document.Current.OrigRefNbr, Document.Current.OrigDocType).FirstTableItems.FirstOrDefault();
			if (document != null)

			{
				PXRedirectHelper.TryRedirect(this, document, PXRedirectHelper.WindowMode.NewWindow);
			}

			return adapter.Get();
		}

		public PXAction<PRPayment> ViewPaymentBatch;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		protected virtual void viewPaymentBatch()
		{
			var batch = SelectFrom<PRCABatch>.Where<PRCABatch.batchNbr.IsEqual<PRPayment.paymentBatchNbr.FromCurrent>>.View.Select(this).TopFirst;
			if (batch != null)
			{
				var graph = PXGraph.CreateInstance<PRDirectDepositBatchEntry>();
				graph.Document.Current = batch;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
			}
		}

		public PXAction<PRPayment> ViewDirectDepositSplits;
		[PXUIField(DisplayName = "View Direct Deposit Splits", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual void viewDirectDepositSplits()
		{
			DirectDepositSplits.AskExt();
		}

		public PXAction<PRPayment> ViewExistingPayment;
		[PXUIField(DisplayName = "View Existing Paycheck", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewExistingPayment(PXAdapter adapter)
		{
			PRPayment existingPayment = SelectFrom<PRPayment>.
				Where<PRPayment.docType.IsEqual<P.AsString>.
					And<PRPayment.refNbr.IsEqual<P.AsString>>>.View.
					SelectSingleBound(this, null, ExistingPayment.Current.DocType, ExistingPayment.Current.RefNbr);

			Clear();
			if (existingPayment != null)
				PXRedirectHelper.TryRedirect(this, existingPayment, PXRedirectHelper.WindowMode.Same);

			return adapter.Get();
		}

		public PXAction<PRPayment> ViewExistingPayrollBatch;
		[PXUIField(DisplayName = "View Existing Payroll Batch", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewExistingPayrollBatch(PXAdapter adapter)
		{
			PRBatch existingPayrollBatch = SelectFrom<PRBatch>.Where<PRBatch.batchNbr.IsEqual<P.AsString>>.View.
				SelectSingleBound(this, null, ExistingPayrollBatch.Current.BatchNbr);

			if (existingPayrollBatch != null)
			{
				Clear();
				PRPayBatchEntry payBatchEntryGraph = CreateInstance<PRPayBatchEntry>();
				PXRedirectHelper.TryRedirect(payBatchEntryGraph, existingPayrollBatch, PXRedirectHelper.WindowMode.Same);
			}

			return adapter.Get();
		}

		public PXAction<PRPayment> ViewProjectDeductionAndBenefitPackages;
		[PXUIField(DisplayName = "Deduction and Benefit Packages", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewProjectDeductionAndBenefitPackages()
		{
			ProjectPackageDeductions.AskExt();
		}

		public CatchRightsErrorAction<PRPayment> CheckTaxUpdateTimestamp;
		[PXButton]
		[PXUIField(Visible = false)]
		public virtual void checkTaxUpdateTimestamp()
		{
			if (!TaxUpdateHelpers.CheckTaxUpdateTimestamp(UpdateHistory.View))
			{
				UpdateTaxesPopupView.Current.Message = Messages.TaxUpdateNeeded;
				UpdateTaxesPopupView.AskExt();
			}
		}

		public PXAction<PRPayment> RedirectTaxMaintenance;
		[PXButton]
		[PXUIField(DisplayName = "Tax Maintenance")]
		protected virtual IEnumerable redirectTaxMaintenance(PXAdapter adapter)
		{
			PXRedirectHelper.TryRedirect(CreateInstance<PRTaxMaintenance>(), PXRedirectHelper.WindowMode.Same);
			return adapter.Get();
		}

		public PXAction<PRPayment> DeleteEarningDetail;
		[PXUIField(DisplayName = "Delete", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual void deleteEarningDetail()
		{
			if (Earnings.Current?.IsPayingCarryover == true)
			{
				WebDialogResult promptResult = Earnings.Ask(Messages.AskSkipCarryoverPayments, MessageButtons.YesNo);
				Document.Current.AutoPayCarryover = promptResult == WebDialogResult.No;
			}

			Earnings.DeleteCurrent();
		}

		#endregion

		#region Events
		protected void _(Events.RowSelected<PRPayment> e)
		{
			if (e.Row == null) return;

			PXEntryStatus currentRecordStatus = e.Cache.GetStatus(e.Row);
			e.Cache.Adjust<PXUIFieldAttribute>(e.Row).
				For<PRPayment.payGroupID>(field => field.Enabled = currentRecordStatus == PXEntryStatus.Inserted)
				.SameFor<PRPayment.payPeriodID>()
				.SameFor<PRPayment.employeeID>()
				.For<PRPayment.paymentMethodID>(field => field.Enabled = e.Row.PaymentBatchNbr == null)
				.SameFor<PRPayment.cashAccountID>()
				.For<PRPayment.branchID>(field => field.Enabled = e.Row.DocType != PayrollType.VoidCheck)
				.SameFor<PRPayment.extRefNbr>()
				.SameFor<PRPayment.empType>()
				.SameFor<PRPayment.finPeriodID>()
				.For<PRPayment.chkVoidType>(field => field.Enabled = e.Row.DocType == PayrollType.VoidCheck)
				.For<PRPayment.transactionDate>(field => field.Enabled = e.Row.DocType == PayrollType.Final)
				.For<PRPayment.endDate>(field => field.Enabled = e.Row.DocType == PayrollType.Final);

			bool notPaid = !IsPaid(e.Row);
			bool enableDetailEdit = ShouldEnableDetailEdit(e.Row);
			bool isReadyForInput = IsReadyForInput(e.Row);

			Document.Cache.AllowUpdate = notPaid;
			Document.Cache.AllowDelete = notPaid;
			PXUIFieldAttribute.SetEnabled<PRPayment.branchID>(e.Cache, e.Row, e.Row.EmployeeID != null);
			PaymentPTOBanks.Cache.AllowUpdate = notPaid;
			Earnings.Cache.AllowInsert = isReadyForInput && notPaid;
			Earnings.Cache.AllowUpdate = e.Row.Released == false;
			Earnings.Cache.AllowDelete = notPaid;
			Deductions.Cache.AllowInsert = isReadyForInput && notPaid;
			Deductions.Cache.AllowUpdate = notPaid;
			DeductionDetails.Cache.AllowInsert = enableDetailEdit;
			DeductionDetails.Cache.AllowUpdate = e.Row.Released == false;
			DeductionDetails.Cache.AllowDelete = enableDetailEdit;
			BenefitDetails.Cache.AllowInsert = enableDetailEdit;
			BenefitDetails.Cache.AllowUpdate = e.Row.Released == false;
			BenefitDetails.Cache.AllowDelete = enableDetailEdit;
			Taxes.Cache.AllowInsert = enableDetailEdit && isReadyForInput;
			Taxes.Cache.AllowUpdate = enableDetailEdit;
			Taxes.Cache.AllowDelete = enableDetailEdit;
			TaxDetails.Cache.AllowInsert = enableDetailEdit;
			TaxDetails.Cache.AllowUpdate = e.Row.Released == false;
			TaxDetails.Cache.AllowDelete = enableDetailEdit;
			TaxSplits.Cache.AllowUpdate = enableDetailEdit;
			PaymentTaxApplicableAmounts.Cache.AllowInsert = enableDetailEdit && isReadyForInput;
			PaymentTaxApplicableAmounts.Cache.AllowUpdate = enableDetailEdit;
			PaymentTaxApplicableAmounts.Cache.AllowDelete = enableDetailEdit;
			PaymentOvertimeRules.Cache.AllowUpdate = e.Row.ExemptFromOvertimeRules == false && notPaid;
			WCPremiums.Cache.AllowInsert = enableDetailEdit && isReadyForInput;
			WCPremiums.Cache.AllowUpdate = enableDetailEdit;
			WCPremiums.Cache.AllowDelete = enableDetailEdit;
			UnionPackageDeductions.Cache.AllowInsert = enableDetailEdit && isReadyForInput;
			UnionPackageDeductions.Cache.AllowUpdate = enableDetailEdit;
			UnionPackageDeductions.Cache.AllowDelete = enableDetailEdit;
			ProjectPackageDeductions.Cache.AllowInsert = enableDetailEdit && isReadyForInput;
			ProjectPackageDeductions.Cache.AllowUpdate = enableDetailEdit;
			ProjectPackageDeductions.Cache.AllowDelete = enableDetailEdit;

			Calculate.SetEnabled(IsCalculateActionEnabled(e.Row));
			ProcessPayment.SetEnabled(e.Row.Hold == false && e.Row.Paid == false && e.Row.PaymentBatchNbr == null
				&& (e.Row.Calculated == true || e.Row.DocType == PayrollType.Adjustment)
				&& e.Row.NetAmount > 0 && PRSetup.Current.UpdateGL == true
			);
			Release.SetEnabled(IsReleaseActionEnabled(e.Row, PRSetup.Current.UpdateGL ?? false));
			PrintPayStub.SetEnabled(e.Row.Paid == true && e.Row.Voided != true);
			VoidPayment.SetEnabled(e.Row.Released == true
				|| e.Row.Status == PaymentStatus.Closed && e.Row.HasUpdatedGL == false);
			CopySelectedEarningDetailLine.SetEnabled(Earnings.Cache.AllowInsert);
			ViewOvertimeRules.SetEnabled(e.Row.ExemptFromOvertimeRules == false);
			ImportTimeActivities.SetEnabled(isReadyForInput && notPaid);
			CreateROE.SetVisible(e.Row.DocType == PayrollType.Final);
			CreateROE.SetEnabled(e.Row.DocType == PayrollType.Final && e.Row.Released == true && !RecordsOfEmployment.Select().Any());

			if (e.Row.DocType == PayrollType.VoidCheck)
			{
				Document.Cache.AllowUpdate = (e.Row.Released == false);
				Document.Cache.AllowDelete = (e.Row.Released == false);
				Earnings.Cache.AllowUpdate = false;
				Earnings.Cache.AllowDelete = false;
				DeductionDetails.Cache.AllowUpdate = false;
				BenefitDetails.Cache.AllowUpdate = false;
				TaxDetails.Cache.AllowUpdate = false;
				VoidPayment.SetEnabled(false);
				Calculate.SetEnabled(false);
				PrintPayStub.SetEnabled(false);
				ProcessPayment.SetEnabled(false);
				CopySelectedEarningDetailLine.SetEnabled(false);
				ViewOvertimeRules.SetEnabled(false);
				ImportTimeActivities.SetEnabled(false);
				CreateROE.SetEnabled(false);
			}

			PXUIFieldAttribute.SetVisible<PRPayment.countryID>(e.Cache, e.Row, CountryPayrollFeatureSwitches.Count(x => PXAccess.FeatureInstalled(x.FullName)) > 1);
		}

		protected void _(Events.RowInserted<PRPayment> e)
		{
			// Force PXNoteAttribute to create a Note record referencing the NoteID field,
			// so that project transactions contain the payment's RefNbr
			PXNoteAttribute.GetNoteID(e.Cache, e.Row, nameof(PRPayment.noteID));
		}

		protected void _(Events.RowPersisting<PRPayment> e)
		{
			foreach (PREarningDetail earning in Earnings.Select())
			{
				object value = earning.Amount;
				Earnings.Cache.RaiseFieldVerifying<PREarningDetail.amount>(earning, ref value);
			}

			if (!PXUIFieldAttribute.GetErrors(Earnings.Cache, null).Any())
			{
				DeleteEmptySummaryEarnings(SummaryEarnings.View, Earnings.Cache);
			}

			//Verify if tax details account values are empty
			TaxDetails.Select().FirstTableItems.ForEach(detailRow =>
			{
				object expenseAccountValue = detailRow.ExpenseAccountID;
				TaxDetails.Cache.RaiseFieldVerifying<PRTaxDetail.expenseAccountID>(detailRow, ref expenseAccountValue);
				object liabilityAccountValue = detailRow.LiabilityAccountID;
				TaxDetails.Cache.RaiseFieldVerifying<PRTaxDetail.liabilityAccountID>(detailRow, ref liabilityAccountValue);
			});
		}
				
		public void _(Events.FieldUpdated<PRPayment.employeeID> e)
		{
			var row = e.Row as PRPayment;
			if (row == null || IsVoidCheckInProgress)
			{
				return;
			}

			UpdateChildrenRecords(row);

			PREmployee employee = CurrentEmployee.SelectSingle();
			if (employee != null && !string.Equals(employee.PayGroupID, row.PayGroupID))
				e.Cache.SetValueExt<PRPayment.payGroupID>(e.Row, employee.PayGroupID);

			if (string.IsNullOrWhiteSpace(row.PayPeriodID) && !IsCopyPasteContext)
				e.Cache.SetDefaultExt<PRPayment.payPeriodID>(row);

			if (row.TerminationDate == null)
			{
				e.Cache.SetDefaultExt<PRPayment.terminationDate>(row);
			}

			UpdatePaymentOvertimeRules(Document.Current.ApplyOvertimeRules ?? false);

			if (row.DocType == PayrollType.VoidCheck)
			{
				e.Cache.Adjust<EmployeeActiveInPayGroupAttribute>(e.Row).For<PRPayment.employeeID>(x => x.FilterActive = true);
			}
		}

		public void _(Events.FieldUpdated<PRPayment.payGroupID> e)
		{
			var row = e.Row as PRPayment;
			if (row == null)
			{
				return;
			}

			PREmployee employee = CurrentEmployee.SelectSingle();
			if (employee != null && !string.Equals(employee.PayGroupID, row.PayGroupID))
				row.EmployeeID = null;

			if (!IsCopyPasteContext)
			{
				e.Cache.SetDefaultExt<PRPayment.payPeriodID>(row);
			}
		}

		public void _(Events.FieldDefaulting<PRPayment.payPeriodID> e)
		{
			var row = e.Row as PRPayment;
			if (row == null || row.PayGroupID == null || row.EmployeeID == null || IsCopyPasteContext)
			{
				return;
			}

			var latestPayment = SelectFrom<PRPayment>
				.Where<PRPayment.payGroupID.IsEqual<@P.AsString>
					.And<PRPayment.employeeID.IsEqual<@P.AsInt>>>
				.OrderBy<PRPayment.payPeriodID.Desc>.View.Select(this, row.PayGroupID, row.EmployeeID).FirstTableItems.FirstOrDefault();

			if (latestPayment?.PayPeriodID != null)
			{
				PRPayGroupPeriod nextPayPeriod = PRPayPeriodMaint.FindNextPayPeriod(this, row.PayGroupID, latestPayment.PayPeriodID);
				if (nextPayPeriod != null &&
					GetExistingPayment(row.EmployeeID, nextPayPeriod.FinPeriodID, row.PayGroupID) == null &&
					GetExistingPayrollBatch(row.EmployeeID, nextPayPeriod.FinPeriodID, row.PayGroupID) == null)
				{
					e.NewValue = FinPeriodIDFormattingAttribute.FormatForDisplay(nextPayPeriod.FinPeriodID);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PRPayment.applyOvertimeRules> e)
		{
			bool? applyOvertimeRulesNewValue = e.NewValue as bool?;
			if (!Equals(applyOvertimeRulesNewValue, e.OldValue))
				UpdatePaymentOvertimeRules(applyOvertimeRulesNewValue ?? false);
		}

		protected virtual void _(Events.FieldUpdated<PRPayment.payPeriodID> e)
		{
			if (!Equals(e.NewValue, e.OldValue))
				UpdatePaymentOvertimeRules(Document.Current.ApplyOvertimeRules ?? false);

			PRPayment row = e.Row as PRPayment;
			if (row != null && !IsVoidCheckInProgress)
				UpdateChildrenRecords(row);
		}

		private void UpdatePaymentOvertimeRules(bool applyOvertimeRules)
		{
			if (Document.Current.DocType == PayrollType.VoidCheck)
				return;

			DeletePaymentOvertimeRules();
			if (applyOvertimeRules && !IsCopyPasteContext)
				InsertPaymentOvertimeRules();
		}

		private void InsertPaymentOvertimeRules()
		{
			if (Document.Current.ExemptFromOvertimeRules == true || Document.Current.IsWeeklyOrBiWeeklyPeriod == null)
				return;

			PXResultset<PRBatchOvertimeRule> batchOvertimeRules = BatchOvertimeRules.Select();
			PXResultset<PROvertimeRule> activeOvertimeRules =
				SelectFrom<PROvertimeRule>.
				Where<PROvertimeRule.isActive.IsEqual<True>
					.And<PROvertimeRule.countryID.IsEqual<PRPayment.countryID.FromCurrent>>>.View.Select(this);

			bool paymentOvertimeRulesModified = false;
			bool weeklyOvertimeRulesAllowed = Document.Current.IsWeeklyOrBiWeeklyPeriod == true;
			if (!batchOvertimeRules.Any_())
			{
				foreach (PROvertimeRule overtimeRule in activeOvertimeRules)
				{
					PRPaymentOvertimeRule paymentOvertimeRule = new PRPaymentOvertimeRule
					{
						OvertimeRuleID = overtimeRule.OvertimeRuleID,
						IsActive = weeklyOvertimeRulesAllowed || overtimeRule.RuleType == PROvertimeRuleType.Daily
					};
					PaymentOvertimeRules.Update(paymentOvertimeRule);
					paymentOvertimeRulesModified = true;
				}
			}
			else
			{
				Dictionary<string, PROvertimeRule> currentActiveOvertimeRules =
					activeOvertimeRules.Select(item => (PROvertimeRule)item).ToDictionary(item => item.OvertimeRuleID);
				foreach (PRBatchOvertimeRule batchOvertimeRule in batchOvertimeRules)
				{
					PROvertimeRule currentOvertimeRule;
					if (!currentActiveOvertimeRules.TryGetValue(batchOvertimeRule.OvertimeRuleID, out currentOvertimeRule))
						continue;

					PRPaymentOvertimeRule paymentOvertimeRule = new PRPaymentOvertimeRule
					{
						OvertimeRuleID = batchOvertimeRule.OvertimeRuleID,
						IsActive = batchOvertimeRule.IsActive.GetValueOrDefault() && (weeklyOvertimeRulesAllowed || currentOvertimeRule.RuleType == PROvertimeRuleType.Daily)
					};

					PaymentOvertimeRules.Update(paymentOvertimeRule);
					paymentOvertimeRulesModified = true;
				}
			}

			if (paymentOvertimeRulesModified)
				Document.Current.Calculated = false;
		}

		private void DeletePaymentOvertimeRules()
		{
			bool paymentOvertimeRulesModified = false;
			PaymentOvertimeRules.Select().ForEach(paymentOvertimeRule =>
			{
				PaymentOvertimeRules.Delete(paymentOvertimeRule);
				paymentOvertimeRulesModified = true;
			});

			if (paymentOvertimeRulesModified)
				Document.Current.Calculated = false;
		}

		public void _(Events.RowPersisted<PRPayment> e)
		{
			if (!e.Cache.Deleted.Any_())
				return;

			string payBatchNumber = e.Row?.PayBatchNbr;
			int? employeeID = e.Row?.EmployeeID;
			if (!string.IsNullOrWhiteSpace(payBatchNumber) && employeeID != null)
			{
				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Payroll batch needs to be updated on paycheck delete]
				// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [Payroll batch needs to be updated on paycheck delete]
				UpdatePayrollBatch(payBatchNumber, employeeID);
			}

			if (!string.IsNullOrEmpty(e.Row.PaymentBatchNbr))
			{
				PRCABatch paymentBatch = new SelectFrom<PRCABatch>.Where<PRCABatch.batchNbr.IsEqual<P.AsString>>.View(this).SelectSingle(e.Row.PaymentBatchNbr);
				if (paymentBatch != null)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [Payment batch needs to be updated on paycheck delete]
					// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [Payment batch needs to be updated on paycheck delete]
					PRCABatchUpdate.RecalculatePaymentBatchTotal(paymentBatch);
				}
			}
		}

		public void _(Events.RowPersisted<PREarningDetail> e)
		{
			if (e.TranStatus != PXTranStatus.Completed)
				return;

			if (Document.Current == null)
				return;

			string payBatchNumber = Document.Current.PayBatchNbr;
			int? employeeID = Document.Current.EmployeeID;

			if (string.IsNullOrWhiteSpace(payBatchNumber))
				return;

			UpdatePayrollBatch(payBatchNumber, employeeID);
		}

		public void _(Events.RowInserted<PRPaymentEarning> e)
		{
			var row = e.Row as PRPaymentEarning;
			if (row == null)
			{
				return;
			}

			UpdateSummaryEarning(this, Document.Current, row);
		}

		public void _(Events.FieldUpdated<PRPaymentDeduct.codeID> e)
		{
			var row = e.Row as PRPaymentDeduct;
			if (row == null)
			{
				return;
			}

			UpdateSummaryDeductions(this, Document.Current, row);
		}

		protected void _(Events.RowSelected<PRPaymentDeduct> e)
		{
			var row = e.Row as PRPaymentDeduct;
			if (row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.codeID>(e.Cache, row, row.CodeID == null);
			PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.saveOverride>(e.Cache, row, row.Source == PaymentDeductionSourceAttribute.EmployeeSettings);

			bool amountsEnabled = row.SaveOverride == true && row.IsActive == true || IsCopyPasteContext;
			PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.dedAmount>(e.Cache, row, amountsEnabled);
			PXUIFieldAttribute.SetEnabled<PRPaymentDeduct.cntAmount>(e.Cache, row, amountsEnabled);
		}

		protected virtual void _(Events.FieldVerifying<PREarningDetail.hours> e)
		{
			PREarningDetail row = e.Row as PREarningDetail;
			CheckForNegative<PREarningDetail.hours>((decimal?)e.NewValue, row.PaymentDocType, () => !_IsTimeActivitiesImportInProgress);
		}

		protected virtual void _(Events.FieldVerifying<PREarningDetail.units> e)
		{
			PREarningDetail row = e.Row as PREarningDetail;
			CheckForNegative<PREarningDetail.units>((decimal?)e.NewValue, row.PaymentDocType);
		}

		protected virtual void _(Events.FieldVerifying<PREarningDetail.amount> e)
		{
			PREarningDetail row = e.Row as PREarningDetail;
			var newvalue = (decimal?)e.NewValue;
			PREarningType earningType = (PXSelectorAttribute.Select(e.Cache, e.Row, nameof(PREarningDetail.typeCD)) as EPEarningType)?.GetExtension<PREarningType>();
			CheckForNegative<PREarningDetail.amount>(newvalue, row.PaymentDocType, () => earningType?.IsAmountBased == true);
			if(row.PaymentDocType == PayrollType.Regular && newvalue < 0 && IsCopyPasteContext == true)
			{
				e.NewValue = 0m;
			}
		}

		protected virtual void _(Events.FieldVerifying<PRPaymentDeduct.dedAmount> e)
		{
			PRPaymentDeduct row = e.Row as PRPaymentDeduct;
			CheckForNegative<PRPaymentDeduct.dedAmount>((decimal?)e.NewValue, row.DocType);
		}

		protected virtual void _(Events.FieldVerifying<PRPaymentDeduct.cntAmount> e)
		{
			PRPaymentDeduct row = e.Row as PRPaymentDeduct;
			CheckForNegative<PRPaymentDeduct.cntAmount>((decimal?)e.NewValue, row.DocType);
		}

		protected virtual void _(Events.FieldVerifying<PRPayment.employeeID> e)
		{
			CheckExistingPaychecksAndBatches(e, e.NewValue as int?, Document.Current.PayPeriodID);
		}

		protected virtual void _(Events.FieldVerifying<PRPayment.payPeriodID> e)
		{
			CheckExistingPaychecksAndBatches(e, Document.Current.EmployeeID, e.NewValue as string);
		}

		public void _(Events.FieldUpdated<PRPaymentTax.taxAmount> e)
		{
			var row = e.Row as PRPaymentTax;
			//if e.ExternalCall is false, the PRPaymentTax row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (row == null || !e.ExternalCall || CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			RecreateTaxDetails(row, row.TaxID);
		}

		public void _(Events.FieldUpdated<PRPaymentTax.taxID> e)
		{
			var row = e.Row as PRPaymentTax;
			if (row == null || e.OldValue == null || CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			RecreateTaxDetails(row, (int?)e.OldValue);
			TaxSplits.Select().FirstTableItems.Where(x => x.TaxID == (int?)e.OldValue).ForEach(x => TaxSplits.Delete(x));
		}

		public void _(Events.FieldUpdated<PRPaymentDeduct.isActive> e)
		{
			var row = e.Row as PRPaymentDeduct;
			//if e.ExternalCall is false, the PRPaymentDeduct row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (row == null || !e.ExternalCall || CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			RecreateDeductionDetails(row);
			RecreateBenefitDetails(row);
		}

		public void _(Events.FieldUpdated<PRPaymentDeduct.dedAmount> e)
		{
			var row = e.Row as PRPaymentDeduct;
			//if e.ExternalCall is false, the PRPaymentDeduct row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (row == null || !e.ExternalCall || CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			RecreateDeductionDetails(row);
		}

		public void _(Events.FieldUpdated<PRPaymentDeduct.cntAmount> e)
		{
			var row = e.Row as PRPaymentDeduct;
			//if e.ExternalCall is false, the PRPaymentDeduct row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (row == null || !e.ExternalCall || CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			RecreateBenefitDetails(row);
		}

		//Skips RefNbr verification during Void process as the RefNbr is already set
		protected virtual void _(Events.FieldVerifying<PRPayment.refNbr> e)
		{
			if (IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}
		}

		public void _(Events.RowSelected<PRPaymentPTOBank> e)
		{
			var row = e.Row as PRPaymentPTOBank;
			if (row == null)
			{
				return;
			}

			bool enableCondition = CurrentDocument.Current.DocType == PayrollType.Adjustment
				&& CurrentDocument.Current.Released == false && CurrentDocument.Current.Paid == false;
			PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.accrualAmount>(e.Cache, row, enableCondition);
			PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.accrualMoney>(e.Cache, row, enableCondition && row.CreateFinancialTransaction == true);
			PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.disbursementAmount>(e.Cache, row, enableCondition);
			PXUIFieldAttribute.SetEnabled<PRPaymentPTOBank.disbursementMoney>(e.Cache, row, enableCondition && row.CreateFinancialTransaction == true);

			if (row.AvailableAmount < 0)
			{
				PXUIFieldAttribute.SetWarning<PRPaymentPTOBank.availableAmount>(e.Cache, row, Messages.PTOUsedExceedsAvailable);
			}
			else if (row.AvailableMoney < 0)
			{
				PXUIFieldAttribute.SetWarning<PRPaymentPTOBank.availableMoney>(e.Cache, row, Messages.PTOUsedExceedsAvailable);
			}
		}

		public void _(Events.FieldUpdated<PRPaymentPTOBank.accrualAmount> e)
		{
			PRPaymentPTOBank row = e.Row as PRPaymentPTOBank;
			if (row == null)
				return;

			if (row.AccrualAmount != 0)
			{
				e.Cache.SetValue<PRPaymentPTOBank.isActive>(row, true);
			}
		}

		public void _(Events.FieldUpdated<PRPaymentPTOBank.accrualMoney> e)
		{
			PRPaymentPTOBank row = e.Row as PRPaymentPTOBank;
			if (row == null)
				return;

			if (row.AccrualMoney != 0)
			{
				e.Cache.SetValue<PRPaymentPTOBank.isActive>(row, true);
			}
		}

		public void _(Events.FieldUpdating<PRPaymentPTOBank.accrualAmount> e)
		{
			var row = e.Row as PRPaymentPTOBank;
			if (row == null || e.NewValue == null)
			{
				return;
			}

			var newValue = (decimal)e.NewValue;
			if (newValue + row.AccumulatedAmount > row.AccrualLimit)
			{
				var diff = (row.AccrualLimit - row.AccumulatedAmount).GetValueOrDefault();
				e.NewValue = Math.Max(diff, 0m);
			}
		}

		public void _(Events.FieldUpdated<PRPayment.paymentMethodID> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowInserted<PREarningDetail> e)
		{
			OnEarningDetailInserted(e.Row);
		}

		public void _(Events.RowUpdated<PREarningDetail> e)
		{
			if (!e.Cache.ObjectsEqualExceptFields<PREarningDetail.accountID, PREarningDetail.subID>(e.Row, e.OldRow))
			{
				Document.Current.Calculated = false;
			}
			DeleteEmptySummaryEarnings(SummaryEarnings.View, e.Cache);
		}

		public void _(Events.RowDeleted<PREarningDetail> e)
		{
			Document.Current.Calculated = false;
			DeleteEmptySummaryEarnings(SummaryEarnings.View, e.Cache);
		}

		public void _(Events.RowInserted<PRPaymentDeduct> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowUpdated<PRPaymentDeduct> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowDeleted<PRPaymentDeduct> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowInserted<PRPaymentTax> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowUpdated<PRPaymentTax> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowDeleted<PRPaymentTax> e)
		{
			Document.Current.Calculated = false;
		}

		public void _(Events.RowUpdated<PRPaymentPTOBank> e)
		{
			Document.Current.Calculated = false;
		}

		protected void _(Events.FieldUpdated<PREarningDetail.unionID> e)
		{
			AddUnionDeductions(e.Row as PREarningDetail, true);
		}

		protected void _(Events.FieldUpdated<PREarningDetail.projectID> e)
		{
			AddProjectDeductions(e.Row as PREarningDetail, true);
			e.Cache.SetDefaultExt<PREarningDetail.certifiedJob>(e.Row);
		}

		protected void _(Events.FieldUpdated<PREarningDetail.labourItemID> e)
		{
			AddUnionDeductions(e.Row as PREarningDetail, true);
			AddProjectDeductions(e.Row as PREarningDetail, true);
		}

		protected virtual void _(Events.FieldUpdated<PMTimeActivity.selected> e)
		{
			ImportTimeActivitiesHelper.TimeActivitySelectionUpdated(e, TimeActivities.Select(), ImportTimeActivitiesFilter.Current);
		}
		
		protected void _(Events.FieldUpdated<PREarningDetail.typeCD> e)
		{
			PREarningDetail row = e.Row as PREarningDetail;
			if (row == null)
			{
				return;
			}

			SetEarningFinancialPTOFlags(row);
		}

		protected void _(Events.RowSelected<PREarningDetail> e)
		{
			if (e.Row == null)
			{
				return;
			}

			ImportTimeActivitiesHelper.EarningDetailSelected(e.Cache, e.Row, e.Row.PaymentDocType != PayrollType.Adjustment && e.Row.PaymentDocType != PayrollType.VoidCheck, out bool warningOnHoursField);

			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, e.Row.IsPayingCarryover == false);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.sourceNoteID>(e.Cache, e.Row, false);
			if (e.Row.IsFringeRateEarning == true || e.Row.BaseOvertimeRecordID != null || e.Row.BasePTORecordID != null|| e.Row.PaymentDocType == PayrollType.VoidCheck)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
			}
			else if (Document.Current.Status == PaymentStatus.Paid && Document.Current.Released == false)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<PREarningDetail.accountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PREarningDetail.subID>(e.Cache, e.Row, true);
			}
			else
			{
				PXUIFieldAttribute.SetEnabled<PREarningDetail.rate>(Earnings.Cache, e.Row, e.Row.PTODisbursementWithAverageRate != true);
				PXUIFieldAttribute.SetEnabled<PREarningDetail.manualRate>(Earnings.Cache, e.Row, e.Row.PTODisbursementWithAverageRate != true);
				PXUIFieldAttribute.SetEnabled<PREarningDetail.accountID>(Earnings.Cache, e.Row, e.Row.PTODisbursementWithFinancialTransaction != true);
				PXUIFieldAttribute.SetEnabled<PREarningDetail.subID>(Earnings.Cache, e.Row, e.Row.PTODisbursementWithFinancialTransaction != true);
			}

			if (!warningOnHoursField)
			{
				string errorMessage = string.Empty;
				if(e.Row.IsPayingCarryover == true)
				{
					errorMessage = Messages.CarryoverPaidWithThisEarningLine;
				}
				else if(e.Row.IsPayingSettlement == true)
				{
					errorMessage = Messages.EarningIsPayingSettlementWarning;
				}

				PXUIFieldAttribute.SetWarning<PREarningDetail.hours>(e.Cache, e.Row, errorMessage);
			}

			if (e.Row.PTODisbursementWithFinancialTransaction == true)
			{
				string message;
				if (e.Row.PTODisbursementWithAverageRate == true)
				{
					message = string.Format(Messages.PTOFieldWillBeOverridden, PXUIFieldAttribute.GetDisplayName<PREarningDetail.rate>(Earnings.Cache));
					PXUIFieldAttribute.SetWarning<PREarningDetail.rate>(Earnings.Cache, e.Row, message);
				}

				message = PXMessages.LocalizeFormatNoPrefix(Messages.PTOFieldWillBeOverridden, PXUIFieldAttribute.GetDisplayName<PREarningDetail.accountID>(Earnings.Cache));
				PXUIFieldAttribute.SetWarning<PREarningDetail.accountID>(Earnings.Cache, e.Row, message);
				message = PXMessages.LocalizeFormatNoPrefix(Messages.PTOFieldWillBeOverridden, PXUIFieldAttribute.GetDisplayName<PREarningDetail.subID>(Earnings.Cache));
				PXUIFieldAttribute.SetWarning<PREarningDetail.subID>(Earnings.Cache, e.Row, message);
			}

			string salariedExemptError = null;
			if (EmployeeType.IsOvertimeEarningForSalariedExempt<PRPayment>(e.Cache, e.Row))
			{
				salariedExemptError = Messages.SalariedExemptWithOT;
			}
			PXUIFieldAttribute.SetError<PREarningDetail.typeCD>(e.Cache, e.Row, salariedExemptError);
		}

		protected virtual void _(Events.RowSelected<PMTimeActivityExt> e)
		{
			ImportTimeActivitiesHelper.TimeActivitySelected(e, TimeActivities.Cache);
		}

		protected virtual void _(Events.RowPersisting<PREarningDetail> e)
		{
			if (e.Operation.Command() == PXDBOperation.Delete)
			{
				return;
			}

			if (EmployeeType.IsOvertimeEarningForSalariedExempt<PRPayment>(e.Cache, e.Row))
			{
				throw new PXException(Messages.SalariedExemptWithOT);
			}
		}

		protected void _(Events.RowSelected<PRDeductionDetail> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRDeductionDetail.codeID>(e.Cache, e.Row, e.Row.CodeID == null);
			if (Document.Current.Paid == true && Document.Current.Released == false)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<PRDeductionDetail.accountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRDeductionDetail.subID>(e.Cache, e.Row, true);
			}
		}

		public void _(Events.RowSelected<PRPaymentOvertimeRule> e)
		{
			if (e.Row == null || !BatchOvertimeRules.AllowUpdate)
				return;

			bool overtimeRuleEnabled = Document.Current.IsWeeklyOrBiWeeklyPeriod == true || e.Row.RuleType == PROvertimeRuleType.Daily;
			PXUIFieldAttribute.SetEnabled(PaymentOvertimeRules.Cache, e.Row, overtimeRuleEnabled);
			if (!overtimeRuleEnabled)
				PXUIFieldAttribute.SetWarning<PRPaymentOvertimeRule.overtimeRuleID>(PaymentOvertimeRules.Cache, e.Row, Messages.WeeklyOvertimeRulesApplyToWeeklyPeriods);
		}

		protected void _(Events.RowUpdated<PRPaymentWCPremium> e)
		{
			if (e.Row?.DeductCodeID == null || string.IsNullOrEmpty(e.Row?.WorkCodeID) || !e.ExternalCall)
			{
				return;
			}

			if (Document.Current.DocType == PayrollType.VoidCheck || e.Cache.ObjectsEqual<
				PRPaymentWCPremium.workCodeID,
				PRPaymentWCPremium.deductCodeID,
				PRPaymentWCPremium.deductionAmount,
				PRPaymentWCPremium.amount>(e.Row, e.OldRow))
			{
				return;
			}

			RecreateWorkCompensationDeductionSummaryAndDetails();
		}

		protected void _(Events.RowInserted<PRPaymentWCPremium> e)
		{
			if (e.Row?.DeductCodeID == null || string.IsNullOrEmpty(e.Row?.WorkCodeID) || !e.ExternalCall || Document.Current.DocType == PayrollType.VoidCheck)
			{
				return;
			}

			RecreateWorkCompensationDeductionSummaryAndDetails();
		}

		protected void _(Events.RowDeleted<PRPaymentWCPremium> e)
		{
			if (e.Row?.DeductCodeID == null || string.IsNullOrEmpty(e.Row?.WorkCodeID) || !e.ExternalCall)
			{
				return;
			}

			RecreateWorkCompensationDeductionSummaryAndDetails();
		}

		protected void _(Events.RowUpdated<PRPaymentProjectPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.ProjectID == null || !e.ExternalCall)
			{
				return;
			}

			if (Document.Current.DocType == PayrollType.VoidCheck || e.Cache.ObjectsEqual<
				PRPaymentProjectPackageDeduct.projectID,
				PRPaymentProjectPackageDeduct.laborItemID,
				PRPaymentProjectPackageDeduct.deductCodeID,
				PRPaymentProjectPackageDeduct.deductionAmount,
				PRPaymentProjectPackageDeduct.benefitAmount>(e.Row, e.OldRow))
			{
				return;
			}

			RecreateProjectDeductionSummaryAndDetails();
		}

		protected void _(Events.RowInserted<PRPaymentProjectPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.ProjectID == null || !e.ExternalCall)
			{
				return;
			}

			RecreateProjectDeductionSummaryAndDetails();
		}

		protected void _(Events.RowDeleted<PRPaymentProjectPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.ProjectID == null || !e.ExternalCall)
			{
				return;
			}

			RecreateProjectDeductionSummaryAndDetails();
		}

		protected void _(Events.RowUpdated<PRPaymentUnionPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.UnionID == null || !e.ExternalCall)
			{
				return;
			}

			if (Document.Current.DocType == PayrollType.VoidCheck || e.Cache.ObjectsEqual<
				PRPaymentUnionPackageDeduct.unionID,
				PRPaymentUnionPackageDeduct.laborItemID,
				PRPaymentUnionPackageDeduct.deductCodeID,
				PRPaymentUnionPackageDeduct.deductionAmount,
				PRPaymentUnionPackageDeduct.benefitAmount>(e.Row, e.OldRow))
			{
				return;
			}

			RecreateUnionDeductionSummaryAndDetails();
		}

		protected void _(Events.RowInserted<PRPaymentUnionPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.UnionID == null || !e.ExternalCall)
			{
				return;
			}

			RecreateUnionDeductionSummaryAndDetails();
		}

		protected void _(Events.RowDeleted<PRPaymentUnionPackageDeduct> e)
		{
			if (e.Row?.DeductCodeID == null || e.Row?.UnionID == null || !e.ExternalCall)
			{
				return;
			}

			RecreateUnionDeductionSummaryAndDetails();
		}

		protected void _(Events.RowPersisting<PRPaymentDeduct> e)
		{
			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted && e.Row.CodeID == null)
			{
				e.Cancel = true;
			}
		}

		protected void _(Events.FieldUpdated<PRPaymentDeduct.saveOverride> e)
		{
			PRPaymentDeduct row = e.Row as PRPaymentDeduct;
			if (row == null || !e.NewValue.Equals(false))
			{
				return;
			}

			e.Cache.SetDefaultExt<PRPaymentDeduct.dedAmount>(row);
			e.Cache.SetDefaultExt<PRPaymentDeduct.cntAmount>(row);
		}

		protected void _(Events.RowSelected<PRBenefitDetail> e)
		{
			var row = e.Row as PRBenefitDetail;
			if (row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRBenefitDetail.codeID>(e.Cache, row, row.CodeID == null);

			if (Document.Current.Paid == true && Document.Current.Released == false)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<PRBenefitDetail.liabilityAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRBenefitDetail.liabilitySubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRBenefitDetail.expenseAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRBenefitDetail.expenseSubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRBenefitDetail.costCodeID>(e.Cache, e.Row, true);
			}
		}

		protected void _(Events.RowSelected<PRTaxDetail> e)
		{
			var row = e.Row as PRTaxDetail;
			if (row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRTaxDetail.taxID>(e.Cache, row, row.TaxID == null);
			PXUIFieldAttribute.SetError<PRTaxDetail.amount>(e.Cache, row, row.AmountErrorMessage, row.Amount.ToString());

			if (Document.Current.Paid == true && Document.Current.Released == false)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<PRTaxDetail.liabilityAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRTaxDetail.liabilitySubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRTaxDetail.expenseAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRTaxDetail.expenseSubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRTaxDetail.costCodeID>(e.Cache, e.Row, true);
			}
		}

		protected void _(Events.FieldUpdated<PRDeductionDetail.amount> e)
		{
			PRDeductionDetail row = e.Row as PRDeductionDetail;
			if (row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustDeductionSummary(row.CodeID);
		}

		protected void _(Events.RowInserted<PRDeductionDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustDeductionSummary(e.Row.CodeID);
		}

		protected void _(Events.RowDeleted<PRDeductionDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustDeductionSummary(e.Row.CodeID);
		}

		protected void _(Events.FieldUpdated<PRBenefitDetail.amount> e)
		{
			PRBenefitDetail row = e.Row as PRBenefitDetail;
			if (row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustBenefitSummary(row.CodeID);
		}

		protected void _(Events.RowInserted<PRBenefitDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustBenefitSummary(e.Row.CodeID);
		}

		protected void _(Events.RowDeleted<PRBenefitDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustBenefitSummary(e.Row.CodeID);
		}

		protected void _(Events.FieldUpdated<PRTaxDetail.amount> e)
		{
			PRTaxDetail row = e.Row as PRTaxDetail;
			if (row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustTaxSummary(row.TaxID);
		}

		protected void _(Events.RowInserted<PRTaxDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustTaxSummary(e.Row.TaxID);
		}

		protected void _(Events.RowDeleted<PRTaxDetail> e)
		{
			if (e.Row == null || Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustTaxSummary(e.Row.TaxID);
		}

		protected void _(Events.FieldUpdated<PRBenefitDetail.labourItemID> e)
		{
			PRBenefitDetail row = e.Row as PRBenefitDetail;
			if (row == null)
			{
				return;
			}

			DefaultBenefitExpenseAcctSub(e.Cache, row);
		}

		protected void _(Events.FieldUpdated<PRBenefitDetail.earningTypeCD> e)
		{
			PRBenefitDetail row = e.Row as PRBenefitDetail;
			if (row == null)
			{
				return;
			}

			DefaultBenefitExpenseAcctSub(e.Cache, row);
		}

		protected void _(Events.FieldUpdated<PRTaxDetail.labourItemID> e)
		{
			PRTaxDetail row = e.Row as PRTaxDetail;
			if (row == null)
			{
				return;
			}

			DefaultTaxExpenseAcctSub(e.Cache, row);
		}

		protected void _(Events.FieldUpdated<PRTaxDetail.earningTypeCD> e)
		{
			PRTaxDetail row = e.Row as PRTaxDetail;
			if (row == null)
			{
				return;
			}

			DefaultTaxExpenseAcctSub(e.Cache, row);
		}

		protected void _(Events.FieldVerifying<PRPayment.paymentMethodID> e)
		{
			PRPayment row = e.Row as PRPayment;
			if (row == null || row.EmployeeID == null || e.NewValue == null)
			{
				return;
			}

			var paymentMethod = PaymentMethod.SelectSingle(e.NewValue);
			var paymentMethodExt = paymentMethod.GetExtension<PRxPaymentMethod>();
			if (paymentMethodExt.PRPrintChecks == false && !SelectFrom<PREmployeeDirectDeposit>
				.Where<PREmployeeDirectDeposit.bAccountID.IsEqual<P.AsInt>>.View.Select(this, row.EmployeeID).Any())
			{
				throw new PXSetPropertyException<PRPayment.paymentMethodID>(Messages.NoBankAccountForDirectDeposit);
			}
		}

		protected void _(Events.FieldUpdated<PRPayment.docType> e)
		{
			if (e.NewValue.Equals(PayrollType.VoidCheck))
			{
				// Allow void checks to be created for inactive employees
				e.Cache.Adjust<EmployeeActiveInPayGroupAttribute>(e.Row).For<PRPayment.employeeID>(x => x.FilterActive = false);
			}
		}

		protected virtual void _(Events.RowUpdating<PRPayment> e)
		{
			if (!IsCopyPasteContext || e.Row == null || e.NewRow == null)
			{
				return;
			}

			if (e.Row.Paid == true || e.Row.Released == true)
			{
				throw new PXException(Messages.CannotPasteIntoPaidOrReleasedPaycheck);
			}

			if (e.Row.EmployeeID != null)
			{
				e.NewRow.EmployeeID = e.Row.EmployeeID;
			}

			if (e.Row.ApplyOvertimeRules == false)
			{
				e.NewRow.ApplyOvertimeRules = false;
			}

			if (e.Row.AutoPayCarryover == false)
			{
				e.NewRow.AutoPayCarryover = false;
			}
		}

		protected virtual void _(Events.FieldUpdating<PRPayment, PRPayment.payPeriodID> e)
		{
			if (!IsCopyPasteContext || e.Row == null)
			{
				return;
			}

			string oldPayPeriodID = e.OldValue as string;
			string newPayPeriodID = FinPeriodIDFormattingAttribute.FormatForStoring(e.NewValue as string);

			if (!string.IsNullOrWhiteSpace(oldPayPeriodID) && oldPayPeriodID != newPayPeriodID)
			{
				e.NewValue = FinPeriodIDFormattingAttribute.FormatForDisplay(oldPayPeriodID);
			}
		}

		protected virtual void _(Events.FieldUpdating<PREarningDetail.date> e)
		{
			if (!IsCopyPasteContext || !(e.NewValue is DateTime newDate))
			{
				return;
			}

			PRPayment payment = Document.Current;
			if(newDate < payment.StartDate || newDate > payment.EndDate)
			{
				var groupPeriods = SelectFrom<PRPayGroupPeriod>.Where<PRPayGroupPeriod.payGroupID.IsEqual<PRPayment.payGroupID.FromCurrent>>.View.Select(this).FirstTableItems;
				int daysFromStart = 0;
				foreach(var period in groupPeriods)
				{
					// < period.EndDate instead of <= because there is overlap in date ranges in database (which doesn't appear on the screen)
					if (period.StartDate <= newDate && newDate < period.EndDate)
				{
						daysFromStart = (newDate - period.StartDate).Value.Days;
						break;
				}
			}

				var currentPaymentPeriod = groupPeriods.Single(x => x.FinPeriodID == payment.FinPeriodID);
				newDate = currentPaymentPeriod.StartDate.Value.AddDays(daysFromStart);
			}

			e.NewValue = newDate;
		}

		protected virtual void _(Events.RowUpdating<PRPaymentOvertimeRule> e)
		{
			if (IsCopyPasteContext && e.NewRow != null && Document.Current.IsWeeklyOrBiWeeklyPeriod == false && e.NewRow.RuleType == PROvertimeRuleType.Weekly)
			{
				e.NewRow.IsActive = false;
			}
		}

		protected virtual void _(Events.RowInserting<PRPaymentOvertimeRule> e)
		{
			if (IsCopyPasteContext && CurrentEmployee.SelectSingle()?.ExemptFromOvertimeRules == true)
			{
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.RowUpdating<PRPaymentPTOBank> e)
		{
			if (IsCopyPasteContext && e.NewRow != null && e.Row?.IsActive == false)
			{
				e.NewRow.IsActive = false;
			}
		}

		protected virtual void _(Events.RowInserting<PRPaymentPTOBank> e)
		{
			if (IsCopyPasteContext && !PTOHelper.GetEmployeeBanks(this, Document.Current).Any(item => item.BankID == e.Row?.BankID))
			{
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.RowUpdating<PRPaymentDeduct> e)
		{
			if (IsCopyPasteContext && e.NewRow != null && e.Row?.IsActive == false)
			{
				e.NewRow.IsActive = false;
			}
		}

		protected virtual void _(Events.FieldUpdated<PRPayment, PRPayment.empType> e)
		{
			var newValue = (string)e.NewValue;
			var oldValue = (string)e.OldValue;
			if (EmployeeType.IsSalaried(newValue) && !EmployeeType.IsSalaried(oldValue))
			{
				e.Cache.SetValueExt<PRPayment.applyOvertimeRules>(e.Row, false);
				RevertPaymentOvertimeCalculation(this, Document.Current, Earnings.View);
			}
		}

		protected virtual void _(Events.FieldVerifying<PRPaymentTaxApplicableAmounts.wageTypeID> e)
		{
			if (e.Row == null || e.NewValue != null)
			{
				return;
			}

			e.Cache.RaiseExceptionHandling<PRPaymentTaxApplicableAmounts.wageTypeID>(
				e.Row,
				null,
				new PXSetPropertyException(PXMessages.LocalizeFormat(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<PRPaymentTaxApplicableAmounts.wageTypeID>(e.Cache))));
		}

		protected virtual void _(Events.FieldUpdated<PRPaymentTax, PRPaymentTax.wageBaseGrossAmt> e)
		{
			if (e.Row == null || e.Row.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			e.Row.AdjustedGrossAmount = e.Row.WageBaseGrossAmt;
		}

		protected virtual void _(Events.FieldUpdated<PRPaymentEarning, PRPaymentEarning.amount> e)
		{
			UpdateWageAmountsAllowed(e.Row);
		}

		protected virtual void _(Events.RowDeleted<PRPaymentEarning> e)
		{
			UpdateWageAmountsAllowed(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PRPayment, PRPayment.endDate> e)
		{
			if (e.NewValue == null || e.Row?.PayGroupID == null || e.Row?.FinPeriodID == null)
			{
				return;
			}

			var periodSettings = PRPayGroupPeriod.PK.Find(this, e.Row.PayGroupID, e.Row.FinPeriodID);
			var periodSettingsEndDate = PRPayGroupPeriodEndDateUIAttribute.ResolveEndDateValue(periodSettings.StartDate, periodSettings.EndDate);

			if ((DateTime)e.NewValue != periodSettingsEndDate)
			{
				e.Row.EmpType = EmployeeType.Hourly;
			}
			else if (e.Row.EndDate == periodSettingsEndDate && e.Row.DocType == PayrollType.Final)
			{
				e.Cache.SetDefaultExt<PRPayment.empType>(e.Row);
			}
		}

		protected virtual void _(Events.FieldVerifying<PRPayment, PRPayment.empType> e)
		{
			if (e.Row?.EndDate == null || e.Row?.PayGroupID == null || e.Row?.PayPeriodID == null || e.Row.EmpType == null)
			{
				return;
			}

			var periodSettings = PRPayGroupPeriod.PK.Find(this, e.Row.PayGroupID, e.Row.PayPeriodID);
			var periodSettingsEndDate = PRPayGroupPeriodEndDateUIAttribute.ResolveEndDateValue(periodSettings.StartDate, periodSettings.EndDate);

			if (e.Row.EndDate != periodSettingsEndDate && e.Row.EmpType != EmployeeType.Hourly)
			{
				throw new PXSetPropertyException(Messages.ModifiedPeriodEndNeedsHourlyError);
			}
		}

		protected virtual void _(Events.FieldVerifying<PRPayment, PRPayment.endDate> e)
		{
			if (e.Row?.EndDate == null || e.Row?.PayGroupID == null || e.Row?.PayPeriodID == null || e.NewValue == null)
			{
				return;
			}

			var periodSettings = PRPayGroupPeriod.PK.Find(this, e.Row.PayGroupID, e.Row.PayPeriodID);
			var periodSettingsEndDate = PRPayGroupPeriodEndDateUIAttribute.ResolveEndDateValue(periodSettings.StartDate, periodSettings.EndDate);

			if ((DateTime)e.NewValue < periodSettings.StartDate || (DateTime)e.NewValue > periodSettingsEndDate)
			{
				throw new PXSetPropertyException(Messages.OutOfPeriodDateError);
			}
		}

		#endregion Events

		#region Helpers
		public PRPayment InsertNewPayment(PRPayment payment)
		{
			_AllowUpdatePaymentChildrenRecords = false;
			Document.Current = payment;
			payment = Document.Insert(payment);
			if (payment.FinPeriodID == null)
			{
				throw new PXException(Messages.CantFindPostingPeriod, PRPayGroupPeriodIDAttribute.FormatForError(payment.PayPeriodID));
			}
			UpdatePaymentOvertimeRules(payment.ApplyOvertimeRules ?? true);
			_AllowUpdatePaymentChildrenRecords = true;
			UpdateChildrenRecords(payment);
			Actions.PressSave();
			return payment;
		}

		public virtual void OnEarningDetailInserted(PREarningDetail row)
		{
			Document.Current.Calculated = false;

			AddUnionDeductions(row, true);
			AddProjectDeductions(row, true);
		}

		private void UpdateChildrenRecords(PRPayment row)
		{
			if (!_AllowUpdatePaymentChildrenRecords)
				return;

			RecreateSummaryEarnings(row);
			RecreateSummaryDeductions(row);
			RecreateSummaryTaxes(row);

			if (row.TransactionDate.HasValue)
			{
				foreach (PRPaymentEarning summary in SummaryEarnings.Select())
				{
					UpdateSummaryEarning(this, Document.Current, summary);
				}
				CreatePTOBanks(row);
			}
		}

		public static void DeleteEmptySummaryEarnings(PXView summaryEarningsView, PXCache earningDetailCache)
		{
			foreach (PRPaymentEarning row in summaryEarningsView.SelectMulti().Select(x =>
			{
				if (x is PRPaymentEarning)
				{
					return x as PRPaymentEarning;
				}
				else
				{
					return ((PXResult)x)[0] as PRPaymentEarning;
				}
			}))
			{
				if (row.Amount == 0 && row.MTDAmount == 0 && row.QTDAmount == 0 && row.YTDAmount == 0 &&
					!PXParentAttribute.SelectChildren(earningDetailCache, row, typeof(PRPaymentEarning)).Any())
				{
					summaryEarningsView.Cache.Delete(row);
				}
			}
		}

		/// <summary>
		/// Recalculates earnings summary MTD, QTD, YTD amounts according to current document values
		/// </summary>
		public static void UpdateSummaryEarning(PXGraph graph, PRPayment payment, PRPaymentEarning summaryEarning)
		{
			List<PRYtdEarnings> results = SelectFrom<PRYtdEarnings>.
				Where<PRYtdEarnings.employeeID.IsEqual<PRPayment.employeeID.FromCurrent>
					.And<PRYtdEarnings.typeCD.IsEqual<P.AsString>
					.And<PRYtdEarnings.locationID.IsEqual<P.AsInt>>
					.And<PRYtdEarnings.year.IsEqual<P.AsString>>>>.View.Select(graph, summaryEarning.TypeCD, summaryEarning.LocationID, payment.TransactionDate.Value.Year).Select(x => (PRYtdEarnings)x).ToList();

			if (results.Any())
			{
				summaryEarning.MTDAmount = results.SingleOrDefault(x => x.Month == payment.TransactionDate.Value.Month)?.Amount ?? 0;
				summaryEarning.YTDAmount = results.Sum(x => x.Amount ?? 0);
				var quarterMonths = PRDateTime.GetQuarterMonths(payment.TransactionDate.Value);
				summaryEarning.QTDAmount = results.Join(quarterMonths, result => result.Month, month => month, (result, month) => result).Sum(result => result.Amount ?? 0);
			}
			else
			{
				summaryEarning.MTDAmount = 0;
				summaryEarning.YTDAmount = 0;
				summaryEarning.QTDAmount = 0;
			}
		}

		/// <summary>
		/// Adds Earning Summary rows for amount already accumulated by the Employee in previous pays
		/// </summary>
		/// <param name="row"></param>
		private void RecreateSummaryEarnings(PRPayment row)
		{
			if (row.TransactionDate != null && row.EmployeeID != null)
			{
				SummaryEarnings.Select().ForEach(x => SummaryEarnings.Delete(x));
				foreach (PRYtdEarnings ytdEarning in EmployeeYTDEarnings.Select(row.TransactionDate.Value.Year))
				{
					var summary = new PRPaymentEarning();
					summary.TypeCD = ytdEarning.TypeCD;
					summary.LocationID = ytdEarning.LocationID;
					summary.Amount = 0;
					summary.Hours = 0;
					SummaryEarnings.Insert(summary);
				}
			}
		}

		/// <summary>
		/// Recalculates deduction summary YTD amounts according to current document values
		/// </summary>
		public static void UpdateSummaryDeductions(PXGraph graph, PRPayment payment, PRPaymentDeduct summaryDeduct)
		{
			var result = (PRYtdDeductions)SelectFrom<PRYtdDeductions>
				.Where<PRYtdDeductions.employeeID.IsEqual<PRPayment.employeeID.FromCurrent>
					.And<PRYtdDeductions.codeID.IsEqual<P.AsInt>
					.And<PRYtdDeductions.year.IsEqual<P.AsString>>>>.View.Select(graph, summaryDeduct.CodeID, payment.TransactionDate.Value.Year);
			summaryDeduct.YtdAmount = result?.Amount ?? 0;
			summaryDeduct.EmployerYtdAmount = result?.EmployerAmount ?? 0;
		}

		/// <summary>
		/// Adds deduction Summary rows for amount already accumulated by the Employee in previous pays
		/// </summary>
		/// <param name="row"></param>
		private void RecreateSummaryDeductions(PRPayment row)
		{
			if (row.TransactionDate != null && row.EmployeeID != null)
			{
				Deductions.Select().ForEach(x => Deductions.Delete(x));
				var inserted = new HashSet<int>();

				// 1. Add deductions that are active in the employee as active by default. If pay checks comes from batch and deduction in inactive
				// in batch, add as inactive.
				PXSelectJoin<PREmployeeDeduct,
					InnerJoin<PRDeductCode, On<PRDeductCode.codeID, Equal<PREmployeeDeduct.codeID>>,
					LeftJoin<PRBatchDeduct, On<PRBatchDeduct.codeID, Equal<PREmployeeDeduct.codeID>,
						And<PRBatchDeduct.batchNbr, Equal<Current<PRPayment.payBatchNbr>>>>>>,
					Where<PREmployeeDeduct.isActive, Equal<True>,
						And<PREmployeeDeduct.bAccountID, Equal<Current<PRPayment.employeeID>>,
						And<PRDeductCode.isActive, Equal<True>,
						And<PREmployeeDeduct.startDate, LessEqual<Current<PRPayment.transactionDate>>,
						And<PRDeductCode.countryID, Equal<Current<PRPayment.countryID>>,
						And<Where<PREmployeeDeduct.endDate, GreaterEqual<Current<PRPayment.transactionDate>>,
							Or<PREmployeeDeduct.endDate, IsNull>>>>>>>>>.Select(this).Select(x => (PXResult<PREmployeeDeduct, PRDeductCode, PRBatchDeduct>)x)
					.ForEach(r =>
					{
						int? codeID = ((PRDeductCode)r).CodeID;
						var summary = new PRPaymentDeduct();
						summary.CodeID = codeID;
						summary.IsActive = !(((PRBatchDeduct)r).IsEnabled == false);
						Deductions.Insert(summary);
						inserted.Add(codeID.GetValueOrDefault());
					});

				// 2. Add deductions for which there is a YTD tally as inactive
				foreach (PRYtdDeductions ytdDeduction in EmployeeYTDDeductions.Select(row.TransactionDate.Value.Year))
				{
					var codeID = ytdDeduction.CodeID;

					// YTD values will be set by UpdateSummaryDeductions through CodeID FieldUpdated event
					var summary = new PRPaymentDeduct();
					summary.CodeID = codeID;
					if (!inserted.Contains(codeID.GetValueOrDefault()))
					{
						summary.IsActive = false;
					}
					Deductions.Insert(summary);
				}
			}
		}

		private void RecreateDeductionDetails(PRPaymentDeduct row)
		{
			DeductionDetails.Select().FirstTableItems.Where(x => x.CodeID == row.CodeID)
				.ForEach(x => DeductionDetails.Delete(x));

			PRCalculationEngine.CreateDeductionDetail(this, DeductionDetails.Cache, row, Earnings.Select().FirstTableItems);
		}

		private void RecreateBenefitDetails(PRPaymentDeduct row)
		{
			if (row.NoFinancialTransaction == true)
			{
				return;
			}

			BenefitDetails.Select().FirstTableItems.Where(x => x.CodeID == row.CodeID)
				.ForEach(x => BenefitDetails.Delete(x));

			PRCalculationEngine.CreateBenefitDetail(this, BenefitDetails.Cache, row, Earnings.Select().FirstTableItems);
		}

		private void RecreateTaxDetails(PRPaymentTax summary, int? deleteTaxID)
		{
			TaxDetails.Select().FirstTableItems.Where(x => x.TaxID == deleteTaxID).ForEach(x => TaxDetails.Delete(x));

			PRTaxCode taxCode = (PRTaxCode)PXSelectorAttribute.Select<PRPaymentTax.taxID>(Taxes.Cache, summary);
			PRCalculationEngine.CreateTaxDetail(this, taxCode, summary, Earnings.Select().FirstTableItems);
		}

		/// <summary>
		/// Adds taxes summary rows for amount already accumulated by the Employee in previous pays
		/// </summary>
		/// <param name="row"></param>
		private void RecreateSummaryTaxes(PRPayment row)
		{
			if (row.TransactionDate != null && row.EmployeeID != null)
			{
				Taxes.Select().ForEach(x => Taxes.Delete(x));
				foreach (PRYtdTaxes ytdTaxes in EmployeeYTDTaxes.Select(row.TransactionDate.Value.Year))
				{
					var summary = new PRPaymentTax();
					summary.TaxID = ytdTaxes.TaxID;
					Taxes.Insert(summary);
				}
			}
		}

		private void RecreateWorkCompensationDeductionSummaryAndDetails()
		{
			List<PRPaymentDeduct> wcDeductions = Deductions.Select().FirstTableItems.Where(x => x.Source == PaymentDeductionSourceAttribute.WorkCode).ToList();
			wcDeductions.ForEach(x =>
			{
				x.CntAmount = 0m;
				Deductions.Update(x);
			});

			Dictionary<int?, (decimal deductionAmount, decimal benefitAmount)> premiumTotalsByDeductCode = new Dictionary<int?, (decimal, decimal)>();
			foreach (PRPaymentWCPremium premium in WCPremiums.Select().FirstTableItems.Where(x => x.DeductionAmount != 0 || x.Amount != 0))
			{
				decimal deductionAmount = 0;
				decimal benefitAmount = 0;
				if (premiumTotalsByDeductCode.ContainsKey(premium.DeductCodeID))
				{
					(deductionAmount, benefitAmount) = premiumTotalsByDeductCode[premium.DeductCodeID];
				}
				deductionAmount += premium.DeductionAmount.GetValueOrDefault();
				benefitAmount += premium.Amount.GetValueOrDefault();
				premiumTotalsByDeductCode[premium.DeductCodeID] = (deductionAmount, benefitAmount);
			}

			foreach (KeyValuePair<int?, (decimal deductionAmount, decimal benefitAmount)> kvp in premiumTotalsByDeductCode)
			{
				PRPaymentDeduct deduction = wcDeductions.FirstOrDefault(x => x.CodeID == kvp.Key);
				if (deduction == null)
				{
					deduction = new PRPaymentDeduct()
					{
						CodeID = kvp.Key,
						Source = PaymentDeductionSourceAttribute.WorkCode
					};
					wcDeductions.Add(deduction);
				}
				deduction.IsActive = true;
				deduction.DedAmount = kvp.Value.deductionAmount;
				deduction.CntAmount = kvp.Value.benefitAmount;
				Deductions.Update(deduction);
			}

			wcDeductions.ForEach(x =>
			{
				RecreateDeductionDetails(x);
				RecreateBenefitDetails(x);
			});
		}

		private void RecreateProjectDeductionSummaryAndDetails()
		{
			List<PRPaymentDeduct> projectDeductions = Deductions.Select().FirstTableItems.Where(x => x.Source == PaymentDeductionSourceAttribute.CertifiedProject).ToList();
			projectDeductions.ForEach(x =>
			{
				x.DedAmount = 0m;
				x.CntAmount = 0m;
				Deductions.Update(x);
			});

			Dictionary<int?, (decimal dedAmount, decimal cntAmount)> totalsByDeductCode = new Dictionary<int?, (decimal, decimal)>();
			foreach (IGrouping<int?, PRPaymentProjectPackageDeduct> packageDeductGroup in ProjectPackageDeductions.Select().FirstTableItems
				.Where(x => x.DeductionAmount != 0 || x.BenefitAmount != 0)
				.GroupBy(x => x.RecordID))
			{
				PRPaymentProjectPackageDeduct packageDeduct = packageDeductGroup.First();
				decimal dedAmount = 0m;
				decimal cntAmount = 0m;
				if (totalsByDeductCode.ContainsKey(packageDeduct.DeductCodeID))
				{
					dedAmount = totalsByDeductCode[packageDeduct.DeductCodeID].dedAmount;
					cntAmount = totalsByDeductCode[packageDeduct.DeductCodeID].cntAmount;
				}
				dedAmount += packageDeduct.DeductionAmount.GetValueOrDefault();
				cntAmount += packageDeduct.BenefitAmount.GetValueOrDefault();
				totalsByDeductCode[packageDeduct.DeductCodeID] = (dedAmount, cntAmount);
			}

			foreach (KeyValuePair<int?, (decimal dedAmount, decimal cntAmount)> kvp in totalsByDeductCode)
			{
				PRPaymentDeduct deduction = projectDeductions.FirstOrDefault(x => x.CodeID == kvp.Key);
				if (deduction == null)
				{
					deduction = new PRPaymentDeduct()
					{
						CodeID = kvp.Key,
						Source = PaymentDeductionSourceAttribute.CertifiedProject
					};
					projectDeductions.Add(deduction);
				}
				deduction.IsActive = true;
				deduction.DedAmount = kvp.Value.dedAmount;
				deduction.CntAmount = kvp.Value.cntAmount;
				Deductions.Update(deduction);
			}

			projectDeductions.ForEach(x =>
			{
				RecreateDeductionDetails(x);
				RecreateBenefitDetails(x);
			});
		}

		private void RecreateUnionDeductionSummaryAndDetails()
		{
			List<PRPaymentDeduct> unionDeductions = Deductions.Select().FirstTableItems.Where(x => x.Source == PaymentDeductionSourceAttribute.Union).ToList();
			unionDeductions.ForEach(x =>
			{
				x.DedAmount = 0m;
				x.CntAmount = 0m;
				Deductions.Update(x);
			});

			Dictionary<int?, (decimal dedAmount, decimal cntAmount)> totalsByDeductCode = new Dictionary<int?, (decimal, decimal)>();
			foreach (IGrouping<int?, PRPaymentUnionPackageDeduct> packageDeductGroup in UnionPackageDeductions.Select().FirstTableItems
				.Where(x => x.DeductionAmount != 0 || x.BenefitAmount != 0)
				.GroupBy(x => x.RecordID))
			{
				PRPaymentUnionPackageDeduct packageDeduct = packageDeductGroup.First();
				decimal dedAmount = 0m;
				decimal cntAmount = 0m;
				if (totalsByDeductCode.ContainsKey(packageDeduct.DeductCodeID))
				{
					dedAmount = totalsByDeductCode[packageDeduct.DeductCodeID].dedAmount;
					cntAmount = totalsByDeductCode[packageDeduct.DeductCodeID].cntAmount;
				}
				dedAmount += packageDeduct.DeductionAmount.GetValueOrDefault();
				cntAmount += packageDeduct.BenefitAmount.GetValueOrDefault();
				totalsByDeductCode[packageDeduct.DeductCodeID] = (dedAmount, cntAmount);
			}

			foreach (KeyValuePair<int?, (decimal dedAmount, decimal cntAmount)> kvp in totalsByDeductCode)
			{
				PRPaymentDeduct deduction = unionDeductions.FirstOrDefault(x => x.CodeID == kvp.Key);
				if (deduction == null)
				{
					deduction = new PRPaymentDeduct()
					{
						CodeID = kvp.Key,
						Source = PaymentDeductionSourceAttribute.Union
					};
					unionDeductions.Add(deduction);
				}
				deduction.IsActive = true;
				deduction.DedAmount = kvp.Value.dedAmount;
				deduction.CntAmount = kvp.Value.cntAmount;
				Deductions.Update(deduction);
			}

			unionDeductions.ForEach(x =>
			{
				RecreateDeductionDetails(x);
				RecreateBenefitDetails(x);
			});
		}

		private void CreatePTOBanks(PRPayment row)
		{
			if (row.EmployeeID != null)
			{
				PaymentPTOBanks.Select().ForEach(x => PaymentPTOBanks.Delete(x));
				var banksToInsert = new List<IPTOBank>();
				foreach (IGrouping<string, IPTOBank> group in PTOHelper.GetEmployeeBanks(this, Document.Current).GroupBy(x => x.BankID))
				{
					// Take all banks starting during period
					var banksEffectiveInPeriod = group.OrderByDescending(x => x.StartDate).TakeWhile(x => x.StartDate >= row.StartDate);
					banksToInsert.AddRange(banksEffectiveInPeriod);

					// If no banks starts exatcly at the start of the period, find the latest bank effective before the period
					if (!banksEffectiveInPeriod.Any(x => x.StartDate == row.StartDate))
					{
						var prePeriodEffectiveBank = group.OrderByDescending(x => x.StartDate).Except(banksEffectiveInPeriod).FirstOrDefault();
						if (prePeriodEffectiveBank != null)
						{
							banksToInsert.Add(prePeriodEffectiveBank);
						}
					}
				}

				DateTime paymentStartDate = Document.Current.StartDate.Value;
				DateTime paymentEndDate = Document.Current.EndDate.Value;

				foreach (IPTOBank bank in banksToInsert)
				{
					PTOHelper.GetPTOBankYear(paymentStartDate, bank.PTOYearStartDate.Value, out DateTime paymentStartPTOYearStart, out DateTime paymentStartPTOYearEnd);
					PTOHelper.GetPTOBankYear(paymentEndDate, bank.PTOYearStartDate.Value, out DateTime paymentEndPTOYearStart, out DateTime paymentEndPTOYearEnd);
					var ptoYears = new HashSet<(DateTime, DateTime)>()
					{
						(paymentEndPTOYearStart, paymentEndPTOYearEnd),
						(paymentStartPTOYearStart, paymentStartPTOYearEnd),
					};

					foreach ((DateTime, DateTime) yearSpan in ptoYears)
					{
						var ptoYearStart = yearSpan.Item1;
						var ptoYearEnd = yearSpan.Item2;

						// Skip banks not effective in that PTO year
						if (bank.StartDate > ptoYearEnd)
						{
							continue;
						}
						// Find bank effective start date in payment period
						// If bank effective day is in pto year, use it. Otherwise, use pto year start day.
						var day = ptoYearStart <= bank.StartDate && bank.StartDate <= ptoYearEnd ? bank.StartDate.Value.Day : bank.PTOYearStartDate.Value.Day;
						var effectiveStartDate = bank.StartDate.Value.Year != ptoYearStart.Year ? new DateTime(ptoYearStart.Year, ptoYearStart.Month, day)
							: new DateTime(ptoYearStart.Year, bank.StartDate.Value.Month, bank.StartDate.Value.Day);

						//Find the effectiveEndDate
						PTOHelper.GetPTOBankYear(effectiveStartDate, bank.PTOYearStartDate.Value, out DateTime _, out DateTime effectiveEndDate);
						IEnumerable<IPTOBank> bankGroupEffectiveTheSameYear = PTOHelper.GetEmployeeBanksPerBankIDAndYear(this, Document.Current, bank.BankID, ptoYearStart);
						IPTOBank nextEffectiveBankInTheSameYear = bankGroupEffectiveTheSameYear.Where(x => x.StartDate > effectiveStartDate).OrderBy(x => x.StartDate).FirstOrDefault();

						if (nextEffectiveBankInTheSameYear != null)
						{
							effectiveEndDate = nextEffectiveBankInTheSameYear.StartDate.Value.AddDays(-1);
						}

						var paymentBank = new PRPaymentPTOBank();
						paymentBank.BankID = bank.BankID;
						paymentBank.AccrualMethod = bank.AccrualMethod;
						paymentBank.AccrualRate = bank.AccrualRate;
						paymentBank.HoursPerYear = bank.HoursPerYear;
						paymentBank.AccrualLimit = bank.AccrualLimit;
						paymentBank.EffectiveStartDate = effectiveStartDate;
						paymentBank.EffectiveEndDate = effectiveEndDate;
						var ptoBank = PXSelectorAttribute.Select(Caches[bank.GetType()], bank, nameof(PRPTOBank.bankID)) as PRPTOBank;
						paymentBank.IsCertifiedJob = ptoBank.IsCertifiedJobAccrual;
						paymentBank.IsActive = bank.IsActive == true && ptoBank.IsActive == true;
						
						PTOHelper.PTOHistoricalAmounts history = PTOHelper.GetPTOHistory(this, ptoYearStart, row.EmployeeID.Value, bank);
						paymentBank.AccumulatedAmount = history.AccumulatedHours;
						paymentBank.AccumulatedMoney = history.AccumulatedMoney;
						paymentBank.UsedAmount = history.UsedHours;
						paymentBank.UsedMoney = history.UsedMoney;
						paymentBank.AvailableAmount = history.AvailableHours;
						paymentBank.AvailableMoney = history.AvailableMoney;
						
						paymentBank = PaymentPTOBanks.Insert(paymentBank);
					}
				}
			}
		}

		public static void RevertPaymentOvertimeCalculation(PXGraph graph, PRPayment document, PXView earningDetailView)
		{
			Dictionary<int?, PREarningDetail> paymentEarningDetails = earningDetailView.SelectMulti()
				.Select(x => (PREarningDetail)(x is PXResult pxResult ? pxResult[0] : x))
				.ToDictionary(x => x.RecordID, x => x);
			bool overtimeRecordsExist = false;

			foreach (PREarningDetail overtimeEarningDetail in paymentEarningDetails.Values)
			{
				int? baseOvertimeRecordID = overtimeEarningDetail.BaseOvertimeRecordID;

				if (baseOvertimeRecordID == null)
					continue;

				if (!paymentEarningDetails.TryGetValue(baseOvertimeRecordID, out PREarningDetail baseEarningDetail))
				{
					earningDetailView.Cache.Delete(overtimeEarningDetail);
					PXTrace.WriteWarning(Messages.InconsistentBaseEarningDetailRecord, baseOvertimeRecordID, overtimeEarningDetail.RecordID);
					continue;
				}

				using (PXTransactionScope transactionScope = new PXTransactionScope())
				{
					if (overtimeEarningDetail.IsFringeRateEarning != true)
					{
						baseEarningDetail.Hours = baseEarningDetail.Hours.GetValueOrDefault() + overtimeEarningDetail.Hours.GetValueOrDefault();
						earningDetailView.Cache.Update(baseEarningDetail);
					}

					earningDetailView.Cache.Delete(overtimeEarningDetail);
					transactionScope.Complete(graph);
				}
				overtimeRecordsExist = true;
			}

			if (overtimeRecordsExist)
			{
				document.Calculated = false;
				graph.Actions.PressSave();
			}
		}

		private void AddUnionDeductions(PREarningDetail row, bool forceActive)
		{
			if (string.IsNullOrEmpty(row?.UnionID))
			{
				return;
			}

			foreach (PRDeductionAndBenefitUnionPackage unionDeduction in EarningUnionDeductions.Select(row.UnionID, row.LabourItemID, row.LabourItemID, row.LabourItemID, row.UnionID))
			{
				AddPackageDeduction(unionDeduction.DeductionAndBenefitCodeID, PaymentDeductionSourceAttribute.Union, forceActive);
			}
		}

		private void AddProjectDeductions(PREarningDetail row, bool forceActive)
		{
			if (row?.ProjectID == null || CurrentEmployee.SelectSingle()?.ExemptFromCertifiedReporting == true)
			{
				return;
			}

			foreach (PRDeductionAndBenefitProjectPackage projectDeduction in EarningProjectDeductions.Select(row.ProjectID, row.LabourItemID, row.LabourItemID, row.LabourItemID, row.ProjectID))
			{
				AddPackageDeduction(projectDeduction.DeductionAndBenefitCodeID, PaymentDeductionSourceAttribute.CertifiedProject, forceActive);
			}
		}

		private void AddPackageDeduction(int? deductCodeID, string source, bool forceActive)
		{
			PRPaymentDeduct existingDeduct = Deductions.Select()
				.SingleOrDefault(x => ((PRPaymentDeduct)x).CodeID == deductCodeID && ((PRPaymentDeduct)x).Source == source);

			if (existingDeduct != null)
			{
				existingDeduct.IsActive = (existingDeduct.IsActive == true) || forceActive;
				Deductions.Update(existingDeduct);
			}
			else
			{
				PRPaymentDeduct deduct = new PRPaymentDeduct();
				deduct.CodeID = deductCodeID;
				deduct.IsActive = true;
				deduct.Source = source;
				Deductions.Insert(deduct);
			}
		}

		private void CheckForNegative<TField>(decimal? newValue, string docType, Func<bool> showError = null) where TField : IBqlField
		{
			if ((showError == null || showError()) &&
				newValue < 0 && docType != PayrollType.VoidCheck && docType != PayrollType.Adjustment)
			{
				throw new PXSetPropertyException<TField>(Messages.InvalidNegative, PXErrorLevel.Error);
			}
		}

		private void CheckExistingPaychecksAndBatches<F>(Events.FieldVerifying<F> e, int? employeeID, string payPeriodID) where F : class, IBqlField
		{
			ExistingPayment.Cache.Clear();
			PRPayment existingPayment = GetExistingPayment(employeeID, payPeriodID, (e.Row as PRPayment)?.PayGroupID);
			if (existingPayment != null)
			{
				if (IsCopyPasteContext)
				{
					e.Cancel = true;
					throw new PXException(PXMessages.LocalizeFormat(Messages.EmployeeAlreadyAddedToAnotherPaycheckError, existingPayment.PaymentDocAndRef));
				}

				ExistingPayment.Current.DocType = existingPayment.DocType;
				ExistingPayment.Current.RefNbr = existingPayment.RefNbr;
				ExistingPayment.Current.Message = PXMessages.LocalizeFormat(Messages.EmployeeAlreadyAddedToAnotherPaycheck, existingPayment.PaymentDocAndRef);
				ExistingPayment.AskExt();
				e.NewValue = null;
				e.Cancel = true;
				return;
			}

			ExistingPayrollBatch.Cache.Clear();
			PRBatch existingPayrollBatch = GetExistingPayrollBatch(employeeID, payPeriodID, (e.Row as PRPayment)?.PayGroupID);
			if (existingPayrollBatch != null)
			{
				string warningMessage = PXMessages.LocalizeFormat(Messages.EmployeeAlreadyAddedToBatch, existingPayrollBatch.BatchNbr);
				if (IsCopyPasteContext)
				{
					e.Cache.RaiseExceptionHandling<PRPayment.payPeriodID>(e.Row, payPeriodID, new PXSetPropertyException(warningMessage, PXErrorLevel.Warning));
					return;
				}

				ExistingPayrollBatch.Current.BatchNbr = existingPayrollBatch.BatchNbr;
				ExistingPayrollBatch.Current.Message = warningMessage;
				ExistingPayrollBatch.AskExt();
				return;
			}
		}

		private PRPayment GetExistingPayment(int? employeeID, string payPeriodID, string payGroupID)
		{
			if (employeeID == null || payPeriodID == null
				|| !string.IsNullOrWhiteSpace(Document.Current.PayBatchNbr)
				|| !(Document.Current.DocType == PayrollType.Regular || Document.Current.DocType == PayrollType.Final))
			{
				return null;
			}

			PRPayment existingPayments =
				SelectFrom<PRPayment>.
					Where<PRPayment.refNbr.IsNotEqual<P.AsString>.
						And<Brackets<PRPayment.docType.IsEqual<PayrollType.regular>.
							Or<PRPayment.docType.IsEqual<PayrollType.final>>>>.
						And<PRPayment.payPeriodID.IsEqual<P.AsString>>.
						And<PRPayment.payGroupID.IsEqual<P.AsString>>.
						And<PRPayment.employeeID.IsEqual<P.AsInt>>.
						And<PRPayment.voided.IsNotEqual<True>>>.View.
					SelectSingleBound(this, null, Document.Current.RefNbr, payPeriodID, payGroupID, employeeID);

			return existingPayments;
		}

		private PRBatch GetExistingPayrollBatch(int? employeeID, string payPeriodID, string payGroupID)
		{
			if (Document.Current.DocType != PayrollType.Regular ||
				!string.IsNullOrWhiteSpace(Document.Current.PayBatchNbr) ||
				employeeID == null ||
				string.IsNullOrWhiteSpace(payPeriodID))
			{
				return null;
			}

			PRBatch existingRegularBatchWithSamePayPeriod =
				SelectFrom<PRBatch>.
					InnerJoin<PRBatchEmployee>.On<PRBatch.batchNbr.IsEqual<PRBatchEmployee.batchNbr>>.
					Where<PRBatch.open.IsNotEqual<True>.
						And<PRBatch.closed.IsNotEqual<True>.
						And<PRBatch.payrollType.IsEqual<PayrollType.regular>>.
						And<PRBatch.payPeriodID.IsEqual<P.AsString>>.
						And<PRBatch.payGroupID.IsEqual<P.AsString>>.
						And<PRBatchEmployee.employeeID.IsEqual<P.AsInt>>>>.View.
					SelectSingleBound(this, null, payPeriodID, payGroupID, employeeID);

			return existingRegularBatchWithSamePayPeriod;
		}

		private void UpdateWCPremiumRate(PRPaymentWCPremium row)
		{
			if (!string.IsNullOrEmpty(row.WorkCodeID) && row.DeductCodeID != null && CurrentDocument.Current.DocType != PayrollType.VoidCheck)
			{
				PRWorkCompensationBenefitRate rate = new SelectFrom<PRWorkCompensationBenefitRate>
					.InnerJoin<PMWorkCode>.On<PMWorkCode.workCodeID.IsEqual<PRWorkCompensationBenefitRate.workCodeID>>
					.Where<PRWorkCompensationBenefitRate.workCodeID.IsEqual<P.AsString>
						.And<PRWorkCompensationBenefitRate.deductCodeID.IsEqual<P.AsInt>>
						.And<PMWorkCode.isActive.IsEqual<True>>
						.And<PRWorkCompensationBenefitRate.effectiveDate.IsLessEqual<PRPayment.transactionDate.FromCurrent>>>
					.OrderBy<PRWorkCompensationBenefitRate.effectiveDate.Desc>.View(this).SelectSingle(row.WorkCodeID, row.DeductCodeID);

				if (rate != null)
				{
					WCPremiums.Cache.SetValueExt<PRPaymentWCPremium.deductionRate>(row, rate.DeductionRate);
					WCPremiums.Cache.SetValueExt<PRPaymentWCPremium.rate>(row, rate.Rate);
				}
			}
		}

		public static void DefaultDescription(PXCache cache, PRPayment payment)
		{
			if (!string.IsNullOrWhiteSpace(payment.DocDesc))
			{
				return;
			}

			PRSetup payrollSettings = PRSetupHelper.GetPayrollPreferences(cache.Graph);
			if (payrollSettings?.HideEmployeeInfo == true)
			{
				payment.DocDesc = PXMessages.LocalizeFormatNoPrefix(Messages.DefaultPaymentDescriptionWithHiddenNameFormat, payment.PaymentDocAndRef, PRPayGroupPeriodIDAttribute.FormatForError(payment.PayPeriodID));
				return;
			}

			EPEmployee employee = (EPEmployee)PXSelectorAttribute.Select<PRPayment.employeeID>(cache, payment);
			payment.DocDesc = PXMessages.LocalizeFormatNoPrefix(Messages.DefaultPaymentDescriptionFormat, employee.AcctName, PRPayGroupPeriodIDAttribute.FormatForError(payment.PayPeriodID));

			int maxDocDescLength = cache.GetAttributesOfType<PXDBStringAttribute>(payment, nameof(PRPayment.DocDesc)).First().Length;

			if (payment.DocDesc.Length > maxDocDescLength)
			{
				int maxAcctNameLength = maxDocDescLength - payment.DocDesc.Length + employee.AcctName.Length;

				if (maxAcctNameLength > 0)
				{
					string acctName = employee.AcctName.Substring(0, maxAcctNameLength);
					payment.DocDesc = PXMessages.LocalizeFormatNoPrefix(Messages.DefaultPaymentDescriptionFormat, acctName, PRPayGroupPeriodIDAttribute.FormatForError(payment.PayPeriodID));
				}
				else
				{
					payment.DocDesc = payment.DocDesc.Substring(0, maxDocDescLength);
				}
			}
		}

		private void SetCostAssignmentType(PRPayment payment)
		{
			PRCalculationEngine.PRCalculationEngineUtils calculationUtils = PXGraph.CreateInstance<PRCalculationEngine.PRCalculationEngineUtils>();

			DetailSplitType splitType = calculationUtils.GetExpenseSplitSettings(
				PRSetup.Cache,
				PRSetup.Current,
				typeof(PRSetup.earningsAcctDefault),
				typeof(PRSetup.earningsSubMask),
				PREarningsAcctSubDefault.MaskEarningType,
				PREarningsAcctSubDefault.MaskLaborItem);
			bool earningsAssignedToProject = PRSetup.Current.ProjectCostAssignment != ProjectCostAssignmentType.NoCostAssigned;
			bool earningsAssignedToLaborItem = splitType.SplitByLaborItem;
			bool earningsAssignedToEarningType = splitType.SplitByEarningType;
			CostAssignmentSetting earningSetting = new CostAssignmentSetting(earningsAssignedToProject, earningsAssignedToLaborItem, earningsAssignedToEarningType);

			splitType = calculationUtils.GetExpenseSplitSettings(
				PRSetup.Cache,
				PRSetup.Current,
				typeof(PRSetup.benefitExpenseAcctDefault),
				typeof(PRSetup.benefitExpenseSubMask),
				PRBenefitExpenseAcctSubDefault.MaskEarningType,
				PRBenefitExpenseAcctSubDefault.MaskLaborItem);
			bool benefitsAssignedToProject = PRSetup.Current.ProjectCostAssignment == ProjectCostAssignmentType.WageLaborBurdenAssigned;
			bool benefitsAssignedToLaborItem = splitType.SplitByLaborItem;
			bool benefitsAssignedToEarningType = splitType.SplitByEarningType;
			CostAssignmentSetting benefitSetting = new CostAssignmentSetting(benefitsAssignedToProject, benefitsAssignedToLaborItem, benefitsAssignedToEarningType);

			splitType = calculationUtils.GetExpenseSplitSettings(
				PRSetup.Cache,
				PRSetup.Current,
				typeof(PRSetup.taxExpenseAcctDefault),
				typeof(PRSetup.taxExpenseSubMask),
				PRTaxExpenseAcctSubDefault.MaskEarningType,
				PRTaxExpenseAcctSubDefault.MaskLaborItem);
			bool taxesAssignedToProject = PRSetup.Current.ProjectCostAssignment == ProjectCostAssignmentType.WageLaborBurdenAssigned;
			bool taxesAssignedToLaborItem = splitType.SplitByLaborItem;
			bool taxesAssignedToEarningType = splitType.SplitByEarningType;
			CostAssignmentSetting taxSetting = new CostAssignmentSetting(taxesAssignedToProject, taxesAssignedToLaborItem, taxesAssignedToEarningType);

			splitType = calculationUtils.GetExpenseSplitSettings(
				PRSetup.Cache,
				PRSetup.Current,
				typeof(PRSetup.ptoExpenseAcctDefault),
				typeof(PRSetup.ptoExpenseSubMask),
				PRPTOExpenseAcctSubDefault.MaskEarningType,
				PRPTOExpenseAcctSubDefault.MaskLaborItem);
			bool ptoAssignedToProject = PRSetup.Current.ProjectCostAssignment == ProjectCostAssignmentType.WageLaborBurdenAssigned;
			bool ptoAssignedToLaborItem = splitType.SplitByLaborItem;
			bool ptoAssignedToEarningType = splitType.SplitByEarningType;
			CostAssignmentSetting ptoSetting = new CostAssignmentSetting(ptoAssignedToProject, ptoAssignedToLaborItem, ptoAssignedToEarningType);

			payment.LaborCostSplitType = CostAssignmentType.GetLaborCostSplitCode(earningSetting, benefitSetting, taxSetting);
			payment.PTOCostSplitType = CostAssignmentType.GetSingleSplitCode(ptoSetting).ToString();
			Document.Update(payment);
		}

		private void AdjustDeductionSummary(int? codeID)
		{
			PRPaymentDeduct paymentDeduct = Deductions.Select().FirstTableItems.FirstOrDefault(x => x.CodeID == codeID);
			decimal? detailTotalAmount = DeductionDetails.Select().FirstTableItems.Where(x => x.CodeID == codeID).Sum(x => x.Amount);
			if (detailTotalAmount != paymentDeduct?.DedAmount)
			{
				paymentDeduct = paymentDeduct ??
					new PRPaymentDeduct()
					{
						CodeID = codeID
					};
				paymentDeduct.IsActive = true;
				if (string.IsNullOrEmpty(paymentDeduct.Source) || paymentDeduct.Source == DeductionSourceListAttribute.EmployeeSettings)
				{
					paymentDeduct.SaveOverride = true;
				}
				paymentDeduct.DedAmount = detailTotalAmount;
				Deductions.Update(paymentDeduct);
			}
		}

		private void AdjustBenefitSummary(int? codeID)
		{
			PRPaymentDeduct paymentDeduct = Deductions.Select().FirstTableItems.FirstOrDefault(x => x.CodeID == codeID);
			decimal? detailTotalAmount = BenefitDetails.Select().FirstTableItems.Where(x => x.CodeID == codeID).Sum(x => x.Amount);
			if (detailTotalAmount != paymentDeduct?.CntAmount)
			{
				paymentDeduct = paymentDeduct ??
					new PRPaymentDeduct()
					{
						CodeID = codeID
					};
				paymentDeduct.IsActive = true;
				if (string.IsNullOrEmpty(paymentDeduct.Source) || paymentDeduct.Source == DeductionSourceListAttribute.EmployeeSettings)
				{
					paymentDeduct.SaveOverride = true;
				}
				paymentDeduct.CntAmount = detailTotalAmount;
				Deductions.Update(paymentDeduct);
			}
		}

		private void AdjustTaxSummary(int? taxID)
		{
			if (taxID == null)
			{
				return;
			}

			PRPaymentTax paymentTax = Taxes.Select().FirstTableItems.FirstOrDefault(x => x.TaxID == taxID);
			decimal? detailTotalAmount = TaxDetails.Select().FirstTableItems.Where(x => x.TaxID == taxID).Sum(x => x.Amount);
			if (detailTotalAmount != paymentTax?.TaxAmount)
			{
				paymentTax = paymentTax ??
					new PRPaymentTax()
					{
						TaxID = taxID
					};
				paymentTax.TaxAmount = detailTotalAmount;
				Taxes.Update(paymentTax);
			}
		}

		private void VerifyTaxDetails(List<PRPaymentTax> taxSummaries, List<PRTaxDetail> taxDetails, int? taxID, bool throwException)
		{
			PRPaymentTax taxSummary = taxSummaries.FirstOrDefault(x => x.TaxID == taxID);
			List<PRTaxDetail> matchingTaxDetails = taxDetails.Where(x => x.TaxID == taxID).ToList();
			decimal? detailTotalAmount = matchingTaxDetails.Sum(x => x.Amount);

			if (detailTotalAmount != (taxSummary?.TaxAmount ?? 0m))
			{
				string taxCD = taxID.ToString();
				if (taxSummary != null)
				{
					taxCD = (PXSelectorAttribute.Select<PRPaymentTax.taxID>(Taxes.Cache, taxSummary) as PRTaxCode).TaxCD;
				}
				else
				{
					PRTaxDetail matchingTaxDetail = matchingTaxDetails.FirstOrDefault();
					if (matchingTaxDetail != null)
					{
						taxCD = (PXSelectorAttribute.Select<PRTaxDetail.taxID>(TaxDetails.Cache, matchingTaxDetail) as PRTaxCode).TaxCD;
					}
				}

				if (throwException)
				{
					throw new PXException(Messages.TaxDetailSumDoesntMatch, taxCD, taxSummary.TaxAmount);
				}
				else
				{
					foreach (PRTaxDetail detail in matchingTaxDetails)
					{
						detail.AmountErrorMessage = PXMessages.LocalizeFormat(Messages.TaxDetailSumDoesntMatch, taxCD, taxSummary.TaxAmount);
						TaxDetails.Cache.Update(detail);
						TaxDetails.View.RequestRefresh();
					}
				}
			}
			else
			{
				foreach (PRTaxDetail detail in matchingTaxDetails)
				{
					detail.AmountErrorMessage = null;
					TaxDetails.Cache.Update(detail);
					TaxDetails.View.RequestRefresh();
				}
			}
		}

		private void DefaultBenefitExpenseAcctSub(PXCache cache, PRBenefitDetail row)
		{
			if (row.ExpenseAccountID == null)
			{
				cache.SetDefaultExt<PRBenefitDetail.expenseAccountID>(row);
			}

			if (row.ExpenseSubID == null)
			{
				cache.SetDefaultExt<PRBenefitDetail.expenseSubID>(row);
			}
		}

		private void DefaultTaxExpenseAcctSub(PXCache cache, PRTaxDetail row)
		{
			if (row.ExpenseAccountID == null)
			{
				cache.SetDefaultExt<PRTaxDetail.expenseAccountID>(row);
			}

			if (row.ExpenseSubID == null)
			{
				cache.SetDefaultExt<PRTaxDetail.expenseSubID>(row);
			}
		}

		private bool IsCalculateActionEnabled(PRPayment payment)
		{
			switch (payment.DocType)
			{
				case PayrollType.VoidCheck:
					return false;
				case PayrollType.Final:
					return payment.TransactionDate != null
						&& payment.Hold == false
						&& payment.Released == false
						&& payment.Paid == false;
				default:
					return payment.TransactionDate != null
						&& payment.Hold == false
						&& payment.Released == false
						&& payment.Paid == false
						&& payment.GrossAmount > 0;
			}
		}

		public static bool IsReleaseActionEnabled(PRPayment payment, bool updateGL)
		{
			if (payment.Hold != false || payment.Released != false || payment.TransactionDate == null)
				return false;

			if (payment.EmployeeID == null || payment.PaymentMethodID == null || payment.CashAccountID == null)
				return false;

			switch (payment.DocType)
			{
				case PayrollType.VoidCheck:
					return true;
				case PayrollType.Adjustment:
					return payment.PaymentBatchNbr == null || payment.Paid == true;
				case PayrollType.Regular:
				case PayrollType.Special:
				case PayrollType.Final:
				{
					if (payment.Calculated == false)
					{
						return false;
					}
					else if (payment.Paid == true && payment.GrossAmount > 0)
					{
						return true;
					}
					break;
				}
			}

			return !updateGL && (payment.GrossAmount <= 0 || payment.Calculated == true);
		}

		public virtual bool GetChildDeductRecordsWithSourceNotMatching(
			out List<PRPaymentDeduct> paymentDeducts,
			out List<PRPaymentProjectPackageDeduct> projectPackages,
			out List<PRPaymentUnionPackageDeduct> unionPackages,
			out List<PRPaymentWCPremium> workCodePackages)
		{
			paymentDeducts = Deductions.Select()
				.Select(x => (PXResult<PRPaymentDeduct, PRDeductCode>)x)
				.Where(x => !DeductionMatchesSource(x, ((PRPaymentDeduct)x).Source))
				.Select(x => (PRPaymentDeduct)x)
				.ToList();
			projectPackages = ProjectPackageDeductions.Select()
				.Select(x => (PXResult<PRPaymentProjectPackageDeduct, PRDeductCode>)x)
				.Where(x => !DeductionMatchesSource(x, DeductionSourceListAttribute.CertifiedProject))
				.Select(x => (PRPaymentProjectPackageDeduct)x)
				.ToList();
			unionPackages = UnionPackageDeductions.Select()
				.Select(x => (PXResult<PRPaymentUnionPackageDeduct, PRDeductCode>)x)
				.Where(x => !DeductionMatchesSource(x, DeductionSourceListAttribute.Union))
				.Select(x => (PRPaymentUnionPackageDeduct)x)
				.ToList();
			workCodePackages = WCPremiums.Select()
				.Select(x => (PXResult<PRPaymentWCPremium, PMWorkCode, PRDeductCode>)x)
				.Where(x => !DeductionMatchesSource(x, DeductionSourceListAttribute.WorkCode))
				.Select(x => (PRPaymentWCPremium)x)
				.ToList();

			return paymentDeducts.Any() || projectPackages.Any() || unionPackages.Any() || workCodePackages.Any();
		}

		public virtual void DeleteChildDeductRecordsWithSourceNotMatching(
			List<PRPaymentDeduct> paymentDeducts,
			List<PRPaymentProjectPackageDeduct> projectPackages,
			List<PRPaymentUnionPackageDeduct> unionPackages,
			List<PRPaymentWCPremium> workCodePackages)
		{
			HashSet<int?> affectedDeductCodeIds = new HashSet<int?>();

			foreach (PRPaymentDeduct paymentDeduct in paymentDeducts)
			{
				Deductions.Delete(paymentDeduct);
				affectedDeductCodeIds.Add(paymentDeduct.CodeID);
			}

			foreach (PRPaymentProjectPackageDeduct projectPackage in projectPackages)
			{
				ProjectPackageDeductions.Delete(projectPackage);
				affectedDeductCodeIds.Add(projectPackage.DeductCodeID);
			}

			foreach (PRPaymentUnionPackageDeduct unionPackage in unionPackages)
			{
				UnionPackageDeductions.Delete(unionPackage);
				affectedDeductCodeIds.Add(unionPackage.DeductCodeID);
			}

			foreach (PRPaymentWCPremium workCodePackage in workCodePackages)
			{
				WCPremiums.Delete(workCodePackage);
				affectedDeductCodeIds.Add(workCodePackage.DeductCodeID);
			}

			foreach (PRDeductionDetail deductionDetail in DeductionDetails.Select().FirstTableItems.Where(x => affectedDeductCodeIds.Contains(x.CodeID)))
			{
				DeductionDetails.Delete(deductionDetail);
			}

			foreach (PRBenefitDetail benefitDetail in BenefitDetails.Select().FirstTableItems.Where(x => affectedDeductCodeIds.Contains(x.CodeID)))
			{
				BenefitDetails.Delete(benefitDetail);
			}

			Document.Current.Calculated = false;
		}

		protected virtual bool DeductionMatchesSource(PRDeductCode deductCode, string source)
		{
			return DeductionSourceListAttribute.GetSource(deductCode) == source;
		}

		protected virtual PRPayGroupPeriod GetPayGroupPeriod(string payGroupID, string payPeriodID)
		{
			return SelectFrom<PRPayGroupPeriod>
				.Where<PRPayGroupPeriod.payGroupID.IsEqual<P.AsString>
					.And<PRPayGroupPeriod.finPeriodID.IsEqual<P.AsString>>>
				.View.SelectSingleBound(this, null, payGroupID, payPeriodID);
		}

		protected virtual void UpdateWageAmountsAllowed(PRPaymentEarning row)
		{
			if (row == null || row.DocType != PayrollType.Adjustment || Document.Current.CountryID != LocationConstants.CanadaCountryCode)
			{
				return;
			}

			List<PRPaymentTax> taxesInUse = Taxes.SelectMain().Where(x => x.WageBaseGrossAmt != 0).ToList();
			if (!taxesInUse.Any())
			{
				return;
			}

			EPEarningType earningType = new SelectFrom<EPEarningType>.Where<EPEarningType.typeCD.IsEqual<P.AsString>>.View(this).SelectSingle(row.TypeCD);
			PREarningType prEarningType = PXCache<EPEarningType>.GetExtension<PREarningType>(earningType);
			int? wageTypeID = prEarningType.WageTypeCDCAN;
			bool? isSupplemental = prEarningType.IsSupplementalCAN;
			decimal? amountSumForWageType = SelectFrom<PRPaymentEarning>
				.InnerJoin<EPEarningType>.On<PRPaymentEarning.FK.EarningType>
				.Where<PRPaymentEarning.FK.Payment.SameAsCurrent
					.And<PREarningType.wageTypeCDCAN.IsEqual<P.AsInt>>
					.And<PREarningType.isSupplementalCAN.IsEqual<P.AsBool>>>.View.Select(this, wageTypeID, isSupplemental).FirstTableItems.Sum(x => x.Amount);

			List<PRPaymentTaxApplicableAmounts> wageAmountsAllowedRecords = PaymentTaxApplicableAmounts.SelectMain()
				.Where(x => x.WageTypeID == wageTypeID && x.IsSupplemental == isSupplemental).ToList();
			if (wageAmountsAllowedRecords.Any())
			{
				foreach (PRPaymentTaxApplicableAmounts wageAmountsAllowed in wageAmountsAllowedRecords)
				{
					wageAmountsAllowed.AmountAllowed = amountSumForWageType;
					PaymentTaxApplicableAmounts.Update(wageAmountsAllowed);
				} 
			}
			else if (amountSumForWageType != 0 && wageTypeID != WageType.Default)
			{
				foreach (int? taxID in taxesInUse.Select(x => x.TaxID))
				{
					PaymentTaxApplicableAmounts.Insert(new PRPaymentTaxApplicableAmounts()
					{
						TaxID = taxID,
						WageTypeID = wageTypeID,
						IsSupplemental = isSupplemental,
						AmountAllowed = amountSumForWageType
					});
				}
			}
		}

		public virtual void PreparePaymentsForPrint(List<PRPayment> list)
		{
			foreach (PRPayment payment in list)
			{
				PXProcessing.SetCurrentItem(payment);
				Document.Current = payment;
				Persist();
				PRValidatePaycheckTotals validationGraph = CreateInstance<PRValidatePaycheckTotals>();
				validationGraph.ValidateTotals(Document.Current, false);
				SetCostAssignmentType(payment);
				Actions.PressSave();

				if (payment.DocType == PayrollType.Adjustment && payment.Calculated != true)
				{
					PRCalculationEngine calculationEngine = CreateInstance<PRCalculationEngine>();
					calculationEngine.SetDirectDepositSplit(payment);
					calculationEngine.Persist();
				}
			}
		}
		
		protected virtual void SetEarningFinancialPTOFlags(PREarningDetail row)
		{
			Dictionary<string, PXResult<PRPaymentPTOBank, PRPTOBank, PREmployee, PREmployeePTOBank, PREmployeeClassPTOBank>> _ = null;
			SetEarningFinancialPTOFlags(row, ref _);
		}

		protected virtual void SetEarningFinancialPTOFlags(
			PREarningDetail row,
			ref Dictionary<string, PXResult<PRPaymentPTOBank, PRPTOBank, PREmployee, PREmployeePTOBank, PREmployeeClassPTOBank>> financialPTOBanks)
		{
			row.PTODisbursementWithFinancialTransaction = false;
			row.PTODisbursementWithAverageRate = false;

			if (Document.Current.CountryID != LocationConstants.CanadaCountryCode)
			{
				return;
			}

			if (financialPTOBanks == null)
			{
				financialPTOBanks = FinancialPTOBanks.Select().ToList()
					.Select(x => (PXResult<PRPaymentPTOBank, PRPTOBank, PREmployee, PREmployeePTOBank, PREmployeeClassPTOBank>)x)
					.ToDictionary(k => ((PRPTOBank)k).EarningTypeCD, v => v);
			}

			if (!string.IsNullOrEmpty(row.TypeCD) && financialPTOBanks.ContainsKey(row.TypeCD))
			{
				PXResult<PRPaymentPTOBank, PRPTOBank, PREmployee, PREmployeePTOBank, PREmployeeClassPTOBank> bankResult = financialPTOBanks[row.TypeCD];
				IPTOBank sourceBank = PTOHelper.GetSourceBank(bankResult, bankResult, bankResult);
				row.PTODisbursementWithAverageRate = sourceBank.DisbursingType == PTODisbursingType.AverageRate;
				row.PTODisbursementWithFinancialTransaction = true;
			}
		}

		protected virtual void DeleteCalculatedData()
		{
			Taxes.Select().ForEach(x => Taxes.Delete(x));
			PaymentTaxApplicableAmounts.Select().ForEach(x => PaymentTaxApplicableAmounts.Delete(x));
			DeductionDetails.Select().FirstTableItems.ForEach(x => DeductionDetails.Delete(x));
			BenefitDetails.Select().FirstTableItems.ForEach(x => BenefitDetails.Delete(x));
			WCPremiums.Select().FirstTableItems.ForEach(x => WCPremiums.Delete(x));
			ProjectPackageDeductions.Select().FirstTableItems.ForEach(x => ProjectPackageDeductions.Delete(x));
			UnionPackageDeductions.Select().FirstTableItems.ForEach(x => UnionPackageDeductions.Delete(x));
			PaymentFringeBenefits.Select().FirstTableItems.ForEach(x => PaymentFringeBenefits.Delete(x));
			AllPaymentFringeBenefitsDecreasingRate.Select().FirstTableItems.ForEach(x => AllPaymentFringeBenefitsDecreasingRate.Delete(x));
			AllPaymentFringeEarningsDecreasingRate.Select().FirstTableItems.ForEach(x => AllPaymentFringeEarningsDecreasingRate.Delete(x));
		}

		public virtual bool IsPaid(PRPayment row)
		{
			return row.Released == true || row.Paid == true;
		}

		public virtual bool ShouldEnableDetailEdit(PRPayment row)
		{
			return row.DocType == PayrollType.Adjustment && !IsPaid(row);
		}

		public virtual bool IsReadyForInput(PRPayment row)
		{
			return row.TransactionDate != null && row.EmployeeID != null;
		}

		public virtual void InsertRecordOfEmployment(PXGraph graph, PRPayment payment)
		{
			PRRecordOfEmployment roe = new PRRecordOfEmployment();

			roe.Amendment = false;
			roe.OrigDocType = payment.DocType;
			roe.OrigRefNbr = payment.RefNbr;
			roe.Status = ROEStatus.Open;
			roe.PeriodType = PRPayGroupYearSetup.PK.Find(graph, payment.PayGroupID).PeriodType;
			roe.ReasonForROE = ROEReason.GetROEReason(payment.TerminationReason);
			roe.EmployeeID = payment.EmployeeID;

			EPEmployee employee = EPEmployee.PK.Find(graph, payment.EmployeeID);
			roe.DocDesc = PXMessages.LocalizeFormatNoPrefix(Messages.DefaultRoeDescriptionFormat, employee.AcctName, PRPayGroupPeriodIDAttribute.FormatForError(payment.PayPeriodID));

			BAccount bAccount = PXSelectJoin<BAccountR,
				InnerJoin<Branch, On<Branch.bAccountID, Equal<BAccountR.bAccountID>>>,
				Where<Branch.branchID, Equal<Required<Branch.branchID>>>>.Select(graph, payment.BranchID);

			roe.BranchID = payment.BranchID;
			roe.AddressID = bAccount.DefAddressID;
			roe.CRAPayrollAccountNumber = PXCache<BAccount>.GetExtension<PRxBAccount>(bAccount)?.CRAPayrollAccountNumber;

			roe.FirstDayWorked = EmploymentHistoryHelper.GetEmploymentDates(graph, payment.EmployeeID, payment.StartDate).ContinuousHireDate;
			roe.LastDayForWhichPaid = payment.TerminationDate;
			roe.FinalPayPeriodEndingDate = payment.EndDate;

			PRRecordOfEmploymentMaint roeGraph = PXGraph.CreateInstance<PRRecordOfEmploymentMaint>();
			roe = roeGraph.Document.Insert(roe);
			roeGraph.Actions.PressSave();

			roeGraph.Document.Current = PRRecordOfEmployment.PK.Find(roeGraph, roe.RefNbr);

			List<string> vacationTypes = SelectFrom<EPEarningType>
				.Where<PREarningType.isPTO.IsEqual<True>.And<PREarningType.publicHoliday.IsEqual<False>>>.View.Select(graph).FirstTableItems.Select(item => item.TypeCD).ToList();

			roe.VacationPay = SelectFrom<PRPaymentEarning>
				.Where<PRPaymentEarning.docType.IsEqual<PRPayment.docType.FromCurrent>
					.And<PRPaymentEarning.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>>
				.View.Select(graph, payment).FirstTableItems.Where(item => vacationTypes.Contains(item.TypeCD)).Sum(item => item.Amount);

			string year = FinPeriodUtils.FiscalYear(payment.PayPeriodID);

			var insurableEarnings = SelectFrom<PRPaymentTax>
				.InnerJoin<PRPayment>
					.On<PRPaymentTax.FK.Payment>
				.InnerJoin<PRPayGroupPeriod>
					.On<PRPayGroupPeriod.payGroupID.IsEqual<PRPayment.payGroupID>
						.And<PRPayGroupPeriod.finPeriodID.IsEqual<PRPayment.payPeriodID>>>
				.Where<PRPayGroupPeriod.finYear.IsEqual<P.AsString>
					.And<PRPayGroupPeriod.finPeriodID.IsLessEqual<P.AsString>>
					.And<PRPayment.employeeID.IsEqual<P.AsInt>>
					.And<PRPayment.released.IsEqual<True>>
					.And<PRPayment.voided.IsEqual<False>>
					.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>
				.AggregateTo<GroupBy<PRPayGroupPeriod.finPeriodID>, Max<PRPaymentTax.wageBaseHours>, Max<PRPaymentTax.wageBaseGrossAmt>>
				.View.Select(graph, year, payment.PayPeriodID, payment.EmployeeID);

			decimal totalInsurableHours = 0m;
			decimal totalInsurableEarnings = 0m;
			foreach (PXResult<PRPaymentTax, PRPayment, PRPayGroupPeriod> record in insurableEarnings)
			{
				PRPayGroupPeriod payGroupPeriod = record;
				PRPaymentTax paymentTax = record;

				totalInsurableHours += (paymentTax.WageBaseHours ?? 0m);
				totalInsurableEarnings += (paymentTax.WageBaseGrossAmt ?? 0m);

				PRROEInsurableEarningsByPayPeriod insurableEarningsByPayPeriod = new PRROEInsurableEarningsByPayPeriod();
				insurableEarningsByPayPeriod.RefNbr = roe.RefNbr;
				insurableEarningsByPayPeriod.PayPeriodID = payGroupPeriod.FinPeriodID;
				insurableEarningsByPayPeriod.InsurableHours = (paymentTax.WageBaseHours ?? 0m);
				insurableEarningsByPayPeriod.InsurableEarnings = (paymentTax.WageBaseGrossAmt ?? 0m);
				roeGraph.InsurableEarnings.Insert(insurableEarningsByPayPeriod);
			}

			roe.TotalInsurableHours = totalInsurableHours;
			roe.TotalInsurableEarnings = totalInsurableEarnings;
			roeGraph.Document.Update(roe);

			PREarningDetail[] statutoryHolidays = SelectFrom<PREarningDetail>
				.InnerJoin<PRPayment>
					.On<PREarningDetail.FK.Payment>
				.InnerJoin<EPEarningType>
					.On<PREarningDetail.FK.EarningType>
				.Where<PREarningType.isPTO.IsEqual<True>
					.And<PREarningType.publicHoliday.IsEqual<True>>
					.And<PRPayment.docType.IsEqual<P.AsString>>
					.And<PRPayment.refNbr.IsEqual<P.AsString>>>
				.View.Select(graph, payment.DocType, payment.RefNbr).FirstTableItems.ToArray();

			foreach (PREarningDetail statutoryHoliday in statutoryHolidays)
			{
				PRROEStatutoryHolidayPay statutoryHolidayPay = new PRROEStatutoryHolidayPay();
				statutoryHolidayPay.RefNbr = roe.RefNbr;
				statutoryHolidayPay.Date = statutoryHoliday.Date;
				statutoryHolidayPay.Amount = statutoryHoliday.Amount;
				roeGraph.StatutoryHolidays.Cache.Insert(statutoryHolidayPay);
			}

			PREarningDetail[] otherMonies = SelectFrom<PREarningDetail>
				.InnerJoin<PRPayment>
					.On<PREarningDetail.FK.Payment>
				.InnerJoin<EPEarningType>
					.On<PREarningDetail.FK.EarningType>
				.Where<PREarningType.isAmountBased.IsEqual<True>
					.And<PRPayment.docType.IsEqual<P.AsString>>
					.And<PRPayment.refNbr.IsEqual<P.AsString>>>
				.View.Select(graph, payment.DocType, payment.RefNbr).FirstTableItems.ToArray();

			foreach (PREarningDetail otherMoney in otherMonies)
			{
				PRROEOtherMonies otherMoneyPay = new PRROEOtherMonies();
				otherMoneyPay.RefNbr = roe.RefNbr;
				otherMoneyPay.TypeCD = otherMoney.TypeCD;
				otherMoneyPay.Amount = otherMoney.Amount;
				roeGraph.OtherMonies.Cache.Insert(otherMoneyPay);
			}

			roeGraph.Actions.PressSave();
		}

		#endregion Helpers

		public virtual void VoidCheckProc(PRPayment doc)
		{
			Clear(PXClearOption.PreserveTimeStamp);
			Document.View.Answer = WebDialogResult.No;

			foreach (PXResult<PRPayment, CurrencyInfo> res in PRPayment_CurrencyInfo.Select(this, doc.DocType, doc.RefNbr))
			{
				doc = res;
				CurrencyInfo info = PXCache<CurrencyInfo>.CreateCopy(res);
				info.CuryInfoID = null;
				info.IsReadOnly = false;
				info = PXCache<CurrencyInfo>.CreateCopy(CurrencyInfo.Insert(info));

				var payment = new PRPayment
				{
					DocType = PayrollType.VoidCheck,
					RefNbr = doc.RefNbr,
					CuryInfoID = info.CuryInfoID,
				};
				Document.Insert(payment);

				payment = PXCache<PRPayment>.CreateCopy(res);
				payment.DocType = PayrollType.VoidCheck;
				payment.CuryInfoID = info.CuryInfoID;
				payment.CATranID = null;
				payment.Released = false;
				payment.NoteID = Guid.NewGuid();
				//Set original document reference
				payment.OrigDocType = doc.DocType;
				payment.OrigRefNbr = doc.RefNbr;

				Document.Cache.SetDefaultExt<PRPayment.hold>(payment);
				payment.BatchNbr = null;
				payment.TotalEarnings = 0;
				payment.GrossAmount = 0;
				payment.DedAmount = 0;
				payment.TaxAmount = 0;
				payment.PayableBenefitAmount = 0;
				payment.BenefitAmount = 0;
				payment.EmployerTaxAmount = 0;
				payment.TotalHours = 0;
				payment = Document.Update(payment);

				if (info != null)
				{
					CurrencyInfo b_info = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<PRPayment.curyInfoID>>>>.Select(this);
					b_info.CuryID = info.CuryID;
					b_info.CuryEffDate = info.CuryEffDate;
					b_info.CuryRateTypeID = info.CuryRateTypeID;
					b_info.CuryRate = info.CuryRate;
					b_info.RecipRate = info.RecipRate;
					b_info.CuryMultDiv = info.CuryMultDiv;
					CurrencyInfo.Update(b_info);
				}
			}

			foreach (PREarningDetail earningDetail in SelectFrom<PREarningDetail>.
				Where<PREarningDetail.paymentDocType.IsEqual<P.AsString>.
					And<PREarningDetail.paymentRefNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PREarningDetail copy = EarningDetailHelper.CreateEarningDetailCopy(Earnings.Cache, earningDetail);
				copy.PaymentDocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.BatchNbr = null;
				copy.Hours = -1 * copy.Hours;
				copy.Units = -1 * copy.Units;
				copy.Amount = -1 * copy.Amount;
				copy = Earnings.Update(copy);
			}

			foreach (PRDeductionDetail deductionDetail in SelectFrom<PRDeductionDetail>.
				Where<PRDeductionDetail.paymentDocType.IsEqual<P.AsString>.
					And<PRDeductionDetail.paymentRefNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRDeductionDetail copy = PXCache<PRDeductionDetail>.CreateCopy(deductionDetail);
				copy.RecordID = null;
				copy.OriginalRecordID = deductionDetail.RecordID;
				copy.PaymentDocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.BatchNbr = null;
				copy.APInvoiceDocType = null;
				copy.APInvoiceRefNbr = null;
				copy.LiabilityPaid = false;
				copy.Amount = -1 * copy.Amount;

				DeductionDetails.Update(copy);
			}

			foreach (PRBenefitDetail benefitDetail in SelectFrom<PRBenefitDetail>.
				Where<PRBenefitDetail.paymentDocType.IsEqual<P.AsString>.
					And<PRBenefitDetail.paymentRefNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRBenefitDetail copy = PXCache<PRBenefitDetail>.CreateCopy(benefitDetail);
				copy.RecordID = null;
				copy.OriginalRecordID = benefitDetail.RecordID;
				copy.PaymentDocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.BatchNbr = null;
				copy.APInvoiceDocType = null;
				copy.APInvoiceRefNbr = null;
				copy.LiabilityPaid = false;
				copy.Amount = -1 * copy.Amount;

				BenefitDetails.Update(copy);
			}

			foreach (PRPaymentDeduct deduction in SelectFrom<PRPaymentDeduct>.
				Where<PRPaymentDeduct.docType.IsEqual<P.AsString>.
					And<PRPaymentDeduct.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentDeduct copy = PXCache<PRPaymentDeduct>.CreateCopy(deduction);
				copy.DocType = PayrollType.VoidCheck;
				copy.DedAmount = -1 * copy.DedAmount;
				copy.CntAmount = -1 * copy.CntAmount;

				Deductions.Update(copy);
			}

			foreach (PRPaymentTaxSplit taxSplit in SelectFrom<PRPaymentTaxSplit>.
				Where<PRPaymentTaxSplit.docType.IsEqual<P.AsString>.
					And<PRPaymentTaxSplit.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentTaxSplit copy = PXCache<PRPaymentTaxSplit>.CreateCopy(taxSplit);
				copy.RecordID = null;
				copy.DocType = PayrollType.VoidCheck;
				copy.WageBaseAmount = -1 * copy.WageBaseAmount;

				TaxSplits.Update(copy);
			}

			foreach (PRTaxDetail taxDetail in SelectFrom<PRTaxDetail>.
				Where<PRTaxDetail.paymentDocType.IsEqual<P.AsString>.
					And<PRTaxDetail.paymentRefNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRTaxDetail copy = PXCache<PRTaxDetail>.CreateCopy(taxDetail);
				copy.RecordID = null;
				copy.OriginalRecordID = taxDetail.RecordID;
				copy.PaymentDocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.BatchNbr = null;
				copy.APInvoiceDocType = null;
				copy.APInvoiceRefNbr = null;
				copy.LiabilityPaid = false;
				copy.Amount = -1 * copy.Amount;

				TaxDetails.Update(copy);
			}

			foreach (PRPaymentTax taxSummary in SelectFrom<PRPaymentTax>.
				Where<PRPaymentTax.docType.IsEqual<P.AsString>.
					And<PRPaymentTax.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentTax copy = PXCache<PRPaymentTax>.CreateCopy(taxSummary);
				copy.DocType = PayrollType.VoidCheck;
				copy.TaxAmount = -1 * copy.TaxAmount;
				copy.WageBaseAmount = -1 * copy.WageBaseAmount;
				copy.WageBaseGrossAmt = -1 * copy.WageBaseGrossAmt;
				copy.WageBaseHours = -1 * copy.WageBaseHours;
				copy.AdjustedGrossAmount = -1 * copy.AdjustedGrossAmount;
				copy.ExemptionAmount = -1 * copy.ExemptionAmount;

				Taxes.Update(copy);
			}

			foreach (PRPaymentTaxApplicableAmounts taxApplicableAmounts in SelectFrom<PRPaymentTaxApplicableAmounts>.
				Where<PRPaymentTaxApplicableAmounts.docType.IsEqual<P.AsString>.
					And<PRPaymentTaxApplicableAmounts.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentTaxApplicableAmounts copy = PXCache<PRPaymentTaxApplicableAmounts>.CreateCopy(taxApplicableAmounts);
				copy.DocType = PayrollType.VoidCheck;
				copy.AmountAllowed = -1 * copy.AmountAllowed;

				PaymentTaxApplicableAmounts.Update(copy);
			}

			foreach (PRPaymentPTOBank ptoBank in SelectFrom<PRPaymentPTOBank>
				.Where<PRPaymentPTOBank.docType.IsEqual<P.AsString>
					.And<PRPaymentPTOBank.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentPTOBank copy = PXCache<PRPaymentPTOBank>.CreateCopy(ptoBank);
				copy.DocType = PayrollType.VoidCheck;
				copy.AccrualAmount = -1 * copy.AccrualAmount;
				copy.AccrualMoney = -1 * copy.AccrualMoney;
				copy.DisbursementAmount = -1 * copy.DisbursementAmount;
				copy.DisbursementMoney = -1 * copy.DisbursementMoney;
				copy.FrontLoadingAmount = -1 * copy.FrontLoadingAmount;
				copy.CarryoverAmount = -1 * copy.CarryoverAmount;
				copy.CarryoverMoney = -1 * copy.CarryoverMoney;
				copy.PaidCarryoverAmount = -1 * copy.PaidCarryoverAmount;
				copy.SettlementDiscardAmount = -1 * copy.SettlementDiscardAmount;
				copy.AccruingHours = -1 * copy.AccruingHours;
				copy.HoursPerYear = -1 * copy.HoursPerYear;

				PaymentPTOBanks.Update(copy);
			}

			foreach (PRDirectDepositSplit split in DirectDepositSplits.Select(doc.DocType, doc.RefNbr))
			{
				PRDirectDepositSplit copy = PXCache<PRDirectDepositSplit>.CreateCopy(split);
				copy.DocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.CATranID = null;
				copy.Amount = -1 * copy.Amount;

				DirectDepositSplits.Update(copy);
			}

			foreach (PRPaymentWCPremium premium in SelectFrom<PRPaymentWCPremium>
				.Where<PRPaymentWCPremium.docType.IsEqual<P.AsString>
					.And<PRPaymentWCPremium.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentWCPremium copy = PXCache<PRPaymentWCPremium>.CreateCopy(premium);
				copy.DocType = PayrollType.VoidCheck;
				copy.DeductionAmount = -1 * copy.DeductionAmount;
				copy.Amount = -1 * copy.Amount;
				copy.RegularWageBaseAmount = -1 * copy.RegularWageBaseAmount;
				copy.OvertimeWageBaseAmount = -1 * copy.OvertimeWageBaseAmount;
				copy.WageBaseAmount = -1 * copy.WageBaseAmount;
				copy.RegularWageBaseHours = -1 * copy.RegularWageBaseHours;
				copy.OvertimeWageBaseHours = -1 * copy.OvertimeWageBaseHours;
				copy.WageBaseHours = -1 * copy.WageBaseHours;

				WCPremiums.Update(copy);
			}

			foreach (PRPaymentProjectPackageDeduct packageDeduct in SelectFrom<PRPaymentProjectPackageDeduct>
				.Where<PRPaymentProjectPackageDeduct.docType.IsEqual<P.AsString>
					.And<PRPaymentProjectPackageDeduct.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentProjectPackageDeduct copy = PXCache<PRPaymentProjectPackageDeduct>.CreateCopy(packageDeduct);
				copy.RecordID = null;
				copy.DocType = PayrollType.VoidCheck;
				copy.DeductionAmount = -1 * copy.DeductionAmount;
				copy.BenefitAmount = -1 * copy.BenefitAmount;
				copy.RegularWageBaseAmount = -1 * copy.RegularWageBaseAmount;
				copy.OvertimeWageBaseAmount = -1 * copy.OvertimeWageBaseAmount;
				copy.WageBaseAmount = -1 * copy.WageBaseAmount;
				copy.RegularWageBaseHours = -1 * copy.RegularWageBaseHours;
				copy.OvertimeWageBaseHours = -1 * copy.OvertimeWageBaseHours;
				copy.WageBaseHours = -1 * copy.WageBaseHours;

				ProjectPackageDeductions.Update(copy);
			}

			foreach (PRPaymentUnionPackageDeduct packageDeduct in SelectFrom<PRPaymentUnionPackageDeduct>
				.Where<PRPaymentUnionPackageDeduct.docType.IsEqual<P.AsString>
					.And<PRPaymentUnionPackageDeduct.refNbr.IsEqual<P.AsString>>>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				PRPaymentUnionPackageDeduct copy = PXCache<PRPaymentUnionPackageDeduct>.CreateCopy(packageDeduct);
				copy.RecordID = null;
				copy.DocType = PayrollType.VoidCheck;
				copy.DeductionAmount = -1 * copy.DeductionAmount;
				copy.BenefitAmount = -1 * copy.BenefitAmount;
				copy.RegularWageBaseAmount = -1 * copy.RegularWageBaseAmount;
				copy.OvertimeWageBaseAmount = -1 * copy.OvertimeWageBaseAmount;
				copy.WageBaseAmount = -1 * copy.WageBaseAmount;
				copy.RegularWageBaseHours = -1 * copy.RegularWageBaseHours;
				copy.OvertimeWageBaseHours = -1 * copy.OvertimeWageBaseHours;
				copy.WageBaseHours = -1 * copy.WageBaseHours;

				UnionPackageDeductions.Update(copy);
			}

			foreach (PRRecordOfEmployment recordOfEmployment in SelectFrom<PRRecordOfEmployment>
				.Where<PRRecordOfEmployment.origDocType.IsEqual<P.AsString>
					.And<PRRecordOfEmployment.origRefNbr.IsEqual<P.AsString>>>
				.OrderBy<PRRecordOfEmployment.refNbr.Desc>.View.Select(this, doc.DocType, doc.RefNbr))
			{
				if (recordOfEmployment.Status == ROEStatus.Open || recordOfEmployment.Status == ROEStatus.Exported)
				{
					RecordsOfEmployment.Delete(recordOfEmployment);
				}
				else if (recordOfEmployment.Status == ROEStatus.NeedsAmendment)
				{
					break;
				}
				else if (recordOfEmployment.Status == ROEStatus.Submitted || recordOfEmployment.Status == ROEStatus.Amended)
				{
					recordOfEmployment.Status = ROEStatus.NeedsAmendment;
					RecordsOfEmployment.Update(recordOfEmployment);
					break;
				}
			}
		}

		#region Avoid breaking changes in 2020R2
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected virtual void _(Events.CacheAttached<PRDeductionDetail.amount> e) { }

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected virtual void _(Events.CacheAttached<PRBenefitDetail.amount> e) { }

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected void _(Events.RowSelected<PRPaymentEarning> e) { }
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected virtual void UpdatePaymentEarningRate(PRPaymentEarning paymentEarning) { }

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public virtual decimal GetPaymentEarningRate(PRPaymentEarning paymentEarning, PREarningDetail[] childrenEarningDetails)
		{
			return paymentEarning.Rate.GetValueOrDefault();
		}

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public virtual void _(Events.FieldUpdated<PRPaymentEarning.hours> e) { }

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public virtual void _(Events.FieldUpdated<PRPaymentEarning.amount> e) { }
		#endregion Avoid breaking changes in 2020R2
	}

	public class PXCopyPastePaymentAction : PXCopyPasteAction<PRPayment>
	{
		public PXCopyPastePaymentAction(PXGraph graph, string name)
			: base(graph, name)
		{
		}

		protected override void RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			base.RowSelected(cache, e);

			if (e.Row is PRPayment payment)
			{
				if (payment.DocType == PayrollType.VoidCheck)
				{
					bmCopy.Enabled = false;
					bmSaveTemplate.Enabled = false;
					bmPaste.Enabled = false;
				}

				if (payment.Paid == true || payment.Released == true)
				{
					bmPaste.Enabled = false;
				}

				foreach (ButtonMenu buttonMenu in pasteFromTemplateButtons)
				{
					buttonMenu.Enabled = payment.DocType != PayrollType.VoidCheck && payment.Paid != true && payment.Released != true;
				}
			}
		}
	}

	public class PRPayment_CurrencyInfo : SelectFrom<PRPayment>.
	InnerJoin<CurrencyInfo>.On<CurrencyInfo.curyInfoID.IsEqual<PRPayment.curyInfoID>>.
	Where<PRPayment.docType.IsEqual<P.AsString>.
		And<PRPayment.refNbr.IsEqual<P.AsString>>>.View
	{
		public PRPayment_CurrencyInfo(PXGraph graph)
			: base(graph)
		{
		}
	}

	//TODO 29-04-2019 Find a way to not show already selected value in selector popup
	public class PRUniqueDeductionCodeSelector : PXSelectorAttribute
	{
		public PRUniqueDeductionCodeSelector(Type type) : base(type)
		{
		}

		public PRUniqueDeductionCodeSelector(Type type, params Type[] fieldList) : base(type, fieldList)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			_LookupSelect = _LookupSelect.WhereAnd(typeof(
				Where<Brackets<PRDeductCode.isActive.IsEqual<True>
						.Or<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>>
					.And<PRDeductCode.countryID.IsEqual<PRPayment.countryID.FromCurrent>>>));
			CreateView(sender);
		}

		public override void FieldSelecting(PXCache cache, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(cache, e);
			var row = e.Row as PRPaymentDeduct;
			if (row != null)
			{
				var code = (PRDeductCode)SelectFrom<PRDeductCode>.Where<PRDeductCode.codeID.IsEqual<P.AsInt>>.View.Select(cache.Graph, row.CodeID);
				if (code != null)
				{
					e.ReturnValue = code.CodeCD;
				}
			}
		}

		/// <summary>
		/// Overrides PXSelectorAttribute validation that checks if field value exists in the system. 
		/// Instead of using GetRecords() to validate possible values, this is checking from cache.
		/// It also validates that inserted lines doesn't have same CodeID as an existing line.
		/// </summary>
		/// <remark>
		/// A cache.Locate(e.Row) that returns a row with CodeID == null  means we are inserting a new line, otherwise an existing line is being updated.
		/// Validation should only occurs when inserting new lines.
		/// </remark>
		public override void SubstituteKeyFieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
		{
			if (e.NewValue != null)
			{
				var graph = (PRPayChecksAndAdjustments)cache.Graph;

				PRDeductCode deductionCode;
				if (e.NewValue is string)
				{
					deductionCode = SelectFrom<PRDeductCode>.Where<PRDeductCode.codeCD.IsEqual<P.AsString>>.View.SelectWindowed(graph, 0, 1, e.NewValue);
				}
				else
				{
					deductionCode = SelectFrom<PRDeductCode>.Where<PRDeductCode.codeID.IsEqual<P.AsInt>>.View.SelectWindowed(graph, 0, 1, e.NewValue);
				}
				var locatedRow = (PRPaymentDeduct)cache.Locate(e.Row);
				if (locatedRow != null && locatedRow.CodeID == null && deductionCode != null)
				{
					//Check if there is already another row with same CodeID
					if (graph.Deductions.Select().FirstTableItems.Any(x => x.CodeID == deductionCode.CodeID && x.Source == PaymentDeductionSourceAttribute.EmployeeSettings))
					{
						throw new PXException(Messages.DuplicateDeductionCode);
					}
				}

				if (deductionCode != null)
				{
					e.NewValue = deductionCode.CodeID;
					e.Cancel = true;
					return;
				}
			}

			base.SubstituteKeyFieldUpdating(cache, e);
		}
	}

	[Serializable]
	[PXHidden]
	public class ExistingPayment : IBqlTable
	{
		#region DocType
		public abstract class docType : BqlString.Field<docType> { }
		[PXString(3, IsFixed = true)]
		public string DocType { get; set; }
		#endregion
		#region RefNbr
		public abstract class refNbr : BqlString.Field<refNbr> { }
		[PXString(15, IsUnicode = true)]
		public string RefNbr { get; set; }
		#endregion
		#region Message
		public abstract class message : BqlString.Field<message> { }
		[PXString]
		public string Message { get; set; }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class ExistingPayrollBatch : IBqlTable
	{
		#region BatchNbr
		public abstract class batchNbr : BqlString.Field<batchNbr> { }
		[PXString(15, IsUnicode = true)]
		public string BatchNbr { get; set; }
		#endregion
		#region Message
		public abstract class message : BqlString.Field<message> { }
		[PXString]
		public string Message { get; set; }
		#endregion
	}
}
