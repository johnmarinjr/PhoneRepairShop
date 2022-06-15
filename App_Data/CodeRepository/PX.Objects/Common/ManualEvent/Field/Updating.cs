using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class Updating
			{
				[DebuggerStepThrough]
				public class Args<TFieldType>
				{
					public PXCache Cache { get; }
					public PXFieldUpdatingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TFieldType NewValue
					{
						get => (TFieldType)EventArgs.NewValue;
						set => EventArgs.NewValue = value;
					}
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXFieldUpdatingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TFieldType newValue)
						: this(cache, new PXFieldUpdatingEventArgs(row, newValue)) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdating>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdating.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdating>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdating.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXFieldUpdating Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class Updating
			{
				[DebuggerStepThrough]
				public class Args<TFieldType> : FieldOf<TTable>.Updating.Args<TFieldType>
				{
					public Args(PXCache cache, PXFieldUpdatingEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row, TFieldType newValue) : base(cache, row, newValue) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdating>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdating.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldUpdating>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldUpdating.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXFieldUpdating Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}
	}
}
