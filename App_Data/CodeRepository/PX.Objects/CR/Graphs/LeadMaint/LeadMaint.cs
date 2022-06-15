using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Objects.AR;
using PX.Objects.CR.MassProcess;
using PX.Objects.CR.Extensions.CRCreateActions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Objects.IN;
using PX.SM;
using PX.TM;
using PX.Data.MassProcess;
using PX.Objects.CR.Extensions;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.CR.Extensions.Relational;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.CRContactAccountDataSync;
using PX.Objects.CR.Extensions.PinActivity;
using PX.Data.UI;
using PX.Objects.CR.LeadMaint_Extensions;

namespace PX.Objects.CR
{
    public class LeadMaint : PXGraph<LeadMaint, CRLead, CRLead.displayName>, ICaptionable
	{
        #region Selects
        

        //TODO: need review
        [PXHidden]
		public PXSelect<BAccount>
			bAccountBasic;

        [PXHidden]
		[PXCheckCurrent]
		public PXSetup<Company>
			company;

		[PXHidden]
		[PXCheckCurrent]
		public PXSetup<CRSetup>
			Setup;

		[PXViewName(Messages.Address)]
		public SelectFrom<Address>
			.Where<
				Address.addressID.IsEqual<CRLead.defAddressID.FromCurrent>>
			.View
			AddressCurrent;

		[PXViewName(Messages.Lead)]
		[PXCopyPasteHiddenFields(typeof(CRLead.status), typeof(CRLead.resolution), typeof(CRLead.duplicateStatus), typeof(CRLead.duplicateFound))]
		public PXSelect<CRLead,
			Where<CRLead.contactType, Equal<ContactTypesAttribute.lead>>>
			Lead;

		[PXHidden]
		public PXSelect<CRLead,
			Where<CRLead.contactID, Equal<Current<CRLead.contactID>>>>
			LeadCurrent;

		[PXCopyPasteHiddenView]
		public PXSelect<CRActivityStatistics,
				Where<CRActivityStatistics.noteID, Equal<Current<CRLead.noteID>>>>
			LeadActivityStatistics;

		[PXHidden]
		public PXSelect<CRLead,
			Where<CRLead.contactID, Equal<Current<CRLead.contactID>>>>
			LeadCurrent2;

		[PXViewName(Messages.Answers)]
		public CRAttributeList<CRLead>
			Answers;

		[PXViewName(Messages.Activities)]
		[PXFilterable]
		[CRDefaultMailTo]
		[CRActivityPinnedView]
		[CRReference(typeof(CRLead.bAccountID), typeof(CRLead.refContactID))]
		public LeadActivities
			Activities;
		
		[PXCopyPasteHiddenView]
		[PXViewName(Messages.Relations)]
		[PXFilterable]
		public CRRelationsList<CRLead.noteID>
			Relations;

		[PXViewName(Messages.Opportunities)]
		[PXCopyPasteHiddenView]
		public SelectFrom<CROpportunity>
					.InnerJoin<CRLead>
						.On<CROpportunity.leadID.IsEqual<CRLead.noteID>>
					.LeftJoin<CROpportunityClass>
						.On<CROpportunityClass.cROpportunityClassID.IsEqual<CROpportunity.classID>>
					.Where<
						CRLead.contactID.IsEqual<CRLead.contactID.FromCurrent>
						.And<CRLead.contactType.IsEqual<ContactTypesAttribute.lead>>
					>
				.View
			Opportunities;

		[PXViewName(Messages.CampaignMember)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(CRLead),
			typeof(Select<CRCampaign,
				Where<CRCampaign.campaignID, Equal<Current<CRCampaignMembers.campaignID>>>>))]
		public PXSelectJoin<CRCampaignMembers,
			InnerJoin<CRCampaign, On<CRCampaignMembers.campaignID, Equal<CRCampaign.campaignID>>>,
			Where<CRCampaignMembers.contactID, Equal<Current<CRLead.contactID>>>>
			Members;
		
		[PXHidden]
		public PXSelect<CRMarketingListMember>
			Subscriptions_stub;

		[PXViewName(Messages.Subscriptions)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(CRLead),
			typeof(Select<CRMarketingList,
				Where<CRMarketingList.marketingListID, Equal<Current<CRMarketingListMember.marketingListID>>>>))]
		public CRMMarketingContactSubscriptions<CRLead, CRLead.contactID>
			Subscriptions;

		[PXHidden]
		public PXSelectReadonly<CRLeadClass, Where<CRLeadClass.classID, Equal<Current<CRLead.classID>>>>
			LeadClass;

		#endregion

		#region Delegates

		#endregion

		#region Ctors

		public LeadMaint()
		{
			PXUIFieldAttribute.SetDisplayName<BAccountCRM.acctCD>(bAccountBasic.Cache, Messages.BAccountCD);

            PXUIFieldAttribute.SetEnabled<CRLead.assignDate>(Lead.Cache, Lead.Cache.Current, false);

            Activities.GetNewEmailAddress =
				() =>
				{
					var contact = Lead.Current;
					return contact != null && !string.IsNullOrWhiteSpace(contact.EMail)
						? PXDBEmailAttribute.FormatAddressesWithSingleDisplayName(contact.EMail, contact.DisplayName)
						: String.Empty;
				};
			if (this.IsImport && !this.IsExport)
			{
				Lead.WhereNew<Where<CRLead.contactType, Equal<ContactTypesAttribute.lead>>>();
				Lead.OrderByNew<OrderBy<Desc<CRLead.isActive, Desc<CRLead.duplicateStatus, Asc<CRLead.contactID>>>>>();
			}
			PXUIFieldAttribute.SetVisible<CRMarketingListMember.format>(Subscriptions.Cache,null, false);

			PXUIFieldAttribute.SetVisible<Contact.languageID>(LeadCurrent.Cache, null, PXDBLocalizableStringAttribute.HasMultipleLocales);
		}

		public string Caption()
		{
			CRLead currentItem = this.Lead.Current;
			if (currentItem == null) return "";

			var trueMemberName = !String.IsNullOrEmpty(currentItem.DisplayName)
				? currentItem.DisplayName
				: currentItem.FullName;

			if (!String.IsNullOrEmpty(currentItem.LastName) && !String.IsNullOrEmpty(currentItem.FullName))
			{
				return $"{trueMemberName} - {currentItem.FullName}";
			}
			else
			{
				return $"{trueMemberName}";
			}
		}

		#endregion

		#region Actions

		public PXMenuAction<CRLead> Action;

		#endregion

		#region Event Handlers

		#region Lead

		protected virtual void _(Events.RowSelected<CRLead> e)
		{
			CRLead row = e.Row as CRLead;
			if (row == null) return;
            PXUIFieldAttribute.SetEnabled<CRLead.contactID>(e.Cache, row, true);
			ConfigureAddressSectionUI();
			
			CRLeadClass leadClass = row.ClassID.
				With(_ => (CRLeadClass)PXSelectReadonly<CRLeadClass,
					Where<CRLeadClass.classID, Equal<Required<CRLeadClass.classID>>>>.
					Select(this, _));
			if (leadClass != null)
			{
				Activities.DefaultEMailAccountId = leadClass.DefaultEMailAccountID;
			}

			PXUIFieldAttribute.SetEnabled<CRLead.overrideRefContact>(e.Cache, row, row.RefContactID != null || row.BAccountID != null);
		}

		protected virtual void _(Events.RowPersisting<CRLead> e)
		{
			CRLead row = e.Row as CRLead;
			if (row != null && IsImport && !IsExport)
			{
				bool needupdate = false;
				foreach (var field in Caches[typeof(CRLead)].Fields)
				{
					object oldfieldvalue = Caches[typeof(CRLead)].GetValueOriginal(row, field);
					object newfieldvalue = Caches[typeof(CRLead)].GetValue(row, field);
					if (!Equals(oldfieldvalue, newfieldvalue))
					{
						needupdate = true;
						break;
					}
				}

				if (!needupdate)
					e.Cancel = true;
			}
		}

		#endregion

		#region Address

		protected virtual void _(Events.RowSelected<Address> e)
		{
			Address row = e.Row as Address;
			if (row == null) return;

			PXUIFieldAttribute.SetEnabled<Address.isValidated>(e.Cache, row, false);
			ConfigureAddressSectionUI();
		}

		[PopupMessage]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRLead.bAccountID> e) { }
		#endregion

		#region CRCampaignMembers

		[PXDBDefault(typeof(CRLead.contactID))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRCampaignMembers.contactID> e) { }

		#endregion

		#region CRMarketingListMember

		[PXSelector(typeof(Search<CRMarketingList.marketingListID,
			Where<CRMarketingList.isDynamic, IsNull, Or<CRMarketingList.isDynamic, NotEqual<True>>>>),
			DescriptionField = typeof(CRMarketingList.mailListCode))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRMarketingListMember.marketingListID> e) { }

		[PXDBDefault(typeof(CRLead.contactID))]
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Name")]
		[PXSelector(typeof(Search<CRLead.contactID>),
			typeof(CRLead.fullName),
			typeof(CRLead.displayName),
			typeof(CRLead.eMail),
			typeof(CRLead.phone1),
			typeof(CRLead.bAccountID),
			typeof(CRLead.salutation),
			typeof(CRLead.contactType),
			typeof(CRLead.isActive),
			typeof(CRLead.memberName),
			DescriptionField = typeof(CRLead.memberName),
			Filterable = true,
			DirtyRead = true)]
		[PXParent(typeof(Select<CRLead, Where<CRLead.contactID, Equal<Current<CRMarketingListMember.contactID>>>>))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void _(Events.CacheAttached<CRMarketingListMember.contactID> e) { }

		#endregion

		#region CRPMTimeActivity

		[PXDBChildIdentity(typeof(CRLead.contactID))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRPMTimeActivity.contactID> e) { }

		[PopupMessage]
		[PXDBDefault(typeof(CRLead.bAccountID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRPMTimeActivity.bAccountID> e) { }

		#endregion

		#region CROpportunityClass

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), Messages.ClassDescription)]
		protected virtual void _(Events.CacheAttached<CROpportunityClass.description> e) { }

		#endregion

		private void ConfigureAddressSectionUI()
		{
			PXCache leadCache = Caches[typeof(CRLead)];
			CRLead thisLead = leadCache.Current as CRLead;

			PXCache addressCache = Caches[typeof(Address)];
			Address thisAddress = addressCache.Current as Address;

			if (thisLead == null || thisAddress == null) return;

			string warningAboutAccountAddressChange = "";
			bool addressEnabled;
			if (thisLead.OverrideRefContact == true || thisLead.RefContactID == null)
			{
				addressEnabled = true;
			}
			else
			{
				var results = SelectFrom<Contact>
					.LeftJoin<BAccount>
						.On<BAccount.bAccountID.IsEqual<Contact.bAccountID>>
					.Where<
						Contact.contactID.IsEqual<CRLead.refContactID.FromCurrent>
					>.View.ReadOnly.SelectSingleBound(this, new object[] { });

				BAccount refContactAccount = null;
				Contact refContact = null;

				foreach (PXResult<Contact, BAccount> result in results)
				{
					refContact = (Contact)result;
					refContactAccount = (BAccount)result;
				}

				bool refContactAccountIsProspect = refContactAccount != null && refContactAccount.Type == BAccountType.ProspectType;
				bool refContactAddressLinkedToAccount = refContact != null && refContactAccount != null && refContact.DefAddressID == refContactAccount.DefAddressID;

				addressEnabled = refContactAccount == null || refContactAccountIsProspect || !refContactAddressLinkedToAccount;
				warningAboutAccountAddressChange = refContactAccountIsProspect && refContactAddressLinkedToAccount ? Messages.WarningAboutAccountAddressChange : "";
			}

			PXUIFieldAttribute.SetWarning<CRLead.overrideRefContact>(Caches[typeof(CRLead)], thisLead, warningAboutAccountAddressChange);

			PXUIFieldAttribute.SetEnabled<Address.addressLine1>(Caches[typeof(Address)], thisAddress, addressEnabled);
			PXUIFieldAttribute.SetEnabled<Address.addressLine2>(Caches[typeof(Address)], thisAddress, addressEnabled);
			PXUIFieldAttribute.SetEnabled<Address.city>(Caches[typeof(Address)], thisAddress, addressEnabled);
			PXUIFieldAttribute.SetEnabled<Address.state>(Caches[typeof(Address)], thisAddress, addressEnabled);
			PXUIFieldAttribute.SetEnabled<Address.postalCode>(Caches[typeof(Address)], thisAddress, addressEnabled);
			PXUIFieldAttribute.SetEnabled<Address.countryID>(Caches[typeof(Address)], thisAddress, addressEnabled);
		}


		#endregion

		#region Extensions

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class DefaultLeadOwnerGraphExt : CRDefaultDocumentOwner<
			LeadMaint, CRLead,
			CRLead.classID, CRLead.ownerID, CRLead.workgroupID>
		{ }

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LeadBAccountSharedAddressOverrideGraphExt : SharedChildOverrideGraphExt<LeadMaint, LeadBAccountSharedAddressOverrideGraphExt>
		{
			#region Initialization 

			public override bool ViewHasADelegate => true;

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(CRLead))
				{
					RelatedID = typeof(CRLead.bAccountID),
					ChildID = typeof(CRLead.defAddressID),
					IsOverrideRelated = typeof(CRLead.overrideAddress)
				};
			}

			protected override RelatedMapping GetRelatedMapping()
			{
				return new RelatedMapping(typeof(BAccount))
				{
					RelatedID = typeof(BAccount.bAccountID),
					ChildID = typeof(BAccount.defAddressID)
				};
			}

			protected override ChildMapping GetChildMapping()
			{
				return new ChildMapping(typeof(Address))
				{
					ChildID = typeof(Address.addressID),
					RelatedID = typeof(Address.bAccountID),
				};
			}

			#endregion

			#region Events

			protected override void _(Events.RowUpdating<Document> e)
			{
				if (e.NewRow?.Base is CRLead newRow
				    && e.Row?.Base is CRLead oldRow
				    && newRow.OverrideRefContact != oldRow.OverrideRefContact)
				{
					// hacks
					e.NewRow.IsOverrideRelated = newRow.OverrideRefContact is true
						|| IsSharedBAccountAddressAvailable(newRow) is false;

					e.Row.IsOverrideRelated = oldRow.OverrideRefContact is true
						|| IsSharedBAccountAddressAvailable(oldRow) is false;

					base._(e);
				}
			}

			#endregion

			#region Methods

			public virtual void UpdateRelated(CRLead newRow, CRLead oldRow)
			{
				var newRowDoc = newRow.GetExtension<Document>();
				var oldRowDoc = oldRow.GetExtension<Document>();
				UpdateRelated(newRowDoc, oldRowDoc);
			}

			public virtual void UpdateRelatedOnBAccountIDChange(CRLead newRow, int? oldBAccountID)
			{
				if (oldBAccountID == newRow.BAccountID)
					return;

				// IsOverrideRelated is set in row selected so it could be wrong here
				newRow.GetExtension<Document>().IsOverrideRelated = newRow.OverrideAddress is true
					|| IsSharedBAccountAddressAvailable(newRow) is false;
				var oldRow = (CRLead)Base.Lead.Cache.CreateCopy(newRow);
				Base.Lead.Cache.SetValue<CRLead.bAccountID>(oldRow, oldBAccountID);
				Base.Lead.Cache.SetValue<CRLead.defAddressID>(newRow, null);
				UpdateRelated(newRow, oldRow);
			}

			public virtual bool IsSharedBAccountAddressAvailable(CRLead lead)
			{
				var baccount = BAccount.PK.Find(Base, lead.BAccountID);
				// link only for prospect
				return baccount?.Type == BAccountType.ProspectType;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LeadAddressActions : CRAddressActions<LeadMaint, CRLead>
		{
			#region Initialization

			protected override ChildMapping GetChildMapping()
			{
				return new ChildMapping(typeof(Address))
				{
					ChildID = typeof(Address.addressID),
					RelatedID = typeof(Address.bAccountID)
				};
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(CRLead))
				{
					RelatedID = typeof(CRLead.bAccountID),
					ChildID = typeof(CRLead.defAddressID)
				};
			}

			#endregion
		}

		/// <exclude/>
		public class CRDuplicateEntitiesForLeadGraphExt : CRDuplicateEntities<LeadMaint, CRLead>
		{
			#region Initialization 

			public override Type AdditionalConditions => typeof(

				Brackets<
					// do not show contact, that is currently attached to the Lead
					DuplicateDocument.refContactID.FromCurrent.IsNotNull
					.And<CRDuplicateGrams.entityID.IsNotEqual<DuplicateDocument.refContactID.FromCurrent>>
					.Or<DuplicateDocument.refContactID.FromCurrent.IsNull>
				>
				.And<Brackets<
					// do not show BA, that is currently attached to the Lead
					DuplicateDocument.bAccountID.FromCurrent.IsNotNull
					.And<Brackets<
						DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
							.And<BAccountR.bAccountID.IsNotEqual<DuplicateDocument.bAccountID.FromCurrent>>
						.Or<DuplicateContact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>>
					>>
					.Or<DuplicateDocument.bAccountID.FromCurrent.IsNull>
				>>
				.And<
					DuplicateContact.isActive.IsEqual<True>.And<DuplicateContact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>>
					.Or<BAccountR.status.IsNotEqual<CustomerStatus.inactive>>
				>
				);

			public override string WarningMessage => Messages.LeadHavePossibleDuplicates;

			public static bool IsActive()
			{
				return IsExtensionActive();
			}

			public override void Initialize()
			{
				base.Initialize();

				DuplicateDocuments = new PXSelectExtension<DuplicateDocument>(Base.LeadCurrent);
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(CRLead)) { Key = typeof(CRLead.contactID) };
			}

			protected override DuplicateDocumentMapping GetDuplicateDocumentMapping()
			{
				return new DuplicateDocumentMapping(typeof(CRLead)) { Email = typeof(CRLead.eMail) };
			}

			#endregion

			#region Events

			protected virtual void _(Events.FieldUpdated<CRLead, CRLead.isActive> e)
			{
				CRLead row = e.Row as CRLead;
				if (e.Row == null)
					return;

				if (row.IsActive == true && row.IsActive != (bool?)e.OldValue)
				{
					row.DuplicateStatus = DuplicateStatusAttribute.NotValidated;
				}
			}

			protected virtual void _(Events.RowSelected<CRDuplicateRecord> e)
			{
				var rec = e.Row;
				if (rec == null) return;

				if (rec.CanBeMerged != true)
				{
					DuplicatesForMerging.Cache.RaiseExceptionHandling<CRDuplicateRecord.canBeMerged>(rec, rec.CanBeMerged,
							new PXSetPropertyException(Messages.ContactAssocRecordDiff, PXErrorLevel.RowWarning));
				}
			}

			#endregion

			#region Overrides

			public override CRLead GetTargetEntity(int targetID)
			{
				return PXSelect<CRLead, Where<CRLead.contactID, Equal<Required<CRLead.contactID>>>>.Select(Base, targetID);
			}

			public override Contact GetTargetContact(CRLead targetEntity)
			{
				return targetEntity as Contact;
			}

			public override Address GetTargetAddress(CRLead targetEntity)
			{
				return PXSelect<Address, Where<Address.addressID, Equal<Required<Address.addressID>>>>.Select(Base, targetEntity.DefAddressID);
			}


			public override void GetAllProperties(List<FieldValue> values, HashSet<string> fieldNames)
			{
				int order = 0;

				values.AddRange(GetMarkedPropertiesOf<CRLead>(Base, ref order).Where(fld => fieldNames.Add(fld.Name)));

				base.GetAllProperties(values, fieldNames);
			}

			protected override bool WhereMergingMet(CRDuplicateResult result)
			{
				var doc = DuplicateDocuments.Current;
				var duplicate = result.GetItem<CRDuplicateRecord>();

				if (duplicate == null)
					return false;

				bool isOfSameType = duplicate.DuplicateContactType == ContactTypesAttribute.Lead;
				return isOfSameType;
			}

			protected override bool CanBeMerged(CRDuplicateResult result)
			{
				var doc = DuplicateDocuments.Current;
				var duplicate = result.GetItem<CRDuplicateRecord>();

				bool isOfSameParentOrRefContact =
					duplicate != null
					&& (doc.RefContactID == null || duplicate.DuplicateRefContactID == null || duplicate.DuplicateRefContactID == doc.RefContactID)
					&& (doc.BAccountID == null || duplicate.DuplicateBAccountID == null || duplicate.DuplicateBAccountID == doc.BAccountID);
				return isOfSameParentOrRefContact;
			}

			public override void DoDuplicateAttach(DuplicateDocument duplicateDocument)
			{
				var duplicateRecord = DuplicatesForLinking.Cache.Current as CRDuplicateRecord;

				if (duplicateRecord == null)
					return;

				Contact duplicate = PXSelect<Contact,
						Where<Contact.contactID, Equal<Current<CRDuplicateRecord.duplicateContactID>>>>
					.SelectSingleBound(Base, new object[] { duplicateRecord });

				if (duplicate == null)
					return;

				if (duplicate.ContactType == ContactTypesAttribute.BAccountProperty)
				{
					duplicateDocument.RefContactID = null;
					duplicateDocument.BAccountID = null;

					DuplicateDocuments.Update(duplicateDocument);

					duplicateDocument.BAccountID = duplicate.BAccountID;
				}
				else if (duplicate.ContactType == ContactTypesAttribute.Person)
				{
					duplicateDocument.RefContactID = duplicate.ContactID;
					duplicateDocument.BAccountID = duplicate.BAccountID;
				}
				else
				{
					throw new PXException(Messages.CanAttachToContactOrBAccount);
				}

				DuplicateDocuments.Update(duplicateDocument);
			}

			public override void ValidateEntitiesBeforeMerge(List<CRLead> duplicateEntities)
			{
				int? firstRefContactID = null;
				foreach (CRLead lead in duplicateEntities)
				{
					if (lead.RefContactID != null)
					{
						if (firstRefContactID == null)
						{
							firstRefContactID = lead.RefContactID;
						}
						else if (firstRefContactID != lead.RefContactID)
						{
							throw new PXException(Messages.DuplicatesMergeProhibitedDueToDifferentContacts);
						}
					}
				}
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateAccountFromLeadGraphExt : CRCreateAccountAction<LeadMaint, CRLead>
		{
			#region Initialization

			protected override string TargetType => CRTargetEntityType.Lead;

			public override void Initialize()
			{
				base.Initialize();

				Addresses = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentAddress>(Base.AddressCurrent);
				Contacts = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContact>(Base.LeadCurrent);
			}

			protected override DocumentContactMapping GetDocumentContactMapping()
			{
				return new DocumentContactMapping(typeof(CRLead)) { Email = typeof(CRLead.eMail) };
			}
			protected override DocumentAddressMapping GetDocumentAddressMapping()
			{
				return new DocumentAddressMapping(typeof(Address));
			}

			protected override PXSelectBase<CRPMTimeActivity> Activities => Base.Activities;

			#endregion

			#region Events

			protected override void _(Events.RowSelected<AccountsFilter> e)
			{
				this.NeedToUse = Base
					.LeadClass
					.SelectSingle()
					?.RequireBAccountCreation ?? true;

				base._(e);

				e.Cache.AdjustUI(e.Row)
					.For<AccountsFilter.linkContactToAccount>(_ => _.Visible = false);
			}

			protected virtual void _(Events.FieldDefaulting<AccountsFilter, AccountsFilter.accountClass> e)
			{
				if (ExistingAccount.SelectSingle() is BAccount existingAccount)
				{
					e.NewValue = existingAccount.ClassID;
					e.Cancel = true;
					return;
				}

				CRLead lead = Base.Lead.Current;
				if (lead == null) return;

				CRLeadClass cls = Base
					.LeadClass
					.SelectSingle();

				if (cls?.TargetBAccountClassID != null)
				{
					e.NewValue = cls.TargetBAccountClassID;
				}
				else
				{
					e.NewValue = Base.Setup.Current?.DefaultCustomerClassID;
				}

				e.Cancel = true;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateContactFromLeadGraphExt : CRCreateContactAction<LeadMaint, CRLead>
		{
			#region Initialization

			protected override string TargetType => CRTargetEntityType.Lead;

			public override void Initialize()
			{
				base.Initialize();

				Addresses = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentAddress>(Base.AddressCurrent);
				Contacts = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContact>(Base.LeadCurrent);
				ContactMethod = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContactMethod>(Base.LeadCurrent);
			}

			protected override DocumentContactMapping GetDocumentContactMapping()
			{
				return new DocumentContactMapping(typeof(CRLead)) { Email = typeof(CRLead.eMail) };
			}
			protected override DocumentContactMethodMapping GetDocumentContactMethodMapping()
			{
				return new DocumentContactMethodMapping(typeof(CRLead));
			}
			protected override DocumentAddressMapping GetDocumentAddressMapping()
			{
				return new DocumentAddressMapping(typeof(Address));
			}

			protected override PXSelectBase<CRPMTimeActivity> Activities => Base.Activities;

			#endregion

			#region Events

			protected virtual void _(Events.FieldDefaulting<ContactFilter, ContactFilter.contactClass> e)
			{
				if (ExistingContact.SelectSingle() is Contact existingContact)
				{
					e.NewValue = existingContact.ClassID;
					e.Cancel = true;
					return;
				}

				CRLead lead = Base.Lead.Current;
				if (lead == null) return;

				CRLeadClass cls = PXSelect<
						CRLeadClass,
						Where<
							CRLeadClass.classID,
							Equal<Required<CRLead.classID>>>>
					.Select(Base, lead.ClassID);

				if (cls?.TargetContactClassID != null)
				{
					e.NewValue = cls.TargetContactClassID;
				}
				else
				{
					e.NewValue = Base.Setup.Current?.DefaultContactClassID;
				}

				e.Cancel = true;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateOpportunityFromLeadGraphExt : CRCreateOpportunityAction<LeadMaint, CRLead>
		{
			#region Initialization

			protected override string TargetType => CRTargetEntityType.Lead;

			public override void Initialize()
			{
				base.Initialize();

				Addresses = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentAddress>(Base.AddressCurrent);
				Contacts = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContact>(Base.LeadCurrent);
			}

			protected override DocumentContactMapping GetDocumentContactMapping()
			{
				return new DocumentContactMapping(typeof(CRLead)) { Email = typeof(CRLead.eMail) };
			}
			protected override DocumentAddressMapping GetDocumentAddressMapping()
			{
				return new DocumentAddressMapping(typeof(Address));
			}

			protected override PXSelectBase<CRPMTimeActivity> Activities => Base.Activities;

			#endregion

			#region Events

			public virtual void _(Events.FieldDefaulting<OpportunityFilter, OpportunityFilter.opportunityClass> e)
			{
				e.NewValue = Base
					.LeadClass
					.SelectSingle()
					?.TargetOpportunityClassID is string oppCls
						? oppCls
						: Base.Setup.Current?.DefaultOpportunityClassID;
				e.Cancel = true;
			}

			#endregion

			#region Overrides

			protected override CROpportunity CreateMaster(OpportunityMaint graph, OpportunityConversionOptions options)
			{
				var opp =  base.CreateMaster(graph, options);
				if (Base.LeadClass.SelectSingle()?.TargetOpportunityStage is string stage)
				{
					opp.StageID = stage;
				}
				
				opp = graph.Opportunity.Update(opp);

				CROpportunity.Events.Select(o => o.OpportunityCreatedFromLead).FireOn(graph, opp);

				return opp;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateBothAccountAndContactFromLeadGraphExt : CRCreateBothContactAndAccountAction<LeadMaint, CRLead, CreateAccountFromLeadGraphExt, CreateContactFromLeadGraphExt> { }

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateOpportunityAllFromLeadGraphExt : CRCreateOpportunityAllAction<LeadMaint, CRLead, CreateOpportunityFromLeadGraphExt, CreateAccountFromLeadGraphExt, CreateContactFromLeadGraphExt> { }

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class UpdateRelatedContactInfoFromLeadGraphExt : CRUpdateRelatedContactInfoGraphExt<LeadMaint>
		{
			#region Events

			protected virtual void _(Events.RowPersisting<CRLead> e)
			{
				if (e.Row == null)
					return;

				SetUpdateRelatedInfo(e, e.Cache.GetFields_ContactInfo().Union(new[] { nameof(CR.Contact.DefAddressID) }));

				SetUpdateRelatedInfo<CRLead, CRLead.refContactID>(e);
			}

			protected virtual void _(Events.RowPersisting<Address> e)
			{
				if (e.Row == null)
					return;

				SetUpdateRelatedInfo(e, e.Cache.GetFields_ContactInfo());
			}

			protected virtual void _(Events.RowPersisted<CRLead> e)
			{
				var row = e.Row;
				if (row == null
					|| UpdateRelatedInfo != true
					|| e.TranStatus != PXTranStatus.Open
					|| e.Operation.Command().IsNotIn(PXDBOperation.Update, PXDBOperation.Insert)
					|| row.OverrideRefContact == true)
				{
					return;
				}

				if (row.RefContactID != null)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
					UpdateContact(e.Cache, row,
						new SelectFrom<Contact>
							.LeftJoin<Standalone.CRLead>
								.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
							.Where<Standalone.CRLead.refContactID.IsEqual<@P.AsInt>
								.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>
								// Account's Contact info itself
								.Or<Contact.contactID.IsEqual<@P.AsInt>>>
						.View(Base),
						row.RefContactID, row.RefContactID);

				}
				else if (row.BAccountID != null)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
					UpdateContact(e.Cache, row,
						new SelectFrom<Contact>
							.LeftJoin<Standalone.CRLead>
								.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
							.LeftJoin<BAccount>
								.On<BAccount.defContactID.IsEqual<Contact.contactID>>
							.Where<
								// Leads that are linked to the same Account
								Contact.bAccountID.IsEqual<@P.AsInt>
									.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>

								// Account's Contact info itself
								.Or<BAccount.bAccountID.IsEqual<@P.AsInt>
									.And<BAccount.type.IsEqual<BAccountType.prospectType>>>>
						.View(Base),
						row.BAccountID, row.BAccountID);
				}
			}

			protected virtual void _(Events.RowPersisted<Address> e)
			{
				var row = e.Row;
				if (row == null
					|| UpdateRelatedInfo != true
					|| e.TranStatus != PXTranStatus.Open
					|| e.Operation.Command().IsNotIn(PXDBOperation.Update, PXDBOperation.Insert))
				{
					return;
				}

				CRLead lead = Base.Lead.Current ?? PXSelect<
						CRLead,
						Where<CRLead.defAddressID.IsEqual<@P.AsInt>>>
					.Select(Base, row.AddressID);

				if (lead == null || lead.OverrideRefContact == true)
					return;

				if (lead.RefContactID != null)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
					UpdateAddress(e.Cache, row,
						new SelectFrom<Address>
							.InnerJoin<Contact>
								.On<Contact.defAddressID.IsEqual<Address.addressID>>
							.LeftJoin<Standalone.CRLead>
								.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
							.LeftJoin<BAccount>
								.On<BAccount.bAccountID.IsEqual<Contact.bAccountID>>
							.Where<
								// Leads that are linked to the same Contact
								Standalone.CRLead.refContactID.IsEqual<@P.AsInt>
									.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>

								// linked to BA
								.Or<BAccount.bAccountID.IsNotNull>
									.And<Brackets<

									// unlinked Contacts of Customers and Vendors
									BAccount.type.IsIn<BAccountType.customerType, BAccountType.vendorType, BAccountType.combinedType>>
										.And<Contact.defAddressID.IsNotEqual<BAccount.defAddressID>>

									// Contact of Prospect
									.Or<BAccount.type.IsEqual<BAccountType.prospectType>>>
								.And<Contact.contactID.IsEqual<@P.AsInt>>

							// Contact without BA
							.Or<Contact.bAccountID.IsNull>
								.And<Contact.contactID.IsEqual<@P.AsInt>>>
							.View(Base),
						lead.RefContactID, lead.RefContactID, lead.RefContactID);
				}
				else if (lead.BAccountID != null)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
					UpdateAddress(e.Cache, row,
						new SelectFrom<Address>
							.InnerJoin<Contact>
								.On<Contact.defAddressID.IsEqual<Address.addressID>>
							.LeftJoin<Standalone.CRLead>
								.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
							.LeftJoin<BAccount>
								.On<BAccount.defContactID.IsEqual<Contact.contactID>>
							.Where<
								// Leads that are linked to the same Account
								Contact.bAccountID.IsEqual<@P.AsInt>
									.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>


								// Account's Contact info itself
								.Or<BAccount.bAccountID.IsEqual<@P.AsInt>
									.And<BAccount.type.IsEqual<BAccountType.prospectType>>>>
							.View(Base),
						lead.BAccountID, lead.BAccountID);
				}
			}

			#endregion

			#region Overrides

			[PXOverride]
			public virtual void Persist(Action del)
			{
				del();
				if (UpdateRelatedInfo is true)
				{
					// clear contact cache to properly display refContactId
					// should be after completed transaction
					PXSelectorAttribute.ClearGlobalCache<Contact>();
					Base.Caches<Contact>().Clear();
				}
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LastNameOrCompanyNameRequiredGraphExt : PXGraphExtension<LeadMaint>
		{
			[PXRemoveBaseAttribute(typeof(PXUIRequiredAttribute))]
			protected virtual void _(Events.CacheAttached<CRLead.displayName> e) { }

			[PXRemoveBaseAttribute(typeof(CRLastNameDefaultAttribute))]
			protected virtual void _(Events.CacheAttached<CRLead.lastName> e) { }

			protected virtual void _(Events.RowPersisting<CRLead> e)
			{
				var row = e.Row;
				if (row == null) return;

				if (row.LastName == null && row.FullName == null)
					throw new PXSetPropertyException(Messages.LastNameOrFullNameReqired);
			}
		}

		/// <exclude/>
		public class ExtensionSort
			: SortExtensionsBy<ExtensionOrderFor<LeadMaint>
				.FilledWith<
					DefaultLeadOwnerGraphExt,
					LeadMaint_LinkContactExt,
					UpdateRelatedContactInfoFromLeadGraphExt,
					LastNameOrCompanyNameRequiredGraphExt,
					CreateContactFromLeadGraphExt,
					CreateAccountFromLeadGraphExt,
					CreateOpportunityFromLeadGraphExt,
					CreateBothAccountAndContactFromLeadGraphExt,
					CreateOpportunityAllFromLeadGraphExt,
					LeadMaint_LinkAccountExt>> { }

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LeadMaintAddressLookupExtension : CR.Extensions.AddressLookupExtension<LeadMaint, CRLead, Address>
		{
			protected override string AddressView => nameof(Base.AddressCurrent);
		}

		#endregion
	}
}
