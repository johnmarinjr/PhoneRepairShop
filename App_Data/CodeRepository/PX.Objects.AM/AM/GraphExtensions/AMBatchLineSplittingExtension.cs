using System;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
	public abstract class AMBatchLineSplittingExtension<TBatchGraph> : IN.GraphExtensions.LineSplittingExtension<TBatchGraph, AMBatch, AMMTran, AMMTranSplit>
		where TBatchGraph : AMBatchEntryBase
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(AMMTranSplit.FK.Batch.SameAsCurrent);

		protected override Type LineQtyField => typeof(AMMTran.qty);

		public override AMMTranSplit LineToSplit(AMMTran line)
		{
			using (new InvtMultScope(line))
			{
				AMMTranSplit ret = line;
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<AMBatch>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region AMBatch
		protected virtual void EventHandler(ManualEvent.Row<AMBatch>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (AMMTran line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(AMBatch)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<AMMTran.qty>(line, line.Qty, new PXSetPropertyException(Messages.LSAMMTranLinesUnassigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region AMMTran
		protected override void EventHandler(ManualEvent.Row<AMMTran>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<AMMTran.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<AMMTran.expireDate>(e.Row, null);
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMMTran>.Updated.Args e)
		{
			if (e.Row?.InventoryID == null || e.Row.OperationID == null || string.IsNullOrWhiteSpace(e.Row.ProdOrdID))
				return;

			var amProdItem = (AMProdItem)PXSelectorAttribute.Select<AMMTran.prodOrdID>(e.Cache, e.Row);
			if (amProdItem == null)
				return;

			if (e.OldRow.InventoryID != null && e.Row.InventoryID == null || e.Row.InventoryID != e.OldRow.InventoryID)
				foreach (AMMTranSplit split in PXParentAttribute.SelectSiblings(SplitCache, (AMMTranSplit)e.Row, typeof(AMMTran)))
					SplitCache.Delete(split); //Change of item will need a change of splits

			if (e.Row.InvtMult != 0 && e.Row.IsStockItem == true)
			{
				if (e.Row.TranType != e.OldRow.TranType)
					SyncSplitTranType(e.Row);

				var lastOper = amProdItem.LastOperationID.GetValueOrDefault() == e.Row.OperationID;
				var validItemEntry = lastOper || e.Row.DocType == AMDocType.Material || e.Row.IsScrap == true;				

				if (validItemEntry && e.Row.TranType.IsIn(AMTranType.Receipt, AMTranType.Issue, AMTranType.Return, AMTranType.Adjustment))
					base.EventHandler(e);

				return;
			}

			e.Cache.SetValue<AMMTran.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<AMMTran.expireDate>(e.Row, null);
		}

		protected override void EventHandler(ManualEvent.Row<AMMTran>.Deleted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<AMMTran>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				var doc = PXParentAttribute.SelectParent<AMBatch>(e.Cache, e.Row) ?? Base.AMBatchDataMember.Current;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<AMMTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(AMMTran.qty), e.Row.Qty, IN.Messages.BinLotSerialNotAssigned);
			}

			base.EventHandler(e);
		}
		#endregion
		#region AMMTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.invtMult>.Defaulting.Args<short?> e)
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

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			if (e.Row?.InventoryID == null)
				return;

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (e.Row.InvtMult == null)
				e.Cache.RaiseFieldDefaulting<AMMTranSplit.invtMult>(e.Row, out _);

			if (e.Row.TranType == null)
				e.Cache.RaiseFieldDefaulting<AMMTranSplit.tranType>(e.Row, out _);

			INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
			if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
			{
				foreach (AMMTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMMTranSplit>(e.Cache, item, mode, 1m))
				{
					e.NewValue = lssplit.LotSerialNbr;
					e.Cancel = true;
				}
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMMTranSplit>.Deleted.Args e)
		{
			AMMTran parent = SelectLine(e.Row);
			if (parent == null || parent.TranTypeChanged != true)
				base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMMTranSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);

			if (e.Row?.InventoryID == null)
				return;

			(var _, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
			if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && lsClass.LotSerAssign == INLotSerAssign.WhenReceived)
				if (e.NewValue.IsNotIn(null, 0m, 1m))
					e.NewValue = 1m;
		}
		#endregion
		#endregion

		protected virtual void SyncSplitTranType(AMMTran line)
		{
			LineCache.SetDefaultExt<AMMTran.invtMult>(line);
			foreach (AMMTranSplit split in PXParentAttribute
				.SelectSiblings(SplitCache, (AMMTranSplit)line, typeof(AMMTran))
				.Cast<AMMTranSplit>()
				.Where(s => s.TranType != line.TranType))
			{
				var copy = PXCache<AMMTranSplit>.CreateCopy(split);
				split.TranType = line.TranType;
				SplitCache.MarkUpdated(split);
				//SplitCache.RaiseRowUpdated(split, copy);
			}
		}
	}
}
