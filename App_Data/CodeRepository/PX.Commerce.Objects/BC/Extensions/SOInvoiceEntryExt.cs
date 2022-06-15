using PX.Commerce.Core;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{
	public class BCSOInvoiceEntryExt : PXGraphExtension<SOInvoiceEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		protected virtual void _(PX.Data.Events.RowInserted<ARTran> e)
		{
			ARTran row = e.Row;
			if (row == null)
				return;

			var soLine = SOLine.PK.Find(Base, row.SOOrderType, row.SOOrderNbr, row.SOOrderLineNbr);
			row.GetExtension<BCARTranExt>().AssociatedOrderLineNbr = soLine?.GetExtension<BCSOLineExt>()?.AssociatedOrderLineNbr;
			row.GetExtension<BCARTranExt>().GiftMessage = soLine?.GetExtension<BCSOLineExt>()?.GiftMessage;
		}
		public delegate  void InvoiceOrderDelegate(InvoiceOrderArgs args);

		[PXOverride]
		public virtual void InvoiceOrder(InvoiceOrderArgs args, InvoiceOrderDelegate handler)
		{
			SOAddress soBillAddress = args.SoBillAddress;
			var pseudonymizationStatus = soBillAddress?.GetExtension<PX.Objects.GDPR.SOAddressExt>().PseudonymizationStatus;
			if (pseudonymizationStatus == PXPseudonymizationStatusListAttribute.Pseudonymized || pseudonymizationStatus == PXPseudonymizationStatusListAttribute.Erased)
				throw new PXException(BCMessages.CannotCreateInvoice);

			handler(args);
		}

		public delegate void PersistDelegate();
		[PXOverride]
		public void Persist(PersistDelegate handler)
		{
			foreach (ARInvoice doc in Base.Document.Cache.Inserted
				   .Concat_(Base.Document.Cache.Updated)
				   .Cast<ARInvoice>())
			{
				SOInvoice soInvoice = Base.SODocument.Select(doc.DocType, doc.RefNbr);
				SOOrderType orderType = null;
				if (soInvoice?.SOOrderType != null)
				{
					orderType = Base.soordertype.Select(soInvoice?.SOOrderType);
				}
				else
				{
					// for multiple order just get first and decide
					var type = Base.AllTransactions.Select(doc.DocType, doc.RefNbr).RowCast<ARTran>().ToList().Select(x => x.SOOrderType).FirstOrDefault();
					orderType = Base.soordertype.Select(type);
				}

				ARShippingContact shippingContact = Base.Shipping_Contact.Select(doc.ShipContactID);
				var overriden = shippingContact?.OverrideContact;
				shippingContact.IsEncrypted = SetEncryptionValue(overriden, orderType);
				Base.Shipping_Contact.Cache.Update(shippingContact);

				ARContact contact = Base.Billing_Contact.Select(doc.BillContactID);
				overriden = contact?.OverrideContact;
				contact.IsEncrypted = SetEncryptionValue(overriden, orderType);
				Base.Billing_Contact.Cache.Update(contact);

				ARShippingAddress shippingAddress = Base.Shipping_Address.Select(doc.ShipAddressID);
				overriden = shippingAddress?.OverrideAddress;
				shippingAddress.IsEncrypted = SetEncryptionValue(overriden, orderType);
				Base.Shipping_Address.Cache.Update(shippingAddress);

				ARAddress address = Base.Billing_Address.Select(doc.BillAddressID);
				overriden = address?.OverrideAddress;
				address.IsEncrypted = SetEncryptionValue(overriden, orderType);
				Base.Billing_Address.Cache.Update(address);
			}

			handler();

		}
		
		protected virtual void ARAddress_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARAddress row = (ARAddress)e.Row;
			if (row == null)
				return;
			if (Base.Document.Current?.Released == true && row.AddressID == Base.Document.Current?.BillAddressID)
			{
				if (row.IsEncrypted == true)
					PXUIFieldAttribute.SetEnabled<ARAddress.overrideAddress>(cache, row, false);
			}

		}

		protected virtual void ARContact_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARContact row = (ARContact)e.Row;
			if (row == null)
				return;
			if (Base.Document.Current?.Released == true && row.ContactID == Base.Document.Current?.BillContactID)
			{
				if (row.IsEncrypted == true)
					PXUIFieldAttribute.SetEnabled<ARContact.overrideContact>(cache, row, false);
			}

		}
		private bool SetEncryptionValue(bool? overriden, SOOrderType orderType)
		{
			if (overriden == null) return false;
			return overriden.Value && (orderType?.GetExtension<SOOrderTypeExt>()?.EncryptAndPseudonymizePII ?? false);

		}

	}
}
