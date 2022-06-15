using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Objects.PM.GraphExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.PM.RegisterEntry;

namespace PX.Objects.PR
{
	public class PRxJournalEntryTranRef : PXGraphExtension<JournalEntryTranRef>
	{
		public SelectFrom<PMTran>
			.LeftJoin<GLTran>
				.On<GLTran.pMTranID.IsEqual<PMTran.tranID>
					.And<GLTran.batchNbr.IsEqual<PMTran.batchNbr>>
					.And<GLTran.tranType.IsEqual<PMTran.tranType>>>
			.Where<GLTran.pMTranID.IsNull.And<PMTran.origRefID.IsIn<P.AsGuid>>>
			.OrderBy<PMTran.tranID.Asc>
			.View TimeModulePMTransactionsToReverse;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		public delegate string GetDocTypeDelegate(APTran apTran, ARTran arTran, GLTran glTran);
		[PXOverride]
		public virtual string GetDocType(APTran apTran, ARTran arTran, GLTran glTran, GetDocTypeDelegate baseMethod)
		{
			if (glTran.Module == BatchModule.PR)
			{
				switch (glTran.TranType)
				{
					case PayrollType.Regular:
						return PMOrigDocType.RegularPaycheck;
					case PayrollType.Special:
						return PMOrigDocType.SpecialPaycheck;
					case PayrollType.Adjustment:
						return PMOrigDocType.AdjustmentPaycheck;
					case PayrollType.VoidCheck:
						return PMOrigDocType.VoidPaycheck;
					default: return null;
				}
			}

			return baseMethod(apTran, arTran, glTran);
		}

		public delegate Guid? GetNoteIDDelegate(APTran apTran, ARTran arTran, GLTran glTran);
		[PXOverride]
		public virtual Guid? GetNoteID(APTran apTran, ARTran arTran, GLTran glTran, GetNoteIDDelegate baseMethod)
		{
			if (glTran.Module == BatchModule.PR)
			{
				return new SelectFrom<PRPayment>
					.Where<PRPayment.docType.IsEqual<P.AsString>
					.And<PRPayment.refNbr.IsEqual<P.AsString>>>.View(Base).SelectSingle(glTran.TranType, glTran.RefNbr).NoteID;
			}

			return baseMethod(apTran, arTran, glTran);
		}

		public delegate void AssignCustomerVendorEmployeeDelegate(GLTran glTran, PMTran pmTran);
		[PXOverride]
		public virtual void AssignCustomerVendorEmployee(GLTran glTran, PMTran pmTran, AssignCustomerVendorEmployeeDelegate baseMethod)
		{
			if (glTran.Module == BatchModule.PR)
			{
				pmTran.ResourceID = glTran.ReferenceID;
			}
			else
			{
				baseMethod(glTran, pmTran);
			}
		}

		public delegate void AssignAdditionalFieldsDelegate(GLTran glTran, PMTran pmTran);
		[PXOverride]
		public virtual void AssignAdditionalFields(GLTran glTran, PMTran pmTran, AssignAdditionalFieldsDelegate baseMethod)
		{
			baseMethod(glTran, pmTran);

			if (glTran.Module == BatchModule.PR)
			{
				PRxPMTran pmTranExt = PXCache<PMTran>.GetExtension<PRxPMTran>(pmTran);
				PRxGLTran glTranExt = PXCache<GLTran>.GetExtension<PRxGLTran>(glTran);
				pmTranExt.PayrollWorkLocationID = glTranExt.PayrollWorkLocationID;
				pmTran.Qty = glTran.Qty;
				pmTran.BillableQty = pmTran.Qty;
				pmTran.TranCuryUnitRate = glTran.Qty != null && glTran.Qty != 0 ? (glTran.DebitAmt - glTran.CreditAmt) / glTran.Qty : 0;
				if (!string.IsNullOrEmpty(glTranExt.EarningTypeCD))
				{
					pmTran.EarningType = glTranExt.EarningTypeCD;
					EPEarningType earningType = new SelectFrom<EPEarningType>.Where<EPEarningType.typeCD.IsEqual<P.AsString>>.View(Base).SelectSingle(glTranExt.EarningTypeCD);
					pmTran.OvertimeMultiplier = earningType?.OvertimeMultiplier;
				}
			}
		}

		public delegate List<TranWithInfo> GetAdditionalProjectTransDelegate(string module, string tranType, string refNbr);
		[PXOverride]
		public virtual List<TranWithInfo> GetAdditionalProjectTrans(string module, string tranType, string refNbr, GetAdditionalProjectTransDelegate baseMethod)
		{
			List<TranWithInfo> result = new List<TranWithInfo>();

			string projectCostAssignment = PRSetupHelper.GetPayrollPreferences(Base).ProjectCostAssignment;
			if (module == BatchModule.PR && projectCostAssignment != ProjectCostAssignmentType.NoCostAssigned)
			{
				RegisterEntry registerGraph = PXGraph.CreateInstance<RegisterEntry>();
				registerGraph.Clear();

				Dictionary<Guid?, PMTimeActivity> timeActivitiesToReverse = ImportTimeActivitiesHelper.GetTimeActivitiesToReverse(Base, refNbr);

				foreach (PXResult<PMTran, GLTran> record in TimeModulePMTransactionsToReverse.Select(timeActivitiesToReverse.Keys.ToArray()))
				{
					PMTran pmTran = record;
					PMTimeActivity timeActivity = timeActivitiesToReverse[pmTran.OrigRefID];
					Account account = null;
					PMAccountGroup accountGroup = null;

					if (pmTran.AccountID != null)
					{
						account = PXSelect<Account,
							Where<Account.accountID, Equal<Required<PMTran.accountID>>,
								And<Account.accountGroupID, IsNotNull>>>.Select(Base, pmTran.AccountID);

						if (account == null)
							continue;
					}

					if (account?.AccountGroupID != null)
					{
						accountGroup = PXSelect<PMAccountGroup,
							Where<PMAccountGroup.groupID, Equal<Required<Account.accountGroupID>>,
								And<PMAccountGroup.type, NotEqual<PMAccountType.offBalance>>>>.Select(Base, account.AccountGroupID);
					}
					else if (pmTran.AccountGroupID != null)
					{
						accountGroup = PXSelect<PMAccountGroup,
							Where<PMAccountGroup.groupID, Equal<Required<PMTran.accountGroupID>>,
								And<PMAccountGroup.type, Equal<PMAccountType.offBalance>>>>.Select(Base, pmTran.AccountGroupID);
					}

					if (accountGroup == null)
						continue;

					PMProject project = PXSelect<PMProject,
						Where<PMProject.contractID, Equal<Required<PMTran.projectID>>>>.Select(Base, pmTran.ProjectID);

					if (project == null)
						continue;

					PMTask task = PXSelect<PMTask,
						Where<PMTask.projectID, Equal<Required<GLTran.projectID>>,
							And<PMTask.taskID, Equal<Required<GLTran.taskID>>>>>.Select(Base, pmTran.ProjectID, pmTran.TaskID);

					if (task == null)
						continue;

					int sign = tranType == PayrollType.VoidCheck ? 1 : -1;
					int? timeSpent = (int)(sign * Math.Round(pmTran.Qty.GetValueOrDefault() * 60m, 2, MidpointRounding.AwayFromZero));
					int? timeBillable = (int)(sign * Math.Round(pmTran.BillableQty.GetValueOrDefault() * 60m, 2, MidpointRounding.AwayFromZero));

					PMTran transactionToReverse = registerGraph.CreateTransaction(new CreatePMTran(timeActivity, pmTran.ResourceID, timeActivity.Date.Value, timeSpent, timeBillable, pmTran.TranCuryUnitRate, pmTran.OvertimeMultiplier, null, false));
					transactionToReverse.TranType = BatchModule.PR;
					transactionToReverse.SubID = pmTran.SubID;
					transactionToReverse.OffsetAccountGroupID = pmTran.OffsetAccountGroupID;
					transactionToReverse.OffsetAccountID = pmTran.OffsetAccountID;
					transactionToReverse.OffsetSubID = pmTran.OffsetSubID;
					transactionToReverse.TranPeriodID = pmTran.TranPeriodID;
					transactionToReverse.FinPeriodID = pmTran.FinPeriodID;
					transactionToReverse.TranDate = pmTran.TranDate;
					transactionToReverse.Description = string.Format(Messages.TimeTransactionReverse, pmTran.Description);
					transactionToReverse.OrigRefID = null;
					transactionToReverse.Released = true;

					result.Add(new TranWithInfo(transactionToReverse, account, accountGroup, project, task));
				}
			}

			return result;
		}
	}
}
