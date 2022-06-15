using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class APInvoiceEntryExt : PXGraphExtension<APInvoiceEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<APInvoice.projectID>))]
		protected virtual void _(Events.CacheAttached<APInvoice.taxZoneID> e) { }

		public static bool IsActive()
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>())
				return false;

			ProjectSettingsManager settings = new ProjectSettingsManager();
			return settings.CalculateProjectSpecificTaxes;
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(APInvoice row,
		   Func<APInvoice, string> baseMethod)
		{
			PMProject project = PMProject.PK.Find(Base, row?.ProjectID);
			if (project != null && !string.IsNullOrEmpty(project.CostTaxZoneID) && Base.apsetup.Current.RequireSingleProjectPerDocument == true)
			{
				return project.CostTaxZoneID;
			}
			else
			{
				return baseMethod(row);
			}
		}
	}
}
