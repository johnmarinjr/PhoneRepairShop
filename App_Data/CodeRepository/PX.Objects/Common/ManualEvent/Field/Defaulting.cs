using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class Defaulting
			{
				[DebuggerStepThrough]
				public class Args<TFieldType>
				{
					public PXCache Cache { get; }
					public PXFieldDefaultingEventArgs EventArgs { get; }

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

					public Args(PXCache cache, PXFieldDefaultingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row)
						: this(cache, new PXFieldDefaultingEventArgs(row)) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldDefaulting>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldDefaulting.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldDefaulting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldDefaulting.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXFieldDefaulting Wrap<TFieldType>(Action<Args<TFieldType>> handler)
					=> (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class Defaulting
			{
				[DebuggerStepThrough]
				public class Args<TFieldType> : FieldOf<TTable>.Defaulting.Args<TFieldType>
				{
					public Args(PXCache cache, PXFieldDefaultingEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row) : base(cache, row) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldDefaulting>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldDefaulting.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldDefaulting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldDefaulting.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXFieldDefaulting Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}
	}
}
