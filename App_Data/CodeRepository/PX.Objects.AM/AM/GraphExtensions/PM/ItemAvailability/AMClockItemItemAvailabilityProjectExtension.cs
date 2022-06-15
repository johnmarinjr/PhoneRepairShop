using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in ClockEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(AMClockItemItemAvailabilityExtension))]
	public abstract class AMClockItemItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<ClockEntry, AMClockItemItemAvailabilityExtension, AMClockItem, AMClockItemSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(AMClockItem line) => null;
	}
}
