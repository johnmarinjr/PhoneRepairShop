using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Data;

using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public class LSSOShipLine : LSSelectSOBase<SOShipLine, SOShipLineSplit,
		Where<SOShipLineSplit.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>>
	{
		#region State

		public bool IsLocationEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(this._Graph);
				if (ordertype == null || (ordertype.RequireShipping == false && ordertype.RequireLocation == true && ordertype.INDocType != INTranType.NoUpdate)) return true;
				else return false;
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
		#endregion
		#region Ctor
		public LSSOShipLine(PXGraph graph)
			: base(graph)
		{
			MasterQtyField = typeof(SOShipLine.shippedQty);
			graph.FieldDefaulting.AddHandler<SOShipLineSplit.subItemID>(SOShipLineSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<SOShipLineSplit.locationID>(SOShipLineSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<SOShipLineSplit.invtMult>(SOShipLineSplit_InvtMult_FieldDefaulting);
			graph.RowPersisting.AddHandler<SOShipLine>(SOShipLine_RowPersisting);
			graph.RowPersisting.AddHandler<SOShipLineSplit>(SOShipLineSplit_RowPersisting);

			graph.RowUpdated.AddHandler<SOShipment>(SOShipment_RowUpdated);
			graph.RowUpdating.AddHandler<SOShipLineSplit>(Detail_RowUpdating);
			graph.RowSelected.AddHandler<SOShipLine>(SOShipLine_RowSelected);
		}

		#endregion
		public override IEnumerable BinLotSerial(PXAdapter adapter)
		{
			View.AskExt(true);
			return adapter.Get();
		}

		#region Implementation

		private void SOShipLine_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			var row = (SOShipLine)e.Row;
			if (row == null)
				return;
			bool unassignederror = false;
			if (row.InventoryID != null)
			{
				if (Math.Abs(row.BaseQty ?? 0m) >= 0.0000005m && Math.Abs(row.UnassignedQty ?? 0m) >= 0.0000005m)
				{
					unassignederror = true;
					sender.RaiseExceptionHandling<SOShipLine.unassignedQty>(row, null,
						new PXSetPropertyException(Messages.LineBinLotSerialNotAssigned, PXErrorLevel.Warning, sender.GetValueExt<SOShipLine.inventoryID>(row)));
				}
			}

			if (unassignederror == false)
				sender.RaiseExceptionHandling<SOShipLine.unassignedQty>(row, null, null);
		}

		protected override bool IsLotSerOptionsEnabled(PXCache sender, LotSerOptions opt)
		{
			return base.IsLotSerOptionsEnabled(sender, opt) &&
				((SOShipment)sender.Graph.Caches<SOShipment>().Current)?.Confirmed != true;
		}

		public override SOShipLine CloneMaster(SOShipLine item)
		{
			SOShipLine copy = base.CloneMaster(item);
			copy.OrigOrderType = null;
			copy.OrigOrderNbr = null;
			copy.OrigLineNbr = null;
			copy.OrigSplitLineNbr = null;
			copy.IsClone = true;

			return copy;
		}

		protected virtual void SOShipment_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<SOShipment.confirmed>(e.Row, e.OldRow) && (bool?)sender.GetValue<SOShipment.confirmed>(e.Row) == true)
			{
				PXCache cache = sender.Graph.Caches[typeof(SOShipLine)];

				foreach (SOShipLine item in PXParentAttribute.SelectSiblings(cache, null, typeof(SOShipment)))
				{
					if (Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<SOShipLine.unassignedQty>(item, item.UnassignedQty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						//this code is placed to obligate platform call command preparing for current row and as result get an error. Normally it's not necessary, but in this case the code could be called from Unnatended mode
						cache.MarkUpdated(item);
					}
				}
			}
		}

		protected virtual void OrderAvailabilityCheck(PXCache sender, SOShipLine Row)
		{
			if (UnattendedMode)
				return;

			if (Row.OrigOrderNbr != null)
			{
				SOLineSplit2 split = PXSelect<SOLineSplit2,
					Where<SOLineSplit2.orderType, Equal<Current<SOShipLine.origOrderType>>,
						And<SOLineSplit2.orderNbr, Equal<Current<SOShipLine.origOrderNbr>>,
						And<SOLineSplit2.lineNbr, Equal<Current<SOShipLine.origLineNbr>>,
						And<SOLineSplit2.splitLineNbr, Equal<Current<SOShipLine.origSplitLineNbr>>>>>>>
					.SelectSingleBound(_Graph, new object[] { Row });

				SOLine2 soLine = PXSelect<SOLine2,
					Where<SOLine2.orderType, Equal<Current<SOShipLine.origOrderType>>,
						And<SOLine2.orderNbr, Equal<Current<SOShipLine.origOrderNbr>>,
						And<SOLine2.lineNbr, Equal<Current<SOShipLine.origLineNbr>>>>>>
					.SelectSingleBound(_Graph, new object[] { Row });

				if (split != null && soLine != null)
				{
					if (split.IsAllocated == true && split.Qty * soLine.CompleteQtyMax / 100 < split.ShippedQty)
						throw new PXSetPropertyException(Messages.OrderSplitCheck_QtyNegative,
							sender.GetValueExt<SOShipLine.inventoryID>(Row),
							sender.GetValueExt<SOShipLine.subItemID>(Row),
							sender.GetValueExt<SOShipLine.origOrderType>(Row),
							sender.GetValueExt<SOShipLine.origOrderNbr>(Row));

					if (PXDBPriceCostAttribute.Round((decimal)(soLine.OrderQty * soLine.CompleteQtyMax / 100m - soLine.ShippedQty)) < 0m &&
						PXDBPriceCostAttribute.Round((decimal)(split.Qty * soLine.CompleteQtyMax / 100m - split.ShippedQty)) < 0m)
					{
						throw new PXSetPropertyException(Messages.OrderCheck_QtyNegative, sender.GetValueExt<SOShipLine.inventoryID>(Row), sender.GetValueExt<SOShipLine.subItemID>(Row), sender.GetValueExt<SOShipLine.origOrderType>(Row), sender.GetValueExt<SOShipLine.origOrderNbr>(Row));
					}
				}
			}
		}

		public override void AvailabilityCheck(PXCache sender, ILSMaster Row)
		{
			base.AvailabilityCheck(sender, Row);

			if (Row is SOShipLine)
			{
				try
				{
					OrderAvailabilityCheck(sender, (SOShipLine)Row);
				}
				catch (PXSetPropertyException ex)
				{
					sender.RaiseExceptionHandling<SOShipLine.shippedQty>(Row, ((SOShipLine)Row).ShippedQty, ex);
				}
			}
			else
			{
				object parent = PXParentAttribute.SelectParent(sender, Row, typeof(SOShipLine));
				try
				{
					OrderAvailabilityCheck(sender.Graph.Caches[typeof(SOShipLine)], (SOShipLine)parent);
				}
				catch (PXSetPropertyException ex)
				{
					sender.RaiseExceptionHandling<SOShipLineSplit.qty>(Row, ((SOShipLineSplit)Row).Qty, ex);
				}
			}
		}

		protected override void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			using (ResolveNotDecimalUnitErrorRedirectorScope<SOShipLineSplit.qty>(e.Row))
				base.Master_RowUpdated(sender, e);
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			SOShipLine row = (SOShipLine)e.Row;
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				PXCache cache = sender.Graph.Caches[typeof(SOShipment)];
				object doc = PXParentAttribute.SelectParent(sender, row, typeof(SOShipment)) ?? cache.Current;

				bool? Confirmed = (bool?)cache.GetValue<SOShipment.confirmed>(doc);
				if (Confirmed == true)
				{
					if (Math.Abs((decimal)row.BaseQty) >= 0.0000005m && row.UnassignedQty >= 0.0000005m || row.UnassignedQty <= -0.0000005m)
					{
						if (sender.RaiseExceptionHandling<SOShipLine.unassignedQty>(row, row.UnassignedQty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						{
							throw new PXRowPersistingException(typeof(SOShipLine.unassignedQty).Name, row.UnassignedQty, Messages.BinLotSerialNotAssigned);
						}
					}
				}

			}

			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				try
				{
					OrderAvailabilityCheck(sender, row);
				}
				catch (PXSetPropertyException ex)
				{
					sender.RaiseExceptionHandling<SOShipLine.shippedQty>(row, row.ShippedQty, ex);
				}
			}
			base.Master_RowPersisting(sender, e);
		}

		public int? lastComponentID = null;
		protected override void _Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs<SOShipLine> e)
		{
			if (lastComponentID == e.Row.InventoryID)
			{
				return;
			}
			base._Master_RowUpdated(sender, e);
		}

		protected virtual void UpdateKit(PXCache sender, SOShipLineSplit row)
		{
			SOShipLine newline = SelectMaster(sender, row);

			if (newline == null)
			{
				return;
			}

			decimal KitQty = (decimal)newline.BaseQty;
			if (newline.InventoryID != row.InventoryID && row.IsStockItem == true)
			{
				foreach (PXResult<INKitSpecStkDet, InventoryItem> res in PXSelectJoin<INKitSpecStkDet,
					InnerJoin<InventoryItem,
						On<INKitSpecStkDet.FK.ComponentInventoryItem>>,
					Where<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>>>.Search<INKitSpecStkDet.compInventoryID>(sender.Graph, row.InventoryID, newline.InventoryID))
				{
					INKitSpecStkDet kititem = res;
					decimal ComponentQty = INUnitAttribute.ConvertToBase<SOShipLineSplit.inventoryID>(sender, row, kititem.UOM, KitQty * (decimal)kititem.DfltCompQty, INPrecision.NOROUND);

					SOShipLine copy = CloneMaster(newline);
					copy.InventoryID = row.InventoryID;

					Counters counters;
					if (!DetailCounters.TryGetValue(copy, out counters))
					{
						DetailCounters[copy] = counters = new Counters();
						foreach (SOShipLineSplit detail in SelectDetail(sender, copy))
						{
							UpdateCounters(sender, counters, detail);
						}
					}

					if (ComponentQty != 0m && (decimal)counters.BaseQty != ComponentQty)
					{
						KitQty = PXDBQuantityAttribute.Round(KitQty * (decimal)counters.BaseQty / ComponentQty);
						lastComponentID = kititem.CompInventoryID;
					}
				}
			}
			else if (newline.InventoryID != row.InventoryID)
			{
				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in PXSelectJoin<INKitSpecNonStkDet,
					InnerJoin<InventoryItem,
						On<INKitSpecNonStkDet.FK.ComponentInventoryItem>>,
					Where<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>>>.Search<INKitSpecNonStkDet.compInventoryID>(sender.Graph, row.InventoryID, newline.InventoryID))
				{
					INKitSpecNonStkDet kititem = res;

					decimal ComponentQty = INUnitAttribute.ConvertToBase<SOShipLineSplit.inventoryID>(sender, row, kititem.UOM, (decimal)kititem.DfltCompQty, INPrecision.NOROUND);

					if (ComponentQty != 0m && row.BaseQty != ComponentQty)
					{
						KitQty = PXDBQuantityAttribute.Round((decimal)row.BaseQty / ComponentQty);
						lastComponentID = kititem.CompInventoryID;
					}
				}
			}

			if (lastComponentID != null)
			{
				SOShipLine copy = PXCache<SOShipLine>.CreateCopy(newline);
				copy.ShippedQty = INUnitAttribute.ConvertFromBase<SOShipLine.inventoryID>(MasterCache, newline, newline.UOM, KitQty, INPrecision.QUANTITY);
				try
				{
					MasterCache.Update(copy);
				}
				finally
				{
					lastComponentID = null;
				}

				if (sender.Graph is SOShipmentEntry)
				{
					((SOShipmentEntry)sender.Graph).splits.View.RequestRefresh();
				}
			}
		}

		protected void Detail_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			SOShipLineSplit row = (SOShipLineSplit)e.Row;

			if (!_InternallCall && !UnattendedMode)
			{
				UpdateKit(sender, row);
			}
		}

		protected override void Detail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			base.Detail_RowUpdated(sender, e);

			SOShipLineSplit row = (SOShipLineSplit)e.Row;

			if (!sender.ObjectsEqual<SOShipLineSplit.lotSerialNbr>(e.Row, e.OldRow) && ((SOShipLineSplit)e.Row).LotSerialNbr != null && ((SOShipLineSplit)e.Row).Operation == SOOperation.Issue)
			{
				LotSerialNbr_Updated(sender, e);
			}

			if (!sender.ObjectsEqual<SOShipLineSplit.locationID>(e.Row, e.OldRow) && ((SOShipLineSplit)e.Row).LotSerialNbr != null && e.ExternalCall)
			{
				Location_Updated(sender, e);
			}

			if (!_InternallCall && !UnattendedMode)
			{
				UpdateKit(sender, row);
			}
		}

		protected override void Detail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			SOShipLineSplit row = (SOShipLineSplit)e.Row;

			PXResult<InventoryItem, INLotSerClass> res = ReadInventoryItem(sender, row.InventoryID);
			InventoryItem item = (InventoryItem)res;
			bool NonStockKit = item.KitItem == true && item.StkItem == false;

			if (NonStockKit)
			{
				row.InventoryID = null;
			}

			base.Detail_RowInserting(sender, e);
		}

		protected override void Detail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			base.Detail_RowInserted(sender, e);

			SOShipLineSplit row = (SOShipLineSplit)e.Row;
			if (!_InternallCall && !UnattendedMode)
			{
				UpdateKit(sender, row);
			}
		}

		protected override void Detail_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			base.Detail_RowDeleted(sender, e);

			SOShipLineSplit row = (SOShipLineSplit)e.Row;
			if (!_InternallCall && !UnattendedMode)
			{
				UpdateKit(sender, row);
			}
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			SOShipLine tran = (SOShipLine)e.Row;

			AvailabilityFetchMode fetchMode = AvailabilityFetchMode.ExcludeCurrent | AvailabilityFetchMode.TryOptimize;
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

			base.Availability_FieldSelecting(sender, e);
		}

		private IStatus GetAvailability(PXCache sender, SOShipLine tran, AvailabilityFetchMode fetchMode)
		{
			IStatus availability = AvailabilityFetch(sender, tran, fetchMode);

			if (availability != null)
			{
				decimal unitRate = INUnitAttribute.ConvertFromBase<SOShipLine.inventoryID, SOShipLine.uOM>(sender, tran, 1m, INPrecision.NOROUND);
				availability.QtyOnHand = PXDBQuantityAttribute.Round((decimal)availability.QtyOnHand * unitRate);
				availability.QtyAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyAvail * unitRate);
				availability.QtyNotAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyNotAvail * unitRate);
				availability.QtyHardAvail = PXDBQuantityAttribute.Round((decimal)availability.QtyHardAvail * unitRate);

			}

			return availability;
		}

		private string BuildAvailabilityStatusLine(PXCache sender, SOShipLine tran, IStatus availability)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(IN.Messages.Availability_Info),
					sender.GetValue<SOShipLine.uOM>(tran),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail));
		}

		private string BuildAvailabilityStatusLine(PXCache sender, SOShipLine tran, IStatus availability, IStatus availabilityProject)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(IN.Messages.Availability_Info_Project),
					sender.GetValue<SOShipLine.uOM>(tran),
					FormatQty(availabilityProject.QtyOnHand),
					FormatQty(availabilityProject.QtyAvail),
					FormatQty(availabilityProject.QtyHardAvail),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail));
		}

		protected int _detailsRequested = 0;

		protected override IStatus AvailabilityFetch<TNode>(ILSDetail Row, IStatus allocated, IStatus status, AvailabilityFetchMode fetchMode)
		{
			if (status != null)
			{
				allocated.QtyOnHand += status.QtyOnHand;
				allocated.QtyHardAvail += status.QtyHardAvail;
			}
			allocated.QtyAvail = allocated.QtyHardAvail;

			if (fetchMode.HasFlag(AvailabilityFetchMode.TryOptimize) && _detailsRequested++ == 5)
			{
				foreach (PXResult<SOShipLine, INSiteStatus, INLocationStatus, INLotSerialStatus> res in
						PXSelectReadonly2<SOShipLine,
						InnerJoin<INSiteStatus,
						On<INSiteStatus.inventoryID, Equal<SOShipLine.inventoryID>,
						And<INSiteStatus.subItemID, Equal<SOShipLine.subItemID>,
							And<INSiteStatus.siteID, Equal<SOShipLine.siteID>>>>,
						LeftJoin<INLocationStatus,
						On<INLocationStatus.inventoryID, Equal<SOShipLine.inventoryID>,
						And<INLocationStatus.subItemID, Equal<SOShipLine.subItemID>,
						And<INLocationStatus.siteID, Equal<SOShipLine.siteID>,
						And<INLocationStatus.locationID, Equal<SOShipLine.locationID>>>>>,
					LeftJoin<INLotSerialStatus,
						On<INLotSerialStatus.inventoryID, Equal<SOShipLine.inventoryID>,
						And<INLotSerialStatus.subItemID, Equal<SOShipLine.subItemID>,
						And<INLotSerialStatus.siteID, Equal<SOShipLine.siteID>,
						And<INLotSerialStatus.locationID, Equal<SOShipLine.locationID>,
						And<INLotSerialStatus.lotSerialNbr, Equal<SOShipLine.lotSerialNbr>>>>>>>>>,
						Where<SOShipLine.shipmentNbr, Equal<Current<SOShipment.shipmentNbr>>>>
						.Select(this._Graph))
				{
					SOShipLine line = res;
					INSiteStatus siteStatus = res;
					INLocationStatus locStatus = res;
					INLotSerialStatus lotSerStatus = res;

					INSiteStatus.PK.StoreCached(this._Graph, siteStatus);

					if (locStatus.LocationID != null)
						INLocationStatus.PK.StoreCached(this._Graph, locStatus);

					if (lotSerStatus?.LotSerialNbr != null)
						IN.INLotSerialStatus.PK.StoreCached(this._Graph, lotSerStatus);
				}
			}

			if (fetchMode.HasFlag(AvailabilityFetchMode.ExcludeCurrent))
			{
				decimal SignQtyAvail;
				decimal SignQtyHardAvail;
				INItemPlanIDAttribute.GetInclQtyAvail<TNode>(DetailCache, Row, out SignQtyAvail, out SignQtyHardAvail);

				if (SignQtyHardAvail != 0)
				{
					allocated.QtyAvail -= SignQtyHardAvail * (Row.BaseQty ?? 0m);
					allocated.QtyNotAvail += SignQtyHardAvail * (Row.BaseQty ?? 0m);
					allocated.QtyHardAvail -= SignQtyHardAvail * (Row.BaseQty ?? 0m);
				}
				//Exclude Unassigned
				foreach (Unassigned.SOShipLineSplit detail in SelectUnassignedDetails(DetailCache, Row))
				{
					if (SignQtyHardAvail != 0 && (Row.LocationID == null || Row.LocationID == detail.LocationID) &&
						(Row.LotSerialNbr == null || string.IsNullOrEmpty(detail.LotSerialNbr) || string.Equals(Row.LotSerialNbr, detail.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase)))
					{
						allocated.QtyAvail -= SignQtyHardAvail * (detail.BaseQty ?? 0m);
						allocated.QtyHardAvail -= SignQtyHardAvail * (detail.BaseQty ?? 0m);
					}
				}
			}
			return allocated;
		}

		protected virtual bool LotSerialNbr_Updated(PXCache sender, EventArgs e)
		{
			SOShipLineSplit split = (SOShipLineSplit)(e is PXRowUpdatedEventArgs ? ((PXRowUpdatedEventArgs)e).Row : ((PXRowInsertedEventArgs)e).Row);
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, split.InventoryID);
			INSiteLotSerial siteLotSerial = PXSelect<INSiteLotSerial,
				Where<INSiteLotSerial.inventoryID, Equal<Required<INSiteLotSerial.inventoryID>>,
				And<INSiteLotSerial.siteID, Equal<Required<INSiteLotSerial.siteID>>,
				And<INSiteLotSerial.lotSerialNbr, Equal<Required<INSiteLotSerial.lotSerialNbr>>>>>>.Select(sender.Graph, split.InventoryID, split.SiteID, split.LotSerialNbr);

			if (INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null && siteLotSerial != null && siteLotSerial.LotSerAssign != INLotSerAssign.WhenUsed)
			{
				decimal qtyHardAvail = siteLotSerial.QtyHardAvail.GetValueOrDefault();

				//Exclude unasigned
				foreach (Unassigned.SOShipLineSplit detail in SelectUnassignedDetails(DetailCache, split))
				{
					if ((split.LocationID == null || split.LocationID == detail.LocationID) &&
						(string.IsNullOrEmpty(detail.LotSerialNbr) || split.LotSerialNbr == null || string.Equals(split.LotSerialNbr, detail.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase)))
					{
						qtyHardAvail += split.BaseQty.GetValueOrDefault();
					}

				}

				if (qtyHardAvail < split.BaseQty)
				{
					split.LotSerialNbr = null;
					sender.RaiseExceptionHandling<SOShipLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
					return false;
				}
			}
			return true;
		}

		protected virtual void Location_Updated(PXCache sender, EventArgs e)
		{
			SOShipLineSplit split = (SOShipLineSplit)(e is PXRowUpdatedEventArgs ? ((PXRowUpdatedEventArgs)e).Row : ((PXRowInsertedEventArgs)e).Row);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, split.InventoryID);

			SOShipLine line = SelectMaster(sender, split);
			if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null)
			{
				INLotSerialStatus res = PXSelect<INLotSerialStatus, Where<INLotSerialStatus.inventoryID, Equal<Required<INLotSerialStatus.inventoryID>>,
				 And<INLotSerialStatus.subItemID, Equal<Required<INLotSerialStatus.subItemID>>,
				 And<INLotSerialStatus.siteID, Equal<Required<INLotSerialStatus.siteID>>,
				 And<INLotSerialStatus.lotSerialNbr, Equal<Required<INLotSerialStatus.lotSerialNbr>>,
				 And<INLotSerialStatus.locationID, Equal<Required<INLotSerialStatus.locationID>>>>>>>>.Select(sender.Graph, split.InventoryID, split.SubItemID, split.SiteID, split.LotSerialNbr, split.LocationID);
				if (res == null)
				{
					split.LotSerialNbr = null;
				}
			}
		}

		public override SOShipLineSplit Convert(SOShipLine item)
		{
			using (InvtMultScope<SOShipLine> ms = new InvtMultScope<SOShipLine>(item))
			{
				SOShipLineSplit ret = item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				ret.LotSerialNbr = string.Empty;
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
		public virtual void SOShipLine_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && AdvancedAvailCheck(sender, e.Row) &&
				((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				if (((SOShipLine)e.Row).BaseQty != 0m)
				{
					AvailabilityCheck(sender, (SOShipLine)e.Row);
				}
			}
		}

		public virtual void SOShipLineSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				bool RequireLocationAndSubItem = ((SOShipLineSplit)e.Row).IsStockItem == true && ((SOShipLineSplit)e.Row).BaseQty != 0m;

				PXDefaultAttribute.SetPersistingCheck<SOShipLineSplit.subItemID>(sender, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<SOShipLineSplit.locationID>(sender, e.Row, RequireLocationAndSubItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

				if (AdvancedAvailCheck(sender, e.Row) && ((SOShipLineSplit)e.Row).BaseQty != 0m)
				{
					AvailabilityCheck(sender, (SOShipLineSplit)e.Row);
				}
			}
		}

		public virtual void SOShipLineSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOShipLine)];
			if (cache.Current != null && (e.Row == null || ((SOShipLine)cache.Current).LineNbr == ((SOShipLineSplit)e.Row).LineNbr && ((SOShipLineSplit)e.Row).IsStockItem == true))
			{
				e.NewValue = ((SOShipLine)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void SOShipLineSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOShipLine)];
			if (cache.Current != null && ((SOShipLine)cache.Current).IsUnassigned != true && (e.Row == null || ((SOShipLine)cache.Current).LineNbr == ((SOShipLineSplit)e.Row).LineNbr && ((SOShipLineSplit)e.Row).IsStockItem == true))
			{
				e.NewValue = ((SOShipLine)cache.Current).LocationID;
				e.Cancel = (_InternallCall == true || e.NewValue != null);
			}
		}

		public virtual void SOShipLineSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOShipLine)];
			if (cache.Current != null && (e.Row == null || ((SOShipLine)cache.Current).LineNbr == ((SOShipLineSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<SOShipLine> ms = new InvtMultScope<SOShipLine>((SOShipLine)cache.Current))
				{
					e.NewValue = ((SOShipLine)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			PXErrorLevel level = AdvancedAvailCheck(sender, row) ? PXErrorLevel.Error : PXErrorLevel.Warning;
			if (row is SOShipLine)
			{
				sender.RaiseExceptionHandling<SOShipLine.shippedQty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, level, sender.GetStateExt<SOShipLine.inventoryID>(row), sender.GetStateExt<SOShipLine.subItemID>(row), sender.GetStateExt<SOShipLine.siteID>(row), sender.GetStateExt<SOShipLine.locationID>(row), sender.GetValue<SOShipLine.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<SOShipLineSplit.qty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, level, sender.GetStateExt<SOShipLineSplit.inventoryID>(row), sender.GetStateExt<SOShipLineSplit.subItemID>(row), sender.GetStateExt<SOShipLineSplit.siteID>(row), sender.GetStateExt<SOShipLineSplit.locationID>(row), sender.GetValue<SOShipLineSplit.lotSerialNbr>(row)));
			}
		}

		protected bool AdvancedAvailCheck(PXCache sender, object row)
		{
			SOSetup setup = (SOSetup)sender.Graph.Caches[typeof(SOSetup)].Current;
			if (setup != null && setup.AdvancedAvailCheck == true)
			{
				if (_advancedAvailCheck != null) return _advancedAvailCheck == true;
			}
			return false;
		}

		public void OverrideAdvancedAvailCheck(bool checkRequired)
		{
			_advancedAvailCheck = checkRequired;
		}
		private bool? _advancedAvailCheck;
		#endregion

		protected override PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(PXCache sender, SOShipLine Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (!IsLocationEnabled && IsLotSerialRequired)
			{
				return new PXSelectJoin<INLotSerialStatus,
				InnerJoin<INLocation,
					On<INLotSerialStatus.FK.Location>,
				InnerJoin<INSiteLotSerial, On<INSiteLotSerial.inventoryID, Equal<INLotSerialStatus.inventoryID>,
						And<INSiteLotSerial.siteID, Equal<INLotSerialStatus.siteID>,
						And<INSiteLotSerial.lotSerialNbr, Equal<INLotSerialStatus.lotSerialNbr>>>>>>,
				Where<INLotSerialStatus.inventoryID, Equal<Current<INLotSerialStatus.inventoryID>>,
				And<INLotSerialStatus.siteID, Equal<Current<INLotSerialStatus.siteID>>,
				And<INLotSerialStatus.qtyOnHand, Greater<decimal0>,
				And<INSiteLotSerial.qtyHardAvail, Greater<decimal0>>>>>>(sender.Graph);
			}
			else
			{
				return base.GetSerialStatusCmdBase(sender, Row, item);
			}
		}

		public override PXSelectBase<PM.PMLotSerialStatus> GetSerialStatusCmdProject(PXCache sender, SOShipLine Row, PXResult<InventoryItem, INLotSerClass> item)
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
				And<PM.PMLotSerialStatus.qtyOnHand, Greater<decimal0>,
				And<INSiteLotSerial.qtyHardAvail, Greater<decimal0>>>>>>(sender.Graph);
			}
			else
			{
				return base.GetSerialStatusCmdProject(sender, Row, item);
			}
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, SOShipLine Row, INLotSerClass lotSerClass)
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
				{
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				}
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr, Equal<Current<INLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		protected override void AppendSerialStatusCmdWhereProject(PXSelectBase<PM.PMLotSerialStatus> cmd, SOShipLine Row, INLotSerClass lotSerClass)
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
				{
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				}
				else
					cmd.WhereAnd<Where<PM.PMLotSerialStatus.lotSerialNbr, Equal<Current<PM.PMLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		public override void AvailabilityCheck(PXCache sender, ILSMaster Row, IStatus availability)
		{
			base.AvailabilityCheck(sender, Row, availability);
			if (Row.InvtMult == (short)-1 && Row.BaseQty > 0m && availability != null)
			{
				SOShipment doc = (SOShipment)sender.Graph.Caches[typeof(SOShipment)].Current;
				if (availability.QtyOnHand - Row.Qty < 0m && doc != null && doc.Confirmed == false)
				{
					switch (GetWarningLevel(availability))
					{
						case AvailabilityWarningLevel.LotSerial:
							RaiseQtyRowExceptionHandling(sender, Row, Row.Qty, new PXSetPropertyException(IN.Messages.StatusCheck_QtyLotSerialOnHandNegative));
							break;
						case AvailabilityWarningLevel.Location:
							RaiseQtyRowExceptionHandling(sender, Row, Row.Qty, new PXSetPropertyException(IN.Messages.StatusCheck_QtyLocationOnHandNegative));
							break;
						case AvailabilityWarningLevel.Site:
							RaiseQtyRowExceptionHandling(sender, Row, Row.Qty, new PXSetPropertyException(IN.Messages.StatusCheck_QtyOnHandNegative));
							break;
					}
				}
			}
		}
		private void RaiseQtyRowExceptionHandling(PXCache sender, object row, object newValue, PXSetPropertyException e)
		{
			PXErrorLevel level = AdvancedAvailCheck(sender, row) ? PXErrorLevel.Error : PXErrorLevel.Warning;
			if (row is SOShipLine)
			{
				sender.RaiseExceptionHandling<SOShipLine.shippedQty>(row, newValue,
					e == null ? e : new PXSetPropertyException(e.Message, level, sender.GetStateExt<SOShipLine.inventoryID>(row), sender.GetStateExt<SOShipLine.subItemID>(row), sender.GetStateExt<SOShipLine.siteID>(row), sender.GetStateExt<SOShipLine.locationID>(row), sender.GetValue<SOShipLine.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<SOShipLineSplit.qty>(row, newValue,
					e == null ? e : new PXSetPropertyException(e.Message, level, sender.GetStateExt<SOShipLineSplit.inventoryID>(row), sender.GetStateExt<SOShipLineSplit.subItemID>(row), sender.GetStateExt<SOShipLineSplit.siteID>(row), sender.GetStateExt<INTranSplit.locationID>(row), sender.GetValue<SOShipLineSplit.lotSerialNbr>(row)));
			}
		}

		/// <summary>
		/// Inserts SOShipLine into cache without adding the splits.
		/// The Splits have to be added manually.
		/// </summary>
		/// <param name="line">Master record.</param>
		public virtual SOShipLine InsertMasterWithoutSplits(SOShipLine line)
		{
			_InternallCall = true;
			try
			{
				var row = (SOShipLine)MasterCache.Insert(line);
				DetailCounters.Remove(row);
				return row;
			}
			finally
			{
				_InternallCall = false;
			}
		}

		protected virtual List<Unassigned.SOShipLineSplit> SelectUnassignedDetails(PXCache sender, ILSDetail row)
		{
			Unassigned.SOShipLineSplit unassignedRow = new Unassigned.SOShipLineSplit();
			unassignedRow.ShipmentNbr = ((SOShipLineSplit)row).ShipmentNbr;
			unassignedRow.LineNbr = ((SOShipLineSplit)row).LineNbr;
			unassignedRow.SplitLineNbr = ((SOShipLineSplit)row).SplitLineNbr;
			object[] ret = PXParentAttribute.SelectSiblings(sender.Graph.Caches[typeof(Unassigned.SOShipLineSplit)], unassignedRow,
				(_detailsRequested > 5) ? typeof(SOShipment) : typeof(SOShipLine));
			List<Unassigned.SOShipLineSplit> list = new List<Unassigned.SOShipLineSplit>(ret.Cast<Unassigned.SOShipLineSplit>());
			return list.FindAll(a => SameInventoryItem(a, row) && a.LineNbr == ((SOShipLineSplit)row).LineNbr);
		}

		protected override void SetMasterQtyFromBase(PXCache sender, SOShipLine master)
		{
			if (master.UOM == master.OrderUOM && master.BaseQty == master.BaseFullOrderQty)
			{
				master.Qty = master.FullOrderQty;
				return;
			}
			base.SetMasterQtyFromBase(sender, master);
		}
	}
}
