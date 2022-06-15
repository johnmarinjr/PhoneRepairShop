using PX.Data;
using PX.Objects.AR;

namespace PX.Objects.SO.GraphExtensions.ARPaymentEntryExt
{
	public class SortExtensions : SortExtensionsBy<ExtensionOrderFor<ARPaymentEntry>
		.FilledWith<
			AffectedSOOrdersWithCreditLimitExt,
			AffectedSOOrdersWithPaymentInPendingProcessingExt,
			AffectedSOOrdersWithPrepaymentRequirementsExt>>
	{
	}
}
