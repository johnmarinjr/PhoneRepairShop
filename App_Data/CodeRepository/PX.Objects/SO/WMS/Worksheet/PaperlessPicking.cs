using System;
using System.Linq;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class PaperlessPicking : WorksheetPicking.ScanExtension
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.wMSPaperlessPicking>();

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.CreateScanModes"/>
		[PXOverride]
		public virtual IEnumerable<ScanMode<PickPackShip>> CreateScanModes(Func<IEnumerable<ScanMode<PickPackShip>>> base_CreateScanModes)
		{
			foreach (var mode in base_CreateScanModes())
				yield return mode;

			yield return new SinglePickMode();
		}

		public sealed class SinglePickMode : PickPackShip.ScanMode
		{
			public const string Value = "SNGL";
			public class value : BqlString.Constant<value> { public value() : base(SinglePickMode.Value) { } }

			public PaperlessPicking PPBasis => Get<PaperlessPicking>();

			public override string Code => Value;
			public override string Description => Msg.DisplayName;

			protected override bool IsModeActive() => Basis.HasPick;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickListState();
				yield return new WorksheetPicking.AssignToteState();
				yield return new WMSBase.LocationState();
				yield return new WMSBase.InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true };
				yield return new WMSBase.LotSerialState();
				yield return new WMSBase.ExpireDateState() { IsForIssue = true };
				yield return new ConfirmState();

				// directly set states
				yield return new WarehouseState();
				yield return new NearestLocationState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.ForkBy(basis => basis.Remove != true)
					.PositiveBranch(pfl => pfl
						.From<PickListState>()
						.NextTo<WorksheetPicking.AssignToteState>()
						.NextTo<WMSBase.LocationState>()
						.NextTo<WMSBase.InventoryItemState>()
						.NextTo<WMSBase.LotSerialState>()
						.NextTo<WMSBase.ExpireDateState>())
					.NegativeBranch(nfl => nfl
						.From<PickListState>()
						.NextTo<WMSBase.InventoryItemState>()
						.NextTo<WMSBase.LotSerialState>()
						.NextTo<WMSBase.LocationState>()));
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new WMSBase.RemoveCommand()
					.Intercept.IsEnabled.ByConjoin(basis => !(basis.CurrentState is WorksheetPicking.AssignToteState));
				yield return new WMSBase.QtySupport.SetQtyCommand();
				yield return new WorksheetPicking.ConfirmPickListCommand();
				yield return new TakeNextPickListCommand();
				yield return new ConfirmPickListAndTakeNextCommand();
				yield return new ConfirmLineQtyCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<WMSBase.LocationState>(when: fullReset);
				Clear<WMSBase.InventoryItemState>(when: fullReset);
				Clear<WMSBase.LotSerialState>();
				Clear<WMSBase.ExpireDateState>();
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Paperless Pick";
			}
			#endregion
		}

		#region State
		public PaperlessScanHeader PPHeader => Basis.Header.Get<PaperlessScanHeader>() ?? new PaperlessScanHeader();
		public ValueSetter<ScanHeader>.Ext<PaperlessScanHeader> PPSetter => Basis.HeaderSetter.With<PaperlessScanHeader>();

		#region LastVisitedLocationID
		public int? LastVisitedLocationID
		{
			get => PPHeader.LastVisitedLocationID;
			set => PPSetter.Set(h => h.LastVisitedLocationID, value);
		}
		#endregion
		#region PathInversedDirection
		public bool? PathInversedDirection
		{
			get => PPHeader.PathInversedDirection;
			set => PPSetter.Set(h => h.PathInversedDirection, value);
		}
		#endregion
		#region WantedLineNbr
		public int? WantedLineNbr
		{
			get => PPHeader.WantedLineNbr;
			set => PPSetter.Set(h => h.WantedLineNbr, value);
		}
		#endregion
		#region SingleShipmentNbr
		public string SingleShipmentNbr
		{
			get => PPHeader.SingleShipmentNbr;
			set => PPSetter.Set(h => h.SingleShipmentNbr, value);
		}
		#endregion
		#region IgnoredPickingJobs
		public ISet<int> IgnoredPickingJobs => PPHeader.IgnoredPickingJobs;
		#endregion
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			if (e.Row == null)
				return;

			e.Cache.AdjustUI()
				.For<PaperlessScanHeader.singleShipmentNbr>(a => a.Visible = e.Row.Mode == SinglePickMode.Value)
				.For<WMSScanHeader.refNbr>(a => a.Visible &= e.Row.Mode != SinglePickMode.Value);
		}
		#endregion

		#region Logic
		public virtual bool ReturnCurrentJobToQueue()
		{
			bool anyChanged = false;

			if (WSBasis.PickingJob.Current != null && WSBasis.PickingJob.Current.ActualAssigneeID == Graph.Accessinfo.UserID)
			{
				IgnoredPickingJobs.Add(WSBasis.PickingJob.Current.JobID.Value);

				WSBasis.PickingJob.Current.ActualAssigneeID = null;
				if (WSBasis.PickingJob.Current.Status == SOPickingJob.status.Picking)
					WSBasis.PickingJob.Current.Status = SOPickingJob.status.Reenqueued;
				WSBasis.PickingJob.UpdateCurrent();
				anyChanged = true;
			}

			foreach (SOPickerToShipmentLink link in WSBasis.ShipmentsOfPicker.Select())
			{
				if (link.ToteID != null)
				{
					link.ToteID = null;
					WSBasis.ShipmentsOfPicker.Update(link);
					anyChanged = true;
				}
			}

			if (WSBasis.Picker.Current != null && WSBasis.Picker.Current.UserID == Graph.Accessinfo.UserID)
			{
				WSBasis.Picker.Current.UserID = null;
				WSBasis.Picker.UpdateCurrent();
				anyChanged = true;
			}

			if (WSBasis.Worksheet.Current != null && WSBasis.Worksheet.Current.Status == SOPickingWorksheet.status.Picking)
			{
				SOPicker pickingPicker =
					SelectFrom<SOPicker>.
					Where<
						SOPicker.userID.IsNotNull.
						And<SOPicker.pickerNbr.IsNotEqual<SOPicker.pickerNbr.FromCurrent>>.
						And<SOPicker.FK.Worksheet.SameAsCurrent>>.
					View.ReadOnly.Select(Basis);

				if (pickingPicker == null)
				{
					WSBasis.Worksheet.Current.Status = SOPickingWorksheet.status.Open;
					WSBasis.Worksheet.UpdateCurrent();
					anyChanged = true;
				}
			}

			return anyChanged;
		}

		public virtual int? GetNextWantedLineNbr()
		{
			var notPickedLines = WSBasis.PickListOfPicker.SelectMain().Where(ent => ent.PickedQty < ent.Qty && ent.ForceCompleted != true);
			return notPickedLines.FirstOrDefault()?.EntryNbr;
		}

		public virtual SOPickerListEntry GetSplitForRemoval()
		{
			if (Basis.Remove != true || Basis.InventoryID == null)
				return null;

			var notPickedLines = WSBasis.PickListOfPicker.SelectMain().Where(ent => ent.PickedQty > 0 && ent.ForceCompleted != true && WSBasis.IsSelectedSplit(ent));
			return notPickedLines.FirstOrDefault();
		}

		public virtual bool NeedInversedDirection(SOPicker picker, int nearestLocationID)
		{
			var firstNotPickedLine = (PXResult<SOPickerListEntry, INLocation>)
				SelectFrom<SOPickerListEntry>.
				InnerJoin<INLocation>.On<SOPickerListEntry.FK.Location>.
				InnerJoin<InventoryItem>.On<SOPickerListEntry.FK.InventoryItem>.
				Where<
					SOPickerListEntry.pickedQty.IsLess<SOPickerListEntry.qty>.
					And<SOPickerListEntry.forceCompleted.IsNotEqual<True>>.
					And<SOPickerListEntry.FK.Picker.SameAsCurrent>>.
				OrderBy<
					INLocation.pathPriority.Asc,
					INLocation.locationCD.Asc,
					InventoryItem.inventoryID.Asc,
					SOPickerListEntry.lotSerialNbr.Asc>.
				View.SelectSingleBound(Basis, new[] { picker });

			var lastNotPickedLine = (PXResult<SOPickerListEntry, INLocation>)
				SelectFrom<SOPickerListEntry>.
				InnerJoin<INLocation>.On<SOPickerListEntry.FK.Location>.
				InnerJoin<InventoryItem>.On<SOPickerListEntry.FK.InventoryItem>.
				Where<
					SOPickerListEntry.pickedQty.IsLess<SOPickerListEntry.qty>.
					And<SOPickerListEntry.forceCompleted.IsNotEqual<True>>.
					And<SOPickerListEntry.FK.Picker.SameAsCurrent>>.
				OrderBy<
					INLocation.pathPriority.Desc,
					INLocation.locationCD.Desc,
					InventoryItem.inventoryID.Desc,
					SOPickerListEntry.lotSerialNbr.Desc>.
				View.SelectSingleBound(Basis, new[] { picker });

			if (firstNotPickedLine != null && lastNotPickedLine != null)
			{
				int distanceToFirst = GetDistanceBetweenLocations(firstNotPickedLine.GetItem<SOPickerListEntry>().LocationID, nearestLocationID);
				int distanceToLast = GetDistanceBetweenLocations(lastNotPickedLine.GetItem<SOPickerListEntry>().LocationID, nearestLocationID);
				return distanceToLast < distanceToFirst;
			}

			return false;
		}

		public virtual int GetDistanceBetweenLocations(int? leftlocationID, int? rightLocationID)
		{
			var leftLocation = INLocation.PK.Find(Basis, leftlocationID);
			var rightLocation = INLocation.PK.Find(Basis, rightLocationID);

			int distance = Math.Abs((leftLocation.PathPriority - rightLocation.PathPriority) ?? 0);
			return distance;
		} 

		public virtual SOPickerListEntry GetWantedSplit() => WantedLineNbr == null ? null : WSBasis.PickListOfPicker.Search<SOPickerListEntry.entryNbr>(WantedLineNbr);

		public virtual string PromptWantedItem()
		{
			SOPickerListEntry wantedSplit = GetWantedSplit();
			if (wantedSplit != null)
			{
				var inventory = InventoryItem.PK.Find(Basis, wantedSplit.InventoryID);
				var lotSerialClass = Basis.GetLotSerialClassOf(inventory);
				bool noCertainLotSerial =
					lotSerialClass == null ||
					lotSerialClass.LotSerTrack == INLotSerTrack.NotNumbered ||
					lotSerialClass.LotSerAssign == INLotSerAssign.WhenUsed ||
					lotSerialClass.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable;
				string msg = Basis.DefaultLocation
					? noCertainLotSerial
						? Msg.PickItemFromLocationNoLotSerial
						: Msg.PickItemFromLocationWithLotSerial
					: noCertainLotSerial
						? Msg.PickItemNoLotSerial
						: Msg.PickItemWithLotSerial;
				return Basis.Localize(
					msg,
					wantedSplit.Qty - wantedSplit.PickedQty,
					wantedSplit.UOM,
					Basis.SightOf<SOPickerListEntry.inventoryID>(wantedSplit),
					wantedSplit.LotSerialNbr,
					Basis.SightOf<SOPickerListEntry.locationID>(wantedSplit));
			}

			return null;
		}

		public virtual string PromptWantedLocation()
		{
			SOPickerListEntry wantedSplit = GetWantedSplit();
			if (wantedSplit != null)
				return Basis.Localize(Msg.GoToLocation, Basis.SightOf<SOPickerListEntry.locationID>(wantedSplit));

			return null;
		}

		public virtual string PromptLocationForRemoval()
		{
			SOPickerListEntry splitForRemoval = GetSplitForRemoval();
			if (splitForRemoval != null)
				return Basis.Localize(Msg.GoToLocation, Basis.SightOf<SOPickerListEntry.locationID>(splitForRemoval));

			return null;
		}
		#endregion

		#region States
		public sealed class PickListState : WorksheetPicking.PickListState
		{
			public PaperlessPicking PPBasis => Basis.Get<PaperlessPicking>();

			protected override string WorksheetType => SOPickingWorksheet.worksheetType.Single;

			protected override string StatePrompt => Msg.Prompt;

			protected override AbsenceHandling.Of<PXResult<SOPickingWorksheet, SOPicker>> HandleAbsence(string barcode)
			{
				if (barcode.Contains("/") == false)
				{
					var singleWorksheet = (PXResult<SOPickingWorksheet, SOPicker>)
						SelectFrom<SOPickingWorksheet>.
						InnerJoin<SOPicker>.On<SOPicker.FK.Worksheet>.
						InnerJoin<SOShipment>.On<SOPickingWorksheet.FK.SingleShipment>.
						InnerJoin<INSite>.On<SOShipment.FK.Site>.
						LeftJoin<Customer>.On<SOShipment.FK.Customer>.SingleTableOnly.
						Where<
							SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>.
							And<SOShipment.shipmentNbr.IsEqual<@P.AsString>>.
							And<MatchUserFor<INSite>>.
							And<
								Customer.bAccountID.IsNull.
								Or<MatchUserFor<Customer>>>>.
						View.ReadOnly.Select(Basis, barcode);

					if (singleWorksheet != null)
						return AbsenceHandling.ReplaceWith(singleWorksheet);
				}

				return base.HandleAbsence(barcode);
			}

			protected override void Apply(PXResult<SOPickingWorksheet, SOPicker> pickList)
			{
				base.Apply(pickList);
				PPBasis.SingleShipmentNbr = pickList.GetItem<SOPickingWorksheet>().SingleShipmentNbr;
			}

			protected override void ClearState()
			{
				base.ClearState();
				PPBasis.SingleShipmentNbr = null;
			}

			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void ReportSuccess(PXResult<SOPickingWorksheet, SOPicker> pickList) => Basis.ReportInfo(Msg.Ready, pickList.GetItem<SOPickingWorksheet>().SingleShipmentNbr);

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : WorksheetPicking.PickListState.Msg
			{
				public new const string Prompt = "Scan the pick list number or click Next List.";
				public new const string Ready = "The {0} pick list is loaded and ready to be processed.";
				public new const string Missing = "The {0} pick list is not found.";
			}
			#endregion
		}

		public sealed class WarehouseState : WMSBase.WarehouseState
		{
			protected override bool UseDefaultWarehouse => true;
			protected override void SetNextState()
			{
				Basis.SetDefaultState();
				Basis.Get<TakeNextPickListCommand.Logic>().TakeNext();
			}
		}

		public sealed class NearestLocationState : PickPackShip.EntityState<INLocation>
		{
			public const string Value = "NLOC";
			public class value : BqlString.Constant<value> { public value() : base(NearestLocationState.Value) { } }

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			public PaperlessPicking PPBasis => Basis.Get<PaperlessPicking>();

			protected override INLocation GetByBarcode(string barcode)
			{
				return
					SelectFrom<INLocation>.
					Where<
						INLocation.siteID.IsEqual<@P.AsInt>.
						And<INLocation.locationCD.IsEqual<@P.AsString>>>.
					View.Select(Basis, Basis.SiteID, barcode);
			}

			protected override Validation Validate(INLocation location)
			{
				if (location.Active != true)
					return Validation.Fail(IN.Messages.InactiveLocation, location.LocationCD);

				return Validation.Ok;
			}

			protected override void Apply(INLocation location) => PPBasis.LastVisitedLocationID = location.LocationID;
			protected override void ClearState() => PPBasis.LastVisitedLocationID = null;

			protected override void ReportSuccess(INLocation location) { } // if everything is fine then we will get back to the TakeNextPickListCommand execution, it has its own messages
			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(PickPackShip.LocationState.Msg.Missing, barcode, Basis.SightOf<WMSScanHeader.siteID>());

			protected override void SetNextState()
			{
				Basis.SetDefaultState();
				Basis.Get<TakeNextPickListCommand.Logic>().TakeNext();
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the nearest location.";
			}
			#endregion
		}

		public sealed class ConfirmState : PickPackShip.ConfirmationState
		{
			public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
			protected override FlowStatus PerformConfirmation() => Basis.Get<Logic>().Confirm();

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public virtual FlowStatus Confirm()
				{
					bool remove = Basis.Remove == true;

					if (Basis.LocationID == null && !Basis.DefaultLocation)
						Basis.LocationID = PPBasis.LastVisitedLocationID;

					SOPickerListEntry pickedSplit = remove
						? PPBasis.GetSplitForRemoval()
						: PPBasis.GetWantedSplit();

					var confirmResult = WSBasis.ConfirmSplit(pickedSplit, Sign.MinusIf(remove) * Basis.BaseQty);
					if (confirmResult.IsError != false)
						return confirmResult;

					VisitSplit(pickedSplit);

					Basis.ReportInfo(
						remove
							? Msg.InventoryRemoved
							: Msg.InventoryAdded,
						Basis.SightOf<SOPickerListEntry.inventoryID>(pickedSplit), Basis.Qty, pickedSplit.UOM);

					return FlowStatus.Ok.WithDispatchNext;
				}

				public virtual void VisitSplit(SOPickerListEntry pickedSplit)
				{
					bool remove = Basis.Remove == true;

					PPBasis.LastVisitedLocationID = pickedSplit.LocationID;
					PPBasis.WantedLineNbr = PPBasis.GetNextWantedLineNbr();

					SOPickerListEntry nextWantedSplit = PPBasis.GetWantedSplit();

					if (remove || nextWantedSplit == null || pickedSplit.InventoryID != nextWantedSplit.InventoryID)
						Basis.Clear<WMSBase.InventoryItemState>();

					if (remove || nextWantedSplit == null || pickedSplit.LocationID != nextWantedSplit.LocationID)
						Basis.Clear<WMSBase.LocationState>();
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ConfirmationState.Msg
			{
				public const string Prompt = WaveBatchPicking.ConfirmState.Msg.Prompt;

				public const string InventoryAdded = "{0} x {1} {2} has been added to the tote.";
				public const string InventoryRemoved = "{0} x {1} {2} has been removed from the tote.";
			}
			#endregion
		}
		#endregion

		#region Commands
		public sealed class TakeNextPickListCommand : PickPackShip.ScanCommand
		{
			public override string Code => "NEXT*PICKLIST";
			public override string ButtonName => "scanTakeNextPickList";
			public override string DisplayName => Msg.DisplayName;

			protected override bool IsEnabled => Basis.RefNbr == null && (WSBasis.WorksheetNbr == null || Basis.DocumentIsConfirmed || WSBasis.NotStarted);

			public WorksheetPicking WSBasis => Basis.Get<WorksheetPicking>();

			protected override bool Process() => Basis.Get<Logic>().TakeNext();

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public virtual bool TakeNext()
				{
					if (Basis.RefNbr == null && Basis.CurrentMode is PickPackShip.PickMode)
						Basis.SetScanMode<SinglePickMode>();

					if (WSBasis.WorksheetNbr != null && WSBasis.PickingJob.Current?.JobID != null && !Basis.DocumentIsConfirmed)
						PPBasis.IgnoredPickingJobs.Add(WSBasis.PickingJob.Current.JobID.Value);

					if (PPBasis.LastVisitedLocationID == null)
					{
						Basis.ReportInfo(Msg.NearestLocationIsNotSet);

						if (Basis.SiteID == null)
							Basis.SetScanState<WarehouseState>();
						else
							Basis.SetScanState<NearestLocationState>();

						return true;
					}

					if (TryTakeNext(PPBasis.LastVisitedLocationID.Value))
						return true;

					if (PPBasis.IgnoredPickingJobs.Count != 0)
					{
						if (WSBasis.WorksheetNbr != null)
							if (PPBasis.ReturnCurrentJobToQueue())
								Basis.SaveChanges();

						PPBasis.IgnoredPickingJobs.Clear();
						Basis.Reset(fullReset: true);
						Basis.SetDefaultState();
						Basis.ReportInfo(Msg.QueueEnded);
					}
					else
					{
						Basis.ReportInfo(Msg.QueueIsEmpty);
					}
					return true;
				}

				public virtual bool TryTakeNext(int nearestLocationID)
				{
					if (TryTakeIncomplete(nearestLocationID))
						return true;

					if (TryTakeDirectlyAssigned(nearestLocationID))
						return true;

					if (TryTakeFromSharedQueue(nearestLocationID))
						return true;

					return false;
				}

				public virtual bool TryTakeIncomplete(int nearestLocationID)
				{
					var incompleteJobsView = new
						SelectFrom<SOPickingJob>.
						InnerJoin<SOPicker>.On<SOPickingJob.FK.Picker>.
						InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
						Where<
							SOPickingJob.actualAssigneeID.IsEqual<AccessInfo.userID.FromCurrent>.
							And<SOPickingJob.status.IsIn<SOPickingJob.status.enqueued, SOPickingJob.status.reenqueued, SOPickingJob.status.picking>>>.
						OrderBy<
							Desc<TestIf<SOPickingJob.status.IsEqual<SOPickingJob.status.picking>>>>.
						View(Basis);
					ApplyCommonFilters(incompleteJobsView);

					var incomplete = incompleteJobsView.Select();

					foreach (PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> potentialJob in incomplete)
					{
						SOPickingJob job = potentialJob;
						if (PPBasis.IgnoredPickingJobs.Contains(job.JobID.Value))
							continue;

						LoadPickingJob(potentialJob);
						return true;
					}

					return false;
				}

				public virtual bool TryTakeDirectlyAssigned(int nearestLocationID)
				{
					var directlyAssignedView = new
						SelectFrom<SOPickingJob>.
						InnerJoin<SOPicker>.On<SOPickingJob.FK.Picker>.
						InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
						Where<
							SOPickingJob.preferredAssigneeID.IsEqual<AccessInfo.userID.FromCurrent>.
							And<SOPickingJob.status.IsEqual<SOPickingJob.status.assigned>>.
							And<SOPickingJob.priority.IsEqual<@P.AsInt>>>.
						View(Basis);
					ApplyCommonFilters(directlyAssignedView);

					PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> selectedJob = SelectJobFrom(directlyAssignedView, nearestLocationID);
					if (selectedJob != null)
					{
						LoadPickingJob(selectedJob);
						return true;
					}
					return false;
				}

				public virtual bool TryTakeFromSharedQueue(int nearestLocationID)
				{
					var sharedQueueView = new
						SelectFrom<SOPickingJob>.
						InnerJoin<SOPicker>.On<SOPickingJob.FK.Picker>.
						InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
						Where<
							SOPickingJob.preferredAssigneeID.IsNull.
							And<SOPickingJob.actualAssigneeID.IsNull.Or<SOPickingJob.minutesSinceLastModification.IsGreater<minutes15>>>.
							And<SOPickingJob.status.IsIn<SOPickingJob.status.enqueued, SOPickingJob.status.reenqueued>>.
							And<SOPickingJob.priority.IsEqual<@P.AsInt>>>.
						View(Basis);
					ApplyCommonFilters(sharedQueueView);

					PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> selectedJob = SelectJobFrom(sharedQueueView, nearestLocationID);
					if (selectedJob != null)
					{
						LoadPickingJob(selectedJob);
						return true;
					}
					return false;
				}

				protected virtual void ApplyCommonFilters(PXSelectBase<SOPickingJob> command) { }

				protected virtual PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> SelectJobFrom(PXSelectBase<SOPickingJob> queue, int nearestLocationID)
				{
					foreach (int priority in new[] { WMSJob.priority.Urgent, WMSJob.priority.High, WMSJob.priority.Medium, WMSJob.priority.Low })
					{
						var distancesToPickList = new List<(SOPickingJob Job, SOPicker Picker, SOPickingWorksheet Sheet, int Distance)>();
						foreach (PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> potentialJob in queue.Select(priority))
						{
							var (job, picker, sheet) = potentialJob;

							if (PPBasis.IgnoredPickingJobs.Contains(job.JobID.Value))
								continue;

							int distanceToFirst = PPBasis.GetDistanceBetweenLocations(picker.FirstLocationID, nearestLocationID);
							int distanceToLast = PPBasis.GetDistanceBetweenLocations(picker.LastLocationID, nearestLocationID);
							int distance = Math.Min(distanceToFirst, distanceToLast);

							distancesToPickList.Add((job, picker, sheet, distance));
						}

						if (distancesToPickList.Count > 0)
						{
							var selected = distancesToPickList.OrderBy(r => r.Distance).ThenByDescending(r => r.Job.EnqueuedAt).First();
							return new PXResult<SOPickingJob, SOPicker, SOPickingWorksheet>(selected.Job, selected.Picker, selected.Sheet);
						}
					}

					return null;
				}

				protected virtual void LoadPickingJob(PXResult<SOPickingJob, SOPicker, SOPickingWorksheet> selectedJob)
				{
					if (selectedJob == null)
						throw new ArgumentNullException(nameof(selectedJob));

					bool success = false;
					Exception exception = null;

					var (job, picker, sheet) = selectedJob;

					try
					{
						if (WSBasis.FindModeForWorksheet(sheet) is IScanMode mode)
						{
							if (Basis.FindMode(mode.Code).TryProcessBy<WorksheetPicking.PickListState>(picker.PickListNbr))
							{
								// we need to keep the previous pick list so it can be properly returned to picking queue
								var oldPickList = WSBasis.Picker.Current != null
									? new PXResult<SOPickingWorksheet, SOPicker>(WSBasis.Worksheet.Current, WSBasis.Picker.Current)
									: null;

								if (Basis.CurrentMode.Code != mode.Code)
								{
									Basis.SetScanMode(mode.Code); // mode change will cause fullReset and default state
								}
								else
								{
									Basis.Reset(fullReset: true);
									Basis.SetDefaultState();
								}

								if (oldPickList != null)
									WSBasis.SetPickList(oldPickList);

								success = Basis.FindState<WorksheetPicking.PickListState>().Process(picker.PickListNbr);
							}
						}
					}
					catch (Exception ex)
					{
						exception = ex;
					}
					finally
					{
						if (success == false)
						{
							PPBasis.IgnoredPickingJobs.Add(job.JobID.Value); // something went wrong, lets ignore such pick list

							if (exception != null)
								PXTrace.WriteError(exception);
							else if (Basis.Info.Current.MessageType == ScanMessageTypes.Error)
								PXTrace.WriteError(Basis.Info.Current.Message);

							Basis.ReportError(Msg.PickListSkipped, job.PickListNbr);
						}
					}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Next List";
				public const string NearestLocationIsNotSet = "Your current picking location is not defined.";
				public const string QueueIsEmpty = "The picking queue is empty.";
				public const string QueueEnded = "The end of the picking queue has been reached.";
				public const string PickListSkipped = "The {0} pick list was skipped because of an error. See the trace for details.";
			}
			#endregion
		}

		public sealed class ConfirmPickListAndTakeNextCommand : PickPackShip.ScanCommand
		{
			public override string Code => "CONFIRM*PICK*AND*NEXT";
			public override string ButtonName => "scanConfirmPickListAndTakeNext";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.CurrentMode.Commands.OfType<WorksheetPicking.ConfirmPickListCommand>().First().IsApplicable;

			private bool _inProcess = false;
			protected override bool Process()
			{
				try
				{
					_inProcess = true;
					return Basis.CurrentMode.Commands.OfType<WorksheetPicking.ConfirmPickListCommand>().First().Execute();
				}
				finally
				{
					_inProcess = false;
				}
			}

			/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic"/>
			public class AlterConfirmPickListCommandLogic : PXGraphExtension<WorksheetPicking.ConfirmPickListCommand.Logic, WorksheetPicking, PickPackShip, PickPackShip.Host>
			{
				public static bool IsActive() => PaperlessPicking.IsActive();

				/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic.ConfigureOnSuccessAction(ScanLongRunAwaiter{PickPackShip, SOPicker}.IResultProcessor)"/>
				[PXOverride]
				public virtual void ConfigureOnSuccessAction(ScanLongRunAwaiter<PickPackShip, SOPicker>.IResultProcessor onSuccess, Action<ScanLongRunAwaiter<PickPackShip, SOPicker>.IResultProcessor> base_ConfigureOnSuccessAction)
				{
					base_ConfigureOnSuccessAction(onSuccess);
					if (Base1.CurrentMode.Commands.OfType<ConfirmPickListAndTakeNextCommand>().FirstOrDefault()?._inProcess == true)
						onSuccess.Do((basis, picker) => basis.CurrentMode.Commands.OfType<TakeNextPickListCommand>().First().Execute());
				}
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Finish and Next";
			}
			#endregion
		}

		public sealed class ConfirmLineQtyCommand : PickPackShip.ScanCommand
		{
			public override string Code => "CONFIRM*LINE*QTY";
			public override string ButtonName => "scanConfirmLineQty";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable && Basis.Remove != true && Basis.Get<PaperlessPicking>().WantedLineNbr != null;

			protected override bool Process() => Get<Logic>().ConfirmQtyOfWantedSplit();

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public PXAction<ScanHeader> ReopenLineQty;
				[PXButton(CommitChanges = true, DisplayOnMainToolbar = false), PXUIField(DisplayName = "Proceed Picking")]
				protected virtual void reopenLineQty() => Basis.Get<Logic>().ReopenQtyOfCurrentSplit();

				public virtual bool ConfirmQtyOfWantedSplit()
				{
					var wantedSplit = PPBasis.GetWantedSplit();
					if (wantedSplit != null)
					{
						SetForceCompletedOfSplit(wantedSplit, true);
						return true;
					}
					return false;
				}

				public virtual bool ReopenQtyOfCurrentSplit()
				{
					if (WSBasis.PickListOfPicker.Current is SOPickerListEntry entry && entry.ForceCompleted == true)
					{
						SetForceCompletedOfSplit(entry, false);
						return true;
					}
					return false;
				}

				public virtual void SetForceCompletedOfSplit(SOPickerListEntry entry, bool value)
				{
					entry.ForceCompleted = value;
					WSBasis.PickListOfPicker.Update(entry);
					Basis.SaveChanges();
					Basis.ReportInfo(value ? Msg.LineQtyConfirmed : Msg.LineQtyReopened, Basis.SightOf<SOPickerListEntry.inventoryID>(entry));

					PPBasis.WantedLineNbr = PPBasis.GetNextWantedLineNbr();
					Basis.Reset(fullReset: false);
					Basis.SetDefaultState();
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Confirm Line Quantity";
				public const string LineQtyConfirmed = "Quantity of the line has been confirmed.";
				public const string LineQtyReopened = "You can proceed to pick the {0} item.";
			}
			#endregion
		}
		#endregion

		#region Decoration
		public virtual void InjectShipmentPromptWithTakeNext(PickPackShip.ShipmentState pickShipment)
		{
			pickShipment.Intercept.StatePrompt.ByReplace(basis =>
				basis.Localize(PickListState.Msg.Prompt));
		}

		public virtual void InjectSuppressShipmentWithWorksheetOfSingleType(PickPackShip.ShipmentState pickShipment)
		{
			pickShipment.Intercept.GetByBarcode.ByOverride((basis, barcode, base_GetByBarcode) =>
			{
				var shipment = base_GetByBarcode(barcode);

				if (shipment?.CurrentWorksheetNbr != null)
					if (SOPickingWorksheet.PK.Find(basis, shipment.CurrentWorksheetNbr) is SOPickingWorksheet sheet)
						if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Single)
							return null; // makes sure that shipments with paperless pick list won't be picked via simple pick mode

				return shipment;
			});
		}

		public virtual void InjectShipmentAbsenceHandlingByWorksheetOfSingleType(PickPackShip.PickMode.ShipmentState pickShipment)
		{
			pickShipment.Intercept.HandleAbsence.ByPrepend((basis, barcode) =>
			{
				if (barcode.Contains("/") == false)
				{
					if (basis.FindMode<SinglePickMode>() is SinglePickMode single && single.IsActive)
					{
						if (single.TryProcessBy<PickListState>(barcode, StateSubstitutionRule.KeepAbsenceHandling))
						{
							basis.SetScanMode<SinglePickMode>();
							basis.FindState<PickListState>().Process(barcode);
							return AbsenceHandling.Done;
						}
					}
				}

				return AbsenceHandling.Skipped;
			});
		}

		public virtual void InjectPickListPaperless(WorksheetPicking.PickListState pickListState)
		{
			pickListState
				.Intercept.StatePrompt.ByReplace(basis =>
					basis.Localize(PickListState.Msg.Prompt))
				.Intercept.Validate.ByAppend((basis, pickList) =>
				{
					var ppBasis = basis.Get<PaperlessPicking>();
					var wsBasis = ppBasis.WSBasis;

					if (PickListRejection(wsBasis, pickList))
						if (wsBasis.PickListOfPicker.SelectMain().Any(pl => pl.PickedQty > 0))
							return Validation.Fail(Msg.CannotReturnCurrentListToQueue, wsBasis.PickingJob.Current.PickListNbr);

					var pickingJob = SOPickingJob.FK.Picker.SelectChildren(basis, pickList).FirstOrDefault();
					if (pickingJob != null)
					{
						if (pickingJob.Status.IsNotIn(SOPickingJob.status.Enqueued, SOPickingJob.status.Reenqueued, SOPickingJob.status.Assigned, SOPickingJob.status.Picking))
							return Validation.Fail(Msg.PickingJobWrongStatus, pickingJob.PickListNbr, basis.SightOf<SOPickingJob.status>(pickingJob));

						if (pickingJob.PreferredAssigneeID.IsNotIn(null, basis.Graph.Accessinfo.UserID))
							return Validation.Fail(Msg.PickingJobAlreadyTaken, pickingJob.PickListNbr);

						if (pickingJob.ActualAssigneeID.IsNotIn(null, basis.Graph.Accessinfo.UserID) && pickingJob.MinutesSinceLastModification < new minutes15().Value)
							return Validation.Fail(Msg.PickingJobAlreadyTaken, pickingJob.PickListNbr);
					}

					return Validation.Ok;
				})
				.Intercept.Apply.ByOverride((basis, pickList, base_Apply) =>
				{
					bool anyChanged = false;
					var ppBasis = basis.Get<PaperlessPicking>();
					var wsBasis = ppBasis.WSBasis;

					if (PickListRejection(wsBasis, pickList))
						anyChanged |= ppBasis.ReturnCurrentJobToQueue();

					base_Apply(pickList);

					ppBasis.PathInversedDirection = ppBasis.LastVisitedLocationID != null && ppBasis.NeedInversedDirection(pickList, ppBasis.LastVisitedLocationID.Value);
					ppBasis.WantedLineNbr = ppBasis.GetNextWantedLineNbr();

					basis.Clear<WMSBase.LocationState>();
					basis.Clear<WMSBase.InventoryItemState>();

					anyChanged |= wsBasis.AssignUser(startPicking: false);
					if (anyChanged)
						basis.SaveChanges();
				})
				.Intercept.ClearState.ByAppend(basis =>
				{
					var ppBasis = basis.Get<PaperlessPicking>();

					ppBasis.PathInversedDirection = null;
					ppBasis.WantedLineNbr = null;

					basis.Clear<WMSBase.LocationState>();
					basis.Clear<WMSBase.InventoryItemState>();
				});
		}

		public virtual void InjectNavigationOnLocation(WMSBase.LocationState locState)
		{
			locState
				.Intercept.StateInstructions.ByAppend(basis =>
					basis.Remove != true
						? basis.Get<PaperlessPicking>().PromptWantedLocation()
						: basis.Get<PaperlessPicking>().PromptLocationForRemoval())

				.Intercept.IsStateActive.ByConjoin(basis =>
					(basis.Remove != true).Implies(!basis.DefaultLocation))

				.Intercept.IsStateSkippable.ByDisjoin(basis =>
					basis.Remove != true && basis.LocationID != null)

				.Intercept.IsStateSkippable.ByDisjoin(basis =>
					basis.Remove != true && basis.Get<PaperlessPicking>().With(it => it.GetWantedSplit()?.LocationID is int wantedLocationID && it.LastVisitedLocationID == wantedLocationID))

				.Intercept.Validate.ByAppend((basis, location) =>
					location.LocationID == basis.Get<PaperlessPicking>().With(it => it.Basis.Remove != true ? it.GetWantedSplit() : it.GetSplitForRemoval())?.LocationID
						? Validation.Ok
						: Validation.Fail(Msg.WrongLocation, location.LocationCD))

				.Intercept.Apply.ByAppend((basis, location) =>
					basis.Get<PaperlessPicking>().LastVisitedLocationID = location.LocationID);
		}

		public virtual void InjectNavigationOnItem(WMSBase.InventoryItemState itemState)
		{
			itemState
				.Intercept.StateInstructions.ByAppend(basis =>
					basis.Remove != true ? basis.Get<PaperlessPicking>().PromptWantedItem() : null)

				.Intercept.IsStateSkippable.ByDisjoin(basis =>
					basis.Remove != true && basis.InventoryID != null && basis.FindState<WMSBase.LotSerialState>().With(ls => ls.IsActive && !ls.IsSkippable))

				.Intercept.Validate.ByAppend((basis, item) =>
					basis.Remove == true ||
					(basis.Get<PaperlessPicking>().GetWantedSplit().With(it => (it.InventoryID, it.SubItemID)))
						.Equals
					(item.GetItem<INItemXRef>().With(it => (it.InventoryID, it.SubItemID)))
						? Validation.Ok
						: Validation.Fail(Msg.WrongItem, item.GetItem<INItemXRef>().AlternateID));
		}

		public virtual void InjectNavigationOnLotSerial(WMSBase.LotSerialState lsState)
		{
			lsState
				.Intercept.StateInstructions.ByAppend(basis =>
					basis.Remove != true ? basis.Get<PaperlessPicking>().PromptWantedItem() : null)

				.Intercept.Validate.ByAppend((basis, lotSerialNbr) =>
					basis.Remove == true || basis.IsEnterableLotSerial(isForIssue: true) || lotSerialNbr == basis.Get<PaperlessPicking>().GetWantedSplit().LotSerialNbr
						? Validation.Ok
						: Validation.Fail(Msg.WrongLotSerial, lotSerialNbr));
		}

		public virtual void InjectRemoveClearLocationAndInventory(WMSBase.RemoveCommand remove)
		{
			remove.Intercept.Process.ByOverride((basis, base_Process) =>
			{
				basis.Clear<WMSBase.LocationState>();
				basis.Clear<WMSBase.InventoryItemState>();
				return base_Process();
			});
		}

		public virtual void InjectConfirmPickListSuppressionOnCanPick(WorksheetPicking.ConfirmPickListCommand confirm)
		{
			confirm.Intercept.IsEnabled.ByConjoin(basis =>
				!basis.Get<WorksheetPicking>().CanWSPick);
		}

		public static bool PickListRejection(WorksheetPicking wsBasis, SOPicker newPicker) =>
			wsBasis.WorksheetNbr != null &&
			SOPicker.PK.Find(wsBasis.Graph, wsBasis.WorksheetNbr, wsBasis.PickerNbr) is SOPicker picker &&
			picker.PickListNbr != newPicker?.PickListNbr &&
			picker.Confirmed != true;
		#endregion

		#region Overrides
		/// Overrides <see cref="WorksheetPicking.GetListEntries(string, int?)"/>
		[PXOverride]
		public virtual IEnumerable<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>> GetListEntries(string worksheetNbr, int? pickerNbr, Func<string, int?, IEnumerable<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>>> baseIgnored)
			=> WSBasis.GetListEntries(worksheetNbr, pickerNbr, PathInversedDirection == true);

		/// Overrides <see cref="WorksheetPicking.IsWorksheetMode(string)"/>
		[PXOverride]
		public virtual bool IsWorksheetMode(string modeCode, Func<string, bool> base_IsWorksheetMode)
			=> base_IsWorksheetMode(modeCode) || modeCode == SinglePickMode.Value;

		/// Overrides <see cref="WorksheetPicking.ShowWorksheetNbrForMode(string)"/>
		[PXOverride]
		public virtual bool ShowWorksheetNbrForMode(string modeCode, Func<string, bool> base_ShowWorksheetNbrForMode)
			=> base_ShowWorksheetNbrForMode(modeCode) && modeCode != SinglePickMode.Value;

		/// Overrides <see cref="WorksheetPicking.AssignUser"/>
		[PXOverride]
		public virtual bool AssignUser(bool startPicking, Func<bool, bool> base_AssignUser)
		{
			bool anyChanged = false;
			var (job, picker, sheet) = (WSBasis.PickingJob.Current, WSBasis.Picker.Current, WSBasis.Worksheet.Current);

			if (startPicking && sheet.Status == SOPickingWorksheet.status.Open)
			{
				sheet.Status = SOPickingWorksheet.status.Picking;
				sheet = WSBasis.Worksheet.Update(sheet);
				anyChanged = true;
			}

			if (startPicking && picker.UserID != Graph.Accessinfo.UserID)
			{
				picker.UserID = Graph.Accessinfo.UserID;
				picker = WSBasis.Picker.Update(picker);
				anyChanged = true;
			}

			if (startPicking && job.Status.IsIn(SOPickingJob.status.Enqueued, SOPickingJob.status.Reenqueued, SOPickingJob.status.Assigned))
			{
				job.Status = SOPickingJob.status.Picking;
				job = WSBasis.PickingJob.Update(job);
				anyChanged = true;
			}

			if (job.ActualAssigneeID != Graph.Accessinfo.UserID)
			{
				job.ActualAssigneeID = Graph.Accessinfo.UserID;
				job = WSBasis.PickingJob.Update(job);
				anyChanged = true;
			}

			if (startPicking)
				IgnoredPickingJobs.Clear();

			return anyChanged;
		}

		/// Overrides <see cref="WorksheetPicking.SetPickList(PXResult{SOPickingWorksheet, SOPicker})"/>
		[PXOverride]
		public virtual void SetPickList(PXResult<SOPickingWorksheet, SOPicker> pickList, Action<PXResult<SOPickingWorksheet, SOPicker>> base_SetPickList)
		{
			base_SetPickList(pickList);

			if (Basis.CurrentMode is SinglePickMode)
			{
				var shipmentNbr = pickList?.GetItem<SOPickingWorksheet>()?.SingleShipmentNbr;
				var shipment = SOShipment.PK.Find(Basis, shipmentNbr);
				Basis.NoteID = shipment?.NoteID;
			}
		}

		/// Overrides <see cref="WorksheetPicking.FindModeForWorksheet(SOPickingWorksheet)"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> FindModeForWorksheet(SOPickingWorksheet sheet, Func<SOPickingWorksheet, ScanMode<PickPackShip>> base_FindModeForWorksheet)
		{
			if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Single)
				return Basis.FindMode<SinglePickMode>();

			return base_FindModeForWorksheet(sheet);
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState"/>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (state.ModeCode == SinglePickMode.Value)
			{
				if (state is PickListState pickList)
					InjectPickListPaperless(pickList);
				else if (state is WMSBase.LocationState locState)
					InjectNavigationOnLocation(locState);
				else if (state is WMSBase.InventoryItemState itemState)
					InjectNavigationOnItem(itemState);
				else if (state is WMSBase.LotSerialState lsState)
					InjectNavigationOnLotSerial(lsState);
			}
			else
			{
				if (state is PickPackShip.PickMode.ShipmentState pickShipment)
				{
					InjectShipmentPromptWithTakeNext(pickShipment);
					InjectSuppressShipmentWithWorksheetOfSingleType(pickShipment);
					InjectShipmentAbsenceHandlingByWorksheetOfSingleType(pickShipment);
				}
			}

			return state;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> DecorateScanMode(ScanMode<PickPackShip> original, Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
		{
			var mode = base_DecorateScanMode(original);

			if (mode is PickPackShip.PickMode pick)
				mode.Intercept.CreateCommands.ByAppend(basis => new[] { new TakeNextPickListCommand() });

			return mode;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanCommand"/>
		[PXOverride]
		public virtual ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
		{
			var command = base_DecorateScanCommand(original);

			if (command.ModeCode == SinglePickMode.Value)
			{
				if (command is WMSBase.RemoveCommand remove)
					InjectRemoveClearLocationAndInventory(remove);
				else if (command is WorksheetPicking.ConfirmPickListCommand confirm)
					InjectConfirmPickListSuppressionOnCanPick(confirm);
			}

			return command;
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public abstract class Msg
		{
			public const string CannotReturnCurrentListToQueue = "The {0} pick list cannot be returned to the picking queue because it is already being picked.";
			public const string PickingJobAlreadyTaken = "The {0} pick list cannot be processed because it is already assigned to another picker.";
			public const string PickingJobWrongStatus = "The {0} pick list cannot be processed because it has the {1} status.";

			public const string GoToLocation = "Go to the {0} location.";
			public const string PickItemNoLotSerial = "Pick {2}, quantity: {0} {1} left.";
			public const string PickItemWithLotSerial = "Pick {2}, lot/serial: {3}, quantity: {0} {1} left.";
			public const string PickItemFromLocationNoLotSerial = "Go to {4} and pick {2}, quantity: {0} {1} left.";
			public const string PickItemFromLocationWithLotSerial = "Go to {4} and pick {2}, lot/serial: {3}, quantity: {0} {1} left.";

			public const string WrongLocation = "Incorrect location barcode ({0}) has been scanned.";
			public const string WrongItem = "Incorrect item barcode ({0}) has been scanned.";
			public const string WrongLotSerial = "Incorrect lot/serial number ({0}) has been scanned.";
		}
		#endregion

		#region Extensibility
		public abstract class ScanExtension : PXGraphExtension<PaperlessPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => PaperlessPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
			public PaperlessPicking PPBasis => Base3;
		}

		public abstract class ScanExtension<TTargetExtension> : PXGraphExtension<TTargetExtension, PaperlessPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
			where TTargetExtension : PXGraphExtension<PaperlessPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => PaperlessPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
			public PaperlessPicking PPBasis => Base3;
			public TTargetExtension Target => Base4;
		}
		#endregion

		public class minutes15 : BqlInt.Constant<minutes15> { public minutes15() : base(15) { } }
	}

	public sealed class PaperlessScanHeader : PXCacheExtension<WorksheetScanHeader, WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => PaperlessPicking.IsActive();

		#region LastVisitedLocationID
		[Location(Enabled = false, DisplayName = "Current Location")]
		[PXUIVisible(typeof(lastVisitedLocationID.IsNotNull))]
		public int? LastVisitedLocationID { get; set; }
		public abstract class lastVisitedLocationID : BqlInt.Field<lastVisitedLocationID> { }
		#endregion
		#region PathInversedDirection
		[PXBool]
		public bool? PathInversedDirection { get; set; }
		public abstract class pathInversedDirection : BqlBool.Field<pathInversedDirection> { }
		#endregion
		#region WantedLineNbr
		[PXInt]
		public int? WantedLineNbr { get; set; }
		public abstract class wantedLineNbr : BqlInt.Field<wantedLineNbr> { }
		#endregion
		#region SingleShipmentNbr
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		[PXSelector(typeof(SOShipment.shipmentNbr))]
		public string SingleShipmentNbr { get; set; }
		public abstract class singleShipmentNbr : BqlString.Field<singleShipmentNbr> { }
		#endregion
		#region IgnoredPickingJobs
		// Acuminator disable once PX1032 MethodInvocationInDac [field initialization]
		public HashSet<int> IgnoredPickingJobs { get; set; } = new HashSet<int>();
		public abstract class ignoredPickingJobs : IBqlField { }
		#endregion
	}
}
