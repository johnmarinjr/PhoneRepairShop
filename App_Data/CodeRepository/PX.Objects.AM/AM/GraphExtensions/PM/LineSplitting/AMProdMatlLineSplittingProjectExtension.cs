using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for AMProdMatl graphs
	public abstract class AMProdMatlLineSplittingProjectExtension<TGraph, TLSExt> : LineSplittingProjectExtension<TGraph, TLSExt, AMProdItem, AMProdMatl, AMProdMatlSplit>
		where TGraph : PXGraph
		where TLSExt : AMProdMatlLineSplittingExtension<TGraph>
	{
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for ProdDetail
	[PXProtectedAccess(typeof(ProdDetail.MatlLineSplittingExtension))]
	public abstract class ProdDetail_MatlLineSplittingProjectExtension
		: AMProdMatlLineSplittingProjectExtension<ProdDetail, ProdDetail.MatlLineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for ProdMaint
	[PXProtectedAccess(typeof(ProdMaint.MatlLineSplittingExtension))]
	public abstract class ProdMaint_MatlLineSplittingProjectExtension
		: AMProdMatlLineSplittingProjectExtension<ProdMaint, ProdMaint.MatlLineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for ProductionScheduleEngine
	[PXProtectedAccess(typeof(ProductionScheduleEngine.MatlLineSplittingExtension))]
	public abstract class ProductionScheduleEngine_MatlLineSplittingProjectExtension
		: AMProdMatlLineSplittingProjectExtension<ProductionScheduleEngine, ProductionScheduleEngine.MatlLineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
