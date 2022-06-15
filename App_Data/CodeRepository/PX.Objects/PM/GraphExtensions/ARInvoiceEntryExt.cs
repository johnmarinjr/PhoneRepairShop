using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.GL;
using PX.Objects.SO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PMBudgetLite = PX.Objects.PM.Lite.PMBudget;

namespace PX.Objects.PM
{
	public class ARInvoiceEntryExt : PXGraphExtension<ARInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
		}

		[InjectDependency]
		public IProjectMultiCurrency MultiCurrencyService { get; set; }

		#region Views/Selects/Delegates

		[PXCopyPasteHiddenView]
		public PXSelect<PMBillingRecord> ProjectBillingRecord;

		[PXCopyPasteHiddenView]
		public PXSelect<PMProforma> ProjectProforma;

		[PXCopyPasteHiddenView]
		public PXSelect<PMRegister> ProjectRegister;

		public PXSelect<PMBudgetAccum> Budget;

		#endregion

		#region Actions/Buttons

		public PXAction<ARInvoice> viewProforma;
		[PXUIField(DisplayName = Messages.ViewProforma, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable ViewProforma(PXAdapter adapter)
		{
			if (Base.Document.Current != null && Base.Document.Current.ProformaExists == true)
			{
				ProformaEntry target = PXGraph.CreateInstance<ProformaEntry>();
				target.Document.Current = PXSelect<PMProforma, Where<PMProforma.aRInvoiceDocType, Equal<Current<ARInvoice.docType>>,
					And<PMProforma.aRInvoiceRefNbr, Equal<Current<ARInvoice.refNbr>>, Or<PMProforma.aRInvoiceRefNbr, Equal<Current<ARInvoice.origRefNbr>>>>>>.Select(Base);
				throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}

			return adapter.Get();
		}

		public PXAction<ARInvoice> viewPMTrans;
		[PXUIField(DisplayName = Messages.ViewPMTrans, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable ViewPMTrans(PXAdapter adapter)
		{
			if (Base.Document.Current != null)
			{
				var graph = PXGraph.CreateInstance<TransactionInquiry>();
				var filter = graph.Filter.Insert();
				filter.ARDocType = Base.Document.Current.DocType;
				filter.ARRefNbr = Base.Document.Current.RefNbr;

				throw new PXRedirectRequiredException(graph, true, "ViewPMTrans") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}

			return adapter.Get();
		}

		#endregion

		#region Event Handlers

		protected virtual void _(Events.RowSelected<ARInvoice> e)
		{
			this.viewProforma.SetEnabled(e.Row?.ProformaExists == true);
			SetViewPMTransEnabled(e.Row);
		}

		private void SetViewPMTransEnabled(ARInvoice doc)
		{
			var enabled = false;
			PMProject project;
			if (doc != null && ProjectDefaultAttribute.IsProject(Base, doc.ProjectID, out project))
			{
				enabled = project.BaseType == CT.CTPRType.Project;
			}
			this.viewPMTrans.SetEnabled(enabled);
		}

		private bool isARInvoiceDeleting = false;
		protected virtual void _(Events.RowDeleting<ARInvoice> e)
		{
			if (e.Row != null && e.Row.ProjectID != null && e.Row.ProjectID != PM.ProjectDefaultAttribute.NonProject())
			{
				var selectReleased = new PXSelectJoin<PMBillingRecord,
				InnerJoin<PMBillingRecordEx, On<PMBillingRecord.projectID, Equal<PMBillingRecordEx.projectID>,
				And<PMBillingRecord.billingTag, Equal<PMBillingRecordEx.billingTag>,
				And<PMBillingRecord.recordID, Less<PMBillingRecordEx.recordID>,
				And<PMBillingRecordEx.proformaRefNbr, IsNotNull>>>>>,
				Where<PMBillingRecord.projectID, Equal<Required<PMBillingRecord.projectID>>,
				And<PMBillingRecord.aRDocType, Equal<Required<PMBillingRecord.aRDocType>>,
				And<PMBillingRecord.aRRefNbr, Equal<Required<PMBillingRecord.aRRefNbr>>>>>>(Base);

				var resultset = selectReleased.Select(e.Row.ProjectID, e.Row.DocType, e.Row.RefNbr);
				if (resultset.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (PXResult<PMBillingRecord, PMBillingRecordEx> res in resultset)
					{
						PMBillingRecordEx item = (PMBillingRecordEx)res;
						sb.AppendFormat("{0}-{1},", item.ARDocType, item.ARRefNbr);
					}

					string list = sb.ToString().TrimEnd(',');

					throw new PXException(AR.Messages.ReleasedProforma, list);
				}
			}

			isARInvoiceDeleting = true;
		}
		protected virtual void _(Events.RowDeleted<ARInvoice> e)
		{
			var select = new PXSelectJoin<PMBillingRecord,
				LeftJoin<PMProforma, On<PMBillingRecord.proformaRefNbr, Equal<PMProforma.refNbr>, And<PMProforma.corrected, Equal<False>>>>,
				Where<PMBillingRecord.aRDocType, Equal<Required<PMBillingRecord.aRDocType>>,
					And<PMBillingRecord.aRRefNbr, Equal<Required<PMBillingRecord.aRRefNbr>>>>>(Base);

			var resultset = select.Select(e.Row.DocType, e.Row.RefNbr);
			if (resultset.Count > 0)
			{
				PMBillingRecord billingRecord = PXResult.Unwrap<PMBillingRecord>(resultset[0]);
				if (billingRecord != null)
				{
					if (billingRecord.ProformaRefNbr != null)
					{
						billingRecord.ARDocType = null;
						billingRecord.ARRefNbr = null;
						ProjectBillingRecord.Update(billingRecord);

						PMProforma proforma = PXResult.Unwrap<PMProforma>(resultset[0]);
						if (proforma != null && !string.IsNullOrEmpty(proforma.RefNbr))
						{
							proforma.ARInvoiceDocType = null;
							proforma.ARInvoiceRefNbr = null;
							proforma.Released = false;
							proforma.Status = ProformaStatus.Open;
							ProjectProforma.Update(proforma);
						}
					}
					else
					{
						ProjectBillingRecord.Delete(billingRecord);
					}
				}

				PMRegister allocationReversal = PXSelect<PMRegister,
						Where<PMRegister.origDocType, Equal<PMOrigDocType.allocationReversal>,
							And<PMRegister.origNoteID, Equal<Required<ARInvoice.noteID>>,
							And<PMRegister.released, Equal<False>>>>>.Select(Base, e.Row.NoteID);
				if (allocationReversal != null)
					ProjectRegister.Delete(allocationReversal);
			}

			AddToUnbilledSummary(e.Row);
		}

		protected virtual void _(Events.RowInserted<ARTran> e)
		{
			if (Base.Document.Current.IsRetainageDocument != true && e.Row.TaskID != null && Base.Document.Current.ProformaExists != true)
			{
				AddToInvoiced(e.Row, GetProjectedAccountGroup(e.Row), (int)ARDocType.SignAmount(e.Row.TranType).GetValueOrDefault(1));
				AddToDraftRetained(e.Row, GetProjectedAccountGroup(e.Row), (int)ARDocType.SignAmount(e.Row.TranType).GetValueOrDefault(1));
				RemoveObsoleteLines();
			}
		}

		protected virtual void _(Events.RowUpdated<ARTran> e)
		{
			if (e.Row != null)
			{
				SyncBudgets(e.Row, e.OldRow);
			}
		}

		protected virtual void _(Events.RowUpdated<CurrencyInfo> e)
		{
			if (e.Row == null) return;

			foreach (ARTran tran in Base.Transactions.Select())
			{
				decimal newTranAmt = 0;
				if (e.Row.CuryRate != null)
					newTranAmt = e.Row.CuryConvBase(tran.CuryTranAmt.GetValueOrDefault());
				var newTran = Base.Transactions.Cache.CreateCopy(tran) as ARTran;
				newTran.TranAmt = newTranAmt;

				decimal oldTranAmt = 0;
				if (e.OldRow.CuryRate != null)
					oldTranAmt = e.OldRow.CuryConvBase(tran.CuryTranAmt.GetValueOrDefault());
				var oldTran = Base.Transactions.Cache.CreateCopy(tran) as ARTran;
				oldTran.TranAmt = oldTranAmt;

				SyncBudgets(newTran, oldTran);
			}
		}

		protected virtual void _(Events.RowDeleted<ARTran> e)
		{
			if (Base.Document.Current.IsRetainageDocument != true && e.Row.TaskID != null && Base.Document.Current.ProformaExists != true)
			{
				AddToInvoiced(e.Row, GetProjectedAccountGroup(e.Row), -1 * (int)ARDocType.SignAmount(e.Row.TranType).GetValueOrDefault(1));
				AddToDraftRetained(e.Row, GetProjectedAccountGroup(e.Row), -1 * (int)ARDocType.SignAmount(e.Row.TranType).GetValueOrDefault(1));

				var select = new PXSelect<PMTran, Where<PMTran.aRTranType, Equal<Required<PMTran.aRTranType>>,
					And<PMTran.aRRefNbr, Equal<Required<PMTran.aRRefNbr>>,
					And<PMTran.refLineNbr, Equal<Required<PMTran.refLineNbr>>>>>>(Base);

				string tranType = e.Row.TranType;
				string refNbr = e.Row.RefNbr;
				int? lineNbr = e.Row.LineNbr;

				if (tranType == ARDocType.CreditMemo && !string.IsNullOrEmpty(Base.Document.Current.OrigRefNbr))
				{
					tranType = Base.Document.Current.OrigDocType;
					refNbr = Base.Document.Current.OrigRefNbr;
					lineNbr = e.Row.OrigLineNbr;
				}

				PMTran original = select.SelectWindowed(0, 1, tranType, refNbr, lineNbr);

				if (original == null)//progressive line
					SubtractValuesToInvoice(((ARTran)e.Row), GetProjectedAccountGroup((ARTran)e.Row), -1 * (int)ARDocType.SignAmount(((ARTran)e.Row).TranType).GetValueOrDefault(1)); //Restoring AmountToInvoice

				RemoveObsoleteLines();
			}

			if (e.Row != null)
			{
				if (!isARInvoiceDeleting)
				{
					foreach (PM.PMTran pMRef in Base.RefContractUsageTran.Select(((ARTran)e.Row).TranType, ((ARTran)e.Row).RefNbr, ((ARTran)e.Row).LineNbr))
					{
						if (pMRef != null)
						{
							pMRef.ARRefNbr = null;
							pMRef.ARTranType = null;
							pMRef.RefLineNbr = null;
							if (Base.Document.Current != null && Base.Document.Current.ProformaExists != true)
							{
								pMRef.Billed = false;
								pMRef.BilledDate = null;
								pMRef.InvoicedQty = 0;
								pMRef.InvoicedAmount = 0;
								PM.RegisterReleaseProcess.AddToUnbilledSummary(Base, pMRef);
							}

							Base.RefContractUsageTran.Update(pMRef);
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<ARTran, ARTran.costCodeID> e)
		{
			PMProject project;
			if (CostCodeAttribute.UseCostCode() && ProjectDefaultAttribute.IsProject(Base, e.Row.ProjectID, out project))
			{
				if (project.BudgetLevel == BudgetLevels.Task)
				{
					e.NewValue = CostCodeAttribute.GetDefaultCostCode();
				}
			}
		}

		protected virtual void _(Events.RowPersisted<ARInvoice> e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
			{
				List<PXDataFieldParam> commands = new List<PXDataFieldParam>();

				commands.Add(new PXDataFieldRestrict<PMTran.aRTranType>(PXDbType.Char, 3, e.Row.DocType, PXComp.EQ));
				commands.Add(new PXDataFieldRestrict<PMTran.aRRefNbr>(PXDbType.NChar, 15, e.Row.RefNbr, PXComp.EQ));
				commands.Add(new PXDataFieldAssign(typeof(PMTran.aRTranType).Name, PXDbType.Char, null));
				commands.Add(new PXDataFieldAssign(typeof(PMTran.aRRefNbr).Name, PXDbType.Char, null));
				commands.Add(new PXDataFieldAssign(typeof(PMTran.refLineNbr).Name, PXDbType.Int, null));
				if (e.Row.ProformaExists != true)
				{
					commands.Add(new PXDataFieldAssign(typeof(PMTran.billed).Name, PXDbType.Bit, false));
					commands.Add(new PXDataFieldAssign(typeof(PMTran.billedDate).Name, PXDbType.DateTime, null));
					commands.Add(new PXDataFieldAssign(typeof(PMTran.invoicedQty).Name, PXDbType.Decimal, 0m));
					commands.Add(new PXDataFieldAssign(typeof(PMTran.invoicedAmount).Name, PXDbType.Decimal, 0m));
				}
				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Reseting the links on document delete - for a large document there can be thousands of records causing timeout on delete if done through PXCache.delete]
				PXDatabase.Update<PMTran>(commands.ToArray());
			}
		}

		#endregion

		private void SyncBudgets(ARTran row, ARTran oldRow)
		{
			if (Base.Document.Current.IsRetainageDocument != true &&
				Base.Document.Current.ProformaExists != true &&
				(row.TaskID != oldRow.TaskID ||
				row.TranAmt != oldRow.TranAmt ||
				row.AccountID != oldRow.AccountID ||
				row.CostCodeID != oldRow.CostCodeID ||
				row.UOM != oldRow.UOM ||
				row.Qty != oldRow.Qty))
			{
				if (oldRow.TaskID != null)
				{
					AddToInvoiced(oldRow, GetProjectedAccountGroup(oldRow), -1 * (int)ARDocType.SignAmount(oldRow.TranType).GetValueOrDefault(1));
					AddToDraftRetained(oldRow, GetProjectedAccountGroup(oldRow), -1 * (int)ARDocType.SignAmount(oldRow.TranType).GetValueOrDefault(1));
				}
				if (row.TaskID != null)
				{
					AddToInvoiced(row, GetProjectedAccountGroup(row), (int)ARDocType.SignAmount(oldRow.TranType).GetValueOrDefault(1));
					AddToDraftRetained(row, GetProjectedAccountGroup(row), (int)ARDocType.SignAmount(oldRow.TranType).GetValueOrDefault(1));
				}
				RemoveObsoleteLines();
			}
		}

		public virtual int? GetProjectedAccountGroup(ARTran line)
		{
			int? projectedRevenueAccountGroupID = null;

			if (line.AccountID != null)
			{
				Account revenueAccount = PXSelectorAttribute.Select<ARTran.accountID>(Base.Transactions.Cache, line, line.AccountID) as Account;
				if (revenueAccount != null)
				{
					if (revenueAccount.AccountGroupID == null)
						throw new PXException(PM.Messages.RevenueAccountIsNotMappedToAccountGroup, revenueAccount.AccountCD);

					projectedRevenueAccountGroupID = revenueAccount.AccountGroupID;
				}
			}

			return projectedRevenueAccountGroupID;
		}

		public virtual void AddToInvoiced(ARTran line, int? revenueAccountGroup, int mult)
		{
			if (base.Base.Document.Current.Scheduled != true)
			{
				PMBudgetAccum budget = GetTargetBudget(revenueAccountGroup, line);

				if (budget != null)
				{
					decimal curyAmount = mult * GetValueInProjectCurrency(budget.ProjectID, line.CuryTranAmt + line.CuryRetainageAmt);
					budget.CuryInvoicedAmount += curyAmount;
					budget.InvoicedAmount += GetBaseValueForBudget(PMProject.PK.Find(Base, line.ProjectID), curyAmount);
					IN.INUnitAttribute.TryConvertGlobalUnits(Base, line.UOM, budget.UOM, line.Qty.GetValueOrDefault(), IN.INPrecision.QUANTITY, out decimal qty);
					budget.InvoicedQty += mult * qty;
				}
			}
		}

		public virtual void SubtractValuesToInvoice(ARTran line, int? revenueAccountGroup, int mult)
		{
			PMBudgetAccum budget = GetTargetBudget(revenueAccountGroup, line);

			if (budget != null)
			{
				decimal curyAmount = mult * GetValueInProjectCurrency(budget.ProjectID, line.CuryTranAmt + line.CuryRetainageAmt);
				budget.CuryAmountToInvoice -= curyAmount;
				budget.AmountToInvoice -= GetBaseValueForBudget(PMProject.PK.Find(Base, line.ProjectID), curyAmount);
				if (budget.ProgressBillingBase == ProgressBillingBase.Quantity)
				{
					IN.INUnitAttribute.TryConvertGlobalUnits(Base, line.UOM, budget.UOM, line.Qty.GetValueOrDefault(), IN.INPrecision.QUANTITY, out decimal qty);
					budget.QtyToInvoice -= mult * qty;
				}
			}
		}

		protected virtual void AddToUnbilledSummary(ARInvoice row)
		{
			var selectPMTrans = new PXSelect<PM.PMTran,
				Where<PM.PMTran.aRTranType, Equal<Required<ARTran.tranType>>,
				And<PM.PMTran.aRRefNbr, Equal<Required<ARTran.refNbr>>>>>(Base);

			if (row.ProformaExists != true)
			{
				foreach (PM.PMTran pMRef in selectPMTrans.Select(row.DocType, row.RefNbr))
				{
					pMRef.Billed = false;
					PM.RegisterReleaseProcess.AddToUnbilledSummary(Base, pMRef);
				}
			}
		}

		public virtual void AddToDraftRetained(ARTran line, int? revenueAccountGroup, int mult)
		{
			PMBudgetAccum budget = GetTargetBudget(revenueAccountGroup, line);

			if (budget != null)
			{
				decimal curyAmount = mult * GetValueInProjectCurrency(budget.ProjectID, line.CuryRetainageAmt);
				budget.CuryDraftRetainedAmount += curyAmount;
				budget.DraftRetainedAmount += GetBaseValueForBudget(PMProject.PK.Find(Base, line.ProjectID), curyAmount);
			}
		}

		private PMBudgetAccum GetTargetBudget(int? accountGroupID, ARTran line)
		{
			if (line.ProjectID == null)
				return null;

			if (line.TaskID == null)
				return null;

			if (accountGroupID == null)
				return null;

			PMProject project = PMProject.PK.Find(Base, line.ProjectID);

			if (project == null)
				return null;

			if (project.NonProject == true)
				return null;

			PMAccountGroup ag = PMAccountGroup.PK.Find(Base, accountGroupID);

			BudgetService budgetService = new BudgetService(Base);
			PMBudgetLite budget = budgetService.SelectProjectBalance(ag, project, line.TaskID, line.InventoryID, line.CostCodeID, out bool isExisting);

			return Budget.Insert(new PMBudgetAccum
			{
				Type = budget.Type,
				ProjectID = budget.ProjectID,
				ProjectTaskID = budget.TaskID,
				AccountGroupID = budget.AccountGroupID,
				InventoryID = budget.InventoryID,
				CostCodeID = budget.CostCodeID,
				UOM = budget.UOM,
				Description = budget.Description,
				CuryInfoID = project.CuryInfoID,
				ProgressBillingBase = budget.ProgressBillingBase
			});
		}

		public override void Configure(PXScreenConfiguration config)
		{
			WorkflowContext<ARInvoiceEntry, ARInvoice> context = config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>();

			var relatedDocumentsCategory = context.Categories.Get(ARInvoiceEntry_Workflow.CategoryID.RelatedDocuments);

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add<ARInvoiceEntryExt>(g => g.viewProforma,
							c => c.InFolder(relatedDocumentsCategory).IsHiddenWhen(context.Conditions.Get("IsFinCharge"))
						);
						actions.Add<ARInvoiceEntryExt>(g => g.viewPMTrans,
							c => c.InFolder(FolderType.InquiriesFolder)
						);
					});
			});
		}

		protected virtual void RemoveObsoleteLines()
		{
			foreach (PMBudgetAccum item in Budget.Cache.Inserted)
			{
				if (item.CuryInvoicedAmount.GetValueOrDefault() == 0 && item.InvoicedAmount.GetValueOrDefault() == 0
					&& item.CuryAmountToInvoice.GetValueOrDefault() == 0 && item.AmountToInvoice.GetValueOrDefault() == 0
					&& item.InvoicedQty.GetValueOrDefault() == 0.0m)
				{
					Budget.Cache.Remove(item);
				}
			}
		}

		private decimal GetValueInProjectCurrency(int? projectID, decimal? value)
		{
			PMProject project = PMProject.PK.Find(Base, projectID);

			return MultiCurrencyService.GetValueInProjectCurrency(Base, project, Base.Document.Current.CuryID, Base.Document.Current.DocDate, value);
		}

		private decimal GetBaseValueForBudget(PMProject project, decimal curyValue)
		{
			if (project.CuryID == project.BaseCuryID) return curyValue;
			else
			{
				CurrencyInfo currencyInfo = Base.FindImplementation<IPXCurrencyHelper>().GetCurrencyInfo(project.CuryInfoID);
				return currencyInfo.CuryConvBase(curyValue);
			}
		}
	}
}
