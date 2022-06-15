using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;

namespace PX.Objects.PR
{
	public sealed class PRxPMTask : PXCacheExtension<PMTask>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region Keys
		public static class FK
		{
			public class EarningsAccount : Account.PK.ForeignKeyOf<PMTask>.By<earningsAcctID> { }
			public class EarningsSubaccount : Sub.PK.ForeignKeyOf<PMTask>.By<earningsSubID> { }
			public class BenefitExpenseAccount : Account.PK.ForeignKeyOf<PMTask>.By<benefitExpenseAcctID> { }
			public class BenefitExpenseSubaccount : Sub.PK.ForeignKeyOf<PMTask>.By<benefitExpenseSubID> { }
			public class TaxExpenseAccount : Account.PK.ForeignKeyOf<PMTask>.By<taxExpenseAcctID> { }
			public class TaxExpenseSubaccount : Sub.PK.ForeignKeyOf<PMTask>.By<taxExpenseSubID> { }
			public class PTOExpenseAccount : Account.PK.ForeignKeyOf<PMTask>.By<ptoExpenseAcctID> { }
			public class PTOExpenseSubaccount : Sub.PK.ForeignKeyOf<PMTask>.By<ptoExpenseSubID> { }
		}
		#endregion

		#region EarningsAcctID
		public abstract class earningsAcctID : PX.Data.BQL.BqlInt.Field<earningsAcctID> { }
		[Account(DisplayName = "Earnings Account", Visible = false,  DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<earningsAcctID>.IsRelatedTo<Account.accountID>))]
		[PREarningAccountRequired(GLAccountSubSource.Task)]
		public int? EarningsAcctID { get; set; }
		#endregion

		#region EarningsSubID
		public abstract class earningsSubID : PX.Data.BQL.BqlInt.Field<earningsSubID> { }
		[SubAccount(typeof(earningsAcctID), Visible = false, DisplayName = "Earnings Sub.",  DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<earningsSubID>.IsRelatedTo<Sub.subID>))]
		[PREarningSubRequired(GLAccountSubSource.Task)]
		public int? EarningsSubID { get; set; }
		#endregion

		#region BenefitExpenseAcctID
		public abstract class benefitExpenseAcctID : PX.Data.BQL.BqlInt.Field<benefitExpenseAcctID> { }
		[Account(DisplayName = "Benefit Expense Account", Visible = false, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<benefitExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRBenExpenseAccountRequired(GLAccountSubSource.Task)]
		public int? BenefitExpenseAcctID { get; set; }
		#endregion

		#region BenefitExpenseSubID
		public abstract class benefitExpenseSubID : PX.Data.BQL.BqlInt.Field<benefitExpenseSubID> { }
		[SubAccount(typeof(benefitExpenseAcctID), Visible = false, DisplayName = "Benefit Expense Sub.",  DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<benefitExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRBenExpenseSubRequired(GLAccountSubSource.Task)]
		public int? BenefitExpenseSubID { get; set; }
		#endregion

		#region TaxExpenseAcctID
		public abstract class taxExpenseAcctID : PX.Data.BQL.BqlInt.Field<taxExpenseAcctID> { }
		[Account(DisplayName = "Tax Expense Account", Visible = false, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(Field<taxExpenseAcctID>.IsRelatedTo<Account.accountID>))]
		[PRTaxExpenseAccountRequired(GLAccountSubSource.Task)]
		public int? TaxExpenseAcctID { get; set; }
		#endregion

		#region TaxExpenseSubID
		public abstract class taxExpenseSubID : PX.Data.BQL.BqlInt.Field<taxExpenseSubID> { }
		[SubAccount(typeof(taxExpenseAcctID), Visible = false, DisplayName = "Tax Expense Sub.",  DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(Field<taxExpenseSubID>.IsRelatedTo<Sub.subID>))]
		[PRTaxExpenseSubRequired(GLAccountSubSource.Task)]
		public int? TaxExpenseSubID { get; set; }
		#endregion

		#region PTOExpenseAcctID
		public abstract class ptoExpenseAcctID : PX.Data.BQL.BqlInt.Field<ptoExpenseAcctID> { }
		[Account(DisplayName = "PTO Expense Account", Visible = false, DescriptionField = typeof(Account.description), AvoidControlAccounts = true, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseAccount))]
		[PRPTOExpenseAccountRequired(GLAccountSubSource.Task, typeof(Where<FeatureInstalled<FeaturesSet.payrollCAN>>))]
		public int? PTOExpenseAcctID { get; set; }
		#endregion

		#region PTOExpenseSubID
		public abstract class ptoExpenseSubID : PX.Data.BQL.BqlInt.Field<ptoExpenseSubID> { }
		[SubAccount(typeof(ptoExpenseAcctID), Visible = false, DisplayName = "PTO Expense Sub.", DescriptionField = typeof(Sub.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseSubaccount))]
		[PRPTOExpenseSubRequired(GLAccountSubSource.Task, typeof(Where<FeatureInstalled<FeaturesSet.payrollCAN>>))]
		public int? PTOExpenseSubID { get; set; }
		#endregion
	}
}
