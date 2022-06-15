using PX.CloudServices.DocumentRecognition;
using PX.Data;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal interface IVendorSearchService
	{
		VendorSearchResult FindVendor(PXGraph graph, string vendorName, IList<FullTextTerm> fullTextTerms, string email);
	}
}
