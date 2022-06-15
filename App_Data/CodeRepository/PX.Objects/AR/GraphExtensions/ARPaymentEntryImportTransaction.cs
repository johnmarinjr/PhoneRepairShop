using PX.Common;
using PX.Data;
using PX.Objects;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.AR.Repositories;
using PX.Objects.AR.Standalone;
using PX.Objects.CA;
using PX.Objects.Common;
using PX.Objects.Extensions.PaymentTransaction;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AR.GraphExtensions
{
	public class ARPaymentEntryImportTransaction : PXGraphExtension<ARPaymentEntry>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.integratedCardProcessing>();

		public PXFilter<InputCCTransaction> apiInputCCTran;
		public PXAction<ARPayment> voidCardPayment;
		[PXUIField(Visible = false)]
		[PXButton]
		public virtual IEnumerable VoidCardPayment(PXAdapter adapter)
		{
			List<ARPayment> retList = new List<ARPayment>();
			if (Base.IsContractBasedAPI)
			{
				foreach (ARPayment payment in adapter.Get<ARPayment>())
				{
					InputCCTransaction inputTran = apiInputCCTran.Current;
					retList.Add(RecordTranAndVoidPayment(payment, inputTran));
				}
			}
			else
			{
				retList.AddRange(adapter.Get<ARPayment>());
			}
			return retList;
		}

		public PXAction<ARPayment> cardOperation;
		[PXUIField(Visible = false)]
		[PXButton]
		public virtual IEnumerable CardOperation(PXAdapter adapter)
		{
			List<ARPayment> retList = new List<ARPayment>();
			if (Base.IsContractBasedAPI)
			{
				foreach (ARPayment payment in adapter.Get<ARPayment>())
				{
					InputCCTransaction inputTran = apiInputCCTran.Current;
					retList.Add(RecordCardOperation(payment, inputTran));
				}
			}
			else
			{
				retList.AddRange(adapter.Get<ARPayment>());
			}
			return retList;
		}

		protected virtual void RowSelected(Events.RowSelected<ARPayment> e)
		{
			this.apiInputCCTran.Cache.AllowInsert = true;
			this.apiInputCCTran.Cache.AllowUpdate = true;
		}

		protected virtual void InputCCTransactionRowInserted(Events.RowInserted<InputCCTransaction> e) =>
			InsertImportedCreditCardTransaction(e.Row);

		protected virtual void InputCCTransactionRowUpdated(Events.RowUpdated<InputCCTransaction> e) =>
			InsertImportedCreditCardTransaction(e.Row);

		protected virtual ARPayment RecordTranAndVoidPayment(ARPayment doc, InputCCTransaction inputTran)
		{
			ARPayment ret = doc;
			if (doc == null || inputTran == null) return ret;

			ValidateBeforeVoiding(doc, inputTran);

			var storedTran = GetExtTrans().Where(i => i.TranNumber == inputTran.PCTranNumber).FirstOrDefault();
			if (storedTran != null)
			{
				var state = ExternalTranHelper.GetTransactionState(Base, storedTran);
				if (state.IsVoided || state.IsRefunded)
				{
					var adapter = ARPaymentEntry.CreateAdapterWithDummyView(Base, doc);
					Base.VoidCheck(adapter);
					return doc;
				}
			}

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(inputTran.TranType);
			if (tran == CCTranType.Unknown)
			{
				inputTran.NeedValidation = true;
			}
			if (inputTran.TranDate == null)
			{
				inputTran.TranDate = PXTimeZoneInfo.Now;
			}

			int? refInnerTranId = null;
			var activeState = GetActiveTransactionState();
			if (tran == CCTranType.Void && inputTran.OrigPCTranNumber != null && inputTran.NeedValidation == false)
			{
				refInnerTranId = activeState.ExternalTransaction?.TransactionID;
			}

			if (tran != CCTranType.Credit)
			{
				inputTran.OrigPCTranNumber = null;
			}

			TranRecordData recordData = FormatRecordData(inputTran);
			recordData.RefInnerTranId = refInnerTranId;
			ret = CreateVoidDocWithTran(doc, tran, recordData);
			return ret;
		}

		protected virtual ARPayment RecordCardOperation(ARPayment doc, InputCCTransaction inputTran)
		{
			ARPayment ret = doc;
			if (doc == null || inputTran == null) return ret;

			UpdateDocBeforeApiRecording(doc, inputTran);
			ValidateBeforeRecordingCardOperation(doc, inputTran);

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(inputTran.TranType);
			if (inputTran.TranDate == null)
			{
				inputTran.TranDate = PXTimeZoneInfo.Now;
			}

			int? refInnerTranId = null;
			if (inputTran.OrigPCTranNumber != null && inputTran.NeedValidation == false)
			{
				var activeState = GetActiveTransactionState();
				refInnerTranId = activeState.ExternalTransaction?.TransactionID;
			}

			inputTran.OrigPCTranNumber = null;

			TranRecordData recordData = FormatRecordData(inputTran);
			recordData.RefInnerTranId = refInnerTranId;
			recordData.Amount = inputTran.Amount;
			if (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment)
			{
				CCPaymentEntry paymentEntry = GetCCPaymentEntry(Base);
				var manager = GetAfterProcessingManager();
				manager.ReleaseDoc = true;
				paymentEntry.AfterProcessingManager = manager;
				var tranAdapter = new GenericExternalTransactionAdapter<ExternalTransaction>(Base.ExternalTran);
				if (tran == CCTranType.PriorAuthorizedCapture)
				{
					using (PXTransactionScope scope = new PXTransactionScope())
					{
						paymentEntry.RecordPriorAuthCapture(doc, recordData, tranAdapter);
						scope.Complete();
					}
				}
			}
			return doc;
		}

		protected virtual ARPayment CreateVoidDocWithTran(ARPayment doc, CCTranType tran, TranRecordData recordData)
		{
			CCPaymentEntry paymentEntry = GetCCPaymentEntry(Base);
			var manager = GetAfterProcessingManager();
			manager.ReleaseDoc = true;
			manager.Graph = Base;
			paymentEntry.AfterProcessingManager = manager;
			if (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment)
			{
				ARSetup setup = Base.arsetup.Current;
				setup.HoldEntry = false;
				var tranAdapter = new GenericExternalTransactionAdapter<ExternalTransaction>(Base.ExternalTran);
				if (tran == CCTranType.Void)
				{
					using (PXTransactionScope scope = new PXTransactionScope())
					{
						var adapter = ARPaymentEntry.CreateAdapterWithDummyView(Base, doc);
						if (doc.Released == false && doc.Voided == false)
						{
							paymentEntry.RecordVoid(doc, recordData, tranAdapter);
							Base.VoidCheck(adapter).RowCast<ARPayment>().FirstOrDefault();
						}
						else
						{
							var afterVoidDoc = Base.VoidCheck(adapter).RowCast<ARPayment>().FirstOrDefault();
							if (afterVoidDoc != null && afterVoidDoc.DocType == ARDocType.VoidPayment)
							{
								Base.Save.Press();
								recordData.AllowFillVoidRef = true;
								recordData.Amount = doc.CuryDocBal;
								paymentEntry.RecordVoid(afterVoidDoc, recordData, tranAdapter);
							}
						}
						scope.Complete();
					}
				}
				if (tran == CCTranType.Credit)
				{
					using (PXTransactionScope scope = new PXTransactionScope())
					{
						var adapter = ARPaymentEntry.CreateAdapterWithDummyView(Base, doc);
						Base.VoidCheck(adapter);
						Base.Save.Press();
						
						paymentEntry.RecordCCCredit(Base.Document.Current, recordData, tranAdapter);
						if (NeedRelease(Base.Document.Current))
						{
							PaymentTransactionGraph<ARPaymentEntry, ARPayment>.ReleaseARDocument(Base.Document.Current);
						}
						scope.Complete();
					}
				}
				if (tran == CCTranType.Unknown)
				{
					using (PXTransactionScope scope = new PXTransactionScope())
					{
						recordData.KeepNewTranDeactivated = true;
						paymentEntry.RecordUnknown(doc, recordData, tranAdapter);
						scope.Complete();
					}
				}
			}
			return doc;
		}

		protected virtual TranRecordData FormatRecordData(InputCCTransaction inputData)
		{
			var doc = Base.Document.Current;
			TranRecordData tranRecord = new TranRecordData();
			tranRecord.ExternalTranId = inputData.PCTranNumber;
			tranRecord.AuthCode = inputData.AuthNumber;
			tranRecord.ResponseText = Messages.ImportedExternalCCTransaction;
			tranRecord.Imported = true;
			tranRecord.CreateProfile = doc.SaveCard == true;
			tranRecord.NeedSync = inputData.NeedValidation.GetValueOrDefault();
			tranRecord.TransactionDate = inputData.TranDate;
			tranRecord.ProcessingCenterId = Base.Document.Current.ProcessingCenterID;
			tranRecord.ExtProfileId = inputData.ExtProfileId;
			tranRecord.ResponseText = Messages.ImportedExternalCCTransaction;
			tranRecord.TranStatus = CCTranStatusCode.Approved;
			tranRecord.RefExternalTranId = inputData.OrigPCTranNumber;
			tranRecord.CardType = CardType.GetCardTypeEnumByCode(inputData.CardType);
			tranRecord.ProcCenterCardTypeCode = inputData.CardType;
			return tranRecord;
		}

		protected virtual void SetSyncLock(ARPayment doc, InputCCTransaction inputData)
		{
			if (doc == null || inputData == null)
				return;

			bool docInserted = Base.Caches[typeof(ARPayment)].GetStatus(doc) == PXEntryStatus.Inserted;
			if (!docInserted) return;

			if (inputData?.TranType == null) return;

			int? pmInstanceId = doc.PMInstanceID;
			CCTranType tranType = TranTypeList.GetTranTypeByStrCode(inputData.TranType);
			if (doc.SaveCard == true && pmInstanceId >= 0 || doc.DocType == ARDocType.Refund)
			{
				doc.SaveCard = false;
			}

			if (doc.SaveCard == true || tranType == CCTranType.Unknown)
			{
				inputData.NeedValidation = true;
			}

			if (inputData.NeedValidation == false && CheckNeedValidationForSettledTran(doc, inputData))
			{
				inputData.NeedValidation = true;
			}

			if (doc.SyncLock != inputData.NeedValidation)
			{
				doc.SyncLock = inputData.NeedValidation;
				doc.SyncLockReason = (doc.SyncLock == true) ? ARPayment.syncLockReason.NeedValidation : null;

				Base.Document.Update(doc);
			}
		}

		protected virtual void ProcessDocWithTranOneScope(ARPayment doc, InputCCTransaction inputData)
		{
			bool docInserted = Base.Caches[typeof(ARPayment)].GetStatus(doc) == PXEntryStatus.Inserted;
			if (!docInserted) return;

			if (doc.CustomerID == null || Base.ExternalTran.Select().Count != 0 || inputData?.TranType == null) return;

			if (Base.arsetup.Current?.MigrationMode == true)
			{
				throw new PXException(Messages.MigrationModeIsActivated);
			}

			UpdateDocBeforeApiRecording(doc, inputData);
			var storedRefData = GetRefExternalTransactionWithPayment(inputData, doc.ProcessingCenterID);

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(inputData.TranType);
			if (inputData.TranDate == null)
			{
				inputData.TranDate = PXTimeZoneInfo.Now;
			}

			ValidateRecordedInfoBeforeDocCreation(doc, inputData, storedRefData);

			int? refInnerTranId = null;
			if (inputData.OrigPCTranNumber != null && storedRefData != null
				&& inputData.NeedValidation == false && tran == CCTranType.Void)
			{
				refInnerTranId = storedRefData.Item1?.TransactionID;
			}

			if (tran != CCTranType.Credit)
			{
				inputData.OrigPCTranNumber = null;
			}

			TranRecordData recordData = FormatRecordData(inputData);
			recordData.RefInnerTranId = refInnerTranId;
			recordData.NewDoc = true;
			if (tran == CCTranType.AuthorizeOnly)
			{
				recordData.ExpirationDate = inputData.ExpirationDate;
			}
			CCPaymentEntry paymentEntry = GetCCPaymentEntry(Base);
			paymentEntry.NeedPersistAfterRecord = false;
			var manager = GetAfterProcessingManager();
			manager.Graph = Base;
			paymentEntry.AfterProcessingManager = manager;
			if (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment)
			{
				if (tran == CCTranType.AuthorizeOnly)
				{
					paymentEntry.RecordAuthorization(doc, recordData);
				}
				else if (tran == CCTranType.PriorAuthorizedCapture)
				{
					paymentEntry.RecordPriorAuthCapture(doc, recordData);
				}
				else if (tran == CCTranType.AuthorizeAndCapture)
				{
					paymentEntry.RecordAuthCapture(doc, recordData);
				}
				else if (tran == CCTranType.Unknown)
				{
					paymentEntry.RecordUnknown(doc, recordData);
				}
			}
			if (doc.DocType == ARDocType.Refund)
			{
				if (tran == CCTranType.Credit)
				{
					paymentEntry.RecordCredit(doc, recordData);
				}
				else if (tran == CCTranType.Void || tran == CCTranType.Unknown)
				{
					recordData.AllowFillVoidRef = true;
					if (storedRefData != null)
					{
						SetVoidDocTypeVoidRefNbrDefaultByDoc();
					}
					if (tran == CCTranType.Void)
					{
						paymentEntry.RecordVoid(doc, recordData);
					}
					else
					{
						paymentEntry.RecordUnknown(doc, recordData);
					}
				}
			}
		}

		protected virtual void ValidateRecordedInfoBeforeDocCreation(ARPayment doc, InputCCTransaction info, Tuple<ExternalTransaction, ARPayment> refExtTran)
		{
			ValidateDocBeforeApiRecording(doc, info);
			CommonRecordValidation(doc, info);

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(info.TranType);

			if (info.NeedValidation == true && (tran == CCTranType.Void || tran == CCTranType.Credit))
			{
				throw new PXException(Messages.ERR_NeedValidationModeIsNotSupported, info.PCTranNumber);
			}

			if (doc.DocType == ARDocType.Refund && tran != CCTranType.Credit && tran != CCTranType.Void
				&& tran != CCTranType.Unknown)
			{
				throw new PXException(Messages.ERR_IncorrectTranType, info.PCTranNumber);
			}
			if ((doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment)
				&& (tran == CCTranType.Credit || tran == CCTranType.Void))
			{
				throw new PXException(Messages.ERR_IncorrectTranType, info.PCTranNumber);
			}

			if (refExtTran != null)
			{
				var storedDoc = refExtTran.Item2;
				ARPayment voidDoc = GetVoidedDocForOrigPayment(storedDoc); 
				if (voidDoc != null)
				{
					string storedPmtName = TranValidationHelper.GetDocumentName(storedDoc.DocType);
					throw new PXException(Messages.ERR_VoidedTranRecordOtherDoc, info.PCTranNumber, storedDoc.RefNbr, storedPmtName);
				}
			}

			if (tran == CCTranType.AuthorizeOnly && info.ExpirationDate != null && info.ExpirationDate <= info.TranDate)
			{
				throw new PXException(Messages.ERR_IncorrectExpirationDate);
			}

			if (doc.DocType == ARDocType.Refund && (tran == CCTranType.Void || tran == CCTranType.Unknown))
			{
				if (refExtTran == null)
				{
					if (tran == CCTranType.Void && info.NeedValidation == false)
					{
						throw new PXException(Messages.ERR_CCNoTransactionToVoid);
					}
				}
				else
				{
					var storedExtTran = refExtTran.Item1;
					var storedPmt = refExtTran.Item2;
					TranValidationHelper.CheckNewAndStoredPayment(doc, storedPmt, storedExtTran);
					if (tran == CCTranType.Void && doc.CuryDocBal != storedExtTran.Amount)
					{
						throw new PXException(Messages.ERR_IncorrectTranAmount, info.PCTranNumber);
					}
					var storedState = ExternalTranHelper.GetTransactionState(Base, storedExtTran);
					if (storedState.IsRefunded)
					{
						throw new PXException(Messages.ERR_IncorrectRefundTranType, storedExtTran.TranNumber);
					}
					if (storedState.IsVoided)
					{
						string storedPmtName = TranValidationHelper.GetDocumentName(storedPmt.DocType);
						throw new PXException(Messages.ERR_VoidedTranRecordOtherDoc, storedExtTran.TranNumber, storedPmt.RefNbr, storedPmtName);
					}
				}

				foreach (ARRegisterAlias item in Base.Adjustments.Select().RowCast<ARRegisterAlias>())
				{
					ARReleaseProcess.EnsureNoUnreleasedVoidPaymentExists(Base, item, Common.Messages.ActionRefunded);
				}
			}
		}

		protected virtual void CommonRecordValidation(ARPayment doc, InputCCTransaction info)
		{
			if (string.IsNullOrEmpty(info.PCTranNumber))
			{
				throw new PXException(ErrorMessages.FieldIsEmpty, nameof(InputCCTransaction.PCTranNumber));
			}
			if (string.IsNullOrEmpty(info.TranType))
			{
				throw new PXException(ErrorMessages.FieldIsEmpty, nameof(InputCCTransaction.TranType));
			}

			var res = TranTypeList.GetCommonInputTypes().FirstOrDefault(i => i.Item1 == info.TranType);
			if (res == null)
			{
				throw new PXException(Messages.ERR_IncorrectTranType, info.PCTranNumber);
			}
		}

		protected virtual bool CheckNeedValidationForSettledTran(ARPayment doc, InputCCTransaction inputData)
		{
			bool ret = false;
			if (inputData.TranType == TranTypeList.AUTCode)
			{
				string procCenterID = null;
				if (doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
				{
					CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
					if (cpm?.CCProcessingCenterID != null)
					{
						procCenterID = cpm.CCProcessingCenterID;
					}
				}
				else
				{
					procCenterID = doc.ProcessingCenterID;
				}

				if (procCenterID != null)
				{
					var batchTran = GetCCBatchTransaction(procCenterID, inputData.PCTranNumber);
					if (batchTran?.SettlementStatus == CCBatchTranSettlementStatusCode.SettledSuccessfully)
					{
						ret = true;
					}
				}
			}
			return ret;
		}

		protected virtual void ValidateBeforeRecordingCardOperation(ARPayment doc, InputCCTransaction inputTran)
		{
			string docTypeRefNbr = doc.DocType + doc.RefNbr;
			var status = Base.Caches[typeof(ARPayment)].GetStatus(doc);
			if (status == PXEntryStatus.Inserted)
			{
				throw new PXException(Messages.DocumentNotFoundGenericErr);
			}

			CommonRecordValidation(doc, inputTran);

			if (inputTran.NeedValidation == true)
			{
				throw new PXException(Messages.ERR_NeedValidationModeIsNotSupported, inputTran.PCTranNumber);
			}

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(inputTran.TranType);
			if (tran != CCTranType.PriorAuthorizedCapture)
			{
				throw new PXException(Messages.ERR_IncorrectTranType, inputTran.PCTranNumber);
			}

			if (doc.Released == true)
			{
				string docName = TranValidationHelper.GetDocumentName(doc.DocType);
				throw new PXException(Messages.TranCanNotBeImportedPaymentIsReleased, doc.RefNbr, docName);
			}

			ExternalTransactionState activeState = GetActiveTransactionState();
			if (!activeState.IsPreAuthorized)
			{
				throw new PXException(Messages.ERR_CCTransactionMustBeAuthorizedBeforeCapturing);
			}
			if (activeState.IsCaptured)
			{
				throw new PXException(Messages.ERR_CCAuthorizedPaymentAlreadyCaptured);
			}
			if (activeState.IsRefunded)
			{
				throw new PXException(Messages.ERR_CCPaymentIsAlreadyRefunded);
			}
			var activeTran = activeState.ExternalTransaction;
			if (activeTran != null && inputTran.NeedValidation == false)
			{
				if (inputTran.OrigPCTranNumber == null && inputTran.PCTranNumber != activeTran.TranNumber)
				{
					string capLabel = CCProcessingHelper.GetTransactionTypeName(CCTranType.PriorAuthorizedCapture);
					string authLabel = CCProcessingHelper.GetTransactionTypeName(CCTranType.AuthorizeOnly);
					throw new PXException(Messages.ERR_TranNotLinked, inputTran.PCTranNumber, capLabel,
						activeTran.TranNumber, authLabel);
				}
				if (inputTran.OrigPCTranNumber != null && inputTran.OrigPCTranNumber != activeTran.TranNumber)
				{
					throw new PXException(Messages.ERR_CCProcessingCouldNotFindTransaction, inputTran.OrigPCTranNumber, docTypeRefNbr);
				}
			}

			if (inputTran.Amount != null)
			{ 
				if(inputTran.Amount <= 0)
				{
					throw new PXException(Messages.ERR_CCAmountMustBePositive);
				}
				if (inputTran.Amount > activeTran.Amount)
				{
					throw new PXException(Messages.ERR_TranAmtIsGreaterPaymentAmt, inputTran.PCTranNumber);
				}
			}

			CheckInputTranDate(activeTran, inputTran);
			CheckProcCenterSupportTransactionValidation(doc.ProcessingCenterID, inputTran);
		}

		protected virtual void ValidateBeforeVoiding(ARPayment doc, InputCCTransaction info)
		{
			string docTypeRefNbr = doc.DocType + doc.RefNbr;
			var docLabel = TranValidationHelper.GetDocumentName(doc.DocType);
			var status = Base.Caches[typeof(ARPayment)].GetStatus(doc);
			if (status == PXEntryStatus.Inserted)
			{
				throw new PXException(Messages.DocumentNotFoundGenericErr);
			}

			CommonRecordValidation(doc, info);

			ARPayment voidDoc = GetVoidedDocForOrigPayment(doc);
			if (voidDoc != null)
			{
				throw new PXException(Messages.ERR_VoidedTranRecordOtherDoc, info.PCTranNumber, doc.RefNbr, docLabel);
			}

			CCTranType tran = TranTypeList.GetTranTypeByStrCode(info.TranType);

			if (info.NeedValidation == true && (tran == CCTranType.Void || tran == CCTranType.Credit))
			{
				throw new PXException(Messages.ERR_NeedValidationModeIsNotSupported, info.PCTranNumber);
			}

			var activeTranState = GetActiveTransactionState();
			var activeTran = activeTranState.ExternalTransaction;

			if (tran == CCTranType.AuthorizeAndCapture || tran == CCTranType.AuthorizeOnly
				|| tran == CCTranType.CaptureOnly || tran == CCTranType.PriorAuthorizedCapture)
			{
				throw new PXException(Messages.ERR_IncorrectTranType, info.PCTranNumber);
			}

			if (doc.Released == false && (activeTranState.IsCaptured || tran == CCTranType.Credit))
			{
				throw new PXException(Messages.PaymentIsNotReleased, docTypeRefNbr);
			}

			if (activeTran != null && tran == CCTranType.Credit
				&& info.OrigPCTranNumber != null && activeTran.TranNumber != info.OrigPCTranNumber)
			{
				throw new PXException(Messages.ERR_RefundTranNotLinkedOrigTran, info.PCTranNumber, activeTran.TranNumber);
			}

			if (activeTran != null && tran == CCTranType.Void && info.NeedValidation == false)
			{
				if (info.OrigPCTranNumber == null && info.PCTranNumber != activeTran.TranNumber)
				{
					var voidLabel = CCProcessingHelper.GetTransactionTypeName(CCTranType.Void);
					var activeTranLabel = activeTranState.IsPreAuthorized
						? CCProcessingHelper.GetTransactionTypeName(CCTranType.AuthorizeOnly)
						: CCProcessingHelper.GetTransactionTypeName(CCTranType.AuthorizeAndCapture);
					throw new PXException(Messages.ERR_TranNotLinked, info.PCTranNumber, voidLabel, activeTran.TranNumber, activeTranLabel);
				}
				if (info.OrigPCTranNumber != null && info.OrigPCTranNumber != activeTran.TranNumber)
				{
					throw new PXException(Messages.ERR_CCProcessingCouldNotFindTransaction, info.OrigPCTranNumber, docTypeRefNbr);
				}
			}

			if ((activeTran == null || activeTranState.IsPreAuthorized) && tran == CCTranType.Credit)
			{
				throw new PXException(Messages.ERR_CCNoTransactionToRefund, info.PCTranNumber, doc.RefNbr, docLabel);
			}

			if (activeTran == null && tran == CCTranType.Unknown)
			{
				throw new PXException(Messages.ERR_CCNoSuccessfulTransaction, info.PCTranNumber, doc.RefNbr, docLabel);
			}

			var hasAlreadyImportedTran = GetExtTrans().Where(i => i.TranNumber == info.PCTranNumber && i.NeedSync == true).FirstOrDefault();
			if (hasAlreadyImportedTran != null)
			{
				throw new PXException(Messages.ERR_TranWasAlreadyImported, info.PCTranNumber, doc.RefNbr, docLabel);
			}

			CheckInputTranDate(activeTran, info);
			CheckProcCenterSupportTransactionValidation(doc.ProcessingCenterID, info);
		}

		protected virtual CCPaymentEntry GetCCPaymentEntry(PXGraph graph)
		{
			var paymentEntry = new CCPaymentEntry(graph);
			return paymentEntry;
		}

		protected virtual ARPaymentAfterProcessingManager GetAfterProcessingManager()
		{
			return new ARPaymentAfterProcessingManager();
		}

		protected virtual ICCPaymentProcessingRepository GetPaymentRepository()
		{
			var	repo = new CCPaymentProcessingRepository(Base);
			return repo;
		}

		protected virtual ExternalTransactionState GetActiveTransactionState()
		{
			var trans = GetExtTrans();
			var ret = ExternalTranHelper.GetActiveTransactionState(Base, trans);
			return ret;
		}

		protected virtual IEnumerable<IExternalTransaction> GetExtTrans()
		{
			if (Base.ExternalTran == null)
				yield break;
			foreach (ExternalTransaction tran in Base.ExternalTran.Select().RowCast<ExternalTransaction>())
			{
				yield return tran;
			}
		}

		protected CCProcessingCenter GetProcessingCenterById(string id)
		{
			CCProcessingCenter procCenter = PXSelect<CCProcessingCenter,
				Where<CCProcessingCenter.processingCenterID, Equal<Required<CCProcessingCenter.processingCenterID>>>>.Select(Base, id);
			return procCenter;
		}

		private bool NeedRelease(ARPayment doc)
		{
			return doc.Released == false && Base.arsetup.Current.IntegratedCCProcessing == true;
		}

		private void CheckInputTranDate(IExternalTransaction activeTran, InputCCTransaction inputTran)
		{
			if (activeTran != null && inputTran.TranDate != null && activeTran.LastActivityDate > inputTran.TranDate)
			{
				var repo = GetPaymentRepository();
				var procTrans = repo.GetCCProcTranByTranID(activeTran.TransactionID);
				var lastSuccessful = CCProcTranHelper.FindCCLastSuccessfulTran(procTrans);
				if (lastSuccessful != null)
				{
					var lastCCprocTran = Base.ccProcTran.Select().RowCast<CCProcTran>().FirstOrDefault(i => i.TranNbr == lastSuccessful.TranNbr);
					if (lastCCprocTran != null && lastCCprocTran.EndTime.Value.Date > inputTran.TranDate.Value.Date)
					{
						throw new PXException(Messages.ERR_NotValidImportedTranDate);
					}
				}
			}
		}

		private void InsertImportedCreditCardTransaction(InputCCTransaction inputTransaction)
		{
			ARPayment payment = Base.Document.Current;
			if (payment == null || inputTransaction == null) return;
			SetSyncLock(payment, inputTransaction);
			if (Base.IsContractBasedAPI)
			{
				ProcessDocWithTranOneScope(payment, inputTransaction);
			}
		}

		private CCBatchTransaction GetCCBatchTransaction(string procCenterID, string pcTranNumber)
		{
			var query = new PXSelectJoin<CCBatchTransaction,
				InnerJoin<CCBatch, On<CCBatch.batchID, Equal<CCBatchTransaction.batchID>>>,
				Where<CCBatch.processingCenterID, Equal<Required<CCBatch.processingCenterID>>,
				And<CCBatchTransaction.pCTranNumber, Equal<Required<CCBatchTransaction.pCTranNumber>>,
				And<CCBatchTransaction.processingStatus, Equal<CCBatchTranProcessingStatusCode.missing>>>>>(Base);

			return query.SelectSingle(procCenterID, pcTranNumber);
		}

		private CustomerPaymentMethod GetCustomerPaymentMethodById(int? pmInstanceId)
		{
			CustomerPaymentMethodRepository repo = new CustomerPaymentMethodRepository(Base);
			CustomerPaymentMethod cpm = repo.GetCustomerPaymentMethod(pmInstanceId);
			return cpm;
		}

		private ARPayment GetVoidedDocForOrigPayment(ARPayment doc)
		{
			var query = new PXSelect<ARPayment, Where<ARPayment.origDocType,
					Equal<Required<ARPayment.origDocType>>, And<ARPayment.origRefNbr, Equal<Required<ARPayment.origRefNbr>>>>>(Base);
			ARPayment voidDoc = query.SelectSingle(doc.DocType, doc.RefNbr);
			return voidDoc;
		}

		private void UpdateDocBeforeApiRecording(ARPayment doc, InputCCTransaction inputData)
		{
			bool needUpdate = false;
			if (doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
			{
				CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
				if (cpm?.CCProcessingCenterID != null)
				{
					doc.ProcessingCenterID = cpm.CCProcessingCenterID;
					needUpdate = true;
				}
			}
			if (doc.Hold == true)
			{
				doc.Hold = false;
				needUpdate = true;
			}
			if (needUpdate)
			{
				doc = Base.Document.Update(doc);
			}
			if (!string.IsNullOrEmpty(inputData?.OrigPCTranNumber) && doc.DocType == ARDocType.Refund)
			{
				UpdateOrigTranNumber(doc, inputData);
			}
		}

		private void ValidateDocBeforeApiRecording(ARPayment doc, InputCCTransaction inputData)
		{
			PaymentMethod pm = Base.paymentmethod.Current;
			string procCenterId = doc.ProcessingCenterID;
			if (pm != null && pm.PaymentType == PaymentMethodType.CreditCard
				&& string.IsNullOrEmpty(procCenterId))
			{
				throw new PXException(ErrorMessages.FieldIsEmpty, nameof(ARPayment.processingCenterID));
			}
			if (pm != null && pm.PaymentType != PaymentMethodType.CreditCard && inputData?.TranType != null)
			{
				throw new PXException(Messages.ERR_PaymentMethodDoesNotSupportCrediCards, pm.PaymentMethodID);
			}
			CCProcessingCenter procCenter = GetProcessingCenterById(procCenterId);

			if (procCenter == null)
			{
				throw new PXException(Messages.ERR_CCProcessingCenterNotFound);
			}

			CheckProcCenterSupportTransactionValidation(procCenter, inputData);

			if (procCenter.AllowSaveProfile == false && doc.SaveCard == true)
			{
				throw new PXException(Messages.SavingCardsNotAllowedForProcCenter, procCenterId);
			}
		}

		private void UpdateOrigTranNumber(ARPayment doc, InputCCTransaction inputData)
		{
			string savedProcCenter = doc.ProcessingCenterID;
			int? savedCashAccount = doc.CashAccountID;
			doc.RefTranExtNbr = inputData.OrigPCTranNumber;
			doc = Base.Document.Update(doc);
			bool needUpdate = false;
			if (doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile)
			{
				if (savedProcCenter != doc.ProcessingCenterID)
				{
					doc.ProcessingCenterID = savedProcCenter;
					needUpdate = true;
				}
			}
			else
			{
				CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
				if (cpm?.CCProcessingCenterID != null && cpm.CCProcessingCenterID != doc.ProcessingCenterID)
				{
					doc.ProcessingCenterID = cpm.CCProcessingCenterID;
					needUpdate = true;
				}
			}
			if (savedCashAccount != doc.CashAccountID)
			{
				doc.CashAccountID = savedCashAccount;
				needUpdate = true;
			}
			if (needUpdate)
			{
				Base.Document.Update(doc);
			}
		}

		private void CheckProcCenterSupportTransactionValidation(string procCenterId, InputCCTransaction info)
		{
			CCProcessingCenter procCenter = GetProcessingCenterById(procCenterId);
			if (procCenter == null)
			{
				throw new PXException(Messages.ERR_CCProcessingCenterNotFound);
			}
			CheckProcCenterSupportTransactionValidation(procCenter, info);
		}

		private void CheckProcCenterSupportTransactionValidation(CCProcessingCenter procCenter, InputCCTransaction info)
		{
			if (info.NeedValidation == true || info.TranType == TranTypeList.UKNCode)
			{
				if (!CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.TransactionGetter))
				{
					throw new PXException(Messages.ERR_GettingInformationByTranIdNotSupported, procCenter.ProcessingCenterID);
				}
			}
		}

		private Tuple<ExternalTransaction, ARPayment> GetRefExternalTransactionWithPayment(InputCCTransaction inputTran, string procCenterId)
		{
			CCTranType tran = TranTypeList.GetTranTypeByStrCode(inputTran.TranType);
			var repo = GetPaymentRepository();
			Tuple<ExternalTransaction, ARPayment> ret;
			if (inputTran.NeedValidation == false && inputTran.OrigPCTranNumber != null)
			{
				ret = repo.GetExternalTransactionWithPayment(inputTran.OrigPCTranNumber, procCenterId);
			}
			else
			{
				ret = repo.GetExternalTransactionWithPayment(inputTran.PCTranNumber, procCenterId);
			}
			return ret;
		}

		private void SetVoidDocTypeVoidRefNbrDefaultByDoc()
		{
			PXDBDefaultAttribute.SetSourceType<ExternalTransaction.voidDocType>(Base.Caches[typeof(ExternalTransaction)], typeof(ARRegister.docType));
			PXDBDefaultAttribute.SetSourceType<ExternalTransaction.voidRefNbr>(Base.Caches[typeof(ExternalTransaction)], typeof(ARRegister.refNbr));
		}
	}
}
