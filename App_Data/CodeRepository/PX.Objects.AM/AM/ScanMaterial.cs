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

namespace PX.Objects.AM
{
	using WMSBase = ScanProductionBase<ScanMaterial, ScanMaterial.Host, AMDocType.material>;

	public class ScanMaterial : WMSBase
	{
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;
		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInMaterials != true;
		protected bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInMaterials == true;
		protected bool UseRemainingQty => Setup.Current.UseRemainingQtyInMaterials == true;
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;

		public class Host : MaterialEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Overrides
		protected override IEnumerable<ScanMode<ScanMaterial>> CreateScanModes() { yield return new MaterialMode();  }

		#endregion
		public sealed class MaterialMode : ScanMode
		{
			public const string Value = "MATL";
			public class value : BqlString.Constant<value> { public value() : base(MaterialMode.Value) { } }
			public override string Code => Value;
			public override string Description => Msg.Description;

			protected override IEnumerable<ScanState<ScanMaterial>> CreateStates()
			{
				yield return new OrderTypeState().Intercept.IsStateActive.ByConjoin(basis => basis.UseDefaultOrderType() == false);
				yield return new ProdOrdState();
				yield return new OperationState();
				yield return new WarehouseState(); //.Intercept.IsStateSkippable.ByConjoin(basis => basis.IsWarehouseRequired() == false);
				yield return new LocationState().Intercept.IsStateActive.ByConjoin(basis => basis.PromptLocationForEveryLine == true || basis.LocationID == null);
				yield return new MaterialItemState();
				yield return new LotSerialState(); //.Intercept.IsStateSkippable.ByConjoin(basis => basis.IsLotSerialRequired() == false);
				yield return new ExpireDateState(); //.Intercept.IsStateActive.ByConjoin(basis => basis.IsExpirationDateRequired() == true);
				yield return new ParentLotSerialState().Intercept.IsStateActive.ByConjoin(basis => basis.ParentLotSerialRequired == ParentLotSerialAssignment.OnIssue);
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<ScanMaterial>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<OrderTypeState>()
					.NextTo<ProdOrdState>()
					.NextTo<OperationState>()
					.NextTo<WarehouseState>()
					.NextTo<LocationState>()
					.NextTo<InventoryItemState>()
					.NextTo<LotSerialState>()
					.NextTo<ExpireDateState>()
					.NextTo<ParentLotSerialState>()
				);
			}

			protected override IEnumerable<ScanCommand<ScanMaterial>> CreateCommands()
			{
				return new ScanCommand<ScanMaterial>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

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
				Clear<ParentLotSerialState>();
			}


			#region States
			public new sealed class MaterialItemState : WMSBase.InventoryItemState
			{
				protected override void Apply(PXResult<INItemXRef, InventoryItem> entity)
				{
					base.Apply(entity);

					if(Basis.UseRemainingQty)
					{
						var matls = SelectFrom<AMProdMatl>
							.Where<AMProdMatl.orderType.IsEqual<@P.AsString>
							.And<AMProdMatl.prodOrdID.IsEqual<@P.AsString>>
							.And<AMProdMatl.operationID.IsEqual<@P.AsInt>>
							.And<AMProdMatl.inventoryID.IsEqual<@P.AsInt>>>.View.ReadOnly
							.Select(Basis.Graph, Basis.OrderType, Basis.ProdOrdID,
							Basis.OperationID, Basis.InventoryID);
						foreach (AMProdMatl matl in matls)
						{
							if (matl.QtyRemaining > 0)
							{
								Basis.Qty = matl.QtyRemaining;
								continue;
							}
						}
					}

				}
			}

			public new sealed class ParentLotSerialState : EntityState<AMProdItemSplit>
			{
				public const string Value = "PRLS";
				public class value : BqlString.Constant<value> { public value() : base(ParentLotSerialState.Value) { } }
				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override AMProdItemSplit GetByBarcode(string barcode) {
					return SelectFrom<AMProdItemSplit>.Where<AMProdItemSplit.orderType.IsEqual<@P.AsString>
						.And<AMProdItemSplit.prodOrdID.IsEqual<@P.AsString>>
						.And<AMProdItemSplit.lotSerialNbr.IsEqual<@P.AsString>>>.View.Select(Basis, Basis.OrderType, Basis.ProdOrdID, barcode);
				}
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
				protected override void Apply(AMProdItemSplit split) => Basis.ParentLotSerialNbr = split.LotSerialNbr;
				protected override void ClearState() => Basis.ParentLotSerialNbr = null;

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the Parent Lot/Serial number.";
					public const string Missing = "The {0} parent lot or serial number is not found.";
				}
				#endregion
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

						AMMTran existTransaction = FindIssueRow();

						Action rollbackAction = null;
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

							var backup = PXCache<AMMTran>.CreateCopy(existTransaction) as AMMTran;

							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							if (lsClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
								Basis.Details.Cache.SetValueExt<INTran.expireDate>(existTransaction, Basis.ExpireDate);
							Basis.Details.Cache.SetValueExt<AMMTran.parentLotSerialNbr>(existTransaction, Basis.ParentLotSerialNbr);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
							existTransaction = Basis.Details.Update(existTransaction);

							rollbackAction = () =>
							{
								Basis.Details.Delete(existTransaction);
								Basis.Details.Insert(backup);
							};
						}
						else
						{
							Basis.Graph.IsImport = true;
							existTransaction = Basis.Details.Insert();
							Basis.Details.Cache.SetValueExt<AMMTran.orderType>(existTransaction, Basis.OrderType);
							Basis.Details.Cache.SetValueExt<AMMTran.prodOrdID>(existTransaction, Basis.ProdOrdID);
							Basis.Details.Cache.SetValueExt<AMMTran.operationID>(existTransaction, Basis.OperationID);
							Basis.Details.Cache.SetValueExt<AMMTran.inventoryID>(existTransaction, Basis.InventoryID);
							Basis.Details.Cache.SetValueExt<AMMTran.siteID>(existTransaction, Basis.SiteID);
							Basis.Details.Cache.SetValueExt<AMMTran.locationID>(existTransaction, Basis.LocationID);
							Basis.Details.Cache.SetValueExt<AMMTran.uOM>(existTransaction, Basis.UOM);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<AMMTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							if (lsClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
								Basis.Details.Cache.SetValueExt<AMMTran.expireDate>(existTransaction, Basis.ExpireDate);
							Basis.Details.Cache.SetValueExt<AMMTran.parentLotSerialNbr>(existTransaction, Basis.ParentLotSerialNbr);

							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
							existTransaction = Basis.Details.Update(existTransaction);
							Basis.Graph.IsImport = false;

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
						if (HasSplitLotSerialError(tran.LotSerialNbr, out error))
							return true;
						if (HasLocationError(tran, out error))
							return true;
						if (HasAvailabilityError(tran, out error))
							return true;

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasSplitLotSerialError(string lotSerialNbr, out FlowStatus error)
					{
						if (!string.IsNullOrEmpty(lotSerialNbr) &&
							Basis.Graph.splits.SelectMain().Any(s => s.LotSerialNbr != lotSerialNbr))
						{
							error = FlowStatus.Fail(
								Msg.QtyIssueExceedsQtyOnLot,
								Basis.LotSerialNbr,
								Basis.SightOf<WMSScanHeader.inventoryID>());
							return true;
						}
						foreach (AMMTranSplit split in Basis.Graph.splits.Select())
						{
							split.ParentLotSerialNbr = Basis.ParentLotSerialNbr;
							Basis.Graph.splits.Update(split);
						}
						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasLocationError(AMMTran tran, out FlowStatus error)
					{
						if (Basis.CurrentMode.HasActive<LocationState>() && Basis.Graph.splits.SelectMain().Any(s => s.LocationID != Basis.LocationID))
						{
							error = FlowStatus.Fail(
								Msg.QtyIssueExceedsQtyOnLocation,
								Basis.SightOf<WMSScanHeader.locationID>(),
								Basis.SightOf<WMSScanHeader.inventoryID>());
							return true;
						}

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasAvailabilityError(AMMTran tran, out FlowStatus error)
					{
						var errorInfo = Basis.Graph.ItemAvailabilityExt.GetCheckErrors(tran).FirstOrDefault();
						if (errorInfo != null)
						{
							PXCache lsCache = Basis.Graph.transactions.Cache;
							error = FlowStatus.Fail(errorInfo.MessageFormat, new object[]
							{
								lsCache.GetStateExt<AMMTran.inventoryID>(tran),
								lsCache.GetStateExt<AMMTran.subItemID>(tran),
								lsCache.GetStateExt<AMMTran.siteID>(tran),
								lsCache.GetStateExt<AMMTran.locationID>(tran),
								lsCache.GetValue<AMMTran.lotSerialNbr>(tran)
							});
							return true;
						}

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual FlowStatus ConfirmRemove()
					{
						AMMTran existTransaction = FindIssueRow();

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

					protected virtual AMMTran FindIssueRow()
					{
						var existTransactions = Basis.Details.SelectMain().Where(t =>
							t.OrderType == Basis.OrderType &&
							t.ProdOrdID == Basis.ProdOrdID &&
							t.OperationID == Basis.OperationID &&
							t.InventoryID == Basis.InventoryID &&
							t.SiteID == Basis.SiteID &&
							t.LocationID == (Basis.LocationID ?? t.LocationID) &&
							t.UOM == Basis.UOM);

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
							error = FlowStatus.Fail(InventoryItemState.Msg.NotSet);
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
					public const string Prompt = "Confirm issue of Material {0} x {1} {2}.";					
					public const string SerialItemNotComplexQty = "Serialized items can be processed only with the base UOM and the 1.00 quantity.";
					public const string LineMissing = "The {0} item is not found in the issue.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the issue.";
					public const string InventoryAdded = "{0} x {1} {2} has been added.";
					public const string QtyIssueExceedsQtyOnLot = "The quantity of the {1} item in the issue exceeds the item quantity in the {0} lot.";
					public const string QtyIssueExceedsQtyOnLocation = "The quantity of the {1} item in the issue exceeds the item quantity in the {0} location.";
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
					public const string DocumentReleasing = "The {0} material is being released.";
					public const string DocumentIsReleased = "The material transaction has been successfully released.";
					public const string DocumentReleaseFailed = "The material release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan Material";
			}
			#endregion
		}
	}

}
