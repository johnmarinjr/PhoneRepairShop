using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

using PX.Objects.Common;
using PX.Objects.Extensions;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class WorksheetPicking : PickPackShip.ScanExtension
	{
		public static bool IsActive() => WaveBatchPicking.IsActive() || PaperlessPicking.IsActive();

		#region Views
		public
			SelectFrom<SOPickingWorksheet>.
			Where<SOPickingWorksheet.worksheetNbr.IsEqual<WorksheetScanHeader.worksheetNbr.FromCurrent.NoDefault>>.
			View Worksheet;

		public
			SelectFrom<SOPicker>.
			Where<
				SOPicker.worksheetNbr.IsEqual<WorksheetScanHeader.worksheetNbr.FromCurrent.NoDefault>.
				And<SOPicker.pickerNbr.IsEqual<WorksheetScanHeader.pickerNbr.FromCurrent.NoDefault>>>.
			View Picker;

		public
			SelectFrom<SOPickingJob>.
			Where<SOPickingJob.FK.Picker.SameAsCurrent>.
			View PickingJob;

		public
			SelectFrom<SOPickerToShipmentLink>.
			Where<SOPickerToShipmentLink.FK.Picker.SameAsCurrent>.
			View ShipmentsOfPicker;

		public
			SelectFrom<SOPickListEntryToCartSplitLink>.
			InnerJoin<INCartSplit>.On<SOPickListEntryToCartSplitLink.FK.CartSplit>.
			Where<SOPickListEntryToCartSplitLink.FK.Cart.SameAsCurrent>.
			View PickerCartSplitLinks;

		public
			SelectFrom<SOPickerListEntry>.
			InnerJoin<INLocation>.On<SOPickerListEntry.FK.Location>.
			LeftJoin<SOPickerToShipmentLink>.On<
				SOPickerToShipmentLink.FK.Picker.
				And<SOPickerToShipmentLink.shipmentNbr.IsEqual<SOPickerListEntry.shipmentNbr>>>.
			Where<SOPickerListEntry.FK.Picker.SameAsCurrent>.
			View PickListOfPicker;
		protected virtual IEnumerable pickListOfPicker()
		{
			var delegateResult = new PXDelegateResult { IsResultSorted = true };
			delegateResult.AddRange(GetListEntries(WorksheetNbr, PickerNbr));
			return delegateResult;
		}
		#endregion

		#region Buttons
		public PXAction<ScanHeader> ReviewPickWS;
		[PXButton, PXUIField(DisplayName = "Review")]
		protected virtual IEnumerable reviewPickWS(PXAdapter adapter) => adapter.Get();
		#endregion

		#region State
		public WorksheetScanHeader WSHeader => Basis.Header.Get<WorksheetScanHeader>() ?? new WorksheetScanHeader();
		public ValueSetter<ScanHeader>.Ext<WorksheetScanHeader> WSSetter => Basis.HeaderSetter.With<WorksheetScanHeader>();

		#region WorksheetNbr
		public string WorksheetNbr
		{
			get => WSHeader.WorksheetNbr;
			set => WSSetter.Set(h => h.WorksheetNbr, value);
		}
		#endregion
		#region PickerNbr
		public Int32? PickerNbr
		{
			get => WSHeader.PickerNbr;
			set => WSSetter.Set(h => h.PickerNbr, value);
		}
		#endregion
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			if (e.Row == null)
				return;

			ReviewPickWS.SetVisible(Base.IsMobile && IsWorksheetMode(e.Row.Mode));

			bool isWorksheetMode = ShowWorksheetNbrForMode(e.Row.Mode);
			e.Cache.AdjustUI()
				.For<WMSScanHeader.refNbr>(ui => ui.Visible = !isWorksheetMode)
				.For<WorksheetScanHeader.worksheetNbr>(ui => ui.Visible = isWorksheetMode)
				.SameFor<WorksheetScanHeader.pickerNbr>();

			if (String.IsNullOrEmpty(WorksheetNbr))
			{
				Worksheet.Current = null;
				Picker.Current = null;
				PickingJob.Current = null;
			}
			else
			{
				Worksheet.Current = Worksheet.Select();
				Picker.Current = Picker.Select();
				PickingJob.Current = PickingJob.Select();
			}
		}
		#endregion

		#region DAC overrides
		[ShipmentAndWorksheetBorrowedNote]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		#region Logic
		public virtual IEnumerable<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>> GetListEntries(string worksheetNbr, int? pickerNbr)
			=> GetListEntries(worksheetNbr, pickerNbr, inverseList: false);
		public virtual IEnumerable<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>> GetListEntries(string worksheetNbr, int? pickerNbr, bool inverseList)
		{
			var cmd = new
				SelectFrom<SOPickerListEntry>.
				InnerJoin<SOPicker>.On<SOPickerListEntry.FK.Picker>.
				InnerJoin<INLocation>.On<SOPickerListEntry.FK.Location>.
				InnerJoin<InventoryItem>.On<SOPickerListEntry.FK.InventoryItem>.
				LeftJoin<SOPickerToShipmentLink>.On<
					SOPickerToShipmentLink.FK.Picker.
					And<SOPickerToShipmentLink.shipmentNbr.IsEqual<SOPickerListEntry.shipmentNbr>>>.
				Where<
					SOPicker.worksheetNbr.IsEqual<@P.AsString>.
					And<SOPicker.pickerNbr.IsEqual<@P.AsInt>>>.
				View(Basis);

			var entries = cmd
				.View.QuickSelect(new object[] { worksheetNbr, pickerNbr })
				.Cast<PXResult<SOPickerListEntry, SOPicker, INLocation, InventoryItem, SOPickerToShipmentLink>>();

			bool isProcessed(SOPickerListEntry e) => e.PickedQty >= e.Qty || e.ForceCompleted == true;
			(var processed, var notProcessed) = entries.DisuniteBy(s => isProcessed(s.GetItem<SOPickerListEntry>()));

			var result = new List<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>>();

			result.AddRange(
				notProcessed
				.OrderBy(r => r.GetItem<INLocation>().PathPriority)
				.ThenBy(r => r.GetItem<INLocation>().LocationCD)
				.ThenBy(r => r.GetItem<SOPickerListEntry>().IsUnassigned == false) // unassigned first
				.ThenBy(r => r.GetItem<SOPickerListEntry>().HasGeneratedLotSerialNbr == false) // generated numbers are similar to unassigned - they are both vacant ones
				.ThenBy(r => r.GetItem<InventoryItem>().InventoryCD)
				.ThenBy(r => r.GetItem<SOPickerListEntry>().LotSerialNbr)
				.ThenBy(r => r.GetItem<SOPickerListEntry>().PickedQty == 0)
				.ThenBy(r => r.GetItem<SOPickerListEntry>().With(e => e.Qty - e.PickedQty))
				.Select(r => new PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>(r, r, r))
				.With(rs => inverseList ? rs.Reverse() : rs));

			result.AddRange(
				processed
				.OrderBy(r => r.GetItem<INLocation>().PathPriority)
				.ThenBy(r => r.GetItem<INLocation>().LocationCD)
				.ThenBy(r => r.GetItem<InventoryItem>().InventoryCD)
				.ThenBy(r => r.GetItem<SOPickerListEntry>().LotSerialNbr)
				.Select(r => new PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>(r, r, r))
				.With(rs => inverseList ? rs : rs.Reverse()));

			return result;
		}

		public virtual bool IsWorksheetMode(string modeCode) => false;
		protected virtual bool ShowWorksheetNbrForMode(string modeCode) => IsWorksheetMode(modeCode);
		public virtual ScanMode<PickPackShip> FindModeForWorksheet(SOPickingWorksheet sheet)
			=> throw new InvalidOperationException($"Worksheet of the {Basis.SightOf<SOPickingWorksheet.worksheetType>(sheet)} type is not supported");

		public virtual SOPicker PickList => SOPicker.PK.Find(Basis, WorksheetNbr, PickerNbr);
		public virtual bool CanWSPick => PickListOfPicker.SelectMain().Any(s => s.PickedQty < s.Qty && s.ForceCompleted != true);
		public virtual bool NotStarted => PickListOfPicker.SelectMain().All(s => s.PickedQty == 0 && s.ForceCompleted != true);
		public string ShipmentSpecialPickType
		{
			get =>
				Basis.Shipment is SOShipment sh &&
				sh.PickedViaWorksheet == true &&
				sh.CurrentWorksheetNbr != null &&
				SOPickingWorksheet.PK.Find(Base, sh.CurrentWorksheetNbr) is SOPickingWorksheet ws
				? ws.WorksheetType
				: null;
		}

		public virtual bool IsLocationMissing(INLocation location, out Validation error)
		{
			if (PickListOfPicker.SelectMain().All(t => t.LocationID != location.LocationID))
			{
				error = Validation.Fail(Msg.LocationMissingInPickList, location.LocationCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsItemMissing(PXResult<INItemXRef, InventoryItem> item, out Validation error)
		{
			(INItemXRef xref, InventoryItem inventoryItem) = item;
			if (PickListOfPicker.SelectMain().All(t => t.InventoryID != inventoryItem.InventoryID))
			{
				error = Validation.Fail(Msg.InventoryMissingInPickList, inventoryItem.InventoryCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsLotSerialMissing(string lotSerialNbr, out Validation error)
		{
			if (Basis.IsEnterableLotSerial(isForIssue: true) == false && PickListOfPicker.SelectMain().All(t => t.LotSerialNbr != lotSerialNbr))
			{
				error = Validation.Fail(Msg.LotSerialMissingInPickList, lotSerialNbr);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool SetLotSerialNbrAndQty(SOPickerListEntry pickedSplit, decimal deltaQty)
		{
			if (pickedSplit.PickedQty == 0 && pickedSplit.IsUnassigned == false)
			{
				if (Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && Basis.SelectedLotSerialClass.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable)
				{
					SOPickerListEntry originalSplit =
						PickListOfPicker.Search<SOPickerListEntry.lotSerialNbr>(Basis.LotSerialNbr);

					if (originalSplit == null)
					{
						pickedSplit.LotSerialNbr = Basis.LotSerialNbr;
						pickedSplit.PickedQty += deltaQty;
						pickedSplit = PickListOfPicker.Update(pickedSplit);
					}
					else
					{
						if (originalSplit.LotSerialNbr == Basis.LotSerialNbr) return false;

						var tempOriginalSplit = PXCache<SOPickerListEntry>.CreateCopy(originalSplit);
						var tempPickedSplit = PXCache<SOPickerListEntry>.CreateCopy(pickedSplit);

						originalSplit.Qty = 0;
						originalSplit.LotSerialNbr = Basis.LotSerialNbr;
						originalSplit = PickListOfPicker.Update(originalSplit);
						originalSplit.Qty = tempOriginalSplit.Qty;
						originalSplit.PickedQty = tempPickedSplit.PickedQty + deltaQty;
						originalSplit.ExpireDate = tempPickedSplit.ExpireDate;
						originalSplit = PickListOfPicker.Update(originalSplit);

						pickedSplit.Qty = 0;
						pickedSplit.LotSerialNbr = tempOriginalSplit.LotSerialNbr;
						pickedSplit = PickListOfPicker.Update(pickedSplit);
						pickedSplit.Qty = tempPickedSplit.Qty;
						pickedSplit.PickedQty = tempOriginalSplit.PickedQty;
						pickedSplit.ExpireDate = tempOriginalSplit.ExpireDate;
						pickedSplit = PickListOfPicker.Update(pickedSplit);
					}
				}
				else if (pickedSplit.HasGeneratedLotSerialNbr == true)
				{
					var donorSplit = PXCache<SOPickerListEntry>.CreateCopy(pickedSplit);
					if (donorSplit.Qty == deltaQty)
					{
						PickListOfPicker.Delete(donorSplit);
					}
					else
					{
						donorSplit.Qty -= deltaQty;
						donorSplit.PickedQty -= Math.Min(deltaQty, donorSplit.PickedQty.Value);
						PickListOfPicker.Update(donorSplit);
					}

					var existingSplit = PickListOfPicker.SelectMain().FirstOrDefault(s =>
						s.HasGeneratedLotSerialNbr == false &&
						s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) &&
						IsSelectedSplit(s));

					if (existingSplit == null)
					{
						var newSplit = PXCache<SOPickerListEntry>.CreateCopy(pickedSplit);
						newSplit.EntryNbr = null;
						newSplit.LotSerialNbr = Basis.LotSerialNbr;
						if (Basis.ExpireDate != null)
							newSplit.ExpireDate = Basis.ExpireDate;
						newSplit.Qty = deltaQty;
						newSplit.PickedQty = deltaQty;
						newSplit.HasGeneratedLotSerialNbr = false;

						newSplit = PickListOfPicker.Insert(newSplit);
					}
					else
					{
						existingSplit.Qty += deltaQty;
						existingSplit.PickedQty += deltaQty;
						if (Basis.ExpireDate != null)
							existingSplit.ExpireDate = Basis.ExpireDate;
						existingSplit = PickListOfPicker.Update(existingSplit);
					}
				}
				else
				{
					pickedSplit.LotSerialNbr = Basis.LotSerialNbr;

					if (Basis.SelectedLotSerialClass.LotSerTrackExpiration == true)
					{
						if (Basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenReceived)
							pickedSplit.ExpireDate = LSSelect.ExpireDateByLot(Basis, PropertyTransfer.Transfer(pickedSplit, new SOShipLineSplit()), null);
						else if (Basis.ExpireDate != null)
						pickedSplit.ExpireDate = Basis.ExpireDate; // TODO: use expire date of the same lot/serial in the pick list
					}

					pickedSplit.PickedQty += deltaQty;
					pickedSplit = PickListOfPicker.Update(pickedSplit);
				}
			}
			else
			{
				var existingAssignedSplit = pickedSplit.IsUnassigned == true || Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.LotNumbered
					? PickListOfPicker.SelectMain().FirstOrDefault(s =>
						s.IsUnassigned == false &&
						s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) &&
						IsSelectedSplit(s))
					: null;

				if (pickedSplit.IsUnassigned == false) // Unassigned splits will be processed automatically
				{
					if (pickedSplit.Qty - deltaQty <= 0)
						pickedSplit = PickListOfPicker.Delete(pickedSplit);
					else
					{
						pickedSplit.Qty -= deltaQty;
						pickedSplit = PickListOfPicker.Update(pickedSplit);
					}
				}

				if (existingAssignedSplit != null)
				{
					existingAssignedSplit.PickedQty += deltaQty;
					if (existingAssignedSplit.PickedQty > existingAssignedSplit.Qty)
						existingAssignedSplit.Qty = existingAssignedSplit.PickedQty;

					existingAssignedSplit = PickListOfPicker.Update(existingAssignedSplit);
				}
				else
				{
					var newSplit = PXCache<SOPickerListEntry>.CreateCopy(pickedSplit);

					newSplit.EntryNbr = null;
					newSplit.LotSerialNbr = Basis.LotSerialNbr;
					if (pickedSplit.Qty > 0 || pickedSplit.IsUnassigned == true)
					{
						newSplit.Qty = deltaQty;
						newSplit.PickedQty = deltaQty;
						newSplit.IsUnassigned = false;
						if (Basis.SelectedLotSerialClass.LotSerTrackExpiration == true)
						{
							if (Basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenReceived)
								newSplit.ExpireDate = LSSelect.ExpireDateByLot(Basis, PropertyTransfer.Transfer(newSplit, new SOShipLineSplit()), null);
							else if (Basis.ExpireDate != null)
							newSplit.ExpireDate = Basis.ExpireDate;
					}
					}
					else
					{
						newSplit.Qty = pickedSplit.Qty;
						newSplit.PickedQty = pickedSplit.PickedQty;
					}

					newSplit = PickListOfPicker.Insert(newSplit);
				}
			}

			return true;
		}

		public virtual bool IsSelectedSplit(SOPickerListEntry split)
		{
			return
				split.InventoryID == Basis.InventoryID &&
				split.SubItemID == Basis.SubItemID &&
				split.SiteID == Basis.SiteID &&
				split.LocationID == (Basis.LocationID ?? split.LocationID) &&
				(split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr) ||
					Basis.Remove == false &&
					Basis.IsEnterableLotSerial(isForIssue: true));
		}

		public virtual FlowStatus ConfirmSplit(SOPickerListEntry pickedSplit, decimal deltaQty)
		{
			if (pickedSplit == null)
				return FlowStatus.Fail(deltaQty < 0 ? Msg.NothingToRemove : Msg.NothingToPick).WithModeReset;

			//decimal threshold = Basis.Graph.GetQtyThreshold(pickedSplit);

			if (deltaQty != 0)
			{
				bool splitUpdated = false;

				if (deltaQty < 0)
				{
					if (pickedSplit.PickedQty + deltaQty < 0)
						return FlowStatus.Fail(Msg.Underpicking);
				}
				else
				{
					if (pickedSplit.HasGeneratedLotSerialNbr == true && Basis.LotSerialNbr != null && pickedSplit.LotSerialNbr != Basis.LotSerialNbr)
					{
						var originalSplit = PickListOfPicker.SelectMain().FirstOrDefault(s =>
							s.HasGeneratedLotSerialNbr == false &&
							s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) &&
							s.PickedQty < s.Qty &&
							IsSelectedSplit(s));

						if (originalSplit != null)
							pickedSplit = originalSplit;
					}

					if (pickedSplit.PickedQty + deltaQty > pickedSplit.Qty/* * threshold*/)
						return FlowStatus.Fail(Msg.Overpicking);

					if (Basis.LotSerialNbr != null && Basis.SelectedLotSerialClass.LotSerTrack != INLotSerTrack.NotNumbered && Basis.IsEnterableLotSerial(isForIssue: true))
					{
						var availabilityStatus = CheckAvailability(deltaQty);
						if (availabilityStatus.IsError != false)
							return availabilityStatus;

						if (pickedSplit.LotSerialNbr != Basis.LotSerialNbr)
						{
							if (!SetLotSerialNbrAndQty(pickedSplit, deltaQty))
								return FlowStatus.Fail(Msg.Overpicking);
							splitUpdated = true;
						}
					}
				}

				AssignUser(startPicking: true);

				if (!splitUpdated)
				{
					//EnsureAssignedSplitEditing(pickedSplit);

					pickedSplit.PickedQty += deltaQty;

					if (deltaQty < 0 && Basis.IsEnterableLotSerial(isForIssue: true))
					{
						if (pickedSplit.PickedQty == 0)
						{
							PickListOfPicker.Delete(pickedSplit);
						}
						else
						{
							pickedSplit.Qty = pickedSplit.PickedQty;
							PickListOfPicker.Update(pickedSplit);
						}
					}
					else
						PickListOfPicker.Update(pickedSplit);
				}
			}

			// should be aware of cart extension
			if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSupport && cartSupport.CartID != null)
			{
				FlowStatus cartStatus = SyncWithCart(cartSupport, pickedSplit, deltaQty);
				if (cartStatus.IsError != false)
					return cartStatus;
			}

			return FlowStatus.Ok;
		}

		public virtual FlowStatus CheckAvailability(decimal deltaQty)
		{
			if (Basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenUsed && Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.LotNumbered)
				return FlowStatus.Ok;

			var virtuallyIssued =
				SelectFrom<SOPickerListEntry>.
				InnerJoin<SOPickingWorksheet>.On<SOPickerListEntry.FK.Worksheet>.
				Where<
					SOPickingWorksheet.status.IsEqual<SOPickingWorksheet.status.picking>.
					And<SOPickerListEntry.siteID.IsEqual<@P.AsInt>>.
					And<SOPickerListEntry.locationID.IsEqual<@P.AsInt>>.
					And<SOPickerListEntry.inventoryID.IsEqual<@P.AsInt>>.
					And<SOPickerListEntry.subItemID.IsEqual<@P.AsInt>>.
					And<SOPickerListEntry.lotSerialNbr.IsEqual<@P.AsString>>>.
				AggregateTo<Sum<SOPickerListEntry.basePickedQty>>.
				View.Select(Basis, Basis.SiteID, Basis.LocationID, Basis.InventoryID, Basis.SubItemID, Basis.LotSerialNbr).TopFirst;
			
			var allocation =
				SelectFrom<INLotSerialStatus>.
				Where<
					INLotSerialStatus.siteID.IsEqual<@P.AsInt>.
					And<INLotSerialStatus.locationID.IsEqual<@P.AsInt>>.
					And<INLotSerialStatus.inventoryID.IsEqual<@P.AsInt>>.
					And<INLotSerialStatus.subItemID.IsEqual<@P.AsInt>>.
					And<INLotSerialStatus.lotSerialNbr.IsEqual<@P.AsString>>>.
				View.Select(Basis, Basis.SiteID, Basis.LocationID, Basis.InventoryID, Basis.SubItemID, Basis.LotSerialNbr).TopFirst;

			if (Basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenUsed && Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered)
			{
				if ((virtuallyIssued?.BasePickedQty ?? 0) > 0 || (allocation?.QtyHardAvail ?? 0) < 0)
					return
						FlowStatus.Fail(
							IN.Messages.SerialNumberAlreadyIssued,
							Basis.LotSerialNbr,
							Basis.SightOf<WMSScanHeader.inventoryID>());
			}
			else // lot\serial + when received + user enterable
			{
				if (allocation == null)
					return 
						FlowStatus.Fail(
							Msg.MissingLotSerailOnLocation,
							Basis.SightOf<WMSScanHeader.inventoryID>(),
							Basis.LotSerialNbr,
							Basis.SightOf<WMSScanHeader.siteID>(),
							Basis.SightOf<WMSScanHeader.locationID>());

				if ((virtuallyIssued?.BasePickedQty ?? 0) + deltaQty > (allocation.QtyHardAvail ?? 0))
					return
						FlowStatus.Fail(
							Msg.ExceededAvailability,
							Basis.SightOf<WMSScanHeader.inventoryID>(),
							Basis.LotSerialNbr,
							Basis.SightOf<WMSScanHeader.siteID>(),
							Basis.SightOf<WMSScanHeader.locationID>());
			}

			return FlowStatus.Ok;
		}

		public virtual bool AssignUser(bool startPicking = false)
		{
			bool anyChanged = false;
			if (Picker.Current.UserID == null)
			{
				Picker.Current.UserID = Graph.Accessinfo.UserID;
				Picker.UpdateCurrent();
				anyChanged = true;
			}

			if (startPicking && SOPickingWorksheet.PK.Find(Basis, Worksheet.Current).Status == SOPickingWorksheet.status.Open)
			{
				Worksheet.Current.Status = SOPickingWorksheet.status.Picking;
				Worksheet.UpdateCurrent();
				anyChanged = true;
			}

			return anyChanged;
		}

		protected virtual FlowStatus SyncWithCart(PPSCartSupport cartSupport, SOPickerListEntry entry, decimal deltaQty)
		{
			INCartSplit[] linkedSplits =
				SelectFrom<SOPickListEntryToCartSplitLink>.
				InnerJoin<INCartSplit>.On<SOPickListEntryToCartSplitLink.FK.CartSplit>.
				Where<SOPickListEntryToCartSplitLink.FK.PickListEntry.SameAsCurrent.
					And<SOPickListEntryToCartSplitLink.siteID.IsEqual<@P.AsInt>>.
					And<SOPickListEntryToCartSplitLink.cartID.IsEqual<@P.AsInt>>>.
				View
				.SelectMultiBound(Basis, new object[] { entry }, Basis.SiteID, cartSupport.CartID)
				.RowCast<INCartSplit>()
				.ToArray();

			INCartSplit[] appropriateSplits =
				SelectFrom<INCartSplit>.
				Where<INCartSplit.cartID.IsEqual<@P.AsInt>.
					And<INCartSplit.inventoryID.IsEqual<SOPickerListEntry.inventoryID.FromCurrent>>.
					And<INCartSplit.subItemID.IsEqual<SOPickerListEntry.subItemID.FromCurrent>>.
					And<INCartSplit.siteID.IsEqual<SOPickerListEntry.siteID.FromCurrent>>.
					And<INCartSplit.fromLocationID.IsEqual<SOPickerListEntry.locationID.FromCurrent>>.
					And<INCartSplit.lotSerialNbr.IsEqual<SOPickerListEntry.lotSerialNbr.FromCurrent>>>.
				View
				.SelectMultiBound(Basis, new object[] { entry }, cartSupport.CartID)
				.RowCast<INCartSplit>()
				.ToArray();

			INCartSplit[] existingINSplits = linkedSplits.Concat(appropriateSplits).ToArray();

			INCartSplit cartSplit = existingINSplits.FirstOrDefault(s => s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr));
			if (cartSplit == null)
			{
				cartSplit = cartSupport.CartSplits.Insert(new INCartSplit
				{
					CartID = cartSupport.CartID,
					InventoryID = entry.InventoryID,
					SubItemID = entry.SubItemID,
					LotSerialNbr = entry.LotSerialNbr,
					ExpireDate = entry.ExpireDate,
					UOM = entry.UOM,
					SiteID = entry.SiteID,
					FromLocationID = entry.LocationID,
					Qty = deltaQty
				});
			}
			else
			{
				cartSplit.Qty += deltaQty;
				cartSplit = cartSupport.CartSplits.Update(cartSplit);
			}

			if (cartSplit.Qty == 0)
			{
				cartSupport.CartSplits.Delete(cartSplit);
				return FlowStatus.Ok;
			}
			else
				return EnsurePickerCartSplitLink(cartSupport, entry, cartSplit, deltaQty);
		}

		protected virtual FlowStatus EnsurePickerCartSplitLink(PPSCartSupport cartSupport, SOPickerListEntry entry, INCartSplit cartSplit, decimal deltaQty)
		{
			var allLinks =
				SelectFrom<SOPickListEntryToCartSplitLink>.
				Where<SOPickListEntryToCartSplitLink.FK.CartSplit.SameAsCurrent.
					Or<SOPickListEntryToCartSplitLink.FK.PickListEntry.SameAsCurrent>>.
				View
				.SelectMultiBound(Basis, new object[] { cartSplit, entry })
				.RowCast<SOPickListEntryToCartSplitLink>()
				.ToArray();

			SOPickListEntryToCartSplitLink currentLink = allLinks.FirstOrDefault(
				link => SOPickListEntryToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link)
					&& SOPickListEntryToCartSplitLink.FK.PickListEntry.Match(Basis, entry, link));

			decimal cartQty = allLinks.Where(link => SOPickListEntryToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link)).Sum(_ => _.Qty ?? 0);

			if (cartQty + deltaQty > cartSplit.Qty)
			{
				return FlowStatus.Fail(PPSCartSupport.Msg.LinkCartOverpicking);
			}
			if (currentLink == null ? deltaQty < 0 : currentLink.Qty + deltaQty < 0)
			{
				return FlowStatus.Fail(PPSCartSupport.Msg.LinkUnderpicking);
			}

			if (currentLink == null)
			{
				currentLink = PickerCartSplitLinks.Insert(new SOPickListEntryToCartSplitLink
				{
					WorksheetNbr = entry.WorksheetNbr,
					PickerNbr = entry.PickerNbr,
					EntryNbr = entry.EntryNbr,
					SiteID = cartSplit.SiteID,
					CartID = cartSplit.CartID,
					CartSplitLineNbr = cartSplit.SplitLineNbr,
					Qty = deltaQty
				});
			}
			else
			{
				currentLink.Qty += deltaQty;
				currentLink = PickerCartSplitLinks.Update(currentLink);
			}

			if (currentLink.Qty == 0)
				PickerCartSplitLinks.Delete(currentLink);

			return FlowStatus.Ok;
		}

		public virtual void SetPickList(PXResult<SOPickingWorksheet, SOPicker> pickList)
		{
			SOPickingWorksheet sheet = pickList;
			SOPicker picker = pickList;

			WorksheetNbr = sheet?.WorksheetNbr;
			Worksheet.Current = sheet;
			PickerNbr = picker?.PickerNbr;
			Picker.Current = picker;
			PickingJob.Current = PickingJob.Select();

			Basis.SiteID = sheet?.SiteID;
			Basis.TranDate = sheet?.PickDate;
			Basis.NoteID = sheet?.NoteID;
		}
		#endregion

		#region States
		public abstract class PickListState : PickPackShip.RefNbrState<PXResult<SOPickingWorksheet, SOPicker>>
		{
			private int _pickerNbr;

			public WorksheetPicking WSBasis => Basis.Get<WorksheetPicking>();

			protected abstract string WorksheetType { get; }

			protected override string StatePrompt => Msg.Prompt;
			protected override bool IsStateSkippable() => base.IsStateSkippable() || (WSBasis.WorksheetNbr != null && Basis.Header.ProcessingSucceeded != true);

			protected override PXResult<SOPickingWorksheet, SOPicker> GetByBarcode(string barcode)
			{
				if (barcode.Contains("/") == false)
					return null;

				(string worksheetNbr, string pickerNbrStr) = barcode.Split('/');
				_pickerNbr = int.Parse(pickerNbrStr);

				var doc = (PXResult<SOPickingWorksheet, INSite, SOPicker>)
					SelectFrom<SOPickingWorksheet>.
					InnerJoin<INSite>.On<SOPickingWorksheet.FK.Site>.
					LeftJoin<SOPicker>.On<SOPicker.FK.Worksheet.And<SOPicker.pickerNbr.IsEqual<@P.AsInt>>>.
					Where<
						SOPickingWorksheet.worksheetNbr.IsEqual<@P.AsString>.
						And<SOPickingWorksheet.worksheetType.IsEqual<@P.AsString>>.
						And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
					View.Select(Basis, _pickerNbr, worksheetNbr, WorksheetType);

				if (doc != null)
					return new PXResult<SOPickingWorksheet, SOPicker>(doc, doc);
				else
					return null;
			}

			protected override AbsenceHandling.Of<PXResult<SOPickingWorksheet, SOPicker>> HandleAbsence(string barcode)
			{
				if (Basis.FindMode<PickPackShip.PickMode>() is PickPackShip.PickMode pickMode && pickMode.IsActive)
				{
					if (pickMode.TryProcessBy<PickPackShip.PickMode.ShipmentState>(barcode) == true)
					{
						Basis.SetScanMode<PickPackShip.PickMode>();
						Basis.FindState<PickPackShip.PickMode.ShipmentState>().Process(barcode);
						return AbsenceHandling.Done;
					}
				}

				return base.HandleAbsence(barcode);
			}

			protected override Validation Validate(PXResult<SOPickingWorksheet, SOPicker> pickList)
			{
				(var worksheet, var picker) = pickList;

				if (worksheet.Status.IsNotIn(SOPickingWorksheet.status.Picking, SOPickingWorksheet.status.Open))
					return Validation.Fail(Msg.InvalidStatus, worksheet.WorksheetNbr, Basis.SightOf<SOPickingWorksheet.status>(worksheet));

				if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSup && cartSup.CartID != null && worksheet.SiteID != Basis.SiteID)
					return Validation.Fail(Msg.InvalidSite, worksheet.WorksheetNbr);

				if (picker?.PickerNbr == null)
					return Validation.Fail(Msg.PickerPositionMissing, _pickerNbr, worksheet.WorksheetNbr);

				if (picker.UserID.IsNotIn(null, Basis.Graph.Accessinfo.UserID))
					return Validation.Fail(Msg.PickerPositionOccupied, picker.PickerNbr, worksheet.WorksheetNbr);

				return base.Validate(pickList);
			}

			protected override void Apply(PXResult<SOPickingWorksheet, SOPicker> pickList)
			{
				Basis.RefNbr = null;
				Basis.Graph.Document.Current = null;

				WSBasis.SetPickList(pickList);
			}

			protected override void ClearState() => WSBasis.SetPickList(null);

			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void ReportSuccess(PXResult<SOPickingWorksheet, SOPicker> pickList) => Basis.ReportInfo(Msg.Ready, pickList.GetItem<SOPicker>().PickListNbr);

			protected override void SetNextState()
			{
				if (Basis.Remove == false && WSBasis.CanWSPick == false)
					Basis.SetScanState(BuiltinScanStates.Command, WorksheetPicking.Msg.Completed, WSBasis.PickingJob.Current?.PickListNbr ?? WSBasis.Picker.Current.PickListNbr);
				else
					base.SetNextState();
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the picking worksheet number.";
				public const string Ready = "The {0} picking worksheet is loaded and ready to be processed.";
				public const string Missing = "The {0} picking worksheet is not found.";

				public const string InvalidStatus = "The {0} picking worksheet cannot be processed because it has the {1} status.";
				public const string InvalidSite = "The warehouse specified in the {0} picking worksheet differs from the warehouse assigned to the selected cart.";

				public const string PickerPositionMissing = "The picker slot {0} is not found in the {1} picking worksheet.";
				public const string PickerPositionOccupied = "The picker slot {0} is already assigned to another user in the {1} picking worksheet.";
			}
			#endregion
		}

		public abstract class ToteState : PickPackShip.EntityState<INTote>
		{
			public WorksheetPicking WSBasis => Get<WorksheetPicking>();

			protected override INTote GetByBarcode(string barcode) => INTote.UK.Find(Basis, Basis.SiteID, barcode);

			protected override Validation Validate(INTote tote)
			{
				if (tote.Active == false)
					return Validation.Fail(Msg.Inactive, tote.ToteCD);

				return base.Validate(tote);
			}

			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Ready = "The {0} tote is selected.";
				public const string Missing = "The {0} tote is not found.";
				public const string Inactive = "The {0} tote is inactive.";
			}
			#endregion
		}

		public sealed class AssignToteState : ToteState
		{
			public const string Value = "ASST";
			public class value : BqlString.Constant<value> { public value() : base(AssignToteState.Value) { } }

			private string shipmentJustAssignedWithTote;
			private SOPickerToShipmentLink NextShipmentWithoutTote => WSBasis.ShipmentsOfPicker.SelectMain().FirstOrDefault(s => s.ToteID == null);

			public override string Code => Value;
			protected override string StatePrompt => Basis.Localize(Msg.Prompt, NextShipmentWithoutTote?.ShipmentNbr);

			protected override bool IsStateSkippable() => NextShipmentWithoutTote == null;

			protected override AbsenceHandling.Of<INTote> HandleAbsence(string barcode)
			{
				bool TryAssignTotesFromCart()
				{
					INCart cart =
						SelectFrom<INCart>.
						InnerJoin<INSite>.On<INCart.FK.Site>.
						Where<
							INCart.siteID.IsEqual<WMSScanHeader.siteID.FromCurrent>.
							And<INCart.cartCD.IsEqual<@P.AsString>>.
							And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
						View.Select(Basis, barcode);
					if (cart == null)
						return false;

					var shipmentsOfPicker = WSBasis.ShipmentsOfPicker.SelectMain();
					if (shipmentsOfPicker.Any(s => s.ToteID != null))
					{
						Basis.Reporter.Error(Msg.ToteAlreadyAssignedCannotAssignCart, cart.CartCD);
						return true;
					}

					bool cartIsBusy =
						SelectFrom<SOPickerToShipmentLink>.
						InnerJoin<SOPickingWorksheet>.On<SOPickerToShipmentLink.FK.Worksheet>.
						InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
						InnerJoin<INCart>.On<INTote.FK.Cart>.
						InnerJoin<SOShipment>.On<SOPickerToShipmentLink.FK.Shipment>.
						Where<
							SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.wave>.
							And<SOShipment.confirmed.IsEqual<False>>.
							And<INCart.siteID.IsEqual<@P.AsInt>>.
							And<INCart.cartID.IsEqual<@P.AsInt>>>.
						View.ReadOnly.Select(Basis, cart.SiteID, cart.CartID).Any();
					if (cartIsBusy)
					{
						Basis.Reporter.Error(PPSCartSupport.CartState.Msg.IsOccupied, cart.CartCD);
						return true;
					}

					var totes = INTote.FK.Cart.SelectChildren(Basis, cart).Where(t => t.Active == true).ToArray();
					if (shipmentsOfPicker.Length > totes.Length)
					{
						Basis.Reporter.Error(Msg.TotesAreNotEnoughInCart, cart.CartCD);
						return true;
					}

					foreach (var (link, tote) in shipmentsOfPicker.Zip(totes, (link, tote) => (link, tote)))
					{
						link.ToteID = tote.ToteID;
						WSBasis.ShipmentsOfPicker.Update(link);
					}
					WSBasis.Picker.Current.CartID = cart.CartID;
					WSBasis.Picker.UpdateCurrent();

					Basis.SaveChanges();

					if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSup)
						cartSup.CartID = cart.CartID;

					Basis.DispatchNext(Msg.TotesFromCartAreAssigned, shipmentsOfPicker.Length, cart.CartCD);
					return true;
				}

				if (TryAssignTotesFromCart())
					return AbsenceHandling.Done;

				return base.HandleAbsence(barcode);
			}

			protected override Validation Validate(INTote tote)
			{
				if (Basis.HasFault(tote, base.Validate, out var fault))
					return fault;

				if (tote.AssignedCartID != null)
					return Validation.Fail(Msg.CannotBeUsedSeparatly, tote.ToteCD);

				bool toteIsBusy =
					SelectFrom<SOPickerToShipmentLink>.
					InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
					InnerJoin<SOShipment>.On<SOPickerToShipmentLink.FK.Shipment>.
					Where<
						INTote.siteID.IsEqual<@P.AsInt>.
						And<INTote.toteID.IsEqual<@P.AsInt>>.
						And<SOShipment.confirmed.IsEqual<False>>>.
					View.Select(Basis, tote.SiteID, tote.ToteID).Any();
				if (toteIsBusy)
					return Validation.Fail(Msg.Busy, tote.ToteCD);

				return Validation.Ok;
			}

			protected override void Apply(INTote tote)
			{
				var shipmentLink = NextShipmentWithoutTote;
				shipmentJustAssignedWithTote = shipmentLink.ShipmentNbr;

				shipmentLink.ToteID = tote.ToteID;
				WSBasis.ShipmentsOfPicker.Update(shipmentLink);

				WSBasis.AssignUser(startPicking: false);
				Basis.SaveChanges();
			}

			protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD, shipmentJustAssignedWithTote);

			protected override void SetNextState()
			{
				if (NextShipmentWithoutTote != null)
					Basis.SetScanState<AssignToteState>(); // set the same state to change the prompt message
				else
					base.SetNextState();
			}

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ToteState.Msg
			{
				public const string Prompt = "Scan the tote barcode for the {0} shipment.";
				public new const string Ready = "The {0} tote is selected for the {1} shipment.";

				public const string CannotBeUsedSeparatly = "The {0} tote cannot be used separately from the cart.";
				public const string Busy = "The {0} tote cannot be selected because it is already assigned to another shipment.";

				public const string ToteAlreadyAssignedCannotAssignCart = "Totes from the {0} cart cannot be auto assigned to the pick list because it already has manual assignments.";
				public const string TotesAreNotEnoughInCart = "There are not enough active totes in the {0} cart to assign them to all of the shipments of the pick list.";
				public const string TotesFromCartAreAssigned = "The {0} first totes from the {1} cart were automatically assigned to the shipments of the pick list.";
			}
			#endregion
		}
		#endregion

		#region Commands
		public sealed class ConfirmPickListCommand : PickPackShip.ScanCommand
		{
			public override string Code => "CONFIRM*PICK";
			public override string ButtonName => "scanConfirmPickList";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable;

			protected override bool Process()
			{
				Basis.Get<Logic>().ConfirmPickList();
				return true;
			}

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public virtual void ConfirmPickList()
				{
					if (WSBasis.PickListOfPicker.SelectMain().All(s => s.PickedQty == 0))
					{
						Basis.ReportError(Msg.CannotBeConfirmed);
					}
					else if (Basis.Info.Current.MessageType != ScanMessageTypes.Warning && WSBasis.PickListOfPicker.SelectMain().Any(s => s.PickedQty < s.Qty))
					{
						if (Basis.CannotConfirmPartialShipments)
							Basis.ReportError(Msg.CannotBeConfirmedInPart);
						else
							Basis.ReportWarning(Msg.ShouldNotBeConfirmedInPart);
					}
					else
						ConfirmPickList(sortingLocationID: null);
				}

				public virtual void ConfirmPickList(int? sortingLocationID)
				{
					SOPickingWorksheet worksheet = WSBasis.Worksheet.Current;
					SOPicker picker = WSBasis.Picker.Current;
					SOPickingJob job = WSBasis.PickingJob.Current;

					Basis.Reset(fullReset: false);

					Basis
					.WaitFor<SOPicker>((basis, doc) => ConfirmPickListHandler(worksheet, doc, sortingLocationID))
					.WithDescription(Msg.InProcess, job?.PickListNbr ?? picker.PickListNbr)
					.ActualizeDataBy((basis, doc) => SOPicker.PK.Find(basis, doc))
					.OnSuccess(ConfigureOnSuccessAction)
					.OnFail(x => x.Say(Msg.Fail))
					.BeginAwait(picker);
				}

				public virtual void ConfigureOnSuccessAction(ScanLongRunAwaiter<PickPackShip, SOPicker>.IResultProcessor onSuccess)
				{
					onSuccess
						.Say(Msg.Success)
						.ChangeStateTo<WorksheetPicking.PickListState>();
				}

				protected static void ConfirmPickListHandler(SOPickingWorksheet worksheet, SOPicker pickList, int? sortingLocationID)
				{
					using (var ts = new PXTransactionScope())
					{
						PickPackShip.WithSuppressedRedirects(() =>
						{
							var wsGraph = PXGraph.CreateInstance<SOPickingWorksheetReview>();
							wsGraph.PickListConfirmation.ConfirmPickList(pickList, sortingLocationID);
							wsGraph.PickListConfirmation.FulfillShipmentsAndConfirmWorksheet(worksheet);
						});
						ts.Complete();
					}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Confirm Pick List";
				public const string InProcess = "The {0} pick list is being confirmed.";
				public const string Success = "The pick list has been successfully confirmed.";
				public const string Fail = "The pick list confirmation failed.";

				public const string CannotBeConfirmed = "The pick list cannot be confirmed because no items have been picked.";
				public const string CannotBeConfirmedInPart = "The pick list cannot be confirmed because it is not complete.";
				public const string ShouldNotBeConfirmedInPart = "The pick list is incomplete and should not be confirmed. Do you want to confirm the pick list?";
			}
			#endregion
		}

		public sealed class PackAllIntoBoxCommand : PickPackShip.ScanCommand
		{
			private const string ActionName = "scanPackAllIntoBox";

			public override string Code => "PACK*ALL*INTO*BOX";
			public override string ButtonName => ActionName;
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable && Basis.Get<PickPackShip.PackMode.Logic>().With(pack => pack.CanPack && pack.SelectedPackage != null);

			protected override bool Process() => Get<Logic>().PutAllIntoBox();

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public virtual bool PutAllIntoBox()
				{
					var packMode = Basis.Get<PickPackShip.PackMode.Logic>();
					var packConfirm = Basis.Get<PickPackShip.PackMode.ConfirmState.Logic>();

					var packageDetail = packMode.SelectedPackage;
					var packedSplits = packMode.PickedForPack.SelectMain().Where(r => packConfirm.TargetQty(r) > r.PackedQty);

					bool anyChanged = false;
					foreach (var packedSplit in packedSplits)
					{
						decimal currentQty = packConfirm.TargetQty(packedSplit).Value - packedSplit.PackedQty.Value;
						anyChanged |= packConfirm.PackSplit(packedSplit, packageDetail, currentQty);
					}

					if (anyChanged)
					{
						Basis.EnsureShipmentUserLink();
						packMode.PackageLineNbrUI = packMode.PackageLineNbr;

						Basis.SaveChanges();
						Basis.SetDefaultState();

						return true;
					}

					return false;
				}

				protected virtual void _(Events.RowSelected<ScanHeader> args) => Basis.Graph.Actions[ActionName]?.SetVisible(false);
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Pack All Into One Box";
			}
			#endregion
		}
		#endregion

		#region Decoration
		public virtual void InjectLocationPresenceValidation(WMSBase.LocationState locationState)
		{
			locationState.Intercept.Validate.ByAppend((basis, location) =>
				basis.Get<WorksheetPicking>().IsLocationMissing(location, out var error) ? error : Validation.Ok);
		}

		public virtual void InjectItemPresenceValidation(WMSBase.InventoryItemState inventoryState)
		{
			inventoryState.Intercept.Validate.ByAppend((basis, item) =>
				basis.Get<WorksheetPicking>().IsItemMissing(item, out var error) ? error : Validation.Ok);
		}

		public virtual void InjectLotSerialPresenceValidation(WMSBase.LotSerialState lotSerialState)
		{
			lotSerialState.Intercept.Validate.ByAppend((basis, lotSerialNbr) =>
				basis.Get<WorksheetPicking>().IsLotSerialMissing(lotSerialNbr, out var error) ? error : Validation.Ok);
		}

		public virtual void InjectExpireDateForWSPickDeactivationOnAlreadyEnteredLot(WMSBase.ExpireDateState expireDateState)
		{
			expireDateState.Intercept.IsStateActive.ByConjoin(basis =>
				basis.SelectedLotSerialClass?.LotSerAssign == INLotSerAssign.WhenUsed &&
				basis.Get<WorksheetPicking>().PickListOfPicker.SelectMain().Any(t =>
					t.IsUnassigned == true ||
					t.LotSerialNbr == basis.LotSerialNbr && t.PickedQty == 0));
		}

		public virtual void InjectShipmentAbsenceHandlingByWorksheet(PickPackShip.PickMode.ShipmentState pickShipment)
		{
			pickShipment.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
			{
				if (barcode.Contains("/"))
				{
					(string worksheetNbr, string pickerNbrStr) = barcode.Split('/');
					if (int.TryParse(pickerNbrStr, out int _))
					{
						if (SOPickingWorksheet.PK.Find(basis, worksheetNbr) is SOPickingWorksheet sheet)
						{
							if (basis.Get<WorksheetPicking>().FindModeForWorksheet(sheet) is IScanMode mode)
							{
								if (basis.FindMode(mode.Code).TryProcessBy<PickListState>(barcode))
								{
									basis.SetScanMode(mode.Code);
									basis.FindState<PickListState>().Process(barcode);
									return AbsenceHandling.Done;
								}
							}
						}
					}
				}

				return AbsenceHandling.Skipped;
			});
		}

		public virtual void InjectShipmentValidationForSeparatePicking(PickPackShip.PickMode.ShipmentState pickShipment)
		{
			pickShipment.Intercept.Validate.ByAppend((basis, shipment) =>
				shipment.CurrentWorksheetNbr != null
					? Validation.Fail(Msg.ShipmentCannotBePickedSeparately, shipment.ShipmentNbr, shipment.CurrentWorksheetNbr)
					: Validation.Ok);
		}

		public virtual void InjectShipmentAbsenceHandlingByTote(PickPackShip.PackMode.ShipmentState packShipment)
		{
			packShipment.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
			{
				SOShipment shipmentInTote =
					SelectFrom<SOShipment>.
					InnerJoin<SOPickerToShipmentLink>.On<SOPickerToShipmentLink.FK.Shipment>.
					InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
					InnerJoin<SOPicker>.On<SOPickerToShipmentLink.FK.Picker>.
					InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
					Where<
						INTote.toteCD.IsEqual<@P.AsString>.
						And<SOPickingWorksheet.worksheetType.IsIn<
							SOPickingWorksheet.worksheetType.wave,
							SOPickingWorksheet.worksheetType.single>>.
						And<SOPicker.confirmed.IsEqual<True>>.
						And<SOShipment.picked.IsEqual<True>>.
						And<SOShipment.confirmed.IsEqual<False>>.
						And<Not<Exists<
							SelectFrom<SOShipLineSplit>.
							Where<
								SOShipLineSplit.shipmentNbr.IsEqual<SOShipment.shipmentNbr>.
								And<SOShipLineSplit.packedQty.IsNotEqual<decimal0>>>>>>>.
					View.Select(basis, barcode).TopFirst;

				if (shipmentInTote != null)
					return AbsenceHandling.ReplaceWith(shipmentInTote);

				return AbsenceHandling.Skipped;
			});
		}

		public virtual void InjectValidationPickFirst(PickPackShip.ShipmentState refNbrState)
		{
			refNbrState.Intercept.Validate.ByAppend((basis, shipment) =>
				shipment.CurrentWorksheetNbr != null && shipment.Picked == false
					? Validation.Fail(PickPackShip.PackMode.ShipmentState.Msg.ShouldBePickedFirst, shipment.ShipmentNbr)
					: Validation.Ok);
		}

		public virtual void InjectPackAllToBoxCommand(PickPackShip.PackMode pack)
		{
			pack.Intercept.CreateCommands.ByAppend(() => new[] { new PackAllIntoBoxCommand() });
		}
		#endregion

		#region Overrides
		/// Overrides <see cref="PickPackShip.DocumentIsConfirmed"/>
		[PXOverride]
		public virtual bool get_DocumentIsConfirmed(Func<bool> base_DocumentIsConfirmed) => IsWorksheetMode(Basis.CurrentMode?.Code)
			? PickList?.Confirmed == true
			: base_DocumentIsConfirmed();

		/// Overrides <see cref="WarehouseManagementSystem{TSelf, TGraph}.DocumentLoaded"/>
		[PXOverride]
		public virtual bool get_DocumentLoaded(Func<bool> base_DocumentLoaded) => IsWorksheetMode(Basis.CurrentMode?.Code)
			? WorksheetNbr != null
			: base_DocumentLoaded();

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState"/>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (IsWorksheetMode(state.ModeCode))
			{
				if (state is WMSBase.LocationState locationState)
				{
					InjectLocationPresenceValidation(locationState);
				}
				else if (state is WMSBase.InventoryItemState itemState)
				{
					InjectItemPresenceValidation(itemState);
				}
				else if (state is WMSBase.LotSerialState lotSerialState)
				{
					Basis.InjectLotSerialDeactivationOnDefaultLotSerialOption(lotSerialState, isEntranceAllowed: true);
					InjectLotSerialPresenceValidation(lotSerialState);
				}
				else if (state is WMSBase.ExpireDateState expireState)
				{
					InjectExpireDateForWSPickDeactivationOnAlreadyEnteredLot(expireState);
				}
			}
			else
			{
				if (state is PickPackShip.PickMode.ShipmentState pickShipment)
				{
					InjectShipmentAbsenceHandlingByWorksheet(pickShipment);
					InjectShipmentValidationForSeparatePicking(pickShipment);
				}
				else if (state is PickPackShip.PackMode.ShipmentState packShipment)
				{
					InjectShipmentAbsenceHandlingByTote(packShipment);
					InjectValidationPickFirst(packShipment);
				}
				else if (state is PickPackShip.ShipMode.ShipmentState shipShipment)
				{
					InjectValidationPickFirst(shipShipment);
				}
			}

			return state;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode(ScanMode{TSelf})"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> DecorateScanMode(ScanMode<PickPackShip> original, Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
		{
			var mode = base_DecorateScanMode(original);

			if (mode is PickPackShip.PackMode pack)
				InjectPackAllToBoxCommand(pack);

			return mode;
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public abstract class Msg : PickPackShip.Msg
		{
			public const string Completed = "The {0} pick list is picked.";

			public const string ShipmentCannotBePickedSeparately = "The {0} shipment cannot be picked individually because the shipment is assigned to the {1} picking worksheet.";

			public const string InventoryMissingInPickList = "The {0} inventory item is not present in the pick list.";
			public const string LocationMissingInPickList = "The {0} location is not present in the pick list.";
			public const string LotSerialMissingInPickList = "The {0} lot/serial number is not present in the pick list.";

			public const string NothingToPick = "No items to pick.";
			public const string NothingToRemove = "No items to remove from the shipment.";

			public const string Overpicking = "The picked quantity cannot be greater than the quantity in the pick list line.";
			public const string Underpicking = "The picked quantity cannot become negative.";

			public const string MissingLotSerailOnLocation = "The {1} lot/serial number does not exist for the {0}​​​​​​ item in the {3} location of {2}​​​​​​.";
			public const string ExceededAvailability = "The picked quantity of the {0} {1} cannot be greater than the available quantity in the {3}​​​​​ location of {2}​​​​​​.";
		}
		#endregion

		#region Attached Fields
		[PXUIField(Visible = false)]
		public class ShowPickWS : PickPackShip.FieldAttached.To<ScanHeader>.AsBool.Named<ShowPickWS>
		{
			public static bool IsActive() => WorksheetPicking.IsActive();
			public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowPickTab == true && Base.WMS.Get<WorksheetPicking>().IsWorksheetMode(row.Mode);
		}

		[PXUIField(DisplayName = PickPackShip.Msg.Fits)]
		public class FitsWS : PickPackShip.FieldAttached.To<SOPickerListEntry>.AsBool.Named<FitsWS>
		{
			public static bool IsActive() => WorksheetPicking.IsActive();
			public override bool? GetValue(SOPickerListEntry row)
			{
				bool fits = true;
				if (Base.WMS.LocationID != null)
					fits &= Base.WMS.LocationID == row.LocationID;
				if (Base.WMS.InventoryID != null)
					fits &= Base.WMS.InventoryID == row.InventoryID && Base.WMS.SubItemID == row.SubItemID;
				if (Base.WMS.LotSerialNbr != null)
					fits &= Base.WMS.LotSerialNbr == row.LotSerialNbr || Base.WMS.IsEnterableLotSerial(isForIssue: true) && row.PickedQty == 0;
				return fits;
			}
		}
		#endregion

		#region Extensibility
		public abstract class ScanExtension : PXGraphExtension<WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => WorksheetPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
		}

		public abstract class ScanExtension<TTargetExtension> : PXGraphExtension<TTargetExtension, WorksheetPicking, PickPackShip, PickPackShip.Host>
			where TTargetExtension : PXGraphExtension<WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => WorksheetPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
			public TTargetExtension Target => Base3;
		}
		#endregion
	}

	public sealed class WorksheetScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => WorksheetPicking.IsActive();

		#region WorksheetNbr
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Worksheet Nbr.", Enabled = false, Visible = false)]
		[PXSelector(typeof(SOPickingWorksheet.worksheetNbr))]
		public string WorksheetNbr { get; set; }
		public abstract class worksheetNbr : BqlString.Field<worksheetNbr> { }
		#endregion
		#region PickerNbr
		[PXInt]
		[PXUIField(DisplayName = "Picker Nbr.", Enabled = false, Visible = false)]
		public Int32? PickerNbr { get; set; }
		public abstract class pickerNbr : BqlInt.Field<pickerNbr> { }
		#endregion
	}

	public class ShipmentAndWorksheetBorrowedNoteAttribute : PXNoteAttribute
	{
		protected override string GetEntityType(PXCache cache, Guid? noteId) => cache.Graph is PickPackShip.Host pps
			? IsWorksheet(pps, noteId)
				? typeof(SOPickingWorksheet).FullName
				: typeof(SOShipment).FullName
			: base.GetEntityType(cache, noteId);

		protected override string GetGraphType(PXGraph graph) => graph is PickPackShip.Host pps
			? IsWorksheet(pps)
				? typeof(SOPickingWorksheetReview).FullName
				: typeof(SOShipmentEntry).FullName
			: base.GetGraphType(graph);

		protected virtual bool IsWorksheet(PickPackShip.Host pps)
			=> WorksheetPicking.IsActive() && pps.WMS.Get<WorksheetPicking>().IsWorksheetMode(pps.WMS.CurrentMode.Code);
		protected virtual bool IsWorksheet(PickPackShip.Host pps, Guid? noteID)
			=> WorksheetPicking.IsActive() && noteID != null &&
				SelectFrom<SOPickingWorksheet>.
				Where<
					SOPickingWorksheet.worksheetType.IsIn<
						SOPickingWorksheet.worksheetType.wave,
						SOPickingWorksheet.worksheetType.batch>.
					And<SOPickingWorksheet.noteID.IsEqual<@P.AsGuid>>>.
				View.Select(pps, noteID).AsEnumerable().Any();
	}
}
