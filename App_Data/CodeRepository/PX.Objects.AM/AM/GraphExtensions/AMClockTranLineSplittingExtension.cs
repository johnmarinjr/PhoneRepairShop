using System;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMClockTranLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<ClockApprovalProcess, AMClockTran, AMClockTran, AMClockTranSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(
			AMClockTranSplit.employeeID.IsEqual<AMClockTran.employeeID.FromCurrent>.
			And<AMClockTranSplit.lineNbr.IsEqual<AMClockTran.lineNbr>>);

		protected override Type LineQtyField => typeof(AMClockTran.qty);

		public override AMClockTranSplit LineToSplit(AMClockTran line)
		{
			using (new InvtMultScope(line))
			{
				AMClockTranSplit ret = line;
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Event Handlers
		#region AMClockTran
		protected override void SubscribeForLineEvents()
		{
			base.SubscribeForLineEvents();
			ManualEvent.FieldOf<AMClockTran, AMClockTran.lastOper>.Updated.Subscribe<bool?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTran, AMClockTran.qty>.Updated.Subscribe<decimal?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTran, AMClockTran.operationID>.Updated.Subscribe<int?>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTran, AMClockTran.lastOper>.Updated.Args<bool?> e)
		{
			SetTranTypeInvtMult(e.Row);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTran, AMClockTran.qty>.Updated.Args<decimal?> e)
		{
			SetTranTypeInvtMult(e.Row);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTran, AMClockTran.operationID>.Updated.Args<int?> e)
		{
			if (!string.IsNullOrWhiteSpace(e.Row?.ProdOrdID) && e.Row.OperationID != null)
				SetTranTypeInvtMult(e.Row);
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTran>.Selected.Args e)
		{
			if (e.Row == null || string.IsNullOrWhiteSpace(e.Row.ProdOrdID))
				return;

			AllowSplits(e.Row.Qty > 0 && e.Row.LastOper.GetValueOrDefault());
			if (e.Row.Qty > 0)
				e.Cache.RaiseFieldUpdated(LineQtyField.Name, e.Row, e.Row.Qty);
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTran>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<AMClockTran.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<AMClockTran.expireDate>(e.Row, null);
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTran>.Updated.Args e)
		{
			if (e.Row?.InventoryID == null || e.Row.OperationID == null || string.IsNullOrWhiteSpace(e.Row.ProdOrdID))
				return;

			var amProdItem = (AMProdItem)PXSelectorAttribute.Select<AMClockTran.prodOrdID>(e.Cache, e.Row);
			if (amProdItem == null)
				return;

			if (e.OldRow.InventoryID != null && e.Row.InventoryID == null || e.Row.InventoryID != e.OldRow.InventoryID)
				foreach (AMClockTranSplit split in PXParentAttribute.SelectSiblings(SplitCache, (AMClockTranSplit)e.Row, typeof(AMClockTran)))
					SplitCache.Delete(split); //Change of item will need a change of splits

			if (e.Row.InvtMult != 0 && e.Row.IsStockItem == true)
			{
				if (e.Row.TranType != e.OldRow.TranType)
					SyncSplitTranType(e.Row);

				if (amProdItem.LastOperationID.GetValueOrDefault() == e.Row.OperationID)
					base.EventHandler(e);

				return;
			}

			e.Cache.SetValue<AMClockTran.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<AMClockTran.expireDate>(e.Row, null);
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTran>.Deleted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTran>.Persisting.Args e)
		{
			//don't need to check this if it's not the last operation
			if (e.Row.LastOper == true && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<AMClockTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(AMClockTran.qty), e.Row.Qty, IN.Messages.BinLotSerialNotAssigned);

			base.EventHandler(e);
		}
		#endregion
		#region AMClockTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.invtMult>.Updated.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.invtMult>.Updated.Args<short?> e)
		{
			if (LineCurrent == null || e.Row == null)
				return;

			if (LineCurrent.LineNbr == e.Row.LineNbr)
				e.Row.TranType = e.Row.InvtMult < 1 ? AMTranType.Adjustment : AMTranType.Receipt;
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent == null || e.Row == null || e.Row.LineNbr != LineCurrent.LineNbr)
				return;

#if DEBUG
			AMDebug.TraceWriteMethodName($"TranType = {e.Row.TranType} [{LineCurrent.TranType}]; InvtMult = {e.Row.InvtMult} [{LineCurrent.InvtMult}]; [{LineCurrent.DebuggerDisplay}]");
#endif
			//Not sure why we would ever want ot use InvtMultScope since it is changing the InvtMult value incorrectly on us when qty < 0
			using (new InvtMultScope(LineCurrent))
			{
				e.NewValue = LineCurrent.InvtMult;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMClockTranSplit, AMClockTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			if (e.Row?.InventoryID == null)
				return;

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (e.Row.InvtMult == null)
				e.Cache.RaiseFieldDefaulting<AMClockTranSplit.invtMult>(e.Row, out _);

			if (e.Row.TranType == null)
				e.Cache.RaiseFieldDefaulting<AMClockTranSplit.tranType>(e.Row, out _);

			INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
			if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
			{
				foreach (AMClockTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMClockTranSplit>(e.Cache, item, mode, 1m))
				{
					e.NewValue = lssplit.LotSerialNbr;
					e.Cancel = true;
				}
			}
			//otherwise default via attribute
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMClockTranSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);
			if (e.Row?.InventoryID != null)
			{
				(var _, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
				if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && lsClass.LotSerAssign == INLotSerAssign.WhenReceived)
					if (e.NewValue.IsNotIn(null, 0m, 1m))
						e.NewValue = 1M;
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMClockTranSplit>.Inserting.Args e)
		{
			base.EventHandler(e);

			if (e.Row == null)
				return;

			var rowParent = PXParentAttribute.SelectParent<AMClockTran>(e.Cache, e.Row);
			if (rowParent == null)
				return;

			e.Row.TranType = rowParent.TranType ?? e.Row.TranType;
			e.Row.InvtMult = AMTranType.InvtMult(e.Row.TranType, rowParent.Qty);
		}
		#endregion
		#endregion

		protected virtual void AllowSplits(bool allow)
		{
			SplitCache.AllowInsert = allow;
			SplitCache.AllowUpdate = allow && LineCache.AllowUpdate;
		}

		protected virtual void SyncSplitTranType(AMClockTran line)
		{
			//LineCache.SetDefaultExt<AMClockTran.invtMult>(tran);
			foreach (var split in PXParentAttribute
				.SelectSiblings(SplitCache, (AMClockTranSplit)line, typeof(AMClockTran))
				.Cast<AMClockTranSplit>()
				.Where(s => s.TranType != line.TranType))
			{
				var copy = PXCache<AMClockTranSplit>.CreateCopy(split);
				split.TranType = line.TranType;
				SplitCache.MarkUpdated(split);
				SplitCache.RaiseRowUpdated(split, copy);
			}
		}

		protected virtual void SetTranTypeInvtMult(AMClockTran line)
		{
			if (line == null)
				return;
#if DEBUG
			var tranTypeOld = line.TranType;
			var invtMultOld = line.InvtMult;
#endif
			var tranTypeNew = line.Qty.GetValueOrDefault() < 0 ?
				AMTranType.Adjustment : AMTranType.Receipt;
			var invtMultNew = line.LastOper.GetValueOrDefault()
				? AMTranType.InvtMult(tranTypeNew, line.Qty)
				: 0;

#if DEBUG
			AMDebug.TraceWriteMethodName($"TranType = {tranTypeNew} (old value = {tranTypeOld}); InvtMult = {invtMultNew} (old value = {invtMultOld})");
#endif
			var syncSplits = false;
			if (invtMultNew != line.InvtMult)
			{
				syncSplits |= line.InvtMult != null;
				LineCache.SetValueExt<AMClockTran.invtMult>(line, invtMultNew);
			}

			if (tranTypeNew != line.TranType)
			{
				syncSplits |= line.TranType != null;
				LineCache.SetValueExt<AMClockTran.tranType>(line, tranTypeNew);
			}

			if (syncSplits)
			{
				SyncSplitTranType(line);
			}
		}
	}
}
