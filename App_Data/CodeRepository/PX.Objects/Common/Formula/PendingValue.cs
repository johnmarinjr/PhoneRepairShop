using System;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.SQLTree;
using static PX.Data.BqlCommand;

namespace PX.Objects.Common
{
	public class PendingValue<Field> : BqlFormula, IBqlCreator, IBqlOperand
		where Field : IBqlField
	{

		protected bool IsExternalCall;

		/// <exclude />
		public virtual bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			return true;
		}

		/// <exclude/>
		public virtual void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			BqlFormula.ItemContainer container = item as BqlFormula.ItemContainer;
			if (container != null)
			{
				IsExternalCall = container.IsExternalCall;
			}

			if (container != null && container.PendingValue != null)
				value = container.PendingValue;
			else
			{
				item = ItemContainer.Unwrap(item);
				value = cache.GetValuePending<Field>(item);
			}
			if (value == null && cache.Graph.IsCopyPasteContext)
				value = new object();

		}
	}

	[PXInternalUseOnly]
	public class IsPending : IBqlComparison
	{
		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			result = (value != null && value != PXCache.NotSetValue);
		}

		public virtual bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, Selection selection)
		{
			return true;
		}
	}

	[PXInternalUseOnly]
	public class IsNotPending : IBqlComparison
	{
		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			result = (value == null || value == PXCache.NotSetValue);
		}

		public virtual bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, Selection selection)
		{
			return true;
		}
	}
}
