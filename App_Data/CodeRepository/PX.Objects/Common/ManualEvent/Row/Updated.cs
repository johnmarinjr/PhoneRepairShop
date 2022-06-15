using System;
using System.Collections.Generic;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class Row<TTable>
		{
			public static class Updated
			{
				[System.Diagnostics.DebuggerStepThrough]
				public class Args
				{
					public PXCache Cache { get; }
					public PXRowUpdatedEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TTable OldRow => (TTable)EventArgs.OldRow;
					public bool ExternalCall => EventArgs.ExternalCall;

					public Args(PXCache cache, PXRowUpdatedEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TTable oldRow, bool externalCall)
						: this(cache, new PXRowUpdatedEventArgs(row, oldRow, externalCall)) { }

					public IDictionary<string, (object OldValue, object NewValue)> GetDifference() => Cache.GetDifference(OldRow, Row);
				}

				public static void Subscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowUpdated>.Subscribe(
						graph,
						handler,
						(g, h) => g.RowUpdated.AddHandler<TTable>(h),
						h => Wrap(h));
				}
				public static void Unsubscribe(PXGraph graph, Action<Args> handler)
				{
					Synchronizer<Action<Args>, PXRowUpdated>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.RowUpdated.RemoveHandler<TTable>(h));
				}
				private static PXRowUpdated Wrap(Action<Args> handler) => (c, e) => handler(new Args(c, e));
			}
		}
	}
}
