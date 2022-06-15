using PX.Data;
using System;
using System.Linq;

namespace PX.Objects.PR
{
	public class HideValueIfDisabledAttribute : PXUIEnabledAttribute
	{
		private bool? _Enable = null;
		private Type _ShowCondition = null;

		public HideValueIfDisabledAttribute(Type enableAndShowCondition) : base(enableAndShowCondition)
		{
			_ShowCondition = enableAndShowCondition;
		}

		public HideValueIfDisabledAttribute(bool enable, Type showCondition) : base(null)
		{
			_Enable = enable;
			_ShowCondition = showCondition;
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			PXFieldState fieldState = e.ReturnState as PXFieldState;
			if (e.ReturnState == null || e.Row == null)
			{
				return;
			}

			if (_Enable != null)
			{
				fieldState.Enabled = _Enable.Value;
			}

			if (_ShowCondition != null && !ConditionEvaluator.GetResult(sender, e.Row, _ShowCondition))
			{
				e.ReturnValue = null;
			}
		}
	}
}
