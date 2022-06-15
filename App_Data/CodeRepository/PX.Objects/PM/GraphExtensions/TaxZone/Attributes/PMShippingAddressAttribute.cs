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
	public class PMShippingAddress2Attribute : PMShippingAddressAttribute
	{
		public PMShippingAddress2Attribute(Type SelectType, Type customerID)
			: base(SelectType, customerID)
		{
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<PMShippingAddress, PMShippingAddress.addressID>(sender, DocumentRow, Row);
		}

		public override void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow)
		{
			int startRow = -1;
			int totalRows = 0;
			bool addressFound = false;

			int? projectID = (int?)sender.GetValue<PMProforma.projectID>(DocumentRow);
			if (projectID == null || projectID == 0)
			{
				DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow, ref startRow, ref totalRows, ref addressFound);
				return;
			}

			PMProject project = PMProject.PK.Find(sender.Graph, projectID);
			if (project == null || project.NonProject == true)
			{
				DefaultAddress<TAddress, TAddressID>(sender, DocumentRow, AddressRow, ref startRow, ref totalRows, ref addressFound);
				return;
			}

			object[] billingAddressPMParams = new object[] { project.BillAddressID };
			BqlCommand selectBillingAddressPM = BqlCommand.CreateInstance(
					typeof(SelectFrom<PMAddress>.Where<PMAddress.addressID.IsEqual<@P.AsInt>>));
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

				addressFound = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, new PXResult<Address, PMShippingAddress>(address, new PMShippingAddress()));
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
					Address address = new Address();
					addressFound = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, res);
					break;
				}
			}
		}
	}
}
