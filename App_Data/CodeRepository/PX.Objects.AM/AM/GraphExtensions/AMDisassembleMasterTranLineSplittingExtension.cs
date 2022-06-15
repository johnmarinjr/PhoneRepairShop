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
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMDisassembleMasterTranLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<DisassemblyEntry, AMDisassembleBatch, AMDisassembleBatch, AMDisassembleBatchSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(
			AMDisassembleBatchSplit.docType.IsEqual<AMDisassembleBatch.docType.FromCurrent>.
			And<AMDisassembleBatchSplit.batNbr.IsEqual<AMDisassembleBatch.batchNbr.FromCurrent>>);

		public override AMDisassembleBatchSplit LineToSplit(AMDisassembleBatch line)
		{
			using (new InvtMultScope(line))
			{
				AMDisassembleBatchSplit ret = line;
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Views
		protected override void AddLotSerOptionsView()
		{
			base.AddLotSerOptionsView();

			Base.Views[TypePrefixed(nameof(LSSelect.LotSerOptions))].AllowSelect =
				PXAccess.FeatureInstalled<CS.FeaturesSet.lotSerialTracking>() && (
					PXAccess.FeatureInstalled<CS.FeaturesSet.warehouseLocation>() ||
					PXAccess.FeatureInstalled<CS.FeaturesSet.subItem>() ||
					PXAccess.FeatureInstalled<CS.FeaturesSet.replenishment>());
		}
		#endregion

		#region Event Handlers
		#region AMDisassembleBatch
		protected override void EventHandler(ManualEvent.Row<AMDisassembleBatch>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				//base.EventHandler(e);
			}
			else
			{
				//this piece of code supposed to support dropships and landed costs for dropships.ReceiptCostAdjustment is generated for landedcosts and ppv adjustments, so we need actual lotSerialNbr, thats why it has to stay
				if (e.Row.TranType == AMTranType.Disassembly)
				{
					e.Cache.SetValue<AMMTran.lotSerialNbr>(e.Row, null);
					e.Cache.SetValue<AMMTran.expireDate>(e.Row, null);
				}
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMDisassembleBatch>.Updated.Args e)
		{
			if (e.Row?.InventoryID != null && e.Row.UOM != null && e.Row.InvtMult.GetValueOrDefault() != 0)
				base.EventHandler(e);

			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
				if (e.Row.UnassignedQty != 0)
					e.Cache.RaiseExceptionHandling<AMDisassembleBatch.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned));
		}

		protected override void EventHandler(ManualEvent.Row<AMDisassembleBatch>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row?.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<AMDisassembleBatch.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(IN.Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(AMDisassembleBatch.qty), e.Row.Qty, IN.Messages.BinLotSerialNotAssigned);

			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMDisassembleBatch>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);
			if (e.NewValue < 0m)
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
		}
		#endregion
		#region AMDisassembleBatchSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				using (new InvtMultScope(LineCurrent))
				{
					e.NewValue = LineCurrent.InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.LineNbr == LineCurrent.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<AMDisassembleBatchSplit, AMDisassembleBatchSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				object invtMult = e.Row.InvtMult;
				if (invtMult == null)
					e.Cache.RaiseFieldDefaulting<AMDisassembleBatchSplit.invtMult>(e.Row, out invtMult);

				object tranType = e.Row.TranType;
				if (tranType == null)
					e.Cache.RaiseFieldDefaulting<AMDisassembleBatchSplit.tranType>(e.Row, out tranType);

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, (string)tranType, (short?)invtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					foreach (AMDisassembleBatchSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMDisassembleBatchSplit>(e.Cache, item, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMDisassembleBatchSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);
			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, e.Row.TranType, e.Row.InvtMult))
				if (e.NewValue.IsNotIn(null, 0m, 1m))
					e.NewValue = 1m;
		}
		#endregion
		#endregion

		#region Select Helpers
		protected override AMDisassembleBatchSplit[] SelectSplits(AMDisassembleBatchSplit split)
		{
			AMDisassembleBatch line = PXParentAttribute.SelectParent<AMDisassembleBatch>(SplitCache, split);
			return SelectSplits(line);
		}

		protected override AMDisassembleBatchSplit[] SelectSplits(AMDisassembleBatch line)
		{
			return
				SelectFrom<AMDisassembleBatchSplit>.
				Where<
					AMDisassembleBatchSplit.docType.IsEqual<@P.AsString.ASCII>.
					And<AMDisassembleBatchSplit.batNbr.IsEqual<@P.AsString>>.
					And<AMDisassembleBatchSplit.lineNbr.IsEqual<@P.AsInt>>>.
				View.Select(Base, line.DocType, line.BatchNbr, line.RefLineNbr)
				.RowCast<AMDisassembleBatchSplit>()
				.ToArray();
		}
		#endregion

		public override void UpdateParent(AMDisassembleBatch line, AMDisassembleBatchSplit newSplit, AMDisassembleBatchSplit oldSplit, out decimal baseQty)
		{
			base.UpdateParent(line, newSplit, oldSplit, out baseQty);
			if (CurrentCounters.RecordCount > 0)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);
				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, line.TranType, line.InvtMult);
				if (mode == INLotSerTrack.Mode.None)
				{
					line.LotSerialNbr = string.Empty;
				}
				else if ((mode & INLotSerTrack.Mode.Create) > 0 || (mode & INLotSerTrack.Mode.Issue) > 0)
				{
					//if more than 1 split exist at lotserial creation time ignore equalness and display <SPLIT>
					line.LotSerialNbr = null;
				}
			}
		}
	}
}
