using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// Stores the Record of Employment information that was generated from the Final Paycheck or created manually.
	/// </summary>
	[PXCacheName(Messages.PRRecordOfEmployment)]
	[Serializable]
	[PXPrimaryGraph(typeof(PRRecordOfEmploymentMaint))]
	public class PRRecordOfEmployment : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRRecordOfEmployment>.By<refNbr>
		{
			public static PRRecordOfEmployment Find(PXGraph graph, string refNbr) =>
				FindBy(graph, refNbr);
		}

		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<PRRecordOfEmployment>.By<branchID> { }
			public class PREmployee : PR.PREmployee.PK.ForeignKeyOf<PRRecordOfEmployment>.By<employeeID> { }
			public class PRPayment : PR.PRPayment.PK.ForeignKeyOf<PRRecordOfEmployment>.By<origDocType, origRefNbr> { }
			public class Address : CR.Address.PK.ForeignKeyOf<PRRecordOfEmployment>.By<addressID> { }
		}
		#endregion

		#region RefNbr
		/// <summary>
		/// The user-friendly unique identifier of the Record of Employment.
		/// </summary>
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Reference Nbr. (Block 1)", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(SelectFrom<PRRecordOfEmployment>
			.InnerJoin<EPEmployee>.On<EPEmployee.bAccountID.IsEqual<PRRecordOfEmployment.employeeID>>
			.SearchFor<PRRecordOfEmployment.refNbr>),
			typeof(refNbr), typeof(status), typeof(amendment), typeof(employeeID), typeof(EPEmployee.acctName))]
		[AutoNumber(typeof(PRSetup.roeNumberingCD), typeof(PRRecordOfEmployment.finalPayPeriodEndingDate))]
		[PXFieldDescription]
		public String RefNbr { get; set; }
		#endregion

		#region Status
		/// <summary>
		/// The status of the Record of Employment.
		/// </summary>
		/// <value>
		/// The field can have one of the values described in <see cref="ROEStatus.ListAttribute"/>.
		/// </value>
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Status", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault(ROEStatus.Open)]
		[ROEStatus.List]
		public virtual string Status { get; set; }
		#endregion

		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		[PXUIField(DisplayName = "Terminated Employee (Block 9)")]
		[PXDefault]
		[Employee]
		[PXForeignReference(typeof(Field<employeeID>.IsRelatedTo<PREmployee.bAccountID>))]
		public virtual int? EmployeeID { get; set; }
		#endregion

		#region Amendment
		public abstract class amendment : PX.Data.BQL.BqlBool.Field<amendment> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Amendment")]
		public bool? Amendment { get; set; }
		#endregion

		#region AmendedRefNbr
		public abstract class amendedRefNbr : PX.Data.BQL.BqlString.Field<amendedRefNbr> { }
		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Amended ROE Ref. Nbr. (Block 2)", Visibility = PXUIVisibility.SelectorVisible)]
		[PXUIVisible(typeof(Where<amendment.IsEqual<True>>))]
		[PXFieldDescription]
		public String AmendedRefNbr { get; set; }
		#endregion

		#region ReasonForROE
		public abstract class reasonForROE : PX.Data.BQL.BqlString.Field<reasonForROE> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Reason for ROE (Block 16)", Required = true)]
		[PXDefault]
		[ROEReason.List]
		public virtual string ReasonForROE { get; set; }
		#endregion

		#region PeriodType
		public abstract class periodType : PX.Data.BQL.BqlString.Field<periodType> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Period Type (Block 6)", Required = true)]
		[PayPeriodType.ROEList]
		public virtual string PeriodType { get; set; }
		#endregion

		#region OrigDocType
		public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }
		[PXDBString(3, IsFixed = true)]
		[PayrollType.List]
		[PXUIField(DisplayName = "Original Doc. Type")]
		public virtual String OrigDocType {	get; set; }
		#endregion

		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Original Document")]
		[PXForeignReference(typeof(FK.PRPayment))]
		public virtual string OrigRefNbr { get; set; }
		#endregion

		#region Comments
		public abstract class comments : PX.Data.BQL.BqlString.Field<comments> { }
		[PXDBString(128, IsUnicode = true)]
		[PXUIField(DisplayName = "Comments (Block 18)")]
		public virtual string Comments { get; set; }
		#endregion

		#region DocDesc
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		[PXDBString(128, IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		public virtual string DocDesc { get; set; }
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXFormula(typeof(Default<employeeID>))]
		[GL.Branch(
			typeof(SelectFrom<GL.Branch>
				.InnerJoin<EPEmployee>.On<GL.Branch.bAccountID.IsEqual<EPEmployee.parentBAccountID>>
				.Where<EPEmployee.bAccountID.IsEqual<employeeID.FromCurrent>>
				.SearchFor<GL.Branch.branchID>),
			IsDetail = false,
			Visibility = PXUIVisibility.SelectorVisible,
			Enabled = false)]
		public int? BranchID { get; set; }
		#endregion

		#region AddressID
		public abstract class addressID : PX.Data.BQL.BqlInt.Field<addressID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Address ID", Visible = false)]
		[PXDBChildIdentity(typeof(Address.addressID))]
		[PXSelector(typeof(Address.addressID))]
		public int? AddressID { get; set; }
		#endregion

		#region CRAPayrollAccountNumber
		public abstract class craPayrollAccountNumber : PX.Data.BQL.BqlString.Field<craPayrollAccountNumber> { }
		[PXDBString(15, InputMask = ">000000000LL0000")]
		[PXUIField(DisplayName = "CRA Payroll Account Number (Block 5)")]
		public virtual String CRAPayrollAccountNumber { get; set; }
		#endregion

		#region FirstDayWorked
		public abstract class firstDayWorked : PX.Data.BQL.BqlDateTime.Field<firstDayWorked> { }
		[PXDBDate(UseSmallDateTime = true)]
		[PXDefault]
		[PXUIField(DisplayName = "First Day Worked (Block 10)", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public DateTime? FirstDayWorked { get; set; }
		#endregion

		#region LastDayForWhichPaid
		public abstract class lastDayForWhichPaid : PX.Data.BQL.BqlDateTime.Field<lastDayForWhichPaid> { }
		[PXDBDate(UseSmallDateTime = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Last Day for Which Paid (Block 11)", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public DateTime? LastDayForWhichPaid { get; set; }
		#endregion

		#region FinalPayPeriodEndingDate
		public abstract class finalPayPeriodEndingDate : PX.Data.BQL.BqlDateTime.Field<finalPayPeriodEndingDate> { }
		[PXDBDate(UseSmallDateTime = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Final Pay Period Ending Date (Block 12)", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public DateTime? FinalPayPeriodEndingDate { get; set; }
		#endregion

		#region VacationPay
		public abstract class vacationPay : PX.Data.BQL.BqlDecimal.Field<vacationPay> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.00")]
		[PXUIField(DisplayName = "Vacation Pay (Block 17A)")]
		public virtual decimal? VacationPay { get; set; }
		#endregion

		#region TotalInsurableHours
		public abstract class totalInsurableHours : PX.Data.BQL.BqlDecimal.Field<totalInsurableHours> { }
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Insurable Hours (Block 15A)")]
		public virtual decimal? TotalInsurableHours { get; set; }
		#endregion

		#region TotalInsurableEarnings
		public abstract class totalInsurableEarnings : PX.Data.BQL.BqlDecimal.Field<totalInsurableEarnings> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.00")]
		[PXUIField(DisplayName = "Total Insurable Earnings (Block 15B)")]
		public virtual decimal? TotalInsurableEarnings { get; set; }
		#endregion

		#region NoteID
		public abstract class noteID : IBqlField { }
		[PXSearchable(SM.SearchCategory.PR, Messages.SearchableTitlePRROE, new Type[] { typeof(refNbr) },
			new Type[] { typeof(refNbr), typeof(status), typeof(amendment), typeof(periodType), typeof(employeeID) },
			NumberFields = new Type[] { typeof(refNbr) },
			Line1Format = "{0}{1}{2}", Line1Fields = new Type[] { typeof(refNbr), typeof(status), typeof(periodType) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(docDesc) }
		)]
		[PXNote]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region System Columns
		#region CreatedByID
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID()]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion System Columns
	}
}
