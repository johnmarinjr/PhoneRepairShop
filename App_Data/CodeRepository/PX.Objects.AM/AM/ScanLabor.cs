using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AM
{
	using WMSBase = ScanProductionBase<ScanLabor, ScanLabor.Host, AMDocType.labor>;

	public class ScanLabor : WMSBase
	{
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;
		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInMove != true;
		protected bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInMove == true;
		protected bool UseRemainingQty => Setup.Current.UseDefaultQtyInMove == true;
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;
		protected bool IsIndirectLabor => LaborType == AMLaborType.Indirect;
		protected bool UseDefaultEmployee => Base.ampsetup.Current.DefaultEmployee == true;

		public class Host : LaborEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Overrides
		protected override IEnumerable<ScanMode<ScanLabor>> CreateScanModes() { yield return new LaborMode();  }

		#endregion
		public sealed class LaborMode : ScanMode
		{
			public const string Value = "LABR";
			public class value : BqlString.Constant<value> { public value() : base(LaborMode.Value) { } }
			public override string Code => Value;
			public override string Description => Msg.Description;

			protected override IEnumerable<ScanState<ScanLabor>> CreateStates()
			{
				yield return new IndirectCodeState().Intercept.HandleAbsence.ByOverride(
					(basis, barcode, base_HandleAbsence) =>
					{
						if (basis.TryProcessBy<OrderTypeState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication
							| StateSubstitutionRule.KeepStateChange))
							return AbsenceHandling.Done;
						if (basis.TryProcessBy<ProdOrdState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication
							| StateSubstitutionRule.KeepStateChange))
							return AbsenceHandling.Done;
						return base_HandleAbsence(barcode);
					});
				yield return new OrderTypeState().Intercept.IsStateSkippable.ByDisjoin(basis => basis.IsIndirectLabor || basis.UseDefaultOrderType() == true);
				yield return new ProdOrdState().Intercept.IsStateSkippable.ByDisjoin(basis => basis.IsIndirectLabor || basis.ProdOrdID != null);
				yield return new OperationState().Intercept.IsStateActive.ByConjoin(basis => !basis.IsIndirectLabor);
				yield return new WarehouseState().Intercept.IsStateActive.ByConjoin(basis => !basis.IsIndirectLabor); 
				yield return new LocationState().Intercept.IsStateActive.ByConjoin(basis => !basis.IsIndirectLabor && (basis.PromptLocationForEveryLine == true || basis.LocationID == null));
				yield return new LotSerialState(); 
				yield return new ExpireDateState();
				yield return new ShiftState();
				yield return new LaborTimeState();
				yield return new EmployeeState();
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<ScanLabor>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<IndirectCodeState>()
					.NextTo<OrderTypeState>()
					.NextTo<ProdOrdState>()
					.NextTo<OperationState>()
					.NextTo<WarehouseState>()
					.NextTo<LocationState>()
					.NextTo<LotSerialState>()
					.NextTo<ExpireDateState>()
					.NextTo<ShiftState>()
					.NextTo<LaborTimeState>()
					.NextTo<EmployeeState>()
					.NextTo<ConfirmState>()
				);
			}

			protected override IEnumerable<ScanCommand<ScanLabor>> CreateCommands()
			{
				return new ScanCommand<ScanLabor>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<ScanLabor>> CreateRedirects() => AllWMSRedirects.CreateFor<ScanLabor>();

			protected override void ResetMode(bool fullReset = false)
			{
				Clear<IndirectCodeState>();
				Clear<OrderTypeState>();
				Clear<ProdOrdState>();
				Clear<OperationState>();
				Clear<WarehouseState>();
				Clear<LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
				Clear<ExpireDateState>();
				Clear<ShiftState>();
				Clear<LaborTimeState>();
				Clear<EmployeeState>();
			}


			#region States			
			public new sealed class IndirectCodeState : EntityState<AMLaborCode>
			{
				public const string Value = "INDR";
				public class value : BqlString.Constant<value> { public value() : base(IndirectCodeState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;
				protected override AMLaborCode GetByBarcode(string barcode) => AMLaborCode.PK.Find(Basis, barcode);
				protected override Validation Validate(AMLaborCode selection)
				{
					if (selection.LaborType != AMLaborType.Indirect)
						return Validation.Fail(Msg.NotIndirect, selection.LaborCodeID);
					return Validation.Ok;
				}
				protected override void Apply(AMLaborCode selection)
				{
					Basis.LaborCodeID = selection.LaborCodeID;
					Basis.LaborType = AMLaborType.Indirect;
				}
				protected override void ReportSuccess(AMLaborCode selection) => Basis.Reporter.Info(Msg.Ready, selection.LaborCodeID);
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);

				protected override void ClearState()
				{
					Basis.LaborCodeID = null;					
				}

				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan an Order Type, Production Order ID or Indirect Labor Code.";
					public const string Ready = "The {0} indirect code is selected.";
					public const string Missing = "{0} is not found as a production order type, production order number, or indirect code.";
					public const string NotIndirect = "The {0} labor code is not indirect.";
				}
			}

			public new sealed class ShiftState : EntityState<EPShiftCode>
			{
				public const string Value = "SHFT";
				public class value : BqlString.Constant<value> { public value() : base(ShiftState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;
				protected override EPShiftCode GetByBarcode(string barcode) => EPShiftCode.UK.Find(Basis, barcode);
				protected override void Apply(EPShiftCode shift) => Basis.ShiftCD = shift.ShiftCD;
				protected override void ReportSuccess(EPShiftCode shift) => Basis.Reporter.Info(Msg.Ready, shift.ShiftCD);
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
				protected override void ClearState() => Basis.ShiftCD = null;

				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the Shift ID.";
					public const string Ready = "The {0} shift is selected.";
					public const string Missing = "The {0} shift is not found.";
				}
			}

			public new sealed class LaborTimeState : EntityState<int>
			{
				public const string Value = "LTME";
				public class value : BqlString.Constant<value> { public value() : base(LaborTimeState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;
				protected override int GetByBarcode(string barcode)
				{
					TimeSpan time;

					//if the string contains a colon, parse it as is, otherwise process digits into a time
					if (barcode.Contains(":"))
					{
						if (!TimeSpan.TryParse(barcode, out time))
						{
							Basis.ReportError(Msg.TimeFormatInvalid);
							return 0;
						}
					}
					else
					{
						Int32 input;
						if (!Int32.TryParse(barcode, out input))
						{
							Basis.ReportError(Msg.TimeFormatInvalid);
							return 0;
						}
						if (input < 24)
							time = new TimeSpan(input, 0, 0);
						else
						{
							Int32 hours = barcode.Length <= 2 ? 0 : Convert.ToInt32(barcode.Substring(0, barcode.Length - 2));
							Int32 minutes = Convert.ToInt32(barcode.Substring(barcode.Length - 2));
							time = new TimeSpan(hours, minutes, 0);
						}
					}
					return Convert.ToInt32(time.TotalMinutes);
				}
				protected override void Apply(int totalMinutes) => Basis.LaborTime = totalMinutes;
				protected override void ReportSuccess(int totalMinutes) => Basis.Reporter.Info(Msg.Ready, totalMinutes);
				protected override void ClearState() => Basis.LaborTime = null;

				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the Labor Time (HH:MM).";
					public const string Ready = "The {0} labor time is selected.";
					public const string TimeFormatInvalid = "Time must be formatted as HH:MM or HHMM";
				}
			}

			public new sealed class EmployeeState : EntityState<EPEmployee>
			{
				public const string Value = "EMPL";
				public class value : BqlString.Constant<value> { public value() : base(EmployeeState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;
				protected override EPEmployee GetByBarcode(string barcode) => EPEmployee.UK.Find(Basis, barcode);
				protected override Validation Validate(EPEmployee employee)
				{
					if (employee.GetExtension<EPEmployeeExt>()?.AMProductionEmployee != true)
					{
						return Validation.Fail(Msg.EmployeeNotProduction);
					}
					return Validation.Ok;
				}
				protected override void Apply(EPEmployee employee) => Basis.EmployeeID = employee.BAccountID;
				protected override void ReportSuccess(EPEmployee employee) => Basis.Reporter.Info(Msg.Ready, employee.AcctCD);
				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.NotFound);
				protected override void ClearState() => Basis.ShiftCD = null;

				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the Employee ID.";
					public const string Ready = "The {0} employee is selected.";
					public const string NotFound = "The {0} employee is not found.";
					public const string EmployeeNotProduction = "Employee {0} is not set as a Production Employee.";
				}
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				protected override FlowStatus PerformConfirmation() => Get<Logic>().Confirm();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual FlowStatus Confirm()
					{
						if (!CanConfirm(out var error))
							return error;

						return Basis.Remove == true
							? ConfirmRemove()
							: ConfirmAdd();
					}

					protected virtual FlowStatus ConfirmAdd()
					{
						var lsClass = Basis.SelectedLotSerialClass;

						bool newDocument = Basis.Batch == null;
						if (newDocument)
						{
							Basis.BatchView.Insert();
							Basis.BatchView.Current.NoteID = Basis.NoteID;
						}

						AMMTran existTransaction = FindMoveRow();

						Action rollbackAction = null;
						decimal? newQty = Basis.Qty;

						if (existTransaction != null)
						{
							newQty += existTransaction.Qty;

							if (Basis.CurrentMode.HasActive<LotSerialState>() &&
								lsClass?.LotSerTrack == INLotSerTrack.SerialNumbered &&
								newQty != 1) // TODO: use base qty
							{
								return FlowStatus.Fail(Msg.SerialItemNotComplexQty);
							}

							Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
							existTransaction = Basis.Details.Update(existTransaction);
						}
						else
						{
							existTransaction = Basis.Details.Insert();
							Basis.Details.Cache.SetValueExt<AMMTran.laborType>(existTransaction, Basis.LaborType);
							if(!Basis.IsIndirectLabor)
							{
								Basis.Details.Cache.SetValueExt<AMMTran.orderType>(existTransaction, Basis.OrderType);
								Basis.Details.Cache.SetValueExt<AMMTran.prodOrdID>(existTransaction, Basis.ProdOrdID);
								Basis.Details.Cache.SetValueExt<AMMTran.operationID>(existTransaction, Basis.OperationID);
								Basis.Details.Cache.SetValueExt<AMMTran.inventoryID>(existTransaction, Basis.InventoryID);
								Basis.Details.Cache.SetValueExt<AMMTran.siteID>(existTransaction, Basis.SiteID);
								Basis.Details.Cache.SetValueExt<AMMTran.locationID>(existTransaction, Basis.LocationID);
								Basis.Details.Cache.SetValueExt<AMMTran.uOM>(existTransaction, Basis.UOM);	
							}						
							Basis.Details.Cache.SetValueExt<AMMTran.laborCodeID>(existTransaction, Basis.LaborCodeID);
							Basis.Details.Cache.SetValueExt<AMMTran.shiftCD>(existTransaction, Basis.ShiftCD);
							Basis.Details.Cache.SetValueExt<AMMTran.laborTime>(existTransaction, Basis.LaborTime);
							Basis.Details.Cache.SetValueExt<AMMTran.employeeID>(existTransaction, Basis.EmployeeID);
							existTransaction = Basis.Details.Update(existTransaction);

							if (!Basis.IsIndirectLabor)
							{
								Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
								existTransaction = Basis.Details.Update(existTransaction);

								Basis.Details.Cache.SetValueExt<AMMTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
								if (lsClass?.LotSerTrackExpiration == true && Basis.ExpireDate != null)
									Basis.Details.Cache.SetValueExt<AMMTran.expireDate>(existTransaction, Basis.ExpireDate);
								existTransaction = Basis.Details.Update(existTransaction);
							}


							rollbackAction = () => Basis.Details.Delete(existTransaction);
						}

						if (HasErrors(existTransaction, out FlowStatus error))
						{
							rollbackAction();
							return error;
						}
						else
						{
							Basis.DispatchNext(
								Msg.InventoryAdded,
								Basis.SightOf<WMSScanHeader.inventoryID>(),
								Basis.Qty,
								Basis.UOM);

							if (Basis.BatchView.Cache.GetStatus(Basis.BatchView.Current) == PXEntryStatus.Inserted)
								return FlowStatus.Ok.WithSaveSkip;
							else
								return FlowStatus.Ok;
						}
					}

					protected virtual bool HasErrors(AMMTran tran, out FlowStatus error)
					{
						error = FlowStatus.Ok;
						return false;
					}


					protected virtual FlowStatus ConfirmRemove()
					{
						AMMTran existTransaction = FindMoveRow();

						if (existTransaction == null)
							return FlowStatus.Fail(Msg.LineMissing, Basis.LaborCodeID);

						if (existTransaction.Qty == Basis.Qty)
						{
							Basis.Details.Delete(existTransaction);
						}
						else
						{
							var newQty = existTransaction.Qty - Basis.Qty;

							if (!Basis.IsValid<AMMTran.qty, AMMTran>(existTransaction, newQty, out string error))
								return FlowStatus.Fail(error);

							Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
							Basis.Details.Update(existTransaction);
						}

						Basis.DispatchNext(
							Msg.InventoryRemoved,
							Basis.SightOf<WMSScanHeader.inventoryID>(),
							Basis.Qty,
							Basis.UOM);

						if (Basis.BatchView.Cache.GetStatus(Basis.BatchView.Current) == PXEntryStatus.Inserted)
							return FlowStatus.Ok.WithSaveSkip;
						else
							return FlowStatus.Ok;
					}

					protected virtual AMMTran FindMoveRow()
					{
						IEnumerable<AMMTran> existTransactions;

						if(Basis.IsIndirectLabor)
						{
							existTransactions = Basis.Details.SelectMain().Where(t =>
								t.LaborType == Basis.LaborType &&
								t.LaborCodeID == Basis.LaborCodeID &&
								t.ShiftCD == Basis.ShiftCD &&
								t.EmployeeID == Basis.EmployeeID);
						}
						else
						{
							existTransactions = Basis.Details.SelectMain().Where(t =>								
								t.ShiftCD == Basis.ShiftCD &&
								t.EmployeeID == Basis.EmployeeID &&
								t.OrderType == Basis.OrderType &&
								t.ProdOrdID == Basis.ProdOrdID &&
								t.OperationID == Basis.OperationID &&
								t.InventoryID == Basis.InventoryID &&
								t.SiteID == Basis.SiteID &&
								t.LocationID == (Basis.LocationID ?? t.LocationID));
						}


						AMMTran existTransaction = null;

						if (Basis.CurrentMode.HasActive<LotSerialState>())
						{
							foreach (var tran in existTransactions)
							{
								Basis.Details.Current = tran;
								if (Basis.Graph.splits.SelectMain().Any(t => (t.LotSerialNbr ?? "") == (Basis.LotSerialNbr ?? "")))
								{
									existTransaction = tran;
									break;
								}
							}
						}
						else
						{
							existTransaction = existTransactions.FirstOrDefault();
						}

						return existTransaction;
					}

					protected virtual bool CanConfirm(out FlowStatus error)
					{
						if(Basis.Batch?.Released == true)
						{
							error = FlowStatus.Fail(PX.Objects.IN.Messages.Document_Status_Invalid);
							return false;
						}
						if (!Basis.IsIndirectLabor && Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && Basis.Qty != 1)
						{
							error = FlowStatus.Fail(Msg.SerialItemNotComplexQty);
							return false;
						}
						error = FlowStatus.Ok;
						return true;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string Prompt = "Confirm Labor Item {0} x {1} {2}.";
					public const string SerialItemNotComplexQty = "Serialized items can be processed only with the base UOM and the 1.00 quantity.";
					public const string LineMissing = "The {0} item is not found in the batch.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the batch.";
					public const string InventoryAdded = "{0} x {1} {2} has been added to the batch.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public new sealed class ReleaseCommand : WMSBase.ReleaseCommand
			{
				protected override string DocumentReleasing => Msg.DocumentReleasing;
				protected override string DocumentIsReleased => Msg.DocumentIsReleased;
				protected override string DocumentReleaseFailed => Msg.DocumentReleaseFailed;

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WMSBase.ReleaseCommand.Msg
				{
					public const string DocumentReleasing = "The {0} labor is being released.";
					public const string DocumentIsReleased = "The labor transaction has been successfully released.";
					public const string DocumentReleaseFailed = "The labor release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan Labor";
			}
			#endregion
		}
	}

}
