using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public abstract class PRAccountAttribute : AccountAttribute
	{
		protected Type _CodeField;
		protected Type _EmployeeField;
		public Type PayGroupField { get; set; }
		public bool CheckIfEmpty { get; set; } = true;

		protected abstract int? GetAccountValue(PXCache cache, object setupFieldValue, object row);
		protected abstract Type SetupField { get; }
		protected abstract int? GetAcctIDFromEmployee(PREmployee employee);
		protected abstract int? GetAcctIDFromPayGroup(PRPayGroup payGroup);

		protected virtual bool IsFieldRequired(PXCache cache, object row)
		{
			return true;
		}

		protected virtual PRSetup GetPRSetup(PXGraph graph)
		{
			return PXSetup<PRSetup>.Select(graph).TopFirst;
		}


		#region Constructor
		public PRAccountAttribute(Type codeField, Type employeeField, Type payGroupField) : this(null, codeField, employeeField, payGroupField)
		{
		}

		public PRAccountAttribute(Type branchID, Type codeField, Type employeeField, Type payGroupField) : base(branchID)
		{
			_CodeField = codeField;
			_EmployeeField = employeeField;
			PayGroupField = payGroupField;
		}

		public PRAccountAttribute(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField) : base(branchID, SearchType)
		{
			_CodeField = codeField;
			_EmployeeField = employeeField;
			PayGroupField = payGroupField;
		}
		#endregion Constructor

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldDefaulting.AddHandler(sender.GetItemType(), FieldName, FieldDefaulting);
			sender.Graph.RowUpdated.AddHandler(sender.GetItemType(), RowUpdated);
			sender.Graph.RowInserted.AddHandler(sender.GetItemType(), RowInserted);
			if (CheckIfEmpty)
			{
				sender.Graph.RowSelected.AddHandler(sender.GetItemType(), RowSelected);
				sender.Graph.FieldVerifying.AddHandler(sender.GetItemType(), FieldName, OptionalFieldVerifying);
			}
		}

		#region Events

		protected virtual void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetError(sender, e.Row, FieldName, null);
				var setupFieldValue = (string)CacheHelper.GetValue(sender.Graph, GetPRSetup(sender.Graph), SetupField);
				int? defaultedValue = DefaultValue(sender, setupFieldValue, e.Row);
				if (defaultedValue != null)
				{
					e.NewValue = defaultedValue;
					e.Cancel = true;
				}
			}
		}

		protected virtual int? DefaultValue(PXCache sender, string setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PREarningsAcctSubDefault.MaskEmployee:
					var employeeID = (int?)sender.GetValue(row, _EmployeeField.Name);
					var employee = (PREmployee)SelectFrom<PREmployee>.Where<PREmployee.bAccountID.IsEqual<P.AsInt>>.View.Select(sender.Graph, employeeID);
					if (employee == null)
					{
						return null;
					}
					return GetAcctIDFromEmployee(employee);
				case PREarningsAcctSubDefault.MaskPayGroup:
					var payGroupID = GetCurrentValue(sender.Graph, PayGroupField);
					var payGroup = (PRPayGroup)SelectFrom<PRPayGroup>.Where<PRPayGroup.payGroupID.IsEqual<P.AsString>>.View.Select(sender.Graph, payGroupID);
					if (payGroup == null)
					{
						return null;
					}
					return GetAcctIDFromPayGroup(payGroup);
				default:
					return GetAccountValue(sender, setupFieldValue, row);
			}
		}

		protected virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			RowInsertedUpdated(sender, e.Row, e.OldRow);
		}

		protected virtual void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			RowInsertedUpdated(sender, e.Row, null);
		}

		protected virtual void RowInsertedUpdated(PXCache sender, object row, object oldRow)
		{
			if (row == null)
			{
				return;
			}

			object setupFieldValue = CacheHelper.GetValue(sender.Graph, GetPRSetup(sender.Graph), SetupField);
			if (!Equals(sender.GetValue(row, _CodeField.Name), sender.GetValue(oldRow, _CodeField.Name))
				|| (!Equals(sender.GetValue(row, _EmployeeField.Name), sender.GetValue(oldRow, _EmployeeField.Name)) && setupFieldValue.Equals(GLAccountSubSource.Employee)))
			{
				sender.SetDefaultExt(row, FieldName);
			}
		}

		protected virtual void RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (cache.GetValue(e.Row, _CodeField.Name) != null && cache.GetValue(e.Row, FieldName) == null && IsFieldRequired(cache, e.Row))
			{
				PXUIFieldAttribute.SetError(cache, e.Row, FieldName, PXMessages.LocalizeFormat(Messages.PostingValueNotFound, this.FieldName));
			}
			else
			{
				PXUIFieldAttribute.SetError(cache, e.Row, FieldName, null);
			}
		}

		public virtual void OptionalFieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			base.FieldVerifying(cache, e);
			if (e.Row == null)
			{
				return;
			}

			if (e.NewValue == null && IsFieldRequired(cache, e.Row))
			{
				PXUIFieldAttribute.SetWarning(cache, e.Row, this.FieldName, PXMessages.LocalizeFormat(Messages.PostingValueNotFound, this.FieldName));
			}
		}

		#endregion Events


		protected object GetCurrentValue(PXGraph graph, Type type)
		{
			return graph.Caches[BqlCommand.GetItemType(type)].GetValue(graph.Caches[BqlCommand.GetItemType(type)].Current, type.Name);
		}
	}

	public abstract class PRExpenseAccountAttribte : PRAccountAttribute
	{
		protected Type _EarningCodeField;
		protected Type _LaborItemField;
		protected Type _ProjectIDField;
		protected Type _TaskIDField;
		protected abstract Type SetupAlternateField { get; }

		#region Constructors
		public PRExpenseAccountAttribte(Type branchID, Type codeField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, codeField, employeeField, payGroupField)
		{
			_EarningCodeField = earningCodeField;
			_LaborItemField = laborItemField;
			_ProjectIDField = projectIDField;
			_TaskIDField = taskIDField;
		}

		public PRExpenseAccountAttribte(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, SearchType, codeField, employeeField, payGroupField)
		{
			_EarningCodeField = earningCodeField;
			_LaborItemField = laborItemField;
			_ProjectIDField = projectIDField;
			_TaskIDField = taskIDField;
		}
		#endregion Constructors

		protected abstract Type EarningTypeAccountField { get; }
		protected abstract Type InventoryItemAccountField { get; }
		protected abstract Type ProjectAccountField { get; }
		protected abstract Type TaskAccountField { get; }

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PREarningsAcctSubDefault.MaskEarningType:
					var earningTypeCD = cache.GetValue(row, _EarningCodeField.Name);
					var earningTypeView = new SelectFrom<EPEarningType>.Where<EPEarningType.typeCD.IsEqual<P.AsString>>.View(cache.Graph);
					EPEarningType earning = earningTypeView.SelectSingle(earningTypeCD);
					if (earning == null)
					{
						return null;
					}

					return earningTypeView.Cache.GetValue(earning, EarningTypeAccountField.Name) as int?;
				case PREarningsAcctSubDefault.MaskLaborItem:
					InventoryItem laborItem = null;
					object itemID = cache.GetValue(row, _LaborItemField.Name);
					if (itemID != null)
					{
						laborItem = (InventoryItem)PXSelectorAttribute.Select(cache, row, _LaborItemField.Name);
					}
					else
					{
						var alternateSetupValue = GetCurrentValue(cache.Graph, SetupAlternateField);
						if (alternateSetupValue != setupFieldValue)
						{
							return DefaultValue(cache, (string)alternateSetupValue, row);
						}
					}

					if (laborItem == null)
					{
						return null;
					}
					PXCache inventoryItemCache = cache.Graph.Caches[typeof(InventoryItem)];
					return inventoryItemCache.GetValue(laborItem, InventoryItemAccountField.Name) as int?;
				case PREarningsAcctSubDefault.MaskProject:
					{
						PMProject project = null;
						int? projectID = (int?)cache.GetValue(row, _ProjectIDField.Name);
						if (projectID != null && !ProjectDefaultAttribute.IsNonProject(projectID))
						{
							project = (PMProject)PXSelectorAttribute.Select(cache, row, _ProjectIDField.Name);
						}
						else
						{
							var alternateSetupValue = GetCurrentValue(cache.Graph, SetupAlternateField);
							if (alternateSetupValue != setupFieldValue)
							{
								return DefaultValue(cache, (string)alternateSetupValue, row);
							}
						}
						if (project == null)
						{
							return null;
						}

						PXCache projectCache = cache.Graph.Caches[typeof(PMProject)];
						return projectCache.GetValue(project, ProjectAccountField.Name) as int?;
					}
				case PREarningsAcctSubDefault.MaskTask:
					{
						PMTask task = null;
						int? projectID = (int?)cache.GetValue(row, _ProjectIDField.Name);
						if (projectID != null && !ProjectDefaultAttribute.IsNonProject(projectID))
						{
							task = (PMTask)PXSelectorAttribute.Select(cache, row, _TaskIDField.Name);
						}
						else
						{
							var alternateSetupValue = GetCurrentValue(cache.Graph, SetupAlternateField);
							if (alternateSetupValue != setupFieldValue)
							{
								return DefaultValue(cache, (string)alternateSetupValue, row);
							}
						}
						if (task == null)
						{
							return null;
						}

						PXCache taskCache = cache.Graph.Caches[typeof(PMTask)];
						return taskCache.GetValue(task, TaskAccountField.Name) as int?;
					}
			}

			return null;
		}

		protected override void RowInsertedUpdated(PXCache sender, object row, object oldRow)
		{
			if (row == null || sender.Graph.IsImportFromExcel && sender.GetValue(row, FieldName) != null)
			{
				return;
			}

			object setupFieldValue = CacheHelper.GetValue(sender.Graph, GetPRSetup(sender.Graph), SetupField);
			if ((!Equals(sender.GetValue(row, _EarningCodeField.Name), sender.GetValue(oldRow, _EarningCodeField.Name)) && setupFieldValue.Equals(PREarningsAcctSubDefault.MaskEarningType))
				|| (!Equals(sender.GetValue(row, _LaborItemField.Name), sender.GetValue(oldRow, _LaborItemField.Name)) && setupFieldValue.Equals(PREarningsAcctSubDefault.MaskLaborItem))
				|| (!Equals(sender.GetValue(row, _ProjectIDField.Name), sender.GetValue(oldRow, _ProjectIDField.Name)) && setupFieldValue.Equals(PREarningsAcctSubDefault.MaskProject))
				|| (!Equals(sender.GetValue(row, _TaskIDField.Name), sender.GetValue(oldRow, _TaskIDField.Name)) && setupFieldValue.Equals(PREarningsAcctSubDefault.MaskTask)))
			{
				sender.SetDefaultExt(row, FieldName);
			}

			base.RowInsertedUpdated(sender, row, oldRow);
		}
	}

	public class EarningsAccountAttribute : PRExpenseAccountAttribte
	{
		#region Constructors
		public EarningsAccountAttribute(Type codeField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			this(null, codeField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{ }

		public EarningsAccountAttribute(Type branchID, Type codeField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, codeField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{ }

		public EarningsAccountAttribute(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, SearchType, codeField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{ }
		#endregion Constructors

		protected override Type SetupField => typeof(PRSetup.earningsAcctDefault);
		protected override Type SetupAlternateField => typeof(PRSetup.earningsAlternateAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.EarningsAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.EarningsAcctID;
		protected override Type EarningTypeAccountField => typeof(PREarningType.earningsAcctID);
		protected override Type InventoryItemAccountField => typeof(PRxInventoryItem.earningsAcctID);
		protected override Type ProjectAccountField => typeof(PMProjectExtension.earningsAcctID);
		protected override Type TaskAccountField => typeof(PRxPMTask.earningsAcctID);
	}

	public class DedLiabilityAccountAttribute : PRAccountAttribute
	{
		protected Type _DeductionCodeIDField;

		#region Constructors
		public DedLiabilityAccountAttribute(Type codeField, Type employeeField, Type payGroupField, Type deductionCodeIDField) : this(null, codeField, employeeField, payGroupField, deductionCodeIDField)
		{
		}

		public DedLiabilityAccountAttribute(Type branchID, Type codeField, Type employeeField, Type payGroupField, Type deductionCodeIDField) : base(branchID, codeField, employeeField, payGroupField)
		{
			_DeductionCodeIDField = deductionCodeIDField;
		}

		public DedLiabilityAccountAttribute(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField, Type deductionCodeIDField) : base(branchID, SearchType, codeField, employeeField, payGroupField)
		{
			_DeductionCodeIDField = deductionCodeIDField;
		}
		#endregion Constructors

		protected override Type SetupField => typeof(PRSetup.deductLiabilityAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.DedLiabilityAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.DedLiabilityAcctID;

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRDeductAcctSubDefault.MaskDeductionCode:
					var deductionCodeID = cache.GetValue(row, _DeductionCodeIDField.Name);
					var deductionBenefitCode = (PRDeductCode)SelectFrom<PRDeductCode>.Where<PRDeductCode.codeID.IsEqual<P.AsInt>>.View.Select(cache.Graph, deductionCodeID);
					return deductionBenefitCode?.DedLiabilityAcctID;
			}

			return null;
		}
	}

	public class BenExpenseAccountAttribute : PRExpenseAccountAttribte
	{
		protected Type _BenefitCodeIDField;

		public BenExpenseAccountAttribute(Type branchID, Type benefitCodeIDField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, benefitCodeIDField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{
			_BenefitCodeIDField = benefitCodeIDField;
		}

		protected override Type SetupField => typeof(PRSetup.benefitExpenseAcctDefault);
		protected override Type SetupAlternateField => typeof(PRSetup.benefitExpenseAlternateAcctDefault);

		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.BenefitExpenseAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.BenefitExpenseAcctID;
		protected override Type EarningTypeAccountField => typeof(PREarningType.benefitExpenseAcctID);
		protected override Type InventoryItemAccountField => typeof(PRxInventoryItem.benefitExpenseAcctID);
		protected override Type ProjectAccountField => typeof(PMProjectExtension.benefitExpenseAcctID);
		protected override Type TaskAccountField => typeof(PRxPMTask.benefitExpenseAcctID);

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRDeductAcctSubDefault.MaskDeductionCode:
					var deductionCodeID = cache.GetValue(row, _BenefitCodeIDField.Name);
					var deductionBenefitCode = (PRDeductCode)SelectFrom<PRDeductCode>.Where<PRDeductCode.codeID.IsEqual<P.AsInt>>.View.Select(cache.Graph, deductionCodeID);
					return deductionBenefitCode?.BenefitExpenseAcctID;
			}

			return base.GetAccountValue(cache, setupFieldValue, row);
		}

		protected override void RowInsertedUpdated(PXCache sender, object row, object oldRow)
		{
			if (row == null)
			{
				return;
			}

			object setupFieldValue = CacheHelper.GetValue(sender.Graph, GetPRSetup(sender.Graph), SetupField);
			if (!Equals(sender.GetValue(row, _BenefitCodeIDField.Name), sender.GetValue(oldRow, _BenefitCodeIDField.Name)) && setupFieldValue.Equals(PRDeductAcctSubDefault.MaskDeductionCode))
			{
				sender.SetDefaultExt(row, FieldName);
			}

			base.RowInsertedUpdated(sender, row, oldRow);
		}
	}

	public class BenLiabilityAccountAttribute : PRAccountAttribute
	{
		protected Type _BenefitCodeIDField;
		protected Type _IsPayableBenefitField;

		#region Constructors
		public BenLiabilityAccountAttribute(Type branchID, Type codeField, Type employeeField, Type payGroupField, Type benefitCodeIDField, Type isPayableBenefitField) : base(branchID, codeField, employeeField, payGroupField)
		{
			_BenefitCodeIDField = benefitCodeIDField;
			_IsPayableBenefitField = isPayableBenefitField;
		}

		public BenLiabilityAccountAttribute(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField, Type benefitCodeIDField, Type isPayableBenefitField) : base(branchID, SearchType, codeField, employeeField, payGroupField)
		{
			_BenefitCodeIDField = benefitCodeIDField;
			_IsPayableBenefitField = isPayableBenefitField;
		}
		#endregion Constructors

		protected override Type SetupField => typeof(PRSetup.benefitLiabilityAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.BenefitLiabilityAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.BenefitLiabilityAcctID;

		protected override bool IsFieldRequired(PXCache cache, object row)
		{
			return cache.GetValue(row, _IsPayableBenefitField.Name) as bool? != true;
		}

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRDeductAcctSubDefault.MaskDeductionCode:
					var deductionCodeID = cache.GetValue(row, _BenefitCodeIDField.Name);
					var deductionBenefitCode = (PRDeductCode)SelectFrom<PRDeductCode>.Where<PRDeductCode.codeID.IsEqual<P.AsInt>>.View.Select(cache.Graph, deductionCodeID);
					return deductionBenefitCode?.BenefitLiabilityAcctID;
			}

			return null;
		}
	}

	public class TaxExpenseAccountAttribute : PRExpenseAccountAttribte
	{
		protected Type _TaxCodeIDField;
		protected Type _TaxCategoryField;


		public TaxExpenseAccountAttribute(Type branchID, Type taxCodeIDField, Type employeeField, Type payGroupField, Type taxCategoryField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(branchID, taxCodeIDField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{
			_TaxCodeIDField = taxCodeIDField;
			_TaxCategoryField = taxCategoryField;
		}

		protected override Type SetupField => typeof(PRSetup.taxExpenseAcctDefault);
		protected override Type SetupAlternateField => typeof(PRSetup.taxExpenseAlternateAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.PayrollTaxExpenseAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.TaxExpenseAcctID;
		protected override Type EarningTypeAccountField => typeof(PREarningType.taxExpenseAcctID);
		protected override Type InventoryItemAccountField => typeof(PRxInventoryItem.taxExpenseAcctID);
		protected override Type ProjectAccountField => typeof(PMProjectExtension.taxExpenseAcctID);
		protected override Type TaskAccountField => typeof(PRxPMTask.taxExpenseAcctID);

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRTaxAcctSubDefault.MaskTaxCode:
					var taxCodeID = cache.GetValue(row, _TaxCodeIDField.Name);
					var taxCode = (PRTaxCode)SelectFrom<PRTaxCode>.Where<PRTaxCode.taxID.IsEqual<P.AsInt>>.View.Select(cache.Graph, taxCodeID);
					return taxCode?.ExpenseAcctID;
			}

			return base.GetAccountValue(cache, setupFieldValue, row);
		}

		#region Events
		protected override void FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
		{
			//Only Employer Tax needs to default the Expense Account, otherwise it should stay null.
			if ((string)cache.GetValue(e.Row, _TaxCategoryField.Name) == TaxCategory.EmployerTax)
			{
				base.FieldDefaulting(cache, e);
			}
			else
			{
				e.Cancel = true;
			}
		}

		protected override void RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			if ((string)cache.GetValue(e.Row, _TaxCategoryField.Name) == TaxCategory.EmployerTax)
			{
				base.RowSelected(cache, e);
			}
		}

		public override void OptionalFieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			base.FieldVerifying(cache, e);
			if (e.Row == null)
			{
				return;
			}

			if ((string)cache.GetValue(e.Row, _TaxCategoryField.Name) == TaxCategory.EmployerTax)
			{
				base.OptionalFieldVerifying(cache, e);
			}
		}

		protected override void RowInsertedUpdated(PXCache sender, object row, object oldRow)
		{
			if (row == null)
			{
				return;
			}

			object setupFieldValue = CacheHelper.GetValue(sender.Graph, GetPRSetup(sender.Graph), SetupField);
			if (!Equals(sender.GetValue(row, _TaxCodeIDField.Name), sender.GetValue(oldRow, _TaxCodeIDField.Name)) && setupFieldValue.Equals(PRTaxAcctSubDefault.MaskTaxCode))
			{
				sender.SetDefaultExt(row, FieldName);
			}

			base.RowInsertedUpdated(sender, row, oldRow);
		}
		#endregion Events
	}

	public class TaxLiabilityAccountAttribute : PRAccountAttribute
	{
		protected Type _TaxCodeIDField;

		#region Constructors
		public TaxLiabilityAccountAttribute(Type codeField, Type employeeField, Type payGroupField, Type taxCodeIDField) : this(null, codeField, employeeField, payGroupField, taxCodeIDField)
		{
		}

		public TaxLiabilityAccountAttribute(Type branchID, Type codeField, Type employeeField, Type payGroupField, Type taxCodeIDField) : base(branchID, codeField, employeeField, payGroupField)
		{
			_TaxCodeIDField = taxCodeIDField;
		}

		public TaxLiabilityAccountAttribute(Type branchID, Type SearchType, Type codeField, Type employeeField, Type payGroupField, Type taxCodeIDField) : base(branchID, SearchType, codeField, employeeField, payGroupField)
		{
			_TaxCodeIDField = taxCodeIDField;
		}
		#endregion Constructors

		protected override Type SetupField => typeof(PRSetup.taxLiabilityAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.PayrollTaxLiabilityAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.TaxLiabilityAcctID;

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRTaxAcctSubDefault.MaskTaxCode:
					var taxCodeID = cache.GetValue(row, _TaxCodeIDField.Name);
					var taxCode = (PRTaxCode)SelectFrom<PRTaxCode>.Where<PRTaxCode.taxID.IsEqual<P.AsInt>>.View.Select(cache.Graph, taxCodeID);
					return taxCode?.LiabilityAcctID;
			}

			return null;
		}
	}

	public class PTOExpenseAccountAttribute : PRExpenseAccountAttribte
	{
		protected Type _PTOBankIDField;

		public PTOExpenseAccountAttribute(Type ptoBankIDField, Type employeeField, Type payGroupField, Type earningCodeField, Type laborItemField, Type projectIDField, Type taskIDField) :
			base(null, ptoBankIDField, employeeField, payGroupField, earningCodeField, laborItemField, projectIDField, taskIDField)
		{
			_PTOBankIDField = ptoBankIDField;
		}

		protected override Type SetupField => typeof(PRSetup.ptoExpenseAcctDefault);
		protected override Type SetupAlternateField => typeof(PRSetup.ptoExpenseAlternateAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.PTOExpenseAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.PTOExpenseAcctID;
		protected override Type EarningTypeAccountField => typeof(PREarningType.ptoExpenseAcctID);
		protected override Type InventoryItemAccountField => typeof(PRxInventoryItem.ptoExpenseAcctID);
		protected override Type ProjectAccountField => typeof(PMProjectExtension.ptoExpenseAcctID);
		protected override Type TaskAccountField => typeof(PRxPMTask.ptoExpenseAcctID);

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRPTOAcctSubDefault.MaskPTOBank:
					object bankID = cache.GetValue(row, _PTOBankIDField.Name);
					PRPTOBank ptoBank = new SelectFrom<PRPTOBank>.Where<PRPTOBank.bankID.IsEqual<P.AsString>>.View(cache.Graph).SelectSingle(bankID);
					return ptoBank?.PTOExpenseAcctID;
			}

			return base.GetAccountValue(cache, setupFieldValue, row);
		}
	}

	public class PTOLiabilityAccountAttribute : PRAccountAttribute
	{
		protected Type _BankIDField;

		public PTOLiabilityAccountAttribute(Type codeField, Type employeeField, Type payGroupField) : base(codeField, employeeField, payGroupField)
		{
			_BankIDField = codeField;
		}

		protected override Type SetupField => typeof(PRSetup.ptoLiabilityAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.PTOLiabilityAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.PTOLiabilityAcctID;

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRPTOAcctSubDefault.MaskPTOBank:
					object bankID = cache.GetValue(row, _BankIDField.Name);
					PRPTOBank bank = new SelectFrom<PRPTOBank>.Where<PRPTOBank.bankID.IsEqual<P.AsString>>.View(cache.Graph).SelectSingle(bankID);
					return bank?.PTOLiabilityAcctID;
			}

			return null;
		}
	}

	public class PTOAssetAccountAttribute : PRAccountAttribute
	{
		protected Type _BankIDField;

		public PTOAssetAccountAttribute(Type codeField, Type employeeField, Type payGroupField) : base(codeField, employeeField, payGroupField)
		{
			_BankIDField = codeField;
		}

		protected override Type SetupField => typeof(PRSetup.ptoAssetAcctDefault);
		protected override int? GetAcctIDFromEmployee(PREmployee employee) => employee.PTOAssetAcctID;
		protected override int? GetAcctIDFromPayGroup(PRPayGroup payGroup) => payGroup.PTOAssetAcctID;

		protected override int? GetAccountValue(PXCache cache, object setupFieldValue, object row)
		{
			switch (setupFieldValue)
			{
				case PRPTOAcctSubDefault.MaskPTOBank:
					string bankID = cache.GetValue(row, _BankIDField.Name) as string;
					PRPTOBank bank = PRPTOBank.PK.Find(cache.Graph, bankID);
					return bank?.PTOAssetAcctID;
			}

			return null;
		}
	}
}
