using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.PM;

namespace PX.Objects.PR
{
	[PXPrimaryGraph(typeof(PRWorkCodeMaint))]
	public sealed class PRxPMWorkCode : PXCacheExtension<PMWorkCode>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region Keys
		public static class FK
		{
			public class Country : CS.Country.PK.ForeignKeyOf<PMWorkCode>.By<countryID> { }
		}
		#endregion

		#region CountryID
		public abstract class countryID : BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true)]
		[PRCountry]
		[PXUIField(Visible = false)]
		public string CountryID { get; set; }
		#endregion
	}
}
