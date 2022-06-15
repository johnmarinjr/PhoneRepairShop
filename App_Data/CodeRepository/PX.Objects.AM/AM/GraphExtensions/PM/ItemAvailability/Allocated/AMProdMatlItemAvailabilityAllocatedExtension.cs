using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability.Allocated
{
	// TODO: ensure this class is even needed - could project availability be used in ProdMatl graphs?
	// if yes, then the GetStatusWithAllocatedProject's meaningful implementation is missing, otherwise this class should be removed
	public abstract class AMProdMatlItemAvailabilityAllocatedProjectExtension<TGraph, TProdMatlAvailExt, TProdMatlAvailAllocExt, TProdMatlAvailProjExt> : ItemAvailabilityAllocatedProjectExtension<TGraph, TProdMatlAvailExt, TProdMatlAvailAllocExt, TProdMatlAvailProjExt, AMProdMatl, AMProdMatlSplit>
		where TGraph : PXGraph
		where TProdMatlAvailExt : AMProdMatlItemAvailabilityExtension<TGraph>
		where TProdMatlAvailAllocExt : AMProdMatlItemAvailabilityAllocatedExtension<TGraph, TProdMatlAvailExt>
		where TProdMatlAvailProjExt : AMProdMatlItemAvailabilityProjectExtension<TGraph, TProdMatlAvailExt>
	{
		protected override string GetStatusWithAllocatedProject(AMProdMatl line) => null;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdDetail?
	public class ProdDetail_MatlItemAvailabilityAllocatedProjectExtension : AMProdMatlItemAvailabilityAllocatedProjectExtension<
		ProdDetail,
		ProdDetail.MatlItemAvailabilityExtension,
		ProdDetail.MatlItemAvailabilityAllocatedExtension,
		ProdDetail_MatlItemAvailabilityProjectExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdMaint?
	public class ProdMaint_MatlItemAvailabilityAllocatedProjectExtension : AMProdMatlItemAvailabilityAllocatedProjectExtension<
		ProdMaint,
		ProdMaint.MatlItemAvailabilityExtension,
		ProdMaint.MatlItemAvailabilityAllocatedExtension,
		ProdMaint_MatlItemAvailabilityProjectExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProductionScheduleEngine?
	public class ProductionScheduleEngine_MatlItemAvailabilityAllocatedProjectExtension : AMProdMatlItemAvailabilityAllocatedProjectExtension<
		ProductionScheduleEngine,
		ProductionScheduleEngine.MatlItemAvailabilityExtension,
		ProductionScheduleEngine.MatlItemAvailabilityAllocatedExtension,
		ProductionScheduleEngine_MatlItemAvailabilityProjectExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
