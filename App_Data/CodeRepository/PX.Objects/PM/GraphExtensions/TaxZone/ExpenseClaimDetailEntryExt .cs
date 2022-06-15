using PX.Data;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public class ExpenseClaimDetailEntryExt : PXGraphExtension<ExpenseClaimDetailEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Default<EPExpenseClaimDetails.contractID>))]
		protected virtual void EPExpenseClaimDetails_TaxZoneID_CacheAttached(PXCache cache)
		{
		}

		public static bool IsActive()
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>())
				return false;

			ProjectSettingsManager settings = new ProjectSettingsManager();
			return settings.CalculateProjectSpecificTaxes;
		}

		[PXOverride]
		public virtual string GetDefaultTaxZone(EPExpenseClaimDetails row, Func<EPExpenseClaimDetails, string> baseMethod)
		{
			PMProject project = PMProject.PK.Find(Base, row?.ContractID);
			if (project != null && !string.IsNullOrEmpty(project.CostTaxZoneID))
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
