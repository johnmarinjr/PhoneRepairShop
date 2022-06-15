using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	public class CROpportunityAddressAttribute : CRAddressAttribute, IPXRowUpdatedSubscriber
	{
		public CROpportunityAddressAttribute(Type SelectType)
			: base(typeof(CRAddress.addressID), typeof(CRAddress.isDefaultAddress), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<CRAddress.overrideAddress>(Record_Override_FieldVerifying);
		}

		protected override (PXView, object[]) GetViewWithParameters(PXCache sender, object DocumentRow, object ContactRow)
		{
			PXView view = null;
			object parm = null;

			if (sender.GetValue<CROpportunity.contactID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.contactID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Address>
					.LeftJoin<Contact>
						.On<Contact.defAddressID.IsEqual<Address.addressID>>
					.LeftJoin<CRAddress>
						.On<CRAddress.bAccountID.IsEqual<Address.bAccountID>
						.And<CRAddress.bAccountAddressID.IsEqual<Address.addressID>>
						.And<CRAddress.revisionID.IsEqual<Address.revisionID>>
						.And<CRAddress.isDefaultAddress.IsEqual<boolTrue>>>
					.Where<
						Contact.contactID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}
			else if (sender.GetValue<CROpportunity.bAccountID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.bAccountID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Address>
					.LeftJoin<BAccount>
						.On<BAccount.defAddressID.IsEqual<Address.addressID>>
					.LeftJoin<CRAddress>
						.On<CRAddress.bAccountID.IsEqual<Address.bAccountID>
						.And<CRAddress.bAccountAddressID.IsEqual<Address.addressID>>
						.And<CRAddress.revisionID.IsEqual<Address.revisionID>>
						.And<CRAddress.isDefaultAddress.IsEqual<boolTrue>>>
					.Where<
						BAccount.bAccountID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}
			else if (sender.GetValue<CROpportunity.locationID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.locationID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Address>
					.LeftJoin<Location>
						.On<Location.defAddressID.IsEqual<Address.addressID>>
					.LeftJoin<CRAddress>
						.On<CRAddress.bAccountID.IsEqual<Address.bAccountID>
						.And<CRAddress.bAccountAddressID.IsEqual<Address.addressID>>
						.And<CRAddress.revisionID.IsEqual<Address.revisionID>>
						.And<CRAddress.isDefaultAddress.IsEqual<boolTrue>>>
					.Where<
						Location.locationID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}

			return (view, new[] { parm });
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<CRAddress, CRAddress.addressID>(sender, DocumentRow, Row);
		}

		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyAddress<CRAddress, CRAddress.addressID>(sender, DocumentRow, SourceRow, clone);
		}

		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Address_IsDefaultAddress_FieldVerifying<CRAddress>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<CRAddress.overrideAddress>(sender, e.Row, true);
				PXUIFieldAttribute.SetEnabled<CRAddress.isValidated>(sender, e.Row, false);
			}
		}       
	}
}