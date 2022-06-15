using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRPTODetail)]
	[Serializable]
	public class PRPTODetail : IBqlTable, IPaycheckExpenseDetail<string>
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRPTODetail>.By<recordID>
		{
			public static PRPTODetail Find(PXGraph graph, int? recordID) => FindBy(graph, recordID);
		}

		public class UK : PrimaryKeyOf<PRPTODetail>.By<branchID, paymentDocType, paymentRefNbr, bankID, projectID, projectTaskID, labourItemID, earningTypeCD, costCodeID>
		{
			public static PRPTODetail Find(PXGraph graph, int? branchID, string paymentDocType, string paymentRefNbr, int? bankID, int? projectID, int? projectTaskID, int? laborItemID, string earningTypeCD, int? costCodeID) =>
				FindBy(graph, branchID, paymentDocType, paymentRefNbr, bankID, projectID, projectTaskID, laborItemID, earningTypeCD, costCodeID);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PRPTODetail>.By<employeeID> { }
			public class Payment : PRPayment.PK.ForeignKeyOf<PRPTODetail>.By<paymentDocType, paymentRefNbr> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<PRPTODetail>.By<branchID> { }
			public class PTOBank : PRPTOBank.PK.ForeignKeyOf<PRPTODetail>.By<bankID> { }
			public class LaborItem : InventoryItem.PK.ForeignKeyOf<PRPTODetail>.By<labourItemID> { }
			public class EarningType : EPEarningType.PK.ForeignKeyOf<PRPTODetail>.By<earningTypeCD> { }
			public class ExpenseAccount : Account.PK.ForeignKeyOf<PRPTODetail>.By<expenseAccountID> { }
			public class ExpenseSubaccount : Sub.PK.ForeignKeyOf<PRPTODetail>.By<expenseSubID> { }
			public class LiabilityAccount : Account.PK.ForeignKeyOf<PRPTODetail>.By<liabilityAccountID> { }
			public class LiabilitySubaccount : Sub.PK.ForeignKeyOf<PRPTODetail>.By<liabilitySubID> { }
			public class AssetAccount : Account.PK.ForeignKeyOf<PRPTODetail>.By<assetAccountID> { }
			public class AssetSubaccount : Sub.PK.ForeignKeyOf<PRPTODetail>.By<assetSubID> { }
			public class Project : PMProject.PK.ForeignKeyOf<PRPTODetail>.By<projectID> { }
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PRPTODetail>.By<projectTaskID> { }
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PRPTODetail>.By<costCodeID> { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID { get; set; }
		#endregion
		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		[Employee]
		[PXDefault(typeof(PRPayment.employeeID.FromCurrent))]
		public int? EmployeeID { get; set; }
		#endregion
		#region PaymentDocType
		public abstract class paymentDocType : PX.Data.BQL.BqlString.Field<paymentDocType> { }
		[PXDBString(3, IsFixed = true)]
		[PXDBDefault(typeof(PRPayment.docType))]
		public string PaymentDocType { get; set; }
		#endregion
		#region PaymentRefNbr
		public abstract class paymentRefNbr : PX.Data.BQL.BqlString.Field<paymentRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXDBDefault(typeof(PRPayment.refNbr))]
		[PXParent(typeof(Select<PRPayment, Where<PRPayment.docType, Equal<Current<paymentDocType>>, And<PRPayment.refNbr, Equal<Current<paymentRefNbr>>>>>))]
		public string PaymentRefNbr { get; set; }
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[GL.Branch(typeof(Parent<PRPayment.branchID>), IsDetail = false)]
		public int? BranchID { get; set; }
		#endregion
		#region BankID
		public abstract class bankID : PX.Data.BQL.BqlString.Field<bankID> { }
		[PXDBString(3, IsUnicode = true)]
		[PXUIField(DisplayName = "PTO Bank", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault]
		[PXSelector(
			typeof(SelectFrom<PRPTOBank>
				.InnerJoin<PREmployeePTOBank>.On<PREmployeePTOBank.bankID.IsEqual<PRPTOBank.bankID>
					.And<PREmployeePTOBank.bAccountID.IsEqual<employeeID.FromCurrent>>>
				.Where<paymentDocType.FromCurrent.IsEqual<PayrollType.voidCheck>
					.Or<PRPTOBank.createFinancialTransaction.IsEqual<True>>>
				.SearchFor<PRPTOBank.bankID>),
			DescriptionField = typeof(PRPTOBank.description))]
		[PXCheckUnique(typeof(branchID), typeof(paymentRefNbr), typeof(paymentDocType), typeof(projectID), typeof(projectTaskID), typeof(labourItemID), typeof(earningTypeCD), typeof(costCodeID),
			ErrorMessage = Messages.CantDuplicatePTODetail,
			ClearOnDuplicate = false)]
		[PXRestrictor(typeof(Where<PRPTOBank.isActive.IsEqual<True>>),
			Messages.PTOBankNotActive)]
		public string BankID { get; set; }
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PRCurrency]
		[PXUIField(DisplayName = "Amount")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public virtual Decimal? Amount { get; set; }
		#endregion
		#region LabourItemID
		public abstract class labourItemID : PX.Data.BQL.BqlInt.Field<labourItemID> { }
		[PMLaborItem(typeof(projectID), null, null)]
		[PXForeignReference(typeof(FK.LaborItem))]
		[PXUIVisible(typeof(Where<CostAssignmentColumnVisibilityEvaluator.PTOLaborItem, Equal<True>>))]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public virtual int? LabourItemID { get; set; }
		#endregion
		#region EarningTypeCD
		public abstract class earningTypeCD : PX.Data.BQL.BqlString.Field<earningTypeCD> { }
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXUIField(DisplayName = "Earning Type Code")]
		[PREarningTypeSelector]
		[PXForeignReference(typeof(FK.EarningType))]
		[PXUIVisible(typeof(Where<CostAssignmentColumnVisibilityEvaluator.PTOEarningType, Equal<True>>))]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public string EarningTypeCD { get; set; }
		#endregion
		#region ExpenseAccountID
		public abstract class expenseAccountID : PX.Data.BQL.BqlInt.Field<expenseAccountID> { }
		[PTOExpenseAccount(
			typeof(bankID),
			typeof(employeeID),
			typeof(PRPayment.payGroupID),
			typeof(earningTypeCD),
			typeof(labourItemID),
			typeof(projectID),
			typeof(projectTaskID),
			DisplayName = "Expense Account")]
		public virtual int? ExpenseAccountID { get; set; }
		#endregion
		#region ExpenseSubID
		public abstract class expenseSubID : PX.Data.BQL.BqlInt.Field<expenseSubID> { }
		[PTOExpenseSubAccount(typeof(expenseAccountID), DisplayName = "Expense Sub.", Visibility = PXUIVisibility.Visible, Filterable = true)]
		public virtual int? ExpenseSubID { get; set; }
		#endregion
		#region LiabilityAccountID
		public abstract class liabilityAccountID : PX.Data.BQL.BqlInt.Field<liabilityAccountID> { }
		[PTOLiabilityAccount(
			typeof(bankID),
			typeof(employeeID),
			typeof(PRPayment.payGroupID),
			DisplayName = "Liability Account")]
		public virtual int? LiabilityAccountID { get; set; }
		#endregion
		#region LiabilitySubID
		public abstract class liabilitySubID : PX.Data.BQL.BqlInt.Field<liabilitySubID> { }
		[PTOLiabilitySubAccount(typeof(liabilityAccountID), DisplayName = "Liability Sub.", Visibility = PXUIVisibility.Visible, Filterable = true)]
		public virtual int? LiabilitySubID { get; set; }
		#endregion
		#region AssetAccountID
		public abstract class assetAccountID : PX.Data.BQL.BqlInt.Field<assetAccountID> { }
		[PTOAssetAccount(
			typeof(bankID),
			typeof(employeeID),
			typeof(PRPayment.payGroupID),
			DisplayName = "Asset Account")]
		public virtual int? AssetAccountID { get; set; }
		#endregion
		#region AssetSubID
		public abstract class assetSubID : PX.Data.BQL.BqlInt.Field<assetSubID> { }
		[PTOAssetSubAccount(typeof(assetAccountID), DisplayName = "Asset Sub.", Visibility = PXUIVisibility.Visible, Filterable = true)]
		public virtual int? AssetSubID { get; set; }
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		[ProjectBase(DisplayName = "Project")]
		[ProjectDefault]
		[PXUIVisible(typeof(Where<CostAssignmentColumnVisibilityEvaluator.PTOProject, Equal<True>>))]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public int? ProjectID { get; set; }
		#endregion
		#region ProjectTaskID
		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Task", FieldClass = ProjectAttribute.DimensionName)]
		[PXSelector(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>>>),
			typeof(PMTask.taskCD), typeof(PMTask.description), SubstituteKey = typeof(PMTask.taskCD))]
		[PXUIVisible(typeof(Where<CostAssignmentColumnVisibilityEvaluator.PTOProject, Equal<True>>))]
		[PXUIEnabled(typeof(Where<PRPayment.docType.FromCurrent.IsEqual<PayrollType.adjustment>>))]
		public int? ProjectTaskID { get; set; }
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		[CostCode(typeof(liabilityAccountID), typeof(projectTaskID), GL.AccountType.Expense, SkipVerificationForDefault = true, AllowNullValue = true, ReleasedField = typeof(released))]
		[PXForeignReference(typeof(FK.CostCode))]
		public virtual int? CostCodeID { get; set; }
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		/// <summary>
		/// Indicates whether the line is released or not.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? Released { get; set; }
		#endregion
		#region CreditAmountFromAssetAccount
		public abstract class creditAmountFromAssetAccount : PX.Data.BQL.BqlInt.Field<creditAmountFromAssetAccount> { }
		[PXDBDecimal]
		public virtual decimal? CreditAmountFromAssetAccount { get; set; }
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

		public string ParentKeyID { get => BankID; set => BankID = value; }
	}
}
