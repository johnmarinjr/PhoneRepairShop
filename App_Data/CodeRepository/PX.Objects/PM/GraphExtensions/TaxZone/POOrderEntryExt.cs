using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class POOrderEntryExt : PXGraphExtension<POOrderEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<POOrder.projectID>))]
		protected virtual void _(Events.CacheAttached<POOrder.taxZoneID> e) { }

		public static bool IsActive()
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>())
				return false;

			ProjectSettingsManager settings = new ProjectSettingsManager();
			return settings.CalculateProjectSpecificTaxes;
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(POOrder row,
		   Func<POOrder, string> baseMethod)
		{
			PMProject project = PMProject.PK.Find(Base, row?.ProjectID);
			if (project != null && !string.IsNullOrEmpty(project.CostTaxZoneID) && Base.RequireSingleProject((POOrder)row))
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
