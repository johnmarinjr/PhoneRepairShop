using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in AMBatchEntryBase?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	public abstract class AMBatchItemAvailabilityProjectExtension<TBatchGraph, TBatchItemAvailExt> : ItemAvailabilityProjectExtension<TBatchGraph, TBatchItemAvailExt, AMMTran, AMMTranSplit>
		where TBatchGraph : AMBatchEntryBase
		where TBatchItemAvailExt : AMBatchItemAvailabilityExtension<TBatchGraph>
	{
		protected override string GetStatusProject(AMMTran line) => null;
	}

	// TODO: ensure this class is even needed - could project availability be used in MaterialEntry?
	[PXProtectedAccess(typeof(MaterialEntry.ItemAvailabilityExtension))]
	public abstract class MaterialEntry_ItemAvailabilityProjectExtension
		: AMBatchItemAvailabilityProjectExtension<MaterialEntry, MaterialEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in MoveEntry?
	[PXProtectedAccess(typeof(MoveEntry.ItemAvailabilityExtension))]
	public abstract class MoveEntry_ItemAvailabilityProjectExtension
		: AMBatchItemAvailabilityProjectExtension<MoveEntry, MoveEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in LaborEntry?
	[PXProtectedAccess(typeof(LaborEntry.ItemAvailabilityExtension))]
	public abstract class LaborEntry_ItemAvailabilityProjectExtension
		: AMBatchItemAvailabilityProjectExtension<LaborEntry, LaborEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
