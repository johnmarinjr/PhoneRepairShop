using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.EP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR
{
	public class PRSetupMaint : PXGraph<PRSetupMaint>
	{
		#region Actions
		public PXSave<PRSetup> Save;
		public PXCancel<PRSetup> Cancel;
		#endregion

		#region Views
		public PXSelect<PRSetup> Setup;

		public SelectFrom<PRTransactionDateException>
			.Where<MatchPRCountry<PRTransactionDateException.countryID>>
			.OrderBy<PRTransactionDateException.date.Asc>.View TransactionDateExceptions;

		public SelectFrom<PRPayment>
			.Where<PRPayment.paid.IsEqual<False>
				.And<PRPayment.released.IsEqual<False>>
				.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>.View EditablePayments;

		public SelectFrom<PRBenefitDetail>
			.InnerJoin<PRPayment>.On<PRPayment.docType.IsEqual<PRBenefitDetail.paymentDocType>
				.And<PRPayment.refNbr.IsEqual<PRBenefitDetail.paymentRefNbr>>>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRBenefitDetail.codeID>>
			.Where<PRPayment.paid.IsEqual<False>
				.And<PRPayment.released.IsEqual<False>>
				.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>.View EditableBenefitDetails;

		public SelectFrom<PRTaxDetail>
			.InnerJoin<PRPayment>.On<PRPayment.docType.IsEqual<PRTaxDetail.paymentDocType>
				.And<PRPayment.refNbr.IsEqual<PRTaxDetail.paymentRefNbr>>>
			.Where<PRPayment.paid.IsEqual<False>
				.And<PRPayment.released.IsEqual<False>>
				.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>.View EditableTaxDetails;

		public SelectFrom<PRPTODetail>
			.InnerJoin<PRPayment>.On<PRPayment.docType.IsEqual<PRPTODetail.paymentDocType>
				.And<PRPayment.refNbr.IsEqual<PRPTODetail.paymentRefNbr>>>
			.Where<PRPayment.paid.IsEqual<False>
				.And<PRPayment.released.IsEqual<False>>
				.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>.View EditablePTODetails;

		public PXSetup<EPSetup> TimeExpenseSettings;
		#endregion

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXCustomizeBaseAttribute(typeof(BenExpenseAccountAttribute), nameof(BenExpenseAccountAttribute.CheckIfEmpty), false)]
		public void _(Events.CacheAttached<PRBenefitDetail.expenseAccountID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXCustomizeBaseAttribute(typeof(BenExpenseSubAccountAttribute), nameof(BenExpenseSubAccountAttribute.CheckIfEmpty), false)]
		public void _(Events.CacheAttached<PRBenefitDetail.expenseSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXCustomizeBaseAttribute(typeof(TaxExpenseAccountAttribute), nameof(TaxExpenseAccountAttribute.CheckIfEmpty), false)]
		public void _(Events.CacheAttached<PRTaxDetail.expenseAccountID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXCustomizeBaseAttribute(typeof(TaxExpenseSubAccountAttribute), nameof(TaxExpenseSubAccountAttribute.CheckIfEmpty), false)]
		public void _(Events.CacheAttached<PRTaxDetail.expenseSubID> e) { }
		#endregion CacheAttached

		#region Events
		protected virtual void _(Events.RowSelected<PRSetup> e)
		{
			if (e.Row == null)
			{
				return;
			}
			
			SetAlternateAccountSubVisible(e.Cache, e.Row);
		}

		protected virtual void _(Events.RowPersisting<PRTransactionDateException> e)
		{
			PRTransactionDateException row = e.Row as PRTransactionDateException;
			if (row == null)
			{
				return;
			}

			// Check that each date has at most one record.
			// We can't use a SQL unique index to check for this condition because PRTransactionDateException uses
			// a CompanyMask.
			if (((e.Operation & PXDBOperation.Command) == PXDBOperation.Update || (e.Operation & PXDBOperation.Command) == PXDBOperation.Insert) &&
				new SelectFrom<PRTransactionDateException>
					.Where<PRTransactionDateException.date.IsEqual<P.AsDateTime>
						.And<PRTransactionDateException.recordID.IsNotEqual<P.AsInt>>>.View(this).SelectSingle(row.Date, row.RecordID) != null)
			{
				throw new PXException(Messages.DuplicateExceptionDate);
			}
		}

		protected virtual void _(Events.FieldVerifying<PRSetup.enablePieceworkEarningType> e)
		{
			if (e.Row == null || Convert.ToBoolean(e.NewValue))
				return;

			CheckPieceworkEarningType();
			CheckEmployeesWithMiscEarningType();
			CheckPaymentsWithMiscEarningType();
		}

		public virtual void _(Events.RowPersisting<PRSetup> e)
		{
			if (e.Row == null || e.Operation == PXDBOperation.Delete)
			{
				return;
			}

			if ((e.Row.TimePostingOption == EPPostOptions.PostToOffBalance || e.Row.TimePostingOption == EPPostOptions.OverridePMInPayroll) && e.Row.OffBalanceAccountGroupID == null)
			{
				e.Cache.RaiseExceptionHandling<PRSetup.offBalanceAccountGroupID>(e.Row, null,
					new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<PRSetup.offBalanceAccountGroupID>(e.Cache)));
			}

			VerifyProjectCostAssignment(e.Cache, e.Row, e.Row.ProjectCostAssignment);
			VerifyTimePostingOption(e.Cache, e.Row, e.Row.TimePostingOption);
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.projectCostAssignment> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			string oldValue = e.OldValue as string;
			string newValue = e.NewValue as string;

			if (string.IsNullOrWhiteSpace(oldValue) && !string.IsNullOrWhiteSpace(newValue) ||
				oldValue == ProjectCostAssignmentType.NoCostAssigned && newValue != ProjectCostAssignmentType.NoCostAssigned ||
				(oldValue == ProjectCostAssignmentType.WageCostAssigned || oldValue == ProjectCostAssignmentType.WageLaborBurdenAssigned) &&
				newValue != ProjectCostAssignmentType.WageCostAssigned && newValue != ProjectCostAssignmentType.WageLaborBurdenAssigned)
			{
				row.TimePostingOption = null;
			}

			if (oldValue.Equals(ProjectCostAssignmentType.WageLaborBurdenAssigned))
			{
				DeleteDetails<PRSetup.projectCostAssignment>(e.Cache, row, CostAssignmentType.DetailType.Benefit);
				DeleteDetails<PRSetup.projectCostAssignment>(e.Cache, row, CostAssignmentType.DetailType.Tax);
				UncalculateEditablePayments();
			}
			else if (row.ProjectCostAssignment == ProjectCostAssignmentType.WageLaborBurdenAssigned)
			{
				UncalculateEditablePayments();
			}
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitExpenseAcctDefault> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((e.OldValue.Equals(PRBenefitExpenseAcctSubDefault.MaskEarningType) && !e.NewValue.Equals(PRBenefitExpenseAcctSubDefault.MaskEarningType) &&
					!SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, row.BenefitExpenseSubMask, PRBenefitExpenseAcctSubDefault.MaskEarningType)) ||
				(e.OldValue.Equals(PRBenefitExpenseAcctSubDefault.MaskLaborItem) && !e.NewValue.Equals(PRBenefitExpenseAcctSubDefault.MaskLaborItem) &&
					!SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, row.BenefitExpenseSubMask, PRBenefitExpenseAcctSubDefault.MaskLaborItem)))
			{
				DeleteDetails<PRSetup.benefitExpenseAcctDefault>(e.Cache, row, CostAssignmentType.DetailType.Benefit);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitExpenseSubMask> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, (string)e.OldValue, PRBenefitExpenseAcctSubDefault.MaskEarningType) &&
					!SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, (string)e.NewValue, PRBenefitExpenseAcctSubDefault.MaskEarningType) &&
					row.BenefitExpenseAcctDefault != PRBenefitExpenseAcctSubDefault.MaskEarningType) ||
				(SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, (string)e.OldValue, PRBenefitExpenseAcctSubDefault.MaskLaborItem) &&
					!SubMaskContainsValue<PRSetup.benefitExpenseSubMask>(e.Cache, row, (string)e.NewValue, PRBenefitExpenseAcctSubDefault.MaskLaborItem) &&
					row.BenefitExpenseAcctDefault != PRBenefitExpenseAcctSubDefault.MaskLaborItem))
			{
				DeleteDetails<PRSetup.benefitExpenseSubMask>(e.Cache, row, CostAssignmentType.DetailType.Benefit);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxExpenseAcctDefault> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((e.OldValue.Equals(PRTaxExpenseAcctSubDefault.MaskEarningType) && !e.NewValue.Equals(PRTaxExpenseAcctSubDefault.MaskEarningType) &&
					!SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, row.TaxExpenseSubMask, PRTaxExpenseAcctSubDefault.MaskEarningType)) ||
				(e.OldValue.Equals(PRTaxExpenseAcctSubDefault.MaskLaborItem) && !e.NewValue.Equals(PRTaxExpenseAcctSubDefault.MaskLaborItem) &&
					!SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, row.TaxExpenseSubMask, PRTaxExpenseAcctSubDefault.MaskLaborItem)))
			{
				DeleteDetails<PRSetup.taxExpenseAcctDefault>(e.Cache, row, CostAssignmentType.DetailType.Tax);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxExpenseSubMask> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, (string)e.OldValue, PRTaxExpenseAcctSubDefault.MaskEarningType) &&
					!SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, (string)e.NewValue, PRTaxExpenseAcctSubDefault.MaskEarningType) &&
					row.TaxExpenseAcctDefault != PRTaxExpenseAcctSubDefault.MaskEarningType) ||
				(SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, (string)e.OldValue, PRTaxExpenseAcctSubDefault.MaskLaborItem) &&
					!SubMaskContainsValue<PRSetup.taxExpenseSubMask>(e.Cache, row, (string)e.NewValue, PRTaxExpenseAcctSubDefault.MaskLaborItem) &&
					row.TaxExpenseAcctDefault != PRTaxExpenseAcctSubDefault.MaskLaborItem))
			{
				DeleteDetails<PRSetup.taxExpenseSubMask>(e.Cache, row, CostAssignmentType.DetailType.Tax);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoExpenseAcctDefault> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((e.OldValue.Equals(GLAccountSubSource.EarningType) && !e.NewValue.Equals(GLAccountSubSource.EarningType) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, row.PTOExpenseSubMask, GLAccountSubSource.EarningType))
				|| (e.OldValue.Equals(GLAccountSubSource.LaborItem) && !e.NewValue.Equals(GLAccountSubSource.LaborItem) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, row.PTOExpenseSubMask, GLAccountSubSource.LaborItem))
				|| (e.OldValue.Equals(GLAccountSubSource.Project) && !e.NewValue.Equals(GLAccountSubSource.Project) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, row.PTOExpenseSubMask, GLAccountSubSource.Project))
				|| (e.OldValue.Equals(GLAccountSubSource.Task) && !e.NewValue.Equals(GLAccountSubSource.Task) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, row.PTOExpenseSubMask, GLAccountSubSource.Task)))
			{
				DeleteDetails<PRSetup.ptoExpenseAcctDefault>(e.Cache, row, CostAssignmentType.DetailType.PTO);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoExpenseAlternateAcctDefault> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if (e.OldValue != null && e.OldValue.Equals(GLAccountSubSource.EarningType)
				&& e.NewValue != null && !e.NewValue.Equals(GLAccountSubSource.EarningType))
			{
				DeleteDetails<PRSetup.ptoExpenseAlternateAcctDefault>(e.Cache, row, CostAssignmentType.DetailType.PTO);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoExpenseSubMask> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if ((SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.OldValue, GLAccountSubSource.EarningType) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.NewValue, GLAccountSubSource.EarningType) &&
					row.PTOExpenseAcctDefault != GLAccountSubSource.EarningType) ||
				(SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.OldValue, GLAccountSubSource.LaborItem) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.NewValue, GLAccountSubSource.LaborItem) &&
					row.PTOExpenseAcctDefault != GLAccountSubSource.LaborItem) ||
				(SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.OldValue, GLAccountSubSource.Project) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.NewValue, GLAccountSubSource.Project) &&
					row.PTOExpenseAcctDefault != GLAccountSubSource.Project) ||
				(SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.OldValue, GLAccountSubSource.Task) &&
					!SubMaskContainsValue<PRSetup.ptoExpenseSubMask>(e.Cache, row, (string)e.NewValue, GLAccountSubSource.Task) &&
					row.PTOExpenseAcctDefault != GLAccountSubSource.Task))
			{
				DeleteDetails<PRSetup.ptoExpenseSubMask>(e.Cache, row, CostAssignmentType.DetailType.PTO);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoExpenseAlternateSubMask> e)
		{
			PRSetup row = e.Row as PRSetup;
			if (row == null)
			{
				return;
			}

			if (SubMaskContainsValue<PRSetup.ptoExpenseAlternateSubMask>(e.Cache, row, (string)e.OldValue, GLAccountSubSource.EarningType)
				&& !SubMaskContainsValue<PRSetup.ptoExpenseAlternateSubMask>(e.Cache, row, (string)e.NewValue, GLAccountSubSource.EarningType))
			{
				DeleteDetails<PRSetup.ptoExpenseAlternateSubMask>(e.Cache, row, CostAssignmentType.DetailType.PTO);
			}

			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.deductLiabilityAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.deductLiabilitySubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitLiabilityAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitLiabilitySubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxLiabilityAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxLiabilitySubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoLiabilityAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoLiabilitySubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoAssetAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.ptoAssetSubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.earningsAlternateAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.earningsAlternateSubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitExpenseAlternateAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.benefitExpenseAlternateSubMask> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxExpenseAlternateAcctDefault> e)
		{
			UncalculateEditablePayments();
		}

		protected virtual void _(Events.FieldUpdated<PRSetup.taxExpenseAlternateSubMask> e)
		{
			UncalculateEditablePayments();
		}

		#endregion Events

		#region Helpers
		private void CheckPieceworkEarningType()
		{
			EPEarningType pieceworkEarningType =
				SelectFrom<EPEarningType>.
					Where<PREarningType.isPiecework.IsEqual<True>>.View.SelectSingleBound(this, null);

			if (pieceworkEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotDeactivatePieceworkEarningType, PXErrorLevel.Error,
				pieceworkEarningType.TypeCD);
		}

		private void CheckEmployeesWithMiscEarningType()
		{
			EPEmployee employeeWithMiscEarningType =
				SelectFrom<EPEmployee>.
				InnerJoin<PREmployeeEarning>.
					On<EPEmployee.bAccountID.IsEqual<PREmployeeEarning.bAccountID>>.
				InnerJoin<EPEarningType>.
					On<PREmployeeEarning.typeCD.IsEqual<EPEarningType.typeCD>>.
				Where<PREarningType.isPiecework.IsEqual<True>.
					Or<PREmployeeEarning.unitType.IsEqual<UnitType.misc>>>.View.SelectSingleBound(this, null);

			if (employeeWithMiscEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotDeactivatePieceworkEarningTypeEmployee, PXErrorLevel.Error,
				Messages.Misc, employeeWithMiscEarningType.AcctName);
		}

		private void CheckPaymentsWithMiscEarningType()
		{
			PRPayment paymentsWithMiscEarningType =
				SelectFrom<PRPayment>.
				InnerJoin<PREarningDetail>.
					On<PRPayment.refNbr.IsEqual<PREarningDetail.paymentRefNbr>.
						And<PRPayment.docType.IsEqual<PREarningDetail.paymentDocType>>>.
				InnerJoin<EPEarningType>.
					On<PREarningDetail.typeCD.IsEqual<EPEarningType.typeCD>>.
				Where<PREarningType.isPiecework.IsEqual<True>.
					Or<PREarningDetail.unitType.IsEqual<UnitType.misc>>>.View.SelectSingleBound(this, null);

			if (paymentsWithMiscEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotDeactivatePieceworkEarningTypePayment, PXErrorLevel.Error,
				Messages.Misc, string.Format("{0},{1}", paymentsWithMiscEarningType.DocType, paymentsWithMiscEarningType.RefNbr));
		}

		private bool SubMaskContainsValue<TSubMaskField>(PXCache cache, PRSetup setup, string subMask, string compareValue) where TSubMaskField : IBqlField
		{
			return SubMaskContainsValue(cache, setup, typeof(TSubMaskField), subMask, compareValue);
		}

		public static bool SubMaskContainsValue(PXCache cache, PRSetup setup, Type subMaskField, string subMask, string compareValue)
		{
			if (string.IsNullOrEmpty(subMask))
			{
				return false;
			}

			PRSubAccountMaskAttribute subMaskAttribute = cache.GetAttributesOfType<PRSubAccountMaskAttribute>(setup, subMaskField.Name).FirstOrDefault();
			if (subMaskAttribute != null)
			{
				PRDimensionMaskAttribute dimensionMaskAttribute = subMaskAttribute.GetAttribute<PRDimensionMaskAttribute>();
				if (dimensionMaskAttribute != null)
				{
					List<string> maskValues = dimensionMaskAttribute.GetSegmentMaskValues(subMask).ToList();
					return maskValues.Contains(compareValue);
				}
			}

			return false;
		}

		private void DeleteDetails<TUpdatedField>(PXCache cache, PRSetup row, CostAssignmentType.DetailType detailType) where TUpdatedField : IBqlField
		{
			HashSet<string> affectedAdjChecks = new HashSet<string>();
			switch(detailType)
			{
				case CostAssignmentType.DetailType.Benefit:
					foreach (PXResult<PRBenefitDetail, PRPayment> result in EditableBenefitDetails.Select().Select(x => (PXResult<PRBenefitDetail, PRPayment>)x))
					{
						PRBenefitDetail benefitDetail = result;
						PRPayment payment = result;
						EditableBenefitDetails.Delete(benefitDetail);

						if (payment.DocType == PayrollType.Adjustment)
						{
							affectedAdjChecks.Add(EditablePayments.Cache.GetValueExt<PRPayment.paymentDocAndRef>(payment).ToString());
						}
					}
					break;
				case CostAssignmentType.DetailType.Tax:
					foreach (PXResult<PRTaxDetail, PRPayment> result in EditableTaxDetails.Select().Select(x => (PXResult<PRTaxDetail, PRPayment>)x))
					{
						PRTaxDetail taxDetail = result;
						PRPayment payment = result;
						EditableTaxDetails.Delete(taxDetail);

						if (payment.DocType == PayrollType.Adjustment)
						{
							affectedAdjChecks.Add(EditablePayments.Cache.GetValueExt<PRPayment.paymentDocAndRef>(payment).ToString());
						}
					}
					break;
				case CostAssignmentType.DetailType.PTO:
					foreach (PXResult<PRPTODetail, PRPayment> result in EditablePTODetails.Select().Select(x => (PXResult<PRPTODetail, PRPayment>)x))
					{
						PRPTODetail ptoDetail = result;
						PRPayment payment = result;
						EditablePTODetails.Delete(ptoDetail);

						if (payment.DocType == PayrollType.Adjustment)
						{
							affectedAdjChecks.Add(EditablePayments.Cache.GetValueExt<PRPayment.paymentDocAndRef>(payment).ToString());
						}
					}
					break;
			}

			if (affectedAdjChecks.Any())
			{
				string detailTypeMessage = detailType == CostAssignmentType.DetailType.Benefit ? Messages.Benefit : Messages.Tax;
				PXUIFieldAttribute.SetWarning<TUpdatedField>(
					cache,
					row,
					PXMessages.LocalizeFormat(Messages.AdjustmentDetailsWillBeDeleted, detailTypeMessage));

				StringBuilder sb = new StringBuilder(PXMessages.LocalizeFormatNoPrefix(Messages.AdjustmentListWithDeletedDetails, detailTypeMessage));
				sb.AppendLine();
				affectedAdjChecks.ForEach(x => sb.AppendLine(x));
				PXTrace.WriteWarning(sb.ToString());
			}
		}

		private void UncalculateEditablePayments()
		{
			foreach (PRPayment payment in EditablePayments.Select())
			{
				payment.Calculated = false;
				EditablePayments.Update(payment);
			}
		}

		public static string GetEarningTypeFromSetup<TField>(PXGraph graph)
			where TField : IBqlField
		{
			PXCache setupCache;
			PRSetup setup;
			if (graph.Caches.TryGetValue(typeof(PRSetup), out setupCache))
			{
				setup = setupCache?.Current as PRSetup;
			}
			else
			{
				var setupView = new SelectFrom<PRSetup>.View(graph);
				setup = setupView.SelectSingle();
				setupCache = setupView.Cache;
			}

			return setupCache.GetValue<TField>(setup) as string;
		}
		
		protected virtual void VerifyProjectCostAssignment(PXCache cache, PRSetup row, object value)
		{
			if (row.TimePostingOption == null)
			{
				return;
			}

			if (ProjectCostAssignmentType.NoCostAssigned.Equals(value)
				&& row.TimePostingOption != EPPostOptions.DoNotPost
				&& row.TimePostingOption != EPPostOptions.PostToOffBalance)
			{
				cache.RaiseExceptionHandling<PRSetup.projectCostAssignment>(row, value, new PXSetPropertyException(Messages.EPPostingOptionNotDoNotPost, PXErrorLevel.Error));
			}

			if ((ProjectCostAssignmentType.WageCostAssigned.Equals(value) || ProjectCostAssignmentType.WageLaborBurdenAssigned.Equals(value))
				&& row.TimePostingOption != EPPostOptions.OverridePMInPayroll
				&& row.TimePostingOption != EPPostOptions.OverridePMAndGLInPayroll
				&& row.TimePostingOption != EPPostOptions.PostPMAndGLFromPayroll)
			{
				cache.RaiseExceptionHandling<PRSetup.projectCostAssignment>(row, value, new PXSetPropertyException(Messages.EPPostingOptionNotWageCostsAssigned, PXErrorLevel.Error));
			}
		}

		protected virtual void VerifyTimePostingOption(PXCache cache, PRSetup row, object value)
		{
			if ((EPPostOptions.DoNotPost.Equals(value) || EPPostOptions.PostToOffBalance.Equals(value)) 
				&& row.ProjectCostAssignment != ProjectCostAssignmentType.NoCostAssigned)
			{
				cache.RaiseExceptionHandling<PRSetup.timePostingOption>(row, value, new PXSetPropertyException(Messages.ProjectCostAssignmentNotNoCostAssigned, PXErrorLevel.Error));
			}

			if ((EPPostOptions.OverridePMInPayroll.Equals(value) || EPPostOptions.OverridePMAndGLInPayroll.Equals(value) || EPPostOptions.PostPMAndGLFromPayroll.Equals(value))
				&& row.ProjectCostAssignment != ProjectCostAssignmentType.WageCostAssigned
				&& row.ProjectCostAssignment != ProjectCostAssignmentType.WageLaborBurdenAssigned)
			{
				cache.RaiseExceptionHandling<PRSetup.timePostingOption>(row, value, new PXSetPropertyException(Messages.ProjectCostAssignmentNotWageCostsAssigned, PXErrorLevel.Error));
			}
		}

		protected virtual void SetAlternateAccountSubVisible(PXCache cache, PRSetup row)
		{
			PXUIFieldAttribute.SetVisible<PRSetup.earningsAlternateAcctDefault>(cache, row, row.EarningsAcctDefault == GLAccountSubSource.Project || row.EarningsAcctDefault == GLAccountSubSource.Task || row.EarningsAcctDefault == GLAccountSubSource.LaborItem);
			PXUIFieldAttribute.SetVisible<PRSetup.earningsAlternateSubMask>(cache, row, row.EarningsSubMask.Any(x => x.ToString() == GLAccountSubSource.Project || x.ToString() == GLAccountSubSource.Task || x.ToString() == GLAccountSubSource.LaborItem));

			PXUIFieldAttribute.SetVisible<PRSetup.benefitExpenseAlternateAcctDefault>(cache, row, row.BenefitExpenseAcctDefault == GLAccountSubSource.Project || row.BenefitExpenseAcctDefault == GLAccountSubSource.Task || row.BenefitExpenseAcctDefault == GLAccountSubSource.LaborItem);
			PXUIFieldAttribute.SetVisible<PRSetup.benefitExpenseAlternateSubMask>(cache, row, row.BenefitExpenseSubMask.Any(x => x.ToString() == GLAccountSubSource.Project || x.ToString() == GLAccountSubSource.Task || x.ToString() == GLAccountSubSource.LaborItem));

			PXUIFieldAttribute.SetVisible<PRSetup.taxExpenseAlternateAcctDefault>(cache, row, row.TaxExpenseAcctDefault == GLAccountSubSource.Project || row.TaxExpenseAcctDefault == GLAccountSubSource.Task || row.TaxExpenseAcctDefault == GLAccountSubSource.LaborItem);
			PXUIFieldAttribute.SetVisible<PRSetup.taxExpenseAlternateSubMask>(cache, row, row.TaxExpenseSubMask.Any(x => x.ToString() == GLAccountSubSource.Project || x.ToString() == GLAccountSubSource.Task || x.ToString() == GLAccountSubSource.LaborItem));
		}

		public virtual bool IsAlternateFieldVisible(string accountSub)
		{
			return accountSub == GLAccountSubSource.Project || accountSub == GLAccountSubSource.Task || accountSub == GLAccountSubSource.LaborItem;
		}
		#endregion Helpers
	}

	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
	public class PRxEPSetupMaint : PXGraphExtension<EPSetupMaint>
	{
		public static bool IsActive()
		{
			return false;
		}

		public virtual void _(Events.FieldVerifying<EPSetup.postingOption> e) { }

		public virtual void _(Events.RowPersisting<EPSetup> e) { }

		public virtual void _(Events.FieldSelecting<EPSetup, EPSetup.postingOption> e) { }

		protected virtual void VerifyPostingOption(PXCache cache, EPSetup row, object value) { }
	}
}
