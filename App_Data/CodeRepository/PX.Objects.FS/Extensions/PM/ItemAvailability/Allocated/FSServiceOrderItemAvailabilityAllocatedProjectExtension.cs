using PX.Objects.FS;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability.Allocated
{
	// TODO: ensure this class is even needed - could project availability be used in ServiceOrderEntry?
	// if yes, then the GetStatusWithAllocatedProject's meaningful implementation is missing, otherwise this class should be removed
	public class FSServiceOrderItemAvailabilityAllocatedProjectExtension : ItemAvailabilityAllocatedProjectExtension<
		ServiceOrderEntry,
		FSServiceOrderItemAvailabilityExtension,
		FSServiceOrderItemAvailabilityAllocatedExtension,
		FSServiceOrderItemAvailabilityProjectExtension,
		FSSODet, FSSODetSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusWithAllocatedProject(FSSODet line) => null;
	}
}
