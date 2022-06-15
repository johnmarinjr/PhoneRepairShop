using System;
using PX.Common;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO.GraphExtensions;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
	public abstract class AMProdMatlItemAvailabilityAllocatedExtension<TGraph, TProdMatlAvailExt> : ItemAvailabilityAllocatedExtension<TGraph, TProdMatlAvailExt, AMProdMatl, AMProdMatlSplit>
		where TGraph : PXGraph
		where TProdMatlAvailExt : AMProdMatlItemAvailabilityExtension<TGraph>
	{
		public override bool IsAllocationEntryEnabled => true;

		protected override string GetStatusWithAllocated(AMProdMatl line)
		{
			string status = string.Empty;

			if (Base1.FetchWithLineUOM(line, excludeCurrent: !IsMaterialCompleted(line)) is IStatus availability)
			{
				decimal allocated = GetAllocatedQty(line);

				status = FormatStatusAllocated(availability, allocated, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		protected virtual decimal GetAllocatedQty(AMProdMatl line)
			=> PXDBQuantityAttribute.Round((line.LineQtyHardAvail ?? 0m) * GetUnitRate(line));

		private string FormatStatusAllocated(IStatus availability, decimal allocated, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				SO.Messages.Availability_AllocatedInfo,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(allocated));
		}

		protected virtual bool IsMaterialCompleted(AMProdMatl line)
			=> line != null && (line.QtyRemaining.GetValueOrDefault() == 0m || line.StatusID.IsIn(ProductionOrderStatus.Completed, ProductionOrderStatus.Closed, ProductionOrderStatus.Cancel));


		protected override Type LineQtyAvail => typeof(AMProdMatl.lineQtyAvail);
		protected override Type LineQtyHardAvail => typeof(AMProdMatl.lineQtyHardAvail);

		protected override AMProdMatlSplit[] GetSplits(AMProdMatl line) => Base.FindImplementation<AMProdMatlLineSplittingExtension<TGraph>>().GetSplits(line);

		protected override AMProdMatlSplit EnsurePlanType(AMProdMatlSplit split)
		{
			if (split.PlanID != null && GetItemPlan(split.PlanID) is INItemPlan plan)
			{
				split = PXCache<AMProdMatlSplit>.CreateCopy(split);
				//split.PlanType = plan.PlanType;
			}
			return split;
		}

		protected override Guid? DocumentNoteID => ((AMProdMatl)Base.Caches<AMProdMatl>().Current)?.NoteID;


		#region PXProtectedAccess
		[PXProtectedAccess] protected abstract string FormatQty(decimal? value);
		[PXProtectedAccess] protected abstract decimal GetUnitRate(AMProdMatl line);
		[PXProtectedAccess] protected abstract void Check(ILSMaster row, IStatus availability);
		#endregion
	}
}
