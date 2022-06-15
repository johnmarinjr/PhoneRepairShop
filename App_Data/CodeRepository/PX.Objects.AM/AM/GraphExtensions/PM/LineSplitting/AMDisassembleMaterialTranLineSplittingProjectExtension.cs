using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for DisassemblyEntry
	[PXProtectedAccess(typeof(AMDisassembleMaterialTranLineSplittingExtension))]
	public abstract class AMDisassembleMaterialTranLineSplittingProjectExtension
		: LineSplittingProjectExtension<DisassemblyEntry, AMDisassembleMaterialTranLineSplittingExtension, AMDisassembleBatch, AMDisassembleTran, AMDisassembleTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
