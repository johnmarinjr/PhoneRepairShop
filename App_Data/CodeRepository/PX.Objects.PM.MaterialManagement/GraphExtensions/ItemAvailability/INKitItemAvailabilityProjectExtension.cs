using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in KitAssemblyEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(INKitItemAvailabilityExtension))]
	public abstract class INKitItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<KitAssemblyEntry, INKitItemAvailabilityExtension, INKitRegister, INKitTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(INKitRegister line) => null;
	}
}
