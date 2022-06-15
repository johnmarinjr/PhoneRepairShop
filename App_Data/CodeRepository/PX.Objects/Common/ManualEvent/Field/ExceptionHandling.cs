using System;
using System.Diagnostics;
using PX.Data;

namespace PX.Objects.Common
{
	public static partial class ManualEvent
	{
		public static partial class FieldOf<TTable>
		{
			public static class ExceptionHandling
			{
				[DebuggerStepThrough]
				public class Args<TFieldType>
				{
					public PXCache Cache { get; }
					public PXExceptionHandlingEventArgs EventArgs { get; }

					public TTable Row => (TTable)EventArgs.Row;
					public TFieldType NewValue
					{
						get => (TFieldType)EventArgs.NewValue;
						set => EventArgs.NewValue = value;
					}
					public Exception Exception => EventArgs.Exception;
					public bool Cancel
					{
						get => EventArgs.Cancel;
						set => EventArgs.Cancel = value;
					}

					public Args(PXCache cache, PXExceptionHandlingEventArgs args) => (Cache, EventArgs) = (cache, args);
					public Args(PXCache cache, TTable row, TFieldType newValue, Exception exception)
						: this(cache, new PXExceptionHandlingEventArgs(row, newValue, exception)) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXExceptionHandling>.Subscribe(
						graph,
						handler,
						(g, h) => g.ExceptionHandling.AddHandler(typeof(TTable), fieldName, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, string fieldName, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXExceptionHandling>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.ExceptionHandling.RemoveHandler(typeof(TTable), fieldName, h));
				}
				private static PXExceptionHandling Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}

		public static partial class FieldOf<TTable, TField>
		{
			public static class ExceptionHandling
			{
				[DebuggerStepThrough]
				public class Args<TFieldType> : FieldOf<TTable>.ExceptionHandling.Args<TFieldType>
				{
					public Args(PXCache cache, PXExceptionHandlingEventArgs args) : base(cache, args) { }
					public Args(PXCache cache, TTable row, TFieldType newValue, Exception exception) : base(cache, row, newValue, exception) { }
				}
				public static void Subscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXExceptionHandling>.Subscribe(
						graph,
						handler,
						(g, h) => g.ExceptionHandling.AddHandler(typeof(TTable), typeof(TField).Name, h),
						h => Wrap(h));
				}
				public static void Unsubscribe<TFieldType>(PXGraph graph, Action<Args<TFieldType>> handler)
				{
					Synchronizer<Action<Args<TFieldType>>, PXExceptionHandling>.Unsubscribe(
						graph,
						handler,
						(g, h) => g.ExceptionHandling.RemoveHandler(typeof(TTable), typeof(TField).Name, h));
				}
				private static PXExceptionHandling Wrap<TFieldType>(Action<Args<TFieldType>> handler) => (c, e) => handler(new Args<TFieldType>(c, e));
			}
		}
	}
}
