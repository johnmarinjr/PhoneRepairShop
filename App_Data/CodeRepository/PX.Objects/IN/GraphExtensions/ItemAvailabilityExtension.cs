using System;
using System.Collections.Generic;

using PX.Common;
using PX.Data;

using PX.Objects.Common;
using PX.Objects.Common.Exceptions;

using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;

namespace PX.Objects.IN.GraphExtensions
{
	public abstract class ItemAvailabilityExtension<TGraph, TLine, TSplit> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		#region Cache Helpers
		#region TLine
		private PXCache<TLine> _lineCache;
		public PXCache<TLine> LineCache => _lineCache ?? (_lineCache = Base.Caches<TLine>());
		#endregion
		#region TSplit
		private PXCache<TSplit> _splitCache;
		public PXCache<TSplit> SplitCache => _splitCache ?? (_splitCache = Base.Caches<TSplit>());
		#endregion
		#endregion

		#region Configuration
		protected abstract TSplit EnsureSplit(ILSMaster row);

		protected abstract string GetStatus(TLine line);

		protected abstract decimal GetUnitRate(TLine line);

		protected abstract void RaiseQtyExceptionHandling(TLine line, PXExceptionInfo ei, decimal? newValue);

		protected abstract void RaiseQtyExceptionHandling(TSplit split, PXExceptionInfo ei, decimal? newValue);
		#endregion

		#region Initialization
		public override void Initialize()
		{
			AddStatusField();
		}
		#endregion

		#region Status Field
		public (string Name, string DisplayName) StatusField { get; protected set; } = (Messages.Availability_Field, Messages.Availability_Field);

		protected virtual void AddStatusField()
		{
			StatusField = (Messages.Availability_Field, PXMessages.LocalizeNoPrefix(Messages.Availability_Field));
			LineCache.Fields.Add(StatusField.Name);
			ManualEvent.FieldOf<TLine>.Selecting.Subscribe(Base, StatusField.Name, EventHandlerStatusField);
		}

		protected virtual void EventHandlerStatusField(ManualEvent.FieldOf<TLine>.Selecting.Args e)
		{
			if (e.Row != null && e.Row.InventoryID != null && e.Row.SiteID != null && !PXLongOperation.Exists(Base))
				e.ReturnValue = GetStatus(e.Row);
			else
				e.ReturnValue = string.Empty;

			var returnState = PXStringState.CreateInstance(e.ReturnState, 255, null, StatusField.Name, false, null, null, null, null, null, null);
			returnState.Visible = false;
			returnState.Visibility = PXUIVisibility.Invisible;
			returnState.DisplayName = StatusField.DisplayName;
			e.ReturnState = returnState;
		}
		#endregion

		#region Check
		public virtual void Check(ILSMaster row)
		{
			if (row != null && row.InvtMult == -1 && row.BaseQty > 0m)
			{
				IStatus availability = FetchWithBaseUOM(row, excludeCurrent: true);
				Check(row, availability);
			}
		}

		protected virtual void Check(ILSMaster row, IStatus availability)
		{
			foreach (var errorInfo in GetCheckErrors(row, availability))
				RaiseQtyExceptionHandling(row, errorInfo, row.Qty);
		}

		protected virtual void RaiseQtyExceptionHandling(ILSMaster row, PXExceptionInfo ei, decimal? newValue)
		{
			if (row is TLine line)
				RaiseQtyExceptionHandling(line, ei, newValue);
			else if (row is TSplit split)
				RaiseQtyExceptionHandling(split, ei, newValue);
		}

		public virtual IEnumerable<PXExceptionInfo> GetCheckErrors(ILSMaster row)
		{
			if (row != null && row.InvtMult == -1 && row.BaseQty > 0m)
			{
				IStatus availability = FetchWithBaseUOM(row, excludeCurrent: true);

				return GetCheckErrors(row, availability);
			}
			return Array.Empty<PXExceptionInfo>();
		}

		protected virtual IEnumerable<PXExceptionInfo> GetCheckErrors(ILSMaster row, IStatus availability)
		{
			if (!IsAvailableQty(row, availability))
			{
				string message = GetErrorMessageQtyAvail(GetStatusLevel(availability));

				if (message != null)
					yield return new PXExceptionInfo(PXErrorLevel.Warning, message);
			}
		}

		protected virtual bool IsAvailableQty(ILSMaster row, IStatus availability)
		{
			if (row.InvtMult == -1 && row.BaseQty > 0m && availability != null)
				if (availability.QtyNotAvail < 0m && (availability.QtyAvail + availability.QtyNotAvail) < 0m)
					return false;

			return true;
		}

		protected virtual string GetErrorMessageQtyAvail(StatusLevel level)
		{
			switch (level)
			{
				case StatusLevel.LotSerial: return Messages.StatusCheck_QtyLotSerialNegative;
				case StatusLevel.Location: return Messages.StatusCheck_QtyLocationNegative;
				case StatusLevel.Site: return Messages.StatusCheck_QtyNegative;
				default: throw new ArgumentOutOfRangeException(nameof(level));
			}
		}

		protected virtual string GetErrorMessageQtyOnHand(StatusLevel level)
		{
			switch (level)
			{
				case StatusLevel.LotSerial: return Messages.StatusCheck_QtyLotSerialOnHandNegative;
				case StatusLevel.Location: return Messages.StatusCheck_QtyLocationOnHandNegative;
				case StatusLevel.Site: return Messages.StatusCheck_QtyOnHandNegative;
				default: throw new ArgumentOutOfRangeException(nameof(level));
			}
		}

		protected virtual StatusLevel GetStatusLevel(IStatus availability)
		{
			switch (availability)
			{
				case LotSerialStatus _: return StatusLevel.LotSerial;
				case LocationStatus _: return StatusLevel.Location;
				case SiteStatus _: return StatusLevel.Site;
				default: throw new ArgumentOutOfRangeException(nameof(availability));
			}
		}
		#endregion

		#region Fetch
		public bool IsFetching { get; protected set; }

		public IStatus FetchWithLineUOM(TLine line, bool excludeCurrent = false)
		{
			if (FetchWithBaseUOM(line, excludeCurrent) is IStatus availability)
				return availability.Multiply(GetUnitRate(line));

			return null;
		}

		public virtual IStatus FetchWithBaseUOM(ILSMaster row, bool excludeCurrent = false)
		{
			if (row == null)
				return null;

			try
			{
				IsFetching = true;

				TSplit split = EnsureSplit(row);
				return Fetch(split, excludeCurrent);
			}
			finally
			{
				IsFetching = false;
			}
		}


		protected virtual IStatus Fetch(ILSDetail split, bool excludeCurrent)
		{
			if (split == null || split.InventoryID == null || split.SubItemID == null || split.SiteID == null)
				return null;

			INLotSerClass lsClass =
				InventoryItem.PK.Find(Base, split.InventoryID)
				.With(ii => ii.StkItem == true ? INLotSerClass.PK.Find(Base, ii.LotSerClassID) : null);

			if (lsClass?.LotSerTrack == null)
				return null;

			if (_detailsRequested++ == DetailsCountToEnableOptimization)
				Optimize();

			if (split.LocationID != null)
			{
				if (string.IsNullOrEmpty(split.LotSerialNbr) == false &&
					(string.IsNullOrEmpty(split.AssignedNbr) || INLotSerialNbrAttribute.StringsEqual(split.AssignedNbr, split.LotSerialNbr) == false) &&
					lsClass.LotSerAssign == INLotSerAssign.WhenReceived)
				{
					return FetchLotSerial(split, excludeCurrent);
				}

				return FetchLocation(split, excludeCurrent);
			}

			return FetchSite(split, excludeCurrent);
		}

		protected virtual IStatus FetchLotSerial(ILSDetail split, bool excludeCurrent)
		{
			var acc = InitializeRecord(new LotSerialStatus
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				LocationID = split.LocationID,
				LotSerialNbr = split.LotSerialNbr
			});

			var status = INLotSerialStatus.PK.Find(Base, split.InventoryID, split.SubItemID, split.SiteID, split.LocationID, split.LotSerialNbr);

			return Fetch<LotSerialStatus>(split, PXCache<LotSerialStatus>.CreateCopy(acc), status, excludeCurrent);
		}

		protected virtual IStatus FetchLocation(ILSDetail split, bool excludeCurrent)
		{
			var acc = InitializeRecord(new LocationStatus
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				LocationID = split.LocationID
			});

			var status = INLocationStatus.PK.Find(Base, split.InventoryID, split.SubItemID, split.SiteID, split.LocationID);

			return Fetch<LocationStatus>(split, PXCache<LocationStatus>.CreateCopy(acc), status, excludeCurrent);
		}

		protected virtual IStatus FetchSite(ILSDetail split, bool excludeCurrent)
		{
			var acc = InitializeRecord(new SiteStatus
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID
			});

			var status = INSiteStatus.PK.Find(Base, split.InventoryID, split.SubItemID, split.SiteID);

			return Fetch<SiteStatus>(split, PXCache<SiteStatus>.CreateCopy(acc), status, excludeCurrent);
		}

		protected virtual IStatus Fetch<TQtyAllocated>(ILSDetail split, IStatus allocated, IStatus existing, bool excludeCurrent)
			where TQtyAllocated : class, IQtyAllocated, IBqlTable, new()
		{
			Summarize(allocated, existing);

			if (excludeCurrent)
			{
				INItemPlanIDAttribute.GetInclQtyAvail<TQtyAllocated>(SplitCache, split,
					out decimal signQtyAvail,
					out decimal signQtyHardAvail,
					out decimal signQtyActual);
				ExcludeCurrent(split, allocated, signQtyAvail, signQtyHardAvail, signQtyActual);
			}

			return allocated;
		}


		protected virtual void Summarize(IStatus allocated, IStatus existing) => allocated.Add(existing);

		protected virtual void ExcludeCurrent(ILSDetail currentSplit, IStatus allocated, decimal signQtyAvail, decimal signQtyHardAvail, decimal signQtyActual)
		{
			if (signQtyAvail != 0)
			{
				allocated.QtyAvail -= signQtyAvail * (currentSplit.BaseQty ?? 0m);
				allocated.QtyNotAvail += signQtyAvail * (currentSplit.BaseQty ?? 0m);
			}

			if (signQtyHardAvail != 0)
			{
				allocated.QtyHardAvail -= signQtyHardAvail * (currentSplit.BaseQty ?? 0m);
			}

			if (signQtyActual != 0)
			{
				allocated.QtyActual -= signQtyActual * (currentSplit.BaseQty ?? 0m);
			}
		}
		#endregion

		#region Helpers
		protected T InitializeRecord<T>(T row)
			where T : class, IBqlTable, new()
		{
			Base.RowInserted.AddHandler<T>(CleanUpOnInsert);
			try
			{
				return PXCache<T>.Insert(Base, row);
			}
			finally
			{
				Base.RowInserted.RemoveHandler<T>(CleanUpOnInsert);
			}

			void CleanUpOnInsert(PXCache cache, PXRowInsertedEventArgs e)
			{
				cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
				cache.IsDirty = false;
			}
		}

		protected decimal GetUnitRate<TInventoryID, TUOM>(TLine line)
			where TInventoryID : IBqlField
			where TUOM : IBqlField
			=> INUnitAttribute.ConvertFromBase<TInventoryID, TUOM>(LineCache, line, 1m, INPrecision.NOROUND);

		protected virtual string FormatQty(decimal? value)
			=> value?.ToString("N" + CommonSetupDecPl.Qty.ToString(), System.Globalization.NumberFormatInfo.CurrentInfo) ?? string.Empty;
		#endregion

		#region Optimization
		protected int _detailsRequested = 0;

		protected virtual int DetailsCountToEnableOptimization => 5;
		public bool IsOptimizationEnabled => _detailsRequested > DetailsCountToEnableOptimization;

		protected virtual void Optimize() { }
		#endregion

		public enum StatusLevel
		{
			Site,
			Location,
			LotSerial
		}
	}
}
