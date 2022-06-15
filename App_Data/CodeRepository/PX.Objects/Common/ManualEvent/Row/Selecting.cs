using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Selecting
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowSelectingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public PXDataRecord Record => EventArgs.Record;
					public bool IsReadOnly => EventArgs.IsReadOnly;
					public int Position
					{
						get => EventArgs.Position;
						set => EventArgs.Position = value;
					}
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXRowSelectingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, PXDataRecord record, int position, bool isReadOnly)
						: this(cache, new PXRowSelectingEventArgs(row, record, position, isReadOnly)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowSelecting>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowSelecting.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowSelecting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowSelecting.RemoveHandler<TTable>(h));
				}
				private static PXRowSelecting Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
