using PX.Data;
using System;

namespace PX.Objects.PR
{
	public class PXUIRequiredIfVisibleAttribute : PXUIRequiredAttribute
	{
		public PXUIRequiredIfVisibleAttribute(Type conditionType) : base(conditionType)
		{
		}

		public PXUIRequiredIfVisibleAttribute() : this(typeof(True.IsEqual<True>))
		{
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			var state = (PXFieldState)e.ReturnState;
			if(state.Visible == false)
			{
				return;
			}

			base.FieldSelecting(sender, e);
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			var state = (PXFieldState) sender.GetStateExt(e.Row, FieldName);
			if (state.Visible == false)
			{
				return;
			}

			base.RowPersisting(sender, e);
		}
	}
}