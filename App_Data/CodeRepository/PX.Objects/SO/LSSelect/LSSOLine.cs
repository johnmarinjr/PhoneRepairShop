using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;

using PurchaseSupplyBaseExt = PX.Objects.SO.GraphExtensions.SOOrderEntryExt.PurchaseSupplyBaseExt;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.SO
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public class LSSOLine : LSSelectSOBase<SOLine, SOLineSplit,
		Where<SOLineSplit.orderType, Equal<Current<SOOrder.orderType>>,
		And<SOLineSplit.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
	{
		#region State
		public bool IsLocationEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				if (ordertype == null || (ordertype.RequireShipping == false && ordertype.RequireLocation == true && ordertype.INDocType != INTranType.NoUpdate))
					return true;
				else
					return false;
			}
		}

		public bool IsLSEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				return (ordertype == null || ordertype.RequireLocation == true || ordertype.RequireLotSerial == true);
			}
		}

		public bool IsLotSerialRequired
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				return (ordertype == null || ordertype.RequireLotSerial == true);
			}
		}

		public bool IsAllocationEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				return (ordertype == null || ordertype.RequireShipping == true);
			}
		}

		public bool IsAllocationRequired
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				return (ordertype == null || ordertype.RequireAllocation == true);
			}
		}
		#endregion
		#region Ctor
		public LSSOLine(PXGraph graph)
			: base(graph)
		{
			MasterQtyField = typeof(SOLine.orderQty);
			graph.FieldDefaulting.AddHandler<SOLineSplit.subItemID>(SOLineSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<SOLineSplit.locationID>(SOLineSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<SOLineSplit.invtMult>(SOLineSplit_InvtMult_FieldDefaulting);
			graph.RowSelected.AddHandler<SOOrder>(Parent_RowSelected);
			graph.RowUpdated.AddHandler<SOOrder>(SOOrder_RowUpdated);
			graph.RowSelected.AddHandler<SOLineSplit>(SOLineSplit_RowSelected);
			graph.RowPersisting.AddHandler<SOLineSplit>(SOLineSplit_RowPersisting);
		}
		#endregion

		#region Implementation
		public override IEnumerable BinLotSerial(PXAdapter adapter)
		{
			if (IsLSEntryEnabled || IsAllocationEntryEnabled)
			{
				SOLine currentSOLine = (SOLine)MasterCache.Current;
				if (currentSOLine != null && ((IsLSEntryEnabled && currentSOLine.LineType != SOLineType.Inventory) || currentSOLine.LineType == SOLineType.MiscCharge))
				{
					throw new PXSetPropertyException(Messages.BinLotSerialInvalid);
				}

				if (currentSOLine != null && currentSOLine.POCreate == true && currentSOLine.IsLegacyDropShip != true
					&& currentSOLine.POSource.IsIn(INReplenishmentSource.DropShipToOrder, INReplenishmentSource.BlanketDropShipToOrder))
				{
					if (!IsLotSerialsAllowedForDropShipLine(MasterCache, currentSOLine))
					{
						var inventory = InventoryItem.PK.Find(MasterCache.Graph, currentSOLine.InventoryID);
						throw new PXSetPropertyException(Messages.BinLotSerialEntryDisabledDS, inventory.InventoryCD);
					}
				}

				View.AskExt(true);
			}
			return adapter.Get();
		}

		protected virtual void SOOrder_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (IsLSEntryEnabled && (bool?)sender.GetValue<SOOrder.cancelled>(e.Row) == false &&
				((!sender.ObjectsEqual<SOOrder.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<SOOrder.hold>(e.Row) == false) ||
				!sender.ObjectsEqual<SOOrder.cancelled>(e.Row, e.OldRow)))
			{
				PXCache cache = sender.Graph.Caches[typeof(SOLine)];

				foreach (SOLine item in PXParentAttribute.SelectSiblings(cache, null, typeof(SOOrder)))
				{
					if (Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<SOLine.orderQty>(item, item.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						cache.MarkUpdated(item);
					}
				}
			}
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				var row = (SOLine)e.Row;
				if ((row.LineType == SOLineType.Inventory || row.LineType == SOLineType.NonInventory && row.InvtMult == (short)-1) && row.TranType != INTranType.NoUpdate && row.BaseQty < 0m)
				{
					if (sender.RaiseExceptionHandling<SOLine.orderQty>(e.Row, ((SOLine)e.Row).Qty, new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString())))
					{
						throw new PXRowPersistingException(typeof(SOLine.orderQty).Name, ((SOLine)e.Row).Qty, CS.Messages.Entry_GE, ((int)0).ToString());
					}
					return;
				}

				if (IsLSEntryEnabled)
				{
					PXCache cache = sender.Graph.Caches[typeof(SOOrder)];
					object doc = PXParentAttribute.SelectParent(sender, e.Row, typeof(SOOrder)) ?? cache.Current;

					bool? OnHold = (bool?)cache.GetValue<SOOrder.hold>(doc);
					bool? Cancelled = (bool?)cache.GetValue<SOOrder.cancelled>(doc);

					if (Cancelled == false && OnHold == false && Math.Abs((decimal)((SOLine)e.Row).BaseQty) >= 0.0000005m && (((SOLine)e.Row).UnassignedQty >= 0.0000005m || ((SOLine)e.Row).UnassignedQty <= -0.0000005m))
					{
						if (sender.RaiseExceptionHandling<SOLine.orderQty>(e.Row, ((SOLine)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						{
							throw new PXRowPersistingException(typeof(SOLine.orderQty).Name, ((SOLine)e.Row).Qty, Messages.BinLotSerialNotAssigned);
						}
					}
				}
			}

			//for normal orders there are only when received numbers which do not require any additional processing
			if (!IsLSEntryEnabled)
			{
				if (((SOLine)e.Row).TranType == INTranType.Transfer && DetailCounters.ContainsKey((SOLine)e.Row))
				{
					//keep Counters when adding splits to Transfer order
					DetailCounters[(SOLine)e.Row].UnassignedNumber = 0;
				}
				else
				{
					DetailCounters[(SOLine)e.Row] = new Counters { UnassignedNumber = 0 };
				}
			}

			base.Master_RowPersisting(sender, e);
		}

		public bool AvailabilityFetching { get; private set; }
		public override IStatus AvailabilityFetch(PXCache sender, ILSMaster Row, AvailabilityFetchMode fetchMode)
		{
			try
			{
				AvailabilityFetching = true;
				return AvailabilityFetchImpl(sender, Row, fetchMode);
			}
			finally
			{
				AvailabilityFetching = false;
			}
		}

		public virtual IStatus AvailabilityFetchImpl(PXCache sender, ILSMaster Row, AvailabilityFetchMode fetchMode)
		{
			if (Row != null)
			{
				SOLineSplit copy = Row as SOLineSplit;
				if (copy == null)
				{
					copy = Convert(Row as SOLine);

					PXParentAttribute.SetParent(DetailCache, copy, typeof(SOLine), Row);

					if (string.IsNullOrEmpty(Row.LotSerialNbr) == false)
					{
						DefaultLotSerialNbr(sender.Graph.Caches[typeof(SOLineSplit)], copy);
					}

					if (fetchMode.HasFlag(AvailabilityFetchMode.TryOptimize) && _detailsRequested++ == 5)
					{
						foreach (PXResult<SOLine, INUnit, INSiteStatus> res in
							PXSelectReadonly2<SOLine,
							InnerJoin<INUnit, On<
								INUnit.inventoryID, Equal<SOLine.inventoryID>,
								And<INUnit.fromUnit, Equal<SOLine.uOM>>>,
							InnerJoin<INSiteStatus, On<
								SOLine.inventoryID, Equal<INSiteStatus.inventoryID>,
								And<SOLine.subItemID, Equal<INSiteStatus.subItemID>,
								And<SOLine.siteID, Equal<INSiteStatus.siteID>>>>>>,
							Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>,
								And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
							.Select(sender.Graph))
						{
							INSiteStatus status = res;
							INUnit unit = res;

							INUnit.UK.ByInventory.StoreCached(sender.Graph, unit);
							INSiteStatus.PK.StoreCached(sender.Graph, status);
						}

						foreach (INItemPlan plan in PXSelect<INItemPlan, Where<INItemPlan.refNoteID, Equal<Current<SOOrder.noteID>>>>.Select(this._Graph))
						{
							PXSelect<INItemPlan,
							Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>
							.StoreResult(this._Graph, plan);
						}
					}

					if (fetchMode.HasFlag(AvailabilityFetchMode.ExcludeCurrent))
					{
						IStatus result = AvailabilityFetch(sender, copy, fetchMode.HasFlag(AvailabilityFetchMode.Project) ? AvailabilityFetchMode.Project : AvailabilityFetchMode.None);
						return DeductAllocated(sender, (SOLine)Row, result);
					}
				}

				return AvailabilityFetch(sender, copy, fetchMode);
			}
			return null;
		}

		public virtual IStatus DeductAllocated(PXCache sender, SOLine soLine, IStatus result)
		{
			if (result == null) return null;
			decimal? lineQtyAvail = (decimal?)sender.GetValue<SOLine.lineQtyAvail>(soLine);
			decimal? lineQtyHardAvail = (decimal?)sender.GetValue<SOLine.lineQtyHardAvail>(soLine);

			if (lineQtyAvail == null || lineQtyHardAvail == null)
			{
				lineQtyAvail = 0m;
				lineQtyHardAvail = 0m;

				foreach (SOLineSplit split in SelectDetail(DetailCache, soLine))
				{
					SOLineSplit detail = split;
					if (detail.PlanID != null)
					{
						INItemPlan plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(this._Graph, detail.PlanID);
						if (plan != null)
						{
							detail = PXCache<SOLineSplit>.CreateCopy(detail);
							detail.PlanType = plan.PlanType;
						}
					}

					PXParentAttribute.SetParent(DetailCache, detail, typeof(SOLine), soLine);

					decimal signQtyAvail;
					decimal signQtyHardAvail;
					INItemPlanIDAttribute.GetInclQtyAvail<SiteStatus>(DetailCache, detail, out signQtyAvail, out signQtyHardAvail);

					if (signQtyAvail != 0m)
					{
						lineQtyAvail -= signQtyAvail * (detail.BaseQty ?? 0m);
					}

					if (signQtyHardAvail != 0m)
					{
						lineQtyHardAvail -= signQtyHardAvail * (detail.BaseQty ?? 0m);
					}
				}

				sender.SetValue<SOLine.lineQtyAvail>(soLine, lineQtyAvail);
				sender.SetValue<SOLine.lineQtyHardAvail>(soLine, lineQtyHardAvail);
			}

			result.QtyAvail += lineQtyAvail;
			result.QtyHardAvail += lineQtyHardAvail;
			result.QtyNotAvail = -lineQtyAvail;

			return result;
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			SOLine tran = (SOLine)e.Row;
			AvailabilityFetchMode fetchMode = tran?.Completed == true
				? AvailabilityFetchMode.None
				: AvailabilityFetchMode.ExcludeCurrent;
			IStatus availability = GetAvailability(sender, tran, fetchMode);
			if (availability != null)
			{
				if (!PXAccess.FeatureInstalled<FeaturesSet.materialManagement>())
				{
					e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability);
					AvailabilityCheck(sender, tran, availability);
				}
				else
				{
					IStatus availabilityProject = GetAvailability(sender, tran, fetchMode | AvailabilityFetchMode.Project);
					if (availabilityProject != null)
					{
						e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability, availabilityProject);
						AvailabilityCheck(sender, tran, availabilityProject);
					}
				}
			}
			else
			{
				//handle missing UOM
				INUnitAttribute.ConvertFromBase<SOLine.inventoryID, SOLine.uOM>(sender, e.Row, 0m, INPrecision.QUANTITY);
				e.ReturnValue = string.Empty;
			}

			base.Availability_FieldSelecting(sender, e);
		}

		private IStatus GetAvailability(PXCache sender, SOLine tran, AvailabilityFetchMode fetchMode)
		{
			IStatus availability = AvailabilityFetch(sender, tran, fetchMode);

			if (availability != null)
			{
				decimal unitRate = INUnitAttribute.ConvertFromBase<SOLine.inventoryID, SOLine.uOM>(sender, tran, 1m, INPrecision.NOROUND);
				availability.QtyOnHand = PXDBQuantityAttribute.Round((decimal)availability.QtyOnHand * unitRate);
				availability.QtyAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyAvail * unitRate);
				availability.QtyNotAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyNotAvail * unitRate);
				availability.QtyHardAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyHardAvail * unitRate);
			}

			return availability;
		}

		private string BuildAvailabilityStatusLine(PXCache sender, SOLine tran, IStatus availability)
		{
			if (IsAllocationEntryEnabled)
			{
				decimal unitRate = INUnitAttribute.ConvertFromBase<SOLine.inventoryID, SOLine.uOM>(sender, tran, 1m, INPrecision.NOROUND);
				Decimal? allocated = PXDBQuantityAttribute.Round((decimal)(tran.LineQtyHardAvail ?? 0m) * unitRate); ;
				return PXMessages.LocalizeFormatNoPrefix(Messages.Availability_AllocatedInfo,
						sender.GetValue<SOLine.uOM>(tran), FormatQty(availability.QtyOnHand), FormatQty(availability.QtyAvail), FormatQty(availability.QtyHardAvail), FormatQty(allocated));
			}
			else
				return PXMessages.LocalizeFormatNoPrefix(Messages.Availability_Info,
						sender.GetValue<SOLine.uOM>(tran), FormatQty(availability.QtyOnHand), FormatQty(availability.QtyAvail), FormatQty(availability.QtyHardAvail));
		}

		private string BuildAvailabilityStatusLine(PXCache sender, SOLine tran, IStatus availability, IStatus availabilityProject)
		{
			if (IsAllocationEntryEnabled)
			{
				decimal unitRate = INUnitAttribute.ConvertFromBase<SOLine.inventoryID, SOLine.uOM>(sender, tran, 1m, INPrecision.NOROUND);
				Decimal? allocated = PXDBQuantityAttribute.Round((decimal)(tran.LineQtyHardAvail ?? 0m) * unitRate); ;
				return PXMessages.LocalizeFormatNoPrefix(Messages.Availability_AllocatedInfo_Project,
					sender.GetValue<SOLine.uOM>(tran),
					FormatQty(availabilityProject.QtyOnHand),
					FormatQty(availabilityProject.QtyAvail),
					FormatQty(availabilityProject.QtyHardAvail),
					FormatQty(allocated),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail));
			}
			else
				return string.Format(
					PXMessages.LocalizeNoPrefix(IN.Messages.Availability_Info_Project),
					sender.GetValue<SOLine.uOM>(tran),
					FormatQty(availabilityProject.QtyOnHand),
					FormatQty(availabilityProject.QtyAvail),
					FormatQty(availabilityProject.QtyHardAvail),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail));
		}

		protected SOOrder _LastSelected;

		protected virtual void Parent_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (_LastSelected == null || !object.ReferenceEquals(_LastSelected, e.Row))
			{
				PXUIFieldAttribute.SetRequired<SOLine.locationID>(this.MasterCache, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.locationID>(this.MasterCache, null, IsLocationEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.lotSerialNbr>(this.MasterCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLine.expireDate>(this.MasterCache, null, IsLSEntryEnabled);

				PXUIFieldAttribute.SetVisible<SOLineSplit.inventoryID>(this.DetailCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.locationID>(this.DetailCache, null, IsLocationEnabled);
				//PXUIFieldAttribute.SetVisible<SOLineSplit.lotSerialNbr>(this.DetailCache, null, IsLSEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.expireDate>(this.DetailCache, null, IsLSEntryEnabled);

				PXUIFieldAttribute.SetVisible<SOLineSplit.shipDate>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.isAllocated>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.completed>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.shippedQty>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.shipmentNbr>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOType>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pONbr>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOReceiptNbr>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOSource>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.pOCreate>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.receivedQty>(this.DetailCache, null, IsAllocationEntryEnabled);
				PXUIFieldAttribute.SetVisible<SOLineSplit.refNoteID>(this.DetailCache, null, IsAllocationEntryEnabled);

				PXView view;
				if (sender.Graph.Views.TryGetValue(Prefixed("lotseropts"), out view))
				{
					view.AllowSelect = IsLSEntryEnabled;
				}

				if (e.Row is SOOrder)
				{
					_LastSelected = (SOOrder)e.Row;
				}
			}
			this.SetEnabled(IsLSEntryEnabled || IsAllocationEntryEnabled);
		}

		protected virtual void IssueAvailable(PXCache sender, SOLine Row, decimal? BaseQty)
		{
			IssueAvailable(sender, Row, BaseQty, false);
		}

		protected virtual void IssueAvailable(PXCache sender, SOLine Row, decimal? BaseQty, bool isUncomplete)
		{
			DetailCounters.Remove(Row);
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
			foreach (INSiteStatus avail in PXSelectReadonly<INSiteStatus,
				Where<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>,
				And<INSiteStatus.subItemID, Equal<Required<INSiteStatus.subItemID>>,
				And<INSiteStatus.siteID, Equal<Required<INSiteStatus.siteID>>>>>,
				OrderBy<Asc<INLocation.pickPriority>>>.Select(this._Graph, Row.InventoryID, Row.SubItemID, Row.SiteID))
			{
				SOLineSplit split = (SOLineSplit)Row;
				if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
				{
					split.UOM = ((InventoryItem)item).BaseUnit;
				}
				split.SplitLineNbr = null;
				split.IsAllocated = Row.RequireAllocation;
				split.SiteID = Row.SiteID;

				object newval;
				DetailCache.RaiseFieldDefaulting<SOLineSplit.allocatedPlanType>(split, out newval);
				DetailCache.SetValue<SOLineSplit.allocatedPlanType>(split, newval);

				DetailCache.RaiseFieldDefaulting<SOLineSplit.backOrderPlanType>(split, out newval);
				DetailCache.SetValue<SOLineSplit.backOrderPlanType>(split, newval);

				decimal SignQtyAvail;
				decimal SignQtyHardAvail;
				INItemPlanIDAttribute.GetInclQtyAvail<SiteStatus>(DetailCache, split, out SignQtyAvail, out SignQtyHardAvail);

				if (SignQtyHardAvail < 0m)
				{
					SiteStatus accumavail = new SiteStatus();
					PXCache<INSiteStatus>.RestoreCopy(accumavail, avail);

					accumavail = (SiteStatus)this._Graph.Caches[typeof(SiteStatus)].Insert(accumavail);

					decimal? AvailableQty = avail.QtyHardAvail + accumavail.QtyHardAvail;

					if (AvailableQty <= 0m)
					{
						continue;
					}

					if (AvailableQty < BaseQty)
					{
						split.BaseQty = AvailableQty;
						SetDetailQtyWithMaster(sender, split, Row);
						DetailCache.Insert(split);

						BaseQty -= AvailableQty;
					}
					else
					{
						split.BaseQty = BaseQty;
						SetDetailQtyWithMaster(sender, split, Row);
						DetailCache.Insert(split);

						BaseQty = 0m;
						break;
					}
				}
			}

			if (BaseQty > 0m && Row.InventoryID != null && Row.SiteID != null && (Row.SubItemID != null || (Row.SubItemID == null && Row.IsStockItem != true && Row.IsKit == true) || Row.LineType == SOLineType.NonInventory))
			{
				SOLineSplit split = (SOLineSplit)Row;
				if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
				{
					split.UOM = ((InventoryItem)item).BaseUnit;
				}
				split.SplitLineNbr = null;
				split.IsAllocated = false;
				split.BaseQty = BaseQty;
				SetDetailQtyWithMaster(sender, split, Row);

				BaseQty = 0m;

				if (isUncomplete)
				{
					split.POCreate = false;
					split.POSource = null;
				}

				DetailCache.Insert(PXCache<SOLineSplit>.CreateCopy(split));
			}
		}

		public override void UpdateParent(PXCache sender, SOLine Row)
		{
			if (Row != null && Row.RequireShipping == true && !IsSplitRequired(sender, Row))
			{
				decimal BaseQty;
				UpdateParent(sender, Row, null, null, out BaseQty);
			}
			else
			{
				base.UpdateParent(sender, Row);
			}
		}

		public override void UpdateParent(PXCache sender, SOLineSplit Row, SOLineSplit OldRow)
		{
			SOLine parent = (SOLine)LSParentAttribute.SelectParent(sender, Row ?? OldRow, typeof(SOLine));

			if (parent != null && parent.RequireShipping == true)
			{
				if ((Row ?? OldRow) != null && SameInventoryItem((ILSMaster)(Row ?? OldRow), (ILSMaster)parent))
				{
					SOLine oldrow = PXCache<SOLine>.CreateCopy(parent);
					decimal BaseQty;

					UpdateParent(sender, parent, (Row != null && Row.Completed == false ? Row : null), (OldRow != null && OldRow.Completed == false ? OldRow : null), out BaseQty);

					using (InvtMultScope<SOLine> ms = new InvtMultScope<SOLine>(parent))
					{
						if (IsLotSerialRequired && Row != null)
						{
							parent.UnassignedQty = 0m;
							if (IsLotSerialItem(sender, Row))
							{
								object[] splits = SelectDetail(sender, Row);
								foreach (SOLineSplit split in splits)
								{
									if (split.LotSerialNbr == null)
									{
										parent.UnassignedQty += split.BaseQty;
									}
								}
							}
						}
						parent.BaseQty = BaseQty + parent.BaseClosedQty;
						SetMasterQtyFromBase(sender, parent);
					}

					sender.Graph.Caches[typeof(SOLine)].MarkUpdated(parent);

					if (parent.Qty != oldrow.Qty)
					{
						sender.Graph.Caches[typeof(SOLine)].RaiseFieldUpdated(_MasterQtyField, parent, oldrow.Qty);
					}
					if (sender.Graph.Caches[typeof(SOLine)].RaiseRowUpdating(oldrow, parent))
					{
						sender.Graph.Caches[typeof(SOLine)].RaiseRowUpdated(parent, oldrow);
					}
					else
					{
						sender.Graph.Caches[typeof(SOLine)].RestoreCopy(parent, oldrow);
					}
				}
			}
			else
			{
				base.UpdateParent(sender, Row, OldRow);
			}
		}

		public static void ResetAvailabilityCounters(SOLine row)
		{
			row.LineQtyAvail = null;
			row.LineQtyHardAvail = null;
		}

		public override void UpdateParent(PXCache sender, SOLine Row, SOLineSplit Det, SOLineSplit OldDet, out decimal BaseQty)
		{
			ResetAvailabilityCounters(Row);

			bool counted = DetailCounters.ContainsKey(Row);

			base.UpdateParent(sender, Row, Det, OldDet, out BaseQty);

			if (!counted && OldDet != null)
			{
				Counters counters;
				if (DetailCounters.TryGetValue(Row, out counters))
				{
					if (OldDet.POCreate == true || OldDet.AMProdCreate == true)
					{
						counters.BaseQty += (decimal)OldDet.BaseReceivedQty + (decimal)OldDet.BaseShippedQty;
					}
					//if (OldDet.ShipmentNbr != null)
					//{
					//    counters.BaseQty += (decimal)(OldDet.BaseQty - OldDet.BaseShippedQty);
					//}
					BaseQty = counters.BaseQty;
				}
			}
		}

		protected override void UpdateCounters(PXCache sender, Counters counters, SOLineSplit detail)
		{
			base.UpdateCounters(sender, counters, detail);

			if (detail.POCreate == true || detail.AMProdCreate == true)
			{
				//base shipped qty in context of purchase for so is meaningless and equals zero, so it's appended for dropship context
				counters.BaseQty -= (decimal)detail.BaseReceivedQty + (decimal)detail.BaseShippedQty;
			}

			if (IsAllocationEntryEnabled)
			{
				counters.LotSerNumbersNull = -1;
				counters.LotSerNumber = null;
				counters.LotSerNumbers.Clear();
			}

			//if (detail.ShipmentNbr != null)
			//{
			//    counters.BaseQty -= (decimal)(detail.BaseQty - detail.BaseShippedQty);
			//}
		}

		protected int _detailsRequested = 0;

		protected override object[] SelectDetail(PXCache sender, SOLineSplit row)
		{
			return SelectDetail(sender, row, true);
		}

		protected virtual object[] SelectDetail(PXCache sender, SOLineSplit row, bool ExcludeCompleted = true)
		{
			object[] ret;
			if (_detailsRequested > 5)
			{
				ret = PXParentAttribute.SelectSiblings(sender, row, typeof(SOOrder));

				return Array.FindAll(ret, a =>
					SameInventoryItem((SOLineSplit)a, row) && ((SOLineSplit)a).LineNbr == row.LineNbr && (((SOLineSplit)a).Completed == false || ExcludeCompleted == false && ((SOLineSplit)a).PONbr == null && ((SOLineSplit)a).SOOrderNbr == null));
			}

			ret = base.SelectDetail(sender, row);
			return Array.FindAll<object>(ret, a => (((SOLineSplit)a).Completed == false || ExcludeCompleted == false && ((SOLineSplit)a).PONbr == null && ((SOLineSplit)a).SOOrderNbr == null));
		}


		protected override object[] SelectDetail(PXCache sender, SOLine row)
		{
			object[] ret = SelectAllDetails(sender, row);
			if (_detailsRequested > 5)
			{
				return Array.FindAll(ret, a => ((SOLineSplit)a).Completed == false);
			}

			return Array.FindAll<object>(ret, a => ((SOLineSplit)a).Completed == false);
		}

		protected virtual object[] SelectAllDetails(PXCache sender, SOLine row)
		{
			if (_detailsRequested > 5)
			{
				object[] ret = PXParentAttribute.SelectSiblings(sender, Convert(row), typeof(SOOrder));
				return Array.FindAll(ret, a => SameInventoryItem((SOLineSplit)a, row) && ((SOLineSplit)a).LineNbr == row.LineNbr);
			}

			return base.SelectDetail(sender, row);
		}

		protected override object[] SelectDetailOrdered(PXCache sender, SOLineSplit row)
		{
			return SelectDetailOrdered(sender, row, true);
		}

		protected virtual object[] SelectDetailOrdered(PXCache sender, SOLineSplit row, bool ExcludeCompleted = true)
		{
			object[] ret = SelectDetail(sender, row, ExcludeCompleted);

			Array.Sort<object>(ret, new Comparison<object>(delegate (object a, object b)
			{
				object aIsAllocated = ((SOLineSplit)a).Completed == true ? 0 : ((SOLineSplit)a).IsAllocated == true ? 1 : 2;
				object bIsAllocated = ((SOLineSplit)b).Completed == true ? 0 : ((SOLineSplit)b).IsAllocated == true ? 1 : 2;

				int res = ((IComparable)aIsAllocated).CompareTo(bIsAllocated);

				if (res != 0)
				{
					return res;
				}

				object aSplitLineNbr = ((SOLineSplit)a).SplitLineNbr;
				object bSplitLineNbr = ((SOLineSplit)b).SplitLineNbr;

				return ((IComparable)aSplitLineNbr).CompareTo(bSplitLineNbr);
			}));

			return ret;
		}

		protected override object[] SelectDetailReversed(PXCache sender, SOLineSplit row)
		{
			return SelectDetailReversed(sender, row, true);
		}

		protected virtual object[] SelectDetailReversed(PXCache sender, SOLineSplit row, bool ExcludeCompleted = true)
		{
			object[] ret = SelectDetail(sender, row, ExcludeCompleted);

			Array.Sort<object>(ret, new Comparison<object>(delegate (object a, object b)
			{
				object aIsAllocated = ((SOLineSplit)a).Completed == true ? 0 : ((SOLineSplit)a).IsAllocated == true ? 1 : 2;
				object bIsAllocated = ((SOLineSplit)b).Completed == true ? 0 : ((SOLineSplit)b).IsAllocated == true ? 1 : 2;

				int res = -((IComparable)aIsAllocated).CompareTo(bIsAllocated);

				if (res != 0)
				{
					return res;
				}

				object aSplitLineNbr = ((SOLineSplit)a).SplitLineNbr;
				object bSplitLineNbr = ((SOLineSplit)b).SplitLineNbr;

				return -((IComparable)aSplitLineNbr).CompareTo(bSplitLineNbr);
			}));

			return ret;
		}

		public virtual bool IsLotSerialsAllowedForDropShipLine(PXCache masterCache, SOLine row)
		{
			return row.Operation == SOOperation.Receipt
				&& ((INLotSerClass)ReadInventoryItem(masterCache, row.InventoryID)).RequiredForDropship == true;
		}

		public virtual bool HasMultipleSplitsOrAllocation(PXCache detailCache, SOLine row)
		{
			var details = SelectAllDetails(detailCache, row);
			return details.Length > 1 || details.Length == 1 && ((SOLineSplit)details[0]).IsAllocated == true;
		}

		public virtual void UncompleteSchedules(PXCache sender, SOLine Row)
		{
			DetailCounters.Remove(Row);

			decimal? UnshippedQty = Row.BaseOpenQty;

			foreach (object detail in SelectDetailReversed(DetailCache, Row, false))
			{
				if (((SOLineSplit)detail).ShipmentNbr == null)
				{
					UnshippedQty -= ((SOLineSplit)detail).BaseQty;

					SOLineSplit newdetail = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)detail);
					newdetail.Completed = false;

					DetailCache.Update(newdetail);
				}
			}

			if (IsDropShipNotLegacy(Row))
			{
				decimal shippedQty = UncompleteDSSchedules(DetailCache, Row);
				UnshippedQty -= shippedQty;
			}

			IssueAvailable(sender, Row, (decimal)UnshippedQty, true);
		}

		public virtual decimal UncompleteDSSchedules(PXCache detailCache, SOLine row)
		{
			decimal shippedQty = 0;
			IEnumerable<SOLineSplit> splits = SelectAllDetails(detailCache, row).Cast<SOLineSplit>()
				.Where(s => s.Completed == true && s.PONbr != null);

			foreach (SOLineSplit split in splits)
			{
				if (split.BaseQty == null)
					continue;

				shippedQty += split.BaseQty.Value;

				SOLineSplit newdetail = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)split);
				newdetail.Completed = false;

				detailCache.Update(newdetail);
			}

			return shippedQty;
		}

		public virtual void CompleteSchedules(PXCache sender, SOLine Row)
		{
			DetailCounters.Remove(Row);

			string LastShipmentNbr = null;
			decimal? LastUnshippedQty = 0m;
			foreach (object detail in SelectDetailReversed(DetailCache, Row, false))
			{
				if (LastShipmentNbr == null && ((SOLineSplit)detail).ShipmentNbr != null)
				{
					LastShipmentNbr = ((SOLineSplit)detail).ShipmentNbr;
				}

				if (LastShipmentNbr != null && ((SOLineSplit)detail).ShipmentNbr == LastShipmentNbr)
				{
					LastUnshippedQty += ((SOLineSplit)detail).BaseOpenQty;
				}
			}

			TruncateSchedules(sender, Row, (decimal)LastUnshippedQty);

			foreach (object detail in SelectDetailReversed(DetailCache, Row))
			{
				SOLineSplit newdetail = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)detail);
				newdetail.Completed = true;

				DetailCache.Update(newdetail);
			}
		}

		public virtual void TruncateSchedules(PXCache sender, SOLine Row, decimal BaseQty)
		{
			DetailCounters.Remove(Row);
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);

			foreach (object detail in SelectDetailReversed(DetailCache, Row))
			{
				if (BaseQty >= ((ILSDetail)detail).BaseQty)
				{
					BaseQty -= (decimal)((ILSDetail)detail).BaseQty;
					DetailCache.Delete(detail);
				}
				else
				{
					SOLineSplit newdetail = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)detail);
					newdetail.BaseQty -= BaseQty;
					SetDetailQtyWithMaster(sender, newdetail, Row);

					DetailCache.Update(newdetail);
					break;
				}
			}
		}

		protected virtual void IssueAvailable(PXCache sender, SOLine Row)
		{
			IssueAvailable(sender, Row, Row.BaseOpenQty);
		}

		protected override void _Master_RowInserted(PXCache sender, PXRowInsertedEventArgs<SOLine> e)
		{
			SOLine row = e.Row as SOLine;
			if (row == null) return;

			if (IsSplitRequired(sender, row))
			{
				base._Master_RowInserted(sender, e);
			}
			else
			{
				sender.SetValue<SOLine.locationID>(e.Row, null);
				sender.SetValue<SOLine.lotSerialNbr>(e.Row, null);
				sender.SetValue<SOLine.expireDate>(e.Row, null);

				if (IsAllocationEntryEnabled && e.Row != null && e.Row.BaseOpenQty != 0m)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, e.Row.InventoryID);

					//if (e.Row.InvtMult == -1 && item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
					if (item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
					{
						IssueAvailable(sender, e.Row);

					}
				}
				AvailabilityCheck(sender, e.Row);
			}
		}

		protected override void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			try
			{
				using (ResolveNotDecimalUnitErrorRedirectorScope<SOLineSplit.qty>(e.Row))
					base.Master_RowUpdated(sender, e);
			}
			catch (PXUnitConversionException ex)
			{
				if (!PXUIFieldAttribute.GetErrors(sender, e.Row, PXErrorLevel.Error).Keys.Any(a => string.Compare(a, typeof(SOLine.uOM).Name, StringComparison.InvariantCultureIgnoreCase) == 0))
					sender.RaiseExceptionHandling<SOLine.uOM>(e.Row, null, ex);
			}
		}

		protected override void _Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs<SOLine> e)
		{
			SOLine row = e.Row as SOLine;
			if (row == null) return;

			if (IsSplitRequired(sender, row, out InventoryItem ii)) //check condition
			{
				base._Master_RowUpdated(sender, e);

				if (ii != null && (ii.KitItem == true || ii.StkItem == true))
				{
					AvailabilityCheck(sender, (SOLine)e.Row);
				}
			}
			else
			{
				sender.SetValue<SOLine.locationID>(e.Row, null);
				sender.SetValue<SOLine.lotSerialNbr>(e.Row, null);
				sender.SetValue<SOLine.expireDate>(e.Row, null);

				if (IsAllocationEntryEnabled)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, e.Row.InventoryID);

					if (e.OldRow != null && (e.OldRow.InventoryID != e.Row.InventoryID || e.OldRow.SiteID != e.Row.SiteID || e.OldRow.SubItemID != e.Row.SubItemID || e.OldRow.InvtMult != e.Row.InvtMult || e.OldRow.UOM != e.Row.UOM || e.OldRow.ProjectID != e.Row.ProjectID || e.OldRow.TaskID != e.Row.TaskID))
					{
						RaiseRowDeleted(sender, e.OldRow);
						RaiseRowInserted(sender, e.Row);
					}
					//else if (e.Row.InvtMult == -1 && item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
					else if (item != null && (e.Row.LineType == SOLineType.Inventory || e.Row.LineType == SOLineType.NonInventory))
					{
						// prevent setting null to quantity from mobile app
						if (this._Graph.IsMobile && e.Row.OrderQty == null)
						{
							e.Row.OrderQty = e.OldRow.OrderQty;
						}

						//ConfirmShipment(), CorrectShipment() use SuppressedMode and never end up here.
						//OpenQty is calculated via formulae, ExternalCall is used to eliminate duplicating formula arguments here
						//direct OrderQty for AddItem()
						if (e.Row.OrderQty != e.OldRow.OrderQty || e.Row.Completed != e.OldRow.Completed)
						{
							e.Row.BaseOpenQty = INUnitAttribute.ConvertToBase(sender, e.Row.InventoryID, e.Row.UOM, (decimal)e.Row.OpenQty, e.Row.BaseOpenQty, INPrecision.QUANTITY);

							//mimic behavior of Shipment Confirmation where at least one schedule will always be present for processed line
							//but additional schedules will never be created and thus should be truncated when ShippedQty > 0
							if (e.Row.Completed == true && e.OldRow.Completed == false)
							{
								CompleteSchedules(sender, e.Row);
								UpdateParent(sender, e.Row);
							}
							else if (e.Row.Completed == false && e.OldRow.Completed == true)
							{
								UncompleteSchedules(sender, e.Row);
								UpdateParent(sender, e.Row);
							}
							else if (e.Row.BaseOpenQty > e.OldRow.BaseOpenQty)
							{
								IssueAvailable(sender, e.Row, (decimal)e.Row.BaseOpenQty - (decimal)e.OldRow.BaseOpenQty);
								UpdateParent(sender, e.Row);
							}
							else if (e.Row.BaseOpenQty < e.OldRow.BaseOpenQty)
							{
								TruncateSchedules(sender, e.Row, (decimal)e.OldRow.BaseOpenQty - (decimal)e.Row.BaseOpenQty);
								UpdateParent(sender, e.Row);
							}
						}

						if (!sender.ObjectsEqual<SOLine.pOCreate, SOLine.pOSource, SOLine.vendorID, SOLine.pOSiteID>(e.Row, e.OldRow))
						{
							foreach (object detail in SelectDetail(DetailCache, row))
							{
								SOLineSplit split = (SOLineSplit)DetailCache.CreateCopy(detail);
								if (split.IsAllocated == false && split.Completed == false && split.PONbr == null)
								{
									split.POCreate = row.POCreate;
									split.POSource = row.POSource;
									split.VendorID = row.VendorID;
									split.POSiteID = row.POSiteID;

									DetailCache.Update(split);
								}
							}
						}

						if (!sender.ObjectsEqual<SOLine.shipDate>(e.Row, e.OldRow) ||
							(!sender.ObjectsEqual<SOLine.shipComplete>(e.Row, e.OldRow) && ((SOLine)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed))
						{
							foreach (object detail in SelectDetail(DetailCache, row))
							{
								SOLineSplit split = detail as SOLineSplit;
								split.ShipDate = row.ShipDate;

								DetailCache.Update(split);
							}
						}
					}
				}
				else
				{
					if (e.OldRow != null && e.OldRow.InventoryID != e.Row.InventoryID)
					{
						RaiseRowDeleted(sender, e.OldRow);
					}
				}

				AvailabilityCheck(sender, (SOLine)e.Row);
			}
		}

		protected virtual bool IsSplitRequired(PXCache sender, SOLine row)
			=> IsSplitRequired(sender, row, out InventoryItem item);

		protected virtual bool IsSplitRequired(PXCache sender, SOLine row, out InventoryItem item)
		{
			if (row == null)
			{
				item = null;
				return false;
			}

			bool skipSplitCreating = false;
			item = InventoryItem.PK.Find(sender.Graph, row.InventoryID);

			if (IsLocationEnabled && item != null && item.StkItem == false && item.KitItem == false && item.NonStockShip == false)
			{
				skipSplitCreating = true;
			}

			if (item != null && item.StkItem == false && item.KitItem == true && row.Behavior != SOBehavior.CM && row.Behavior != SOBehavior.IN)
			{
				skipSplitCreating = true;
			}

			return !skipSplitCreating && (IsLocationEnabled || (IsLotSerialRequired && row.POCreate != true && IsLotSerialItem(sender, row)));
		}

		protected virtual bool SchedulesEqual(SOLineSplit a, SOLineSplit b)
			=> SchedulesEqual(a, b, false);

		protected virtual bool SchedulesEqual(SOLineSplit a, SOLineSplit b, bool ignorePOLink)
		{
			if (a != null && b != null)
			{
				return (a.InventoryID == b.InventoryID &&
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
								a.SOSplitLineNbr == b.SOSplitLineNbr &&
								a.AMProdCreate == b.AMProdCreate);
			}
			else
			{
				return (a != null);
			}
		}

		protected override void Detail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			if (IsLSEntryEnabled)
			{
				if (e.ExternalCall)
				{
					if (((SOLineSplit)e.Row).LineType != SOLineType.Inventory)
					{
						throw new PXSetPropertyException(ErrorMessages.CantInsertRecord);
					}
				}

				base.Detail_RowInserting(sender, e);

				if (e.Row != null && !IsLocationEnabled && ((SOLineSplit)e.Row).LocationID != null)
				{
					((SOLineSplit)e.Row).LocationID = null;
				}
			}
			else if (IsAllocationEntryEnabled)
			{
				SOLineSplit a = (SOLineSplit)e.Row;

				if (!e.ExternalCall && _Operation == PXDBOperation.Update)
				{
					bool isDropShipNotLegacy = IsDropShipNotLegacy(a, sender);
					if (isDropShipNotLegacy)
					{
						var linksExt = sender.Graph.GetExtension<PurchaseSupplyBaseExt>();
						linksExt.FillInsertingSchedule(sender, (SOLineSplit)e.Row);
					}

					foreach (object item in SelectDetail(sender, (SOLineSplit)e.Row))
					{
						SOLineSplit detailitem = (SOLineSplit)item;

						if (SchedulesEqual((SOLineSplit)e.Row, detailitem, ignorePOLink: isDropShipNotLegacy))
						{
							object old_item = PXCache<SOLineSplit>.CreateCopy(detailitem);
							detailitem.BaseQty += ((SOLineSplit)e.Row).BaseQty;
							SetDetailQtyWithMaster(sender, detailitem, null);

							detailitem.BaseUnreceivedQty += ((SOLineSplit)e.Row).BaseQty;
							SetUnreceivedQty(sender, detailitem);

							sender.Current = detailitem;
							sender.RaiseRowUpdated(detailitem, old_item);
							sender.MarkUpdated(detailitem);
							PXDBQuantityAttribute.VerifyForDecimal(sender, detailitem);
							e.Cancel = true;
							break;
						}
					}
				}

				if (((SOLineSplit)e.Row).InventoryID == null || string.IsNullOrEmpty(((SOLineSplit)e.Row).UOM))
				{
					e.Cancel = true;
				}

				if (!e.Cancel)
				{
				}
			}
		}

		protected virtual void SetUnreceivedQty(PXCache sender, SOLineSplit detail)
		{
			SOLine master = SelectMaster(sender, detail);
			if (detail.InventoryID == master?.InventoryID && detail.BaseUnreceivedQty == master?.BaseQty
				&& string.Equals(detail.UOM, master?.UOM, StringComparison.OrdinalIgnoreCase))
			{
				detail.UnreceivedQty = master.Qty;
				return;
			}
			detail.UnreceivedQty = INUnitAttribute.ConvertFromBase(sender, detail.InventoryID, detail.UOM, (decimal)detail.BaseUnreceivedQty, INPrecision.QUANTITY);
		}

		public virtual bool IsDropShipNotLegacy(SOLineSplit split, PXCache sender)
		{
			SOLine soLine = split != null ? PXParentAttribute.SelectParent<SOLine>(sender, split) : null;
			return IsDropShipNotLegacy(soLine);
		}

		public virtual bool IsDropShipNotLegacy(SOLine line)
		{
			return line != null && line.POCreate == true && line.POSource == INReplenishmentSource.DropShipToOrder && line.IsLegacyDropShip != true;
		}

		protected virtual bool Allocated_Updated(PXCache sender, EventArgs e)
		{
			SOLineSplit split = (SOLineSplit)(e is PXRowUpdatedEventArgs ? ((PXRowUpdatedEventArgs)e).Row : ((PXRowInsertedEventArgs)e).Row);
			SiteStatus accum = new SiteStatus();
			accum.InventoryID = split.InventoryID;
			accum.SiteID = split.SiteID;
			accum.SubItemID = split.SubItemID;

			accum = (SiteStatus)sender.Graph.Caches[typeof(SiteStatus)].Insert(accum);
			accum = PXCache<SiteStatus>.CreateCopy(accum);

			INSiteStatus stat = INSiteStatus.PK.Find(sender.Graph, split.InventoryID, split.SubItemID, split.SiteID);
			if (stat != null)
			{
				accum.QtyAvail += stat.QtyAvail;
				accum.QtyHardAvail += stat.QtyHardAvail;
			}

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, split.InventoryID);
			if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult))
			{
				if (split.LotSerialNbr != null)
				{
					LotSerialNbr_Updated(sender, e);
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
						SetDetailQtyWithMaster(sender, split, null);
						sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.qty)), split, split.Qty);
					}
					else
					{
						split.IsAllocated = false;
						sender.RaiseExceptionHandling<SOLineSplit.isAllocated>(split, true, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
					}

					sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), split, copy.IsAllocated);

					using (SuppressedModeScope(true))
					{
						sender.RaiseRowUpdated(split, copy);

						if (split.IsAllocated == true)
						{
							InsertAllocationRemainder(sender, copy, -accum.QtyHardAvail);
						}
					}
					RefreshView(sender);

					return true;
				}
			}
			return false;
		}

		protected virtual bool LotSerialNbr_Updated(PXCache sender, EventArgs e)
		{

			SOLineSplit split = (SOLineSplit)(e is PXRowUpdatedEventArgs ? ((PXRowUpdatedEventArgs)e).Row : ((PXRowInsertedEventArgs)e).Row);
			SOLineSplit oldsplit = (SOLineSplit)(e is PXRowUpdatedEventArgs ? ((PXRowUpdatedEventArgs)e).OldRow : ((PXRowInsertedEventArgs)e).Row);

			SiteLotSerial accum = new SiteLotSerial();

			accum.InventoryID = split.InventoryID;
			accum.SiteID = split.SiteID;
			accum.LotSerialNbr = split.LotSerialNbr;

			accum = (SiteLotSerial)sender.Graph.Caches[typeof(SiteLotSerial)].Insert(accum);
			accum = PXCache<SiteLotSerial>.CreateCopy(accum);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, split.InventoryID);

			INSiteLotSerial siteLotSerial = PXSelectReadonly<INSiteLotSerial, Where<INSiteLotSerial.inventoryID, Equal<Required<INSiteLotSerial.inventoryID>>, And<INSiteLotSerial.siteID, Equal<Required<INSiteLotSerial.siteID>>,
				And<INSiteLotSerial.lotSerialNbr, Equal<Required<INSiteLotSerial.lotSerialNbr>>>>>>.Select(sender.Graph, split.InventoryID, split.SiteID, split.LotSerialNbr);

			if (siteLotSerial != null)
			{
				accum.QtyAvail += siteLotSerial.QtyAvail;
				accum.QtyHardAvail += siteLotSerial.QtyHardAvail;
			}

			Lazy<bool> externalCall = Lazy.By(() =>
			{
				bool extCall = false;
				if (e is PXRowUpdatedEventArgs) extCall = ((PXRowUpdatedEventArgs)e).ExternalCall;
				if (e is PXRowInsertedEventArgs) extCall = ((PXRowInsertedEventArgs)e).ExternalCall;
				return extCall;
			});

			//Serial-numbered items
			if (INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null)
			{
				SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
				if (siteLotSerial != null && siteLotSerial.QtyAvail > 0 && siteLotSerial.QtyHardAvail > 0)
				{
					if (split.Operation != SOOperation.Receipt)
					{
						split.BaseQty = 1;
						SetDetailQtyWithMaster(sender, split, null);
						split.IsAllocated = true;
					}
					else
					{
						split.IsAllocated = false;
						sender.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefixNLA(IN.Messages.SerialNumberAlreadyReceived, ((InventoryItem)item).InventoryCD, split.LotSerialNbr)));
					}
				}
				else
				{

					if (split.Operation != SOOperation.Receipt)
					{
						if (externalCall.Value)
						{
							split.IsAllocated = false;
							split.LotSerialNbr = null;
							sender.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
							if (split.IsAllocated == true)
							{
								return false;
							}
						}
					}
					else
					{
						split.BaseQty = 1;
						SetDetailQtyWithMaster(sender, split, null);
					}
				}

				sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), split, copy.IsAllocated);

				bool needNewSplit = (copy.BaseQty > 1m);
				using (needNewSplit ? SuppressedModeScope(true) : null)
				{
					sender.RaiseRowUpdated(split, copy);

					if (needNewSplit)
					{
						if (split.IsAllocated == true || (split.IsAllocated != true && split.Operation == SOOperation.Receipt))
						{
							copy.SplitLineNbr = null;
							copy.PlanID = null;
							copy.IsAllocated = false;
							copy.LotSerialNbr = null;
							copy.BaseQty -= 1;
							SetDetailQtyWithMaster(sender, copy, null);
							sender.Insert(copy);

							if (IsLotSerialRequired)
							{
								//because we are now using SuppressedMode need to adjust Unassigned Quantity
								SOLine parent = (SOLine)PXParentAttribute.SelectParent(sender, copy, typeof(SOLine));
								if (parent != null && IsLotSerialItem(sender, parent))
								{
									parent.UnassignedQty += copy.BaseQty;
									sender.Graph.Caches[typeof(SOLine)].MarkUpdated(parent);
								}
							}
						}
					}
				}
				if (needNewSplit)
				{
					RefreshView(sender);
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
							if (externalCall.Value)
							{
								return NegativeInventoryError(sender, split);
							}
						}
						else
						{
							SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
							split.IsAllocated = true;
							sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), split, copy.IsAllocated);
							sender.RaiseRowUpdated(split, copy);
						}
						return true;
					}

					//Lot/Serial Nbr. selected on allocated line. Available Qty. verification procedure 
					if (split.IsAllocated == true)
					{
						SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);
						if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail >= 0m && split.Operation != SOOperation.Receipt)
						{
							SetDetailQtyWithMaster(sender, split, null);
						}
						else if (siteLotSerial != null && siteLotSerial.QtyOnHand > 0 && accum.QtyHardAvail < 0m && split.Operation != SOOperation.Receipt)
						{
							split.BaseQty += accum.QtyHardAvail;
							if (split.BaseQty <= 0m)
							{
								if (NegativeInventoryError(sender, split)) return false;
							}
							SetDetailQtyWithMaster(sender, split, null);
						}
						else if (siteLotSerial == null || (siteLotSerial.QtyOnHand <= 0m && split.Operation != SOOperation.Receipt))
						{
							if (NegativeInventoryError(sender, split)) return false;
						}

						INItemPlanIDAttribute.RaiseRowUpdated(sender, split, copy);

						if ((copy.BaseQty - split.BaseQty) > 0m && split.IsAllocated == true)
						{
							_InternallCall = true;
							try
							{
								copy.LotSerialNbr = null;
								copy = InsertAllocationRemainder(sender, copy, copy.BaseQty - split.BaseQty);
								if (copy.LotSerialNbr != null && copy.IsAllocated != true)
								{
									sender.SetValue<SOLineSplit.lotSerialNbr>(copy, null);
								}
							}
							finally
							{
								_InternallCall = false;
							}
						}
						RefreshView(sender);

						return true;
					}
				}
			}
			return false;
		}

		protected virtual bool NegativeInventoryError(PXCache sender, SOLineSplit split)
		{
			split.IsAllocated = false;
			split.LotSerialNbr = null;
			sender.RaiseExceptionHandling<SOLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
			if (split.IsAllocated == true)
			{
				return true;
			}
			return false;
		}

		private void RefreshView(PXCache sender)
		{
			foreach (KeyValuePair<string, PXView> pair in sender.Graph.Views)
			{
				PXView view = pair.Value;
				if (view.IsReadOnly == false && view.GetItemType() == sender.GetItemType())
				{
					view.RequestRefresh();
				}
			}
		}

		protected override void Detail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			base.Detail_RowInserted(sender, e);

			if ((_InternallCall == false || !string.IsNullOrEmpty(((SOLineSplit)e.Row).LotSerialNbr) && ((SOLineSplit)e.Row).IsAllocated != true) && IsAllocationEntryEnabled)
			{
				if (((SOLineSplit)e.Row).IsAllocated == true || (!string.IsNullOrEmpty(((SOLineSplit)e.Row).LotSerialNbr) && ((SOLineSplit)e.Row).IsAllocated != true))
				{
					Allocated_Updated(sender, e);

					sender.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
					AvailabilityCheck(sender, (SOLineSplit)e.Row);
				}
			}
		}

		protected override void Detail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			base.Detail_RowUpdated(sender, e);

			if (_InternallCall == false && IsAllocationEntryEnabled)
			{
				if (!sender.ObjectsEqual<SOLineSplit.isAllocated>(e.Row, e.OldRow) || !sender.ObjectsEqual<SOLineSplit.pOLineNbr>(e.Row, e.OldRow) && ((SOLineSplit)e.Row).POLineNbr == null && ((SOLineSplit)e.Row).IsAllocated == false)
				{
					if (((SOLineSplit)e.Row).IsAllocated == true)
					{
						Allocated_Updated(sender, e);

						sender.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
						AvailabilityCheck(sender, (SOLineSplit)e.Row);
					}
					else
					{
						//clear link to created transfer
						SOLineSplit row = (SOLineSplit)e.Row;
						row.ClearSOReferences();

						foreach (SOLineSplit s in this.SelectDetailReversed(sender, (SOLineSplit)e.Row))
						{
							if (s.SplitLineNbr != ((SOLineSplit)e.Row).SplitLineNbr &&
								SchedulesEqual(s, (SOLineSplit)e.Row))
							{
								((SOLineSplit)e.Row).Qty += s.Qty;
								((SOLineSplit)e.Row).BaseQty += s.BaseQty;

								((SOLineSplit)e.Row).UnreceivedQty += s.Qty;
								((SOLineSplit)e.Row).BaseUnreceivedQty += s.BaseQty;

								if (((SOLineSplit)e.Row).LotSerialNbr != null)
								{
									SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)e.Row);
									((SOLineSplit)e.Row).LotSerialNbr = null;
									//sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), s, copy.IsAllocated);
									sender.RaiseRowUpdated((SOLineSplit)e.Row, copy);
								}
								sender.SetStatus(s, sender.GetStatus(s) == PXEntryStatus.Inserted ? PXEntryStatus.InsertedDeleted : PXEntryStatus.Deleted);
								sender.ClearQueryCache();

								PXCache cache = sender.Graph.Caches[typeof(INItemPlan)];
								INItemPlan plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(sender.Graph, ((SOLineSplit)e.Row).PlanID);
								if (plan != null)
								{
									plan.PlanQty += s.BaseQty;
									if (cache.GetStatus(plan) != PXEntryStatus.Inserted)
									{
										cache.SetStatus(plan, PXEntryStatus.Updated);
									}
								}

								INItemPlan old_plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(sender.Graph, s.PlanID);
								if (old_plan != null)
								{
									cache.SetStatus(old_plan, cache.GetStatus(old_plan) == PXEntryStatus.Inserted ? PXEntryStatus.InsertedDeleted : PXEntryStatus.Deleted);
									cache.ClearQueryCacheObsolete();

								}
								RefreshView(sender);
							}
							else if (s.SplitLineNbr == ((SOLineSplit)e.Row).SplitLineNbr &&
								SchedulesEqual(s, (SOLineSplit)e.Row) && ((SOLineSplit)e.Row).LotSerialNbr != null)
							{
								SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy((SOLineSplit)e.Row);
								((SOLineSplit)e.Row).LotSerialNbr = null;
								sender.RaiseRowUpdated((SOLineSplit)e.Row, copy);
							}
						}
					}
				}

				if (!sender.ObjectsEqual<SOLineSplit.lotSerialNbr>(e.Row, e.OldRow))
				{
					if (((SOLineSplit)e.Row).LotSerialNbr != null)
					{
						LotSerialNbr_Updated(sender, e);

						sender.RaiseExceptionHandling<SOLineSplit.qty>(e.Row, null, null);
						AvailabilityCheck(sender, (SOLineSplit)e.Row); //???
					}
					else
					{
						foreach (SOLineSplit s in this.SelectDetailReversed(sender, (SOLineSplit)e.Row))
						{
							if (s.SplitLineNbr == ((SOLineSplit)e.Row).SplitLineNbr &&
								SchedulesEqual(s, (SOLineSplit)e.Row))
							{
								SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(s);
								((SOLineSplit)e.Row).IsAllocated = false;
								sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), (SOLineSplit)e.Row, ((SOLineSplit)e.Row).IsAllocated);
								//sender.RaiseFieldUpdated(sender.GetField(typeof(SOLineSplit.isAllocated)), s, copy.IsAllocated);
								sender.RaiseRowUpdated(s, copy);
							}
						}
					}
				}

			}
		}

		public override void Detail_UOM_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((ILSDetail)e.Row).InventoryID);

			if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
			{
				e.NewValue = ((InventoryItem)item).BaseUnit;
				e.Cancel = true;
			}
			else if (!IsAllocationEntryEnabled)
			{
				base.Detail_UOM_FieldDefaulting(sender, e);
			}
		}

		public override void Detail_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (!IsAllocationEntryEnabled)
			{
				base.Detail_Qty_FieldVerifying(sender, e);
			}
			else
			{
				VerifySNQuantity(sender, e, (ILSDetail)e.Row, typeof(SOLineSplit.qty).Name);
			}
		}



		public override SOLineSplit Convert(SOLine item)
		{
			using (InvtMultScope<SOLine> ms = new InvtMultScope<SOLine>(item))
			{
				SOLineSplit ret = (SOLineSplit)item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;

				return ret;
			}
		}

		public void ThrowFieldIsEmpty<Field>(PXCache sender, object data)
			where Field : IBqlField
		{
			if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
			{
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
			}
		}

		public virtual void SOLineSplit_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			SOLineSplit split = e.Row as SOLineSplit;

			if (split != null)
			{
				bool isLineTypeInventory = (split.LineType == SOLineType.Inventory);
				object val = sender.GetValueExt<SOLineSplit.isAllocated>(e.Row);
				bool isAllocated = split.IsAllocated == true || (bool?)PXFieldState.UnwrapValue(val) == true;
				bool isCompleted = split.Completed == true;
				bool isIssue = split.Operation == SOOperation.Issue;
				bool IsLinked = split.PONbr != null || split.SOOrderNbr != null && split.IsAllocated == true;
				bool isPOSchedule = split.POCreate == true || split.POCompleted == true;

				SOLine parent = (SOLine)PXParentAttribute.SelectParent(sender, split, typeof(SOLine));
				PXUIFieldAttribute.SetEnabled<SOLineSplit.subItemID>(sender, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.completed>(sender, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shippedQty>(sender, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shipmentNbr>(sender, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.isAllocated>(sender, e.Row, isLineTypeInventory && isIssue && !isCompleted && !isPOSchedule);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.siteID>(sender, e.Row, isLineTypeInventory && isAllocated && !IsLinked);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.qty>(sender, e.Row, !isCompleted && !IsLinked);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.shipDate>(sender, e.Row, !isCompleted && parent?.ShipComplete == SOShipComplete.BackOrderAllowed);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.pONbr>(sender, e.Row, false);
				PXUIFieldAttribute.SetEnabled<SOLineSplit.pOReceiptNbr>(sender, e.Row, false);
				sender.Adjust<INLotSerialNbrAttribute>(e.Row).For<SOLineSplit.lotSerialNbr>(a => a.ForceDisable = isCompleted || isPOSchedule);

				if (split.Completed == true)
				{
					PXUIFieldAttribute.SetEnabled(sender, e.Row, false);
				}
			}
		}

		public virtual void SOLineSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				bool RequireLocationAndSubItem = ((SOLineSplit)e.Row).RequireLocation == true && ((SOLineSplit)e.Row).IsStockItem == true && ((SOLineSplit)e.Row).BaseQty != 0m;

				PXDefaultAttribute.SetPersistingCheck<SOLineSplit.subItemID>(sender, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<SOLineSplit.locationID>(sender, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}
		}

		public virtual void SOLineSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOLine)];
			if (cache.Current != null && (e.Row == null || ((SOLine)cache.Current).LineNbr == ((SOLineSplit)e.Row).LineNbr && ((SOLineSplit)e.Row).IsStockItem == true))
			{
				e.NewValue = ((SOLine)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void SOLineSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOLine)];
			if (cache.Current != null && (e.Row == null || ((SOLine)cache.Current).LineNbr == ((SOLineSplit)e.Row).LineNbr && ((SOLineSplit)e.Row).IsStockItem == true))
			{
				e.NewValue = ((SOLine)cache.Current).LocationID;
				e.Cancel = (_InternallCall == true || e.NewValue != null || !IsLocationEnabled);
			}
		}

		public virtual void SOLineSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOLine)];
			if (cache.Current != null && (e.Row == null || ((SOLine)cache.Current).LineNbr == ((SOLineSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<SOLine> ms = new InvtMultScope<SOLine>((SOLine)cache.Current))
				{
					e.NewValue = ((SOLine)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		protected override IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster row, IStatus availability)
		{
			var soLine = row as SOLine;
			if (soLine != null
				&& availability is SiteStatus
				&& PXAccess.FeatureInstalled<FeaturesSet.relatedItems>()
				&& !IsAvailableQty(sender, row, availability))
			{
				var substitutableLine = soLine.GetExtension<SubstitutableSOLine>();
				if (substitutableLine?.SuggestRelatedItems == true
					&& substitutableLine.RelatedItemsRelation > 0)
				{
					var relatedItemsAttribute = sender.GetAttributesOfType<RelatedItemsAttribute>(row, nameof(substitutableLine.RelatedItems)).FirstOrDefault();
					if (relatedItemsAttribute != null)
					{
						var msgInfo = relatedItemsAttribute.QtyMessage(
							sender.GetStateExt<SOLine.inventoryID>(row),
							sender.GetStateExt<SOLine.subItemID>(row),
							sender.GetStateExt<SOLine.siteID>(row),
							(InventoryRelation.RelationType)substitutableLine.RelatedItemsRelation);
						if (msgInfo != null)
							return new[]
							{
								new PXExceptionInfo(PXErrorLevel.Warning, msgInfo.MessageFormat, msgInfo.MessageArguments)
							};
					}
				}
			}
			return base.GetAvailabilityCheckErrors(sender, row, availability);
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			if (row is SOLine)
			{
				var arguments = ei.MessageArguments;
				if (arguments.Length == 0)
					arguments = new object[]
					{
						sender.GetStateExt<SOLine.inventoryID>(row),
						sender.GetStateExt<SOLine.subItemID>(row),
						sender.GetStateExt<SOLine.siteID>(row),
						sender.GetStateExt<SOLine.locationID>(row),
						sender.GetValue<SOLine.lotSerialNbr>(row)
					};
				sender.RaiseExceptionHandling<SOLine.orderQty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, arguments));
			}
			else
			{
				sender.RaiseExceptionHandling<SOLineSplit.qty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<SOLineSplit.inventoryID>(row), sender.GetStateExt<SOLineSplit.subItemID>(row), sender.GetStateExt<SOLineSplit.siteID>(row), sender.GetStateExt<SOLineSplit.locationID>(row), sender.GetValue<SOLineSplit.lotSerialNbr>(row)));
			}
		}

		protected void RaiseMemoQtyExceptionHanding<Target>(PXCache sender, SOLine Row, ILSMaster Split, Exception e)
			where Target : class, ILSMaster
		{
			if (typeof(Target) == typeof(SOLine))
			{
				sender.Graph.Caches[typeof(SOLine)].RaiseExceptionHandling<SOLine.orderQty>(Row, Row.OrderQty, new PXSetPropertyException(e.Message, sender.GetValueExt<SOLine.inventoryID>(Row), sender.GetValueExt<SOLine.subItemID>(Row), sender.GetValueExt<SOLine.invoiceNbr>(Row), sender.GetValueExt<SOLine.lotSerialNbr>(Row)));
			}
			else
			{
				PXCache cache = sender.Graph.Caches[typeof(SOLineSplit)];
				cache.RaiseExceptionHandling<SOLineSplit.qty>(Split, ((SOLineSplit)Split).Qty, new PXSetPropertyException(e.Message, sender.GetValueExt<SOLine.inventoryID>(Row), cache.GetValueExt<SOLineSplit.subItemID>(Split), sender.GetValueExt<SOLine.invoiceNbr>(Row), cache.GetValueExt<SOLineSplit.lotSerialNbr>(Split)));
			}
		}

		public virtual bool MemoAvailabilityCheck(PXCache sender, SOLine Row)
			=> MemoAvailabilityCheck(sender, Row, false);

		public virtual bool MemoAvailabilityCheck(PXCache sender, SOLine Row, bool persisting)
		{
			if (Row.Operation == SOOperation.Issue)
				return true;

			var result = MemoAvailabilityCheckQty(sender, Row);
			if (result.Success != true)
			{
				var documents = result.ReturnRecords?
					.Select(x => x.DocumentNbr)
					.Where(nbr => nbr != Row.OrderNbr);

				RaiseException<SOLine.orderQty>(sender, Row, persisting,
					Messages.InvoiceCheck_DecreaseQty,
					sender.GetValueExt<SOLine.invoiceNbr>(Row),
					sender.GetValueExt<SOLine.inventoryID>(Row),
					documents == null ? string.Empty : string.Join(", ", documents));
			}

			return result.Success;
		}

		protected virtual void RaiseException<TField>(PXCache sender, object row, bool persisting, string errorMessage, params object[] args)
			where TField : IBqlField
		{
			object value = sender.GetValue<TField>(row);
			if (sender.RaiseExceptionHandling<TField>(row, value, new PXSetPropertyException(errorMessage, args)) && persisting)
				throw new PXRowPersistingException(typeof(TField).Name, value, errorMessage, args);
		}

		protected virtual ReturnedQtyResult MemoAvailabilityCheckQty(PXCache sender, SOLine row)
		{
			return MemoAvailabilityCheckQty(sender, row.InventoryID, row.InvoiceType, row.InvoiceNbr, row.InvoiceLineNbr, row.OrigOrderType, row.OrigOrderNbr, row.OrigLineNbr);
		}

		protected virtual bool MemoAvailabilityCheck<Target>(PXCache sender, SOLine Row, ILSMaster Split)
			where Target : class, ILSMaster
		{
			bool success = true;
			if (Row.InvoiceNbr != null)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Split.InventoryID);

				if (item != null && ((INLotSerClass)item).LotSerTrack != INLotSerTrack.NotNumbered && Split.SubItemID != null && string.IsNullOrEmpty(Split.LotSerialNbr) == false)
				{
					PXResult<INTran> orig_line = PXSelectJoinGroupBy<INTran,
					InnerJoin<INTranSplit,
						On<INTranSplit.FK.Tran>>,
					Where<INTran.sOOrderType, Equal<Optional<SOLine.origOrderType>>,
					And<INTran.sOOrderNbr, Equal<Optional<SOLine.origOrderNbr>>,
					And<INTran.sOOrderLineNbr, Equal<Optional<SOLine.origLineNbr>>,
					And<INTran.aRDocType, Equal<Optional<SOLine.invoiceType>>,
					And<INTran.aRRefNbr, Equal<Optional<SOLine.invoiceNbr>>,
					And<INTranSplit.inventoryID, Equal<Optional<SOLineSplit.inventoryID>>,
					And<INTranSplit.subItemID, Equal<Optional<SOLineSplit.subItemID>>,
					And<INTranSplit.lotSerialNbr, Equal<Optional<SOLineSplit.lotSerialNbr>>>>>>>>>>,
					Aggregate<GroupBy<INTranSplit.inventoryID, GroupBy<INTranSplit.subItemID, GroupBy<INTranSplit.lotSerialNbr, Sum<INTranSplit.baseQty>>>>>>.SelectSingleBound(sender.Graph, new object[] { Row, Split as SOLineSplit });

					PXResult<SOLine> memo_line = PXSelectJoinGroupBy<SOLine,
					InnerJoin<SOLineSplit, On<SOLineSplit.orderType, Equal<SOLine.orderType>, And<SOLineSplit.orderNbr, Equal<SOLine.orderNbr>, And<SOLineSplit.lineNbr, Equal<SOLine.lineNbr>>>>>,
					Where2<Where<SOLine.orderType, NotEqual<Optional<SOLine.orderType>>, Or<SOLine.orderNbr, NotEqual<Optional<SOLine.orderNbr>>>>,
					And<SOLine.origOrderType, Equal<Optional<SOLine.origOrderType>>,
					And<SOLine.origOrderNbr, Equal<Optional<SOLine.origOrderNbr>>,
					And<SOLine.origLineNbr, Equal<Optional<SOLine.origLineNbr>>,
					And<SOLine.invoiceType, Equal<Optional<SOLine.invoiceType>>,
					And<SOLine.invoiceNbr, Equal<Optional<SOLine.invoiceNbr>>,
					And<SOLine.operation, Equal<SOOperation.receipt>,
					And<SOLineSplit.inventoryID, Equal<Optional<SOLineSplit.inventoryID>>,
					And<SOLineSplit.subItemID, Equal<Optional<SOLineSplit.subItemID>>,
					And<SOLineSplit.lotSerialNbr, Equal<Optional<SOLineSplit.lotSerialNbr>>,
					And<Where<
						SOLine.baseBilledQty, Greater<decimal0>,
						Or2<Where<SOLine.baseShippedQty, Greater<decimal0>, And<SOLineSplit.baseShippedQty, Greater<decimal0>>>,
						Or<Where<SOLine.completed, NotEqual<True>, And<SOLineSplit.completed, NotEqual<True>>>>>>>>>>>>>>>>>,
					Aggregate<GroupBy<SOLineSplit.inventoryID, GroupBy<SOLineSplit.subItemID, GroupBy<SOLineSplit.lotSerialNbr, Sum<SOLineSplit.baseQty, Sum<SOLineSplit.baseShippedQty>>>>>>>.SelectSingleBound(sender.Graph, new object[] { Row, Split as SOLineSplit });

					if (orig_line == null)
					{
						if (Split is SOLineSplit && string.IsNullOrEmpty(((SOLineSplit)Split).AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(((SOLineSplit)Split).AssignedNbr, ((SOLineSplit)Split).LotSerialNbr))
						{
							((SOLineSplit)Split).AssignedNbr = null;
							((SOLineSplit)Split).LotSerialNbr = null;
						}
						else
						{
							RaiseMemoQtyExceptionHanding<Target>(sender, Row, Split, new PXSetPropertyException(Messages.InvoiceCheck_LotSerialInvalid));
							success = false;
						}
						return success;
					}

					decimal? QtyInvoicedLotBase = ((INTranSplit)(PXResult<INTran, INTranSplit>)orig_line).BaseQty;

					if (memo_line != null)
					{
						if (((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
						{
							RaiseMemoQtyExceptionHanding<Target>(sender, Row, Split, new PXSetPropertyException(Messages.InvoiceCheck_SerialAlreadyReturned));
							success = false;
						}
						else
						{
							decimal returnedQty = ((SOLineSplit)(PXResult<SOLine, SOLineSplit>)memo_line).BaseShippedQty ?? 0m;
							if (returnedQty == 0)
								returnedQty = ((SOLineSplit)(PXResult<SOLine, SOLineSplit>)memo_line).BaseQty ?? 0m;

							QtyInvoicedLotBase -= returnedQty;
						}
					}

					if (Split is SOLine)
					{
						QtyInvoicedLotBase -= Split.BaseQty;
					}
					else
					{
						foreach (SOLineSplit split in PXParentAttribute.SelectSiblings(sender.Graph.Caches[typeof(SOLineSplit)], Split, typeof(SOLine)))
						{
							if (object.Equals(split.SubItemID, Split.SubItemID) && object.Equals(split.LotSerialNbr, Split.LotSerialNbr))
							{
								QtyInvoicedLotBase -= split.BaseQty;
							}
						}
					}

					if (QtyInvoicedLotBase < 0m)
					{
						RaiseMemoQtyExceptionHanding<Target>(sender, Row, Split, new PXSetPropertyException(Messages.InvoiceCheck_QtyLotSerialNegative));
						success = false;
					}
				}
			}
			return success;
		}

		public override void AvailabilityCheck(PXCache sender, ILSMaster Row)
		{
			base.AvailabilityCheck(sender, Row);

			if (Row is SOLine)
			{
				MemoAvailabilityCheck(sender, (SOLine)Row);

				SOLineSplit copy = Convert(Row as SOLine);

				if (string.IsNullOrEmpty(Row.LotSerialNbr) == false)
				{
					DefaultLotSerialNbr(sender.Graph.Caches[typeof(SOLineSplit)], copy);
				}

				MemoAvailabilityCheck<SOLine>(sender, (SOLine)Row, copy);

				if (copy.LotSerialNbr == null)
				{
					Row.LotSerialNbr = null;
				}
			}
			else
			{
				object parent = PXParentAttribute.SelectParent(sender, Row, typeof(SOLine));
				MemoAvailabilityCheck(sender.Graph.Caches[typeof(SOLine)], (SOLine)parent);
				MemoAvailabilityCheck<SOLineSplit>(sender.Graph.Caches[typeof(SOLine)], (SOLine)parent, Row);
			}
		}

		public override void DefaultLotSerialNbr(PXCache sender, SOLineSplit row)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, row.InventoryID);

			if (item != null)
			{
				if (IsAllocationEntryEnabled && ((INLotSerClass)item).LotSerAssign == INLotSerAssign.WhenUsed)
					return;
				else
					base.DefaultLotSerialNbr(sender, row);
			}
		}
		#endregion

		protected override PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(PXCache sender, SOLine Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			return new PXSelectJoin<INLotSerialStatus,
			InnerJoin<INLocation,
				On<INLotSerialStatus.FK.Location>,
			InnerJoin<INSiteLotSerial, On<INSiteLotSerial.inventoryID, Equal<INLotSerialStatus.inventoryID>,
					And<INSiteLotSerial.siteID, Equal<INLotSerialStatus.siteID>,
					And<INSiteLotSerial.lotSerialNbr, Equal<INLotSerialStatus.lotSerialNbr>>>>>>,
			Where<INLotSerialStatus.inventoryID, Equal<Current<INLotSerialStatus.inventoryID>>,
			And<INLotSerialStatus.siteID, Equal<Current<INLotSerialStatus.siteID>>,
			And<INLotSerialStatus.qtyOnHand, Greater<decimal0>>>>>(sender.Graph);
		}

		protected override decimal? GetSerialStatusAvailableQty2(IStatus lsmaster, IStatus accumavail, PXResult data)
		{
			var iNSiteLotSerial = (INSiteLotSerial)data[typeof(INSiteLotSerial)];

			var siteaccumavail = (SiteLotSerial)_Graph.Caches[typeof(SiteLotSerial)].Locate(new SiteLotSerial()
			{
				InventoryID = iNSiteLotSerial.InventoryID,
				SiteID = iNSiteLotSerial.SiteID,
				LotSerialNbr = iNSiteLotSerial.LotSerialNbr,
			});

			decimal? siteAvailableQty = iNSiteLotSerial.QtyAvail + (siteaccumavail?.QtyAvail ?? 0m);
			decimal? availableQty = base.GetSerialStatusAvailableQty2(lsmaster, accumavail, data);

			return Math.Min(availableQty ?? 0m, siteAvailableQty ?? 0m);
		}

		protected override decimal? GetSerialStatusQtyOnHand2(IStatus lsmaster, PXResult data)
		{
			var iNSiteLotSerial = (INSiteLotSerial)data[typeof(INSiteLotSerial)];

			var siteaccumavail = (SiteLotSerial)_Graph.Caches[typeof(SiteLotSerial)].Locate(new SiteLotSerial()
			{
				InventoryID = iNSiteLotSerial.InventoryID,
				SiteID = iNSiteLotSerial.SiteID,
				LotSerialNbr = iNSiteLotSerial.LotSerialNbr,
			});

			decimal? qtyHardAvail = iNSiteLotSerial.QtyHardAvail + (siteaccumavail?.QtyHardAvail ?? 0m);
			decimal? qtyOnHand = base.GetSerialStatusQtyOnHand2(lsmaster, data);

			return Math.Min(qtyOnHand ?? 0m, qtyHardAvail ?? 0m);
		}

		protected override PXSelectBase<PM.PMLotSerialStatus> GetSerialStatusCmdBaseProject(PXCache sender, SOLine Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (!IsLocationEnabled && IsLotSerialRequired)
			{
				return new PXSelectJoin<PM.PMLotSerialStatus,
				InnerJoin<INLocation,
					On<PM.PMLotSerialStatus.FK.Location>,
				InnerJoin<INSiteLotSerial, On<INSiteLotSerial.inventoryID, Equal<PM.PMLotSerialStatus.inventoryID>,
						And<INSiteLotSerial.siteID, Equal<PM.PMLotSerialStatus.siteID>,
						And<INSiteLotSerial.lotSerialNbr, Equal<PM.PMLotSerialStatus.lotSerialNbr>>>>>>,
				Where<PM.PMLotSerialStatus.inventoryID, Equal<Current<PM.PMLotSerialStatus.inventoryID>>,
					And<PM.PMLotSerialStatus.siteID, Equal<Current<PM.PMLotSerialStatus.siteID>>,
					And<PM.PMLotSerialStatus.projectID, Equal<Current<PM.PMLotSerialStatus.projectID>>,
					And<PM.PMLotSerialStatus.taskID, Equal<Current<PM.PMLotSerialStatus.taskID>>,
					And<PM.PMLotSerialStatus.qtyOnHand, Greater<decimal0>,
					And<INSiteLotSerial.qtyHardAvail, Greater<decimal0>>>>>>>>(sender.Graph);
			}
			else
			{
				return base.GetSerialStatusCmdBaseProject(sender, Row, item);
			}
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, SOLine Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID, Equal<Current<INLotSerialStatus.subItemID>>>>();
			}
			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID, Equal<Current<INLotSerialStatus.locationID>>>>();
			}
			else
			{
				switch (Row.TranType)
				{
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid, Equal<boolTrue>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid, Equal<boolTrue>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr, Equal<Current<INLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		protected override void AppendSerialStatusCmdWhereProject(PXSelectBase<PM.PMLotSerialStatus> cmd, SOLine Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
			{
				cmd.WhereAnd<Where<PM.PMLotSerialStatus.subItemID, Equal<Current<PM.PMLotSerialStatus.subItemID>>>>();
			}
			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<PM.PMLotSerialStatus.locationID, Equal<Current<PM.PMLotSerialStatus.locationID>>>>();
			}
			else
			{
				switch (Row.TranType)
				{
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid, Equal<boolTrue>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid, Equal<boolTrue>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<PM.PMLotSerialStatus.lotSerialNbr, Equal<Current<PM.PMLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		public virtual bool IsLotSerialItem(PXCache sender, ILSMaster line)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, line.InventoryID);

			if (item == null)
				return false;

			return INLotSerialNbrAttribute.IsTrack(item, line.TranType, line.InvtMult);
		}

		protected virtual SOLineSplit InsertAllocationRemainder(PXCache sender, SOLineSplit copy, decimal? baseQty)
		{
			copy.SplitLineNbr = null;
			copy.PlanID = null;
			copy.IsAllocated = false;
			copy.BaseQty = baseQty;
			SetDetailQtyWithMaster(sender, copy, null);
			copy.OpenQty = null;
			copy.BaseOpenQty = null;
			copy.UnreceivedQty = null;
			copy.BaseUnreceivedQty = null;
			copy = (SOLineSplit)sender.Insert(copy);
			return copy;
		}
	}
}
