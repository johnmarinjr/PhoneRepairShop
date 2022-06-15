using PX.Data;
using System;

namespace PX.Objects.EP
{
	public static class CacheHelper
	{
		public static object GetValue<Field>(PXGraph graph, object data)
			where Field : IBqlField
		{
			return GetValue(graph, data, typeof(Field));
		}

		public static object GetValue(PXGraph graph, object data, Type field)
		{
			return graph.Caches[BqlCommand.GetItemType(field)].GetValue(data, field.Name);
		}

		public static object GetCurrentValue(PXGraph graph, Type type)
		{
			PXCache cache = graph.Caches[BqlCommand.GetItemType(type)];
			return cache?.GetValue(cache.Current, type.Name);
		}

		public static object GetCurrentRecord(PXGraph graph, Type fieldType)
		{
			PXCache cache = graph.Caches[fieldType];
			return cache?.Current;
		}
	}
}
