using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.CR;
using CRLocation = PX.Objects.CR.Standalone.Location;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class SOOrderEntryExt : ProjectRevenueTaxZoneExtension<SOOrderEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<SOOrder.projectID>))]
		protected virtual void _(Events.CacheAttached<SOOrder.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt()]
		[SOShippingAddress2(typeof(
					Select2<Address,
						InnerJoin<CRLocation,
				  On<CRLocation.bAccountID, Equal<Address.bAccountID>,
				 And<Address.addressID, Equal<CRLocation.defAddressID>,
					 And<CRLocation.bAccountID, Equal<Current<SOOrder.customerID>>,
							And<CRLocation.locationID, Equal<Current<SOOrder.customerLocationID>>>>>>,
						LeftJoin<SOShippingAddress,
							On<SOShippingAddress.customerID, Equal<Address.bAccountID>,
							And<SOShippingAddress.customerAddressID, Equal<Address.addressID>,
							And<SOShippingAddress.revisionID, Equal<Address.revisionID>,
							And<SOShippingAddress.isDefaultAddress, Equal<True>>>>>>>,
						Where<True, Equal<True>>>))]
		protected virtual void _(Events.CacheAttached<SOOrder.shipAddressID> e)
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
			return new DocumentMapping(typeof(SOOrder))
			{
				ProjectID = typeof(SOOrder.projectID)
			};
		}

		protected override void SetDefaultShipToAddress(PXCache sender, Document row)
		{
			SOShippingAddress2Attribute.DefaultRecord<SOOrder.shipAddressID>(sender, row);
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(SOOrder row,
			Func<SOOrder, string> baseMethod)
		{
			//Do not redefault if value exists and overide flag is ON:
			if (row != null && row.OverrideTaxZone == true)
			{
				return row.TaxZoneID;
			}

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
	}

}
