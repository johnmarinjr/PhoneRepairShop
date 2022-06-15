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
	public class AMVendorShipmentLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<VendorShipmentEntry, AMVendorShipment, AMVendorShipLine, AMVendorShipLineSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(AMVendorShipLineSplit.FK.Shipment.SameAsCurrent);

		protected override Type LineQtyField => typeof(AMVendorShipLine.qty);

		public override AMVendorShipLineSplit LineToSplit(AMVendorShipLine line)
		{
			using (new InvtMultScope(line))
			{
				AMVendorShipLineSplit ret = line;
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<AMVendorShipment>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region AMVendorShipment
		protected virtual void EventHandler(ManualEvent.Row<AMVendorShipment>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (AMVendorShipLine line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(AMVendorShipment)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<AMVendorShipLine.qty>(line, line.Qty, new PXSetPropertyException(Messages.LSAMMTranLinesUnassigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region AMVendorShipLine
		protected override void EventHandler(ManualEvent.Row<AMVendorShipLine>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<AMVendorShipLine.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<AMVendorShipLine.expireDate>(e.Row, null);
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMVendorShipLine>.Updated.Args e)
		{
			if (e.Row?.InventoryID == null || e.Row.OperationID == null || string.IsNullOrWhiteSpace(e.Row.ProdOrdID))
				return;

			var amProdItem = (AMProdItem)PXSelectorAttribute.Select<AMVendorShipLine.prodOrdID>(e.Cache, e.Row);
			if (amProdItem == null)
				return;

			if (e.OldRow.InventoryID != null && e.Row.InventoryID == null || e.Row.InventoryID != e.OldRow.InventoryID)
				foreach (AMVendorShipLineSplit split in PXParentAttribute.SelectSiblings(SplitCache, (AMVendorShipLineSplit)e.Row, typeof(AMVendorShipLine)))
					SplitCache.Delete(split); //Change of item will need a change of splits

			if (e.Row.InvtMult != 0) //&& e.Row.IsStockItem == true)
			{
				if (e.Row.TranType != e.OldRow.TranType)
					SyncSplitTranType(e.Row);

				if (e.Row.TranType.IsIn(AMTranType.Receipt, AMTranType.Issue, AMTranType.Return, AMTranType.Adjustment))
					base.EventHandler(e);

				return;
			}

			e.Cache.SetValue<AMVendorShipLine.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<AMVendorShipLine.expireDate>(e.Row, null);
		}

		protected override void EventHandler(ManualEvent.Row<AMVendorShipLine>.Deleted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<AMVendorShipLine>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				var doc = PXParentAttribute.SelectParent<AMVendorShipment>(e.Cache, e.Row) ?? Base.Document.Current;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<AMVendorShipLine.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(AMVendorShipLine.qty), e.Row.Qty, IN.Messages.BinLotSerialNotAssigned);
			}

			base.EventHandler(e);
		}
		#endregion
		#region AMVendorShipLineSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent == null || e.Row == null || e.Row.LineNbr != LineCurrent.LineNbr)
				return;

			//#if DEBUG
			//            AMDebug.TraceWriteMethodName($"TranType = {e.Row.TranType} [{LineCurrent.TranType}]; InvtMult = {e.Row.InvtMult} [{LineCurrent.InvtMult}]; [{LineCurrent.DebuggerDisplay}]");
			//#endif
			//Not sure why we would ever want ot use InvtMultScope since it is changing the InvtMult value incorrectly on us when qty < 0
			using (new InvtMultScope(LineCurrent))
			{
				e.NewValue = LineCurrent.InvtMult;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMVendorShipLineSplit, AMVendorShipLineSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			if (e.Row?.InventoryID == null)
				return;

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			object invtMult = e.Row.InvtMult;
			if (invtMult == null)
				e.Cache.RaiseFieldDefaulting<AMVendorShipLineSplit.invtMult>(e.Row, out invtMult);

			object tranType = e.Row.TranType;
			if (tranType == null)
				e.Cache.RaiseFieldDefaulting<AMVendorShipLineSplit.tranType>(e.Row, out tranType);

			//don't default in a lot/serial number for WIP transactions
			if ((string)tranType == INTranType.Receipt)
				return;

			INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
			if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
			{
				foreach (AMVendorShipLineSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMVendorShipLineSplit>(e.Cache, item, mode, 1m))
				{
					e.NewValue = lssplit.LotSerialNbr;
					e.Cancel = true;
				}
			}
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMVendorShipLineSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);
			if (e.Row?.InventoryID != null)
			{
				(var _, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
				if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && lsClass.LotSerAssign == INLotSerAssign.WhenReceived)
					if (e.NewValue.IsNotIn(null, 0m, 1m))
						e.NewValue = 1m;
			}
		}
		#endregion
		#endregion

		protected virtual void SyncSplitTranType(AMVendorShipLine line)
		{
			LineCache.SetDefaultExt<AMVendorShipLine.invtMult>(line);
			foreach (AMVendorShipLineSplit split in PXParentAttribute
				.SelectSiblings(SplitCache, (AMVendorShipLineSplit)line, typeof(AMVendorShipLine))
				.Cast<AMVendorShipLineSplit>()
				.Where(s => s.TranType != line.TranType))
			{
				var copy = PXCache<AMVendorShipLineSplit>.CreateCopy(split);
				split.TranType = line.TranType;
				SplitCache.MarkUpdated(split);
				SplitCache.RaiseRowUpdated(split, copy);
			}
		}

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			return row.TranType == INTranType.Receipt
				? INLotSerTrack.Mode.Manual
				: INLotSerialNbrAttribute.TranTrackMode(lotSerClass, row.TranType, row.InvtMult);
		}
	}
}
