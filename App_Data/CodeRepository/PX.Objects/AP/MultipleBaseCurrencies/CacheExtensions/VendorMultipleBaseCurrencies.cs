using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class VendorMultipleBaseCurrencies : PXCacheExtension<VendorVisibilityRestriction, Vendor>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region PayToVendorID
		[PXRestrictor(typeof(Where<Vendor.baseCuryID, Equal<Vendor.baseCuryID.FromCurrent>>),"")]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public int? PayToVendorID { get; set; }
		#endregion
	}
}
