using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal interface IVendorRepository
	{
		Vendor GetActiveVendorByNoteId(PXGraph graph, Guid noteId);
		bool IsExcludedDomain(string domain);
		IEnumerable<Vendor> GetVendorsByEmail(PXGraph graph, string email);
		(string DomainQuery, IEnumerable<Vendor> Results) GetVendorsByDomain(PXGraph graph, string domain);
		int? GetActiveVendorIdByVendorName(PXGraph graph, string vendorName);
	}
}
