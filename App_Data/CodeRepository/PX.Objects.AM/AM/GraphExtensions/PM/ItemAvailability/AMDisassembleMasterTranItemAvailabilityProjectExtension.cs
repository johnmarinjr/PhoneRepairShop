using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in DisassemblyEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(AMDisassembleMasterTranItemAvailabilityExtension))]
	public abstract class AMDisassembleMasterTranItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<DisassemblyEntry, AMDisassembleMasterTranItemAvailabilityExtension, AMDisassembleBatch, AMDisassembleBatchSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(AMDisassembleBatch line) => null;
	}
}
