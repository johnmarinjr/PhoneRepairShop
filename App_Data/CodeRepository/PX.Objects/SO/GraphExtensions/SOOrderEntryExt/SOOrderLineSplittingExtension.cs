using System;
using System.Collections;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;

using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class SOOrderLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<SOOrderEntry, SOOrder, SOLine, SOLineSplit>
	{
		#region State
		public bool IsLocationEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || (ordertype.RequireShipping == false && ordertype.RequireLocation == true && ordertype.INDocType != INTranType.NoUpdate);
			}
		}

		public bool IsLotSerialRequired
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireLotSerial == true;
			}
		}

		public bool IsLSEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireLocation == true || ordertype.RequireLotSerial == true;
			}
		}

		public bool IsBlanketOrder
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype?.Behavior == SOBehavior.BL;
			}
		}
		#endregion

		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(SOLineSplit.FK.Order.SameAsCurrent);

		protected override Type LineQtyField => typeof(SOLine.orderQty);

		public override SOLineSplit LineToSplit(SOLine line)
		{
			using (new InvtMultScope(line))
			{
				SOLineSplit ret = (SOLineSplit)line;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;

				return ret;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<SOOrder>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.Row<SOOrder>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Actions
		public override IEnumerable ShowSplits(PXAdapter adapter)
		{
			SOLine currentSOLine = LineCurrent;

			if (currentSOLine?.LineType == SOLineType.MiscCharge && !IsBlanketOrder)
				throw new PXSetPropertyException(Messages.BinLotSerialInvalid);

			if (IsLSEntryEnabled)
				if (currentSOLine != null && currentSOLine.LineType != SOLineType.Inventory)
					throw new PXSetPropertyException(Messages.BinLotSerialInvalid);

			return base.ShowSplits(adapter);
		}

		#endregion

		#region Event Handlers
		#region SOOrder
		protected SOOrder _LastSelected;

		protected virtual void EventHandler(ManualEvent.Row<SOOrder>.Selected.Args e)
		{
			if (_LastSelected == null || !ReferenceEquals(_LastSelected, e.Row))
			{
				PXUIFieldAttribute.SetRequired<SOLine.locationID>(LineCache, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.locationID>(LineCache, null, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.lotSerialNbr>(LineCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.expireDate>(LineCache, null, IsLSEntryEnabled);

				PXUIFieldAttribute.SetVisible<SOLineSplit.inventoryID>(SplitCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.locationID>(SplitCache, null, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.lotSerialNbr>(SplitCache, null, !IsBlanketOrder);
				PXUIFieldAttribute.SetVisible<SOLineSplit.expireDate>(SplitCache, null, IsLSEntryEnabled);

				if (Base.Views.TryGetValue(TypePrefixed(nameof(LotSerOptions)), out PXView view))
					view.AllowSelect = IsLSEntryEnabled;

				if (e.Row != null)
					_LastSelected = e.Row;
			}

			showSplits.SetEnabled(false);

			if (IsLSEntryEnabled)
				showSplits.SetEnabled(true);
		}

		protected virtual void EventHandler(ManualEvent.Row<SOOrder>.Updated.Args e)
		{
			if ((IsLSEntryEnabled || IsBlanketOrder) && e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (SOLine line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(SOOrder)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						string errorMsg = IsBlanketOrder ? Messages.BlanketSplitTotalQtyNotEqualLineQty : Messages.BinLotSerialNotAssigned;
						LineCache.RaiseExceptionHandling<SOLine.orderQty>(line, line.Qty, new PXSetPropertyException(errorMsg));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region SOLine
		protected override void EventHandler(ManualEvent.Row<SOLine>.Updated.Args e)
		{
			try
			{
				using (ResolveNotDecimalUnitErrorRedirectorScope<SOLineSplit.qty>(e.Row))
					base.EventHandler(e);
			}
			catch (PXUnitConversionException ex)
			{
				bool isUomField(string f) => string.Equals(f, nameof(SOLine.uOM), StringComparison.InvariantCultureIgnoreCase);
				if (!PXUIFieldAttribute.GetErrors(e.Cache, e.Row, PXErrorLevel.Error).Keys.Any(isUomField))
					e.Cache.RaiseExceptionHandling<SOLine.uOM>(e.Row, null, ex);
			}
		}

		protected override void EventHandler(ManualEvent.Row<SOLine>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (e.Row.LineType.IsIn(SOLineType.Inventory, SOLineType.NonInventory) && e.Row.InvtMult == -1 && e.Row.TranType != INTranType.NoUpdate && e.Row.BaseQty < 0m)
				{
					if (e.Cache.RaiseExceptionHandling<SOLine.orderQty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GE, 0)))
						throw new PXRowPersistingException(nameof(SOLine.orderQty), e.Row.Qty, CS.Messages.Entry_GE, 0);

					return;
				}

				if (IsLSEntryEnabled || IsBlanketOrder)
				{
					SOOrder doc = PXParentAttribute.SelectParent<SOOrder>(e.Cache, e.Row) ?? Base.Document.Current;

					if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					{
						string errorMsg = IsBlanketOrder ? Messages.BlanketSplitTotalQtyNotEqualLineQty : Messages.BinLotSerialNotAssigned;
						if (e.Cache.RaiseExceptionHandling<SOLine.orderQty>(e.Row, e.Row.Qty, new PXSetPropertyException(errorMsg)))
							throw new PXRowPersistingException(nameof(SOLine.orderQty), e.Row.Qty, errorMsg);
					}
				}
			}

			//for normal orders there are only when received numbers which do not require any additional processing
			if (!IsLSEntryEnabled)
			{
				if (e.Row.TranType == INTranType.Transfer && LineCounters.ContainsKey(e.Row))
				{
					//keep Counters when adding splits to Transfer order
					LineCounters[e.Row].UnassignedNumber = 0;
				}
				else
				{
					LineCounters[e.Row] = new Counters { UnassignedNumber = 0 };
				}
			}

			base.EventHandler(e);
		}
		#endregion
		#region SOLineSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();

			ManualEvent.Row<SOLineSplit>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.FieldOf<SOLineSplit, SOLineSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<SOLineSplit, SOLineSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<SOLineSplit, SOLineSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.Row<SOLineSplit>.Selected.Args e)
		{
			if (e.Row != null)
			{
				bool isLineTypeInventory = e.Row.LineType == SOLineType.Inventory;
				object val = e.Cache.GetValueExt<SOLineSplit.isAllocated>(e.Row);
				bool isAllocated = e.Row.IsAllocated == true || (bool?)PXFieldState.UnwrapValue(val) == true;
				bool isCompleted = e.Row.Completed == true;
				bool isIssue = e.Row.Operation == SOOperation.Issue;
				bool IsLinked = e.Row.PONbr != null || e.Row.SOOrderNbr != null && e.Row.IsAllocated == true;
				bool isPOSchedule = e.Row.POCreate == true || e.Row.POCompleted == true;

				SOLine parent = PXParentAttribute.SelectParent<SOLine>(e.Cache, e.Row);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.subItemID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.completed>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shippedQty>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shipmentNbr>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.isAllocated>(e.Cache, e.Row, isLineTypeInventory && isIssue && !isCompleted && !isPOSchedule && e.Row.ChildLineCntr == 0);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.siteID>(e.Cache, e.Row, !isCompleted && isLineTypeInventory && isAllocated && !IsLinked && !IsBlanketOrder);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.qty>(e.Cache, e.Row, !isCompleted && !IsLinked);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shipDate>(e.Cache, e.Row, !isCompleted && parent?.ShipComplete == SOShipComplete.BackOrderAllowed);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.pOCreate>(e.Cache, e.Row, IsBlanketOrder && !isCompleted && !isAllocated && parent?.POCreate == true && e.Row.PONbr == null);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.pONbr>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.pOReceiptNbr>(e.Cache, e.Row, false);
				e.Cache.Adjust<INLotSerialNbrAttribute>(e.Row).For<SOLineSplit.lotSerialNbr>(a => a.ForceDisable = isCompleted || isPOSchedule);
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOLineSplit, SOLineSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				using (new InvtMultScope(LineCurrent))
				{
					e.NewValue = LineCurrent.InvtMult;
					e.Cancel = true;
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOLineSplit, SOLineSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOLineSplit, SOLineSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = SuppressedMode == true || e.NewValue != null || !IsLocationEnabled;
			}
		}

		protected override void EventHandler(ManualEvent.Row<SOLineSplit>.Inserting.Args e)
		{
			if (IsLSEntryEnabled)
			{
				if (e.ExternalCall && e.Row.LineType != SOLineType.Inventory)
					throw new PXSetPropertyException(ErrorMessages.CantInsertRecord);

				base.EventHandler(e);

				if (e.Row != null && !IsLocationEnabled && e.Row.LocationID != null)
					e.Row.LocationID = null;
			}
		}

		public override void EventHandler(ManualEvent.Row<SOLineSplit>.Persisting.Args e)
		{
			base.EventHandler(e);
			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				bool requireLocationAndSubItem = e.Row.RequireLocation == true && e.Row.IsStockItem == true && e.Row.BaseQty != 0m;

				PXDefaultAttribute.SetPersistingCheck<SOLineSplit.subItemID>(e.Cache, e.Row, requireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<SOLineSplit.locationID>(e.Cache, e.Row, requireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}
		}

		public override void EventHandlerUOM(ManualEvent.FieldOf<SOLineSplit>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (UseBaseUnitInSplit(e.Row, item))
			{
				e.NewValue = ((InventoryItem)item).BaseUnit;
				e.Cancel = true;
			}
			else
			{
				base.EventHandlerUOM(e);
			}
		}
		#endregion
		#endregion

		#region Select Helpers
		internal SOLineSplit[] GetSplits(SOLine line) => SelectSplits(line);

		protected override SOLineSplit[] SelectSplits(SOLineSplit split) => SelectSplits(split, excludeCompleted: true);
		protected virtual SOLineSplit[] SelectSplits(SOLineSplit split, bool excludeCompleted = true)
		{
			bool NotCompleted(SOLineSplit a) => a.Completed == false || excludeCompleted == false && a.PONbr == null && a.SOOrderNbr == null;
			if (Availability.IsOptimizationEnabled)
				return SelectAllSplits(split).Where(NotCompleted).ToArray();

			return base.SelectSplits(split).Where(NotCompleted).ToArray();
		}

		protected override SOLineSplit[] SelectSplits(SOLine line) => SelectAllSplits(line).Where(s => s.Completed == false).ToArray();

		protected virtual SOLineSplit[] SelectAllSplits(SOLine line)
		{
			if (Availability.IsOptimizationEnabled)
				return SelectAllSplits(LineToSplit(line));

			return base.SelectSplits(line);
		}

		private SOLineSplit[] SelectAllSplits(SOLineSplit split)
		{
			return PXParentAttribute
				.SelectSiblings(SplitCache, split, typeof(SOOrder))
				.Cast<SOLineSplit>()
				.Where(a =>
					SameInventoryItem(a, split) &&
					a.LineNbr == split.LineNbr)
				.ToArray();
		}

		protected override SOLineSplit[] SelectSplitsOrdered(SOLineSplit split) => SelectSplitsOrdered(split, excludeCompleted: true);
		protected virtual SOLineSplit[] SelectSplitsOrdered(SOLineSplit split, bool excludeCompleted = true)
		{
			return SelectSplits(split, excludeCompleted)
				.OrderBy(s => s.Completed == true ? 0 : s.IsAllocated == true ? 1 : 2)
				.ThenBy(s => s.SplitLineNbr)
				.ToArray();
		}

		protected override SOLineSplit[] SelectSplitsReversed(SOLineSplit split) => SelectSplitsReversed(split, excludeCompleted: true);
		protected virtual SOLineSplit[] SelectSplitsReversed(SOLineSplit split, bool excludeCompleted = true)
		{
			return SelectSplits(split, excludeCompleted)
				.OrderByDescending(s => s.Completed == true ? 0 : s.IsAllocated == true ? 1 : 2)
				.ThenByDescending(s => s.SplitLineNbr)
				.ToArray();
		}
		#endregion

		#region Select LotSerial Status
		protected override PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(SOLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (IsLocationEnabled || !IsLotSerialRequired)
				return base.GetSerialStatusCmdBase(line, item);

			return new
				SelectFrom<INLotSerialStatus>.
				InnerJoin<INLocation>.On<INLotSerialStatus.FK.Location>.
				InnerJoin<INSiteLotSerial>.On<
					INSiteLotSerial.inventoryID.IsEqual<INLotSerialStatus.inventoryID>.
					And<INSiteLotSerial.siteID.IsEqual<INLotSerialStatus.siteID>>.
					And<INSiteLotSerial.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr>>>.
				Where<
					INLotSerialStatus.inventoryID.IsEqual<INLotSerialStatus.inventoryID.FromCurrent>.
					And<INLotSerialStatus.siteID.IsEqual<INLotSerialStatus.siteID.FromCurrent>>.
					And<INLotSerialStatus.qtyOnHand.IsGreater<decimal0>>.
					And<INSiteLotSerial.qtyHardAvail.IsGreater<decimal0>>>.
				View(Base);
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, SOLine line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			}
			else
			{
				switch (line.TranType)
				{
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid.IsEqual<True>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(line.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}
		#endregion

		protected override void UpdateCounters(Counters counters, SOLineSplit split)
		{
			base.UpdateCounters(counters, split);

			if (split.POCreate == true || split.AMProdCreate == true)
			{
				//base shipped qty in context of purchase for so is meaningless and equals zero, so it's appended for dropship context
				counters.BaseQty -= split.BaseReceivedQty.Value + split.BaseShippedQty.Value;
			}
		}

		protected virtual bool UseBaseUnitInSplit(SOLineSplit split, PXResult<InventoryItem, INLotSerClass> item)
			=> !IsBlanketOrder && item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered;
	}
}
