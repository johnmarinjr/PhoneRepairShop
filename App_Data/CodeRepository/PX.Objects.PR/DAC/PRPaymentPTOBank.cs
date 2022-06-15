using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.EP;
using System;
using System.Diagnostics;

namespace PX.Objects.PR
{
	[Serializable]
	[PXCacheName(Messages.PRPaymentPTOBank)]
	[DebuggerDisplay("{GetType().Name,nq}: DocType = {DocType,nq}, RefNbr = {RefNbr,nq}, BankID = {BankID,nq}, EffectiveStartDate = {EffectiveStartDate,nq}")]
	public class PRPaymentPTOBank : IBqlTable, PTOHelper.IPTOHistory
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRPaymentPTOBank>.By<docType, refNbr, bankID, effectiveStartDate>
		{
			public static PRPaymentPTOBank Find(PXGraph graph, string docType, string refNbr, string bankID, DateTime? effectiveStartDate) =>
				FindBy(graph, docType, refNbr, bankID, effectiveStartDate);
		}

		public static class FK
		{
			public class Payment : PRPayment.PK.ForeignKeyOf<PRPaymentPTOBank>.By<docType, refNbr> { }
			public class PTOBank : PRPTOBank.PK.ForeignKeyOf<PRPaymentPTOBank>.By<bankID> { }
			public class DisbursingEarningType : EPEarningType.PK.ForeignKeyOf<PRPaymentPTOBank>.By<earningTypeCD> { }
		}
		#endregion

		#region DocType
		[PXDBString(3, IsKey = true, IsFixed = true)]
		[PXUIField(DisplayName = "Type")]
		[PXDBDefault(typeof(PRPayment.docType))]
		[PayrollType.List]
		public string DocType { get; set; }
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion

		#region RefNbr
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr.")]
		[PXDBDefault(typeof(PRPayment.refNbr))]
		[PXParent(typeof(Select<PRPayment, 
			Where<PRPayment.docType, Equal<Current<PRPaymentPTOBank.docType>>,
			And<PRPayment.refNbr, Equal<Current<PRPaymentPTOBank.refNbr>>>>>))]
		public String RefNbr { get; set; }
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion

		#region BankID
		[PXDBString(3, IsKey = true, IsUnicode = true, InputMask = ">CCC")]
		[PXUIField(DisplayName = "PTO Bank", Enabled = false)]
		[PXSelector(typeof(SearchFor<PRPTOBank.bankID>), DescriptionField = typeof(PRPTOBank.description))]
		[PXForeignReference(typeof(Field<bankID>.IsRelatedTo<PREmployeePTOBank.bankID>))]
		public virtual string BankID { get; set; }
		public abstract class bankID : PX.Data.BQL.BqlString.Field<bankID> { }
		#endregion

		#region EffectiveStartDate
		[PXDBDate(IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Effective Date", Enabled = false)]
		public virtual DateTime? EffectiveStartDate { get; set; }
		public abstract class effectiveStartDate : PX.Data.BQL.BqlDateTime.Field<effectiveStartDate> { }
		#endregion

		#region EffectiveEndDate
		[PXDBDate(PreserveTime = true, UseTimeZone = false)]
		[PXUIField(DisplayName = "Effective End Date")]
		public virtual DateTime? EffectiveEndDate { get; set; }
		public abstract class effectiveEndDate : PX.Data.BQL.BqlDateTime.Field<effectiveEndDate> { }
		#endregion

		#region EarningTypeCD
		[PXString(EPEarningType.typeCD.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Disbursing Earning Type", Visible = false)]
		[PXFormula(typeof(Selector<PRPaymentPTOBank.bankID, PRPTOBank.earningTypeCD>))]
		public virtual string EarningTypeCD { get; set; }
		public abstract class earningTypeCD : PX.Data.BQL.BqlString.Field<earningTypeCD> { }
		#endregion

		#region IsActive
		[PXDBBool]
		[PXUIField(DisplayName = "Active")]
		[PXDefault(true)]
		public virtual bool? IsActive { get; set; }
		public abstract class isActive : PX.Data.BQL.BqlBool.Field<isActive> { }
		#endregion

		#region IsCertifiedJob
		[PXDBBool]
		[PXUIField(DisplayName = "Applies to Certified Job Only")]
		[PXDefault(false)]
		[PXUIEnabled(typeof(PRPaymentPTOBank.isActive))]
		public virtual bool? IsCertifiedJob { get; set; }
		public abstract class isCertifiedJob : PX.Data.BQL.BqlBool.Field<isCertifiedJob> { }
		#endregion

		#region AccrualMethod
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Accrual Method", Enabled = false)]
		[PTOAccrualMethod.List]
		public virtual string AccrualMethod { get; set; }
		public abstract class accrualMethod : PX.Data.BQL.BqlString.Field<accrualMethod> { }
		#endregion

		#region AccrualRate
		[PXDBDecimal(6, MinValue = 0)]
		[PXUIField(DisplayName = "Accrual %")]
		[PXUIEnabled(typeof(Where<PRPaymentPTOBank.isActive.IsEqual<True>.And<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>>))]
		[ShowValueWhen(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.percentage>>))]
		[PXDefault(TypeCode.Decimal, "0")]
		[PXDependsOnFields(typeof(accrualMethod))]
		public virtual Decimal? AccrualRate { get; set; }
		public abstract class accrualRate : PX.Data.BQL.BqlDecimal.Field<accrualRate> { }
		#endregion

		#region HoursPerYear
		[PXDBDecimal]
		[PXUIField(DisplayName = "Hours per Year")]
		[PXUIEnabled(typeof(Where<PRPaymentPTOBank.isActive.IsEqual<True>.And<accrualMethod.IsEqual<PTOAccrualMethod.totalHoursPerYear>>>))]
		[ShowValueWhen(typeof(Where<accrualMethod.IsEqual<PTOAccrualMethod.totalHoursPerYear>>))]
		[PXDefault(TypeCode.Decimal, "0")]
		[PXDependsOnFields(typeof(accrualMethod))]
		public virtual Decimal? HoursPerYear { get; set; }
		public abstract class hoursPerYear : PX.Data.BQL.BqlDecimal.Field<hoursPerYear> { }
		#endregion

		#region AccrualLimit
		[PXDBDecimal]
		[PXUIField(DisplayName = "Accrual Limit", Enabled = false)]
		public virtual Decimal? AccrualLimit
		{
			get => _AccrualLimit != 0 ? _AccrualLimit : null;
			set => _AccrualLimit = value;
		}
		private decimal? _AccrualLimit;
		public abstract class accrualLimit : PX.Data.BQL.BqlDecimal.Field<accrualLimit> { }
		#endregion

		#region AccumulatedAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Hours Accrued", Enabled = false)]
		public virtual Decimal? AccumulatedAmount { get; set; }
		public abstract class accumulatedAmount : PX.Data.BQL.BqlDecimal.Field<accumulatedAmount> { }
		#endregion

		#region AccumulatedMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Amount Accrued", Enabled = false, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[HideValueIfDisabled(false, typeof(Where<createFinancialTransaction, Equal<True>>))]
		public virtual Decimal? AccumulatedMoney { get; set; }
		public abstract class accumulatedMoney : PX.Data.BQL.BqlDecimal.Field<accumulatedMoney> { }
		#endregion

		#region UsedAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Hours Used", Enabled = false)]
		public virtual Decimal? UsedAmount { get; set; }
		public abstract class usedAmount : PX.Data.BQL.BqlDecimal.Field<usedAmount> { }
		#endregion

		#region UsedMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Amount Used", Enabled = false, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[HideValueIfDisabled(false, typeof(Where<createFinancialTransaction, Equal<True>>))]
		public virtual Decimal? UsedMoney { get; set; }
		public abstract class usedMoney : PX.Data.BQL.BqlDecimal.Field<usedMoney> { }
		#endregion

		#region AvailableAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Hours Available", Enabled = false)]
		public virtual Decimal? AvailableAmount { get; set; }
		public abstract class availableAmount : PX.Data.BQL.BqlDecimal.Field<availableAmount> { }
		#endregion

		#region AvailableMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Amount Available", Enabled = false, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[HideValueIfDisabled(false, typeof(Where<createFinancialTransaction, Equal<True>>))]
		public virtual Decimal? AvailableMoney { get; set; }
		public abstract class availableMoney : PX.Data.BQL.BqlDecimal.Field<availableMoney> { }
		#endregion

		#region AccruingHours
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? AccruingHours { get; set; }
		public abstract class accruingHours : PX.Data.BQL.BqlDecimal.Field<accruingHours> { }
		#endregion

		#region AccruingDays
		[PXDBInt]
		public virtual int? AccruingDays { get; set; }
		public abstract class accruingDays : PX.Data.BQL.BqlInt.Field<accruingDays> { }
		#endregion

		#region DaysInPeriod
		[PXDBInt]
		public virtual int? DaysInPeriod { get; set; }
		public abstract class daysInPeriod : PX.Data.BQL.BqlInt.Field<daysInPeriod> { }
		#endregion

		#region EffectiveCoefficient
		[PXDecimal]
		[PXDependsOnFields(typeof(daysInPeriod), typeof(accruingDays))]
		public virtual decimal? EffectiveCoefficient => DaysInPeriod.HasValue ? AccruingDays / (decimal)DaysInPeriod.Value : null;
		public abstract class effectiveCoefficient : PX.Data.BQL.BqlDecimal.Field<effectiveCoefficient> { }
		#endregion

		#region AccrualAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Accrual Hours")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PXUIEnabled(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		public virtual Decimal? AccrualAmount { get; set; }
		public abstract class accrualAmount : PX.Data.BQL.BqlDecimal.Field<accrualAmount> { }
		#endregion

		#region AccrualMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Accrual Amount", FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[HideValueIfDisabled(typeof(Where<createFinancialTransaction.IsEqual<True>
			.And<docType.IsEqual<PayrollType.adjustment>>>))]
		public virtual Decimal? AccrualMoney { get; set; }
		public abstract class accrualMoney : PX.Data.BQL.BqlDecimal.Field<accrualMoney> { }
		#endregion

		#region DisbursementAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Disbursement Hours")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PXUIEnabled(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		public virtual Decimal? DisbursementAmount { get; set; }
		public abstract class disbursementAmount : PX.Data.BQL.BqlDecimal.Field<disbursementAmount> { }
		#endregion

		#region DisbursementMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Disbursement Amount", FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[HideValueIfDisabled(typeof(Where<createFinancialTransaction.IsEqual<True>
			.And<docType.IsEqual<PayrollType.adjustment>>>))]
		public virtual Decimal? DisbursementMoney { get; set; }
		public abstract class disbursementMoney : PX.Data.BQL.BqlDecimal.Field<disbursementMoney> { }
		#endregion

		#region ProcessedFrontLoading
		[PXDBBool]
		[PXUIField(DisplayName = "Processed Front Loading", Visible = false)]
		[PXDefault(false)]
		public virtual bool? ProcessedFrontLoading { get; set; }
		public abstract class processedFrontLoading : PX.Data.BQL.BqlBool.Field<processedFrontLoading> { }
		#endregion

		#region FrontLoadingAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Front Loading Hours")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PXUIEnabled(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PTOProcessedFlag(typeof(processedFrontLoading))]
		public virtual Decimal? FrontLoadingAmount { get; set; }
		public abstract class frontLoadingAmount : PX.Data.BQL.BqlDecimal.Field<frontLoadingAmount> { }
		#endregion

		#region ProcessedCarryover
		[PXDBBool]
		[PXUIField(DisplayName = "Processed Carryover", Visible = false)]
		[PXDefault(false)]
		public virtual bool? ProcessedCarryover { get; set; }
		public abstract class processedCarryover : PX.Data.BQL.BqlBool.Field<processedCarryover> { }
		#endregion

		#region CarryoverAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Carryover Hours")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PXUIEnabled(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PTOProcessedFlag(typeof(processedCarryover))]
		public virtual Decimal? CarryoverAmount { get; set; }
		public abstract class carryoverAmount : PX.Data.BQL.BqlDecimal.Field<carryoverAmount> { }
		#endregion

		#region CarryoverMoney
		[PRCurrency]
		[PXUIField(DisplayName = "Carryover Amount", FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[HideValueIfDisabled(typeof(Where<createFinancialTransaction.IsEqual<True>
			.And<docType.IsEqual<PayrollType.adjustment>>>))]
		public virtual Decimal? CarryoverMoney { get; set; }
		public abstract class carryoverMoney : PX.Data.BQL.BqlDecimal.Field<carryoverMoney> { }
		#endregion

		#region ProcessedPaidCarryover
		[PXDBBool]
		[PXUIField(DisplayName = "Processed Paid Carryover", Visible = false)]
		[PXDefault(false)]
		public virtual bool? ProcessedPaidCarryover { get; set; }
		public abstract class processedPaidCarryover : PX.Data.BQL.BqlBool.Field<processedPaidCarryover> { }
		#endregion

		#region PaidCarryoverAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Paid Carryover Hours")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIVisible(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PXUIEnabled(typeof(Where<Parent<PRPayment.docType>, Equal<PayrollType.adjustment>>))]
		[PTOProcessedFlag(typeof(processedPaidCarryover))]
		public virtual Decimal? PaidCarryoverAmount { get; set; }
		public abstract class paidCarryoverAmount : PX.Data.BQL.BqlDecimal.Field<paidCarryoverAmount> { }
		#endregion

		#region TotalAccrual
		[PXDecimal]
		[PXUIField(DisplayName = "Total Accrual Hours", Enabled = false)]
		[PXFormula(typeof(Add<Add<PRPaymentPTOBank.accrualAmount, PRPaymentPTOBank.frontLoadingAmount>, PRPaymentPTOBank.carryoverAmount>))]
		public virtual Decimal? TotalAccrual { get; set; }
		public abstract class totalAccrual : PX.Data.BQL.BqlDecimal.Field<totalAccrual> { }
		#endregion

		#region TotalAccrualMoney
		[PXDecimal]
		[PXUIField(DisplayName = "Total Accrual Amount", Enabled = false, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXFormula(typeof(Add<accrualMoney, carryoverMoney>))]
		[HideValueIfDisabled(false, typeof(Where<createFinancialTransaction, Equal<True>>))]
		public virtual Decimal? TotalAccrualMoney { get; set; }
		public abstract class totalAccrualMoney : PX.Data.BQL.BqlDecimal.Field<totalAccrualMoney> { }
		#endregion

		#region TotalDisbursement
		[PXDecimal]
		[PXUIField(DisplayName = "Total Disbursement Hours", Enabled = false)]
		[PXFormula(typeof(Add<Add<PRPaymentPTOBank.disbursementAmount, PRPaymentPTOBank.paidCarryoverAmount>, PRPaymentPTOBank.settlementDiscardAmount>))]
		public virtual Decimal? TotalDisbursement { get; set; }
		public abstract class totalDisbursement : PX.Data.BQL.BqlDecimal.Field<totalDisbursement> { }
		#endregion

		#region TotalDisbursementMoney
		[PXDecimal]
		[PXUIField(DisplayName = "Total Disbursement Amount", Enabled = false, FieldClass = nameof(FeaturesSet.PayrollCAN))]
		[PXUnboundDefault(typeof(disbursementMoney.FromCurrent))]
		[HideValueIfDisabled(false, typeof(Where<createFinancialTransaction, Equal<True>>))]
		public virtual Decimal? TotalDisbursementMoney { get; set; }
		public abstract class totalDisbursementMoney : PX.Data.BQL.BqlDecimal.Field<totalDisbursementMoney> { }
		#endregion

		#region CreateFinancialTransaction
		[PXBool]
		[PXUnboundDefault(typeof(Selector<bankID, PRPTOBank.createFinancialTransaction>))]
		public virtual bool? CreateFinancialTransaction { get; set; }
		public abstract class createFinancialTransaction : PX.Data.BQL.BqlBool.Field<createFinancialTransaction> { }
		#endregion

		#region NbrOfPayPeriods
		[PXShort]
		[PXDBScalar(typeof(Search2<PRPayGroupYear.finPeriods,
			InnerJoin<PRPayment, On<PRPayment.payGroupID, Equal<PRPayGroupYear.payGroupID>,
				And<PRPayGroupYear.year, Equal<DatePart<DatePart.year, PRPayment.transactionDate>>>>>,
			Where<PRPayment.docType, Equal<docType>, And<PRPayment.refNbr, Equal<refNbr>>>>))]
		public virtual short? NbrOfPayPeriods { get; set; }
		public abstract class nbrOfPayPeriods : PX.Data.BQL.BqlShort.Field<nbrOfPayPeriods> { }
		#endregion NbrOfPayPeriods

		#region CalculationFormula
		[PXString]
		[PXUIField(DisplayName = "Total Accrual Calculation", Enabled = false)]
		[PXDependsOnFields(typeof(accrualMethod), typeof(accrualRate), typeof(accruingHours), typeof(frontLoadingAmount), typeof(carryoverAmount), typeof(totalAccrual), typeof(hoursPerYear),
			typeof(nbrOfPayPeriods), typeof(effectiveCoefficient), typeof(accruingDays), typeof(daysInPeriod))]
		public virtual string CalculationFormula
		{
			get
			{
				if (AccrualMethod == PTOAccrualMethod.Percentage)
				{
					decimal rate = AccrualRate / 100 ?? 0;
					decimal calculation = (rate * AccruingHours) + FrontLoadingAmount + CarryoverAmount ?? 0;
					if (Math.Round(calculation, 2) == TotalAccrual)
					{
						return $"({rate:0.00} * {AccruingHours:0.00}) + {FrontLoadingAmount:0.00} + {CarryoverAmount:0.00} = {TotalAccrual:0.00}";
					}
				}
				else if (AccrualMethod == PTOAccrualMethod.TotalHoursPerYear && NbrOfPayPeriods > 0)
				{
					decimal calculation = (HoursPerYear / NbrOfPayPeriods * EffectiveCoefficient) + FrontLoadingAmount + CarryoverAmount ?? 0;
					if (Math.Round(calculation, 2) == TotalAccrual)
					{
						return $"{HoursPerYear:0.00} / {NbrOfPayPeriods} * {AccruingDays} / {DaysInPeriod} + {FrontLoadingAmount:0.00} + {CarryoverAmount:0.00} = {TotalAccrual:0.00}";
					}
				}

				return Messages.Unknown;
			}

			set { }
		}
		public abstract class calculationFormula : PX.Data.BQL.BqlString.Field<calculationFormula> { }
		#endregion

		#region SettlementDiscardAmount
		[PXDBDecimal]
		[PXUIField(DisplayName = "Settlement Discard Amount", Enabled = false, Visible = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? SettlementDiscardAmount { get; set; }
		public abstract class settlementDiscardAmount : PX.Data.BQL.BqlDecimal.Field<settlementDiscardAmount> { }
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

	public class PTOProcessedFlagAttribute : PXEventSubscriberAttribute, IPXFieldUpdatedSubscriber
	{
		private Type _ProcessedFlagField;

		public PTOProcessedFlagAttribute(Type processedFlagField)
		{
			_ProcessedFlagField = processedFlagField;
		}

		public void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (!sender.Graph.IsImport)
			{
				return;
			}

			decimal? value = sender.GetValue(e.Row, _FieldName) as decimal?;
			if (value != null && value != 0)
			{
				sender.SetValue(e.Row, _ProcessedFlagField.Name, true);
			}
		}
	}
}
