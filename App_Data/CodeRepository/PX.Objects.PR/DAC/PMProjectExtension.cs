using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Payroll.Data;

namespace PX.Objects.PR
{
	public sealed class PMProjectExtension : PXCacheExtension<PMProject>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region Keys
		public static class FK
		{
			public class PayrollWorkLocation : PRLocation.PK.ForeignKeyOf<PMProject>.By<payrollWorkLocationID> { }
			public class BenefitCodeWithInjectedFringeRate : PRDeductCode.PK.ForeignKeyOf<PMProject>.By<benefitCodeReceivingFringeRate> { }
			public class EarningsAccount : Account.PK.ForeignKeyOf<PMProject>.By<earningsAcctID> { }
			public class EarningsSubaccount : Sub.PK.ForeignKeyOf<PMProject>.By<earningsSubID> { }
			public class BenefitExpenseAccount : Account.PK.ForeignKeyOf<PMProject>.By<benefitExpenseAcctID> { }
			public class BenefitExpenseSubaccount : Sub.PK.ForeignKeyOf<PMProject>.By<benefitExpenseSubID> { }
			public class TaxExpenseAccount : Account.PK.ForeignKeyOf<PMProject>.By<taxExpenseAcctID> { }
			public class TaxExpenseSubaccount : Sub.PK.ForeignKeyOf<PMProject>.By<taxExpenseSubID> { }
			public class PTOExpenseAccount : Account.PK.ForeignKeyOf<PMProject>.By<ptoExpenseAcctID> { }
			public class PTOExpenseSubaccount : Sub.PK.ForeignKeyOf<PMProject>.By<ptoExpenseSubID> { }
		}
		#endregion

		#region PayrollWorkLocationID
		public abstract class payrollWorkLocationID : BqlInt.Field<payrollWorkLocationID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Payroll Work Location", FieldClass = nameof(FeaturesSet.PayrollModule))]
		[PXSelector(typeof(PRLocation.locationID), SubstituteKey = typeof(PRLocation.locationCD))]
		[PXRestrictor(typeof(Where<PRLocation.isActive.IsEqual<True>>), Messages.LocationIsInactive, typeof(PRLocation.locationID))]
		public int? PayrollWorkLocationID { get; set; }
		#endregion
		#region WageAbovePrevailingAnnualizationException
		public abstract class wageAbovePrevailingAnnualizationException : BqlBool.Field<wageAbovePrevailingAnnualizationException> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Excess Pay Rate Annualization Exception")]
		[PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? WageAbovePrevailingAnnualizationException { get; set; }
		#endregion
		#region BenefitCodeReceivingFringeRate
		public abstract class benefitCodeReceivingFringeRate : BqlInt.Field<benefitCodeReceivingFringeRate> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Benefit Code to Use for the Fringe Rate")]
		[DeductionActiveSelector(
			typeof(Where<PRDeductCode.contribType.IsNotEqual<ContributionTypeListAttribute.employeeDeduction>
				.And<PRDeductCode.isCertifiedProject.IsEqual<True>>>),
			typeof(countryUS))]
		[PXForeignReference(typeof(FK.BenefitCodeWithInjectedFringeRate))]
		public int? BenefitCodeReceivingFringeRate { get; set; }
		#endregion
		#region FileEmptyCertifiedReport
		public abstract class fileEmptyCertifiedReport : PX.Data.BQL.BqlBool.Field<fileEmptyCertifiedReport> { }
		[PXDBBool]
		[PXUIField(DisplayName = "File Empty Report")]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? FileEmptyCertifiedReport { get; set; }
		#endregion
		#region ApplyOTMultiplierToFringeRate
		public abstract class applyOTMultiplierToFringeRate : PX.Data.BQL.BqlBool.Field<applyOTMultiplierToFringeRate> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Use Overtime Multiplier for Fringe Rate")]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? ApplyOTMultiplierToFringeRate { get; set; }
		#endregion

		#region EarningsAcctID
		public abstract class earningsAcctID : PX.Data.BQL.BqlInt.Field<earningsAcctID> { }
		[Account(DisplayName = "Earnings Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<earningsAcctID>.IsRelatedTo<Account.accountID>))]
		[PREarningAccountRequired(GLAccountSubSource.Project)]
		public int? EarningsAcctID { get; set; }
		#endregion

		#region EarningsSubID
		public abstract class earningsSubID : PX.Data.BQL.BqlInt.Field<earningsSubID> { }
		[SubAccount(typeof(earningsAcctID), DisplayName = "Earnings Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<earningsSubID>.IsRelatedTo<Sub.subID>))]
		[PREarningSubRequired(GLAccountSubSource.Project)]
		public int? EarningsSubID { get; set; }
		#endregion

		#region BenefitExpenseAcctID
		public abstract class benefitExpenseAcctID : PX.Data.BQL.BqlInt.Field<benefitExpenseAcctID> { }
		[Account(DisplayName = "Benefit Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<benefitExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRBenExpenseAccountRequired(GLAccountSubSource.Project)]
		public int? BenefitExpenseAcctID { get; set; }
		#endregion

		#region BenefitExpenseSubID
		public abstract class benefitExpenseSubID : PX.Data.BQL.BqlInt.Field<benefitExpenseSubID> { }
		[SubAccount(typeof(benefitExpenseAcctID), DisplayName = "Benefit Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<benefitExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRBenExpenseSubRequired(GLAccountSubSource.Project)]
		public int? BenefitExpenseSubID { get; set; }
		#endregion

		#region TaxExpenseAcctID
		public abstract class taxExpenseAcctID : PX.Data.BQL.BqlInt.Field<taxExpenseAcctID> { }
		[Account(DisplayName = "Tax Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<taxExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRTaxExpenseAccountRequired(GLAccountSubSource.Project)]
		public int? TaxExpenseAcctID { get; set; }
		#endregion

		#region TaxExpenseSubID
		public abstract class taxExpenseSubID : PX.Data.BQL.BqlInt.Field<taxExpenseSubID> { }
		[SubAccount(typeof(taxExpenseAcctID), DisplayName = "Tax Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<taxExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRTaxExpenseSubRequired(GLAccountSubSource.Project)]
		public int? TaxExpenseSubID { get; set; }
		#endregion
		
		#region PTOExpenseAcctID
		public abstract class ptoExpenseAcctID : PX.Data.BQL.BqlInt.Field<ptoExpenseAcctID> { }
		[Account(DisplayName = "PTO Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseAccount))]
		[PRPTOExpenseAccountRequired(GLAccountSubSource.Project, typeof(Where<FeatureInstalled<FeaturesSet.payrollCAN>>))]
		public int? PTOExpenseAcctID { get; set; }
		#endregion

		#region PTOExpenseSubID
		public abstract class ptoExpenseSubID : PX.Data.BQL.BqlInt.Field<ptoExpenseSubID> { }
		[SubAccount(typeof(ptoExpenseAcctID), DisplayName = "PTO Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseSubaccount))]
		[PRPTOExpenseSubRequired(GLAccountSubSource.Project, typeof(Where<FeatureInstalled<FeaturesSet.payrollCAN>>))]
		public int? PTOExpenseSubID { get; set; }
		#endregion

		#region CountryUS
		[PXString(2)]
		[PXUnboundDefault(typeof(BQLLocationConstants.CountryUS))]
		public string CountryUS { get; set; }
		public abstract class countryUS : BqlString.Field<countryUS> { }
		#endregion

	}
}
