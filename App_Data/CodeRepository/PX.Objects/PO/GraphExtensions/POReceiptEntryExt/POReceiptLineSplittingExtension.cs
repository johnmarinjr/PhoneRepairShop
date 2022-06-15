using System;
using System.Collections;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class POReceiptLineSplittingExtension : LineSplittingExtension<POReceiptEntry, POReceipt, POReceiptLine, POReceiptLineSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(POReceiptLineSplit.FK.Receipt.SameAsCurrent);

		protected override Type LineQtyField => typeof(POReceiptLine.receiptQty);

		public override POReceiptLineSplit LineToSplit(POReceiptLine item)
		{
			using (new InvtMultScope(item))
			{
				POReceiptLineSplit ret = item;
				// baseQty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}
		#endregion
		#region Initialization
		public override void Initialize()
		{
			base.Initialize();

			ManualEvent.Row<POReceipt>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion
		#region Actions
		public override IEnumerable GenerateNumbers(PXAdapter adapter)
		{
			if (LineCurrent == null)
				return adapter.Get();

			if (!IsLSEntryEnabled(LineCurrent))
				return adapter.Get();

			return base.GenerateNumbers(adapter);
		}

		public override IEnumerable ShowSplits(PXAdapter adapter)
		{
			if (LineCurrent == null)
				return adapter.Get();

			if (!IsLSEntryEnabled(LineCurrent))
				throw new PXSetPropertyException(Messages.BinLotSerialEntryDisabled);

			return base.ShowSplits(adapter);
		}
		#endregion
		#region Event Handlers
		#region POReceipt
		protected virtual void EventHandler(ManualEvent.Row<POReceipt>.Updated.Args e)
		{
			if (e.Row.Hold != e.OldRow.Hold && e.Row.Hold == false)
			{
				foreach (POReceiptLine line in PXParentAttribute.SelectSiblings(LineCache, null, typeof(POReceipt)))
				{
					if (IsLSEntryEnabled(line) && Math.Abs(line.BaseQty.Value) >= 0.0000005m && (line.UnassignedQty >= 0.0000005m || line.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<POReceiptLine.receiptQty>(line, line.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
						LineCache.MarkUpdated(line);
					}
				}
			}
		}
		#endregion
		#region POReceiptLine
		protected override void SubscribeForLineEvents()
		{
			base.SubscribeForLineEvents();
			ManualEvent.FieldOf<POReceiptLine, POReceiptLine.receiptQty>.Updated.Subscribe<decimal?>(Base, EventHandler);
			ManualEvent.FieldOf<POReceiptLine, POReceiptLine.origOrderQty>.Selecting.Subscribe(Base, EventHandler);
			ManualEvent.FieldOf<POReceiptLine, POReceiptLine.openOrderQty>.Selecting.Subscribe(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<POReceiptLine, POReceiptLine.receiptQty>.Updated.Args<decimal?> e)
		{
			if (e.Row != null && e.Row.ReceiptQty != e.OldValue)
				e.Cache.RaiseFieldUpdated<POReceiptLine.baseReceiptQty>(e.Row, e.Row.BaseReceiptQty);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<POReceiptLine, POReceiptLine.origOrderQty>.Selecting.Args e)
		{
			if (e.Row?.PONbr != null)
			{
				POLineR origLine =
					SelectFrom<POLineR>.
					Where<
						POLineR.orderType.IsEqual<@P.AsString.ASCII>.
						And<POLineR.orderNbr.IsEqual<@P.AsString>>.
						And<POLineR.lineNbr.IsEqual<@P.AsInt>>>.
					View.Select(Base, e.Row.POType, e.Row.PONbr, e.Row.POLineNbr);

				if (origLine != null && e.Row.InventoryID == origLine.InventoryID)
				{
					if (string.Equals(e.Row.UOM, origLine.UOM) == false)
					{
						decimal baseOrderQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(e.Cache, e.Row, origLine.UOM, origLine.OrderQty.Value, INPrecision.QUANTITY);
						e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(e.Cache, e.Row, e.Row.UOM, baseOrderQty, INPrecision.QUANTITY);
					}
					else
					{
						e.ReturnValue = origLine.OrderQty;
					}
				}
			}

			if (e.Row?.OrigRefNbr != null)
			{
				INTran origLine =
					SelectFrom<INTran>.
					Where<
						INTran.tranType.IsEqual<INTranType.transfer>.
						And<INTran.refNbr.IsEqual<POReceiptLine.origRefNbr.FromCurrent>>.
						And<INTran.lineNbr.IsEqual<POReceiptLine.origLineNbr.FromCurrent>>.
						And<INTran.docType.IsEqual<POReceiptLine.origDocType.FromCurrent>>>.
					View.SelectSingleBound(Base, new object[] { e.Row });

				//is it needed at all? UOM conversion seems to be right thing to do. Also must it be origQty or origleftqty?
				if (origLine != null)
				{
					//if (string.Equals(row.UOM, origLine.UOM) == false)
					//{
					//    decimal baseOpenQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(e.Cache, e.Row, origLine.UOM, origLine.Qty.Value, INPrecision.QUANTITY);
					//    e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(e.Cache, e.Row, e.Row.UOM, baseOpenQty, INPrecision.QUANTITY);
					//}
					//else
					{
						e.ReturnValue = origLine.Qty;
					}
				}
			}

			var state = PXDecimalState.CreateInstance(
				e.ReturnState,
				precision: ((CommonSetup)Base.Caches<CommonSetup>().Current).DecPlQty,
				fieldName: nameof(POReceiptLine.OrigOrderQty),
				isKey: false,
				required: 0,
				minValue: decimal.MinValue,
				maxValue: decimal.MaxValue);
			state.DisplayName = PXMessages.LocalizeNoPrefix(SO.Messages.OrigOrderQty);
			state.Enabled = false;
			e.ReturnState = state;
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<POReceiptLine, POReceiptLine.openOrderQty>.Selecting.Args e)
		{
			if (e.Row?.PONbr != null)
			{
				POLineR origLine =
					SelectFrom<POLineR>.
					Where<
						POLineR.orderType.IsEqual<@P.AsString.ASCII>.
						And<POLineR.orderNbr.IsEqual<@P.AsString>>.
						And<POLineR.lineNbr.IsEqual<@P.AsInt>>>.
					View.Select(Base, e.Row.POType, e.Row.PONbr, e.Row.POLineNbr);

				if (origLine != null && e.Row.InventoryID == origLine.InventoryID)
				{
					decimal? openQty;
					if (string.Equals(e.Row.UOM, origLine.UOM) == false)
					{
						decimal baseOpenQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(e.Cache, e.Row, origLine.UOM, origLine.OrderQty.Value - origLine.ReceivedQty.Value, INPrecision.QUANTITY);
						openQty = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(e.Cache, e.Row, e.Row.UOM, baseOpenQty, INPrecision.QUANTITY);
					}
					else
					{
						openQty = origLine.OrderQty - origLine.ReceivedQty;
					}
					e.ReturnValue = (openQty < 0m) ? 0m : openQty;
				}
			}

			if (e.Row?.OrigRefNbr != null)
			{
				INTransitLineStatus origLineStat =
					SelectFrom<INTransitLineStatus>.
					Where<
						INTransitLineStatus.transferNbr.IsEqual<POReceiptLine.origRefNbr.FromCurrent>.
						And<INTransitLineStatus.transferLineNbr.IsEqual<POReceiptLine.origLineNbr.FromCurrent>>>.
					View.SelectSingleBound(Base, new object[] { e.Row });

				if (origLineStat != null)
				{
					decimal baseOpenQty = origLineStat.QtyOnHand.Value - ((e.Row.Released ?? false) ? 0 : e.Row.BaseReceiptQty.GetValueOrDefault());
					e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(e.Cache, e.Row, e.Row.UOM, baseOpenQty, INPrecision.QUANTITY);
				}
			}

			var state = PXDecimalState.CreateInstance(
				e.ReturnState,
				precision: ((CommonSetup)Base.Caches<CommonSetup>().Current).DecPlQty,
				fieldName: nameof(POReceiptLine.OpenOrderQty),
				isKey: false,
				required: 0,
				minValue: decimal.MinValue,
				maxValue: decimal.MaxValue);
			state.DisplayName = PXMessages.LocalizeNoPrefix(SO.Messages.OpenOrderQty);
			state.Enabled = false;
			e.ReturnState = state;
		}


		protected override void EventHandler(ManualEvent.Row<POReceiptLine>.Selected.Args e)
		{
			if (e.Row == null) return;

			bool lsEntryEnabled = IsLSEntryEnabled(e.Row) && e.Row.Released != true && !IsDropshipReturn();

			SplitCache.AllowInsert = lsEntryEnabled;
			SplitCache.AllowUpdate = lsEntryEnabled;
			SplitCache.AllowDelete = lsEntryEnabled;

			e.Cache.Adjust<POLotSerialNbrAttribute>(e.Row)
				.For<POReceiptLine.lotSerialNbr>(a => a.ForceDisable = !lsEntryEnabled);
		}

		protected override void EventHandler(ManualEvent.Row<POReceiptLine>.Inserted.Args e)
		{
			if (IsLSEntryEnabled(e.Row))
			{
				base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<POReceiptLine.locationID>(e.Row, null);
				e.Cache.SetValue<POReceiptLine.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<POReceiptLine.expireDate>(e.Row, null);
			}
		}

		protected override void EventHandler(ManualEvent.Row<POReceiptLine>.Updated.Args e)
		{
			if (IsLSEntryEnabled(e.Row) && (e.Row.LineType != POLineType.GoodsForProject || e.Row.ReceiptType != POReceiptType.POReturn))
			{
				using (ResolveNotDecimalUnitErrorRedirectorScope<POReceiptLineSplit.qty>(e.Row))
					base.EventHandler(e);
			}
			else
			{
				e.Cache.SetValue<POReceiptLine.locationID>(e.Row, null);
				e.Cache.SetValue<POReceiptLine.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<POReceiptLine.expireDate>(e.Row, null);

				if (e.Row != null && e.OldRow != null && e.Row.InventoryID != e.OldRow.InventoryID)
					base.RaiseRowDeleted(e.OldRow);
			}
		}

		protected override void EventHandler(ManualEvent.Row<POReceiptLine>.Deleted.Args e)
		{
			if (IsLSEntryEnabled(e.Row))
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<POReceiptLine>.Persisting.Args e)
		{
			if (IsLSEntryEnabled(e.Row) && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				POReceipt doc = PXParentAttribute.SelectParent<POReceipt>(e.Cache, e.Row) ?? Base.Document.Current;

				if (doc.Hold == false && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<POReceiptLine.receiptQty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(nameof(POReceiptLine.receiptQty), e.Row.Qty, Messages.BinLotSerialNotAssigned);
			}

			if (e.Operation.Command() != PXDBOperation.Delete)
				VerifyReceiptedQty(e.Row, e.Row.ReceiptQty, true);

			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<POReceiptLine>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);

			VerifyReceiptedQty(e.Row, e.NewValue, false);
		}
		#endregion
		#region POReceiptLineSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.invtMult>.Defaulting.Args<short?> e)
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

		public virtual void EventHandler(ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		public virtual void EventHandler(ManualEvent.FieldOf<POReceiptLineSplit, POReceiptLineSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				if (e.Row.InvtMult == null)
					e.Cache.RaiseFieldDefaulting<POReceiptLineSplit.invtMult>(e.Row, out _);

				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (POReceiptLineSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<POReceiptLineSplit>(e.Cache, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<POReceiptLineSplit>.Verifying.Args<decimal?> e)
		{
			if (IsTrackSerial(e.Row))
				base.EventHandlerQty(e);
			else
				e.NewValue = VerifySNQuantity(e.Cache, e.Row, e.NewValue, nameof(POReceiptLineSplit.qty));
		}

		public virtual void EventHandlerPOReceiptLineSplit(ManualEvent.Row<POReceiptLineSplit>.Persisting.Args e) // seems to be not used
		{
			if (e.Row != null && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row.BaseQty != 0m && e.Row.LocationID == null)
					ThrowFieldIsEmpty<POReceiptLineSplit.locationID>(e.Cache, e.Row);
		}
		#endregion
		#endregion

		public override POReceiptLine Clone(POReceiptLine item)
		{
			POReceiptLine copy = base.Clone(item);
			copy.POType = null;
			copy.PONbr = null;
			copy.POLineNbr = null;
			return copy;
		}

		public override bool IsTrackSerial(POReceiptLineSplit split)
		{
			if (split.LineType == POLineType.GoodsForDropShip)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);
				if (item == null)
					return false;

				return ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered;
			}
			else
			{
				return base.IsTrackSerial(split);
			}
		}

		protected override bool IsLotSerOptionsEnabled(LotSerOptions opt)
		{
			return base.IsLotSerOptionsEnabled(opt)
				&& Base.Document.Current?.Released != true
				&& !IsDropshipReturn();
		}

		public override void DefaultLotSerialNbr(POReceiptLineSplit row)
		{
			if (row.ReceiptType == POReceiptType.TransferReceipt)
				row.AssignedNbr = null;
			else
				base.DefaultLotSerialNbr(row);
		}

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			if (row is POReceiptLine line && line.LineType == POLineType.GoodsForDropShip
				&& lotSerClass != null && lotSerClass.LotSerTrack != null && lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered)
			{
				return INLotSerTrack.Mode.Create;
			}

			return base.GetTranTrackMode(row, lotSerClass);
		}


		protected virtual bool IsLSEntryEnabled(POReceiptLine line)
		{
			if (line != null && line.IsLSEntryBlocked == true)
				return false;

			if (line == null)
				return true;

			if (line.IsStockItem())
				return true;

			if (line.LineType.IsIn(POLineType.GoodsForDropShip, POLineType.GoodsForProject))
			{
				(var _, var lsClass) = ReadInventoryItem(line.InventoryID);

				if (lsClass.RequiredForDropship == true)
					return true;
			}

			return false;
		}

		protected virtual bool IsDropshipReturn() => !string.IsNullOrEmpty(Base.Document.Current?.SOOrderNbr);

		public virtual bool VerifyReceiptedQty(POReceiptLine row, decimal? value, bool persisting)
		{
			bool istransfer = row.ReceiptType == POReceiptType.TransferReceipt;
			if (istransfer && row.MaxTransferBaseQty.HasValue)
			{
				decimal? max = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(LineCache, row, row.MaxTransferBaseQty.Value, INPrecision.QUANTITY);
				if (value > max)
				{
					if (persisting)
						throw new PXRowPersistingException(nameof(POReceiptLine.receiptQty), row.ReceiptQty, CS.Messages.Entry_LE, new object[] { max });

					LineCache.RaiseExceptionHandling<POReceiptLineSplit.qty>(row, row.ReceiptQty, new PXSetPropertyException<INTran.qty>(CS.Messages.Entry_LE, PXErrorLevel.Error, max));
					return false;
				}
			}
			return true;
		}


		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, POReceiptLine Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (Row.LocationID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			else
				cmd.WhereAnd<Where<INLocation.receiptsValid.IsEqual<True>>>();

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
