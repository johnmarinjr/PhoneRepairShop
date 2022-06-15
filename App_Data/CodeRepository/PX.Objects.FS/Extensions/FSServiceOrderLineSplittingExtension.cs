using System;
using System.Collections;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.FS
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class FSServiceOrderLineSplittingExtension : LineSplittingExtension<ServiceOrderEntry, FSServiceOrder, FSSODet, FSSODetSplit>
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
		#endregion

		#region Confirugation
		protected override Type SplitsToDocumentCondition => typeof(FSSODetSplit.FK.ServiceOrder.SameAsCurrent);

		protected override Type LineQtyField => typeof(FSSODet.orderQty);

		public override FSSODetSplit LineToSplit(FSSODet line)
		{
			using (new InvtMultScope(line))
			{
				FSSODetSplit ret = (FSSODetSplit)line;
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
			ManualEvent.Row<FSServiceOrder>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.Row<FSServiceOrder>.Updated.Subscribe(Base, EventHandler);

			showSplits?.SetVisible(
				PXAccess.FeatureInstalled<FeaturesSet.inventory>() &&
				Base.ServiceOrderTypeSelected?.Current?.Behavior != FSSrvOrdType.behavior.Values.Quote);
		}
		#endregion

		#region Actions
		public override IEnumerable ShowSplits(PXAdapter adapter)
		{
			if (LineCurrent == null)
				return adapter.Get();

			if (LineCurrent.InventoryID == null)
				throw new PXSetPropertyException(TX.Error.NotValidFunctionWithInstructionOrCommentLines);

			if (LineCurrent.LineType.IsIn(ID.LineType_ALL.SERVICE, ID.LineType_ALL.NONSTOCKITEM) && LineCurrent.EnablePO == false)
				throw new PXSetPropertyException(SO.Messages.BinLotSerialInvalid);

			if (IsLSEntryEnabled)
			{
				// TODO: Disable all editing in the split window when the item is a non-stock.
				// We must allow opening the split window with non-stock items
				// so that the user can see the PO receipt information.
				/*
				if (LineCurrent.SOLineType != SOLineType.Inventory)
				{
					throw new PXSetPropertyException(TX.Error.CANNOT_USE_ALLOCATIONS_FOR_NONSTOCK_ITEMS);
				}
				*/
			}

			return base.ShowSplits(adapter);
		}
		#endregion

		#region Event Handlers
		#region FSServiceOrder
		protected FSServiceOrder _LastSelected;

		protected virtual void EventHandler(ManualEvent.Row<FSServiceOrder>.Selected.Args e)
		{
			if (_LastSelected == null || !ReferenceEquals(_LastSelected, e.Row))
			{
				PXUIFieldAttribute.SetRequired<FSSODet.locationID>(LineCache, IsLocationEnabled);

				PXUIFieldAttribute.SetVisible<FSSODet.locationID>(LineCache, null, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<FSSODet.lotSerialNbr>(LineCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODet.expireDate>(LineCache, null, IsLSEntryEnabled);

				PXUIFieldAttribute.SetVisible<FSSODetSplit.inventoryID>(SplitCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.expireDate>(SplitCache, null, IsLSEntryEnabled);

				if (Base.Views.TryGetValue(TypePrefixed(nameof(LotSerOptions)), out PXView view))
					view.AllowSelect = IsLSEntryEnabled;

				if (e.Row != null)
					_LastSelected = e.Row;
			}

			showSplits.SetEnabled(false);

			if (IsLSEntryEnabled)
				showSplits.SetEnabled(true);
		}

		protected virtual void EventHandler(ManualEvent.Row<FSServiceOrder>.Updated.Args e)
		{
			if (IsLSEntryEnabled && e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (FSSODet line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(FSServiceOrder)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<FSSODet.orderQty>(line, line.Qty, new PXSetPropertyException(SO.Messages.BinLotSerialNotAssigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region FSSODet
		protected override void EventHandler(ManualEvent.Row<FSSODet>.Updated.Args e)
		{
			try
			{
				base.EventHandler(e);
			}
			catch (PXUnitConversionException ex)
			{
				bool isUomField(string f) => string.Equals(f, nameof(FSSODet.uOM), StringComparison.InvariantCultureIgnoreCase);
				if (!PXUIFieldAttribute.GetErrors(e.Cache, e.Row, PXErrorLevel.Error).Keys.Any(isUomField))
					e.Cache.RaiseExceptionHandling<FSSODet.uOM>(e.Row, null, ex);
			}
		}

		protected override void EventHandler(ManualEvent.Row<FSSODet>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if ((e.Row.SOLineType == SOLineType.Inventory || e.Row.SOLineType == SOLineType.NonInventory && e.Row.InvtMult == -1) && e.Row.TranType != INTranType.NoUpdate && e.Row.BaseQty < 0m)
				{
					if (e.Cache.RaiseExceptionHandling<FSSODet.orderQty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GE, 0)))
						throw new PXRowPersistingException(nameof(FSSODet.orderQty), e.Row.Qty, CS.Messages.Entry_GE, 0);

					return;
				}

				if (IsLSEntryEnabled)
				{
					FSServiceOrder doc = PXParentAttribute.SelectParent<FSServiceOrder>(e.Cache, e.Row) ?? Base.ServiceOrderRecords.Current;

					if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
						if (e.Cache.RaiseExceptionHandling<FSSODet.orderQty>(e.Row, e.Row.Qty, new PXSetPropertyException(SO.Messages.BinLotSerialNotAssigned)))
							throw new PXRowPersistingException(nameof(FSSODet.orderQty), e.Row.Qty, SO.Messages.BinLotSerialNotAssigned);
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
		#region FSSODetSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.Row<FSSODetSplit>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.Row<FSSODetSplit>.Selected.Args e)
		{
			if (e.Row != null)
			{
				bool isLineTypeInventory = e.Row.LineType == SOLineType.Inventory;
				object val = e.Cache.GetValueExt<FSSODetSplit.isAllocated>(e.Row);
				bool isAllocated = e.Row.IsAllocated == true || (bool?)PXFieldState.UnwrapValue(val) == true;
				bool isCompleted = e.Row.Completed == true;
				bool isIssue = e.Row.Operation == SOOperation.Issue;
				bool IsLinked = e.Row.PONbr != null || e.Row.SOOrderNbr != null && e.Row.IsAllocated == true;

				FSSODet parent = PXParentAttribute.SelectParent<FSSODet>(e.Cache, e.Row);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.subItemID>(e.Cache, e.Row, isLineTypeInventory);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.completed>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.shippedQty>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.shipmentNbr>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.isAllocated>(e.Cache, e.Row, isLineTypeInventory && isIssue && !isCompleted);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.siteID>(e.Cache, e.Row, isLineTypeInventory && isAllocated && !IsLinked);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.qty>(e.Cache, e.Row, !isCompleted && !IsLinked);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.shipDate>(e.Cache, e.Row, !isCompleted && parent?.ShipComplete == SOShipComplete.BackOrderAllowed);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.pONbr>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<FSSODetSplit.pOReceiptNbr>(e.Cache, e.Row, false);

				if (e.Row.Completed == true)
				{
					PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				}
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.invtMult>.Defaulting.Args<short?> e)
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

		public virtual void EventHandler(ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<FSSODetSplit, FSSODetSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && LineCurrent.LocationID != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = SuppressedMode == true || e.NewValue != null || !IsLocationEnabled;
			}
		}

		protected override void EventHandler(ManualEvent.Row<FSSODetSplit>.Inserting.Args e)
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

		public override void EventHandler(ManualEvent.Row<FSSODetSplit>.Persisting.Args e)
		{
			base.EventHandler(e);
			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				bool requireLocationAndSubItem = e.Row.RequireLocation == true && e.Row.IsStockItem == true && e.Row.BaseQty != 0m;

				PXDefaultAttribute.SetPersistingCheck<FSSODetSplit.subItemID>(e.Cache, e.Row, requireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<FSSODetSplit.locationID>(e.Cache, e.Row, requireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}
		}

		public override void EventHandlerUOM(ManualEvent.FieldOf<FSSODetSplit>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
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
		internal FSSODetSplit[] GetSplits(FSSODet line) => SelectSplits(line);

		protected override FSSODetSplit[] SelectSplits(FSSODetSplit split) => SelectSplits(split, true);
		protected virtual FSSODetSplit[] SelectSplits(FSSODetSplit split, bool excludeCompleted = true)
		{
			bool NotCompleted(FSSODetSplit a) => a.Completed == false || excludeCompleted == false && a.PONbr == null && a.SOOrderNbr == null;
			if (Availability.IsOptimizationEnabled)
				return SelectAllSplits(split).Where(NotCompleted).ToArray();

			return base.SelectSplits(split).Where(NotCompleted).ToArray();
		}

		//protected override FSSODetSplit[] SelectSplits(FSSODet line) => SelectAllSplits(line).Where(s => s.Completed == false).ToArray();

		//protected virtual FSSODetSplit[] SelectAllSplits(FSSODet line)
		//{
		//	if (Availability.IsOptimizationEnabled)
		//		return SelectAllSplits(LineToSplit(line));

		//	return base.SelectSplits(line);
		//}

		private FSSODetSplit[] SelectAllSplits(FSSODetSplit split)
		{
			return PXParentAttribute
				.SelectSiblings(SplitCache, split, typeof(FSServiceOrder))
				.Cast<FSSODetSplit>()
				.Where(a =>
					SameInventoryItem(a, split) &&
					a.LineNbr == split.LineNbr)
				.ToArray();
		}


		protected override FSSODetSplit[] SelectSplitsOrdered(FSSODetSplit split) => SelectSplitsOrdered(split, excludeCompleted: true);
		protected virtual FSSODetSplit[] SelectSplitsOrdered(FSSODetSplit split, bool excludeCompleted = true)
		{
			return SelectSplits(split, excludeCompleted)
				.OrderBy(s => s.Completed == true ? 0 : s.IsAllocated == true ? 1 : 2)
				.ThenBy(s => s.SplitLineNbr)
				.ToArray();
		}

		protected override FSSODetSplit[] SelectSplitsReversed(FSSODetSplit split) => SelectSplitsReversed(split, excludeCompleted: true);
		protected virtual FSSODetSplit[] SelectSplitsReversed(FSSODetSplit split, bool excludeCompleted = true)
		{
			return SelectSplits(split, excludeCompleted)
				.OrderByDescending(s => s.Completed == true ? 0 : s.IsAllocated == true ? 1 : 2)
				.ThenByDescending(s => s.SplitLineNbr)
				.ToArray();
		}
		#endregion

		#region Select LotSerial Status
		public override PXSelectBase<INLotSerialStatus> GetSerialStatusCmd(FSSODet line, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<INLotSerialStatus> cmd = new
				SelectFrom<INLotSerialStatus>.
				InnerJoin<INLocation>.On<INLotSerialStatus.FK.Location>.
				Where<
					INLotSerialStatus.inventoryID.IsEqual<INLotSerialStatus.inventoryID.FromCurrent>.
					And<INLotSerialStatus.siteID.IsEqual<INLotSerialStatus.siteID.FromCurrent>>.
					And<INLotSerialStatus.qtyOnHand.IsGreater<decimal0>>>.
				View(Base);

			if (!IsLocationEnabled && IsLotSerialRequired)
			{
				cmd.Join<
					InnerJoin<INSiteLotSerial, On<
						INSiteLotSerial.inventoryID.IsEqual<INLotSerialStatus.inventoryID>.
						And<INSiteLotSerial.siteID.IsEqual<INLotSerialStatus.siteID>>.
						And<INSiteLotSerial.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr>>>>>();
				cmd.WhereAnd<
					Where<INSiteLotSerial.qtyHardAvail.IsGreater<decimal0>>>();
			}

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

			switch (item.GetItem<INLotSerClass>().LotSerIssueMethod)
			{
				case INLotSerIssueMethod.FIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<INLotSerialStatus.receiptDate, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.LIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Desc<INLotSerialStatus.receiptDate, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Expiration:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<INLotSerialStatus.expireDate, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Sequential:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<INLotSerialStatus.lotSerialNbr>>>>();
					break;
				case INLotSerIssueMethod.UserEnterable:
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
					break;
				default:
					throw new PXException();
			}

			return cmd;
		}
		#endregion

		protected override void UpdateCounters(Counters counters, FSSODetSplit split)
		{
			base.UpdateCounters(counters, split);

			if (split.POCreate == true)
			{
				//base shipped qty in context of purchase for so is meaningless and equals zero, so it's appended for dropship context
				counters.BaseQty -= split.BaseReceivedQty.Value + split.BaseShippedQty.Value;
			}
		}
	}
}
