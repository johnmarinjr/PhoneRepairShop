using System;
using System.Collections.Generic;
using PX.Objects.IN;
using PX.Data;
using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;
using PX.Objects.AM.Attributes;
using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using System.Collections;

namespace PX.Objects.AM
{
    [Obsolete]
    public class LSAMClockItem : LSSelect<AMClockItem, AMClockItemSplit, Where<AMClockItemSplit.employeeID, Equal<Current<AMClockItem.employeeID>>,
        And<AMClockItemSplit.lineNbr, Equal<int0>>>>
    {
        public LSAMClockItem(PXGraph graph)
     : base(graph)
        {
            this.MasterQtyField = typeof(AMClockItem.qty);
            graph.FieldUpdated.AddHandler<AMClockItem.lastOper>(AMClockItem_LastOper_FieldUpdated);
            graph.FieldUpdated.AddHandler<AMClockItem.qty>(AMClockItem_Qty_FieldUpdated);
            graph.FieldUpdated.AddHandler<AMClockItem.operationID>(AMClockItem_OperationID_FieldUpdated);
            graph.RowSelected.AddHandler<AMClockItem>(AMClockItem_RowSelected);
            graph.FieldUpdated.AddHandler<AMClockItemSplit.invtMult>(AMClockItemSplit_InvtMult_FieldUpdated);
            graph.FieldDefaulting.AddHandler<AMClockItemSplit.invtMult>(AMClockItemSplit_InvtMult_FieldDefaulting);
            graph.FieldVerifying.AddHandler<AMClockItemSplit.qty>(AMClockItemSplit_Qty_FieldVerifying);
            graph.FieldDefaulting.AddHandler<AMClockItemSplit.subItemID>(AMClockItemSplit_SubItemID_FieldDefaulting);
            graph.FieldDefaulting.AddHandler<AMClockItemSplit.locationID>(AMClockItemSplit_LocationID_FieldDefaulting);
            graph.FieldDefaulting.AddHandler<AMClockItemSplit.lotSerialNbr>(AMClockItemSplit_LotSerialNbr_FieldDefaulting);
        }

        #region Handlers
        protected virtual void AMClockItem_LastOper_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            SetTranTypeInvtMult(sender, (AMClockItem)e.Row);
        }

        protected virtual void AMClockItem_Qty_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            SetTranTypeInvtMult(sender, (AMClockItem)e.Row);
        }

        protected virtual void AMClockItem_OperationID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var tran = (AMClockItem)e.Row;
            if (!string.IsNullOrWhiteSpace(tran?.ProdOrdID) && tran.OperationID != null)
            {
                SetTranTypeInvtMult(sender, tran);
            }
        }

        protected virtual void AMClockItem_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            var row = (AMClockItem)e.Row;
            if (row == null || string.IsNullOrWhiteSpace(row.ProdOrdID))
            {
                return;
            }

            AllowDetail(row.Qty > 0 && row.IsClockedIn == true && row.LastOper.GetValueOrDefault());
            if (row.Qty > 0 && row.IsClockedIn == true)
            {
                cache.RaiseFieldUpdated(_MasterQtyField, row, row.Qty);
            }
        }

        protected virtual void AMClockItemSplit_InvtMult_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            PXCache pxCache = sender.Graph.Caches[typeof(AMClockItem)];
            if (pxCache.Current == null)
            {
                return;
            }
            if (e.Row != null)
            {
                int? lineNbr1 = (int)0;
                int? lineNbr2 = ((AMClockItemSplit)e.Row).LineNbr;
                if ((lineNbr1.GetValueOrDefault() != lineNbr2.GetValueOrDefault() ? 0 : (lineNbr1.HasValue == lineNbr2.HasValue ? 1 : 0)) == 0)
                {
                    return;
                }
                ((AMClockItemSplit)e.Row).TranType = ((AMClockItemSplit)e.Row).InvtMult < 1 ? AMTranType.Adjustment : AMTranType.Receipt;
            }
        }

        public virtual void AMClockItemSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            var cache = sender.Graph.Caches[typeof(AMClockItem)];
            if (cache.Current == null || (AMClockItemSplit)e.Row == null ||
                ((int)0 != ((AMClockItemSplit)e.Row).LineNbr))
            {
                return;
            }

#if DEBUG
            AMDebug.TraceWriteMethodName($"TranType = {((AMClockItemSplit)e.Row).TranType} [{((AMClockItem)cache.Current).TranType}]; InvtMult = {((AMClockItemSplit)e.Row).InvtMult} [{((AMClockItem)cache.Current).InvtMult}]; [{((AMClockItem)cache.Current).DebuggerDisplay}]");
#endif
            //Not sure why we would ever want ot use InvtMultScope since it is changing the InvtMult value incorrectly on us when qty < 0
            using (InvtMultScope<AMClockItem> ms = new InvtMultScope<AMClockItem>((AMClockItem)cache.Current))
            {
                e.NewValue = ((AMClockItem)cache.Current).InvtMult;
                e.Cancel = true;
            }
        }

        public void AMClockItemSplit_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            PXResult<InventoryItem, INLotSerClass> result = base.ReadInventoryItem(sender, ((AMClockItemSplit)e.Row).InventoryID);
            if ((((result != null) && (((INLotSerClass)result).LotSerTrack == INLotSerTrack.SerialNumbered)) && (((INLotSerClass)result).LotSerAssign == INLotSerAssign.WhenReceived)) && (((e.NewValue != null) && (e.NewValue is decimal)) && ((((decimal)e.NewValue) != 0M) && (((decimal)e.NewValue) != 1M))))
            {
                e.NewValue = 1M;
            }
        }

        public void AMClockItemSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            PXCache cache = sender.Graph.Caches[typeof(AMClockItem)];
            if (cache.Current != null && (e.Row == null || (int)0 == ((AMClockItemSplit)e.Row).LineNbr))
            {
                e.NewValue = ((AMClockItem)cache.Current).SubItemID;
                e.Cancel = true;
            }
        }

        public virtual void AMClockItemSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            PXCache cache = sender.Graph.Caches[typeof(AMClockItem)];
            if (cache.Current != null && (e.Row == null || (int)0 == ((AMClockItemSplit)e.Row).LineNbr))
            {
                e.NewValue = ((AMClockItem)cache.Current).LocationID;
                e.Cancel = true;
            }
        }

        public virtual void AMClockItemSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            var row = (AMClockItemSplit)e.Row;
            if (row == null)
            {
                return;
            }

            PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, row.InventoryID);

            if (item == null)
            {
                return;
            }

            object InvtMult = row.InvtMult;
            if (InvtMult == null)
            {
                sender.RaiseFieldDefaulting<AMClockItemSplit.invtMult>(e.Row, out InvtMult);
            }

            object TranType = row.TranType;
            if (TranType == null)
            {
                sender.RaiseFieldDefaulting<AMClockItemSplit.tranType>(e.Row, out TranType);
            }

            INLotSerTrack.Mode mode = GetTranTrackMode((ILSMaster)e.Row, item);
            if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
            {
                foreach (AMClockItemSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMClockItemSplit>(sender, item, mode, 1m))
                {
                    e.NewValue = lssplit.LotSerialNbr;
                    e.Cancel = true;
                }
            }
        }
        #endregion

        public void AMClockItemSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
            {
                if (((AMClockItemSplit)e.Row).BaseQty != 0m && ((AMClockItemSplit)e.Row).LocationID == null)
                {
                    ThrowFieldIsEmpty<AMClockItemSplit.locationID>(sender, e.Row);
                }
            }
        }

        protected override void Master_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            if (((AMClockItem)e.Row).InvtMult != 0)
            {
                base.Master_RowDeleted(sender, e);
            }
        }

        protected override void Master_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
            if (((AMClockItem)e.Row).InvtMult != 0)
            {
                base.Master_RowInserted(sender, e);
            }
            else
            {
                sender.SetValue<AMClockItem.lotSerialNbr>(e.Row, null);
                sender.SetValue<AMClockItem.expireDate>(e.Row, null);
            }
        }

        protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
            {
                if (Math.Abs((decimal)((AMClockItem)e.Row).BaseQty) >= 0.0000005m && (((AMClockItem)e.Row).UnassignedQty >= 0.0000005m || ((AMClockItem)e.Row).UnassignedQty <= -0.0000005m))
                {
                    if (sender.RaiseExceptionHandling<AMClockItem.qty>(e.Row, ((AMClockItem)e.Row).Qty, new PXSetPropertyException(PX.Objects.IN.Messages.BinLotSerialNotAssigned)))
                    {
                        throw new PXRowPersistingException(typeof(AMClockItem.qty).Name, ((AMClockItem)e.Row).Qty, PX.Objects.IN.Messages.BinLotSerialNotAssigned);
                    }
                }
            }
            base.Master_RowPersisting(sender, e);
        }

        protected override void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            var row = (AMClockItem)e.Row;
            if (row?.InventoryID == null || row.OperationID == null || string.IsNullOrWhiteSpace(row.ProdOrdID))
            {
                return;
            }

            var amProdItem = (AMProdItem)PXSelectorAttribute.Select<AMClockItem.prodOrdID>(sender, e.Row);
            if (amProdItem == null)
            {
                return;
            }

            var cache = sender.Graph.Caches[typeof(AMClockItemSplit)];
            if (((AMClockItem)e.OldRow).InventoryID != null && row.InventoryID == null || row.InventoryID != ((AMClockItem)e.OldRow).InventoryID)
            {
                foreach (AMClockItemSplit split in PXParentAttribute.SelectSiblings(cache, (AMClockItemSplit)row, typeof(AMClockItem)))
                {
                    cache.Delete(split); //Change of item will need a change of splits
                }
            }

            if (row.InvtMult != 0)
            {
                if (!sender.ObjectsEqual<AMClockItem.tranType>(row, e.OldRow))
                {
                    SyncSplitTranType(sender, row, cache);
                }

                var lastOper = amProdItem.LastOperationID.GetValueOrDefault() == row.OperationID;
                var validItemEntry = lastOper;

                if (validItemEntry)
                {
                    base.Master_RowUpdated(sender, e);
                }

                return;
            }

            sender.SetValue<AMClockItem.lotSerialNbr>(e.Row, null);
            sender.SetValue<AMClockItem.expireDate>(e.Row, null);
        }

        protected override void Detail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
        {
            base.Detail_RowInserting(sender, e);
            var row = (AMClockItemSplit)e.Row;
            if (row == null)
            {
                return;
            }

            var rowParent = (AMClockItem)PXParentAttribute.SelectParent(sender, row, typeof(AMClockItem));
            if (rowParent == null)
            {
                return;
            }

            row.TranType = rowParent.TranType ?? row.TranType;
            row.InvtMult = AMTranType.InvtMult(row.TranType, rowParent.Qty);
        }

        public override void Detail_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
            {
                if (string.IsNullOrEmpty(((ILSDetail)e.Row).AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(((ILSDetail)e.Row).AssignedNbr, ((ILSDetail)e.Row).LotSerialNbr))
                {
                    string numVal = string.Empty;
                    PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((ILSDetail)e.Row).InventoryID);
                    ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
                    try
                    {
                        numVal = AutoNumberAttribute.NextNumber(lotSerNum.LotSerNumVal);
                    }
                    catch (AutoNumberException)
                    {
                        ThrowEmptyLotSerNumVal(sender, e.Row);
                    }

                    string _KeyToAbort = INLotSerialNbrAttribute.UpdateNumber(
                        ((ILSDetail)e.Row).AssignedNbr,
                        ((ILSDetail)e.Row).LotSerialNbr,
                        numVal);

                    ((ILSDetail)e.Row).LotSerialNbr = _KeyToAbort;

                    try
                    {
                        _persisted.Add(e.Row, _KeyToAbort);
                    }
                    catch (ArgumentException)
                    {
                        //the only reason can be overflow in serial numbering which will cause '0000' number to be treated like not-generated
                        ThrowEmptyLotSerNumVal(sender, e.Row);
                    }
                    UpdateLotSerNumVal(lotSerNum, numVal, item);
                    sender.RaiseRowUpdated(e.Row, PXCache<AMClockItemSplit>.CreateCopy((AMClockItemSplit)e.Row));
                }
            }
        }

        protected override void Detail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            PXCache lscache = sender.Graph.Caches[typeof(INLotSerialStatus)];
            ExpireCached(lscache, INLotSerialStatus((AMClockItemSplit)e.OldRow));

            if (_InternallCall)
            {
                return;
            }

            if (((AMClockItemSplit)e.Row).LotSerialNbr != ((AMClockItemSplit)e.OldRow).LotSerialNbr)
            {
                ((AMClockItemSplit)e.Row).ExpireDate = ExpireDateByLot(sender, ((AMClockItemSplit)e.Row), null);
            }

    ((AMClockItemSplit)e.Row).BaseQty = INUnitAttribute.ConvertToBase(sender, ((AMClockItemSplit)e.Row).InventoryID, ((AMClockItemSplit)e.Row).UOM, (decimal)((AMClockItemSplit)e.Row).Qty, ((AMClockItemSplit)e.Row).BaseQty, INPrecision.QUANTITY);

            try
            {
                _InternallCall = true;
                UpdateParent(sender, (AMClockItemSplit)e.Row, (AMClockItemSplit)e.OldRow);

                if (!UnattendedMode)
                {
                    AvailabilityCheck(sender, (AMClockItemSplit)e.Row);
                }
            }
            finally
            {
                _InternallCall = false;
            }
        }

        protected virtual void AllowDetail(bool allow)
        {
            DetailCache.AllowInsert = allow && MasterCache.AllowInsert;
            DetailCache.AllowUpdate = allow && MasterCache.AllowUpdate;
        }

        public virtual IStatus AvailabilityFetchTranUom(PXCache sender, AMClockItem Row, bool ExcludeCurrent)
        {
            if (!PXLongOperation.Exists(sender.Graph.UID))
            {
                IStatus availability = AvailabilityFetch(sender, Row, Row != null && Row.Released.GetValueOrDefault() ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent);

                if (availability != null)
                {
                    decimal unitRate = INUnitAttribute.ConvertFromBase<AMClockItem.inventoryID, AMClockItem.uOM>(sender, Row, 1m, INPrecision.NOROUND);
                    availability.QtyOnHand = PXDBQuantityAttribute.Round(availability.QtyOnHand.GetValueOrDefault() * unitRate);
                    availability.QtyAvail = PXDBQuantityAttribute.Round(availability.QtyAvail.GetValueOrDefault() * unitRate);
                    availability.QtyNotAvail = PXDBQuantityAttribute.Round(availability.QtyNotAvail.GetValueOrDefault() * unitRate);
                    availability.QtyHardAvail = PXDBQuantityAttribute.Round(availability.QtyHardAvail.GetValueOrDefault() * unitRate);

                    return availability;
                }
            }
            return null;
        }

        public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            var row = (AMClockItem)e.Row;
            e.ReturnValue = string.Empty;
            if (row == null
                || row.InventoryID.GetValueOrDefault() == 0
                || row.SiteID.GetValueOrDefault() == 0)
            {
                return;
            }

            if (!PXLongOperation.Exists(sender.Graph.UID))
            {
                IStatus availability = AvailabilityFetchTranUom(sender, (AMClockItem)e.Row, !(e.Row != null && (((AMClockItem)e.Row).Released == true)));

                if (availability != null)
                {
                    e.ReturnValue = PXMessages.LocalizeFormatNoPrefix(
                        Messages.LSTranStatus,
                        sender.GetValue<AMClockItem.uOM>(e.Row),
                        FormatQty(availability.QtyOnHand.GetValueOrDefault()),
                        FormatQty(availability.QtyAvail.GetValueOrDefault()),
                        FormatQty(availability.QtyHardAvail.GetValueOrDefault()));
                }
                else
                {
                    //handle missing UOM
                    INUnitAttribute.ConvertFromBase<AMClockItem.inventoryID, AMClockItem.uOM>(sender, e.Row, 0m, INPrecision.QUANTITY);
                }
            }

            base.Availability_FieldSelecting(sender, e);
        }

        public void ThrowFieldIsEmpty<Field>(PXCache sender, object data) where Field : IBqlField
        {
            if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
            {
                throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, new object[] { typeof(Field).Name });
            }
        }

        protected virtual void SyncSplitTranType(PXCache cache, AMClockItem tran, PXCache splitCache)
        {
            //cache.SetDefaultExt<AMClockItem.invtMult>(tran);
            foreach (AMClockItemSplit split in PXParentAttribute.SelectSiblings(splitCache, (AMClockItemSplit)tran, typeof(AMClockItem)))
            {
                var copy = PXCache<AMClockItemSplit>.CreateCopy(split);
                if (split.TranType == tran.TranType)
                {
                    continue;
                }
                split.TranType = tran.TranType;
                if (splitCache.GetStatus(split) == PXEntryStatus.Notchanged)
                {
                    splitCache.SetStatus(split, PXEntryStatus.Updated);
                }
                splitCache.RaiseRowUpdated(split, copy);
            }
        }

        protected virtual void SetTranTypeInvtMult(PXCache cache, AMClockItem tran)
        {
            if (tran == null)
            {
                return;
            }
#if DEBUG
            var tranTypeOld = tran.TranType;
            var invtMultOld = tran.InvtMult;
#endif
            var tranTypeNew = tran.Qty.GetValueOrDefault() < 0 ?
                AMTranType.Adjustment : AMTranType.Receipt;
            var invtMultNew = tran.LastOper.GetValueOrDefault()
                ? AMTranType.InvtMult(tranTypeNew, tran.Qty)
                : 0;

#if DEBUG
            AMDebug.TraceWriteMethodName($"TranType = {tranTypeNew} (old value = {tranTypeOld}); InvtMult = {invtMultNew} (old value = {invtMultOld})");
#endif
            var syncSplits = false;
            if (invtMultNew != tran.InvtMult)
            {
                syncSplits |= tran.InvtMult != null;
                cache.SetValueExt<AMClockItem.invtMult>(tran, invtMultNew);
            }

            if (tranTypeNew != tran.TranType)
            {
                syncSplits |= tran.TranType != null;
                cache.SetValueExt<AMClockItem.tranType>(tran, tranTypeNew);
            }

            if (syncSplits)
            {
                SyncSplitTranType(cache, tran, cache.Graph.Caches[typeof(AMClockItemSplit)]);
            }
        }

        #region Overrides
        public override AMClockItemSplit Convert(AMClockItem item)
        {
            using (InvtMultScope<AMClockItem> ms = new InvtMultScope<AMClockItem>(item))
            {
                AMClockItemSplit ret = item;
                ret.BaseQty = item.BaseQty - item.UnassignedQty;
                return ret;
            }
        }

        protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo e)
        {
            if (row is AMClockItem)
            {
#if DEBUG
                AMDebug.TraceWriteMethodName(e.MessageFormat, sender.GetStateExt<AMClockItem.inventoryID>(row), sender.GetStateExt<AMClockItem.subItemID>(row), sender.GetStateExt<AMClockItem.siteID>(row), sender.GetStateExt<AMClockItem.locationID>(row), sender.GetValue<AMClockItem.lotSerialNbr>(row));
#endif
                sender.RaiseExceptionHandling<AMClockItem.qty>(row, newValue,
                    new PXSetPropertyException(e.MessageFormat, PXErrorLevel.Warning,
                    sender.GetStateExt<AMClockItem.inventoryID>(row),
                    sender.GetStateExt<AMClockItem.subItemID>(row),
                    sender.GetStateExt<AMClockItem.siteID>(row),
                    sender.GetStateExt<AMClockItem.locationID>(row),
                    sender.GetValue<AMClockItem.lotSerialNbr>(row)));

                return;
            }

            sender.RaiseExceptionHandling<AMClockItemSplit.qty>(row, newValue,
                new PXSetPropertyException(e.MessageFormat, PXErrorLevel.Warning,
                    sender.GetStateExt<AMClockItemSplit.inventoryID>(row),
                    sender.GetStateExt<AMClockItemSplit.subItemID>(row),
                    sender.GetStateExt<AMClockItemSplit.siteID>(row),
                    sender.GetStateExt<AMClockItemSplit.locationID>(row),
                    sender.GetValue<AMClockItemSplit.lotSerialNbr>(row)));
        }

        public override void UpdateParent(PXCache sender, AMClockItem Row, AMClockItemSplit Det, AMClockItemSplit OldDet, out decimal BaseQty)
        {
            counters = null;
            if (!DetailCounters.TryGetValue(Row, out counters))
            {
                DetailCounters[Row] = counters = new Counters();
                foreach (AMClockItemSplit detail in SelectDetail(sender.Graph.Caches[typeof(AMClockItemSplit)], Row))
                {
                    UpdateCounters(sender, counters, detail);
                }
            }
            else
            {
                if (Det != null)
                {
                    UpdateCounters(sender, counters, Det);
                }
                if (OldDet != null)
                {
                    AMClockItemSplit detail = OldDet;
                    counters.RecordCount -= 1;
                    detail.BaseQty = INUnitAttribute.ConvertToBase(sender, detail.InventoryID, detail.UOM, (decimal)detail.Qty, detail.BaseQty, INPrecision.QUANTITY);
                    counters.BaseQty -= (decimal)detail.BaseQty;
                    if (detail.ExpireDate == null)
                    {
                        counters.ExpireDatesNull -= 1;
                    }
                    else if (counters.ExpireDates.ContainsKey(detail.ExpireDate))
                    {
                        if ((counters.ExpireDates[detail.ExpireDate] -= 1) == 0)
                        {
                            counters.ExpireDates.Remove(detail.ExpireDate);
                        }
                    }
                    if (detail.SubItemID == null)
                    {
                        counters.SubItemsNull -= 1;
                    }
                    else if (counters.SubItems.ContainsKey(detail.SubItemID))
                    {
                        if ((counters.SubItems[detail.SubItemID] -= 1) == 0)
                        {
                            counters.SubItems.Remove(detail.SubItemID);
                        }
                    }
                    if (detail.LocationID == null)
                    {
                        counters.LocationsNull -= 1;
                    }
                    else if (counters.Locations.ContainsKey(detail.LocationID))
                    {
                        if ((counters.Locations[detail.LocationID] -= 1) == 0)
                        {
                            counters.Locations.Remove(detail.LocationID);
                        }
                    }
                    if (detail.TaskID == null)
                    {
                        counters.ProjectTasksNull -= 1;
                    }
                    else
                    {
                        var kv = new KeyValuePair<int?, int?>(detail.ProjectID, detail.TaskID);
                        if (counters.ProjectTasks.ContainsKey(kv))
                        {
                            if ((counters.ProjectTasks[kv] -= 1) == 0)
                            {
                                counters.ProjectTasks.Remove(kv);
                            }
                        }
                    }
                    if (detail.LotSerialNbr == null)
                    {
                        counters.LotSerNumbersNull -= 1;
                    }
                    else if (counters.LotSerNumbers.ContainsKey(detail.LotSerialNbr))
                    {
                        if (string.IsNullOrEmpty(detail.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(detail.AssignedNbr, detail.LotSerialNbr))
                        {
                            counters.UnassignedNumber--;
                        }
                        if ((counters.LotSerNumbers[detail.LotSerialNbr] -= 1) == 0)
                        {
                            counters.LotSerNumbers.Remove(detail.LotSerialNbr);
                        }
                    }
                }
                if (Det == null && OldDet != null)
                {
                    if (counters.ExpireDates.Count == 1 && counters.ExpireDatesNull == 0)
                    {
                        foreach (DateTime? key in counters.ExpireDates.Keys)
                        {
                            counters.ExpireDate = key;
                        }
                    }
                    if (counters.SubItems.Count == 1 && counters.SubItemsNull == 0)
                    {
                        foreach (int? key in counters.SubItems.Keys)
                        {
                            counters.SubItem = key;
                        }
                    }
                    if (counters.Locations.Count == 1 && counters.LocationsNull == 0)
                    {
                        foreach (int? key in counters.Locations.Keys)
                        {
                            counters.Location = key;
                        }
                    }
                    if (counters.ProjectTasks.Count == 1 && counters.ProjectTasksNull == 0)
                    {
                        foreach (KeyValuePair<int?, int?> key in counters.ProjectTasks.Keys)
                        {
                            counters.ProjectID = key.Key;
                            counters.TaskID = key.Value;
                        }
                    }
                    if (counters.LotSerNumbers.Count == 1 && counters.LotSerNumbersNull == 0)
                    {
                        foreach (string key in counters.LotSerNumbers.Keys)
                        {
                            counters.LotSerNumber = key;
                        }
                    }
                }
            }

            BaseQty = counters.BaseQty;

            switch (counters.RecordCount)
            {
                case 0:
                    Row.LotSerialNbr = string.Empty;
                    Row.HasMixedProjectTasks = false;
                    break;
                case 1:
                    Row.ExpireDate = counters.ExpireDate;
                    Row.SubItemID = counters.SubItem;
                    Row.LocationID = counters.Location;
                    Row.LotSerialNbr = counters.LotSerNumber;
                    Row.HasMixedProjectTasks = false;
                    if (counters.ProjectTasks.Count > 0 && Det != null && counters.ProjectID != null)
                    {
                        Row.ProjectID = counters.ProjectID;
                        Row.TaskID = counters.TaskID;
                    }
                    break;
                default:
                    Row.ExpireDate = counters.ExpireDates.Count == 1 && counters.ExpireDatesNull == 0 ? counters.ExpireDate : null;
                    Row.SubItemID = counters.SubItems.Count == 1 && counters.SubItemsNull == 0 ? counters.SubItem : null;
                    Row.LocationID = counters.Locations.Count == 1 && counters.LocationsNull == 0 ? counters.Location : null;
                    Row.HasMixedProjectTasks = counters.ProjectTasks.Count + (counters.ProjectTasks.Count > 0 ? counters.ProjectTasksNull : 0) > 1;
                    if (Row.HasMixedProjectTasks != true && Det != null && counters.ProjectID != null)
                    {
                        Row.ProjectID = counters.ProjectID;
                        Row.TaskID = counters.TaskID;
                    }

                    PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
                    INLotSerTrack.Mode mode = GetTranTrackMode(Row, item);
                    if (mode == INLotSerTrack.Mode.None)
                    {
                        Row.LotSerialNbr = string.Empty;
                    }
                    else if ((mode & INLotSerTrack.Mode.Create) > 0 || (mode & INLotSerTrack.Mode.Issue) > 0)
                    {
                        //if more than 1 split exist at lotserial creation time ignore equilness and display <SPLIT>
                        Row.LotSerialNbr = null;
                    }
                    else
                    {
                        Row.LotSerialNbr = counters.LotSerNumbers.Count == 1 && counters.LotSerNumbersNull == 0 ? counters.LotSerNumber : null;
                    }
                    break;
            }
        }

        public override void UpdateParent(PXCache sender, AMClockItem Row)
        {
            decimal BaseQty;
            UpdateParent(sender, Row, null, null, out BaseQty);
            Row.UnassignedQty = PXDBQuantityAttribute.Round((decimal)(Row.BaseQty - BaseQty));
        }

        public override void UpdateParent(PXCache sender, AMClockItemSplit Row, AMClockItemSplit OldRow)
        {
            AMClockItem parent = (AMClockItem)PXParentAttribute.SelectParent(sender, Row ?? OldRow, typeof(AMClockItem));

            if (parent != null && (Row ?? OldRow) != null && SameInventoryItem((ILSMaster)(Row ?? OldRow), (ILSMaster)parent))
            {
                AMClockItem oldrow = PXCache<AMClockItem>.CreateCopy(parent);
                decimal BaseQty;

                UpdateParent(sender, parent, Row, OldRow, out BaseQty);

                using (InvtMultScope<AMClockItem> ms = new InvtMultScope<AMClockItem>(parent))
                {
                    if (BaseQty < parent.BaseQty)
                    {
                        parent.UnassignedQty = PXDBQuantityAttribute.Round((decimal)(parent.BaseQty - BaseQty));
                    }
                    else
                    {
                        parent.UnassignedQty = 0m;
                        parent.BaseQty = BaseQty;
                        parent.Qty = INUnitAttribute.ConvertFromBase(sender, parent.InventoryID, parent.UOM, (decimal)parent.BaseQty, INPrecision.QUANTITY);
                    }
                }

                sender.Graph.Caches[typeof(AMClockItem)].MarkUpdated(parent);

                if (Math.Abs((Decimal)oldrow.Qty - (Decimal)parent.Qty) >= 0.0000005m)
                {
                    sender.Graph.Caches[typeof(AMClockItem)].RaiseFieldUpdated(_MasterQtyField, parent, oldrow.Qty);
                    sender.Graph.Caches[typeof(AMClockItem)].RaiseRowUpdated(parent, oldrow);
                }
            }
        }

        public override IEnumerable GenerateLotSerial(PXAdapter adapter)
        {
            LotSerOptions opt = (LotSerOptions)_Graph.Caches[typeof(LotSerOptions)].Current;
            if (opt.StartNumVal == null || opt.Qty == null)
                return adapter.Get();

            PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(MasterCache, MasterCurrent.InventoryID);
            var lsClass = (INLotSerClass)item;
            if (lsClass == null)
                return adapter.Get();

            ILotSerNumVal lotSerNum = ReadLotSerNumVal(MasterCache, item);

            string lotSerialNbr = null;
            INLotSerialNbrAttribute.LSParts parts = INLotSerialNbrAttribute.GetLSParts(MasterCache, lsClass, lotSerNum);
            string numVal = opt.StartNumVal.Substring(parts.nidx, parts.nlen);
            string numStr = opt.StartNumVal.Substring(0, parts.flen) + new string('0', parts.nlen) + opt.StartNumVal.Substring(parts.lidx, parts.llen);

            try
            {
                MasterCurrent.LotSerialNbr = null;

                List<AMClockItemSplit> existingSplits = new List<AMClockItemSplit>();
                if (lsClass.LotSerTrack == INLotSerTrack.LotNumbered)
                {
                    foreach (AMClockItemSplit split in PXParentAttribute.SelectSiblings(DetailCache, null, typeof(AMClockItem)))
                    {
                        existingSplits.Add(split);
                    }
                }

                if (lsClass.LotSerTrack != INLotSerTrack.LotNumbered || (opt.Qty != 0 && MasterCurrent.BaseQty != 0m))
                {
                    CreateNumbers(MasterCache, MasterCurrent, (decimal)opt.Qty, true);
                }

                foreach (AMClockItemSplit split in PXParentAttribute.SelectSiblings(DetailCache, null, typeof(AMClockItem)))
                {
                    if (string.IsNullOrEmpty(split.AssignedNbr) ||
                        !INLotSerialNbrAttribute.StringsEqual(split.AssignedNbr, split.LotSerialNbr)) continue;

                    AMClockItemSplit copy = PXCache<AMClockItemSplit>.CreateCopy(split);

                    if (lotSerialNbr != null)
                        numVal = AutoNumberAttribute.NextNumber(numVal);

                    if ((decimal)opt.Qty != split.Qty && lsClass.LotSerTrack == INLotSerTrack.LotNumbered && !existingSplits.Contains(split))
                    {
                        split.BaseQty = (decimal)opt.Qty;
                        split.Qty = (decimal)opt.Qty;
                    }

                    lotSerialNbr = INLotSerialNbrAttribute.UpdateNumber(split.AssignedNbr, numStr, numVal);
                    split.LotSerialNbr = lotSerialNbr;
                    DetailCache.RaiseRowUpdated(split, copy);
                }
            }
            catch (Exception)
            {
                UpdateParent(MasterCache, MasterCurrent);
            }

            if (lotSerialNbr != null)
                UpdateLotSerNumVal(lotSerNum, numVal, item);
            return adapter.Get();
        }

        public override void CreateNumbers(PXCache sender, AMClockItem Row, decimal BaseQty, bool ForceAutoNextNbr)
        {
            PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
            AMClockItemSplit split = Convert(Row);

            if (Row != null)
                DetailCounters.Remove(Row);

            if (!ForceAutoNextNbr && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered &&
                ((INLotSerClass)item).AutoSerialMaxCount > 0 && ((INLotSerClass)item).AutoSerialMaxCount < BaseQty)
            {
                BaseQty = ((INLotSerClass)item).AutoSerialMaxCount.GetValueOrDefault();
            }

            INLotSerTrack.Mode mode = GetTranTrackMode(Row, item);
            ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
            foreach (AMClockItemSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<AMClockItemSplit>(sender, item, lotSerNum, mode, ForceAutoNextNbr, BaseQty))
            {
                string LotSerTrack = (mode & INLotSerTrack.Mode.Create) > 0 ? ((INLotSerClass)item).LotSerTrack : INLotSerTrack.NotNumbered;

                split.SplitLineNbr = null;
                split.LotSerialNbr = lssplit.LotSerialNbr;
                split.AssignedNbr = lssplit.AssignedNbr;
                split.LotSerClassID = lssplit.LotSerClassID;

                if (!string.IsNullOrEmpty(Row.LotSerialNbr) &&
                    ((LotSerTrack == INLotSerTrack.SerialNumbered && Row.Qty == 1m) ||
                        LotSerTrack == INLotSerTrack.LotNumbered))
                {
                    split.LotSerialNbr = Row.LotSerialNbr;
                }

                if (LotSerTrack == "S")
                {
                    split.UOM = null;
                    split.Qty = 1m;
                    split.BaseQty = 1m;
                }
                else
                {
                    split.UOM = null;
                    split.BaseQty = BaseQty;
                    split.Qty = BaseQty;
                }
                if (((INLotSerClass)item).LotSerTrackExpiration == true)
                    split.ExpireDate = ExpireDateByLot(sender, split, Row);

                sender.Graph.Caches[typeof(AMClockItemSplit)].Insert(PXCache<AMClockItemSplit>.CreateCopy(split));
                BaseQty -= (decimal)split.BaseQty;
            }

            if (BaseQty > 0m && (((INLotSerClass)item).LotSerTrack != "S" || decimal.Remainder(BaseQty, 1m) == 0m))
            {
                Row.UnassignedQty += BaseQty;
            }
            else if (BaseQty > 0m)
            {
                AMClockItem oldrow = PXCache<AMClockItem>.CreateCopy(Row);

                Row.BaseQty -= BaseQty;
                Row.Qty = INUnitAttribute.ConvertFromBase(sender, Row.InventoryID, Row.UOM, (decimal)Row.BaseQty, INPrecision.QUANTITY);

                if (Math.Abs((Decimal)oldrow.Qty - (Decimal)Row.Qty) >= 0.0000005m)
                {
                    sender.RaiseFieldUpdated(_MasterQtyField, Row, oldrow.Qty);
                    sender.RaiseRowUpdated(Row, oldrow);
                }
            }
            if (Row.UnassignedQty > 0)
                sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(PX.Objects.IN.Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
        }
        #endregion
    }
}
