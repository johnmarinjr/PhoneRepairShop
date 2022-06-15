using System;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.CS;
using PX.Objects.Common;

namespace PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class INKitLineSplittingExtension : LineSplittingExtension<KitAssemblyEntry, INKitRegister, INKitRegister, INKitTranSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(
			Where<INKitTranSplit.docType.IsEqual<INKitRegister.docType.FromCurrent>.
			And<INKitTranSplit.refNbr.IsEqual<INKitRegister.refNbr.FromCurrent>>>);

		public override INKitTranSplit LineToSplit(INKitRegister line)
		{
			using (new InvtMultScope(line))
			{
				INKitTranSplit ret = line;
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
			ManualEvent.Row<INKitRegister>.Updated.Subscribe(Base, EventHandlerHeader);
		}
		#endregion

		#region Views
		protected override void AddLotSerOptionsView()
		{
			base.AddLotSerOptionsView();

			Base.Views[TypePrefixed(nameof(LSSelect.LotSerOptions))].AllowSelect =
				PXAccess.FeatureInstalled<FeaturesSet.subItem>() ||
				PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>() ||
				PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>() ||
				PXAccess.FeatureInstalled<FeaturesSet.replenishment>() ||
				PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>();
		}
		#endregion

		#region Event Handlers
		#region INKitRegister as TPrimary
		protected virtual void EventHandlerHeader(ManualEvent.Row<INKitRegister>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
				if (e.Row.UnassignedQty != 0)
					e.Cache.RaiseExceptionHandling<INKitRegister.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
		}
		#endregion
		#region INKitRegister as TLine
		protected override void EventHandler(ManualEvent.Row<INKitRegister>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INKitRegister doc = e.Row;

				if (doc != null && doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (doc.UnassignedQty >= 0.0000005m || doc.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<INKitRegister.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(typeof(INKitRegister.qty).Name, e.Row.Qty, Messages.BinLotSerialNotAssigned);
			}
			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<INKitRegister>.Verifying.Args<decimal?> e) // former INTran_Qty_FieldVerifying added separately to INKitRegister.qty
		{
			base.EventHandlerQty(e);
			if (e.NewValue < 0m)
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, 0);
		}
		#endregion
		#region INKitTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
			ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.qty>.Verifying.Subscribe<decimal?>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.invtMult>.Defaulting.Args<short?> e)
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

		public virtual void EventHandler(ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				if (e.Row.InvtMult == null)
					e.Cache.RaiseFieldDefaulting<INKitTranSplit.invtMult>(e.Row, out _);

				if (e.Row.TranType == null)
					e.Cache.RaiseFieldDefaulting<INKitTranSplit.tranType>(e.Row, out _);

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, e.Row.TranType, e.Row.InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (INKitTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INKitTranSplit>(e.Cache, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INKitTranSplit, INKitTranSplit.qty>.Verifying.Args<decimal?> e) // seems to be an override to EventHandlerQty
		{
			if (e.Row.InventoryID == null)
				return;

			(var _, var lsClass) = ReadInventoryItem(e.Row.InventoryID);

			if (INLotSerialNbrAttribute.IsTrackSerial(lsClass, e.Row.TranType, e.Row.InvtMult))
				if (e.NewValue.IsNotIn(null, 0m, 1m))
					e.NewValue = 1m;
		}

		public virtual void EventHandlerINKitTranSplit(ManualEvent.Row<INKitTranSplit>.Persisting.Args e) // seems to be not used
		{
			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row.BaseQty != 0m && e.Row.LocationID == null)
					ThrowFieldIsEmpty<INKitTranSplit.locationID>(e.Cache, e.Row);
		}
		#endregion
		#endregion

		#region Select Helpers
		protected override INKitTranSplit[] SelectSplits(INKitRegister row)
		{
			return
				SelectFrom<INKitTranSplit>.
				Where<
					INKitTranSplit.docType.IsEqual<@P.AsString.ASCII>.
					And<INKitTranSplit.refNbr.IsEqual<@P.AsString>>.
					And<INKitTranSplit.lineNbr.IsEqual<@P.AsInt>>>.
				View.Select(Base, row.DocType, row.RefNbr, row.KitLineNbr)
				.AsEnumerable().RowCast<INKitTranSplit>().ToArray();
		}

		protected override INKitTranSplit[] SelectSplits(INKitTranSplit row)
		{
			INKitRegister kitRow = PXParentAttribute.SelectParent<INKitRegister>(SplitCache, row);
			return SelectSplits(kitRow);
		}
		#endregion
	}
}
