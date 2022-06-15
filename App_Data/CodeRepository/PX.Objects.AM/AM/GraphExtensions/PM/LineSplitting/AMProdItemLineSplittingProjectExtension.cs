using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for ProdItem graphs
	public abstract class AMProdItemLineSplittingProjectExtension<TGraph, TLSExt> : LineSplittingProjectExtension<TGraph, TLSExt, AMProdItem, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
		where TLSExt : AMProdItemLineSplittingExtension<TGraph>
	{
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for ProdDetail
	[PXProtectedAccess(typeof(ProdDetail.ItemLineSplittingExtension))]
	public abstract class ProdDetail_ItemLineSplittingProjectExtension
		: AMProdItemLineSplittingProjectExtension<ProdDetail, ProdDetail.ItemLineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// Added for formal backward compatibility - remove if project availability is not applicable for ProdMaint
	[PXProtectedAccess(typeof(ProdMaint.ItemLineSplittingExtension))]
	public abstract class ProdMaint_ItemLineSplittingProjectExtension
		: AMProdItemLineSplittingProjectExtension<ProdMaint, ProdMaint.ItemLineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
