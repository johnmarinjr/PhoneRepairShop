using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class Verifying
			{
				[DebuggerStepThrough]
				public class Args<TFieldType>
				{
					public PXCache Cache { get; }
					public PXFieldVerifyingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TFieldType NewValue
					{
						get => (TFieldType)EventArgs.NewValue;
						set => EventArgs.NewValue = value;
					}
					public bool ExternalCall => EventArgs.ExternalCall;
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXFieldVerifyingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TFieldType newValue, bool externalCall)
						: this(cache, new PXFieldVerifyingEventArgs(row, newValue, externalCall)) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldVerifying>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldVerifying.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldVerifying>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldVerifying.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXFieldVerifying Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class Verifying
			{
				[DebuggerStepThrough]
				public class Args<TFieldType> : FieldOf<TTable>.Verifying.Args<TFieldType>
				{
					public Args(PXCache cache, PXFieldVerifyingEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row, TFieldType newValue, bool externalCall) : base(cache, row, newValue, externalCall) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldVerifying>.Subscribe(
						graph,
						handler,
						(g, h) => g.FieldVerifying.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXFieldVerifying>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.FieldVerifying.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXFieldVerifying Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}
	}
}
