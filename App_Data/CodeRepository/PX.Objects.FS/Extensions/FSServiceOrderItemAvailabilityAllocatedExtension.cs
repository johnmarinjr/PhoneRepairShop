using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO.GraphExtensions;

namespace PX.Objects.FS
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	[PXProtectedAccess(typeof(FSServiceOrderItemAvailabilityExtension))]
	public abstract class FSServiceOrderItemAvailabilityAllocatedExtension : ItemAvailabilityAllocatedExtension<ServiceOrderEntry, FSServiceOrderItemAvailabilityExtension, FSSODet, FSSODetSplit>
	{
		protected override string GetStatusWithAllocated(FSSODet line)
		{
			string status = string.Empty;

			if (Base1.FetchWithLineUOM(line, excludeCurrent: line?.Completed != true) is IStatus availability)
			{
				decimal allocated = GetAllocatedQty(line);

				status = FormatStatusAllocated(availability, allocated, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		protected virtual decimal GetAllocatedQty(FSSODet line)
			=> PXDBQuantityAttribute.Round((line.LineQtyHardAvail ?? 0m) * GetUnitRate(line));

		private string FormatStatusAllocated(IStatus availability, decimal? allocated, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				SO.Messages.Availability_AllocatedInfo,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(allocated));
		}


		protected override Type LineQtyAvail => typeof(FSSODet.lineQtyAvail);
		protected override Type LineQtyHardAvail => typeof(FSSODet.lineQtyHardAvail);

		protected override FSSODetSplit[] GetSplits(FSSODet line) => Base.FindImplementation<FSServiceOrderLineSplittingExtension>().GetSplits(line);

		protected override FSSODetSplit EnsurePlanType(FSSODetSplit split)
		{
			if (split.PlanID != null && GetItemPlan(split.PlanID) is INItemPlan plan)
			{
				split = PXCache<FSSODetSplit>.CreateCopy(split);
				split.PlanType = plan.PlanType;
			}
			return split;
		}

		protected override Guid? DocumentNoteID => Base.ServiceOrderRecords.Current?.NoteID;


		#region PXProtectedAccess
		[PXProtectedAccess] protected abstract string FormatQty(decimal? value);
		[PXProtectedAccess] protected abstract decimal GetUnitRate(FSSODet line);
		[PXProtectedAccess] protected abstract void Check(ILSMaster row, IStatus availability);
		#endregion
	}
}
