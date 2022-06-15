using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Selected
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowSelectedEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;

					public Args(PXCache cache, PXRowSelectedEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row)
						: this(cache, new PXRowSelectedEventArgs(row)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowSelected>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowSelected.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowSelected>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowSelected.RemoveHandler<TTable>(h));
				}
				private static PXRowSelected Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
