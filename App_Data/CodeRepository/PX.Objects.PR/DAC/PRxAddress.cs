using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.PR
{
	public sealed class PRxAddress : PXCacheExtension<Address>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region PsdCode
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "PSD Code")]
		public string PsdCode { get; set; }
		public abstract class psdCode : BqlString.Field<psdCode> { }
		#endregion
	}
}
