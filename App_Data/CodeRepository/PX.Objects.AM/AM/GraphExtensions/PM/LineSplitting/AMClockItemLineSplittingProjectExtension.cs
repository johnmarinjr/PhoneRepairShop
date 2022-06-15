using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for ClockEntry
	[PXProtectedAccess(typeof(AMClockItemLineSplittingExtension))]
	public abstract class AMClockItemLineSplittingProjectExtension
		: LineSplittingProjectExtension<ClockEntry, AMClockItemLineSplittingExtension, AMClockItem, AMClockItem, AMClockItemSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
