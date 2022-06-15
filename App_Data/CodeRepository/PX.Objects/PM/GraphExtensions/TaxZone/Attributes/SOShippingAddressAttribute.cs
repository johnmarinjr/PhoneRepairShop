using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.CR;
using System;
using System.Linq;
using PX.Objects.SO;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class SOShippingAddress2Attribute : SOShippingAddressAttribute
	{
		public SOShippingAddress2Attribute(Type SelectType)
			: base(SelectType)
		{
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<SOShippingAddress, SOShippingAddress.addressID>(sender, DocumentRow, Row);
		}

		public override void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow)
		{
			int startRow = -1;
			int totalRows = 0;
			bool addressFound = false;

			int? projectID = (int?)sender.GetValue<SOOrder.projectID>(DocumentRow);
			if (projectID == null || projectID == 0)
			{
				DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow, ref startRow, ref totalRows, ref addressFound);
				return;
			}

			PM.PMProject project = PM.PMProject.PK.Find(sender.Graph, projectID);
			if (project == null || project.NonProject == true)
			{
				DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow, ref startRow, ref totalRows, ref addressFound);
				return;
			}

			object[] billingAddressPMParams = new object[] { project.BillAddressID };
			BqlCommand selectBillingAddressPM = BqlCommand.CreateInstance(
					typeof(SelectFrom<PM.PMAddress>.Where<PM.PMAddress.addressID.IsEqual<@P.AsInt>>));
			PXView billingAddressView = sender.Graph.TypedViews.GetView(selectBillingAddressPM, false);


			PM.PMAddress billingAddress = (PM.PMAddress)billingAddressView.Select(null, billingAddressPMParams, null, null, null, null, ref startRow, 1, ref totalRows).FirstOrDefault();

			object[] shippingAddressPMParams = new object[] { project.SiteAddressID };
			BqlCommand shippingAddressPMCommand = BqlCommand.CreateInstance(
					typeof(SelectFrom<PM.PMAddress>.Where<PM.PMAddress.addressID.IsEqual<@P.AsInt>>));
			PXView shippingAddressPMView = sender.Graph.TypedViews.GetView(shippingAddressPMCommand, false);

			PM.PMAddress shippingAddressPM = (PM.PMAddress)shippingAddressPMView.Select(null, shippingAddressPMParams, null, null, null, null, ref startRow, 1, ref totalRows).FirstOrDefault();

			if (shippingAddressPM != null && shippingAddressPM.IsDefaultAddress != true)
			{
				Address address = PropertyTransfer.Transfer(shippingAddressPM, new Address());
				address.AddressID = shippingAddressPM.AddressID;

				address.IsValidated = shippingAddressPM.IsValidated;
				address.BAccountID = billingAddress.BAccountID;

				addressFound = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, new PXResult<Address, SOShippingAddress>(address, new SOShippingAddress()));
			}

			if (!addressFound && !_Required)
				this.ClearRecord(sender, DocumentRow);
		}

		private void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow, ref int startRow, ref int totalRows, ref bool addressFound)
			where TAddress : class, IBqlTable, IAddress, new()
			where TAddressID : IBqlField
		{
			var view = sender.Graph.TypedViews.GetView(_Select, false);
			var addresses = view.Select(new object[] { DocumentRow }, null, null, null, null, null, ref startRow, 1, ref totalRows);
			if (addresses.Any())
			{
				foreach (PXResult res in view.Select(new object[] { DocumentRow }, null, null, null, null, null, ref startRow, 1, ref totalRows))
				{
					addressFound = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, res);
					break;
				}
			}
		}
	}
}
