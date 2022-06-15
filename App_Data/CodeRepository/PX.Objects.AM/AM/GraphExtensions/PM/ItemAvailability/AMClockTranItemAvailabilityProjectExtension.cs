using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in ClockApprovalProcess?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(AMClockTranItemAvailabilityExtension))]
	public abstract class AMClockTranItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<ClockApprovalProcess, AMClockTranItemAvailabilityExtension, AMClockTran, AMClockTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(AMClockTran line) => null;
	}
}
