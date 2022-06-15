using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using System;
using System.Diagnostics;

namespace PX.Objects.PR
{
	[Serializable]
	[PXCacheName(Messages.PRPTOBank)]
	[DebuggerDisplay("{GetType().Name,nq}: BankID = {BankID,nq}, StartDate = {StartDate,nq}")]
	public class PRPTOBank : IBqlTable, IPTOBank
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRPTOBank>.By<bankID>
		{
			public static PRPTOBank Find(PXGraph graph, string bankID) => FindBy(graph, bankID);
		}

		public static class FK
		{
			public class DisbursingEarningType : EPEarningType.PK.ForeignKeyOf<PRPTOBank>.By<earningTypeCD> { }
			public class PTOExpenseAccount : Account.PK.ForeignKeyOf<PRPTOBank>.By<ptoExpenseAcctID> { }
			public class PTOExpenseSubaccount : Sub.PK.ForeignKeyOf<PRPTOBank>.By<ptoExpenseSubID> { }
			public class PTOLiabilityAccount : Account.PK.ForeignKeyOf<PRPTOBank>.By<ptoLiabilityAcctID> { }
			public class PTOLiabilitySubaccount : Sub.PK.ForeignKeyOf<PRPTOBank>.By<ptoLiabilitySubID> { }
			public class PTOAssetAccount : Account.PK.ForeignKeyOf<PRPTOBank>.By<ptoAssetAcctID> { }
			public class PTOAssetSubaccount : Sub.PK.ForeignKeyOf<PRPTOBank>.By<ptoAssetSubID> { }
		}
		#endregion

		#region BankID
		[PXDBString(3, IsKey = true, IsUnicode = true, InputMask = ">CCC")]
		[PXUIField(DisplayName = "Bank ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault]
		[PXSelector(typeof(SearchFor<PRPTOBank.bankID>), DescriptionField = typeof(PRPTOBank.description))]
		[PXReferentialIntegrityCheck]
		public virtual string BankID { get; set; }
		public abstract class bankID : PX.Data.BQL.BqlString.Field<bankID> { }
		#endregion

		#region Description
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string Description { get; set; }
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		#endregion

		#region AccrualMethod
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Accrual Method")]
		[PXDefault(typeof(PTOAccrualMethod.percentage))]
		[PXUIEnabled(typeof(Where<createFinancialTransaction.IsEqual<False>>))]
		[PTOAccrualMethod.List]
		public virtual string AccrualMethod { get; set; }
		public abstract class accrualMethod : PX.Data.BQL.BqlString.Field<accrualMethod> { }
		#endregion

		#region AccrualRate
		[PXDBDecimal(6, MinValue = 0)]
		[PXUIField(DisplayName = "Default Accrual %")]
		[PXDefault(TypeCode.Decimal, "0")]
		[PXUIVisible(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>))]
		public virtual Decimal? AccrualRate { get; set; }
		public abstract class accrualRate : PX.Data.BQL.BqlDecimal.Field<accrualRate> { }
		#endregion

		#region HoursPerYear
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Hours per Year")]
		[PXDefault(TypeCode.Decimal, "0")]
		[PXUIVisible(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.totalHoursPerYear>>))]
		public virtual Decimal? HoursPerYear { get; set; }
		public abstract class hoursPerYear : PX.Data.BQL.BqlDecimal.Field<hoursPerYear> { }
		#endregion

		#region EarningTypeCD
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXUIField(DisplayName = "Disbursing Earning Type")]
		[PXSelector(typeof(SearchFor<EPEarningType.typeCD>.
			Where<EPEarningType.isActive.IsEqual<True>.
				And<EPEarningType.typeCD.IsNotInSubselect<SearchFor<PRPTOBank.earningTypeCD>.Where<bankID.FromCurrent.IsNull.Or<PRPTOBank.bankID.IsNotEqual<bankID.FromCurrent>>>>>.
				And<PREarningType.isPTO.IsEqual<True>>>), DescriptionField = typeof(EPEarningType.description))]
		[PXDefault]
		[PXCheckUnique(ErrorMessage = Messages.DuplicateEarningType)]
		[PXForeignReference(typeof(Field<earningTypeCD>.IsRelatedTo<EPEarningType.typeCD>))]
		public virtual string EarningTypeCD { get; set; }
		public abstract class earningTypeCD : PX.Data.BQL.BqlString.Field<earningTypeCD> { }
		#endregion

		#region IsActive
		[PXDBBool]
		[PXUIField(DisplayName = "Active")]
		[PXDefault(true)]
		public virtual bool? IsActive { get; set; } //ToDo AC-149516: Check that the Earning Type is still correct when the PTOBank is re-activated.
		public abstract class isActive : PX.Data.BQL.BqlBool.Field<isActive> { }
		#endregion

		#region IsCertifiedJobAccrual
		[PXDBBool]
		[PXUIField(DisplayName = "Accrue on Certified Job Only")]
		[PXDefault(false)]
		public virtual bool? IsCertifiedJobAccrual { get; set; }
		public abstract class isCertifiedJobAccrual : PX.Data.BQL.BqlBool.Field<isCertifiedJobAccrual> { }
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
		[PXUIField(DisplayName = "Start Date", Required = true)]
		[PXDefault]
		public virtual DateTime? StartDate { get; set; }
		public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }
		#endregion

		#region PTOYearStartDate
		[PXDate]
		public virtual DateTime? PTOYearStartDate { get => StartDate; set => StartDate = value; }
		public abstract class pTOYearStartDate : PX.Data.BQL.BqlDateTime.Field<pTOYearStartDate> { }
		#endregion

		#region CarryoverType
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Carryover Type")]
		[PXDefault(typeof(CarryoverType.none))]
		[PXUIEnabled(typeof(Where<createFinancialTransaction.IsEqual<False>.Or<Not<FeatureInstalled<FeaturesSet.payrollCAN>>>>))]
		[PXFormula(typeof(CarryoverType.total.When<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>
			.Else<carryoverType>))]
		[CarryoverType.List]
		public virtual string CarryoverType { get; set; }
		public abstract class carryoverType : PX.Data.BQL.BqlString.Field<carryoverType> { }
		#endregion

		#region CarryoverAmount
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Carryover Amount")]
		[PXUIEnabled(typeof(Where<carryoverType.IsEqual<CarryoverType.partial>>))]
		[PXFormula(typeof(carryoverAmount.When<carryoverType.IsEqual<CarryoverType.partial>>.Else<decimal0>))]
		public virtual Decimal? CarryoverAmount { get; set; }
		public abstract class carryoverAmount : PX.Data.BQL.BqlDecimal.Field<carryoverAmount> { }
		#endregion

		#region FrontLoadingAmount
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Front Loading Amount")]
		[PXDefault(TypeCode.Decimal, "0")]
		[PXUIEnabled(typeof(Where<createFinancialTransaction.IsEqual<False>.Or<Not<FeatureInstalled<FeaturesSet.payrollCAN>>>>))]
		[PXFormula(typeof(decimal0.When<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>
			.Else<frontLoadingAmount>))]
		public virtual Decimal? FrontLoadingAmount { get; set; }
		public abstract class frontLoadingAmount : PX.Data.BQL.BqlDecimal.Field<frontLoadingAmount> { }
		#endregion

		#region AllowNegativeBalance
		[PXDBBool]
		[PXUIField(DisplayName = "Allow Negative Balance")]
		[PXDefault(true)]
		public virtual bool? AllowNegativeBalance { get; set; }
		public abstract class allowNegativeBalance : PX.Data.BQL.BqlBool.Field<allowNegativeBalance> { }
		#endregion

		#region CarryoverPayMonthLimit
		[PXDBInt(MinValue = 0, MaxValue = 12)]
		[PXUIField(DisplayName = "Pay Carryover after (Months)")]
		[PXUIEnabled(typeof(Where<carryoverType.IsEqual<CarryoverType.paidOnTimeLimit>>))]
		[PXFormula(typeof(carryoverPayMonthLimit.When<carryoverType.IsEqual<CarryoverType.paidOnTimeLimit>>.Else<Zero>))]
		public virtual int? CarryoverPayMonthLimit { get; set; }
		public abstract class carryoverPayMonthLimit : PX.Data.BQL.BqlInt.Field<carryoverPayMonthLimit> { }
		#endregion

		#region DisburseFromCarryover
		[PXDBBool]
		[PXUIField(DisplayName = "Can Only Disburse from Carryover")]
		[PXDefault(false)]
		public virtual bool? DisburseFromCarryover { get; set; }
		public abstract class disburseFromCarryover : PX.Data.BQL.BqlBool.Field<disburseFromCarryover> { }
		#endregion

		#region PTOExpenseAcctID
		public abstract class ptoExpenseAcctID : PX.Data.BQL.BqlInt.Field<ptoExpenseAcctID> { }
		[Account(DisplayName = "Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseAccount))]
		[PRPTOExpenseAccountRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOExpenseAcctID { get; set; }
		#endregion

		#region PTOExpenseSubID
		public abstract class ptoExpenseSubID : PX.Data.BQL.BqlInt.Field<ptoExpenseSubID> { }
		[SubAccount(typeof(ptoExpenseAcctID), DisplayName = "Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOExpenseSubaccount))]
		[PRPTOExpenseSubRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOExpenseSubID { get; set; }
		#endregion

		#region PTOLiabilityAcctID
		public abstract class ptoLiabilityAcctID : PX.Data.BQL.BqlInt.Field<ptoLiabilityAcctID> { }
		[Account(DisplayName = "Liability Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOLiabilityAccount))]
		[PRPTOLiabilityAccountRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOLiabilityAcctID { get; set; }
		#endregion

		#region PTOLiabilitySubID
		public abstract class ptoLiabilitySubID : PX.Data.BQL.BqlInt.Field<ptoLiabilitySubID> { }
		[SubAccount(typeof(ptoLiabilityAcctID), DisplayName = "Liability Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOLiabilitySubaccount))]
		[PRPTOLiabilitySubRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOLiabilitySubID { get; set; }
		#endregion

		#region PTOAssetAcctID
		public abstract class ptoAssetAcctID : PX.Data.BQL.BqlInt.Field<ptoAssetAcctID> { }
		[Account(DisplayName = "Asset Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOAssetAccount))]
		[PRPTOAssetAccountRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOAssetAcctID { get; set; }
		#endregion

		#region PTOAssetSubID
		public abstract class ptoAssetSubID : PX.Data.BQL.BqlInt.Field<ptoAssetSubID> { }
		[SubAccount(typeof(ptoAssetAcctID), DisplayName = "Asset Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXForeignReference(typeof(FK.PTOAssetSubaccount))]
		[PRPTOAssetSubRequired(GLAccountSubSource.PTOBank, typeof(Where<createFinancialTransaction.IsEqual<True>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		public virtual Int32? PTOAssetSubID { get; set; }
		#endregion

		#region CreateFinancialTransaction
		[PXDBBool]
		[PXUIField(DisplayName = "Create Financial Transaction", FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXDefault(false)]
		[PXUIEnabled(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>))]
		public virtual bool? CreateFinancialTransaction { get; set; }
		public abstract class createFinancialTransaction : PX.Data.BQL.BqlBool.Field<createFinancialTransaction> { }
		#endregion

		#region DisbursingType
		[PXDBString(1, IsFixed = true)]
		[PXDefault(PTODisbursingType.CurrentRate)]
		[PXUIField(DisplayName = "Default Disbursing Type", FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXUIVisible(typeof(Where<createFinancialTransaction.IsEqual<True>>))]
		[PTODisbursingType.List]
		public virtual string DisbursingType { get; set; }
		public abstract class disbursingType : PX.Data.BQL.BqlString.Field<disbursingType> { }
		#endregion

		#region SettlementBalanceType
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "On Settlement")]
		[PXDefault(typeof(SettlementBalanceType.pay))]
		[SettlementBalanceType.List]
		public virtual string SettlementBalanceType { get; set; }
		public abstract class settlementBalanceType : PX.Data.BQL.BqlString.Field<settlementBalanceType> { }
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
