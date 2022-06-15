using System;

using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	public abstract class ItemAvailabilityProjectExtension<TGraph, TItemAvailExt, TLine, TSplit> : PXGraphExtension<TItemAvailExt, TGraph>
		where TGraph : PXGraph
		where TItemAvailExt : ItemAvailabilityExtension<TGraph, TLine, TSplit>
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		protected TItemAvailExt ItemAvailBase => Base1;

		protected static bool UseProjectAvailability => PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();

		protected virtual bool IsLinkedProject(int? projectID)
		{
			return
				projectID.IsNotIn(null, ProjectDefaultAttribute.NonProject()) &&
				PMProject.PK.Find(Base, projectID) is PMProject project &&
				project.AccountingMode == ProjectAccountingModes.Linked;
		}


		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.GetStatus(TLine)"/>
		[PXOverride]
		public virtual string GetStatus(TLine line,
			Func<TLine, string> base_GetStatus)
		{
			if (UseProjectAvailability)
				return GetStatusProject(line) ?? base_GetStatus(line);
			else
				return base_GetStatus(line);
		}

		protected abstract string GetStatusProject(TLine line);

		#region Check
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.Check(ILSMaster)"/>
		[PXOverride]
		public virtual void Check(ILSMaster row,
			Action<ILSMaster> base_Check)
		{
			using (UseProjectAvailability ? ProjectAvailabilityScope() : null)
				base_Check(row);
		}

		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.GetStatusLevel(IStatus)"/>
		[PXOverride]
		public virtual ItemAvailabilityExtension<TGraph, TLine, TSplit>.StatusLevel GetStatusLevel(IStatus availability,
			Func<IStatus, ItemAvailabilityExtension<TGraph, TLine, TSplit>.StatusLevel> base_GetWarningLevel)
		{
			switch (availability)
			{
				case PMLotSerialStatusAccum _:
					return ItemAvailabilityExtension<TGraph, TLine, TSplit>.StatusLevel.LotSerial;
				case PMLocationStatusAccum _:
					return ItemAvailabilityExtension<TGraph, TLine, TSplit>.StatusLevel.Location;
				case PMSiteStatusAccum _:
				case PMSiteSummaryStatusAccum _:
					return ItemAvailabilityExtension<TGraph, TLine, TSplit>.StatusLevel.Site;
				default:
					return base_GetWarningLevel(availability);
			}
		}
		#endregion

		#region Fetch
		public IStatus FetchWithLineUOMProject(TLine line, bool excludeCurrent = false)
		{
			using (ProjectAvailabilityScope())
				return FetchWithLineUOM(line, excludeCurrent);
		}

		public virtual IStatus FetchWithBaseUOMProject(ILSMaster row, bool excludeCurrent = false)
		{
			using (ProjectAvailabilityScope())
				return FetchWithBaseUOM(row, excludeCurrent);
		}

		#region Overrides
		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchLotSerial(ILSDetail, bool)"/>
		[PXOverride]
		public virtual IStatus FetchLotSerial(ILSDetail split, bool excludeCurrent,
			Func<ILSDetail, bool, IStatus> base_FetchLotSerial)
		{
			if (!_projectAvailability)
				return base_FetchLotSerial(split, excludeCurrent);

			if (split.ProjectID == null)
			{
				TLine line = PXParentAttribute.SelectParent<TLine>(SplitCache, split);
				if (line != null)
				{
					split.ProjectID = line.ProjectID;
					split.TaskID = line.TaskID;
				}
			}

			var acc = InitializeRecord(new PMLotSerialStatusAccum
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				LocationID = split.LocationID,
				LotSerialNbr = split.LotSerialNbr,
				ProjectID = split.ProjectID ?? ProjectDefaultAttribute.NonProject() ?? 0,
				TaskID = split.TaskID ?? 0
			});

			var status = PMLotSerialStatus.PK.Find(Base, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.LocationID, acc.LotSerialNbr, acc.ProjectID, acc.TaskID);

			return Fetch<PMLotSerialStatusAccum>(split, PXCache<PMLotSerialStatusAccum>.CreateCopy(acc), status, excludeCurrent);
		}

		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchLocation(ILSDetail, bool)"/>
		[PXOverride]
		public virtual IStatus FetchLocation(ILSDetail split, bool excludeCurrent,
			Func<ILSDetail, bool, IStatus> base_FetchLocation)
		{
			if (!_projectAvailability)
				return base_FetchLocation(split, excludeCurrent);

			if (split.ProjectID == null)
			{
				TLine line = PXParentAttribute.SelectParent<TLine>(SplitCache, split);
				if (line != null)
				{
					split.ProjectID = line.ProjectID;
					split.TaskID = line.TaskID;
				}
			}

			var acc = InitializeRecord(new PMLocationStatusAccum
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				LocationID = split.LocationID,
				ProjectID = split.ProjectID ?? ProjectDefaultAttribute.NonProject() ?? 0,
				TaskID = split.TaskID ?? 0
			});

			var status = PMLocationStatus.PK.Find(Base, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.LocationID, acc.ProjectID, acc.TaskID);

			return Fetch<PMLocationStatusAccum>(split, PXCache<PMLocationStatusAccum>.CreateCopy(acc), status, excludeCurrent);
		}

		/// Overrides <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchSite(ILSDetail, bool)"/>
		[PXOverride]
		public virtual IStatus FetchSite(ILSDetail split, bool excludeCurrent,
			Func<ILSDetail, bool, IStatus> base_FetchSite)
		{
			if (!_projectAvailability)
				return base_FetchSite(split, excludeCurrent);

			if (split.ProjectID == null)
			{
				TLine line = PXParentAttribute.SelectParent<TLine>(SplitCache, split);
				if (line != null)
				{
					split.ProjectID = line.ProjectID;
					split.TaskID = line.TaskID;
				}
			}

			if (split.TaskID == null)
				return FetchSiteByProject(split, excludeCurrent);
			else
				return FetchSiteByTask(split, excludeCurrent);
		}

		private IStatus FetchSiteByProject(ILSDetail split, bool excludeCurrent)
		{
			int? projectID = split.ProjectID;

			if (IsLinkedProject(projectID))
				projectID = ProjectDefaultAttribute.NonProject();

			var acc = InitializeRecord(new PMSiteSummaryStatusAccum
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				ProjectID = projectID
			});

			var status = PMSiteSummaryStatus.PK.Find(Base, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.ProjectID);

			return Fetch<PMSiteSummaryStatusAccum>(split, PXCache<PMSiteSummaryStatusAccum>.CreateCopy(acc), status, excludeCurrent);
		}

		private IStatus FetchSiteByTask(ILSDetail split, bool excludeCurrent)
		{
			int? projectID = split.ProjectID;
			int? taskID = split.TaskID;

			if (projectID == null)
			{
				TLine line = PXParentAttribute.SelectParent<TLine>(SplitCache, split);
				if (line != null)
				{
					projectID = line.ProjectID;
					taskID = line.TaskID;
				}
			}

			if (IsLinkedProject(projectID))
			{
				projectID = ProjectDefaultAttribute.NonProject();
				taskID = 0;
			}

			var acc = InitializeRecord(new PMSiteStatusAccum
			{
				InventoryID = split.InventoryID,
				SubItemID = split.SubItemID,
				SiteID = split.SiteID,
				ProjectID = projectID,
				TaskID = taskID
			});

			var status = PMSiteStatus.PK.Find(Base, acc.InventoryID, acc.SubItemID, acc.SiteID, acc.ProjectID, acc.TaskID);

			return Fetch<PMSiteStatusAccum>(split, PXCache<PMSiteStatusAccum>.CreateCopy(acc), status, excludeCurrent);
		}
		#endregion
		#endregion

		#region ProjectAvailabilityScope
		protected IDisposable ProjectAvailabilityScope() => new Common.SimpleScope(
			onOpen: () => _projectAvailability = true,
			onClose: () => _projectAvailability = false);
		private bool _projectAvailability;
		#endregion

		#region Protected Access
		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.LineCache"/>
		[PXProtectedAccess] protected abstract PXCache<TLine> LineCache { get; }

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.SplitCache"/>
		[PXProtectedAccess] protected abstract PXCache<TSplit> SplitCache { get; }

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.Fetch{TQtyAllocated}(ILSDetail, IStatus, IStatus, bool)"/>
		[PXProtectedAccess] protected abstract IStatus Fetch<TQtyAllocated>(ILSDetail split, IStatus allocated, IStatus existing, bool excludeCurrent) where TQtyAllocated : class, IQtyAllocated, IBqlTable, new();

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.InitializeRecord{T}(T)"/>
		[PXProtectedAccess] protected abstract T InitializeRecord<T>(T row) where T : class, IBqlTable, new();

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchWithLineUOM(TLine, bool)"/>
		[PXProtectedAccess] protected abstract IStatus FetchWithLineUOM(TLine line, bool excludeCurrent = false);

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FetchWithBaseUOM(ILSMaster, bool)"/>
		[PXProtectedAccess] protected abstract IStatus FetchWithBaseUOM(ILSMaster row, bool excludeCurrent = false);

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.Check(ILSMaster, IStatus)"/>
		[PXProtectedAccess] protected abstract void Check(ILSMaster row, IStatus availability);

		/// Uses <see cref="ItemAvailabilityExtension{TGraph, TLine, TSplit}.FormatQty(decimal?)"/>
		[PXProtectedAccess] protected abstract string FormatQty(decimal? value);
		#endregion
	}
}
