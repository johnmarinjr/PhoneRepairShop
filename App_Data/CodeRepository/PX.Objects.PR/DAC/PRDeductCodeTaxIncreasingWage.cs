using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRDeductCodeTaxIncreasingWage)]
	[Serializable]
	public class PRDeductCodeTaxIncreasingWage : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRDeductCodeTaxIncreasingWage>.By<deductCodeID, applicableTaxID>
		{
			public static PRDeductCodeTaxIncreasingWage Find(PXGraph graph, int? deductCodeID, int? applicableTaxID) =>
				FindBy(graph, deductCodeID, applicableTaxID);
		}

		public static class FK
		{
			public class DeductionCode : PRDeductCode.PK.ForeignKeyOf<PRDeductCodeTaxIncreasingWage>.By<deductCodeID> { }
			public class TaxCode : PRTaxCode.PK.ForeignKeyOf<PRDeductCodeTaxIncreasingWage>.By<applicableTaxID> { }
		}
		#endregion

		#region DeductCodeID
		public abstract class deductCodeID : BqlInt.Field<deductCodeID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(PRDeductCode.codeID))]
		[PXParent(typeof(Select<PRDeductCode, Where<PRDeductCode.codeID, Equal<Current<deductCodeID>>>>))]
		public virtual int? DeductCodeID { get; set; }
		#endregion
		#region ApplicableTaxID
		public abstract class applicableTaxID : BqlInt.Field<applicableTaxID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Tax Code")]
		[PXSelector(typeof(SearchFor<PRTaxCode.taxID>
				.Where<PRTaxCode.taxCategory.IsEqual<TaxCategory.employerTax>
					.And<PRTaxCode.countryID.IsEqual<PRDeductCode.countryID.FromCurrent>>>),
			SubstituteKey = typeof(PRTaxCode.taxCD),
			DescriptionField = typeof(PRTaxCode.description))]
		[PXForeignReference(typeof(Field<applicableTaxID>.IsRelatedTo<PRTaxCode.taxID>))]
		public virtual int? ApplicableTaxID { get; set; }
		#endregion
		#region System Columns
		#region TStamp
		public abstract class tStamp : BqlByteArray.Field<tStamp> { }
		[PXDBTimestamp]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
