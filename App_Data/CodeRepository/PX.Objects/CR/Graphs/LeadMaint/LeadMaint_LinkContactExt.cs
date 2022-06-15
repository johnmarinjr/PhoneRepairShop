using PX.Data;
using PX.Objects.CR.Extensions.SideBySideComparison;
using System.Collections.Generic;
using System.Linq;
using PX.Data.BQL.Fluent;
using PX.Objects.CR.Extensions.SideBySideComparison.Link;

namespace PX.Objects.CR.LeadMaint_Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class LeadMaint_LinkContactExt : LinkEntitiesExt_EventBased<LeadMaint, CRLead, LinkFilter, CRLead, CRLead.refContactID>
	{
		#region Initialization

		public override string LeftValueDescription => Messages.Lead;
		public override string RightValueDescription => Messages.Contact;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public SelectFrom<ContactAccount>
			.Where<
				ContactAccount.bAccountID.IsEqual<CRLead.bAccountID.FromCurrent>
				.And<ContactAccount.contactType.IsEqual<ContactTypesAttribute.person>>
			>
			.OrderBy<
				ContactAccount.isPrimary.Desc,
				ContactAccount.displayName.Asc
			>
			.View Link_SelectEntityForLink;

		#endregion

		#region Overrides

		public override EntitiesContext GetLeftEntitiesContext()
		{
			return new EntitiesContext(Base,
				new EntityEntry(typeof(Contact), Base.Lead.Cache, Base.Lead.Current),
				new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, Base.AddressCurrent.SelectSingle()));
		}

		public override EntitiesContext GetRightEntitiesContext()
		{
			var graph = PXGraph.CreateInstance<ContactMaint>();
			var contactId = int.TryParse(Filter.Current.LinkedEntityID, out int res) ? (int?)res : null;

			int? addressId = null;
			// if no refcontact specified, then take contact info from baccount
			if (contactId == null)
			{
				var baccount = BAccount.PK.Find(Base, Base.Lead.Current.BAccountID);
				contactId = baccount?.DefContactID;
				addressId = baccount?.DefAddressID;
			}

			var contact = Contact.PK.Find(graph, contactId);
			if (contact is null)
				throw new PXException(MessagesNoPrefix.ContactNotFound, contactId);

			addressId ??= contact.DefAddressID;
			var address = Address.PK.Find(graph, addressId);
			if (address is null)
				throw new PXException(MessagesNoPrefix.AddressNotFound, addressId);

			graph.Contact.Current = contact;
			graph.Contact.Cache.RaiseRowSelected(contact);
			graph.AddressCurrent.Current = address;
			graph.AddressCurrent.Cache.RaiseRowSelected(address);
			return new EntitiesContext(graph,
				new EntityEntry(graph.Contact.Cache, contact),
				new EntityEntry(graph.AddressCurrent.Cache, address));
		}

		public override void UpdateMainAfterProcess()
		{
			UpdatingEntityCurrent.OverrideRefContact = Filter.Current.ProcessLink != true;
			base.UpdateMainAfterProcess();
		}

		public override void UpdateRightEntitiesContext(EntitiesContext context, IEnumerable<LinkComparisonRow> result) { }

		protected override object GetSelectedEntityID()
		{
			return Link_SelectEntityForLink.Current?.ContactID;
		}

		#endregion

		#region Events

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Sync with Lead")]
		protected virtual void _(Events.CacheAttached<LinkFilter.processLink> e) { }

		protected virtual void _(Events.FieldUpdated<CRLead, CRLead.overrideRefContact> e)
		{
			if (e.NewValue is false && !false.Equals(e.OldValue))
			{
				PreventRecursionCall.Execute(() =>
				{
					Filter.Current.LinkedEntityID = e.Row.RefContactID?.ToString();
					var items = GetPreparedComparisons().ToList();
					items.ForEach(item => item.Selection = ComparisonSelection.Right /* contact */);
					ProcessComparisons(items);
				});
			}
		}


		protected virtual void _(Events.FieldUpdated<CRLead, CRLead.refContactID> e)
		{
			if (e.OldValue != null
				&& e.NewValue == null
				&& e.Row.BAccountID != null)
			{
				e.Row.OverrideRefContact = true;
			}
		}

		#endregion
	}
}
