using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	[PXProtectedAccess(typeof(SOShipmentItemAvailabilityExtension))]
	public abstract class SOShipmentItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<SOShipmentEntry, SOShipmentItemAvailabilityExtension, SOShipLine, SOShipLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;

		protected override string GetStatusProject(SOShipLine line)
		{
			string status = string.Empty;

			if (FetchWithLineUOM(line, excludeCurrent: true) is IStatus availability &&
				FetchWithLineUOMProject(line, excludeCurrent: true) is IStatus availabilityProject)
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
