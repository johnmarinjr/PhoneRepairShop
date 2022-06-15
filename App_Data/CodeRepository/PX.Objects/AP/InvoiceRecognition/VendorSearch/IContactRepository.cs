using PX.Data;
using PX.Objects.CR;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal interface IContactRepository
	{
		Contact GetAccountContact(PXGraph graph, int baccountId, int defContactId);
		Contact GetPrimaryContact(PXGraph graph, int baccountId, int primaryContactID);
		List<string> GetOtherContactEmails(PXGraph graph, int baccountId, Contact accountContact, Contact primaryContact);
	}
}
