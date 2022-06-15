using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.CS;

namespace PX.Objects.AP.InvoiceRecognition.GraphExtensions
{
	[PXInternalUseOnly]
	public class VendorMaintExt : PXGraphExtension<VendorMaint>
	{
		public SelectFrom<RecognizedVendorMapping>.
			Where<RecognizedVendorMapping.vendorID.IsEqual<VendorR.bAccountID.FromCurrent>>.
			View RecognizedVendors;

		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.apDocumentRecognition>();
	}
}
