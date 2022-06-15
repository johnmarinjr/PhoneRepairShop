using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class Selecting
			{
				[DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXFieldSelectingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public bool ExternalCall => EventArgs.ExternalCall;
					public bool IsAltered
					{
						get => EventArgs.IsAltered;
						set => EventArgs.IsAltered = value;
					}
					public object ReturnValue
					{
						get => EventArgs.ReturnValue;
						set => EventArgs.ReturnValue = value;
					}
					public object ReturnState
					{
						get => EventArgs.ReturnState;
						set => EventArgs.ReturnState = value;
					}
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXFieldSelectingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, object returnValue, bool isAltered, bool externalCall)
						: this(cache, new PXFieldSelectingEventArgs(row, returnValue, isAltered, externalCall)) { }
				}
				public static void Subscribe(PXGraph graph, string fieldName, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXFieldSelecting>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldSelecting.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, string fieldName, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXFieldSelecting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldSelecting.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXFieldSelecting Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class Selecting
			{
				[DebuggerStepThrough]
				public class Args : FieldOf<TTable>.Selecting.Args
				{
					public Args(PXCache cache, PXFieldSelectingEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row, object returnValue, bool isAltered, bool externalCall) : base(cache, row, returnValue, isAltered, externalCall) { }
				}
				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXFieldSelecting>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldSelecting.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXFieldSelecting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldSelecting.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXFieldSelecting Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}

		}
	}
}
