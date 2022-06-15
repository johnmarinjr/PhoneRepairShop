using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Extensions;
using PX.Objects.SO.Attributes;
using PX.Objects.SO.DAC.Projections;
using PX.Objects.TX;

namespace PX.Objects.SO.GraphExtensions
{
	public abstract class AffectedBlanketOrderByChildOrders<TSelf, TGraph> : ProcessAffectedEntitiesInPrimaryGraphBase<TSelf, TGraph, BlanketSOOrder, SOOrderEntry>
		where TSelf : AffectedBlanketOrderByChildOrders<TSelf, TGraph>
		where TGraph : PXGraph
	{
		#region SuppressionScope

		/// <summary>
		/// Indicates that the logic of SOOrder.MinSchedOrderDate recalculation is suppressed.
		/// </summary>
		public bool SuppressedMode { get; private set; }

		private class SuppressionScope : IDisposable
		{
			private readonly AffectedBlanketOrderByChildOrders<TSelf, TGraph> _ext;
			private readonly SOOrder _order;

			public SuppressionScope(AffectedBlanketOrderByChildOrders<TSelf, TGraph> ext, SOOrder order)
			{
				_ext = ext;
				_ext.SuppressedMode = true;
				_order = order;
			}

			void IDisposable.Dispose()
			{
				_ext.SuppressedMode = false;
				var blanketOrder = _ext.GetBlanketOrder(_order.OrderType, _order.OrderNbr);
				_ext.RecalculateMinSchedOrderDate(blanketOrder);
			}
		}

		/// <summary>
		/// Create a scope for suppressing recalculation of SOOrder.MinSchedOrderDate
		/// </summary>
		public IDisposable SuppressedModeScope(SOOrder order) => new SuppressionScope(this, order);

		#endregion

		private class AffectedOrderInfo
		{
			internal bool SwitchStatus { get; set; }
			internal Dictionary<int, SOLine> OriginalLines { get; set; }
		}

		private Dictionary<BlanketSOOrder, AffectedOrderInfo> _affectedBlanketOrders;

		public override void Initialize()
		{
			base.Initialize();

			Base.EnsureCachePersistence<BlanketSOOrder>();
			Base.EnsureCachePersistence<BlanketSOLine>();
			Base.EnsureCachePersistence<BlanketSOLineSplit>();

			// we need to instantiate the cache for correct initialization of PlanIDAttribute and status tables
			Base.Caches<BlanketSOLineSplit>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[BlanketSOLineSplitPlanID(typeof(SOOrder.noteID), typeof(SOOrder.hold))]
		protected virtual void _(Events.CacheAttached<BlanketSOLineSplit.planID> e)
		{
		}

		protected override bool ClearAffectedCaches => false;
		protected override bool PersistInSameTransaction => true;

		protected override bool EntityIsAffected(BlanketSOOrder entity) => false;

		protected override IEnumerable<BlanketSOOrder> GetLatelyAffectedEntities()
		{
			var cache = Base.Caches<BlanketSOOrder>();
			_affectedBlanketOrders = new Dictionary<BlanketSOOrder, AffectedOrderInfo>(cache.GetComparer());
			foreach (BlanketSOOrder blanket in cache.Updated)
			{
				bool calcCompleted = blanket.OpenLineCntr == 0,
					origCalcCompleted = blanket.OrigOpenLineCntr == 0;
				if (calcCompleted != origCalcCompleted)
				{
					_affectedBlanketOrders.Add(blanket, new AffectedOrderInfo { SwitchStatus = true });
				}
			}

			var lineCache = Base.Caches<BlanketSOLine>();
			foreach (var blanketLinesGroup in lineCache.Updated.RowCast<BlanketSOLine>()
				.Where(l => l.CuryOpenAmt != l.OrigCuryOpenAmt)
				.GroupBy(line => new { line.OrderType, line.OrderNbr }))
			{
				var blanketOrder = PXParentAttribute.SelectParent<BlanketSOOrder>(lineCache, blanketLinesGroup.First());
				if (!_affectedBlanketOrders.TryGetValue(blanketOrder, out AffectedOrderInfo info))
				{
					_affectedBlanketOrders.Add(blanketOrder, info = new AffectedOrderInfo());
				}

				var recalcOpenTaxesLineNbrs = blanketLinesGroup.Select(line => line.LineNbr.Value).ToHashSet();
				info.OriginalLines = SelectFrom<SOLine>
					.Where<SOLine.orderType.IsEqual<@P.AsString.ASCII>
						.And<SOLine.orderNbr.IsEqual<@P.AsString>>>
					.View.ReadOnly.Select(Base, blanketOrder.OrderType, blanketOrder.OrderNbr)
					.AsEnumerable().RowCast<SOLine>()
					.Where(l => recalcOpenTaxesLineNbrs.Contains(l.LineNbr.Value))
					.ToDictionary(l => l.LineNbr.Value);
			}

			return _affectedBlanketOrders.Any() ? _affectedBlanketOrders.Keys : null;
		}

		protected override void ProcessAffectedEntity(SOOrderEntry primaryGraph, BlanketSOOrder entity)
		{
			primaryGraph.Document.Current = primaryGraph.Document.Search<SOOrder.orderNbr>(entity.OrderNbr, entity.OrderType);

			AffectedOrderInfo info = null;
			_affectedBlanketOrders?.TryGetValue(entity, out info);

			if (info?.OriginalLines?.Any() == true)
			{
				foreach (SOLine line in primaryGraph.Transactions.Select().AsEnumerable().RowCast<SOLine>()
					.Where(line => info.OriginalLines.ContainsKey(line.LineNbr.Value)))
				{
					TaxAttribute.Calculate<SOLine.taxCategoryID, SOOpenTaxAttribute>(primaryGraph.Transactions.Cache,
						new PXRowUpdatedEventArgs(line, info.OriginalLines[line.LineNbr.Value], true));
				}
			}
			if (info?.SwitchStatus == true)
			{
				if (entity.OpenLineCntr == 0)
				{
					SOOrder.Events
						.Select(e => e.BlanketCompleted)
						.FireOn(primaryGraph, primaryGraph.Document.Current);
				}
				else
				{
					SOOrder.Events
						.Select(e => e.BlanketReopened)
						.FireOn(primaryGraph, primaryGraph.Document.Current);
				}
			}
		}

		protected override void OnProcessed(SOOrderEntry foreignGraph)
		{
			base.OnProcessed(foreignGraph);
			_affectedBlanketOrders = null;
		}

		private DateTime? CalcSchedOrderDate(BlanketSOLineSplit s)
			=> (s.Completed == false && s.Qty > s.QtyOnOrders + s.ReceivedQty) ? s.SchedOrderDate : null;

		protected virtual void _(Events.RowInserted<BlanketSOLineSplit> e)
		{
			OnSchedOrderDateUpdated(e.Row, null, CalcSchedOrderDate(e.Row));
		}

		protected virtual void _(Events.RowUpdated<BlanketSOLineSplit> e)
		{
			OnSchedOrderDateUpdated(e.Row, CalcSchedOrderDate(e.OldRow), CalcSchedOrderDate(e.Row));
		}

		protected virtual void _(Events.RowDeleted<BlanketSOLineSplit> e)
		{
			OnSchedOrderDateUpdated(e.Row, CalcSchedOrderDate(e.Row), null);
		}

		private BlanketSOOrder GetBlanketOrder(string orderType, string orderNbr)
		{
			BlanketSOOrder res = SelectFrom<BlanketSOOrder>
				.Where<BlanketSOOrder.orderType.IsEqual<@P.AsString.ASCII>
				.And<BlanketSOOrder.orderNbr.IsEqual<@P.AsString>>>
				.View.Select(Base, orderType, orderNbr);
			if (res == null)
			{
				throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOOrder>(), orderType, orderNbr);
			}
			return res;
		}

		protected virtual void OnSchedOrderDateUpdated(BlanketSOLineSplit split, DateTime? oldDate, DateTime? newDate)
		{
			if (oldDate == newDate || SuppressedMode)
				return;

			BlanketSOOrder blanketOrder = GetBlanketOrder(split.OrderType, split.OrderNbr);
			bool decreased = (newDate != null && (oldDate == null || newDate < oldDate));
			if (decreased)
			{
				if (blanketOrder.MinSchedOrderDate == null
					|| blanketOrder.MinSchedOrderDate > newDate)
				{
					blanketOrder.MinSchedOrderDate = newDate;
					blanketOrder = (BlanketSOOrder)Base.Caches<BlanketSOOrder>().Update(blanketOrder);
				}
			}
			else
			{
				if (blanketOrder.MinSchedOrderDate == null
					|| blanketOrder.MinSchedOrderDate >= oldDate)
				{
					RecalculateMinSchedOrderDate(blanketOrder);
				}
			}
		}

		protected void RecalculateMinSchedOrderDate(BlanketSOOrder blanketOrder)
		{
			blanketOrder.MinSchedOrderDate =
				SelectFrom<BlanketSOLineSplit>
					.Where<BlanketSOLineSplit.FK.BlanketOrder.SameAsCurrent
						.And<BlanketSOLineSplit.completed.IsEqual<False>>
						.And<BlanketSOLineSplit.qty.IsGreater<BlanketSOLineSplit.qtyOnOrders.Add<BlanketSOLineSplit.receivedQty>>>>
					.View.SelectMultiBound(Base, new[] { blanketOrder }).RowCast<BlanketSOLineSplit>()
					.Min(s => s.SchedOrderDate);
			blanketOrder = (BlanketSOOrder)Base.Caches<BlanketSOOrder>().Update(blanketOrder);
		}
	}
}
