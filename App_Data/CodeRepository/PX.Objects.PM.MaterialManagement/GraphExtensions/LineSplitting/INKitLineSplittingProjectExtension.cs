using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for KitAssemblyEntry
	[PXProtectedAccess(typeof(INKitLineSplittingExtension))]
	public abstract class INKitLineSplittingProjectExtension
		: LineSplittingProjectExtension<KitAssemblyEntry, INKitLineSplittingExtension, INKitRegister, INKitRegister, INKitTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
