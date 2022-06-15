using System;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.BarcodeProcessing;

using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class PaperlessWaveBatchPicking : WaveBatchPicking.ScanExtension
	{
		public static bool IsActive() => IsActiveBase() && PaperlessPicking.IsActive();

		#region Decoration
		public virtual void InjectPaperlessWaveMode(WaveBatchPicking.WavePickMode wavePick)
		{
			wavePick
				.Intercept.CreateCommands.ByAppend(basis => new PickPackShip.ScanCommand[]
				{
					new PaperlessPicking.TakeNextPickListCommand(),
					new PaperlessPicking.ConfirmPickListAndTakeNextCommand(),
					new PaperlessPicking.ConfirmLineQtyCommand()
				})
				.Intercept.CreateStates.ByAppend(basis => new ScanState<PickPackShip>[]
				{
					new PaperlessPicking.WarehouseState(),
					new PaperlessPicking.NearestLocationState()
				})
				.Intercept.CreateTransitions.ByBaseSubstitute((basis, _) =>
				{
					return basis.StateFlow(flow => flow
						.ForkBy(b => b.Remove != true)
						.PositiveBranch(directFlow => directFlow
							.From<WaveBatchPicking.WavePickMode.PickListState>()
							.NextTo<WorksheetPicking.AssignToteState>()
							.NextTo<WMSBase.LocationState>()
							.NextTo<WMSBase.InventoryItemState>()
							.NextTo<WMSBase.LotSerialState>()
							.NextTo<WMSBase.ExpireDateState>()
							.NextTo<WaveBatchPicking.WavePickMode.ConfirmToteState>())
						.NegativeBranch(removeFlow => removeFlow
							.From<WaveBatchPicking.WavePickMode.PickListState>()
							.NextTo<WMSBase.InventoryItemState>()
							.NextTo<WMSBase.LotSerialState>()
							.NextTo<WMSBase.LocationState>()));
				})
				.Intercept.ResetMode.ByBaseSubstitute((basis, fullReset, _) =>
				{
					basis.Clear<WaveBatchPicking.WavePickMode.PickListState>(when: fullReset && !basis.IsWithinReset);
					basis.Clear<WMSBase.LocationState>(when: fullReset);
					basis.Clear<WMSBase.InventoryItemState>(when: fullReset);
					basis.Clear<WMSBase.LotSerialState>();
					basis.Clear<WMSBase.ExpireDateState>();
					basis.Clear<WaveBatchPicking.WavePickMode.RemoveFromToteState>();
				});
		}

		public virtual void InjectPaperlessBatchMode(WaveBatchPicking.BatchPickMode batchPick)
		{
			batchPick
				.Intercept.CreateCommands.ByAppend(basis => new PickPackShip.ScanCommand[]
				{
					new PaperlessPicking.TakeNextPickListCommand(),
					new PaperlessPicking.ConfirmPickListAndTakeNextCommand(),
					new PaperlessPicking.ConfirmLineQtyCommand()
				})
				.Intercept.CreateStates.ByAppend(basis => new ScanState<PickPackShip>[]
				{
					new PaperlessPicking.WarehouseState(),
					new PaperlessPicking.NearestLocationState()
				})
				.Intercept.CreateTransitions.ByBaseSubstitute((basis, _) =>
				{
					return basis.StateFlow(flow => flow
						.ForkBy(b => b.Remove != true)
						.PositiveBranch(directFlow => directFlow
							.ForkBy(b => b.Get<PPSCartSupport>().With(cs => cs.IsCartRequired()))
							.PositiveBranch(directCartFlow => directCartFlow
								.From<WaveBatchPicking.BatchPickMode.PickListState>()
								.NextTo<PPSCartSupport.CartState>()
								.NextTo<WMSBase.LocationState>()
								.NextTo<WMSBase.InventoryItemState>()
								.NextTo<WMSBase.LotSerialState>()
								.NextTo<WMSBase.ExpireDateState>())
							.NegativeBranch(directNoCartFlow => directNoCartFlow
								.From<WaveBatchPicking.BatchPickMode.PickListState>()
								.NextTo<WMSBase.LocationState>()
								.NextTo<WMSBase.InventoryItemState>()
								.NextTo<WMSBase.LotSerialState>()
								.NextTo<WMSBase.ExpireDateState>()))
						.NegativeBranch(removeFlow => removeFlow
							.From<WaveBatchPicking.BatchPickMode.PickListState>()
							.NextTo<WMSBase.InventoryItemState>()
							.NextTo<WMSBase.LotSerialState>()
							.NextTo<WMSBase.LocationState>()));
				})
				.Intercept.ResetMode.ByBaseSubstitute((basis, fullReset, _) =>
				{
					basis.Clear<WaveBatchPicking.BatchPickMode.PickListState>(when: fullReset && !basis.IsWithinReset);
					basis.Clear<PPSCartSupport.CartState>(when: fullReset && !basis.IsWithinReset);
					basis.Clear<PickPackShip.LocationState>(when: fullReset);
					basis.Clear<PickPackShip.InventoryItemState>(when: fullReset);
					basis.Clear<PickPackShip.LotSerialState>();
					basis.Clear<PickPackShip.ExpireDateState>();
				});
		}
		#endregion

		#region Overrides
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> DecorateScanMode(ScanMode<PickPackShip> original, Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
		{
			var mode = base_DecorateScanMode(original);

			if (mode is WaveBatchPicking.WavePickMode wavePick)
				InjectPaperlessWaveMode(wavePick);
			else if (mode is WaveBatchPicking.BatchPickMode batchPick)
				InjectPaperlessBatchMode(batchPick);

			return mode;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState"/>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var ppBasis = Basis.Get<PaperlessPicking>();
			var state = base_DecorateScanState(original);

			if (WaveBatchPicking.MatchMode(state.ModeCode))
			{
				if (state is WorksheetPicking.PickListState pickList)
					ppBasis.InjectPickListPaperless(pickList);
				else if (state is WMSBase.LocationState locState)
					ppBasis.InjectNavigationOnLocation(locState);
				else if (state is WMSBase.InventoryItemState itemState)
					ppBasis.InjectNavigationOnItem(itemState);
				else if (state is WMSBase.LotSerialState lsState)
					ppBasis.InjectNavigationOnLotSerial(lsState);
			}

			return state;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanCommand"/>
		[PXOverride]
		public virtual ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
		{
			var ppBasis = Basis.Get<PaperlessPicking>();
			var command = base_DecorateScanCommand(original);

			if (WaveBatchPicking.MatchMode(command.ModeCode))
			{
				if (command is WMSBase.RemoveCommand remove)
					ppBasis.InjectRemoveClearLocationAndInventory(remove);
				else if (command is WorksheetPicking.ConfirmPickListCommand confirm)
					ppBasis.InjectConfirmPickListSuppressionOnCanPick(confirm);
			}

			return command;
		}

		/// Overrides <see cref="PickPackShip.InjectLocationSkippingOnPromptLocationForEveryLineOption"/>
		[PXOverride]
		public virtual void InjectLocationSkippingOnPromptLocationForEveryLineOption(WMSBase.LocationState locationState, Action<WMSBase.LocationState> base_InjectLocationSkippingOnPromptLocationForEveryLineOption)
		{
			if (WaveBatchPicking.MatchMode(locationState.ModeCode))
			{
				/// suppress changes applied in <see cref="WaveBatchPicking.DecorateScanState(ScanState{PickPackShip}, Func{ScanState{PickPackShip}, ScanState{PickPackShip}})"/>
			}
			else
			{
				base_InjectLocationSkippingOnPromptLocationForEveryLineOption(locationState);
			}
		}

		/// Overrides <see cref="PickPackShip.InjectItemAbsenceHandlingByLocation"/>
		[PXOverride]
		public virtual void InjectItemAbsenceHandlingByLocation(WMSBase.InventoryItemState inventoryState, Action<WMSBase.InventoryItemState> base_InjectItemAbsenceHandlingByLocation)
		{
			if (WaveBatchPicking.MatchMode(inventoryState.ModeCode))
			{
				/// suppress changes applied in <see cref="WaveBatchPicking.DecorateScanState(ScanState{PickPackShip}, Func{ScanState{PickPackShip}, ScanState{PickPackShip}})"/>
			}
			else
			{
				base_InjectItemAbsenceHandlingByLocation(inventoryState);
			}
		}

		/// Overrides <see cref="WaveBatchPicking.ConfirmState.Logic"/>
		public class AlterWaveBatchPickingConfirmStateLogic : WaveBatchPicking.ScanExtension<WaveBatchPicking.ConfirmState.Logic>
		{
			public static bool IsActive() => PaperlessWaveBatchPicking.IsActive();

			/// Overrides <see cref="WaveBatchPicking.ConfirmState.Logic.Confirm"/>
			[PXOverride]
			public virtual FlowStatus Confirm(Func<FlowStatus> baseIgnored)
			{
				var ppBasis = Basis.Get<PaperlessPicking>();
				bool remove = Basis.Remove == true;
				bool wave = WSBasis.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Wave;

				if (Basis.LocationID == null && !Basis.DefaultLocation)
					Basis.LocationID = ppBasis.LastVisitedLocationID;

				SOPickerListEntry pickedSplit = remove
					? wave
						? GetSplitForRemovalFromTote(WBBasis.RemoveFromToteID)
						: ppBasis.GetSplitForRemoval()
					: ppBasis.GetWantedSplit();

				var confirmResult = WSBasis.ConfirmSplit(pickedSplit, Sign.MinusIf(remove) * Basis.BaseQty);
				if (confirmResult.IsError != false)
					return confirmResult;

				Basis.Get<PaperlessPicking.ConfirmState.Logic>().VisitSplit(pickedSplit);

				INTote targetTote = wave
					? Basis.Get<WaveBatchPicking.WavePickMode.Logic>().GetToteForPickListEntry(pickedSplit)
					: null;

				Basis.ReportInfo(
					remove
						? wave ? Msg.InventoryRemovedFromTote : Msg.InventoryRemoved
						: wave ? Msg.InventoryAddedToTote : Msg.InventoryAdded,
					Basis.SightOf<SOPickerListEntry.inventoryID>(pickedSplit), Basis.Qty, pickedSplit.UOM, targetTote?.ToteCD);

				return FlowStatus.Ok.WithDispatchNext;
			}

			public virtual SOPickerListEntry GetSplitForRemovalFromTote(int? toteID)
			{
				if (Basis.Remove != true || Basis.InventoryID == null || toteID == null)
					return null;

				var notPickedLines = WSBasis.PickListOfPicker.Select()
					.AsEnumerable()
					.Cast<PXResult<SOPickerListEntry, INLocation, SOPickerToShipmentLink>>()
					.Where(ent =>
						ent.GetItem<SOPickerListEntry>().PickedQty > 0 &&
						ent.GetItem<SOPickerListEntry>().ForceCompleted != true &&
						ent.GetItem<SOPickerToShipmentLink>().ToteID == toteID &&
						WSBasis.IsSelectedSplit(ent));

				return notPickedLines.Select(e => e.GetItem<SOPickerListEntry>()).FirstOrDefault();
			}

			[PXLocalizable]
			public abstract class Msg : WaveBatchPicking.ConfirmState.Msg { }
		}

		/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic"/>
		public class AlterConfirmPickListCommandLogic : WorksheetPicking.ScanExtension<WorksheetPicking.ConfirmPickListCommand.Logic>
		{
			public static bool IsActive() => PaperlessWaveBatchPicking.IsActive();

			/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic.ConfigureOnSuccessAction(ScanLongRunAwaiter{PickPackShip, SOPicker}.IResultProcessor)"/>
			[PXOverride]
			public virtual void ConfigureOnSuccessAction(ScanLongRunAwaiter<PickPackShip, SOPicker>.IResultProcessor onSuccess, Action<ScanLongRunAwaiter<PickPackShip, SOPicker>.IResultProcessor> base_ConfigureOnSuccessAction)
			{
				base_ConfigureOnSuccessAction(onSuccess);
				if (Base1.CurrentMode is WaveBatchPicking.BatchPickMode)
				{
					var confirmAndNextCommand = Base1.CurrentMode.Commands.OfType<PaperlessPicking.ConfirmPickListAndTakeNextCommand>().FirstOrDefault();
					if (confirmAndNextCommand != null)
					{
						var preSortLocationScan = Base1.Logs.Select().RowCast<ScanLog>().Select(log => log.HeaderStateBefore).SkipWhile(h => h.InitialScanState == WaveBatchPicking.BatchPickMode.SortingLocationState.Value).FirstOrDefault();
						if (preSortLocationScan != null && preSortLocationScan.Barcode.Substring(1) == confirmAndNextCommand.Code)
							onSuccess.Do((basis, picker) => basis.CurrentMode.Commands.OfType<PaperlessPicking.TakeNextPickListCommand>().First().Execute());
					}
				}
			}
		}
		#endregion
	}
}
