using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRTaxWebServiceData)]
	[Serializable]
	public class PRTaxWebServiceData : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRTaxWebServiceData>.By<countryID>
		{
			public static PRTaxWebServiceData Find(PXGraph graph, string countryID) => 
				FindBy(graph, countryID);
		}
		#endregion

		#region CountryID
		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXDefault]
		public virtual string CountryID { get; set; }
		public abstract class countryID : BqlString.Field<countryID> { }
		#endregion
		#region TaxSettings
		[PXDBString]
		public virtual string TaxSettings { get; set; }
		public abstract class taxSettings : BqlString.Field<taxSettings> { }
		#endregion
		#region DeductionTypes
		[PXDBString]
		public virtual string DeductionTypes { get; set; }
		public abstract class deductionTypes : BqlString.Field<deductionTypes> { }
		#endregion
		#region WageTypes
		[PXDBString]
		public virtual string WageTypes { get; set; }
		public abstract class wageTypes : BqlString.Field<wageTypes> { }
		#endregion
		#region ReportingTypes
		[PXDBString]
		public virtual string ReportingTypes { get; set; }
		public abstract class reportingTypes : BqlString.Field<reportingTypes> { }
		#endregion
		#region States
		[PXDBString]
		public virtual string States { get; set; }
		public abstract class states : BqlString.Field<states> { }
		#endregion

		#region System Columns
		#region TStamp
		public class tStamp : IBqlField { }
		[PXDBTimestamp()]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public class createdByID : IBqlField { }
		[PXDBCreatedByID()]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public class createdByScreenID : IBqlField { }
		[PXDBCreatedByScreenID()]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public class createdDateTime : IBqlField { }
		[PXDBCreatedDateTime()]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public class lastModifiedByID : IBqlField { }
		[PXDBLastModifiedByID()]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public class lastModifiedByScreenID : IBqlField { }
		[PXDBLastModifiedByScreenID()]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public class lastModifiedDateTime : IBqlField { }
		[PXDBLastModifiedDateTime()]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}