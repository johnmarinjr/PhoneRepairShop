using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.CS;

using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;

using LotSerOptions = PX.Objects.IN.LSSelect.LotSerOptions;
using Counters = PX.Objects.IN.LSSelect.Counters;

namespace PX.Objects.IN.GraphExtensions
{
	public abstract class LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
		where TPrimary : class, IBqlTable, new()
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		#region State
		public bool IsCorrectionMode => lsselect.AllowUpdate && lsselect.AllowInsert == false && lsselect.AllowDelete == false;
		public bool IsFullMode => lsselect.AllowUpdate && lsselect.AllowInsert && lsselect.AllowDelete;

		protected PXDBOperation CurrentOperation = PXDBOperation.Normal;

		protected Counters CurrentCounters;
		protected readonly Dictionary<TLine, Counters> LineCounters = new Dictionary<TLine, Counters>();

		private BqlCommand _splitByLotSerialStatus;
		protected BqlCommand SplitByLotSerialStatusCommand
		{
			get => _splitByLotSerialStatus ??
				(_splitByLotSerialStatus = BqlTemplate.OfCommand<
					Select<TSplit,
					Where<
							BqlPlaceholder.I, Equal<Required<BqlPlaceholder.I>>,
						And<BqlPlaceholder.S, Equal<Required<BqlPlaceholder.S>>,
						And<BqlPlaceholder.W, Equal<Required<BqlPlaceholder.W>>,
						And<BqlPlaceholder.L, Equal<Required<BqlPlaceholder.L>>,
						And<BqlPlaceholder.N, Equal<Required<BqlPlaceholder.N>>,
						And<BqlPlaceholder.D>>>>>>>>
				.Replace<BqlPlaceholder.I>(SplitInventoryField)
				.Replace<BqlPlaceholder.S>(SplitSubItemField)
				.Replace<BqlPlaceholder.W>(SplitSiteField)
				.Replace<BqlPlaceholder.L>(SplitLocationField)
				.Replace<BqlPlaceholder.N>(SplitLotSerialNbrField)
				.Replace<BqlPlaceholder.D>(SplitsToDocumentCondition)
				.ToCommand());
		}

		/// <summary>
		/// Indicates that logic specific to UI is suppressed.
		/// Use <see cref="ForceUnattendedModeScope(bool)"/> to force this suppression for a code section.
		/// </summary>
		public bool UnattendedMode { get; private set; }

		/// <summary>
		/// Indicates that major internal logic is suppressed.
		/// Use <see cref="SuppressedModeScope(PXDBOperation?)"/> or <see cref="SuppressedModeScope(bool)"/> to activate suppression for a code section.
		/// </summary>
		public bool SuppressedMode { get; private set; }

		protected virtual ItemAvailabilityExtension<TGraph, TLine, TSplit> Availability
			=> Base.FindImplementation<ItemAvailabilityExtension<TGraph, TLine, TSplit>>();
		#endregion

		#region Configuration
		/// <summary>
		/// The condition of the <see cref="IBqlUnary"/> type
		/// that describes the rule of selecting <typeparamref name="TSplit"/> entities
		/// for a given (<see cref="Current{Field}"/>) <typeparamref name="TPrimary"/> entity.
		/// Consider using a <typeparamref name="TSplit"/>.FK.<typeparamref name="TPrimary"/>.SameAsCurrent condition.
		/// </summary>
		protected abstract Type SplitsToDocumentCondition { get; }

		public abstract TSplit LineToSplit(TLine line);
		protected virtual void SetEditMode() { }

		#region Line Fields Map
		/// <summary>
		/// The InventoryID field of the <typeparamref name="TLine"/> entity.
		/// </summary>
		protected virtual Type LineInventoryField { get; private set; }

		/// <summary>
		/// The Qty field of the <typeparamref name="TLine"/> entity.
		/// </summary>
		protected virtual Type LineQtyField { get; private set; }

		protected virtual bool TryInferLineFieldFromAttribute(PXEventSubscriberAttribute attr)
		{
			if (LineInventoryField == null && attr is BaseInventoryAttribute)
			{
				LineInventoryField = LineCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (LineQtyField == null && attr.FieldName.Equals(nameof(ILSMaster.Qty), StringComparison.OrdinalIgnoreCase))
			{
				LineQtyField = LineCache.GetBqlField(attr.FieldName);
				return true;
			}

			return false;
		}
		#endregion

		#region Split Fields Map
		/// <summary>
		/// The InventoryID field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitInventoryField { get; private set; }

		/// <summary>
		/// The SubItemID field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitSubItemField { get; private set; }

		/// <summary>
		/// The SiteID field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitSiteField { get; private set; }

		/// <summary>
		/// The LocationID field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitLocationField { get; private set; }

		/// <summary>
		/// The LotSerialNbr field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitLotSerialNbrField { get; private set; }

		/// <summary>
		/// The UOM field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitUomField { get; private set; }

		/// <summary>
		/// The Qty field of the <typeparamref name="TSplit"/> entity.
		/// </summary>
		protected virtual Type SplitQtyField { get; private set; }

		protected virtual bool TryInferSplitFieldFromAttribute(PXEventSubscriberAttribute attr)
		{
			if (SplitInventoryField == null && attr is BaseInventoryAttribute)
			{
				SplitInventoryField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitSubItemField == null && attr is SubItemAttribute)
			{
				SplitSubItemField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitSiteField == null && attr is SiteAttribute)
			{
				SplitSiteField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitLocationField == null && attr is LocationAttribute)
			{
				SplitLocationField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitLotSerialNbrField == null && attr is INLotSerialNbrAttribute)
			{
				SplitLotSerialNbrField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitUomField == null && attr is INUnitAttribute)
			{
				SplitUomField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			if (SplitQtyField == null && attr is PXDBQuantityAttribute qAttr && qAttr.KeyField != null)
			{
				SplitQtyField = SplitCache.GetBqlField(attr.FieldName);
				return true;
			}

			return false;
		}
		#endregion
		#endregion

		#region Initialization
		public override void Initialize()
		{
			UnattendedMode = Base.UnattendedMode;

			foreach (PXEventSubscriberAttribute attr in LineCache.GetAttributesReadonly(null))
				TryInferLineFieldFromAttribute(attr);

			foreach (PXEventSubscriberAttribute attr in SplitCache.GetAttributesReadonly(null))
				TryInferSplitFieldFromAttribute(attr);

			SubscribeForLineEvents();
			SubscribeForSplitEvents();

			AddMainView();

			AddLotSerOptionsView();
			SubscribeForLotSerOptionsEvents();

			AddShowSplitsAction();
			AddGenerateNumbersAction();

			InitializeLotSerNumVals();

			PXParentAttribute.SetLeaveChildren(SplitCache, true, typeof(TLine));
		}
		#endregion

		#region Views
		#region LSView
		public PXSelectBase<TLine> lsselect { get; private set; }

		protected virtual void AddMainView()
		{
			lsselect = new LSView(this);
			Base.Views.Add(TypePrefixed(nameof(lsselect)), lsselect.View);
		}

		protected class LSView : SelectFrom<TLine>.View, IEqualityComparer<TLine>
		{
			public LSView(LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> lsParent)
				: base(lsParent.Base) => LSParent = lsParent;

			public LSView(PXGraph graph)
				: base(graph) { }

			public LSView(PXGraph graph, Delegate handler)
				: base(graph, handler) { }

			protected LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> LSParent { get; }

			protected bool _AllowInsert = true;
			public override bool AllowInsert
			{
				get => _AllowInsert;
				set
				{
					_AllowInsert = value;
					LSParent.LineCache.AllowInsert = value;

					LSParent.SetEditMode();
				}
			}

			protected bool _AllowUpdate = true;
			public override bool AllowUpdate
			{
				get => _AllowUpdate;
				set
				{
					_AllowUpdate = value;
					LSParent.LineCache.AllowUpdate = value;
					LSParent.SplitCache.AllowInsert = value;
					LSParent.SplitCache.AllowUpdate = value;
					LSParent.SplitCache.AllowDelete = value;

					LSParent.SetEditMode();
				}
			}

			protected bool _AllowDelete = true;
			public override bool AllowDelete
			{
				get => _AllowDelete;
				set
				{
					_AllowDelete = value;
					LSParent.LineCache.AllowDelete = value;

					LSParent.SetEditMode();
				}
			}

			public override TLine Insert(TLine line)
			{
				using (LSParent.OperationModeScope(alterCurrentOperation: PXDBOperation.Delete, restoreToNormal: true))
				using (LSParent.SuppressedModeScope())
					return (TLine)LSParent.LineCache.Insert(line);
			}

			#region IEqualityComparer<TLine> Members
			public bool Equals(TLine x, TLine y) => Cache.GetComparer().Equals(x, y) && x.InventoryID == y.InventoryID;

			public int GetHashCode(TLine obj)
			{
				unchecked
				{
					return Cache.GetComparer().GetHashCode(obj) * 37 + obj.InventoryID.GetHashCode();
				}
			}
			#endregion
		}
		#endregion

		#region LotSerOptions
		protected virtual void AddLotSerOptionsView()
		{
			Base.Views.Add(
				TypePrefixed(nameof(LotSerOptions)),
				new PXView(
					Base,
					isReadOnly: false,
					select: new Select<LotSerOptions>(),
					handler: new PXSelectDelegate(GetLotSerialOpts)));
		}

		public virtual IEnumerable GetLotSerialOpts()
		{
			LotSerOptions opt = new LotSerOptions();
			PXResult<InventoryItem, INLotSerClass> item = null;

			if (LineCurrent != null)
			{
				opt.UnassignedQty = LineCurrent.UnassignedQty;
				item = ReadInventoryItem(LineCurrent.InventoryID);
			}

			if (item != null && (INLotSerClass)item != null)
			{
				var lsClass = (INLotSerClass)item;
				bool disabled;
				bool allowGernerate;

				using (new InvtMultScope(LineCurrent))
				{
					INLotSerTrack.Mode mode = GetTranTrackMode(LineCurrent, lsClass);
					disabled = mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Manual) != 0;
					allowGernerate = (mode & INLotSerTrack.Mode.Create) != 0;
				}

				if (!disabled && lsselect.AllowUpdate)
				{
					var lotSerNum = ReadLotSerNumVal(item);

					string numval = AutoNumberAttribute.NextNumber(lotSerNum == null || string.IsNullOrEmpty(lotSerNum.LotSerNumVal)
						? new string('0', INLotSerialNbrAttribute.GetNumberLength(null))
						: lotSerNum.LotSerNumVal);
					string emptynbr = INLotSerialNbrAttribute.GetNextNumber(LineCache, lsClass, lotSerNum);
					string format = INLotSerialNbrAttribute.GetNextFormat(lsClass, lotSerNum);
					opt.StartNumVal = INLotSerialNbrAttribute.UpdateNumber(format, emptynbr, numval);
					opt.AllowGenerate = allowGernerate;
					if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered)
						opt.Qty = (int)(LineCurrent.UnassignedQty ?? 0);
					else opt.Qty = (LineCurrent.UnassignedQty ?? 0);
					opt.IsSerial = lsClass.LotSerTrack == INLotSerTrack.SerialNumbered;
				}
			}
			Base.Caches<LotSerOptions>().Clear();
			opt = PXCache<LotSerOptions>.Insert(Base, opt);
			Base.Caches<LotSerOptions>().IsDirty = false;
			yield return opt;
		}

		protected virtual bool IsLotSerOptionsEnabled(LotSerOptions opt) => opt?.StartNumVal != null;
		#endregion
		#endregion

		#region Actions
		#region ShowSplits
		protected virtual void AddShowSplitsAction() => showSplits = AddAction(TypePrefixed(nameof(ShowSplits)), Messages.BinLotSerial, true, ShowSplits, PXCacheRights.Select);

		public PXAction showSplits { get; protected set; }
		public virtual IEnumerable ShowSplits(PXAdapter adapter)
		{
			lsselect.View.AskExt(true);
			return adapter.Get();
		}
		#endregion

		#region GenerateNumbers
		protected virtual void AddGenerateNumbersAction() => generateNumbers = AddAction(TypePrefixed(nameof(GenerateNumbers)), Messages.Generate, true, GenerateNumbers, PXCacheRights.Update);

		public PXAction generateNumbers { get; protected set; }
		public virtual IEnumerable GenerateNumbers(PXAdapter adapter)
		{
			LotSerOptions opt = (LotSerOptions)Base.Caches<LotSerOptions>().Current ?? (LotSerOptions)GetLotSerialOpts().FirstOrDefault_();
			if (opt?.StartNumVal == null || opt.Qty == null)
				return adapter.Get();

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(LineCurrent.InventoryID);
			var lsClass = (INLotSerClass)item;
			if (lsClass == null)
				return adapter.Get();

			ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);

			string lotSerialNbr = null;
			INLotSerialNbrAttribute.LSParts parts = INLotSerialNbrAttribute.GetLSParts(LineCache, lsClass, lotSerNum);
			string numVal = opt.StartNumVal.Substring(parts.nidx, parts.nlen);
			string numStr = opt.StartNumVal.Substring(0, parts.flen) + new string('0', parts.nlen) + opt.StartNumVal.Substring(parts.lidx, parts.llen);

			try
			{
				LineCurrent.LotSerialNbr = null;

				List<TSplit> existingSplits = new List<TSplit>();
				if (lsClass.LotSerTrack == INLotSerTrack.LotNumbered)
				{
					foreach (TSplit split in PXParentAttribute.SelectSiblings(SplitCache, null, typeof(TLine)))
					{
						existingSplits.Add(split);
					}
				}

				if (lsClass.LotSerTrack != INLotSerTrack.LotNumbered || (opt.Qty != 0 && LineCurrent.BaseQty != 0m))
				{
					CreateNumbers(LineCurrent, opt.Qty.Value, forceAutoNextNbr: true);
				}

				foreach (TSplit split in PXParentAttribute.SelectSiblings(SplitCache, null, typeof(TLine)))
				{
					if (string.IsNullOrEmpty(split.AssignedNbr) ||
						!INLotSerialNbrAttribute.StringsEqual(split.AssignedNbr, split.LotSerialNbr)) continue;

					TSplit copy = Clone(split);

					if (lotSerialNbr != null)
						numVal = AutoNumberAttribute.NextNumber(numVal);

					if ((decimal)opt.Qty != split.Qty && lsClass.LotSerTrack == INLotSerTrack.LotNumbered && !existingSplits.Contains(split))
					{
						split.BaseQty = (decimal)opt.Qty;
						split.Qty = (decimal)opt.Qty;
					}

					lotSerialNbr = INLotSerialNbrAttribute.UpdateNumber(split.AssignedNbr, numStr, numVal);
					SplitCache.SetValue(split, nameof(ILSMaster.LotSerialNbr), lotSerialNbr);
					SplitCache.RaiseRowUpdated(split, copy);
				}
			}
			catch (Exception)
			{
				UpdateParent(LineCurrent);
			}

			if (lotSerialNbr != null)
				UpdateLotSerNumVal(lotSerNum, numVal, item);
			return adapter.Get();
		}
		#endregion

		protected PXAction AddAction(string name, string displayName, bool visible, PXButtonDelegate handler, PXCacheRights enableRights)
		{
			var buttAttr = new PXButtonAttribute { DisplayOnMainToolbar = false };
			var uiAtt = new PXUIFieldAttribute
			{
				DisplayName = PXMessages.LocalizeNoPrefix(displayName),
				MapEnableRights = enableRights,
			};
			if (!visible) uiAtt.Visible = false;

			var addAttrs = new List<PXEventSubscriberAttribute> { uiAtt, buttAttr };

			var primaryViewType = Base.PrimaryItemType ?? throw new PXException(Messages.CantGetPrimaryView, Base.GetType().FullName);

			var action = (PXAction)Activator.CreateInstance(
				typeof(PXNamedAction<>).MakeGenericType(new[] { primaryViewType }),
				new object[] { Base, name, handler, addAttrs.ToArray() });

			Base.Actions[name] = action;
			return action;
		}
		#endregion

		#region Event Handlers
		#region TLine
		protected virtual void SubscribeForLineEvents()
		{
			ManualEvent.Row<TLine>.Selected.Subscribe(Base, EventHandler);
			ManualEvent.Row<TLine>.Inserted.Subscribe(Base, EventHandler);
			ManualEvent.Row<TLine>.Updated.Subscribe(Base, EventHandler);
			ManualEvent.Row<TLine>.Deleted.Subscribe(Base, EventHandler);

			ManualEvent.Row<TLine>.Persisting.Subscribe(Base, EventHandler);
			ManualEvent.Row<TLine>.Persisted.Subscribe(Base, EventHandler);

			if (LineInventoryField != null)
				ManualEvent.FieldOf<TLine>.Verifying.Subscribe<int?>(Base, LineInventoryField.Name, EventHandlerInventoryID);

			if (LineQtyField != null)
				ManualEvent.FieldOf<TLine>.Verifying.Subscribe<decimal?>(Base, LineQtyField.Name, EventHandlerQty);
		}

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Selected.Args e) // former Master_RowSelected
		{
			// Logic located in child entities
		}

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Inserted.Args e) // former Master_RowInserted
		{
			if (SuppressedMode)
				return;

			using (OperationModeScope(alterCurrentOperation: PXDBOperation.Insert, restoreToNormal: true))
			using (SuppressedModeScope())
				using (new InvtMultScope(e.Row))
					EventHandlerInternal(e);
		}

		protected virtual void EventHandlerInternal(ManualEvent.Row<TLine>.Inserted.Args e) // former _Master_RowInserted
		{
			e.Row.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.Qty.Value, e.Row.BaseQty, INPrecision.QUANTITY);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null && (((InventoryItem)item).StkItem == true || ((InventoryItem)item).KitItem != true))
			{
				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					//count for ZERO serial items only here
					if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false && (e.Row.BaseQty == 0m || e.Row.BaseQty == 1m || ((INLotSerClass)item).LotSerTrack != INLotSerTrack.SerialNumbered))
					{
						UpdateNumbers(e.Row, e.Row.BaseQty.Value);
					}
					else
					{
						CreateNumbers(e.Row, e.Row.BaseQty.Value);
					}
					UpdateParent(e.Row);
				}
				else if ((mode & INLotSerTrack.Mode.Issue) > 0 && e.Row.BaseQty > 0m)
				{
					IssueNumbers(e.Row, e.Row.BaseQty.Value);

					//do not set Zero LotSerial which will prevent IssueNumbers() on quantity update
					if (e.Row.BaseQty > 0)
					{
						UpdateParent(e.Row);
					}
				}
				else if (e.Row.BaseQty == 0m && e.Row.UnassignedQty != 0m)
					e.Row.UnassignedQty = 0m;

				//PCB AvailabilityCheck(sender, e.Row);
			}
			else if (item != null)
			{
				KitInProcessing = item;
				try
				{
					foreach (PXResult<INKitSpecStkDet, InventoryItem> res in
						SelectFrom<INKitSpecStkDet>.
						InnerJoin<InventoryItem>.On<INKitSpecStkDet.FK.ComponentInventoryItem>.
						Where<INKitSpecStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
						View.Select(Base, e.Row.InventoryID))
					{
						(var kitItem, var componentItem) = res;

						if (componentItem.ItemStatus == INItemStatus.Inactive)
						{
							throw new PXException(SO.Messages.KitComponentIsInactive, componentItem.InventoryCD);
						}

						TLine copy = Clone(e.Row);

						copy.InventoryID = kitItem.CompInventoryID;
						copy.SubItemID = kitItem.CompSubItemID;
						copy.UOM = kitItem.UOM;
						copy.Qty = kitItem.DfltCompQty * copy.BaseQty;

						try
						{
							EventHandlerInternal(new ManualEvent.Row<TLine>.Inserted.Args(e.Cache, copy, e.ExternalCall));
						}
						catch (PXException ex)
						{
							throw new PXException(ex, Messages.FailedToProcessComponent, componentItem.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
						}
					}
				}
				finally
				{
					KitInProcessing = null;
				}

				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in
					SelectFrom<INKitSpecNonStkDet>.
					InnerJoin<InventoryItem>.On<INKitSpecNonStkDet.FK.ComponentInventoryItem>.
					Where<
						INKitSpecNonStkDet.kitInventoryID.IsEqual<@P.AsInt>.
						And<
							InventoryItem.kitItem.IsEqual<True>.
							Or<InventoryItem.nonStockShip.IsEqual<True>>>>.
					View.Select(Base, e.Row.InventoryID))
				{
					(var kitItem, var componentItem) = res;

					TLine copy = Clone(e.Row);

					copy.InventoryID = kitItem.CompInventoryID;
					copy.SubItemID = null;
					copy.UOM = kitItem.UOM;
					copy.Qty = kitItem.DfltCompQty * copy.BaseQty;

					try
					{
						EventHandlerInternal(new ManualEvent.Row<TLine>.Inserted.Args(e.Cache, copy, e.ExternalCall));
					}
					catch (PXException ex)
					{
						throw new PXException(ex, Messages.FailedToProcessComponent, componentItem.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
					}
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Updated.Args e) // former Master_RowUpdated
		{
			if (SuppressedMode)
				return;

			using (OperationModeScope(alterCurrentOperation: PXDBOperation.Update, restoreToNormal: true))
			using (SuppressedModeScope())
				using (new InvtMultScope(e.Row, e.OldRow))
					EventHandlerInternal(e);
		}

		protected virtual void EventHandlerInternal(ManualEvent.Row<TLine>.Updated.Args e) // former _Master_RowUpdated
		{
			//Debug.Print("_Master_RowUpdated");
			if (e.OldRow != null && (
				e.OldRow.InventoryID != e.Row.InventoryID ||
				e.OldRow.InvtMult != e.Row.InvtMult ||
				(e.OldRow.UOM == null ^ e.Row.UOM == null)))
			{
				if (!Base.IsContractBasedAPI)
				{
					if (e.OldRow.InventoryID != e.Row.InventoryID)
					{
						e.Row.LotSerialNbr = null;
						e.Row.ExpireDate = null;
					}
					else if (e.OldRow.InvtMult != e.Row.InvtMult)
					{
						if (e.Row.LotSerialNbr == e.OldRow.LotSerialNbr)
							e.Row.LotSerialNbr = null;

						if (e.Row.ExpireDate == e.OldRow.ExpireDate)
							e.Row.ExpireDate = null;
					}
				}

				RaiseRowDeleted(e.OldRow);
				RaiseRowInserted(e.Row);
			}
			else
			{
				e.Row.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.Qty.Value, e.Row.BaseQty, INPrecision.QUANTITY);

				if (e.Row.ExpireDate == e.OldRow.ExpireDate && e.OldRow.LotSerialNbr != e.Row.LotSerialNbr)
					e.Row.ExpireDate = null;

				if (e.Row.InventoryID == null)
					return;

				(InventoryItem item, INLotSerClass lsClass) = ReadInventoryItem(e.Row.InventoryID);

				if (item.StkItem == true || item.KitItem != true)
				{
					string itemLotSerTrack = lsClass.LotSerTrack;

					INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, lsClass);
					if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
					{
						if (IsPrimaryFieldsUpdated(e.Row, e.OldRow) || e.Row.ExpireDate != e.OldRow.ExpireDate)
						{
							if (IsCorrectionMode == false && (itemLotSerTrack == INLotSerTrack.NotNumbered || itemLotSerTrack == null))
							{
								RaiseRowDeleted(e.OldRow);
								RaiseRowInserted(e.Row);
								return;
							}
							else
							{
								UpdateNumbers(e.Row);
							}
						}

						//count for ZERO serial items only here
						if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false && (e.Row.BaseQty == 0m || e.Row.BaseQty == 1m || itemLotSerTrack != INLotSerTrack.SerialNumbered))
						{
							UpdateNumbers(e.Row, e.Row.BaseQty.Value - e.OldRow.BaseQty.Value);
						}
						else if (e.Row.BaseQty > e.OldRow.BaseQty)
						{
							CreateNumbers(e.Row, e.Row.BaseQty.Value - e.OldRow.BaseQty.Value);
						}
						//do not truncate ZERO quantity lotserials
						else if (e.Row.BaseQty < e.OldRow.BaseQty)
						{
							TruncateNumbers(e.Row, e.OldRow.BaseQty.Value - e.Row.BaseQty.Value);
						}

						UpdateParent(e.Row);
					}
					else if ((mode & INLotSerTrack.Mode.Issue) > 0)
					{
						if (IsPrimaryFieldsUpdated(e.Row, e.OldRow) || string.Equals(e.Row.LotSerialNbr, e.OldRow.LotSerialNbr) == false)
						{
							RaiseRowDeleted(e.OldRow);
							RaiseRowInserted(e.Row);
						}
						else if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false &&
							(e.Row.BaseQty == 1m || (itemLotSerTrack != INLotSerTrack.SerialNumbered && e.OldRow.UnassignedQty == 0)))
						{
							UpdateNumbers(e.Row, e.Row.BaseQty.Value - e.OldRow.BaseQty.Value);
						}
						else if (e.Row.BaseQty > e.OldRow.BaseQty)
						{
							IssueNumbers(e.Row, e.Row.BaseQty.Value - e.OldRow.BaseQty.Value);
						}
						else if (e.Row.BaseQty < e.OldRow.BaseQty)
						{
							TruncateNumbers(e.Row, e.OldRow.BaseQty.Value - e.Row.BaseQty.Value);
						}

						//do not set Zero LotSerial which will prevent IssueNumbers() on quantity update
						if (e.Row.BaseQty > 0)
							UpdateParent(e.Row);
					}
					//PCB AvailabilityCheck(sender, e.Row);
				}
				else
				{
					KitInProcessing = item;
					try
					{
						foreach (PXResult<INKitSpecStkDet, InventoryItem> res in
							SelectFrom<INKitSpecStkDet>.
							InnerJoin<InventoryItem>.On<INKitSpecStkDet.FK.ComponentInventoryItem>.
							Where<INKitSpecStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
							View.Select(Base, e.Row.InventoryID))
						{
							(var kititem, var compItem) = res;

							if (compItem.ItemStatus == INItemStatus.Inactive)
								throw new PXException(SO.Messages.KitComponentIsInactive, compItem.InventoryCD);

							TLine copy = Clone(e.Row);

							copy.InventoryID = kititem.CompInventoryID;
							copy.SubItemID = kititem.CompSubItemID;
							copy.UOM = kititem.UOM;
							copy.Qty = kititem.DfltCompQty * copy.BaseQty;

							TLine oldcopy = Clone(e.OldRow);

							oldcopy.InventoryID = kititem.CompInventoryID;
							oldcopy.SubItemID = kititem.CompSubItemID;
							oldcopy.UOM = kititem.UOM;
							oldcopy.Qty = kititem.DfltCompQty * oldcopy.BaseQty;

							if (!LineCounters.TryGetValue(copy, out CurrentCounters))
							{
								LineCounters[copy] = CurrentCounters = new Counters();
								foreach (TSplit detail in SelectSplits(copy))
									UpdateCounters(CurrentCounters, detail);
							}

							//oldcopy.BaseQty = INUnitAttribute.ConvertToBase(sender, oldcopy.InventoryID, oldcopy.UOM, (decimal)oldcopy.Qty, INPrecision.QUANTITY);
							oldcopy.BaseQty = CurrentCounters.BaseQty;

							try
							{
								EventHandlerInternal(new ManualEvent.Row<TLine>.Updated.Args(e.Cache, copy, oldcopy, e.ExternalCall));
							}
							catch (PXException ex)
							{
								throw new PXException(ex, Messages.FailedToProcessComponent, compItem.InventoryCD, item.InventoryCD, ex.MessageNoPrefix);
							}

						}
					}
					finally
					{
						KitInProcessing = null;
					}


					foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in
						SelectFrom<INKitSpecNonStkDet>.
						InnerJoin<InventoryItem>.On<INKitSpecNonStkDet.FK.ComponentInventoryItem>.
						Where<
							INKitSpecNonStkDet.kitInventoryID.IsEqual<@P.AsInt>.
							And<
								InventoryItem.kitItem.IsEqual<True>.
								Or<InventoryItem.nonStockShip.IsEqual<True>>>>.
						View.Select(Base, e.Row.InventoryID))
					{
						(var kitItem, var componentItem) = res;

						if (componentItem.ItemStatus == INItemStatus.Inactive)
							throw new PXException(SO.Messages.KitComponentIsInactive, componentItem.InventoryCD);

						TLine copy = Clone(e.Row);

						copy.InventoryID = kitItem.CompInventoryID;
						copy.SubItemID = null;
						copy.UOM = kitItem.UOM;
						copy.Qty = kitItem.DfltCompQty * copy.BaseQty;

						TLine oldcopy = Clone(e.OldRow);

						oldcopy.InventoryID = kitItem.CompInventoryID;
						oldcopy.SubItemID = null;
						oldcopy.UOM = kitItem.UOM;
						oldcopy.Qty = kitItem.DfltCompQty * oldcopy.BaseQty;
						oldcopy.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, oldcopy.InventoryID, oldcopy.UOM, oldcopy.Qty.Value, oldcopy.BaseQty, INPrecision.QUANTITY);

						try
						{
							EventHandlerInternal(new ManualEvent.Row<TLine>.Updated.Args(e.Cache, copy, oldcopy, e.ExternalCall));
						}
						catch (PXException ex)
						{
							throw new PXException(ex, Messages.FailedToProcessComponent, componentItem.InventoryCD, item.InventoryCD, ex.MessageNoPrefix);
						}
					}
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Deleted.Args e) // former Master_RowDeleted
		{
			using (OperationModeScope(alterCurrentOperation: PXDBOperation.Delete, restoreToNormal: true))
			using (SuppressedModeScope())
				foreach (var split in PXParentAttribute.SelectChildren(SplitCache, e.Row, typeof(TLine)))
					SplitCache.Delete(split);
		}

		protected virtual void EventHandlerInternal(ManualEvent.Row<TLine>.Deleted.Args e) // former _Master_RowDeleted
		{
			if (e.Row != null)
				LineCounters.Remove(e.Row);

			foreach (var detail in SelectSplits(e.Row))
				SplitCache.Delete(detail);
		}


		protected readonly Dictionary<TLine, TSplit[]> PersistedLinesToRelatedSplits = new Dictionary<TLine, TSplit[]>();

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Persisting.Args e) // former Master_RowPersisting
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (LineCounters.TryGetValue(e.Row, out Counters counters) && counters.UnassignedNumber == 0)
					return;

				TSplit[] splits = SelectSplits(e.Row);
				if (e.Row != null)
					PersistedLinesToRelatedSplits[e.Row] = splits;

				foreach (TSplit split in splits)
				{
					using (SuppressedModeScope())
						EventHandler(new ManualEvent.Row<TSplit>.Persisting.Args(SplitCache, e.Operation, split));

					if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false)
					{
						e.Row.LotSerialNbr = split.LotSerialNbr;
						break;
					}
				}
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TLine>.Persisted.Args e) // former Master_RowPersisted
		{
			if (e.TranStatus == PXTranStatus.Aborted)
			{
				RestoreLotSerNumbers();

				if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				{
					if (e.Row == null || !PersistedLinesToRelatedSplits.TryGetValue(e.Row, out TSplit[] lineSplits))
						lineSplits = SelectSplits(e.Row);

					foreach (TSplit split in lineSplits)
					{
						EventHandler(new ManualEvent.Row<TSplit>.Persisted.Args(SplitCache, split, e.Operation, e.TranStatus, e.Exception));
						if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false)
						{
							e.Row.LotSerialNbr = split.LotSerialNbr;
							break;
						}
					}
				}
			}

			if (e.Row != null && e.TranStatus != PXTranStatus.Open)
				PersistedLinesToRelatedSplits.Remove(e.Row);
		}

		public virtual void EventHandlerInventoryID(ManualEvent.FieldOf<TLine>.Verifying.Args<int?> e) // former Master_InventoryID_FieldVerifying
		{
			if (Base.UnattendedMode || Base.IsImport)
				return;

			var item = InventoryItem.PK.Find(Base, e.NewValue);
			if (item != null && item.KitItem == true && item.StkItem == false && KitHasNoComponentsFor(e.NewValue))
			{
				e.NewValue = null;
				throw new PXSetPropertyException(Messages.EmptyKitNotAllowed, PXErrorLevel.Error);
			}

			bool KitHasNoComponentsFor(int? inventoryID)
			{
				INKitSpecStkDet stockSpec =
					SelectFrom<INKitSpecStkDet>.
					Where<INKitSpecStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
					View.ReadOnly.Select(Base, inventoryID);
				if (stockSpec != null)
					return false;

				INKitSpecNonStkDet nonStockSpec =
					SelectFrom<INKitSpecNonStkDet>.
					Where<INKitSpecNonStkDet.kitInventoryID.IsEqual<@P.AsInt>>.
					View.ReadOnly.Select(Base, inventoryID);
				if (nonStockSpec != null)
					return false;

				return true;
			}
		}

		public virtual void EventHandlerQty(ManualEvent.FieldOf<TLine>.Verifying.Args<decimal?> e) // former Master_Qty_FieldVerifying
		{
			e.NewValue = VerifySNQuantity(e.Cache, e.Row, e.NewValue, LineQtyField.Name);
		}


		public virtual void RaiseRowInserted(TLine line) => EventHandlerInternal(new ManualEvent.Row<TLine>.Inserted.Args(LineCache, line, false));

		public virtual void RaiseRowDeleted(TLine line) => EventHandlerInternal(new ManualEvent.Row<TLine>.Deleted.Args(LineCache, line, false));
		#endregion
		#region TSplit
		protected virtual void SubscribeForSplitEvents()
		{
			ManualEvent.Row<TSplit>.Inserting.Subscribe(Base, EventHandler);
			ManualEvent.Row<TSplit>.Inserted.Subscribe(Base, EventHandler);

			ManualEvent.Row<TSplit>.Updated.Subscribe(Base, EventHandler);
			ManualEvent.Row<TSplit>.Deleted.Subscribe(Base, EventHandler);

			ManualEvent.Row<TSplit>.Persisting.Subscribe(Base, EventHandler);
			ManualEvent.Row<TSplit>.Persisted.Subscribe(Base, EventHandler);

			if (SplitUomField != null)
				ManualEvent.FieldOf<TSplit>.Defaulting.Subscribe<string>(Base, SplitUomField.Name, EventHandlerUOM);

			if (SplitQtyField != null && SplitCache.GetAttributesReadonly(SplitQtyField.Name).OfType<PXDBQuantityAttribute>().Select(qa => qa.KeyField).FirstOrDefault() != null)
				ManualEvent.FieldOf<TSplit>.Verifying.Subscribe<decimal?>(Base, SplitQtyField.Name, EventHandlerQty);
		}

		protected virtual void EventHandler(ManualEvent.Row<TSplit>.Inserting.Args e) // former Detail_RowInserting
		{
			if (!string.IsNullOrEmpty(e.Row.AssignedNbr) && INLotSerialNbrAttribute.StringsEqual(e.Row.AssignedNbr, e.Row.LotSerialNbr))
				return;

			if (SuppressedMode && CurrentOperation == PXDBOperation.Insert)
			{
				Counters counters = LineCounters.Ensure(LineCurrent, () => new Counters());
				UpdateCounters(counters, e.Row);
			}

			if (SuppressedMode && CurrentOperation == PXDBOperation.Update)
			{
				var sameSibling = SelectSplits(e.Row).FirstOrDefault(s => AreSplitsEqual(e.Row, s));
				if (sameSibling != null)
				{
					(InventoryItem _, INLotSerClass lsClass) = ReadInventoryItem(e.Row.InventoryID);

					if (lsClass.LotSerTrack != INLotSerTrack.SerialNumbered || sameSibling.BaseQty == 0m)
					{
						var oldSameSibling = Clone(sameSibling);
						sameSibling.BaseQty += e.Row.BaseQty;
						SetSplitQtyWithLine(sameSibling, null);

						e.Cache.RaiseRowUpdated(sameSibling, oldSameSibling);
						e.Cache.MarkUpdated(sameSibling);
						PXDBQuantityAttribute.VerifyForDecimal(e.Cache, sameSibling);
					}
					e.Cancel = true;
				}
			}

			if (e.Row.InventoryID == null || string.IsNullOrEmpty(e.Row.UOM))
				e.Cancel = true;

			if (!e.Cancel)
			{
				(InventoryItem item, INLotSerClass lsClass) = ReadInventoryItem(e.Row.InventoryID);
				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, lsClass);

				if (mode != INLotSerTrack.Mode.None && lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && e.Row.Qty == 0 && LineCurrent.UnassignedQty >= 1)
					e.Row.Qty = 1;

				if (e.Row.BaseQty == null || e.Row.BaseQty == 0m || e.Row.BaseQty != e.Row.Qty || e.Row.UOM != item.BaseUnit)
					e.Row.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.Qty ?? 0m, e.Row.BaseQty, INPrecision.QUANTITY);
				e.Row.UOM = item.BaseUnit;
				e.Row.Qty = e.Row.BaseQty;
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TSplit>.Inserted.Args e) // former Detail_RowInserted
		{
			if (SuppressedMode)
				return;

			e.Row.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.Qty.Value, e.Row.BaseQty, INPrecision.QUANTITY);

			DefaultLotSerialNbr(e.Row);

			if (!UnattendedMode)
				e.Row.ExpireDate = ExpireDateByLot(e.Row, null);

			using (SuppressedModeScope())
			{
				UpdateParent(e.Row, null);

				if (!UnattendedMode)
					Availability?.Check(e.Row);
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TSplit>.Updated.Args e) // former Detail_RowUpdated
		{
			ExpireCachedItems(e.OldRow);

			if (SuppressedMode)
				return;

			if (e.Row.LotSerialNbr != e.OldRow.LotSerialNbr)
				e.Row.ExpireDate = ExpireDateByLot(e.Row, null);
			e.Row.BaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.InventoryID, e.Row.UOM, e.Row.Qty.Value, e.Row.BaseQty, INPrecision.QUANTITY);

			using (SuppressedModeScope())
			{
				UpdateParent(e.Row, e.OldRow);

				if (!UnattendedMode)
					Availability?.Check(e.Row);
			}
		}

		protected virtual void EventHandler(ManualEvent.Row<TSplit>.Deleted.Args e) // former Detail_RowDeleted
		{
			ExpireCachedItems(e.Row);

			if (SuppressedMode)
				return;

			using (SuppressedModeScope())
				UpdateParent(null, e.Row);
		}


		protected readonly Dictionary<TSplit, string> PersistedSplitsToLotSerialNbrs = new Dictionary<TSplit, string>();
		public virtual void EventHandler(ManualEvent.Row<TSplit>.Persisting.Args e) // former Detail_RowPersisting
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (string.IsNullOrEmpty(e.Row.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(e.Row.AssignedNbr, e.Row.LotSerialNbr))
				{
					string numVal = string.Empty;
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);

					try
					{
						numVal = AutoNumberAttribute.NextNumber(lotSerNum.LotSerNumVal);
					}
					catch (AutoNumberException)
					{
						ThrowEmptyLotSerNumVal(e.Row);
					}

					e.Row.LotSerialNbr = INLotSerialNbrAttribute.UpdateNumber(
						e.Row.AssignedNbr,
						e.Row.LotSerialNbr,
						numVal);

					try
					{
						PersistedSplitsToLotSerialNbrs.Add(e.Row, e.Row.LotSerialNbr);
					}
					catch (ArgumentException)
					{
						//the only reason can be overflow in serial numbering which will cause '0000' number to be treated like not-generated
						ThrowEmptyLotSerNumVal(e.Row);
					}

					UpdateLotSerNumVal(lotSerNum, numVal, item);
					e.Cache.RaiseRowUpdated(e.Row, Clone(e.Row));
				}
			}
		}

		public virtual void EventHandler(ManualEvent.Row<TSplit>.Persisted.Args e) // former Detail_RowPersisted
		{
			if (e.TranStatus == PXTranStatus.Aborted)
			{
				if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && PersistedSplitsToLotSerialNbrs.ContainsKey(e.Row))
				{
					e.Row.LotSerialNbr = INLotSerialNbrAttribute.MakeNumber(e.Row.AssignedNbr, e.Row.LotSerialNbr, Base.Accessinfo.BusinessDate.Value);
					PersistedSplitsToLotSerialNbrs.Remove(e.Row);
				}

				if (!UnattendedMode && e.Exception is PXOuterException exception && ReferenceEquals(e.Row, exception.Row))
				{
					TLine line = SelectLine(e.Row);

					foreach (var error in exception.InnerFields.Zip(exception.InnerMessages, (f, m) => (Field: f, Message: m)))
						if (!LineCache.RaiseExceptionHandling(error.Field, line, null, new PXSetPropertyException(error.Message)))
							exception.InnerRemove(error.Field);
				}
			}
			else if (e.TranStatus == PXTranStatus.Completed)
				PersistedSplitsToLotSerialNbrs.Remove(e.Row);
		}

		public virtual void EventHandlerUOM(ManualEvent.FieldOf<TSplit>.Defaulting.Args<string> e) // former Detail_UOM_FieldDefaulting
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(e.Row.InventoryID);

			if (item != null)
			{
				e.NewValue = ((InventoryItem)item).BaseUnit;
				e.Cancel = true;
				//otherwise default via attribute
			}
		}

		public virtual void EventHandlerQty(ManualEvent.FieldOf<TSplit>.Verifying.Args<decimal?> e) // former Detail_Qty_FieldVerifying
		{
			if (IsTrackSerial(e.Row) && e.Row.LotSerialNbr != Messages.Unassigned) // crutch for AC-97716
				if (e.NewValue is decimal qty && qty != 0m && qty != 1m)
					e.NewValue = 1m;
		}
		#endregion
		#region LotSerOptions
		protected virtual void SubscribeForLotSerOptionsEvents()
		{
			ManualEvent.Row<LotSerOptions>.Persisting.Subscribe(Base, EventHandler);
			ManualEvent.Row<LotSerOptions>.Selected.Subscribe(Base, EventHandler);

			ManualEvent.FieldOf<LotSerOptions, LotSerOptions.startNumVal>.Verifying.Subscribe<string>(Base, EventHandler);
			ManualEvent.FieldOf<LotSerOptions, LotSerOptions.startNumVal>.Selecting.Subscribe(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.Row<LotSerOptions>.Selected.Args e)
		{
			bool enabled = IsLotSerOptionsEnabled(e.Row);

			PXUIFieldAttribute.SetEnabled<LotSerOptions.startNumVal>(e.Cache, e.Row, enabled);
			PXUIFieldAttribute.SetEnabled<LotSerOptions.qty>(e.Cache, e.Row, enabled);
			PXDBDecimalAttribute.SetPrecision(e.Cache, e.Row, nameof(LotSerOptions.Qty), e.Row.IsSerial == true ? 0 : CommonSetupDecPl.Qty);
			generateNumbers.SetEnabled(e.Row?.AllowGenerate == true && enabled);
		}

		protected virtual void EventHandler(ManualEvent.Row<LotSerOptions>.Persisting.Args e) => e.Cancel = true;

		protected virtual void EventHandler(ManualEvent.FieldOf<LotSerOptions, LotSerOptions.startNumVal>.Verifying.Args<string> e)
		{
			if (LineCurrent == null)
				return;

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(LineCurrent.InventoryID);
			LotSerOptions opt = (LotSerOptions)Base.Caches<LotSerOptions>().Current;
			if (item == null || opt == null)
				return;

			ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
			INLotSerialNbrAttribute.LSParts parts = INLotSerialNbrAttribute.GetLSParts(LineCache, item, lotSerNum);
			if (string.IsNullOrEmpty(e.NewValue) || e.NewValue.Length < parts.len)
			{
				opt.StartNumVal = null;
				throw new PXSetPropertyException(Messages.TooShortNum, parts.len);
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<LotSerOptions, LotSerOptions.startNumVal>.Selecting.Args e)
		{
			if (e.Row == null || e.Row.StartNumVal == null || LineCurrent == null)
				return;

			var item = ReadInventoryItem(LineCurrent.InventoryID);
			var lotSerNum = ReadLotSerNumVal(item);

			if (INLotSerialNbrAttribute.GetDisplayMask(LineCache, item, lotSerNum) is string mask)
				e.ReturnState = PXStringState.CreateInstance(
					e.ReturnState,
					length: mask.Length,
					isUnicode: true,
					fieldName: nameof(LotSerOptions.StartNumVal),
					isKey: false,
					required: 1,
					inputMask: mask,
					allowedValues: null,
					allowedLabels: null,
					exclusiveValues: null,
					defaultValue: null);
		}
		#endregion
		#endregion

		#region Store & Restore Lot/Serial numbers after aborted
		private Dictionary<ILotSerNumVal, ILotSerNumVal> _lotSerNumVals;

		private void InitializeLotSerNumVals()
		{
			Base.OnBeforePersist += StoreLotSerNumVals;
			Base.EnsureCachePersistence<INLotSerClassLotSerNumVal>();
			Base.EnsureCachePersistence<InventoryItemLotSerNumVal>();
		}

		private bool RestoreLotSerNumbers()
		{
			if (_lotSerNumVals == null)
				return false;

			var lscache = Base.Caches<INLotSerClassLotSerNumVal>();
			lscache.Current = null;
			RestoreLotSerNumVals(lscache, lscache.Cached);
			lscache.Normalize();
			lscache.ClearQueryCache();

			var iicache = Base.Caches<InventoryItemLotSerNumVal>();
			iicache.Current = null;
			RestoreLotSerNumVals(iicache, iicache.Cached);
			iicache.Normalize();
			iicache.ClearQueryCache();

			_lotSerNumVals = null;
			return true;

			void RestoreLotSerNumVals(PXCache cache, IEnumerable numbersCollection)
			{
				var numbers = numbersCollection.OfType<ILotSerNumVal>().ToList();
				foreach (var newNumber in numbers)
				{
					if (_lotSerNumVals.TryGetValue(newNumber, out ILotSerNumVal oldNumber))
					{
						cache.RestoreCopy(newNumber, oldNumber);
						_lotSerNumVals.Remove(newNumber);
					}
					else
						cache.Remove(newNumber);
				}
			}
		}

		private void StoreLotSerNumVals(PXGraph graph)
		{
			_lotSerNumVals = new Dictionary<ILotSerNumVal, ILotSerNumVal>();

			var lscache = graph.Caches<INLotSerClassLotSerNumVal>();
			StoreLotSerNumVals(lscache, lscache.Inserted);
			StoreLotSerNumVals(lscache, lscache.Updated);

			var iicache = graph.Caches<InventoryItemLotSerNumVal>();
			StoreLotSerNumVals(iicache, iicache.Inserted);
			StoreLotSerNumVals(iicache, iicache.Updated);

			void StoreLotSerNumVals(PXCache cache, IEnumerable numbersCollection)
			{
				foreach (ILotSerNumVal lotSerNumVal in numbersCollection)
					_lotSerNumVals.Add(lotSerNumVal, (ILotSerNumVal)cache.CreateCopy(lotSerNumVal));
			}
		}

		#endregion

		#region Create/Truncate/Update/Issue Numbers
		#region Create
		public virtual void CreateNumbers(TLine line, decimal deltaBaseQty)
			=> CreateNumbers(line, deltaBaseQty, false);

		public virtual void CreateNumbers(TLine line, decimal deltaBaseQty, bool forceAutoNextNbr)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);
			TSplit split = LineToSplit(line);
			INLotSerClass lsClass = item;

			if (line != null)
				LineCounters.Remove(line);

			if (!forceAutoNextNbr && lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && lsClass.AutoSerialMaxCount > 0 && lsClass.AutoSerialMaxCount < deltaBaseQty)
				deltaBaseQty = lsClass.AutoSerialMaxCount ?? 0;

			INLotSerTrack.Mode mode = GetTranTrackMode(line, lsClass);
			ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
			foreach (TSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<TSplit>(LineCache, lsClass, lotSerNum, mode, forceAutoNextNbr, deltaBaseQty))
			{
				string LotSerTrack = (mode & INLotSerTrack.Mode.Create) > 0
					? lsClass.LotSerTrack
					: INLotSerTrack.NotNumbered;

				split.SplitLineNbr = null;
				split.LotSerialNbr = lssplit.LotSerialNbr;
				split.AssignedNbr = lssplit.AssignedNbr;
				split.LotSerClassID = lssplit.LotSerClassID;
				if (split is ILSGeneratedDetail gsplit && lssplit is ILSGeneratedDetail glssplit)
					gsplit.HasGeneratedLotSerialNbr = glssplit.HasGeneratedLotSerialNbr;


				if (!string.IsNullOrEmpty(line.LotSerialNbr) && (LotSerTrack == INLotSerTrack.LotNumbered || LotSerTrack == INLotSerTrack.SerialNumbered && line.Qty == 1m))
				{
					split.LotSerialNbr = line.LotSerialNbr;
				}

				if (LotSerTrack == INLotSerTrack.SerialNumbered)
				{
					split.UOM = null;
					split.Qty = 1m;
					split.BaseQty = 1m;
				}
				else
				{
					split.UOM = null;
					split.BaseQty = deltaBaseQty;
					split.Qty = deltaBaseQty;
				}

				if (lsClass.LotSerTrackExpiration == true)
					split.ExpireDate = ExpireDateByLot(split, line);

				PXCache<TSplit>.Insert(Base, Clone(split));
				deltaBaseQty -= split.BaseQty.Value;
			}

			if (deltaBaseQty > 0m && (lsClass.LotSerTrack != INLotSerTrack.SerialNumbered || decimal.Remainder(deltaBaseQty, 1m) == 0m))
			{
				line.UnassignedQty += deltaBaseQty;
			}
			else if (deltaBaseQty > 0m)
			{
				TLine oldLine = PXCache<TLine>.CreateCopy(line);

				line.BaseQty -= deltaBaseQty;
				SetLineQtyFromBase(line);

				if (Math.Abs(oldLine.Qty.Value - line.Qty.Value) >= 0.0000005m)
				{
					LineCache.RaiseFieldUpdated(LineQtyField.Name, line, oldLine.Qty);
					LineCache.RaiseRowUpdated(line, oldLine);
				}
			}

			if (line.UnassignedQty > 0)
				LineCache.RaiseExceptionHandling(LineQtyField.Name, line, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
		}
		#endregion

		#region Truncate
		public virtual void TruncateNumbers(TLine line, decimal deltaBaseQty)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			if (((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered && Math.Abs(Decimal.Floor(deltaBaseQty) - deltaBaseQty) > 0.0000005m)
			{
				TLine oldrow = PXCache<TLine>.CreateCopy(line);
				line.BaseQty += deltaBaseQty - Decimal.Truncate(deltaBaseQty);
				SetLineQtyFromBase(line);
				LineCache.RaiseFieldUpdated(LineQtyField.Name, line, oldrow.Qty);
				LineCache.RaiseRowUpdated(line, oldrow);

				deltaBaseQty = Decimal.Truncate(deltaBaseQty);
			}

			if (line != null)
				LineCounters.Remove(line);

			if (line.UnassignedQty > 0m)
			{
				if (line.UnassignedQty >= deltaBaseQty)
				{
					line.UnassignedQty -= deltaBaseQty;
					deltaBaseQty = 0m;
				}
				else
				{
					deltaBaseQty -= (decimal)line.UnassignedQty;
					line.UnassignedQty = 0m;
				}
			}

			foreach (TSplit split in SelectSplitsReversed(line))
			{
				if (deltaBaseQty >= split.BaseQty)
				{
					deltaBaseQty -= split.BaseQty.Value;
					SplitCache.Delete(split);
					ExpireLotSerialStatusCacheFor(split);
				}
				else
				{
					TSplit newSplit = Clone(split);
					newSplit.BaseQty -= deltaBaseQty;
					SetSplitQtyWithLine(newSplit, line);
					SplitCache.Update(newSplit);
					ExpireLotSerialStatusCacheFor(split);
					break;
				}
			}
		}
		#endregion

		#region Update
		public virtual void UpdateNumbers(TLine line)
		{
			if (line != null)
				LineCounters.Remove(line);

			foreach (TSplit split in SelectSplits(line))
			{
				TSplit newSplit = Clone(split);

				if (line.LocationID == null && newSplit.LocationID != null && SplitCache.GetStatus(newSplit) == PXEntryStatus.Inserted && newSplit.Qty == 0m)
				{
					SplitCache.Delete(newSplit);
				}
				else
				{
					newSplit.SubItemID = line.SubItemID ?? newSplit.SubItemID;
					newSplit.SiteID = line.SiteID;
					newSplit.LocationID = line.LocationID ?? newSplit.LocationID;
					newSplit.ExpireDate = ExpireDateByLot(newSplit, line);
					SplitCache.Update(newSplit);
				}
			}
		}

		public virtual void UpdateNumbers(TLine line, decimal deltaBaseQty)
		{
			bool deleteflag = false;

			if (line != null)
				LineCounters.Remove(line);

			if (CurrentOperation == PXDBOperation.Update)
			{
				foreach (TSplit split in SelectSplits(line))
				{
					if (deleteflag)
					{
						SplitCache.Delete(split);
						ExpireLotSerialStatusCacheFor(split);
					}
					else
					{
						TSplit newSplit = Clone(split);

						newSplit.SubItemID = line.SubItemID;
						newSplit.SiteID = line.SiteID;
						newSplit.LocationID = line.LocationID;
						newSplit.LotSerialNbr = line.LotSerialNbr;
						newSplit.ExpireDate = ExpireDateByLot(newSplit, line);

						newSplit.BaseQty = line.BaseQty;
						SetSplitQtyWithLine(newSplit, line);
						SplitCache.Update(newSplit);
						ExpireLotSerialStatusCacheFor(split);

						deleteflag = true;
					}
				}
			}

			if (!deleteflag)
			{
				TSplit newSplit = LineToSplit(line);
				newSplit.SplitLineNbr = null;
				newSplit.ExpireDate = ExpireDateByLot(newSplit, line);
				DefaultLotSerialNbr(newSplit);

				if (string.IsNullOrEmpty(newSplit.LotSerialNbr) && !string.IsNullOrEmpty(line.LotSerialNbr))
					newSplit.LotSerialNbr = line.LotSerialNbr;
				SplitCache.Insert(newSplit);
				ExpireLotSerialStatusCacheFor(newSplit);
			}
		}
		#endregion

		#region Issue
		public virtual void IssueNumbers(TLine line, decimal deltaBaseQty)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);

			PXDBOperation prevOperation = CurrentOperation;
			if (CurrentOperation == PXDBOperation.Update && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered && SelectSplits(line).Count() == 0)
			{
				CurrentOperation = PXDBOperation.Normal;
			}

			try
			{
				IssueNumbersInternal(line, deltaBaseQty);
			}
			finally
			{
				CurrentOperation = prevOperation;
			}
		}

		protected virtual void IssueNumbersInternal(TLine line, decimal deltaBaseQty)
		{
			IssueNumbers(line, deltaBaseQty, Base.Caches<INLotSerialStatus>(), Base.Caches<LotSerialStatus>());
		}

		protected void IssueNumbers(TLine line, decimal deltaBaseQty, PXCache statusCache, PXCache statusAccumCache)
		{
			TSplit split = LineToSplit(line);

			if (line != null)
				LineCounters.Remove(line);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);
			var lotSerClass = (INLotSerClass)item;
			if ((GetTranTrackMode(line, lotSerClass) & INLotSerTrack.Mode.Issue) > 0)
			{
				List<ILotSerial> lotSerialStatuses = SelectSerialStatus(line, item);

				MoveItemToTopOfList(lotSerialStatuses, line.LotSerialNbr);

				foreach (ILotSerial lsmaster in lotSerialStatuses)
				{
					split.SplitLineNbr = null;
					split.SubItemID = lsmaster.SubItemID;
					split.LocationID = lsmaster.LocationID;
					split.LotSerialNbr = lsmaster.LotSerialNbr;
					split.ExpireDate = lsmaster.ExpireDate;
					split.UOM = ((InventoryItem)item).BaseUnit;

					INItemPlanIDAttribute.GetInclQtyAvail<SiteLotSerial>(SplitCache, split, out decimal signQtyAvail, out decimal signQtyHardAvail);

					if (signQtyAvail < 0m)
					{
						IStatus accumavail = (IStatus)statusAccumCache.CreateInstance();
						statusCache.RestoreCopy(accumavail, lsmaster);
						accumavail = (IStatus)statusAccumCache.Insert(accumavail);

						decimal? availableQty = ((IStatus)lsmaster).QtyAvail + accumavail.QtyAvail;

						if (availableQty <= 0m)
						{
							continue;
						}

						if (availableQty <= deltaBaseQty)
						{
							split.BaseQty = availableQty;
							deltaBaseQty -= (decimal)availableQty;
						}
						else
						{
							if (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
							{
								split.BaseQty = 1m;
								deltaBaseQty -= 1m;
							}
							else
							{
								split.BaseQty = deltaBaseQty;
								deltaBaseQty = 0m;
							}
						}
					}
					else
					{
						if (statusCache.GetStatus(lsmaster) == PXEntryStatus.Notchanged)
							Summarize(statusCache, line, lsmaster);

						if (lsmaster.QtyOnHand <= 0m)
							continue;

						if (lsmaster.QtyOnHand <= deltaBaseQty)
						{
							split.BaseQty = lsmaster.QtyOnHand;
							deltaBaseQty -= lsmaster.QtyOnHand.Value;
						}
						else
						{
							if (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
							{
								split.BaseQty = 1m;
								deltaBaseQty -= 1m;
							}
							else
							{
								split.BaseQty = deltaBaseQty;
								deltaBaseQty = 0m;
							}
						}

						lsmaster.QtyOnHand -= split.BaseQty;
						statusCache.SetStatus(lsmaster, PXEntryStatus.Held);
					}

					SetSplitQtyWithLine(split, line);
					PXCache<TSplit>.Insert(Base, Clone(split));

					if (deltaBaseQty <= 0m)
						break;
				}
			}

			if (deltaBaseQty > 0m && line.InventoryID != null && line.SubItemID != null && line.SiteID != null && line.LocationID != null && !string.IsNullOrEmpty(line.LotSerialNbr))
			{
				if (lotSerClass.IsManualAssignRequired != true && lotSerClass.LotSerTrack != INLotSerTrack.SerialNumbered)
				{
					split.SplitLineNbr = null;
					split.BaseQty = deltaBaseQty;

					SetSplitQtyWithLine(split, line);
					split.ExpireDate = ExpireDateByLot(split, null);

					try
					{
						PXCache<TSplit>.Insert(Base, Clone(split));
					}
					catch
					{
						line.UnassignedQty += deltaBaseQty;
						LineCache.RaiseExceptionHandling(LineQtyField.Name, line, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
					}
					finally
					{
						deltaBaseQty = 0m;
					}
				}
			}

			if (deltaBaseQty != 0)
			{
				var haveRemainder = lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered && decimal.Remainder(deltaBaseQty, 1m) != 0m;
				if (haveRemainder || deltaBaseQty < 0)
				{
					TLine oldLine = PXCache<TLine>.CreateCopy(line);

					line.BaseQty -= deltaBaseQty;
					SetLineQtyFromBase(line);
					LineCache.RaiseFieldUpdated(LineQtyField.Name, line, oldLine.Qty);
					LineCache.RaiseRowUpdated(line, oldLine);

					if (haveRemainder)
					{
						LineCache.RaiseExceptionHandling(LineQtyField.Name, line, null, new PXSetPropertyException(Messages.SerialItem_LineQtyUpdated, PXErrorLevel.Warning));
					}
					else
					{
						LineCache.RaiseExceptionHandling(LineQtyField.Name, line, null, new PXSetPropertyException(Messages.InsuffQty_LineQtyUpdated, PXErrorLevel.Warning));
					}
				}
				else
				{
					line.UnassignedQty += deltaBaseQty;
					LineCache.RaiseExceptionHandling(LineQtyField.Name, line, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
				}
			}

			void MoveItemToTopOfList(List<ILotSerial> lotSerialStatuses, string lotSerialNbr)
			{
				if (!string.IsNullOrEmpty(lotSerialNbr))
				{
					int lotSerialIndex = lotSerialStatuses.FindIndex(
						x => string.Equals(
							x.LotSerialNbr?.Trim(),
							lotSerialNbr.Trim(),
							StringComparison.InvariantCultureIgnoreCase));
					if (lotSerialIndex > 0)
					{
						ILotSerial lotSerialStatus = lotSerialStatuses[lotSerialIndex];
						lotSerialStatuses.RemoveAt(lotSerialIndex);
						lotSerialStatuses.Insert(0, lotSerialStatus);
					}
				}
			}
		}

		public virtual void Summarize(PXCache statusCache, TLine line, ILotSerial lotSerialRow)
		{
			PXView view = Base.TypedViews.GetView(SplitByLotSerialStatusCommand, false);
			foreach (TSplit split in view.SelectMultiBound(new object[] { line }, lotSerialRow.InventoryID, lotSerialRow.SubItemID, lotSerialRow.SiteID, lotSerialRow.LocationID, lotSerialRow.LotSerialNbr))
			{
				((IStatus)lotSerialRow).QtyOnHand += split.InvtMult * split.BaseQty;
			}
			statusCache.SetStatus(lotSerialRow, PXEntryStatus.Held);
		}
		#endregion

		protected virtual INLotSerialStatus MakeINLotSerialStatus(ILSMaster item)
		{
			var ret = new INLotSerialStatus
			{
				InventoryID = item.InventoryID,
				SiteID = item.SiteID,
				LocationID = item.LocationID,
				SubItemID = item.SubItemID,
				LotSerialNbr = item.LotSerialNbr
			};

			return ret;
		}

		protected virtual void ExpireLotSerialStatusCacheFor(TSplit split)
		{
			ExpireCached(MakeINLotSerialStatus(split));
		}
		#endregion

		#region UpdateParent helper
		public virtual void UpdateParent(TLine line)
		{
			UpdateParent(line, null, null, out decimal baseQty);
			SetUnassignedQty(line, baseQty, true);
		}

		public virtual void UpdateParent(TSplit newSplit, TSplit oldSplit)
		{
			TSplit anySplit = newSplit ?? oldSplit;
			TLine parent = PXParentAttribute.SelectParent<TLine>(SplitCache, anySplit);

			if (parent != null && anySplit != null && SameInventoryItem(anySplit, parent))
			{
				TLine oldParent = PXCache<TLine>.CreateCopy(parent);

				UpdateParent(parent, newSplit, oldSplit, out decimal baseQty);

				using (new InvtMultScope(parent))
				{
					SetUnassignedQty(parent, baseQty, false);
				}

				LineCache.MarkUpdated(parent);

				if (Math.Abs(oldParent.Qty.Value - parent.Qty.Value) >= 0.0000005m)
				{
					LineCache.RaiseFieldUpdated(LineQtyField.Name, parent, oldParent.Qty);
					LineCache.RaiseRowUpdated(parent, oldParent);
				}
			}
		}

		protected virtual void SetUnassignedQty(TLine line, decimal detailsBaseQty, bool allowNegative)
		{
			if (detailsBaseQty < line.BaseQty || allowNegative)
			{
				line.UnassignedQty = PXDBQuantityAttribute.Round(line.BaseQty.Value - detailsBaseQty);
			}
			else
			{
				line.UnassignedQty = 0m;
				line.BaseQty = detailsBaseQty;
				SetLineQtyFromBase(line);
			}
		}

		public virtual void UpdateParent(TLine line, TSplit newSplit, TSplit oldSplit, out decimal baseQty)
		{
			CurrentCounters = null;
			if (!LineCounters.TryGetValue(line, out CurrentCounters))
			{
				LineCounters[line] = CurrentCounters = new Counters();
				foreach (TSplit detail in SelectSplits(line))
					UpdateCounters(CurrentCounters, detail);
			}
			else
			{
				if (newSplit != null)
					UpdateCounters(CurrentCounters, newSplit);

				if (oldSplit != null)
				{
					CurrentCounters.RecordCount -= 1;
					oldSplit.BaseQty = INUnitAttribute.ConvertToBase(SplitCache, oldSplit.InventoryID, oldSplit.UOM, oldSplit.Qty.Value, oldSplit.BaseQty, INPrecision.QUANTITY);
					CurrentCounters.BaseQty -= oldSplit.BaseQty.Value;

					if (oldSplit.ExpireDate == null)
					{
						CurrentCounters.ExpireDatesNull -= 1;
					}
					else if (CurrentCounters.ExpireDates.ContainsKey(oldSplit.ExpireDate))
					{
						if ((CurrentCounters.ExpireDates[oldSplit.ExpireDate] -= 1) == 0)
							CurrentCounters.ExpireDates.Remove(oldSplit.ExpireDate);
					}

					if (oldSplit.SubItemID == null)
					{
						CurrentCounters.SubItemsNull -= 1;
					}
					else if (CurrentCounters.SubItems.ContainsKey(oldSplit.SubItemID))
					{
						if ((CurrentCounters.SubItems[oldSplit.SubItemID] -= 1) == 0)
							CurrentCounters.SubItems.Remove(oldSplit.SubItemID);
					}

					if (oldSplit.LocationID == null)
					{
						CurrentCounters.LocationsNull -= 1;
					}
					else if (CurrentCounters.Locations.ContainsKey(oldSplit.LocationID))
					{
						if ((CurrentCounters.Locations[oldSplit.LocationID] -= 1) == 0)
							CurrentCounters.Locations.Remove(oldSplit.LocationID);
					}

					if (oldSplit.TaskID == null)
					{
						CurrentCounters.ProjectTasksNull -= 1;
					}
					else
					{
						var kv = new KeyValuePair<int?, int?>(oldSplit.ProjectID, oldSplit.TaskID);
						if (CurrentCounters.ProjectTasks.ContainsKey(kv))
							if ((CurrentCounters.ProjectTasks[kv] -= 1) == 0)
								CurrentCounters.ProjectTasks.Remove(kv);
					}

					if (oldSplit.LotSerialNbr == null)
					{
						CurrentCounters.LotSerNumbersNull -= 1;
					}
					else if (CurrentCounters.LotSerNumbers.ContainsKey(oldSplit.LotSerialNbr))
					{
						if (string.IsNullOrEmpty(oldSplit.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(oldSplit.AssignedNbr, oldSplit.LotSerialNbr))
							CurrentCounters.UnassignedNumber--;

						if ((CurrentCounters.LotSerNumbers[oldSplit.LotSerialNbr] -= 1) == 0)
							CurrentCounters.LotSerNumbers.Remove(oldSplit.LotSerialNbr);
					}
				}

				if (newSplit == null && oldSplit != null)
				{
					if (CurrentCounters.ExpireDates.Count == 1 && CurrentCounters.ExpireDatesNull == 0)
						foreach (DateTime? key in CurrentCounters.ExpireDates.Keys)
							CurrentCounters.ExpireDate = key;

					if (CurrentCounters.SubItems.Count == 1 && CurrentCounters.SubItemsNull == 0)
						foreach (int? key in CurrentCounters.SubItems.Keys)
							CurrentCounters.SubItem = key;

					if (CurrentCounters.Locations.Count == 1 && CurrentCounters.LocationsNull == 0)
						foreach (int? key in CurrentCounters.Locations.Keys)
							CurrentCounters.Location = key;

					if (CurrentCounters.ProjectTasks.Count == 1 && CurrentCounters.ProjectTasksNull == 0)
						foreach (KeyValuePair<int?, int?> key in CurrentCounters.ProjectTasks.Keys)
							(CurrentCounters.ProjectID, CurrentCounters.TaskID) = key;

					if (CurrentCounters.LotSerNumbers.Count == 1 && CurrentCounters.LotSerNumbersNull == 0)
						foreach (string key in CurrentCounters.LotSerNumbers.Keys)
							CurrentCounters.LotSerNumber = key;
				}
			}

			baseQty = CurrentCounters.BaseQty;

			switch (CurrentCounters.RecordCount)
			{
				case 0:
					line.LotSerialNbr = string.Empty;
					line.HasMixedProjectTasks = false;
					break;

				case 1:
					line.ExpireDate = CurrentCounters.ExpireDate;
					line.SubItemID = CurrentCounters.SubItem;
					line.LocationID = CurrentCounters.Location;
					line.LotSerialNbr = CurrentCounters.LotSerNumber;
					line.HasMixedProjectTasks = false;
					if (CurrentCounters.ProjectTasks.Count > 0 && newSplit != null && CurrentCounters.ProjectID != null)
					{
						line.ProjectID = CurrentCounters.ProjectID;
						line.TaskID = CurrentCounters.TaskID;
					}
					break;

				default:
					line.ExpireDate = CurrentCounters.ExpireDates.Count == 1 && CurrentCounters.ExpireDatesNull == 0 ? CurrentCounters.ExpireDate : null;
					line.SubItemID = CurrentCounters.SubItems.Count == 1 && CurrentCounters.SubItemsNull == 0 ? CurrentCounters.SubItem : null;
					line.LocationID = CurrentCounters.Locations.Count == 1 && CurrentCounters.LocationsNull == 0 ? CurrentCounters.Location : null;
					line.HasMixedProjectTasks = CurrentCounters.ProjectTasks.Count + (CurrentCounters.ProjectTasks.Count > 0 ? CurrentCounters.ProjectTasksNull : 0) > 1;
					if (line.HasMixedProjectTasks != true && newSplit != null && CurrentCounters.ProjectID != null)
					{
						line.ProjectID = CurrentCounters.ProjectID;
						line.TaskID = CurrentCounters.TaskID;
					}

					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(line.InventoryID);
					INLotSerTrack.Mode mode = GetTranTrackMode(line, item);
					if (mode == INLotSerTrack.Mode.None)
					{
						line.LotSerialNbr = string.Empty;
					}
					else if ((mode & INLotSerTrack.Mode.Create) > 0 || (mode & INLotSerTrack.Mode.Issue) > 0)
					{
						//if more than 1 split exist at lotserial creation time ignore equilness and display <SPLIT>
						line.LotSerialNbr = null;
					}
					else
					{
						line.LotSerialNbr = CurrentCounters.LotSerNumbers.Count == 1 && CurrentCounters.LotSerNumbersNull == 0 ? CurrentCounters.LotSerNumber : null;
					}
					break;
			}
		}
		#endregion

		#region Select Helpers
		protected virtual TLine SelectLine(TSplit split) => PXParentAttribute.SelectParent<TLine>(SplitCache, split);

		#region Select Splits
		protected virtual TSplit[] SelectSplits(TLine line)
		{
			return PXParentAttribute
				.SelectChildren(SplitCache, line, typeof(TLine))
				.Cast<TSplit>()
				.Where(split => SameInventoryItem(split, line))
				.ToArray();
		}

		protected virtual TSplit[] SelectSplits(TSplit split)
		{
			return PXParentAttribute
				.SelectSiblings(SplitCache, split, typeof(TLine))
				.Cast<TSplit>()
				.Where(sibling => SameInventoryItem(sibling, split))
				.ToArray();
		}

		protected TSplit[] SelectSplitsOrdered(TLine line) => SelectSplitsOrdered(LineToSplit(line));

		protected virtual TSplit[] SelectSplitsOrdered(TSplit split) => SelectSplits(split).OrderBy(s => s.SplitLineNbr).ToArray();

		protected TSplit[] SelectSplitsReversed(TLine line) => SelectSplitsReversed(LineToSplit(line));

		protected virtual TSplit[] SelectSplitsReversed(TSplit split) => SelectSplits(split).OrderByDescending(s => s.SplitLineNbr).ToArray();
		#endregion

		#region Select LotSerial Status
		protected virtual List<ILotSerial> SelectSerialStatus(TLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<INLotSerialStatus> cmd = GetSerialStatusCmd(line, item);
			INLotSerialStatus pars = MakeINLotSerialStatus(line);
			List<INLotSerialStatus> list = cmd.View.SelectMultiBound(new object[] { pars }).RowCast<INLotSerialStatus>().ToList();
			return new List<ILotSerial>(list);
		}

		public virtual PXSelectBase<INLotSerialStatus> GetSerialStatusCmd(TLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<INLotSerialStatus> cmd = GetSerialStatusCmdBase(line, item);
			AppendSerialStatusCmdWhere(cmd, line, item);
			AppendSerialStatusCmdOrderBy(cmd, line, item);

			return cmd;
		}

		protected virtual PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(TLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			return new
				SelectFrom<INLotSerialStatus>.
				InnerJoin<INLocation>.On<INLotSerialStatus.FK.Location>.
				Where<
					INLotSerialStatus.inventoryID.IsEqual<INLotSerialStatus.inventoryID.FromCurrent>.
					And<INLotSerialStatus.siteID.IsEqual<INLotSerialStatus.siteID.FromCurrent>>.
					And<INLotSerialStatus.qtyOnHand.IsGreater<decimal0>>>.
				View(Base);
		}

		protected virtual void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, TLine line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID.IsEqual<INLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
				cmd.WhereAnd<Where<INLotSerialStatus.locationID.IsEqual<INLotSerialStatus.locationID.FromCurrent>>>();
			else
				cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(line.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr.IsEqual<INLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}

		public virtual void AppendSerialStatusCmdOrderBy(PXSelectBase<INLotSerialStatus> cmd, TLine line, INLotSerClass lotSerClass)
		{
			switch (lotSerClass.LotSerIssueMethod)
			{
				case INLotSerIssueMethod.FIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<INLotSerialStatus.receiptDate, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.LIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Desc<INLotSerialStatus.receiptDate, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Expiration:
					cmd.OrderByNew<OrderBy<Asc<INLotSerialStatus.expireDate, Asc<INLocation.pickPriority, Asc<INLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Sequential:
				case INLotSerIssueMethod.UserEnterable:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<INLotSerialStatus.lotSerialNbr>>>>();
					break;
				default:
					throw new PXException();
			}
		}
		#endregion

		protected virtual PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(int? inventoryID)
		{
			if (inventoryID == null)
				return null;

			var inventory = InventoryItem.PK.Find(Base, inventoryID);
			if (inventory == null)
				throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, Messages.InventoryItem, inventoryID);

			INLotSerClass lotSerClass;
			if (inventory.StkItem == true)
			{
				lotSerClass = INLotSerClass.PK.Find(Base, inventory.LotSerClassID);
				if (lotSerClass == null)
					throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, Messages.LotSerClass, inventory.LotSerClassID);
			}
			else
			{
				lotSerClass = new INLotSerClass();
			}

			return new PXResult<InventoryItem, INLotSerClass>(inventory, lotSerClass);
		}

		/// <summary>
		/// Read ILotSerNumVal implemented object which store auto-incremental value
		/// </summary>
		protected virtual ILotSerNumVal ReadLotSerNumVal(PXResult<InventoryItem, INLotSerClass> item)
			=> INLotSerialNbrAttribute.ReadLotSerNumVal(Base, item);
		#endregion

		#region Cache Helpers
		#region TPrimary
		private PXCache<TPrimary> _docCache;
		public PXCache<TPrimary> DocumentCache => _docCache ?? (_docCache = Base.Caches<TPrimary>());
		public TPrimary DocumentCurrent => (TPrimary)DocumentCache.Current;
		#endregion
		#region TLine
		private PXCache<TLine> _lineCache;
		public PXCache<TLine> LineCache => _lineCache ?? (_lineCache = Base.Caches<TLine>());
		public TLine LineCurrent => (TLine)LineCache.Current;
		public virtual TLine Clone(TLine line) => PXCache<TLine>.CreateCopy(line);
		#endregion
		#region TSplit
		private PXCache<TSplit> _splitCache;
		public PXCache<TSplit> SplitCache => _splitCache ?? (_splitCache = Base.Caches<TSplit>());
		public TSplit SplitCurrent => (TSplit)SplitCache.Current;
		public virtual TSplit Clone(TSplit split) => PXCache<TSplit>.CreateCopy(split);
		#endregion
		#endregion

		#region Utility Helpers
		public virtual TSplit EnsureSplit(ILSMaster row)
		{
			if (row is TSplit split)
				return split;
			else
			{
				split = LineToSplit(row as TLine);

				PXParentAttribute.SetParent(SplitCache, split, typeof(TLine), row);

				if (string.IsNullOrEmpty(row.LotSerialNbr) == false)
					DefaultLotSerialNbr(split);

				return split;
			}
		}

		/// <summary>
		/// Save new auto-incremental value
		/// </summary>
		/// <param name="lotSerNum">object with auto-incremental value</param>
		/// <param name="value">new auto-incremental value</param>
		/// <param name="item">settings</param>
		protected virtual void UpdateLotSerNumVal(ILotSerNumVal lotSerNum, string value, PXResult<InventoryItem, INLotSerClass> item)
		{
			Type type;
			PXCache cache;
			if (lotSerNum == null)
			{
				lotSerNum = ((INLotSerClass)item).LotSerNumShared ?? false
					? (ILotSerNumVal)new INLotSerClassLotSerNumVal { LotSerClassID = ((INLotSerClass)item).LotSerClassID }
					: new InventoryItemLotSerNumVal { InventoryID = ((InventoryItem)item).InventoryID };
				lotSerNum.LotSerNumVal = value;

				type = lotSerNum.GetType();
				cache = Base.Caches[type];
				cache.Insert(lotSerNum);
			}
			else
			{
				type = lotSerNum.GetType();
				cache = Base.Caches[type];

				var copy = (ILotSerNumVal)cache.CreateCopy(lotSerNum);
				copy.LotSerNumVal = value;
				cache.Update(copy);
			}
		}

		protected virtual INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			string tranType = row.TranType;

			if (lotSerClass.LotSerAssign == INLotSerAssign.WhenUsed && row.InvtMult < 0 && row.IsIntercompany == true)
				tranType = INTranType.Transfer;

			return INLotSerialNbrAttribute.TranTrackMode(lotSerClass, tranType, row.InvtMult);
		}

		protected virtual void SetLineQtyFromBase(TLine line)
		{
			line.Qty = INUnitAttribute.ConvertFromBase(LineCache, line.InventoryID, line.UOM, line.BaseQty.Value, INPrecision.QUANTITY);
		}

		protected virtual void SetSplitQtyWithLine(TSplit split, TLine line)
		{
			line = line ?? SelectLine(split);
			if (split.InventoryID == line?.InventoryID && split.BaseQty == line?.BaseQty && string.Equals(split.UOM, line?.UOM, StringComparison.OrdinalIgnoreCase))
			{
				split.Qty = line.Qty;
			}
			else
			{
				split.Qty = INUnitAttribute.ConvertFromBase(SplitCache, split.InventoryID, split.UOM, split.BaseQty.Value, INPrecision.QUANTITY);
			}
		}

		public virtual void DefaultLotSerialNbr(TSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);

			if (item != null)
			{
				INLotSerTrack.Mode mode = GetTranTrackMode(split, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(item);
					foreach (TSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<TSplit>(SplitCache, item, lotSerNum, mode, 1m))
					{
						if (string.IsNullOrEmpty(split.LotSerialNbr))
							split.LotSerialNbr = lssplit.LotSerialNbr;

						split.AssignedNbr = lssplit.AssignedNbr;
						split.LotSerClassID = lssplit.LotSerClassID;
						if (split is ILSGeneratedDetail gSplit && lssplit is ILSGeneratedDetail glssplit)
							gSplit.HasGeneratedLotSerialNbr = glssplit.HasGeneratedLotSerialNbr;
					}
				}
			}
		}

		protected virtual DateTime? ExpireDateByLot(ILSMaster item, ILSMaster master) => LSSelect.ExpireDateByLot(Base, item, master);

		protected virtual void ExpireCachedItems(TSplit split)
		{
			ExpireCached(MakeINLotSerialStatus(split));
		}

		protected virtual void ExpireCached<T>(T item)
			where T : class, IBqlTable, new()
		{
			PXCache cache = Base.Caches<T>();
			if (cache.Locate(item) is object cached && cache.GetStatus(cached).IsIn(PXEntryStatus.Held, PXEntryStatus.Notchanged))
			{
				cache.SetStatus(cached, PXEntryStatus.Notchanged);
				cache.Remove(cached);
				cache.ClearQueryCache();
			}
		}

		protected virtual void UpdateCounters(Counters counters, TSplit split)
		{
			counters.RecordCount += 1;
			split.BaseQty = INUnitAttribute.ConvertToBase(SplitCache, split.InventoryID, split.UOM, split.Qty.Value, split.BaseQty, INPrecision.QUANTITY);
			counters.BaseQty += (decimal)split.BaseQty;

			if (split.ExpireDate == null)
			{
				counters.ExpireDatesNull += 1;
			}
			else
			{
				if (counters.ExpireDates.ContainsKey(split.ExpireDate))
					counters.ExpireDates[split.ExpireDate] += 1;
				else
					counters.ExpireDates[split.ExpireDate] = 1;

				counters.ExpireDate = split.ExpireDate;
			}

			if (split.SubItemID == null)
			{
				counters.SubItemsNull += 1;
			}
			else
			{
				if (counters.SubItems.ContainsKey(split.SubItemID))
					counters.SubItems[split.SubItemID] += 1;
				else
					counters.SubItems[split.SubItemID] = 1;

				counters.SubItem = split.SubItemID;
			}

			if (split.LocationID == null)
			{
				counters.LocationsNull += 1;
			}
			else
			{
				if (counters.Locations.ContainsKey(split.LocationID))
					counters.Locations[split.LocationID] += 1;
				else
					counters.Locations[split.LocationID] = 1;

				counters.Location = split.LocationID;
			}

			if (split.TaskID == null)
			{
				counters.ProjectTasksNull += 1;
			}
			else
			{
				var kv = new KeyValuePair<int?, int?>(split.ProjectID, split.TaskID);
				if (counters.ProjectTasks.ContainsKey(kv))
					counters.ProjectTasks[kv] += 1;
				else
					counters.ProjectTasks[kv] = 1;

				(counters.ProjectID, counters.TaskID) = (split.ProjectID, split.TaskID);
			}

			if (split.LotSerialNbr == null)
			{
				counters.LotSerNumbersNull += 1;
			}
			else
			{
				if (string.IsNullOrEmpty(split.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(split.AssignedNbr, split.LotSerialNbr))
					counters.UnassignedNumber++;

				if (counters.LotSerNumbers.ContainsKey(split.LotSerialNbr))
					counters.LotSerNumbers[split.LotSerialNbr] += 1;
				else
					counters.LotSerNumbers[split.LotSerialNbr] = 1;

				counters.LotSerNumber = split.LotSerialNbr;
			}
		}


		protected virtual void ThrowEmptyLotSerNumVal(TSplit split)
		{
			string itemFieldName = SplitCache.GetAttributesReadonly(null).OfType<InventoryAttribute>().Select(a => a.FieldName).FirstOrDefault();
			//the only reason can be overflow in serial numbering which will cause '0000' number to be treated like not-generated
			throw new PXException(Messages.LSCannotAutoNumberItem, SplitCache.GetValueExt(split, itemFieldName));
		}

		public virtual decimal? VerifySNQuantity(PXCache cache, ILSMaster row, decimal? newValue, string qtyFieldName)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(row.InventoryID);

			if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
			{
				if (newValue != null)
				{
					try
					{
						decimal BaseQty = INUnitAttribute.ConvertToBase(cache, row.InventoryID, row.UOM, newValue.Value, INPrecision.NOROUND);
						if (decimal.Remainder(BaseQty, 1m) > 0m)
						{
							decimal power = (decimal)Math.Pow(10, CommonSetupDecPl.Qty);
							for (decimal i = Math.Floor(BaseQty); ; i++)
							{
								newValue = INUnitAttribute.ConvertFromBase(cache, row.InventoryID, row.UOM, i, INPrecision.NOROUND);

								if (decimal.Remainder(newValue.Value * power, 1m) == 0m)
									break;
							}
							cache.RaiseExceptionHandling(qtyFieldName, row, null, new PXSetPropertyException(IN.Messages.SerialItem_LineQtyUpdated, PXErrorLevel.Warning));
						}
					}
					catch (PXUnitConversionException) { }
				}
			}

			return newValue;
		}

		private string _prefix;
		protected string Prefix => _prefix ??= PX.Api.CustomizedTypeManager.GetTypeNotCustomized(GetType()).Name;
		protected string TypePrefixed(string name) => $"{Prefix}_{name}";


		protected virtual bool AreSplitsEqual(TSplit a, TSplit b)
		{
			if (a != null && b != null)
			{
				return a.InventoryID == b.InventoryID &&
					(a.IsStockItem != true ||
						a.SubItemID == b.SubItemID &&
						a.LocationID == b.LocationID &&
						(string.Equals(a.LotSerialNbr, b.LotSerialNbr) || string.IsNullOrEmpty(a.LotSerialNbr) && string.IsNullOrEmpty(b.LotSerialNbr)) &&
						(string.IsNullOrEmpty(a.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(a.AssignedNbr, a.LotSerialNbr) == false) &&
						(string.IsNullOrEmpty(b.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(b.AssignedNbr, b.LotSerialNbr) == false));
			}
			else
			{
				return a != null;
			}
		}

		protected virtual bool SameInventoryItem(ILSMaster a, ILSMaster b) => a.InventoryID == b.InventoryID;

		public virtual bool IsTrackSerial(TSplit split)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(split.InventoryID);

			if (item == null)
				return false;

			string tranType = split.TranType;
			INLotSerClass lotSerClass = item;
			if (lotSerClass.LotSerAssign == INLotSerAssign.WhenUsed && split.InvtMult < 0 && split.IsIntercompany == true)
			{
				tranType = INTranType.Transfer;
			}

			return INLotSerialNbrAttribute.IsTrackSerial(item, tranType, split.InvtMult);
		}

		public virtual bool IsIndivisibleComponent(InventoryItem inventory) => KitInProcessing != null && inventory.DecimalBaseUnit != true;
		private InventoryItem KitInProcessing { get; set; }

		protected virtual bool IsPrimaryFieldsUpdated(TLine line, TLine oldLine)
		{
			return line.SubItemID != oldLine.SubItemID ||
				line.SiteID != oldLine.SiteID ||
				line.LocationID != oldLine.LocationID ||
				line.ProjectID != oldLine.ProjectID ||
				line.TaskID != oldLine.TaskID;
		}

		protected virtual NotDecimalUnitErrorRedirectorScope<TDetailQty> ResolveNotDecimalUnitErrorRedirectorScope<TDetailQty>(object row)
			where TDetailQty : IBqlField
		{
			if (LineQtyField == null)
				throw new PXArgumentException(nameof(LineQtyField));
			return new NotDecimalUnitErrorRedirectorScope<TDetailQty>(LineCache, row, LineQtyField);
		}

		public void ThrowFieldIsEmpty<Field>(PXCache cache, object row)
			where Field : IBqlField
		{
			if (cache.RaiseExceptionHandling<Field>(row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
		}
		#endregion

		#region Scopes
		#region InvtMultScope
		public class InvtMultScope : IDisposable
		{
			private readonly TLine _line;
			private readonly TLine _oldLine;
			private readonly bool? _reverse;
			private readonly bool? _reverseOld;

			public InvtMultScope(TLine line)
			{
				_reverse = line.Qty < 0m;
				_line = line;

				if (_reverse == true)
				{
					_line.InvtMult *= -1;
					_line.Qty = -1m * _line.Qty.Value;
					_line.BaseQty = -1m * _line.BaseQty.Value;
				}
			}

			public InvtMultScope(TLine line, TLine oldLine)
				: this(line)
			{
				_reverseOld = oldLine.Qty < 0m;
				_oldLine = oldLine;

				if (_reverseOld == true)
				{
					_oldLine.InvtMult *= -1;
					_oldLine.Qty = -1m * _oldLine.Qty.Value;
					_oldLine.BaseQty = -1m * _oldLine.BaseQty.Value;
				}
			}

			void IDisposable.Dispose()
			{
				if (_reverse == true)
				{
					_line.InvtMult *= -1;
					_line.Qty = -1m * _line.Qty.Value;
					_line.BaseQty = -1m * _line.BaseQty.Value;
				}

				if (_reverseOld == true)
				{
					_oldLine.InvtMult *= -1;
					_oldLine.Qty = -1m * _oldLine.Qty.Value;
					_oldLine.BaseQty = -1m * _oldLine.BaseQty.Value;
				}
			}
		}
		#endregion

		#region KitProcessScope
		private class KitProcessScope : IDisposable
		{
			private readonly LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> _lsParent;

			public KitProcessScope(LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> lsParent, InventoryItem kitItem)
			{
				_lsParent = lsParent;
				_lsParent.KitInProcessing = kitItem;
			}

			void IDisposable.Dispose()
			{
				_lsParent.KitInProcessing = null;
			}
		}
		public IDisposable KitProcessingScope(InventoryItem kitItem) => kitItem != null ? new KitProcessScope(this, kitItem) : null;
		#endregion

		#region OperationScope
		private class OperationScope : IDisposable
		{
			private readonly LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> _lsParent;
			private readonly PXDBOperation _initOperation;
			private readonly bool _restoreToNormal;

			public OperationScope(LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> lsParent, PXDBOperation alterCurrentOperation, bool restoreToNormal)
			{
				_lsParent = lsParent;
				_restoreToNormal = restoreToNormal;
				_initOperation = _lsParent.CurrentOperation;
				_lsParent.CurrentOperation = alterCurrentOperation;
			}

			void IDisposable.Dispose()
			{
				_lsParent.CurrentOperation = _restoreToNormal ? PXDBOperation.Normal : _initOperation;
			}
		}

		/// <summary>
		/// Create a scope for changing current operation mode
		/// </summary>
		protected IDisposable OperationModeScope(PXDBOperation alterCurrentOperation, bool restoreToNormal = false) => new OperationScope(this, alterCurrentOperation, restoreToNormal);
		#endregion

		#region SuppressionScope
		private class SuppressionScope : IDisposable
		{
			private readonly LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> _lsParent;
			private readonly bool _initSuppressedMode;

			public SuppressionScope(LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> lsParent, PXDBOperation? alterCurrentOperation = null)
			{
				_lsParent = lsParent;
				_initSuppressedMode = _lsParent.SuppressedMode;
				_lsParent.SuppressedMode = true;
			}

			void IDisposable.Dispose()
			{
				_lsParent.SuppressedMode = _initSuppressedMode;
			}
		}

		/// <summary>
		/// Create a scope for suppressing of the major internal logic
		/// </summary>
		public IDisposable SuppressedModeScope(bool suppress) => suppress ? new SuppressionScope(this) : null;
		protected IDisposable SuppressedModeScope(PXDBOperation? alterCurrentOperation = null) => new SuppressionScope(this, alterCurrentOperation);
		#endregion

		#region ForcedUnattendedModeScope
		private class ForcedUnattendedModeScope : IDisposable
		{
			private readonly LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> _lsParent;

			public ForcedUnattendedModeScope(LineSplittingExtension<TGraph, TPrimary, TLine, TSplit> lsParent)
			{
				_lsParent = lsParent;
				_lsParent.UnattendedMode = true;
			}

			void IDisposable.Dispose()
			{
				_lsParent.UnattendedMode = false;
			}
		}

		/// <summary>
		/// Create a scope for suppressing of the UI logic
		/// </summary>
		public IDisposable ForceUnattendedModeScope(bool suppress) => suppress && !UnattendedMode ? new ForcedUnattendedModeScope(this) : null;
		#endregion
		#endregion
	}
}
