using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for KitAssemblyEntry
	[PXProtectedAccess(typeof(INComponentLineSplittingExtension))]
	public abstract class INComponentLineSplittingProjectExtension
		: LineSplittingProjectExtension<KitAssemblyEntry, INComponentLineSplittingExtension, INKitRegister, INComponentTran, INComponentTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
