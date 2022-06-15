using PX.Data.BQL;
using PX.Payroll.Data;

namespace PX.Objects.PR
{
	public static class BQLLocationConstants
	{
		public class FederalUS : BqlString.Constant<FederalUS>
		{
			public FederalUS() : base(LocationConstants.USFederalStateCode) { }
		}

		public class FederalCAN : BqlString.Constant<FederalCAN>
		{
			public FederalCAN() : base(LocationConstants.CanadaFederalStateCode) { }
		}

		public class CountryUS : BqlString.Constant<CountryUS>
		{
			public CountryUS() : base(LocationConstants.USCountryCode) { }
		}

		public class CountryCAN : BqlString.Constant<CountryCAN>
		{
			public CountryCAN() : base(LocationConstants.CanadaCountryCode) { }
		}
	}
}
