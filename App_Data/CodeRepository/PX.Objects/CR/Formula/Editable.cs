using System.Collections.Generic;
using PX.Data;
using PX.Data.SQLTree;

namespace PX.Objects.CR
{
	public class Editable<Field> : IBqlCreator, IBqlOperand
		where Field : IBqlField
	{
		#region IBqlCreator Members

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			return false;
		}

		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			PXCache c = cache.Graph.Caches[BqlCommand.GetItemType(typeof(Field))];
			object row = null;
			if (c.GetItemType().IsAssignableFrom(cache.GetItemType()))
			{
				row = BqlFormula.ItemContainer.Unwrap(item);
			}

			var state = c.GetStateExt<Field>(row) as PXFieldState;

			value = state == null || (state.Enabled && state.Visible && !state.IsReadOnly);
		}

		#endregion
	}
}
