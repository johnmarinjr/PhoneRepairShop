using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	[PXProtectedAccess(typeof(SOOrderItemAvailabilityExtension))]
	public abstract class SOOrderItemAvailabilityAllocatedExtension : ItemAvailabilityAllocatedExtension<SOOrderEntry, SOOrderItemAvailabilityExtension, SOLine, SOLineSplit>
	{
		protected override string GetStatusWithAllocated(SOLine line)
		{
			string status = string.Empty;

			if (Base1.FetchWithLineUOM(line, excludeCurrent: line?.Completed != true) is IStatus availability)
			{
				decimal? allocated = GetAllocatedQty(line);

				status = FormatStatusAllocated(availability, allocated, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		protected virtual decimal GetAllocatedQty(SOLine line)
			=> PXDBQuantityAttribute.Round((line.LineQtyHardAvail ?? 0m) * GetUnitRate(line));

		private string FormatStatusAllocated(IStatus availability, decimal? allocated, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				Messages.Availability_AllocatedInfo,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(allocated));
		}


		protected override Type LineQtyAvail => typeof(SOLine.lineQtyAvail);
		protected override Type LineQtyHardAvail => typeof(SOLine.lineQtyHardAvail);

		protected override SOLineSplit[] GetSplits(SOLine line) => Base.FindImplementation<SOOrderLineSplittingExtension>().GetSplits(line);

		protected override SOLineSplit EnsurePlanType(SOLineSplit split)
		{
			if (split.PlanID != null && GetItemPlan(split.PlanID) is INItemPlan plan)
			{
				split = PXCache<SOLineSplit>.CreateCopy(split);
				split.PlanType = plan.PlanType;
			}
			return split;
		}

		protected override Guid? DocumentNoteID => Base.Document.Current?.NoteID;


		#region PXProtectedAccess
		[PXProtectedAccess] protected abstract string FormatQty(decimal? value);
		[PXProtectedAccess] protected abstract decimal GetUnitRate(SOLine line);
		[PXProtectedAccess] protected abstract void Check(ILSMaster row, IStatus availability);
		#endregion
	}	
}
