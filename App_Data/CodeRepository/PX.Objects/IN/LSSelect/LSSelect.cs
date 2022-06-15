using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.Common.Exceptions;
using PX.Objects.Common.Scopes;
using PX.Objects.CS;
using PX.Objects.PM;

using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.IN
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	[LSDynamicButton(new string[] { "generateLotSerial", "binLotSerial" },
					new string[] { Messages.Generate, Messages.BinLotSerial },
					TranslationKeyType = typeof(Messages))]
	public abstract class LSSelect<TLSMaster, TLSDetail, Where> : PXSelect<TLSMaster>, IEqualityComparer<TLSMaster>
		where TLSMaster : class, IBqlTable, ILSPrimary, new()
		where TLSDetail : class, IBqlTable, ILSDetail, new()
		where Where : IBqlWhere, new()
	{

		#region IEqualityComparer<TLSMaster> Members

		public bool Equals(TLSMaster x, TLSMaster y)
		{
			return this.Cache.ObjectsEqual(x, y) && x.InventoryID == y.InventoryID;
		}

		public int GetHashCode(TLSMaster obj)
		{
			unchecked
			{
				return this.Cache.GetObjectHashCode(obj) * 37 + obj.InventoryID.GetHashCode();
			}
		}
		#endregion

		#region State
		protected bool _InternallCall = false;
		protected PXDBOperation _Operation = PXDBOperation.Normal;
		protected string _MasterQtyField = "qty";
		private Type _MasterQtyFieldType;
		protected string _AvailField = PX.Objects.IN.Messages.Availability_Field;
		protected string _AvailFieldDisplayName = PX.Objects.IN.Messages.Availability_Field;
		protected bool _UnattendedMode = false;

		protected BqlCommand _detailbylotserialstatus;

		public string AvailabilityField
		{
			get
			{
				return _AvailField;
			}
		}

		protected Type MasterQtyField
		{
			get { return _MasterQtyFieldType; }
			set
			{
				if (!value.IsNested)
					throw new PXArgumentException("value", "Nested type is expected.");
				if (BqlCommand.GetItemType(value) != typeof(TLSMaster))
					throw new PXArgumentException("value", "'{0}' is expected.", typeof(TLSMaster).GetLongName());
				_MasterQtyFieldType = value;
				this._MasterQtyField = value.Name.ToLower();
				this._Graph.FieldVerifying.AddHandler(MasterCache.GetItemType(), _MasterQtyField, Master_Qty_FieldVerifying);
			}
		}

		protected PXCache MasterCache
		{
			get
			{
				return this._Graph.Caches[typeof(TLSMaster)];
			}
		}

		protected TLSMaster MasterCurrent
		{
			get
			{
				return (TLSMaster)MasterCache.Current;
			}
		}

		protected PXCache DetailCache
		{
			get
			{
				return this._Graph.Caches[typeof(TLSDetail)];
			}
		}

		protected virtual void SetEditMode()
		{
		}

		public virtual void SetEnabled(bool isEnabled)
		{
			this._Graph.Actions[Prefixed("binLotSerial")].SetEnabled(isEnabled);
		}

		protected bool _AllowInsert = true;
		public override bool AllowInsert
		{
			get
			{
				return this._AllowInsert;
			}
			set
			{
				this._AllowInsert = value;
				this.MasterCache.AllowInsert = value;

				SetEditMode();
			}
		}

		protected Type _MasterInventoryType;

		protected bool _AllowUpdate = true;
		public override bool AllowUpdate
		{
			get
			{
				return this._AllowUpdate;
			}
			set
			{
				this._AllowUpdate = value;
				this.MasterCache.AllowUpdate = value;
				this.DetailCache.AllowInsert = value;
				this.DetailCache.AllowUpdate = value;
				this.DetailCache.AllowDelete = value;

				SetEditMode();
			}
		}


		protected bool _AllowDelete = true;
		public override bool AllowDelete
		{
			get
			{
				return this._AllowDelete;
			}
			set
			{
				this._AllowDelete = value;
				this.MasterCache.AllowDelete = value;

				SetEditMode();
			}
		}

		protected bool Initialized;
		protected bool PrevCorrectionMode;
		protected bool PrevFullMode;

		protected bool CorrectionMode
		{
			get
			{
				return this._AllowUpdate && this._AllowInsert == false && this._AllowDelete == false;
			}
		}

		protected bool FullMode
		{
			get
			{
				return this._AllowUpdate && this._AllowInsert && this._AllowDelete;
			}
		}

		protected Type PrimaryViewType
		{
			get
			{
				if (_Graph.PrimaryItemType == null)
					throw new PXException(Messages.CantGetPrimaryView, _Graph.GetType().FullName);
				return _Graph.PrimaryItemType;
			}
		}

		/// <summary>
		/// Suppresses logic specific to UI
		/// </summary>
		public bool UnattendedMode
		{
			get
			{
				return _UnattendedMode;
			}
			set
			{
				_UnattendedMode = value;
			}
		}

		/// <summary>
		/// Suppresses major internal logic
		/// </summary>
		public bool SuppressedMode
		{
			get
			{
				return _InternallCall;
			}
			set
			{
				_InternallCall = value;
			}
		}

		/// <summary>
		/// Create a scope for suppressing of the major internal logic
		/// </summary>
		/// <returns>null if <paramref name="suppress"/> is false</returns>
		public IDisposable SuppressedModeScope(bool suppress)
		{
			return suppress
				? new LSSelectSuppressedModeScope<TLSMaster, TLSDetail, Where>(this)
				: null;
		}

		protected Dictionary<object, Counters> DetailCounters;

		#endregion

		#region Ctor

		public LSSelect(PXGraph graph)
			: base(graph)
		{
			graph.OnBeforePersist += OnBeforePersist;

			graph.RowSelected.AddHandler<TLSMaster>(Master_RowSelected);
			graph.RowInserted.AddHandler<TLSMaster>(Master_RowInserted);
			graph.RowUpdated.AddHandler<TLSMaster>(Master_RowUpdated);
			graph.RowDeleted.AddHandler<TLSMaster>(Master_RowDeleted);
			graph.RowPersisting.AddHandler<TLSMaster>(Master_RowPersisting);
			graph.RowPersisted.AddHandler<TLSMaster>(Master_RowPersisted);

			graph.RowInserting.AddHandler<TLSDetail>(Detail_RowInserting);
			graph.RowInserted.AddHandler<TLSDetail>(Detail_RowInserted);
			graph.RowUpdated.AddHandler<TLSDetail>(Detail_RowUpdated);
			graph.RowDeleted.AddHandler<TLSDetail>(Detail_RowDeleted);
			graph.RowPersisting.AddHandler<TLSDetail>(Detail_RowPersisting);
			graph.RowPersisted.AddHandler<TLSDetail>(Detail_RowPersisted);

			Type inventoryType = null;
			Type subItemType = null;
			Type siteType = null;
			Type locationType = null;
			Type lotSerialNbrType = null;
			UnattendedMode = graph.UnattendedMode;

			foreach (PXEventSubscriberAttribute attr in this.MasterCache.GetAttributesReadonly(null))
			{
				if (attr is BaseInventoryAttribute)
				{
					_MasterInventoryType = this.DetailCache.GetBqlField(attr.FieldName);
				}
			}

			foreach (PXEventSubscriberAttribute attr in this.DetailCache.GetAttributesReadonly(null))
			{
				if (attr is INUnitAttribute)
				{
					graph.FieldDefaulting.AddHandler(this.DetailCache.GetItemType(), attr.FieldName, Detail_UOM_FieldDefaulting);
				}

				if (attr is BaseInventoryAttribute)
				{
					inventoryType = this.DetailCache.GetBqlField(attr.FieldName);
				}

				if (attr is SubItemAttribute)
				{
					subItemType = this.DetailCache.GetBqlField(attr.FieldName);
				}

				if (attr is SiteAttribute)
				{
					siteType = this.DetailCache.GetBqlField(attr.FieldName);
				}

				if (attr is LocationAttribute)
				{
					locationType = this.DetailCache.GetBqlField(attr.FieldName);
				}

				if (attr is INLotSerialNbrAttribute)
				{
					lotSerialNbrType = this.DetailCache.GetBqlField(attr.FieldName);
				}

				if (attr is PXDBQuantityAttribute && ((PXDBQuantityAttribute)attr).KeyField != null)
				{
					graph.FieldVerifying.AddHandler(this.DetailCache.GetItemType(), attr.FieldName, Detail_Qty_FieldVerifying);
				}
			}

			_detailbylotserialstatus = BqlCommand.CreateInstance(
				typeof(Select<,>),
				typeof(TLSDetail),
				typeof(Where<,,>),
				inventoryType,
				typeof(Equal<>),
				typeof(Required<>),
				inventoryType,
				typeof(And<,,>),
				subItemType,
				typeof(Equal<>),
				typeof(Required<>),
				subItemType,
				typeof(And<,,>),
				siteType,
				typeof(Equal<>),
				typeof(Required<>),
				siteType,
				typeof(And<,,>),
				locationType,
				typeof(Equal<>),
				typeof(Required<>),
				locationType,
				typeof(And<,,>),
				lotSerialNbrType,
				typeof(Equal<>),
				typeof(Required<>),
				lotSerialNbrType,
				typeof(And<>),
				typeof(Where));

			_AvailFieldDisplayName = PXMessages.LocalizeNoPrefix(PX.Objects.IN.Messages.Availability_Field);
			graph.Caches[typeof(TLSMaster)].Fields.Add(_AvailField);
			graph.FieldSelecting.AddHandler(typeof(TLSMaster), _AvailField, Availability_FieldSelecting);

			graph.Views.Add(Prefixed("lotseropts"), new PXView(graph, false, new Select<LotSerOptions>(), new PXSelectDelegate(GetLotSerialOpts)));
			graph.RowPersisting.AddHandler<LotSerOptions>(LotSerOptions_RowPersisting);
			graph.RowSelected.AddHandler<LotSerOptions>(LotSerOptions_RowSelected);
			graph.FieldSelecting.AddHandler(typeof(LotSerOptions), "StartNumVal", LotSerOptions_StartNumVal_FieldSelecting);
			graph.FieldVerifying.AddHandler(typeof(LotSerOptions), "StartNumVal", LotSerOptions_StartNumVal_FieldVerifying);

			if (!graph.Views.Caches.Contains(typeof(INLotSerClassLotSerNumVal)))
				graph.Views.Caches.Add(typeof(INLotSerClassLotSerNumVal));
			if (!graph.Views.Caches.Contains(typeof(InventoryItemLotSerNumVal)))
				graph.Views.Caches.Add(typeof(InventoryItemLotSerNumVal));

			if (_MasterInventoryType != null)
			{
				graph.FieldVerifying.AddHandler(typeof(TLSMaster), _MasterInventoryType.Name, Master_InventoryID_FieldVerifying);
			}

			AddAction(Prefixed("generateLotSerial"), Messages.Generate, true, GenerateLotSerial, PXCacheRights.Update);
			AddAction(Prefixed("binLotSerial"), Messages.BinLotSerial, true, BinLotSerial, PXCacheRights.Select);

			DetailCounters = new Dictionary<object, Counters>();

			PXParentAttribute.SetLeaveChildren(DetailCache, true, typeof(TLSMaster));
		}
		#endregion

		#region Implementation

		protected string Prefixed(string name)
		{
			return string.Format("{0}_{1}", GetType().Name, name);
		}

		protected void AddAction(string name, string displayName, bool visible, PXButtonDelegate handler, PXCacheRights EnableRights)
		{
			var uiAtt = new PXUIFieldAttribute
			{
				DisplayName = PXMessages.LocalizeNoPrefix(displayName),
				MapEnableRights = EnableRights,
			};
			if (!visible) uiAtt.Visible = false;
			var buttAttr = new PXButtonAttribute() { DisplayOnMainToolbar = false };
			var addAttrs = new List<PXEventSubscriberAttribute> { uiAtt, buttAttr };
			_Graph.Actions[name] = (PXAction)Activator.CreateInstance(typeof(PXNamedAction<>).MakeGenericType(
				new[] { PrimaryViewType }), new object[] { _Graph, name, handler, addAttrs.ToArray() });
		}

		public virtual IEnumerable BinLotSerial(PXAdapter adapter)
		{
			View.AskExt(true);
			return adapter.Get();
		}

		protected virtual void LotSerOptions_StartNumVal_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (MasterCurrent == null) return;
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(MasterCache, MasterCurrent.InventoryID);
			LotSerOptions opt = (LotSerOptions)_Graph.Caches[typeof(LotSerOptions)].Current;
			if (item == null || opt == null) return;

			ILotSerNumVal lotSerNum = ReadLotSerNumVal(MasterCache, item);
			INLotSerialNbrAttribute.LSParts parts = INLotSerialNbrAttribute.GetLSParts(MasterCache, item, lotSerNum);
			if (string.IsNullOrEmpty(((string)e.NewValue)) || ((string)e.NewValue).Length < parts.len)
			{
				opt.StartNumVal = null;
				throw new PXSetPropertyException(Messages.TooShortNum, parts.len);
			}
		}

		public virtual IEnumerable GenerateLotSerial(PXAdapter adapter)
		{
			LotSerOptions opt = (LotSerOptions)_Graph.Caches[typeof(LotSerOptions)].Current ?? (LotSerOptions)GetLotSerialOpts().FirstOrDefault_();
			if (opt?.StartNumVal == null || opt.Qty == null)
				return adapter.Get();

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(MasterCache, MasterCurrent.InventoryID);
			var lsClass = (INLotSerClass)item;
			if (lsClass == null)
				return adapter.Get();

			ILotSerNumVal lotSerNum = ReadLotSerNumVal(MasterCache, item);

			string lotSerialNbr = null;
			INLotSerialNbrAttribute.LSParts parts = INLotSerialNbrAttribute.GetLSParts(MasterCache, lsClass, lotSerNum);
			string numVal = opt.StartNumVal.Substring(parts.nidx, parts.nlen);
			string numStr = opt.StartNumVal.Substring(0, parts.flen) + new string('0', parts.nlen) + opt.StartNumVal.Substring(parts.lidx, parts.llen);

			try
			{
				MasterCurrent.LotSerialNbr = null;

				List<TLSDetail> existingSplits = new List<TLSDetail>();
				if (lsClass.LotSerTrack == INLotSerTrack.LotNumbered)
				{
					foreach (TLSDetail split in PXParentAttribute.SelectSiblings(DetailCache, null, typeof(TLSMaster)))
					{
						existingSplits.Add(split);
					}
				}

				if (lsClass.LotSerTrack != INLotSerTrack.LotNumbered || (opt.Qty != 0 && MasterCurrent.BaseQty != 0m))
				{
					CreateNumbers(MasterCache, MasterCurrent, (decimal)opt.Qty, true);
				}

				foreach (TLSDetail split in PXParentAttribute.SelectSiblings(DetailCache, null, typeof(TLSMaster)))
				{
					if (string.IsNullOrEmpty(split.AssignedNbr) ||
						!INLotSerialNbrAttribute.StringsEqual(split.AssignedNbr, split.LotSerialNbr)) continue;

					TLSDetail copy = PXCache<TLSDetail>.CreateCopy(split);

					if (lotSerialNbr != null)
						numVal = AutoNumberAttribute.NextNumber(numVal);

					if ((decimal)opt.Qty != split.Qty && lsClass.LotSerTrack == INLotSerTrack.LotNumbered && !existingSplits.Contains(split))
					{
						split.BaseQty = (decimal)opt.Qty;
						split.Qty = (decimal)opt.Qty;
					}

					lotSerialNbr = INLotSerialNbrAttribute.UpdateNumber(split.AssignedNbr, numStr, numVal);
					DetailCache.SetValue(split, nameof(ILSMaster.LotSerialNbr), lotSerialNbr);
					DetailCache.RaiseRowUpdated(split, copy);
				}
			}
			catch (Exception)
			{
				UpdateParent(MasterCache, MasterCurrent);
			}

			if (lotSerialNbr != null)
				UpdateLotSerNumVal(lotSerNum, numVal, item);
			return adapter.Get();
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
				cache = _Graph.Caches[type];
				cache.Insert(lotSerNum);
			}
			else
			{
				type = lotSerNum.GetType();
				cache = _Graph.Caches[type];

				var copy = (ILotSerNumVal)cache.CreateCopy(lotSerNum);
				copy.LotSerNumVal = value;
				cache.Update(copy);
			}
		}

		public virtual IEnumerable GetLotSerialOpts()
		{
			LotSerOptions opt = new LotSerOptions();
			PXResult<InventoryItem, INLotSerClass> item = null;
			if (MasterCurrent != null)
			{
				opt.UnassignedQty = MasterCurrent.UnassignedQty;
				item = ReadInventoryItem(MasterCache, MasterCurrent.InventoryID);
			}
			if (item != null && (INLotSerClass)item != null)
			{
				var lsClass = (INLotSerClass)item;
				bool disabled;
				bool allowGernerate;
				using (InvtMultScope<TLSMaster> ms = new InvtMultScope<TLSMaster>(MasterCurrent))
				{
					INLotSerTrack.Mode mode = GetTranTrackMode(MasterCurrent, lsClass);
					disabled = (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Manual) != 0);
					allowGernerate = (mode & INLotSerTrack.Mode.Create) != 0;
				}
				if (!disabled && AllowUpdate)
				{
					var lotSerNum = ReadLotSerNumVal(MasterCache, item);

					string numval = AutoNumberAttribute.NextNumber(lotSerNum == null || string.IsNullOrEmpty(lotSerNum.LotSerNumVal)
						? new string('0', INLotSerialNbrAttribute.GetNumberLength(null))
						: lotSerNum.LotSerNumVal);
					string emptynbr = INLotSerialNbrAttribute.GetNextNumber(MasterCache, lsClass, lotSerNum);
					string format = INLotSerialNbrAttribute.GetNextFormat(lsClass, lotSerNum);
					opt.StartNumVal = INLotSerialNbrAttribute.UpdateNumber(format, emptynbr, numval);
					opt.AllowGenerate = allowGernerate;
					if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered)
						opt.Qty = (int)(MasterCurrent.UnassignedQty ?? 0);
					else opt.Qty = (MasterCurrent.UnassignedQty ?? 0);
					opt.IsSerial = lsClass.LotSerTrack == INLotSerTrack.SerialNumbered;
				}
			}
			_Graph.Caches[typeof(LotSerOptions)].Clear();
			opt = (LotSerOptions)_Graph.Caches[typeof(LotSerOptions)].Insert(opt);
			_Graph.Caches[typeof(LotSerOptions)].IsDirty = false;
			yield return opt;
		}

		public virtual void Master_InventoryID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (sender.Graph.UnattendedMode || sender.Graph.IsImport)
				return;
			var item = InventoryItem.PK.Find(sender.Graph, (int?)e.NewValue);
			if (item != null && item.KitItem == true && item.StkItem == false &&
				((INKitSpecStkDet)PXSelectReadonly<INKitSpecStkDet, Where<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>>>.SelectWindowed(sender.Graph, 0, 1, e.NewValue)) == null &&
				((INKitSpecNonStkDet)PXSelectReadonly<INKitSpecNonStkDet, Where<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>>>.SelectWindowed(sender.Graph, 0, 1, e.NewValue)) == null)
			{
				e.NewValue = null;
				throw new PXSetPropertyException(Messages.EmptyKitNotAllowed, PXErrorLevel.Error);
			}
		}

		protected virtual void LotSerOptions_StartNumVal_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			LotSerOptions opt = (LotSerOptions)e.Row;
			if (opt == null || opt.StartNumVal == null || MasterCurrent == null) return;
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(MasterCache, MasterCurrent.InventoryID);
			var lotSerNum = ReadLotSerNumVal(MasterCache, item);
			string mask = INLotSerialNbrAttribute.GetDisplayMask(MasterCache, item, lotSerNum);
			if (mask == null) return;
			e.ReturnState = PXStringState.CreateInstance(e.ReturnState, mask.Length, true, "StartNumVal", false, 1, mask, null, null, null, null);
		}

		protected virtual void LotSerOptions_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			LotSerOptions opt = (LotSerOptions)e.Row;

			bool enabled = IsLotSerOptionsEnabled(sender, opt);

			PXUIFieldAttribute.SetEnabled<LotSerOptions.startNumVal>(sender, opt, enabled);
			PXUIFieldAttribute.SetEnabled<LotSerOptions.qty>(sender, opt, enabled);
			PXDBDecimalAttribute.SetPrecision(sender, opt, "Qty", (opt.IsSerial == true ? 0 : CommonSetupDecPl.Qty));
			_Graph.Actions[Prefixed("generateLotSerial")].SetEnabled(opt != null && opt.AllowGenerate == true && enabled);
		}

		protected virtual bool IsLotSerOptionsEnabled(PXCache sender, LotSerOptions opt)
		{
			return opt != null && opt.StartNumVal != null;
		}

		protected virtual void LotSerOptions_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual INLotSerTrack.Mode GetTranTrackMode(ILSMaster row, INLotSerClass lotSerClass)
		{
			string tranType = row.TranType;
			if (lotSerClass.LotSerAssign == INLotSerAssign.WhenUsed && row.InvtMult < 0 && row.IsIntercompany == true)
			{
				tranType = INTranType.Transfer;
			}
			return INLotSerialNbrAttribute.TranTrackMode(lotSerClass, tranType, row.InvtMult);
		}

		public class InvtMultScope<TNode> : IDisposable
			where TNode : class, ILSMaster
		{
			private TNode _item;
			private TNode _olditem;
			private bool? _Reverse;
			private bool? _ReverseOld;

			public InvtMultScope(TNode item)
			{
				_Reverse = (item.Qty < 0m);
				_item = item;

				if (_Reverse == true)
				{
					_item.InvtMult *= (short)-1;
					_item.Qty = -1m * (decimal)_item.Qty;
					_item.BaseQty = -1m * (decimal)_item.BaseQty;
				}
			}

			public InvtMultScope(TNode item, TNode olditem)
				: this(item)
			{
				_ReverseOld = (olditem.Qty < 0m);
				_olditem = olditem;

				if (_ReverseOld == true)
				{
					_olditem.InvtMult *= (short)-1;
					_olditem.Qty = -1m * (decimal)_olditem.Qty;
					_olditem.BaseQty = -1m * (decimal)_olditem.BaseQty;
				}
			}

			void IDisposable.Dispose()
			{
				if (_Reverse == true)
				{
					_item.InvtMult *= (short)-1;
					_item.Qty = -1m * (decimal)_item.Qty;
					_item.BaseQty = -1m * (decimal)_item.BaseQty;
				}

				if (_ReverseOld == true)
				{
					_olditem.InvtMult *= (short)-1;
					_olditem.Qty = -1m * (decimal)_olditem.Qty;
					_olditem.BaseQty = -1m * (decimal)_olditem.BaseQty;
				}
			}
		}

		public class PXRowInsertedEventArgs<Table>
			where Table : class, IBqlTable
		{
			protected Table _Row;
			protected bool Cancel;
			public readonly bool ExternalCall;

			public Table Row
			{
				get
				{
					return this._Row;
				}
				set
				{
					this._Row = value;
				}
			}

			public PXRowInsertedEventArgs(Table Row, bool ExternalCall)
			{
				this.Row = Row;
				this.ExternalCall = ExternalCall;
			}

			public PXRowInsertedEventArgs(PXRowInsertedEventArgs e)
				: this((Table)e.Row, e.ExternalCall)
			{
			}
		}

		public class PXRowUpdatedEventArgs<Table>
			where Table : class, IBqlTable
		{
			protected Table _Row;
			protected Table _OldRow;
			protected bool Cancel;
			public readonly bool ExternalCall;

			public Table Row
			{
				get
				{
					return this._Row;
				}
				set
				{
					this._Row = value;
				}
			}

			public Table OldRow
			{
				get
				{
					return this._OldRow;
				}
				set
				{
					this._OldRow = value;
				}
			}

			public PXRowUpdatedEventArgs(Table Row, Table OldRow, bool ExternalCall)
			{
				this.Row = Row;
				this.OldRow = OldRow;
				this.ExternalCall = ExternalCall;
			}

			public PXRowUpdatedEventArgs(PXRowUpdatedEventArgs e)
				: this((Table)e.Row, (Table)e.OldRow, e.ExternalCall)
			{
			}
		}

		public class PXRowDeletedEventArgs<Table>
			where Table : class, IBqlTable
		{
			protected Table _Row;
			protected bool Cancel;
			public readonly bool ExternalCall;

			public Table Row
			{
				get
				{
					return this._Row;
				}
				set
				{
					this._Row = value;
				}
			}

			public PXRowDeletedEventArgs(Table Row, bool ExternalCall)
			{
				this.Row = Row;
				this.ExternalCall = ExternalCall;
			}

			public PXRowDeletedEventArgs(PXRowDeletedEventArgs e)
				: this((Table)e.Row, e.ExternalCall)
			{
			}
		}

		public abstract TLSDetail Convert(TLSMaster item);

		protected virtual object Convert<T>(ILSMaster item)
			where T : class, ILotSerial, IStatus, IBqlTable, new()
		{
			if (typeof(T) == typeof(INLotSerialStatus))
				return INLotSerialStatus(item);
			else if (typeof(T) == typeof(PMLotSerialStatus))
				return PMLotSerialStatus(item);

			throw new PXArgumentException();

		}

		protected virtual INLotSerialStatus INLotSerialStatus(ILSMaster item)
		{
			INLotSerialStatus ret = new INLotSerialStatus();
			ret.InventoryID = item.InventoryID;
			ret.SiteID = item.SiteID;
			ret.LocationID = item.LocationID;
			ret.SubItemID = item.SubItemID;
			ret.LotSerialNbr = item.LotSerialNbr;

			return ret;
		}

		protected virtual PMLotSerialStatus PMLotSerialStatus(ILSMaster item)
		{
			PMLotSerialStatus ret = new PMLotSerialStatus();
			ret.InventoryID = item.InventoryID;
			ret.SiteID = item.SiteID;
			ret.LocationID = item.LocationID;
			ret.SubItemID = item.SubItemID;
			ret.LotSerialNbr = item.LotSerialNbr;
			ret.ProjectID = item.ProjectID;
			ret.TaskID = item.TaskID.GetValueOrDefault(0);

			return ret;
		}

		public virtual string FormatQty(decimal? value)
		{
			return (value == null) ? string.Empty : ((decimal)value).ToString("N" + CommonSetupDecPl.Qty.ToString(), System.Globalization.NumberFormatInfo.CurrentInfo);
		}

		protected virtual TLSMaster SelectMaster(PXCache sender, TLSDetail row)
		{
			return (TLSMaster)PXParentAttribute.SelectParent(sender, row, typeof(TLSMaster));
		}

		protected virtual bool SameInventoryItem(ILSMaster a, ILSMaster b)
		{
			return a.InventoryID == b.InventoryID;
		}

		protected virtual object[] SelectDetail(PXCache sender, TLSMaster row)
		{
			object[] ret = PXParentAttribute.SelectChildren(sender, row, typeof(TLSMaster));

			return Array.FindAll<object>(ret, new Predicate<object>(delegate (object a)
			{
				return SameInventoryItem((ILSMaster)a, (ILSMaster)row);
			}));
		}

		protected virtual object[] SelectDetail(PXCache sender, TLSDetail row)
		{
			object[] ret = PXParentAttribute.SelectSiblings(sender, row, typeof(TLSMaster));

			return Array.FindAll<object>(ret, new Predicate<object>(delegate (object a)
			{
				return SameInventoryItem((ILSMaster)a, (ILSMaster)row);
			}));
		}

		protected object[] SelectDetailOrdered(PXCache sender, TLSMaster row)
		{
			return SelectDetailOrdered(sender, Convert(row));
		}

		protected virtual object[] SelectDetailOrdered(PXCache sender, TLSDetail row)
		{
			object[] ret = SelectDetail(sender, row);

			Array.Sort<object>(ret, new Comparison<object>(delegate (object a, object b)
			{
				object aSplitLineNbr = ((ILSDetail)a).SplitLineNbr;
				object bSplitLineNbr = ((ILSDetail)b).SplitLineNbr;

				return ((IComparable)aSplitLineNbr).CompareTo(bSplitLineNbr);
			}));

			return ret;
		}

		protected object[] SelectDetailReversed(PXCache sender, TLSMaster row)
		{
			return SelectDetailReversed(sender, Convert(row));
		}

		protected virtual object[] SelectDetailReversed(PXCache sender, TLSDetail row)
		{
			object[] ret = SelectDetail(sender, row);

			Array.Sort<object>(ret, new Comparison<object>(delegate (object a, object b)
			{
				object aSplitLineNbr = ((ILSDetail)a).SplitLineNbr;
				object bSplitLineNbr = ((ILSDetail)b).SplitLineNbr;

				return -((IComparable)aSplitLineNbr).CompareTo(bSplitLineNbr);
			}));

			return ret;
		}

		protected virtual void ExpireCachedItems(PXCache sender, ILSMaster Row)
		{
			ExpireCached(sender.Graph.Caches[typeof(INLotSerialStatus)], INLotSerialStatus(Row));
			if (UseProjectAvailability(sender, Row))
				ExpireCached(sender.Graph.Caches[typeof(PMLotSerialStatus)], PMLotSerialStatus(Row));
		}

		protected virtual void ExpireCached(PXCache sender, object item)
		{
			object cached = sender.Locate(item);

			if (cached != null && (sender.GetStatus(cached) == PXEntryStatus.Held || sender.GetStatus(cached) == PXEntryStatus.Notchanged))
			{
				sender.SetStatus(cached, PXEntryStatus.Notchanged);
				sender.Remove(cached);
				sender.ClearQueryCache();
			}
		}

		protected virtual PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(PXCache sender, int? inventoryID)
		{
			if (inventoryID == null)
				return null;
			var inventory = InventoryItem.PK.Find(sender.Graph, inventoryID);
			if (inventory == null)
				throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, Messages.InventoryItem, inventoryID);
			INLotSerClass lotSerClass;
			if (inventory.StkItem == true)
			{
				lotSerClass = INLotSerClass.PK.Find(sender.Graph, inventory.LotSerClassID);
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
		/// <param name="sender">cache</param>
		/// <param name="item">settings</param>
		/// <returns></returns>
		protected virtual ILotSerNumVal ReadLotSerNumVal(PXCache sender, PXResult<InventoryItem, INLotSerClass> item)
		{
			return INLotSerialNbrAttribute.ReadLotSerNumVal(sender.Graph, item);
		}

		public virtual void CreateNumbers(PXCache sender, TLSMaster Row, decimal BaseQty)
		{
			CreateNumbers(sender, Row, BaseQty, false);
		}

		public virtual void CreateNumbers(PXCache sender, TLSMaster Row, decimal BaseQty, bool ForceAutoNextNbr)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
			TLSDetail split = Convert(Row);

			if (Row != null)
				DetailCounters.Remove(Row);

			if (!ForceAutoNextNbr && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered &&
				((INLotSerClass)item).AutoSerialMaxCount > 0 && ((INLotSerClass)item).AutoSerialMaxCount < BaseQty)
			{
				BaseQty = ((INLotSerClass)item).AutoSerialMaxCount.GetValueOrDefault();
			}

			INLotSerTrack.Mode mode = GetTranTrackMode(Row, item);
			ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
			foreach (TLSDetail lssplit in INLotSerialNbrAttribute.CreateNumbers<TLSDetail>(sender, item, lotSerNum, mode, ForceAutoNextNbr, BaseQty))
			{
				string LotSerTrack = (mode & INLotSerTrack.Mode.Create) > 0 ? ((INLotSerClass)item).LotSerTrack : INLotSerTrack.NotNumbered;

				split.SplitLineNbr = null;
				split.LotSerialNbr = lssplit.LotSerialNbr;
				split.AssignedNbr = lssplit.AssignedNbr;
				split.LotSerClassID = lssplit.LotSerClassID;

				if (!string.IsNullOrEmpty(Row.LotSerialNbr) &&
					((LotSerTrack == INLotSerTrack.SerialNumbered && Row.Qty == 1m) ||
						LotSerTrack == INLotSerTrack.LotNumbered))
				{
					split.LotSerialNbr = Row.LotSerialNbr;
				}

				if (LotSerTrack == "S")
				{
					split.UOM = null;
					split.Qty = 1m;
					split.BaseQty = 1m;
				}
				else
				{
					split.UOM = null;
					split.BaseQty = BaseQty;
					split.Qty = BaseQty;
				}
				if (((INLotSerClass)item).LotSerTrackExpiration == true)
					split.ExpireDate = ExpireDateByLot(sender, split, Row);

				DetailCache.Insert(PXCache<TLSDetail>.CreateCopy(split));
				BaseQty -= (decimal)split.BaseQty;
			}

			if (BaseQty > 0m && (((INLotSerClass)item).LotSerTrack != "S" || decimal.Remainder(BaseQty, 1m) == 0m))
			{
				Row.UnassignedQty += BaseQty;
			}
			else if (BaseQty > 0m)
			{
				TLSMaster oldrow = PXCache<TLSMaster>.CreateCopy(Row);

				Row.BaseQty -= BaseQty;
				SetMasterQtyFromBase(sender, Row);

				if (Math.Abs((Decimal)oldrow.Qty - (Decimal)Row.Qty) >= 0.0000005m)
				{
					sender.RaiseFieldUpdated(_MasterQtyField, Row, oldrow.Qty);
					sender.RaiseRowUpdated(Row, oldrow);
				}
			}
			if (Row.UnassignedQty > 0)
				sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
		}

		public virtual void CreateNumbers(PXCache sender, TLSMaster Row)
		{
			CreateNumbers(sender, Row, (decimal)Row.BaseQty);
		}

		public virtual void TruncateNumbers(PXCache sender, TLSMaster Row, decimal BaseQty)
		{
			if (UseProjectAvailability(sender, Row))
				TruncateNumbersImp<PMLotSerialStatus>(sender, Row, BaseQty);
			else
				TruncateNumbersImp<INLotSerialStatus>(sender, Row, BaseQty);
		}

		protected virtual void TruncateNumbersImp<T>(PXCache sender, TLSMaster Row, decimal BaseQty)
			where T : class, ILotSerial, IStatus, IBqlTable, new()
		{
			PXCache cache = sender.Graph.Caches[typeof(TLSDetail)];
			PXCache lscache = sender.Graph.Caches[typeof(T)];

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);

			if (((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered && Math.Abs(Decimal.Floor(BaseQty) - BaseQty) > 0.0000005m)
			{
				TLSMaster oldrow = PXCache<TLSMaster>.CreateCopy(Row);
				Row.BaseQty += BaseQty - Decimal.Truncate(BaseQty);
				SetMasterQtyFromBase(sender, Row);

				sender.RaiseFieldUpdated(_MasterQtyField, Row, oldrow.Qty);
				sender.RaiseRowUpdated(Row, oldrow);

				BaseQty = Decimal.Truncate(BaseQty);
			}

			if (Row != null)
				DetailCounters.Remove(Row);
			if (Row.UnassignedQty > 0m)
			{
				if (Row.UnassignedQty >= BaseQty)
				{
					Row.UnassignedQty -= BaseQty;
					BaseQty = 0m;
				}
				else
				{
					BaseQty -= (decimal)Row.UnassignedQty;
					Row.UnassignedQty = 0m;
				}
			}

			foreach (object detail in SelectDetailReversed(cache, (TLSMaster)Row))
			{
				if (BaseQty >= ((ILSDetail)detail).BaseQty)
				{
					BaseQty -= (decimal)((ILSDetail)detail).BaseQty;
					cache.Delete(detail);

					ExpireCached(lscache, Convert<T>((TLSDetail)detail));
				}
				else
				{
					TLSDetail newdetail = PXCache<TLSDetail>.CreateCopy((TLSDetail)detail);
					newdetail.BaseQty -= BaseQty;
					SetDetailQtyWithMaster(sender, newdetail, Row);

					cache.Update(newdetail);

					ExpireCached(lscache, Convert<T>((TLSDetail)detail));
					break;
				}
			}
		}

		public virtual void UpdateNumbers(PXCache sender, object Row)
		{
			PXCache cache = sender.Graph.Caches[typeof(TLSDetail)];

			if (Row is TLSMaster)
				DetailCounters.Remove((TLSMaster)Row);
			foreach (object detail in SelectDetail(cache, (TLSMaster)Row))
			{
				TLSDetail newdetail = PXCache<TLSDetail>.CreateCopy((TLSDetail)detail);

				if (((ILSMaster)Row).LocationID == null && newdetail.LocationID != null && cache.GetStatus(newdetail) == PXEntryStatus.Inserted && newdetail.Qty == 0m)
				{
					cache.Delete(newdetail);
				}
				else
				{
					newdetail.SubItemID = ((ILSMaster)Row).SubItemID ?? newdetail.SubItemID;
					newdetail.SiteID = ((ILSMaster)Row).SiteID;
					newdetail.LocationID = ((ILSMaster)Row).LocationID ?? newdetail.LocationID;
					newdetail.ExpireDate = ExpireDateByLot(sender, newdetail, (ILSMaster)Row);

					cache.Update(newdetail);
				}
			}
		}

		public virtual void UpdateNumbers(PXCache sender, object Row, decimal BaseQty)
		{
			if (UseProjectAvailability(sender, (TLSMaster)Row))
			{
				UpdateNumbersImp<PMLotSerialStatus>(sender, Row, BaseQty);
			}
			else
			{
				UpdateNumbersImp<INLotSerialStatus>(sender, Row, BaseQty);
			}
		}

		protected virtual void UpdateNumbersImp<T>(PXCache sender, object Row, decimal BaseQty)
			where T : class, ILotSerial, IStatus, IBqlTable, new()
		{
			PXCache lscache = sender.Graph.Caches[typeof(T)];

			bool deleteflag = false;

			if (Row is TLSMaster)
				DetailCounters.Remove((TLSMaster)Row);

			if (_Operation == PXDBOperation.Update)
			{
				foreach (object detail in SelectDetail(DetailCache, (TLSMaster)Row))
				{
					if (deleteflag)
					{
						DetailCache.Delete(detail);
						ExpireCached(lscache, Convert<T>((TLSDetail)detail));
					}
					else
					{
						TLSDetail newdetail = PXCache<TLSDetail>.CreateCopy((TLSDetail)detail);

						var master = (TLSMaster)Row;
						newdetail.SubItemID = master.SubItemID;
						newdetail.SiteID = master.SiteID;
						newdetail.LocationID = master.LocationID;
						newdetail.LotSerialNbr = master.LotSerialNbr;
						newdetail.ExpireDate = ExpireDateByLot(sender, newdetail, master);

						newdetail.BaseQty = master.BaseQty;
						SetDetailQtyWithMaster(sender, newdetail, master);

						DetailCache.Update(newdetail);

						ExpireCached(lscache, Convert<T>((TLSDetail)detail));

						deleteflag = true;
					}
				}
			}

			if (!deleteflag)
			{
				TLSDetail newdetail = Convert((TLSMaster)Row);
				newdetail.SplitLineNbr = null;
				newdetail.ExpireDate = ExpireDateByLot(sender, newdetail, (TLSMaster)Row);
				DefaultLotSerialNbr(DetailCache, newdetail);

				if (string.IsNullOrEmpty(newdetail.LotSerialNbr) && !string.IsNullOrEmpty(((TLSMaster)Row).LotSerialNbr))
				{
					newdetail.LotSerialNbr = ((TLSMaster)Row).LotSerialNbr;
				}

				DetailCache.Insert(newdetail);

				ExpireCached(lscache, Convert<T>((TLSDetail)newdetail));
			}
		}

		protected virtual DateTime? ExpireDateByLot(PXCache sender, ILSMaster item, ILSMaster master) => LSSelect.ExpireDateByLot(sender.Graph, item, master);

		public virtual void Summarize(PXCache sender, object Row, ILotSerial LSRow)
		{
			PXView view = sender.Graph.TypedViews.GetView(_detailbylotserialstatus, false);
			foreach (TLSDetail det in view.SelectMultiBound(new object[] { Row }, LSRow.InventoryID, LSRow.SubItemID, LSRow.SiteID, LSRow.LocationID, LSRow.LotSerialNbr))
			{
				((IStatus)LSRow).QtyOnHand += (decimal?)det.InvtMult * det.BaseQty;
			}
			sender.SetStatus(LSRow, PXEntryStatus.Held);
		}

		public virtual PXSelectBase<INLotSerialStatus> GetSerialStatusCmd(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<INLotSerialStatus> cmd = this.GetSerialStatusCmdBase(sender, Row, item);
			this.AppendSerialStatusCmdWhere(cmd, Row, item);
			this.AppendSerialStatusCmdOrderBy(cmd, Row, item);

			return cmd;
		}

		public virtual PXSelectBase<PMLotSerialStatus> GetSerialStatusCmdProject(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<PMLotSerialStatus> cmd = this.GetSerialStatusCmdBaseProject(sender, Row, item);
			this.AppendSerialStatusCmdWhereProject(cmd, Row, item);
			this.AppendSerialStatusCmdOrderByProject(cmd, Row, item);

			return cmd;
		}

		protected virtual PXSelectBase<INLotSerialStatus> GetSerialStatusCmdBase(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			return new PXSelectJoin<INLotSerialStatus,
				InnerJoin<INLocation, On<INLocation.locationID, Equal<INLotSerialStatus.locationID>>>,
				Where<INLotSerialStatus.inventoryID, Equal<Current<INLotSerialStatus.inventoryID>>,
					And<INLotSerialStatus.siteID, Equal<Current<INLotSerialStatus.siteID>>,
					And<INLotSerialStatus.qtyOnHand, Greater<decimal0>>>>>(sender.Graph);
		}

		protected virtual PXSelectBase<PMLotSerialStatus> GetSerialStatusCmdBaseProject(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			return new PXSelectJoin<PMLotSerialStatus,
				InnerJoin<INLocation, On<INLocation.locationID, Equal<PMLotSerialStatus.locationID>>>,
				Where<PMLotSerialStatus.inventoryID, Equal<Current<PMLotSerialStatus.inventoryID>>,
					And<PMLotSerialStatus.siteID, Equal<Current<PMLotSerialStatus.siteID>>,
					And<PMLotSerialStatus.projectID, Equal<Current<PMLotSerialStatus.projectID>>,
					And<PMLotSerialStatus.taskID, Equal<Current<PMLotSerialStatus.taskID>>,
					And<PMLotSerialStatus.qtyOnHand, Greater<decimal0>>>>>>>(sender.Graph);
		}

		protected virtual void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, TLSMaster Row, INLotSerClass lotSerClass)
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
				cmd.WhereAnd<Where<INLocation.salesValid, Equal<boolTrue>>>();
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr, Equal<Current<INLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		protected virtual void AppendSerialStatusCmdWhereProject(PXSelectBase<PMLotSerialStatus> cmd, TLSMaster Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
			{
				cmd.WhereAnd<Where<PMLotSerialStatus.subItemID, Equal<Current<PMLotSerialStatus.subItemID>>>>();
			}
			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<PMLotSerialStatus.locationID, Equal<Current<PMLotSerialStatus.locationID>>>>();
			}
			else
			{
				cmd.WhereAnd<Where<INLocation.salesValid, Equal<boolTrue>>>();
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<PMLotSerialStatus.lotSerialNbr, Equal<Current<PMLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		public virtual void AppendSerialStatusCmdOrderBy(PXSelectBase<INLotSerialStatus> cmd, TLSMaster Row, INLotSerClass lotSerClass)
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

		public virtual void AppendSerialStatusCmdOrderByProject(PXSelectBase<PMLotSerialStatus> cmd, TLSMaster Row, INLotSerClass lotSerClass)
		{
			switch (lotSerClass.LotSerIssueMethod)
			{
				case INLotSerIssueMethod.FIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<PMLotSerialStatus.receiptDate, Asc<PMLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.LIFO:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Desc<PMLotSerialStatus.receiptDate, Asc<PMLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Expiration:
					cmd.OrderByNew<OrderBy<Asc<PMLotSerialStatus.expireDate, Asc<INLocation.pickPriority, Asc<PMLotSerialStatus.lotSerialNbr>>>>>();
					break;
				case INLotSerIssueMethod.Sequential:
				case INLotSerIssueMethod.UserEnterable:
					cmd.OrderByNew<OrderBy<Asc<INLocation.pickPriority, Asc<PMLotSerialStatus.lotSerialNbr>>>>();
					break;
				default:
					throw new PXException();
			}
		}

		// Please also rename GetSerialStatusAvailableQty2 to GetSerialStatusAvailableQty.
		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		protected virtual decimal? GetSerialStatusAvailableQty(INLotSerialStatus lsmaster, LotSerialStatus accumavail, PXResult data)
		{
			return lsmaster.QtyAvail + accumavail.QtyAvail;
		}

		protected virtual decimal? GetSerialStatusAvailableQty2(IStatus lsmaster, IStatus accumavail, PXResult data)
		{
			return lsmaster.QtyAvail + accumavail.QtyAvail;
		}

		// Please also rename GetSerialStatusQtyOnHand2 to GetSerialStatusQtyOnHand.
		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		protected virtual decimal? GetSerialStatusQtyOnHand(INLotSerialStatus lsmaster, PXResult data)
		{
			return lsmaster.QtyOnHand;
		}

		protected virtual decimal? GetSerialStatusQtyOnHand2(IStatus lsmaster, PXResult data)
		{
			return lsmaster.QtyOnHand;
		}

		public virtual void IssueNumbers(PXCache sender, TLSMaster Row, decimal BaseQty)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);

			PXDBOperation prevOperation = _Operation;
			if (_Operation == PXDBOperation.Update && ((INLotSerClass)item).LotSerTrack == "S" && SelectDetail(DetailCache, Row).Count() == 0)
			{
				_Operation = PXDBOperation.Normal;
			}

			try
			{
				if (UseProjectAvailability(sender, Row))
				{
					IssueNumbersInt<PMLotSerialStatus, PMLotSerialStatusAccum>(sender, Row, item, BaseQty);
				}
				else
				{
					IssueNumbersInt<INLotSerialStatus, LotSerialStatus>(sender, Row, item, BaseQty);
				}
			}
			finally
			{
				_Operation = prevOperation;
			}
		}

		private bool UseProjectAvailability(PXCache sender, ILSMaster Row)
		{
			return PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
		}

		// Please also rename SelectSerialStatus2 too SelectSerialStatus
		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		protected List<ILotSerial> SelectSerialStatus(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (UseProjectAvailability(sender, Row))
			{
				PXSelectBase<PMLotSerialStatus> cmd = GetSerialStatusCmdProject(sender, Row, item);
				PMLotSerialStatus pars = PMLotSerialStatus(Row);
				if (IsLinkedProject(Row.ProjectID))
				{
					pars.ProjectID = PM.ProjectDefaultAttribute.NonProject();
					pars.TaskID = 0;
				}

				List<PMLotSerialStatus> list = cmd.View.SelectMultiBound(new object[] { pars }).RowCast<PMLotSerialStatus>().ToList();
				return new List<ILotSerial>(list);
			}
			else
			{
				PXSelectBase<INLotSerialStatus> cmd = GetSerialStatusCmd(sender, Row, item);
				INLotSerialStatus pars = INLotSerialStatus(Row);
				List<INLotSerialStatus> list = cmd.View.SelectMultiBound(new object[] { pars }).RowCast<INLotSerialStatus>().ToList();
				return new List<ILotSerial>(list);
			}
		}

		protected List<object> SelectSerialStatus2(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (UseProjectAvailability(sender, Row))
			{
				PXSelectBase<PMLotSerialStatus> cmd = GetSerialStatusCmdProject(sender, Row, item);
				PMLotSerialStatus pars = PMLotSerialStatus(Row);
				if (IsLinkedProject(Row.ProjectID))
				{
					pars.ProjectID = PM.ProjectDefaultAttribute.NonProject();
					pars.TaskID = 0;
				}

				return cmd.View.SelectMultiBound(new object[] { pars });
			}
			else
			{
				PXSelectBase<INLotSerialStatus> cmd = GetSerialStatusCmd(sender, Row, item);
				INLotSerialStatus pars = INLotSerialStatus(Row);
				return cmd.View.SelectMultiBound(new object[] { pars });
			}
		}

		protected virtual bool IsLinkedProject(int? projectID)
		{
			if (projectID == null)
				return false;
			if (projectID == ProjectDefaultAttribute.NonProject())
				return false;

			PMProject project = PMProject.PK.Find(_Graph, projectID);
			if (project != null)
			{
				return project.AccountingMode == ProjectAccountingModes.Linked;
			}

			return false;

		}

		protected void IssueNumbersInt<T, A>(PXCache sender, TLSMaster Row, PXResult<InventoryItem, INLotSerClass> item, decimal BaseQty)
			where T : class, ILotSerial, IStatus, IBqlTable, new()
			where A : class, ILotSerial, IStatus, IBqlTable, new()
		{
			TLSDetail split = Convert(Row);

			PXCache lscache = sender.Graph.Caches[typeof(T)];

			if (Row != null)
				DetailCounters.Remove(Row);
			var lotSerClass = (INLotSerClass)item;
			if ((GetTranTrackMode(Row, lotSerClass) & INLotSerTrack.Mode.Issue) > 0)
			{
				List<object> lotSerialStatuses = SelectSerialStatus2(sender, Row, item);

				MoveItemToTopOfList<T>(lotSerialStatuses, Row.LotSerialNbr);

				foreach (PXResult res in lotSerialStatuses)
				{
					ILotSerial lsmaster = (ILotSerial)res[typeof(T)];

					split.SplitLineNbr = null;
					split.SubItemID = lsmaster.SubItemID;
					split.LocationID = lsmaster.LocationID;
					split.LotSerialNbr = lsmaster.LotSerialNbr;
					split.ExpireDate = lsmaster.ExpireDate;
					split.UOM = ((InventoryItem)item).BaseUnit;

					decimal SignQtyAvail;
					decimal SignQtyHardAvail;
					INItemPlanIDAttribute.GetInclQtyAvail<SiteLotSerial>(_Graph.Caches[typeof(TLSDetail)], split, out SignQtyAvail, out SignQtyHardAvail);

					if (SignQtyAvail < 0m)
					{
						A accumavail = new A();
						lscache.RestoreCopy(accumavail, lsmaster);

						accumavail = (A)_Graph.Caches[typeof(A)].Insert(accumavail);

						decimal? AvailableQty = GetSerialStatusAvailableQty2((IStatus)lsmaster, accumavail, res);

						if (AvailableQty <= 0m)
						{
							continue;
						}

						if (AvailableQty <= BaseQty)
						{
							split.BaseQty = AvailableQty;
							BaseQty -= (decimal)AvailableQty;
						}
						else
						{
							if (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
							{
								split.BaseQty = 1m;
								BaseQty -= 1m;
							}
							else
							{
								split.BaseQty = BaseQty;
								BaseQty = 0m;
							}
						}
					}
					else
					{
						if (lscache.GetStatus(lsmaster) == PXEntryStatus.Notchanged)
						{
							Summarize(lscache, Row, lsmaster);
						}

						decimal? qtyOnHand = GetSerialStatusQtyOnHand2((IStatus)lsmaster, res);

						if (qtyOnHand <= 0m)
						{
							continue;
						}

						if (qtyOnHand <= BaseQty)
						{
							split.BaseQty = qtyOnHand;
							BaseQty -= (decimal)qtyOnHand;
						}
						else
						{
							if (lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
							{
								split.BaseQty = 1m;
								BaseQty -= 1m;
							}
							else
							{
								split.BaseQty = BaseQty;
								BaseQty = 0m;
							}
						}

						lsmaster.QtyOnHand -= split.BaseQty;
						sender.Graph.Caches[typeof(T)].SetStatus(lsmaster, PXEntryStatus.Held);
					}

					SetDetailQtyWithMaster(sender, split, Row);
					DetailCache.Insert(PXCache<TLSDetail>.CreateCopy(split));

					if (BaseQty <= 0m)
					{
						break;
					}
				}
			}

			if (BaseQty > 0m && Row.InventoryID != null && Row.SubItemID != null && Row.SiteID != null && Row.LocationID != null && !string.IsNullOrEmpty(Row.LotSerialNbr))
			{
				if (lotSerClass.IsManualAssignRequired == true || lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
				{

				}
				else
				{
					split.SplitLineNbr = null;
					split.BaseQty = BaseQty;

					SetDetailQtyWithMaster(sender, split, Row);
					split.ExpireDate = ExpireDateByLot(sender, split, null);

					try
					{
						DetailCache.Insert(PXCache<TLSDetail>.CreateCopy(split));
					}
					catch
					{
						Row.UnassignedQty += BaseQty;
						sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
					}
					finally
					{
						BaseQty = 0m;
					}
				}
			}

			if (BaseQty != 0)
			{
				var haveRemainder = lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered && decimal.Remainder(BaseQty, 1m) != 0m;
				if (haveRemainder || BaseQty < 0)
				{
					TLSMaster oldrow = PXCache<TLSMaster>.CreateCopy(Row);

					Row.BaseQty -= BaseQty;
					SetMasterQtyFromBase(sender, Row);

					sender.RaiseFieldUpdated(_MasterQtyField, Row, oldrow.Qty);
					sender.RaiseRowUpdated(Row, oldrow);

					if (haveRemainder)
					{
						sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(Messages.SerialItem_LineQtyUpdated, PXErrorLevel.Warning));
					}
					else
					{
						sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(Messages.InsuffQty_LineQtyUpdated, PXErrorLevel.Warning));
					}
				}
				else
				{
					Row.UnassignedQty += BaseQty;
					sender.RaiseExceptionHandling(_MasterQtyField, Row, null, new PXSetPropertyException(Messages.BinLotSerialNotAssigned, PXErrorLevel.Warning));
				}
			}
		}

		private void MoveItemToTopOfList<T>(List<object> lotSerialStatuses, string lotSerialNbr)
			where T : class, ILotSerial, IStatus, IBqlTable, new()
		{
			if (!string.IsNullOrEmpty(lotSerialNbr))
			{
				int lotSerialIndex = lotSerialStatuses.FindIndex(
					x =>
					{
						ILotSerial s = (ILotSerial)((PXResult)x)[typeof(T)];
						return string.Equals(
							s.LotSerialNbr?.Trim(),
							lotSerialNbr.Trim(),
							StringComparison.InvariantCultureIgnoreCase);
					});
				if (lotSerialIndex > 0)
				{
					object lotSerialStatus = lotSerialStatuses[lotSerialIndex];
					lotSerialStatuses.RemoveAt(lotSerialIndex);
					lotSerialStatuses.Insert(0, lotSerialStatus);
				}
			}
		}

		public virtual void IssueNumbers(PXCache sender, TLSMaster Row)
		{
			IssueNumbers(sender, Row, (decimal)Row.BaseQty);
		}

		protected virtual void UpdateCounters(PXCache sender, Counters counters, TLSDetail detail)
		{
			counters.RecordCount += 1;
			detail.BaseQty = INUnitAttribute.ConvertToBase(sender, detail.InventoryID, detail.UOM, (decimal)detail.Qty, detail.BaseQty, INPrecision.QUANTITY);
			counters.BaseQty += (decimal)detail.BaseQty;
			if (detail.ExpireDate == null)
			{
				counters.ExpireDatesNull += 1;
			}
			else
			{
				if (counters.ExpireDates.ContainsKey(detail.ExpireDate))
				{
					counters.ExpireDates[detail.ExpireDate] += 1;
				}
				else
				{
					counters.ExpireDates[detail.ExpireDate] = 1;
				}
				counters.ExpireDate = detail.ExpireDate;
			}
			if (detail.SubItemID == null)
			{
				counters.SubItemsNull += 1;
			}
			else
			{
				if (counters.SubItems.ContainsKey(detail.SubItemID))
				{
					counters.SubItems[detail.SubItemID] += 1;
				}
				else
				{
					counters.SubItems[detail.SubItemID] = 1;
				}
				counters.SubItem = detail.SubItemID;
			}
			if (detail.LocationID == null)
			{
				counters.LocationsNull += 1;
			}
			else
			{
				if (counters.Locations.ContainsKey(detail.LocationID))
				{
					counters.Locations[detail.LocationID] += 1;
				}
				else
				{
					counters.Locations[detail.LocationID] = 1;
				}
				counters.Location = detail.LocationID;
			}
			if (detail.TaskID == null)
			{
				counters.ProjectTasksNull += 1;
			}
			else
			{
				var kv = new KeyValuePair<int?, int?>(detail.ProjectID, detail.TaskID);
				if (counters.ProjectTasks.ContainsKey(kv))
				{
					counters.ProjectTasks[kv] += 1;
				}
				else
				{
					counters.ProjectTasks[kv] = 1;
				}
				counters.ProjectID = detail.ProjectID;
				counters.TaskID = detail.TaskID;
			}
			if (detail.LotSerialNbr == null)
			{
				counters.LotSerNumbersNull += 1;
			}
			else
			{
				if (string.IsNullOrEmpty(detail.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(detail.AssignedNbr, detail.LotSerialNbr))
				{
					counters.UnassignedNumber++;
				}

				if (counters.LotSerNumbers.ContainsKey(detail.LotSerialNbr))
				{
					counters.LotSerNumbers[detail.LotSerialNbr] += 1;
				}
				else
				{
					counters.LotSerNumbers[detail.LotSerialNbr] = 1;
				}
				counters.LotSerNumber = detail.LotSerialNbr;
			}

		}

		protected Counters counters;

		public virtual void UpdateParent(PXCache sender, TLSMaster Row, TLSDetail Det, TLSDetail OldDet, out decimal BaseQty)
		{
			counters = null;
			if (!DetailCounters.TryGetValue(Row, out counters))
			{
				DetailCounters[Row] = counters = new Counters();
				foreach (TLSDetail detail in SelectDetail(sender.Graph.Caches[typeof(TLSDetail)], Row))
				{
					UpdateCounters(sender, counters, detail);
				}
			}
			else
			{
				if (Det != null)
				{
					UpdateCounters(sender, counters, Det);
				}
				if (OldDet != null)
				{
					TLSDetail detail = OldDet;
					counters.RecordCount -= 1;
					detail.BaseQty = INUnitAttribute.ConvertToBase(sender, detail.InventoryID, detail.UOM, (decimal)detail.Qty, detail.BaseQty, INPrecision.QUANTITY);
					counters.BaseQty -= (decimal)detail.BaseQty;
					if (detail.ExpireDate == null)
					{
						counters.ExpireDatesNull -= 1;
					}
					else if (counters.ExpireDates.ContainsKey(detail.ExpireDate))
					{
						if ((counters.ExpireDates[detail.ExpireDate] -= 1) == 0)
						{
							counters.ExpireDates.Remove(detail.ExpireDate);
						}
					}
					if (detail.SubItemID == null)
					{
						counters.SubItemsNull -= 1;
					}
					else if (counters.SubItems.ContainsKey(detail.SubItemID))
					{
						if ((counters.SubItems[detail.SubItemID] -= 1) == 0)
						{
							counters.SubItems.Remove(detail.SubItemID);
						}
					}
					if (detail.LocationID == null)
					{
						counters.LocationsNull -= 1;
					}
					else if (counters.Locations.ContainsKey(detail.LocationID))
					{
						if ((counters.Locations[detail.LocationID] -= 1) == 0)
						{
							counters.Locations.Remove(detail.LocationID);
						}
					}
					if (detail.TaskID == null)
					{
						counters.ProjectTasksNull -= 1;
					}
					else
					{
						var kv = new KeyValuePair<int?, int?>(detail.ProjectID, detail.TaskID);
						if (counters.ProjectTasks.ContainsKey(kv))
						{
							if ((counters.ProjectTasks[kv] -= 1) == 0)
							{
								counters.ProjectTasks.Remove(kv);
							}
						}
					}
					if (detail.LotSerialNbr == null)
					{
						counters.LotSerNumbersNull -= 1;
					}
					else if (counters.LotSerNumbers.ContainsKey(detail.LotSerialNbr))
					{
						if (string.IsNullOrEmpty(detail.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(detail.AssignedNbr, detail.LotSerialNbr))
						{
							counters.UnassignedNumber--;
						}
						if ((counters.LotSerNumbers[detail.LotSerialNbr] -= 1) == 0)
						{
							counters.LotSerNumbers.Remove(detail.LotSerialNbr);
						}
					}
				}
				if (Det == null && OldDet != null)
				{
					if (counters.ExpireDates.Count == 1 && counters.ExpireDatesNull == 0)
					{
						foreach (DateTime? key in counters.ExpireDates.Keys)
						{
							counters.ExpireDate = key;
						}
					}
					if (counters.SubItems.Count == 1 && counters.SubItemsNull == 0)
					{
						foreach (int? key in counters.SubItems.Keys)
						{
							counters.SubItem = key;
						}
					}
					if (counters.Locations.Count == 1 && counters.LocationsNull == 0)
					{
						foreach (int? key in counters.Locations.Keys)
						{
							counters.Location = key;
						}
					}
					if (counters.ProjectTasks.Count == 1 && counters.ProjectTasksNull == 0)
					{
						foreach (KeyValuePair<int?, int?> key in counters.ProjectTasks.Keys)
						{
							counters.ProjectID = key.Key;
							counters.TaskID = key.Value;
						}
					}
					if (counters.LotSerNumbers.Count == 1 && counters.LotSerNumbersNull == 0)
					{
						foreach (string key in counters.LotSerNumbers.Keys)
						{
							counters.LotSerNumber = key;
						}
					}
				}
			}

			BaseQty = counters.BaseQty;

			switch (counters.RecordCount)
			{
				case 0:
					Row.LotSerialNbr = string.Empty;
					Row.HasMixedProjectTasks = false;
					break;
				case 1:
					Row.ExpireDate = counters.ExpireDate;
					Row.SubItemID = counters.SubItem;
					Row.LocationID = counters.Location;
					Row.LotSerialNbr = counters.LotSerNumber;
					Row.HasMixedProjectTasks = false;
					if (counters.ProjectTasks.Count > 0 && Det != null && counters.ProjectID != null)
					{
						Row.ProjectID = counters.ProjectID;
						Row.TaskID = counters.TaskID;
					}
					break;
				default:
					Row.ExpireDate = counters.ExpireDates.Count == 1 && counters.ExpireDatesNull == 0 ? counters.ExpireDate : null;
					Row.SubItemID = counters.SubItems.Count == 1 && counters.SubItemsNull == 0 ? counters.SubItem : null;
					Row.LocationID = counters.Locations.Count == 1 && counters.LocationsNull == 0 ? counters.Location : null;
					Row.HasMixedProjectTasks = counters.ProjectTasks.Count + (counters.ProjectTasks.Count > 0 ? counters.ProjectTasksNull : 0) > 1;
					if (Row.HasMixedProjectTasks != true && Det != null && counters.ProjectID != null)
					{
						Row.ProjectID = counters.ProjectID;
						Row.TaskID = counters.TaskID;
					}

					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
					INLotSerTrack.Mode mode = GetTranTrackMode(Row, item);
					if (mode == INLotSerTrack.Mode.None)
					{
						Row.LotSerialNbr = string.Empty;
					}
					else if ((mode & INLotSerTrack.Mode.Create) > 0 || (mode & INLotSerTrack.Mode.Issue) > 0)
					{
						//if more than 1 split exist at lotserial creation time ignore equilness and display <SPLIT>
						Row.LotSerialNbr = null;
					}
					else
					{
						Row.LotSerialNbr = counters.LotSerNumbers.Count == 1 && counters.LotSerNumbersNull == 0 ? counters.LotSerNumber : null;
					}
					break;
			}
		}

		public virtual void UpdateParent(PXCache sender, TLSMaster Row)
		{
			decimal BaseQty;
			UpdateParent(sender, Row, null, null, out BaseQty);
			Row.UnassignedQty = PXDBQuantityAttribute.Round((decimal)(Row.BaseQty - BaseQty));
		}

		public virtual void UpdateParent(PXCache sender, TLSDetail Row, TLSDetail OldRow)
		{
			TLSMaster parent = (TLSMaster)PXParentAttribute.SelectParent(sender, Row ?? OldRow, typeof(TLSMaster));

			if (parent != null && (Row ?? OldRow) != null && SameInventoryItem((ILSMaster)(Row ?? OldRow), (ILSMaster)parent))
			{
				TLSMaster oldrow = PXCache<TLSMaster>.CreateCopy(parent);
				decimal BaseQty;

				UpdateParent(sender, parent, Row, OldRow, out BaseQty);

				using (InvtMultScope<TLSMaster> ms = new InvtMultScope<TLSMaster>(parent))
				{
					if (BaseQty < parent.BaseQty)
					{
						parent.UnassignedQty = PXDBQuantityAttribute.Round((decimal)(parent.BaseQty - BaseQty));
					}
					else
					{
						parent.UnassignedQty = 0m;
						parent.BaseQty = BaseQty;
						SetMasterQtyFromBase(sender, parent);
					}
				}

				sender.Graph.Caches[typeof(TLSMaster)].MarkUpdated(parent);

				if (Math.Abs((Decimal)oldrow.Qty - (Decimal)parent.Qty) >= 0.0000005m)
				{
					sender.Graph.Caches[typeof(TLSMaster)].RaiseFieldUpdated(_MasterQtyField, parent, oldrow.Qty);
					sender.Graph.Caches[typeof(TLSMaster)].RaiseRowUpdated(parent, oldrow);
				}
			}
		}

		protected virtual void SetMasterQtyFromBase(PXCache sender, TLSMaster master)
		{
			master.Qty = INUnitAttribute.ConvertFromBase(sender, master.InventoryID, master.UOM, (decimal)master.BaseQty, INPrecision.QUANTITY);
		}

		protected virtual void SetDetailQtyWithMaster(PXCache sender, TLSDetail detail, TLSMaster master)
		{
			master = master ?? SelectMaster(sender, detail);
			if (detail.InventoryID == master?.InventoryID && detail.BaseQty == master?.BaseQty
				&& string.Equals(detail.UOM, master?.UOM, StringComparison.OrdinalIgnoreCase))
			{
				detail.Qty = master.Qty;
				return;
			}
			detail.Qty = INUnitAttribute.ConvertFromBase(sender, detail.InventoryID, detail.UOM, (decimal)detail.BaseQty, INPrecision.QUANTITY);
		}

		public virtual void DefaultLotSerialNbr(PXCache sender, TLSDetail row)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, row.InventoryID);

			if (item != null)
			{
				INLotSerTrack.Mode mode = GetTranTrackMode(row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (TLSDetail lssplit in INLotSerialNbrAttribute.CreateNumbers<TLSDetail>(sender, item, lotSerNum, mode, 1m))
					{
						if (string.IsNullOrEmpty(row.LotSerialNbr))
							row.LotSerialNbr = lssplit.LotSerialNbr;

						row.AssignedNbr = lssplit.AssignedNbr;
						row.LotSerClassID = lssplit.LotSerClassID;
					}
				}
			}
		}

		protected virtual bool Detail_ObjectsEqual(TLSDetail a, TLSDetail b)
		{
			if (a != null && b != null)
			{
				return (a.InventoryID == b.InventoryID && (a.IsStockItem != true ||
								a.SubItemID == b.SubItemID &&
								a.LocationID == b.LocationID &&
								(string.Equals(a.LotSerialNbr, b.LotSerialNbr) || string.IsNullOrEmpty(a.LotSerialNbr) && string.IsNullOrEmpty(b.LotSerialNbr)) &&
								(string.IsNullOrEmpty(a.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(a.AssignedNbr, a.LotSerialNbr) == false) &&
								(string.IsNullOrEmpty(b.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(b.AssignedNbr, b.LotSerialNbr) == false)));
			}
			else
			{
				return (a != null);
			}
		}

		protected virtual void Detail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			TLSDetail a = (TLSDetail)e.Row;

			if (!string.IsNullOrEmpty(a.AssignedNbr) && INLotSerialNbrAttribute.StringsEqual(a.AssignedNbr, a.LotSerialNbr))
			{
				return;
			}

			if (_InternallCall && _Operation == PXDBOperation.Insert)
			{
				Counters counters;
				if (!DetailCounters.TryGetValue((TLSMaster)MasterCache.Current, out counters))
				{
					DetailCounters[(TLSMaster)MasterCache.Current] = counters = new Counters();
				}
				UpdateCounters(MasterCache, counters, a);
			}

			if (_InternallCall && _Operation == PXDBOperation.Update)
			{
				foreach (object item in SelectDetail(sender, (TLSDetail)e.Row))
				{
					TLSDetail detailitem = (TLSDetail)item;

					if (Detail_ObjectsEqual((TLSDetail)e.Row, detailitem))
					{
						PXResult<InventoryItem, INLotSerClass> invtitem = ReadInventoryItem(sender, a.InventoryID);

						if (((INLotSerClass)invtitem).LotSerTrack != "S" || detailitem.BaseQty == 0m)
						{
							object oldDetailItem = PXCache<TLSDetail>.CreateCopy(detailitem);
							detailitem.BaseQty += ((TLSDetail)e.Row).BaseQty;
							SetDetailQtyWithMaster(sender, detailitem, null);

							sender.RaiseRowUpdated(detailitem, oldDetailItem);
							sender.MarkUpdated(detailitem);
							PXDBQuantityAttribute.VerifyForDecimal(sender, detailitem);
						}
						e.Cancel = true;
						break;
					}
				}
			}

			if (((TLSDetail)e.Row).InventoryID == null || string.IsNullOrEmpty(((TLSDetail)e.Row).UOM))
			{
				e.Cancel = true;
			}

			if (!e.Cancel)
			{
				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((TLSDetail)e.Row).InventoryID);
				INLotSerTrack.Mode mode = GetTranTrackMode((TLSDetail)e.Row, item);

				if (mode != INLotSerTrack.Mode.None && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered && ((TLSDetail)e.Row).Qty == 0 && MasterCurrent.UnassignedQty >= 1)
					((TLSDetail)e.Row).Qty = 1;

				if (((TLSDetail)e.Row).BaseQty == null || ((TLSDetail)e.Row).BaseQty == 0m || ((TLSDetail)e.Row).BaseQty != ((TLSDetail)e.Row).Qty || ((TLSDetail)e.Row).UOM != ((InventoryItem)item).BaseUnit)
				{
					((TLSDetail)e.Row).BaseQty = INUnitAttribute.ConvertToBase(sender, ((TLSDetail)e.Row).InventoryID, ((TLSDetail)e.Row).UOM, ((TLSDetail)e.Row).Qty ?? 0m, ((TLSDetail)e.Row).BaseQty, INPrecision.QUANTITY);
				}

				((TLSDetail)e.Row).UOM = ((InventoryItem)item).BaseUnit;
				((TLSDetail)e.Row).Qty = ((TLSDetail)e.Row).BaseQty;
			}
		}

		protected virtual void Detail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if (_InternallCall)
			{
				return;
			}
			((TLSDetail)e.Row).BaseQty = INUnitAttribute.ConvertToBase(sender, ((TLSDetail)e.Row).InventoryID, ((TLSDetail)e.Row).UOM, (decimal)((TLSDetail)e.Row).Qty, ((TLSDetail)e.Row).BaseQty, INPrecision.QUANTITY);

			DefaultLotSerialNbr(sender, (TLSDetail)e.Row);

			if (!UnattendedMode)
			{
				((TLSDetail)e.Row).ExpireDate = ExpireDateByLot(sender, ((TLSDetail)e.Row), null);
			}

			try
			{
				_InternallCall = true;
				UpdateParent(sender, (TLSDetail)e.Row, null);

				if (!UnattendedMode)
				{
					AvailabilityCheck(sender, (TLSDetail)e.Row);
				}
			}
			finally
			{
				_InternallCall = false;
			}
		}

		protected virtual void Detail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			ExpireCachedItems(sender, (ILSMaster)e.OldRow);

			if (_InternallCall)
			{
				return;
			}

			if (((TLSDetail)e.Row).LotSerialNbr != ((TLSDetail)e.OldRow).LotSerialNbr)
			{
				((TLSDetail)e.Row).ExpireDate = ExpireDateByLot(sender, ((TLSDetail)e.Row), null);
			}

			((TLSDetail)e.Row).BaseQty = INUnitAttribute.ConvertToBase(sender, ((TLSDetail)e.Row).InventoryID, ((TLSDetail)e.Row).UOM, (decimal)((TLSDetail)e.Row).Qty, ((TLSDetail)e.Row).BaseQty, INPrecision.QUANTITY);

			try
			{
				_InternallCall = true;
				UpdateParent(sender, (TLSDetail)e.Row, (TLSDetail)e.OldRow);

				if (!UnattendedMode)
				{
					AvailabilityCheck(sender, (TLSDetail)e.Row);
				}
			}
			finally
			{
				_InternallCall = false;
			}
		}

		protected virtual void Detail_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			ExpireCachedItems(sender, (ILSMaster)e.Row);

			if (_InternallCall)
			{
				return;
			}

			try
			{
				_InternallCall = true;
				UpdateParent(sender, null, (TLSDetail)e.Row);
			}
			finally
			{
				_InternallCall = false;
			}
		}

		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R2)]
		protected virtual void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXSetPropertyException e)
			=> RaiseQtyExceptionHandling(sender, row, newValue, new PXExceptionInfo(e.ErrorLevel, e.MessageNoPrefix));

		protected virtual void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei) { }

		public virtual void AvailabilityCheck(PXCache sender, ILSMaster Row)
		{
			if (Row != null && Row.InvtMult == (short)-1 && Row.BaseQty > 0m)
			{
				AvailabilityFetchMode fetchMode = AvailabilityFetchMode.ExcludeCurrent;
				if (UseProjectAvailability(sender, Row))
				{
					fetchMode = AvailabilityFetchMode.ExcludeCurrent | AvailabilityFetchMode.Project;
				}

				IStatus availability = AvailabilityFetch(sender, Row, fetchMode);



				AvailabilityCheck(sender, Row, availability);
			}
		}

		public virtual void AvailabilityCheck(PXCache sender, ILSMaster Row, IStatus availability)
		{
			foreach (var errorInfo in GetAvailabilityCheckErrors(sender, Row, availability))
				RaiseQtyExceptionHandling(sender, Row, Row.Qty, errorInfo);
		}

		public virtual IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster Row)
		{
			if (Row != null && Row.InvtMult == -1 && Row.BaseQty > 0m)
			{
				IStatus availability = AvailabilityFetch(sender, Row, AvailabilityFetchMode.ExcludeCurrent);

				return GetAvailabilityCheckErrors(sender, Row, availability);
			}
			return Array.Empty<PXExceptionInfo>();
		}

		protected virtual bool IsAvailableQty(PXCache sender, ILSMaster Row, IStatus availability)
		{
			if (Row.InvtMult == -1 && Row.BaseQty > 0m && availability != null)
			{
				if (availability.QtyNotAvail < 0m && (availability.QtyAvail + availability.QtyNotAvail) < 0m)
					return false;
			}
			return true;
		}

		protected virtual IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster Row, IStatus availability)
		{
			if (!IsAvailableQty(sender, Row, availability))
			{
				switch (GetWarningLevel(availability))
				{
					case AvailabilityWarningLevel.LotSerial:
						yield return new PXExceptionInfo(PXErrorLevel.Warning, Messages.StatusCheck_QtyLotSerialNegative);
						break;
					case AvailabilityWarningLevel.Location:
						yield return new PXExceptionInfo(PXErrorLevel.Warning, Messages.StatusCheck_QtyLocationNegative);
						break;
					case AvailabilityWarningLevel.Site:
						yield return new PXExceptionInfo(PXErrorLevel.Warning, Messages.StatusCheck_QtyNegative);
						break;
				}
			}
		}

		protected virtual AvailabilityWarningLevel GetWarningLevel(IStatus availability)
		{
			if (availability is LotSerialStatus || availability is PMLotSerialStatusAccum)
				return AvailabilityWarningLevel.LotSerial;
			else if (availability is LocationStatus || availability is PMLocationStatusAccum)
				return AvailabilityWarningLevel.Location;
			else if (availability is SiteStatus || availability is PMSiteStatusAccum || availability is PMSiteSummaryStatusAccum)
				return AvailabilityWarningLevel.Site;
			else

				return AvailabilityWarningLevel.None;
		}

		public virtual void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			e.ReturnState = PXStringState.CreateInstance(e.ReturnState, 255, null, _AvailField, false, null, null, null, null, null, null);
			((PXFieldState)e.ReturnState).Visible = false;
			((PXFieldState)e.ReturnState).Visibility = PXUIVisibility.Invisible;
			((PXFieldState)e.ReturnState).DisplayName = _AvailFieldDisplayName;
		}

		public virtual IStatus AvailabilityFetch(PXCache sender, ILSMaster Row, AvailabilityFetchMode fetchMode)
		{
			if (Row != null)
			{
				TLSDetail copy = Row as TLSDetail;
				if (copy == null)
				{
					copy = Convert(Row as TLSMaster);

					PXParentAttribute.SetParent(DetailCache, copy, typeof(TLSMaster), Row);

					if (string.IsNullOrEmpty(Row.LotSerialNbr) == false)
					{
						DefaultLotSerialNbr(sender.Graph.Caches[typeof(TLSDetail)], copy);
					}
				}
				return AvailabilityFetch(sender, copy, fetchMode);
			}
			return null;
		}

		protected T InsertWith<T>(PXGraph graph, T row, PXRowInserted handler)
			where T : class, IBqlTable, new()
		{
			graph.RowInserted.AddHandler<T>(handler);
			try
			{
				return PXCache<T>.Insert(graph, row);
			}
			finally
			{
				graph.RowInserted.RemoveHandler<T>(handler);
			}

		}

		public virtual IStatus AvailabilityFetch(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if (Row?.InventoryID == null) return null;
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
			if (item == null || (INLotSerClass)item == null || ((INLotSerClass)item).LotSerTrack == null) return null;

			if (Row.SubItemID != null
					&& Row.SiteID != null
					&& Row.LocationID != null
					&& string.IsNullOrEmpty(Row.LotSerialNbr) == false
					&& (string.IsNullOrEmpty(Row.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(Row.AssignedNbr, Row.LotSerialNbr) == false)
					&& ((INLotSerClass)item).LotSerAssign == INLotSerAssign.WhenReceived)
			{
				return AvailabilityFetchLotSerial(sender, Row, fetchMode);
			}
			else if (Row.SubItemID != null && Row.SiteID != null && Row.LocationID != null)
			{
				return AvailabilityFetchLocation(sender, Row, fetchMode);
			}
			else if (Row.SubItemID != null && Row.SiteID != null)
			{
				return AvailabilityFetchSite(sender, Row, fetchMode);
			}
			else return null;
		}

		private IStatus AvailabilityFetchLotSerial(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if ((fetchMode & AvailabilityFetchMode.Project) == AvailabilityFetchMode.Project)
			{
				return AvailabilityFetchLotSerialProject(sender, Row, fetchMode);
			}
			else
			{
				return AvailabilityFetchLotSerialBase(sender, Row, fetchMode);
			}
		}

		private IStatus AvailabilityFetchLotSerialBase(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			LotSerialStatus acc = new LotSerialStatus
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				LocationID = Row.LocationID,
				LotSerialNbr = Row.LotSerialNbr
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			INLotSerialStatus status = IN.INLotSerialStatus.PK.Find(sender.Graph, Row.InventoryID, Row.SubItemID, Row.SiteID, Row.LocationID, Row.LotSerialNbr);

			return AvailabilityFetch<LotSerialStatus>(Row as TLSDetail, PXCache<LotSerialStatus>.CreateCopy(acc), status, fetchMode);
		}

		private IStatus AvailabilityFetchLotSerialProject(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if (Row.ProjectID == null)
			{
				TLSMaster line = (TLSMaster)PXParentAttribute.SelectParent(sender, Row, typeof(TLSMaster));
				if (line != null)
				{
					Row.ProjectID = line.ProjectID;
					Row.TaskID = line.TaskID;
				}
			}

			PM.PMLotSerialStatusAccum acc = new PM.PMLotSerialStatusAccum
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				LocationID = Row.LocationID,
				LotSerialNbr = Row.LotSerialNbr,
				ProjectID = Row.ProjectID.GetValueOrDefault(PM.ProjectDefaultAttribute.NonProject().GetValueOrDefault()),
				TaskID = Row.TaskID.GetValueOrDefault(0)
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			PM.PMLotSerialStatus status = PM.PMLotSerialStatus.PK.Find(sender.Graph, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.LocationID, acc.LotSerialNbr, acc.ProjectID, acc.TaskID);

			return AvailabilityFetch<PM.PMLotSerialStatusAccum>(Row as TLSDetail, PXCache<PM.PMLotSerialStatusAccum>.CreateCopy(acc), status, fetchMode);
		}

		private IStatus AvailabilityFetchLocation(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if ((fetchMode & AvailabilityFetchMode.Project) == AvailabilityFetchMode.Project)
			{
				return AvailabilityFetchLocationProject(sender, Row, fetchMode);
			}
			else
			{
				return AvailabilityFetchLocationBase(sender, Row, fetchMode);
			}
		}

		private IStatus AvailabilityFetchLocationBase(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			LocationStatus acc = new LocationStatus
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				LocationID = Row.LocationID
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			INLocationStatus status = INLocationStatus.PK.Find(sender.Graph, Row.InventoryID, Row.SubItemID, Row.SiteID, Row.LocationID);

			return AvailabilityFetch<LocationStatus>(Row as TLSDetail, PXCache<LocationStatus>.CreateCopy(acc), status, fetchMode);
		}

		private IStatus AvailabilityFetchLocationProject(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if (Row.ProjectID == null)
			{
				TLSMaster line = (TLSMaster)PXParentAttribute.SelectParent(sender, Row, typeof(TLSMaster));
				if (line != null)
				{
					Row.ProjectID = line.ProjectID;
					Row.TaskID = line.TaskID;
				}
			}

			PM.PMLocationStatusAccum acc = new PM.PMLocationStatusAccum
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				LocationID = Row.LocationID,
				ProjectID = Row.ProjectID.GetValueOrDefault(PM.ProjectDefaultAttribute.NonProject().GetValueOrDefault()),
				TaskID = Row.TaskID.GetValueOrDefault(0)
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			PM.PMLocationStatus status = PM.PMLocationStatus.PK.Find(sender.Graph, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.LocationID, acc.ProjectID, acc.TaskID);

			return AvailabilityFetch<PM.PMLocationStatusAccum>(Row as TLSDetail, PXCache<PM.PMLocationStatusAccum>.CreateCopy(acc), status, fetchMode);
		}

		protected IStatus AvailabilityFetchSite(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if ((fetchMode & AvailabilityFetchMode.Project) == AvailabilityFetchMode.Project)
			{
				return AvailabilityFetchSiteProject(sender, Row, fetchMode);
			}
			else
			{
				return AvailabilityFetchSiteBase(sender, Row, fetchMode);
			}
		}

		private IStatus AvailabilityFetchSiteBase(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			SiteStatus acc = new SiteStatus
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			INSiteStatus status = INSiteStatus.PK.Find(sender.Graph, Row.InventoryID, Row.SubItemID, Row.SiteID);

			return AvailabilityFetch<SiteStatus>(Row as TLSDetail, PXCache<SiteStatus>.CreateCopy(acc), status, fetchMode);
		}

		private IStatus AvailabilityFetchSiteProject(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			if (Row.ProjectID == null)
			{
				TLSMaster line = (TLSMaster)PXParentAttribute.SelectParent(sender, Row, typeof(TLSMaster));
				if (line != null)
				{
					Row.ProjectID = line.ProjectID;
					Row.TaskID = line.TaskID;
				}
			}

			if (Row.TaskID == null)
			{
				return AvailabilityFetchSiteByProject(sender, Row, fetchMode);

			}
			else
			{
				return AvailabilityFetchSiteByTask(sender, Row, fetchMode);
			}
		}

		private IStatus AvailabilityFetchSiteByProject(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			int? projectID = Row.ProjectID;
			int? taskID = Row.TaskID;

			if (IsLinkedProject(projectID))
			{
				projectID = PM.ProjectDefaultAttribute.NonProject();
				taskID = 0;
			}

			PM.PMSiteSummaryStatusAccum acc = new PM.PMSiteSummaryStatusAccum
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				ProjectID = projectID
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			PM.PMSiteSummaryStatus status = PM.PMSiteSummaryStatus.PK.Find(sender.Graph, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.ProjectID);


			return AvailabilityFetch<PM.PMSiteSummaryStatusAccum>(Row as TLSDetail, PXCache<PM.PMSiteSummaryStatusAccum>.CreateCopy(acc), status, fetchMode);
		}

		private IStatus AvailabilityFetchSiteByTask(PXCache sender, ILSDetail Row, AvailabilityFetchMode fetchMode)
		{
			int? projectID = Row.ProjectID;
			int? taskID = Row.TaskID;

			if (projectID == null)
			{
				TLSMaster line = (TLSMaster)PXParentAttribute.SelectParent(sender, Row, typeof(TLSMaster));
				if (line != null)
				{
					projectID = line.ProjectID;
					taskID = line.TaskID;
				}
			}

			if (IsLinkedProject(projectID))
			{
				projectID = PM.ProjectDefaultAttribute.NonProject();
				taskID = 0;
			}

			PM.PMSiteStatusAccum acc = new PM.PMSiteStatusAccum
			{
				InventoryID = Row.InventoryID,
				SubItemID = Row.SubItemID,
				SiteID = Row.SiteID,
				ProjectID = projectID,
				TaskID = taskID
			};

			acc = InsertWith(sender.Graph, acc,
				(cache, e) =>
				{
					cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
					cache.IsDirty = false;
				});

			PM.PMSiteStatus status = PM.PMSiteStatus.PK.Find(sender.Graph, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.ProjectID, acc.TaskID);


			return AvailabilityFetch<PM.PMSiteStatusAccum>(Row as TLSDetail, PXCache<PM.PMSiteStatusAccum>.CreateCopy(acc), status, fetchMode);
		}

		protected virtual IStatus AvailabilityFetch<TNode>(ILSDetail Row, IStatus allocated, IStatus status, AvailabilityFetchMode fetchMode)
			where TNode : class, IQtyAllocated, IBqlTable, new()
		{
			if (status != null)
			{
				allocated.QtyOnHand += status.QtyOnHand;
				allocated.QtyAvail += status.QtyAvail;
				allocated.QtyHardAvail += status.QtyHardAvail;
				allocated.QtyActual += status.QtyActual;
			}

			if (fetchMode.HasFlag(AvailabilityFetchMode.ExcludeCurrent))
			{
				decimal SignQtyAvail;
				decimal SignQtyHardAvail;
				decimal SignQtyActual;
				INItemPlanIDAttribute.GetInclQtyAvail<TNode>(DetailCache, Row, out SignQtyAvail, out SignQtyHardAvail, out SignQtyActual);

				if (SignQtyAvail != 0)
				{
					allocated.QtyAvail -= SignQtyAvail * (Row.BaseQty ?? 0m);
					allocated.QtyNotAvail += SignQtyAvail * (Row.BaseQty ?? 0m);
				}

				if (SignQtyHardAvail != 0)
				{
					allocated.QtyHardAvail -= SignQtyHardAvail * (Row.BaseQty ?? 0m);
				}

				if (SignQtyActual != 0)
				{
					allocated.QtyActual -= SignQtyActual * (Row.BaseQty ?? 0m);
				}
			}
			return allocated;
		}


		public virtual TLSMaster CloneMaster(TLSMaster item)
		{
			return PXCache<TLSMaster>.CreateCopy(item);
		}

		protected virtual void Master_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null) return;

			//Logic located in child entities
		}

		protected virtual void _Master_RowInserted(PXCache sender, PXRowInsertedEventArgs<TLSMaster> e)
		{
			e.Row.BaseQty = INUnitAttribute.ConvertToBase(sender, e.Row.InventoryID, e.Row.UOM, (decimal)e.Row.Qty, e.Row.BaseQty, INPrecision.QUANTITY);

			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, e.Row.InventoryID);

			if (item != null && (((InventoryItem)item).StkItem == true || ((InventoryItem)item).KitItem != true))
			{
				INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					//count for ZERO serial items only here
					if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false && (e.Row.BaseQty == 0m || e.Row.BaseQty == 1m || ((INLotSerClass)item).LotSerTrack != INLotSerTrack.SerialNumbered))
					{
						UpdateNumbers(sender, e.Row, (decimal)e.Row.BaseQty);
					}
					else
					{
						CreateNumbers(sender, e.Row);
					}
					UpdateParent(sender, e.Row);
				}
				else if ((mode & INLotSerTrack.Mode.Issue) > 0 && e.Row.BaseQty > 0m)
				{
					IssueNumbers(sender, e.Row);

					//do not set Zero LotSerial which will prevent IssueNumbers() on quantity update
					if (e.Row.BaseQty > 0)
					{
						UpdateParent(sender, e.Row);
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
					foreach (PXResult<INKitSpecStkDet, InventoryItem> res in PXSelectJoin<INKitSpecStkDet,
						InnerJoin<InventoryItem, On<INKitSpecStkDet.FK.ComponentInventoryItem>>,
						Where<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>>>.Select(sender.Graph, e.Row.InventoryID))
					{
						INKitSpecStkDet kititem = (INKitSpecStkDet)res;
						InventoryItem item2 = (InventoryItem)res;

						if (item2.ItemStatus == INItemStatus.Inactive)
						{
							throw new PXException(SO.Messages.KitComponentIsInactive, item2.InventoryCD);
						}

						TLSMaster copy = CloneMaster(e.Row);

						copy.InventoryID = kititem.CompInventoryID;
						copy.SubItemID = kititem.CompSubItemID;
						copy.UOM = kititem.UOM;
						copy.Qty = kititem.DfltCompQty * copy.BaseQty;

						try
						{
							_Master_RowInserted(sender, new PXRowInsertedEventArgs<TLSMaster>(copy, e.ExternalCall));
						}
						catch (PXException ex)
						{
							throw new PXException(ex, Messages.FailedToProcessComponent, item2.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
						}
					}
				}
				finally
				{
					KitInProcessing = null;
				}

				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in PXSelectJoin<INKitSpecNonStkDet,
					InnerJoin<InventoryItem, On<INKitSpecNonStkDet.FK.ComponentInventoryItem>>,
					Where<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>,
						And<Where<InventoryItem.kitItem, Equal<True>, Or<InventoryItem.nonStockShip, Equal<True>>>>>>.Select(sender.Graph, e.Row.InventoryID))
				{
					INKitSpecNonStkDet kititem = res;
					InventoryItem item2 = res;

					TLSMaster copy = CloneMaster(e.Row);

					copy.InventoryID = kititem.CompInventoryID;
					copy.SubItemID = null;
					copy.UOM = kititem.UOM;
					copy.Qty = kititem.DfltCompQty * copy.BaseQty;

					try
					{
						_Master_RowInserted(sender, new PXRowInsertedEventArgs<TLSMaster>(copy, e.ExternalCall));
					}
					catch (PXException ex)
					{
						throw new PXException(ex, Messages.FailedToProcessComponent, item2.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
					}
				}
			}
		}

		protected virtual void _Master_RowDeleted(PXCache sender, PXRowDeletedEventArgs<TLSMaster> e)
		{
			PXCache cache = sender.Graph.Caches[typeof(TLSDetail)];
			if (e.Row != null)
				DetailCounters.Remove(e.Row);
			foreach (object detail in SelectDetail(cache, e.Row))
			{
				cache.Delete(detail);
			}
		}

		public virtual void RaiseRowInserted(PXCache sender, TLSMaster Row)
		{
			_Master_RowInserted(sender, new PXRowInsertedEventArgs<TLSMaster>(Row, false));
		}

		public virtual void RaiseRowDeleted(PXCache sender, TLSMaster Row)
		{
			_Master_RowDeleted(sender, new PXRowDeletedEventArgs<TLSMaster>(Row, false));
		}

		protected virtual void _Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs<TLSMaster> e)
		{
			//Debug.Print("_Master_RowUpdated");
			if (e.OldRow != null && (e.OldRow.InventoryID != e.Row.InventoryID || e.OldRow.InvtMult != e.Row.InvtMult || (e.OldRow.UOM != null && e.Row.UOM == null) || (e.OldRow.UOM == null && e.Row.UOM != null)))
			{
				if (!sender.Graph.IsContractBasedAPI)
				{
					if (e.OldRow.InventoryID != e.Row.InventoryID)
					{
						((TLSMaster)e.Row).LotSerialNbr = null;
						((TLSMaster)e.Row).ExpireDate = null;
					}
					else if (e.OldRow.InvtMult != e.Row.InvtMult)
					{
						if (((TLSMaster)e.Row).LotSerialNbr == ((TLSMaster)e.OldRow).LotSerialNbr)
						{
							((TLSMaster)e.Row).LotSerialNbr = null;
						}
						if (((TLSMaster)e.Row).ExpireDate == ((TLSMaster)e.OldRow).ExpireDate)
						{
							((TLSMaster)e.Row).ExpireDate = null;
						}
					}
				}

				RaiseRowDeleted(sender, e.OldRow);
				RaiseRowInserted(sender, e.Row);
			}
			else
			{
				e.Row.BaseQty = INUnitAttribute.ConvertToBase(sender, e.Row.InventoryID, e.Row.UOM, (decimal)e.Row.Qty, e.Row.BaseQty, INPrecision.QUANTITY);

				PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, e.Row.InventoryID);

				if (e.Row.ExpireDate == e.OldRow.ExpireDate && e.OldRow.LotSerialNbr != e.Row.LotSerialNbr)
				{
					((TLSMaster)e.Row).ExpireDate = null;
				}
				if (item != null && (((InventoryItem)item).StkItem == true || ((InventoryItem)item).KitItem != true))
				{
					string itemLotSerTrack = ((INLotSerClass)item).LotSerTrack;

					INLotSerTrack.Mode mode = GetTranTrackMode(e.Row, item);
					if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
					{
						if (IsMasterPrimaryFieldsUpdated(e.Row, e.OldRow) || e.Row.ExpireDate != e.OldRow.ExpireDate)
						{
							if (CorrectionMode == false && (itemLotSerTrack == INLotSerTrack.NotNumbered || itemLotSerTrack == null))
							{
								RaiseRowDeleted(sender, e.OldRow);
								RaiseRowInserted(sender, e.Row);
								return;
							}
							else
							{
								UpdateNumbers(sender, e.Row);
							}
						}
						//count for ZERO serial items only here
						if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false && (e.Row.BaseQty == 0m || e.Row.BaseQty == 1m || itemLotSerTrack != INLotSerTrack.SerialNumbered))
						{
							UpdateNumbers(sender, e.Row, (decimal)e.Row.BaseQty - (decimal)e.OldRow.BaseQty);
						}
						else if (e.Row.BaseQty > e.OldRow.BaseQty)
						{
							CreateNumbers(sender, e.Row, (decimal)e.Row.BaseQty - (decimal)e.OldRow.BaseQty);
						}
						//do not truncate ZERO quantity lotserials
						else if (e.Row.BaseQty < e.OldRow.BaseQty)
						{
							TruncateNumbers(sender, e.Row, (decimal)e.OldRow.BaseQty - (decimal)e.Row.BaseQty);
						}

						UpdateParent(sender, e.Row);
					}
					else if ((mode & INLotSerTrack.Mode.Issue) > 0)
					{
						if (IsMasterPrimaryFieldsUpdated(e.Row, e.OldRow) || string.Equals(e.Row.LotSerialNbr, e.OldRow.LotSerialNbr) == false)
						{
							RaiseRowDeleted(sender, e.OldRow);
							RaiseRowInserted(sender, e.Row);
						}
						else if (string.IsNullOrEmpty(e.Row.LotSerialNbr) == false &&
							(e.Row.BaseQty == 1m || (itemLotSerTrack != INLotSerTrack.SerialNumbered && e.OldRow.UnassignedQty == 0)))
						{
							UpdateNumbers(sender, e.Row, (decimal)e.Row.BaseQty - (decimal)e.OldRow.BaseQty);
						}
						else if (e.Row.BaseQty > e.OldRow.BaseQty)
						{
							IssueNumbers(sender, e.Row, (decimal)e.Row.BaseQty - (decimal)e.OldRow.BaseQty);
						}
						else if (e.Row.BaseQty < e.OldRow.BaseQty)
						{
							TruncateNumbers(sender, e.Row, (decimal)e.OldRow.BaseQty - (decimal)e.Row.BaseQty);
						}

						//do not set Zero LotSerial which will prevent IssueNumbers() on quantity update
						if (e.Row.BaseQty > 0)
						{
							UpdateParent(sender, e.Row);
						}
					}
					//PCB AvailabilityCheck(sender, e.Row);
				}
				else if (item != null)
				{
					KitInProcessing = item;
					try
					{
						foreach (PXResult<INKitSpecStkDet, InventoryItem> res in PXSelectJoin<INKitSpecStkDet, InnerJoin<InventoryItem, On<INKitSpecStkDet.FK.ComponentInventoryItem>>, Where<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>>>.Select(sender.Graph, e.Row.InventoryID))
						{
							INKitSpecStkDet kititem = (INKitSpecStkDet)res;
							InventoryItem item2 = (InventoryItem)res;

							if (item2.ItemStatus == INItemStatus.Inactive)
							{
								throw new PXException(SO.Messages.KitComponentIsInactive, item2.InventoryCD);
							}

							TLSMaster copy = CloneMaster(e.Row);

							copy.InventoryID = kititem.CompInventoryID;
							copy.SubItemID = kititem.CompSubItemID;
							copy.UOM = kititem.UOM;
							copy.Qty = kititem.DfltCompQty * copy.BaseQty;

							TLSMaster oldcopy = CloneMaster(e.OldRow);

							oldcopy.InventoryID = kititem.CompInventoryID;
							oldcopy.SubItemID = kititem.CompSubItemID;
							oldcopy.UOM = kititem.UOM;
							oldcopy.Qty = kititem.DfltCompQty * oldcopy.BaseQty;

							if (!DetailCounters.TryGetValue(copy, out counters))
							{
								DetailCounters[copy] = counters = new Counters();
								foreach (TLSDetail detail in SelectDetail(sender.Graph.Caches[typeof(TLSDetail)], copy))
								{
									UpdateCounters(sender, counters, detail);
								}
							}

							//oldcopy.BaseQty = INUnitAttribute.ConvertToBase(sender, oldcopy.InventoryID, oldcopy.UOM, (decimal)oldcopy.Qty, INPrecision.QUANTITY);
							oldcopy.BaseQty = counters.BaseQty;

							try
							{
								_Master_RowUpdated(sender, new PXRowUpdatedEventArgs<TLSMaster>(copy, oldcopy, e.ExternalCall));
							}
							catch (PXException ex)
							{
								throw new PXException(ex, Messages.FailedToProcessComponent, item2.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
							}

						}
					}
					finally
					{
						KitInProcessing = null;
					}


					foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in PXSelectJoin<INKitSpecNonStkDet,
						InnerJoin<InventoryItem, On<INKitSpecNonStkDet.FK.ComponentInventoryItem>>,
						Where<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>,
							And<Where<InventoryItem.kitItem, Equal<True>, Or<InventoryItem.nonStockShip, Equal<True>>>>>>.Select(sender.Graph, e.Row.InventoryID))
					{
						INKitSpecNonStkDet kititem = res;
						InventoryItem item2 = res;

						if (item2.ItemStatus == INItemStatus.Inactive)
						{
							throw new PXException(SO.Messages.KitComponentIsInactive, item2.InventoryCD);
						}

						TLSMaster copy = CloneMaster(e.Row);

						copy.InventoryID = kititem.CompInventoryID;
						copy.SubItemID = null;
						copy.UOM = kititem.UOM;
						copy.Qty = kititem.DfltCompQty * copy.BaseQty;

						TLSMaster oldcopy = CloneMaster(e.OldRow);

						oldcopy.InventoryID = kititem.CompInventoryID;
						oldcopy.SubItemID = null;
						oldcopy.UOM = kititem.UOM;
						oldcopy.Qty = kititem.DfltCompQty * oldcopy.BaseQty;
						oldcopy.BaseQty = INUnitAttribute.ConvertToBase(sender, oldcopy.InventoryID, oldcopy.UOM, (decimal)oldcopy.Qty, oldcopy.BaseQty, INPrecision.QUANTITY);

						try
						{
							_Master_RowUpdated(sender, new PXRowUpdatedEventArgs<TLSMaster>(copy, oldcopy, e.ExternalCall));
						}
						catch (PXException ex)
						{
							throw new PXException(ex, Messages.FailedToProcessComponent, item2.InventoryCD, ((InventoryItem)item).InventoryCD, ex.MessageNoPrefix);
						}
					}
				}
			}
		}

		protected virtual void Master_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if (_InternallCall)
			{
				return;
			}

			try
			{
				_InternallCall = true;
				_Operation = PXDBOperation.Insert;
				using (InvtMultScope<TLSMaster> ms = new InvtMultScope<TLSMaster>((TLSMaster)e.Row))
				{
					_Master_RowInserted(sender, new PXRowInsertedEventArgs<TLSMaster>(e));
				}
			}
			finally
			{
				_InternallCall = false;
				_Operation = PXDBOperation.Normal;
			}
		}

		protected virtual void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (_InternallCall)
			{
				return;
			}

			try
			{
				_InternallCall = true;
				_Operation = PXDBOperation.Update;
				using (InvtMultScope<TLSMaster> ms = new InvtMultScope<TLSMaster>((TLSMaster)e.Row, (TLSMaster)e.OldRow))
				{
					_Master_RowUpdated(sender, new PXRowUpdatedEventArgs<TLSMaster>(e));
				}
			}
			finally
			{
				_InternallCall = false;
				_Operation = PXDBOperation.Normal;
			}
		}

		protected virtual void Master_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			try
			{
				_InternallCall = true;
				_Operation = PXDBOperation.Delete;
				foreach (var split in PXParentAttribute.SelectChildren(DetailCache, e.Row, typeof(TLSMaster)))
				{
					DetailCache.Delete(split);
				}
			}
			finally
			{
				_InternallCall = false;
				_Operation = PXDBOperation.Normal;
			}
		}

		public override TLSMaster Insert(TLSMaster item)
		{
			try
			{
				_InternallCall = true;
				_Operation = PXDBOperation.Delete;
				return (TLSMaster)MasterCache.Insert(item);
			}
			finally
			{
				_InternallCall = false;
				_Operation = PXDBOperation.Normal;
			}
		}

		protected virtual void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				PXCache cache = sender.Graph.Caches[typeof(TLSDetail)];

				Counters counters;
				if (DetailCounters.TryGetValue((TLSMaster)e.Row, out counters) && counters.UnassignedNumber == 0)
				{
					return;
				}

				TLSMaster master = (TLSMaster)e.Row;
				object[] selected = SelectDetail(cache, master);
				if (master != null)
				{
					_selected[master] = selected;
				}
				foreach (object detail in selected)
				{
					try
					{
						_InternallCall = true;
						Detail_RowPersisting(cache, new PXRowPersistingEventArgs(e.Operation, detail));
					}
					finally
					{
						_InternallCall = false;
					}
					if (string.IsNullOrEmpty(((TLSMaster)e.Row).LotSerialNbr) == false)
					{
						((TLSMaster)e.Row).LotSerialNbr = ((TLSDetail)detail).LotSerialNbr;
						break;
					}

					//if (((TLSDetail)detail).ExpireDate == null)
					//{
					//    PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((TLSMaster)e.Row).InventoryID);
					//    if (item != null && ((INLotSerClass)item).LotSerTrackExpiration == true && 
					//        sender.RaiseExceptionHandling<INComponentTran.inventoryID>(e.Row, ((InventoryItem)item).InventoryCD, new PXSetPropertyException(Messages.OneOrMoreExpDateIsEmpty)))
					//    {
					//        throw new PXRowPersistingException(typeof(INComponentTran.inventoryID).Name, null, Messages.OneOrMoreExpDateIsEmpty);
					//    }
					//}
				}
			}
		}

		protected virtual void Master_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			TLSMaster master = (TLSMaster)e.Row;
			if (e.TranStatus == PXTranStatus.Aborted)
			{
				RestoreLotSerNumbers(sender.Graph);

				if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
				{
					PXCache cache = sender.Graph.Caches[typeof(TLSDetail)];

					object[] selected;
					if (master == null || !_selected.TryGetValue(master, out selected))
					{
						selected = SelectDetail(cache, master);
					}
					foreach (object detail in selected)
					{
						Detail_RowPersisted(cache, new PXRowPersistedEventArgs(detail, e.Operation, e.TranStatus, e.Exception));
						if (string.IsNullOrEmpty(((TLSMaster)e.Row).LotSerialNbr) == false)
						{
							((TLSMaster)e.Row).LotSerialNbr = ((TLSDetail)detail).LotSerialNbr;
							break;
						}
					}
				}
			}
			if (master != null && e.TranStatus != PXTranStatus.Open)
			{
				_selected.Remove(master);
			}
		}

		public virtual void Master_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			VerifySNQuantity(sender, e, (ILSMaster)e.Row, _MasterQtyField);
		}

		public virtual void Detail_UOM_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((ILSDetail)e.Row).InventoryID);

			if (item != null)
			{
				e.NewValue = ((InventoryItem)item).BaseUnit;
				e.Cancel = true;
				//otherwise default via attribute
			}
		}

		public virtual void Detail_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var detail = (ILSDetail)e.Row;
			if (IsTrackSerial(sender, detail)
				&& detail.LotSerialNbr != Messages.Unassigned)  // crutch for AC-97716
			{
				if (e.NewValue != null && e.NewValue is decimal && (decimal)e.NewValue != 0m && (decimal)e.NewValue != 1m)
				{
					e.NewValue = 1m;
				}
			}
		}

		public virtual void VerifySNQuantity(PXCache sender, PXFieldVerifyingEventArgs e, ILSMaster line, string qtyFieldName)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, line.InventoryID);

			if (item != null && ((INLotSerClass)item).LotSerTrack == INLotSerTrack.SerialNumbered)
			{
				if (e.NewValue != null)
				{
					try
					{
						decimal BaseQty = INUnitAttribute.ConvertToBase(sender, line.InventoryID, line.UOM, (decimal)e.NewValue, INPrecision.NOROUND);
						if (decimal.Remainder(BaseQty, 1m) > 0m)
						{
							decimal power = (decimal)Math.Pow(10, (double)CommonSetupDecPl.Qty);
							for (decimal i = Math.Floor(BaseQty); ; i++)
							{
								e.NewValue = INUnitAttribute.ConvertFromBase(sender, line.InventoryID, line.UOM, i, INPrecision.NOROUND);

								if (decimal.Remainder((decimal)e.NewValue * power, 1m) == 0m)
									break;
							}
							sender.RaiseExceptionHandling(qtyFieldName, e.Row, null, new PXSetPropertyException(IN.Messages.SerialItem_LineQtyUpdated, PXErrorLevel.Warning));
						}
					}
					catch (PXUnitConversionException) { }
				}
			}
		}

		public virtual bool IsTrackSerial(PXCache sender, ILSDetail row)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, row.InventoryID);

			if (item == null)
				return false;

			string tranType = row.TranType;
			var lotSerClass = (INLotSerClass)item;
			if (lotSerClass.LotSerAssign == INLotSerAssign.WhenUsed && row.InvtMult < 0 && row.IsIntercompany == true)
			{
				tranType = INTranType.Transfer;
			}

			return INLotSerialNbrAttribute.IsTrackSerial(item, tranType, row.InvtMult);
		}

		protected Dictionary<object, string> _persisted = new Dictionary<object, string>();
		protected Dictionary<object, object[]> _selected = new Dictionary<object, object[]>();

		protected virtual void ThrowEmptyLotSerNumVal(PXCache sender, object data)
		{
			string _ItemFieldName = null;
			foreach (PXEventSubscriberAttribute attr in sender.GetAttributesReadonly(null))
			{
				if (attr is InventoryAttribute)
				{
					_ItemFieldName = attr.FieldName;
					break;
				}
			}
			//the only reason can be overflow in serial numbering which will cause '0000' number to be treated like not-generated
			throw new PXException(Messages.LSCannotAutoNumberItem, sender.GetValueExt(data, _ItemFieldName));
		}

		public virtual void Detail_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				if (string.IsNullOrEmpty(((ILSDetail)e.Row).AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(((ILSDetail)e.Row).AssignedNbr, ((ILSDetail)e.Row).LotSerialNbr))
				{
					string numVal = string.Empty;
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((ILSDetail)e.Row).InventoryID);
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					try
					{
						numVal = AutoNumberAttribute.NextNumber(lotSerNum.LotSerNumVal);
					}
					catch (AutoNumberException)
					{
						ThrowEmptyLotSerNumVal(sender, e.Row);
					}

					string _KeyToAbort = INLotSerialNbrAttribute.UpdateNumber(
						((ILSDetail)e.Row).AssignedNbr,
						((ILSDetail)e.Row).LotSerialNbr,
						numVal);

					((ILSDetail)e.Row).LotSerialNbr = _KeyToAbort;

					try
					{
						_persisted.Add(e.Row, _KeyToAbort);
					}
					catch (ArgumentException)
					{
						//the only reason can be overflow in serial numbering which will cause '0000' number to be treated like not-generated
						ThrowEmptyLotSerNumVal(sender, e.Row);
					}
					UpdateLotSerNumVal(lotSerNum, numVal, item);
					sender.RaiseRowUpdated(e.Row, PXCache<TLSDetail>.CreateCopy((TLSDetail)e.Row));
				}
			}
		}

		public virtual void Detail_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			if (e.TranStatus == PXTranStatus.Aborted)
			{
				string _KeyToAbort = null;

				if (((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update) &&
					_persisted.TryGetValue(e.Row, out _KeyToAbort))
				{
					((ILSDetail)e.Row).LotSerialNbr = INLotSerialNbrAttribute.MakeNumber(((ILSDetail)e.Row).AssignedNbr, ((ILSDetail)e.Row).LotSerialNbr, sender.Graph.Accessinfo.BusinessDate.GetValueOrDefault());
					_persisted.Remove(e.Row);
				}
				PXOuterException exception = e.Exception as PXOuterException;
				if (exception != null && object.ReferenceEquals(e.Row, exception.Row) && !UnattendedMode)
				{
					TLSDetail row = (TLSDetail)e.Row;
					TLSMaster master = SelectMaster(sender, row);

					for (int i = 0; i < exception.InnerFields.Length; i++)
					{
						if (!MasterCache.RaiseExceptionHandling(exception.InnerFields[i], master, null, new PXSetPropertyException(exception.InnerMessages[i])))
						{
							exception.InnerRemove(exception.InnerFields[i]);
						}
					}
				}
			}
			else if (e.TranStatus == PXTranStatus.Completed)
			{
				_persisted.Remove(e.Row);
			}

		}

		protected virtual bool IsMasterPrimaryFieldsUpdated(TLSMaster row, TLSMaster oldRow)
		{
			return row.SubItemID != oldRow.SubItemID ||
				row.SiteID != oldRow.SiteID ||
				row.LocationID != oldRow.LocationID ||
				row.ProjectID != oldRow.ProjectID ||
				row.TaskID != oldRow.TaskID;
		}

		#endregion

		#region Store & Restore Lot/Serial numbers after aborted

		private Dictionary<ILotSerNumVal, ILotSerNumVal> _lotSerNumVals;

		private void StoreLotSerNumVals(PXCache cache, IEnumerable numbersCollection)
		{
			foreach (ILotSerNumVal lotSerNumVal in numbersCollection)
				_lotSerNumVals.Add(lotSerNumVal, (ILotSerNumVal)cache.CreateCopy(lotSerNumVal));
		}

		private void RestoreLotSerNumVals(PXCache cache, IEnumerable numbersCollection)
		{
			var numbers = numbersCollection.OfType<ILotSerNumVal>().ToList();
			foreach (var newNumber in numbers)
			{
				ILotSerNumVal oldNumber;
				if (_lotSerNumVals.TryGetValue(newNumber, out oldNumber))
				{
					cache.RestoreCopy(newNumber, oldNumber);
					_lotSerNumVals.Remove(newNumber);
				}
				else
					cache.Remove(newNumber);
			}
		}

		private bool RestoreLotSerNumbers(PXGraph graph)
		{
			if (_lotSerNumVals == null)
				return false;

			var cache = graph.Caches[typeof(INLotSerClassLotSerNumVal)];
			cache.Current = null;
			RestoreLotSerNumVals(cache, cache.Cached);
			cache.Normalize();
			cache.ClearQueryCache();

			cache = graph.Caches[typeof(InventoryItemLotSerNumVal)];
			cache.Current = null;
			RestoreLotSerNumVals(cache, cache.Cached);
			cache.Normalize();
			cache.ClearQueryCache();

			_lotSerNumVals = null;
			return true;
		}

		private void OnBeforePersist(PXGraph graph)
		{
			_lotSerNumVals = new Dictionary<ILotSerNumVal, ILotSerNumVal>();

			var cache = graph.Caches[typeof(INLotSerClassLotSerNumVal)];
			StoreLotSerNumVals(cache, cache.Inserted);
			StoreLotSerNumVals(cache, cache.Updated);

			cache = graph.Caches[typeof(InventoryItemLotSerNumVal)];
			StoreLotSerNumVals(cache, cache.Inserted);
			StoreLotSerNumVals(cache, cache.Updated);
		}

		#endregion

		#region Inner Types
		[Serializable]
		public partial class LotSerOptions : LSSelect.LotSerOptions
		{
		}

		public class Counters : LSSelect.Counters
		{
		}

		protected virtual NotDecimalUnitErrorRedirectorScope<TDetailQty> ResolveNotDecimalUnitErrorRedirectorScope<TDetailQty>(object row)
			where TDetailQty : IBqlField
		{
			if (MasterQtyField == null)
				throw new PXArgumentException(nameof(MasterQtyField));
			return new NotDecimalUnitErrorRedirectorScope<TDetailQty>(MasterCache, row, MasterQtyField);
		}

		public enum AvailabilityWarningLevel
		{
			None,
			Site,
			Location,
			LotSerial
		}
		#endregion

		public virtual InventoryItem KitInProcessing { get; set; }

		public virtual bool IsIndivisibleComponent(InventoryItem inventory) => KitInProcessing != null && inventory.DecimalBaseUnit != true;
	}

	public interface ILotSerNumVal
	{
		String LotSerNumVal
		{
			get;
			set;
		}
	}

	#region LSParentAttribute

	public class LSParentAttribute : PXParentAttribute
	{
		public LSParentAttribute(Type selectParent)
			: base(selectParent)
		{
		}

		public new static object SelectParent(PXCache cache, object row, Type ParentType)
		{
			List<PXEventSubscriberAttribute> parents = new List<PXEventSubscriberAttribute>();
			foreach (PXEventSubscriberAttribute attr in cache.GetAttributesReadonly(null))
			{
				if (attr is PXParentAttribute)
				{
					if (((PXParentAttribute)attr).ParentType == ParentType)
					{
						parents.Insert(0, attr);
					}
					else if (ParentType.IsSubclassOf(((PXParentAttribute)attr).ParentType))
					{
						parents.Add(attr);
					}
				}
			}

			if (parents.Count > 0)
			{
				PXParentAttribute attr = (PXParentAttribute)parents[0];
				PXView parentview = attr.GetParentSelect(cache);

				if (!(parentview.CacheGetItemType() == ParentType || ParentType.IsAssignableFrom(parentview.CacheGetItemType())))
				{
					return null;
				}

				//clear view cache
				parentview.Clear();
				return parentview.SelectSingleBound(new object[] { row });
			}
			return null;
		}
	}

	#endregion

	#region LSDynamicButton
	public class LSDynamicButton : PXDynamicButtonAttribute
	{
		public LSDynamicButton(string[] dynamicButtonNames, string[] dynamicButtonDisplayNames) :
			base(dynamicButtonNames, dynamicButtonDisplayNames)
		{ }

		public override List<PXActionInfo> GetDynamicActions(Type graphType, Type viewType)
		{
			List<PXActionInfo> actions = new List<PXActionInfo>();

			foreach (var action in base.GetDynamicActions(graphType, viewType))
				actions.Add(new PXActionInfo(string.Format("{0}_{1}", viewType.Name, action.Name),
											 action.DisplayName,
											 GraphHelper.GetPrimaryCache(graphType.FullName).CacheType));
			return actions;
		}
	}
	#endregion
}
