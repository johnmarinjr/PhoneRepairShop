using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.SO.GraphExtensions
{
	public abstract class ItemAvailabilityAllocatedExtension<TGraph, TItemAvailExt, TLine, TSplit> : PXGraphExtension<TItemAvailExt, TGraph>
		where TGraph : PXGraph
		where TItemAvailExt : ItemAvailabilityExtension<TGraph, TLine, TSplit>
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		protected TItemAvailExt ItemAvailBase => Base1;

		protected abstract Type LineQtyAvail { get; }
		protected abstract Type LineQtyHardAvail { get; }

		public virtual bool IsAllocationEntryEnabled
		{
			get
			{
				SOOrderType ordertype = PXSetup<SOOrderType>.Select(Base);
				return ordertype == null || ordertype.RequireShipping == true;
			}
		}


		/// <summary>
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.GetStatus(TLine)"/>
		/// </summary>
		[PXOverride]
		public virtual string GetStatus(TLine line,
			Func<TLine, string> base_GetStatus)
		{
			if (IsAllocationEntryEnabled)
				return GetStatusWithAllocated(line);
			else
				return base_GetStatus(line);
		}

		protected abstract string GetStatusWithAllocated(TLine line);


		protected TLine LineToExcludeAllocated { get; private set; }

		/// <summary>
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchWithLineUOM(TLine, bool)"/>
		/// </summary>
		[PXOverride]
		public virtual IStatus FetchWithBaseUOM(ILSMaster row, bool excludeCurrent,
			Func<ILSMaster, bool, IStatus> base_FetchWithBaseUOM)
		{
			try
			{
				if (row is TLine line)
					LineToExcludeAllocated = line;

				return base_FetchWithBaseUOM(row, excludeCurrent);
			}
			finally
			{
				LineToExcludeAllocated = null;
			}
		}

		/// <summary>
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.ExcludeCurrent(ILSDetail, IStatus, decimal, decimal, decimal)"/>
		/// </summary>
		[PXOverride]
		public virtual void ExcludeCurrent(ILSDetail currentSplit, IStatus allocated, decimal signQtyAvail, decimal signQtyHardAvail, decimal signQtyActual,
			Action<ILSDetail, IStatus, decimal, decimal, decimal> base_ExcludeCurrent)
		{
			if (LineToExcludeAllocated != null)
				ExcludeAllocated(LineToExcludeAllocated, allocated);
			else
				base_ExcludeCurrent(currentSplit, allocated, signQtyAvail, signQtyHardAvail, signQtyActual);
		}

		protected virtual IStatus ExcludeAllocated(TLine line, IStatus availability)
		{
			if (availability == null)
				return null;

			var lineCache = Base.Caches<TLine>();

			decimal? lineQtyAvail = (decimal?) lineCache.GetValue(line, LineQtyAvail.Name);
			decimal? lineQtyHardAvail = (decimal?) lineCache.GetValue(line, LineQtyHardAvail.Name);

			if (lineQtyAvail == null || lineQtyHardAvail == null)
			{
				var splitCache = Base.Caches<TSplit>();

				lineQtyAvail = 0m;
				lineQtyHardAvail = 0m;

				foreach (TSplit split in GetSplits(line))
				{
					TSplit actualSplit = EnsurePlanType(split);

					PXParentAttribute.SetParent(splitCache, actualSplit, typeof(TLine), line);

					INItemPlanIDAttribute.GetInclQtyAvail<SiteStatus>(splitCache, actualSplit, out decimal signQtyAvail, out decimal signQtyHardAvail);

					if (signQtyAvail != 0m)
						lineQtyAvail -= signQtyAvail * (actualSplit.BaseQty ?? 0m);

					if (signQtyHardAvail != 0m)
						lineQtyHardAvail -= signQtyHardAvail * (actualSplit.BaseQty ?? 0m);
				}

				lineCache.SetValue(line, LineQtyAvail.Name, lineQtyAvail);
				lineCache.SetValue(line, LineQtyHardAvail.Name, lineQtyHardAvail);
			}

			availability.QtyAvail += lineQtyAvail;
			availability.QtyHardAvail += lineQtyHardAvail;
			availability.QtyNotAvail = -lineQtyAvail;

			return availability;
		}

		protected abstract TSplit EnsurePlanType(TSplit split);
		protected virtual INItemPlan GetItemPlan(long? planID) => SelectFrom<INItemPlan>.Where<INItemPlan.planID.IsEqual<@P.AsLong>>.View.Select(Base, planID);

		protected abstract TSplit[] GetSplits(TLine line);


		/// <summary>
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.Optimize"/>
		/// </summary>
		[PXOverride]
		public virtual void Optimize(Action base_Optimize)
		{
			base_Optimize();

			if (DocumentNoteID != null)
				foreach (INItemPlan plan in
					SelectFrom<INItemPlan>.
					Where<INItemPlan.refNoteID.IsEqual<@P.AsGuid>>.
					View.Select(Base, DocumentNoteID))
				{
					SelectFrom<INItemPlan>.
					Where<INItemPlan.planID.IsEqual<@P.AsLong>>.
					View.StoreResult(Base, plan);
				}
		}

		protected abstract Guid? DocumentNoteID { get; }
	}
}
