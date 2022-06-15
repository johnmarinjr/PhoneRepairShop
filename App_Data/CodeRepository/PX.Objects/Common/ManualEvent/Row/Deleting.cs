using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Deleting
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowDeletingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}
					public bool ExternalCall => EventArgs.ExternalCall;

					public Args(PXCache cache, PXRowDeletingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, bool externalCall)
						: this(cache, new PXRowDeletingEventArgs(row, externalCall)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowDeleting>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowDeleting.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowDeleting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowDeleting.RemoveHandler<TTable>(h));
				}
				private static PXRowDeleting Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
