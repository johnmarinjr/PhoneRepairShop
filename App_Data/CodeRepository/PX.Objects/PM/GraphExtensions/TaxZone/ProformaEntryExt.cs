using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CR;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class ProformaEntryExt : ProjectRevenueTaxZoneExtension<ProformaEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<PMProforma.projectID, PMProforma.locationID>))]
		protected virtual void _(Events.CacheAttached<PMProforma.taxZoneID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		[PMShippingAddress2(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
				And<CR.Standalone.Location.locationID, Equal<Current<PMProforma.locationID>>>>,
			InnerJoin<Address, On<Address.bAccountID, Equal<Customer.bAccountID>,
				And<Address.addressID, Equal<CR.Location.defAddressID>>>,
			LeftJoin<PMShippingAddress, On<PMShippingAddress.customerID, Equal<Address.bAccountID>,
				And<PMShippingAddress.customerAddressID, Equal<Address.addressID>,
				And<PMShippingAddress.revisionID, Equal<Address.revisionID>,
				And<PMShippingAddress.isDefaultBillAddress, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<PMProforma.customerID>>>>), typeof(PMProforma.customerID))]
		protected virtual void _(Events.CacheAttached<PMProforma.shipAddressID> e)
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
			return new DocumentMapping(typeof(PMProforma))
			{
				ProjectID = typeof(PMProforma.projectID)
			};
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(PMProforma row,
			Func<PMProforma, string> baseMethod)
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
			PMShippingAddress2Attribute.DefaultRecord<CROpportunity.shipAddressID>(sender, row);
		}
	}
}
