using PX.Data;
using PX.Objects.AM;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in ProdItem graphs?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	public abstract class AMProdItemAvailabilityProjectExtension<TGraph, TProdItemAvailExt> : ItemAvailabilityProjectExtension<TGraph, TProdItemAvailExt, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
		where TProdItemAvailExt : AMProdItemAvailabilityExtension<TGraph>
	{
		protected override string GetStatusProject(AMProdItem line) => null;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdDetail?
	[PXProtectedAccess(typeof(ProdDetail.ItemAvailabilityExtension))]
	public abstract class ProdDetail_ItemAvailabilityProjectExtension
		: AMProdItemAvailabilityProjectExtension<ProdDetail, ProdDetail.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	// TODO: ensure this class is even needed - could project availability be used in ProdMaint?
	[PXProtectedAccess(typeof(ProdMaint.ItemAvailabilityExtension))]
	public abstract class ProdMaint_ItemAvailabilityProjectExtension
		: AMProdItemAvailabilityProjectExtension<ProdMaint, ProdMaint.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
