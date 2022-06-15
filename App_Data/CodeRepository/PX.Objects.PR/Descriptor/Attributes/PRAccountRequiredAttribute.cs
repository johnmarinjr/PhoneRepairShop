using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;
using System.Collections.Generic;

namespace PX.Objects.PR
{
	public abstract class PRAccountRequiredAttribute : PXEventSubscriberAttribute, IPXRowSelectedSubscriber, IPXRowPersistingSubscriber
	{
		protected List<string> _SetupFieldNameList = new List<string>();
		protected string _ValueWhenRequired;
		protected Type _RequiredCondition;

		public PRAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
		{
			_ValueWhenRequired = valueWhenRequired;
			_RequiredCondition = requiredCondition;
		}

		public virtual void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetRequired(sender, this.FieldName, IsRequired(sender, e.Row));
		}

		public virtual void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object newValue = sender.GetValue(e.Row, this.FieldName);
			object oldValue = sender.GetValueOriginal(e.Row, this.FieldName);
			if (e.Operation != PXDBOperation.Delete && newValue == null && oldValue != null && IsRequired(sender, e.Row))
			{
				PXUIFieldAttribute.SetError(sender, e.Row, this.FieldName, PXMessages.LocalizeFormatNoPrefix(Messages.CantBeEmpty, PXUIFieldAttribute.GetDisplayName(sender, this.FieldName)));
			}
		}

		public virtual bool IsRequired(PXCache sender, object row)
		{
			PXCache prSetupCache = sender.Graph.Caches[typeof(PRSetup)];
			PRSetup setupRecord = SelectFrom<PRSetup>.View.Select(sender.Graph).TopFirst;
			if (setupRecord == null)
			{
				return false;
			}

			if (row != null && _RequiredCondition != null && !ConditionEvaluator.GetResult(sender, row, _RequiredCondition))
			{
				return false;
			}

			foreach (var fieldName in _SetupFieldNameList)
			{
				var setupValue = prSetupCache.GetValue(setupRecord, fieldName) as string;
				if(setupValue == _ValueWhenRequired)
				{
					return true;
				}
			}

			return false;
		}
	}

	public class PREarningAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PREarningAccountRequiredAttribute(string valueWhenRequired)
			: base(valueWhenRequired)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.earningsAcctDefault).Name);
			_SetupFieldNameList.Add(typeof(PRSetup.earningsAlternateAcctDefault).Name);
		}
	}

	public class PRDedLiabilityAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRDedLiabilityAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.deductLiabilityAcctDefault).Name);
		}
	}

	public class PRBenExpenseAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRBenExpenseAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.benefitExpenseAcctDefault).Name);
			_SetupFieldNameList.Add(typeof(PRSetup.benefitExpenseAlternateAcctDefault).Name);
		}
	}

	public class PRBenLiabilityAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRBenLiabilityAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.benefitLiabilityAcctDefault).Name);
		}
	}

	public class PRTaxExpenseAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRTaxExpenseAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.taxExpenseAcctDefault).Name);
			_SetupFieldNameList.Add(typeof(PRSetup.taxExpenseAlternateAcctDefault).Name);
		}
	}

	public class PRTaxLiabilityAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRTaxLiabilityAccountRequiredAttribute(string valueWhenRequired)
			: base(valueWhenRequired)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.taxLiabilityAcctDefault).Name);
		}
	}

	public class PRPTOExpenseAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRPTOExpenseAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.ptoExpenseAcctDefault).Name);
			_SetupFieldNameList.Add(typeof(PRSetup.ptoExpenseAlternateAcctDefault).Name);
		}
	}

	public class PRPTOLiabilityAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRPTOLiabilityAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.ptoLiabilityAcctDefault).Name);
		}
	}

	public class PRPTOAssetAccountRequiredAttribute : PRAccountRequiredAttribute
	{
		public PRPTOAssetAccountRequiredAttribute(string valueWhenRequired, Type requiredCondition = null)
			: base(valueWhenRequired, requiredCondition)
		{
			_SetupFieldNameList.Add(typeof(PRSetup.ptoAssetAcctDefault).Name);
		}
	}
}
