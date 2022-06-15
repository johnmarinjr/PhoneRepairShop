using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Api.Services;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing.Wrappers;
using PX.Objects.AR.CCPaymentProcessing;
using V2 = PX.CCProcessingBase.Interfaces.V2;
using Newtonsoft.Json.Linq;
using PX.CCProcessingBase;
using System.Text.RegularExpressions;
using System;
using PX.Common;
using PX.Objects.Common;
using PX.Objects.CN.Common.Extensions;
using CCTranType = PX.Objects.AR.CCPaymentProcessing.Common.CCTranType;
using System.Web;

namespace PX.Objects.Extensions.PaymentTransaction
{
	public abstract class PaymentTransactionAcceptFormGraph<TGraph, TPrimary> : PaymentTransactionGraph<TGraph, TPrimary>
		where TGraph : PXGraph, new()
		where TPrimary : class, IBqlTable, new()
	{
		protected bool UseAcceptHostedForm;
		protected Guid? DocNoteId;
		protected bool EnableMobileMode;
		protected bool CheckSyncLockOnPersist;

		private string checkedProcessingCenter = null;
		private bool checkedProcessingCenterResult;
		private RetryPolicy<IEnumerable<V2.TransactionData>> retryUnsettledTran;

		[InjectDependency]
		public ICompanyService CompanyService { get; set; }

		[PXUIField(DisplayName = "Authorize", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
		restrictInMigrationMode: true,
		restrictForRegularDocumentInMigrationMode: true,
		restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public override IEnumerable AuthorizeCCPayment(PXAdapter adapter)
		{
			IEnumerable ret;
			string methodName = GetClassMethodName();
			PXTrace.WriteInformation($"{methodName} started.");
			ShowProcessingWarnIfLock(adapter);
			CheckProcCenterDisabled();
			if (!UseAcceptHostedForm)
			{
				ret = base.AuthorizeCCPayment(adapter);
			}
			else
			{
				if (!IsSupportPaymentHostedForm(SelectedProcessingCenter))
				{
					throw new PXException(AR.Messages.ERR_ProcessingCenterNotSupportAcceptPaymentForm);
				}
				ret = AuthorizeThroughForm(adapter);
			}
			return ret;
		}

		[PXUIField(DisplayName = "Capture", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: true,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public override IEnumerable CaptureCCPayment(PXAdapter adapter)
		{
			IEnumerable ret;
			string methodName = GetClassMethodName();
			PXTrace.WriteInformation($"{methodName} started.");
			ShowProcessingWarnIfLock(adapter);
			CheckProcCenterDisabled();
			if (!UseAcceptHostedForm)
			{
				ret = base.CaptureCCPayment(adapter);
			}
			else
			{
				if (!IsSupportPaymentHostedForm(SelectedProcessingCenter))
				{
					throw new PXException(AR.Messages.ERR_ProcessingCenterNotSupportAcceptPaymentForm);
				}
				ret = CaptureThroughForm(adapter);
			}
			return ret;
		}

		[PXUIField(DisplayName = "Validate Card Payment", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Visible = true)]
		[PXButton]
		public override IEnumerable ValidateCCPayment(PXAdapter adapter)
		{
			string methodName = GetClassMethodName();
			PXTrace.WriteInformation($"{methodName} started.");
			ShowProcessingWarnIfLock(adapter);
			CheckProcCenterDisabled();
			Base.Actions.PressCancel();

			var list = adapter.Get<TPrimary>().ToList();
			PXLongOperation.StartOperation(Base, delegate
			{
				var processingGraph = GetProcessingGraph();
				foreach (TPrimary doc in list)
				{
					var currDoc = SetCurrentDocument(processingGraph, doc);
					var ext = GetPaymentTransactionAcceptFormExt(processingGraph);
					ext.DoValidateCCPayment(currDoc);
				}
			});
			return list;
		}

		protected override TPrimary DoValidateCCPayment(TPrimary doc)
		{
			if (!RunPendingOperations(doc))
			{
				CheckPaymentTransaction(doc);
				IExternalTransaction storedTran = GetExtTrans().FirstOrDefault();
				bool needSyncUnsettled = false;
				if (storedTran != null)
				{
					bool synced = TrySyncByTranNumber(doc, storedTran);
					if (!synced && !ExternalTranHelper.GetTransactionState(Base, storedTran).IsActive)
					{
						needSyncUnsettled = true;
					}
				}
				else
				{
					needSyncUnsettled = true;
				}

				if (needSyncUnsettled)
				{
					ICCPayment pDoc = GetPaymentDoc(doc);
					IEnumerable<V2.TransactionData> trans = GetPaymentProcessing().GetUnsettledTransactions(SelectedProcessingCenter);
					IEnumerable<string> result = PrepareTransactionIds(GetTransByDoc(pDoc, trans));

					SyncPaymentTransactionById(doc, result);
				}
			}
			if (LockExists(doc))
			{
				RemoveSyncLock(doc);
			}
			RestoreDocStateByTransactionIfNeeded(doc);
			return doc;
		}

		private bool TrySyncByTranNumber(TPrimary doc, IExternalTransaction extTran)
		{
			if (string.IsNullOrEmpty(extTran.TranNumber))
			{
				return false;
			}

			try
			{
				var procTran = GetPaymentTranDetails().FirstOrDefault(i => i.TransactionID == extTran.TransactionID);
				if (procTran?.ProcStatus == CCProcStatus.Opened && procTran?.Imported == false)
				{
					SyncPaymentTransactionById(doc, extTran.TranNumber.AsSingleEnumerable());
					return true;
				}
			}
			catch (PXException ex) when (ex.InnerException is V2.CCProcessingException innerEx
					&& innerEx?.Reason == V2.CCProcessingException.ExceptionReason.TranNotFound)
			{ }

			return false;
		}

		public PXAction<TPrimary> syncPaymentTransaction;
		[PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXButton]
		public virtual IEnumerable SyncPaymentTransaction(PXAdapter adapter)
		{
			string methodName = GetClassMethodName();
			PXTrace.WriteInformation($"{methodName} started.");

			bool cancelled = false;
			string tranResponseStr = null;

			string commandArguments = adapter.CommandArguments;
			if (!string.IsNullOrEmpty(commandArguments))
			{
				if (commandArguments == "__CLOSECCHFORM")
					cancelled = true;
				else
					tranResponseStr = commandArguments;
			}
			else
			{
				var cancelStr = GetStringFromContext("__CLOSECCHFORM");
				if (bool.TryParse(cancelStr, out bool isCancel) && isCancel)
					cancelled = true;
				else
					tranResponseStr = GetStringFromContext("__TRANID");
			}

			if (cancelled)
			{
				var lastTran = GetExtTrans().FirstOrDefault();
				if (lastTran != null)
				{
					RemoveLockAndFinalizeTran(lastTran);
				}
				return adapter.Get();
			}

			if (string.IsNullOrEmpty(tranResponseStr))
			{
				throw new PXException(AR.Messages.ERR_AcceptHostedFormResponseNotFound);
			}

			string tranId;
			TPrimary doc = adapter.Get<TPrimary>().First<TPrimary>();
			CCProcessingCenter procCenter = GetProcessingCenterById(SelectedProcessingCenter);
			if (CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.PaymentForm))
			{
				ICCPayment pDoc = GetPaymentDoc(doc);
				var response = GetPaymentProcessing().ProcessPaymentFormResponse(pDoc, SelectedProcessingCenter, SelectedBAccount, tranResponseStr);
				tranId = response?.TranID;
			}
			else
			{
				var response = GetPaymentProcessing().ParsePaymentFormResponse(tranResponseStr, SelectedProcessingCenter);
				tranId = response?.TranID;
			}

			if (string.IsNullOrEmpty(tranId))
			{
				throw new PXException(AR.Messages.ERR_CouldNotGetTransactionIdFromResponse);
			}

			PXLongOperation.StartOperation(Base, () => {
				var processingGraph = GetProcessingGraph();
				var currDoc = SetCurrentDocument(processingGraph, doc);
				var ext = GetPaymentTransactionAcceptFormExt(processingGraph);
				ext.SyncPaymentTransactionById(currDoc, new List<string>() { tranId });
			});

			return adapter.Get();
		}

		protected virtual string GetStringFromContext(string key)
		{
			var request = System.Web.HttpContext.Current?.Request;
			return request != null
				? request.Form.Get(key)
				: PXContext.GetSlot<string>(GetContextFullKey(key));
		}

		protected virtual string GetContextFullKey(string key)
			=> $"{nameof(PaymentTransactionAcceptFormGraph<TGraph, TPrimary>)}${key}";

		public virtual void SetContextString(string key, string value)
		{
			PXContext.SetSlot(GetContextFullKey(key), value);
		}

		private IEnumerable AuthorizeThroughForm(PXAdapter adapter)
		{
			List<TPrimary> list = new List<TPrimary>();
			foreach (TPrimary doc in adapter.Get<TPrimary>())
			{
				CheckDocumentUpdatedInDb(doc);
				ICCPayment pDoc = GetPaymentDoc(doc);
				if (pDoc.CuryDocBal <= 0)
				{
					throw new PXException(AR.Messages.ERR_CCAmountMustBePositive);
				}

				var extTran = StartCreditCardTransaction(pDoc, CCTranType.AuthorizeOnly);

				if (pDoc.Released == false)
				{
					Base.Actions.PressSave();
					BeforeAuthorizePayment(doc);
				}
				list.Add(doc);

				if (EnableMobileMode)
				{
					CheckPaymentTransaction(doc);
					ProcessMobilePayment(doc, extTran, V2.CCTranType.AuthorizeOnly);
				}
				else
				{
					PXBaseRedirectException redirectEx = null;
					try
					{
						CCProcessingCenter procCenter = GetProcessingCenterById(SelectedProcessingCenter);
						if (CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.PaymentForm))
						{
							var options = GetPaymentProcessing().PreparePaymentForm(pDoc, SelectedProcessingCenter, SelectedBAccount, NeedSaveCard(), V2.CCTranType.AuthorizeOnly, extTran.NoteID);
							extTran = UpdateExtTran(extTran, options);
							throw new PXPluginRedirectException<PXPluginRedirectOptions>(options);
						}
						else
						{
							GetPaymentProcessing().ShowAcceptPaymentForm(V2.CCTranType.AuthorizeOnly, pDoc, SelectedProcessingCenter, SelectedBAccount, extTran.NoteID);
						}
					}
					catch (PXBaseRedirectException ex)
					{
						redirectEx = ex;
					}

					PXLongOperation.StartOperation(Base, () =>
					{
						CheckPaymentTransaction(doc);
						if (pDoc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile && !TranHeldForReview() && redirectEx != null)
						{
							SetSyncLock(doc);
							throw redirectEx;
						}
						RemoveSyncLock(doc);
					});
				}
			}
			return list;
		}
		private IEnumerable CaptureThroughForm(PXAdapter adapter)
		{
			List<TPrimary> list = new List<TPrimary>();
			foreach (TPrimary doc in adapter.Get<TPrimary>())
			{
				CheckDocumentUpdatedInDb(doc);
				ICCPayment pDoc = GetPaymentDoc(doc);
				if (pDoc.CuryDocBal <= 0)
				{
					throw new PXException(AR.Messages.ERR_CCAmountMustBePositive);
				}

				var extTran = StartCreditCardTransaction(pDoc, CCTranType.AuthorizeAndCapture);

				if (pDoc.Released == false)
				{
					Base.Actions.PressSave();
					BeforeCapturePayment(doc);
				}
				list.Add(doc);

				if (EnableMobileMode)
				{
					CheckPaymentTransaction(doc);
					if (FindPreAuthorizing())
						continue;
					ProcessMobilePayment(doc, extTran, V2.CCTranType.AuthorizeAndCapture);
				}
				else
				{
					PXBaseRedirectException redirectEx = null;
					try
					{
						CCProcessingCenter procCenter = GetProcessingCenterById(SelectedProcessingCenter);
						if (CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.PaymentForm))
						{
							var options = GetPaymentProcessing().PreparePaymentForm(pDoc, SelectedProcessingCenter, SelectedBAccount, NeedSaveCard(), V2.CCTranType.AuthorizeAndCapture, extTran.NoteID);
							extTran = UpdateExtTran(extTran, options);
							throw new PXPluginRedirectException<PXPluginRedirectOptions>(options);
						}
						else
						{
							GetPaymentProcessing().ShowAcceptPaymentForm(V2.CCTranType.AuthorizeAndCapture, pDoc, SelectedProcessingCenter, SelectedBAccount, extTran.NoteID);
						}
					}
					catch (PXPaymentRedirectException ex)
					{
						redirectEx = ex;
					}

					PXLongOperation.StartOperation(Base, () =>
					{
						CheckPaymentTransaction(doc);
						if (pDoc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile && !TranHeldForReview() && !FindPreAuthorizing() && redirectEx != null)
						{
							SetSyncLock(doc);
							throw redirectEx;
						}
						RemoveSyncLock(doc);
					});
				}
			}
			return list;
		}
		private void ProcessMobilePayment(TPrimary doc, ExternalTransactionDetail extTran, V2.CCTranType tranType)
		{
			ICCPayment pDoc = GetPaymentDoc(doc);
			if (pDoc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile && !TranHeldForReview())
			{
				string redirectUrl = null;
				PXPluginRedirectOptions options;
				SetSyncLock(doc);
				CCProcessingCenter procCenter = GetProcessingCenterById(SelectedProcessingCenter);
				Dictionary<string, string> appendParams;
				if (CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.PaymentForm))
				{
					options = GetPaymentProcessing().PreparePaymentForm(pDoc, SelectedProcessingCenter, SelectedBAccount, NeedSaveCard(), tranType, extTran.NoteID);
					extTran = UpdateExtTran(extTran, options);
					appendParams = CreateMobileDict(pDoc.DocType, pDoc.RefNbr, tranType, GetCompanyName(), extTran.NoteID);
					appendParams.Add("ProcCenterId", SelectedProcessingCenter);
					redirectUrl = V2.CCServiceEndpointHelper.GetPaymentUrl("Payment.aspx", options, appendParams);
				}
				else
				{
					appendParams = CreateMobileDict(pDoc.DocType, pDoc.RefNbr, tranType, GetCompanyName(), extTran.NoteID);
					redirectUrl = V2.CCServiceEndpointHelper.GetUrl(V2.CCServiceAction.GetAcceptPaymentForm, appendParams);
				}
				if (redirectUrl == null)
					throw new PXException(AR.Messages.ERR_CCProcessingCouldNotGenerateRedirectUrl);
				PXTrace.WriteInformation("Redirect to endpoint. Url: {redirectUrl}", redirectUrl);
				throw new PXRedirectToUrlException(redirectUrl, PXBaseRedirectException.WindowMode.New, true, "Redirect:" + redirectUrl);
			}
			RemoveSyncLock(doc);
		}
		private Dictionary<string, string> CreateMobileDict(string docType, string refNbr, V2.CCTranType tranType, string companyName, Guid? tranUID)
		{
			Dictionary<string, string> appendParams = new Dictionary<string, string>();
			appendParams.Add("NoteId", DocNoteId.ToString());
			appendParams.Add("DocType", docType);
			appendParams.Add("RefNbr", refNbr);
			appendParams.Add("TranType", tranType.ToString());
			appendParams.Add("CompanyName", companyName);
			appendParams.Add("TranUID", tranUID.ToString());
			return appendParams;
		}
		private void ShowProcessingWarnIfLock(PXAdapter adapter)
		{
			TPrimary doc = adapter.Get<TPrimary>().FirstOrDefault();
			IExternalTransaction extTran = ExternalTranHelper.GetActiveTransaction(GetExtTrans());
			if (doc != null && adapter.ExternalCall && LockExists(doc) && extTran == null)
			{
				var state = ExternalTranHelper.GetLastTransactionState(Base, GetExtTrans());
				if (!(state.IsVoided && state.NeedSync))
				{
					WebDialogResult result = PaymentTransaction.Ask(AR.Messages.CCProcessingARPaymentAlreadyProcessed, MessageButtons.OKCancel);
					if (result == WebDialogResult.No)
					{
						throw new PXException(AR.Messages.CCProcessingOperationCancelled);
					}
				}
			}
		}

		protected override bool RunPendingOperations(TPrimary doc)
		{
			bool supported = IsFeatureSupported(SelectedProcessingCenter, CCProcessingFeature.TransactionGetter, false);
			if (supported)
			{
				IExternalTransaction extTran;
				var trans = GetExtTrans();
				extTran = ExternalTranHelper.GetDeactivatedNeedSyncTransaction(trans);
				if (extTran == null)
				{ 
					extTran = ExternalTranHelper.GetActiveTransaction(trans);
				}
				
				if (extTran == null || extTran.NeedSync == false) return false;

				using (PXTransactionScope scope = new PXTransactionScope())
				{
					IsNeedSyncContext = true;
					ExternalTransactionDetail extTranDetail = GetExtTranDetails().First(i=>i.TransactionID == extTran.TransactionID);
					V2.TransactionData tranData = null;
					try
					{
						tranData = GetPaymentProcessing().GetTransactionById(extTran.TranNumber, SelectedProcessingCenter);
						ValidateTran(doc, tranData);
						RemoveSyncLock(doc);
						UpdateSyncStatus(tranData, extTranDetail);
						SyncProfile(doc, tranData);
						UpdateNeedSyncDoc(doc, tranData);
						scope.Complete();
					}
					catch (TranValidationHelper.TranValidationException ex)
					{
						UpdateSyncStatus(extTranDetail, SyncStatus.Error, ex.Message);
						DeactivateAndUpdateProcStatus(extTranDetail);
						RemoveSyncLock(doc);
						PersistChangesIfNeeded();
						var lastProcTran = GetPaymentTranDetails().First(i => i.TransactionID == extTran.TransactionID);
						var tranType = CCTranTypeCode.GetTranTypeByTranTypeStr(lastProcTran.TranType);
						RunCallbacks(doc, tranType);
						scope.Complete();
						return true;
					}
					catch (PXException ex)
					{
						V2.CCProcessingException innerEx = ex.InnerException as V2.CCProcessingException;
						if (innerEx?.Reason == V2.CCProcessingException.ExceptionReason.TranNotFound)
						{
							DeactivateNotFoundTran(extTranDetail);
							RemoveSyncLock(doc);
							PersistChangesIfNeeded();
							var lastProcTran = GetPaymentTranDetails().First(i => i.TransactionID == extTran.TransactionID);
							var tranType = CCTranTypeCode.GetTranTypeByTranTypeStr(lastProcTran.TranType);
							RunCallbacks(doc, tranType);
							scope.Complete();
							return true;
						}
						throw;
					}
					finally
					{
						IsNeedSyncContext = false;
					}
				}
			}
			return true;
		}

		private string GetCompanyName()
		{
			string companyName;
			if (CompanyService.IsMultiCompany)
			{
				companyName = CompanyService.ExtractCompany(PXContext.PXIdentity.User.Identity.Name);
			}
			else
			{
				companyName = CompanyService.GetSingleCompanyLoginName();
			}
			return companyName;
		}

		private void CheckPaymentTransaction(TPrimary doc)
		{
			if (!IsFeatureSupported(SelectedProcessingCenter, CCProcessingFeature.TransactionGetter, false))
				return;
			ICCPayment pDoc = GetPaymentDoc(doc);
			IEnumerable<V2.TransactionData> trans = null;

			if (LockExists(doc))
			{
				retryUnsettledTran.HandleError(i => GetTransByDoc(pDoc, i).Count > 0 ? true : false);
				try
				{
					trans = retryUnsettledTran.Execute(() => GetPaymentProcessing().GetUnsettledTransactions(SelectedProcessingCenter));
				}
				catch (InvalidOperationException)
				{ }
			}

			if (trans != null)
			{
				IEnumerable<string> result = PrepareTransactionIds(GetTransByDoc(pDoc, trans));
				SyncPaymentTransactionById(doc, result);
			}
			else
			{
				IExternalTransaction tran = ExternalTranHelper.GetActiveTransaction(GetExtTrans());
				if (tran != null)
				{
					SyncPaymentTransactionById(doc, new List<string>() { tran.TranNumber });
				}
			}
		}

		public virtual void SyncPaymentTransactionById(TPrimary doc, IEnumerable<string> tranIds)
		{
			if (!IsPaymentHostedFormSupported(SelectedProcessingCenter)) return;

			using (PXTransactionScope scope = new PXTransactionScope())
			{
				foreach (string tranId in tranIds)
				{
					var tranData = GetTranData(tranId);
					bool recordTran = false;
					ExternalTransactionDetail storedExtTran = null;
					IList<ExternalTransactionDetail> externalTransactions = GetExtTranDetails().ToList();
					if (tranData.TranUID != null)
					{
						storedExtTran = externalTransactions.FirstOrDefault(t => t.NoteID == tranData.TranUID);
						if (storedExtTran != null)
						{
							recordTran = true;
						}
						if (storedExtTran != null && string.IsNullOrEmpty(storedExtTran.TranNumber))
						{
							storedExtTran.TranNumber = tranData.TranID;
							storedExtTran = ExternalTransaction.Update(storedExtTran);
						}
					}
					else
					{
						storedExtTran = externalTransactions.FirstOrDefault(i => i.TranNumber == tranId);
						recordTran = true;
					}

					if (recordTran)
					{
						CheckAndRecordTransaction(doc, storedExtTran, tranData);
					}
				}

				FinalizeTransactionsNotFoundInProcCenter();
				scope.Complete();
			}
		}

		public void CheckAndRecordTransaction(ExternalTransactionDetail extTranDetail, V2.TransactionData tranData)
		{
			TPrimary doc = Base.Caches[typeof(TPrimary)].Current as TPrimary;
			CheckAndRecordTransaction(doc, extTranDetail, tranData);
		}

		public virtual void RemoveLockAndFinalizeTran(IExternalTransaction extTran)
		{
			TPrimary doc = Base.Caches[typeof(TPrimary)].Current as TPrimary;
			RemoveSyncLock(doc);
			string message = PXMessages.LocalizeNoPrefix(AR.Messages.ERR_CCProcessingCenterUserClickedCancel);
			FinalizeTran(extTran, message);
		}

		protected virtual void CheckAndRecordTransaction(TPrimary doc, ExternalTransactionDetail storedExtTran, V2.TransactionData tranData)
		{
			string newProcStatus = GetProcessingStatus(tranData);
			if (storedExtTran != null && storedExtTran.ProcStatus == newProcStatus)
			{
				return;
			}
			if (tranData?.CustomerId != null && !SuitableCustomerProfileId(tranData?.CustomerId))
			{
				return;
			}

			PXTrace.WriteInformation($"Synchronize tran. TranId = {tranData.TranID}, TranType = {tranData.TranType}, DocNum = {tranData.DocNum}, " +
				$"SubmitTime = {tranData.SubmitTime}, Amount = {tranData.Amount}, PCCustomerID = {tranData.CustomerId}, PCCustomerPaymentID = {tranData.PaymentId}");

			V2.CCTranType tranType = tranData.TranType.Value;

			if (storedExtTran != null)
			{
				UpdateSyncStatus(tranData, storedExtTran);
			}
			RemoveSyncLock(doc);

			ICCPayment pDoc = GetPaymentDoc(doc);
			if (tranData.TranStatus == V2.CCTranStatus.Approved && tranType != V2.CCTranType.Void)
			{
				GetOrCreatePaymentProfileByTran(tranData, pDoc);
			}
			PersistChangesIfNeeded();

			switch (tranType)
			{
				case V2.CCTranType.Void:
					RecordVoid(pDoc, tranData);
					break;
				case V2.CCTranType.AuthorizeOnly:
					RecordAuth(pDoc, tranData);
					break;
				case V2.CCTranType.PriorAuthorizedCapture:
				case V2.CCTranType.AuthorizeAndCapture:
				case V2.CCTranType.CaptureOnly:
					RecordCapture(pDoc, tranData);
					break;
			}
		}

		protected override void UpdateSyncStatus(V2.TransactionData tranData, ExternalTransactionDetail extTranDetail)
		{
			bool ok = true;
			ProcessingStatus procStatus = CCProcessingHelper.GetProcessingStatusByTranData(tranData);
			if (procStatus == ProcessingStatus.CaptureSuccess && tranData.Amount < extTranDetail.Amount)
			{
				ok = false;
				string msg = PXMessages.LocalizeFormatNoPrefix(AR.Messages.CCProcessingTranAmountHasChanged, tranData.TranID);
				UpdateSyncStatus(extTranDetail, SyncStatus.Warning, msg);
			}

			if (ok && extTranDetail.SyncStatus != CCSyncStatusCode.Warning && extTranDetail.SyncStatus != CCSyncStatusCode.Success)
			{
				UpdateSyncStatus(extTranDetail, SyncStatus.Success, null);
			}
		}

		protected virtual bool SuitableCustomerProfileId(string customerId)
		{
			bool ret = true;
			if (customerId != null)
			{
				var query = new PXSelect<CustomerPaymentMethod, Where<CustomerPaymentMethod.customerCCPID, Equal<Required<CustomerPaymentMethod.customerCCPID>>,
					And<CustomerPaymentMethod.cCProcessingCenterID, Equal<Required<CustomerPaymentMethod.cCProcessingCenterID>>>>>(Base);
				CustomerPaymentMethod cpm = query.SelectSingle(customerId, this.SelectedProcessingCenter);
				if (cpm != null && cpm.BAccountID != SelectedBAccount)
				{
					ret = false;
				}
			}
			return ret;
		}

		protected virtual int? GetOrCreatePaymentProfileByTran(V2.TransactionData tranData, ICCPayment pDoc)
		{
			if (pDoc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
			{
				return pDoc.PMInstanceID;
			}

			int? instanceID = PaymentTranExtConstants.NewPaymentProfile;

			V2.TranProfile profile = null;
			if (tranData.CustomerId != null && tranData.PaymentId != null)
			{
				profile = new V2.TranProfile()
				{ CustomerProfileId = tranData.CustomerId, PaymentProfileId = tranData.PaymentId };
			}

			if (!NeedSaveCard() || !CheckAllowSavingCards())
			{
				if (profile != null)
				{
					instanceID = GetInstanceId(profile);
				}
				if (instanceID != PaymentTranExtConstants.NewPaymentProfile)
				{
				SetPmInstanceId(instanceID);
				}
				return instanceID;
			}

			var creator = GetPaymentProfileCreator();
			try
			{
				CustomerPaymentMethod cpm = creator.PrepeareCpmRecord();

				if (profile == null)
				{
					profile = GetOrCreateCustomerProfileByTranId(cpm, tranData.TranID);
				}

				instanceID = GetInstanceId(profile);

				if (instanceID == PaymentTranExtConstants.NewPaymentProfile)
				{
					instanceID = creator.CreatePaymentProfile(profile);
				}
				if (instanceID != PaymentTranExtConstants.NewPaymentProfile)
				{
				creator.CreateCustomerProcessingCenterRecord(profile);
			}
			}
			finally
			{
				creator.ClearCaches();
			}
			SetPmInstanceId(instanceID);
			return instanceID;
		}

		protected virtual void FinalizeTransactionsNotFoundInProcCenter()
		{
			string message = PXMessages.LocalizeNoPrefix(AR.Messages.ERR_CCProcessingCenterPCResponseReasonNotExists);
			foreach (IExternalTransaction extTran in GetExtTrans())
			{
				FinalizeTran(extTran, message);
			}
		}

		protected virtual void FinalizeTran(IExternalTransaction extTran, string message)
		{
			var processing = GetPaymentProcessing();
			if (extTran.ProcStatus == ExtTransactionProcStatusCode.Unknown)
			{
				var procTran = PaymentTransaction.Select().RowCast<PaymentTransactionDetail>()
					.Where(i => i.TransactionID == extTran.TransactionID).FirstOrDefault();
				if (procTran != null && procTran.ProcStatus == CCProcStatus.Opened && procTran.Imported == false)
				{
					processing.FinalizeTransaction(procTran.TranNbr, message);
				}
			}
		}

		protected virtual bool IsPaymentHostedFormSupported(string procCenterId)
		{
			bool ret = CCProcessingFeatureHelper.IsPaymentHostedFormSupported(GetProcessingCenterById(procCenterId));
			return ret;
		}

		protected virtual ExternalTransactionDetail StartCreditCardTransaction(ICCPayment pDoc, CCTranType ccTranType)
		{
			CCProcTran tran = new CCProcTran();
			tran.Copy(pDoc);
			tran.ProcessingCenterID = SelectedProcessingCenter;
			var processing = GetPaymentProcessing();
			CCProcessingCenter procCenter = processing.GetAndCheckProcessingCenterFromTransaction(tran);
			tran = processing.StartCreditCardTransaction(ccTranType, tran, procCenter);
			ExternalTransactionDetail extTranDetail = GetExtTranDetails().Where(i => i.TransactionID == tran.TransactionID).First();
			return extTranDetail;
		}

		protected virtual ExternalTransactionDetail UpdateExtTran(ExternalTransactionDetail extTran, PXPluginRedirectOptions options)
		{
			if (options is V2.ICCRedirectOptionsWithTransactionID optionsWithTranID && !string.IsNullOrEmpty(optionsWithTranID.TransactionID))
			{
				extTran.TranNumber = optionsWithTranID.TransactionID;
				extTran = ExternalTransaction.Update(extTran);
			}
			return extTran;
		}

		public override void Initialize()
		{
			base.Initialize();
			CheckSyncLockOnPersist = true;
			retryUnsettledTran = new RetryPolicy<IEnumerable<V2.TransactionData>>();
			retryUnsettledTran.RetryCnt = 1;
			retryUnsettledTran.StaticSleepDuration = 6000;
		}

		protected void CreateCustomerProcessingCenterRecord(V2.TranProfile input)
		{
			PXCache customerProcessingCenterCache = Base.Caches[typeof(CustomerProcessingCenterID)];
			customerProcessingCenterCache.ClearQueryCacheObsolete();
			PXSelectBase<CustomerProcessingCenterID> checkRecordExist = new PXSelectReadonly<CustomerProcessingCenterID,
				Where<CustomerProcessingCenterID.cCProcessingCenterID, Equal<Required<CustomerProcessingCenterID.cCProcessingCenterID>>,
				And<CustomerProcessingCenterID.bAccountID, Equal<Required<CustomerProcessingCenterID.bAccountID>>,
				And<CustomerProcessingCenterID.customerCCPID, Equal<Required<CustomerProcessingCenterID.customerCCPID>>>>>>(Base);

			CustomerProcessingCenterID cProcessingCenter = checkRecordExist.SelectSingle(SelectedProcessingCenter, SelectedBAccount, input.CustomerProfileId);

			if (cProcessingCenter == null)
			{
				cProcessingCenter = customerProcessingCenterCache.CreateInstance() as CustomerProcessingCenterID;
				cProcessingCenter.BAccountID = SelectedBAccount;
				cProcessingCenter.CCProcessingCenterID = SelectedProcessingCenter;
				cProcessingCenter.CustomerCCPID = input.CustomerProfileId;
				customerProcessingCenterCache.Insert(cProcessingCenter);
				customerProcessingCenterCache.Persist(PXDBOperation.Insert);
			}
		}

		protected int? GetInstanceId(V2.TranProfile input)
		{
			int? instanceID = PaymentTranExtConstants.NewPaymentProfile;
			PXCache cpmCache = Base.Caches[typeof(CustomerPaymentMethod)];
			cpmCache.ClearQueryCacheObsolete();
			var repo = GetPaymentProcessing().Repository;
			var result = repo.GetCustomerPaymentMethodWithProfileDetail(SelectedProcessingCenter, input.CustomerProfileId, input.PaymentProfileId);

			if (result != null)
			{
				var cpm = result.Item1;
				if (cpm != null && cpm.BAccountID == SelectedBAccount && cpm.IsActive == true)
				{
					instanceID = cpm.PMInstanceID;
				}
			}
			return instanceID;
		}

		protected V2.TranProfile GetOrCreateCustomerProfileByTranId(CustomerPaymentMethod cpm, string tranId)
		{
			PXSelectBase<CustomerPaymentMethod> query = new PXSelectReadonly<CustomerPaymentMethod,
				Where<CustomerPaymentMethod.bAccountID, Equal<Required<CustomerPaymentMethod.bAccountID>>,
					And<CustomerPaymentMethod.cCProcessingCenterID, Equal<Required<CustomerPaymentMethod.cCProcessingCenterID>>>>,
				OrderBy<Desc<CustomerPaymentMethod.createdDateTime>>>(Base);

			IEnumerable<CustomerPaymentMethod> cpmRes = query.Select(SelectedBAccount, SelectedProcessingCenter).RowCast<CustomerPaymentMethod>();
			CustomerPaymentMethod searchCpm = cpmRes.FirstOrDefault();
			if (searchCpm != null)
			{
				cpm.CustomerCCPID = searchCpm.CustomerCCPID;
			}

			PXSelect<CustomerPaymentMethod> cpmNew = new PXSelect<CustomerPaymentMethod>(Base);
			V2.TranProfile ret = null;
			try
			{
				cpmNew.Insert(cpm);
				CCCustomerInformationManagerGraph infoManagerGraph = PXGraph.CreateInstance<CCCustomerInformationManagerGraph>();
				GenericCCPaymentProfileAdapter<CustomerPaymentMethod> cpmAdapter =
				new GenericCCPaymentProfileAdapter<CustomerPaymentMethod>(cpmNew);
				ret = infoManagerGraph.GetOrCreatePaymentProfileByTran(Base, cpmAdapter, tranId);
			}
			finally
			{
				cpmNew.Cache.Clear();
			}
			return ret;
		}

		protected V2.TransactionData GetTranData(string tranId)
		{
			V2.TransactionData tranData = GetPaymentProcessing().GetTransactionById(tranId, SelectedProcessingCenter);
			return tranData;
		}

		protected Customer GetCustomerByAccountId(int? id)
		{
			Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(Base, id);
			return customer;
		}

		protected virtual bool IsSupportPaymentHostedForm(string processingCenterId)
		{
			if (processingCenterId != checkedProcessingCenter)
			{
				checkedProcessingCenterResult = IsPaymentHostedFormSupported(processingCenterId);
				checkedProcessingCenter = processingCenterId;
			}
			return checkedProcessingCenterResult;
		}

		private List<V2.TransactionData> GetTransByDoc(ICCPayment payment, IEnumerable<V2.TransactionData> trans)
		{
			string searchDocNum = payment.DocType + payment.RefNbr;
			List<V2.TransactionData> targetTran = trans.Where(i => i.DocNum == searchDocNum).ToList();
			return targetTran;
		}

		private IEnumerable<string> PrepareTransactionIds(List<V2.TransactionData> list)
		{
			return list.OrderBy(i => i.SubmitTime).Select(i => i.TranID);
		}

		private bool FindPreAuthorizing()
		{
			ExternalTransactionState state = GetActiveTransactionState();
			return state.IsPreAuthorized ? true : false;
		}

		private bool TranHeldForReview()
		{ 
			ExternalTransactionState state = GetActiveTransactionState();
			return state.IsOpenForReview ? true : false;
		}

		protected override void RowSelected(Events.RowSelected<TPrimary> e)
		{
			base.RowSelected(e);
		}

		protected virtual TPrimary GetDocWithoutChanges(TPrimary input)
		{
			return null;
		}
		protected virtual PaymentTransactionAcceptFormGraph<TGraph, TPrimary> GetPaymentTransactionAcceptFormExt(TGraph graph)
		{
			throw new NotImplementedException();
		}
	}
}
