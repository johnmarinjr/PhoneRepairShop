using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CR;
using ARCashSale = PX.Objects.AR.Standalone.ARCashSale;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class ARCashSaleEntryExt : ProjectRevenueTaxZoneExtension<ARCashSaleEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<ARCashSale.projectID>))]
		protected virtual void _(Events.CacheAttached<ARCashSale.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt(BqlField = typeof(ARInvoice.shipAddressID))]
		[ARShippingAddress2(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
				And<CR.Standalone.Location.locationID, Equal<Current<AR.Standalone.ARCashSale.customerLocationID>>>>,
			InnerJoin<Address, On<Address.bAccountID, Equal<Customer.bAccountID>,
				And<Address.addressID, Equal<CR.Location.defAddressID>>>,
			LeftJoin<ARShippingAddress, On<ARShippingAddress.customerID, Equal<Address.bAccountID>,
				And<ARShippingAddress.customerAddressID, Equal<Address.addressID>,
				And<ARShippingAddress.revisionID, Equal<Address.revisionID>,
				And<ARShippingAddress.isDefaultBillAddress, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<AR.Standalone.ARCashSale.customerID>>>>))]
		protected virtual void _(Events.CacheAttached<AR.Standalone.ARCashSale.shipAddressID> e)
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
			return new DocumentMapping(typeof(ARCashSale))
			{
				ProjectID = typeof(ARCashSale.projectID)
			};
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(ARCashSale row,
			Func<ARCashSale, string> baseMethod)
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
