using System;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Updating
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowUpdatingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TTable NewRow => (TTable)EventArgs.NewRow;
					public bool ExternalCall => EventArgs.ExternalCall;
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXRowUpdatingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TTable newRow, bool externalCall)
						: this(cache, new PXRowUpdatingEventArgs(row, newRow, externalCall)) { }
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowUpdating>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowUpdating.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowUpdating>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowUpdating.RemoveHandler<TTable>(h));
				}
				private static PXRowUpdating Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
