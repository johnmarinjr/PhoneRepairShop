using PX.Api;
using PX.CarrierService;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CN.ProjectAccounting.Descriptor;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.PO.GraphExtensions.APReleaseProcessExt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using UpdatePOOnRelease = PX.Objects.PO.GraphExtensions.APReleaseProcessExt.UpdatePOOnRelease;

namespace PX.Objects.CN.ProjectAccounting.AP.GraphExtensions
{
	using static BoundedTo<APInvoiceEntry, APInvoice>;
	public class APInvoiceEntryReclassifyingExt : PXGraphExtension<APInvoiceEntry>
	{
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = true)]
		protected virtual void _(Events.CacheAttached<POLine.lineNbr> e)
		{
		}

		[PXDBInt]
		[PXUIField(DisplayName = "PO Line", Visible = false)]
		[PXSelector(typeof(Search<POLine.lineNbr, Where<POLine.orderType, Equal<Current<APTran.pOOrderType>>,
			And<POLine.orderNbr, Equal<Current<APTran.pONbr>>>>>),
			typeof(POLine.lineNbr), typeof(POLine.projectID), typeof(POLine.taskID), typeof(POLine.costCodeID),
			typeof(POLine.inventoryID), typeof(POLine.lineType), typeof(POLine.tranDesc), typeof(POLine.uOM),
			typeof(POLine.orderQty), typeof(POLine.curyUnitCost), typeof(POLine.curyExtCost))]
		protected virtual void _(Events.CacheAttached<APTran.pOLineNbr> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[APOpenPeriod(
			typeof(APRegister.docDate),
			branchSourceType: typeof(APRegister.branchID),
			masterFinPeriodIDType: typeof(APRegister.tranPeriodID),
			IsHeader = true, ValidatePeriod = PeriodValidation.DefaultUpdate)]
		protected virtual void _(Events.CacheAttached<APInvoice.finPeriodID> e)
		{
		}

		[PXOverride]
		public virtual IEnumerable Release(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseHandler)
		{
			if (Base.Document.Current.Status == APDocStatus.UnderReclassification)
			{
				Base.Save.Press();
				var list = adapter.Get<APInvoice>().ToList();
				PXLongOperation.StartOperation(Base, delegate () { ReleaseDoc(list); });
				return list;
			}
			else
			{
				return baseHandler(adapter);
			}
		}				

		public PXAction<APInvoice> reclassify;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reclassify Bill", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual void Reclassify()
		{
			if (Base.Document.Current.IsMigratedRecord == true)
				throw new PXException(ProjectAccountingMessages.CannotReclassifiedMigratedDoc);

			if (Base.Document.Current.OrigDocType == Objects.EP.EPExpenseClaim.DocType)
				throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithExpenseClaim);

			int billedCount = SelectFrom<PMTran>
				.Where<PMTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>
					.And<PMTran.origModule.IsEqual<BatchModule.moduleAP>>
					.And<PMTran.billed.IsEqual<True>>>
				.View.Select(Base).Count();
			if (billedCount > 0) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithBilledTransactions);

			int allocatedCount = SelectFrom<PMTran>
				.Where<PMTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>
				.And<PMTran.origModule.IsEqual<BatchModule.moduleAP>>
				.And<PMTran.allocated.IsEqual<True>>>
				.View.Select(Base).Count();
			if (allocatedCount > 0) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithAllocatedTransactions);

			int consolidatedCount = SelectFrom<GLTran>
				.Where<GLTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>
					.And<GLTran.module.IsEqual<BatchModule.moduleAP>>
					.And<GLTran.refNbr.IsNotNull>>
				.AggregateTo<GroupBy<GLTran.refNbr>>.View.Select(Base).Count();
			if (consolidatedCount > 1) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithConsolidatedBatch, Base.Document.Current.BatchNbr);

			int retainagedCount = SelectFrom<APInvoice>
				.Where<APInvoice.origDocType.IsEqual<APInvoice.docType.FromCurrent>
					.And<APInvoice.origRefNbr.IsEqual<APInvoice.refNbr.FromCurrent>>
					.And<APInvoice.isRetainageDocument.IsEqual<True>>>
				.View.Select(Base).Count();
			if (retainagedCount > 0) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithWithReleasedRetainage);

			int reclassCount = SelectFrom<GLTran>
				.Where<GLTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>
					.And<GLTran.module.IsEqual<BatchModule.moduleAP>>
					.And<GLTran.reclassBatchNbr.IsNotNull>>
				.View.Select(Base).Count();
			if (reclassCount > 0) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithReclassifiedBatch);

			int detailedCount = SelectFrom<GLTran>
				.Where<GLTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>
					.And<GLTran.module.IsEqual<BatchModule.moduleAP>>
					.And<GLTran.summPost.IsEqual<False>>>
				.View.Select(Base).Count();
			if (detailedCount == 0) throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithSummaryPosted);

			if (Base.Document.Current.TermsID != null)
			{
				Terms terms = (Terms)PXSelectorAttribute.Select<APInvoice.termsID>(Base.Document.Cache, Base.Document.Current);
				if (terms.InstallmentType == TermsInstallmentType.Multiple)
					throw new PXException(ProjectAccountingMessages.CannotReclassifiedWithMultipleTerms);
			}

			foreach (APTran apTran in Base.Transactions.Select())
			{
				var extAPTran = Base.Transactions.Cache.GetExtension<APTranExt>(apTran);
				extAPTran.PrevPOLineNbr = apTran.POLineNbr;
				Base.Transactions.Update(apTran);
			}

			try
			{
				APOpenPeriodAttribute openPeriodAttribute =
					Base.Document.Cache.GetAttributesReadonly<APInvoice.finPeriodID>().
					OfType<APOpenPeriodAttribute>().FirstOrDefault();

				PeriodValidation periodValidation = openPeriodAttribute.ValidatePeriod;
				openPeriodAttribute.ValidatePeriod = PeriodValidation.DefaultSelectUpdate;
				openPeriodAttribute?.IsValidPeriod(Base.Document.Cache, Base.Document.Current, Base.Document.Current.FinPeriodID);
				openPeriodAttribute.ValidatePeriod = periodValidation;
			}
			catch (PXSetPropertyException ex)
			{
				Base.Caches<APInvoice>().RaiseExceptionHandling<APInvoice.finPeriodID>(Base.Document.Current, 0m, ex);
			}
		}

		public virtual void ReleaseDoc(List<APInvoice> list)
		{
			APInvoiceEntry graph = PXGraph.CreateInstance<APInvoiceEntry>();
			APReleaseProcess release = PXGraph.CreateInstance<APReleaseProcess>();
			PostGraph pg = PXGraph.CreateInstance<PostGraph>();
			JournalEntry je = PXGraph.CreateInstance<JournalEntry>();
			je.Mode |= JournalEntry.Modes.InvoiceReclassification;

			foreach (APInvoice apDoc in list)
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					graph.Document.Current = apDoc;

					Batch batch = SelectFrom<Batch>.Where<Batch.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>>.View.Select(graph);

					bool changed = MarkChangedLines(graph);

					if (changed)
					{
						ReverseOrigBatch(je, pg, release, batch);
						je.Clear();
						pg.Clear();

						CreateNewBatch(je, pg, graph, release, apDoc, batch);
						je.Clear();
						pg.Clear();
					}

					UpdatePOLines(graph, release, apDoc);

					release.Persist();
					release.Clear();

					APInvoice.Events
							.Select(e => e.ReleaseDocument)
							.FireOn(graph, apDoc);

					graph.Persist();
					graph.Clear();

					ts.Complete();
				}
			}
		}

		private bool MarkChangedLines(APInvoiceEntry graph)
		{
			bool changed = false;

			var glTrans = SelectFrom<GLTran>
						.LeftJoin<APTran>.On<APTran.lineNbr.IsEqual<GLTran.tranLineNbr>
							.And<APTran.tranType.IsEqual<APInvoice.docType.FromCurrent>>
							.And<APTran.refNbr.IsEqual<APInvoice.refNbr.FromCurrent>>>
						.Where<GLTran.batchNbr.IsEqual<APInvoice.batchNbr.FromCurrent>>
						.OrderBy<GLTran.lineNbr.Asc>
						.View.Select(graph);
			
			foreach (PXResult<GLTran> result in glTrans)
			{
				APTran apTran = PXResult.Unwrap<APTran>(result);
				GLTran glTran = PXResult.Unwrap<GLTran>(result);
				APTranExt extAPTran = graph.Transactions.Cache.GetExtension<APTranExt>(apTran);
				if (apTran.RefNbr == null) continue;

				bool costCodeChanged = (apTran.CostCodeID != glTran.CostCodeID) && !(
						apTran.ProjectID == ProjectDefaultAttribute.NonProject() &&
						apTran.CostCodeID == null && 
						glTran.ProjectID == ProjectDefaultAttribute.NonProject() &&
						glTran.CostCodeID == CostCodeAttribute.DefaultCostCode);

				if (apTran.ProjectID != glTran.ProjectID  ||
					apTran.TaskID != glTran.TaskID ||
					apTran.AccountID != glTran.AccountID ||
					apTran.SubID  != glTran.SubID ||
					extAPTran.PrevPOLineNbr != apTran.POLineNbr ||
					costCodeChanged)
				{
					extAPTran.Reclassified = true;
					apTran = graph.Transactions.Update(apTran);
					changed = true;
				}				
			}

			return changed;
		}

		private void ReverseOrigBatch(JournalEntry je,PostGraph pg, APReleaseProcess release, Batch batch)
		{
			je.ReverseBatchProc(batch);
			if (je.BatchModule.Current.Status == BatchStatus.Hold)
			{
				je.releaseFromHold.Press();
			}
			je.Save.Press();
			pg.ReleaseBatchProc(je.BatchModule.Current);
			if (release.AutoPost)
			{
				pg.PostBatchProc(je.BatchModule.Current);
			}
		}

		private void CreateNewBatch(JournalEntry je, PostGraph pg, APInvoiceEntry graph, APReleaseProcess release, APInvoice apDoc, Batch batch)
		{
			release.IsInvoiceReclassification = true;
			apDoc.Released = false;
			foreach (PXResult<APInvoice, CM.Extensions.CurrencyInfo, Terms, Vendor> res in release.APInvoice_DocType_RefNbr.Select(apDoc.DocType, apDoc.RefNbr))
			{
				JournalEntry.SegregateBatch(je, BatchModule.AP, apDoc.BranchID, apDoc.CuryID, apDoc.DocDate, apDoc.FinPeriodID, apDoc.DocDesc, ((CM.Extensions.CurrencyInfo)res).GetCM(), null);
				APRegister doc = apDoc;
				release.ReleaseInvoice(je, ref doc, res, false, out List<INRegister> inDocs);

				je.BatchModule.Current.OrigBatchNbr = batch.BatchNbr;
				je.BatchModule.Current.OrigModule = batch.Module;

				je.Save.Press();
			}
			apDoc.Released = true;
			apDoc.BatchNbr = je.BatchModule.Current.BatchNbr;

			APInvoiceExt extAPDoc = graph.Document.Cache.GetExtension<APInvoiceExt>(apDoc);
			extAPDoc.Reclassified = true;

			if (release.AutoPost)
			{
				pg.PostBatchProc(je.BatchModule.Current);
			}
		}

		private void UpdatePOLines(APInvoiceEntry graph, APReleaseProcess release, APInvoice apDoc)
		{
			foreach (APTran apTran in graph.Transactions.Select())
			{
				var extAPTran = graph.Transactions.Cache.GetExtension<APTranExt>(apTran);
				if (extAPTran.PrevPOLineNbr != apTran.POLineNbr)
				{
					POReceiptLine receipt = SelectFrom<POReceiptLine>
						.Where<POReceiptLine.pOType.IsEqual<P.AsString>
							.And<POReceiptLine.pONbr.IsEqual<P.AsString>>
							.And<POReceiptLine.pOLineNbr.IsEqual<P.AsInt>.Or<POReceiptLine.pOLineNbr.IsEqual<P.AsInt>>>>
						.View.Select(graph, apTran.POOrderType, apTran.PONbr, apTran.POLineNbr, extAPTran.PrevPOLineNbr).FirstOrDefault();

					if (receipt != null)
						throw new PXException(ProjectAccountingMessages.CannotReleasedWithLinkedReceipts, receipt.ReceiptNbr, apTran.LineNbr);

					release.IsInvoiceReclassification = false;

					apTran.POAccrualLineNbr = apTran.POLineNbr;
					UpdatePOLine(release, apDoc, apTran, apTran.POLineNbr.Value, false);

					var reverse = (APTran)graph.Transactions.Cache.CreateCopy(apTran);
					reverse.POAccrualLineNbr = extAPTran.PrevPOLineNbr.Value;
					reverse.CuryLineAmt *= -1;
					reverse.LineAmt *= -1;
					reverse.CuryTranAmt *= -1;
					reverse.TranAmt *= -1;
					reverse.CuryRetainageAmt *= -1;
					reverse.RetainageAmt *= -1;
					reverse.Qty *= -1;
					reverse.BaseQty *= -1;
					UpdatePOLine(release, apDoc, reverse, extAPTran.PrevPOLineNbr.Value, true);

					DeletePOLineRevision(release, apDoc, reverse, extAPTran.PrevPOLineNbr.Value);
				}
			}
		}
		
		private void UpdatePOLine(APReleaseProcess release, APInvoice apDoc, APTran apTran, int poLineNbr, bool open)
		{
			POAccrualStatus accrual = SelectFrom<POAccrualStatus>
				.Where<POAccrualStatus.refNoteID.IsEqual<P.AsGuid>.And<POAccrualStatus.lineNbr.IsEqual<P.AsInt>>>
				.View.Select(release, apTran.POAccrualRefNoteID, apTran.POAccrualLineNbr);

			var updatePO = release.FindImplementation<UpdatePOOnRelease>();

			var poRes = (PXResult<POLineUOpen, POLine, POOrder>)updatePO.poOrderLineUPD.Select(apTran.POOrderType, apTran.PONbr, poLineNbr);

			updatePO.UpdatePOAccrualStatus(accrual, apTran, apDoc, poRes, poRes, null);

			if (poRes != null)
			{
				var srcLine = (POLine)poRes;
				var updLine = (POLineUOpen)poRes;
				var order = (POOrder)poRes;

				if (open)
				{
					updLine.Completed = false;
					updLine.Closed = false;
					updatePO.poOrderUPD.Update(order);
				}
				updLine = updatePO.UpdatePOLine(apTran, apDoc, srcLine, (POOrder)poRes, updLine, false);
			}
		}

		private void DeletePOLineRevision(APReleaseProcess release, APInvoice apDoc, APTran apTran, int poLineNbr)
		{
			var updatePO = release.FindImplementation<UpdatePOOnRelease>();

			var poRes = (PXResult<POLineUOpen, POLine, POOrder>)updatePO.poOrderLineUPD.Select(apTran.POOrderType, apTran.PONbr, poLineNbr);

			if (poRes != null)
			{
				var srcLine = (POLine)poRes;

				APInvoicePOValidation releaseExt = release.GetExtension<APInvoicePOValidation>();
				if (releaseExt != null)
					releaseExt.DeletePOLineRevision(apDoc, srcLine, (POOrder)poRes);
			}
		}

		protected virtual void _(Events.RowSelected<APInvoice> e)
		{
			if (e.Row == null) return;

			bool isProjAcct = !string.IsNullOrEmpty(PredefinedRoles.ProjectAccountant)
				&& PXContext.PXIdentity.User.IsInRole(PredefinedRoles.ProjectAccountant);
			bool isFinSuperviser = !string.IsNullOrEmpty(PredefinedRoles.FinancialSupervisor) 
				&& PXContext.PXIdentity.User.IsInRole(PredefinedRoles.FinancialSupervisor);
			
			if (!isProjAcct && !isFinSuperviser)
				reclassify.SetEnabled(false);

			if (e.Row.Status == APDocStatus.UnderReclassification)
			{
				if (!isProjAcct && !isFinSuperviser)
					Base.release.SetEnabled(false);

				Base.Transactions.Cache.AllowUpdate = true;

				APInvoiceState invoiceState = Base.GetDocumentState(e.Cache, e.Row);
				PXUIFieldAttribute.SetEnabled<APInvoice.projectID>(e.Cache, e.Row, !invoiceState.HasPOLink && !invoiceState.IsFromExpenseClaims);
			}
		}

		protected virtual void _(Events.RowSelected<APTran> e)
		{
			if (e.Row != null && Base.Document.Current.Status == APDocStatus.UnderReclassification)
			{
				PXUIFieldAttribute.SetEnabled<APTran.branchID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.inventoryID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.tranDesc>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.qty>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.uOM>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.curyUnitCost>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.curyLineAmt>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.curyDiscAmt>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.nonBillable>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.taxCategoryID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.date>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.box1099>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.discountID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.discPct>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.manualPrice>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.manualDisc>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.curyRetainageAmt>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<APTran.retainagePct>(e.Cache, e.Row, false);

				if (e.Row.PONbr != null || e.Row.DeferredCode != null)
				{
					PXUIFieldAttribute.SetEnabled<APTran.projectID>(e.Cache, e.Row, false);
					PXUIFieldAttribute.SetEnabled<APTran.taskID>(e.Cache, e.Row, false);
					PXUIFieldAttribute.SetEnabled<APTran.costCodeID>(e.Cache, e.Row, false);
					PXUIFieldAttribute.SetEnabled<APTran.accountID>(e.Cache, e.Row, false);
					PXUIFieldAttribute.SetEnabled<APTran.subID>(e.Cache, e.Row, false);
				}

				var extAPTran = Base.Transactions.Cache.GetExtension<APTranExt>(e.Row);
				bool isService = true;
				if (extAPTran.PrevPOLineNbr != null)
				{
					POLine pOLine = SelectPOLine(e.Row.POOrderType, e.Row.PONbr, extAPTran.PrevPOLineNbr);
					isService = pOLine.LineType == POLineType.Service;
				}

				PXUIFieldAttribute.SetEnabled<APTran.pOLineNbr>(e.Cache, e.Row, 
					e.Row.PONbr != null && 
					e.Row.POOrderType != POOrderType.RegularSubcontract && 
					isService && 
					e.Row.DeferredCode == null &&
					e.Row.ReceiptNbr == null);

				PXUIFieldAttribute.SetEnabled<CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(e.Cache, e.Row, 
					e.Row.PONbr != null &&
					e.Row.POOrderType == POOrderType.RegularSubcontract && 
					e.Row.DeferredCode == null);

				if (e.Row.DeferredCode != null)
				{
					PXUIFieldAttribute.SetWarning<APTran.deferredCode>(e.Cache, e.Row, ProjectAccountingMessages.CannotReclassifiedWithDeferredCode);
				}
			}
			else
			{
				PXUIFieldAttribute.SetEnabled<APTran.pOLineNbr>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(e.Cache, e.Row, false);
			}
		}

		protected virtual void _(Events.RowPersisting<APTran> e)
		{
			if (e.Row != null && Base.Document.Current != null && Base.Document.Current.Status == APDocStatus.UnderReclassification && e.Row.PONbr != null)
			{
				if (e.Row.POLineNbr == null)
				{
					if (e.Row.POOrderType == POOrderType.RegularSubcontract)
					{
						e.Cache.RaiseExceptionHandling<CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(
							e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty));
					}
					else
					{
						e.Cache.RaiseExceptionHandling<APTran.pOLineNbr>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty));
					}
					return;
				}

				var extAPTran = Base.Transactions.Cache.GetExtension<APTranExt>(e.Row);
				if (e.Row.POLineNbr != extAPTran.PrevPOLineNbr)
				{
					POLine oldPOLine = SelectPOLine(e.Row.POOrderType, e.Row.PONbr, extAPTran.PrevPOLineNbr);
					POLine pOLine = SelectPOLine(e.Row.POOrderType, e.Row.PONbr, e.Row.POLineNbr);

					if (pOLine.InventoryID != oldPOLine.InventoryID)
					{
						PXSetPropertyException exc = null;
						if (oldPOLine.InventoryID == null)
						{
							exc = new PXSetPropertyException(ProjectAccountingMessages.EmptyInventoryItemCannotBeChanged);
						}
						else
						{
							var item = InventoryItem.PK.Find(Base, oldPOLine.InventoryID);
							exc = new PXSetPropertyException(ProjectAccountingMessages.InventoryItemCannotBeChanged, item.InventoryCD);
						}
						
						if (e.Row.POOrderType == POOrderType.RegularSubcontract)
						{
							e.Cache.RaiseExceptionHandling<CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(
								e.Row, null, exc);
						}
						else
						{
							e.Cache.RaiseExceptionHandling<APTran.pOLineNbr>(
								e.Row, null, exc);
						}
					}
				}	
			}
		}

		protected virtual void _(Events.FieldUpdated<APTran, APTran.pOLineNbr> e)
		{
			if (e.NewValue != null && e.OldValue != null)
			{
				POLine pOLine = SelectPOLine(e.Row.POOrderType, e.Row.PONbr, e.Row.POLineNbr);
				if (pOLine.LineType != POLineType.Service)
				{
					e.Cache.RaiseExceptionHandling<APTran.pOLineNbr>(
							e.Row, null, new PXSetPropertyException(ProjectAccountingMessages.CannotAdjustedWithNonServiceLine));
				}
				UpdateAPTran(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<APTran, CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr> e)
		{
			if (e.NewValue != null && e.OldValue != null)
			{
				UpdateAPTran(e.Row);
			}
		}

		private void UpdateAPTran(APTran row)
		{
			POLine poLine = SelectPOLine(row.POOrderType, row.PONbr, row.POLineNbr);
			if (poLine != null)
			{
				row.ProjectID = poLine.ProjectID;
				row.TaskID = poLine.TaskID;
				row.CostCodeID = poLine.CostCodeID;
				row.AccountID = poLine.ExpenseAcctID;
				row.SubID = poLine.ExpenseSubID;
			}
		}

		private POLine SelectPOLine(string orderType, string orderNbr, int? lineNbr)
		{
			POLine result = SelectFrom<POLine>
						.Where<POLine.orderType.IsEqual<P.AsString>
							.And<POLine.orderNbr.IsEqual<P.AsString>
							.And<POLine.lineNbr.IsEqual<P.AsInt>>>>
						.View.Select(Base, orderType, orderNbr, lineNbr);
			return result;
		}

		[PXOverride]
		public virtual void Persist(Action persist)
		{
			if (Base.Document.Current != null && Base.Document.Current.Status == APDocStatus.UnderReclassification)
			{
				var poLines = new Dictionary<Tuple<string, string, int>, APTran>();
				foreach (APTran apTran in Base.Transactions.Select())
				{
					if (apTran.PONbr != null)
					{
						var poLineKey = new Tuple<string, string, int>(apTran.POOrderType, apTran.PONbr, apTran.POLineNbr.Value);
						if (poLines.TryGetValue(poLineKey, out APTran apTranPair))
						{
							var extAPTran = Base.Transactions.Cache.GetExtension<APTranExt>(apTran);
							APTran apTranDup = extAPTran.PrevPOLineNbr == apTran.POLineNbr ? apTranPair : apTran;

							var exc = new PXSetPropertyException(ProjectAccountingMessages.CommitmentLineCannotBeSelected, apTranDup.POLineNbr.Value);
							if (apTranDup.POOrderType == POOrderType.RegularSubcontract)
							{
								Base.Transactions.Cache.RaiseExceptionHandling<CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(apTranDup, apTranDup.POLineNbr, exc);
							}
							else
							{
								Base.Transactions.Cache.RaiseExceptionHandling<APTran.pOLineNbr>(apTranDup, apTranDup.POLineNbr, exc);
							}
							throw new PXException();
						}
						else
						{
							poLines.Add(poLineKey, apTran);
						}
					}
				}
			}
			persist();
		}
	}

	public class APSetupMaintExt : PXGraphExtension<APSetupMaint>
	{
		protected virtual void _(Events.FieldVerifying<APSetup, APSetupExt.reclassifyInvoices> e)
		{
			if (e.Row == null) return;

			if ((bool?)e.OldValue == true && (bool?)e.NewValue == false)
			{
				APInvoice doc = PXSelect<APInvoice,
					Where<APInvoice.status, Equal<APDocStatus.underReclassification>>>.SelectSingleBound(Base, null);
				if (doc != null)
				{
					throw new PXSetPropertyException<APSetupExt.reclassifyInvoices>(ProjectAccountingMessages.CannotClearReclassifyBill);
				}
			}
		}
	}

	public class APInvoiceEntryExt_Workflow : PXGraphExtension<APInvoiceEntry_Workflow, APInvoiceEntry>
	{
		private class APSetupReclassification : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<APSetupReclassification>(nameof(APSetupReclassification), typeof(APSetup)).ReclassifyInvoices;

			private bool ReclassifyInvoices;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord apSetup = PXDatabase.SelectSingle<APSetup>(new PXDataField<APSetupExt.reclassifyInvoices>()))
				{
					if (apSetup != null)
						ReclassifyInvoices = apSetup.GetBoolean(0) == true;
				}
			}
		}
		public class Conditions : Condition.Pack
		{
			public Condition IsReclassificationNotActive => GetOrCreate(c => 
				ReclassificationIsActive()
					? c.FromBql<APInvoice.docType.IsNotEqual<APDocType.invoice>>()
				:c.FromBql<True.IsEqual<True>>());

			public Condition IsRetainageDocument => GetOrCreate(c => c.FromBql<
				APInvoice.isRetainageDocument.IsEqual<True>
			>());
		}
		

		protected static bool ReclassificationIsActive() => APSetupReclassification.IsActive;

		public override void Configure(PXScreenConfiguration config)
		{
			WorkflowContext<APInvoiceEntry, APInvoice> context = config.GetScreenConfigurationContext<APInvoiceEntry, APInvoice>();
			var conditions = context.Conditions.GetPack<Conditions>();


			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Update<APDocStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add<APInvoiceEntryReclassifyingExt>(g => g.reclassify);
									});
							});
							fss.Update<APDocStatus.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add<APInvoiceEntryReclassifyingExt>(g => g.reclassify);
									});
							});
							fss.Add<APDocStatus.underReclassification>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.payInvoice);
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
									});
							});

						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<APDocStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<APDocStatus.underReclassification>()
									.IsTriggeredOn<APInvoiceEntryReclassifyingExt>(g => g.reclassify));
							});
							transitions.UpdateGroupFrom<APDocStatus.closed>(ts =>
							{
								ts.Add(t => t
									.To<APDocStatus.underReclassification>()
									.IsTriggeredOn<APInvoiceEntryReclassifyingExt>(g => g.reclassify));
							});
							transitions.AddGroupFrom<APDocStatus.underReclassification>(ts =>
							{
								ts.Add(t => t
									.To<APDocStatus.open>()
									.IsTriggeredOn(g => g.OnReleaseDocument)
									.When(context.Conditions.Get("IsOpen")));
								ts.Add(t => t
									.To<APDocStatus.closed>()
									.IsTriggeredOn(g => g.OnReleaseDocument)
									.When(context.Conditions.Get("IsClosed")));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add<APInvoiceEntryReclassifyingExt>(g => g.reclassify, c => c
							.WithCategory(context.Categories.Get(APQuickCheckEntry_Workflow.ActionCategoryNames.Corrections))
							.IsDisabledWhen(conditions.IsReclassificationNotActive || conditions.IsRetainageDocument));
						actions.Update(g => g.reverseInvoice, a => a.PlaceAfterInCategory(nameof(APInvoiceEntryReclassifyingExt.reclassify)));
					}));
		}
	}

	public sealed class APInvoiceExt : PXCacheExtension<APInvoice>
	{
		#region Reclassified
		public abstract class reclassified : PX.Data.BQL.BqlInt.Field<reclassified> { }
		/// <summary>
		/// True if the invoice was reclassified
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? Reclassified { get; set; }
		#endregion
	}

	public sealed class APTranExt : PXCacheExtension<APTran>
	{
		#region Reclassified
		public abstract class reclassified : PX.Data.BQL.BqlInt.Field<reclassified> { }
		/// <summary>
		/// True if the line was reclassified
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? Reclassified { get; set; }
		#endregion

		#region PrevPOLineNbr
		public abstract class prevPOLineNbr : PX.Data.BQL.BqlInt.Field<prevPOLineNbr> { }
		/// <summary>
		/// The previous PO Line nbr before bill reclassifying
		/// </summary>
		[PXDBInt]
		public int? PrevPOLineNbr { get; set; }
		#endregion
	}

	public sealed class APSetupExt : PXCacheExtension<APSetup>
	{
		#region ReclassifyInvoices
		public abstract class reclassifyInvoices : PX.Data.BQL.BqlBool.Field<reclassifyInvoices> { }
		/// <summary>
		/// Enable invoice reclassifications
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Allow Bill Reclassification")]
		public bool? ReclassifyInvoices { get; set; }
		#endregion
	}
}
