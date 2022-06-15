using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.CR;
using System;
using System.Linq;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class CRShippingAddress2Attribute : CRShippingAddressAttribute
	{
		public CRShippingAddress2Attribute(Type SelectType)
			: base(SelectType)
		{
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<CRShippingAddress, CRShippingAddress.addressID>(sender, DocumentRow, Row);
		}

		public override void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow)
		{
			int startRow = -1;
			int totalRows = 0;
			bool addressFound = false;

			int? projectID = (int?)sender.GetValue(DocumentRow, "ProjectID");
			if (projectID == null || projectID == 0)
			{
				base.DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow);
				return;
			}

			PMProject project = PMProject.PK.Find(sender.Graph, projectID);
			if (project == null || project.NonProject == true)
			{
				base.DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow);
				return;
			}

			object[] billingAddressPMParams = new object[] { project.BillAddressID };
			BqlCommand selectBillingAddressPM = BqlCommand.CreateInstance(
					typeof(SelectFrom<PM.PMAddress>.Where<PM.PMAddress.addressID.IsEqual<@P.AsInt>>));
			PXView billingAddressView = sender.Graph.TypedViews.GetView(selectBillingAddressPM, false);


			PMAddress billingAddress = (PMAddress)billingAddressView.Select(null, billingAddressPMParams, null, null, null, null, ref startRow, 1, ref totalRows).FirstOrDefault();

			object[] shippingAddressPMParams = new object[] { project.SiteAddressID };
			BqlCommand shippingAddressPMCommand = BqlCommand.CreateInstance(
					typeof(SelectFrom<PMAddress>.Where<PMAddress.addressID.IsEqual<@P.AsInt>>));
			PXView shippingAddressPMView = sender.Graph.TypedViews.GetView(shippingAddressPMCommand, false);

			PMAddress shippingAddressPM = (PMAddress)shippingAddressPMView.Select(null, shippingAddressPMParams, null, null, null, null, ref startRow, 1, ref totalRows).FirstOrDefault();

			if (shippingAddressPM != null && shippingAddressPM.IsDefaultAddress != true)
			{
				Address address = PropertyTransfer.Transfer(shippingAddressPM, new Address());
				address.AddressID = shippingAddressPM.AddressID;

				address.IsValidated = shippingAddressPM.IsValidated;
				address.BAccountID = billingAddress.BAccountID;

				addressFound = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, new PXResult<Address, CRShippingAddress>(address, new CRShippingAddress()));
			}

			if (!addressFound && !_Required)
				this.ClearRecord(sender, DocumentRow);
		}
	}
}
