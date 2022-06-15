using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class Updated
			{
				[DebuggerStepThrough]
				public class Args<TFieldType>
				{
					public PXCache Cache { get; }
					public PXFieldUpdatedEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TFieldType OldValue => (TFieldType)EventArgs.OldValue;

					public Args(PXCache cache, PXFieldUpdatedEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TFieldType oldValue, bool externalCall)
						: this(cache, new PXFieldUpdatedEventArgs(row, oldValue, externalCall)) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdated>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdated.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdated>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdated.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXFieldUpdated Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class Updated
			{
				[DebuggerStepThrough]
				public class Args<TFieldType> : FieldOf<TTable>.Updated.Args<TFieldType>
				{
					public Args(PXCache cache, PXFieldUpdatedEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row, TFieldType oldValue, bool externalCall) : base(cache, row, oldValue, externalCall) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdated>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdated.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdated>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdated.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXFieldUpdated Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}
	}
}
