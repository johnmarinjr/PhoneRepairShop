using PX.Data;
using PX.Objects.FS;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in ServiceOrderEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(FSServiceOrderItemAvailabilityExtension))]
	public abstract class FSServiceOrderItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<ServiceOrderEntry, FSServiceOrderItemAvailabilityExtension, FSSODet, FSSODetSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(FSSODet line) => null;
	}
}
