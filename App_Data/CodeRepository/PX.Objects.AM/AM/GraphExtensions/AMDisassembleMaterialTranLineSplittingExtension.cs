using System;
using PX.Common;
using PX.Data;

using PX.Objects.Common;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMDisassembleMaterialTranLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<DisassemblyEntry, AMDisassembleBatch, AMDisassembleTran, AMDisassembleTranSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(
			AMDisassembleTranSplit.docType.IsEqual<AMDisassembleBatch.docType.FromCurrent>.
			And<AMDisassembleTranSplit.batNbr.IsEqual<AMDisassembleBatch.batchNbr.FromCurrent>>);

		protected override Type LineQtyField => typeof(AMDisassembleTran.qty);

		public override AMDisassembleTranSplit LineToSplit(AMDisassembleTran line)
		{
			using (new InvtMultScope(line))
			{
				AMDisassembleTranSplit ret = line;
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Initialize
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<AMDisassembleBatch>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region AMDisassembleBatch
		protected virtual void EventHandler(ManualEvent.Row<AMDisassembleBatch>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (AMDisassembleTran line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(AMDisassembleBatch)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<AMDisassembleTran.qty>(line, line.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region AMDisassembleTran
		protected override void EventHandler(ManualEvent.Row<AMDisassembleTran>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				var doc = PXParentAttribute.SelectParent<AMDisassembleBatch>(e.Cache, e.Row) ?? Base.Document.Current;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<AMDisassembleTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(AMDisassembleTran.qty), e.Row.Qty, IN.Messages.BinLotSerialNotAssigned);
			}

			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMDisassembleTran>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);
			if (e.NewValue <= 0m)
				throw new PXSetPropertyException(CS.Messages.Entry_GT, PXErrorLevel.Error, 0);
		}
		#endregion
		#region AMDisassembleTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				using (new InvtMultScope(LineCurrent))
				{
					e.NewValue = LineCurrent.InvtMult ?? 1;
					e.Cancel = true;
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleTranSplit, AMDisassembleTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			if (e.Row?.InventoryID != null)
			{
				var item = ReadInventoryItem(e.Row.InventoryID);
				INLotSerClass lsClass = item;

				object invtMult = e.Row.InvtMult;
				if (invtMult == null)
					e.Cache.RaiseFieldDefaulting<AMDisassembleTranSplit.invtMult>(e.Row, out invtMult);

				object tranType = e.Row.TranType;
				if (tranType == null)
					e.Cache.RaiseFieldDefaulting<AMDisassembleTranSplit.tranType>(e.Row, out tranType);

				if ((short?)invtMult == 1 && lsClass.LotSerAssign == INLotSerAssign.WhenReceived || lsClass.LotSerAssign == INLotSerAssign.WhenUsed)
				{
					INLotSerTrack.Mode mode = INLotSerTrack.Mode.None;
					foreach (AMDisassembleTranSplit split in INLotSerialNbrAttribute.CreateNumbers<AMDisassembleTranSplit>(e.Cache, item, mode, 1M))
					{
						e.NewValue = split.LotSerialNbr;
						e.Cancel = true;
					}
				}
			}
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMDisassembleTranSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, e.Row.TranType, e.Row.InvtMult))
				if (e.NewValue.IsNotIn(null, 0m, 1m))
					e.NewValue = 1m;
		} 
		#endregion
		#endregion

		protected override DateTime? ExpireDateByLot(ILSMaster item, ILSMaster master)
		{
			if (master != null && master.InvtMult > 0)
			{
				item.ExpireDate = null;
				return base.ExpireDateByLot(item, null);
			}
			else return base.ExpireDateByLot(item, master);
		}
	}
}
