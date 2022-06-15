using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class QuoteMaintExt : ProjectRevenueTaxZoneExtension<QuoteMaint>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<CRQuote.projectID>))]
		protected virtual void _(Events.CacheAttached<CRQuote.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt(BqlField = typeof(CR.Standalone.CROpportunityRevision.shipAddressID))]
		[CRShippingAddress2(typeof(Select<Address, Where<True, Equal<False>>>))]
		protected virtual void _(Events.CacheAttached<CRQuote.shipAddressID> e) { }

		public static bool IsActive()
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>())
				return false;

			ProjectSettingsManager settings = new ProjectSettingsManager();
			return settings.CalculateProjectSpecificTaxes;
		}

		protected override DocumentMapping GetDocumentMapping()
		{
			return new DocumentMapping(typeof(CRQuote))
			{
				ProjectID = typeof(CRQuote.projectID)
			};
		}

		protected override void SetDefaultShipToAddress(PXCache sender, Document row)
		{
			CRShippingAddress2Attribute.DefaultRecord<CRQuote.shipAddressID>(sender, row);
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(CRQuote row,
			Func<CRQuote, string> baseMethod)
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
	}
}
