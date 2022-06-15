using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	[PXProtectedAccess(typeof(SOOrderItemAvailabilityExtension))]
	public abstract class SOOrderItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<SOOrderEntry, SOOrderItemAvailabilityExtension, SOLine, SOLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;

		protected override string GetStatusProject(SOLine line)
		{
			string status = string.Empty;

			bool excludeCurrent = line?.Completed != true;

			if (FetchWithLineUOM(line, excludeCurrent) is IStatus availability &&
				FetchWithLineUOMProject(line, excludeCurrent) is IStatus availabilityProject)
			{
				status = FormatStatusProject(availability, availabilityProject, line.UOM);
				Check(line, availabilityProject);
			}

			return status;
		}

		private string FormatStatusProject(IStatus availability, IStatus availabilityProject, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				IN.Messages.Availability_Info_Project,
				uom,
				FormatQty(availabilityProject.QtyOnHand),
				FormatQty(availabilityProject.QtyAvail),
				FormatQty(availabilityProject.QtyHardAvail),
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail));
		}
	}
}
