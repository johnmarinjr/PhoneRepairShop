using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public abstract class PRSubaccountAttribute : SubAccountAttribute
	{
		public bool CheckIfEmpty { get; set; } = true;
		public Type EmployeeIDField { get; set; } = typeof(PREarningDetail.employeeID);
		public Type PayGroupIDField { get; set; } = typeof(PRPayment.payGroupID);
		protected abstract List<Dependency> DefaultingDependencies { get; set; }
		protected abstract Type SetupField { get; }

		#region Data view classes
		protected class LocationAddress : SelectFrom<LocationExtAddress>
				.InnerJoin<GL.Branch>.On<GL.Branch.bAccountID.IsEqual<LocationExtAddress.bAccountID>>
				.Where<GL.Branch.branchID.IsEqual<P.AsInt>>.View
		{
			public LocationAddress(PXGraph graph) : base(graph)
			{
			}
		}

		protected class Employee : SelectFrom<PREmployee>
				.Where<PREmployee.bAccountID.IsEqual<P.AsInt>>.View
		{
			public Employee(PXGraph graph) : base(graph)
			{
			}
		}

		protected class PayGroup : SelectFrom<PRPayGroup>
				.Where<PRPayGroup.payGroupID.IsEqual<P.AsString>>.View
		{
			public PayGroup(PXGraph graph) : base(graph)
			{
			}
		}
		#endregion Data view classes

		#region Constructors
		public PRSubaccountAttribute() { }

		public PRSubaccountAttribute(Type AccountType) : base(AccountType) { }

		public PRSubaccountAttribute(Type AccountType, Type BranchType, bool AccountAndBranchRequired = false) : base(AccountType, BranchType, AccountAndBranchRequired) { }
		#endregion Constructors

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldDefaulting.AddHandler(sender.GetItemType(), FieldName, FieldDefaulting);
			sender.Graph.RowUpdated.AddHandler(sender.GetItemType(), RowUpdated);
			sender.Graph.RowInserted.AddHandler(sender.GetItemType(), RowInserted);
			if (CheckIfEmpty)
			{
				sender.Graph.FieldVerifying.AddHandler(sender.GetItemType(), FieldName, FieldVerifying);
			}
		}

		#region Helpers

		protected class Dependency
		{
			public Dependency(string fieldName, string accountSubSource)
			{
				FieldName = fieldName;
				AccountSubSource = accountSubSource;
			}

			public string FieldName { get; set; }
			public string AccountSubSource { get; set; }
		}

		protected static object GetValue<Field>(PXGraph graph, object data)
			where Field : IBqlField
		{
			return CacheHelper.GetValue(graph, data, typeof(Field));
		}

		protected void SetDefaultValue(PXCache cache, object row)
		{
			try
			{
				object currentValue = cache.GetValue(row, FieldName);
				if (!cache.Graph.IsImportFromExcel || currentValue == null)
				{
					cache.SetDefaultExt(row, FieldName);
				}
				else
				{
					cache.RaiseFieldUpdating(FieldName, row, ref currentValue);
					cache.SetValueExt(row, FieldName, currentValue);
				}
			}
			catch (PXSetPropertyException)
			{
				cache.SetValue(row, FieldName, null);
			}
		}

		protected bool HasImpactOnSubDefault(PXCache cache, string fieldName, string mask, object rowA, object rowB)
		{
			// != operator doesn't compare correctly, we need object.Equals
			return !object.Equals(cache.GetValue(rowA, fieldName), cache.GetValue(rowB, fieldName)) && SubMaskContainsValue(cache, mask);
		}

		protected virtual bool SubMaskContainsValue(PXCache cache, string compareValue)
		{
			return SubMaskContainsValue(cache, compareValue, SetupField);
		}

		protected virtual bool SubMaskContainsValue(PXCache cache, string compareValue, Type setupField)
		{
			PXCache setupCache = cache.Graph.Caches[typeof(PRSetup)];
			PRSetup setup = setupCache.Current as PRSetup ?? new SelectFrom<PRSetup>.View(cache.Graph).SelectSingle();
			string subMask = setupCache.GetValue(setup, setupField.Name) as string;
			return PRSetupMaint.SubMaskContainsValue(setupCache, setup, setupField, subMask, compareValue);
		}

		#endregion Helpers

		#region Events
		public override void FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
		{
			base.FieldDefaulting(cache, e);
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetError(cache, e.Row, FieldName, null);
				var maskSources = GetMaskSources(cache, e.Row);
				object value = null;
				try
				{
					var setup = (PRSetup)SelectFrom<PRSetup>.View.Select(cache.Graph);
					if (setup != null)
					{
						cache.Current = e.Row;
						value = MakeSub(cache.Graph, setup, maskSources);
						cache.RaiseFieldUpdating(FieldName, e.Row, ref value);
					}
				}
				catch (PXMaskArgumentException)
				{
					value = null;
				}
				catch (PXSetPropertyException)
				{
					value = null;
				}

				e.NewValue = (int?)value;
				e.Cancel = true;
			}
		}

		public override void RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
		{
			if (CheckIfEmpty && cache.GetValue(e.Row, FieldName) == null && IsFieldRequired(cache, e.Row))
			{
				PXUIFieldAttribute.SetError(cache, e.Row, this.FieldName, PXMessages.LocalizeFormat(Messages.PostingValueNotFound, this.FieldName));
			}

			base.RowPersisting(cache, e);
		}

		protected virtual void FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (e.NewValue == null && IsFieldRequired(cache, e.Row))
			{
				PXUIFieldAttribute.SetWarning(cache, e.Row, this.FieldName, PXMessages.LocalizeFormat(Messages.PostingValueNotFound, this.FieldName));
			}
		}

		protected virtual void RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
		{
			RowInsertedUpdated(cache, e.Row, e.OldRow);
		}

		protected virtual void RowInserted(PXCache cache, PXRowInsertedEventArgs e)
		{
			RowInsertedUpdated(cache, e.Row, null);
		}

		protected virtual void RowInsertedUpdated(PXCache cache, object row, object oldRow)
		{
			if (row == null)
			{
				return;
			}

			if (DefaultingDependencies.Any(dependency => HasImpactOnSubDefault(cache, dependency.FieldName, dependency.AccountSubSource, row, oldRow)))
			{
				SetDefaultValue(cache, row);
			}
		}

		#endregion Events

		protected abstract Type[] MaskFieldTypes { get; }
		public abstract object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources);
		protected abstract IEnumerable<object> GetMaskSources(PXCache cache, object row);

		protected virtual bool IsFieldRequired(PXCache cache, object row)
		{
			return PXAccess.FeatureInstalled<FeaturesSet.subAccount>();
		}
	}

	public abstract class ExpenseSubAccountAttribute : PRSubaccountAttribute
	{
		protected abstract Type AlternateSetupField { get; }

		protected Type _EarningTypeField;
		protected Type _LaborItemField;
		protected Type _BranchField;
		protected Type _ProjectField;
		protected Type _TaskField;

		public ExpenseSubAccountAttribute(Type accountType, Type branchType, Type earningTypeField, Type laborItemField, Type projectField, Type taskField, bool accountAndBranchRequired = false) :
			base(accountType, branchType, accountAndBranchRequired)
		{
			_BranchField = branchType;
			_EarningTypeField = earningTypeField;
			_LaborItemField = laborItemField;
			_ProjectField = projectField;
			_TaskField = taskField;
		}

		protected ExpenseMaskSources GetCommonMaskSources(PXCache cache, object row)
		{
			LocationExtAddress locationAddress = null;
			if (_BranchField != null)
			{
				locationAddress = (LocationExtAddress)LocationAddress.Select(cache.Graph, cache.GetValue(row, _BranchField.Name));
			}
			var employee = (PREmployee)Employee.Select(cache.Graph, (int?)cache.GetValue(row, EmployeeIDField.Name));
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			var earningType = PXSelectorAttribute.Select(cache, row, _EarningTypeField.Name);
			var laborItem = PXSelectorAttribute.Select(cache, row, _LaborItemField.Name);
			var project = PXSelectorAttribute.Select(cache, row, _ProjectField.Name);
			var task = PXSelectorAttribute.Select(cache, row, _TaskField.Name);

			return new ExpenseMaskSources()
			{
				branchSubID = locationAddress == null ? null : GetValue<PRLocationExtAddress.cMPPayrollSubID>(cache.Graph, locationAddress),
				employeeSubID = CacheHelper.GetValue(cache.Graph, employee, EmployeeSubIDField),
				payGroupSubID = CacheHelper.GetValue(cache.Graph, payGroup, PayGroupSubIDField),
				earningTypeSubID = CacheHelper.GetValue(cache.Graph, earningType, EarningTypeSubIDField),
				laborItemSubID = CacheHelper.GetValue(cache.Graph, laborItem, LaborItemSubIDField),
				projectSubID = CacheHelper.GetValue(cache.Graph, project, ProjectSubIDField),
				taskSubID = CacheHelper.GetValue(cache.Graph, task, TaskSubIDField)
			};
		}

		protected virtual bool UseAlternateSubAccount(PXGraph graph, string mask)
		{
			if (mask.Contains(GLAccountSubSource.LaborItem))
			{
				var value = CacheHelper.GetCurrentValue(graph, _LaborItemField);
				if (value == null)
				{
					return true;
				}
			}
			if (mask.Contains(GLAccountSubSource.Project) || mask.Contains(GLAccountSubSource.Task))
			{
				var value = (int?)CacheHelper.GetCurrentValue(graph, _ProjectField);
				if (value == null || value == 0)
				{
					return true;
				}
			}

			return false;
		}

		protected override bool SubMaskContainsValue(PXCache cache, string compareValue)
		{
			return base.SubMaskContainsValue(cache, compareValue) || SubMaskContainsValue(cache, compareValue, AlternateSetupField);
		}

		protected Type BranchSubIDAttribute => typeof(PRLocationExtAddress.cMPPayrollSubID);
		protected abstract Type EmployeeSubIDField { get; }
		protected abstract Type PayGroupSubIDField { get; }
		protected abstract Type EarningTypeSubIDField { get; }
		protected abstract Type LaborItemSubIDField { get; }
		protected abstract Type ProjectSubIDField { get; }
		protected abstract Type TaskSubIDField { get; }

		protected struct ExpenseMaskSources
		{
			public object branchSubID;
			public object employeeSubID;
			public object payGroupSubID;
			public object benefitSubID;
			public object earningTypeSubID;
			public object laborItemSubID;
			public object projectSubID;
			public object taskSubID;
		}
	}

	public class EarningSubAccountAttribute : ExpenseSubAccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PREarningDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PREarningDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PREarningDetail.typeCD).Name, GLAccountSubSource.EarningType ),
			new Dependency(typeof(PREarningDetail.labourItemID).Name, GLAccountSubSource.LaborItem),
			new Dependency(typeof(PREarningDetail.projectID).Name, GLAccountSubSource.Project),
			new Dependency(typeof(PREarningDetail.projectID).Name, GLAccountSubSource.Task),
			new Dependency(typeof(PREarningDetail.projectTaskID).Name, GLAccountSubSource.Task),
		};

		public EarningSubAccountAttribute(Type AccountType, Type BranchType, bool AccountAndBranchRequired = false) :
			base(AccountType, BranchType, typeof(PREarningDetail.typeCD), typeof(PREarningDetail.labourItemID), typeof(PREarningDetail.projectID), typeof(PREarningDetail.projectTaskID), AccountAndBranchRequired)
		{
		}

		protected override Type[] MaskFieldTypes => new[]
		{
			BranchSubIDAttribute,
			EmployeeSubIDField,
			PayGroupSubIDField,
			EarningTypeSubIDField,
			LaborItemSubIDField,
			ProjectSubIDField,
			TaskSubIDField,
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			ExpenseMaskSources sources = GetCommonMaskSources(cache, row);

			//Be careful, this array order needs to match with PREarningsAcctSubDefault.SubListAttribute (used in PREarningsSubAccountMaskAttribute)
			return new object[] { sources.branchSubID, sources.employeeSubID, sources.payGroupSubID, sources.earningTypeSubID, sources.laborItemSubID, sources.projectSubID, sources.taskSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			if (UseAlternateSubAccount(graph, setup.EarningsSubMask))
			{
				if (setup.EarningsAlternateSubMask == null)
				{
					return null;
				}

				return PREarningsSubAccountMaskAttribute.MakeSub<PRSetup.earningsAlternateSubMask>(graph, setup.EarningsAlternateSubMask, maskSources.ToArray(), MaskFieldTypes);
			}

			return PREarningsSubAccountMaskAttribute.MakeSub<PRSetup.earningsSubMask>(graph, setup.EarningsSubMask, maskSources.ToArray(), MaskFieldTypes);
		}

		protected override Type EmployeeSubIDField => typeof(PREmployee.earningsSubID);
		protected override Type PayGroupSubIDField => typeof(PRPayGroup.earningsSubID);
		protected override Type EarningTypeSubIDField => typeof(PREarningType.earningsSubID);
		protected override Type LaborItemSubIDField => typeof(PRxInventoryItem.earningsSubID);
		protected override Type ProjectSubIDField => typeof(PMProjectExtension.earningsSubID);
		protected override Type TaskSubIDField => typeof(PRxPMTask.earningsSubID);
		protected override Type SetupField => typeof(PRSetup.earningsSubMask);
		protected override Type AlternateSetupField => typeof(PRSetup.earningsAlternateSubMask);
	}

	public class DedLiabilitySubAccountAttribute : PRSubaccountAttribute
	{

		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRDeductionDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PRDeductionDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PRDeductionDetail.codeID).Name, GLAccountSubSource.DeductionCode),
		};

		#region Constructors
		public DedLiabilitySubAccountAttribute()
		{
		}

		public DedLiabilitySubAccountAttribute(Type AccountType) : base(AccountType)
		{
		}

		public DedLiabilitySubAccountAttribute(Type AccountType, Type BranchType, bool AccountAndBranchRequired = false) : base(AccountType, BranchType, AccountAndBranchRequired)
		{
		}
		#endregion Constructors

		protected override Type[] MaskFieldTypes { get; } = {
			typeof(PRLocationExtAddress.cMPPayrollSubID),
			typeof(PREmployee.dedLiabilitySubID),
			typeof(PRPayGroup.dedLiabilitySubID),
			typeof(PRDeductCode.dedLiabilitySubID),
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			var line = row as PRDeductionDetail;
			var locationAddress = (LocationExtAddress)LocationAddress.Select(cache.Graph, line.BranchID);
			var employee = (PREmployee)Employee.Select(cache.Graph, line.EmployeeID);
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			var deductionCode = PXSelectorAttribute.Select<PRDeductionDetail.codeID>(cache, line);

			var branchSubID = GetValue<PRLocationExtAddress.cMPPayrollSubID>(cache.Graph, locationAddress);
			var employeeSubID = GetValue<PREmployee.dedLiabilitySubID>(cache.Graph, employee);
			var payGroupSubID = GetValue<PRPayGroup.dedLiabilitySubID>(cache.Graph, payGroup);
			var dedLiabilitySubID = GetValue<PRDeductCode.dedLiabilitySubID>(cache.Graph, deductionCode);

			//Be careful, this array order needs to match with PRDeductAcctSubDefault.SubListAttribute (used in PRDeductSubAccountMaskAttribute)
			return new object[] { branchSubID, employeeSubID, payGroupSubID, dedLiabilitySubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			return PRDeductSubAccountMaskAttribute.MakeSub<PRSetup.deductLiabilitySubMask>(graph, setup.DeductLiabilitySubMask, maskSources.ToArray(), MaskFieldTypes);
		}
		
		protected override Type SetupField => typeof(PRSetup.deductLiabilitySubMask);
	}

	public class BenExpenseSubAccountAttribute : ExpenseSubAccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRBenefitDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PRBenefitDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PRBenefitDetail.earningTypeCD).Name, GLAccountSubSource.DeductionCode),
			new Dependency(typeof(PRBenefitDetail.codeID).Name, GLAccountSubSource.DeductionCode),
			new Dependency(typeof(PRBenefitDetail.labourItemID).Name, GLAccountSubSource.LaborItem),
			new Dependency(typeof(PRBenefitDetail.projectID).Name, GLAccountSubSource.Project),
			new Dependency(typeof(PRBenefitDetail.projectID).Name, GLAccountSubSource.Task),
			new Dependency(typeof(PRBenefitDetail.projectTaskID).Name, GLAccountSubSource.Task),
		};

		public BenExpenseSubAccountAttribute(Type AccountType, Type BranchType, bool AccountAndBranchRequired = false) :
			base(AccountType, BranchType, typeof(PRBenefitDetail.earningTypeCD), typeof(PRBenefitDetail.labourItemID), typeof(PRBenefitDetail.projectID), typeof(PRBenefitDetail.projectTaskID), AccountAndBranchRequired)
		{
		}

		protected override Type[] MaskFieldTypes => new[]
		{
			BranchSubIDAttribute,
			EmployeeSubIDField,
			PayGroupSubIDField,
			typeof(PRDeductCode.benefitExpenseSubID),
			EarningTypeSubIDField,
			LaborItemSubIDField,
			ProjectSubIDField,
			TaskSubIDField,
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			ExpenseMaskSources sources = GetCommonMaskSources(cache, row);

			var line = row as PRBenefitDetail;
			var deductionCode = PXSelectorAttribute.Select<PRBenefitDetail.codeID>(cache, line);
			var benefitSubID = GetValue<PRDeductCode.benefitExpenseSubID>(cache.Graph, deductionCode);

			//Be careful, this array order needs to match with PRBenefitExpenseAcctSubDefault.SubListAttribute (used in PRBenefitExpenseSubAccountMaskAttribute)
			return new object[] { sources.branchSubID, sources.employeeSubID, sources.payGroupSubID, benefitSubID, sources.earningTypeSubID, sources.laborItemSubID, sources.projectSubID, sources.taskSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			if (UseAlternateSubAccount(graph, setup.BenefitExpenseSubMask))
			{
				if (setup.BenefitExpenseAlternateSubMask == null)
				{
					return null;
				}

				return PRBenefitExpenseSubAccountMaskAttribute.MakeSub<PRSetup.benefitExpenseAlternateSubMask>(graph, setup.BenefitExpenseAlternateSubMask, maskSources.ToArray(), MaskFieldTypes);
			}

			return PRBenefitExpenseSubAccountMaskAttribute.MakeSub<PRSetup.benefitExpenseSubMask>(graph, setup.BenefitExpenseSubMask, maskSources.ToArray(), MaskFieldTypes);
		}

		protected override Type EmployeeSubIDField => typeof(PREmployee.benefitExpenseSubID);
		protected override Type PayGroupSubIDField => typeof(PRPayGroup.benefitExpenseSubID);
		protected override Type EarningTypeSubIDField => typeof(PREarningType.benefitExpenseSubID);
		protected override Type LaborItemSubIDField => typeof(PRxInventoryItem.benefitExpenseSubID);
		protected override Type ProjectSubIDField => typeof(PMProjectExtension.benefitExpenseSubID);
		protected override Type TaskSubIDField => typeof(PRxPMTask.benefitExpenseSubID);

		protected override Type SetupField => typeof(PRSetup.benefitExpenseSubMask);
		protected override Type AlternateSetupField => typeof(PRSetup.benefitExpenseAlternateSubMask);
	}

	public class BenLiabilitySubAccountAttribute : PRSubaccountAttribute
	{
		protected Type _IsPayableBenefitField;
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRBenefitDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PRBenefitDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PRBenefitDetail.codeID).Name, GLAccountSubSource.DeductionCode),

		};

		#region Constructors
		public BenLiabilitySubAccountAttribute()
		{
		}

		public BenLiabilitySubAccountAttribute(Type AccountType) : base(AccountType)
		{
		}

		public BenLiabilitySubAccountAttribute(Type AccountType, Type BranchType, Type isPayableBenefitField, bool AccountAndBranchRequired = false) : base(AccountType, BranchType, AccountAndBranchRequired)
		{
			_IsPayableBenefitField = isPayableBenefitField;
		}
		#endregion Constructors

		protected override Type[] MaskFieldTypes { get; } = {
			typeof(PRLocationExtAddress.cMPPayrollSubID),
			typeof(PREmployee.benefitLiabilitySubID),
			typeof(PRPayGroup.benefitLiabilitySubID),
			typeof(PRDeductCode.benefitLiabilitySubID)
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			var line = row as PRBenefitDetail;
			var locationAddress = (LocationExtAddress)LocationAddress.Select(cache.Graph, line.BranchID);
			var employee = (PREmployee)Employee.Select(cache.Graph, line.EmployeeID);
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			var deductionCode = PXSelectorAttribute.Select<PRBenefitDetail.codeID>(cache, line);

			var branchSubID = GetValue<PRLocationExtAddress.cMPPayrollSubID>(cache.Graph, locationAddress);
			var employeeSubID = GetValue<PREmployee.benefitLiabilitySubID>(cache.Graph, employee);
			var payGroupSubID = GetValue<PRPayGroup.benefitLiabilitySubID>(cache.Graph, payGroup);
			var benefitSubID = GetValue<PRDeductCode.benefitLiabilitySubID>(cache.Graph, deductionCode);

			//Be careful, this array order needs to match with PRDeductAcctSubDefault.SubListAttribute (used in PRDeductSubAccountMaskAttribute)
			return new object[] { branchSubID, employeeSubID, payGroupSubID, benefitSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			var cache = graph.Caches[base.BqlTable];
			if (IsPayableBenefit(cache, cache.Current))
				return null;

			return PRDeductSubAccountMaskAttribute.MakeSub<PRSetup.benefitLiabilitySubMask>(graph, setup.BenefitLiabilitySubMask, maskSources.ToArray(), MaskFieldTypes);
		}

		protected override Type SetupField => typeof(PRSetup.benefitLiabilitySubMask);

		protected override bool IsFieldRequired(PXCache cache, object row)
		{
			return base.IsFieldRequired(cache, row) && !IsPayableBenefit(cache, row);
		}

		private bool IsPayableBenefit(PXCache cache, object row)
		{
			return cache.GetValue(row, _IsPayableBenefitField.Name) as bool? == true; 
		}
	}

	public class TaxExpenseSubAccountAttribute : ExpenseSubAccountAttribute
	{
		protected Type _TaxCategoryField;
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRTaxDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PRTaxDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PRTaxDetail.taxID).Name, GLAccountSubSource.TaxCode),
			new Dependency(typeof(PRTaxDetail.earningTypeCD).Name, GLAccountSubSource.EarningType),
			new Dependency(typeof(PRTaxDetail.labourItemID).Name, GLAccountSubSource.LaborItem),
			new Dependency(typeof(PRTaxDetail.projectID).Name, GLAccountSubSource.Project),
			new Dependency(typeof(PRTaxDetail.projectID).Name, GLAccountSubSource.Task),
			new Dependency(typeof(PRTaxDetail.projectTaskID).Name, GLAccountSubSource.Task),
		};

		public TaxExpenseSubAccountAttribute(Type AccountType, Type BranchType, Type taxCategoryField, bool AccountAndBranchRequired = false) :
			base(AccountType, BranchType, typeof(PRTaxDetail.earningTypeCD), typeof(PRTaxDetail.labourItemID), typeof(PRTaxDetail.projectID), typeof(PRTaxDetail.projectTaskID), AccountAndBranchRequired)
		{
			_TaxCategoryField = taxCategoryField;
		}

		#region Events
		public override void RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			if ((string)cache.GetValue(e.Row, _TaxCategoryField.Name) == TaxCategory.EmployerTax)
			{
				base.RowPersisting(cache, e);
			}
		}

		protected override void FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			if ((string)cache.GetValue(e.Row, _TaxCategoryField.Name) == TaxCategory.EmployerTax)
			{
				base.FieldVerifying(cache, e);
			}
		}

		public override void FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
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

		#endregion Events

		protected override Type[] MaskFieldTypes => new[]
		{
			BranchSubIDAttribute,
			EmployeeSubIDField,
			PayGroupSubIDField,
			typeof(PRTaxCode.expenseSubID),
			EarningTypeSubIDField,
			LaborItemSubIDField,
			ProjectSubIDField,
			TaskSubIDField,
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			ExpenseMaskSources sources = GetCommonMaskSources(cache, row);

			var line = row as PRTaxDetail;
			var taxCode = PXSelectorAttribute.Select<PRTaxDetail.taxID>(cache, line);
			var taxSubID = GetValue<PRTaxCode.expenseSubID>(cache.Graph, taxCode);

			//Be careful, this array order needs to match with PRTaxExpenseAcctSubDefault.SubListAttribute (used in PRTaxExpenseSubAccountMaskAttribute)
			return new object[] { sources.branchSubID, sources.employeeSubID, sources.payGroupSubID, taxSubID, sources.earningTypeSubID, sources.laborItemSubID, sources.projectSubID, sources.taskSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			if (UseAlternateSubAccount(graph, setup.TaxExpenseSubMask))
			{
				if (setup.TaxExpenseAlternateSubMask == null)
				{
					return null;
				}

				return PRTaxExpenseSubAccountMaskAttribute.MakeSub<PRSetup.taxExpenseAlternateSubMask>(graph, setup.TaxExpenseAlternateSubMask, maskSources.ToArray(), MaskFieldTypes);
			}
			return PRTaxExpenseSubAccountMaskAttribute.MakeSub<PRSetup.taxExpenseSubMask>(graph, setup.TaxExpenseSubMask, maskSources.ToArray(), MaskFieldTypes);
		}

		protected override Type EmployeeSubIDField => typeof(PREmployee.payrollTaxExpenseSubID);
		protected override Type PayGroupSubIDField => typeof(PRPayGroup.taxExpenseSubID);
		protected override Type EarningTypeSubIDField => typeof(PREarningType.taxExpenseSubID);
		protected override Type LaborItemSubIDField => typeof(PRxInventoryItem.taxExpenseSubID);
		protected override Type ProjectSubIDField => typeof(PMProjectExtension.taxExpenseSubID);
		protected override Type TaskSubIDField => typeof(PRxPMTask.taxExpenseSubID);

		protected override Type SetupField => typeof(PRSetup.taxExpenseSubMask);
		protected override Type AlternateSetupField => typeof(PRSetup.taxExpenseAlternateSubMask);
	}

	public class TaxLiabilitySubAccountAttribute : PRSubaccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRTaxDetail.branchID).Name, GLAccountSubSource.Branch),
			new Dependency(typeof(PRTaxDetail.employeeID).Name, GLAccountSubSource.Employee),
			new Dependency(typeof(PRTaxDetail.taxID).Name, GLAccountSubSource.TaxCode),
		};

		#region Constructors
		public TaxLiabilitySubAccountAttribute()
		{
		}

		public TaxLiabilitySubAccountAttribute(Type AccountType) : base(AccountType)
		{
		}

		public TaxLiabilitySubAccountAttribute(Type AccountType, Type BranchType, bool AccountAndBranchRequired = false) : base(AccountType, BranchType, AccountAndBranchRequired)
		{
		}
		#endregion Constructors

		protected override Type[] MaskFieldTypes { get; } = {
			typeof(PRLocationExtAddress.cMPPayrollSubID),
			typeof(PREmployee.payrollTaxLiabilitySubID),
			typeof(PRPayGroup.taxLiabilitySubID),
			typeof(PRTaxCode.liabilitySubID)
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			var line = row as PRTaxDetail;
			var locationAddress = (LocationExtAddress)LocationAddress.Select(cache.Graph, line.BranchID);
			var employee = (PREmployee)Employee.Select(cache.Graph, line.EmployeeID);
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			var taxCode = PXSelectorAttribute.Select<PRTaxDetail.taxID>(cache, line);

			var branchSubID = GetValue<PRLocationExtAddress.cMPPayrollSubID>(cache.Graph, locationAddress);
			var employeeSubID = GetValue<PREmployee.payrollTaxLiabilitySubID>(cache.Graph, employee);
			var payGroupSubID = GetValue<PRPayGroup.taxLiabilitySubID>(cache.Graph, payGroup);
			var taxSubID = GetValue<PRTaxCode.liabilitySubID>(cache.Graph, taxCode);

			//Be careful, this array order needs to match with PRTaxAcctSubDefault.SubListAttribute (used in PRTaxSubAccountMaskAttribute)
			return new object[] { branchSubID, employeeSubID, payGroupSubID, taxSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			return PRTaxSubAccountMaskAttribute.MakeSub<PRSetup.taxLiabilitySubMask>(graph, setup.TaxLiabilitySubMask, maskSources.ToArray(), MaskFieldTypes);
		}
		protected override Type SetupField => typeof(PRSetup.taxLiabilitySubMask);
	}

	public class PTOExpenseSubAccountAttribute : ExpenseSubAccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRPTODetail.employeeID).Name, PRPTOAcctSubDefault.MaskEmployee),
			new Dependency(typeof(PRPTODetail.bankID).Name, PRPTOAcctSubDefault.MaskPTOBank ),
			new Dependency(typeof(PRPTODetail.earningTypeCD).Name, PRPTOExpenseAcctSubDefault.MaskEarningType ),
			new Dependency(typeof(PRPTODetail.labourItemID).Name, PRPTOExpenseAcctSubDefault.MaskLaborItem ),
			new Dependency(typeof(PRPTODetail.projectID).Name, PRPTOExpenseAcctSubDefault.MaskProject ),
			new Dependency(typeof(PRPTODetail.projectTaskID).Name, PRPTOExpenseAcctSubDefault.MaskTask ),
		};

		public PTOExpenseSubAccountAttribute(Type AccountType) :
			base(AccountType, null, typeof(PRPTODetail.earningTypeCD), typeof(PRPTODetail.labourItemID), typeof(PRPTODetail.projectID), typeof(PRPTODetail.projectTaskID))
		{
		}

		protected override Type[] MaskFieldTypes => new[]
		{
			EmployeeSubIDField,
			PayGroupSubIDField,
			typeof(PRPTOBank.ptoExpenseSubID),
			EarningTypeSubIDField,
			LaborItemSubIDField,
			ProjectSubIDField,
			TaskSubIDField
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			ExpenseMaskSources sources = GetCommonMaskSources(cache, row);

			var line = row as PRPTODetail;
			object ptoCode = PXSelectorAttribute.Select<PRPTODetail.bankID>(cache, line);
			object ptoSubID = GetValue<PRPTOBank.ptoExpenseSubID>(cache.Graph, ptoCode);

			//Be careful, this array order needs to match with PRPTOExpenseAcctSubDefault.SubListAttribute (used in PRPTOExpenseSubAccountMaskAttribute)
			return new object[] { sources.employeeSubID, sources.payGroupSubID, ptoSubID, sources.earningTypeSubID, sources.laborItemSubID, sources.projectSubID, sources.taskSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			if (UseAlternateSubAccount(graph, setup.PTOExpenseSubMask))
			{
				if (setup.PTOExpenseAlternateSubMask == null)
				{
					return null;
				}

				return PRPTOExpenseSubAccountMaskAttribute.MakeSub<PRSetup.ptoExpenseAlternateSubMask>(graph, setup.PTOExpenseAlternateSubMask, maskSources.ToArray(), MaskFieldTypes);
			}

			return PRPTOExpenseSubAccountMaskAttribute.MakeSub<PRSetup.ptoExpenseSubMask>(graph, setup.PTOExpenseSubMask, maskSources.ToArray(), MaskFieldTypes);
		}

		protected override Type EmployeeSubIDField => typeof(PREmployee.ptoExpenseSubID);
		protected override Type PayGroupSubIDField => typeof(PRPayGroup.ptoExpenseSubID);
		protected override Type EarningTypeSubIDField => typeof(PREarningType.ptoExpenseSubID);
		protected override Type LaborItemSubIDField => typeof(PRxInventoryItem.ptoExpenseSubID);
		protected override Type ProjectSubIDField => typeof(PMProjectExtension.ptoExpenseSubID);
		protected override Type TaskSubIDField => typeof(PRxPMTask.ptoExpenseSubID);
		protected override Type SetupField => typeof(PRSetup.ptoExpenseSubMask);
		protected override Type AlternateSetupField => typeof(PRSetup.ptoExpenseAlternateSubMask);
	}

	public class PTOLiabilitySubAccountAttribute : PRSubaccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRPTODetail.employeeID).Name, PRPTOAcctSubDefault.MaskEmployee ),
			new Dependency(typeof(PRPTODetail.bankID).Name, PRPTOAcctSubDefault.MaskPTOBank ),
		};

		public PTOLiabilitySubAccountAttribute(Type AccountType) : base(AccountType) { }

		protected override Type[] MaskFieldTypes { get; } = 
		{
			typeof(PREmployee.ptoLiabilitySubID),
			typeof(PRPayGroup.ptoLiabilitySubID),
			typeof(PRPTOBank.ptoLiabilitySubID)
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			var line = row as PRPTODetail;
			var employee = (PREmployee)Employee.Select(cache.Graph, line.EmployeeID);
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			object ptoBank = PXSelectorAttribute.Select<PRPTODetail.bankID>(cache, line);

			object employeeSubID = GetValue<PREmployee.ptoLiabilitySubID>(cache.Graph, employee);
			object payGroupSubID = GetValue<PRPayGroup.ptoLiabilitySubID>(cache.Graph, payGroup);
			object ptoSubID = GetValue<PRPTOBank.ptoLiabilitySubID>(cache.Graph, ptoBank);

			// Be careful, this array order needs to match with PRPTOAcctSubDefault.SubListAttribute (used in PRPTOSubAccountMaskAttribute)
			return new object[] { employeeSubID, payGroupSubID, ptoSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			return PRPTOSubAccountMaskAttribute.MakeSub<PRSetup.ptoLiabilitySubMask>(graph, setup.PTOLiabilitySubMask, maskSources.ToArray(), MaskFieldTypes);
		}
		protected override Type SetupField => typeof(PRSetup.ptoLiabilitySubMask);
	}

	public class PTOAssetSubAccountAttribute : PRSubaccountAttribute
	{
		protected override List<Dependency> DefaultingDependencies { get; set; } = new List<Dependency>()
		{
			new Dependency(typeof(PRPTODetail.employeeID).Name, PRPTOAcctSubDefault.MaskEmployee ),
			new Dependency(typeof(PRPTODetail.bankID).Name, PRPTOAcctSubDefault.MaskPTOBank ),
		};

		public PTOAssetSubAccountAttribute(Type AccountType) : base(AccountType) { }

		protected override Type[] MaskFieldTypes { get; } =
		{
			typeof(PREmployee.ptoAssetSubID),
			typeof(PRPayGroup.ptoAssetSubID),
			typeof(PRPTOBank.ptoAssetSubID)
		};

		protected override IEnumerable<object> GetMaskSources(PXCache cache, object row)
		{
			var line = row as PRPTODetail;
			var employee = (PREmployee)Employee.Select(cache.Graph, line.EmployeeID);
			var payGroup = (PRPayGroup)PayGroup.Select(cache.Graph, (string)CacheHelper.GetCurrentValue(cache.Graph, PayGroupIDField));
			object ptoBank = PXSelectorAttribute.Select<PRPTODetail.bankID>(cache, line);

			object employeeSubID = GetValue<PREmployee.ptoAssetSubID>(cache.Graph, employee);
			object payGroupSubID = GetValue<PRPayGroup.ptoAssetSubID>(cache.Graph, payGroup);
			object ptoSubID = GetValue<PRPTOBank.ptoAssetSubID>(cache.Graph, ptoBank);

			// Be careful, this array order needs to match with PRPTOAcctSubDefault.SubListAttribute (used in PRPTOSubAccountMaskAttribute)
			return new object[] { employeeSubID, payGroupSubID, ptoSubID };
		}

		public override object MakeSub(PXGraph graph, PRSetup setup, IEnumerable<object> maskSources)
		{
			return PRPTOSubAccountMaskAttribute.MakeSub<PRSetup.ptoAssetSubMask>(graph, setup.PTOAssetSubMask, maskSources.ToArray(), MaskFieldTypes);
		}
		protected override Type SetupField => typeof(PRSetup.ptoAssetSubMask);
	}
}
