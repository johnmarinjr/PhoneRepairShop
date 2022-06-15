using System;
using System.Diagnostics;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Payroll.Data;

namespace PX.Objects.PR
{
	[Serializable]
	[PXCacheName(Messages.PREmployeeClassPTOBank)]
	[DebuggerDisplay("{GetType().Name,nq}: EmployeeClassID = {EmployeeClassID,nq}, BankID = {BankID,nq}")]
	public class PREmployeeClassPTOBank : IBqlTable, IPTOBank
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREmployeeClassPTOBank>.By<employeeClassID, bankID>
		{
			public static PREmployeeClassPTOBank Find(PXGraph graph, string employeeClassID, string bankID) => FindBy(graph, employeeClassID, bankID);
		}

		public static class FK
		{
			public class EmployeeClass : PREmployeeClass.PK.ForeignKeyOf<PREmployeeClassPTOBank>.By<employeeClassID> { }
			public class PTOBank : PRPTOBank.PK.ForeignKeyOf<PREmployeeClassPTOBank>.By<bankID> { }
		}

		[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use FK.PTOBank instead.")]
		public class PTOBankFK : FK.PTOBank { }
		#endregion

		#region EmployeeClassID
		[PXDBString(10, IsKey = true, IsUnicode = true, InputMask = ">CCC")]
		[PXUIField(DisplayName = "Employee Class")]
		[PXDBDefault(typeof(PREmployeeClass.employeeClassID))]
		[PXParent(typeof(Select<PREmployeeClass, Where<PREmployeeClass.employeeClassID, Equal<Current<PREmployeeClassPTOBank.employeeClassID>>>>))]
		[PXReferentialIntegrityCheck]
		public virtual string EmployeeClassID { get; set; }
		public abstract class employeeClassID : PX.Data.BQL.BqlString.Field<employeeClassID> { }
		#endregion

		#region BankID
		[PXDBString(3, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "PTO Bank")]
		[PXSelector(typeof(SearchFor<PRPTOBank.bankID>), DescriptionField = typeof(PRPTOBank.description))]
		[PXRestrictor(typeof(Where<PRPTOBank.isActive.IsEqual<True>>), Messages.InactivePTOBank, typeof(PRPTOBank.bankID))]
		[PXForeignReference(typeof(FK.PTOBank))]
		[PXReferentialIntegrityCheck]
		public virtual string BankID { get; set; }
		public abstract class bankID : PX.Data.BQL.BqlString.Field<bankID> { }
		#endregion

		#region IsActive
		[PXDBBool]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? IsActive { get; set; }
		public abstract class isActive : PX.Data.BQL.BqlBool.Field<isActive> { }
		#endregion

		#region AccrualMethod
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Accrual Method")]
		[PTOAccrualMethod.List]
		[PXDefault(typeof(PTOAccrualMethod.percentage))]
		[PXUIEnabled(typeof(Where<createFinancialTransaction.IsEqual<False>>))]
		public virtual string AccrualMethod { get; set; }
		public abstract class accrualMethod : PX.Data.BQL.BqlString.Field<accrualMethod> { }
		#endregion

		#region AccrualRate
		[PXDBDecimal(6, MinValue = 0)]
		[PXUIField(DisplayName = "Accrual %")]
		[PXUIEnabled(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>))]
		[ShowValueWhen(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>))]
		[PXDefault(TypeCode.Decimal, "0")]
		public virtual Decimal? AccrualRate { get; set; }
		public abstract class accrualRate : PX.Data.BQL.BqlDecimal.Field<accrualRate> { }
		#endregion

		#region HoursPerYear
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Hours per Year")]
		[PXUIEnabled(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.totalHoursPerYear>>))]
		[ShowValueWhen(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.totalHoursPerYear>>))]
		[PXDefault(TypeCode.Decimal, "0")]
		public virtual Decimal? HoursPerYear { get; set; }
		public abstract class hoursPerYear : PX.Data.BQL.BqlDecimal.Field<hoursPerYear> { }
		#endregion

		#region AccrualLimit
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Accrual Limit")]
		public virtual Decimal? AccrualLimit
		{
			get => _AccrualLimit != 0 ? _AccrualLimit : null;
			set => _AccrualLimit = value;
		}
		private decimal? _AccrualLimit;
		public abstract class accrualLimit : PX.Data.BQL.BqlDecimal.Field<accrualLimit> { }
		#endregion

		#region StartDate
		[PXDBDate]
		[PXDefault]
		[PXUIField(DisplayName = "Effective Date")]
		public virtual DateTime? StartDate { get; set; }
		public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }
		#endregion

		#region PTOYearStartDate
		[PXDate]
		[PXDBScalar(typeof(SearchFor<PRPTOBank.startDate>.Where<PRPTOBank.bankID.IsEqual<bankID>>))]
		[PXUnboundDefault(typeof(SearchFor<PRPTOBank.startDate>.Where<PRPTOBank.bankID.IsEqual<bankID.FromCurrent>>))]
		public virtual DateTime? PTOYearStartDate { get; set; }
		public abstract class pTOYearStartDate : PX.Data.BQL.BqlDateTime.Field<pTOYearStartDate> { }
		#endregion

		#region CarryoverType
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Carryover Type")]
		[CarryoverType.List]
		[PXUIEnabled(typeof(Where<createFinancialTransaction.IsEqual<False>>))]
		public virtual string CarryoverType { get; set; }
		public abstract class carryoverType : PX.Data.BQL.BqlString.Field<carryoverType> { }
		#endregion

		#region CarryoverAmount
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Carryover Amount")]
		[PXFormula(typeof(Switch<Case<Where<carryoverType.IsNotEqual<CarryoverType.partial>>, Null>>))]
		[HideValueIfDisabled(typeof(carryoverType.IsEqual<CarryoverType.partial>))]
		public virtual Decimal? CarryoverAmount { get; set; }
		public abstract class carryoverAmount : PX.Data.BQL.BqlDecimal.Field<carryoverAmount> { }
		#endregion

		#region FrontLoadingAmount
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Front Loading Amount")]
		[HideValueIfDisabled(typeof(Where<createFinancialTransaction.IsEqual<False>>))]
		public virtual Decimal? FrontLoadingAmount { get; set; }
		public abstract class frontLoadingAmount : PX.Data.BQL.BqlDecimal.Field<frontLoadingAmount> { }
		#endregion

		#region AllowNegativeBalance
		[PXBool]
		[PXUIField(DisplayName = "Allow Negative Balance")]
		[PXDBScalar(typeof(SearchFor<PRPTOBank.allowNegativeBalance>.Where<PRPTOBank.bankID.IsEqual<PREmployeeClassPTOBank.bankID>>))]
		public virtual bool? AllowNegativeBalance { get; set; }
		public abstract class allowNegativeBalance : PX.Data.BQL.BqlBool.Field<allowNegativeBalance> { }
		#endregion

		#region CarryoverPayMonthLimit
		[PXInt]
		[PXUIField(DisplayName = "Pay Carryover after (Months)")]
		[PXDBScalar(typeof(SearchFor<PRPTOBank.carryoverPayMonthLimit>.Where<PRPTOBank.bankID.IsEqual<PREmployeeClassPTOBank.bankID>>))]
		public virtual int? CarryoverPayMonthLimit { get; set; }
		public abstract class carryoverPayMonthLimit : PX.Data.BQL.BqlInt.Field<carryoverPayMonthLimit> { }
		#endregion

		#region DisburseFromCarryover
		[PXBool]
		[PXUIField(DisplayName = "Can Only Disburse from Carryover")]
		[PXDBScalar(typeof(SearchFor<PRPTOBank.disburseFromCarryover>.Where<PRPTOBank.bankID.IsEqual<PREmployeeClassPTOBank.bankID>>))]
		public virtual bool? DisburseFromCarryover { get; set; }
		public abstract class disburseFromCarryover : PX.Data.BQL.BqlBool.Field<disburseFromCarryover> { }
		#endregion

		#region DisbursingType
		[PXDBString(1, IsFixed = true)]
		[PXDefault(typeof(Selector<bankID, PRPTOBank.disbursingType>))]
		[PXUIField(DisplayName = "Disbursing Type")]
		[PXUIVisible(typeof(Where<Parent<PREmployeeClass.countryID>, Equal<BQLLocationConstants.CountryCAN>>))]
		[PXUIRequired(typeof(Where<Parent<PREmployeeClass.countryID>, Equal<BQLLocationConstants.CountryCAN>, And<createFinancialTransaction, Equal<True>>>))]
		[HideValueIfDisabled(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		[PTODisbursingType.List]
		public virtual string DisbursingType { get; set; }
		public abstract class disbursingType : PX.Data.BQL.BqlString.Field<disbursingType> { }
		#endregion

		#region CreateFinancialTransaction
		[PXBool]
		[PXUnboundDefault(typeof(Selector<bankID, PRPTOBank.createFinancialTransaction>))]
		public virtual bool? CreateFinancialTransaction { get; set; }
		public abstract class createFinancialTransaction : PX.Data.BQL.BqlBool.Field<createFinancialTransaction> { }
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
	}
}
