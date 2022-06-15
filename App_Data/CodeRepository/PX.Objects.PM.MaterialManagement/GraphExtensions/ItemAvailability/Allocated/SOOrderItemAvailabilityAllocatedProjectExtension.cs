using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability.Allocated
{
	[PXProtectedAccess]
	public abstract class SOOrderItemAvailabilityAllocatedProjectExtension : ItemAvailabilityAllocatedProjectExtension<
		SOOrderEntry,
		SOOrderItemAvailabilityExtension,
		SOOrderItemAvailabilityAllocatedExtension,
		SOOrderItemAvailabilityProjectExtension,
		SOLine, SOLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;

		protected override string GetStatusWithAllocatedProject(SOLine line)
		{
			string status = string.Empty;

			bool excludeCurrent = line?.Completed != true;

			if (ItemAvailBase.FetchWithLineUOM(line, excludeCurrent) is IStatus availability &&
				ItemAvailProjExt.FetchWithLineUOMProject(line, excludeCurrent) is IStatus availabilityProject)
			{
				decimal? allocated = GetAllocatedQty(line);

				status = FormatStatusAllocatedProject(availability, availabilityProject, allocated, line.UOM);
				Check(line, availabilityProject);
			}

			return status;
		}

		private string FormatStatusAllocatedProject(IStatus availability, IStatus availabilityProject, decimal? allocated, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				SO.Messages.Availability_AllocatedInfo_Project,
				uom,
				FormatQty(availabilityProject.QtyOnHand),
				FormatQty(availabilityProject.QtyAvail),
				FormatQty(availabilityProject.QtyHardAvail),
				FormatQty(allocated),
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail));
		}

		#region PXProtectedAccess
		/// Uses <see cref="SOOrderItemAvailabilityAllocatedExtension.GetAllocatedQty(SOLine)"/>
		[PXProtectedAccess(typeof(SOOrderItemAvailabilityAllocatedExtension))] protected abstract decimal GetAllocatedQty(SOLine line);

		/// Uses <see cref="IN.GraphExtensions.ItemAvailabilityExtension{TGraph, TLine, TSplit}.Check(ILSMaster, IStatus)"/>
		[PXProtectedAccess(typeof(SOOrderItemAvailabilityExtension))] protected abstract void Check(ILSMaster row, IStatus availability);

		/// Uses <see cref="IN.GraphExtensions.ItemAvailabilityExtension{TGraph, TLine, TSplit}.FormatQty(decimal?)"/>
		[PXProtectedAccess(typeof(SOOrderItemAvailabilityExtension))] protected abstract string FormatQty(decimal? value);
		#endregion
	}
}
