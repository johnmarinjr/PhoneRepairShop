using PX.Data;
using PX.Payroll.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRPaymentTaxSplit)]
	[Serializable]
	public class PRPaymentTaxSplit : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRPaymentTaxSplit>.By<recordID>
		{
			public static PRPaymentTaxSplit Find(PXGraph graph, string field) => FindBy(graph, field);
		}

		public static class FK
		{
			public class Payment : PRPayment.PK.ForeignKeyOf<PRPaymentTaxSplit>.By<docType, refNbr> { }
			public class TaxCode : PRTaxCode.PK.ForeignKeyOf<PRPaymentTaxSplit>.By<taxID> { }
			public class PaymentTax : PRPaymentTax.PK.ForeignKeyOf<PRPaymentTaxSplit>.By<docType, refNbr, taxID> { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID { get; set; }
		#endregion
		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Payment Doc. Type")]
		[PXDBDefault(typeof(PRPayment.docType))]
		public string DocType { get; set; }
		#endregion
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Ref. Number")]
		[PXDBDefault(typeof(PRPayment.refNbr))]
		[PXParent(typeof(Select<PRPayment, Where<PRPayment.docType, Equal<Current<PRPaymentTaxSplit.docType>>, And<PRPayment.refNbr, Equal<Current<PRPaymentTaxSplit.refNbr>>>>>))]
		public String RefNbr { get; set; }
		#endregion
		#region TaxID
		public abstract class taxID : PX.Data.BQL.BqlInt.Field<taxID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Code", Enabled = false)]
		[PXSelector(typeof(PRTaxCode.taxID), DescriptionField = typeof(PRTaxCode.description), SubstituteKey = typeof(PRTaxCode.taxCD))]
		[PXDBDefault(typeof(PRPaymentTax.taxID), DefaultForUpdate = false)]
		[PXParent(typeof(Select<PRPaymentTax,
							Where<PRPaymentTax.docType,
								Equal<Current<PRPaymentTaxSplit.docType>>,
							And<PRPaymentTax.refNbr,
								Equal<Current<PRPaymentTaxSplit.refNbr>>,
							And<PRPaymentTax.taxID,
								Equal<Current<PRPaymentTaxSplit.taxID>>>>>>), ParentCreate = true)]
		[PXCheckUnique(typeof(docType), typeof(refNbr), typeof(wageType))]
		public int? TaxID { get; set; }
		#endregion
		#region WageType
		public abstract class wageType : PX.Data.BQL.BqlInt.Field<wageType> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Type", Enabled = false)]
		[TaxSplitWageTypeList]
		public int? WageType { get; set; }
		#endregion
		#region WageBaseAmount
		public abstract class wageBaseAmount : PX.Data.BQL.BqlDecimal.Field<wageBaseAmount> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Wages")]
		[PXFormula(null, typeof(SumCalc<PRPaymentTax.wageBaseAmount>))]
		public decimal? WageBaseAmount { get; set; }
		#endregion

		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXString]
		[PXUnboundDefault(typeof(Selector<taxID, PRTaxCode.countryID>))]
		public string CountryID { get; set; }
		#endregion

		#region System Columns
		#region TStamp
		public abstract class tStamp : PX.Data.BQL.BqlByteArray.Field<tStamp> { }
		[PXDBTimestamp]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion

		#region Obsolete fields
		#region TaxAmount
		[Obsolete("Use PRPaymentTax.TaxAmount instead.")]
		public abstract class taxAmount : PX.Data.BQL.BqlDecimal.Field<taxAmount> { }
		[Obsolete("Use PRPaymentTax.TaxAmount instead.")]
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public decimal? TaxAmount { get; set; }
		#endregion
		#region WageBaseHours
		[Obsolete("Use PRPaymentTax.WageBaseHours instead.")]
		public abstract class wageBaseHours : PX.Data.BQL.BqlDecimal.Field<wageBaseHours> { }
		[Obsolete("Use PRPaymentTax.WageBaseHours instead.")]
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public decimal? WageBaseHours { get; set; }
		#endregion
		#region WageBaseGrossAmt
		[Obsolete("Use PRPaymentTax.WageBaseGrossAmt instead.")]
		public abstract class wageBaseGrossAmt : PX.Data.BQL.BqlDecimal.Field<wageBaseGrossAmt> { }
		[Obsolete("Use PRPaymentTax.WageBaseGrossAmt instead.")]
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public decimal? WageBaseGrossAmt { get; set; }
		#endregion
		#region SubjectCommissionAmount
		[Obsolete]
		public abstract class subjectCommissionAmount : PX.Data.BQL.BqlDecimal.Field<subjectCommissionAmount> { }
		[Obsolete]
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public decimal? SubjectCommissionAmount { get; set; }
		#endregion
		#endregion
	}
}
