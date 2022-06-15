using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;

namespace PX.Objects.AP.InvoiceRecognition
{
	[PXInternalUseOnly]
	public class ExcludedVendorDomainMaint : PXGraph<ExcludedVendorDomainMaint>
	{
		public SelectFrom<ExcludedVendorDomain>.View Domains;

		public PXSave<ExcludedVendorDomain> Save;
		public PXCancel<ExcludedVendorDomain> Cancel;
	}
}
