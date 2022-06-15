using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PREarningTypeDetail)]
	[Serializable]
	public class PREarningTypeDetail : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREarningTypeDetail>.By<typecd, taxID, countryID>
		{
			public static PREarningTypeDetail Find(PXGraph graph, string typeCD, int? taxID, string countryID) =>
				FindBy(graph, typeCD, taxID, countryID);
		}

		public static class FK
		{
			public class EarningType : EPEarningType.PK.ForeignKeyOf<PREarningTypeDetail>.By<typecd> { }
			public class TaxCode : PRTaxCode.PK.ForeignKeyOf<PREarningTypeDetail>.By<taxID> { }
		}
		#endregion

		#region TypeCD
		public abstract class typecd : BqlString.Field<typecd> { }
		[PXDBString(EPEarningType.typeCD.Length, IsKey = true, IsUnicode = true)]
		[PXDefault(typeof(EPEarningType.typeCD))]
		[PXUIField(DisplayName = "Earning Type Code")]
		[PXParent(typeof(FK.EarningType))]
		public string TypeCD { get; set; }
		#endregion
		#region TaxID
		public abstract class taxID : BqlInt.Field<taxID> { }
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Tax Code")]
		[PXSelector(typeof(
			SearchFor<PRTaxCode.taxID>
				.Where<PRTaxCode.countryID.IsEqual<countryID.FromCurrent>>),
			DescriptionField = typeof(PRTaxCode.description),
			SubstituteKey = typeof(PRTaxCode.taxCD))]
		[PXParent(typeof(FK.TaxCode))]
		public int? TaxID { get; set; }
		#endregion
		#region Taxability
		public abstract class taxability : PX.Data.BQL.BqlInt.Field<taxability> { }
		[PXDBInt]
		[EarningTypeTaxability(typeof(countryID), typeof(taxID))]
		public int? Taxability { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PRCountry]
		public string CountryID { get; set; }
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
