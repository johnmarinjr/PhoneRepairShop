using System;

using PX.Common;
using PX.Data;

using PX.Objects.Common;

namespace PX.Objects.IN.GraphExtensions
{
	public abstract class INRegisterLineSplittingExtension<TRegisterGraph> : LineSplittingExtension<TRegisterGraph, INRegister, INTran, INTranSplit>
		where TRegisterGraph : INRegisterEntryBase
	{
		#region State
		protected bool PrevIsCorrectionMode;
		protected bool PrevIsFullMode;

		protected bool Initialized;
		#endregion

		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(INTranSplit.FK.Register.SameAsCurrent);

		protected override Type LineQtyField => typeof(INTran.qty);

		public override INTranSplit LineToSplit(INTran line)
		{
			using (new InvtMultScope(line))
			{
				INTranSplit ret = line;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}

		protected override void SetEditMode()
		{
			if (!Initialized || PrevIsCorrectionMode != IsCorrectionMode || PrevIsFullMode != IsFullMode)
			{
				PXUIFieldAttribute.SetEnabled(LineCache, null, false);
				PXUIFieldAttribute.SetEnabled(SplitCache, null, false);

				if (PrevIsCorrectionMode = IsCorrectionMode)
				{
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.LocationID), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.LotSerialNbr), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.ExpireDate), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.ReasonCode), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.ProjectID), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.TaskID), true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.CostCodeID), true);

					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.SubItemID), true);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.Qty), true);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.LocationID), true);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.LotSerialNbr), true);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.ExpireDate), true);
				}

				if (PrevIsFullMode = IsFullMode)
				{
					PXUIFieldAttribute.SetEnabled(LineCache, null, true);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.POReceiptNbr), false);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.POReceiptType), false);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.SOOrderNbr), false);
					PXUIFieldAttribute.SetEnabled(LineCache, null, nameof(INTran.SOShipmentNbr), false);

					PXUIFieldAttribute.SetEnabled(SplitCache, null, true);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.UOM), false);
					PXUIFieldAttribute.SetEnabled(SplitCache, null, nameof(INTranSplit.UnitCost), false);
				}

				Initialized = true;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<INRegister>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region INRegister
		protected virtual void EventHandler(ManualEvent.Row<INRegister>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (INTran line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(INRegister)))
				{
					if (Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<INTran.qty>(line, line.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		} 
		#endregion
		#region INTran
		protected override void EventHandler(ManualEvent.Row<INTran>.Selected.Args e)
		{
			if (e.Row != null)
			{
				InventoryItem ii = InventoryItem.PK.Find(LineCache.Graph, e.Row.InventoryID);
				PXUIFieldAttribute.SetReadOnly<INTranSplit.inventoryID>(SplitCache, null, ii == null || !(ii.StkItem == false && (ii.KitItem ?? false)));
			}
		}

		protected override void EventHandler(ManualEvent.Row<INTran>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				base.EventHandler(e);
			}
			else
			{
				//this piece of code supposed to support dropships and landed costs for dropships. ReceiptCostAdjustment is generated for landedcosts and ppv adjustments, so we need actual lotSerialNbr, thats why it has to stay
				if (e.Row.TranType != INTranType.ReceiptCostAdjustment)
				{
					e.Cache.SetValue<INTran.lotSerialNbr>(e.Row, null);
					e.Cache.SetValue<INTran.expireDate>(e.Row, null);
				}
			}
		}

		protected override void EventHandler(ManualEvent.Row<INTran>.Updated.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				if (Equals(e.Row.TranType, e.OldRow.TranType) == false)
				{
					e.Cache.SetDefaultExt<INTran.invtMult>(e.Row);

					foreach (INTranSplit split in PXParentAttribute.SelectSiblings(SplitCache, (INTranSplit)e.Row, typeof(INTran)))
					{
						INTranSplit copy = PXCache<INTranSplit>.CreateCopy(split);

						split.TranType = e.Row.TranType;

						SplitCache.MarkUpdated(split);
						SplitCache.RaiseRowUpdated(split, copy);
					}
				}

				base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<INTran.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<INTran.expireDate>(e.Row, null);
			}
		}

		protected override void EventHandler(ManualEvent.Row<INTran>.Deleted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<INTran>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INRegister doc = PXParentAttribute.SelectParent<INRegister>(e.Cache, e.Row) ?? DocumentCurrent;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(typeof(INTran.qty).Name, e.Row.Qty, Messages.BinLotSerialNotAssigned);
			}

			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<INTran>.Verifying.Args<decimal?> e)
		{
			if (e.NewValue < 0m)
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, 0);
		}
		#endregion
		#region INTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<INTranSplit, INTranSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<INTranSplit, INTranSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INTranSplit, INTranSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<INTranSplit, INTranSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		public override void EventHandler(ManualEvent.Row<INTranSplit>.Persisting.Args e)
		{
			base.EventHandler(e);

			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row.BaseQty != 0m && e.Row.LocationID == null)
					ThrowFieldIsEmpty<INTranSplit.locationID>(e.Cache, e.Row);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<INTranSplit, INTranSplit.invtMult>.Defaulting.Args<short?> e)
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

		protected virtual void EventHandler(ManualEvent.FieldOf<INTranSplit, INTranSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<INTranSplit, INTranSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<INTranSplit, INTranSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				if (e.Row.InvtMult == null)
					e.Cache.RaiseFieldDefaulting<INTranSplit.invtMult>(e.Row, out _);

				if (e.Row.TranType == null)
					e.Cache.RaiseFieldDefaulting<INTranSplit.tranType>(e.Row, out _);

				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (INTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INTranSplit>(e.Cache, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}
		#endregion
		#endregion

		public override void DefaultLotSerialNbr(INTranSplit split)
		{
			if (split.DocType == INDocType.Receipt && split.TranType == INTranType.Transfer
				|| !string.IsNullOrEmpty(split.OrigModule) && split.OrigModule != GL.BatchModule.IN)
				split.AssignedNbr = null;
			else
				base.DefaultLotSerialNbr(split);
		}

		protected override void SetLineQtyFromBase(INTran line)
		{
			if (line.UOM == line.OrigUOM && line.BaseQty == line.BaseOrigFullQty)
			{
				line.Qty = line.OrigFullQty;
				return;
			}

			base.SetLineQtyFromBase(line);
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, INTran Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			}
			else
			{
				switch (Row.TranType)
				{
					case INTranType.Issue:
						cmd.WhereAnd<Where<INLocation.receiptsValid.IsEqual<True>>>();
						break;
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
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}
	}
}
