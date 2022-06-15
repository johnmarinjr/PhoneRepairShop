using PX.Data;
using System;

namespace PX.Objects.PR.Standalone
{
	[PXCacheName("Payroll Tax Code")]
	[Serializable]
	public partial class PRTaxCode : IBqlTable
	{
		#region TaxID
		public abstract class taxID : PX.Data.BQL.BqlInt.Field<taxID> { }
		[PXDBIdentity]
		public int? TaxID { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true)]
		public virtual string CountryID { get; set; }
		#endregion
	}
}
