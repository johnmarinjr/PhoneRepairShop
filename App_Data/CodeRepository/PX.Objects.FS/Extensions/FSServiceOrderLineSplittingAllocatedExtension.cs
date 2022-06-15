using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.SO;
using PX.Objects.IN;
using Counters = PX.Objects.IN.LSSelect.Counters;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;

namespace PX.Objects.FS
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	[PXProtectedAccess(typeof(FSServiceOrderLineSplittingExtension))]
	public abstract class FSServiceOrderLineSplittingAllocatedExtension : PXGraphExtension<FSServiceOrderLineSplittingExtension, ServiceOrderEntry>
	{
		protected virtual FSServiceOrderItemAvailabilityExtension Availability => Base.FindImplementation<FSServiceOrderItemAvailabilityExtension>();

		public bool IsAllocationEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireShipping == true;
			}
		}

		public bool IsQuoteServiceOrder
		{
			get
			{
				FSSrvOrdType srvOrdtype = PXSetup<FSSrvOrdType>.Select(Base);
				return srvOrdtype == null || srvOrdtype.Behavior == FSSrvOrdType.behavior.Values.Quote;
			}
		}


		#region Protected Access
		[PXProtectedAccess] protected abstract Dictionary<FSSODet, Counters> LineCounters { get; }
		[PXProtectedAccess] protected abstract PXDBOperation CurrentOperation { get; }
		[PXProtectedAccess] protected abstract PXCache<FSSODet> LineCache { get; }
		[PXProtectedAccess] protected abstract PXCache<FSSODetSplit> SplitCache { get; }

		[PXProtectedAccess] protected abstract PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(int? inventoryID);

		[PXProtectedAccess] protected abstract FSSODetSplit[] SelectSplits(FSSODetSplit split);
		[PXProtectedAccess] protected abstract FSSODetSplit[] SelectSplitsReversed(FSSODetSplit split);
		[PXProtectedAccess] protected abstract FSSODetSplit[] SelectSplitsReversed(FSSODetSplit split, bool excludeCompleted = true);
		#endregion

		#region Event Handler Overrides
		#region SOOrder
		protected FSServiceOrder _LastSelected;

		/// <summary>
		/// Overrides <see cref="FSServiceOrderLineSplittingExtension.EventHandler(ManualEvent.Row{FSServiceOrder}.Selected.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<FSServiceOrder>.Selected.Args e,
			Action<ManualEvent.Row<FSServiceOrder>.Selected.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (_LastSelected == null || !ReferenceEquals(_LastSelected, e.Row))
			{
				PXUIFieldAttribute.SetVisible<FSSODetSplit.shipDate>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.isAllocated>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.completed>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.shippedQty>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.shipmentNbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.pOType>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.pONbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.pOReceiptNbr>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.pOSource>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.pOCreate>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.receivedQty>(SplitCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<FSSODetSplit.refNoteID>(SplitCache, null, IsAllocationEntryEnabled);

				if (e.Row != null)
					_LastSelected = e.Row;
			}

			if (IsAllocationEntryEnabled)
				Base1.showSplits.SetEnabled(true);
		}
		#endregion
		#region FSSODet
		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandlerInternal(ManualEvent.Row{TLine}.Inserted.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerInternal(ManualEvent.Row<FSSODet>.Inserted.Args e,
			Action<ManualEvent.Row<FSSODet>.Inserted.Args> base_EventHandlerInternal)
		{
			if (e.Row.InventoryID == null || e.Row.IsPrepaid == true || IsQuoteServiceOrder)
				return;

			if (IsSplitRequired(e.Row, e.ExternalCall))
			{
				base_EventHandlerInternal(e);
				return;
			}

			//e.Cache.SetValue<FSSODet.locationID>(e.Row, null);
			//e.Cache.SetValue<FSSODet.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<FSSODet.expireDate>(e.Row, null);

			if (IsAllocationEntryEnabled && e.Row != null && e.Row.BaseOpenQty != 0m)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

				//if (e.Row.InvtMult == -1 && item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
				if (item != null && e.Row.SOLineType.IsIn(SOLineType.Inventory, SOLineType.NonInventory))
					IssueAvailable(e.Row);
			}

			Availability.Check(e.Row);
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandlerInternal(ManualEvent.Row{TLine}.Updated.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerInternal(ManualEvent.Row<FSSODet>.Updated.Args e,
			Action<ManualEvent.Row<FSSODet>.Updated.Args> base_EventHandlerInternal)
		{
			if (e.Row == null || (e.OldRow.InventoryID == null && e.Row.InventoryID == null) || e.Row.IsPrepaid == true || IsQuoteServiceOrder)
				return;

			if (IsSplitRequired(e.Row, e.ExternalCall && e.Row.POCreate != true, out InventoryItem ii)) //check condition
			{
				base_EventHandlerInternal(e);

				if (ii != null && (ii.KitItem == true || ii.StkItem == true))
					Availability.Check(e.Row);
			}
			else
			{
				//e.Cache.SetValue<FSSODet.locationID>(e.Row, null);
				//e.Cache.SetValue<FSSODet.lotSerialNbr>(e.Row, null);
				e.Cache.SetValue<FSSODet.expireDate>(e.Row, null);

				if (IsAllocationEntryEnabled)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

					if (e.OldRow != null && (e.OldRow.InventoryID != e.Row.InventoryID || e.OldRow.SiteID != e.Row.SiteID || e.OldRow.SubItemID != e.Row.SubItemID || e.OldRow.InvtMult != e.Row.InvtMult || e.OldRow.UOM != e.Row.UOM))
					{
						Base1.RaiseRowDeleted(e.OldRow);
						Base1.RaiseRowInserted(e.Row);
					}
					//else if (e.Row.InvtMult == -1 && item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
					else if (item != null && e.Row.SOLineType.IsIn(SOLineType.Inventory, SOLineType.NonInventory))
					{
						// prevent setting null to quantity from mobile app
						if (Base.IsMobile && e.Row.OrderQty == null)
						{
							e.Row.OrderQty = e.OldRow.OrderQty;
						}

						//ConfirmShipment(), CorrectShipment() use SuppressedMode and never end up here.
						//OpenQty is calculated via formulae, ExternalCall is used to eliminate duplicating formula arguments here
						//direct OrderQty for AddItem()
						if (!e.Cache.ObjectsEqual<FSSODet.orderQty, FSSODet.completed>(e.Row, e.OldRow))
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

						if (!e.Cache.ObjectsEqual<FSSODet.pOCreate, FSSODet.pOSource, FSSODet.vendorID, FSSODet.poVendorLocationID, FSSODet.pOSiteID, FSSODet.locationID, FSSODet.siteLocationID>(e.Row, e.OldRow))
						{
							foreach (var split in SelectSplits(e.Row))
							{
								if (split.IsAllocated == false && split.Completed == false && split.PONbr == null)
								{
									split.POCreate = e.Row.POCreate;
									split.POSource = e.Row.POSource;
									split.VendorID = e.Row.VendorID;
									split.POSiteID = e.Row.POSiteID;

									split.LocationID = e.Row.SiteLocationID;

									SplitCache.Update(split);
								}
							}
						}

						if (e.Row.ShipDate != e.OldRow.ShipDate ||
							(e.Row.ShipComplete != e.OldRow.ShipComplete && e.Row.ShipComplete != SOShipComplete.BackOrderAllowed))
						{
							foreach (var split in SelectSplits(e.Row))
							{
								split.ShipDate = e.Row.ShipDate;

								SplitCache.Update(split);
							}
						}
					}
				}

				Availability.Check(e.Row);
			}
		}
		#endregion
		#region FSSODetSplit
		/// <summary>
		/// Overrides <see cref="FSServiceOrderLineSplittingExtension.EventHandler(ManualEvent.Row{FSSODetSplit}.Inserting.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<FSSODetSplit>.Inserting.Args e,
			Action<ManualEvent.Row<FSSODetSplit>.Inserting.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (!Base1.IsLSEntryEnabled && IsAllocationEntryEnabled)
			{
				if (!e.ExternalCall && CurrentOperation == PXDBOperation.Update)
				{
					foreach (var siblingSplit in SelectSplits(e.Row))
					{
						if (SchedulesEqual(e.Row, siblingSplit))
						{
							var oldSiblingSplit = PXCache<FSSODetSplit>.CreateCopy(siblingSplit);
							siblingSplit.BaseQty += e.Row.BaseQty;
							siblingSplit.Qty = INUnitAttribute.ConvertFromBase(e.Cache, siblingSplit.InventoryID, siblingSplit.UOM, siblingSplit.BaseQty.Value, INPrecision.QUANTITY);

							siblingSplit.BaseUnreceivedQty += e.Row.BaseQty;
							siblingSplit.UnreceivedQty = INUnitAttribute.ConvertFromBase(e.Cache, siblingSplit.InventoryID, siblingSplit.UOM, siblingSplit.BaseUnreceivedQty.Value, INPrecision.QUANTITY);

							e.Cache.Current = siblingSplit;
							e.Cache.RaiseRowUpdated(siblingSplit, oldSiblingSplit);
							e.Cache.MarkUpdated(siblingSplit);

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
		public virtual void EventHandler(ManualEvent.Row<FSSODetSplit>.Inserted.Args e,
			Action<ManualEvent.Row<FSSODetSplit>.Inserted.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (IsAllocationEntryEnabled)
			{
				if (Base1.SuppressedMode == false && e.Row.IsAllocated == true || !string.IsNullOrEmpty(e.Row.LotSerialNbr) && e.Row.IsAllocated != true)
				{
					AllocatedUpdated(e.Row, e.ExternalCall);

					e.Cache.RaiseExceptionHandling<FSSODetSplit.qty>(e.Row, null, null);
					Availability.Check(e.Row);
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.EventHandler(ManualEvent.Row{TSplit}.Updated.Args)"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandler(ManualEvent.Row<FSSODetSplit>.Updated.Args e,
			Action<ManualEvent.Row<FSSODetSplit>.Updated.Args> base_EventHandler)
		{
			base_EventHandler(e);

			if (Base1.SuppressedMode == false && IsAllocationEntryEnabled)
			{
				if (e.Row.IsAllocated != e.OldRow.IsAllocated || e.Row.POLineNbr != e.OldRow.POLineNbr && e.Row.POLineNbr == null && e.Row.IsAllocated == false)
				{
					if (e.Row.IsAllocated == true)
					{
						AllocatedUpdated(e.Row, e.ExternalCall);

						e.Cache.RaiseExceptionHandling<FSSODetSplit.qty>(e.Row, null, null);
						Availability.Check(e.Row);
					}
					else
					{
						//clear link to created transfer
						e.Row.SOOrderType = null;
						e.Row.SOOrderNbr = null;
						e.Row.SOLineNbr = null;
						e.Row.SOSplitLineNbr = null;

						foreach (FSSODetSplit siblingSplit in SelectSplitsReversed(e.Row))
						{
							if (siblingSplit.SplitLineNbr != e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row))
							{
								e.Row.Qty += siblingSplit.Qty;
								e.Row.BaseQty += siblingSplit.BaseQty;

								e.Row.UnreceivedQty += siblingSplit.Qty;
								e.Row.BaseUnreceivedQty += siblingSplit.BaseQty;

								if (e.Row.LotSerialNbr != null)
								{
									FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(e.Row);
									e.Row.LotSerialNbr = null;
									//e.Cache.RaiseFieldUpdated(e.Cache.GetField(typeof(FSSODetSplit.isAllocated)), siblingSplit, copy.IsAllocated);
									e.Cache.RaiseRowUpdated(e.Row, copy);
								}
								e.Cache.SetStatus(siblingSplit, e.Cache.GetStatus(siblingSplit) == PXEntryStatus.Inserted ? PXEntryStatus.InsertedDeleted : PXEntryStatus.Deleted);
								e.Cache.ClearQueryCacheObsolete();

								PXCache itemPlanCache = Base.Caches[typeof(INItemPlan)];
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
							else if (siblingSplit.SplitLineNbr == e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row) && e.Row.LotSerialNbr != null)
							{
								FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(e.Row);
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

						e.Cache.RaiseExceptionHandling<FSSODetSplit.qty>(e.Row, null, null);
						Availability.Check(e.Row); //???
					}
					else
					{
						foreach (FSSODetSplit siblingSplit in SelectSplitsReversed(e.Row))
						{
							if (siblingSplit.SplitLineNbr == e.Row.SplitLineNbr && SchedulesEqual(siblingSplit, e.Row))
							{
								FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(siblingSplit);
								e.Row.IsAllocated = false;
								e.Cache.RaiseFieldUpdated<FSSODetSplit.isAllocated>(e.Row, e.Row.IsAllocated);
								//e.Cache.RaiseFieldUpdated(e.Cache.GetField(typeof(FSSODetSplit.isAllocated)), s, copy.IsAllocated);
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
		public virtual void EventHandlerQty(ManualEvent.FieldOf<FSSODetSplit>.Verifying.Args<decimal?> e,
			Action<ManualEvent.FieldOf<FSSODetSplit>.Verifying.Args<decimal?>> base_EventHandlerQty)
		{
			if (IsAllocationEntryEnabled)
				e.NewValue = Base1.VerifySNQuantity(e.Cache, e.Row, e.NewValue, nameof(FSSODetSplit.qty));
			else
				base_EventHandlerQty(e);
		}

		/// <summary>
		/// Overrides <see cref="FSServiceOrderLineSplittingExtension.EventHandlerUOM(ManualEvent.FieldOf{FSSODetSplit}.Defaulting.Args{string})"/>
		/// </summary>
		[PXOverride]
		public virtual void EventHandlerUOM(ManualEvent.FieldOf<FSSODetSplit>.Defaulting.Args<string> e,
			Action<ManualEvent.FieldOf<FSSODetSplit>.Defaulting.Args<string>> base_EventHandlerUOM)
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

		protected virtual bool AllocatedUpdated(FSSODetSplit split, bool externalCall)
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
					FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(split);
					if (split.BaseQty + accum.QtyHardAvail > 0m)
					{
						split.BaseQty += accum.QtyHardAvail;
						split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
					}
					else
					{
						split.IsAllocated = false;
						SplitCache.RaiseExceptionHandling<FSSODetSplit.isAllocated>(split, true, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
					}

					SplitCache.RaiseFieldUpdated<FSSODetSplit.isAllocated>(split, copy.IsAllocated);
					SplitCache.RaiseRowUpdated(split, copy);

					if (split.IsAllocated == true)
					{
						copy.SplitLineNbr = null;
						copy.PlanID = null;
						copy.IsAllocated = false;
						copy.BaseQty = -accum.QtyHardAvail;
						copy.Qty = INUnitAttribute.ConvertFromBase(SplitCache, copy.InventoryID, copy.UOM, copy.BaseQty.Value, INPrecision.QUANTITY);

						SplitCache.Insert(copy);
					}

					RefreshViewOf(SplitCache);
					return true;
				}
			}
			return false;
		}

		protected virtual bool LotSerialNbrUpdated(FSSODetSplit split, bool externalCall)
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
				FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(split);

				if (siteLotSerial != null && siteLotSerial.QtyAvail > 0 && siteLotSerial.QtyHardAvail > 0)
				{
					if (split.Operation != SOOperation.Receipt)
					{
						split.BaseQty = 1;
						split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
						split.IsAllocated = true;
					}
					else
					{
						split.IsAllocated = false;
						SplitCache.RaiseExceptionHandling<FSSODetSplit.lotSerialNbr>(split, null,
							new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefixNLA(IN.Messages.SerialNumberAlreadyReceived,
								((InventoryItem)item).InventoryCD,
								split.LotSerialNbr)));
					}
				}
				else
				{
					if (split.Operation != SOOperation.Receipt)
					{
						bool shouldThrowExceptions = ShouldThrowExceptions();

						if (externalCall || shouldThrowExceptions == true)
						{
							split.IsAllocated = false;
							split.LotSerialNbr = null;
							SplitCache.RaiseExceptionHandling<FSSODetSplit.lotSerialNbr>(split, null,
								new PXSetPropertyException(IN.Messages.Inventory_Negative2));

							if (split.IsAllocated == true)
								return false;
						}
					}
					else
					{
						split.BaseQty = 1;
						split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
					}
				}

				SplitCache.RaiseFieldUpdated<SOLineSplit.isAllocated>(split, copy.IsAllocated);
				SplitCache.RaiseRowUpdated(split, copy);

				if (copy.BaseQty - 1 > 0m)
				{
					if (split.IsAllocated == true || (split.IsAllocated != true && split.Operation == SOOperation.Receipt))
					{
						copy.SplitLineNbr = null;
						copy.PlanID = null;
						copy.IsAllocated = false;
						copy.LotSerialNbr = null;
						copy.BaseQty -= 1;
						copy.Qty = INUnitAttribute.ConvertFromBase(SplitCache, copy.InventoryID, copy.UOM, (decimal)copy.BaseQty, INPrecision.QUANTITY);
						SplitCache.Insert(copy);
					}

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
							return NegativeInventoryError(split);
						}
						else
						{
							FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(split);
							split.IsAllocated = true;
							SplitCache.RaiseFieldUpdated<FSSODetSplit.isAllocated>(split, copy.IsAllocated);
							SplitCache.RaiseRowUpdated(split, copy);
						}
						return true;
					}

					//Lot/Serial Nbr. selected on allocated line. Available Qty. verification procedure 
					if (split.IsAllocated == true)
					{
						FSSODetSplit copy = PXCache<FSSODetSplit>.CreateCopy(split);
						if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail >= 0m && split.Operation != SOOperation.Receipt)
						{
							split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
						}
						else if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail < 0m && split.Operation != SOOperation.Receipt)
						{
							split.BaseQty += accum.QtyHardAvail;
							if (split.BaseQty <= 0m)
								if (NegativeInventoryError(split))
									return false;

							split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
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
								copy.SplitLineNbr = null;
								copy.PlanID = null;
								copy.IsAllocated = false;
								copy.LotSerialNbr = null;
								copy.BaseQty -= split.BaseQty;
								copy.Qty = INUnitAttribute.ConvertFromBase(SplitCache, copy.InventoryID, copy.UOM, copy.BaseQty.Value, INPrecision.QUANTITY);
								copy = (FSSODetSplit)SplitCache.Insert(copy);
								if (copy.LotSerialNbr != null && copy.IsAllocated != true)
								{
									SplitCache.SetValue<FSSODetSplit.lotSerialNbr>(copy, null);
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
		/// Overrides <see cref="FSServiceOrderItemAvailabilityExtension.ShowSplits(PXAdapter)"/>
		/// </summary>
		[PXOverride]
		public virtual IEnumerable ShowSplits(PXAdapter adapter, Func<PXAdapter, IEnumerable> base_ShowSplits)
		{
			if (IsAllocationEntryEnabled)
			{
				// TODO: Disable all editing in the split window when the item is a non-stock.
				// We must allow opening the split window with non-stock items
				// so that the user can see the PO receipt information.
				/*
				if (LineCurrent.SOLineType != SOLineType.Inventory)
				{
					throw new PXSetPropertyException(TX.Error.CANNOT_USE_ALLOCATIONS_FOR_NONSTOCK_ITEMS);
				}
				*/
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
		public virtual void UpdateParent(FSSODet line,
			Action<FSSODet> base_UpdateParent)
		{
			if (line != null && line.RequireShipping == true)
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
		public virtual void UpdateParent(FSSODetSplit newSplit, FSSODetSplit oldSplit,
			Action<FSSODetSplit, FSSODetSplit> base_UpdateParent)
		{
			FSSODetSplit anySplit = newSplit ?? oldSplit;
			FSSODet line = (FSSODet)LSParentAttribute.SelectParent(SplitCache, anySplit, typeof(FSSODet));

			if (line != null && line.RequireShipping == true)
			{
				if (anySplit != null && anySplit.InventoryID == line.InventoryID)
				{
					FSSODet oldLine = PXCache<FSSODet>.CreateCopy(line);

					Base1.UpdateParent(
						line,
						newSplit?.Completed == false ? newSplit : null,
						oldSplit?.Completed == false ? oldSplit : null,
						out decimal baseQty);

					using (new FSServiceOrderLineSplittingExtension.InvtMultScope(line))
					{
						if (Base1.IsLotSerialRequired && newSplit != null)
						{
							if (IsLotSerialItem(newSplit))
								line.UnassignedQty = SelectSplits(newSplit).Where(s => s.LotSerialNbr == null).Sum(s => s.BaseQty);
							else
								line.UnassignedQty = 0m;
						}

						line.BaseQty = baseQty;
						line.Qty = INUnitAttribute.ConvertFromBase(LineCache, line.InventoryID, line.UOM, line.BaseQty.Value, INPrecision.QUANTITY);
					}

					LineCache.MarkUpdated(line);

					LineCache.RaiseFieldUpdated<FSSODet.orderQty>(line, oldLine.Qty);

					if (LineCache.RaiseRowUpdating(oldLine, line))
						LineCache.RaiseRowUpdated(line, oldLine);
					else
						PXCache<FSSODet>.RestoreCopy(line, oldLine);
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
		public virtual void UpdateParent(FSSODet line, FSSODetSplit newSplit, FSSODetSplit oldSplit, out decimal baseQty,
			UpdateParentDelegate base_UpdateParent)
		{
			ResetAvailabilityCounters(line);

			bool counted = LineCounters.ContainsKey(line);

			base_UpdateParent(line, newSplit, oldSplit, out baseQty);

			if (!counted && oldSplit != null)
			{
				if (LineCounters.TryGetValue(line, out Counters counters))
				{
					if (oldSplit.POCreate == true)
						counters.BaseQty += oldSplit.BaseReceivedQty.Value + oldSplit.BaseShippedQty.Value;

					if (oldSplit.ShipmentNbr != null)
						counters.BaseQty += oldSplit.BaseQty.Value - oldSplit.BaseShippedQty.Value;

					baseQty = counters.BaseQty;
				}
			}
		}
		public delegate void UpdateParentDelegate(FSSODet line, FSSODetSplit newSplit, FSSODetSplit oldSplit, out decimal baseQty);

		public static void ResetAvailabilityCounters(FSSODet line)
		{
			line.LineQtyAvail = null;
			line.LineQtyHardAvail = null;
		}
		#endregion

		/// <summary>
		/// Overrides <see cref="FSServiceOrderLineSplittingExtension.UpdateCounters(Counters, FSSODetSplit)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateCounters(Counters counters, FSSODetSplit split,
			Action<Counters, FSSODetSplit> base_UpdateCounters)
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
		public virtual void DefaultLotSerialNbr(FSSODetSplit row,
			Action<FSSODetSplit> base_DefaultLotSerialNbr)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(row.InventoryID);

			if (item != null && !IsAllocationEntryEnabled || ((INLotSerClass)item).LotSerAssign != INLotSerAssign.WhenUsed)
				base_DefaultLotSerialNbr(row);
		}
		#endregion

		#region Schedules
		public virtual void UncompleteSchedules(FSSODet line)
		{
			LineCounters.Remove(line);

			decimal? unshippedQty = line.BaseOpenQty;
			foreach (var split in SelectSplitsReversed(line, false))
			{
				if (split.ShipmentNbr == null)
				{
					unshippedQty -= split.BaseQty;

					FSSODetSplit newdetail = PXCache<FSSODetSplit>.CreateCopy(split);
					newdetail.Completed = false;

					SplitCache.Update(newdetail);
				}
			}

			IssueAvailable(line, unshippedQty.Value, true);
		}

		public virtual void CompleteSchedules(FSSODet line)
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

			foreach (var split in SelectSplitsReversed(line))
			{
				FSSODetSplit newSplit = PXCache<FSSODetSplit>.CreateCopy(split);
				newSplit.Completed = true;

				SplitCache.Update(newSplit);
			}
		}

		public virtual void TruncateSchedules(FSSODet line, decimal baseQty)
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
					FSSODetSplit newSplit = PXCache<FSSODetSplit>.CreateCopy(split);
					newSplit.BaseQty -= baseQty;
					newSplit.Qty = INUnitAttribute.ConvertFromBase(SplitCache, newSplit.InventoryID, newSplit.UOM, newSplit.BaseQty.Value, INPrecision.QUANTITY);

					SplitCache.Update(newSplit);
					break;
				}
			}
		}


		protected virtual bool SchedulesEqual(FSSODetSplit a, FSSODetSplit b)
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
					a.Completed == b.Completed &&
					a.POCreate == b.POCreate &&
					a.POCompleted == b.POCompleted &&
					a.PONbr == b.PONbr &&
					a.POLineNbr == b.POLineNbr &&
					a.SOOrderType == b.SOOrderType &&
					a.SOOrderNbr == b.SOOrderNbr &&
					a.SOLineNbr == b.SOLineNbr &&
					a.SOSplitLineNbr == b.SOSplitLineNbr;
			}
			else
			{
				return a != null;
			}
		}


		protected virtual void IssueAvailable(FSSODet line) => IssueAvailable(line, line.BaseOpenQty);
		protected virtual void IssueAvailable(FSSODet line, decimal? baseQty) => IssueAvailable(line, baseQty, false);
		protected virtual void IssueAvailable(FSSODet line, decimal? baseQty, bool isUncomplete)
		{
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
				FSSODetSplit split = (FSSODetSplit)line;
				if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
					split.UOM = ((InventoryItem)item).BaseUnit;

				split.SplitLineNbr = null;
				split.IsAllocated = line.RequireAllocation;
				split.SiteID = line.SiteID;

				SplitCache.RaiseFieldDefaulting<FSSODetSplit.allocatedPlanType>(split, out var defaultAllocatedPlanType);
				SplitCache.SetValue<FSSODetSplit.allocatedPlanType>(split, defaultAllocatedPlanType);

				SplitCache.RaiseFieldDefaulting<FSSODetSplit.backOrderPlanType>(split, out var defaultBackOrderPlanType);
				SplitCache.SetValue<FSSODetSplit.backOrderPlanType>(split, defaultBackOrderPlanType);

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
						split.Qty = INUnitAttribute.ConvertFromBase(LineCache, split.InventoryID, split.UOM, availableQty.Value, INPrecision.QUANTITY);
						SplitCache.Insert(split);
						baseQty -= availableQty;
					}
					else
					{
						split.BaseQty = baseQty;
						split.Qty = INUnitAttribute.ConvertFromBase(LineCache, split.InventoryID, split.UOM, baseQty.Value, INPrecision.QUANTITY);
						SplitCache.Insert(split);
						baseQty = 0m;
						break;
					}
				}
			}

			if (baseQty > 0m && line.InventoryID != null && line.SiteID != null && (line.SubItemID != null || (line.SubItemID == null && line.IsStockItem != true && line.IsKit == true) || line.SOLineType == SOLineType.NonInventory))
			{
				FSSODetSplit split = (FSSODetSplit)line;
				if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
					split.UOM = ((InventoryItem)item).BaseUnit;

				split.SplitLineNbr = null;
				split.IsAllocated = false;
				split.BaseQty = baseQty;
				split.Qty = INUnitAttribute.ConvertFromBase(LineCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);

				if (isUncomplete)
				{
					split.POCreate = false;
					split.POSource = null;
				}

				SplitCache.Insert(PXCache<FSSODetSplit>.CreateCopy(split));
			}
		}
		#endregion


		protected virtual bool IsSplitRequired(FSSODet line, bool externalCall) => IsSplitRequired(line, externalCall, out _);
		protected virtual bool IsSplitRequired(FSSODet line, bool externalCall, out InventoryItem item)
		{
			if (line == null)
			{
				item = null;
				return false;
			}

			bool skipSplitCreating = false;
			item = (InventoryItem)PXSelectorAttribute.Select<FSSODet.inventoryID>(LineCache, line);

			if (Base1.IsLocationEnabled && item != null && item.StkItem == false && item.KitItem == false && item.NonStockShip == false)
				skipSplitCreating = true;

			if (item != null && item.StkItem == false && item.KitItem == true && line.Behavior != SOBehavior.CM && line.Behavior != SOBehavior.IN)
				skipSplitCreating = true;

			return !skipSplitCreating && (Base1.IsLocationEnabled || Base1.IsLotSerialRequired && externalCall && line.POCreate != true && IsLotSerialItem(line));
		}

		protected virtual bool ShouldThrowExceptions() => Base.GraphAppointmentEntryCaller != null;

		protected virtual bool IsLotSerialItem(ILSMaster line)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			if (item == null)
				return false;

			return INLotSerialNbrAttribute.IsTrack(item, line.TranType, line.InvtMult);
		}

		protected virtual bool NegativeInventoryError(FSSODetSplit split)
		{
			split.IsAllocated = false;
			split.LotSerialNbr = null;
			SplitCache.RaiseExceptionHandling<FSSODetSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
			return split.IsAllocated == true;
		}

		private void RefreshViewOf(PXCache cache)
		{
			foreach (var pair in Base.Views)
			{
				PXView view = pair.Value;
				if (view.IsReadOnly == false && view.GetItemType() == cache.GetItemType())
					view.RequestRefresh();
			}
		}
	}
}
