using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in ProdMatl graphs?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	public abstract class AMProdMatlItemAvailabilityProjectExtension<TGraph, TProdMatlItemAvailExt> : ItemAvailabilityProjectExtension<TGraph, TProdMatlItemAvailExt, AMProdMatl, AMProdMatlSplit>
		where TGraph : PXGraph
		where TProdMatlItemAvailExt : AMProdMatlItemAvailabilityExtension<TGraph>
	{
		protected override string GetStatusProject(AMProdMatl line) => null;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdDetail?
	[PXProtectedAccess(typeof(ProdDetail.MatlItemAvailabilityExtension))]
	public abstract class ProdDetail_MatlItemAvailabilityProjectExtension
		: AMProdMatlItemAvailabilityProjectExtension<ProdDetail, ProdDetail.MatlItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdMaint?
	[PXProtectedAccess(typeof(ProdMaint.MatlItemAvailabilityExtension))]
	public abstract class ProdMaint_MatlItemAvailabilityProjectExtension
		: AMProdMatlItemAvailabilityProjectExtension<ProdMaint, ProdMaint.MatlItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProductionScheduleEngine?
	[PXProtectedAccess(typeof(ProductionScheduleEngine.MatlItemAvailabilityExtension))]
	public abstract class ProductionScheduleEngine_MatlItemAvailabilityProjectExtension
		: AMProdMatlItemAvailabilityProjectExtension<ProductionScheduleEngine, ProductionScheduleEngine.MatlItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
