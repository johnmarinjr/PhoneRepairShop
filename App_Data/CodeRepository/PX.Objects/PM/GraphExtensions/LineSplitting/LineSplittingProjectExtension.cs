using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;

using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	public abstract class LineSplittingProjectExtension<TGraph, TLSExt, TPrimary, TLine, TSplit> : PXGraphExtension<TLSExt, TGraph>
		where TGraph : PXGraph
		where TLSExt: LineSplittingExtension<TGraph, TPrimary, TLine, TSplit>
		where TPrimary : class, IBqlTable, new()
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		protected TLSExt LSBase => Base1;

		protected static bool UseProjectAvailability => PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();

		protected virtual bool IsLinkedProject(int? projectID)
		{
			return
				projectID.IsNotIn(null, ProjectDefaultAttribute.NonProject()) &&
				PMProject.PK.Find(Base, projectID) is PMProject project &&
				project.AccountingMode == ProjectAccountingModes.Linked;
		}


		/// Overrides <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.IssueNumbersInternal(TLine, decimal)"/>
		[PXOverride]
		public virtual void IssueNumbersInternal(TLine line, decimal deltaBaseQty,
			Action<TLine, decimal> base_IssueNumbersInternal)
		{
			if (UseProjectAvailability)
				IssueNumbers(line, deltaBaseQty, Base.Caches<PMLotSerialStatus>(), Base.Caches<PMLotSerialStatusAccum>());
			else
				base_IssueNumbersInternal(line, deltaBaseQty);
		}


		#region Select LotSerial Status
		/// Overrides <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.SelectSerialStatus(TLine, PXResult{InventoryItem, INLotSerClass})"/>
		[PXOverride]
		public virtual List<ILotSerial> SelectSerialStatus(TLine line, PXResult<InventoryItem, INLotSerClass> item,
			Func<TLine, PXResult<InventoryItem, INLotSerClass>, List<ILotSerial>> base_SelectSerialStatus)
		{
			if (UseProjectAvailability)
			{
				PXSelectBase<PMLotSerialStatus> cmd = GetSerialStatusCmdProject(line, item);
				PMLotSerialStatus pars = MakePMLotSerialStatus(line);
				if (IsLinkedProject(line.ProjectID))
				{
					pars.ProjectID = ProjectDefaultAttribute.NonProject();
					pars.TaskID = 0;
				}

				List<PMLotSerialStatus> list = cmd.View.SelectMultiBound(new object[] { pars }).RowCast<PMLotSerialStatus>().ToList();
				return new List<ILotSerial>(list);
			}
			else
			{
				return base_SelectSerialStatus(line, item);
			}
		}

		public virtual PXSelectBase<PMLotSerialStatus> GetSerialStatusCmdProject(TLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			PXSelectBase<PMLotSerialStatus> cmd = GetSerialStatusCmdBaseProject(line, item);
			AppendSerialStatusCmdWhereProject(cmd, line, item);
			AppendSerialStatusCmdOrderByProject(cmd, line, item);

			return cmd;
		}

		protected virtual PXSelectBase<PMLotSerialStatus> GetSerialStatusCmdBaseProject(TLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			return new
				SelectFrom<PMLotSerialStatus>.
				InnerJoin<INLocation>.On<PMLotSerialStatus.FK.Location>.
				Where<
					PMLotSerialStatus.inventoryID.IsEqual<PMLotSerialStatus.inventoryID.FromCurrent>.
					And<PMLotSerialStatus.siteID.IsEqual<PMLotSerialStatus.siteID.FromCurrent>>.
					And<PMLotSerialStatus.projectID.IsEqual<PMLotSerialStatus.projectID.FromCurrent>>.
					And<PMLotSerialStatus.taskID.IsEqual<PMLotSerialStatus.taskID.FromCurrent>>.
					And<PMLotSerialStatus.qtyOnHand.IsGreater<decimal0>>>.
				View(Base);
		}

		protected virtual void AppendSerialStatusCmdWhereProject(PXSelectBase<PMLotSerialStatus> cmd, TLine line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<PMLotSerialStatus.subItemID.IsEqual<PMLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
				cmd.WhereAnd<Where<PMLotSerialStatus.locationID.IsEqual<PMLotSerialStatus.locationID.FromCurrent>>>();
			else
				cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(line.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<PMLotSerialStatus.lotSerialNbr.IsEqual<PMLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}

		public virtual void AppendSerialStatusCmdOrderByProject(PXSelectBase<PMLotSerialStatus> cmd, TLine line, INLotSerClass lotSerClass)
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
		#endregion

		/// Overrides <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.ExpireLotSerialStatusCacheFor(TSplit)"/>
		[PXOverride]
		public virtual void ExpireLotSerialStatusCacheFor(TSplit split,
			Action<TSplit> base_ExpireLotSerialStatusCacheFor)
		{
			if (UseProjectAvailability)
				ExpireCached(MakePMLotSerialStatus(split));
			else
				base_ExpireLotSerialStatusCacheFor(split);
		}

		/// Overrides <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.ExpireCachedItems(TSplit)"/>
		[PXOverride]
		public virtual void ExpireCachedItems(TSplit split,
			Action<TSplit> base_ExpireCachedItems)
		{
			base_ExpireCachedItems(split);
			if (UseProjectAvailability)
				ExpireCached(MakePMLotSerialStatus(split));
		}

		protected virtual PMLotSerialStatus MakePMLotSerialStatus(ILSMaster item)
		{
			var ret = new PMLotSerialStatus
			{
				InventoryID = item.InventoryID,
				SiteID = item.SiteID,
				LocationID = item.LocationID,
				SubItemID = item.SubItemID,
				LotSerialNbr = item.LotSerialNbr,
				ProjectID = item.ProjectID,
				TaskID = item.TaskID ?? 0
			};

			return ret;
		}

		#region Protected Access
		/// Uses <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.IssueNumbers(TLine, decimal, PXCache, PXCache)"/>
		[PXProtectedAccess] protected abstract void IssueNumbers(TLine line, decimal deltaBaseQty, PXCache statusCache, PXCache statusAccumCache);

		/// Uses <see cref="LineSplittingExtension{TGraph, TPrimary, TLine, TSplit}.ExpireCached{T}(T)"/>
		[PXProtectedAccess] protected abstract void ExpireCached<T>(T item) where T : class, IBqlTable, new();
		#endregion
	}
}
