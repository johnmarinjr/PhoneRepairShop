using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for AMBatchEntryBase
	public abstract class AMBatchLineSplittingProjectExtension<TBatchGraph, TBatchLSExt> : LineSplittingProjectExtension<TBatchGraph, TBatchLSExt, AMBatch, AMMTran, AMMTranSplit>
		where TBatchGraph : AMBatchEntryBase
		where TBatchLSExt : AMBatchLineSplittingExtension<TBatchGraph>
	{
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for MaterialEntry
	[PXProtectedAccess(typeof(MaterialEntry.LineSplittingExtension))]
	public abstract class MaterialEntry_LineSplittingProjectExtension
		: AMBatchLineSplittingProjectExtension<MaterialEntry, MaterialEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for MoveEntry
	[PXProtectedAccess(typeof(MoveEntry.LineSplittingExtension))]
	public abstract class MoveEntry_LineSplittingProjectExtension
		: AMBatchLineSplittingProjectExtension<MoveEntry, MoveEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for LaborEntry
	[PXProtectedAccess(typeof(LaborEntry.LineSplittingExtension))]
	public abstract class LaborEntry_LineSplittingProjectExtension
		: AMBatchLineSplittingProjectExtension<LaborEntry, LaborEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
