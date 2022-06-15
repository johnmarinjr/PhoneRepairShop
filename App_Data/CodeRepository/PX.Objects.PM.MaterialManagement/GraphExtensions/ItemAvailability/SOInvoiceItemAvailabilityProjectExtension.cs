using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in SOInvoiceEntry? if yes, then the GetStatusProject's meaningful implementation is missing
	[PXProtectedAccess(typeof(SOInvoiceItemAvailabilityExtension))]
	public abstract class SOInvoiceItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<SOInvoiceEntry, SOInvoiceItemAvailabilityExtension, AR.ARTran, ARTranAsSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(AR.ARTran line) => null;
	}
}
