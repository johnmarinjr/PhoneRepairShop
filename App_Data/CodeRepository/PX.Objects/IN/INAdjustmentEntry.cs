using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Services;

namespace PX.Objects.IN
{
	public class INAdjustmentEntry : INRegisterEntryBase
	{
		#region Views
		public
			PXSelect<INRegister,
			Where<INRegister.docType, Equal<INDocType.adjustment>>>
			adjustment;

		public
			PXSelect<INRegister,
			Where<
				INRegister.docType, Equal<INDocType.adjustment>,
				And<INRegister.refNbr, Equal<Current<INRegister.refNbr>>>>>
			CurrentDocument;

		[PXImport(typeof(INRegister))]
		public
			PXSelect<INTran,
			Where<
				INTran.docType, Equal<INDocType.adjustment>,
				And<INTran.refNbr, Equal<Current<INRegister.refNbr>>>>,
			OrderBy<Asc<INTran.docType, Asc<INTran.refNbr, Asc<INTran.sortOrder, Asc<INTran.lineNbr>>>>>>
			transactions;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INTranSplit,
			Where<
				INTranSplit.tranType, Equal<Argument<string>>,
				And<INTranSplit.refNbr, Equal<Argument<string>>,
				And<INTranSplit.lineNbr, Equal<Argument<Int16?>>>>>>
			splits;
		public virtual void Splits(
			[PXDBString(3, IsFixed = true)] ref string INTran_tranType,
			[PXDBString(10, IsUnicode = true)] ref string INTran_refNbr,
			[PXDBShort] ref Int16? INTran_lineNbr)
		{
			transactions.Current =
				PXSelect<INTran,
				Where<
					INTran.tranType, Equal<Required<INTran.tranType>>,
					And<INTran.refNbr, Equal<Required<INTran.refNbr>>,
					And<INTran.lineNbr, Equal<Required<INTran.lineNbr>>>>>>
				.Select(this, INTran_tranType, INTran_refNbr, INTran_lineNbr);
		}
		#endregion

		[InjectDependency]
		public IInventoryAccountService InventoryAccountService { get; set; }

		#region DAC overrides
		#region INTran
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Cost")]
		[PXFormula(typeof(Mult<INTran.qty, INTran.unitCost>), typeof(SumCalc<INRegister.totalCost>))]
		protected virtual void _(Events.CacheAttached<INTran.tranCost> e) { }

		[PXDefault(typeof(SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>>), SourceField = typeof(InventoryItem.baseUnit), CacheGlobal = true)]
		[INUnit(typeof(INTran.inventoryID))]
		protected virtual void _(Events.CacheAttached<INTran.uOM> e) { }

		[PXDBString(3, IsFixed = true)]
		[PXDefault(INTranType.Adjustment)]
		[PXUIField(Enabled = false, Visible = false)]
		protected virtual void _(Events.CacheAttached<INTran.tranType> e) { }

		[PXMergeAttributes]
		[PXRemoveBaseAttribute(typeof(PXRestrictorAttribute))]
		[PXRestrictor(typeof(Where<ReasonCode.usage, Equal<Optional<INTran.docType>>,
			Or<ReasonCode.usage, Equal<ReasonCodeUsages.vendorReturn>, And<Optional<INTran.origModule>, Equal<BatchModule.modulePO>>>>),
			Messages.ReasonCodeDoesNotMatch)]
		protected virtual void _(Events.CacheAttached<INTran.reasonCode> e) { }
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), true)]
		protected virtual void _(Events.CacheAttached<INTran.origRefNbr> e)
		{
		}


		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0", typeof(Coalesce<
			Search<INItemSite.tranUnitCost, Where<INItemSite.inventoryID, Equal<Current<INTran.inventoryID>>, And<INItemSite.siteID, Equal<Current<INTran.siteID>>>>>,
			Search<INItemCost.tranUnitCost, Where<INItemCost.inventoryID, Equal<Current<INTran.inventoryID>>, And<INItemCost.curyID, EqualBaseCuryID<Current2<INRegister.branchID>>>>>>))]
		[PXUIField(DisplayName = "Unit Cost")]
		protected virtual void _(Events.CacheAttached<INTran.unitCost> e) { }
		#endregion

		#region INTranSplit
		[LocationAvail(typeof(INTranSplit.inventoryID), typeof(INTranSplit.subItemID), typeof(INTranSplit.siteID), typeof(INTranSplit.tranType), typeof(INTranSplit.invtMult))]
		protected virtual void _(Events.CacheAttached<INTranSplit.locationID> e) { }
		#endregion
		#endregion

		#region Initialization
		public INAdjustmentEntry()
		{
			INSetup record = insetup.Current;

			PXUIFieldAttribute.SetVisible<INTran.tranType>(transactions.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INTran.tranType>(transactions.Cache, null, false);

			PXVerifySelectorAttribute.SetVerifyField<INTran.origRefNbr>(transactions.Cache, null, true);
		}
		#endregion

		#region Event Handlers
		#region INRegister
		protected virtual void _(Events.FieldDefaulting<INRegister, INRegister.docType> e) => e.NewValue = INDocType.Adjustment;

		protected virtual void _(Events.RowUpdated<INRegister> e)
		{
			if (insetup.Current.RequireControlTotal == false)
			{
				FillControlValue<INRegister.controlCost, INRegister.totalCost>(e.Cache, e.Row);
				FillControlValue<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
			else if (insetup.Current.RequireControlTotal == true && e.Row.Hold == false && e.Row.Released == false)
			{
				RaiseControlValueError<INRegister.controlCost, INRegister.totalCost>(e.Cache, e.Row);
				RaiseControlValueError<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (e.Row == null)
				return;

			bool allowUpdateDelete = e.Row.Released == false && e.Row.OrigModule != BatchModule.AP;
			//restriction added primary for Landed Cost
			bool allowInsertDeleteDetails = e.Row.OrigModule != BatchModule.PO;
			bool isPIAdjustment = !string.IsNullOrEmpty(e.Row.PIID);

			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, e.Row.Released == false);
			PXUIFieldAttribute.SetEnabled<INRegister.totalQty>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.totalCost>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.status>(e.Cache, e.Row, false);

			e.Cache.AllowInsert = true;
			e.Cache.AllowUpdate = allowUpdateDelete;
			e.Cache.AllowDelete = allowUpdateDelete && allowInsertDeleteDetails;

			transactions.Cache.AllowInsert = allowUpdateDelete && allowInsertDeleteDetails && !isPIAdjustment;
			transactions.Cache.AllowUpdate = allowUpdateDelete;
			transactions.Cache.AllowDelete = allowUpdateDelete && allowInsertDeleteDetails && !isPIAdjustment;

			splits.Cache.AllowInsert = allowUpdateDelete;
			splits.Cache.AllowUpdate = allowUpdateDelete;
			splits.Cache.AllowDelete = allowUpdateDelete;

			PXUIFieldAttribute.SetVisible<INRegister.controlQty>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetVisible<INRegister.controlCost>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetVisible<INRegister.pIID>(e.Cache, e.Row, !string.IsNullOrEmpty(e.Row.PIID));

			release.SetEnabled(e.Row.Hold == false && e.Row.Released == false);
		}

		protected virtual void _(Events.RowPersisting<INRegister> e)
		{
			if (e.Operation.Command() == PXDBOperation.Delete && !string.IsNullOrEmpty(e.Row?.PIID))
			{
				// Acuminator disable once PX1043 SavingChangesInEventHandlers
				// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers
				// Acuminator disable once PX1071 PXActionExecutionInEventHandlers
				CreateInstance<INPIController>().ReopenPI(e.Row.PIID);
			}
		}
		#endregion

		#region INTran
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.docType> e) => e.NewValue = INDocType.Adjustment;

		protected virtual void _(Events.FieldUpdated<INTran, INTran.unitCost> e) => SetupManualCostFlag(e.Cache, e.Row, e.ExternalCall);
		protected virtual void _(Events.FieldUpdated<INTran, INTran.tranCost> e) => SetupManualCostFlag(e.Cache, e.Row, e.ExternalCall);

		protected virtual void _(Events.FieldUpdated<INTran, INTran.lotSerialNbr> e) => DefaultUnitCost(e.Cache, e.Row);
		protected virtual void _(Events.FieldUpdated<INTran, INTran.origRefNbr> e) => DefaultUnitCost(e.Cache, e.Row);

		protected virtual void _(Events.FieldVerifying<INTran, INTran.origRefNbr> e)
		{
			if (e.Row?.TranType == INTranType.ReceiptCostAdjustment)
				e.Cancel = true;
		}

		protected virtual void _(Events.RowSelected<INTran> e)
		{
			InventoryItem item = InventoryItem.PK.Find(this, e.Row?.InventoryID);
			bool noItemOrNonStandard = item == null || item.ValMethod != INValMethod.Standard;

			bool isPIAdjustment = !string.IsNullOrEmpty(adjustment.Current?.PIID);
			bool isFIFOCreditLineFromPI = isPIAdjustment && e.Row?.Qty < 0 && item?.ValMethod == INValMethod.FIFO;
			bool isDebitLineFromPI = isPIAdjustment && e.Row?.Qty > 0;

			PXUIFieldAttribute.SetEnabled<INTran.branchID>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.inventoryID>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.siteID>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.locationID>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.qty>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.uOM>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.unitCost>(e.Cache, e.Row, !isPIAdjustment && noItemOrNonStandard || isDebitLineFromPI);
			PXUIFieldAttribute.SetEnabled<INTran.tranCost>(e.Cache, e.Row, !isPIAdjustment || isDebitLineFromPI);
			PXUIFieldAttribute.SetEnabled<INTran.expireDate>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.origRefNbr>(e.Cache, e.Row, !isPIAdjustment || isFIFOCreditLineFromPI);
			PXUIFieldAttribute.SetEnabled<INTran.reasonCode>(e.Cache, e.Row, !isPIAdjustment);
			PXUIFieldAttribute.SetEnabled<INTran.tranDesc>(e.Cache, e.Row, !isPIAdjustment);

			// To force field disabling as INLotSerialNbrAttribute overrides enable\disable logic of PXUIFieldAttribute
			e.Cache.Adjust<INLotSerialNbrAttribute>(e.Row).For<INTran.lotSerialNbr>(a => a.ForceDisable = isPIAdjustment);

			PXUIFieldAttribute.SetVisible<INTran.manualCost>(e.Cache, null, isPIAdjustment);
			PXUIFieldAttribute.SetVisible<INTran.pILineNbr>(e.Cache, null, isPIAdjustment);
		}

		protected virtual void _(Events.RowUpdated<INTran> e)
		{
			InventoryItem item = InventoryItem.PK.Find(this, e.Row?.InventoryID);

			if (item?.ValMethod == INValMethod.Standard && e.Row.TranType == INTranType.Adjustment && e.Row.InvtMult != 0 && e.Row.BaseQty == 0m && e.Row.TranCost != 0m)
				e.Cache.RaiseExceptionHandling<INTran.tranCost>(e.Row, e.Row.TranCost, new PXSetPropertyException(Messages.StandardCostNoCostOnlyAdjust));
		}

		protected virtual void _(Events.RowPersisting<INTran> e)
		{
			if (e.Operation.Command() == PXDBOperation.Delete)
				return;

			InventoryItem item = InventoryItem.PK.Find(this, e.Row?.InventoryID);
			INLotSerClass lotSerClass = INLotSerClass.PK.Find(this, item?.LotSerClassID);

			PXPersistingCheck check =
				e.Row.InvtMult != 0 &&
					(item?.ValMethod == INValMethod.Specific ||
					(lotSerClass != null &&
					lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered &&
					lotSerClass.LotSerAssign == INLotSerAssign.WhenReceived &&
					e.Row.Qty != 0m))
				 ? PXPersistingCheck.NullOrBlank
				 : PXPersistingCheck.Nothing;


			PXDefaultAttribute.SetPersistingCheck<INTran.subID>(e.Cache, e.Row, PXPersistingCheck.Null);
			PXDefaultAttribute.SetPersistingCheck<INTran.locationID>(e.Cache, e.Row, PXPersistingCheck.Null);
			PXDefaultAttribute.SetPersistingCheck<INTran.lotSerialNbr>(e.Cache, e.Row, check);

			if (adjustment.Current != null && adjustment.Current.OrigModule != INRegister.origModule.PI && item != null && item.ValMethod == INValMethod.FIFO && e.Row.OrigRefNbr == null)
			{
				bool dropShipPO = false;
				if (e.Row != null && e.Row.POReceiptNbr != null && e.Row.POReceiptLineNbr != null)
				{
					PO.POReceiptLine pOReceiptLine = PO.POReceiptLine.PK.Find(this, e.Row.POReceiptType, e.Row.POReceiptNbr, e.Row.POReceiptLineNbr);
					dropShipPO = pOReceiptLine != null && pOReceiptLine.LineType.IsIn(PO.POLineType.GoodsForDropShip, PO.POLineType.NonStockForDropShip);
				}

				if (!dropShipPO && e.Cache.RaiseExceptionHandling<INTran.origRefNbr>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(INTran.origRefNbr)}]")))
				{
					throw new PXRowPersistingException(typeof(INTran.origRefNbr).Name, null, ErrorMessages.FieldIsEmpty, typeof(INTran.origRefNbr).Name);
				}
			}

			if (item?.ValMethod == INValMethod.Standard && e.Row.TranType == INTranType.Adjustment && e.Row.InvtMult != 0 && e.Row.BaseQty == 0m && e.Row.TranCost != 0m)
			{
				if (e.Cache.RaiseExceptionHandling<INTran.tranCost>(e.Row, e.Row.TranCost, new PXSetPropertyException(Messages.StandardCostNoCostOnlyAdjust)))
				{
					throw new PXRowPersistingException(typeof(INTran.tranCost).Name, e.Row.TranCost, Messages.StandardCostNoCostOnlyAdjust);
				}
			}
		}
		#endregion
		#endregion

		

		protected virtual void SetupManualCostFlag(PXCache inTranCache, INTran tran, bool externalCall)
		{
			if (string.IsNullOrEmpty(adjustment.Current?.PIID) || !externalCall)
				return;

			if (tran == null || tran.Qty <= 0 || tran.ManualCost == true)
				return;

			inTranCache.SetValueExt<INTran.manualCost>(tran, true);
		}

		public override void DefaultUnitCost(PXCache cache, INTran tran, bool setZero = false)
		{
			if ((adjustment.Current != null && adjustment.Current.OrigModule == INRegister.origModule.PI)
				|| tran?.InventoryID == null)
				return;

			object UnitCost = null;

			InventoryItem item = InventoryItem.PK.Find(this, tran?.InventoryID);

			if (item.ValMethod == INValMethod.Specific && string.IsNullOrEmpty(tran.LotSerialNbr) == false)
			{
				INCostStatus status =
					SelectFrom<INCostStatus>.
					LeftJoin<INLocation>.On<INLocation.locationID.IsEqual<INTran.locationID.FromCurrent>>.
					InnerJoin<INCostSubItemXRef>.On<INCostSubItemXRef.costSubItemID.IsEqual<INCostStatus.costSubItemID>>.
					Where<
						INCostStatus.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>.
						And<INCostSubItemXRef.subItemID.IsEqual<INTran.subItemID.FromCurrent>>.
						And<INCostStatus.lotSerialNbr.IsEqual<INTran.lotSerialNbr.FromCurrent>>.
						And<
							INLocation.isCosted.IsEqual<False>.
							And<INCostStatus.costSiteID.IsEqual<INTran.siteID.FromCurrent>>.
							Or<INCostStatus.costSiteID.IsEqual<INTran.locationID.FromCurrent>>>>.
					View.SelectSingleBound(this, new object[] { tran });
				if (status != null && status.QtyOnHand != 0m)
				{
					UnitCost = PXDBPriceCostAttribute.Round((decimal)(status.TotalCost / status.QtyOnHand));
				}
			}
			else if (item.ValMethod == INValMethod.FIFO && string.IsNullOrEmpty(tran.OrigRefNbr) == false)
			{
				INCostStatus status =
					SelectFrom<INCostStatus>.
					LeftJoin<INLocation>.On<INLocation.locationID.IsEqual<INTran.locationID.FromCurrent>>.
					InnerJoin<INCostSubItemXRef>.On<INCostSubItemXRef.costSubItemID.IsEqual<INCostStatus.costSubItemID>>.
					Where<
						INCostStatus.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>.
						And<INCostSubItemXRef.subItemID.IsEqual<INTran.subItemID.FromCurrent>>.
						And<INCostStatus.receiptNbr.IsEqual<INTran.origRefNbr.FromCurrent>>.
						And<
							INLocation.isCosted.IsEqual<False>.
							And<INCostStatus.costSiteID.IsEqual<INTran.siteID.FromCurrent>>.
							Or<INCostStatus.costSiteID.IsEqual<INTran.locationID.FromCurrent>>>>.
					View.SelectSingleBound(this, new object[] { tran });
				if (status != null && status.QtyOnHand != 0m)
				{
					UnitCost = PXDBPriceCostAttribute.Round((decimal)(status.TotalCost / status.QtyOnHand));
				}
			}
			else
			{
				if (item.ValMethod == INValMethod.Average)
				{
					cache.RaiseFieldDefaulting<INTran.avgCost>(tran, out UnitCost);
				}
				if (UnitCost == null || (decimal)UnitCost == 0m)
				{
					cache.RaiseFieldDefaulting<INTran.unitCost>(tran, out UnitCost);
				}
			}


			decimal? qty = (decimal?)cache.GetValue<INTran.qty>(tran);

			if (UnitCost != null && ((decimal)UnitCost != 0m || setZero || qty < 0m))
			{
				if ((decimal)UnitCost < 0m)
					cache.RaiseFieldDefaulting<INTran.unitCost>(tran, out UnitCost);

				decimal? unitcost = INUnitAttribute.ConvertToBase<INTran.inventoryID>(cache, tran, tran.UOM, (decimal)UnitCost, INPrecision.UNITCOST);

				//suppress trancost recalculation for cost only adjustments
				if (qty == 0m)
				{
					cache.SetValue<INTran.unitCost>(tran, unitcost);
				}
				else
				{
					cache.SetValueExt<INTran.unitCost>(tran, unitcost);
				}
			}
		}

		#region INRegisterBaseEntry implementation
		public override PXSelectBase<INRegister> INRegisterDataMember => adjustment;
		public override PXSelectBase<INTran> INTranDataMember => transactions;
		public override PXSelectBase<INTran> LSSelectDataMember => LineSplittingExt.lsselect;
		public override PXSelectBase<INTranSplit> INTranSplitDataMember => splits;
		protected override string ScreenID => "IN303000";
		#endregion

		#region SiteStatus Lookup
		public class SiteStatusLookup : SiteStatusLookupExt<INAdjustmentEntry>
		{
			protected override bool IsAddItemEnabled(INRegister doc) => Transactions.AllowDelete;
		}
		#endregion

		#region LineSplitting
		public LineSplittingExtension LineSplittingExt => FindImplementation<LineSplittingExtension>();

		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LineSplittingExtension : GraphExtensions.INRegisterLineSplittingExtension<INAdjustmentEntry>
		{
			protected override void SubscribeForLineEvents()
			{
				base.SubscribeForLineEvents();
				ManualEvent.FieldOf<INTran, INTran.uOM>.Verifying.Subscribe<string>(Base, EventHandler);
			}

			public virtual void EventHandler(ManualEvent.FieldOf<INTran, INTran.uOM>.Verifying.Args<string> e)
			{
				if (e.Row.InventoryID == null)
					return;

				(var item, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
				if (INLotSerialNbrAttribute.IsTrackSerial(lsClass, e.Row.TranType, e.Row.InvtMult))
				{
					e.Cache.RaiseFieldDefaulting<INTran.uOM>(e.Row, out object defaultValue);

					if (object.Equals(defaultValue, e.NewValue) == false)
					{
						e.NewValue = (string)defaultValue;
						e.Cache.RaiseExceptionHandling<INTran.uOM>(e.Row, null, new PXSetPropertyException(Messages.SerialItemAdjustment_UOMUpdated, PXErrorLevel.Warning, defaultValue));
					}
				}
			}

			public override void EventHandlerQty(ManualEvent.FieldOf<INTran>.Verifying.Args<decimal?> e)
			{
				if (e.Row.InventoryID == null)
					return;

				(var item, var lsClass) = ReadInventoryItem(e.Row.InventoryID);
				if (INLotSerialNbrAttribute.IsTrackSerial(lsClass, e.Row.TranType, e.Row.InvtMult))
				{
					if (e.NewValue.IsNotIn(null, 0m, 1m, -1m))
					{
						e.NewValue = e.NewValue > 0 ? 1m : -1m;
						e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, null, new PXSetPropertyException(Messages.SerialItemAdjustment_LineQtyUpdated, PXErrorLevel.Warning, item.BaseUnit));
					}
				}
			}

			public override void CreateNumbers(INTran line, decimal deltaBaseQty, bool forceAutoNextNbr)
			{
				if (line.InventoryID == null)
					return;

				(var _, var lsClass) = ReadInventoryItem(line.InventoryID);

				if (lsClass.LotSerTrack != INLotSerTrack.NotNumbered && lsClass.LotSerAssign != INLotSerAssign.WhenUsed && (line.SubItemID == null || line.LocationID == null))
					return;

				base.CreateNumbers(line, deltaBaseQty, forceAutoNextNbr);
			}

			public override void IssueNumbers(INTran line, decimal deltaBaseQty)
			{
				if (line.InventoryID == null)
					return;

				(var _, var lsClass) = ReadInventoryItem(line.InventoryID);

				if (lsClass.LotSerTrack != INLotSerTrack.NotNumbered && lsClass.LotSerAssign != INLotSerAssign.WhenUsed && (line.LotSerialNbr == null || line.SubItemID == null || line.LocationID == null))
					return;

				base.IssueNumbers(line, deltaBaseQty);
			}
		}
		#endregion

		#region ItemAvailability
		public ItemAvailabilityExtension ItemAvailabilityExt => FindImplementation<ItemAvailabilityExtension>();

		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ItemAvailabilityExtension : GraphExtensions.INRegisterItemAvailabilityExtension<INAdjustmentEntry> { }
		#endregion
	}

	public class INAdjustmentEntrySplit : PXGraphExtension<INAdjustmentEntry>
    {
		public PXSetup<CommonSetup> setup;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.inventory>();
		}

		public override void Initialize()
		{
			base.Initialize();
			Base.OnBeforePersist += BaseOnBeforePersist;
		}

		public PXAction<INRegister> split;
		[PXButton(CommitChanges = true, VisibleOnDataSource = false)]
		[PXUIField(DisplayName = "Split", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		public virtual IEnumerable Split(PXAdapter adapter)
		{
			if (Base.transactions.Current != null)
            {
				INTran newLine = SplitTransaction(Base.transactions.Current);
				if (newLine != null)
				{
					newLine.SortOrder++;

					foreach (INTran tran in Base.transactions.Select())
					{
						if (tran.SortOrder >= newLine.SortOrder)
						{
							Base.transactions.Cache.SetValue<INTran.sortOrder>(tran, tran.SortOrder + 1);
							Base.transactions.Cache.MarkUpdated(tran);
						}
					}

					InsertNewSplit(newLine);
				}
			}

			return adapter.Get();
		}

		public virtual INTran SplitTransaction(INTran source)
        {
			INTran copy = (INTran) Base.transactions.Cache.CreateCopy(source);
			copy.LineNbr = null;
			copy.Qty = 0;
			copy.BaseQty = 0;
			copy.NoteID = null;
			copy.TranCost = null;

			return copy;
        }

		public virtual INTran InsertNewSplit(INTran newLine)
        {
			return Base.transactions.Insert(newLine);
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
        {
			if (e.Row != null)
            {
				bool isPIAdjustment = !string.IsNullOrEmpty(e.Row.PIID);
				if (isPIAdjustment)
				{
					Base.transactions.Cache.AllowDelete = Base.transactions.Cache.AllowUpdate;
				}
				split.SetVisible(isPIAdjustment);
				split.SetEnabled(isPIAdjustment);
			}
        }
				
		protected virtual void _(Events.RowSelected<INTran> e)
		{
			PXUIFieldAttribute.SetEnabled<INTran.qty>(e.Cache, e.Row, true);
		}

		protected virtual void _(Events.RowDeleting<INTran> e)
		{
			bool isPIAdjustment = !string.IsNullOrEmpty(e.Row.PIID);
			if (isPIAdjustment)
			{
				if (Base.adjustment.Cache.GetStatus(Base.adjustment.Current) != PXEntryStatus.Deleted)
				{
					e.Cancel = e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted &&
					e.Cache.GetStatus(e.Row) != PXEntryStatus.InsertedDeleted;
				}
			}
		}


		protected virtual void BaseOnBeforePersist(PXGraph obj)
		{
			bool isPIAdjustment = !string.IsNullOrEmpty(Base.adjustment.Current?.PIID);

			if (isPIAdjustment)
			{
				ValidateQtyVariance();
			}
		}
		
		protected virtual void ValidateQtyVariance()
		{
			bool error = false;

			var groupedByLineHavingSplits = Base.transactions.Select().RowCast<INTran>().ToList()
				.GroupBy(x => x.PILineNbr)
				.Where(g => g.Count() > 1);

			foreach (var group in groupedByLineHavingSplits)
			{
				decimal total = group.Sum(x => x.Qty.GetValueOrDefault());
				INPIDetail detail = INPIDetail.PK.Find(Base, Base.adjustment.Current.PIID, group.Key);

				if (detail.VarQty != total)
				{
					error = true;

					foreach (INTran tran in group)
					{
						Base.transactions.Cache.RaiseExceptionHandling<INTran.qty>(tran, tran.Qty,
							new PXSetPropertyException(Messages.QtyVariance,
							tran.PILineNbr,
							decimal.Round(total, setup.Current.DecPlQty.GetValueOrDefault(2)),
							decimal.Round(detail.VarQty.GetValueOrDefault(), setup.Current.DecPlQty.GetValueOrDefault(2))));
					}
				}
			}

			if (error)
				throw new PXException(Messages.AdjustmentError);
		}
	}
}
