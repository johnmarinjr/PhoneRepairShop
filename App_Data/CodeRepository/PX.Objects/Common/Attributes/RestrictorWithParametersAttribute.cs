using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Attributes
{
	public class RestrictorWithParametersAttribute : PXRestrictorAttribute
	{
		protected Type[] _messageParameters;

		public RestrictorWithParametersAttribute(Type where,
			string message, params Type[] messageParameters)
			: base(where, message)
		{
			_messageParameters = messageParameters;
		}

		public override object[] GetMessageParameters(PXCache sender, object itemres, object row)
			=> _messageParameters.Select(
				parameter => GetMessageParameter(sender, itemres, parameter)).ToArray();

		protected virtual object GetMessageParameter(PXCache sender, object itemres, Type parameter)
		{
			if (parameter.IsGenericType)
			{
				if (parameter.GetGenericTypeDefinition().IsIn(typeof(Current<>), typeof(Current2<>)))
					return GetCurrentValue(sender.Graph, parameter);

				if (parameter.GetGenericTypeDefinition() == typeof(Selector<,>))
					return GetSelectorValue(sender.Graph, parameter);
			}

			return GetSelectedRowValue(sender, itemres, parameter);
		}

		protected virtual object GetCurrentValue(PXGraph graph, Type current)
		{
			var parameters = current.GetGenericArguments();
			if (parameters?.Length != 1)
				throw new ArgumentException();

			var field = parameters[0];

			Type currentType = BqlCommand.GetItemType(field);
			var currentCache = graph.Caches[currentType];

			if (currentCache.Current == null)
				return null;

			var value = currentCache.GetValueExt(currentCache.Current, field.Name);
			if (value is PXFieldState state)
				return state.Value;

			return value;
		}

		protected virtual object GetSelectorValue(PXGraph graph, Type selector)
		{
			var parameters = selector.GetGenericArguments();
			if (parameters?.Length != 2)
				throw new ArgumentException();

			var key = parameters[0];
			var field = parameters[1];

			Type keyType = BqlCommand.GetItemType(key);
			var keyCache = graph.Caches[keyType];

			if (keyCache.Current == null)
				return null;

			var row = PXSelectorAttribute.Select(keyCache, keyCache.Current, key.Name);
			var rowType = BqlCommand.GetItemType(field);
			var rowCache = graph.Caches[rowType];

			var value = rowCache.GetValueExt(row, field.Name);
			if (value is PXFieldState state)
				return state.Value;

			return value;
		}

		protected virtual object GetSelectedRowValue(PXCache sender, object itemres, Type parameter)
		{
			Type itemType = BqlCommand.GetItemType(parameter);
			var item = PXResult.Unwrap(itemres, itemType);

			return sender.Graph.Caches[itemType].GetStateExt(item, parameter.Name);
		}
	}
}
