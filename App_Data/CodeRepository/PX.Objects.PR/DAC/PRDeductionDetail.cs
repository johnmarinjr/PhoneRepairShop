using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.GL;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRDeductionDetails)]
	[Serializable]
	public class PRDeductionDetail : IBqlTable, IPaycheckDetail<int?>
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRDeductionDetail>.By<recordID>
		{
			public static PRDeductionDetail Find(PXGraph graph, int? recordID) => FindBy(graph, recordID);
		}

		public class UK : PrimaryKeyOf<PRDeductionDetail>.By<branchID, paymentDocType, paymentRefNbr, codeID>
		{
			public static PRDeductionDetail Find(PXGraph graph, int? branchID, string paymentDocType, string paymentRefNbr, int? codeID) => 
				FindBy(graph, branchID, paymentDocType, paymentRefNbr, codeID);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PRDeductionDetail>.By<employeeID> { }
			public class PayrollBatch : PRBatch.PK.ForeignKeyOf<PRDeductionDetail>.By<batchNbr> { }
			public class Payment : PRPayment.PK.ForeignKeyOf<PRDeductionDetail>.By<paymentDocType, paymentRefNbr> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<PRDeductionDetail>.By<branchID> { }
			public class DeductionCode : PRDeductCode.PK.ForeignKeyOf<PRDeductionDetail>.By<codeID> { }
			public class LiabilityAccount : Account.PK.ForeignKeyOf<PRDeductionDetail>.By<accountID> { }
			public class LiabilitySubaccount : Sub.PK.ForeignKeyOf<PRDeductionDetail>.By<subID> { }
			public class Invoice : APInvoice.PK.ForeignKeyOf<PRDeductionDetail>.By<apInvoiceDocType, apInvoiceRefNbr> { }
			public class PaymentDeduction : PRPaymentDeduct.PK.ForeignKeyOf<PRDeductionDetail>.By<paymentDocType, paymentRefNbr, codeID> { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID { get; set; }
		#endregion
		#region OriginalRecordID
		public abstract class originalRecordID : PX.Data.BQL.BqlInt.Field<originalRecordID> { }
		[PXDBInt]
		public virtual Int32? OriginalRecordID { get; set; }
		#endregion
		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		[Employee]
		[PXDefault(typeof(PRPayment.employeeID.FromCurrent))]
		public int? EmployeeID { get; set; }
		#endregion
		#region BatchNbr
		public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Batch Number")]
		[PXDBDefault(typeof(PRBatch.batchNbr), DefaultForUpdate = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXParent(typeof(Select<PRBatch, Where<PRBatch.batchNbr, Equal<Current<PRDeductionDetail.batchNbr>>>>))]
		public string BatchNbr { get; set; }
		#endregion
		#region PaymentDocType
		public abstract class paymentDocType : PX.Data.BQL.BqlString.Field<paymentDocType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Payment Doc. Type")]
		[PXDBDefault(typeof(PRPayment.docType))]
		public string PaymentDocType { get; set; }
		#endregion
		#region PaymentRefNbr
		public abstract class paymentRefNbr : PX.Data.BQL.BqlString.Field<paymentRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Ref. Number")]
		[PXDBDefault(typeof(PRPayment.refNbr))]
		[PXParent(typeof(Select<PRPayment, Where<PRPayment.docType, Equal<Current<PRDeductionDetail.paymentDocType>>, And<PRPayment.refNbr, Equal<Current<PRDeductionDetail.paymentRefNbr>>>>>))]
		public string PaymentRefNbr { get; set; }
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[GL.Branch(typeof(Parent<PRPayment.branchID>), IsDetail = false)]
		public int? BranchID { get; set; }
		#endregion
		#region CodeID
		public abstract class codeID : PX.Data.BQL.BqlInt.Field<codeID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Code", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault]
		[PXSelector(
			typeof(SearchFor<PRDeductCode.codeID>
				.Where<paymentDocType.FromCurrent.IsEqual<PayrollType.voidCheck>
					.And<PRDeductCode.countryID.IsEqual<paymentCountryID.FromCurrent>>
					.Or<PRDeductCode.contribType.IsNotEqual<ContributionTypeListAttribute.employerContribution>>>),
			SubstituteKey = typeof(PRDeductCode.codeCD),
			DescriptionField = typeof(PRDeductCode.description))]
		[PXCheckUnique(typeof(branchID), typeof(paymentRefNbr), typeof(paymentDocType),
			ErrorMessage = Messages.CantDuplicateDeductionDetail,
			ClearOnDuplicate = false)]
		[PXRestrictor(typeof(
			Where<PRDeductCode.isActive.IsEqual<True>>),
			Messages.DeductCodeInactive)]
		public int? CodeID { get; set; }
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PRCurrency]
		[PXUIField(DisplayName = "Amount")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public virtual Decimal? Amount { get; set; }
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[DedLiabilityAccount(typeof(PRDeductionDetail.branchID),
		   typeof(PRDeductionDetail.codeID),
		   typeof(PRDeductionDetail.employeeID),
		   typeof(PRPayment.payGroupID),
		   typeof(PRDeductionDetail.codeID), DisplayName = "Liability Account")]
		public virtual Int32? AccountID { get; set; }
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[DedLiabilitySubAccount(typeof(PRDeductionDetail.accountID), typeof(PRDeductionDetail.branchID), true, DisplayName = "Liability Sub.", Visibility = PXUIVisibility.Visible, Filterable = true)]
		public virtual int? SubID { get; set; }
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		/// <summary>
		/// Indicates whether the line is released or not.
		/// </summary>
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Released")]
		public virtual Boolean? Released { get; set; }
		#endregion
		#region APInvoiceDocType
		public abstract class apInvoiceDocType : PX.Data.BQL.BqlString.Field<apInvoiceDocType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Type")]
		public string APInvoiceDocType { get; set; }
		#endregion
		#region RefNbr
		public abstract class apInvoiceRefNbr : PX.Data.BQL.BqlString.Field<apInvoiceRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr.")]
		public string APInvoiceRefNbr { get; set; }
		#endregion
		#region LiabilityPaid
		public abstract class liabilityPaid : PX.Data.BQL.BqlBool.Field<liabilityPaid> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Liability Paid")]
		public virtual Boolean? LiabilityPaid { get; set; }
		#endregion

		#region PaymentCountryID
		[PXString(2)]
		[PXUnboundDefault(typeof(Parent<PRPayment.countryID>))]
		public virtual string PaymentCountryID { get; set; }
		public abstract class paymentCountryID : PX.Data.BQL.BqlString.Field<paymentCountryID> { }
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

		public int? ParentKeyID { get => CodeID; set => CodeID = value; }
	}
}
