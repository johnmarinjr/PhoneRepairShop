using System;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class SOInvoiceLineSplittingExtension : IN.GraphExtensions.LineSplittingExtension<SOInvoiceEntry, ARInvoice, ARTran, ARTranAsSplit>
	{
		#region Configuration
		protected override Type SplitsToDocumentCondition => typeof(
			ARTranAsSplit.tranType.IsEqual<ARInvoice.docType.FromCurrent>.
			And<ARTranAsSplit.refNbr.IsEqual<ARInvoice.refNbr.FromCurrent>>.
			And<ARTranAsSplit.lineType.IsNotIn<SOLineType.freight, SOLineType.discount>>);

		protected override Type LineQtyField => typeof(ARTran.qty);

		public override ARTranAsSplit LineToSplit(ARTran line)
		{
			using (new InvtMultScope(line))
			{
				ARTranAsSplit ret = ARTranAsSplit.FromARTran(line);
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = line.BaseQty - line.UnassignedQty;
				return ret;
			}
		}
		#endregion

		#region Event Handlers
		#region ARTran
		protected override void SubscribeForLineEvents()
		{
			base.SubscribeForLineEvents();
			ManualEvent.FieldOf<ARTran, ARTran.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<ARTran, ARTran.uOM>.Verifying.Subscribe<string>(Base, EventHandler);
		}

		public virtual void EventHandler(ManualEvent.FieldOf<ARTran, ARTran.locationID>.Defaulting.Args<int?> e)
		{
			if (e.Row != null && (e.Row.InvtMult == 0 || e.Row.LineType != SOLineType.Inventory))
				e.Cancel = true;
		}

		public virtual void EventHandler(ManualEvent.FieldOf<ARTran, ARTran.uOM>.Verifying.Args<string> e)
		{
			if (e.Row.InvtMult == 0)
				return;

			PXResult<InventoryItem, INLotSerClass> item = base.ReadInventoryItem(e.Row.InventoryID);
			string inTranType = INTranType.TranTypeFromInvoiceType(e.Row.TranType, e.Row.Qty);
			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, inTranType, e.Row.InvtMult))
			{
				e.Cache.RaiseFieldDefaulting<ARTran.uOM>(e.Row, out object defaultValue);

				if (object.Equals(defaultValue, e.NewValue) == false)
				{
					e.NewValue = (string)defaultValue;
					e.Cache.RaiseExceptionHandling<ARTran.uOM>(e.Row, null, new PXSetPropertyException(IN.Messages.SerialItemAdjustment_UOMUpdated, PXErrorLevel.Warning, defaultValue));
				}
			}
		}

		protected override void EventHandler(ManualEvent.Row<ARTran>.Selected.Args e)
		{
			base.EventHandler(e);

			if (e.Row == null)
				return;

			bool
				directLine = e.Row.InvtMult != 0,
				directStockLine = directLine && (e.Row.LineType == SOLineType.Inventory);
			PXUIFieldAttribute.SetEnabled<ARTran.subItemID>(e.Cache, e.Row, directStockLine);
			PXUIFieldAttribute.SetEnabled<ARTran.siteID>(e.Cache, e.Row, directLine);
			PXUIFieldAttribute.SetEnabled<ARTran.locationID>(e.Cache, e.Row, directStockLine);

			PXPersistingCheck checkValues = directStockLine ? PXPersistingCheck.Null : PXPersistingCheck.Nothing;
			PXDefaultAttribute.SetPersistingCheck<ARTran.subItemID>(e.Cache, e.Row, checkValues);
			PXDefaultAttribute.SetPersistingCheck<ARTran.siteID>(e.Cache, e.Row, checkValues);
			PXDefaultAttribute.SetPersistingCheck<ARTran.locationID>(e.Cache, e.Row, checkValues);
		}

		protected override void EventHandler(ManualEvent.Row<ARTran>.Inserted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<ARTran>.Deleted.Args e)
		{
			if (e.Row.InvtMult != 0)
				base.EventHandler(e);
		}

		protected override void EventHandler(ManualEvent.Row<ARTran>.Updated.Args e)
		{
			if (e.Row.InvtMult != 0)
			{
				if (e.Row.TranType != e.OldRow.TranType)
					e.Cache.SetDefaultExt<ARTran.invtMult>(e.Row);

				base.EventHandler(e);
			}
		}

		protected override void EventHandler(ManualEvent.Row<ARTran>.Persisting.Args e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (Math.Abs(e.Row.BaseQty.Value) >= 0.0000005m && (e.Row.UnassignedQty >= 0.0000005m || e.Row.UnassignedQty <= -0.0000005m))
					if (e.Cache.RaiseExceptionHandling<ARTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
						throw new PXRowPersistingException(typeof(ARTran.qty).Name, e.Row.Qty, Messages.BinLotSerialNotAssigned);

				Base.FindImplementation<SOInvoiceItemAvailabilityExtension>()?.MemoOrderCheck(e.Row);
			}

			base.EventHandler(e);
		}

		public override void EventHandlerQty(ManualEvent.FieldOf<ARTran>.Verifying.Args<decimal?> e)
		{
			if (e.Row.InvtMult == 0)
				return;

			if (e.Row.InventoryID == null)
				return;

			(var item, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
			string inTranType = INTranType.TranTypeFromInvoiceType(e.Row.TranType, e.Row.Qty);
			if (INLotSerialNbrAttribute.IsTrackSerial(lsClass, inTranType, e.Row.InvtMult))
			{
				if (e.NewValue.IsNotIn(null, 0m, 1m, -1m))
				{
					e.NewValue = e.NewValue.Value > 0 ? 1m : -1m;
					e.Cache.RaiseExceptionHandling<ARTran.qty>(e.Row, null,
						new PXSetPropertyException(IN.Messages.SerialItemAdjustment_LineQtyUpdated, PXErrorLevel.Warning, item.BaseUnit));
				}
			}
		}

		#endregion
		#region ARTranAsSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.invtMult>.Defaulting.Subscribe<short?>(Base, EventHandler);
			ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.subItemID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.locationID>.Defaulting.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.lotSerialNbr>.Defaulting.Subscribe<string>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.invtMult>.Defaulting.Args<short?> e)
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

		protected virtual void EventHandler(ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.subItemID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.SubItemID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.locationID>.Defaulting.Args<int?> e)
		{
			if (LineCurrent != null && (e.Row == null || LineCurrent.LineNbr == e.Row.LineNbr))
			{
				e.NewValue = LineCurrent.LocationID;
				e.Cancel = true;
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<ARTranAsSplit, ARTranAsSplit.lotSerialNbr>.Defaulting.Args<string> e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				if (e.Row.InvtMult == null)
					e.Cache.RaiseFieldDefaulting<ARTranAsSplit.invtMult>(e.Row, out _);

				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (ARTranAsSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<ARTranAsSplit>(e.Cache, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}
		#endregion
		#endregion

		#region Create/Truncate/Update/Issue Numbers
		public override void CreateNumbers(ARTran line, decimal deltaBaseQty, bool forceAutoNextNbr)
		{
			if (!ShouldSkipLotSerailNbrCreation(line))
				base.CreateNumbers(line, deltaBaseQty, forceAutoNextNbr);
		}

		public override void IssueNumbers(ARTran line, decimal deltaBaseQty)
		{
			if (!ShouldSkipLotSerailNbrCreation(line))
				base.IssueNumbers(line, deltaBaseQty);
		}

		protected virtual bool ShouldSkipLotSerailNbrCreation(ARTran line)
		{
			if (line.SubItemID != null && line.LocationID != null)
				return false;

			(var _, var lsClass) = ReadInventoryItem(line.InventoryID);
			return lsClass.LotSerTrack != INLotSerTrack.NotNumbered && lsClass.LotSerAssign == INLotSerAssign.WhenReceived;
		}
		#endregion

		#region Select LotSerial Status
		protected override PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(ARTran line, PXResult<InventoryItem, INLotSerClass> item)
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

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, ARTran line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			else
				cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();

			if (!string.IsNullOrEmpty(line.LotSerialNbr))
				cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			else if (lotSerClass.IsManualAssignRequired == true)
				cmd.WhereAnd<Where<True.IsEqual<False>>>();
		}
		#endregion

		public override void UpdateParent(ARTran line, ARTranAsSplit newSplit, ARTranAsSplit oldSplit, out decimal baseQty)
		{
			ARTran oldRow = Clone(line);

			base.UpdateParent(line, newSplit, oldSplit, out baseQty);

			if (!LineCache.ObjectsEqual<ARTran.subItemID, ARTran.locationID, ARTran.lotSerialNbr, ARTran.expireDate>(oldRow, line))
				ARTranPlanIDAttribute.RaiseRowUpdated(LineCache, line, oldRow);
		}

		protected override INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			string inTranType = INTranType.TranTypeFromInvoiceType(row.TranType, row.Qty);
			return INLotSerialNbrAttribute.TranTrackMode(lotSerClass, inTranType, row.InvtMult);
		}

		public class ARLotSerialNbrAttribute : INLotSerialNbrAttribute
		{
			public ARLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType)
				: base(InventoryType, SubItemType, LocationType)
			{
			}

			protected override bool IsTracked(ILSMaster row, INLotSerClass lotSerClass, string tranType, int? invMult)
			{
				string inTranType = INTranType.TranTypeFromInvoiceType(tranType, row.Qty);
				return invMult != 0 && base.IsTracked(row, lotSerClass, inTranType, invMult);
			}
		}

		public class ARExpireDateAttribute : INExpireDateAttribute
		{
			public ARExpireDateAttribute(Type InventoryType)
				: base(InventoryType)
			{
			}

			protected override bool IsTrackExpiration(PXCache sender, ILSMaster row)
			{
				return row.InvtMult != 0 && base.IsTrackExpiration(sender, row);
			}
		}
	}
}
