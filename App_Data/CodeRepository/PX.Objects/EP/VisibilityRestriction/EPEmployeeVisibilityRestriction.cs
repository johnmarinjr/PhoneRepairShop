using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.EP
{
	public sealed class EPEmployeeVisibilityRestriction: PXCacheExtension<EPEmployee>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorClassID
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXDefault]
		[PXUIField(DisplayName = "Employee Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<EPEmployeeClass.vendorClassID,
			Where<EPEmployeeClass.orgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>),
			DescriptionField = typeof(VendorClass.descr), CacheGlobal = true)]
		public string VendorClassID { get; set; }
		#endregion
	}
}
