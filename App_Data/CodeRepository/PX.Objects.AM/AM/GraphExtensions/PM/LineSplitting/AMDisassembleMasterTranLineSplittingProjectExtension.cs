using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for DisassemblyEntry
	[PXProtectedAccess(typeof(AMDisassembleMasterTranLineSplittingExtension))]
	public abstract class AMDisassembleMasterTranLineSplittingProjectExtension
		: LineSplittingProjectExtension<DisassemblyEntry, AMDisassembleMasterTranLineSplittingExtension, AMDisassembleBatch, AMDisassembleBatch, AMDisassembleBatchSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
