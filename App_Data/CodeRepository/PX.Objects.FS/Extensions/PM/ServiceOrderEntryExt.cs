using PX.Data;
using PX.Objects.CS;
using PX.Objects.FS;
using PX.Objects.TX;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class ServiceOrderEntryExt : ProjectRevenueTaxZoneExtension<ServiceOrderEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<FSServiceOrder.projectID>))]
		protected virtual void _(Events.CacheAttached<FSServiceOrder.taxZoneID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		[FSSrvOrdAddress2(typeof(Select<
			CR.Address,
			Where<True, Equal<False>>>))]
		protected virtual void _(Events.CacheAttached<FSServiceOrder.serviceOrderAddressID> e)
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
			return new DocumentMapping(typeof(FSServiceOrder))
			{
				ProjectID = typeof(FSServiceOrder.projectID)
			};
		}

		protected override void SetDefaultShipToAddress(PXCache sender, Document row)
		{
			FSSrvOrdAddress2Attribute.DefaultRecord<FSServiceOrder.serviceOrderAddressID>(sender, row);
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(FSServiceOrder row, Func<FSServiceOrder, string> baseMethod)
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
