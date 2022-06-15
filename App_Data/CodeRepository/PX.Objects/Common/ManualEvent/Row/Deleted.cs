using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Deleted
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowDeletedEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;

					public Args(PXCache cache, PXRowDeletedEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, bool externalCall)
						: this(cache, new PXRowDeletedEventArgs(row, externalCall)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowDeleted>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowDeleted.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowDeleted>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowDeleted.RemoveHandler<TTable>(h));
				}
				private static PXRowDeleted Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
