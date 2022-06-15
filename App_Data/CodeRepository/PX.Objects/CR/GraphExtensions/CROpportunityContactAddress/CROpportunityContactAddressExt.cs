using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.CS.Contracts.Interfaces;

namespace PX.Objects.CR.Extensions.CROpportunityContactAddress
{
	[PXInternalUseOnly]
	public abstract class CROpportunityContactAddressExt<TGraph> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		#region Document Mapping

		protected class DocumentMapping : IBqlMapping
		{
			public Type Extension => typeof(Document);
			protected Type _table;
			public Type Table => _table;

			public DocumentMapping(Type table)
			{
				_table = table;
			}

			public Type ContactID = typeof(Document.contactID);
			public Type DocumentContactID = typeof(Document.documentContactID);
			public Type DocumentAddressID = typeof(Document.documentAddressID);
			public Type ShipContactID = typeof(Document.shipContactID);
			public Type ShipAddressID = typeof(Document.shipAddressID);
			public Type BillContactID = typeof(Document.billContactID);
			public Type BillAddressID = typeof(Document.billAddressID);
			public Type LocationID = typeof(Document.locationID);
			public Type BAccountID = typeof(Document.bAccountID);
			public Type AllowOverrideContactAddress = typeof(Document.allowOverrideContactAddress);
			public Type AllowOverrideShippingContactAddress = typeof(Document.allowOverrideShippingContactAddress);
			public Type AllowOverrideBillingContactAddress = typeof(Document.allowOverrideBillingContactAddress);
		}

		protected abstract DocumentMapping GetDocumentMapping();

		#endregion

		#region Document Contact Mapping

		protected class DocumentContactMapping : IBqlMapping
		{
			public Type Extension => typeof(DocumentContact);
			protected Type _table;
			public Type Table => _table;

			public DocumentContactMapping(Type table)
			{
				_table = table;
			}

			public Type FullName = typeof(DocumentContact.fullName);
			public Type Title = typeof(DocumentContact.title);
			public Type FirstName = typeof(DocumentContact.firstName);
			public Type LastName = typeof(DocumentContact.lastName);
			public Type Salutation = typeof(DocumentContact.salutation);
			public Type Attention = typeof(DocumentContact.attention);
			public Type EMail = typeof(DocumentContact.email);
			public Type Phone1 = typeof(DocumentContact.phone1);
			public Type Phone1Type = typeof(DocumentContact.phone1Type);
			public Type Phone2 = typeof(DocumentContact.phone2);
			public Type Phone2Type = typeof(DocumentContact.phone2Type);
			public Type Phone3 = typeof(DocumentContact.phone3);
			public Type Phone3Type = typeof(DocumentContact.phone3Type);
			public Type Fax = typeof(DocumentContact.fax);
			public Type FaxType = typeof(DocumentContact.faxType);
			public Type IsDefaultContact = typeof(DocumentContact.isDefaultContact);
			public Type OverrideContact = typeof(DocumentContact.overrideContact);
		}

		protected abstract DocumentContactMapping GetDocumentContactMapping();

		#endregion

		#region Document Address Mapping

		protected class DocumentAddressMapping : IBqlMapping
		{
			public Type Extension => typeof(DocumentAddress);
			protected Type _table;
			public Type Table => _table;

			public DocumentAddressMapping(Type table)
			{
				_table = table;
			}

			public Type IsDefaultAddress = typeof(DocumentAddress.isDefaultAddress);
			public Type OverrideAddress = typeof(DocumentAddress.overrideAddress);
			public Type AddressLine1 = typeof(DocumentAddress.addressLine1);
			public Type AddressLine2 = typeof(DocumentAddress.addressLine2);
			public Type AddressLine3 = typeof(DocumentAddress.addressLine3);
			public Type City = typeof(DocumentAddress.city);
			public Type CountryID = typeof(DocumentAddress.countryID);
			public Type State = typeof(DocumentAddress.state);
			public Type PostalCode = typeof(DocumentAddress.postalCode);
			public Type IsValidated = typeof(DocumentAddress.isValidated);
		}

		protected abstract DocumentAddressMapping GetDocumentAddressMapping();

		#endregion

		public PXSelectExtension<Document> Documents;
		public PXSelectExtension<DocumentContact> Contacts;
		public PXSelectExtension<DocumentAddress> Addresses;
		
		[PXOverride]
		public virtual void Persist(Action del)
		{
			PreventSameItemsDeletion();
			del();
		}

		protected virtual void PreventSameItemsDeletion()
		{
			var deletedItemKeys = new HashSet<int>();
			foreach (var cache in new[]
				{
					typeof(CRContact),
					typeof(CRBillingContact),
					typeof(CRShippingContact)
				}
				.Select(type => Base.Caches[type]))
			{
				foreach (var item in cache.Deleted.OfType<CRContact>())
				{
					if (item.ContactID is int id && id > 0 && !deletedItemKeys.Add(id))
					{
						cache.Remove(item);
					}
				}
			}

			deletedItemKeys.Clear();
			foreach (var cache in new[]
				{
					typeof(CRAddress),
					typeof(CRBillingAddress),
					typeof(CRShippingAddress)
				}
				.Select(type => Base.Caches[type]))
			{
				foreach (var item in cache.Deleted.OfType<CRAddress>())
				{
					if (item.AddressID is int id && id > 0 && !deletedItemKeys.Add(id))
					{
						cache.Remove(item);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.contactID> e)
		{
			var row = e.Row as Document;
			if (row == null || object.Equals(e.OldValue, e.NewValue)) return;

			Contact oldContact = null;
			Address oldAddress = null;

			if (e.OldValue != null)
			{
				oldContact = PXSelect<
							Contact,
						Where<
							Contact.contactID, Equal<Required<Document.contactID>>>>
					.Select(Base, (int?)e.OldValue);

				oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>>,
						Where<
							Contact.contactID, Equal<Required<Document.contactID>>>>
					.Select(Base, (int?)e.OldValue);
			}
			else if (row.LocationID != null)
			{
				oldContact = PXSelectJoin<
							Contact,
						LeftJoin<Location,
							On<Location.locationID, Equal<Current<Document.locationID>>>>,
						Where<
							Contact.contactID, Equal<Location.defContactID>>>
					.Select(Base);

				oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>,
						LeftJoin<Location,
							On<Location.locationID, Equal<Current<Document.locationID>>>>>,
						Where<
							Address.addressID, Equal<Location.defAddressID>>>
					.Select(Base);
			}

			DefaultRecords(row,
				changedForContactInfo: ChangedData.ShouldReplace(oldContact, oldAddress),
				changedForShippingInfo: ChangedData.ShouldNotReplace,
				changedForBillingInfo: ChangedData.ShouldNotReplace);
		}

		protected virtual void _(Events.RowInserted<Document> e)
		{
			var row = e.Row as Document;
			if (row == null) return;


			using (new ReadOnlyScope(
				new[] {
					GetContactCache(),
					GetAddressCache(),
					GetBillingContactCache(),
					GetBillingAddressCache(),
					GetShippingContactCache(),
					GetShippingAddressCache()
				}.Where(cache => cache != null)
				.ToArray()
			))
			{
				Contact oldContact = null;
				Address oldAddress = null;

				if (row.ContactID != null)
				{
					oldContact = PXSelect<
							Contact,
						Where<
							Contact.contactID, Equal<Current<Document.contactID>>>>
						.Select(Base);

					oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>>,
						Where<
							Contact.contactID, Equal<Current<Document.contactID>>>>
						.Select(Base);
				}
				else if (row.LocationID != null)
				{
					oldContact = PXSelectJoin<
							Contact,
						LeftJoin<Location,
							On<Location.locationID, Equal<Current<Document.locationID>>>>,
						Where<
							Contact.contactID, Equal<Location.defContactID>>>
						.Select(Base);

					oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>,
						LeftJoin<Location,
							On<Location.locationID, Equal<Current<Document.locationID>>>>>,
						Where<
							Address.addressID, Equal<Location.defAddressID>>>
						.Select(Base);
				}

				var shouldPlace = ChangedData.ShouldReplace(oldContact, oldAddress);
				var shouldPlaceForShipping = ChangedData.ShouldReplace(oldContact, oldAddress);

				DefaultRecords(row,
					changedForContactInfo:  shouldPlace,
					changedForShippingInfo: shouldPlaceForShipping,
					changedForBillingInfo: ChangedData.ShouldNotReplace);
			}
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.locationID> e)
		{
			var row = e.Row as Document;
			if (row == null || object.Equals(e.OldValue, e.NewValue)) return;

			Contact oldContact = null;
			Address oldAddress = null;

			int? oldLocationID = (int?)e.OldValue;

			if (oldLocationID != null)
			{
				oldContact = PXSelectJoin<
						Contact,
					LeftJoin<Location,
						On<Location.locationID, Equal<Required<Document.locationID>>>>,
					Where<
						Contact.contactID, Equal<Location.defContactID>>>
					.Select(Base, oldLocationID);

				oldAddress = PXSelectJoin<
						Address,
					LeftJoin<Contact,
						On<Contact.defAddressID, Equal<Address.addressID>>,
					LeftJoin<Location,
						On<Location.locationID, Equal<Required<Document.locationID>>>>>,
					Where<
						Address.addressID, Equal<Location.defAddressID>>>
					.Select(Base, oldLocationID);
			}

			Location oldLocation = PXSelect<
					Location,
				Where<
					Location.locationID, Equal<Required<Location.locationID>>>>
				.Select(Base, oldLocationID);

			var changedData = ChangedData.ShouldReplace(oldContact, oldAddress);
			bool baccountChanged = row.BAccountID != oldLocation?.BAccountID;
			var baccountChangedData = baccountChanged ? changedData : ChangedData.ShouldNotReplace;

			if (row.LocationID != null)
				DefaultRecords(row,
					changedForContactInfo: baccountChangedData,
					changedForBillingInfo: baccountChangedData,
					changedForShippingInfo: changedData);
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.bAccountID> e)
		{
			var row = e.Row as Document;
			if (row == null || object.Equals(e.OldValue, e.NewValue)) return;

			Contact oldContact = null;
			Address oldAddress = null;
			if (row.BAccountID != null)
			{
				int? oldBAccountID = (int?)e.OldValue;
				int? oldLocationID = null;

				var baccount = (BAccount)PXSelect<
						BAccount,
					Where<
						BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>
					.Select(Base, oldBAccountID);

				if (baccount != null)
				{
					Location oldLocation = PXSelect<
							Location,
						Where<
							Location.locationID, Equal<Required<Location.locationID>>>>
						.Select(Base, baccount.DefLocationID);

					if (oldLocation != null)
					{
						oldLocationID = oldLocation.LocationID;
					}
				}

				if (row.ContactID != null)
				{
					oldContact = PXSelect<
							Contact,
						Where<
							Contact.contactID, Equal<Current<Document.contactID>>>>
						.Select(Base);

					oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>>,
						Where<
							Contact.contactID, Equal<Current<Document.contactID>>>>
						.Select(Base);
				}

				if (oldLocationID != null)
				{
					oldContact = PXSelectJoin<
							Contact,
						LeftJoin<Location,
							On<Location.locationID, Equal<Required<Document.locationID>>>>,
						Where<
							Contact.contactID, Equal<Location.defContactID>>>
						.Select(Base, oldLocationID);

					oldAddress = PXSelectJoin<
							Address,
						LeftJoin<Contact,
							On<Contact.defAddressID, Equal<Address.addressID>>,
						LeftJoin<Location,
							On<Location.locationID, Equal<Required<Document.locationID>>>>>,
						Where<
							Address.addressID, Equal<Location.defAddressID>>>
						.Select(Base, oldLocationID);
				}
			}

			var changedData = ChangedData.ShouldReplace(oldContact, oldAddress);
			DefaultRecords(row, changedData, changedData, changedData);
		}

		protected virtual void _(Events.FieldDefaulting<Document, Document.allowOverrideContactAddress> e)
		{
			var row = e.Row as Document;
			if (row == null) return;

			if (Contacts.Current != null)
				Contacts.Cache.SetValue<DocumentContact.overrideContact>(Contacts.Current, row.AllowOverrideContactAddress);

			if (Addresses.Current != null)
				Addresses.Cache.SetValue<DocumentAddress.overrideAddress>(Addresses.Current, row.AllowOverrideContactAddress);
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.allowOverrideContactAddress> e)
		{
			var row = e.Row as Document;
			if (row == null) return;

			DocumentAddress address = Addresses.SelectSingle();
			DocumentContact contact = Contacts.SelectSingle();


			if (contact != null)
			{
				Contacts.Cache.SetValue<DocumentContact.overrideContact>(contact, row.AllowOverrideContactAddress);
				PXCache cache = GetContactCache();
				if (cache != null)
				{
					IPersonalContact crContact = GetCurrentContact();

					if (crContact != null)
					{
						cache.SetValue<CRContact.isDefaultContact>(crContact, row.AllowOverrideContactAddress != true);
					}
				}
			}

			if (address != null)
			{
				Addresses.Cache.SetValue<DocumentAddress.overrideAddress>(address, row.AllowOverrideContactAddress);
				PXCache cache = GetAddressCache();
				if (cache != null)
				{
					IAddress crAddress = GetCurrentAddress();

					if (crAddress != null)
					{
						cache.SetValue<CRAddress.isDefaultAddress>(crAddress, row.AllowOverrideContactAddress != true);
					}
				}
			}

			Addresses.Cache.Update(address);
			Contacts.Cache.Update(contact);
		}

		protected virtual void _(Events.FieldSelecting<Document, Document.allowOverrideContactAddress> e)
		{
			var row = e.Row as Document;
			if (row == null) return;

			if (row.BAccountID == null && row.LocationID == null && row.ContactID == null)
				e.ReturnValue = false;
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.allowOverrideShippingContactAddress> e)
		{
			if(e.Row?.AllowOverrideShippingContactAddress is bool allowOverride)
				AllowOverrides_Updated(GetShippingContactCache(), GetShippingAddressCache(), allowOverride);
		}

		protected virtual void _(Events.FieldUpdated<Document, Document.allowOverrideBillingContactAddress> e)
		{
			if (e.Row?.AllowOverrideBillingContactAddress is bool allowOverride)
				AllowOverrides_Updated(GetBillingContactCache(), GetBillingAddressCache(), allowOverride);
		}

		private void AllowOverrides_Updated(PXCache contactCache, PXCache addressCache, bool allowOverrideValue)
		{
			RefreshCurrents();


			var contact = contactCache?.Current as IPersonalContact;
			if (contact != null)
			{
				contactCache.SetValueExt<CRContact.overrideContact>(contact, allowOverrideValue);
			}

			var address = addressCache?.Current as IAddress;
			if (address != null)
			{
				addressCache.SetValueExt<CRAddress.overrideAddress>(address, allowOverrideValue);
			}
		}

		protected virtual void _(Events.RowUpdated<DocumentAddress> e)
		{
			DocumentAddress row = e.Row as DocumentAddress;
			if (row == null) return;
			Document doc = Documents.Cache.Current as Document;
			if (doc == null) return;

			if (doc.BAccountID == null && doc.ContactID == null)
			{
				row.IsValidated = false;
			}
		}

		private bool LocationOrContactIsNotNull(Document row)
		{
			return (row.LocationID != null || row.ContactID != null);
		}

		protected virtual void DefaultRecords(Document row, ChangedData changedForContactInfo, ChangedData changedForBillingInfo, ChangedData changedForShippingInfo)
		{
			PXCache cache = Documents.Cache;

			RefreshCurrents();

			bool needAskFromContactAddress = AskForConfirmationForContactAddress(row, changedForContactInfo);
			bool needAskFromBillingContact = AskForConfirmationForBillingContact(row, changedForBillingInfo);
			bool needAskFromBillingAddress = AskForConfirmationForBillingAddress(row, changedForBillingInfo);
			bool needAskFromBillingContactAddress = needAskFromBillingContact || needAskFromBillingAddress;
			bool needAskFromShippingContact = AskForConfirmationForShippingContact(row, changedForShippingInfo);
			bool needAskFromShippingAddress = AskForConfirmationForShippingAddress(row, changedForShippingInfo);
			bool needAskFromShippingContactAddress = needAskFromShippingContact || needAskFromShippingAddress;
			bool documentIsNotNew = cache.GetStatus(row) != PXEntryStatus.Inserted;

			if (LocationOrContactIsNotNull(row))
			{
				if (documentIsNotNew && (needAskFromContactAddress || needAskFromBillingContactAddress || needAskFromShippingContactAddress))
				{
					string message = GetMessageForDefaultRecords(needAskFromContactAddress, needAskFromBillingContactAddress, needAskFromShippingContactAddress);

					WebDialogResult dialogResult = this.Documents.View.Ask_YesNoCancel_WithCallback((object)null, CR.Messages.Warning, message, MessageIcon.Warning);

					if (dialogResult == WebDialogResult.Cancel)
					{
						UpdateBAccountIDAndContactID(cache, row);
					}
					else
					{
						RefreshCurrents();

						bool toReset = dialogResult == WebDialogResult.Yes;

						if (row.AllowOverrideContactAddress == false
							|| toReset && needAskFromContactAddress)
						{
							ResetContactAndAddress(cache, row);
						}

						if (toReset && needAskFromShippingContact
							|| GetCurrentShippingContact()?.IsDefaultContact is true)
						{
							ResetShippingContact(cache, row);
						}

						if (toReset && needAskFromShippingAddress
							|| GetCurrentShippingAddress()?.IsDefaultAddress is true)
						{
							ResetShippingAddress(cache, row);
						}

						if (toReset && needAskFromBillingContact
							|| GetCurrentBillingContact()?.IsDefaultContact is true)
						{
							ResetBillingContact(cache, row);
						}

						if (toReset && needAskFromBillingAddress
							|| GetCurrentBillingAddress()?.IsDefaultAddress is true)
						{
							ResetBillingAddress(cache, row);
						}
					}
				}
				else
				{
					if (changedForContactInfo.CanBeReplaced)
					{
						ResetContactAndAddress(cache, row);
					}

					if (changedForBillingInfo.CanBeReplaced)
					{
						ResetBillingContact(cache, row);
						ResetBillingAddress(cache, row);
					}

					if (changedForShippingInfo.CanBeReplaced)
					{
						ResetShippingContact(cache, row);
						ResetShippingAddress(cache, row);
					}
				}
			}
			else if (row.LocationID == null && row.ContactID == null && row.BAccountID == null)
			{
				RefreshCurrents();

				if (row.AllowOverrideContactAddress == false)
				{
					ResetContactAndAddress(cache, row);
				}

				var billingContact = GetCurrentBillingContact();
				if (billingContact?.IsDefaultContact is true)
				{
					ResetBillingContact(cache, row);
					billingContact = GetCurrentBillingContact(true);
					GetBillingContactCache().RaiseRowUpdated(billingContact, billingContact);
				}

				var billingAddress = GetCurrentBillingAddress();
				if (billingAddress?.IsDefaultAddress is true)
				{
					ResetBillingAddress(cache, row);
					billingAddress = GetCurrentBillingAddress(true);
					GetBillingAddressCache().RaiseRowUpdated(billingAddress, billingAddress);
				}

				var shippingContact = GetCurrentShippingContact();
				if (shippingContact?.IsDefaultContact is true)
				{
					ResetShippingContact(cache, row);
					shippingContact = GetCurrentShippingContact();
					GetShippingContactCache().RaiseRowUpdated(shippingContact, shippingContact);
				}

				var shippingAddress = GetCurrentShippingAddress();
				if (shippingAddress?.IsDefaultAddress is true)
				{
					ResetShippingAddress(cache, row);
					shippingAddress = GetCurrentShippingAddress();
					GetShippingAddressCache().RaiseRowUpdated(shippingAddress, shippingAddress);
				}
			}

			if (IsDefaultContactAddress())
			{
				cache.SetValue<Document.allowOverrideContactAddress>(row, true);
			}
		}


		protected virtual string GetMessageForDefaultRecords(
			bool needAskFromContactAddress,
			bool needAskFromBillingContactAddress,
			bool needAskFromShippingContactAddress)
		{
			if (needAskFromContactAddress)
				if (needAskFromShippingContactAddress)
					if (needAskFromBillingContactAddress)
						return CR.Messages.ReplaceContactDetailsAndBillingAndShippingInfo;
					else
						return CR.Messages.ReplaceContactDetailsAndShippingInfo;
				else
					return CR.Messages.ReplaceContactDetails;
			else if (needAskFromShippingContactAddress)
					if (needAskFromBillingContactAddress)
						return CR.Messages.ReplaceBillingAndShippingInfo;
					else
						return CR.Messages.ReplaceShippingInfo;
			else
				return CR.Messages.ReplaceBillingInfo;
		}

		private void UpdateBAccountIDAndContactID(PXCache cache, Document row)
		{
			cache.SetValue<Document.bAccountID>(row, cache.GetValueOriginal<Document.bAccountID>(cache.GetMain(row)));
			cache.SetValue<Document.contactID>(row, cache.GetValueOriginal<Document.contactID>(cache.GetMain(row)));
		}

		private void ResetContactAndAddress(PXCache cache, Document row)
		{
			SharedRecordAttribute.DefaultRecord<Document.documentAddressID>(cache, cache.GetMain(row));
			SharedRecordAttribute.DefaultRecord<Document.documentContactID>(cache, cache.GetMain(row));
			cache.SetValue<Document.allowOverrideContactAddress>(row, false);
		}

		private void ResetShippingContact(PXCache cache, Document row)
		{
			var contactCache = GetShippingContactCache();
			if (contactCache == null)
				return;
			SharedRecordAttribute.DefaultRecord<Document.shipContactID>(cache, cache.GetMain(row));
			var shippingContact = GetCurrentShippingContact();
			contactCache.SetValue(shippingContact, nameof(IContact.IsDefaultContact), true);
		}

		private void ResetShippingAddress(PXCache cache, Document row)
		{
			var addressCache = GetShippingAddressCache();
			if (addressCache == null)
				return;
			SharedRecordAttribute.DefaultRecord<Document.shipAddressID>(cache, cache.GetMain(row));
			var shippingAddress = GetCurrentShippingAddress();
			addressCache.SetValue(shippingAddress, nameof(IAddress.IsDefaultAddress), true);
		}

		private void ResetBillingContact(PXCache cache, Document row)
		{
			var contactCache = GetBillingContactCache();
			if (contactCache == null)
				return;
			SharedRecordAttribute.DefaultRecord<Document.billContactID>(cache, cache.GetMain(row));
			var billingContact = GetCurrentBillingContact();
			contactCache.SetValue(billingContact, nameof(IContact.IsDefaultContact), true);
		}

		private void ResetBillingAddress(PXCache cache, Document row)
		{
			var addressCache = GetShippingAddressCache();
			if (addressCache == null)
				return;
			SharedRecordAttribute.DefaultRecord<Document.billAddressID>(cache, cache.GetMain(row));
			var billingAddress = GetCurrentBillingAddress();
			addressCache.SetValue(billingAddress, nameof(IAddress.IsDefaultAddress), true);
		}

		protected virtual bool AskForConfirmationForContactAddress(Document row, ChangedData data)
		{
			var contact = GetCurrentContact();
			var address = GetCurrentAddress();
			return row.AllowOverrideContactAddress == true
				&& LocationOrContactIsNotNull(row)
				&& (data.WasFullContactChanged(contact)
					|| data.WasAddressChanged(address))
				&& !IsDefaultContactAddress();
		}

		protected virtual bool AskForConfirmationForBillingContact(Document row, ChangedData data)
		{
			var contact = GetCurrentBillingContact();
			return contact != null
				&& contact.IsDefaultContact == false
				&& !data.SkipAskContact()
				&& !IsDefaultBillingContact();
		}

		protected virtual bool AskForConfirmationForBillingAddress(Document row, ChangedData data)
		{
			var address = GetCurrentBillingAddress();
			return address != null
				&& address.IsDefaultAddress == false
				&& !data.SkipAskAddress()
				&& !IsDefaultBillingAddress();
		}

		protected virtual bool AskForConfirmationForShippingContact(Document row, ChangedData data)
		{
			var contact = GetCurrentShippingContact();
			return contact != null
				&& contact.IsDefaultContact == false
				&& !data.SkipAskContact()
				&& !IsDefaultShippingContact();
		}

		protected virtual bool AskForConfirmationForShippingAddress(Document row, ChangedData data)
		{
			var address = GetCurrentShippingAddress();
			return address != null
				&& address.IsDefaultAddress == false
				&& !data.SkipAskAddress()
				&& !IsDefaultShippingAddress();
		}

		private void RefreshCurrents()
		{
			GetCurrentContact(true);
			GetCurrentShippingContact(true);
			GetCurrentBillingContact(true);
			GetCurrentAddress(true);
			GetCurrentShippingAddress(true);
			GetCurrentBillingAddress(true);
		}

		protected virtual IPersonalContact GetCurrentContact(bool forceSelect = false)
		{
			var cache = GetContactCache();

			if (!forceSelect && cache.Current is IPersonalContact contact)
				return contact;

			return (cache.Current = SelectContact()) as IPersonalContact
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentContactIsNull);
		}

		protected virtual IPersonalContact GetCurrentShippingContact(bool forceSelect = false)
		{
			var cache = GetShippingContactCache();

			if (cache == null)
				return null;

			if (!forceSelect && cache.Current is IPersonalContact contact)
				return contact;

			return (cache.Current = SelectShippingContact()) as IPersonalContact
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentShipToContactIsNull);
		}

		protected virtual IPersonalContact GetCurrentBillingContact(bool forceSelect = false)
		{
			var cache = GetBillingContactCache();

			if (cache == null)
				return null;

			if (!forceSelect && cache.Current is IPersonalContact contact)
				return contact;

			return (cache.Current = SelectBillingContact()) as IPersonalContact
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentBillToContactIsNull);
		}


		protected virtual IAddress GetCurrentAddress(bool forceSelect = false)
		{
			var cache = GetAddressCache();

			if (!forceSelect && cache.Current is IAddress address)
				return address;

			return (cache.Current = SelectAddress()) as IAddress
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentAddressIsNull);
		}

		protected virtual IAddress GetCurrentShippingAddress(bool forceSelect = false)
		{
			var cache = GetShippingAddressCache();

			if (cache == null)
				return null;

			if (!forceSelect && cache.Current is IAddress address)
				return address;

			return (cache.Current = SelectShippingAddress()) as IAddress
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentShipToAddressIsNull);
		}

		protected virtual IAddress GetCurrentBillingAddress(bool forceSelect = false)
		{
			var cache = GetBillingAddressCache();

			if (cache == null)
				return null;

			if (!forceSelect && cache.Current is IAddress address)
				return address;

			return (cache.Current = SelectBillingAddress()) as IAddress
				?? throw new PXInvalidOperationException(MessagesNoPrefix.CurrentBillToAddressIsNull);
		}

		protected abstract IPersonalContact SelectContact();
		protected virtual IPersonalContact SelectBillingContact() => null;
		protected virtual IPersonalContact SelectShippingContact() => null;
		protected abstract IPersonalContact GetEtalonContact();
		protected virtual IPersonalContact GetEtalonBillingContact() => null;
		protected virtual IPersonalContact GetEtalonShippingContact() => null;
		protected abstract IAddress SelectAddress();
		protected virtual IAddress SelectBillingAddress() => null;
		protected virtual IAddress SelectShippingAddress() => null;
		protected abstract IAddress GetEtalonAddress();
		protected virtual IAddress GetEtalonBillingAddress() => null;
		protected virtual IAddress GetEtalonShippingAddress() => null;
		protected abstract PXCache GetContactCache();
		protected abstract PXCache GetAddressCache();
		protected virtual PXCache GetBillingContactCache() => null;
		protected virtual PXCache GetBillingAddressCache() => null;
		protected virtual PXCache GetShippingContactCache() => null;
		protected virtual PXCache GetShippingAddressCache() => null;

		protected virtual bool IsDefaultContactAddress()
		{
			var currentContact = GetCurrentContact(true);
			var currentAddress = GetCurrentAddress(true);

			if (currentContact != null && currentAddress != null)
			{
				return AreFullContactsEquivalent(currentContact, GetEtalonContact())
					&& AreAddressesEquivalent(currentAddress, GetEtalonAddress());
			}

			return true;
		}

		protected virtual bool IsDefaultBillingContact()
		{
			return AreShortContactsEquivalent(GetCurrentBillingContact(), GetEtalonBillingContact());
		}

		protected virtual bool IsDefaultBillingAddress()
		{
			return AreAddressesEquivalent(GetCurrentBillingAddress(), GetEtalonBillingAddress());
		}
		protected virtual bool IsDefaultShippingContact()
		{
			return AreShortContactsEquivalent(GetCurrentShippingContact(), GetEtalonShippingContact()); ;
		}

		protected virtual bool IsDefaultShippingAddress()
		{
			return AreAddressesEquivalent(GetCurrentShippingAddress(), GetEtalonShippingAddress());
		}


		protected object SafeGetEtalon(PXCache cache)
		{
			using (new ReplaceCurrentScope(new[] { new KeyValuePair<PXCache, object>(cache, null) }))
			using (new ReadOnlyScope(cache))
			{
				var item = cache.Insert();
				cache.SetStatus(item, PXEntryStatus.Held);
				return item;
			}
		}

		private static bool AreAddressesEquivalent(IAddressBase currentAddress, IAddressBase etalonAddress)
		{
			if (currentAddress == null || etalonAddress == null)
				return false;

			return !(currentAddress.AddressLine1 != etalonAddress.AddressLine1 ||
				currentAddress.AddressLine2 != etalonAddress.AddressLine2 ||
				currentAddress.City != etalonAddress.City ||
				currentAddress.State != etalonAddress.State ||
				currentAddress.CountryID != etalonAddress.CountryID ||
				currentAddress.PostalCode != etalonAddress.PostalCode);
		}

		private static bool AreFullContactsEquivalent(IPersonalContact currentContact, IPersonalContact etalonContact)
		{
			if (currentContact == null || etalonContact == null)
				return false;

			return !(currentContact.FullName != etalonContact.FullName ||
				currentContact.Title != etalonContact.Title ||
				currentContact.FirstName != etalonContact.FirstName ||
				currentContact.LastName != etalonContact.LastName ||
				currentContact.Salutation != etalonContact.Salutation ||
				currentContact.Attention != etalonContact.Attention ||
				currentContact.Email != etalonContact.Email ||
				currentContact.Phone1 != etalonContact.Phone1 ||
				currentContact.Phone1Type != etalonContact.Phone1Type ||
				currentContact.Phone2 != etalonContact.Phone2 ||
				currentContact.Phone2Type != etalonContact.Phone2Type ||
				currentContact.Phone3 != etalonContact.Phone3 ||
				currentContact.Phone3Type != etalonContact.Phone3Type ||
				currentContact.Fax != etalonContact.Fax ||
				currentContact.FaxType != etalonContact.FaxType);
		}

		private static bool AreShortContactsEquivalent(IContact currentContact, IContact etalonContact)
		{
			if (currentContact == null || etalonContact == null)
				return false;

			return !(currentContact.FullName != etalonContact.FullName ||
				currentContact.Attention != etalonContact.Attention ||
				currentContact.Email != etalonContact.Email ||
				currentContact.Phone1 != etalonContact.Phone1 ||
				currentContact.Phone1Type != etalonContact.Phone1Type ||
				currentContact.Phone2 != etalonContact.Phone2 ||
				currentContact.Phone2Type != etalonContact.Phone2Type);
		}

		protected struct ChangedData
		{
			public static readonly ChangedData ShouldNotReplace = new ChangedData(false, null, null);

			public static ChangedData ShouldReplace(Contact oldContact, Address oldAddress)
			{
				return new ChangedData(true, oldContact, oldAddress);
			}

			private ChangedData(bool canBeReplaced, Contact oldContact, Address oldAddress)
			{
				CanBeReplaced = canBeReplaced;
				OldContact = oldContact;
				OldAddress = oldAddress;
			}

			public bool CanBeReplaced { get; }
			public Contact OldContact { get; }
			public Address OldAddress { get; }

			public bool SkipAskContact()
			{
				return !CanBeReplaced || OldContact is null;
			}

			public bool WasContactChanged(IContact contact)
			{
				if (!CanBeReplaced)
					return false;

				if (OldContact is null)
					return true;

				return !AreShortContactsEquivalent(contact, OldContact);
			}

			public bool WasFullContactChanged(IPersonalContact contact)
			{
				if (!CanBeReplaced)
					return false;

				if (OldContact is null)
					return true;

				return !AreFullContactsEquivalent(contact, OldContact);
			}

			public bool SkipAskAddress()
			{
				return !CanBeReplaced || OldAddress is null;
			}


			public bool WasAddressChanged(IAddress address)
			{
				if (!CanBeReplaced)
					return false;

				if (OldAddress is null)
					return true;

				return !AreAddressesEquivalent(address, OldAddress);
			}
		}
	}
}
