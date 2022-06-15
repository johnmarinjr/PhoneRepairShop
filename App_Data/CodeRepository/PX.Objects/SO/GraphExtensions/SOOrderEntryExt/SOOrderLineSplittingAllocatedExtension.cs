using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.IN;
using Counters = PX.Objects.IN.LSSelect.Counters;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	[PXProtectedAccess(typeof(SOOrderLineSplittingExtension))]
	public abstract class SOOrderLineSplittingAllocatedExtension : PXGraphExtension<SOOrderLineSplittingExtension, SOOrderEntry>
	{
		protected virtual SOOrderItemAvailabilityExtension Availability => Base.FindImplementation<SOOrderItemAvailabilityExtension>();

		public bool IsAllocationEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireShipping == true || ordertype.Behavior == SOBehavior.BL;
			}
		}


		#region Protected Access
		[PXProtectedAccess] protected abstract Dictionary<SOLine, Counters> LineCounters { get; }
		[PXProtectedAccess] protected abstract PXDBOperation CurrentOperation { get; }
		[PXProtectedAccess] protected abstract PXCache<SOLine> LineCache { get; }
		[PXProtectedAccess] protected abstract PXCache<SOLineSplit> SplitCache { get; }

		[PXProtectedAccess] protected abstract IDisposable OperationModeScope(PXDBOperation alterCurrentOperation, bool restoreToNormal = false);
		[PXProtectedAccess] protected abstract PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(int? inventoryID);
		[PXProtectedAccess] protected abstract void SetSplitQtyWithLine(SOLineSplit split, SOLine line);
		[PXProtectedAccess] protected abstract void SetLineQtyFromBase(SOLine line);

		[PXProtectedAccess] protected abstract SOLine SelectLine(SOLineSplit split);
		[PXProtectedAccess] protected abstract SOLineSplit[] SelectAllSplits(SOLine line);
		[PXProtectedAccess] protected abstract SOLineSplit[] SelectSplits(SOLineSplit split);
		[PXProtectedAccess] protected abstract SOLineSplit[] SelectSplitsReversed(SOLineSplit split);
		[PXProtectedAccess] protected abstract SOLineSplit[] SelectSplitsReversed(SOLineSplit split, bool excludeCompleted = true);

		[PXProtectedAccess] protected abstract bool UseBaseUnitInSplit(SOLineSplit split, PXResult<InventoryItem, INLotSerClass> item);
		#endregion

		#region Event Handler Overrides
		#region SOOrder
		protected SOOrder _LastSelected;

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.EventHandler(ManualEvent.Row{SOOrder}.Selected.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<SOOrder>.Selected.Args e,
			Action<ManualEvent.Row<SOOrder>.Selected.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (_LastSelected == null || !ReferenceEquals(_LastSelected, e.Row))
			{
				PXUIFieldAttribute.SetVisible<SOLineSplit.shipDate>(Base1.SplitCache, null, IsAllocationEntryEnabled && !Base1.IsBlanketOrder);
				PXUIFieldAttribute.SetVisible<SOLineSplit.isAllocated>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.completed>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.shippedQty>(SplitCache, null, IsAllocationEntryEnabled && !Base1.IsBlanketOrder);
				PXUIFieldAttribute.SetVisible<SOLineSplit.shipmentNbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOType>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pONbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOReceiptNbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOSource>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOCreate>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.receivedQty>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.refNoteID>(SplitCache, null, IsAllocationEntryEnabled);

				if (e.Row != null)
					_LastSelected = e.Row;
			}

			if (IsAllocationEntryEnabled)
				Base1.showSplits.SetEnabled(true);
		}
		#endregion
		#region SOLine
		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandlerInternal(ManualEvent.Row{TLine}.Inserted.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerInternal(ManualEvent.Row<SOLine>.Inserted.Args e, Action<ManualEvent.Row<SOLine>.Inserted.Args> base_EventHandlerInternal)
		{
			if (e.Row == null)
				return;

			if (IsSplitRequired(e.Row))
			{
				base_EventHandlerInternal(e);
				return;
			}

			e.Cache.SetValue<SOLine.locationID>(e.Row, null);
			e.Cache.SetValue<SOLine.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<SOLine.expireDate>(e.Row, null);

			if (IsAllocationEntryEnabled && e.Row != null && e.Row.BaseOpenQty != 0m)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

				if (item != null && (e.Row.LineType.IsIn(SOLineType.Inventory, SOLineType.NonInventory) || Base1.IsBlanketOrder))
					IssueAvailable(e.Row);
			}

			Availability.Check(e.Row);
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandlerInternal(ManualEvent.Row{TLine}.Updated.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerInternal(ManualEvent.Row<SOLine>.Updated.Args e, Action<ManualEvent.Row<SOLine>.Updated.Args> base_EventHandlerInternal)
		{
			if (e.Row == null)
				return;

			if (IsSplitRequired(e.Row, out InventoryItem ii)) //check condition
			{
				base_EventHandlerInternal(e);

				if (ii != null && (ii.KitItem == true || ii.StkItem == true))
					Availability.Check(e.Row);
			}
			else
			{
				e.Cache.SetValue<SOLine.locationID>(e.Row, null);
				e.Cache.SetValue<SOLine.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<SOLine.expireDate>(e.Row, null);

				if (IsAllocationEntryEnabled)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

					if (e.OldRow != null && !e.Cache.ObjectsEqual<SOLine.inventoryID, SOLine.subItemID, SOLine.siteID, SOLine.invtMult, SOLine.uOM, SOLine.projectID, SOLine.taskID>(e.Row, e.OldRow))
					{
						Base1.RaiseRowDeleted(e.OldRow);
						Base1.RaiseRowInserted(e.Row);
					}
					else if (item != null && (e.Row.LineType.IsIn(SOLineType.Inventory, SOLineType.NonInventory) || Base1.IsBlanketOrder))
					{
						// prevent setting null to quantity from mobile app
						if (Base.IsMobile && e.Row.OrderQty == null)
						{
							e.Row.OrderQty = e.OldRow.OrderQty;
						}

						//ConfirmShipment(), CorrectShipment() use SuppressedMode and never end up here.
						//OpenQty is calculated via formulae, ExternalCall is used to eliminate duplicating formula arguments here
						//direct OrderQty for AddItem()
						if (!e.Cache.ObjectsEqual<SOLine.orderQty, SOLine.completed>(e.Row, e.OldRow))
						{
							e.Row.BaseOpenQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.OpenQty.Value, e.Row.BaseOpenQty, INPrecision.QUANTITY);

							//mimic behavior of Shipment Confirmation where at least one schedule will always be present for processed line
							//but additional schedules will never be created and thus should be truncated when ShippedQty > 0
							if (e.Row.Completed == true && e.OldRow.Completed == false)
							{
								CompleteSchedules(e.Row);
								Base1.UpdateParent(e.Row);
							}
							else if (e.Row.Completed == false && e.OldRow.Completed == true)
							{
								UncompleteSchedules(e.Row);
								Base1.UpdateParent(e.Row);
							}
							else if (e.Row.BaseOpenQty > e.OldRow.BaseOpenQty)
							{
								IssueAvailable(e.Row, e.Row.BaseOpenQty.Value - e.OldRow.BaseOpenQty.Value);
								Base1.UpdateParent(e.Row);
							}
							else if (e.Row.BaseOpenQty < e.OldRow.BaseOpenQty)
							{
								TruncateSchedules(e.Row, e.OldRow.BaseOpenQty.Value - e.Row.BaseOpenQty.Value);
								Base1.UpdateParent(e.Row);
							}
						}

						if (!e.Cache.ObjectsEqual<SOLine.pOCreate, SOLine.pOSource, SOLine.vendorID, SOLine.pOSiteID>(e.Row, e.OldRow))
						{
							foreach (SOLineSplit split in SelectSplits(e.Row))
							{
								if (split.IsAllocated == false && split.Completed == false && split.PONbr == null)
								{
									SOLineSplit splitUpd = PXCache<SOLineSplit>.CreateCopy(split);
									splitUpd.POCreate = e.Row.POCreate;
									splitUpd.POSource = e.Row.POSource;
									splitUpd.VendorID = e.Row.VendorID;
									splitUpd.POSiteID = e.Row.POSiteID;

									SplitCache.Update(splitUpd);
								}
							}
						}

						if (e.Row.ShipDate != e.OldRow.ShipDate ||
							(e.Row.ShipComplete != e.OldRow.ShipComplete && e.Row.ShipComplete != SOShipComplete.BackOrderAllowed))
						{
							foreach (SOLineSplit split in SelectSplits(e.Row))
							{
								split.ShipDate = e.Row.ShipDate;

								SplitCache.Update(split);
							}
						}
					}
				}
				else if (e.OldRow != null && e.OldRow.InventoryID != e.Row.InventoryID)
					Base1.RaiseRowDeleted(e.OldRow);

				Availability.Check(e.Row);
			}
		}
		#endregion
		#region SOLineSplit
		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.EventHandler(ManualEvent.Row{SOLineSplit}.Inserting.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<SOLineSplit>.Inserting.Args e,
			Action<ManualEvent.Row<SOLineSplit>.Inserting.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (!Base1.IsLSEntryEnabled && IsAllocationEntryEnabled)
			{
				if (!e.ExternalCall && CurrentOperation == PXDBOperation.Update)
				{
					bool isDropShipNotLegacy = IsDropShipNotLegacy(e.Row);
					if (isDropShipNotLegacy)
					{
						var linksExt = Base.FindImplementation<PurchaseSupplyBaseExt>();
						linksExt.FillInsertingSchedule(e.Cache, e.Row);
					}

					foreach (SOLineSplit siblingSplit in SelectSplits(e.Row))
					{
						if (SchedulesEqual(e.Row, siblingSplit, PXDBOperation.Insert))
						{
							var oldSiblingSplit = PXCache<SOLineSplit>.CreateCopy(siblingSplit);
							siblingSplit.BaseQty += e.Row.BaseQty;
							SetSplitQtyWithLine(siblingSplit, null);

							siblingSplit.BaseUnreceivedQty += e.Row.BaseQty;
							SetUnreceivedQty(siblingSplit);

							e.Cache.Current = siblingSplit;
							e.Cache.RaiseRowUpdated(siblingSplit, oldSiblingSplit);
							e.Cache.MarkUpdated(siblingSplit);
							PXDBQuantityAttribute.VerifyForDecimal(e.Cache, siblingSplit);
							e.Cancel = true;
							break;
						}
					}
				}

				if (e.Row.InventoryID == null || string.IsNullOrEmpty(e.Row.UOM))
					e.Cancel = true;
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandler(ManualEvent.Row{TSplit}.Inserted.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<SOLineSplit>.Inserted.Args e,
			Action<ManualEvent.Row<SOLineSplit>.Inserted.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (IsAllocationEntryEnabled)
			{
				if (Base1.SuppressedMode == false && e.Row.IsAllocated == true || !string.IsNullOrEmpty(e.Row.LotSerialNbr) && e.Row.IsAllocated != true)
				{
					AllocatedUpdated(e.Row, e.ExternalCall);

					e.Cache.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
					Availability.Check(e.Row);
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandler(ManualEvent.Row{TSplit}.Updated.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<SOLineSplit>.Updated.Args e,
			Action<ManualEvent.Row<SOLineSplit>.Updated.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (Base1.SuppressedMode == false && IsAllocationEntryEnabled)
			{
				if (e.Row.IsAllocated != e.OldRow.IsAllocated || e.Row.POLineNbr != e.OldRow.POLineNbr && e.Row.POLineNbr == null && e.Row.IsAllocated == false)
				{
					if (e.Row.IsAllocated == true)
					{
						AllocatedUpdated(e.Row, e.ExternalCall);

						e.Cache.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
						Availability.Check(e.Row);
					}
					else
					{
						//clear link to created transfer
						e.Row.ClearSOReferences();

						foreach (SOLineSplit siblingSplit in SelectSplitsReversed(e.Row))
						{
							if (siblingSplit.SplitLineNbr != e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row, PXDBOperation.Update))
							{
								e.Row.Qty += siblingSplit.Qty;
								e.Row.BaseQty += siblingSplit.BaseQty;

								e.Row.UnreceivedQty += siblingSplit.Qty;
								e.Row.BaseUnreceivedQty += siblingSplit.BaseQty;

								if (e.Row.LotSerialNbr != null)
								{
									SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(e.Row);
									e.Row.LotSerialNbr = null;
									//sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), siblingSplit, copy.IsAllocated);
									e.Cache.RaiseRowUpdated(e.Row, copy);
								}
								e.Cache.SetStatus(siblingSplit, e.Cache.GetStatus(siblingSplit) == PXEntryStatus.Inserted ? PXEntryStatus.InsertedDeleted : PXEntryStatus.Deleted);
								e.Cache.ClearQueryCache();

								PXCache itemPlanCache = Base.Caches<INItemPlan>();
								INItemPlan newPlan = SelectFrom<INItemPlan>.Where<INItemPlan.planID.IsEqual<@P.AsLong>>.View.Select(Base, e.Row.PlanID);
								if (newPlan != null)
								{
									newPlan.PlanQty += siblingSplit.BaseQty;
									if (itemPlanCache.GetStatus(newPlan) != PXEntryStatus.Inserted)
										itemPlanCache.SetStatus(newPlan, PXEntryStatus.Updated);
								}

								INItemPlan oldPlan = SelectFrom<INItemPlan>.Where<INItemPlan.planID.IsEqual<@P.AsLong>>.View.Select(Base, siblingSplit.PlanID);
								if (oldPlan != null)
								{
									itemPlanCache.SetStatus(oldPlan, itemPlanCache.GetStatus(oldPlan) == PXEntryStatus.Inserted ? PXEntryStatus.InsertedDeleted : PXEntryStatus.Deleted);
									itemPlanCache.ClearQueryCacheObsolete();
								}

								RefreshViewOf(e.Cache);
							}
							else if (siblingSplit.SplitLineNbr == e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row, PXDBOperation.Update) && e.Row.LotSerialNbr != null)
							{
								SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(e.Row);
								e.Row.LotSerialNbr = null;
								e.Cache.RaiseRowUpdated(e.Row, copy);
							}
						}
					}
				}

				if (e.Row.LotSerialNbr != e.OldRow.LotSerialNbr)
				{
					if (e.Row.LotSerialNbr != null)
					{
						LotSerialNbrUpdated(e.Row, e.ExternalCall);

						e.Cache.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
						Availability.Check(e.Row); //???
					}
					else
					{
						foreach (SOLineSplit siblingSplit in SelectSplitsReversed(e.Row))
						{
							if (siblingSplit.SplitLineNbr == e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row, PXDBOperation.Update))
							{
								SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(siblingSplit);
								e.Row.IsAllocated = false;
								e.Cache.RaiseFieldUpdated<SOLineSplit.isAllocated>(e.Row, e.Row.IsAllocated);
								//e.Cache.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), siblingSplit, copy.IsAllocated);
								e.Cache.RaiseRowUpdated(siblingSplit, copy);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandlerQty(ManualEvent.FieldOf{TSplit}.Verifying.Args{decimal?})"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerQty(ManualEvent.FieldOf<SOLineSplit>.Verifying.Args<decimal?> e,
			Action<ManualEvent.FieldOf<SOLineSplit>.Verifying.Args<decimal?>> base_EventHandlerQty)
		{
			if (IsAllocationEntryEnabled)
				e.NewValue = Base1.VerifySNQuantity(e.Cache, e.Row, e.NewValue, nameof(SOLineSplit.qty));
			else
				base_EventHandlerQty(e);
		}

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.EventHandlerUOM(ManualEvent.FieldOf{SOLineSplit}.Defaulting.Args{string})"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerUOM(ManualEvent.FieldOf<SOLineSplit>.Defaulting.Args<string> e,
			Action<ManualEvent.FieldOf<SOLineSplit>.Defaulting.Args<string>> base_EventHandlerUOM)
		{
			if (!IsAllocationEntryEnabled)
			{
				base_EventHandlerUOM(e);
			}
			else
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);
				if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
				{
					e.NewValue = ((InventoryItem)item).BaseUnit;
					e.Cancel = true;
				}
			}
		}


		protected virtual bool AllocatedUpdated(SOLineSplit split, bool externalCall)
		{
			var accum = new SiteStatus
			{
				InventoryID = split.InventoryID,
				SiteID = split.SiteID,
				SubItemID = split.SubItemID
			};
			accum = PXCache<SiteStatus>.Insert(Base, accum);
			accum = PXCache<SiteStatus>.CreateCopy(accum);

			var stat = INSiteStatus.PK.Find(Base, split.InventoryID, split.SubItemID, split.SiteID);
			if (stat != null)
			{
				accum.QtyAvail += stat.QtyAvail;
				accum.QtyHardAvail += stat.QtyHardAvail;
			}

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);
			if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult))
			{
				if (split.LotSerialNbr != null)
				{
					LotSerialNbrUpdated(split, externalCall);
					return true;
				}
			}
			else
			{
				if (accum.QtyHardAvail < 0m)
				{
					SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
					if (split.BaseQty + accum.QtyHardAvail > 0m)
					{
						split.BaseQty += accum.QtyHardAvail;
						SetSplitQtyWithLine(split, null);
						SplitCache.RaiseFieldUpdated<SOLineSplit.qty>(split, split.Qty);
					}
					else
					{
						split.IsAllocated = false;
						SplitCache.RaiseExceptionHandling<SOLineSplit.isAllocated>(split, true, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
					}

					SplitCache.RaiseFieldUpdated<SOLineSplit.isAllocated>(split, copy.IsAllocated);

					using (Base1.SuppressedModeScope(true))
					{
						SplitCache.RaiseRowUpdated(split, copy);

						if (split.IsAllocated == true)
						{
							copy = InsertAllocationRemainder(copy, -accum.QtyHardAvail);
						}
					}

					RefreshViewOf(SplitCache);
					return true;
				}
			}

			return false;
		}

		protected virtual bool LotSerialNbrUpdated(SOLineSplit split, bool externalCall)
		{
			var accum = new SiteLotSerial
			{
				InventoryID = split.InventoryID,
				SiteID = split.SiteID,
				LotSerialNbr = split.LotSerialNbr
			};
			accum = PXCache<SiteLotSerial>.Insert(Base, accum);
			accum = PXCache<SiteLotSerial>.CreateCopy(accum);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);

			var siteLotSerial = INSiteLotSerial.PK.Find(Base, split.InventoryID, split.SiteID, split.LotSerialNbr);
			if (siteLotSerial != null)
			{
				accum.QtyAvail += siteLotSerial.QtyAvail;
				accum.QtyHardAvail += siteLotSerial.QtyHardAvail;
			}

			//Serial-numbered items
			if (INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null)
			{
				SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
				if (siteLotSerial != null && siteLotSerial.QtyAvail > 0 && siteLotSerial.QtyHardAvail > 0)
				{
					if (split.Operation != SOOperation.Receipt)
					{
						split.BaseQty = 1;
						SetSplitQtyWithLine(split, null);
						split.IsAllocated = true;
					}
					else
					{
						split.IsAllocated = false;
						SplitCache.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null,
							new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefixNLA(IN.Messages.SerialNumberAlreadyReceived,
								((InventoryItem)item).InventoryCD,
								split.LotSerialNbr)));
					}
				}
				else
				{
					if (split.Operation != SOOperation.Receipt)
					{
						if (externalCall)
						{
							split.IsAllocated = false;
							split.LotSerialNbr = null;
							SplitCache.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null,
								new PXSetPropertyException(IN.Messages.Inventory_Negative2));

							if (split.IsAllocated == true)
								return false;
						}
					}
					else
					{
						split.BaseQty = 1;
						SetSplitQtyWithLine(split, null);
					}
				}

				SplitCache.RaiseFieldUpdated<SOLineSplit.isAllocated>(split, copy.IsAllocated);

				bool needNewSplit = copy.BaseQty > 1m;
				using (Base1.SuppressedModeScope(needNewSplit))
				{
					SplitCache.RaiseRowUpdated(split, copy);

					if (needNewSplit)
					{
						if (split.IsAllocated == true || (split.IsAllocated != true && split.Operation == SOOperation.Receipt))
						{
							copy.LotSerialNbr = null;
							copy = InsertAllocationRemainder(copy, copy.BaseQty - split.BaseQty);

							if (Base1.IsLotSerialRequired)
							{
								//because we are now using SuppressedMode need to adjust Unassigned Quantity
								SOLine parent = PXParentAttribute.SelectParent<SOLine>(SplitCache, copy);
								if (parent != null && IsLotSerialItem(parent))
								{
									parent.UnassignedQty += copy.BaseQty;
									LineCache.MarkUpdated(parent);
								}
							}
						}
					}
				}

				if (needNewSplit)
				{
					RefreshViewOf(SplitCache);
					return true;
				}
			}
			//Lot-numbered items
			else if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null && !INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult))
			{
				if (split.BaseQty > 0m)
				{
					//Lot/Serial Nbr. selected on non-allocated line. Trying to allocate line first. Verification of Qty. available for allocation will be performed on the next pass-through
					if (split.IsAllocated == false)
					{
						if (siteLotSerial == null || (((siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail <= 0m) || siteLotSerial.QtyOnHand <= 0m) && split.Operation != SOOperation.Receipt))
						{
							if (externalCall)
							{
								return NegativeInventoryError(split);
							}
						}
						else
						{
							SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
							split.IsAllocated = true;
							SplitCache.RaiseFieldUpdated<SOLineSplit.isAllocated>(split, copy.IsAllocated);
							SplitCache.RaiseRowUpdated(split, copy);
						}
						return true;
					}

					//Lot/Serial Nbr. selected on allocated line. Available Qty. verification procedure 
					if (split.IsAllocated == true)
					{
						SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
						if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail >= 0m && split.Operation != SOOperation.Receipt)
						{
							SetSplitQtyWithLine(split, null);
						}
						else if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail < 0m && split.Operation != SOOperation.Receipt)
						{
							split.BaseQty += accum.QtyHardAvail;
							if (split.BaseQty <= 0m)
								if (NegativeInventoryError(split))
									return false;

							SetSplitQtyWithLine(split, null);
						}
						else if (siteLotSerial == null || (siteLotSerial.QtyOnHand <= 0m && split.Operation != SOOperation.Receipt))
						{
							if (NegativeInventoryError(split))
								return false;
						}

						INItemPlanIDAttribute.RaiseRowUpdated(SplitCache, split, copy);

						if ((copy.BaseQty - split.BaseQty) > 0m && split.IsAllocated == true)
						{
							using (Base1.SuppressedModeScope(true))
							{
								copy.LotSerialNbr = null;
								copy = InsertAllocationRemainder(copy, copy.BaseQty - split.BaseQty);

								if (copy.LotSerialNbr != null && copy.IsAllocated != true)
								{
									SplitCache.SetValue<SOLineSplit.lotSerialNbr>(copy, null);
								}
							}
						}

						RefreshViewOf(SplitCache);
						return true;
					}
				}
			}
			return false;
		}
		#endregion
		#endregion

		#region Action Overrides
		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.ShowSplits(PXAdapter)"/>
		/// </summary>
		[PXOverride]
		public virtual IEnumerable ShowSplits(PXAdapter adapter,
			Func<PXAdapter, IEnumerable> base_ShowSplits)
		{
			SOLine currentSOLine = Base1.LineCurrent;

			if (IsAllocationEntryEnabled)
			{
				if (currentSOLine != null &&
					currentSOLine.POCreate == true &&
					currentSOLine.IsLegacyDropShip != true &&
					currentSOLine.POSource.IsIn(INReplenishmentSource.DropShipToOrder, INReplenishmentSource.BlanketDropShipToOrder))
				{
					if (!IsLotSerialsAllowedForDropShipLine(currentSOLine))
					{
						var inventory = InventoryItem.PK.Find(Base, currentSOLine.InventoryID);
						throw new PXSetPropertyException(Messages.BinLotSerialEntryDisabledDS, inventory.InventoryCD);
					}
				}
			}

			return base_ShowSplits(adapter);
		}
		#endregion

		#region Method Overrides
		#region UpdateParent helper
		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.UpdateParent(TLine)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateParent(SOLine line,
			Action<SOLine> base_UpdateParent)
		{
			if (line != null && line.RequireShipping == true && !IsSplitRequired(line))
			{
				Base1.UpdateParent(line, null, null, out _);
			}
			else
			{
				base_UpdateParent(line);
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.UpdateParent(TSplit, TSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateParent(SOLineSplit newSplit, SOLineSplit oldSplit,
			Action<SOLineSplit, SOLineSplit> base_UpdateParent)
		{
			SOLineSplit anySplit = newSplit ?? oldSplit;
			SOLine line = (SOLine)LSParentAttribute.SelectParent(SplitCache, anySplit, typeof(SOLine));

			if (line != null && line.RequireShipping == true)
			{
				if (anySplit != null && anySplit.InventoryID == line.InventoryID)
				{
					SOLine oldLine = PXCache<SOLine>.CreateCopy(line);

					Base1.UpdateParent(
						line,
						newSplit?.Completed == false ? newSplit : null,
						oldSplit?.Completed == false ? oldSplit : null,
						out decimal baseQty);

					using (new SOOrderLineSplittingExtension.InvtMultScope(line))
					{
						if (Base1.IsLotSerialRequired && newSplit != null)
						{
							if (IsLotSerialItem(newSplit))
								line.UnassignedQty = SelectSplits(newSplit).Where(s => s.LotSerialNbr == null).Sum(s => s.BaseQty);
							else
								line.UnassignedQty = 0m;
						}

						line.BaseQty = baseQty + line.BaseClosedQty;
						SetLineQtyFromBase(line);
					}

					LineCache.MarkUpdated(line);

					if (line.Qty != oldLine.Qty)
						LineCache.RaiseFieldUpdated<SOLine.orderQty>(line, oldLine.Qty);

					if (LineCache.RaiseRowUpdating(oldLine, line))
						LineCache.RaiseRowUpdated(line, oldLine);
					else
						PXCache<SOLine>.RestoreCopy(line, oldLine);
				}
			}
			else
			{
				base_UpdateParent(newSplit, oldSplit);
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.UpdateParent(TLine, TSplit, TSplit, out decimal)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateParent(SOLine line, SOLineSplit newSplit, SOLineSplit oldSplit, out decimal baseQty,
			UpdateParentDelegate base_UpdateParent)
		{
			ResetAvailabilityCounters(line);

			bool counted = LineCounters.ContainsKey(line);

			base_UpdateParent(line, newSplit, oldSplit, out baseQty);

			if (counted && oldSplit != null)
			{
				if (LineCounters.TryGetValue(line, out Counters counters))
				{
					if (oldSplit.POCreate == true || oldSplit.AMProdCreate == true)
					{
						counters.BaseQty += oldSplit.BaseReceivedQty.Value + oldSplit.BaseShippedQty.Value;
					}
					else if (oldSplit.Behavior == SOBehavior.BL)
					{
						counters.BaseQty += oldSplit.BaseShippedQty.Value;
					}
					baseQty = counters.BaseQty;
				}
			}
		}
		public delegate void UpdateParentDelegate(SOLine line, SOLineSplit newSplit, SOLineSplit oldSplit, out decimal baseQty);

		public static void ResetAvailabilityCounters(SOLine line)
		{
			line.LineQtyAvail = null;
			line.LineQtyHardAvail = null;
		}
		#endregion

		/// <summary>
		/// Overrides <see cref="SOOrderLineSplittingExtension.UpdateCounters(Counters, SOLineSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateCounters(Counters counters, SOLineSplit split,
			Action<Counters, SOLineSplit> base_UpdateCounters)
		{
			base_UpdateCounters(counters, split);

			if (IsAllocationEntryEnabled)
			{
				counters.LotSerNumbersNull = -1;
				counters.LotSerNumber = null;
				counters.LotSerNumbers.Clear();
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.DefaultLotSerialNbr(TSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual void DefaultLotSerialNbr(SOLineSplit row,
			Action<SOLineSplit> base_DefaultLotSerialNbr)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(row.InventoryID);

			if (item != null && !IsAllocationEntryEnabled || ((INLotSerClass)item).LotSerAssign != INLotSerAssign.WhenUsed)
				base_DefaultLotSerialNbr(row);
		}
		#endregion

		#region Schedules
		public virtual void UncompleteSchedules(SOLine line)
		{
			LineCounters.Remove(line);

			decimal? unshippedQty = line.BaseOpenQty;
			foreach (var split in SelectSplitsReversed(line, false)
				.Where(s => ShouldUncompleteSchedule(line, s)))
			{
				unshippedQty -= split.BaseQty;

				var newSplit = PXCache<SOLineSplit>.CreateCopy(split);
				newSplit.Completed = false;

				SplitCache.Update(newSplit);
			}

			if (IsDropShipNotLegacy(line))
			{
				decimal shippedQty = UncompleteDropShipSchedules(line);
				unshippedQty -= shippedQty;
			}

			IssueAvailable(line, unshippedQty.Value, true);
		}

		protected virtual bool ShouldUncompleteSchedule(SOLine line, SOLineSplit split)
			=> split.ShipmentNbr == null;

		public virtual decimal UncompleteDropShipSchedules(SOLine line)
		{
			decimal shippedQty = 0;
			foreach (var split in SelectAllSplits(line).Where(s => s.Completed == true && s.PONbr != null && s.BaseQty != null))
			{
				shippedQty += split.BaseQty.Value;

				var newSplit = PXCache<SOLineSplit>.CreateCopy(split);
				newSplit.Completed = false;

				SplitCache.Update(newSplit);
			}

			return shippedQty;
		}

		public virtual void CompleteSchedules(SOLine line)
		{
			LineCounters.Remove(line);

			string lastShipmentNbr = null;
			decimal? lastUnshippedQty = 0m;
			foreach (var split in SelectSplitsReversed(line, false))
			{
				if (lastShipmentNbr == null && split.ShipmentNbr != null)
					lastShipmentNbr = split.ShipmentNbr;

				if (lastShipmentNbr != null && split.ShipmentNbr == lastShipmentNbr)
					lastUnshippedQty += split.BaseOpenQty;
			}

			TruncateSchedules(line, lastUnshippedQty.Value);

			foreach (var detail in SelectSplitsReversed(line))
			{
				var newSplit = PXCache<SOLineSplit>.CreateCopy(detail);
				newSplit.Completed = true;

				SplitCache.Update(newSplit);
			}
		}

		public virtual void TruncateSchedules(SOLine line, decimal baseQty)
		{
			LineCounters.Remove(line);

			foreach (var split in SelectSplitsReversed(line))
			{
				if (baseQty >= split.BaseQty)
				{
					baseQty -= split.BaseQty.Value;
					SplitCache.Delete(split);
				}
				else
				{
					var newSplit = PXCache<SOLineSplit>.CreateCopy(split);
					newSplit.BaseQty -= baseQty;
					SetSplitQtyWithLine(newSplit, line);

					SplitCache.Update(newSplit);
					break;
				}
			}
		}

		protected virtual bool SchedulesEqual(SOLineSplit a, SOLineSplit b, PXDBOperation operation)
		{
			if (a != null && b != null)
			{
				return
					a.InventoryID == b.InventoryID &&
					a.SubItemID == b.SubItemID &&
					a.SiteID == b.SiteID &&
					a.ToSiteID == b.ToSiteID &&
					a.ShipDate == b.ShipDate &&
					a.IsAllocated == b.IsAllocated &&
					a.IsMergeable != false && b.IsMergeable != false &&
					a.ShipmentNbr == b.ShipmentNbr &&
					(a.ParentSplitLineNbr == b.ParentSplitLineNbr
						|| a.SplitLineNbr == b.ParentSplitLineNbr
						|| b.SplitLineNbr == a.ParentSplitLineNbr) &&
					a.Completed == b.Completed &&
					a.POCreate == b.POCreate &&
					a.POCompleted == b.POCompleted &&
					a.POType == b.POType &&
					a.PONbr == b.PONbr &&
					a.POLineNbr == b.POLineNbr &&
					a.SOOrderType == b.SOOrderType &&
					a.SOOrderNbr == b.SOOrderNbr &&
					a.SOLineNbr == b.SOLineNbr &&
					a.SOSplitLineNbr == b.SOSplitLineNbr &&
					a.AMProdCreate == b.AMProdCreate;
			}
			else
			{
				return a != null;
			}
		}


		protected virtual void IssueAvailable(SOLine line) => IssueAvailable(line, line.BaseOpenQty);
		protected virtual void IssueAvailable(SOLine line, decimal? baseQty) => IssueAvailable(line, baseQty, false);
		protected virtual void IssueAvailable(SOLine line, decimal? baseQty, bool isUncomplete)
		{
			int? parentSplitLineNbr = null;

			LineCounters.Remove(line);
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			foreach (INSiteStatus avail in
				SelectFrom<INSiteStatus>.
				Where<
					INSiteStatus.inventoryID.IsEqual<@P.AsInt>.
					And<INSiteStatus.subItemID.IsEqual<@P.AsInt>>.
					And<INSiteStatus.siteID.IsEqual<@P.AsInt>>>.
				OrderBy<INLocation.pickPriority.Asc>.
				View.ReadOnly.Select(Base, line.InventoryID, line.SubItemID, line.SiteID))
			{
				SOLineSplit split = (SOLineSplit)line;
				if (UseBaseUnitInSplit(split, item))
					split.UOM = ((InventoryItem)item).BaseUnit;

				split.SplitLineNbr = null;
				split.IsAllocated = line.RequireAllocation;
				split.SiteID = line.SiteID;
				AssignNewSplitFields(split, line);

				SplitCache.RaiseFieldDefaulting<SOLineSplit.allocatedPlanType>(split, out var defaultAllocatedPlanType);
				SplitCache.SetValue<SOLineSplit.allocatedPlanType>(split, defaultAllocatedPlanType);

				SplitCache.RaiseFieldDefaulting<SOLineSplit.backOrderPlanType>(split, out var defaultBackOrderPlanType);
				SplitCache.SetValue<SOLineSplit.backOrderPlanType>(split, defaultBackOrderPlanType);

				INItemPlanIDAttribute.GetInclQtyAvail<SiteStatus>(SplitCache, split, out decimal signQtyAvail, out decimal signQtyHardAvail);

				if (signQtyHardAvail < 0m)
				{
					SiteStatus accumavail = new SiteStatus();
					PXCache<INSiteStatus>.RestoreCopy(accumavail, avail);
					accumavail = PXCache<SiteStatus>.Insert(Base, accumavail);

					decimal? availableQty = avail.QtyHardAvail + accumavail.QtyHardAvail;

					if (availableQty <= 0m)
						continue;

					if (availableQty < baseQty)
					{
						split.BaseQty = availableQty;
						SetSplitQtyWithLine(split, line);
						split = (SOLineSplit)SplitCache.Insert(split);

						parentSplitLineNbr = split?.SplitLineNbr;
						baseQty -= availableQty;
					}
					else
					{
						split.BaseQty = baseQty;
						SetSplitQtyWithLine(split, line);
						SplitCache.Insert(split);

						baseQty = 0m;
						break;
					}
				}
			}

			if (baseQty > 0m && line.InventoryID != null
				&& (line.SiteID != null || line.LineType == SOLineType.MiscCharge)
				&& (line.SubItemID != null || (line.SubItemID == null && line.IsStockItem != true && line.IsKit == true) || line.LineType.IsIn(SOLineType.NonInventory, SOLineType.MiscCharge)))
			{
				SOLineSplit split = (SOLineSplit)line;
				if (UseBaseUnitInSplit(split, item))
					split.UOM = ((InventoryItem)item).BaseUnit;

				split.SplitLineNbr = parentSplitLineNbr;
				split.IsAllocated = false;
				AssignNewSplitFields(split, line);
				split.BaseQty = baseQty;
				SetSplitQtyWithLine(split, line);

				if (isUncomplete)
				{
					split.POCreate = false;
					split.POSource = null;
				}

				InsertAllocationRemainder(split, baseQty);
			}
		}
		#endregion

		protected virtual void AssignNewSplitFields(SOLineSplit split, SOLine line)
		{
		}

		protected virtual bool IsSplitRequired(SOLine line) => IsSplitRequired(line, out _);
		protected virtual bool IsSplitRequired(SOLine line, out InventoryItem item)
		{
			if (line == null)
			{
				item = null;
				return false;
			}

			bool skipSplitCreating = false;
			item = InventoryItem.PK.Find(Base, line.InventoryID);

			if (Base1.IsLocationEnabled && item != null && item.StkItem == false && item.KitItem == false && item.NonStockShip == false)
				skipSplitCreating = true;

			if (item != null && item.StkItem == false && item.KitItem == true && line.Behavior != SOBehavior.CM && line.Behavior != SOBehavior.IN)
				skipSplitCreating = true;

			return !skipSplitCreating && (Base1.IsLocationEnabled || (Base1.IsLotSerialRequired && line.POCreate != true && IsLotSerialItem(line)));
		}

		protected virtual bool IsDropShipNotLegacy(SOLineSplit split)
		{
			SOLine soLine = split != null ? PXParentAttribute.SelectParent<SOLine>(Base1.SplitCache, split) : null;
			return IsDropShipNotLegacy(soLine);
		}

		protected virtual bool IsDropShipNotLegacy(SOLine line)
		{
			return line != null && line.POCreate == true && line.POSource == INReplenishmentSource.DropShipToOrder && line.IsLegacyDropShip != true;
		}

		protected virtual bool IsLotSerialItem(ILSMaster line)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			if (item == null)
				return false;

			return INLotSerialNbrAttribute.IsTrack(item, line.TranType, line.InvtMult);
		}

		protected virtual bool NegativeInventoryError(SOLineSplit split)
		{
			split.IsAllocated = false;
			split.LotSerialNbr = null;
			SplitCache.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
			return split.IsAllocated == true;
		}


		public virtual bool IsLotSerialsAllowedForDropShipLine(SOLine line)
		{
			return line.Operation == SOOperation.Receipt && ReadInventoryItem(line.InventoryID).GetItem<INLotSerClass>().RequiredForDropship == true;
		}

		public virtual bool HasMultipleSplitsOrAllocation(SOLine line)
		{
			var details = SelectAllSplits(line);
			return details.Length > 1 || details.Length == 1 && details[0].IsAllocated == true;
		}


		protected virtual void SetUnreceivedQty(SOLineSplit split)
		{
			SOLine line = SelectLine(split);
			if (split.InventoryID == line?.InventoryID && split.BaseUnreceivedQty == line?.BaseQty && string.Equals(split.UOM, line?.UOM, StringComparison.OrdinalIgnoreCase))
				split.UnreceivedQty = line.Qty;
			else
				split.UnreceivedQty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseUnreceivedQty.Value, INPrecision.QUANTITY);
		}

		protected virtual void RefreshViewOf(PXCache cache)
		{
			foreach (var pair in Base.Views)
			{
				PXView view = pair.Value;
				if (view.IsReadOnly == false && view.GetItemType() == cache.GetItemType())
					view.RequestRefresh();
			}
		}

		protected virtual SOLineSplit InsertAllocationRemainder(SOLineSplit copy, decimal? baseQty)
		{
			copy.ParentSplitLineNbr = copy.SplitLineNbr;
			copy.SplitLineNbr = null;
			copy.PlanID = null;
			copy.IsAllocated = false;
			copy.BaseQty = baseQty;
			SetSplitQtyWithLine(copy, null);
			copy.OpenQty = null;
			copy.BaseOpenQty = null;
			copy.UnreceivedQty = null;
			copy.BaseUnreceivedQty = null;
			copy = PXCache<SOLineSplit>.Insert(Base, copy);
			return copy;
		}

		public virtual SOLineSplit InsertShipmentRemainder(SOLineSplit copy)
		{
			using (OperationModeScope(PXDBOperation.Update))
				return (SOLineSplit)SplitCache.Insert(copy);
		}
	}
}
