﻿using CommonServiceLocator;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.PM.GraphExtensions
{
	public class JournalEntryProjectExt : PXGraphExtension<JournalEntry>
	{
		public PXSelect<PMRegister> ProjectDocs;
		public PXSelect<PMTran> ProjectTrans;
		public PXSelect<PMTaskTotal> ProjectTaskTotals;
		public PXSelect<PMBudgetAccum> ProjectBudget;
		public PXSelect<PMForecastHistoryAccum> ForecastHistory;

		[InjectDependency]
		public IProjectMultiCurrency MultiCurrencyService { get; set; }

		#region Cache Attached Events

		#region PMTran
		#region TranID

		[PXDBLongIdentity(IsKey = true)]
		protected virtual void _(Events.CacheAttached<PMTran.tranID> e)
		{
		}
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<PMTran.refNbr> e)
		{
		}
		#endregion
		#region BatchNbr
		[PXDBDefault(typeof(Batch.batchNbr), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "BatchNbr")]
		protected virtual void _(Events.CacheAttached<PMTran.batchNbr> e)
		{
		}
		#endregion
		#region Date
		[PXDBDate()]
		[PXDefault(typeof(PMRegister.date))]
		public virtual void _(Events.CacheAttached<PMTran.date> e)
		{
		}
		#endregion
		#region FinPeriodID
		[FinPeriodID(typeof(PMRegister.date), typeof(PMTran.branchID))]
		[PXUIField(DisplayName = "Fin. Period", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual void _(Events.CacheAttached<PMTran.finPeriodID> e)
		{
		}
		#endregion
		#region BaseCuryInfoID
		public abstract class baseCuryInfoID : IBqlField { }
		[PXDBLong]
		[CurrencyInfoDBDefault(typeof(CurrencyInfo.curyInfoID))]
		public virtual void _(Events.CacheAttached<PMTran.baseCuryInfoID> e)
		{
		}

		#endregion
		#region ProjectCuryInfoID
		public abstract class projectCuryInfoID : IBqlField { }
		[PXDBLong]
		[CurrencyInfoDBDefault(typeof(CurrencyInfo.curyInfoID))]
		public virtual void _(Events.CacheAttached<PMTran.projectCuryInfoID> e)
		{
		}
		#endregion

		#endregion
		
		#endregion


		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectModule>();
		}
		public override void Initialize()
		{
			Base.OnBeforePersist += OnBeforeGraphPersist;
			Base.OnAfterPersist += OnAfterGraphPersist;
		}

		private List<PMTask> autoAllocateTasks;
		private void OnBeforeGraphPersist(PXGraph obj)
		{
			autoAllocateTasks = CreateProjectTrans();
		}

		private void OnAfterGraphPersist(PXGraph obj)
		{
			if (autoAllocateTasks?.Count > 0)
			{
				try
				{
					AutoAllocateTasks(autoAllocateTasks);
				}
				catch (Exception ex)
				{
					throw new PXException(ex, PM.Messages.AutoAllocationFailed);
				}
			}
		}

		#region PMRegister Events

		protected virtual void PMRegister_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			//CreateProjectTrans() can create more then one PMRegister because of autoallocation
			//hense set the RefNbr Manualy for all child transactios.

			PMRegister row = (PMRegister)e.Row;

			if (e.Operation == PXDBOperation.Insert)
			{
				if (e.TranStatus == PXTranStatus.Open)
				{
					foreach (PMTran tran in ProjectTrans.Cache.Inserted)
					{
						if (tran.TranType == row.Module)
						{
							tran.RefNbr = row.RefNbr;
						}

					}
				}
			}
		}

		#endregion

		#region Actions/Buttons

		public PXAction<Batch> ViewPMTran;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewPMTran(PXAdapter adapter)
		{
			GLTran tran = Base.GLTranModuleBatNbr.Current;

			if (tran?.PMTranID != null)
			{
				var graph = PXGraph.CreateInstance<TransactionInquiry>();
				var filter = graph.Filter.Insert();
				filter.TranID = tran.PMTranID;

				throw new PXRedirectRequiredException(graph, true, "ViewPMTran") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}

			return adapter.Get();
		}

		#endregion

		protected virtual void AutoAllocateTasks(List<PMTask> tasks)
		{
			PMSetup setup = PXSelect<PMSetup>.Select(Base);
			bool autoreleaseAllocation = setup.AutoReleaseAllocation == true;

			PMAllocator allocator = PXGraph.CreateInstance<PMAllocator>();
			allocator.Clear();
			allocator.TimeStamp = Base.TimeStamp;
			allocator.Execute(tasks);
			allocator.Actions.PressSave();

			if (allocator.Document.Current != null && autoreleaseAllocation)
			{
				List<PMRegister> list = new List<PMRegister>();
				list.Add(allocator.Document.Current);
				List<ProcessInfo<Batch>> batchList;
				bool releaseSuccess = RegisterRelease.ReleaseWithoutPost(list, false, out batchList);
				if (!releaseSuccess)
				{
					throw new PXException(PM.Messages.AutoReleaseFailed);
				}
				foreach (var item in batchList)
				{
					Base.created.AddRange(item.Batches);
				}
			}
		}

		public virtual List<PMTask> CreateProjectTrans()
		{
			var autoAllocateTasks = new List<PMTask>();

			if (Base.BatchModule.Current != null && Base.BatchModule.Current.Module != BatchModule.GL)
			{
				PXResultset<GLTran> trans = new PXSelect<GLTran,
					Where<GLTran.module, Equal<Current<Batch.module>>,
						And<GLTran.batchNbr, Equal<Current<Batch.batchNbr>>,
						And<GLTran.pMTranID, IsNull,
						And<GLTran.isNonPM, NotEqual<True>>>>>>(Base).Select();

				if (trans.Count > 0)
				{
					ProjectBalance pb = CreateProjectBalance();
					var tasksToAutoAllocate = new Dictionary<string, PMTask>();
					var sourceForAllocation = new List<PMTran>();

					GLTran glTran = null;

					var doc = new PMRegister();
					doc.Module = Base.BatchModule.Current.Module;
					doc.Date = Base.BatchModule.Current.DateEntered;
					doc.Description = Base.BatchModule.Current.Description;
					doc.Released = true;
					doc.Status = PMRegister.status.Released;
					bool docInserted = false; //to prevent creating empty batch
					JournalEntryTranRef entryRefGraph = PXGraph.CreateInstance<JournalEntryTranRef>();

					foreach (GLTran tran in trans)
					{
						if (glTran == null)
							glTran = tran;

						var acc = (Account)PXSelect<Account,
							Where<Account.accountID, Equal<Required<GLTran.accountID>>,
								And<Account.accountGroupID, IsNotNull>>>.Select(Base, tran.AccountID);
						if (acc == null) continue;

						var ag = (PMAccountGroup)PXSelect<PMAccountGroup,
							Where<PMAccountGroup.groupID, Equal<Required<Account.accountGroupID>>,
								And<PMAccountGroup.type, NotEqual<PMAccountType.offBalance>>>>.Select(Base, acc.AccountGroupID);
						if (ag == null) continue;

						var project = (PMProject)PXSelect<PMProject,
							Where<PMProject.contractID, Equal<Required<GLTran.projectID>>,
								And<PMProject.nonProject, Equal<False>>>>.Select(Base, tran.ProjectID);
						if (project == null) continue;

						var task = (PMTask)PXSelect<PMTask,
							Where<PMTask.projectID, Equal<Required<GLTran.projectID>>,
								And<PMTask.taskID, Equal<Required<GLTran.taskID>>>>>.Select(Base, tran.ProjectID, tran.TaskID);
						if (task == null) continue;

						APTran apTran = null;
						if (Base.BatchModule.Current.Module == BatchModule.AP)
						{
							apTran = (APTran)PXSelect<APTran,
								Where<APTran.refNbr, Equal<Required<GLTran.refNbr>>,
									And<APTran.lineNbr, Equal<Required<GLTran.tranLineNbr>>,
									And<APTran.tranType, Equal<Required<GLTran.tranType>>>>>>.Select(Base, tran.RefNbr, tran.TranLineNbr, tran.TranType);
						}

						ARTran arTran = null;
						if (Base.BatchModule.Current.Module == BatchModule.AR)
						{
							arTran = (ARTran)PXSelect<ARTran,
								Where<ARTran.refNbr, Equal<Required<GLTran.refNbr>>,
									And<ARTran.lineNbr, Equal<Required<GLTran.tranLineNbr>>,
									And<ARTran.tranType, Equal<Required<GLTran.tranType>>>>>>.Select(Base, tran.RefNbr, tran.TranLineNbr, tran.TranType);
						}

						if (!docInserted)
						{
							doc = ProjectDocs.Insert(doc);
							docInserted = true;
						}

						doc.OrigDocType = entryRefGraph.GetDocType(apTran, arTran, tran);
						doc.OrigNoteID = entryRefGraph.GetNoteID(apTran, arTran, tran);

						PMTran pmt = InsertProjectTransaction(project, task, ag, acc, tran, apTran, arTran);
						entryRefGraph.AssignCustomerVendorEmployee(tran, pmt);
						Base.GLTranModuleBatNbr.SetValueExt<GLTran.pMTranID>(tran, pmt.TranID);

						ProcessProjectTransaction(pb, tasksToAutoAllocate, pmt, acc, ag, project, task, arTran);

						sourceForAllocation.Add(pmt);

						entryRefGraph.AssignAdditionalFields(tran, pmt);
					}

					foreach (TranWithInfo additionalTran in entryRefGraph.GetAdditionalProjectTrans(glTran.Module, glTran.TranType, glTran.RefNbr))
					{
						if (!docInserted)
						{
							doc = ProjectDocs.Insert(doc);
							doc.OrigDocType = entryRefGraph.GetDocType(null, null, glTran);
							doc.OrigNoteID = entryRefGraph.GetNoteID(null, null, glTran);
							docInserted = true;
						}

						ProjectTrans.Cache.Insert(additionalTran.Tran);
						ProcessProjectTransaction(pb, tasksToAutoAllocate, additionalTran.Tran, additionalTran.Account, additionalTran.AccountGroup, additionalTran.Project, additionalTran.Task, null);
					}
					autoAllocateTasks.AddRange(tasksToAutoAllocate.Values);
				}
			}
			return autoAllocateTasks;
		}

		public virtual void ProcessProjectTransaction(ProjectBalance pb, Dictionary<string, PMTask> tasksToAutoAllocate, PMTran pmt, Account acc, PMAccountGroup ag, PMProject project, PMTask task, ARTran arTran)
		{
			int sign = 1;
			if (acc?.Type == AccountType.Income || acc?.Type == AccountType.Liability)
			{
				sign = -1;
			}

			ProjectBalance.Result balance = pb.Calculate(project, pmt, ag, acc?.Type, sign, 1);

			if (balance.Status != null)
			{
				PMBudgetAccum ps = ExtractBudget(balance.Status, pmt);
				ps = ProjectBudget.Insert(ps);
				ps.ActualQty += balance.Status.ActualQty.GetValueOrDefault();
				ps.CuryActualAmount += balance.Status.CuryActualAmount.GetValueOrDefault();
				ps.ActualAmount += balance.Status.ActualAmount.GetValueOrDefault();

				if (arTran != null && arTran.LineNbr != null && ag.Type == GL.AccountType.Income)
				{
					ps.CuryInvoicedAmount -= balance.Status.CuryActualAmount.GetValueOrDefault();
					ps.InvoicedAmount -= balance.Status.ActualAmount.GetValueOrDefault();
					ps.InvoicedQty -= balance.Status.ActualQty.GetValueOrDefault();
				}
			}

			if (balance.ForecastHistory != null)
			{
				PMForecastHistoryAccum forecast = new PMForecastHistoryAccum();
				forecast.ProjectID = balance.ForecastHistory.ProjectID;
				forecast.ProjectTaskID = balance.ForecastHistory.ProjectTaskID;
				forecast.AccountGroupID = balance.ForecastHistory.AccountGroupID;
				forecast.InventoryID = balance.ForecastHistory.InventoryID;
				forecast.CostCodeID = balance.ForecastHistory.CostCodeID;
				forecast.PeriodID = balance.ForecastHistory.PeriodID;

				forecast = ForecastHistory.Insert(forecast);

				forecast.ActualQty += balance.ForecastHistory.ActualQty.GetValueOrDefault();
				forecast.CuryActualAmount += balance.ForecastHistory.CuryActualAmount.GetValueOrDefault();
				forecast.ActualAmount += balance.ForecastHistory.ActualAmount.GetValueOrDefault();
			}

			if (balance.TaskTotal != null)
			{
				PMTaskTotal ta = new PMTaskTotal();
				ta.ProjectID = balance.TaskTotal.ProjectID;
				ta.TaskID = balance.TaskTotal.TaskID;

				ta = ProjectTaskTotals.Insert(ta);
				ta.CuryAsset += balance.TaskTotal.CuryAsset.GetValueOrDefault();
				ta.Asset += balance.TaskTotal.Asset.GetValueOrDefault();
				ta.CuryLiability += balance.TaskTotal.CuryLiability.GetValueOrDefault();
				ta.Liability += balance.TaskTotal.Liability.GetValueOrDefault();
				ta.CuryIncome += balance.TaskTotal.CuryIncome.GetValueOrDefault();
				ta.Income += balance.TaskTotal.Income.GetValueOrDefault();
				ta.CuryExpense += balance.TaskTotal.CuryExpense.GetValueOrDefault();
				ta.Expense += balance.TaskTotal.Expense.GetValueOrDefault();
			}

			RegisterReleaseProcess.AddToUnbilledSummary(Base, pmt);

			if (pmt.Allocated != true && pmt.ExcludedFromAllocation != true && project.AutoAllocate == true)
			{
				if (!tasksToAutoAllocate.ContainsKey(string.Format("{0}.{1}", task.ProjectID, task.TaskID)))
				{
					tasksToAutoAllocate.Add(string.Format("{0}.{1}", task.ProjectID, task.TaskID), task);
				}
			}
		}

		public virtual PMTran InsertProjectTransaction(PMProject project, PMTask task, PMAccountGroup ag,
			Account acc, GLTran tran, APTran apTran, ARTran arTran)
		{
			PMTran pmt = (PMTran)ProjectTrans.Cache.Insert();

			pmt.BranchID = tran.BranchID;
			pmt.AccountGroupID = acc.AccountGroupID;
			pmt.AccountID = tran.AccountID;
			pmt.SubID = tran.SubID;

			pmt.Date = tran.TranDate;
			pmt.TranDate = tran.TranDate;
			pmt.Description = tran.TranDesc;
			pmt.FinPeriodID = tran.FinPeriodID;
			pmt.TranPeriodID = tran.TranPeriodID;
			pmt.InventoryID = tran.InventoryID ?? PMInventorySelectorAttribute.EmptyInventoryID;
			pmt.OrigLineNbr = tran.LineNbr;
			pmt.OrigModule = tran.Module;
			pmt.OrigRefNbr = tran.RefNbr;
			pmt.OrigTranType = tran.TranType;
			pmt.ProjectID = tran.ProjectID;
			pmt.TaskID = tran.TaskID;
			pmt.CostCodeID = tran.CostCodeID;
			if (arTran != null)
			{
				pmt.Billable = false;
				pmt.ExcludedFromBilling = true;
				pmt.ExcludedFromBillingReason = arTran.TranType == ARDocType.CreditMemo ?
					PXMessages.LocalizeFormatNoPrefix(Messages.ExcludedFromBillingAsCreditMemoResult, arTran.RefNbr) :
					PXMessages.LocalizeFormatNoPrefix(Messages.ExcludedFromBillingAsARInvoiceResult, arTran.RefNbr);
			}
			else
			{
				pmt.Billable = tran.NonBillable != true;
			}
			pmt.Released = true;

			if (apTran != null && apTran.Date != null)
			{
				pmt.Date = apTran.Date;
			}

			pmt.UseBillableQty = true;
			pmt.UOM = tran.UOM;

			MultiCurrencyService.CalculateCurrencyValues(Base, tran, pmt, Base.BatchModule.Current, project, Base.ledger.Current);

			pmt.Qty = tran.Qty;
			
			if (ProjectBalance.IsFlipRequired(acc.Type, ag.Type))
			{
				pmt.ProjectCuryAmount = -pmt.ProjectCuryAmount;
				pmt.TranCuryAmount = -pmt.TranCuryAmount;
				pmt.Amount = -pmt.Amount;
				pmt.Qty = -pmt.Qty;
			}
			pmt.BillableQty = pmt.Qty;

			if (apTran != null && apTran.NoteID != null)
			{
				PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(AP.APTran)], apTran, ProjectTrans.Cache, pmt);
			}
			else if (arTran != null && arTran.NoteID != null)
			{
				PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(AR.ARTran)], arTran, ProjectTrans.Cache, pmt);
			}

			return pmt;
		}

		public virtual PMBudgetAccum ExtractBudget(PMBudget targetBudget, PMTran tran)
		{
			PMBudgetAccum ps = new PMBudgetAccum();
			ps.ProjectID = targetBudget.ProjectID;
			ps.ProjectTaskID = targetBudget.ProjectTaskID;
			ps.AccountGroupID = targetBudget.AccountGroupID;
			ps.InventoryID = targetBudget.InventoryID;
			ps.CostCodeID = targetBudget.CostCodeID;
			ps.UOM = targetBudget.UOM;
			ps.IsProduction = targetBudget.IsProduction;
			ps.Type = targetBudget.Type;
			ps.Description = targetBudget.Description;
			ps.CuryInfoID = targetBudget.CuryInfoID;

			return ps;
		}

		public virtual ProjectBalance CreateProjectBalance()
		{
			return new ProjectBalance(Base);
		}
	}
}
