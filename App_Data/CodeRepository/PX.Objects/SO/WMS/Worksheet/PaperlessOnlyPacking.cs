using System;
using System.Linq;
using System.Collections.Generic;

using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS.Worksheet
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class PaperlessOnlyPacking : PaperlessPicking.ScanExtension
	{
		public static bool IsActive() => IsActiveBase();
		public bool IsPackOnly => !Basis.HasPick && Basis.HasPack;

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.CreateScanModes"/>
		[PXOverride]
		public virtual IEnumerable<ScanMode<PickPackShip>> CreateScanModes(Func<IEnumerable<ScanMode<PickPackShip>>> base_CreateScanModes)
		{
			foreach (var mode in base_CreateScanModes())
				yield return mode;

			yield return new PaperlessPackOnlyMode();
		}

		public sealed class PaperlessPackOnlyMode : PickPackShip.ScanMode
		{
			public const string Value = "PPAO";
			public class value : BqlString.Constant<value> { public value() : base(PaperlessPackOnlyMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.DisplayName;

			protected override bool IsModeActive() => Basis.Get<PaperlessOnlyPacking>().IsPackOnly;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PaperlessPicking.PickListState();
				yield return new WMSBase.LocationState();
				yield return new PickPackShip.PackMode.BoxState();
				yield return new WMSBase.InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true };
				yield return new WMSBase.LotSerialState();
				yield return new WMSBase.ExpireDateState() { IsForIssue = true };
				yield return new PickPackShip.PackMode.BoxWeightState();
				yield return new PickPackShip.PackMode.ConfirmState();
				yield return new PickPackShip.CommandOrShipmentOnlyState();

				// directly set states
				yield return new PaperlessPicking.WarehouseState();
				yield return new PaperlessPicking.NearestLocationState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.ForkBy(basis => basis.Remove != true)
					.PositiveBranch(pfl => pfl
						.From<PaperlessPicking.PickListState>()
						.NextTo<PickPackShip.PackMode.BoxState>()
						.NextTo<WMSBase.LocationState>()
						.NextTo<WMSBase.InventoryItemState>()
						.NextTo<WMSBase.LotSerialState>()
						.NextTo<WMSBase.ExpireDateState>())
					.NegativeBranch(nfl => nfl
						.From<PaperlessPicking.PickListState>()
						.NextTo<WMSBase.InventoryItemState>()
						.NextTo<WMSBase.LotSerialState>()
						.NextTo<WMSBase.LocationState>()));
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new PickPackShip.PackMode.RemoveCommand();
				yield return new WMSBase.QtySupport.SetQtyCommand();
				yield return new PickPackShip.PackMode.ConfirmPackageCommand();
				yield return new ConfirmPackListCommand();
				yield return new PaperlessPicking.TakeNextPickListCommand();
				yield return new ConfirmPackListAndTakeNextCommand();
				yield return new PaperlessPicking.ConfirmLineQtyCommand();
			}

			protected override IEnumerable<ScanQuestion<PickPackShip>> CreateQuestions()
			{
				yield return new PickPackShip.PackMode.WeightSkipQuestion();
				yield return new PickPackShip.PackMode.ConfirmBoxQuestion(); // backward compatibility
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PaperlessPicking.PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<WMSBase.LocationState>(when: fullReset);
				Clear<PickPackShip.PackMode.BoxState>(when: fullReset);
				Clear<WMSBase.InventoryItemState>(when: fullReset);
				Clear<WMSBase.LotSerialState>();
				Clear<PickPackShip.PackMode.BoxWeightState>();
				Clear<WMSBase.ExpireDateState>();

				if (fullReset)
					Get<PickPackShip.PackMode.Logic>().PackageLineNbrUI = null;
			}
			#endregion

			#region Commands
			public sealed class ConfirmPackListCommand : PickPackShip.ScanCommand
			{
				public override string Code => "CONFIRM*PACK";
				public override string ButtonName => "scanConfirmPackList";
				public override string DisplayName => Msg.DisplayName;
				protected override bool IsEnabled => Basis.DocumentIsEditable;

				protected override bool Process()
				{
					Basis.Get<Logic>().ConfirmPackList();
					return true;
				}

				#region Logic
				[PXProtectedAccess]
				public abstract class Logic : PickPackShip.ScanExtension<PickPackShip.ConfirmShipmentCommand.Logic>
				{
					public static bool IsActive() => PaperlessOnlyPacking.IsActive();

					public virtual void ConfirmPackList()
					{
						if (!CanConfirm(false))
							return;

						var packLogic = Basis.Get<PickPackShip.PackMode.Logic>();
						if (packLogic.SelectedPackage?.Confirmed == false)
							if (packLogic.AutoConfirmPackage(Basis.Setup.Current.ConfirmEachPackageWeight == false) == false || Basis.Header.ScanState == PickPackShip.PackMode.BoxWeightState.Value)
								return;

						int? packageLineNbr = packLogic.PackageLineNbr;
						Basis.CurrentMode.Reset(fullReset: false);
						packLogic.PackageLineNbr = packageLineNbr;

						packLogic.HasSingleAutoPackage(Basis.RefNbr, out SOPackageDetailEx autoPackageToConfirm);

						var (shipmentNbr, setup, userSetup) = (Basis.RefNbr, Basis.Setup.Current, PickPackShip.UserSetup.For(Basis));

						Basis.SaveChanges();

						Basis
						.WaitFor<SOShipment>((basis, doc) => ConfirmShipmentHandler(doc.ShipmentNbr, setup, userSetup, autoPackageToConfirm))
						.WithDescription(Msg.InProcess, Basis.RefNbr)
						.ActualizeDataBy((basis, doc) => SOShipment.PK.Find(basis, doc))
						.OnSuccess(ConfigureOnSuccessAction)
						.OnFail(x => x.Say(Msg.Fail))
						.BeginAwait(Basis.Shipment);
					}

					public virtual void ConfigureOnSuccessAction(ScanLongRunAwaiter<PickPackShip, SOShipment>.IResultProcessor onSuccess)
					{
						onSuccess
							.Say(Msg.Success)
							.ChangeStateTo<WorksheetPicking.PickListState>();
					}

					protected static void ConfirmShipmentHandler(string shipmentNbr, SOPickPackShipSetup setup, SOPickPackShipUserSetup userSetup, SOPackageDetailEx autoPackageToConfirm)
					{
						PXRedirectToUrlException redirectToExternalApplication = null;
						using (var ts = new PXTransactionScope())
						{
							PickPackShip.WithSuppressedRedirects(() =>
							{
								var wsGraph = PXGraph.CreateInstance<SOPickingWorksheetReview>();
								var (sheet, pickList, pickingJob) =
									SelectFrom<SOPickingWorksheet>.
									InnerJoin<SOPicker>.On<SOPicker.FK.Worksheet>.
									InnerJoin<SOPickingJob>.On<SOPickingJob.FK.Picker>.
									Where<SOPickingWorksheet.singleShipmentNbr.IsEqual<@P.AsString>>.
									View.Select(wsGraph, shipmentNbr)
									.AsEnumerable().Cast<PXResult<SOPickingWorksheet, SOPicker, SOPickingJob>>().Single();

								wsGraph.PickListConfirmation.ConfirmPickList(pickList, sortingLocationID: null);
								wsGraph.PickListConfirmation.FulfillShipmentsAndConfirmWorksheet(sheet);

								if (pickingJob.AutomaticShipmentConfirmation == true)
								{
									try
									{
										PXGraph
											.CreateInstance<SOShipmentEntry>()
											.FindImplementation<PickPackShip.ConfirmShipmentCommand.PickPackShipShipmentConfirmation>()
											.ApplyPickedQtyAndConfirmShipment(shipmentNbr, false, setup, userSetup, autoPackageToConfirm);
									}
									catch (PXRedirectToUrlException ex)
									{
										redirectToExternalApplication = ex;
									}
								}
							});
							ts.Complete();
						}

						if (redirectToExternalApplication != null)
							throw redirectToExternalApplication;
					}

					/// Uses <see cref="PickPackShip.ConfirmShipmentCommand.Logic.CanConfirm(bool)"/>
					[PXProtectedAccess(typeof(PickPackShip.ConfirmShipmentCommand.Logic))]
					protected abstract bool CanConfirm(bool confirmAsIs);
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg : WorksheetPicking.ConfirmPickListCommand.Msg
				{
					public new const string DisplayName = "Confirm Pack List";
				}
				#endregion
			}

			public sealed class ConfirmPackListAndTakeNextCommand : PickPackShip.ScanCommand
			{
				public override string Code => "CONFIRM*PACK*AND*NEXT";
				public override string ButtonName => "scanConfirmPackListAndTakeNext";
				public override string DisplayName => PaperlessPicking.ConfirmPickListAndTakeNextCommand.Msg.DisplayName;
				protected override bool IsEnabled => Basis.CurrentMode.Commands.OfType<ConfirmPackListCommand>().First().IsApplicable;

				private bool _inProcess = false;
				protected override bool Process()
				{
					try
					{
						_inProcess = true;
						return Basis.CurrentMode.Commands.OfType<ConfirmPackListCommand>().First().Execute();
					}
					finally
					{
						_inProcess = false;
					}
				}

				/// Overrides <see cref="ConfirmPackListCommand.Logic"/>
				public class AlterConfirmPackListCommandLogic : PickPackShip.ScanExtension<ConfirmPackListCommand.Logic>
				{
					private bool _visited; // addresses inheritance hierarchy bug of graph extensions

					public static bool IsActive() => PaperlessPicking.IsActive();

					/// Overrides <see cref="ConfirmPackListCommand.Logic.ConfigureOnSuccessAction(ScanLongRunAwaiter{PickPackShip, SOShipment}.IResultProcessor)"/>
					[PXOverride]
					public virtual void ConfigureOnSuccessAction(ScanLongRunAwaiter<PickPackShip, SOShipment>.IResultProcessor onSuccess, Action<ScanLongRunAwaiter<PickPackShip, SOShipment>.IResultProcessor> base_ConfigureOnSuccessAction)
					{
						base_ConfigureOnSuccessAction(onSuccess);

						if (!_visited && Base1.CurrentMode.Commands.OfType<ConfirmPackListAndTakeNextCommand>().FirstOrDefault()?._inProcess == true)
							onSuccess.Do((basis, picker) => basis.CurrentMode.Commands.OfType<PaperlessPicking.TakeNextPickListCommand>().First().Execute());

						_visited = true;
					}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Paperless Pack";
				public const string BoxConfirmOrContinueByLocationPrompt = "Confirm package, or scan the next location.";
			}
			#endregion
		}

		#region Decorations
		public virtual void InjectPickListHandleAbsenceByPackShipment(PaperlessPicking.PickListState pickList)
		{
			pickList.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
			{
				if (basis.FindMode<PickPackShip.PackMode>() is PickPackShip.PackMode packMode && packMode.IsActive)
				{
					if (packMode.TryProcessBy<PickPackShip.PackMode.ShipmentState>(barcode) == true)
					{
						basis.SetScanMode<PickPackShip.PackMode>();
						basis.FindState<PickPackShip.PackMode.ShipmentState>().Process(barcode);
						return AbsenceHandling.Done;
					}
				}

				return AbsenceHandling.Skipped;
			});
		}

		public virtual void InjectPickListShipmentValidation(PaperlessPicking.PickListState pickListState)
		{
			pickListState.Intercept.Validate.ByAppend((basis, pickList) =>
			{
				var shipment = SOPickingWorksheet.FK.SingleShipment.FindParent(basis, pickList);
				return basis
					.FindMode<PickPackShip.PackMode>()?
					.TryValidate(shipment)
					.By<PickPackShip.PackMode.ShipmentState>()
					?? Validation.Ok;
			});
		}

		public virtual void InjectPickListDispatchToCommandStateOnCantPack(PaperlessPicking.PickListState pickListState)
		{
			pickListState.Intercept.SetNextState.ByReplace(basis =>
			{
				var mode = basis.Get<PickPackShip.PackMode.Logic>();
				if (basis.Remove == true || mode.CanPack || mode.HasConfirmableBoxes)
					basis.DispatchNext();
				else
					basis.SetScanState(BuiltinScanStates.Command, PickPackShip.PackMode.Msg.Completed, basis.RefNbr);
			});
		}

		public virtual void InjectShipmentAbsenceHandlingByWorksheetOfSingleType(PickPackShip.PackMode.ShipmentState packShipment)
		{
			packShipment.Intercept.HandleAbsence.ByPrepend((basis, barcode) =>
			{
				if (barcode.Contains("/") == false)
				{
					if (basis.FindMode<PaperlessPackOnlyMode>() is PaperlessPackOnlyMode paperlessPack && paperlessPack.IsActive)
					{
						if (paperlessPack.TryProcessBy<WorksheetPicking.PickListState>(barcode, StateSubstitutionRule.KeepAbsenceHandling))
						{
							basis.SetScanMode<PaperlessPackOnlyMode>();
							basis.FindState<WorksheetPicking.PickListState>().Process(barcode);
							return AbsenceHandling.Done;
						}
					}
				}

				return AbsenceHandling.Skipped;
			});
		}

		public virtual void InjectItemPromptForPackageConfirmOnPaperlessPack(WMSBase.InventoryItemState itemState)
		{
			itemState.Intercept.StatePrompt.ByOverride((basis, base_StatePrompt) =>
				basis.Get<PickPackShip.PackMode.Logic>().With(mode =>
					basis.Remove != true && mode.CanConfirmPackage
						? PickPackShip.PackMode.Msg.BoxConfirmOrContinuePrompt
						: null)
				?? base_StatePrompt());
		}

		public virtual void InjectLocationPromptForPackageConfirmOnPaperlessPack(WMSBase.LocationState locationState)
		{
			locationState.Intercept.StatePrompt.ByOverride((basis, base_StatePrompt) =>
				basis.Get<PickPackShip.PackMode.Logic>().With(mode =>
					basis.Remove != true && mode.CanConfirmPackage
						? PaperlessPackOnlyMode.Msg.BoxConfirmOrContinueByLocationPrompt
						: null)
				?? base_StatePrompt());
		}

		public virtual void InjectLocationAbsenceHandlingByBox(WMSBase.LocationState locationState)
		{
			locationState.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
				basis.Get<PickPackShip.PackMode.Logic>().TryAutoConfirmCurrentPackageAndLoadNext(barcode) == false
					? AbsenceHandling.Skipped
					: AbsenceHandling.Done);
		}

		public virtual void InjectConfirmCombinedFromPackAndWorksheet(PickPackShip.PackMode.ConfirmState confirmState)
		{
			confirmState.Intercept.PerformConfirmation.ByOverride((basis, base_PerformConfirmation) =>
			{
				var ppBasis = basis.Get<PaperlessPicking>();

				bool remove = basis.Remove == true;

				if (basis.LocationID == null && !basis.DefaultLocation)
					basis.LocationID = ppBasis.LastVisitedLocationID;

				SOPickerListEntry pickedSplit = remove
					? ppBasis.GetSplitForRemoval()
					: ppBasis.GetWantedSplit();

				FlowStatus worksheetConfirm = ppBasis.WSBasis.ConfirmSplit(pickedSplit, Sign.MinusIf(remove) * basis.BaseQty);
				if (worksheetConfirm.IsError != false)
					return worksheetConfirm;

				FlowStatus packConfirm = base_PerformConfirmation();
				if (packConfirm.IsError != false)
					return packConfirm;

				basis.Get<PaperlessPicking.ConfirmState.Logic>().VisitSplit(pickedSplit);

				return packConfirm;
			});
		}

		public virtual void InjectTakeNextEnablingForPaperlessPackOnly(PaperlessPicking.TakeNextPickListCommand takeNext)
		{
			takeNext.Intercept.IsEnabled.ByDisjoin(basis =>
				basis.CurrentMode is PaperlessPackOnlyMode && (basis.RefNbr == null || basis.DocumentIsConfirmed || basis.Get<WorksheetPicking>().NotStarted));
		}
		#endregion

		#region Overrides
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> DecorateScanMode(ScanMode<PickPackShip> original, Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
		{
			var mode = base_DecorateScanMode(original);

			if (mode is PickPackShip.PackMode pack && IsPackOnly)
				pack.Intercept.CreateCommands.ByAppend(basis => new[] { new PaperlessPicking.TakeNextPickListCommand() });

			return mode;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState(ScanState{TSelf})"/>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (state.ModeCode == PaperlessPackOnlyMode.Value)
			{
				if (state is PaperlessPicking.PickListState pickList)
				{
					InjectPickListShipmentValidation(pickList);
					InjectPickListDispatchToCommandStateOnCantPack(pickList);
					InjectPickListHandleAbsenceByPackShipment(pickList);
					PPBasis.InjectPickListPaperless(pickList);
				}
				else if (state is WMSBase.LocationState locState)
				{
					PPBasis.InjectNavigationOnLocation(locState);
					InjectLocationPromptForPackageConfirmOnPaperlessPack(locState);
				}
				else if (state is WMSBase.InventoryItemState itemState)
				{
					PPBasis.InjectNavigationOnItem(itemState);
					InjectItemPromptForPackageConfirmOnPaperlessPack(itemState);
					Basis.Get<PickPackShip.PackMode.Logic>().InjectItemAbsenceHandlingByBox(itemState);
				}
				else if (state is WMSBase.LotSerialState lsState)
				{
					PPBasis.InjectNavigationOnLotSerial(lsState);
					Basis.InjectLotSerialDeactivationOnDefaultLotSerialOption(lsState, isEntranceAllowed: true);
				}
				else if (state is PickPackShip.PackMode.ConfirmState confirmState)
				{
					InjectConfirmCombinedFromPackAndWorksheet(confirmState);
				}
			}
			else
			{
				if (state is PickPackShip.PackMode.ShipmentState packShipment && IsPackOnly)
				{
					PPBasis.InjectShipmentPromptWithTakeNext(packShipment);
					PPBasis.InjectSuppressShipmentWithWorksheetOfSingleType(packShipment);
					InjectShipmentAbsenceHandlingByWorksheetOfSingleType(packShipment);
				}
			}

			return state;
		}

		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanCommand"/>
		[PXOverride]
		public virtual ScanCommand<PickPackShip> DecorateScanCommand(ScanCommand<PickPackShip> original, Func<ScanCommand<PickPackShip>, ScanCommand<PickPackShip>> base_DecorateScanCommand)
		{
			var command = base_DecorateScanCommand(original);

			if (command.ModeCode == PaperlessPackOnlyMode.Value)
			{
				if (command is WMSBase.RemoveCommand remove)
					PPBasis.InjectRemoveClearLocationAndInventory(remove);
				else if (command is PaperlessPicking.TakeNextPickListCommand takeNext)
					InjectTakeNextEnablingForPaperlessPackOnly(takeNext);
			}

			return command;
		}

		/// Overrides <see cref="WorksheetPicking.SetPickList(PXResult{SOPickingWorksheet, SOPicker})"/>
		[PXOverride]
		public virtual void SetPickList(PXResult<SOPickingWorksheet, SOPicker> pickList, Action<PXResult<SOPickingWorksheet, SOPicker>> base_SetPickList)
		{
			base_SetPickList(pickList);

			if (Basis.CurrentMode is PaperlessPackOnlyMode)
			{
				Basis.RefNbr = pickList?.GetItem<SOPickingWorksheet>()?.SingleShipmentNbr;
				Basis.Graph.Document.Current = SOShipment.PK.Find(Basis, Basis.RefNbr);
				Basis.NoteID = Basis.Shipment?.NoteID;
			}
		}

		/// Overrides <see cref="WorksheetPicking.CheckAvailability(decimal)"/>
		[PXOverride]
		public virtual FlowStatus CheckAvailability(decimal deltaQty, Func<decimal, FlowStatus> base_CheckAvailability)
		{
			return Basis.CurrentMode is PaperlessPackOnlyMode
				? FlowStatus.Ok
				: base_CheckAvailability(deltaQty);
		}

		/// Overrides <see cref="WorksheetPicking.ShowWorksheetNbrForMode(string)"/>
		[PXOverride]
		public virtual bool ShowWorksheetNbrForMode(string modeCode, Func<string, bool> base_ShowWorksheetNbrForMode)
			=> base_ShowWorksheetNbrForMode(modeCode) && modeCode != PaperlessPackOnlyMode.Value;

		/// Overrides <see cref="WorksheetPicking.FindModeForWorksheet(SOPickingWorksheet)"/>
		[PXOverride]
		public virtual ScanMode<PickPackShip> FindModeForWorksheet(SOPickingWorksheet sheet, Func<SOPickingWorksheet, ScanMode<PickPackShip>> base_FindModeForWorksheet)
		{
			if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Single && IsPackOnly)
				return Basis.FindMode<PaperlessPackOnlyMode>();

			return base_FindModeForWorksheet(sheet);
		}

		/// Overrides <see cref="PickPackShip.DocumentIsConfirmed"/>
		[PXOverride]
		public virtual bool get_DocumentIsConfirmed(Func<bool> base_DocumentIsConfirmed) => Basis.CurrentMode is PaperlessPackOnlyMode
			? Basis.Shipment?.Confirmed == true || WSBasis.PickList?.Confirmed == true
			: base_DocumentIsConfirmed();

		public class AlterTakeNextPickListCommandLogic : PaperlessPicking.ScanExtension<PaperlessPicking.TakeNextPickListCommand.Logic>
		{
			public static bool IsActive() => IsActiveBase();

			/// Overrides <see cref="PaperlessPicking.TakeNextPickListCommand.Logic.TakeNext"/>
			[PXOverride]
			public virtual bool TakeNext(Func<bool> base_TakeNext)
			{
				if (Basis.RefNbr == null && Basis.CurrentMode is PickPackShip.PackMode && Basis.Get<PaperlessOnlyPacking>().IsPackOnly)
					Basis.SetScanMode<PaperlessPackOnlyMode>();

				return base_TakeNext();
			}

			/// Overrides <see cref="PaperlessPicking.TakeNextPickListCommand.Logic.ApplyCommonFilters(PXSelectBase{SOPickingJob})"/>
			[PXOverride]
			public virtual void ApplyCommonFilters(PXSelectBase<SOPickingJob> command, Action<PXSelectBase<SOPickingJob>> base_ApplyCommonFilters)
			{
				base_ApplyCommonFilters(command);
				if (Basis.CurrentMode is PickPackShip.PackMode || Basis.CurrentMode is PaperlessPackOnlyMode)
					command.WhereAnd<Where<SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>>>();
			}
		}

		[PXProtectedAccess]
		public abstract class AlterPackModeLogic : PickPackShip.ScanExtension<PickPackShip.PackMode.Logic>
		{
			public static bool IsActive() => PaperlessOnlyPacking.IsActive();

			/// Overrides <see cref="PickPackShip.PackMode.Logic.CanPack"/>
			[PXOverride]
			public virtual bool get_CanPack(Func<bool> base_CanPack)
			{
				if (Basis.Get<PaperlessOnlyPacking>().IsPackOnly && Basis.CurrentMode is PaperlessPackOnlyMode)
					return Target.PickedForPack.SelectMain().Any(s => s.PackedQty < s.Qty && RelatedPickListSplitForceCompleted.GetValue(Basis, s) != true);
				else
					return base_CanPack();
			}

			/// Overrides <see cref="PickPackShip.PackMode.Logic.ShowPackTab(ScanHeader)"/>
			[PXOverride]
			public virtual bool ShowPackTab(ScanHeader row, Func<ScanHeader, bool> base_ShowPackTab)
			{
				return base_ShowPackTab(row) || Basis.Get<PaperlessOnlyPacking>().IsPackOnly && row.Mode == PaperlessPackOnlyMode.Value;
			}

			/// Overrides <see cref="PickPackShip.GetCommandOrShipmentOnlyPrompt"/>
			[PXOverride]
			public virtual string GetCommandOrShipmentOnlyPrompt(Func<string> base_GetCommandOrShipmentOnlyPrompt)
			{
				if (Basis.CurrentMode is PaperlessPackOnlyMode && Basis.Get<PickPackShip.PackMode.Logic>() is PickPackShip.PackMode.Logic mode && mode.CanConfirmPackage)
					return PickPackShip.PackMode.Msg.BoxConfirmPrompt;

				return base_GetCommandOrShipmentOnlyPrompt();
			}

			/// Uses <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.RequireConfirmation"/>
			[PXProtectedAccess(typeof(PickPackShip))]
			protected abstract bool RequireConfirmation();

			protected virtual void _(Events.RowSelected<ScanHeader> args)
			{
				if (args.Row?.Mode == PaperlessPackOnlyMode.Value)
				{
					Basis.ScanConfirm.SetVisible(true);
					Basis.ScanConfirm.SetEnabled(Basis.ExplicitConfirmation || RequireConfirmation() || Basis.Info.Current?.MessageType == ScanMessageTypes.Warning);
					Target.ReviewPack.SetVisible(Base.IsMobile);
				}
			}
		}

		public class AlterPaperlessPickingConfirmLineQtyCommandLogic : PaperlessPicking.ScanExtension<PaperlessPicking.ConfirmLineQtyCommand.Logic>
		{
			public static bool IsActive() => PaperlessOnlyPacking.IsActive();

			/// Overrides <see cref="PaperlessPicking.ConfirmLineQtyCommand.Logic.ReopenQtyOfCurrentSplit"/>
			[PXOverride]
			public virtual bool ReopenQtyOfCurrentSplit(Func<bool> base_ReopenQtyOfCurrentSplit)
			{
				if (Basis.CurrentMode is PaperlessPackOnlyMode)
				{
					var selectedSplit = Basis.Get<PickPackShip.PackMode.Logic>().PickedForPack.Current;
					if (selectedSplit != null)
					{
						var pickListEntry = Basis.Get<RelatedPickListSplitForceCompleted>().GetRelatedPickListEntry(selectedSplit);
						if (pickListEntry != null)
							WSBasis.PickListOfPicker.Current = pickListEntry;
					}
				}

				return base_ReopenQtyOfCurrentSplit();
			}
		}

		public class AlterWorksheetPicking : WorksheetPicking.ScanExtension
		{
			public static bool IsActive() => PaperlessOnlyPacking.IsActive();

			/// <see cref="WorksheetPicking.InjectValidationPickFirst(PickPackShip.ShipmentState)"/>
			[PXOverride]
			public virtual void InjectValidationPickFirst(PickPackShip.ShipmentState refNbrState,
				Action<PickPackShip.ShipmentState> base_InjectValidationPickFirst)
			{
				if (Basis.Get<PaperlessOnlyPacking>().IsPackOnly)
					refNbrState.Intercept.Validate.ByAppend((basis, shipment) =>
						shipment.CurrentWorksheetNbr != null && shipment.Picked == false && SOPickingWorksheet.PK.Find(basis, shipment.CurrentWorksheetNbr).With(w => w.WorksheetType != SOPickingWorksheet.worksheetType.Single)
							? Validation.Fail(PickPackShip.PackMode.ShipmentState.Msg.ShouldBePickedFirst, shipment.ShipmentNbr)
							: Validation.Ok);
				else
					base_InjectValidationPickFirst(refNbrState);
			}
		}
		#endregion

		#region Attached Fields
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		[PXUIField(DisplayName = "Quantity Confirmed")]
		public class RelatedPickListSplitForceCompleted : PickPackShip.FieldAttached.To<SOShipLineSplit>.AsBool.Named<RelatedPickListSplitForceCompleted>
		{
			private Dictionary<int, int> splitsToEntries;

			protected override bool? Visible => PaperlessOnlyPacking.IsActive() && Base.WMS.CurrentMode is PaperlessPackOnlyMode;
			public override bool? GetValue(SOShipLineSplit row)
			{
				if (Visible == false || row == null || row.SplitLineNbr == null)
					return null;

				SOPickerListEntry pickListEntry = GetRelatedPickListEntry(row);

				return pickListEntry?.ForceCompleted;
			}

			public virtual SOPickerListEntry GetRelatedPickListEntry(SOShipLineSplit row)
			{
				if (splitsToEntries == null || !splitsToEntries.ContainsKey(row.SplitLineNbr.Value))
				{
					var allShipmentSplits =
						SelectFrom<Table.SOShipLineSplit>.
						Where<Table.SOShipLineSplit.FK.Shipment.SameAsCurrent>.
						OrderBy<
							Table.SOShipLineSplit.locationID.Asc,
							Table.SOShipLineSplit.inventoryID.Asc,
							Table.SOShipLineSplit.subItemID.Asc,
							Table.SOShipLineSplit.lotSerialNbr.Asc,
							Table.SOShipLineSplit.baseQty.Asc,
							Table.SOShipLineSplit.basePickedQty.Asc,
							Table.SOShipLineSplit.splitLineNbr.Asc
						>.
						View.Select(Base).RowCast<Table.SOShipLineSplit>().ToArray();

					var allPickListEntries =
						SelectFrom<SOPickerListEntry>.
						Where<SOPickerListEntry.FK.Picker.SameAsCurrent>.
						OrderBy<
							SOPickerListEntry.locationID.Asc,
							SOPickerListEntry.inventoryID.Asc,
							SOPickerListEntry.subItemID.Asc,
							SOPickerListEntry.lotSerialNbr.Asc,
							SOPickerListEntry.baseQty.Asc,
							SOPickerListEntry.basePickedQty.Asc,
							SOPickerListEntry.entryNbr.Asc
						>.
						View.Select(Base).RowCast<SOPickerListEntry>().ToArray();

					splitsToEntries = allShipmentSplits
						.Zip(allPickListEntries, (s, e) => (SplitKey: s.SplitLineNbr.Value, EntryNbr: e.EntryNbr.Value))
						.ToDictionary(pair => pair.SplitKey, pair => pair.EntryNbr);
				}

				if (splitsToEntries.TryGetValue(row.SplitLineNbr.Value, out int entryNbr))
				return Base.WMS.Get<WorksheetPicking>().PickListOfPicker.Search<SOPickerListEntry.entryNbr>(entryNbr);
				else
					return null;
			}
		}
		#endregion
	}
}
