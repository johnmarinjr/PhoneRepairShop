using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for VendorShipmentEntry
	[PXProtectedAccess(typeof(AMVendorShipmentLineSplittingExtension))]
	public abstract class AMVendorShipmentLineSplittingProjectExtension
		: LineSplittingProjectExtension<VendorShipmentEntry, AMVendorShipmentLineSplittingExtension, AMVendorShipment, AMVendorShipLine, AMVendorShipLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
