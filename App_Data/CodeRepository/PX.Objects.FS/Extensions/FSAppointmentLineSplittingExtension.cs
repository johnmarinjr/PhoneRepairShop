using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

namespace PX.Objects.FS
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class FSAppointmentLineSplittingExtension : LineSplittingExtension<AppointmentEntry, FSAppointment, FSAppointmentDet, FSApptLineSplit>
	{
		#region State
		public virtual bool IsLotSerialRequired
		{
			get
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(LineCurrent.InventoryID);
				return item != null && ((INLotSerClass)item).LotSerTrack.IsNotIn(null, INLotSerTrack.NotNumbered);
			}
		}
		#endregion

		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(FSApptLineSplit.FK.Appointment.SameAsCurrent);

		protected override Type LineQtyField => typeof(FSAppointmentDet.effTranQty);

		public override FSApptLineSplit LineToSplit(FSAppointmentDet line) => StaticConvert(line);

		public static FSApptLineSplit StaticConvert(FSAppointmentDet line)
		{
			using (new InvtMultScope(line))
			{
				FSApptLineSplit ret = line;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				ret.LotSerialNbr = string.Empty;
				ret.SplitLineNbr = null;
				return ret;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();

			showSplits?.SetCaption(TX.Messages.LotsSerials);
			showSplits?.SetVisible(PXAccess.FeatureInstalled<FeaturesSet.inventory>());
		}
		#endregion

		#region Actions
		public override IEnumerable ShowSplits(PXAdapter adapter)
		{
			if (LineCurrent == null)
				return adapter.Get();

			if (LineCurrent.InventoryID == null)
				throw new PXException(TX.Error.NotValidFunctionWithInstructionOrCommentLines);

			if (LineCurrent.LineType.IsIn(ID.LineType_ALL.SERVICE, ID.LineType_ALL.NONSTOCKITEM) && LineCurrent.EnablePO == false)
				throw new PXException(SO.Messages.BinLotSerialInvalid);

			LineCurrent.IsLotSerialRequired = IsLotSerialRequired;

			return base.ShowSplits(adapter);
		}
		#endregion

		#region Event Handlers
		#region FSAppointmentDet
		public int? lastComponentID = null;
		protected override void EventHandlerInternal(ManualEvent.Row<FSAppointmentDet>.Updated.Args e)
		{
			if (lastComponentID == e.Row.InventoryID)
				return;

			if (e.Row.IsCanceledNotPerformed == true)
			{
				if (!Base.IsContractBasedAPI)
				{
					if (e.OldRow.InventoryID != e.Row.InventoryID)
					{
						e.Row.LotSerialNbr = null;
						e.Row.ExpireDate = null;
					}
					else if (e.OldRow.InvtMult != e.Row.InvtMult)
					{
						if (e.Row.LotSerialNbr == e.OldRow.LotSerialNbr)
							e.Row.LotSerialNbr = null;

						if (e.Row.ExpireDate == e.OldRow.ExpireDate)
							e.Row.ExpireDate = null;
					}
				}

				RaiseRowDeleted(e.OldRow);
			}
			else
			{
				INLotSerClass lotSerClass = ReadInventoryItem(e.Row.InventoryID);

				if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false
						&& e.Row.LotSerialNbr != e.OldRow.LotSerialNbr
						&& (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered || lotSerClass.LotSerTrack == INLotSerTrack.LotNumbered)
				)
				{
					UpdateLotSerialSplitsBasedOnLineLotSerial(e.Row, lotSerClass.LotSerTrack, lotSerClass.LotSerTrackExpiration);
				}

				InsertLotSerialsFromServiceOrder(e.Row, lotSerClass);

				_TruncateNumbers(e.Row, e.Row.BaseQty.Value);

				_UpdateParent(e.Row);
			}
		}

		protected override void EventHandler(ManualEvent.Row<FSAppointmentDet>.Updated.Args e)
		{
			using (ResolveNotDecimalUnitErrorRedirectorScope<FSApptLineSplit.qty>(e.Row))
			{
				base.EventHandler(e);
				if (e.Row.LotSerialNbr != e.OldRow.LotSerialNbr)
					e.Cache.RaiseFieldUpdated<FSAppointmentDet.lotSerialNbr>(e.Row, e.OldRow.LotSerialNbr);
			}
		}

		protected override void EventHandler(ManualEvent.Row<FSAppointmentDet>.Persisting.Args e)
		{
			if (e.Row.InventoryID != null &&
				e.Row.Status == FSAppointmentDet.status.COMPLETED &&
				e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update)
			)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);
				if (item == null)
					throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(InventoryItem)));

				string lotSerTrack = ((INLotSerClass)item).LotSerTrack;

				if (lotSerTrack.IsIn(INLotSerTrack.SerialNumbered, INLotSerTrack.LotNumbered))
				{
					GetExistingSplits(e.Row, out _, out decimal existingSplitTotalQty);

					if (e.Row.EffTranQty != existingSplitTotalQty)
					{
						e.Cache.RaiseExceptionHandling<FSAppointmentDet.status>(e.Row, e.Row.Status,
							new PXSetPropertyException(TX.Error.CannotCompleteLineBecauseLotSerialTotalQtyDoesNotMatchItemLineQty, PXErrorLevel.Error));
					}
				}
			}

			base.EventHandler(e);

			VerifyLotSerialTotalQty(e.Row, 0m, false);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<FSAppointmentDet>.Verifying.Args<decimal?> e)
		{
			Base.VerifySrvOrdLineQty(e.Cache, e.Row, e.NewValue, LineQtyField, true);
			base.EventHandlerQty(e);
		}
		#endregion
		#region FSApptLineSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();

			ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.lotSerialNbr>.Updated.Subscribe<string>(Base, EventHandler);

			ManualEvent.Row<FSApptLineSplit>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.Row<FSApptLineSplit>.Updating.Subscribe(Base, EventHandler);
			ManualEvent.Row<FSApptLineSplit>.Deleting.Subscribe(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.invtMult>.Defaulting.Args<short?> e)
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

		protected virtual void EventHandler(ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = SuppressedMode == true || e.NewValue != null;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<FSApptLineSplit, FSApptLineSplit.lotSerialNbr>.Updated.Args<string> e)
		{
			e.Row.OrigLineNbr = null;
			e.Row.OrigSplitLineNbr = null;
			e.Row.OrigSplitLineNbr = null;

			if (LineCurrent == null || LineCurrent.SODetID == null || LineCurrent.SODetID < 0)
				return;

			FSSODet soLine = SelectFrom<FSSODet>.Where<FSSODet.sODetID.IsEqual<P.AsInt>>.View.Select(Base, LineCurrent.SODetID);
			if (soLine != null)
			{
				e.Row.OrigLineNbr = soLine.LineNbr;

				if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false)
				{
					FSSODetSplit soSplit =
						SelectFrom<FSSODetSplit>.
						Where<
							FSSODetSplit.srvOrdType.IsEqual<@P.AsString.ASCII>.
							And<FSSODetSplit.refNbr.IsEqual<@P.AsString>>.
							And<FSSODetSplit.lineNbr.IsEqual<@P.AsInt>>.
							And<FSSODetSplit.lotSerialNbr.IsEqual<@P.AsString>>>.
						View.Select(Base, soLine.SrvOrdType, soLine.RefNbr, soLine.LineNbr, e.Row.LotSerialNbr);

					if (soSplit != null)
						FillLotSerialAndPOFields(e.Row, soSplit);
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<FSApptLineSplit>.Selected.Args e)
		{
			bool isLotSerialRequired = LineCurrent?.IsLotSerialRequired == true;

			SplitCache.AllowInsert = isLotSerialRequired;
			SplitCache.AllowDelete = isLotSerialRequired;
			SplitCache.AllowUpdate = isLotSerialRequired;

			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetEnabled<FSApptLineSplit.subItemID>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<FSApptLineSplit.siteID>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<FSApptLineSplit.locationID>(e.Cache, e.Row, false);
		}

		protected virtual void EventHandler(ManualEvent.Row<FSApptLineSplit>.Updating.Args e)
		{
			if (e.ExternalCall && IsLotSerialRequired == false)
				throw new PXException(TX.Error.CannotEditSplitBecauseItIsReservedForReceiptAllocationInfo);
		}

		protected override void EventHandler(ManualEvent.Row<FSApptLineSplit>.Updated.Args e)
		{
			if (e.Row.LotSerialNbr != e.OldRow.LotSerialNbr && e.Row.LotSerialNbr != null && e.Row.Operation == SO.SOOperation.Issue)
				LotSerialNbrUpdated(e.Row);

			if (e.Row.LocationID != e.OldRow.LocationID && e.Row.LotSerialNbr != null && e.ExternalCall)
				LocationUpdated(e.Row);

			base.EventHandler(e);
			MarkParentAsUpdated();
		}

		protected override void EventHandler(ManualEvent.Row<FSApptLineSplit>.Inserting.Args e)
		{
			if (e.ExternalCall && IsLotSerialRequired == false)
				throw new PXException(TX.Error.CannotEditSplitBecauseItIsReservedForReceiptAllocationInfo);

			(var item, var lsClass) = base.ReadInventoryItem(e.Row.InventoryID);

			if (item.KitItem == true && item.StkItem == false)
				e.Row.InventoryID = null;

			// This is to allow inserting multiple split lines for NotNumbered items with different purchase receipt info.
			using (SuppressedModeScope(lsClass.LotSerTrack.IsIn(INLotSerTrack.SerialNumbered, INLotSerTrack.LotNumbered)))
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<FSApptLineSplit>.Inserted.Args e)
		{
			base.EventHandler(e);
			MarkParentAsUpdated();
		}

		protected virtual void EventHandler(ManualEvent.Row<FSApptLineSplit>.Deleting.Args e)
		{
			if (e.ExternalCall && IsLotSerialRequired == false)
				throw new PXException(TX.Error.CannotEditSplitBecauseItIsReservedForReceiptAllocationInfo);
		}

		protected override void EventHandler(ManualEvent.Row<FSApptLineSplit>.Deleted.Args e)
		{
			base.EventHandler(e);
			MarkParentAsUpdated();
		}

		public override void EventHandler(ManualEvent.Row<FSApptLineSplit>.Persisting.Args e) // former FSApptLineSplit_RowPersisting subscribed separately
		{
			base.EventHandler(e);
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				bool RequireLocationAndSubItem = e.Row.IsStockItem == true && e.Row.BaseQty != 0m;

				PXDefaultAttribute.SetPersistingCheck<FSApptLineSplit.subItemID>(e.Cache, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<FSApptLineSplit.locationID>(e.Cache, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

				bool lotSerialRequired = e.Row.POReceiptNbr == null;

				PXDefaultAttribute.SetPersistingCheck<FSApptLineSplit.lotSerialNbr>(e.Cache, e.Row, lotSerialRequired == true ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

				if (e.Row.Qty == 0m)
				{
					PXSetPropertyException exception = new PXSetPropertyException(TX.Error.QtyMustBeDifferentFromZeroOnAllLotSerialLines, PXErrorLevel.Error);
					e.Cache.RaiseExceptionHandling<FSApptLineSplit.lotSerialNbr>(e.Row, null, exception);
				}
			}
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<FSApptLineSplit>.Verifying.Args<decimal?> e)
		{
			base.EventHandlerQty(e);

			if (e.NewValue.IsIn(null, 0m))
				return;

			// Validates that the total quantity of the split does not exceed the quantity required by the master line.
			VerifyLotSerialTotalQty(e.Row, e.NewValue.Value - e.Row.Qty.Value);

			if (e.Row.InventoryID == null)
				return;

			(var _, var lotSerClass) = ReadInventoryItem(e.Row.InventoryID);

			// Validates the available quantity for the Lot/Serial number
			if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false && lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered)
			{
				GetLotSerialAvailability(
					LineCurrent,
					e.Row.LotSerialNbr,
					splitLineNbr: null,
					ignoreUseByApptLine: true,
					out decimal lotSerialAvailQty,
					out decimal lotSerialUsedQty,
					out bool foundServiceOrderAllocation);

				decimal remainingQty = lotSerialAvailQty - lotSerialUsedQty;

				if (remainingQty < e.NewValue.Value)
				{
					if (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
					{
						if (foundServiceOrderAllocation)
							throw new PXSetPropertyException(TX.Error.LotSerialNbrOnOtherAppointment);
						else
							throw new PXSetPropertyException(TX.Error.LotSerialNotAvailable);
					}
					else
					{
						if (foundServiceOrderAllocation)
						{
							if (lotSerialUsedQty == 0)
								throw new PXSetPropertyException(TX.Error.QtyEnteredXForLotNumberGreaterThanServiceOrderAllocQtyX,
									e.NewValue.Value.ToString("0"),
									lotSerialAvailQty.ToString("0"));
							else
								throw new PXSetPropertyException(TX.Error.QtyEnteredXForLotNumberPlusOtherApptsQtyXGreaterThanServiceOrderAllocQtyX,
									e.NewValue.Value.ToString("0"),
									lotSerialUsedQty.ToString("0"),
									lotSerialAvailQty.ToString("0"));
						}
						else
						{
							throw new PXSetPropertyException(TX.Error.QtyEnteredXForLotNumberGreaterThanINAvailQtyX,
								e.NewValue.Value.ToString("0"),
								lotSerialAvailQty.ToString("0"));
						}
					}
				}
			}
		}

		protected virtual bool LotSerialNbrUpdated(FSApptLineSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);
			INSiteLotSerial siteLotSerial =
				SelectFrom<INSiteLotSerial>.
				Where<
					INSiteLotSerial.inventoryID.IsEqual<@P.AsInt>.
					And<INSiteLotSerial.siteID.IsEqual<@P.AsInt>>.
					And<INSiteLotSerial.lotSerialNbr.IsEqual<@P.AsString>>>.
				View.Select(Base, split.InventoryID, split.SiteID, split.LotSerialNbr);

			if (INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null && siteLotSerial != null && siteLotSerial.LotSerAssign != INLotSerAssign.WhenUsed)
			{
				if (split.BaseQty <= 0m)
				{
					split.BaseQty = 1;
					SplitCache.SetValueExt<FSApptLineSplit.qty>(split, INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY));
				}
			}
			return true;
		}

		protected virtual void LocationUpdated(FSApptLineSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);

			if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null)
			{
				INLotSerialStatus status =
					SelectFrom<INLotSerialStatus>.
					Where<
						INLotSerialStatus.inventoryID.IsEqual<@P.AsInt>.
						And<INLotSerialStatus.subItemID.IsEqual<@P.AsInt>>.
						And<INLotSerialStatus.siteID.IsEqual<@P.AsInt>>.
						And<INLotSerialStatus.lotSerialNbr.IsEqual<@P.AsString>>.
						And<INLotSerialStatus.locationID.IsEqual<@P.AsInt>>>.
					View.Select(Base, split.InventoryID, split.SubItemID, split.SiteID, split.LotSerialNbr, split.LocationID);

				if (status == null)
					split.LotSerialNbr = null;
			}
		}
		#endregion
		#endregion

		#region Select LotSerial Status
		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, FSAppointmentDet apptLine, INLotSerClass lotSerClass)
		{
			if (apptLine.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (apptLine.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			}
			else
			{
				switch (apptLine.TranType)
				{
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
				if (string.IsNullOrEmpty(apptLine.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}
		#endregion

		public override FSAppointmentDet Clone(FSAppointmentDet line)
		{
			FSAppointmentDet copy = base.Clone(line);
			copy.OrigSrvOrdNbr = null;
			copy.OrigLineNbr = null;

			return copy;
		}

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass) => INLotSerTrack.Mode.Manual;


		public virtual void _TruncateNumbers(FSAppointmentDet apptLine, decimal deltaBaseQty)
		{
			GetExistingSplits(apptLine, out decimal baseExistingSplitTotalQty);

			if (baseExistingSplitTotalQty > deltaBaseQty)
			{
				apptLine.UnassignedQty = 0m;
				TruncateNumbers(apptLine, baseExistingSplitTotalQty - deltaBaseQty);
			}
		}

		public virtual void _UpdateParent(FSAppointmentDet apptLine)
		{
			UpdateParent(apptLine);

			(var _, var lsClass) = ReadInventoryItem(apptLine.InventoryID);

			List<FSApptLineSplit> existingSplits = GetExistingSplits(apptLine, out decimal baseExistingSplitTotalQty, out decimal existingSplitTotalQty);

			if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && apptLine.BaseEffTranQty > 1m)
			{
				apptLine.LotSerialNbr = null;
			}
			else if (lsClass.LotSerTrack == INLotSerTrack.LotNumbered)
			{
				if (existingSplits.Count > 1)
				{
					apptLine.LotSerialNbr = null;
				}
			}

			UpdateLineStatusBasedOnReceivedPurchaseItems(
				Base.AppointmentRecords.Current,
				apptLine,
				MustHaveRequestPOStatus(apptLine),
				existingSplits,
				baseExistingSplitTotalQty,
				existingSplitTotalQty,
				runSetValueExt: true);
		}


		public virtual bool MustHaveRequestPOStatus(FSAppointmentDet apptLine)
			=> FSPOReceiptProcess.MustHaveRequestPOStatusStatic(apptLine);

		public virtual void UpdateLineStatusBasedOnReceivedPurchaseItems(FSAppointment appt, FSAppointmentDet apptLine, bool rowMustHaveRequestPOStatus, List<FSApptLineSplit> existingSplits, decimal? baseExistingSplitTotalQty, decimal? existingSplitTotalQty, bool runSetValueExt)
		{
			FSPOReceiptProcess.UpdateLineStatusBasedOnReceivedPurchaseItemsStatic(appt, LineCache, apptLine, rowMustHaveRequestPOStatus, existingSplits, baseExistingSplitTotalQty, existingSplitTotalQty, runSetValueExt);
		}


		protected virtual List<FSApptLineSplit> GetExistingSplits(FSAppointmentDet apptLine, out decimal baseExistingSplitTotalQty) => GetExistingSplits(apptLine, out baseExistingSplitTotalQty, out _);
		protected virtual List<FSApptLineSplit> GetExistingSplits(FSAppointmentDet apptLine, out decimal baseExistingSplitTotalQty, out decimal existingSplitTotalQty)
		{
			baseExistingSplitTotalQty = 0m;
			existingSplitTotalQty = 0m;
			var existingSplits = new List<FSApptLineSplit>();

			foreach (FSApptLineSplit existingSplit in PXParentAttribute.SelectChildren(SplitCache, apptLine, typeof(FSAppointmentDet)))
			{
				baseExistingSplitTotalQty += existingSplit.BaseQty.Value;
				existingSplitTotalQty += existingSplit.Qty.Value;

				existingSplits.Add(existingSplit);
			}

			return existingSplits;
		}


		public virtual void FillLotSerialAndPOFields(FSApptLineSplit split, FSSODetSplit soDetSplit) => FSPOReceiptProcess.FillLotSerialAndPOFieldsStatic(split, soDetSplit);

		public virtual bool VerifyLotSerialTotalQty(FSApptLineSplit split, decimal newIncrease)
		{
			if (newIncrease < 0m)
				return true;

			FSAppointmentDet apptLine = PXParentAttribute.SelectParent<FSAppointmentDet>(SplitCache, split);

			return VerifyLotSerialTotalQty(apptLine, newIncrease, true);
		}

		public virtual bool VerifyLotSerialTotalQty(FSAppointmentDet apptLine, decimal newIncrease, bool runningFieldVerifying)
		{
			GetExistingSplits(apptLine, out _, out decimal existingSplitTotalQty);

			decimal newSplitTotalQty = existingSplitTotalQty + newIncrease;

			if (newSplitTotalQty > apptLine.EffTranQty)
			{
				PXSetPropertyException exception = new PXSetPropertyException(TX.Error.TotalLotSerialQtyXExceedsTheQtyRequiredX, PXErrorLevel.Error,
						newSplitTotalQty.ToString("0"),
						apptLine.EffTranQty.Value.ToString("0"));

				if (runningFieldVerifying)
					throw exception;
				else
					LineCache.RaiseExceptionHandling<FSAppointmentDet.effTranQty>(apptLine, apptLine.EffTranQty, exception);

				return false;
			}

			return true;
		}

		protected virtual void InsertLotSerialsFromServiceOrder(FSAppointmentDet apptLine, INLotSerClass lotSerClass)
		{
			if (apptLine.SODetID == null || apptLine.SODetID < 0)
				return;

			bool allocateFromReceivedPurchaseItems = false;
			if (apptLine.EnablePO == true
				&& MustHaveRequestPOStatus(apptLine) == false
				&& lotSerClass.LotSerTrack.IsNotIn(INLotSerTrack.SerialNumbered, INLotSerTrack.LotNumbered))
			{
				allocateFromReceivedPurchaseItems = true;
			}

			if (allocateFromReceivedPurchaseItems == false)
			{
				var srvOrdType = (FSSrvOrdType)Base.Caches<FSSrvOrdType>().Current;

				if (srvOrdType == null || srvOrdType.SetLotSerialNbrInAppts == false)
					return;
			}

			List<FSApptLineSplit> existingSplits = GetExistingSplits(apptLine, out decimal baseExistingSplitTotalQty, out decimal existingSplitTotalQty);
			decimal pendingQty = apptLine.EffTranQty.Value - existingSplitTotalQty;

			if (pendingQty > 0m)
			{
				FSSODet soDetRow = SelectFrom<FSSODet>.Where<FSSODet.sODetID.IsEqual<@P.AsInt>>.View.Select(Base, apptLine.SODetID);
				if (soDetRow == null)
					throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));

				List<FSSODetSplit> soSplitsWithLotSerial =
					SelectFrom<FSSODetSplit>.
					Where<
						FSSODetSplit.srvOrdType.IsEqual<@P.AsString.ASCII>.
						And<FSSODetSplit.refNbr.IsEqual<@P.AsString>>.
						And<FSSODetSplit.lineNbr.IsEqual<@P.AsInt>>.
						And<FSSODetSplit.pOCreate.IsEqual<False>>>.
					OrderBy<FSSODetSplit.splitLineNbr.Asc>.
					View.Select(Base, soDetRow.SrvOrdType, soDetRow.RefNbr, soDetRow.LineNbr)
					.RowCast<FSSODetSplit>()
					.ToList();

				foreach (FSSODetSplit soSplit in soSplitsWithLotSerial)
				{
					if (string.IsNullOrEmpty(soSplit.LotSerialNbr) == true && soSplit.POReceiptNbr == null)
						continue;

					GetLotSerialAvailability(
						apptLine,
						soSplit.LotSerialNbr,
						soSplit.SplitLineNbr,
						ignoreUseByApptLine: true,
						out decimal lotSerialAvailQty,
						out decimal lotSerialUsedQty,
						out bool foundServiceOrderAllocation);

					decimal soSplitBalance = lotSerialAvailQty - lotSerialUsedQty;
					FSApptLineSplit apptSplit = null;

					if (soSplitBalance > 0m)
					{
						apptSplit = existingSplits.Find(x =>
							x.LotSerialNbr == soSplit.LotSerialNbr && string.IsNullOrEmpty(soSplit.LotSerialNbr) == false ||
							x.OrigSplitLineNbr == soSplit.SplitLineNbr);
						if (apptSplit != null)
							soSplitBalance -= apptSplit.Qty.Value;
					}

					if (soSplitBalance > 0m)
					{
						if (apptSplit == null)
						{
							apptSplit = LineToSplit(apptLine);
							apptSplit.BaseQty = 0m;
							apptSplit.Qty = 0m;

							FillLotSerialAndPOFields(apptSplit, soSplit);
						}

						if (soSplitBalance > pendingQty)
							soSplitBalance = pendingQty;

						apptSplit.Qty += soSplitBalance;
						apptSplit.BaseQty = INUnitAttribute.ConvertToBase(SplitCache, apptSplit.InventoryID, apptSplit.UOM, apptSplit.Qty.Value, apptSplit.BaseQty, INPrecision.QUANTITY);

						pendingQty -= soSplitBalance;

						SplitCache.Update(apptSplit);
					}

					if (pendingQty <= 0m)
						break;
				}
			}
		}

		protected virtual void UpdateLotSerialSplitsBasedOnLineLotSerial(FSAppointmentDet apptLine, string lotSerTrack, bool? lotSerTrackExpiration)
		{
			if (string.IsNullOrEmpty(apptLine.LotSerialNbr) == true || lotSerTrack == INLotSerTrack.NotNumbered)
				return;

			List<FSApptLineSplit> existingSplits = GetExistingSplits(apptLine, out _);

			FSApptLineSplit lotSerialSplit = null;

			// Delete all splits except the split with the specified Lot/Serial
			foreach (FSApptLineSplit split in existingSplits)
			{
				if (split.LotSerialNbr == apptLine.LotSerialNbr)
				{
					lotSerialSplit = split;
				}
				else
				{
					SplitCache.Delete(split);
				}
			}

			GetLotSerialQtyDefault(apptLine, apptLine.LotSerialNbr, lotSerTrack, out decimal? qtyDefault);

			if (qtyDefault > apptLine.EffTranQty)
				qtyDefault = apptLine.EffTranQty;

			if (lotSerialSplit == null && qtyDefault > 0m)
			{
				lotSerialSplit = (FSApptLineSplit)SplitCache.CreateCopy(SplitCache.Insert(new FSApptLineSplit()));

				lotSerialSplit.LotSerialNbr = apptLine.LotSerialNbr;

				if (lotSerTrackExpiration == true)
					lotSerialSplit.ExpireDate = ExpireDateByLot(lotSerialSplit, apptLine);
			}

			if (lotSerialSplit != null)
			{
				if (qtyDefault > 0m)
				{
					lotSerialSplit.Qty = qtyDefault;
					lotSerialSplit.BaseQty = INUnitAttribute.ConvertToBase(SplitCache, lotSerialSplit.InventoryID, lotSerialSplit.UOM, lotSerialSplit.Qty ?? 0m, INPrecision.QUANTITY);

					try
					{
						Base.SkipLotSerialFieldVerifying = true;
						lotSerialSplit = (FSApptLineSplit)SplitCache.Update(lotSerialSplit);
					}
					finally
					{
						Base.SkipLotSerialFieldVerifying = false;
					}
				}
				else
				{
					SplitCache.Delete(lotSerialSplit);
				}
			}
		}

		protected virtual void GetLotSerialQtyDefault(FSAppointmentDet apptLine, string lotSerialNbr, string lotSerTrack, out decimal? qtyDefault)
		{
			GetLotSerialAvailability(
				apptLine,
				lotSerialNbr,
				splitLineNbr: null,
				ignoreUseByApptLine: true,
				out decimal lotSerialAvailQty,
				out decimal lotSerialUsedQty,
				out _);

			qtyDefault = lotSerialAvailQty - lotSerialUsedQty;
		}

		public virtual void GetLotSerialAvailability(FSAppointmentDet apptLine, string lotSerialNbr, int? splitLineNbr, bool ignoreUseByApptLine, out decimal lotSerialAvailQty, out decimal lotSerialUsedQty, out bool foundServiceOrderAllocation)
			=> FSApptLotSerialNbrAttribute.GetLotSerialAvailabilityStatic(Base, apptLine, lotSerialNbr, splitLineNbr, ignoreUseByApptLine, out lotSerialAvailQty, out lotSerialUsedQty, out foundServiceOrderAllocation);

		protected virtual FSINLotSerialNbrAttribute GetLotSerialSelector()
		{
			if (_LotSerialSelector != null)
				return _LotSerialSelector;

			if (SplitCache.GetAttributes<FSApptLineSplit.lotSerialNbr>().OfType<FSINLotSerialNbrAttribute>().FirstOrDefault() is FSINLotSerialNbrAttribute attr)
				return _LotSerialSelector = attr;

			return null;
		}
		protected FSINLotSerialNbrAttribute _LotSerialSelector = null;

		public virtual void MarkParentAsUpdated()
		{
			if (LineCurrent == null)
				return;

			LineCache.MarkUpdated(LineCurrent);

			if (Base.AppointmentRecords.Current != null)
				Base.AppointmentRecords.Current.MustUpdateServiceOrder = true;
		}

		/// <summary>
		/// Inserts FSAppointmentDet into cache without adding the splits.
		/// The Splits have to be added manually.
		/// </summary>
		/// <param name="apptLine">Master record.</param>
		public virtual FSAppointmentDet InsertWithoutSplits(FSAppointmentDet apptLine)
		{
			using (SuppressedModeScope(true))
			{
				var row = (FSAppointmentDet)LineCache.Insert(apptLine);
				LineCounters.Remove(row);
				return row;
			}
		}
	}
}
