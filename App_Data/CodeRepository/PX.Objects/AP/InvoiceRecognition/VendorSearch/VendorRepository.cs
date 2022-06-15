using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.CR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal class VendorRepository : IVendorRepository
	{
		private const string _domainAtPrefix = "@";

		public Vendor GetActiveVendorByNoteId(PXGraph graph, Guid noteId)
		{
			var vendorCache = graph.Caches[typeof(VendorR)];
			if (vendorCache.LocateByNoteID(noteId) != 1)
			{
				return null;
			}

			var vendor = vendorCache.Current as Vendor;
			if (vendor == null || vendor.VStatus == VendorStatus.Hold || vendor.VStatus == VendorStatus.Inactive)
			{
				return null;
			}

			return vendor;
		}

		public bool IsExcludedDomain(string domain)
		{
			var excludedVendorDomainSlot = ExcludedVendorDomainDefinition.GetSlot();

			return excludedVendorDomainSlot?.Contains(domain) == true;
		}

		public IEnumerable<Vendor> GetVendorsByEmail(PXGraph graph, string email)
		{
			email.ThrowOnNullOrWhiteSpace(email);

			return SelectFrom<Vendor>.
				InnerJoin<Contact>.
				On<Vendor.bAccountID.IsEqual<Contact.bAccountID>>.
				Where<Contact.eMail.IsEqual<@P.AsString>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.hold>>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.inactive>>>.
				AggregateTo<GroupBy<Vendor.bAccountID>>.
				View.ReadOnly.Select(graph, email).FirstTableItems;
		}

		public (string DomainQuery, IEnumerable<Vendor> Results) GetVendorsByDomain(PXGraph graph, string domain)
		{
			domain.ThrowOnNullOrWhiteSpace(nameof(domain));

			var domainQuery = _domainAtPrefix + domain;
			var vendorByDomainBaccount = SelectFrom<Vendor>.
				InnerJoin<Contact>.
				On<Vendor.bAccountID.IsEqual<Contact.bAccountID>>.
				Where<Contact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>.
					And<Contact.eMail.EndsWith<@P.AsString>>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.hold>>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.inactive>>>.
				AggregateTo<GroupBy<Vendor.bAccountID>>.
				View.ReadOnly.Select(graph, domainQuery).FirstTableItems.ToList();

			if (vendorByDomainBaccount.Count > 0)
			{
				return (domainQuery, vendorByDomainBaccount);
			}

			var vendorByDomainPerson = SelectFrom<Vendor>.
				InnerJoin<Contact>.
				On<Vendor.bAccountID.IsEqual<Contact.bAccountID>.And<Vendor.primaryContactID.IsEqual<Contact.contactID>>>.
				Where<Contact.contactType.IsEqual<ContactTypesAttribute.person>.
					And<Contact.eMail.EndsWith<@P.AsString>>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.hold>>.
					And<Vendor.vStatus.IsNotEqual<VendorStatus.inactive>>>.
				View.ReadOnly.Select(graph, domainQuery).FirstTableItems;

			return (domainQuery, vendorByDomainPerson);
		}

		public int? GetActiveVendorIdByVendorName(PXGraph graph, string vendorName)
		{
			vendorName.ThrowOnNullOrWhiteSpace(vendorName);

			var vendorNamePrefix = RecognizedVendorMapping.GetVendorPrefixFromName(vendorName);
			var recognizedVendor = SelectFrom<RecognizedVendorMapping>.
				Where<RecognizedVendorMapping.vendorNamePrefix.IsEqual<@P.AsString>.
					And<RecognizedVendorMapping.vendorName.IsEqual<@P.AsString>>>.
				View.ReadOnly.Select(graph, vendorNamePrefix, vendorName)?.TopFirst;
			if (recognizedVendor == null)
			{
				return null;
			}

			var vendor = Vendor.PK.Find(graph, recognizedVendor.VendorID);
			if (vendor == null || vendor.VStatus == VendorStatus.Hold || vendor.VStatus == VendorStatus.Inactive)
			{
				return null;
			}

			return recognizedVendor.VendorID;
		}
	}
}
