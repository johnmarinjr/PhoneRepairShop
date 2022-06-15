using PX.Data;
using PX.Objects.PO;
using PX.Objects.PO.GraphExtensions.POReceiptEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	[PXProtectedAccess(typeof(POReceiptLineSplittingExtension))]
	public abstract class POReceiptLineSplittingProjectExtension
		: LineSplittingProjectExtension<POReceiptEntry, POReceiptLineSplittingExtension, POReceipt, POReceiptLine, POReceiptLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
