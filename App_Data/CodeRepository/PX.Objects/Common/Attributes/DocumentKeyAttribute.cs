using PX.Api;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Attributes
{
	public class DocumentKeyAttribute : PXEventSubscriberAttribute, IPXFieldSelectingSubscriber, IPXFieldUpdatingSubscriber
	{
		protected Type StringListAttributeType;
		protected Dictionary<string, string> ValuesLabels;
		protected Dictionary<string, string> LabelsValues;

		public DocumentKeyAttribute(Type listAttributeType)
		{
			if (!typeof(PXStringListAttribute).IsAssignableFrom(listAttributeType))
			{
				throw new PXArgumentException(nameof(listAttributeType));
			}

			StringListAttributeType = listAttributeType;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			ValuesLabels = new Dictionary<string, string>();
			LabelsValues = new Dictionary<string, string>();
			foreach(var pair in ((PXStringListAttribute)Activator.CreateInstance(StringListAttributeType)).ValueLabelDic
										.ToDictionary(pair => pair.Key, pair => PXMessages.LocalizeNoPrefix(pair.Value)?.Trim())) 
			{
				ValuesLabels.Add(pair.Key, pair.Value);
				string key2=null;
				if (!LabelsValues.TryGetValue(pair.Value, out key2))
				{
					LabelsValues.Add(pair.Value, pair.Key);
				}
				else 
				{
					throw new PXException(Common.Messages.LabelIsDuplicatedForValues, pair.Value, pair.Key, key2);
				}
			}
		}

		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.ReturnValue == null) return;
			string innerValue = ((string)e.ReturnValue).Trim();

			string[] splitted = innerValue.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (splitted == null || splitted.Length < 2)
			{
				throw new ArgumentOutOfRangeException();
			}

			string refNbr = innerValue.Substring(splitted[0].Length).Trim();
			e.ReturnValue = $"{ValuesLabels[splitted[0]]} {refNbr}";
		}

		public virtual void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.NewValue == null) return;
			string outerValue = ((string)e.NewValue).Trim();

			string displayDocType = LabelsValues.Keys
				.Where(docType => outerValue.StartsWith(docType))
				.FirstOrDefault();

			if (displayDocType == null)
			{
				throw new ArgumentOutOfRangeException();
			}

			string refNumber = outerValue.Substring(displayDocType.Length).Trim();
			e.NewValue = $"{LabelsValues[displayDocType]} {refNumber}";
		}
	}

	public class DocumentSelectorAttribute : PXSelectorAttribute
	{
		public DocumentSelectorAttribute(Type type) : base(type)
		{
		}

		public DocumentSelectorAttribute(Type type, params Type[] fieldList) : base(type, fieldList)
		{
		}
 
		public override void SubstituteKeyFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			object newValue = e.NewValue;
			sender.Graph.Caches[BqlCommand.GetItemType(_SubstituteKey)].RaiseFieldUpdating(_SubstituteKey.Name, null, ref newValue);
			e.NewValue = newValue;
			base.SubstituteKeyFieldUpdating(sender, e);
		}

		public override void SubstituteKeyFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.SubstituteKeyFieldSelecting(sender, e);
			object returnValue = e.ReturnValue;
			sender.Graph.Caches[BqlCommand.GetItemType(_SubstituteKey)].RaiseFieldSelecting(_SubstituteKey.Name, null, ref returnValue, false);
			e.ReturnValue = returnValue;

		}
	}
}
