using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.SideBySideComparison;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;
using PX.SM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PX.Objects.CR.ContactMaint_Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class ContactMaint_MergeEntitiesExt : MergeEntitiesExt<ContactMaint, Contact>
	{
		#region Helpers

		public virtual Contact GetSelectedContact()
		{
			if (int.TryParse(Filter.Current.MergeEntityID, out int contactId) is false)
				throw new PXException(MessagesNoPrefix.CannotParseContactId, contactId);

			var contact = Contact.PK.Find(Base, contactId);
			if (contact is null)
				throw new PXException(MessagesNoPrefix.ContactNotFound, contactId);

			return contact;
		}

		#endregion

		#region Overrides

		public override EntitiesContext GetLeftEntitiesContext()
		{
			return new EntitiesContext(Base,
				new EntityEntry(typeof(Contact), Base.Contact.Cache, Base.Contact.Current),
				new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, Base.AddressCurrent.SelectSingle()),
				new EntityEntry(Base.Answers.Cache, Base.Answers.SelectMain()));
		}

		public override EntitiesContext GetRightEntitiesContext()
		{
			var contact = GetSelectedContact();

			var address = Address.PK.Find(Base, contact.DefAddressID);
			if (address is null)
				throw new PXException(MessagesNoPrefix.AddressNotFound, contact.DefAddressID);

			var answers = Base.Answers.SelectInternal(contact);

			return new EntitiesContext(Base,
				new EntityEntry(typeof(Contact), Base.Contact.Cache, contact),
				new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, address),
				new EntityEntry(Base.Answers.Cache, answers));
		}

		public override void MergeRelatedDocuments(Contact targetEntity, Contact duplicateEntity)
		{
			PXCache Activities = Base.Caches[typeof(CRPMTimeActivity)];
			foreach (CRPMTimeActivity activity in PXSelect<CRPMTimeActivity, Where<CRPMTimeActivity.contactID, Equal<Current<Contact.contactID>>>>
				.SelectMultiBound(Base, new object[] { duplicateEntity })
				.RowCast<CRPMTimeActivity>()
				.Select(cas => (CRPMTimeActivity)Activities.CreateCopy(cas)))
			{
				activity.ContactID = targetEntity.ContactID;
				activity.BAccountID = targetEntity.BAccountID;

				Activities.Update(activity);
			}

			PXCache Cases = Base.Caches[typeof(CRCase)];
			foreach (CRCase cas in PXSelect<CRCase,
					Where<CRCase.contactID, Equal<Current<Contact.contactID>>>>
				.SelectMultiBound(Base, new object[] { duplicateEntity })
				.RowCast<CRCase>()
				.Select(cas => (CRCase)Cases.CreateCopy(cas)))
			{
				if (targetEntity.BAccountID != cas.CustomerID)
				{
					throw new PXException(Messages.ContactBAccountCase, duplicateEntity.DisplayName, cas.CaseCD);
				}

				cas.ContactID = targetEntity.ContactID;

				Cases.Update(cas);
			}

			PXCache Opportunities = Base.Caches[typeof(CROpportunity)];
			foreach (CROpportunity opp in PXSelect<CROpportunity,
					Where<CROpportunity.contactID, Equal<Current<Contact.contactID>>>>
				.SelectMultiBound(Base, new object[] { duplicateEntity })
				.RowCast<CROpportunity>()
				.Select(opp => (CROpportunity)Opportunities.CreateCopy(opp)))
			{
				if (targetEntity.BAccountID != opp.BAccountID)
				{
					throw new PXException(Messages.ContactBAccountForOpp, duplicateEntity.DisplayName, duplicateEntity.ContactID, opp.OpportunityID);
				}

				opp.ContactID = targetEntity.ContactID;

				Opportunities.Update(opp);
			}

			PXCache Relations = Base.Caches[typeof(CRRelation)];
			foreach (CRRelation rel in PXSelectJoin<CRRelation,
					LeftJoin<CRRelation2, On<CRRelation.entityID, Equal<CRRelation2.entityID>,
						And<CRRelation.role, Equal<CRRelation2.role>,
							And<CRRelation2.refNoteID, Equal<Required<Contact.noteID>>>>>>,
					Where<CRRelation2.entityID, IsNull,
						And<CRRelation.refNoteID, Equal<Required<Contact.noteID>>>>>
				.Select(Base, targetEntity.NoteID, duplicateEntity.NoteID)
				.RowCast<CRRelation>()
				.Select(rel => (CRRelation)Relations.CreateCopy(rel)))
			{
				rel.RelationID = null;
				rel.RefNoteID = targetEntity.NoteID;
				rel.RefEntityType = targetEntity.GetType().FullName;

				Relations.Insert(rel);
			}

			PXCache Subscriptions = Base.Caches[typeof(CRMarketingListMember)];
			foreach (CRMarketingListMember mmember in PXSelectJoin<CRMarketingListMember,
					LeftJoin<CRMarketingListMember2, On<CRMarketingListMember.marketingListID, Equal<CRMarketingListMember2.marketingListID>,
						And<CRMarketingListMember2.contactID, Equal<Required<Contact.contactID>>>>>,
					Where<CRMarketingListMember.contactID, Equal<Required<Contact.contactID>>,
						And<CRMarketingListMember2.marketingListID, IsNull>>>
				.Select(Base, targetEntity.ContactID, duplicateEntity.ContactID)
				.RowCast<CRMarketingListMember>()
				.Select(mmember => (CRMarketingListMember)Subscriptions.CreateCopy(mmember)))
			{
				mmember.ContactID = targetEntity.ContactID;

				Subscriptions.Insert(mmember);
			}

			PXCache Members = Base.Caches[typeof(CRCampaignMembers)];
			foreach (CRCampaignMembers cmember in PXSelectJoin<CRCampaignMembers,
					LeftJoin<CRCampaignMembers2, On<CRCampaignMembers.campaignID, Equal<CRCampaignMembers2.campaignID>,
						And<CRCampaignMembers2.contactID, Equal<Required<Contact.contactID>>>>>,
					Where<CRCampaignMembers2.campaignID, IsNull,
						And<CRCampaignMembers.contactID, Equal<Required<Contact.contactID>>>>>
				.Select(Base, targetEntity.ContactID, duplicateEntity.ContactID)
				.RowCast<CRCampaignMembers>()
				.Select(cmember => (CRCampaignMembers)Members.CreateCopy(cmember)))
			{
				cmember.ContactID = targetEntity.ContactID;

				Members.Insert(cmember);
			}

			PXCache NWatchers = Base.Caches[typeof(ContactNotification)];
			foreach (ContactNotification watcher in PXSelectJoin<ContactNotification,
					LeftJoin<ContactNotification2, On<ContactNotification.setupID, Equal<ContactNotification2.setupID>,
						And<ContactNotification2.contactID, Equal<Required<Contact.contactID>>>>>,
					Where<ContactNotification2.setupID, IsNull,
						And<ContactNotification.contactID, Equal<Required<Contact.contactID>>>>>
				.Select(Base, targetEntity.ContactID, duplicateEntity.ContactID)
				.RowCast<ContactNotification>()
				.Select(watcher => (ContactNotification)NWatchers.CreateCopy(watcher)))
			{
				watcher.NotificationID = null;
				watcher.ContactID = targetEntity.ContactID;

				NWatchers.Insert(watcher);
			}

			// skip ask
			Base.GetExtension<ContactMaint.LinkLeadFromContactExt>().Filter.View.Answer = WebDialogResult.Ignore;
			PXCache Leads = Base.Caches[typeof(CRLead)];
			foreach (CRLead lead in PXSelect<
						CRLead,
					Where<
						CRLead.refContactID, Equal<Required<Contact.contactID>>>>
				.Select(Base, duplicateEntity.ContactID)
				.RowCast<CRLead>()
				.Select(lead => (CRLead)Leads.CreateCopy(lead)))
			{
				lead.RefContactID = targetEntity.ContactID;
				lead.BAccountID = targetEntity.BAccountID;

				Leads.Update(lead);
			}

			if (duplicateEntity.UserID != null)
			{
				Users user = PXSelect<Users, Where<Users.pKID, Equal<Required<Contact.userID>>>>.Select(Base, duplicateEntity.UserID);
				if (user != null)
				{
					Base.EnsureCachePersistence(typeof(Users));
					user.IsApproved = false;
					Base.Caches[typeof(Users)].Update(user);
				}
			}
		}

		public override MergeEntitiesFilter CreateNewFilter(object mergeEntityID)
		{
			var filter = base.CreateNewFilter(mergeEntityID);
			if (Base.Contact.Current.Status == ContactStatus.Inactive)
				Filter.Cache.SetValueExt<MergeEntitiesFilter.targetRecord>(filter, MergeEntitiesFilter.targetRecord.SelectedRecord);
			return filter;
		}

		#endregion

		#region Events

		public virtual void _(Events.FieldVerifying<MergeEntitiesFilter, MergeEntitiesFilter.targetRecord> e)
		{
			var target = e.NewValue is MergeEntitiesFilter.targetRecord.SelectedRecord
				? GetSelectedContact() : Base.Contact.Current;

			if (target.Status == ContactStatus.Inactive)
			{
				PXUIFieldAttribute.SetWarning<MergeEntitiesFilter.targetRecord>(e.Cache, e.Row,
					MessagesNoPrefix.TargetRecordIsInactive);
			}
		}

		#endregion
	}
}
