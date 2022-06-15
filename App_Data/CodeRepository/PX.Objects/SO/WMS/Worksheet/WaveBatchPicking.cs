using System;
using System.Linq;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class WaveBatchPicking : WorksheetPicking.ScanExtension
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSAdvancedPicking>();
		public static bool MatchMode(string mode) => mode.IsIn(WavePickMode.Value, BatchPickMode.Value);

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.CreateScanModes"/>
		[PXOverride]
		public virtual IEnumerable<ScanMode<PickPackShip>> CreateScanModes(Func<IEnumerable<ScanMode<PickPackShip>>> base_CreateScanModes)
		{
			foreach (var mode in base_CreateScanModes())
				yield return mode;

			yield return new WavePickMode();
			yield return new BatchPickMode();
		}

		public sealed class WavePickMode : PickPackShip.ScanMode
		{
			public const string Value = "WAVE";
			public class value : BqlString.Constant<value> { public value() : base(WavePickMode.Value) { } }

			public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

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
				yield return new ConfirmToteState();
				yield return new ConfirmState();

				// directly set state
				yield return new RemoveFromToteState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<PickListState>()
					.NextTo<WorksheetPicking.AssignToteState>()
					.NextTo<WMSBase.LocationState>()
					.NextTo<WMSBase.InventoryItemState>()
					.NextTo<WMSBase.LotSerialState>()
					.NextTo<WMSBase.ExpireDateState>()
					.NextTo<ConfirmToteState>());
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new WMSBase.RemoveCommand()
					.Intercept.IsEnabled.ByConjoin(basis => !(basis.CurrentState is WorksheetPicking.AssignToteState))
					.Intercept.Process.ByOverride((basis, base_Process) =>
					{
						bool result = base_Process();
						basis.SetScanState<RemoveFromToteState>();
						return result;
					});
				yield return new WMSBase.QtySupport.SetQtyCommand();
				yield return new WorksheetPicking.ConfirmPickListCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<WMSBase.LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<WMSBase.InventoryItemState>();
				Clear<WMSBase.LotSerialState>();
				Clear<WMSBase.ExpireDateState>();
				Clear<RemoveFromToteState>();
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				public static bool IsActive() => IsActiveBase();

				public virtual bool ConfirmToteForEveryLine =>
					Basis.Setup.Current.ConfirmToteForEachItem == true &&
					Basis.Remove == false &&
					WSBasis.WorksheetNbr != null &&
					WSBasis.Worksheet.Current?.WorksheetType == SOPickingWorksheet.worksheetType.Wave &&
					WSBasis.ShipmentsOfPicker.Select().Count > 1;

				public virtual INTote GetToteForPickListEntry(SOPickerListEntry selectedSplit)
				{
					if (selectedSplit == null)
						return null;

					INTote tote =
						SelectFrom<INTote>.
						InnerJoin<SOPickerToShipmentLink>.On<SOPickerToShipmentLink.FK.Tote>.
						Where<
							SOPickerToShipmentLink.worksheetNbr.IsEqual<@P.AsString>.
							And<SOPickerToShipmentLink.pickerNbr.IsEqual<@P.AsInt>>.
							And<SOPickerToShipmentLink.shipmentNbr.IsEqual<@P.AsString>>>.
						View.Select(Basis, selectedSplit.WorksheetNbr, selectedSplit.PickerNbr, selectedSplit.ShipmentNbr);
					return tote;
				}
			}
			#endregion

			#region States
			public sealed class PickListState : WorksheetPicking.PickListState
			{
				protected override string WorksheetType => SOPickingWorksheet.worksheetType.Wave;
			}

			public sealed class RemoveFromToteState : WorksheetPicking.ToteState
			{
				public const string Value = "RMFT";
				public class value : BqlString.Constant<value> { public value() : base(RemoveFromToteState.Value) { } }

				public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override void Apply(INTote tote) => WBBasis.RemoveFromToteID = tote.ToteID;
				protected override void ClearState() => WBBasis.RemoveFromToteID = null;

				protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD);

				protected override void SetNextState() => Basis.SetDefaultState();

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WorksheetPicking.ToteState.Msg
				{
					public const string Prompt = "Scan the barcode of a tote from which you want to remove the items.";
				}
				#endregion
			}

			public sealed class ConfirmToteState : WorksheetPicking.ToteState
			{
				public const string Value = "CNFT";
				public class value : BqlString.Constant<value> { public value() : base(ConfirmToteState.Value) { } }

				public WavePickMode.Logic Mode => Get<WavePickMode.Logic>();
				public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

				private INTote ProperTote => WBBasis.GetSelectedPickListEntry().With(Mode.GetToteForPickListEntry);

				public override string Code => Value;
				protected override string StatePrompt => Basis.Localize(Msg.Prompt, ProperTote?.ToteCD);

				protected override bool IsStateActive() => Mode.ConfirmToteForEveryLine;
				protected override bool IsStateSkippable() => ProperTote == null;

				protected override Validation Validate(INTote tote)
				{
					if (Basis.HasFault(tote, base.Validate, out var fault))
						return fault;

					if (ProperTote.ToteID != tote.ToteID)
						return Validation.Fail(Msg.Mismatch, tote.ToteCD);

					return Validation.Ok;
				}

				protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD);

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WorksheetPicking.ToteState.Msg
				{
					public const string Prompt = "Scan the barcode of the {0} tote to confirm picking of the items.";
					public const string Mismatch = "Incorrect tote barcode ({0}) has been scanned.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Wave Pick";
			}
			#endregion
		}

		public sealed class BatchPickMode : PickPackShip.ScanMode
		{
			public const string Value = "BTCH";
			public class value : BqlString.Constant<value> { public value() : base(BatchPickMode.Value) { } }

			public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

			public override string Code => Value;
			public override string Description => Msg.DisplayName;

			protected override bool IsModeActive() => Basis.HasPick;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickListState();
				yield return new WMSBase.LocationState();
				yield return new WMSBase.InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true };
				yield return new WMSBase.LotSerialState();
				yield return new WMSBase.ExpireDateState() { IsForIssue = true };
				yield return new ConfirmState();

				if (Get<PPSCartSupport>() is PPSCartSupport cartSupport && cartSupport.IsCartRequired())
				{
					yield return new PPSCartSupport.CartState()
						.Intercept.Apply.ByAppend((basis, cart) =>
						{
							var wsBasis = basis.Get<WorksheetPicking>();
							if (wsBasis.Picker.Current != null)
							{
								wsBasis.Picker.Current.CartID = cart.CartID;
								wsBasis.Picker.UpdateCurrent();
							}
						});
				}

				// directly set state
				yield return new SortingLocationState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				var cartSupport = Get<PPSCartSupport>();
				if (cartSupport != null && cartSupport.IsCartRequired())
				{ // With Cart
					return StateFlow(flow => flow
						.From<PickListState>()
						.NextTo<PPSCartSupport.CartState>()
						.NextTo<PickPackShip.LocationState>()
						.NextTo<PickPackShip.InventoryItemState>()
						.NextTo<PickPackShip.LotSerialState>()
						.NextTo<PickPackShip.ExpireDateState>());
				}
				else
				{ // No Cart
					return StateFlow(flow => flow
						.From<PickListState>()
						.NextTo<PickPackShip.LocationState>()
						.NextTo<PickPackShip.InventoryItemState>()
						.NextTo<PickPackShip.LotSerialState>()
						.NextTo<PickPackShip.ExpireDateState>());
				}
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new PickPackShip.RemoveCommand();
				yield return new PickPackShip.QtySupport.SetQtyCommand();
				yield return new WorksheetPicking.ConfirmPickListCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<PPSCartSupport.CartState>(when: fullReset && !Basis.IsWithinReset);
				Clear<PickPackShip.LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<PickPackShip.InventoryItemState>();
				Clear<PickPackShip.LotSerialState>();
				Clear<PickPackShip.ExpireDateState>();
			}
			#endregion

			#region States
			public sealed class PickListState : WorksheetPicking.PickListState
			{
				protected override string WorksheetType => SOPickingWorksheet.worksheetType.Batch;
			}

			public sealed class SortingLocationState : PickPackShip.EntityState<INLocation>
			{
				public const string Value = "SLOC";
				public class value : BqlString.Constant<value> { public value() : base(SortingLocationState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

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

					if (location.IsSorting != true)
						return Validation.Fail(Msg.NotSorting, location.LocationCD);

					return Validation.Ok;
				}

				protected override void Apply(INLocation location) => Basis.Get<WorksheetPicking.ConfirmPickListCommand.Logic>().ConfirmPickList(location.LocationID.Value);

				protected override void ReportSuccess(INLocation location) => Basis.Reporter.Info(Msg.Ready, location.LocationCD);
				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode, Basis.SightOf<WMSScanHeader.siteID>());

				protected override void SetNextState() { }

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the sorting location.";
					public const string Ready = "The {0} sorting location is selected.";
					public const string Missing = PickPackShip.LocationState.Msg.Missing;
					public const string NotSorting = "The {0} location cannot be selected because it is not a sorting location.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Batch Pick";
			}
			#endregion
		}

		#region State
		public WaveBatchScanHeader WBHeader => Basis.Header.Get<WaveBatchScanHeader>() ?? new WaveBatchScanHeader();
		public ValueSetter<ScanHeader>.Ext<WaveBatchScanHeader> WBSetter => Basis.HeaderSetter.With<WaveBatchScanHeader>();

		#region RemoveFromToteID
		public int? RemoveFromToteID
		{
			get => WBHeader.RemoveFromToteID;
			set => WBSetter.Set(h => h.RemoveFromToteID, value);
		}
		#endregion
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			if (e.Row == null)
				return;

			WSBasis.PickListOfPicker.Cache.AdjustUI()
				.For<SOPickerListEntry.shipmentNbr>(a => a.Visible = WSBasis.Worksheet.Current?.WorksheetType == SOPickingWorksheet.worksheetType.Wave);
		}
		#endregion

		#region Logic
		public virtual SOPickerListEntry GetSelectedPickListEntry()
		{
			bool remove = Basis.Remove == true;
			var pickedSplit = WSBasis.PickListOfPicker
				.Select().AsEnumerable()
				.With(view => remove && RemoveFromToteID != null
					? view.Where(e => e.GetItem<SOPickerToShipmentLink>().ToteID == RemoveFromToteID)
					: view)
				.Select(row =>
				(
					Split: row.GetItem<SOPickerListEntry>(),
					Location: row.GetItem<INLocation>()
				))
				.Where(r => WSBasis.IsSelectedSplit(r.Split))
				.OrderByDescending(r => r.Split.IsUnassigned == false && r.Split.HasGeneratedLotSerialNbr == false && remove
					? r.Split.PickedQty > 0
					: r.Split.Qty > r.Split.PickedQty)
				.ThenByDescending(r => remove ? r.Split.PickedQty > 0 : r.Split.Qty > r.Split.PickedQty)
				.ThenByDescending(r => r.Split.LotSerialNbr == (Basis.LotSerialNbr ?? r.Split.LotSerialNbr))
				.ThenByDescending(r => string.IsNullOrEmpty(r.Split.LotSerialNbr))
				.ThenByDescending(r => (r.Split.Qty > r.Split.PickedQty || remove) && r.Split.PickedQty > 0)
				.ThenBy(r => Sign.MinusIf(remove) * r.Location.PathPriority)
				.With(view => remove
					? view.ThenByDescending(r => r.Location.LocationCD)
					: view.ThenBy(r => r.Location.LocationCD))
				.ThenByDescending(r => Sign.MinusIf(remove) * (r.Split.Qty - r.Split.PickedQty))
				.Select(r => r.Split)
				.FirstOrDefault();
			return pickedSplit;
		}
		#endregion

		#region States
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
					SOPickerListEntry pickedSplit = WBBasis.GetSelectedPickListEntry();

					var confirmResult = WSBasis.ConfirmSplit(pickedSplit, Sign.MinusIf(Basis.Remove == true) * Basis.BaseQty);
					if (confirmResult.IsError != false)
						return confirmResult;

					bool wave = WSBasis.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Wave;
					INTote targetTote = wave
						? Basis.Get<WavePickMode.Logic>().GetToteForPickListEntry(pickedSplit)
						: null;

					Basis.DispatchNext(
						Basis.Remove == true
							? wave ? Msg.InventoryRemovedFromTote : Msg.InventoryRemoved
							: wave ? Msg.InventoryAddedToTote : Msg.InventoryAdded,
						Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM, targetTote?.ToteCD);

					return FlowStatus.Ok;
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ConfirmationState.Msg
			{
				public const string Prompt = PickPackShip.PickMode.ConfirmState.Msg.Prompt;

				public const string InventoryAdded = "{0} x {1} {2} has been added to the pick list.";
				public const string InventoryRemoved = "{0} x {1} {2} has been removed from the pick list.";

				public const string InventoryAddedToTote = "{0} x {1} {2} has been added to the {3} tote.";
				public const string InventoryRemovedFromTote = "{0} x {1} {2} has been removed from the {3} tote.";
			}
			#endregion
		}
		#endregion

		#region Decoration
		public virtual void InjectPackLocationDeactivatedBasedOnShipmentSpecialPickType(WMSBase.LocationState locationState)
		{
			locationState.Intercept.IsStateActive.ByConjoin(basis =>
			{
				switch (basis.Get<WorksheetPicking>().ShipmentSpecialPickType)
				{
					case SOPickingWorksheet.worksheetType.Batch: return true;
					case SOPickingWorksheet.worksheetType.Wave: return false;
					case null: return true;
					default: throw new ArgumentOutOfRangeException();
				}
			});
		}
		#endregion

		#region Overrides
		/// Overrides <see cref="WorksheetPicking.IsWorksheetMode(string)"/>
		[PXOverride]
		public virtual bool IsWorksheetMode(string modeCode, Func<string, bool> base_IsWorksheetMode)
			=> base_IsWorksheetMode(modeCode) || modeCode.IsIn(WavePickMode.Value, BatchPickMode.Value);

		/// Overrides <see cref="WorksheetPicking.FindModeForWorksheet(SOPickingWorksheet)"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> FindModeForWorksheet(SOPickingWorksheet sheet, Func<SOPickingWorksheet, ScanMode<PickPackShip>> base_FindModeForWorksheet)
		{
			if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Wave)
				return Basis.FindMode<WavePickMode>();
			
			if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Batch)
				return Basis.FindMode<BatchPickMode>();

			return base_FindModeForWorksheet(sheet);
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState"/>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (MatchMode(state.ModeCode))
			{
				if (state is WMSBase.LocationState locationState)
				{
					Basis.InjectLocationDeactivationOnDefaultLocationOption(locationState);
					Basis.InjectLocationSkippingOnPromptLocationForEveryLineOption(locationState);
				}
				else if (state is WMSBase.InventoryItemState itemState)
				{
					Basis.InjectItemAbsenceHandlingByLocation(itemState);
				}
			}
			else if (state.ModeCode == PickPackShip.PackMode.Value)
			{
				if (state is WMSBase.LocationState locationState)
				{
					InjectPackLocationDeactivatedBasedOnShipmentSpecialPickType(locationState);
				}
			}

			return state;
		}

		/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic"/>
		public class AlterConfirmPickListCommandLogic : WorksheetPicking.ScanExtension<WorksheetPicking.ConfirmPickListCommand.Logic>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive();

			/// Overrides <see cref="WorksheetPicking.ConfirmPickListCommand.Logic.ConfirmPickList(int?)"/>
			[PXOverride]
			public virtual void ConfirmPickList(int? sortingLocationID, Action<int?> base_ConfirmPickList)
			{
				if (sortingLocationID == null && Base2.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Batch)
				{
					Base1.SetScanState<BatchPickMode.SortingLocationState>();
					return;
				}

				base_ConfirmPickList(sortingLocationID);
			}
		}

		/// Overrides <see cref="PickPackShip.PackMode.ConfirmState.Logic"/>
		public class AlterPackConfirmLogic : PickPackShip.ScanExtension<PickPackShip.PackMode.ConfirmState.Logic>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive();

			protected WorksheetPicking WSBasis => Basis.Get<WorksheetPicking>();

			/// Overrides <see cref="PickPackShip.PackMode.ConfirmState.Logic.TargetQty(SOShipLineSplit)"/>
			[PXOverride]
			public virtual decimal? TargetQty(SOShipLineSplit split, Func<SOShipLineSplit, decimal?> base_TargetQty)
			{
				switch (WSBasis.ShipmentSpecialPickType)
				{
					case SOPickingWorksheet.worksheetType.Batch: return split.PickedQty * Graph.GetQtyThreshold(split);
					case SOPickingWorksheet.worksheetType.Wave: return split.PickedQty;
					default: return base_TargetQty(split);
				}
			}
		}

		/// Overrides <see cref="PPSCartSupport"/>
		public class AlterCartSupport : PickPackShip.ScanExtension<PPSCartSupport>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive() && PPSCartSupport.IsActive();

			/// Overrides <see cref="PPSCartSupport.IsCartRequired()"/>
			[PXOverride]
			public virtual bool IsCartRequired(Func<bool> base_IsCartRequired)
			{
				return base_IsCartRequired() ||
					Basis.Setup.Current.UseCartsForPick == true &&
					Basis.Header.Mode == BatchPickMode.Value;
			}
		}
		#endregion

		#region Extensibility
		public abstract class ScanExtension : PXGraphExtension<WaveBatchPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => WaveBatchPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
			public WaveBatchPicking WBBasis => Base3;
		}

		public abstract class ScanExtension<TTargetExtension> : PXGraphExtension<TTargetExtension, WaveBatchPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
			where TTargetExtension : PXGraphExtension<WaveBatchPicking, WorksheetPicking, PickPackShip, PickPackShip.Host>
		{
			protected static bool IsActiveBase() => WaveBatchPicking.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WorksheetPicking WSBasis => Base2;
			public WaveBatchPicking WBBasis => Base3;
			public TTargetExtension Target => Base4;
		}
		#endregion
	}

	public sealed class WaveBatchScanHeader : PXCacheExtension<WorksheetScanHeader, WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => WaveBatchPicking.IsActive();

		#region RemoveFromToteID
		[PXInt]
		public int? RemoveFromToteID { get; set; }
		public abstract class removeFromToteID : BqlInt.Field<removeFromToteID> { }
		#endregion
	}
}
