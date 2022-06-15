using PX.Data;
using System;

namespace PX.Objects.EP
{
	public class ShowValueWhenAttribute : PXBaseConditionAttribute, IPXFieldSelectingSubscriber, IPXRowPersistingSubscriber
	{
		private bool _ClearOnPersisting = false;

		public ShowValueWhenAttribute(Type conditionType, bool clearOnPersisting = false)
			: base(conditionType)
		{
			_ClearOnPersisting = clearOnPersisting;
		}

		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row == null || _Condition == null)
			{
				return;
			}

			if (!GetConditionResult(sender, e.Row, Condition))
			{
				e.ReturnValue = null;
			}
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (_ClearOnPersisting && !GetConditionResult(sender, e.Row, Condition))
			{
				sender.SetValue(e.Row, _FieldName, null);
			}
		}
	}
}
