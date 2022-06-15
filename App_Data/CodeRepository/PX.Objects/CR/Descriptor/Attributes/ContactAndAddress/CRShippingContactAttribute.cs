using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	public class CRShippingContactAttribute : CRContactAttribute
	{
		public CRShippingContactAttribute(Type SelectType)
			: base(typeof(CRShippingContact.contactID), typeof(CRShippingContact.isDefaultContact), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<CRShippingContact.overrideContact>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultContact<CRShippingContact, CRShippingContact.contactID>(sender, DocumentRow, Row);
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyContact<CRShippingContact, CRShippingContact.contactID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var newValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			Contact_IsDefaultContact_FieldVerifying<CRShippingContact>(sender, new PXFieldVerifyingEventArgs(e.Row, newValue, e.ExternalCall));
		}

		protected override (PXView, object[]) GetViewWithParameters(PXCache sender, object DocumentRow, object ContactRow)
		{
			PXView view = null;
			object parm = null;

			if (sender.GetValue<CROpportunity.locationID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.locationID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Contact>
					.LeftJoin<Location>
						.On<Location.defContactID.IsEqual<Contact.contactID>>
					.LeftJoin<CRShippingContact>
						.On<CRShippingContact.bAccountID.IsEqual<Contact.bAccountID>
						.And<CRShippingContact.bAccountContactID.IsEqual<Contact.contactID>>
						.And<CRShippingContact.revisionID.IsEqual<Contact.revisionID>>
						.And<CRShippingContact.isDefaultContact.IsEqual<boolTrue>>>
					.Where<
						Location.locationID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}
			else if (sender.GetValue<CROpportunity.bAccountID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.bAccountID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Contact>
					.LeftJoin<BAccount>
						.On<BAccount.defContactID.IsEqual<Contact.contactID>>
					.LeftJoin<CRShippingContact>
						.On<CRShippingContact.bAccountID.IsEqual<Contact.bAccountID>
						.And<CRShippingContact.bAccountContactID.IsEqual<Contact.contactID>>
						.And<CRShippingContact.revisionID.IsEqual<Contact.revisionID>>
						.And<CRShippingContact.isDefaultContact.IsEqual<boolTrue>>>
					.Where<
						BAccount.bAccountID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}

			return (view, new[] { parm });
		}
	}
}