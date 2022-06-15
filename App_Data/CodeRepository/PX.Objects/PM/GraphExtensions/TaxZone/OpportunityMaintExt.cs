using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class OpportunityMaintExt : ProjectRevenueTaxZoneExtension<OpportunityMaint>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<CROpportunity.projectID>))]
		protected virtual void _(Events.CacheAttached<CROpportunity.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt(BqlField = typeof(CR.Standalone.CROpportunityRevision.shipAddressID))]
		[CRShippingAddress2(typeof(Select<Address, Where<True, Equal<False>>>))]
		protected virtual void _(Events.CacheAttached<CROpportunity.shipAddressID> e)
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
			return new DocumentMapping(typeof(CROpportunity))
			{
				ProjectID = typeof(CROpportunity.projectID)
			};
		}

		protected override void SetDefaultShipToAddress(PXCache sender, Document row)
		{
			if (row.ProjectID == null || row.ProjectID == 0) return;

			PMProject project = PMProject.PK.Find(sender.Graph, row.ProjectID);
			if (project == null || project.NonProject == true) return;

			CRShippingAddress2Attribute.DefaultRecord<CROpportunity.shipAddressID>(sender, row);
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(CROpportunity row, Func<CROpportunity, string> baseMethod)
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
