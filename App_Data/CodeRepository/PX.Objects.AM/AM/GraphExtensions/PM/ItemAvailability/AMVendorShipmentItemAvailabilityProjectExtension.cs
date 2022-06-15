using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in VendorShipmentEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(AMVendorShipmentItemAvailabilityExtension))]
	public abstract class AMVendorShipmentItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<VendorShipmentEntry, AMVendorShipmentItemAvailabilityExtension, AMVendorShipLine, AMVendorShipLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(AMVendorShipLine line) => null;
	}
}
