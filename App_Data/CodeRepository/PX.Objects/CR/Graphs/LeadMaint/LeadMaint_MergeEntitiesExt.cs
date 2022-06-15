using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.SideBySideComparison;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.LeadMaint_Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class LeadMaint_MergeEntitiesExt : MergeEntitiesExt<LeadMaint, CRLead>
	{
		#region Overrides

		public override EntitiesContext GetLeftEntitiesContext()
		{
			return new EntitiesContext(Base,
				new EntityEntry(typeof(Contact), Base.Lead.Cache, Base.Lead.Current),
				new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, Base.AddressCurrent.SelectSingle()),
				new EntityEntry(Base.Answers.Cache, Base.Answers.SelectMain()));
		}

		public override EntitiesContext GetRightEntitiesContext()
		{
			if (int.TryParse(Filter.Current.MergeEntityID, out int contactId) is false)
				throw new PXException(MessagesNoPrefix.CannotParseContactId, contactId);

			var lead = CRLead.PK.Find(Base, contactId);
			if (lead is null)
				throw new PXException(MessagesNoPrefix.LeadNotFound, contactId);

			var address = Address.PK.Find(Base, lead.DefAddressID);
			if (address is null)
				throw new PXException(MessagesNoPrefix.AddressNotFound, lead.DefAddressID);

			var answers = Base.Answers.SelectInternal(lead);

			return new EntitiesContext(Base,
				new EntityEntry(typeof(Contact), Base.Lead.Cache, lead),
				new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, address),
				new EntityEntry(Base.Answers.Cache, answers));
		}

		public override void MergeRelatedDocuments(CRLead targetEntity, CRLead duplicateEntity)
		{
			PXCache Activities = Base.Caches[typeof(CRPMTimeActivity)];
			foreach (CRPMTimeActivity activity in PXSelect<CRPMTimeActivity, Where<CRPMTimeActivity.refNoteID, Equal<Current<CRLead.noteID>>>>
				.SelectMultiBound(Base, new object[] { duplicateEntity })
				.RowCast<CRPMTimeActivity>()
				.Select(cas => (CRPMTimeActivity)Activities.CreateCopy(cas)))
			{
				activity.RefNoteID = targetEntity.NoteID;
				activity.ContactID = targetEntity.RefContactID;
				activity.BAccountID = targetEntity.BAccountID;

				Activities.Update(activity);
			}

			PXCache Opportunities = Base.Caches[typeof(CROpportunity)];
			foreach (CROpportunity opp in PXSelect<CROpportunity, Where<CROpportunity.leadID, Equal<Current<CRLead.noteID>>>>
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
							And<CRRelation2.refNoteID, Equal<Required<CRLead.noteID>>>>>>,
					Where<CRRelation2.entityID, IsNull,
						And<CRRelation.refNoteID, Equal<Required<CRLead.noteID>>>>>
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
						And<CRMarketingListMember2.contactID, Equal<Required<CRLead.contactID>>>>>,
					Where<CRMarketingListMember.contactID, Equal<Required<CRLead.contactID>>,
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
						And<CRCampaignMembers2.contactID, Equal<Required<CRLead.contactID>>>>>,
					Where<CRCampaignMembers2.campaignID, IsNull,
						And<CRCampaignMembers.contactID, Equal<Required<CRLead.contactID>>>>>
				.Select(Base, targetEntity.ContactID, duplicateEntity.ContactID)
				.RowCast<CRCampaignMembers>()
				.Select(cmember => (CRCampaignMembers)Members.CreateCopy(cmember)))
			{
				cmember.ContactID = targetEntity.ContactID;

				Members.Insert(cmember);
			}
		}

		#endregion
	}
}
