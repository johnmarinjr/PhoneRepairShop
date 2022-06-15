using System;
using System.Collections;

using PX.Common;
using PX.Data;

using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.PO
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public class LSPOReceiptLine : LSSelect<POReceiptLine, POReceiptLineSplit,
		Where<POReceiptLineSplit.receiptNbr, Equal<Current<POReceipt.receiptNbr>>>>
	{
		#region State
		protected virtual bool IsLSEntryEnabled(object row)
		{
			POReceiptLine line = row as POReceiptLine;

			if (line != null && line.IsLSEntryBlocked == true)
				return false;

			if (line == null) return true;

			if (line.IsStockItem())
				return true;

			if (line.LineType.IsIn(POLineType.GoodsForDropShip, POLineType.GoodsForProject))
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(MasterCache, line.InventoryID);

				if (((INLotSerClass)item).RequiredForDropship == true)
					return true;
			}

			return false;
		}

		protected virtual bool IsDropshipReturn(PXCache cache)
			=> !string.IsNullOrEmpty(((POReceipt)cache.Graph.Caches<POReceipt>().Current)?.SOOrderNbr);

		protected string _OrigOrderQtyField = "OrigOrderQty";
		protected string _OpenOrderQtyField = "OpenOrderQty";
		#endregion
		#region Ctor
		public LSPOReceiptLine(PXGraph graph)
			: base(graph)
		{
			MasterQtyField = typeof(POReceiptLine.receiptQty);
			graph.FieldDefaulting.AddHandler<POReceiptLineSplit.subItemID>(POReceiptLineSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<POReceiptLineSplit.locationID>(POReceiptLineSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<POReceiptLineSplit.invtMult>(POReceiptLineSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<POReceiptLineSplit.lotSerialNbr>(POReceiptLineSplit_LotSerialNbr_FieldDefaulting);
			graph.RowUpdated.AddHandler<POReceipt>(POReceipt_RowUpdated);
			graph.FieldUpdated.AddHandler<POReceiptLine.receiptQty>(POReceiptLine_ReceiptQty_FieldUpdated);
			graph.FieldSelecting.AddHandler(typeof(POReceiptLine), _OrigOrderQtyField, OrigOrderQty_FieldSelecting);
			graph.FieldSelecting.AddHandler(typeof(POReceiptLine), _OpenOrderQtyField, OpenOrderQty_FieldSelecting);
		}
		#endregion

		#region Implementation
		public override POReceiptLine CloneMaster(POReceiptLine item)
		{
			POReceiptLine copy = base.CloneMaster(item);
			copy.POType = null;
			copy.PONbr = null;
			copy.POLineNbr = null;

			return copy;
		}

		public override void Master_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			base.Master_Qty_FieldVerifying(sender, e);

			VerifyReceiptedQty(sender, (POReceiptLine)e.Row, e.NewValue, false);
		}

		public virtual bool VerifyReceiptedQty(PXCache sender, POReceiptLine row, object value, bool persisting)
		{
			bool istransfer = row.ReceiptType == POReceiptType.TransferReceipt;
			if (istransfer && row.MaxTransferBaseQty.HasValue)
			{
				decimal? max = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>
					(sender, row, row.MaxTransferBaseQty.Value, INPrecision.QUANTITY);
				if ((decimal?)value > max)
				{
					if (persisting)
						throw new PXRowPersistingException(typeof(POReceiptLine.receiptQty).Name, row.ReceiptQty, CS.Messages.Entry_LE, new object[] { max });

					sender.RaiseExceptionHandling<POReceiptLineSplit.qty>(row, row.ReceiptQty, new PXSetPropertyException<INTran.qty>(CS.Messages.Entry_LE, PXErrorLevel.Error, max));
					return false;
				}
			}
			return true;
		}

		protected virtual void POReceipt_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<POReceipt.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<POReceipt.hold>(e.Row) == false)
			{
				PXCache cache = sender.Graph.Caches[typeof(POReceiptLine)];

				foreach (POReceiptLine item in PXParentAttribute.SelectSiblings(cache, null, typeof(POReceipt)))
				{
					if (IsLSEntryEnabled(item) && Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<POReceiptLine.receiptQty>(item, item.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						cache.MarkUpdated(item);
					}
				}
			}
		}
		protected virtual void POReceiptLine_ReceiptQty_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			POReceiptLine row = e.Row as POReceiptLine;
			if (row != null && row.ReceiptQty != (Decimal?)e.OldValue)
				sender.RaiseFieldUpdated<POReceiptLine.baseReceiptQty>(e.Row, row.BaseReceiptQty);
		}

		public override bool IsTrackSerial(PXCache sender, ILSDetail row)
		{
			if (((POReceiptLineSplit)row).LineType == POLineType.GoodsForDropShip)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, row.InventoryID);
				if (item == null)
					return false;

				return ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered;
			}
			else
			{
				return base.IsTrackSerial(sender, row);
			}
		}

		public override void Detail_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			base.Detail_RowPersisting(sender, e);
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (IsLSEntryEnabled(e.Row) && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				PXCache cache = sender.Graph.Caches[typeof(POReceipt)];
				object doc = PXParentAttribute.SelectParent(sender, e.Row, typeof(POReceipt)) ?? cache.Current;

				bool? OnHold = (bool?)cache.GetValue<POReceipt.hold>(doc);

				if (OnHold == false && Math.Abs((decimal)((POReceiptLine)e.Row).BaseQty) >= 0.0000005m && (((POReceiptLine)e.Row).UnassignedQty >= 0.0000005m || ((POReceiptLine)e.Row).UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<POReceiptLine.receiptQty>(e.Row, ((POReceiptLine)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(POReceiptLine.receiptQty).Name, ((POReceiptLine)e.Row).Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Delete)
				VerifyReceiptedQty(sender, (POReceiptLine)e.Row, ((POReceiptLine)e.Row).ReceiptQty, true);

			base.Master_RowPersisting(sender, e);
		}

		protected virtual void OrigOrderQty_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			POReceiptLine row = e.Row as POReceiptLine;

			if (row != null && row.PONbr != null)
			{
				POLineR orig_line = PXSelect<POLineR,
						Where<POLineR.orderType, Equal<Required<POLineR.orderType>>,
						And<POLineR.orderNbr, Equal<Required<POLineR.orderNbr>>,
						And<POLineR.lineNbr, Equal<Required<POLineR.lineNbr>>>>>>
						.Select(sender.Graph, row.POType, row.PONbr, row.POLineNbr);

				if (orig_line != null && row.InventoryID == orig_line.InventoryID)
				{
					if (string.Equals(((POReceiptLine)e.Row).UOM, orig_line.UOM) == false)
					{
						decimal BaseOrderQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(sender, e.Row, orig_line.UOM, (decimal)orig_line.OrderQty, INPrecision.QUANTITY);
						e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(sender, e.Row, ((POReceiptLine)e.Row).UOM, BaseOrderQty, INPrecision.QUANTITY);
					}
					else
					{
						e.ReturnValue = orig_line.OrderQty;
					}
				}
			}

			if (row != null && row.OrigRefNbr != null)
			{
				INTran orig_line = PXSelect<INTran, Where<INTran.tranType, Equal<INTranType.transfer>,
					And<INTran.refNbr, Equal<Current<POReceiptLine.origRefNbr>>,
					And<INTran.lineNbr, Equal<Current<POReceiptLine.origLineNbr>>,
					And<INTran.docType, Equal<Current<POReceiptLine.origDocType>>>>>>>.SelectSingleBound(_Graph, new object[] { (POReceiptLine)e.Row });

				//is it needed at all? UOM conversion seems to be right thing to do. Also must it be origQty or origleftqty?
				if (orig_line != null)
				{
					//if (string.Equals(row.UOM, orig_line.UOM) == false)
					//{
					//    decimal BaseOpenQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(sender, e.Row, orig_line.UOM, (decimal)orig_line.Qty, INPrecision.QUANTITY);
					//    e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(sender, e.Row, ((POReceiptLine)e.Row).UOM, BaseOpenQty, INPrecision.QUANTITY);
					//}
					//else
					{
						e.ReturnValue = orig_line.Qty;
					}
				}
			}

			e.ReturnState = PXDecimalState.CreateInstance(e.ReturnState, ((CommonSetup)_Graph.Caches[typeof(CommonSetup)].Current).DecPlQty, _OrigOrderQtyField, false, 0, decimal.MinValue, decimal.MaxValue);
			((PXFieldState)e.ReturnState).DisplayName = PXMessages.LocalizeNoPrefix(SO.Messages.OrigOrderQty);
			((PXFieldState)e.ReturnState).Enabled = false;
		}

		protected virtual void OpenOrderQty_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			POReceiptLine row = e.Row as POReceiptLine;

			if (row != null && row.PONbr != null)
			{
				POLineR orig_line = PXSelect<POLineR,
						Where<POLineR.orderType, Equal<Required<POLineR.orderType>>,
						And<POLineR.orderNbr, Equal<Required<POLineR.orderNbr>>,
						And<POLineR.lineNbr, Equal<Required<POLineR.lineNbr>>>>>>
						.Select(sender.Graph, row.POType, row.PONbr, row.POLineNbr);

				if (orig_line != null && row.InventoryID == orig_line.InventoryID)
				{
					decimal? openQty;
					if (string.Equals(((POReceiptLine)e.Row).UOM, orig_line.UOM) == false)
					{
						decimal BaseOpenQty = INUnitAttribute.ConvertToBase<POReceiptLine.inventoryID>(sender, e.Row, orig_line.UOM, (decimal)orig_line.OrderQty - (decimal)orig_line.ReceivedQty, INPrecision.QUANTITY);
						openQty = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(sender, e.Row, ((POReceiptLine)e.Row).UOM, BaseOpenQty, INPrecision.QUANTITY);
					}
					else
					{
						openQty = orig_line.OrderQty - orig_line.ReceivedQty;
					}
					e.ReturnValue = (openQty < 0m) ? 0m : openQty;
				}
			}

			if (row != null && row.OrigRefNbr != null)
			{
				INTransitLineStatus origlinestat =
					PXSelect<INTransitLineStatus,
					Where<INTransitLineStatus.transferNbr, Equal<Current<POReceiptLine.origRefNbr>>,
						And<INTransitLineStatus.transferLineNbr, Equal<Current<POReceiptLine.origLineNbr>>>>>
					.SelectSingleBound(_Graph, new object[] { (POReceiptLine)e.Row });

				if (origlinestat != null)
				{
					decimal BaseOpenQty = origlinestat.QtyOnHand.Value - ((row.Released ?? false) ? 0 : row.BaseReceiptQty.GetValueOrDefault());
					e.ReturnValue = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID>(sender, e.Row, ((POReceiptLine)e.Row).UOM, BaseOpenQty, INPrecision.QUANTITY);
				}
			}

			e.ReturnState = PXDecimalState.CreateInstance(e.ReturnState, ((CommonSetup)_Graph.Caches[typeof(CommonSetup)].Current).DecPlQty, _OpenOrderQtyField, false, 0, decimal.MinValue, decimal.MaxValue);
			((PXFieldState)e.ReturnState).DisplayName = PXMessages.LocalizeNoPrefix(SO.Messages.OpenOrderQty);
			((PXFieldState)e.ReturnState).Enabled = false;
		}

		public override IEnumerable GenerateLotSerial(PXAdapter adapter)
		{
			if (MasterCache.Current != null && IsLSEntryEnabled((POReceiptLine)MasterCache.Current))
				return base.GenerateLotSerial(adapter);
			return adapter.Get();
		}

		public override IEnumerable BinLotSerial(PXAdapter adapter)
		{
			if (MasterCache.Current != null)
			{
				if (!IsLSEntryEnabled((POReceiptLine)MasterCache.Current))
				{
					throw new PXSetPropertyException(Messages.BinLotSerialEntryDisabled);
				}
				View.AskExt(true);
			}
			return adapter.Get();
		}

		protected override void Master_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null) return;

			bool lsEntryEnabled =
				IsLSEntryEnabled((POReceiptLine)e.Row)
				&& ((POReceiptLine)e.Row).Released != true
				&& !IsDropshipReturn(sender);
			var splitCache = sender.Graph.Caches[typeof(POReceiptLineSplit)];
			splitCache.AllowInsert = lsEntryEnabled;
			splitCache.AllowUpdate = lsEntryEnabled;
			splitCache.AllowDelete = lsEntryEnabled;

			sender.Adjust<POLotSerialNbrAttribute>(e.Row).For<POReceiptLine.lotSerialNbr>(a => a.ForceDisable = !lsEntryEnabled);
		}

		protected override bool IsLotSerOptionsEnabled(PXCache sender, LotSerOptions opt)
		{
			return base.IsLotSerOptionsEnabled(sender, opt)
				&& ((POReceipt)sender.Graph.Caches<POReceipt>().Current)?.Released != true
				&& !IsDropshipReturn(sender);
		}

		protected override void Master_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if (IsLSEntryEnabled(e.Row))
			{
				base.Master_RowInserted(sender, e);
			}
			else
			{
				sender.SetValue<POReceiptLine.locationID>(e.Row, null);
				sender.SetValue<POReceiptLine.lotSerialNbr>(e.Row, null);
				sender.SetValue<POReceiptLine.expireDate>(e.Row, null);
			}
		}

		protected override void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (IsLSEntryEnabled(e.Row) && (((POReceiptLine)e.Row).LineType != POLineType.GoodsForProject || ((POReceiptLine)e.Row).ReceiptType != POReceiptType.POReturn))
			{
				using (ResolveNotDecimalUnitErrorRedirectorScope<POReceiptLineSplit.qty>(e.Row))
					base.Master_RowUpdated(sender, e);
			}
			else
			{
				sender.SetValue<POReceiptLine.locationID>(e.Row, null);
				sender.SetValue<POReceiptLine.lotSerialNbr>(e.Row, null);
				sender.SetValue<POReceiptLine.expireDate>(e.Row, null);

				POReceiptLine row = (POReceiptLine)e.Row;
				POReceiptLine oldRow = (POReceiptLine)e.OldRow;

				if (row != null && oldRow != null && row.InventoryID != oldRow.InventoryID)
				{
					RaiseRowDeleted(sender, oldRow);
				}
			}
		}

		protected override void Master_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if (IsLSEntryEnabled(e.Row))
			{
				base.Master_RowDeleted(sender, e);
			}
		}

		public override void Detail_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (IsTrackSerial(sender, (ILSDetail)e.Row))
			{
				base.Detail_Qty_FieldVerifying(sender, e);
			}
			else
			{
				VerifySNQuantity(sender, e, (ILSDetail)e.Row, typeof(POReceiptLineSplit.qty).Name);
			}
		}

		public override POReceiptLineSplit Convert(POReceiptLine item)
		{
			using (InvtMultScope<POReceiptLine> ms = new InvtMultScope<POReceiptLine>(item))
			{
				POReceiptLineSplit ret = item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}

		public void ThrowFieldIsEmpty<Field>(PXCache sender, object data)
			where Field : IBqlField
		{
			if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
			{
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
			}
		}


		public virtual void POReceiptLineSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				if (((POReceiptLineSplit)e.Row).BaseQty != 0m && ((POReceiptLineSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<POReceiptLineSplit.locationID>(sender, e.Row);
				}
			}
		}

		public virtual void POReceiptLineSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(POReceiptLine)];
			if (cache.Current != null && (e.Row == null || ((POReceiptLine)cache.Current).LineNbr == ((POReceiptLineSplit)e.Row).LineNbr))
			{
				e.NewValue = ((POReceiptLine)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void POReceiptLineSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(POReceiptLine)];
			if (cache.Current != null && (e.Row == null || ((POReceiptLine)cache.Current).LineNbr == ((POReceiptLineSplit)e.Row).LineNbr))
			{
				e.NewValue = ((POReceiptLine)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void POReceiptLineSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(POReceiptLine)];
			if (cache.Current != null && (e.Row == null || ((POReceiptLine)cache.Current).LineNbr == ((POReceiptLineSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<POReceiptLine> ms = new InvtMultScope<POReceiptLine>((POReceiptLine)cache.Current))
				{
					e.NewValue = ((POReceiptLine)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void POReceiptLineSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((POReceiptLineSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((POReceiptLineSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<POReceiptLineSplit.invtMult>(e.Row, out InvtMult);
				}

				INLotSerTrack.Mode mode = GetTranTrackMode((ILSMaster)e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (POReceiptLineSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<POReceiptLineSplit>(sender, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			if (row is POReceiptLine)
			{
				sender.RaiseExceptionHandling<POReceiptLine.receiptQty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetValueExt<POReceiptLine.inventoryID>(row), sender.GetValueExt<POReceiptLine.subItemID>(row), sender.GetValueExt<POReceiptLine.siteID>(row), sender.GetValueExt<POReceiptLine.locationID>(row), sender.GetValue<POReceiptLine.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<POReceiptLineSplit.qty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetValueExt<POReceiptLineSplit.inventoryID>(row), sender.GetValueExt<POReceiptLineSplit.subItemID>(row), sender.GetValueExt<POReceiptLineSplit.siteID>(row), sender.GetValueExt<POReceiptLineSplit.locationID>(row), sender.GetValue<POReceiptLineSplit.lotSerialNbr>(row)));
			}
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			POReceiptLine tran = (POReceiptLine)e.Row;
			POReceipt receipt = (POReceipt)PXParentAttribute.SelectParent(sender, e.Row, typeof(POReceipt));

			AvailabilityFetchMode fetchMode = (receipt?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent) | AvailabilityFetchMode.TryOptimize;
			IStatus availability = GetAvailability(sender, tran, fetchMode);
			if (availability != null)
			{
				if (!PXAccess.FeatureInstalled<FeaturesSet.materialManagement>())
				{
					e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability);
					AvailabilityCheck(sender, tran, availability);
				}
				else
				{
					IStatus availabilityProject = GetAvailability(sender, tran, fetchMode | AvailabilityFetchMode.Project);
					if (availabilityProject != null)
					{
						e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability, availabilityProject);
						AvailabilityCheck(sender, tran, availabilityProject);
					}
				}
			}

			base.Availability_FieldSelecting(sender, e);
		}

		private IStatus GetAvailability(PXCache sender, POReceiptLine tran, AvailabilityFetchMode fetchMode)
		{
			IStatus availability = AvailabilityFetch(sender, tran, fetchMode);

			if (availability != null)
			{
				availability.QtyOnHand = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(sender, tran, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
				availability.QtyAvail = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(sender, tran, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
				availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(sender, tran, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
				availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(sender, tran, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
				availability.QtyActual = INUnitAttribute.ConvertFromBase<POReceiptLine.inventoryID, POReceiptLine.uOM>(sender, tran, (decimal)availability.QtyActual, INPrecision.QUANTITY);
			}

			return availability;
		}

		private string BuildAvailabilityStatusLine(PXCache sender, POReceiptLine tran, IStatus availability)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(Messages.Availability_Info),
					sender.GetValue<POReceiptLine.uOM>(tran),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail),
					FormatQty(availability.QtyActual));
		}

		private string BuildAvailabilityStatusLine(PXCache sender, POReceiptLine tran, IStatus availability, IStatus availabilityProject)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(IN.Messages.Availability_ActualInfo_Project),
					sender.GetValue<POReceiptLine.uOM>(tran),
					FormatQty(availabilityProject.QtyOnHand),
					FormatQty(availabilityProject.QtyAvail),
					FormatQty(availabilityProject.QtyHardAvail),
					FormatQty(availabilityProject.QtyActual),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail),
					FormatQty(availability.QtyActual));
		}

		protected int _detailsRequested = 0;

		public override IStatus AvailabilityFetch(PXCache sender, ILSMaster row, AvailabilityFetchMode fetchMode)
		{
			if (row == null)
				return null;
			if (fetchMode.HasFlag(AvailabilityFetchMode.TryOptimize) && _detailsRequested++ == 5)
			{
				//package loading and caching
				var select = new PXSelectReadonly2<POReceiptLine,
					InnerJoin<INSiteStatus,
						On<POReceiptLine.FK.SiteStatus>,
					LeftJoin<INLocationStatus,
						On<POReceiptLine.FK.LocationStatus>,
					LeftJoin<INLotSerialStatus,
						On<POReceiptLine.FK.LotSerialStatus>>>>,
					Where<POReceiptLine.receiptType, Equal<Current<POReceipt.receiptType>>,
					And<POReceiptLine.receiptNbr, Equal<Current<POReceipt.receiptNbr>>>>>(sender.Graph);
				using (new PXFieldScope(select.View, typeof(INSiteStatus), typeof(INLocationStatus), typeof(INLotSerialStatus)))
				{
					foreach (PXResult<POReceiptLine, INSiteStatus, INLocationStatus, INLotSerialStatus> res in select.Select())
					{
						INSiteStatus siteStatus = res;
						INLocationStatus locationStatus = res;
						INLotSerialStatus lotSerialStatus = res;

						INSiteStatus.PK.StoreCached(sender.Graph, siteStatus);
						if (locationStatus.LocationID != null)
							INLocationStatus.PK.StoreCached(sender.Graph, locationStatus);
						if (lotSerialStatus?.LotSerialNbr != null)
							IN.INLotSerialStatus.PK.StoreCached(sender.Graph, lotSerialStatus);
					}
				}
			}
			return base.AvailabilityFetch(sender, row, fetchMode);
		}

		public override void DefaultLotSerialNbr(PXCache sender, POReceiptLineSplit row)
		{
			if (row.ReceiptType == POReceiptType.TransferReceipt)
				row.AssignedNbr = null;
			else
				base.DefaultLotSerialNbr(sender, row);
		}
		#endregion

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, POReceiptLine Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID, Equal<Current<INLotSerialStatus.subItemID>>>>();
			}
			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID, Equal<Current<INLotSerialStatus.locationID>>>>();
			}
			else
			{
				cmd.WhereAnd<Where<INLocation.receiptsValid, Equal<boolTrue>>>();
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr, Equal<Current<INLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			POReceiptLine line = row as POReceiptLine;
			if (line != null && line.LineType == POLineType.GoodsForDropShip
				&& lotSerClass != null && lotSerClass.LotSerTrack != null && lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered)
			{
				return INLotSerTrack.Mode.Create;
			}
			else
			{
				return base.GetTranTrackMode(row, lotSerClass);
			}
		}
	}
}
