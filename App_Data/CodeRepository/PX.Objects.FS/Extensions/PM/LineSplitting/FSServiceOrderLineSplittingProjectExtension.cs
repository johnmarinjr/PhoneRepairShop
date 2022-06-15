using PX.Data;
using PX.Objects.FS;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for ServiceOrderEntry
	[PXProtectedAccess(typeof(FSServiceOrderLineSplittingExtension))]
	public abstract class FSServiceOrderLineSplittingProjectExtension
		: LineSplittingProjectExtension<ServiceOrderEntry, FSServiceOrderLineSplittingExtension, FSServiceOrder, FSSODet, FSSODetSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
