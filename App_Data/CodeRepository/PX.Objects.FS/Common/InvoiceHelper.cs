using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using PX.CS.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public class InvoiceHelper 
    {
		public static bool IsRunningServiceContractBilling(PXGraph graph)
		{
			return graph.Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.RUN_SERVICE_CONTRACT_BILLING);
		}

		public static void GetChildCustomerShippingContactAndAddress(PXGraph graph, int? serviceContractID, out Contact shippingContact, out Address shippingAddress)
		{
			shippingContact = null;
			shippingAddress = null;

			if (serviceContractID == null)
			{
				return;
			}

			FSServiceContract serviceContract = PXSelect<FSServiceContract,
					Where<FSServiceContract.serviceContractID, Equal<Required<FSServiceContract.serviceContractID>>>>.
					Select(graph, serviceContractID);

			if (serviceContract == null || serviceContract.CustomerID == serviceContract.BillCustomerID)
			{
				return;
			}

			Customer customer = PXSelect<Customer,
					Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.
					Select(graph, serviceContract.CustomerID);

			if (customer == null || customer.DefLocationID == null)
			{
				return;
			}

			Location location = PXSelect<Location,
					Where<Location.bAccountID, Equal<Required<Location.bAccountID>>,
						And<Location.locationID, Equal<Required<Location.locationID>>>>>.
					Select(graph, customer.BAccountID, customer.DefLocationID);

			if (location == null)
			{
				return;
			}

			Contact contact = null;
			Address address = null;

			if (location.DefContactID != null)
			{
				contact = PXSelect<Contact,
						Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.
						Select(graph, location.DefContactID);
			}

			if (location.DefAddressID != null)
			{
				address = PXSelect<Address,
						Where<Address.addressID, Equal<Required<Address.addressID>>>>.
						Select(graph, location.DefAddressID);
			}

			shippingContact = contact;
			shippingAddress = address;
		}

		public static IInvoiceGraph CreateInvoiceGraph(string targetScreen)
        {
            if (targetScreen == ID.Batch_PostTo.SO)
            {
                if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>())
                {
                    return PXGraph.CreateInstance<SOOrderEntry>().GetExtension<SM_SOOrderEntry>();
                }
                else
                {
                    throw new PXException(TX.Error.DISTRIBUTION_MODULE_IS_DISABLED);
                }
            }
            else if (targetScreen == ID.Batch_PostTo.SI)
            {
                if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() && PXAccess.FeatureInstalled<FeaturesSet.advancedSOInvoices>())
                {
                    return PXGraph.CreateInstance<SOInvoiceEntry>().GetExtension<SM_SOInvoiceEntry>();
                }
                else
                {
                    throw new PXException(TX.Error.ADVANCED_SO_INVOICE_IS_DISABLED);
                }
            }
            else if (targetScreen == ID.Batch_PostTo.AR)
            {
                return PXGraph.CreateInstance<ARInvoiceEntry>().GetExtension<SM_ARInvoiceEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.AP)
            {
                return PXGraph.CreateInstance<APInvoiceEntry>().GetExtension<SM_APInvoiceEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.PM)
            {
                return PXGraph.CreateInstance<RegisterEntry>().GetExtension<SM_RegisterEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.IN)
            {
                return PXGraph.CreateInstance<INIssueEntry>().GetExtension<SM_INIssueEntry>();
            }
            else
            {
                throw new PXException(TX.Error.POSTING_MODULE_IS_INVALID, targetScreen);
            }
        }

        public static bool AreAppointmentsPostedInSO(PXGraph graph, int? sOID)
        {
            if (sOID == null)
            {
                return false;
            }

            return PXSelectReadonly<FSAppointment,
                   Where<
                       FSAppointment.pendingAPARSOPost, Equal<False>,
                   And<
                       FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>>
                   .Select(graph, sOID).Count() > 0;
        }

        public static void CopyContact(IContact dest, IContact source)
        {
            CS.ContactAttribute.CopyContact(dest, source);

            //Copy fields that are missing in the previous method
            dest.Attention = source.Attention;
        }

        public static void CopyAddress(IAddress dest, IAddress source)
        {
            AddressAttribute.Copy(dest, source);

            //Copy fields that are missing in the previous method
            dest.IsValidated = source.IsValidated;
        }

		public static void CopyAddress(IAddress dest, Address source)
		{
			CopyAddress(dest, GetIAddress(source));
		}

		public static IAddress GetIAddress(Address source)
		{
			if (source == null)
			{
				return null;
			}

			var dest = new CRAddress();

			dest.BAccountID = source.BAccountID;
			dest.RevisionID = source.RevisionID;
			dest.IsDefaultAddress = false;
			dest.AddressLine1 = source.AddressLine1;
			dest.AddressLine2 = source.AddressLine2;
			dest.AddressLine3 = source.AddressLine3;
			dest.City = source.City;
			dest.CountryID = source.CountryID;
			dest.State = source.State;
			dest.PostalCode = source.PostalCode;

			dest.IsValidated = source.IsValidated;

			return dest;
		}


	}
}
