using PX.Common;
using PX.Data;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.AR.Repositories;
using PX.Objects.Common;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
namespace PX.Objects.AR.CCPaymentProcessing.Helpers
{
	public static class ExternalTranHelper
	{
		public enum SharedTranStatus
		{ 
			None,
			NeedSynchronization,
			Synchronized,
			ClearState,
		}

		public static bool HasTransactions(PXSelectBase<ExternalTransaction> extTrans)
		{
			return extTrans.Any();
		}

		public static IExternalTransaction GetActiveTransaction(PXSelectBase<ExternalTransaction> extTrans)
		{
			return GetActiveTransaction(extTrans.Select().RowCast<ExternalTransaction>());
		}

		public static IExternalTransaction GetDeactivatedNeedSyncTransaction(IEnumerable<IExternalTransaction> extTrans)
		{
			extTrans = extTrans.OrderByDescending(i => i.TransactionID);
			IExternalTransaction extTran = extTrans.Where(i => i.NeedSync == true && i.Active == false).FirstOrDefault();
			return extTran;
		}

		public static IExternalTransaction GetActiveTransaction(IEnumerable<IExternalTransaction> extTrans)
		{
			extTrans = extTrans.OrderByDescending(i => i.TransactionID);
			IExternalTransaction extTran = extTrans.Where(i => i.Active == true).FirstOrDefault();
			return extTran;
		}

		public static bool HasSuccessfulTrans(PXSelectBase<ExternalTransaction> extTrans)
		{
			IExternalTransaction extTran = GetActiveTransaction(extTrans);
			if (extTran != null && !IsExpired(extTran))
			{
				return true;
			}
			return false;
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R1)]
		public static bool HasVoidPreAuthorizedOrAfterReviewInHistory(PXGraph graph, IExternalTransaction extTran)
		{
			if (graph == null)
			{
				throw new ArgumentNullException(nameof(graph));
			}
			if (extTran == null)
			{
				throw new ArgumentNullException(nameof(extTran));
			}
			CCProcTranRepository repo = new CCProcTranRepository(graph);
			var history = repo.GetCCProcTranByTranID(extTran.TransactionID);

			return CCProcTranHelper.HasVoidPreAuthorized(history) || CCProcTranHelper.HasVoidAfterReview(history);
		}

		public static bool HasTransactions(PXGraph graph, int? pmInstanceId)
		{
			ExternalTransaction tran = PXSelect<ExternalTransaction, 
				Where<ExternalTransaction.pMInstanceID, Equal<Required<ExternalTransaction.pMInstanceID>>>>
				.SelectWindowed(graph, 0, 1, pmInstanceId);
			return tran != null;
		}

		public static bool IsExpired(IExternalTransaction extTran)
		{
			return (extTran.ExpirationDate.HasValue && extTran.ExpirationDate.Value < PXTimeZoneInfo.Now &&
					(extTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeSuccess 
					|| extTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeHeldForReview
					|| extTran.ProcStatus == ExtTransactionProcStatusCode.CaptureHeldForReview)) 
				|| extTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeExpired 
				|| extTran.ProcStatus == ExtTransactionProcStatusCode.CaptureExpired;
		}

		public static bool IsFailed(IExternalTransaction extTran)
		{
			return extTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeFail
				|| extTran.ProcStatus == ExtTransactionProcStatusCode.CaptureFail
				|| extTran.ProcStatus == ExtTransactionProcStatusCode.VoidFail
				|| extTran.ProcStatus == ExtTransactionProcStatusCode.CreditFail;
		}

		public static ExternalTransactionState GetActiveTransactionState(PXGraph graph, PXSelectBase<ExternalTransaction> extTran)
		{
			var trans = extTran.Select().RowCast<ExternalTransaction>();
			return GetActiveTransactionState(graph, trans);
		}

		public static ExternalTransactionState GetLastTransactionState(PXGraph graph, PXSelectBase<ExternalTransaction> extTran)
		{
			var trans = extTran.Select().RowCast<ExternalTransaction>();
			return GetLastTransactionState(graph, trans);
		}

		public static ExternalTransactionState GetLastTransactionState(PXGraph graph, IEnumerable<IExternalTransaction> extTrans)
		{
			extTrans = extTrans.OrderByDescending(i => i.TransactionID);
			IExternalTransaction extTran = extTrans.FirstOrDefault();
			ExternalTransactionState state;
			if (extTran != null)
			{
				state = GetTransactionState(graph, extTran);
			}
			else
			{
				state = new ExternalTransactionState();
			}
			return state;
		}

		public static ExternalTransactionState GetTransactionState(PXGraph graph, IExternalTransaction extTran)
		{
			if (graph == null)
			{
				throw new ArgumentNullException(nameof(graph));
			}
			if (extTran == null)
			{
				throw new ArgumentNullException(nameof(extTran));
			}
			ICCPaymentProcessingRepository repo = new CCPaymentProcessingRepository(graph);
			ExternalTransactionState state = new ExternalTransactionState(extTran);
				
			CheckTranExpired(state);
			if (state.HasErrors && !(state.SyncFailed && extTran.Active == false))
			{
				ApplyLastSuccessfulTran(repo, state);
			}

			FormatDescription(graph, state);
			return state;
		}

		public static bool HasImportedNeedSyncTran(PXGraph graph, PXSelectBase<ExternalTransaction> extTrans)
		{
			var trans = extTrans.Select().RowCast<ExternalTransaction>();
			return HasImportedNeedSyncTran(graph, trans);
		}

		public static bool HasImportedNeedSyncTran(PXGraph graph, IEnumerable<IExternalTransaction> extTrans)
		{
			bool ret = false;
			IExternalTransaction tran = GetImportedNeedSyncTran(graph, extTrans);
			if (tran != null)
			{
				ret = true;
			}
			return ret;
		}

		public static IExternalTransaction GetImportedNeedSyncTran(PXGraph graph, IEnumerable<IExternalTransaction> extTrans)
		{
			IExternalTransaction ret = extTrans.OrderByDescending(i => i.TransactionID).FirstOrDefault(i => i.NeedSync == true 
				|| (i.ProcStatus == ExtTransactionProcStatusCode.Unknown && i.Active == true));
			return ret;
		}

		public static SharedTranStatus GetSharedTranStatus(PXGraph graph, IExternalTransaction extTran)
		{
			SharedTranStatus ret = SharedTranStatus.None;
			if (extTran != null && extTran.VoidDocType == ARDocType.Refund)
			{			
				if (extTran.SyncStatus == CCSyncStatusCode.Error)
				{
					ret = SharedTranStatus.ClearState;
				}
				else
				{
					var repo = new CCProcTranRepository(graph);
					var procTran = repo.GetCCProcTranByTranID(extTran.TransactionID).OrderByDescending(i => i.TranNbr)
						.Where(i => i.ProcStatus == CCProcStatus.Finalized && i.TranStatus == CCTranStatusCode.Approved)
						.FirstOrDefault();
					if (procTran != null && procTran.DocType != extTran.VoidDocType && procTran.RefNbr != extTran.VoidRefNbr)
					{
						ret = SharedTranStatus.ClearState;
					}
				}

				if (ret != SharedTranStatus.ClearState)
				{
					ExternalTransactionState state = GetTransactionState(graph, extTran);
					if (state.NeedSync)
					{
						ret = SharedTranStatus.NeedSynchronization;
					}
					else if (state.IsVoided && !state.NeedSync)
					{
						ret = SharedTranStatus.Synchronized;
					}

				}
			}
			return ret;
		}

		public static bool HasOpenCCProcTran(PXGraph graph, IExternalTransaction extTran)
		{
			bool ret = false;
			if (graph == null)
			{
				throw new ArgumentNullException(nameof(graph));
			}
			if (extTran == null)
				return false;
			
			if (extTran.ProcStatus == ExtTransactionProcStatusCode.Unknown)
			{
				CCProcTranRepository repo = new CCProcTranRepository(graph);
				var records = repo.GetCCProcTranByTranID(extTran.TransactionID);
				CCProcTran procTran = records.FirstOrDefault();
				if (procTran?.ProcStatus == CCProcStatus.Opened)
				{ 
					ret = true;
				}
			}
			return ret;
		}

		public static ExternalTransactionState GetActiveTransactionState(PXGraph graph, IEnumerable<IExternalTransaction> extTrans)
		{
			if (graph == null)
			{
				throw new ArgumentNullException(nameof(graph));
			}
			if (extTrans == null)
			{
				throw new ArgumentNullException(nameof(extTrans));
			}
			ExternalTransactionState state = new ExternalTransactionState();
			CCProcTranRepository repo = new CCProcTranRepository(graph);
			var extTran = GetActiveTransaction(extTrans);
			if (extTran != null)
			{
				state = GetTransactionState(graph, extTran);
			}
			return state;
		}

		public static ProcessingStatus GetPossibleErrorStatusForTran(ExternalTransactionState state)
		{
			if (state.HasErrors)
			{
				return state.ProcessingStatus;
			}
			if (state.IsCaptured)
			{
				return ProcessingStatus.CaptureFail;
			}
			else if (state.IsPreAuthorized)
			{
				return ProcessingStatus.AuthorizeFail;
			}
			else if (state.IsVoided)
			{
				return ProcessingStatus.VoidFail;
			}
			else if (state.IsRefunded)
			{
				return ProcessingStatus.CreditFail;
			}
			return ProcessingStatus.Unknown;
		}

		public static IExternalTransaction GetLastProcessedExtTran(IEnumerable<IExternalTransaction> extTrans, IEnumerable<ICCPaymentTransaction> trans)
		{
			IExternalTransaction ret = null;
			var tran = trans.OrderByDescending(i => i.TranNbr).FirstOrDefault();
			if (tran != null)
			{
				ret = extTrans.FirstOrDefault(i => i.TransactionID == tran.TransactionID);
			}
			return ret;
		}

		private static void ApplyLastSuccessfulTran(ICCPaymentProcessingRepository repo, ExternalTransactionState state)
		{
			ICCPaymentTransaction paymentTran = LastSuccessfulCCProcTran(state.ExternalTransaction, repo);
			if (paymentTran != null)
			{
				switch (paymentTran.TranType)
				{
					case CCTranTypeCode.Authorize: state.IsPreAuthorized = true; break;
					case CCTranTypeCode.AuthorizeAndCapture:
					case CCTranTypeCode.PriorAuthorizedCapture:
					case CCTranTypeCode.CaptureOnly: state.IsCaptured = true; break;
					case CCTranTypeCode.Credit: state.IsRefunded = true; break;
				}
				state.IsOpenForReview = paymentTran.TranStatus == CCTranStatusCode.HeldForReview;
				CheckTranExpired(state);
			}
		}

		public static ICCPaymentTransaction LastSuccessfulCCProcTran(IExternalTransaction tran, ICCPaymentProcessingRepository repo)
		{
			var procTrans = repo.GetCCProcTranByTranID(tran.TransactionID).Cast<ICCPaymentTransaction>();
			ICCPaymentTransaction paymentTran = CCProcTranHelper.FindCCLastSuccessfulTran(procTrans);
			return paymentTran;
		}

		private static void CheckTranExpired(ExternalTransactionState state)
		{
			if ((state.IsPreAuthorized || state.IsCaptured) && IsExpired(state.ExternalTransaction))
			{
				if (state.IsPreAuthorized)
				{
					state.ProcessingStatus = ProcessingStatus.AuthorizeExpired;
				}
				if (state.IsCaptured)
				{
					state.ProcessingStatus = ProcessingStatus.CaptureExpired;
				}

				state.IsPreAuthorized = false;
				state.IsCaptured = false;
				state.IsOpenForReview = false;
				state.HasErrors = false;
				state.IsExpired = true;
			}
		}

		public static void FormatDescription(PXGraph graph, ExternalTransactionState extTranState)
		{
			string currStatus = null;
			IExternalTransaction extTran = extTranState.ExternalTransaction;
			if (extTran == null)
			{
				return;
			}

			if (extTranState.SyncFailed)
			{
				currStatus = GetSyncFailedDescription(graph, extTranState);
			}
			else
			{
				ExtTransactionProcStatusCode.ListAttribute attr = new ExtTransactionProcStatusCode.ListAttribute();
				string procStatusStr = ExtTransactionProcStatusCode.GetProcStatusStrByProcessingStatus(extTranState.ProcessingStatus);
				if (!string.IsNullOrEmpty(procStatusStr))
				{
					currStatus = PXMessages.LocalizeNoPrefix(attr.ValueLabelDic[procStatusStr]);
				}
			}

			if(!string.IsNullOrEmpty(currStatus))
			{
				ExternalTransactionState parentExtTranState = GetParentExternalTransactionState(graph, extTran);

				bool isOrigPayment = CheckDocIsPayment(graph);
				if (extTranState.NeedSync)
				{
					currStatus = AppendPreviousDescriptionForSyncTran(graph, extTranState, currStatus);
				}
				else if(isOrigPayment && parentExtTranState != null)
				{
					currStatus = AppendDescription(graph, parentExtTranState, currStatus);
				}
				else
				{
					currStatus = AppendPreviousDescription(graph, extTranState, currStatus);
				}
			}

			extTranState.Description = string.IsNullOrEmpty(currStatus)
										? string.Empty
										: currStatus;
		}

		private static ExternalTransactionState GetParentExternalTransactionState(PXGraph graph, IExternalTransaction extTran)
		{
			ExternalTransactionRepository extTranRepo = new ExternalTransactionRepository(graph);
			ExternalTransaction parentExtTran = extTranRepo.GetExternalTransaction(extTran.ParentTranID);
			ExternalTransactionState parentExtTranState = null;
			if (parentExtTran != null)
			{
				parentExtTranState = new ExternalTransactionState(parentExtTran);
			}

			return parentExtTranState;
		}

		/// <summary>
		/// Retrieves a current record from the caches and checks its ARDocType value.
		/// This is necessary for setting the "Captured, Refunded" processing status of the document
		/// </summary>
		/// <param name="graph">Current graph</param>
		/// <returns></returns>
		private static bool CheckDocIsPayment(PXGraph graph)
		{
			var currentPaymentRecord = graph.Caches[typeof(ARRegister)].Current as ARRegister;
			bool isOrigPayment = currentPaymentRecord?.DocType == ARDocType.Payment
								|| currentPaymentRecord?.DocType == ARDocType.Prepayment;
			return isOrigPayment;
		}

		private static string AppendPreviousDescriptionForSyncTran(PXGraph graph, ExternalTransactionState state, string currStatus)
		{
			string ret = currStatus;
			if (state.ProcessingStatus == ProcessingStatus.Unknown)
			{
				var payment = graph.Caches[typeof(ARPayment)].Current as ARPayment;
				if (payment?.CCActualExternalTransactionID != null)
				{
					bool needPrevStatus = payment.DocType != ARDocType.Refund;
					string prevStatus = null;
					CCPaymentProcessingRepository repo = new CCPaymentProcessingRepository(graph);
					ExternalTransaction extTran = repo.GetExternalTransaction(payment.CCActualExternalTransactionID);
					ICCPaymentTransaction procTran = GetSuccessfulBeforeSyncTran(extTran, repo);

					if (needPrevStatus && procTran != null)
					{
						prevStatus = GetPreviousStatusForSyncTran(procTran.TranType);
					}

					CCTranType? tranType = null;
					if (procTran != null)
					{
						tranType = CCTranTypeCode.GetTranTypeByTranTypeStr(procTran.TranType);
					}

					if((procTran == null && payment.DocType == ARDocType.Refund) 
						|| (tranType.HasValue && (tranType == CCTranType.AuthorizeOnly
								|| CCTranTypeCode.IsCaptured(tranType.Value))
							)
						)
					{
						ret = state.SyncFailed ? Messages.CCVoidRefundSyncFailed : Messages.CCVoidRefundNeedSync;
						ret = PXMessages.LocalizeNoPrefix(ret);
					}

					if (procTran == null && CheckDocIsPayment(graph))
					{
						ret = state.SyncFailed ? Messages.CCAuthCaptureSyncFailed : Messages.CCAuthCaptureNeedSync;
						ret = PXMessages.LocalizeNoPrefix(ret);
					}

					if (prevStatus != null)
					{
						ret = prevStatus + "; " + ret;
					}
				}
			}
			return ret;
		}

		private static ICCPaymentTransaction GetSuccessfulBeforeSyncTran(ExternalTransaction extTran, ICCPaymentProcessingRepository repo)
		{
			ICCPaymentTransaction procTran = null;
			if (extTran.ProcStatus == ExtTransactionProcStatusCode.Unknown)
			{
				var trans = repo.GetCCProcTranByTranID(extTran.TransactionID)
					.Cast<CCProcTran>().OrderByDescending(i => i.TranNbr)
					.SkipWhile(i => i.TranType == CCTranTypeCode.Unknown);
				procTran = CCProcTranHelper.FindCCLastSuccessfulTran(trans);
			}
			else
			{
				procTran = LastSuccessfulCCProcTran(extTran, repo);
			}
			
			return procTran;
		}

		private static string AppendPreviousDescription(PXGraph graph, ExternalTransactionState extTranState, string currStatus)
		{
			bool needPrevStatus = extTranState.HasErrors && !extTranState.SyncFailed ;
			if (needPrevStatus)
			{
				currStatus = AppendDescription(graph, extTranState, currStatus);
			}
			return currStatus;
		}

		private static string AppendDescription(PXGraph graph, ExternalTransactionState extTranState, string currStatus)
		{
			CCPaymentProcessingRepository repo = new CCPaymentProcessingRepository(graph);
			ICCPaymentTransaction procTran = LastSuccessfulCCProcTran(extTranState.ExternalTransaction, repo);
			string prevStatus = null;
			if (procTran != null)
			{
				prevStatus = GetStatusByTranType(procTran.TranType);
			}

			if (!string.IsNullOrEmpty(prevStatus))
			{
				currStatus = prevStatus + ", " + currStatus;
			}
			return currStatus;
		}

		public static bool UpdateCapturedState<T>(T doc, ExternalTransactionState tranState)
		where T : class, IBqlTable, ICCCapturePayment
		{
			bool needUpdate = false;
			IExternalTransaction extTran = tranState.ExternalTransaction;
			if (doc.IsCCCaptured != tranState.IsCaptured)
			{
				doc.IsCCCaptured = tranState.IsCaptured;
				needUpdate = true;
			}

			if (tranState.IsCaptured)
			{
				doc.CuryCCCapturedAmt = extTran.Amount;
				doc.IsCCCaptureFailed = false;
				needUpdate = true;
			}

			if (tranState.ProcessingStatus == ProcessingStatus.CaptureFail)
			{
				doc.IsCCCaptureFailed = true;
				needUpdate = true;
			}

			if (doc.IsCCCaptured == false && (doc.CuryCCCapturedAmt != decimal.Zero))
			{
				doc.CuryCCCapturedAmt = decimal.Zero;
				needUpdate = true;
			}

			return needUpdate;
		}

		public static bool UpdateCCPaymentState<T>(T doc, ExternalTransactionState tranState)
			where T : class, ICCAuthorizePayment, ICCCapturePayment
		{
			IExternalTransaction externalTran = tranState.ExternalTransaction;
			bool needUpdate = false;

			if (doc.IsCCAuthorized != tranState.IsPreAuthorized || doc.IsCCCaptured != tranState.IsCaptured)
			{
				if (!(tranState.ProcessingStatus == ProcessingStatus.VoidFail || tranState.ProcessingStatus == ProcessingStatus.CreditFail))
				{
					doc.IsCCAuthorized = tranState.IsPreAuthorized;
					doc.IsCCCaptured = tranState.IsCaptured;
					needUpdate = true;
				}
				else
				{
					doc.IsCCAuthorized = false;
					doc.IsCCCaptured = false;
					needUpdate = false;
				}
			}

			if (externalTran != null && tranState.IsPreAuthorized)
			{
				doc.CCAuthExpirationDate = externalTran.ExpirationDate;
				doc.CuryCCPreAuthAmount = externalTran.Amount;
				needUpdate = true;
			}

			if (doc.IsCCAuthorized == false && (doc.CCAuthExpirationDate != null || doc.CuryCCPreAuthAmount > Decimal.Zero))
			{
				doc.CCAuthExpirationDate = null;
				doc.CuryCCPreAuthAmount = Decimal.Zero;

				needUpdate = true;
			}

			if (tranState.IsCaptured)
			{
				doc.CuryCCCapturedAmt = externalTran.Amount;
				doc.IsCCCaptureFailed = false;
				needUpdate = true;
			}
		
			if(tranState.ProcessingStatus == ProcessingStatus.CaptureFail)
			{
				doc.IsCCCaptureFailed = true;
				needUpdate = true;
			}

			if (doc.IsCCCaptured == false && (doc.CuryCCCapturedAmt != decimal.Zero))
			{
				doc.CuryCCCapturedAmt = decimal.Zero;
				needUpdate = true;
			}
			return needUpdate;
		}

		public static IEnumerable<ExternalTransaction> GetSOInvoiceExternalTrans(PXGraph graph, ARInvoice currentInvoice)
		{
			foreach (ExternalTransaction tran in PXSelectReadonly<ExternalTransaction,
				Where<ExternalTransaction.refNbr, Equal<Current<ARInvoice.refNbr>>,
					And<ExternalTransaction.docType, Equal<Current<ARInvoice.docType>>>>,
				OrderBy<Desc<ExternalTransaction.transactionID>>>.SelectMultiBound(graph, new object[] { currentInvoice }))
			{
				yield return tran;
			}

			foreach (ExternalTransaction tran in PXSelectReadonly2<ExternalTransaction,
					InnerJoin<SOOrderShipment, On<SOOrderShipment.orderNbr, Equal<ExternalTransaction.origRefNbr>,
						And<SOOrderShipment.orderType, Equal<ExternalTransaction.origDocType>>>>,
					Where<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>,
						And<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>,
						And<ExternalTransaction.refNbr, IsNull>>>,
					OrderBy<Desc<CCProcTran.tranNbr>>>.SelectMultiBound(graph, new object[] { currentInvoice }))
			{
				yield return tran;
			}
		}

		private static string GetSyncFailedDescription(PXGraph graph, ExternalTransactionState state)
		{
			string ret = null;
			switch (state.ProcessingStatus)
			{
				case ProcessingStatus.AuthorizeFail: ret = PXMessages.LocalizeNoPrefix(Messages.CCAuthorizationSyncFailed); break;
				case ProcessingStatus.CaptureFail: ret = PXMessages.LocalizeNoPrefix(Messages.CCCaptureSyncFailed); break;
				case ProcessingStatus.CreditFail: ret = PXMessages.LocalizeNoPrefix(Messages.CCRefundSyncFailed); break;
				case ProcessingStatus.VoidFail: ret = PXMessages.LocalizeNoPrefix(Messages.CCVoidSyncFailed); break;
				case ProcessingStatus.Unknown: ret = PXMessages.LocalizeNoPrefix(Messages.CCSyncFailed); break;
			}
			ret = AppendPreviousDescriptionForSyncTran(graph, state, ret);
			return ret;
		}

		private static string GetPreviousStatusForSyncTran(string tranType)
		{
			string ret = null;
			switch (tranType)
			{
				case CCTranTypeCode.Authorize: ret = PXMessages.LocalizeNoPrefix(Messages.CCPreAuthorizedShort); break;
				case CCTranTypeCode.PriorAuthorizedCapture:
				case CCTranTypeCode.AuthorizeAndCapture:
				case CCTranTypeCode.CaptureOnly: ret = PXMessages.LocalizeNoPrefix(Messages.CCCaptured); break;
			}
			return ret;
		}

		private static string GetStatusByTranType(string tranType)
		{
			string ret = null;
			switch (tranType)
			{
				case CCTranTypeCode.Authorize: ret = PXMessages.LocalizeNoPrefix(Messages.CCPreAuthorized); break;
				case CCTranTypeCode.PriorAuthorizedCapture:
				case CCTranTypeCode.AuthorizeAndCapture:
				case CCTranTypeCode.CaptureOnly: ret = PXMessages.LocalizeNoPrefix(Messages.CCCaptured); break;
				case CCTranTypeCode.VoidTran: ret = PXMessages.LocalizeNoPrefix(Messages.CCVoided); break;
				case CCTranTypeCode.Credit: ret = PXMessages.LocalizeNoPrefix(Messages.CCRefunded); break;
			}
			return ret;
		}

		public static bool IsOrderSelfCaptured(PXGraph graph, SOOrder doc)
		{
			return PXSelectReadonly<ExternalTransaction,
				Where<ExternalTransaction.origDocType, Equal<Required<SOOrder.orderType>>, And<ExternalTransaction.origRefNbr, Equal<Required<SOOrder.orderNbr>>,
				And<ExternalTransaction.origDocType, NotEqual<ExternalTransaction.docType>, And<ExternalTransaction.origRefNbr, NotEqual<ExternalTransaction.refNbr>>>>>>
					.SelectWindowed(graph, 0, 1, doc.OrderType, doc.OrderNbr).Count == 0;
		}
	}
}
