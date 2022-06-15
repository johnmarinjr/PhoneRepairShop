using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR
{
	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
	public class PRSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		Type _AttributeType;
		protected PRDimensionMaskAttribute DimensionMaskAttribute => (PRDimensionMaskAttribute)_Attributes.SingleOrDefault(x => x.GetType() == typeof(PRDimensionMaskAttribute));

		public PRSubAccountMaskAttribute(Type attributeType, string maskName, string defaultValue)
		{
			_AttributeType = attributeType;
			var subListAttribute = (CustomListAttribute)Activator.CreateInstance(_AttributeType);
			PXDimensionMaskAttribute attr = new PRDimensionMaskAttribute(_DimensionName, maskName, defaultValue, subListAttribute);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

		public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
		{
			base.GetSubscriber<ISubscriber>(subscribers);
			subscribers.Remove(_Attributes.FirstOrDefault(x => x.GetType().IsAssignableFrom(_AttributeType)) as ISubscriber);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			var stringlist = (CustomListAttribute)_Attributes.First(x => x.GetType() == _AttributeType);
			DimensionMaskAttribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, Type attributeType, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			var subListAttribute = (CustomListAttribute)Activator.CreateInstance(attributeType);
			try
			{
				//In MakeSub, -1 is used to raise an error instead of defaulting value, otherwise should be an index from sources[]
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, subListAttribute.AllowedValues, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(subListAttribute.AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}

	}

	public class PRDimensionMaskAttribute : PXDimensionMaskAttribute
	{
		public PRDimensionMaskAttribute(string dimensionName, string maskName, string defaultValue, CustomListAttribute subListAttribute) :
			base(dimensionName, maskName, defaultValue, subListAttribute.AllowedValues, subListAttribute.AllowedLabels)
		{ }

		public IEnumerable<int> GetSegmentsLength()
		{
			for (int i = 0; i < _Definition.Dimensions[_Dimension].Length; i++)
			{
				yield return _Definition.Dimensions[_Dimension][i].Length;
			}
		}

		public string MakeMask(IList<string> segmentsValue)
		{
			var mask = new StringBuilder();
			for (int i = 0; i < segmentsValue.Count; i++)
			{
				for (var j = 0; j < _Definition.Dimensions[_Dimension][i].Length; j++)
				{
					mask.Append(segmentsValue[i]).ToString();
				}
			}

			return mask.ToString();
		}

		public IEnumerable<string> GetSegmentMaskValues(string mask)
		{
			if (string.IsNullOrEmpty(mask))
			{
				yield break;
			}

			for (int i = 0; i < _Definition.Dimensions[_Dimension].Length; i++)
			{
				string input = mask.Substring(0, _Definition.Dimensions[_Dimension][i].Length);

				string matchVal = null;
				foreach (string val in _allowedValues)
				{
					if (new string(char.Parse(val), input.Length).Equals(input))
					{
						matchVal = val;
						break;
					}
				}

				if (!string.IsNullOrEmpty(matchVal))
				{
					yield return matchVal;
					mask = mask.Substring(_Definition.Dimensions[_Dimension][i].Length);
					continue;
				}

				throw new PXException(PXMessages.LocalizeFormat(ErrorMessages.ElementOfFieldDoesntExist, input, _FieldName));
			}
		}
	}

	[PREarningsAcctSubDefault.SubList]
	public class PREarningsSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PREarningsSubAccountMaskAttribute(Type attributeType, string maskName, string defaultValue)
			: base(attributeType, maskName, defaultValue)
		{
		}

		public PREarningsSubAccountMaskAttribute()
			: this(typeof(PREarningsAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.EarningMaskName, PREarningsAcctSubDefault.MaskEarningType)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PREarningsAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRDeductAcctSubDefault.SubList]
	public sealed class PRDeductSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRDeductSubAccountMaskAttribute()
			: base(typeof(PRDeductAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.DeductionMaskName, PRDeductAcctSubDefault.MaskDeductionCode)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRDeductAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRBenefitExpenseAcctSubDefault.SubList]
	public class PRBenefitExpenseSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRBenefitExpenseSubAccountMaskAttribute(Type attributeType, string maskName, string defaultValue)
			: base(attributeType, maskName, defaultValue)
		{
		}

		public PRBenefitExpenseSubAccountMaskAttribute()
			: this(typeof(PRBenefitExpenseAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.BenefitExpenseMaskName, PRDeductAcctSubDefault.MaskDeductionCode)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRBenefitExpenseAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRTaxAcctSubDefault.SubList]
	public sealed class PRTaxSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRTaxSubAccountMaskAttribute()
			: base(typeof(PRTaxAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.TaxMaskName, PRTaxAcctSubDefault.MaskTaxCode)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRTaxAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRTaxExpenseAcctSubDefault.SubList]
	public class PRTaxExpenseSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRTaxExpenseSubAccountMaskAttribute(Type attributeType, string maskName, string defaultValue)
			: base(attributeType, maskName, defaultValue)
		{
		}

		public PRTaxExpenseSubAccountMaskAttribute()
			: this(typeof(PRTaxExpenseAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.TaxExpenseMaskName, PRTaxAcctSubDefault.MaskTaxCode)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRTaxExpenseAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRPTOAcctSubDefault.SubList]
	public sealed class PRPTOSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRPTOSubAccountMaskAttribute()
			: base(typeof(PRPTOAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.PTOMaskName, PRPTOAcctSubDefault.MaskPTOBank)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRPTOAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PRPTOExpenseAcctSubDefault.SubList]
	public class PRPTOExpenseSubAccountMaskAttribute : PRSubAccountMaskAttribute
	{
		public PRPTOExpenseSubAccountMaskAttribute(Type attributeType, string maskName, string defaultValue)
			: base(attributeType, maskName, defaultValue)
		{
		}

		public PRPTOExpenseSubAccountMaskAttribute()
			: base(typeof(PRPTOExpenseAcctSubDefault.SubListAttribute), PRSubAccountMaskConstants.PTOExpenseMaskName, PRPTOAcctSubDefault.MaskPTOBank)
		{
		}

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			return MakeSub<Field>(graph, mask, typeof(PRPTOExpenseAcctSubDefault.SubListAttribute), sources, fields);
		}
	}

	[PREarningsAcctSubDefault.AlternateSubList]
	public class PREarningsAlternateSubAccountMaskAttribute : PREarningsSubAccountMaskAttribute, IPXFieldUpdatingSubscriber
	{
		protected Type _AlternateToField;
		protected PRAlternateSubAccountVeryfier AlternateSubVeryfier;

		public PREarningsAlternateSubAccountMaskAttribute(Type alternateToField)
			: base(typeof(PREarningsAcctSubDefault.AlternateSubListAttribute), PRSubAccountMaskConstants.AlternateEarningMaskName, PREarningsAcctSubDefault.MaskEarningType)
		{
			_AlternateToField = alternateToField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			AlternateSubVeryfier = new PRAlternateSubAccountVeryfier(sender.Graph, _AlternateToField, DimensionMaskAttribute, _FieldName);
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			AlternateSubVeryfier.FieldUpdating(sender, e);
		}
	}

	[PRBenefitExpenseAcctSubDefault.AlternateSubList]
	public class PRBenefitExpenseAlternateSubAccountMaskAttribute : PRBenefitExpenseSubAccountMaskAttribute, IPXFieldUpdatingSubscriber
	{
		protected Type _AlternateToField;
		protected PRAlternateSubAccountVeryfier AlternateSubVeryfier;

		public PRBenefitExpenseAlternateSubAccountMaskAttribute(Type alternateToField)
			: base(typeof(PRBenefitExpenseAcctSubDefault.AlternateSubListAttribute), PRSubAccountMaskConstants.AlternateBenefitExpenseMaskName, PRTaxExpenseAcctSubDefault.MaskEarningType)
		{
			_AlternateToField = alternateToField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			AlternateSubVeryfier = new PRAlternateSubAccountVeryfier(sender.Graph, _AlternateToField, DimensionMaskAttribute, _FieldName);
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			AlternateSubVeryfier.FieldUpdating(sender, e);
		}
	}

	[PRTaxExpenseAcctSubDefault.AlternateSubList]
	public class PRTaxExpenseSubAlternateAccountMaskAttribute : PRTaxExpenseSubAccountMaskAttribute, IPXFieldUpdatingSubscriber
	{
		protected Type _AlternateToField;
		protected PRAlternateSubAccountVeryfier AlternateSubVeryfier;

		public PRTaxExpenseSubAlternateAccountMaskAttribute(Type alternateToField)
			: base(typeof(PRTaxExpenseAcctSubDefault.AlternateSubListAttribute), PRSubAccountMaskConstants.AlternateTaxExpenseMaskName, PRTaxExpenseAcctSubDefault.MaskEarningType)
		{
			_AlternateToField = alternateToField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			AlternateSubVeryfier = new PRAlternateSubAccountVeryfier(sender.Graph, _AlternateToField, DimensionMaskAttribute, _FieldName);
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			AlternateSubVeryfier.FieldUpdating(sender, e);
		}
	}

	[PRPTOExpenseAcctSubDefault.AlternateSubList]
	public class PRPTOExpenseSubAlternateAccountMaskAttribute : PRPTOExpenseSubAccountMaskAttribute, IPXFieldUpdatingSubscriber
	{
		protected Type _AlternateToField;
		protected PRAlternateSubAccountVeryfier AlternateSubVeryfier;

		public PRPTOExpenseSubAlternateAccountMaskAttribute(Type alternateToField)
			: base(typeof(PRPTOExpenseAcctSubDefault.AlternateSubListAttribute), PRSubAccountMaskConstants.AlternatePTOExpenseMaskName, PRPTOExpenseAcctSubDefault.MaskEarningType)
		{
			_AlternateToField = alternateToField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			AlternateSubVeryfier = new PRAlternateSubAccountVeryfier(sender.Graph, _AlternateToField, DimensionMaskAttribute, _FieldName);
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			AlternateSubVeryfier.FieldUpdating(sender, e);
		}
	}

	public class PRAlternateSubAccountVeryfier
	{
		protected Type _AlternateToField;
		protected PRDimensionMaskAttribute _DimensionMaskAttribute;
		protected string _FieldName;

		public PRAlternateSubAccountVeryfier(PXGraph graph, Type alternateToField, PRDimensionMaskAttribute dimensionMaskAttribute, string fieldName)
		{
			_AlternateToField = alternateToField;
			_DimensionMaskAttribute = dimensionMaskAttribute;
			_FieldName = fieldName;

			graph.FieldUpdated.AddHandler(_AlternateToField.DeclaringType, _AlternateToField.Name, UpdateAlternate);
		}
		
		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			var newValue = (string)e.NewValue;
			if (string.IsNullOrEmpty(newValue))
			{
				return;
			}

			var defaultMask = (string)sender.GetValue(e.Row, _AlternateToField.Name);
			var defaultSegments = new List<string>();
			foreach (int segmentLength in _DimensionMaskAttribute.GetSegmentsLength())
			{
				defaultSegments.Add(defaultMask.Substring(0, 1));
				defaultMask = defaultMask.Remove(0, segmentLength);
			}

			var alternateSegments = _DimensionMaskAttribute.GetSegmentMaskValues(newValue).ToList();

			var resultSegment = new string[defaultSegments.Count];
			for (int i = 0; i < defaultSegments.Count; i++)
			{
				if (UsesAlternateSubSource(defaultSegments[i]))
				{
					resultSegment[i] = alternateSegments[i];
					continue;
				}

				resultSegment[i] = defaultSegments[i];
			}

			string newMask = _DimensionMaskAttribute.MakeMask(resultSegment);
			if (newValue != newMask)
			{
				e.NewValue = newMask;
				PXUIFieldAttribute.SetWarning(sender, e.Row, _FieldName, Messages.OnlySomeSegmentsCanBeAlternate);
			}
		}

		protected virtual bool UsesAlternateSubSource(string mask)
		{
			return mask.Contains(GLAccountSubSource.Project) || mask.Contains(GLAccountSubSource.Task) || mask.Contains(GLAccountSubSource.LaborItem);
		}

		protected virtual void UpdateAlternate(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var alternateMask = (string)sender.GetValue(e.Row, _FieldName);
			var newDefaultMask = (string)sender.GetValue(e.Row, _AlternateToField.Name);
			if (alternateMask == null || !UsesAlternateSubSource(newDefaultMask))
			{
				sender.SetValue(e.Row, _FieldName, null);
				return;
			}

			StringBuilder newAlternateMask = new StringBuilder();
			for (int i = 0; i < newDefaultMask.Length; i++)
			{
				if (UsesAlternateSubSource(newDefaultMask[i].ToString()))
				{
					newAlternateMask.Append(alternateMask[i]);
				}
				else
				{
					newAlternateMask = newAlternateMask.Append(newDefaultMask[i]);
				}
			}

			sender.SetValue(e.Row, _FieldName, newAlternateMask.ToString());
		}
	}
}
