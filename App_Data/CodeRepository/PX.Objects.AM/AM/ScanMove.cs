using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM
{
	using WMSBase = ScanProductionBase<ScanMove, ScanMove.Host, AMDocType.move>;

	public class ScanMove : WMSBase
	{
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;
		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInMove != true;
		protected bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInMove == true;
		protected bool UseRemainingQty => Setup.Current.UseDefaultQtyInMove == true;
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;

		public class Host : MoveEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Overrides
		protected override IEnumerable<ScanMode<ScanMove>> CreateScanModes() { yield return new MoveMode();  }

		#endregion
		public sealed class MoveMode : ScanMode
		{
			public const string Value = "MOVE";
			public class value : BqlString.Constant<value> { public value() : base(MoveMode.Value) { } }
			public override string Code => Value;
			public override string Description => Msg.Description;

			protected override IEnumerable<ScanState<ScanMove>> CreateStates()
			{
				yield return new OrderTypeState().Intercept.IsStateActive.ByConjoin(basis => basis.UseDefaultOrderType() == false);
				yield return new ProdOrdState();
				yield return new OperationState();
				yield return new WarehouseState();
				yield return new LocationState().Intercept.IsStateActive.ByConjoin(basis => basis.PromptLocationForEveryLine == true || basis.LocationID == null);				
				yield return new LotSerialState(); 
				yield return new ExpireDateState(); 
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<ScanMove>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<OrderTypeState>()
					.NextTo<ProdOrdState>()
					.NextTo<OperationState>()
					.NextTo<WarehouseState>()
					.NextTo<LocationState>()
					.NextTo<LotSerialState>()
					.NextTo<ExpireDateState>()
					.NextTo<ConfirmState>()
				);
			}

			protected override IEnumerable<ScanCommand<ScanMove>> CreateCommands()
			{
				return new ScanCommand<ScanMove>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<ScanMove>> CreateRedirects() => AllWMSRedirects.CreateFor<ScanMove>();

			protected override void ResetMode(bool fullReset = false)
			{
				Clear<OrderTypeState>();
				Clear<ProdOrdState>();
				Clear<OperationState>();
				Clear<WarehouseState>();
				Clear<LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
				Clear<ExpireDateState>();
			}


			#region States			

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

						decimal? newQty = Basis.Qty;

						if (existTransaction != null)
						{
							newQty += existTransaction.Qty;

							if (Basis.CurrentMode.HasActive<LotSerialState>() &&
								lsClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
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
							Basis.Details.Cache.SetValueExt<AMMTran.orderType>(existTransaction, Basis.OrderType);
							Basis.Details.Cache.SetValueExt<AMMTran.prodOrdID>(existTransaction, Basis.ProdOrdID);
							Basis.Details.Cache.SetValueExt<AMMTran.operationID>(existTransaction, Basis.OperationID);
							Basis.Details.Cache.SetValueExt<AMMTran.inventoryID>(existTransaction, Basis.InventoryID);
							Basis.Details.Cache.SetValueExt<AMMTran.siteID>(existTransaction, Basis.SiteID);
							Basis.Details.Cache.SetValueExt<AMMTran.locationID>(existTransaction, Basis.LocationID);
							Basis.Details.Cache.SetValueExt<AMMTran.uOM>(existTransaction, Basis.UOM);
							Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
							Basis.Details.Cache.SetValueExt<AMMTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							if (lsClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
								Basis.Details.Cache.SetValueExt<AMMTran.expireDate>(existTransaction, Basis.ExpireDate);
							existTransaction = Basis.Details.Update(existTransaction);

							if (HasErrors(existTransaction, out var error))
							{
								Base.transactions.Delete(existTransaction);
								return error;
							}

						}

						if (!string.IsNullOrEmpty(Basis.LotSerialNbr))
						{
							foreach (AMMTranSplit split in Basis.Graph.splits.Select())
							{
								Basis.Graph.splits.Cache.SetValueExt<AMMTranSplit.expireDate>(split, Basis.ExpireDate ?? existTransaction.ExpireDate);
								Basis.Graph.splits.Cache.SetValueExt<AMMTranSplit.lotSerialNbr>(split, Basis.LotSerialNbr);
								Basis.Graph.splits.Update(split);
							}
						}

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

					protected virtual bool HasErrors(AMMTran tran, out FlowStatus error)
					{
						error = FlowStatus.Ok;
						return false;
					}


					protected virtual FlowStatus ConfirmRemove()
					{
						AMMTran existTransaction = FindMoveRow();

						if (existTransaction == null)
							return FlowStatus.Fail(Msg.LineMissing, Basis.SelectedInventoryItem.InventoryCD);

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
						var existTransactions = Basis.Details.SelectMain().Where(t =>
							t.OrderType == Basis.OrderType &&
							t.ProdOrdID == Basis.ProdOrdID &&
							t.OperationID == Basis.OperationID &&
							t.InventoryID == Basis.InventoryID &&
							t.SiteID == Basis.SiteID &&
							t.LocationID == (Basis.LocationID ?? t.LocationID));

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
						if(!Basis.InventoryID.HasValue)
						{
							error = FlowStatus.Fail(Msg.InventoryNotSet);
							return false;
						}
						if (Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && Basis.Qty != 1)
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
					public const string Prompt = "Confirm movement of Item {0} x {1} {2}.";
					public const string InventoryNotSet = "The item is not selected.";
					public const string SerialItemNotComplexQty = "Serialized items can be processed only with the base UOM and the 1.00 quantity.";
					public const string LineMissing = "The {0} item is not found in the batch.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the batch.";
					public const string InventoryAdded = "{0} x {1} {2} has been added.";
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
					public const string DocumentReleasing = "The {0} move is being released.";
					public const string DocumentIsReleased = "The move transaction has been successfully released.";
					public const string DocumentReleaseFailed = "The move release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan Move";
			}
			#endregion
		}
	}

}
