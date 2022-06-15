using PX.Data;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.PO.GraphExtensions.POReceiptEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	[PXProtectedAccess(typeof(POReceiptItemAvailabilityExtension))]
	public abstract class POReceiptItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<POReceiptEntry, POReceiptItemAvailabilityExtension, POReceiptLine, POReceiptLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;

		protected override string GetStatusProject(POReceiptLine line)
		{
			string status = string.Empty;

			bool excludeCurrent = PXParentAttribute.SelectParent<POReceipt>(LineCache, line)?.Released != true;

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
				IN.Messages.Availability_ActualInfo_Project,
				uom,
				FormatQty(availabilityProject.QtyOnHand),
				FormatQty(availabilityProject.QtyAvail),
				FormatQty(availabilityProject.QtyHardAvail),
				FormatQty(availabilityProject.QtyActual),
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(availability.QtyActual));
		}
	}
}
