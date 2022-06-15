using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PREarningDetail)]
	[Serializable]
	public class PREarningDetail : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREarningDetail>.By<recordID>
		{
			public static PREarningDetail Find(PXGraph graph, int? recordID) => FindBy(graph, recordID);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PREarningDetail>.By<employeeID> { }
			public class PayrollBatch : PRBatch.PK.ForeignKeyOf<PREarningDetail>.By<batchNbr> { }
			public class Payment : PRPayment.PK.ForeignKeyOf<PREarningDetail>.By<paymentDocType, paymentRefNbr> { }
			public class EarningType : EPEarningType.PK.ForeignKeyOf<PREarningDetail>.By<typeCD> { }
			public class Location : PRLocation.PK.ForeignKeyOf<PREarningDetail>.By<locationID> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<PREarningDetail>.By<branchID> { }
			public class Account : GL.Account.PK.ForeignKeyOf<PREarningDetail>.By<accountID> { }
			public class Subaccount : GL.Sub.PK.ForeignKeyOf<PREarningDetail>.By<subID> { }
			public class Project : PMProject.PK.ForeignKeyOf<PREarningDetail>.By<projectID> { }
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PREarningDetail>.By<projectTaskID> { }
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PREarningDetail>.By<costCodeID> { }
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PREarningDetail>.By<workCodeID> { }
			public class Union : PMUnion.PK.ForeignKeyOf<PREarningDetail>.By<unionID> { }
			public class LaborItem : InventoryItem.PK.ForeignKeyOf<PREarningDetail>.By<labourItemID> { }
			public class ShiftCode : EPShiftCode.PK.ForeignKeyOf<PREarningDetail>.By<shiftID> { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID { get; set; }
		#endregion
		#region ExcelRecordID
		public abstract class excelRecordID : PX.Data.BQL.BqlString.Field<excelRecordID> { }
		[PXString]
		[PXUIField(DisplayName = "Record ID")]
		public virtual string ExcelRecordID { get; set; }
		#endregion
		#region BaseOvertimeRecordID
		public abstract class baseOvertimeRecordID : PX.Data.BQL.BqlInt.Field<baseOvertimeRecordID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Base RecordID", Visible = false)]
		public virtual int? BaseOvertimeRecordID { get; set; }
		#endregion
		#region BasePTORecordID
		public abstract class basePTORecordID : PX.Data.BQL.BqlInt.Field<basePTORecordID> { }
		[PXDBInt]
		public virtual int? BasePTORecordID { get; set; }
		#endregion
		#region SortingRecordID
		public abstract class sortingRecordID : PX.Data.BQL.BqlInt.Field<sortingRecordID> { }
		[PXInt]
		[PXUIField(DisplayName = "Sorting RecordID", Visible = false)]
		[PXFormula(typeof(baseOvertimeRecordID.When<baseOvertimeRecordID.IsNotNull.And<isFringeRateEarning.IsNotEqual<True>>>
			.Else<basePTORecordID>.When<basePTORecordID.IsNotNull.And<isFringeRateEarning.IsNotEqual<True>>>
			.Else<int0.Subtract<recordID>>.When<recordID.IsLess<int0>>
			.Else<recordID>))]
		public virtual int? SortingRecordID { get; set; }
		#endregion
		#region AllowCopy
		public abstract class allowCopy : PX.Data.BQL.BqlBool.Field<allowCopy> { }
		[PXBool]
		[PXUIField(Visible = false, Enabled = false)]
		[PXFormula(typeof(Where<baseOvertimeRecordID.IsNull
			.And<basePTORecordID.IsNull>
			.And<isFringeRateEarning.IsNotEqual<True>>
			.And<isPayingCarryover.IsNotEqual<True>>>))]
		public virtual bool? AllowCopy { get; set; }
		#endregion
		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		[PXDBInt]
		public int? EmployeeID { get; set; }
		#endregion
		#region BatchNbr
		public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Batch Number")]
		[PXDBDefault(typeof(PRBatch.batchNbr), DefaultForUpdate = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXParent(typeof(Select<PRBatch, Where<PRBatch.batchNbr, Equal<Current<PREarningDetail.batchNbr>>>>))]
		public string BatchNbr { get; set; }
		#endregion
		#region PaymentDocType
		public abstract class paymentDocType : PX.Data.BQL.BqlString.Field<paymentDocType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Payment Doc. Type")]
		public string PaymentDocType { get; set; }
		#endregion
		#region PaymentRefNbr
		public abstract class paymentRefNbr : PX.Data.BQL.BqlString.Field<paymentRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Ref. Number")]
		[PXParent(typeof(Select<PRPayment, Where<PRPayment.docType, Equal<Current<PREarningDetail.paymentDocType>>, And<PRPayment.refNbr, Equal<Current<PREarningDetail.paymentRefNbr>>>>>))]
		public string PaymentRefNbr { get; set; }
		#endregion
		#region Date
		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }
		[PXDBDate]
		[PXDefault]
		[PXUIField(DisplayName = "Date")]
		public DateTime? Date { get; set; }
		#endregion
		#region TypeCD
		public abstract class typeCD : PX.Data.BQL.BqlString.Field<typeCD> { }
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXDefault]
		[PXUIField(DisplayName = "Code")]
		[PXRestrictor(typeof(Where<EPEarningType.isActive.IsEqual<True>>), Messages.EarningTypeIsNotActive, typeof(EPEarningType.typeCD))]
		[PREarningTypeSelector]
		[EarningDetailType(typeof(hours), typeof(units), typeof(workCodeID), new Type[] { typeof(isAmountBased), typeof(isPiecework) })]
		[EarningTypeProjectTaskDefault(typeof(projectID), typeof(projectTaskID))]
		[EarningTypeLaborItemOverride(typeof(employeeID), typeof(labourItemID))]
		[PXForeignReference(typeof(Field<typeCD>.IsRelatedTo<EPEarningType.typeCD>))] //ToDo: AC-142439 Ensure PXForeignReference attribute works correctly with PXCacheExtension DACs.
		[PXParent(typeof(Select<PRPaymentEarning,
							Where<PRPaymentEarning.docType,
								Equal<Current<PREarningDetail.paymentDocType>>,
							And<PRPaymentEarning.refNbr,
								Equal<Current<PREarningDetail.paymentRefNbr>>,
							And<PRPaymentEarning.typeCD,
								Equal<Current<PREarningDetail.typeCD>>,
							And<PRPaymentEarning.locationID,
								Equal<Current<PREarningDetail.locationID>>>>>>>), ParentCreate = true)]
		public string TypeCD { get; set; }
		#endregion
		#region IsOvertime
		public abstract class isOvertime : PX.Data.BQL.BqlBool.Field<isOvertime> { }
		[PXBool]
		[PXFormula(typeof(Selector<typeCD, EPEarningType.isOvertime>))]
		[PXUIField(DisplayName = "Overtime", Visible = false)]
		public bool? IsOvertime { get; set; }
		#endregion
		#region IsPiecework
		public abstract class isPiecework : PX.Data.BQL.BqlBool.Field<isPiecework> { }
		[PXBool]
		[PXFormula(typeof(Selector<typeCD, PREarningType.isPiecework>))]
		[PXUIField(DisplayName = "Piecework", Visible = false)]
		public bool? IsPiecework { get; set; }
		#endregion
		#region IsAmountBased
		public abstract class isAmountBased : PX.Data.BQL.BqlBool.Field<isAmountBased> { }
		[PXBool]
		[PXFormula(typeof(Selector<typeCD, PREarningType.isAmountBased>))]
		[PXUIField(DisplayName = "Amount Based", Visible = false)]
		public bool? IsAmountBased { get; set; }
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Location")]
		[PXSelector(
			typeof(SelectFrom<PRLocation>
				.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<employeeID.FromCurrent>>
				.SearchFor<PRLocation.locationID>),
			SubstituteKey = typeof(PRLocation.locationCD))]
		[PRLocationDefault(typeof(employeeID), typeof(projectID))]
		[WorkLocationRestrictor]
		public int? LocationID { get; set; }
		#endregion
		#region Hours
		public abstract class hours : PX.Data.BQL.BqlDecimal.Field<hours> { }
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Hours")]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>))]
		[PXUIRequired(typeof(isAmountBased.IsNotEqual<True>.And<unitType.IsEqual<UnitType.hour>>))]
		[PXDefault(typeof(Switch<Case<Where<isAmountBased, Equal<True>>, Null>, decimal0>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUnboundFormula(typeof(hours.When<isFringeRateEarning.IsEqual<False>.And<isPayingCarryover.IsEqual<False>>>.Else<decimal0>), typeof(SumCalc<PRBatchEmployee.hourQty>))]
		[PXUnboundFormula(typeof(hours.When<isFringeRateEarning.IsEqual<False>.And<isPayingCarryover.IsEqual<False>>>.Else<decimal0>), typeof(SumCalc<PRPayment.totalHours>))]
		[PXUnboundFormula(typeof(hours.When<isFringeRateEarning.IsEqual<False>>.Else<decimal0>), typeof(SumCalc<PRPaymentEarning.hours>))]
		public decimal? Hours { get; set; }
		#endregion
		#region Units
		public abstract class units : PX.Data.BQL.BqlDecimal.Field<units> { }
		[PXDBDecimal(MinValue = 0)]
		[PXUIField(DisplayName = "Units")]
		[PXUIVisible(typeof(Where<GetSetupValue<PRSetup.enablePieceworkEarningType>, Equal<True>>))]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>.And<unitType.IsEqual<UnitType.misc>>))]
		[PXUIRequired(typeof(isAmountBased.IsNotEqual<True>.And<unitType.IsEqual<UnitType.misc>>))]
		[PXDefault(typeof(Switch<Case<Where<unitType, Equal<UnitType.hour>, Or<isAmountBased, Equal<True>>>, Null>, decimal0>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? Units { get; set; }
		#endregion
		#region UnitType
		public abstract class unitType : PX.Data.BQL.BqlString.Field<unitType> { }
		[PXDBString(3, IsFixed = true)]
		[UnitType.List]
		[PXUIField(DisplayName = "Unit Type", Enabled = false)]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>))]
		[PXFormula(typeof(Switch<
			Case<Where<isAmountBased, Equal<True>>, Null, 
			Case<Where<isPiecework, Equal<True>>, UnitType.misc>>, 
			UnitType.hour>))]
		[PXUIVisible(typeof(Where<GetSetupValue<PRSetup.enablePieceworkEarningType>, Equal<True>>))]
		public string UnitType { get; set; }
		#endregion
		#region Rate
		public abstract class rate : PX.Data.BQL.BqlDecimal.Field<rate> { }
		[PRCurrency]
		[PXUIField(DisplayName = "Rate")]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>.And<isRegularRate.IsNotEqual<True>>))]
		[PXUIRequired(typeof(isAmountBased.IsNotEqual<True>.And<isRegularRate.IsNotEqual<True>>))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PayRate(typeof(Where<isFringeRateEarning.IsEqual<False>>))]
		[PayRatePrecision]
		public decimal? Rate { get; set; }
		#endregion
		#region ManualRate
		public abstract class manualRate : PX.Data.BQL.BqlBool.Field<manualRate> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Manual Rate")]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>.And<isRegularRate.IsNotEqual<True>>))]
		[PXFormula(typeof(Switch<Case<Where<isAmountBased, Equal<True>>, False>, manualRate>))]
		public bool? ManualRate { get; set; }
		#endregion
		#region IsRegularRate
		public abstract class isRegularRate : PX.Data.BQL.BqlBool.Field<isRegularRate> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(Visible = false)]
		public bool? IsRegularRate { get; set; }
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PRCurrency]
		[PXUIField(DisplayName = "Amount")]
		[PXUIEnabled(typeof(isAmountBased.IsEqual<True>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXFormula(typeof(Switch<Case<Where<isAmountBased, NotEqual<True>, And<isRegularRate, NotEqual<True>>>,
			Mult<hours.When<unitType.IsEqual<UnitType.hour>>.Else<units>, rate>>, 
			amount>),
			typeof(SumCalc<PRBatchEmployee.amount>))]
		[PXFormula(null, typeof(SumCalc<PRPaymentEarning.amount>))]
		[PXFormula(null, typeof(SumCalc<PRPayment.totalEarnings>))]
		public virtual Decimal? Amount { get; set; }
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[GL.Branch(typeof(Parent<PRPayment.branchID>), IsDetail = false)]
		public int? BranchID { get; set; }
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		[ProjectWithWarnings(DisplayName = "Project", WarnOfStatus = true)]
		public int? ProjectID { get; set; }
		#endregion
		#region ProjectTaskID
		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
		[ProjectTask(typeof(projectID), DisplayName = "Task", AllowNull = false)]
		[EarningTaskStatusWarning]
		public int? ProjectTaskID { get; set; }
		#endregion
		#region LabourItemID
		public abstract class labourItemID : Data.BQL.BqlInt.Field<labourItemID> { }
		[PRLaborItem(typeof(projectID), typeof(typeCD), typeof(Select<EPEmployee, Where<EPEmployee.bAccountID, Equal<Current<employeeID>>>>))]
		[PXForeignReference(typeof(Field<labourItemID>.IsRelatedTo<InventoryItem.inventoryID>))]
		[PXFormula(typeof(Default<typeCD, projectID, employeeID>))]
		public virtual int? LabourItemID { get; set; }
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[EarningsAccount(typeof(PREarningDetail.branchID),
		   typeof(PREarningDetail.typeCD),
		   typeof(PREarningDetail.employeeID),
		   typeof(PRPayment.payGroupID),
		   typeof(PREarningDetail.typeCD),
		   typeof(PREarningDetail.labourItemID),
		   typeof(PREarningDetail.projectID),
		   typeof(PREarningDetail.projectTaskID))]
		public virtual Int32? AccountID { get; set; }
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[EarningSubAccount(typeof(PREarningDetail.accountID), typeof(PREarningDetail.branchID), true, DisplayName = "Subaccount", Visibility = PXUIVisibility.Visible, Filterable = true)]
		public virtual int? SubID { get; set; }
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		[CostCode(typeof(accountID), typeof(projectTaskID), GL.AccountType.Expense, SkipVerificationForDefault = true, ReleasedField = typeof(released))]
		[PXForeignReference(typeof(Field<costCodeID>.IsRelatedTo<PMCostCode.costCodeID>))]
		public virtual Int32? CostCodeID { get; set; }
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
		#region SourceType
		public abstract class sourceType : PX.Data.BQL.BqlString.Field<sourceType> { }
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Data Source Type")]
		[EarningDetailSourceType.List]
		public string SourceType { get; set; }
		#endregion
		#region SourceNoteID
		public abstract class sourceNoteID : PX.Data.BQL.BqlGuid.Field<sourceNoteID> { }
		[PXDBGuid]
		[PXSelector(typeof(PMTimeActivity.noteID), SubstituteKey = typeof(PMTimeActivity.summary))]
		[PXUIField(DisplayName = "Time Activity", Enabled = false)]
		public Guid? SourceNoteID { get; set; }
		#endregion
		#region TimeCardMinutes
		public abstract class timeCardMinutes : PX.Data.BQL.BqlDecimal.Field<timeCardMinutes> { }
		[PXInt]
		[PXDBScalar(typeof(SearchFor<PMTimeActivity.timeSpent>.Where<sourceType.IsEqual<EarningDetailSourceType.timeActivity>.And<PMTimeActivity.noteID.IsEqual<sourceNoteID>>>))]
		[PXUnboundDefault(typeof(SearchFor<PMTimeActivity.timeSpent>.Where<sourceType.FromCurrent.IsEqual<EarningDetailSourceType.timeActivity>.And<PMTimeActivity.noteID.IsEqual<sourceNoteID.FromCurrent>>>))]
		[PXFormula(typeof(Default<sourceNoteID>))]
		public int? TimeCardMinutes { get; set; }
		#endregion
		#region SourceCommnPeriod
		public abstract class sourceCommnPeriod : PX.Data.BQL.BqlString.Field<sourceCommnPeriod> { }
		[PXDBString(6, IsUnicode = true)]
		[PXUIField(DisplayName = "Source Commn Period")]
		public string SourceCommnPeriod { get; set; }
		#endregion
		#region UnionID
		public abstract class unionID : Data.BQL.BqlString.Field<unionID> { }
		[PXForeignReference(typeof(Field<unionID>.IsRelatedTo<PMUnion.unionID>))]
		[PRUnion]
		[PXDefault(typeof(
			SelectFrom<PREmployee>.
			Where<PREmployee.bAccountID.IsEqual<employeeID.FromCurrent>>.
			SearchFor<PREmployee.unionID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<employeeID>))]
		public virtual string UnionID { get; set; }
		#endregion
		#region CertifiedJob
		public abstract class certifiedJob : Data.BQL.BqlBool.Field<certifiedJob> { }
		[PXDBBool()]
		[PXDefault(typeof(Coalesce<Search<PMProject.certifiedJob, Where<PMProject.contractID, Equal<Current<projectID>>>>,
			Search<PMProject.certifiedJob, Where<PMProject.nonProject, Equal<True>>>>))]
		[PXFormula(typeof(Default<projectID>))]
		[PXUIField(DisplayName = "Certified Job")]
		public virtual bool? CertifiedJob { get; set; }
		#endregion
		#region WorkCodeID
		public abstract class workCodeID : Data.BQL.BqlString.Field<workCodeID> { }
		[PXForeignReference(typeof(FK.WorkCode))]
		[PMWorkCode(typeof(costCodeID), typeof(projectID), typeof(projectTaskID), typeof(labourItemID), typeof(employeeID), FieldClass = null)]
		public virtual string WorkCodeID { get; set; }
		#endregion
		#region IsFringeRateEarning
		public abstract class isFringeRateEarning : Data.BQL.BqlBool.Field<isFringeRateEarning> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(Visible = false)]
		public virtual bool? IsFringeRateEarning { get; set; }
		#endregion
		#region IsPayingCarryover
		public abstract class isPayingCarryover : Data.BQL.BqlBool.Field<isPayingCarryover> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(Visible = false)]
		public virtual bool? IsPayingCarryover { get; set; }
		#endregion
		#region EmployeeAcctCD
		[PXString]
		[PXDBScalar(typeof(SearchFor<PREmployee.acctCD>.Where<PREmployee.bAccountID.IsEqual<employeeID>>))]
		[PXUnboundDefault(typeof(SearchFor<PREmployee.acctCD>.Where<PREmployee.bAccountID.IsEqual<employeeID.FromCurrent>>))]
		public virtual string EmployeeAcctCD { get; set; }
		public abstract class employeeAcctCD : PX.Data.BQL.BqlString.Field<employeeAcctCD> { }
		#endregion
		#region IsTimeActivityBillable
		public abstract class isTimeActivityBillable : Data.BQL.BqlBool.Field<isTimeActivityBillable> { }
		[PXBool]
		[PXDBScalar(typeof(SearchFor<PMTimeActivity.isBillable>.Where<PMTimeActivity.noteID.IsEqual<sourceNoteID>>))]
		[PXUnboundDefault(typeof(SearchFor<PMTimeActivity.isBillable>.Where<PMTimeActivity.noteID.IsEqual<sourceNoteID.FromCurrent>>))]
		public virtual bool? IsTimeActivityBillable { get; set; }
		#endregion
		#region ShiftID
		public abstract class shiftID : PX.Data.BQL.BqlInt.Field<shiftID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Shift Code", FieldClass = nameof(FeaturesSet.ShiftDifferential))]
		[PXUIEnabled(typeof(isAmountBased.IsNotEqual<True>.And<isRegularRate.IsNotEqual<True>.And<isFringeRateEarning.IsNotEqual<True>>>))]
		[ShowValueWhen(typeof(isAmountBased.IsNotEqual<True>.And<isRegularRate.IsNotEqual<True>.And<isFringeRateEarning.IsNotEqual<True>>>), true)]
		[DetailShiftCodeSelector(typeof(employeeID), typeof(date))]
		[EPShiftCodeActiveRestrictor]
		public virtual int? ShiftID { get; set; }
		#endregion
		#region IsPayingSettlement
		public abstract class isPayingSettlement : PX.Data.BQL.BqlBool.Field<isPayingSettlement> { }
		[PXDBBool]
		[PXUIField(Visible = false)]
		[PXDefault(false)]
		public bool? IsPayingSettlement { get; set; }
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
		#region PTOHelper
		#region PTODisbursementWithFinancialTransaction
		[PXBool]
		[PXUnboundDefault(false)]
		public bool? PTODisbursementWithFinancialTransaction { get; set; }
		public abstract class ptoDisbursementWithFinancialTransaction : Data.BQL.BqlBool.Field<ptoDisbursementWithFinancialTransaction> { }
		#endregion
		#region PTODisbursementWithAverageRate
		[PXBool]
		[PXUnboundDefault(false)]
		public bool? PTODisbursementWithAverageRate { get; set; }
		public abstract class ptoDisbursementWithAverageRate : Data.BQL.BqlBool.Field<ptoDisbursementWithAverageRate> { }
		#endregion
		#endregion
	}
}
