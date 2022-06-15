using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CA;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PREmployeeCacheName)]
	[Serializable]
	[PXTable(typeof(CR.BAccount.bAccountID))]
	public class PREmployee : EPEmployee
	{
		#region Keys
		public new class PK : PrimaryKeyOf<PREmployee>.By<bAccountID>
		{
			public static PREmployee Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}

		public new class UK : PrimaryKeyOf<PREmployee>.By<acctCD>
		{
			public static PREmployee Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}

		public new static class FK
		{
			public class EmployeeClass : PREmployeeClass.PK.ForeignKeyOf<PREmployee>.By<employeeClassID> { }
			public class PayGroup : PRPayGroup.PK.ForeignKeyOf<PREmployee>.By<payGroupID> { }
			public class Calendar : CSCalendar.PK.ForeignKeyOf<PREmployee>.By<calendarID> { }
			public class Union : PMUnion.PK.ForeignKeyOf<PREmployee>.By<unionID> { }
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PREmployee>.By<workCodeID> { }
			public class EarningsAccount : Account.PK.ForeignKeyOf<PREmployee>.By<earningsAcctID> { }
			public class EarningsSubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<earningsSubID> { }
			public class DeductionLiabilityAccount : Account.PK.ForeignKeyOf<PREmployee>.By<dedLiabilityAcctID> { }
			public class DeductionLiabilitySubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<dedLiabilitySubID> { }
			public class BenefitExpenseAccount : Account.PK.ForeignKeyOf<PREmployee>.By<benefitExpenseAcctID> { }
			public class BenefitExpenseSubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<benefitExpenseSubID> { }
			public class BenefitLiabilityAccount : Account.PK.ForeignKeyOf<PREmployee>.By<benefitLiabilityAcctID> { }
			public class BenefitLiabilitySubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<benefitLiabilitySubID> { }
			public class TaxExpenseAccount : Account.PK.ForeignKeyOf<PREmployee>.By<payrollTaxExpenseAcctID> { }
			public class TaxExpenseSubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<payrollTaxExpenseSubID> { }
			public class TaxLiabilityAccount : Account.PK.ForeignKeyOf<PREmployee>.By<payrollTaxLiabilityAcctID> { }
			public class TaxLiabilitySubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<payrollTaxLiabilitySubID> { }
			public class PTOExpenseAccount : Account.PK.ForeignKeyOf<PREmployee>.By<ptoExpenseAcctID> { }
			public class PTOExpenseSubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<ptoExpenseSubID> { }
			public class PTOLiabilityAccount : Account.PK.ForeignKeyOf<PREmployee>.By<ptoLiabilityAcctID> { }
			public class PTOLiabilitySubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<ptoLiabilitySubID> { }
			public class PTOAssetAccount : Account.PK.ForeignKeyOf<PREmployee>.By<ptoAssetAcctID> { }
			public class PTOAssetSubaccount : Sub.PK.ForeignKeyOf<PREmployee>.By<ptoAssetSubID> { }
			public class PaymentMethod : CA.PaymentMethod.PK.ForeignKeyOf<PREmployee>.By<paymentMethodID> { }
			public class CashAccount : CA.CashAccount.PK.ForeignKeyOf<PREmployee>.By<cashAccountID> { }
			public class Address : CR.Address.PK.ForeignKeyOf<PREmployee>.By<defAddressID> { }
		}

		[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use FK.EmployeeClass instead.")]
		public class EmployeeClassFK : FK.EmployeeClass { }
		#endregion

		public new abstract class bAccountID : BqlInt.Field<bAccountID> { }
		#region AcctCD
		public new abstract class acctCD : BqlString.Field<acctCD> { }
		[PREmployeeRaw]
		[PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault]
		[PXUIField(DisplayName = "Employee ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PX.Data.EP.PXFieldDescription]
		public override String AcctCD
		{
			get
			{
				return this._AcctCD;
			}
			set
			{
				this._AcctCD = value;
			}
		}
		#endregion
		public new abstract class acctName : BqlString.Field<acctName> { }
		public new abstract class parentBAccountID : BqlInt.Field<parentBAccountID> { }

		public new abstract class defContactID : BqlInt.Field<defContactID> { }

		public new abstract class vStatus : BqlString.Field<vStatus> { }

		#region ActiveInPayroll
		public abstract class activeInPayroll : BqlBool.Field<activeInPayroll> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Active")]
		public bool? ActiveInPayroll { get; set; }
		#endregion
		#region EmployeeClassID
		public abstract class employeeClassID : BqlString.Field<employeeClassID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Class ID")]
		[PXSelector(typeof(SearchFor<PREmployeeClass.employeeClassID>.Where<PREmployeeClass.countryID.IsEqual<countryID.FromCurrent>>))]
		[PXForeignReference(typeof(FK.EmployeeClass))]
		public string EmployeeClassID { get; set; }
		#endregion
		#region EmpType
		public abstract class empType : BqlString.Field<empType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Employee Type")]
		[PXDefault]
		[UseDefaultValue(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.empType), typeof(PREmployee.empTypeUseDflt))]
		[EmployeeType.List]
		public string EmpType { get; set; }
		#endregion
		#region EmpTypeUseDflt
		public abstract class empTypeUseDflt : BqlBool.Field<empTypeUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? EmpTypeUseDflt { get; set; }
		#endregion
		#region PayGroupID
		public abstract class payGroupID : BqlString.Field<payGroupID> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Pay Group")]
		[PXDefault]
		[UseDefaultValue(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.payGroupID), typeof(PREmployee.payGroupUseDflt))]
		[PXSelector(typeof(SearchFor<PRPayGroup.payGroupID>.Where<MatchWithPayGroup<PRPayGroup.payGroupID>>), DescriptionField = typeof(PRPayGroup.description))]
		[PXForeignReference(typeof(FK.PayGroup))]
		public string PayGroupID { get; set; }
		#endregion
		#region PayGroupUseDflt
		public abstract class payGroupUseDflt : BqlBool.Field<payGroupUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? PayGroupUseDflt { get; set; }
		#endregion
		#region CalendarID
		public abstract new class calendarID : BqlString.Field<calendarID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Calendar")]
		[PXSelector(typeof(Search<CSCalendar.calendarID>), DescriptionField = typeof(CSCalendar.description))]
		[UseDefaultValueFromSource(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.calendarID), typeof(PREmployee.calendarIDUseDflt),
			new[] { typeof(PREmployee.bAccountID) },
			new[] { typeof(EPEmployee.bAccountID) }, Required = RequiredMode.Never)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public override string CalendarID { get; set; }
		#endregion
		#region CalendarIDUseDflt
		public abstract class calendarIDUseDflt : BqlBool.Field<calendarIDUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? CalendarIDUseDflt { get; set; }
		#endregion
		#region HoursPerWeek
		public abstract class hoursPerWeek : BqlDecimal.Field<hoursPerWeek> { }
		[HoursPerWeek(typeof(calendarID), typeof(hoursPerYear), typeof(stdWeeksPerYear), typeof(calendarIDUseDflt))]
		public decimal? HoursPerWeek { get; set; }
		#endregion
		#region StdWeeksPerYear
		public abstract class stdWeeksPerYear : BqlByte.Field<stdWeeksPerYear> { }
		[PXDBByte(MaxValue = 52)]
		[PXUIField(DisplayName = "Working Weeks per Year")]
		[PXDefault]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.stdWeeksPerYear), typeof(stdWeeksPerYearUseDflt))]
		public virtual byte? StdWeeksPerYear { get; set; }
		#endregion
		#region StdWeeksPerYearUseDflt
		public abstract class stdWeeksPerYearUseDflt : BqlBool.Field<stdWeeksPerYearUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? StdWeeksPerYearUseDflt { get; set; }
		#endregion
		#region HoursPerYear
		public abstract class hoursPerYear : BqlDecimal.Field<hoursPerYear> { }
		[PXDBDecimal]
		[PXUIField(DisplayName = "Working Hours per Year", Enabled = false)]
		public decimal? HoursPerYear { get; set; }
		#endregion
		#region NetPayMin
		public abstract class netPayMin : BqlDecimal.Field<netPayMin> { }
		[PRCurrency(MinValue = 0)]
		[PXUIField(DisplayName = "Net Pay Minimum")]
		[PXDefault]
		[UseDefaultValue(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.netPayMin), typeof(PREmployee.netPayMinUseDflt))]
		public Decimal? NetPayMin { get; set; }
		#endregion
		#region NetPayMinUseDflt
		public abstract class netPayMinUseDflt : BqlBool.Field<netPayMinUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? NetPayMinUseDflt { get; set; }
		#endregion
		#region GrnMaxPctNet
		public abstract class grnMaxPctNet : BqlDecimal.Field<grnMaxPctNet> { }
		[PXDBDecimal(MinValue = 0, MaxValue = 100)]
		[PXUIField(DisplayName = "Maximum Percent of Net Pay for all Garnishments")]
		[PXDefault]
		[UseDefaultValue(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.grnMaxPctNet), typeof(PREmployee.grnMaxPctUseDflt))]
		public decimal? GrnMaxPctNet { get; set; }
		#endregion
		#region GrnMaxPctUseDflt
		public abstract class grnMaxPctUseDflt : BqlBool.Field<grnMaxPctUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? GrnMaxPctUseDflt { get; set; }
		#endregion
		#region LocationUseDflt
		public abstract class locationUseDflt : BqlBool.Field<locationUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? LocationUseDflt { get; set; }
		#endregion
		#region UnionID
		public abstract new class unionID : BqlString.Field<unionID> { }
		[PXForeignReference(typeof(Field<unionID>.IsRelatedTo<PMUnion.unionID>))]
		[PMUnion(null, typeof(SelectFrom<EPEmployee>.Where<EPEmployee.bAccountID.IsEqual<bAccountID.FromCurrent>>),
			DisplayName = "Default Union", FieldClass = null)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[UseDefaultValueFromSource(typeof(PREmployee.employeeClassID), typeof(PREmployeeClass.unionID), typeof(PREmployee.unionUseDflt),
			new[] { typeof(PREmployee.bAccountID) },
			new[] { typeof(EPEmployee.bAccountID) }, Required = RequiredMode.Never)]
		public override string UnionID { get; set; }
		#endregion
		#region UnionUseDflt
		public abstract class unionUseDflt : BqlBool.Field<unionUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? UnionUseDflt { get; set; }
		#endregion
		#region UsePTOBanksFromClass
		public abstract class usePTOBanksFromClass : BqlBool.Field<usePTOBanksFromClass> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use PTO Banks from Employee Class")]
		public bool? UsePTOBanksFromClass { get; set; }
		#endregion
		#region WorkCodeID
		public abstract class workCodeID : BqlString.Field<workCodeID> { }
		[PXForeignReference(typeof(Field<workCodeID>.IsRelatedTo<PMWorkCode.workCodeID>))]
		[WorkCodeMatchCountry(typeof(countryID), DisplayName = "Default WCC Code", FieldClass = null)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.workCodeID), typeof(workCodeUseDflt), Required = RequiredMode.Never)]
		public string WorkCodeID { get; set; }
		#endregion
		#region WorkCodeUseDflt
		public abstract class workCodeUseDflt : BqlBool.Field<workCodeUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? WorkCodeUseDflt { get; set; }
		#endregion
		#region ExemptFromOvertimeRules
		public abstract class exemptFromOvertimeRules : BqlBool.Field<exemptFromOvertimeRules> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Exempt from Overtime Rules")]
		[PXDefault(false)]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.exemptFromOvertimeRules), typeof(exemptFromOvertimeRulesUseDflt), Required = RequiredMode.Never)]
		public bool? ExemptFromOvertimeRules { get; set; }
		#endregion
		#region ExemptFromOvertimeRulesUseDflt
		public abstract class exemptFromOvertimeRulesUseDflt : BqlBool.Field<exemptFromOvertimeRulesUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? ExemptFromOvertimeRulesUseDflt { get; set; }
		#endregion
		#region OverrideHoursPerYearForCertified
		public abstract class overrideHoursPerYearForCertified : BqlBool.Field<overrideHoursPerYearForCertified> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Override Hours per Year for Certified Project")]
		[PXDefault(false)]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.overrideHoursPerYearForCertified), typeof(overrideHoursPerYearForCertifiedUseDflt))]
		public bool? OverrideHoursPerYearForCertified { get; set; }
		#endregion
		#region OverrideHoursPerYearForCertifiedUseDflt
		public abstract class overrideHoursPerYearForCertifiedUseDflt : BqlBool.Field<overrideHoursPerYearForCertifiedUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? OverrideHoursPerYearForCertifiedUseDflt { get; set; }
		#endregion
		#region HoursPerYearForCertified
		public abstract class hoursPerYearForCertified : BqlInt.Field<hoursPerYearForCertified> { }
		[PXDBInt(MinValue = 0)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Certified Project Hours per Year")]
		[UseDefaultValue(
			typeof(employeeClassID),
			typeof(PREmployeeClass.hoursPerYearForCertified),
			typeof(hoursPerYearForCertifiedUseDflt),
			Required = RequiredMode.Never,
			ManageUIEnabled = false,
			ManageErrorLevel = false)]
		[HoursForCertifiedProject(typeof(hoursPerYear), typeof(overrideHoursPerYearForCertified), typeof(hoursPerYearForCertifiedUseDflt))]
		public int? HoursPerYearForCertified { get; set; }
		#endregion
		#region HoursPerYearForCertifiedUseDflt
		public abstract class hoursPerYearForCertifiedUseDflt : BqlBool.Field<hoursPerYearForCertifiedUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? HoursPerYearForCertifiedUseDflt { get; set; }
		#endregion
		#region ExemptFromCertifiedReporting
		public abstract class exemptFromCertifiedReporting : BqlBool.Field<exemptFromCertifiedReporting> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Exempt from Certified Reporting")]
		[PXDefault(false)]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.exemptFromCertifiedReporting), typeof(exemptFromCertifiedReportingUseDflt), Required = RequiredMode.Never)]
		public bool? ExemptFromCertifiedReporting { get; set; }
		#endregion
		#region ExemptFromCertifiedReportingUseDflt
		public abstract class exemptFromCertifiedReportingUseDflt : BqlBool.Field<exemptFromCertifiedReportingUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? ExemptFromCertifiedReportingUseDflt { get; set; }
		#endregion
		#region UsePayrollProjectWorkLocation
		public abstract class usePayrollProjectWorkLocation : BqlBool.Field<usePayrollProjectWorkLocation> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Use Payroll Work Location from Project")]
		[PXDefault(true)]
		[UseDefaultValue(typeof(employeeClassID), typeof(PREmployeeClass.usePayrollProjectWorkLocation), typeof(usePayrollProjectWorkLocationUseDflt), Required = RequiredMode.Never)]
		public bool? UsePayrollProjectWorkLocation { get; set; }
		#endregion
		#region UsePayrollProjectWorkLocationUseDflt
		public abstract class usePayrollProjectWorkLocationUseDflt : BqlBool.Field<usePayrollProjectWorkLocationUseDflt> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Use Class Default Value")]
		public bool? UsePayrollProjectWorkLocationUseDflt { get; set; }
		#endregion
		#region LineCntr
		public abstract class lineCntr : BqlInt.Field<lineCntr> { }
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? LineCntr { get; set; }
		#endregion
		#region EarningsAcctID
		public abstract class earningsAcctID : BqlInt.Field<earningsAcctID> { }
		[Account(DisplayName = "Earnings Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<PREmployee.earningsAcctID>.IsRelatedTo<Account.accountID>))]
		[PREarningAccountRequired(GLAccountSubSource.Employee)]
		public virtual Int32? EarningsAcctID { get; set; }
		#endregion
		#region EarningsSubID
		public abstract class earningsSubID : BqlInt.Field<earningsSubID> { }
		[SubAccount(typeof(PREmployee.earningsAcctID), DisplayName = "Earnings Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.earningsSubID>.IsRelatedTo<Sub.subID>))]
		[PREarningSubRequired(GLAccountSubSource.Employee)]
		public virtual Int32? EarningsSubID { get; set; }
		#endregion
		#region DedLiabilityAcctID
		public abstract class dedLiabilityAcctID : PX.Data.BQL.BqlInt.Field<dedLiabilityAcctID> { }
		[Account(DisplayName = "Deduction Liability Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		[PXForeignReference(typeof(Field<PREmployee.dedLiabilityAcctID>.IsRelatedTo<Account.accountID>))]
		[PRDedLiabilityAccountRequired(GLAccountSubSource.Employee)]
		public virtual Int32? DedLiabilityAcctID { get; set; }
		#endregion
		#region DedLiabilitySubID
		public abstract class dedLiabilitySubID : PX.Data.BQL.BqlInt.Field<dedLiabilitySubID> { }
		[SubAccount(typeof(PREmployee.dedLiabilityAcctID), DisplayName = "Deduction Liability Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.dedLiabilitySubID>.IsRelatedTo<Sub.subID>))]
		[PRDedLiabilitySubRequired(GLAccountSubSource.Employee)]
		public virtual Int32? DedLiabilitySubID { get; set; }
		#endregion
		#region BenefitExpenseAcctID
		public abstract class benefitExpenseAcctID : PX.Data.BQL.BqlInt.Field<benefitExpenseAcctID> { }
		[Account(DisplayName = "Benefit Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<PREmployee.benefitExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRBenExpenseAccountRequired(GLAccountSubSource.Employee)]
		public virtual Int32? BenefitExpenseAcctID { get; set; }
		#endregion
		#region BenefitExpenseSubID
		public abstract class benefitExpenseSubID : PX.Data.BQL.BqlInt.Field<benefitExpenseSubID> { }
		[SubAccount(typeof(PREmployee.benefitExpenseAcctID), DisplayName = "Benefit Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.benefitExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRBenExpenseSubRequired(GLAccountSubSource.Employee)]
		public virtual Int32? BenefitExpenseSubID { get; set; }
		#endregion
		#region BenefitLiabilityAcctID
		public abstract class benefitLiabilityAcctID : PX.Data.BQL.BqlInt.Field<benefitLiabilityAcctID> { }
		[Account(DisplayName = "Benefit Liability Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		[PXForeignReference(typeof(Field<PREmployee.benefitLiabilityAcctID>.IsRelatedTo<Account.accountID>))]
		[PRBenLiabilityAccountRequired(GLAccountSubSource.Employee)]
		public virtual Int32? BenefitLiabilityAcctID { get; set; }
		#endregion
		#region BenefitLiabilitySubID
		public abstract class benefitLiabilitySubID : PX.Data.BQL.BqlInt.Field<benefitLiabilitySubID> { }
		[SubAccount(typeof(PREmployee.benefitLiabilityAcctID), DisplayName = "Benefit Liability Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.benefitLiabilitySubID>.IsRelatedTo<Sub.subID>))]
		[PRBenLiabilitySubRequired(GLAccountSubSource.Employee)]
		public virtual Int32? BenefitLiabilitySubID { get; set; }
		#endregion
		#region PayrollTaxExpenseAcctID
		public abstract class payrollTaxExpenseAcctID : PX.Data.BQL.BqlInt.Field<payrollTaxExpenseAcctID> { }
		[Account(DisplayName = "Tax Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<PREmployee.payrollTaxExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRTaxExpenseAccountRequired(GLAccountSubSource.Employee)]
		public Int32? PayrollTaxExpenseAcctID { get; set; }
		#endregion
		#region PayrollTaxExpenseSubID
		public abstract class payrollTaxExpenseSubID : PX.Data.BQL.BqlInt.Field<payrollTaxExpenseSubID> { }
		[SubAccount(typeof(PREmployee.payrollTaxExpenseAcctID), DisplayName = "Tax Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.payrollTaxExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRTaxExpenseSubRequired(GLAccountSubSource.Employee)]
		public Int32? PayrollTaxExpenseSubID { get; set; }
		#endregion
		#region PayrollTaxLiabilityAcctID
		public abstract class payrollTaxLiabilityAcctID : PX.Data.BQL.BqlInt.Field<payrollTaxLiabilityAcctID> { }
		[Account(DisplayName = "Tax Liability Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		[PXForeignReference(typeof(Field<PREmployee.payrollTaxLiabilityAcctID>.IsRelatedTo<Account.accountID>))]
		[PRTaxLiabilityAccountRequired(GLAccountSubSource.Employee)]
		public virtual Int32? PayrollTaxLiabilityAcctID { get; set; }
		#endregion
		#region PayrollTaxLiabilitySubID
		public abstract class payrollTaxLiabilitySubID : PX.Data.BQL.BqlInt.Field<payrollTaxLiabilitySubID> { }
		[SubAccount(typeof(PREmployee.payrollTaxLiabilityAcctID), DisplayName = "Tax Liability Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<PREmployee.payrollTaxLiabilitySubID>.IsRelatedTo<Sub.subID>))]
		[PRTaxLiabilitySubRequired(GLAccountSubSource.Employee)]
		public virtual Int32? PayrollTaxLiabilitySubID { get; set; }
		#endregion
		#region PTOExpenseAcctID
		public abstract class ptoExpenseAcctID : PX.Data.BQL.BqlInt.Field<ptoExpenseAcctID> { }
		[Account(DisplayName = "PTO Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOExpenseAccount))]
		[PRPTOExpenseAccountRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public Int32? PTOExpenseAcctID { get; set; }
		#endregion
		#region PTOExpenseSubID
		public abstract class ptoExpenseSubID : PX.Data.BQL.BqlInt.Field<ptoExpenseSubID> { }
		[SubAccount(typeof(ptoExpenseAcctID), DisplayName = "PTO Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOExpenseSubaccount))]
		[PRPTOExpenseSubRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public Int32? PTOExpenseSubID { get; set; }
		#endregion
		#region PTOLiabilityAcctID
		public abstract class ptoLiabilityAcctID : PX.Data.BQL.BqlInt.Field<ptoLiabilityAcctID> { }
		[Account(DisplayName = "PTO Liability Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOLiabilityAccount))]
		[PRPTOLiabilityAccountRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public virtual Int32? PTOLiabilityAcctID { get; set; }
		#endregion
		#region PTOLiabilitySubID
		public abstract class ptoLiabilitySubID : PX.Data.BQL.BqlInt.Field<ptoLiabilitySubID> { }
		[SubAccount(typeof(ptoLiabilityAcctID), DisplayName = "PTO Liability Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOLiabilitySubaccount))]
		[PRPTOLiabilitySubRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public virtual Int32? PTOLiabilitySubID { get; set; }
		#endregion
		#region PTOAssetAcctID
		public abstract class ptoAssetAcctID : PX.Data.BQL.BqlInt.Field<ptoAssetAcctID> { }
		[Account(DisplayName = "PTO Asset Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOAssetAccount))]
		[PRPTOAssetAccountRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public virtual Int32? PTOAssetAcctID { get; set; }
		#endregion
		#region PTOAssetSubID
		public abstract class ptoAssetSubID : PX.Data.BQL.BqlInt.Field<ptoAssetSubID> { }
		[SubAccount(typeof(ptoAssetAcctID), DisplayName = "PTO Asset Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXUIVisible(typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		[PXForeignReference(typeof(FK.PTOAssetSubaccount))]
		[PRPTOAssetSubRequired(GLAccountSubSource.Employee, typeof(Where<countryID.IsEqual<BQLLocationConstants.CountryCAN>>))]
		public virtual Int32? PTOAssetSubID { get; set; }
		#endregion
		#region DedSplitType
		public abstract class dedSplitType : PX.Data.BQL.BqlString.Field<dedSplitType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Split Method")]
		[PXDefault(DeductionSplitType.ProRata)]
		[DeductionSplitType.List]
		public string DedSplitType { get; set; }
		#endregion
		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Payment Method", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<PaymentMethod.paymentMethodID,
			Where<PaymentMethod.isActive, Equal<True>,
				And<PRxPaymentMethod.useForPR, Equal<True>>>>), DescriptionField = typeof(PaymentMethod.descr))]
		[PXForeignReference(typeof(Field<paymentMethodID>.IsRelatedTo<PaymentMethod.paymentMethodID>))]
		public virtual string PaymentMethodID { get; set; }
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		[CashAccount(null, typeof(Search2<CashAccount.cashAccountID,
			InnerJoin<PaymentMethodAccount,
				On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
				And<PaymentMethodAccount.paymentMethodID, Equal<Current<PREmployee.paymentMethodID>>,
				And<PRxPaymentMethodAccount.useForPR, Equal<True>>>>>,
			Where<Match<Current<AccessInfo.userName>>>>), DisplayName = "Cash Account", DescriptionField = typeof(CashAccount.descr), Visibility = PXUIVisibility.Visible)]
		[PXDefault]
		public virtual int? CashAccountID { get; set; }
		#endregion
		#region DefAddressID
		public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
		#endregion
		#region NoteID
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXSearchable(SM.SearchCategory.PR, Messages.SearchableTitlePREmployee, new Type[] { typeof(acctCD), typeof(acctName) },
			new Type[] { typeof(defContactID), typeof(acctCD), typeof(acctName), typeof(Contact.eMail) },
			NumberFields = new Type[] { typeof(acctCD) },
			Line1Format = "{1}{2}", Line1Fields = new Type[] { typeof(defContactID), typeof(Contact.eMail), typeof(Contact.phone1) },
			Line2Format = "{0}{1}{2}{3}", Line2Fields = new Type[] { typeof(employeeClassID), typeof(payGroupID), typeof(departmentID), typeof(EPDepartment.description) },
			SelectForFastIndexing = typeof(Select2<PREmployee, InnerJoin<Contact, On<Contact.contactID, Equal<defContactID>>>>)
		 )]
        [PXNote(
			Selector = typeof(Search2<PREmployee.acctCD,
				InnerJoin<GL.Branch, 
					On<GL.Branch.bAccountID, Equal<PREmployee.parentBAccountID>>,
				LeftJoin<BAccount, 
					On<BAccount.bAccountID, Equal<PREmployee.bAccountID>>>>,
				Where2<MatchWithBranch<GL.Branch.branchID>,
					And<Where2<MatchWithPayGroup<PREmployee.payGroupID>, 
						And<BAccount.bAccountID, IsNull, 
							Or<Match<BAccount, Current<AccessInfo.userName>>>>>>>>),
			DescriptionField = typeof(PREmployee.acctCD),
			ShowInReferenceSelector = true,
			PopupTextEnabled = true)]
		public override Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
		#region ActivePositionID
		public abstract class activePositionID : PX.Data.BQL.BqlString.Field<activePositionID> { }
		[PXString(10, IsUnicode = true)]
		[PXDBScalar(typeof(SearchFor<EPEmployeePosition.positionID>
			.Where<EPEmployeePosition.employeeID.IsEqual<PREmployee.bAccountID>
				.And<EPEmployeePosition.isActive.IsEqual<True>>>))]
		[PXSelector(typeof(EPPosition.positionID), DescriptionField = typeof(EPPosition.description))]
		[PXUIField(DisplayName = "Position", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string ActivePositionID { get; set; }
		#endregion ActivePositionID		
		#region CountryID
		public abstract class countryID : BqlString.Field<countryID> { }
		[PXString(2, IsFixed = true)]
		[PXDBScalar(typeof(SelectFrom<BranchAddress>
			.InnerJoin<BranchAddressBAccount>.On<BranchAddress.addressID.IsEqual<BranchAddressBAccount.defAddressID>>
			.InnerJoin<BAccount>.On<BranchAddressBAccount.bAccountID.IsEqual<BAccount.parentBAccountID>>
			.Where<BAccount.bAccountID.IsEqual<PREmployee.bAccountID>>
			.SearchFor<BranchAddress.countryID>))]
		[PXUnboundDefault(typeof(SelectFrom<BranchAddress>
			.InnerJoin<BranchAddressBAccount>.On<BranchAddress.addressID.IsEqual<BranchAddressBAccount.defAddressID>>
			.Where<BranchAddressBAccount.bAccountID.IsEqual<PREmployee.parentBAccountID.FromCurrent>>
			.SearchFor<BranchAddress.countryID>))]
		public string CountryID { get; set; }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class BranchAddress : Address
	{
		public new abstract class addressID : PX.Data.BQL.BqlInt.Field<addressID> { }
		public new abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
	}

	[Serializable]
	[PXHidden]
	public class BranchAddressBAccount : BAccount
	{
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
		public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
	}
}
