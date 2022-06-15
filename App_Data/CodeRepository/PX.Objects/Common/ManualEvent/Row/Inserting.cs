using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Inserting
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowInsertingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public bool ExternalCall => EventArgs.ExternalCall;
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXRowInsertingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, bool externalCall)
						: this(cache, new PXRowInsertingEventArgs(row, externalCall)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowInserting>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowInserting.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowInserting>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowInserting.RemoveHandler<TTable>(h));
				}
				private static PXRowInserting Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
