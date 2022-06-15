using PX.Data;
using System;

namespace PX.Objects.PR.Standalone
{
	[PXCacheName("Payroll Deduction and Benefit Code")]
	[Serializable]
	public partial class PRDeductCode : IBqlTable
	{
		#region CodeID
		public abstract class codeID : PX.Data.BQL.BqlInt.Field<codeID> { }
		[PXDBIdentity]
		[PXUIField(DisplayName = "Code ID")]
		public int? CodeID { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true)]
		public virtual string CountryID { get; set; }
		#endregion
	}
}
