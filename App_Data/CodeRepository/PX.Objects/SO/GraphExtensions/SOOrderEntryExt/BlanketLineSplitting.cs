using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;
using PX.Objects.SO.DAC.Projections;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	[PXProtectedAccess(typeof(SOOrderLineSplittingExtension))]
	public abstract class BlanketLineSplitting : PXGraphExtension<SOOrderLineSplittingAllocatedExtension, SOOrderLineSplittingExtension, SOOrderEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		#region Protected Access
		[PXProtectedAccess] protected abstract PXCache<SOLine> LineCache { get; }
		[PXProtectedAccess] protected abstract PXCache<SOLineSplit> SplitCache { get; }

		[PXProtectedAccess] protected abstract PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(int? inventoryID);
		[PXProtectedAccess] protected abstract void SetSplitQtyWithLine(SOLineSplit split, SOLine line);
		[PXProtectedAccess] protected abstract void SetLineQtyFromBase(SOLine line);

		[PXProtectedAccess] protected abstract Dictionary<SOLine, Counters> LineCounters { get; }
		[PXProtectedAccess] protected abstract SOLineSplit[] SelectSplitsReversed(SOLineSplit split);
		[PXProtectedAccess(typeof(SOOrderLineSplittingAllocatedExtension))]
		protected abstract void RefreshViewOf(PXCache cache);
		#endregion

		private bool _internalCall = false;

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.schedOrderDate> e)
			=> ForbidChangeIfMultipleSplits(e.Row);

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.schedShipDate> e)
			=> ForbidChangeIfMultipleSplits(e.Row);

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.pOCreateDate> e)
			=> ForbidChangeIfMultipleSplits(e.Row);

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.customerOrderNbr> e)
			=> ForbidChangeIfMultipleSplits(e.Row);

		protected virtual void ForbidChangeIfMultipleSplits(SOLine line)
		{
			if (line.Behavior == SOBehavior.BL)
			{
				var splits = Base.splits.View.SelectMultiBound(new[] { line });
				if (splits.Count > 1)
				{
					throw new PXSetPropertyException(Messages.FieldForLineWithMultipleSplits);
				}
			}
		}

		protected virtual void _(Events.RowUpdated<SOLine> e)
		{
			if (e.Row.Behavior != SOBehavior.BL || _internalCall) return;

			bool schedOrderDateChanged = !e.Cache.ObjectsEqual<SOLine.schedOrderDate>(e.OldRow, e.Row),
				schedShipDateChanged = !e.Cache.ObjectsEqual<SOLine.schedShipDate>(e.OldRow, e.Row),
				poCreateDateChanged = !e.Cache.ObjectsEqual<SOLine.pOCreateDate>(e.OldRow, e.Row),
				custOrderNbrChanged = !e.Cache.ObjectsEqual<SOLine.customerOrderNbr>(e.OldRow, e.Row);
			if (schedOrderDateChanged || schedShipDateChanged || poCreateDateChanged || custOrderNbrChanged)
			{
				_internalCall = true;
				try
				{
					foreach (SOLineSplit split in Base.splits.View.SelectMultiBound(new[] { e.Row }))
					{
						if (schedOrderDateChanged)
							split.SchedOrderDate = e.Row.SchedOrderDate;
						if (schedShipDateChanged)
							split.SchedShipDate = e.Row.SchedShipDate;
						if (poCreateDateChanged)
							split.POCreateDate = e.Row.POCreateDate;
						if (custOrderNbrChanged)
							split.CustomerOrderNbr = e.Row.CustomerOrderNbr;
						Base.splits.Update(split);
					}
				}
				finally
				{
					_internalCall = false;
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOLineSplit, SOLineSplit.customerOrderNbr> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && !string.IsNullOrEmpty(Base.Transactions.Current?.CustomerOrderNbr))
			{
				bool anotherSplitExists = Base.splits.View.SelectMultiBound(new[] { Base.Transactions.Current })
					.RowCast<SOLineSplit>()
					.Any(s => s != e.Row);
				e.NewValue = anotherSplitExists ? null : Base.Transactions.Current.CustomerOrderNbr;
			}
		}

		protected virtual void _(Events.FieldVerifying<SOLineSplit, SOLineSplit.qty> e)
		{
			if (e.Row.Behavior != SOBehavior.BL || Base.Transactions.Current == null || Base1.SuppressedMode) return;

			decimal? diff = (decimal?)e.NewValue - e.Row.Qty;
			if (diff > Base.Transactions.Current.UnassignedQty)
			{
				e.NewValue = e.Row.Qty + Base.Transactions.Current.UnassignedQty;
				Base.splits.Cache.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, e.NewValue,
					new PXSetPropertyException(Messages.BlanketSplitTotalQtyNotEqualLineQty));
			}
		}

		protected virtual void _(Events.RowInserted<SOLineSplit> e)
			=> UpdateDatesFromSplitsToLine(null, e.Row);

		protected virtual void _(Events.RowUpdated<SOLineSplit> e)
			=> UpdateDatesFromSplitsToLine(e.OldRow, e.Row);

		protected virtual void _(Events.RowDeleted<SOLineSplit> e)
			=> UpdateDatesFromSplitsToLine(e.Row, null);

		protected virtual void UpdateDatesFromSplitsToLine(SOLineSplit oldRow, SOLineSplit newRow)
		{
			SOLineSplit row = newRow ?? oldRow;
			if (row.Behavior != SOBehavior.BL || _internalCall) return;

			PXCache cache = Base.splits.Cache;
			bool insertedOrDeleted = (newRow == null || oldRow == null),
				schedOrderDateChanged = !cache.ObjectsEqual<SOLineSplit.schedOrderDate>(oldRow, newRow),
				schedShipDateChanged = !cache.ObjectsEqual<SOLine.schedShipDate>(oldRow, newRow),
				poCreateDateChanged = !cache.ObjectsEqual<SOLineSplit.pOCreateDate>(oldRow, newRow);
			if (insertedOrDeleted || schedOrderDateChanged || schedShipDateChanged || poCreateDateChanged)
			{
				SOLine line = PXParentAttribute.SelectParent<SOLine>(cache, row);
				if (line == null) return;
				var siblingsList = Base.splits.View.SelectMultiBound(new[] { line });
				int siblingsCount = siblingsList.Count;
				var siblings = siblingsList.RowCast<SOLineSplit>();
				SOLineSplit firstSplit = siblings.FirstOrDefault();
				if (firstSplit != null && insertedOrDeleted)
				{
					string newParentValue = siblingsCount > 1 ? null : firstSplit.CustomerOrderNbr;
					if (line.CustomerOrderNbr != newParentValue)
					{
						Base.Transactions.Cache.SetValue<SOLine.customerOrderNbr>(line, newParentValue);
						Base.Transactions.Cache.MarkUpdated(line);
					}
				}
				if (firstSplit != null && (insertedOrDeleted || schedOrderDateChanged))
				{
					DateTime? newParentValue = siblings.All(s => s.SchedOrderDate == firstSplit.SchedOrderDate) ? firstSplit.SchedOrderDate : null;
					if (line.SchedOrderDate != newParentValue)
					{
						Base.Transactions.Cache.SetValue<SOLine.schedOrderDate>(line, newParentValue);
						Base.Transactions.Cache.MarkUpdated(line);
					}
				}
				if (firstSplit != null && (insertedOrDeleted || schedShipDateChanged))
				{
					DateTime? newParentValue = siblings.All(s => s.SchedShipDate == firstSplit.SchedShipDate) ? firstSplit.SchedShipDate : null;
					if (line.SchedShipDate != newParentValue)
					{
						Base.Transactions.Cache.SetValue<SOLine.schedShipDate>(line, newParentValue);
						Base.Transactions.Cache.MarkUpdated(line);
					}
				}
				if (firstSplit != null && (insertedOrDeleted || poCreateDateChanged))
				{
					DateTime? newParentValue = siblings.All(s => s.POCreateDate == firstSplit.POCreateDate) ? firstSplit.POCreateDate : null;
					if (line.POCreateDate != newParentValue)
					{
						Base.Transactions.Cache.SetValue<SOLine.pOCreateDate>(line, newParentValue);
						Base.Transactions.Cache.MarkUpdated(line);
					}
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingAllocatedExtension.TruncateSchedules(SOLine, decimal)"/>
		/// </summary>
		[PXOverride]
		public virtual void TruncateSchedules(SOLine line, decimal baseQty, Action<SOLine, decimal> base_TruncateSchedules)
		{
			if (line.Behavior != SOBehavior.BL)
			{
				base_TruncateSchedules(line, baseQty);
				return;
			}

			LineCounters.Remove(line);
			if (line.UnassignedQty > 0m)
			{
				if (line.UnassignedQty >= baseQty)
				{
					line.UnassignedQty -= baseQty;
					baseQty = 0m;
				}
				else
				{
					baseQty -= (decimal)line.UnassignedQty;
					line.UnassignedQty = 0m;
				}
			}

			foreach (var split in SelectSplitsReversed(line).OrderBy(split => split.ChildLineCntr > 0))
			{
				decimal? splitBaseAvailableQty = split.BaseQty - split.BaseQtyOnOrders;
				if (baseQty >= splitBaseAvailableQty && split.ChildLineCntr == 0)
				{
					SplitCache.Delete(split);
				}
				else
				{
					var newSplit = PXCache<SOLineSplit>.CreateCopy(split);
					newSplit.BaseQty -= Math.Min(splitBaseAvailableQty.Value, baseQty);
					SetSplitQtyWithLine(newSplit, line);

					SplitCache.Update(newSplit);
				}
				baseQty -= splitBaseAvailableQty.Value;
				if (baseQty <= 0) break;
			}
		}

		/// <summary>
		/// Overrides <see cref="LineSplittingExtension{SOOrderEntry, SOOrder, SOLine, SOLineSplit}.SetUnassignedQty"/>
		/// </summary>
		[PXOverride]
		public virtual void SetUnassignedQty(SOLine line, decimal detailsBaseQty, bool allowNegative,
			Action<SOLine, decimal, bool> base_SetUnassignedQty)
		{
			if (line.Behavior != SOBehavior.BL)
			{
				base_SetUnassignedQty(line, detailsBaseQty, allowNegative);
				return;
			}

			line.UnassignedQty = PXDBQuantityAttribute.Round(line.BaseOpenQty.Value - detailsBaseQty);
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingAllocatedExtension.SchedulesEqual(SOLineSplit, SOLineSplit, PXDBOperation)"/>
		/// </summary>
		[PXOverride]
		public virtual bool SchedulesEqual(SOLineSplit a, SOLineSplit b, PXDBOperation operation,
			Func<SOLineSplit, SOLineSplit, PXDBOperation, bool> base_SchedulesEqual)
		{
			if (b.Behavior == SOBehavior.BL)
			{
				if (operation == PXDBOperation.Insert)
				{
					return base_SchedulesEqual(a, b, operation)
						&& a.SchedOrderDate == b.SchedOrderDate
						&& a.SchedShipDate == b.SchedShipDate
						&& a.POCreateDate == b.POCreateDate
						&& a.CustomerOrderNbr == b.CustomerOrderNbr;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return base_SchedulesEqual(a, b, operation);
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingAllocatedExtension.AssignNewSplitFields(SOLineSplit, SOLine)"/>
		/// </summary>
		[PXOverride]
		public virtual void AssignNewSplitFields(SOLineSplit split, SOLine line, Action<SOLineSplit, SOLine> base_AssignNewSplitFields)
		{
			base_AssignNewSplitFields(split, line);

			if (!string.IsNullOrEmpty(line.BlanketNbr))
			{
				var blanketSplit = PXParentAttribute.SelectParent<BlanketSOLineSplit>(LineCache, line);
				if (blanketSplit == null)
				{
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOLineSplit>(),
						line.BlanketType, line.BlanketNbr, line.BlanketLineNbr, line.BlanketSplitLineNbr);
				}
				if (blanketSplit.IsAllocated == true)
				{
					split.IsAllocated = true;
				}
				split.POType = blanketSplit.POType;
				split.PONbr = blanketSplit.PONbr;
				split.POLineNbr = blanketSplit.POLineNbr;
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.UpdateCounters(Counters, SOLineSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateCounters(Counters counters, SOLineSplit split,
			Action<Counters, SOLineSplit> base_UpdateCounters)
		{
			base_UpdateCounters(counters, split);

			if (split.Behavior == SOBehavior.BL && split.POCreate != true && split.AMProdCreate != true)
			{
				counters.BaseQty -= split.BaseShippedQty.Value;
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingAllocatedExtension.ShouldUncompleteSchedule(SOLine, SOLineSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual bool ShouldUncompleteSchedule(SOLine line, SOLineSplit split,
			Func<SOLine, SOLineSplit, bool> base_ShouldUncompleteSchedule)
		{
			if (split.Behavior == SOBehavior.BL)
			{
				// if we turn off Misc lines full reopening
				// then SOLine.ClosedQty must be recalculated on SOLine_Completed_FieldUpdated
				return (split.LineType == SOLineType.MiscCharge) || (split.ShippedQty < split.Qty);
			}
			else
			{
				return base_ShouldUncompleteSchedule(line, split);
			}
		}

		/// <summary>
		/// Overrides <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandler(ManualEvent.Row{TSplit}.Updated.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<SOLineSplit>.Updated.Args e,
			Action<ManualEvent.Row<SOLineSplit>.Updated.Args> base_EventHandler)
		{
			base_EventHandler(e);
			if (e.Row.Behavior == SOBehavior.BL && Base.Transactions.Current != null && !Base1.SuppressedMode && e.ExternalCall)
			{
				// if user decreased the qty on a split then a new split with this qty should appear
				decimal? diff = e.Row.Qty - e.OldRow.Qty;
				if (diff < 0m)
				{
					decimal remainderQty = Math.Min(-diff.Value, Base.Transactions.Current.UnassignedQty ?? 0m);
					var split = (SOLineSplit)SplitCache.Insert();
					if (e.OldRow.IsAllocated == true && e.Row.IsAllocated == true)
					{
						split.IsAllocated = true;
					}
					split.Qty = remainderQty;
					split = (SOLineSplit)SplitCache.Update(split);
					RefreshViewOf(SplitCache);
				}
			}
		}
	}
}
