using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in KitAssemblyEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(INComponentItemAvailabilityExtension))]
	public abstract class INComponentItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<KitAssemblyEntry, INComponentItemAvailabilityExtension, INComponentTran, INComponentTranSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(INComponentTran line) => null;
	}
}
