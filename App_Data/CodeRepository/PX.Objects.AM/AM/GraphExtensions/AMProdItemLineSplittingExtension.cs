using System;
using System.Linq;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.IN;
using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.AM
{
	public abstract class AMProdItemLineSplittingExtension<TGraph> : IN.GraphExtensions.LineSplittingExtension<TGraph, AMProdItem, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(AMProdItemSplit.FK.ProductionOrder.SameAsCurrent);

		protected override Type LineQtyField => typeof(AMProdItem.qtytoProd);

		public override AMProdItemSplit LineToSplit(AMProdItem item)
		{
			using (new InvtMultScope(item))
			{
				AMProdItemSplit ret = (AMProdItemSplit)item;
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Event Handlers
		#region AMProdItem
		protected override void SubscribeForLineEvents()
		{
			base.SubscribeForLineEvents();
			ManualEvent.FieldOf<AMProdItem, AMProdItem.qtytoProd>.Verifying.Subscribe<decimal?>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<AMProdItem, AMProdItem.qtytoProd>.Verifying.Args<decimal?> e)
		{
			if (e.NewValue < 0m)
				throw new PXSetPropertyException(Messages.EntryGreaterEqualZero, PXErrorLevel.Error, 0);
		}

		protected override void EventHandler(ManualEvent.Row<AMProdItem>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				base.EventHandler(e);
				return;
			}

			if(e.Row?.PreassignLotSerial == true)
			{
				return;
			}

			e.Cache.SetValue<AMProdItem.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<AMProdItem.expireDate>(e.Row, null);
		}

		protected override void EventHandler(ManualEvent.Row<AMProdItem>.Updated.Args e)
		{
			base.EventHandler(e);

			if(e.Row?.PreassignLotSerial == true)
			{
				return;
			}

			e.Cache.SetValue<AMProdItem.lotSerialNbr>(e.Row, null);
			e.Cache.SetValue<AMProdItem.expireDate>(e.Row, null);
		}

		protected override void EventHandler(ManualEvent.Row<AMProdItem>.Persisting.Args e)
		{
			var isPreassigned = e.Row?.PreassignLotSerial == true;
			var isLotTracked = false;
			if(isPreassigned)
			{
				var result = (INLotSerClass)ReadInventoryItem(e.Row.InventoryID);
				isLotTracked = result != null && result.LotSerTrack == INLotSerTrack.LotNumbered;
				if(isLotTracked && !string.IsNullOrWhiteSpace(e.Row?.LotSerialNbr))
				{
					// Trigger reset of LotSerialNbr in base
					LineCounters[e.Row] = new Counters { UnassignedNumber = 1 };
				}
			}

			if (!isPreassigned)
			{
				//for normal orders there are only when received numbers which do not require any additional processing
				LineCounters[e.Row] = new Counters { UnassignedNumber = 0 };
			}

			base.EventHandler(e);

			if(isPreassigned && isLotTracked && string.IsNullOrWhiteSpace(e.Row.LotSerialNbr))
			{
				// User changes lot number used on order...
				foreach (AMProdItemSplit detail in SelectSplits(e.Row))
				{
					if(string.IsNullOrWhiteSpace(detail.LotSerialNbr))
					{
						continue;
					}

					e.Row.LotSerialNbr = detail.LotSerialNbr;
				}
			}
		}
		#endregion
		#region AMProdItemSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.lotSerialNbr>.Updated.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.invtMult>.Defaulting.Args<short?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.ProdOrdID == LineCurrent.ProdOrdID))
			{
				using (new InvtMultScope(LineCurrent))
				{
					e.NewValue = LineCurrent.InvtMult;
					e.Cancel = true;
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.ProdOrdID == LineCurrent.ProdOrdID))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || e.Row.ProdOrdID == LineCurrent.ProdOrdID))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMProdItemSplit, AMProdItemSplit.lotSerialNbr>.Updated.Args<string> e)
		{
			if(e.Row?.LotSerialNbr == null)
			{
				return;
			}

			var parent = (AMProdItem)PXParentAttribute.SelectParent(e.Cache, e.Row, typeof(AMProdItem));
			if(parent?.InventoryID == null)
			{
				return;
			}

			if(LineCache.GetStatus(parent) != PXEntryStatus.Inserted)
			{
				return;
			}

			if(!string.IsNullOrWhiteSpace(e.OldValue) && parent.LotSerialNbr == e.OldValue)
			{
				parent.LotSerialNbr = e.Row.LotSerialNbr;
			}
		}

		protected override void EventHandler(ManualEvent.Row<AMProdItemSplit>.Inserting.Args e)
		{
			// _Master_RowUpdated calls into CreateNumbers which does an insert for the new qty difference. This logic is a copy of Acumatica except removing the check on serial items so it will merge the insert into an update
			if (e.Row != null && !e.ExternalCall && CurrentOperation == PXDBOperation.Update)
			{
				foreach (var siblingSplit in SelectSplits(e.Row))
				{
					if (AreSplitsEqual(e.Row, siblingSplit))
					{
						var oldSiblingSplit = PXCache<AMProdItemSplit>.CreateCopy(siblingSplit);
#if DEBUG
						AMDebug.TraceWriteMethodName($"Changing insert to an update. Adding {e.Row.BaseQty} to {siblingSplit.BaseQty}");
#endif
#pragma warning disable PX1048
						siblingSplit.BaseQty += e.Row.BaseQty.GetValueOrDefault();
						siblingSplit.Qty = INUnitAttribute.ConvertFromBase(e.Cache, siblingSplit.InventoryID, siblingSplit.UOM, siblingSplit.BaseQty.GetValueOrDefault(), INPrecision.QUANTITY);
#pragma warning restore PX1048
						e.Cache.Current = siblingSplit;
						e.Cache.RaiseRowUpdated(siblingSplit, oldSiblingSplit);
						e.Cache.MarkUpdated(siblingSplit);
						e.Cancel = true;
						break;
					}
				}
			}

			if (e.Row != null && !e.Cancel && (e.Row.InventoryID == null || string.IsNullOrEmpty(e.Row.UOM)))
				e.Cancel = true;

			if (!e.Cancel)
				base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<AMProdItemSplit>.Verifying.Args<decimal?> e)
		{
			AMProdItemSplit split = e.Row;
			AMProdItem parent = LineCurrent;

			if (parent != null && parent.PreassignLotSerial == true)
			{
				PXResult<InventoryItem, INLotSerClass> result = base.ReadInventoryItem(((AMProdItemSplit)e.Row).InventoryID);
				if ((((result != null) && (((INLotSerClass)result).LotSerTrack == INLotSerTrack.SerialNumbered)) && (((INLotSerClass)result).LotSerAssign == INLotSerAssign.WhenReceived)) && (((e.NewValue != null) && (e.NewValue is decimal)) && ((((decimal)e.NewValue) != 0M) && (((decimal)e.NewValue) != 1M))))
				{
					e.NewValue = 1M;
				}
			}
			else
				e.NewValue = VerifySNQuantity(e.Cache, e.Row, e.NewValue, nameof(AMProdItemSplit.qty));
		}
		#endregion
		#region LotSerOptions
		protected override void EventHandler(ManualEvent.Row<LotSerOptions>.Selected.Args e)
		{
			AMProdItem item = LineCurrent;
			bool enableGenerate = (item != null && item.PreassignLotSerial == true);

			PXUIFieldAttribute.SetEnabled<LotSerOptions.startNumVal>(e.Cache, e.Row, enableGenerate);
			PXUIFieldAttribute.SetEnabled<LotSerOptions.qty>(e.Cache, e.Row, enableGenerate);
			PXDBDecimalAttribute.SetPrecision(e.Cache, e.Row, nameof(LotSerOptions.Qty), e.Row.IsSerial == true ? 0 : CommonSetupDecPl.Qty);
			generateNumbers.SetEnabled(enableGenerate);
		}
		#endregion
		#endregion

		#region UpdateParent Helper
		public override void UpdateParent(AMProdItem line)
		{
			if (line != null)
			{
				UpdateParent(line, null, null, out _);
			}
			else
			{
				base.UpdateParent(line);
			}
		}

		public override void UpdateParent(AMProdItemSplit newSplit, AMProdItemSplit oldSplit)
		{
			AMProdItemSplit anySplit = newSplit ?? oldSplit;
			AMProdItem parent = (AMProdItem)LSParentAttribute.SelectParent(SplitCache, newSplit ?? oldSplit, typeof(AMProdItem));

			if (parent != null)
			{
				if (anySplit != null && SameInventoryItem(anySplit, parent))
				{
					var oldParent = PXCache<AMProdItem>.CreateCopy(parent);
					using (new InvtMultScope(parent))
					{
						parent.UnassignedQty = 0m;
						if (newSplit != null && IsLotSerialItem(newSplit))
						{
							var hasLotSerialNbrQty = SelectSplits(newSplit).Where(s => s.LotSerialNbr != null).Sum(s => s.BaseQty) ?? 0m;
							parent.UnassignedQty = (parent.BaseQtytoProd - hasLotSerialNbrQty).NotLessZero();
						}
						else
						{
							base.UpdateParent(newSplit, oldSplit);
							return;
						}
					}

					LineCache.MarkUpdated(parent);
					LineCache.RaiseFieldUpdated(LineQtyField.Name, parent, oldParent.Qty);

					if (LineCache.RaiseRowUpdating(oldParent, parent))
						LineCache.RaiseRowUpdated(parent, oldParent);
					else
						PXCache<AMProdItem>.RestoreCopy(parent, oldParent);
				}
				return;
			}

			base.UpdateParent(newSplit, oldSplit);
		}

		public override void UpdateParent(AMProdItem line, AMProdItemSplit newSplit, AMProdItemSplit oldSplit, out decimal baseQty)
		{
			ResetAvailabilityCounters(line);

			bool counted = LineCounters.ContainsKey(line);

			base.UpdateParent(line, newSplit, oldSplit, out baseQty);

			if (!counted && oldSplit != null && LineCounters.TryGetValue(line, out Counters counters))
				baseQty = counters.BaseQty;
		}

		public static void ResetAvailabilityCounters(AMProdItem line)
		{
			line.LineQtyAvail = null;
			line.LineQtyHardAvail = null;
		}
		#endregion

		#region Create/Truncate/Update/Issue Numbers
		public override void UpdateNumbers(AMProdItem line)
		{
			if (line != null)
				LineCounters.Remove(line);

			foreach (AMProdItemSplit split in SelectSplits(line))
			{
				AMProdItemSplit newSplit = PXCache<AMProdItemSplit>.CreateCopy(split);

				if (line.LocationID == null && newSplit.LocationID != null && SplitCache.GetStatus(newSplit) == PXEntryStatus.Inserted && newSplit.Qty == 0m)
				{
					SplitCache.Delete(newSplit);
				}
				else
				{
					newSplit.SubItemID = line.SubItemID ?? newSplit.SubItemID;
					newSplit.SiteID = line.SiteID;
					newSplit.ExpireDate = ExpireDateByLot(newSplit, line);
					SplitCache.Update(newSplit);
				}
			}
		}
		#endregion

		public AMProdItemSplit[] GetSplits(AMProdItem line) => SelectSplits(line);

		// Copy logic from 19.105 LSSOLine / Remove previous set of INLotSerClass which was caching the changed value
		protected override PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(int? inventoryID)
		{
			var item = (InventoryItem)PXSelectorAttribute.Select<AMProdItem.inventoryID>(LineCache, null, inventoryID);

			if (item == null)
				return null;

			var lsclass = INLotSerClass.PK.Find(Base, item.LotSerClassID);
			return new PXResult<InventoryItem, INLotSerClass>(item, lsclass ?? new INLotSerClass());
		}
		public INLotSerClass GetLotSerClass(ILSMaster line) => ReadInventoryItem(line.InventoryID);

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			if (row is AMProdItem && ((AMProdItem)row).PreassignLotSerial != true)
				return INLotSerTrack.Mode.None;

			return base.GetTranTrackMode(row, lotSerClass);
		}

		public virtual bool IsLotSerialItem(ILSMaster line)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			if (item == null)
				return false;

			return INLotSerialNbrAttribute.IsTrack(item, line.TranType, line.InvtMult);
		}
	}
}
