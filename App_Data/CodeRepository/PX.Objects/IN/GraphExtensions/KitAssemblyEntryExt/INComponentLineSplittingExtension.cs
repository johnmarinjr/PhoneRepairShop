using System;

using PX.Common;
using PX.Data;

using PX.Objects.Common;

namespace PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class INComponentLineSplittingExtension : LineSplittingExtension<KitAssemblyEntry, INKitRegister, INComponentTran, INComponentTranSplit>
	{
		#region State
		protected bool Initialized;
		#endregion

		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(INComponentTranSplit.FK.KitRegister.SameAsCurrent);

		public override INComponentTranSplit LineToSplit(INComponentTran line)
		{
			using (new InvtMultScope(line))
			{
				INComponentTranSplit ret = line;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		protected override void SetEditMode()
		{
			if (!Initialized)
			{
				PXUIFieldAttribute.SetEnabled(LineCache, null, true);
				PXUIFieldAttribute.SetEnabled(SplitCache, null, true);
				PXUIFieldAttribute.SetEnabled<INComponentTranSplit.uOM>(SplitCache, null, false);
				Initialized = true;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<INKitRegister>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region INKitRegister
		protected virtual void EventHandler(ManualEvent.Row<INKitRegister>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (INComponentTran line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(INKitRegister)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<INComponentTran.qty>(line, line.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region INComponentTran
		public override void EventHandlerQty(ManualEvent.FieldOf<INComponentTran>.Verifying.Args<decimal?> e) // former INTran_Qty_FieldVerifying added separately to INComponentTran.qty
		{
			base.EventHandlerQty(e);
			if (e.NewValue < 0m)
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
		}

		protected override void EventHandler(ManualEvent.Row<INComponentTran>.Persisting.Args e) // former Master_RowPersisting
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INKitRegister doc = PXParentAttribute.SelectParent<INKitRegister>(e.Cache, e.Row) ?? Base.Document.Current;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<INComponentTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(typeof(INComponentTran.qty).Name, e.Row.Qty, Messages.BinLotSerialNotAssigned);
			}

			base.EventHandler(e);
		}
		#endregion
		#region INComponentTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
			ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.qty>.Verifying.Subscribe<decimal?>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.invtMult>.Defaulting.Args<short?> e)
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

		public virtual void EventHandler(ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				if (e.Row.InvtMult == null)
					e.Cache.RaiseFieldDefaulting<INComponentTranSplit.invtMult>(e.Row, out _);

				if (e.Row.TranType == null)
					e.Cache.RaiseFieldDefaulting<INComponentTranSplit.tranType>(e.Row, out _);

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, e.Row.TranType, e.Row.InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (INComponentTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INComponentTranSplit>(e.Cache, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<INComponentTranSplit, INComponentTranSplit.qty>.Verifying.Args<decimal?> e) // seems to be an override to EventHandlerQty
		{
			if (e.Row.InventoryID == null)
				return;

			(var _, var lsClass) = ReadInventoryItem(e.Row.InventoryID);

			if (INLotSerialNbrAttribute.IsTrackSerial(lsClass, e.Row.TranType, e.Row.InvtMult))
				if (e.NewValue.IsNotIn(null, 0m, 1m))
					e.NewValue = 1m;
		}

		public virtual void EventHandlerINComponentTranSplit(ManualEvent.Row<INComponentTranSplit>.Persisting.Args e) // seems to be not used
		{
			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row.BaseQty != 0m && e.Row.LocationID == null)
					ThrowFieldIsEmpty<INComponentTranSplit.locationID>(e.Cache, e.Row);
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
