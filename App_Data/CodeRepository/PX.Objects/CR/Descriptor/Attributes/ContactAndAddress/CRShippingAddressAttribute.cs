using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	public class CRShippingAddressAttribute : CRAddressAttribute
	{
		public CRShippingAddressAttribute(Type SelectType)
			: base(typeof(CRShippingAddress.addressID), typeof(CRShippingAddress.isDefaultAddress), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<CRShippingAddress.overrideAddress>(Record_Override_FieldVerifying);
			sender.Graph.RowInserted.AddHandler<CRShippingAddress>(CRShippingAddress_RowInserted);
		}


		protected virtual void CRShippingAddress_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			CRShippingAddress shipAddress = e.Row as CRShippingAddress;
			if (shipAddress == null) return;

			if (shipAddress.OverrideAddress == true)
			{
				shipAddress.IsValidated = false;
			}
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<CRShippingAddress, CRShippingAddress.addressID>(sender, DocumentRow, Row);
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyAddress<CRShippingAddress, CRShippingAddress.addressID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var newValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			Address_IsDefaultAddress_FieldVerifying<CRShippingAddress>(sender, new PXFieldVerifyingEventArgs(e.Row, newValue, e.ExternalCall));
		}

		protected override (PXView, object[]) GetViewWithParameters(PXCache sender, object DocumentRow, object ContactRow)
		{
			PXView view = null;
			object parm = null;

			if (sender.GetValue<CROpportunity.locationID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.locationID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Address>
					.LeftJoin<Location>
						.On<Location.defAddressID.IsEqual<Address.addressID>>
					.LeftJoin<CRShippingAddress>
						.On<CRShippingAddress.bAccountID.IsEqual<Address.bAccountID>
						.And<CRShippingAddress.bAccountAddressID.IsEqual<Address.addressID>>
						.And<CRShippingAddress.revisionID.IsEqual<Address.revisionID>>
						.And<CRShippingAddress.isDefaultAddress.IsEqual<boolTrue>>>
					.Where<
						Location.locationID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}
			else if (sender.GetValue<CROpportunity.bAccountID>(DocumentRow) != null)
			{
				parm = sender.GetValue<CROpportunity.bAccountID>(DocumentRow);

				BqlCommand Select = new SelectFrom<Address>
					.LeftJoin<BAccount>
						.On<BAccount.bAccountID.IsEqual<Address.bAccountID>
						.And<BAccount.defAddressID.IsEqual<Address.addressID>>>
					.LeftJoin<CRShippingAddress>
						.On<CRShippingAddress.bAccountID.IsEqual<Address.bAccountID>
						.And<CRShippingAddress.bAccountAddressID.IsEqual<Address.addressID>>
						.And<CRShippingAddress.revisionID.IsEqual<Address.revisionID>>
						.And<CRShippingAddress.isDefaultAddress.IsEqual<boolTrue>>>
					.Where<
						BAccount.bAccountID.IsEqual<@P.AsInt>>();

				view = sender.Graph.TypedViews.GetView(Select, false);
			}

			return (view, new[] { parm });
		}
	}
}
