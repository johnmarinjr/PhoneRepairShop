using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class SVATTaxFilterVisibilityRestriction : PXCacheExtension<SVATTaxFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region TaxAgencyID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByOrganization(orgBAccountID: typeof(SVATTaxFilter.orgBAccountID))]
		public int? TaxAgencyID { get; set; }
		#endregion
	}
}