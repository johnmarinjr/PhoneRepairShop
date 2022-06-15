using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Inserted
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowInsertedEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public bool ExternalCall => EventArgs.ExternalCall;

					public Args(PXCache cache, PXRowInsertedEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, bool externalCall)
						: this(cache, new PXRowInsertedEventArgs(row, externalCall)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowInserted>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowInserted.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowInserted>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowInserted.RemoveHandler<TTable>(h));
				}
				private static PXRowInserted Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
