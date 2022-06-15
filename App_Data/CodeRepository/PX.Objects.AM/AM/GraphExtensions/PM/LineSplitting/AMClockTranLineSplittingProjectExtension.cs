using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for ClockApprovalProcess
	[PXProtectedAccess(typeof(AMClockTranLineSplittingExtension))]
	public abstract class AMClockTranLineSplittingProjectExtension
		: LineSplittingProjectExtension<ClockApprovalProcess, AMClockTranLineSplittingExtension, AMClockTran, AMClockTran, AMClockTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
