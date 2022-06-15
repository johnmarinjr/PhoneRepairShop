using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability.Allocated
{
	// TODO: ensure this class is even needed - could project availability be used in ProdItem graphs?
	// if yes, then the GetStatusWithAllocatedProject's meaningful implementation is missing, otherwise this class should be removed
	public abstract class AMProdItemAvailabilityAllocatedProjectExtension<TGraph, TProdItemAvailExt, TProdItemAvailAllocExt, TProdItemAvailProjExt> : ItemAvailabilityAllocatedProjectExtension<TGraph, TProdItemAvailExt, TProdItemAvailAllocExt, TProdItemAvailProjExt, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
		where TProdItemAvailExt : AMProdItemAvailabilityExtension<TGraph>
		where TProdItemAvailAllocExt : AMProdItemAvailabilityAllocatedExtension<TGraph, TProdItemAvailExt>
		where TProdItemAvailProjExt : AMProdItemAvailabilityProjectExtension<TGraph, TProdItemAvailExt>
	{
		protected override string GetStatusWithAllocatedProject(AMProdItem line) => null;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdDetail?
	public class ProdDetail_ItemAvailabilityAllocatedProjectExtension : AMProdItemAvailabilityAllocatedProjectExtension<
		ProdDetail,
		ProdDetail.ItemAvailabilityExtension,
		ProdDetail.ItemAvailabilityAllocatedExtension,
		ProdDetail_ItemAvailabilityProjectExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdMaint?
	public class ProdMaint_ItemAvailabilityAllocatedProjectExtension
		: AMProdItemAvailabilityAllocatedProjectExtension<
			ProdMaint,
			ProdMaint.ItemAvailabilityExtension,
			ProdMaint.ItemAvailabilityAllocatedExtension,
			ProdMaint_ItemAvailabilityProjectExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
