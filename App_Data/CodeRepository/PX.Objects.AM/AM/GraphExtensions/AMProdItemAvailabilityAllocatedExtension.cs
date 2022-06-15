using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO.GraphExtensions;

namespace PX.Objects.AM
{
	public abstract class AMProdItemAvailabilityAllocatedExtension<TGraph, TProdItemAvailExt> : ItemAvailabilityAllocatedExtension<TGraph, TProdItemAvailExt, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
		where TProdItemAvailExt : AMProdItemAvailabilityExtension<TGraph>
	{
		protected override string GetStatusWithAllocated(AMProdItem line) => string.Empty;

		protected override Type LineQtyAvail => typeof(AMProdItem.lineQtyAvail);
		protected override Type LineQtyHardAvail => typeof(AMProdItem.lineQtyHardAvail);

		protected override AMProdItemSplit[] GetSplits(AMProdItem line) => Base.FindImplementation<AMProdItemLineSplittingExtension<TGraph>>().GetSplits(line);

		protected override AMProdItemSplit EnsurePlanType(AMProdItemSplit split)
		{
			if (split.PlanID != null && GetItemPlan(split.PlanID) is INItemPlan plan)
			{
				split = PXCache<AMProdItemSplit>.CreateCopy(split);
				//split.PlanType = plan.PlanType;
			}
			return split;
		}

		protected override Guid? DocumentNoteID => ((AMProdItem)Base.Caches<AMProdItem>().Current)?.NoteID;
	}
}
