using System;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.SO.GraphExtensions.SOShipmentEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class SOShipmentLineSplittingExtension : LineSplittingExtension<SOShipmentEntry, SOShipment, SOShipLine, SOShipLineSplit>
	{
		#region State
		public bool IsLocationEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || (ordertype.RequireShipping == false && ordertype.RequireLocation == true && ordertype.INDocType != INTranType.NoUpdate);
			}
		}

		public bool IsLotSerialRequired
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireLotSerial == true;
			}
		}

		protected new virtual SOShipmentItemAvailabilityExtension Availability
			=> Base.FindImplementation<SOShipmentItemAvailabilityExtension>();
		#endregion

		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(SOShipLineSplit.FK.Shipment.SameAsCurrent);

		protected override Type LineQtyField => typeof(SOShipLine.shippedQty);

		public override SOShipLineSplit LineToSplit(SOShipLine line)
		{
			using (new InvtMultScope(line))
			{
				SOShipLineSplit ret = line;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				ret.LotSerialNbr = string.Empty;
				return ret;
			}
		}
		#endregion

		#region Initialization
		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<SOShipment>.Updated.Subscribe(Base, EventHandler);
		}
		#endregion

		#region Event Handlers
		#region SOShipment
		protected virtual void EventHandler(ManualEvent.Row<SOShipment>.Updated.Args e)
		{
			if (e.Row.Confirmed != e.OldRow.Confirmed && e.Row.Confirmed == true)
			{
				foreach (SOShipLine item in PXParentAttribute.SelectSiblings(LineCache, null, typeof(SOShipment)))
				{
					if (Math.Abs(item.BaseQty.Value) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						LineCache.RaiseExceptionHandling<SOShipLine.unassignedQty>(item, item.UnassignedQty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						//this code is placed to obligate platform call command preparing for current row and as result get an error. Normally it's not necessary, but in this case the code could be called from Unnatended mode
						LineCache.MarkUpdated(item);
					}
				}
			}
		}
		#endregion
		#region SOShipLine
		protected override void EventHandler(ManualEvent.Row<SOShipLine>.Selected.Args e) // former SOShipLine_RowSelected subscribed separately
		{
			if (e.Row == null)
				return;

			bool unassignederror = false;
			if (e.Row.InventoryID != null)
			{
				if (Math.Abs(e.Row.BaseQty ?? 0m) >= 0.0000005m && Math.Abs(e.Row.UnassignedQty ?? 0m) >= 0.0000005m)
				{
					unassignederror = true;
					e.Cache.RaiseExceptionHandling<SOShipLine.unassignedQty>(e.Row, null,
						new PXSetPropertyException(Messages.LineBinLotSerialNotAssigned, PXErrorLevel.Warning, e.Cache.GetValueExt<SOShipLine.inventoryID>(e.Row)));
				}
			}

			if (unassignederror == false)
				e.Cache.RaiseExceptionHandling<SOShipLine.unassignedQty>(e.Row, null, null);
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLine>.Updated.Args e)
		{
			using (ResolveNotDecimalUnitErrorRedirectorScope<SOShipLineSplit.qty>(e.Row))
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLine>.Persisting.Args e) // former Master_RowPersisting
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				SOShipment doc = PXParentAttribute.SelectParent<SOShipment>(e.Cache, e.Row) ?? Base.Document.Current;

				if (doc.Confirmed == true && Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m)
					if (e.Cache.RaiseExceptionHandling<SOShipLine.unassignedQty>(e.Row, e.Row.UnassignedQty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(typeof(SOShipLine.unassignedQty).Name, e.Row.UnassignedQty, Messages.BinLotSerialNotAssigned);

				try
				{
					Availability.OrderCheck(e.Row);
				}
				catch (PXSetPropertyException ex)
				{
					e.Cache.RaiseExceptionHandling<SOShipLine.shippedQty>(e.Row, e.Row.ShippedQty, ex);
				}
			}

			base.EventHandler(e);
		}

		public int? LastComponentID { get; set; }
		protected override void EventHandlerInternal(ManualEvent.Row<SOShipLine>.Updated.Args e)
		{
			if (LastComponentID != e.Row.InventoryID)
				base.EventHandlerInternal(e);
		}
		#endregion
		#region SOShipLineSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.Row<SOShipLineSplit>.Updating.Subscribe(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				using (new InvtMultScope(LineCurrent))
				{
					e.NewValue = LineCurrent.InvtMult;
					e.Cancel = true;
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<SOShipLineSplit, SOShipLineSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && LineCurrent.IsUnassigned != true && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr && e.Row.IsStockItem == true))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = SuppressedMode == true || e.NewValue != null;
			}
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLineSplit>.Inserting.Args e)
		{
			(var item, var _) = ReadInventoryItem(e.Row.InventoryID);

			if (item.KitItem == true && item.StkItem == false)
				e.Row.InventoryID = null;

			base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLineSplit>.Inserted.Args e)
		{
			base.EventHandler(e);

			if (!SuppressedMode && !UnattendedMode)
				UpdateKit(e.Row);
		}

		protected virtual void EventHandler(ManualEvent.Row<SOShipLineSplit>.Updating.Args e)
		{
			if (!SuppressedMode && !UnattendedMode)
				UpdateKit(e.Row);
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLineSplit>.Updated.Args e)
		{
			base.EventHandler(e);

			if (e.Row.LotSerialNbr != e.OldRow.LotSerialNbr && e.Row.LotSerialNbr != null && e.Row.Operation == SOOperation.Issue)
				LotSerialNbrUpdated(e.Row);

			if (e.Row.LocationID != e.OldRow.LocationID && e.Row.LotSerialNbr != null && e.ExternalCall)
				LocationUpdated(e.Row);

			if (!SuppressedMode && !UnattendedMode)
				UpdateKit(e.Row);
		}

		protected override void EventHandler(ManualEvent.Row<SOShipLineSplit>.Deleted.Args e)
		{
			base.EventHandler(e);

			if (!SuppressedMode && !UnattendedMode)
				UpdateKit(e.Row);
		}

		protected virtual bool LotSerialNbrUpdated(SOShipLineSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);
			INSiteLotSerial siteLotSerial =
				SelectFrom<INSiteLotSerial>.
				Where<
					INSiteLotSerial.inventoryID.IsEqual<@P.AsInt>.
					And<INSiteLotSerial.siteID.IsEqual<@P.AsInt>>.
					And<INSiteLotSerial.lotSerialNbr.IsEqual<@P.AsString>>>.
				View.Select(Base, split.InventoryID, split.SiteID, split.LotSerialNbr);

			if (INLotSerialNbrAttribute.IsTrackSerial(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null && siteLotSerial != null && siteLotSerial.LotSerAssign != INLotSerAssign.WhenUsed)
			{
				decimal qtyHardAvail = siteLotSerial.QtyHardAvail ?? 0;

				//Exclude unasigned
				foreach (Unassigned.SOShipLineSplit detail in Availability.SelectUnassignedDetails(split))
				{
					if (split.LocationID.IsIn(null, detail.LocationID) &&
						(string.IsNullOrEmpty(detail.LotSerialNbr) || split.LotSerialNbr == null || string.Equals(split.LotSerialNbr, detail.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase)))
					{
						qtyHardAvail += split.BaseQty ?? 0;
					}
				}

				if (qtyHardAvail < split.BaseQty)
				{
					split.LotSerialNbr = null;
					SplitCache.RaiseExceptionHandling<SOShipLineSplit.lotSerialNbr>(split, null, new PXSetPropertyException(IN.Messages.Inventory_Negative2));
					return false;
				}
			}
			return true;
		}

		protected virtual void LocationUpdated(SOShipLineSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);

			if (INLotSerialNbrAttribute.IsTrack(item, split.TranType, split.InvtMult) && split.LotSerialNbr != null)
			{
				INLotSerialStatus status =
					SelectFrom<INLotSerialStatus>.
					Where<
						INLotSerialStatus.inventoryID.IsEqual<@P.AsInt>.
						And<INLotSerialStatus.subItemID.IsEqual<@P.AsInt>>.
						And<INLotSerialStatus.siteID.IsEqual<@P.AsInt>>.
						And<INLotSerialStatus.lotSerialNbr.IsEqual<@P.AsString>>.
						And<INLotSerialStatus.locationID.IsEqual<@P.AsInt>>>.
					View.Select(Base, split.InventoryID, split.SubItemID, split.SiteID, split.LotSerialNbr, split.LocationID);

				if (status == null)
					split.LotSerialNbr = null;
			}
		}
		#endregion
		#endregion

		#region Select LotSerial Status
		protected override PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(SOShipLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (!IsLocationEnabled && IsLotSerialRequired)
			{
				return new
					SelectFrom<INLotSerialStatus>.
					InnerJoin<INLocation>.On<INLotSerialStatus.FK.Location>.
					InnerJoin<INSiteLotSerial>.On<
						INSiteLotSerial.inventoryID.IsEqual<INLotSerialStatus.inventoryID>.
						And<INSiteLotSerial.siteID.IsEqual<INLotSerialStatus.siteID>>.
						And<INSiteLotSerial.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr>>>.
					Where<
						INLotSerialStatus.inventoryID.IsEqual<INLotSerialStatus.inventoryID.FromCurrent>.
						And<INLotSerialStatus.siteID.IsEqual<INLotSerialStatus.siteID.FromCurrent>>.
						And<INLotSerialStatus.qtyOnHand.IsGreater<decimal0>>.
						And<INSiteLotSerial.qtyHardAvail.IsGreater<decimal0>>>.
					View(Base);
			}
			else
			{
				return base.GetSerialStatusCmdBase(line, item);
			}
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, SOShipLine line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			}
			else
			{
				switch (line.TranType)
				{
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid.IsEqual<True>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(line.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}
		#endregion

		protected override bool IsLotSerOptionsEnabled(LotSerOptions opt) => base.IsLotSerOptionsEnabled(opt) && Base.Document.Current?.Confirmed != true;

		public override SOShipLine Clone(SOShipLine item)
		{
			SOShipLine copy = base.Clone(item);

			copy.OrigOrderType = null;
			copy.OrigOrderNbr = null;
			copy.OrigLineNbr = null;
			copy.OrigSplitLineNbr = null;
			copy.IsClone = true;

			return copy;
		}

		protected override void SetLineQtyFromBase(SOShipLine line)
		{
			if (line.UOM == line.OrderUOM && line.BaseQty == line.BaseFullOrderQty)
			{
				line.Qty = line.FullOrderQty;
				return;
			}
			base.SetLineQtyFromBase(line);
		}

		protected virtual void UpdateKit(SOShipLineSplit split)
		{
			SOShipLine newLine = SelectLine(split);

			if (newLine == null)
				return;

			decimal kitQty = newLine.BaseQty.Value;
			if (newLine.InventoryID != split.InventoryID)
			{
				if (split.IsStockItem == true)
				{
					foreach (INKitSpecStkDet kitItem in
						SelectFrom<INKitSpecStkDet>.
						InnerJoin<InventoryItem>.On<INKitSpecStkDet.FK.ComponentInventoryItem>.
						Where<INKitSpecStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
						View.Search<INKitSpecStkDet.compInventoryID>(Base, split.InventoryID, newLine.InventoryID))
					{
						decimal componentQty = INUnitAttribute.ConvertToBase<SOShipLineSplit.inventoryID>(SplitCache, split, kitItem.UOM, kitQty * kitItem.DfltCompQty.Value, INPrecision.NOROUND);

						SOShipLine copy = Clone(newLine);
						copy.InventoryID = split.InventoryID;

						if (!LineCounters.TryGetValue(copy, out Counters counters))
						{
							LineCounters[copy] = counters = new Counters();
							foreach (SOShipLineSplit detail in SelectSplits(copy))
								UpdateCounters(counters, detail);
						}

						if (componentQty != 0m && counters.BaseQty != componentQty)
						{
							kitQty = PXDBQuantityAttribute.Round(kitQty * counters.BaseQty / componentQty);
							LastComponentID = kitItem.CompInventoryID;
						}
					}
				}
				else
				{
					foreach (INKitSpecNonStkDet kitItem in
						SelectFrom<INKitSpecNonStkDet>.
						InnerJoin<InventoryItem>.On<INKitSpecNonStkDet.FK.ComponentInventoryItem>.
						Where<INKitSpecNonStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
						View.Search<INKitSpecNonStkDet.compInventoryID>(Base, split.InventoryID, newLine.InventoryID))
					{
						decimal componentQty = INUnitAttribute.ConvertToBase<SOShipLineSplit.inventoryID>(SplitCache, split, kitItem.UOM, kitItem.DfltCompQty.Value, INPrecision.NOROUND);

						if (componentQty != 0m && split.BaseQty != componentQty)
						{
							kitQty = PXDBQuantityAttribute.Round(split.BaseQty.Value / componentQty);
							LastComponentID = kitItem.CompInventoryID;
						}
					}
				} 
			}

			if (LastComponentID != null)
			{
				SOShipLine copy = PXCache<SOShipLine>.CreateCopy(newLine);
				copy.ShippedQty = INUnitAttribute.ConvertFromBase<SOShipLine.inventoryID>(LineCache, newLine, newLine.UOM, kitQty, INPrecision.QUANTITY);

				try
				{
					LineCache.Update(copy);
				}
				finally
				{
					LastComponentID = null;
				}

				Base.splits.View.RequestRefresh();
			}
		}

		/// <summary>
		/// Inserts SOShipLine into cache without adding the splits.
		/// The Splits have to be added manually.
		/// </summary>
		/// <param name="line">Master record.</param>
		public virtual SOShipLine InsertWithoutSplits(SOShipLine line)
		{
			using (SuppressedModeScope(true))
			{
				var row = (SOShipLine)LineCache.Insert(line);
				LineCounters.Remove(row);
				return row;
			}
		}
	}
}
