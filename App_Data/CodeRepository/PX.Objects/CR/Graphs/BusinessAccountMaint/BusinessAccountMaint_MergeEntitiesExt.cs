using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.SideBySideComparison;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PX.Objects.CR.BusinessAccountMaint_Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class BusinessAccountMaint_MergeEntitiesExt : MergeEntitiesExt<BusinessAccountMaint, BAccount>
	{
		#region Helpers

		public virtual BAccount GetSelectedBAccount()
		{
			if (int.TryParse(Filter.Current.MergeEntityID, out int baccountId) is false)
				throw new PXException(MessagesNoPrefix.CannotParseBAccountId, baccountId);

			var baccount = BAccount.PK.Find(Base, baccountId);
			if (baccount is null)
				throw new PXException(MessagesNoPrefix.BAccountNotFound, baccountId);

			return baccount;
		}

		#endregion

		#region Overrides

		public override EntitiesContext GetLeftEntitiesContext()
		{
			var current = Base.CurrentBAccount.Select().FirstOrDefault();
			var account = Base.BAccount.Current;
			var ext = Base.GetExtension<BusinessAccountMaint.DefContactAddressExt>();
			var contact = ext.DefContact.SelectSingle();
			var address = ext.DefAddress.SelectSingle();

			return new EntitiesContext(Base,
				new EntityEntry(Base.BAccount.Cache, account),
				new EntityEntry(typeof(Contact), ext.DefContact.Cache, contact),
				new EntityEntry(typeof(Address), ext.DefAddress.Cache, address),
				new EntityEntry(Base.Answers.Cache, Base.Answers.SelectMain()));
		}

		public override EntitiesContext GetRightEntitiesContext()
		{
			var baccount = GetSelectedBAccount();

			var contact = Contact.PK.Find(Base, baccount.DefContactID);
			if (contact is null)
				throw new PXException(MessagesNoPrefix.ContactNotFound, baccount.DefContactID);

			var address = Address.PK.Find(Base, baccount.DefAddressID);
			if (address is null)
				throw new PXException(MessagesNoPrefix.AddressNotFound, baccount.DefAddressID);

			var answers = Base.Answers.SelectInternal(baccount);

			return new EntitiesContext(Base,
				new EntityEntry(Base.BAccount.Cache, baccount),
				new EntityEntry(typeof(Contact), Base.ContactDummy.Cache, contact),
				new EntityEntry(typeof(Address), Base.AddressDummy.Cache, address),
				new EntityEntry(Base.Answers.Cache, answers));
		}

		public override void MergeRelatedDocuments(BAccount targetEntity, BAccount duplicateEntity)
		{
			int? defContactID = duplicateEntity.DefContactID;
			PXCache Contacts = Base.Caches[typeof(Contact)];
			foreach (Contact contact in
				SelectFrom<Contact>
				.Where<Contact.bAccountID.IsEqual<@P.AsInt>
					.And<Contact.contactType.IsNotEqual<ContactTypesAttribute.lead>>
				>
				.View
				.Select(Base, duplicateEntity.BAccountID)
				.RowCast<Contact>()
				.Where(c => c.ContactID != defContactID)
				.Select(c => (Contact)Contacts.CreateCopy(c)))
			{
				contact.BAccountID = targetEntity.BAccountID;

				Contacts.Update(contact);
			}

			PXCache Activities = Base.Caches[typeof(CRPMTimeActivity)];
			foreach (CRPMTimeActivity activity in PXSelect<
						CRPMTimeActivity,
					Where<
						CRPMTimeActivity.bAccountID, Equal<Required<BAccount.bAccountID>>>>
				.Select(Base, duplicateEntity.BAccountID)
				.RowCast<CRPMTimeActivity>()
				.Select(cas => (CRPMTimeActivity)Activities.CreateCopy(cas)))
			{
				if (activity.BAccountID == duplicateEntity.BAccountID)
				{
					activity.BAccountID = targetEntity.BAccountID;
				}
				activity.BAccountID = targetEntity.BAccountID;

				Activities.Update(activity);
			}

			PXCache Cases = Base.Caches[typeof(CRCase)];
			foreach (CRCase cas in PXSelect<
						CRCase,
					Where<
						CRCase.customerID, Equal<Required<BAccount.bAccountID>>>>
				.Select(Base, duplicateEntity.BAccountID)
				.RowCast<CRCase>()
				.Select(cas => (CRCase)Cases.CreateCopy(cas)))
			{
				cas.CustomerID = targetEntity.BAccountID;

				Cases.Update(cas);
			}

			PXCache Opportunities = Base.Caches[typeof(CROpportunity)];
			foreach (CROpportunity opp in PXSelect<
						CROpportunity,
					Where<
						CROpportunity.bAccountID, Equal<Required<BAccount.bAccountID>>>>
				.Select(Base, duplicateEntity.BAccountID)
				.RowCast<CROpportunity>()
				.Select(opp => (CROpportunity)Opportunities.CreateCopy(opp)))
			{
				opp.BAccountID = targetEntity.BAccountID;
				opp.LocationID = targetEntity.DefLocationID;

				Opportunities.Update(opp);
			}

			PXCache Relations = Base.Caches[typeof(CRRelation)];
			foreach (CRRelation rel in PXSelectJoin<CRRelation,
				LeftJoin<CRRelation2,
					On<CRRelation.entityID, Equal<CRRelation2.entityID>,
					And<CRRelation.role, Equal<CRRelation2.role>,
					And<CRRelation2.refNoteID, Equal<Required<BAccount.noteID>>>>>>,
				Where<CRRelation2.entityID, IsNull,
					And<CRRelation.refNoteID, Equal<Required<BAccount.noteID>>>>>
				.Select(Base, targetEntity.NoteID, duplicateEntity.NoteID)
				.RowCast<CRRelation>()
				.Select(rel => (CRRelation)Relations.CreateCopy(rel)))
			{
				rel.RelationID = null;
				rel.RefNoteID = targetEntity.NoteID;
				rel.RefEntityType = targetEntity.GetType().FullName;

				Relations.Insert(rel);
			}

			PXCache Leads = Base.Caches[typeof(CRLead)];
			foreach (CRLead lead in PXSelect<
						CRLead,
					Where<
						CRLead.bAccountID, Equal<Required<BAccount.bAccountID>>>>
				.Select(Base, duplicateEntity.BAccountID)
				.RowCast<CRLead>()
				.Select(lead => (CRLead)Leads.CreateCopy(lead)))
			{
				lead.BAccountID = targetEntity.BAccountID;

				Leads.Update(lead);
			}
		}

		public override IEnumerable<string> GetFieldsForComparison(Type itemType, PXCache leftCache, PXCache rightCache)
		{
			var result = base.GetFieldsForComparison(itemType, leftCache, rightCache);
			if (itemType == typeof(BAccount))
				result = result.Append(nameof(BAccount.PrimaryContactID));
			return result;
		}

		public override (EntitiesContext LeftContext, EntitiesContext RightContext) ProcessComparisons(IReadOnlyCollection<MergeComparisonRow> comparisons)
		{
			var result = base.ProcessComparisons(comparisons);
			var (_, duplicate) = DefineTargetAndDuplicateContexts(result.LeftContext, result.RightContext);
			using (duplicate.PreserveCurrentsScope())
			{
				var entry = duplicate.Entries[typeof(BAccount)];
				var baccount = entry.Single<BAccount>();
				baccount.PrimaryContactID = null;
				entry.Cache.Update(baccount);
				return result;
			}
		}

		public override MergeEntitiesFilter CreateNewFilter(object mergeEntityID)
		{
			var contactId = Convert.ToInt32(mergeEntityID);
			var contact = Contact.PK.Find(Base, contactId);
			if (contact is null)
				throw new PXException(MessagesNoPrefix.ContactNotFound, contactId);

			var baccount = BAccount.PK.Find(Base, contact.BAccountID);
			if (baccount is null)
				throw new PXException(MessagesNoPrefix.BAccountNotFound, contact.BAccountID);

			var filter = base.CreateNewFilter(baccount.BAccountID);

			var target = Base.BAccount.Current.Status == CustomerStatus.Inactive
				|| baccount.Type != BAccountType.ProspectType
				? MergeEntitiesFilter.targetRecord.SelectedRecord
				: MergeEntitiesFilter.targetRecord.CurrentRecord;

			Filter.Cache.SetValueExt<MergeEntitiesFilter.targetRecord>(filter, target);

			return filter;
		}

		public override MergeComparisonRow CreateComparisonRow(string fieldName, Type itemType, ref int order,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) left,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) right)
		{
			var row = base.CreateComparisonRow(fieldName, itemType, ref order, left, right);

			if (row.FieldName == nameof(BAccount.PrimaryContactID))
			{
				row.FieldDisplayName = PXMessages.LocalizeNoPrefix(Messages.PrimaryContact);
				row.LeftFieldState.SelectorMode = PXSelectorMode.DisplayModeText;
				row.RightFieldState.SelectorMode = PXSelectorMode.DisplayModeText;

				if (string.IsNullOrEmpty(row.LeftValue_description)
					&& int.TryParse(row.LeftValue, out int leftId)
					&& leftId < 0)
					row.LeftFieldState.Value = row.LeftValue = null;

				if (string.IsNullOrEmpty(row.RightValue_description)
					&& int.TryParse(row.RightValue, out int rightId)
					&& rightId < 0)
					row.RightFieldState.Value = row.RightValue = null;
			}

			return row;
		}

		#endregion

		#region Events

		public virtual void _(Events.FieldVerifying<MergeEntitiesFilter, MergeEntitiesFilter.targetRecord> e)
		{
			var target = Base.BAccount.Current;
			var nonTarget = GetSelectedBAccount();
			if (e.NewValue is MergeEntitiesFilter.targetRecord.SelectedRecord)
				(target, nonTarget) = (nonTarget, target);

			if (target.Status == CustomerStatus.Inactive)
			{
				PXUIFieldAttribute.SetWarning<MergeEntitiesFilter.targetRecord>(e.Cache, e.Row,
					MessagesNoPrefix.TargetRecordIsInactive);
			}
			if (nonTarget.Type != BAccountType.ProspectType)
			{
				PXUIFieldAttribute.SetError<MergeEntitiesFilter.targetRecord>(e.Cache, e.Row,
					Messages.OnlyBAccountMergeSources);
			}
		}

		#endregion
	}
}
