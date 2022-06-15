using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CR;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class ARInvoiceEntryExt : ProjectRevenueTaxZoneExtension<ARInvoiceEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<ARInvoice.projectID>))]
		protected virtual void _(Events.CacheAttached<ARInvoice.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		[ARShippingAddress2(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
				And<CR.Standalone.Location.locationID, Equal<Current<ARInvoice.customerLocationID>>>>,
			InnerJoin<Address, On<Address.bAccountID, Equal<Customer.bAccountID>,
				And<Address.addressID, Equal<CR.Location.defAddressID>>>,
			LeftJoin<ARShippingAddress, On<ARShippingAddress.customerID, Equal<Address.bAccountID>,
				And<ARShippingAddress.customerAddressID, Equal<Address.addressID>,
				And<ARShippingAddress.revisionID, Equal<Address.revisionID>,
				And<ARShippingAddress.isDefaultBillAddress, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<ARInvoice.customerID>>>>))]
		protected virtual void _(Events.CacheAttached<ARInvoice.shipAddressID> e)
		{
		}

		public static bool IsActive()
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>())
				return false;

			ProjectSettingsManager settings = new ProjectSettingsManager();
			return settings.CalculateProjectSpecificTaxes;
		}

		protected override DocumentMapping GetDocumentMapping()
		{
			return new DocumentMapping(typeof(ARInvoice))
			{
				ProjectID = typeof(ARInvoice.projectID)
			};
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(ARInvoice row,
			Func<ARInvoice, string> baseMethod)
		{
			PMProject project = PMProject.PK.Find(Base, row?.ProjectID);
			if (project != null && !string.IsNullOrEmpty(project.RevenueTaxZoneID))
			{
				return project.RevenueTaxZoneID;
			}
			else
			{
				return baseMethod(row);
			}
		}

		protected override void SetDefaultShipToAddress(PXCache sender, Document row)
		{
			ARShippingAddress2Attribute.DefaultRecord<ARInvoice.shipAddressID>(sender, row);
		}
	}
}
