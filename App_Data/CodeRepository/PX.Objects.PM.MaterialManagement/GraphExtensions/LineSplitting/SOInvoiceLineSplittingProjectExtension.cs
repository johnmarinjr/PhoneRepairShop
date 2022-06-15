using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	[PXProtectedAccess(typeof(SOInvoiceLineSplittingExtension))]
	public abstract class SOInvoiceLineSplittingProjectExtension
		: LineSplittingProjectExtension<SOInvoiceEntry, SOInvoiceLineSplittingExtension, ARInvoice, ARTran, ARTranAsSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
