using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public class APInvoiceEntryVisibilityRestriction : PXGraphExtension<APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APInvoice.branchID))]
		protected virtual void APInvoice_VendorID_CacheAttached(PXCache sender) { }	
	}
}
