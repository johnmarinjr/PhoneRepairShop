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
	public class ARPaymentEntryPaymentTransaction : PaymentTransactionAcceptFormGraph<ARPaymentEntry, ARPayment>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.integratedCardProcessing>();

		public PXFilter<InputPaymentInfo> ccPaymentInfo;

		public bool RaisedVoidForReAuthorization { get; set; }

		

		protected override void RowPersisting(Events.RowPersisting<ARPayment> e)
		{
			ARPayment payment = e.Row;

			CheckSyncLock(payment);
			CheckProcessingCenter(Base.Document.Cache, Base.Document.Current);

			if (payment.CCTransactionRefund == true && !CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(Base.paymentmethod.Current) &&
				payment.DocType != ARDocType.Refund && payment.DocType != ARDocType.VoidRefund)
			{
				throw new PXRowPersistingException(nameof(ARPayment.CCTransactionRefund), payment.CCTransactionRefund,
					Messages.ERR_DocumentNotSupportedLinkedRefunds);
			}

			PaymentMethod pm = Base.paymentmethod.Select();
			if (payment.SaveCard == true && (payment.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile
				|| pm?.PaymentType != PaymentMethodType.CreditCard))
			{
				payment.SaveCard = false;
			}

			base.RowPersisting(e);
		}

		protected virtual void RowUpdated(Events.RowUpdated<ARPayment> e)
		{
			ARPayment payment = e.Row;
			ARPayment oldPayment = e.OldRow;
			if (payment == null) return;

			CheckProcCenterAndCashAccountCurrency(payment);
			UpdateUserAttentionFlagIfNeeded(e);
			if (e.Cache.GetStatus(payment) == PXEntryStatus.Inserted && !Base.IsContractBasedAPI)
			{
				if (payment.ProcessingCenterID != null && payment.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile)
				{
					if (payment.SaveCard == false && ForceSaveCard(payment))
					{
						e.Cache.SetValueExt<ARPayment.saveCard>(payment, true);
					}
					else if (payment.SaveCard == true && ProhibitSaveCard(payment))
					{
						e.Cache.SetValueExt<ARPayment.saveCard>(payment, false);
					}
				}
			}
		}

		public virtual void CheckProcCenterAndCashAccountCurrency(ARPayment doc)
		{
			if (doc.IsCCPayment == true)
			{
				CashAccount docCashAcc = CashAccount.PK.Find(Base, doc.CashAccountID);
				CCProcessingCenter procCenter = GetCCProcessingCenterByProcCenterOrPMInstance(doc);
				CashAccount procCenterCashAcc = CashAccount.PK.Find(Base, procCenter?.CashAccountID);

				bool isCurrencyDifferent = docCashAcc != null && procCenterCashAcc != null && docCashAcc.CuryID != procCenterCashAcc.CuryID;

				if (isCurrencyDifferent)
				{
					PXSetPropertyException exception = GetDiffCurrecyException(doc, docCashAcc, procCenterCashAcc);
					Base.Document.Cache.RaiseExceptionHandling<ARPayment.cashAccountID>(doc, docCashAcc?.CashAccountCD, exception);
				}
				else
				{
					Base.Document.Cache.RaiseExceptionHandling<ARPayment.cashAccountID>(doc, docCashAcc?.CashAccountCD, null);
				}
			}
		}

		public virtual PXSetPropertyException GetDiffCurrecyException(ARPayment doc, CashAccount docCashAcc, CashAccount procCenterCashAcc)
		{
			PXSetPropertyException exception = null;
			CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
			if (cpm == null)
			{
				exception = new PXSetPropertyException(Messages.ProcCenterCuryIDDifferentFromCashAccountCuryID,
					doc.ProcessingCenterID, procCenterCashAcc.CuryID, docCashAcc.CashAccountCD, docCashAcc.CuryID, PXErrorLevel.Error);
			}
			else
			{
				exception = new PXSetPropertyException(Messages.CardCuryIDDifferentFromCashAccountCuryID,
					docCashAcc.CashAccountCD, docCashAcc.CuryID, cpm.Descr, procCenterCashAcc.CuryID, PXErrorLevel.Error);
			}

			return exception;
		}

		protected virtual CCProcessingCenter GetCCProcessingCenterByProcCenterOrPMInstance(ARPayment doc)
		{
			CCProcessingCenter output = null;
			if (doc.ProcessingCenterID != null)
			{
				output = GetProcessingCenterById(doc.ProcessingCenterID);
			}
			else if (doc.PMInstanceID != null)
			{
				CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
				output = GetProcessingCenterById(cpm.CCProcessingCenterID);
			}
			return output;
		}

		protected override void RowSelected(Events.RowSelected<ARPayment> e)
		{
			base.RowSelected(e);
			ARPayment doc = e.Row;
			if (doc == null)
				return;
			TranHeldwarnMsg = AR.Messages.CCProcessingARPaymentTranHeldWarning;
			PXCache cache = e.Cache;
			bool docOnHold = doc.Hold == true;
			bool docOpen = doc.OpenDoc == true;
			bool docReleased = doc.Released == true;
			bool enableCCProcess = EnableCCProcess(doc);
			bool docIsMemoOrBalanceWO = doc.DocType == ARDocType.CreditMemo || doc.DocType == ARDocType.SmallBalanceWO;
			bool isCCPaymentMethod = CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(Base.paymentmethod.Current);
			CCProcessingCenter procCenter = Base.processingCenter.Current;
			bool isExtAuthOnly = procCenter?.IsExternalAuthorizationOnly == true;

			PaymentRefAttribute.SetAllowAskUpdateLastRefNbr<ARPayment.extRefNbr>(cache, doc?.IsCCPayment == false);

			if (doc.DocType == ARDocType.Refund && isCCPaymentMethod)
			{
				SetVisibilityCreditCardControlsForRefund(cache, doc);
			}
			else
			{
				doc.NewCard = isCCPaymentMethod == true && doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile ? true : false;
				bool newCardVal = doc.NewCard.GetValueOrDefault();
				bool showPMInstance = !newCardVal && !docIsMemoOrBalanceWO;
				bool showProcCenter = newCardVal && !docIsMemoOrBalanceWO;
				PXUIFieldAttribute.SetVisible<ARPayment.pMInstanceID>(cache, doc, showPMInstance);
				PXUIFieldAttribute.SetVisible<ARPayment.processingCenterID>(cache, doc, showProcCenter);
			}

			PXPersistingCheck extRefNbrPersistCheck = PXPersistingCheck.Null;

			if (docIsMemoOrBalanceWO || enableCCProcess || ARSetup.Current.RequireExtRef == false)
				extRefNbrPersistCheck = PXPersistingCheck.Nothing;

			ExternalTransactionState extTranState = GetActiveTransactionState();
			doc.CCPaymentStateDescr = GetPaymentStateDescr(extTranState);

			var trans = GetExtTrans();
			bool enableRefTranNbr = enableCCProcess && (doc.DocType == ARDocType.Refund)
				&& !extTranState.IsRefunded && !(extTranState.IsImportedUnknown && !extTranState.SyncFailed)
				&& doc.CCTransactionRefund == true && !docReleased
				&& !RefundDocHasValidSharedTran(trans);
			bool showTranRef = isCCPaymentMethod && doc.DocType == ARDocType.Refund && !extTranState.IsRefunded
				&& HasProcCenterSupportingUnlinkedMode(cache, doc);
			PXUIFieldAttribute.SetEnabled<ARPayment.refTranExtNbr>(cache, doc, enableRefTranNbr);
			PXUIFieldAttribute.SetRequired<ARPayment.processingCenterID>(cache, isCCPaymentMethod
				&& doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile);
			PXUIFieldAttribute.SetVisible<ARPayment.cCTransactionRefund>(cache, doc, showTranRef);
			PXUIFieldAttribute.SetVisible<ARPayment.cCPaymentStateDescr>(cache, doc, doc.IsCCPayment == true);
			PXUIFieldAttribute.SetVisible<ARPayment.refTranExtNbr>(cache, doc, doc.DocType == ARDocType.Refund
				&& enableCCProcess);
			PXDefaultAttribute.SetPersistingCheck<ARPayment.extRefNbr>(cache, doc, extRefNbrPersistCheck);

			SetUsingAcceptHostedForm(doc);
			
			bool canAuthorize = CanAuthorize(doc, extTranState);
			bool canCapture = CanCapture(doc, extTranState);
			bool canCaptureOnly = CanCaptureOnly(doc);
			bool canCredit = CanCredit(doc, extTranState);
			bool canValidate = CanValidate(doc, extTranState);
			bool canVoid = CanVoid(doc, extTranState);
			bool canVoidForReAuthorization = CanVoidForReAuthorization(doc, extTranState);

			this.authorizeCCPayment.SetEnabled(canAuthorize && !isExtAuthOnly);
			this.captureCCPayment.SetEnabled(canCapture);
			this.validateCCPayment.SetEnabled(canValidate);
			this.voidCCPayment.SetEnabled(canVoid);
			this.creditCCPayment.SetEnabled(canCredit);
			this.captureOnlyCCPayment.SetEnabled(doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile && canAuthorize && canCaptureOnly);
			this.recordCCPayment.SetEnabled((canCapture && !extTranState.IsActive) || canAuthorize || canCredit);
			this.voidCCPaymentForReAuthorization.SetEnabled(canVoidForReAuthorization);
			#region CCProcessing integrated with doc
			bool isCCStateClear = !(extTranState.IsCaptured || extTranState.IsPreAuthorized);
			if (enableCCProcess && ARSetup.Current.IntegratedCCProcessing == true && !docReleased)
			{
				if ((bool)doc.VoidAppl == false)
				{
					bool voidTranForRef = doc.DocType == ARDocType.Refund && RefundDocHasValidSharedTran(trans);
					bool enableRelease = !docOnHold && docOpen && (extTranState.IsSettlementDue || voidTranForRef)
						&& doc.PendingProcessing == false;
					Base.release.SetEnabled(enableRelease);
				}
				else
				{
					//We should allow release if CCPayment has just pre-authorization - it will expire anyway.
					Base.release.SetEnabled(!docOnHold && docOpen && (isCCStateClear || (extTranState.IsPreAuthorized && extTranState.ProcessingStatus == ProcessingStatus.VoidFail)));
				}
			}
			#endregion
			
			ShowWarningIfExternalAuthorizationOnly(e, doc);
			ShowUnlinkedRefundWarnIfNeeded(e, extTranState);
			DenyDeletionVoidedPaymentDependingOnTran(cache, doc);
			ShowWarningOnProcessingCenterID(e, extTranState);
		}

		protected virtual void ShowWarningIfExternalAuthorizationOnly(Events.RowSelected<ARPayment> e, ARPayment doc)
		{
			ExternalTransactionState state = GetActiveTransactionState();
			CCProcessingCenter procCenter = Base.processingCenter.Current;

			bool showWarning = procCenter?.IsExternalAuthorizationOnly == true 
				&& (!state.IsActive || state.IsExpired)
				&& doc.Status == ARDocStatus.CCHold
				&& (doc.DocType == ARPaymentType.Payment || doc.DocType == ARPaymentType.Prepayment);

			CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);

			UIState.RaiseOrHideErrorByErrorLevelPriority<ARPayment.pMInstanceID>(e.Cache, e.Row, showWarning,
				Messages.CardAssociatedWithExternalAuthorizationOnlyProcessingCenter, PXErrorLevel.Warning, cpm?.Descr, procCenter?.ProcessingCenterID);
		}

		protected virtual void ShowWarningOnProcessingCenterID(Events.RowSelected<ARPayment> e, ExternalTransactionState state)
		{
			var doc = e.Row;
			if (doc == null) return;

			if (state?.IsActive == true) return;

			CCProcessingCenter procCenter = GetProcessingCenterById(doc.ProcessingCenterID);

			bool isPaymentOrPrepayment = (doc.DocType == ARPaymentType.Payment || doc.DocType == ARPaymentType.Prepayment);
			bool isExternalAuthorizationOnly = procCenter?.IsExternalAuthorizationOnly == true
				&& isPaymentOrPrepayment && doc.PendingProcessing == true;
			bool useAcceptPaymentForm = procCenter?.UseAcceptPaymentForm == false && isPaymentOrPrepayment
				&& doc.PendingProcessing == true;

			string errorMessage = string.Empty;
			bool isIncorrect = false;

			if (isExternalAuthorizationOnly)
			{
				errorMessage = Messages.ProcessingCenterIsExternalAuthorizationOnly;
				isIncorrect = true;
			}
			else if (useAcceptPaymentForm)
			{
				errorMessage = CA.Messages.AcceptPaymentFromNewCardDisabledWarning;
				isIncorrect = true;
			}

			UIState.RaiseOrHideErrorByErrorLevelPriority<Payment.processingCenterID>(e.Cache, e.Row, isIncorrect,
					errorMessage, PXErrorLevel.Warning, procCenter?.ProcessingCenterID);
		}

		public static bool IsDocTypePayment(ARPayment doc)
		{
			bool docTypePayment = doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment;
			return docTypePayment;
		}

		public bool EnableCCProcess(ARPayment doc)
		{
			bool enableCCProcess = false;
			PaymentMethod pm = this.Base.paymentmethod.Current;

			if (doc.IsMigratedRecord != true && CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(pm))
			{
				enableCCProcess = IsDocTypePayment(doc) || doc.DocType == ARDocType.Refund || doc.DocType == ARDocType.VoidPayment;
			}
			enableCCProcess &= !doc.Voided.Value;

			bool disabledProcCenter = IsProcCenterDisabled(SelectedProcessingCenterType);
			enableCCProcess &= !disabledProcCenter;

			return enableCCProcess;
		}

		public bool CanAuthorize()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanAuthorize(doc, state);
		}

		public bool CanCapture()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanCapture(doc, state);
		}

		public bool CanVoid()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanVoid(doc, state);
		}

		public bool CanCredit()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanCredit(doc, state);
		}

		public bool CanValidate()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanValidate(doc, state);
		}

		public bool CanVoidForReAuthorization()
		{
			ARPayment doc = Base.Document.Current;
			ExternalTransactionState state = GetActiveTransactionState();
			return CanVoidForReAuthorization(doc, state);
		}

		private bool CanAuthorize(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);
			if (!enableCCProcess) return false;

			PXCache cache = Base.Document.Cache;
			bool canAuthorize = doc.Hold != true && IsDocTypePayment(doc)
				&& (UseAcceptHostedForm == false || cache.GetStatus(doc) != PXEntryStatus.Inserted);
			if (canAuthorize)
			{
				canAuthorize = !(state.IsPreAuthorized || state.IsCaptured || state.IsImportedUnknown);
			}

			if (canAuthorize)
			{
				var trans = GetExtTrans();
				canAuthorize = !ExternalTranHelper.HasImportedNeedSyncTran(Base, trans)
					&& !RefundDocHasValidSharedTran(trans);
			}
			return canAuthorize;
		}

		private bool CanCapture(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);
			if (!enableCCProcess) return false;

			PXCache cache = Base.Document.Cache;
			bool canCapture = (doc.Hold != true) && IsDocTypePayment(doc);

			if (canCapture)
			{
				CCProcessingCenter procCenter = Base.processingCenter.Current;
				
				bool canCaptureIfExtAuthOnly = procCenter?.IsExternalAuthorizationOnly == false 
					|| (state.IsActive == true && !state.IsExpired);
				
				canCapture = !(state.IsCaptured || state.IsImportedUnknown)
					&& !state.IsOpenForReview && canCaptureIfExtAuthOnly
					&& (UseAcceptHostedForm == false || (cache.GetStatus(doc) != PXEntryStatus.Inserted && !state.IsOpenForReview));
			}

			if (canCapture)
			{
				var trans = GetExtTrans();
				canCapture = !ExternalTranHelper.HasImportedNeedSyncTran(Base, trans)
					&& !RefundDocHasValidSharedTran(trans);
			}
			return canCapture;
		}

		private bool CanCaptureOnly(ARPayment doc)
		{
			return CCProcessingFeatureHelper.IsFeatureSupported(Base.processingCenter.Current, CCProcessingFeature.CapturePreauthorization, false);
		}

		private bool CanVoid(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);
			if (!enableCCProcess) return false;

			bool canVoid = doc.Hold == false && (doc.DocType == ARDocType.VoidPayment && (state.IsCaptured || state.IsPreAuthorized)) ||
			   (state.IsPreAuthorized && IsDocTypePayment(doc));

			if (canVoid)
			{
				canVoid = !(state.IsOpenForReview && GettingDetailsByTranSupported(doc))
					&& !ExternalTranHelper.HasImportedNeedSyncTran(Base, GetExtTrans());
			}
			return canVoid;
		}

		private bool CanCredit(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);
			if (!enableCCProcess) return false;

			bool canCredit = doc.Hold == false && doc.DocType == ARDocType.Refund;

			if (canCredit)
			{
				canCredit = !state.IsRefunded
					&& !(state.IsImportedUnknown && !state.SyncFailed);
			}

			if (canCredit)
			{
				var trans = GetExtTrans();
				canCredit = !RefundDocHasValidSharedTran(trans);
			}
			return canCredit;
		}

		private bool CanValidate(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);

			if (!enableCCProcess)
				return false;

			PXCache cache = Base.Document.Cache;
			bool canValidate = doc.Hold != true && (IsDocTypePayment(doc) || doc.DocType == ARDocType.Refund) &&
				cache.GetStatus(doc) != PXEntryStatus.Inserted;

			if (!canValidate)
				return false;

			canValidate = (CanCapture(doc, state) || CanAuthorize(doc, state) || state.IsOpenForReview
				|| ExternalTranHelper.HasImportedNeedSyncTran(Base, GetExtTrans())
				|| state.NeedSync || state.IsImportedUnknown || doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile);

			if (canValidate && doc.DocType == ARDocType.Refund)
			{
				var sharedTranStatus = ExternalTranHelper.GetSharedTranStatus(Base, GetExtTrans().FirstOrDefault());
				if (sharedTranStatus == ExternalTranHelper.SharedTranStatus.ClearState
					|| sharedTranStatus == ExternalTranHelper.SharedTranStatus.Synchronized)
				{
					canValidate = false;
				}
			}

			if (!canValidate)
			{
				var manager = GetAfterProcessingManager(Base);
				canValidate = manager != null && !manager.CheckDocStateConsistency(doc);
			}

			canValidate = canValidate && GettingDetailsByTranSupported(doc);

			return canValidate;
		}

		private bool CanVoidForReAuthorization(ARPayment doc, ExternalTransactionState state)
		{
			bool enableCCProcess = EnableCCProcess(doc);
			if (!enableCCProcess) return false;

			bool canVoidForReAuthorization = state.IsPreAuthorized && doc.PMInstanceID != null 
				&& !(state.IsOpenForReview && GettingDetailsByTranSupported(doc));
			return canVoidForReAuthorization;
		}

		private void UpdateUserAttentionFlagIfNeeded(Events.RowUpdated<ARPayment> e)
		{
			ARPayment payment = e.Row;
			ARPayment oldPayment = e.OldRow;
			if (!e.Cache.ObjectsEqual<ARPayment.paymentMethodID, ARPayment.pMInstanceID>(payment, oldPayment))
			{
				PaymentMethod pm = Base.paymentmethod.Current;
				if (CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(pm)
					&& (payment.DocType == ARDocType.Payment || payment.DocType == ARDocType.Prepayment))
				{
					var trans = GetExtTrans();
					bool updateFlag = trans.Count() == 0;

					if (!updateFlag)
					{
						var state = ExternalTranHelper.GetTransactionState(Base, trans.First());
						updateFlag = (state.IsVoided || state.IsExpired) && !state.IsActive;
					}

					if (updateFlag)
					{
						bool newProfile = payment.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile;
						e.Cache.SetValueExt<ARPayment.isCCUserAttention>(payment, newProfile);
					}
				}
				else
				{
					e.Cache.SetValueExt<ARPayment.isCCUserAttention>(payment, false);
				}
			}
		}

		private void DenyDeletionVoidedPaymentDependingOnTran(PXCache cache, ARPayment doc)
		{
			if (doc.Released == false && doc.DocType == ARDocType.VoidPayment)
			{
				ExternalTransaction extTran = Base.ExternalTran.SelectSingle();
				if (extTran != null)
				{
					var state = ExternalTranHelper.GetTransactionState(Base, extTran);
					if (state.IsVoided || state.IsRefunded)
					{
						cache.AllowDelete = false;
					}
				}
			}
		}

		private void ShowUnlinkedRefundWarnIfNeeded(Events.RowSelected<ARPayment> e, ExternalTransactionState state)
		{
			ARPayment doc = e.Row;
			if (CanCredit(doc, state) && doc.CCTransactionRefund == false && doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
			{
				CCProcessingCenter procCenter = Base.processingCenter.Current;
				if (procCenter != null && procCenter.AllowUnlinkedRefund == false)
				{
					CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
					e.Cache.RaiseExceptionHandling<ARPayment.pMInstanceID>(doc, doc.PMInstanceID,
						new PXSetPropertyException<ARPayment.pMInstanceID>(Messages.ERR_ProcCenterNotSupportedUnlinkedRefunds, PXErrorLevel.Warning, cpm?.Descr, procCenter.ProcessingCenterID));
				}
				else
				{
					e.Cache.RaiseExceptionHandling<ARPayment.pMInstanceID>(doc, doc.PMInstanceID, null);
				}
			}
		}

		private bool ForceSaveCard(ARPayment payment)
		{
			bool ret = false;
			PaymentMethod pm = Base.paymentmethod.Current;
			CustomerClass custClass = Base.customerclass.Current;
			string saveCustOpt = custClass?.SavePaymentProfiles;
			CCProcessingCenter procCetner = Base.processingCenter.Current;
			if (saveCustOpt == SavePaymentProfileCode.Force
				&& pm?.PaymentType == PaymentMethodType.CreditCard && pm?.IsAccountNumberRequired == true
				&& (payment.DocType == ARDocType.Payment || payment.DocType == ARDocType.Prepayment) && procCetner?.AllowSaveProfile == true)
			{
				ret = true;
			}
			return ret;
		}

		private bool ProhibitSaveCard(ARPayment payment)
		{
			bool ret = false;
			PaymentMethod pm = Base.paymentmethod.Current;
			CustomerClass custClass = Base.customerclass.Current;
			string saveCustOpt = custClass?.SavePaymentProfiles;
			CCProcessingCenter procCetner = Base.processingCenter.Current;
			if ((saveCustOpt == SavePaymentProfileCode.Prohibit || procCetner?.AllowSaveProfile == false)
				&& pm?.PaymentType == PaymentMethodType.CreditCard
				&& (payment.DocType == ARDocType.Payment || payment.DocType == ARDocType.Prepayment))
			{
				ret = true;
			}
			return ret;
		}

		protected override void MapViews(ARPaymentEntry graph)
		{
			base.MapViews(graph);
			PaymentTransaction = new PXSelectExtension<PaymentTransactionDetail>(Base.ccProcTran);
			ExternalTransaction = new PXSelectExtension<Extensions.PaymentTransaction.ExternalTransactionDetail>(Base.ExternalTran);
		}

		[PXUIField(DisplayName = "Record and Capture Preauthorization", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public override IEnumerable CaptureOnlyCCPayment(PXAdapter adapter)
		{
			if (this.Base.Document.Current != null &&
					this.Base.Document.Current.Released == false &&
					this.Base.Document.Current.IsCCPayment == true
					&& ccPaymentInfo.AskExt(initAuthCCInfo) == WebDialogResult.OK)
			{
				return base.CaptureOnlyCCPayment(adapter);
			}
			ccPaymentInfo.View.Clear();
			ccPaymentInfo.Cache.Clear();
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Record Card Payment", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public override IEnumerable RecordCCPayment(PXAdapter adapter)
		{
			if (this.Base.Document.Current != null &&
			this.Base.Document.Current.Released == false &&
			this.Base.Document.Current.IsCCPayment == true)
			{
				var dialogResult = this.Base.Document.AskExt();
				if (dialogResult == WebDialogResult.OK || (Base.IsContractBasedAPI && dialogResult == WebDialogResult.Yes))
				{
					return base.RecordCCPayment(adapter);
				}
			}
			InputPmtInfo.View.Clear();
			InputPmtInfo.Cache.Clear();
			return adapter.Get();
		}

		public PXAction<ARPayment> voidCCPaymentForReAuthorization;
		[PXUIField(DisplayName = "Void and Reauthorize", Visible = false, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public virtual IEnumerable VoidCCPaymentForReAuthorization(PXAdapter adapter)
		{
			var list = adapter.Get<ARPayment>().ToList();

			PXLongOperation.StartOperation(Base, delegate
			{
				var paymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
				var paymentTransactionExt = paymentGraph.GetExtension<ARPaymentEntryPaymentTransaction>();

				foreach (ARPayment doc in list)
				{
					CheckProcCenterDisabled();
					ICCPaymentProcessingRepository repository = CCPaymentProcessingRepository.GetCCPaymentProcessingRepository();
					var processingCenter = repository.GetCCProcessingCenter(doc.ProcessingCenterID);

					if ((processingCenter.ReauthRetryNbr ?? 0) == 0)
					{
						throw new PXException(Messages.ERR_ReauthorizationIsNotSetUp, doc.RefNbr, processingCenter.Name);
					}

					bool creditCardForReauthExists = doc.PMInstanceID != null;

					if (creditCardForReauthExists)
					{
						var cpm = repository.GetCustomerPaymentMethod(doc.PMInstanceID);
						DateTime now = DateTime.Now.Date;
						creditCardForReauthExists = !(cpm.IsActive != true
													|| cpm.ExpirationDate < now);
					}

					if (!creditCardForReauthExists)
					{
						throw new PXException(Messages.ERR_NoActiveCardForReauth);
					}

					paymentGraph.Document.Current = paymentGraph.Document.Search<ARPayment.refNbr>(doc.RefNbr, doc.DocType);
					CheckScheduledDateForReauth(paymentGraph, doc);

					try
					{
						paymentTransactionExt.RaisedVoidForReAuthorization = true;

						paymentTransactionExt.DoValidateCCPayment(doc);

						IExternalTransaction tran = paymentGraph.ExternalTran.SelectSingle();
						ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(paymentGraph, tran);

						if (tranState.IsPreAuthorized && tranState.IsActive)
						{
							var adapterForOnePayment = ARPaymentEntry.CreateAdapterWithDummyView(paymentGraph, paymentGraph.Document.Current);
							paymentTransactionExt.VoidCCPayment(adapterForOnePayment);

							CCProcessingCenterPmntMethod method = paymentGraph.ProcessingCenterPmntMethod.Select();
							if (method?.ReauthDelay == 0)
							{
								paymentTransactionExt.ClearTransactionCaches();
								paymentTransactionExt.RaisedVoidForReAuthorization = false;
								paymentTransactionExt.AuthorizeCCPayment(adapterForOnePayment);
							}
						}
					}
					finally
					{
						paymentTransactionExt.RaisedVoidForReAuthorization = false;
					}
					paymentGraph.Clear();
				}
			});

			return list;
		}

		protected virtual void CheckScheduledDateForReauth(ARPaymentEntry paymentGraph, ARPayment doc)
		{
			CCProcessingCenterPmntMethod method = paymentGraph.ProcessingCenterPmntMethod.Select();

			if (method.ReauthDelay > 0)
			{
				DateTime reauthDate = PXTimeZoneInfo.Now.AddDays(1).AddHours(method.ReauthDelay.Value);
				CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
				if (cpm.ExpirationDate < reauthDate)
				{
					throw new PXException(Messages.ERR_CannotVoidForReauth);
				}
			}

		}

		protected virtual bool RefundDocHasValidSharedTran(IEnumerable<IExternalTransaction> trans)
		{
			var tran = trans.FirstOrDefault();
			var status = ExternalTranHelper.GetSharedTranStatus(Base, tran);
			return status == ExternalTranHelper.SharedTranStatus.Synchronized;
		}

		protected override void BeforeCapturePayment(ARPayment doc)
		{
			base.BeforeCapturePayment(doc);
			ARPaymentEntry.CheckValidPeriodForCCTran(this.Base, doc);
			if (doc.Voided == true)
			{
				string docTypeRefNbr = doc.DocType + doc.RefNbr;
				throw new PXException(Messages.PaymentIsVoided, docTypeRefNbr);
			}
			ReleaseDoc = NeedRelease(doc);
		}

		protected override void BeforeCreditPayment(ARPayment doc)
		{
			base.BeforeCapturePayment(doc);
			ARPaymentEntry.CheckValidPeriodForCCTran(this.Base, doc);
			ReleaseDoc = NeedRelease(doc);
		}

		protected override void BeforeCaptureOnlyPayment(ARPayment doc)
		{
			base.BeforeCaptureOnlyPayment(doc);
			ReleaseDoc = NeedRelease(doc);
		}

		protected override void BeforeVoidPayment(ARPayment doc)
		{
			base.BeforeVoidPayment(doc);
			ICCPayment pDoc = GetPaymentDoc(doc);
			ReleaseDoc = NeedRelease(doc) && ARPaymentType.VoidAppl(pDoc.DocType) == true;
		}

		protected override AfterProcessingManager GetAfterProcessingManager()
		{
			return GetARPaymentAfterProcessingManager();
		}

		protected override AfterProcessingManager GetAfterProcessingManager(ARPaymentEntry graph)
		{
			var manager = GetARPaymentAfterProcessingManager();
			manager.Graph = graph;
			return manager;
		}

		protected override ARPayment SetCurrentDocument(ARPaymentEntry graph, ARPayment doc)
		{
			var document = graph.Document;
			document.Current = document.Search<ARPayment.refNbr>(doc.RefNbr, doc.DocType);
			return document.Current;
		}

		protected override PaymentTransactionAcceptFormGraph<ARPaymentEntry, ARPayment> GetPaymentTransactionAcceptFormExt(ARPaymentEntry graph)
		{
			return graph.GetExtension<ARPaymentEntryPaymentTransaction>();
		}

		protected override PaymentTransactionGraph<ARPaymentEntry, ARPayment> GetPaymentTransactionExt(ARPaymentEntry graph)
		{
			return graph.GetExtension<ARPaymentEntryPaymentTransaction>();
		}

		private ARPaymentAfterProcessingManager GetARPaymentAfterProcessingManager()
		{
			return new ARPaymentAfterProcessingManager()
			{
				ReleaseDoc = true,
				RaisedVoidForReAuthorization = RaisedVoidForReAuthorization,
				NeedSyncContext = IsNeedSyncContext
			};
		}

		protected override PaymentTransactionDetailMapping GetPaymentTransactionMapping()
		{
			return new PaymentTransactionDetailMapping(typeof(CCProcTran));
		}

		protected override PaymentMapping GetPaymentMapping()
		{
			return new PaymentMapping(typeof(ARPayment));
		}

		protected override ExternalTransactionDetailMapping GetExternalTransactionMapping()
		{
			return new ExternalTransactionDetailMapping(typeof(ExternalTransaction));
		}

		protected override void SetSyncLock(ARPayment doc)
		{
			try
			{
				base.SetSyncLock(doc);
				if (doc.SyncLock.GetValueOrDefault() == false)
				{
					CheckSyncLockOnPersist = false;
					var paymentCache = Base.Caches[typeof(ARPayment)];
					paymentCache.SetValue<ARPayment.syncLock>(doc, true);
					paymentCache.SetValue<ARPayment.syncLockReason>(doc, ARPayment.syncLockReason.NewCard);
					paymentCache.Update(doc);
					Base.Actions.PressSave();
				}
			}
			finally
			{
				CheckSyncLockOnPersist = true;
			}
		}

		protected override void RemoveSyncLock(ARPayment doc)
		{
			try
			{
				base.RemoveSyncLock(doc);
				if (doc.SyncLock == true)
				{
					CheckSyncLockOnPersist = false;
					var paymentCache = Base.Caches[typeof(ARPayment)];
					paymentCache.SetValue<ARPayment.syncLock>(doc, false);
					paymentCache.SetValue<ARPayment.syncLockReason>(doc, null);
					paymentCache.Update(doc);
					Base.Actions.PressSave();
				}
			}
			finally
			{
				CheckSyncLockOnPersist = true;
			}
		}

		protected override bool LockExists(ARPayment doc)
		{
			ARPayment sDoc = new PXSelectReadonly<ARPayment,
				Where<ARPayment.noteID, Equal<Required<ARPayment.noteID>>>>(Base).SelectSingle(doc.NoteID);
			return sDoc?.SyncLock == true;
		}

		private bool HasProcCenterSupportingUnlinkedMode(PXCache cache, ARPayment doc)
		{
			CCProcessingCenter procCenter = PXSelectorAttribute.SelectAll<ARPayment.processingCenterID>(cache, doc)
				.RowCast<CCProcessingCenter>().FirstOrDefault(i => i.AllowUnlinkedRefund == true);
			return procCenter != null;
		}

		private bool NeedRelease(ARPayment doc)
		{
			return doc.Released == false && CCProcessingHelper.IntegratedProcessingActivated(ARSetup.Current);
		}

		private void SetUsingAcceptHostedForm(ARPayment doc)
		{
			SelectedBAccount = Base.customer.Current?.BAccountID;
			SelectedPaymentMethod = Base.Document.Current?.PaymentMethodID;
			CCProcessingCenter procCenter = Base.processingCenter.SelectSingle();
			SelectedProcessingCenter = procCenter?.ProcessingCenterID;
			SelectedProcessingCenterType = procCenter?.ProcessingTypeName;
			DocNoteId = Base.Document.Current?.NoteID;
			EnableMobileMode = Base.IsMobile;

			if (doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
			{
				UseAcceptHostedForm = false;
			}
			else
			{
				if (procCenter?.UseAcceptPaymentForm == true && !ExternalTranHelper.HasSuccessfulTrans(Base.ExternalTran))
				{
					UseAcceptHostedForm = true;
				}
				else
				{
					UseAcceptHostedForm = false;
				}
			}
		}

		private bool GettingDetailsByTranSupported(ARPayment doc)
		{
			return CCProcessingFeatureHelper.IsFeatureSupported(GetProcessingCenterById(doc.ProcessingCenterID), CCProcessingFeature.TransactionGetter, false);
		}

		private void SetVisibilityCreditCardControlsForRefund(PXCache cache, ARPayment doc)
		{
			var storedTran = RefTranExtNbrAttribute.GetStoredTran(doc.RefTranExtNbr, Base, cache);
			bool transactionMode = doc.CCTransactionRefund == true;
			if (storedTran != null)
			{
				bool isNewProfile = storedTran.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile;
				PXUIFieldAttribute.SetEnabled<ARPayment.processingCenterID>(cache, doc, false);
				PXUIFieldAttribute.SetVisible<ARPayment.processingCenterID>(cache, doc, isNewProfile);
				PXUIFieldAttribute.SetVisible<ARPayment.pMInstanceID>(cache, doc, !isNewProfile);
			}
			else
			{
				bool pmtWithoutCpm = doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile;
				PXUIFieldAttribute.SetVisible<ARPayment.processingCenterID>(cache, doc, pmtWithoutCpm && transactionMode);
				PXUIFieldAttribute.SetVisible<ARPayment.pMInstanceID>(cache, doc, !pmtWithoutCpm || !transactionMode);
			}
			PXUIFieldAttribute.SetEnabled<ARPayment.pMInstanceID>(cache, doc, Base.IsContractBasedAPI || !transactionMode);
		}

		private string GetPaymentStateDescr(ExternalTransactionState state)
		{
			return GetLastTransactionDescription();
		}

		protected void SetPendingProcessingIfNeeded(PXCache sender, ARPayment document)
		{
			PaymentMethod pm = new PXSelect<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>(Base)
				.SelectSingle(document.PaymentMethodID);
			bool pendingProc = false;
			if (CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(pm) && document.Released == false)
			{
				if (document.DocType == ARDocType.VoidPayment)
				{
					var trans = Base.ccProcTran.Select().RowCast<CCProcTran>();
					var extTrans = Base.ExternalTran.Select().RowCast<ExternalTransaction>();
					var extTran = ExternalTranHelper.GetLastProcessedExtTran(extTrans, trans);
					if (extTran != null && ExternalTranHelper.GetTransactionState(Base, extTran).IsActive)
					{
						pendingProc = true;
					}
				}
				else
				{
					pendingProc = true;
				}
			}
			sender.SetValue<ARRegister.pendingProcessing>(document, pendingProc);
		}

		protected virtual void CheckProcessingCenter(PXCache cache, ARPayment doc)
		{
			if (doc == null)
				return;

			PXEntryStatus status = cache.GetStatus(doc);
			PaymentMethod pm = this.Base.paymentmethod.Current;
			if (doc != null && doc.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
			{
				CustomerPaymentMethod cpm = GetCustomerPaymentMethodById(doc.PMInstanceID);
				if (cpm?.CCProcessingCenterID != null)
				{
					doc.ProcessingCenterID = cpm.CCProcessingCenterID;
				}
			}

			if (doc != null && doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile
				&& doc.ProcessingCenterID != null && status == PXEntryStatus.Inserted)
			{
				IEnumerable<CCProcessingCenter> availableProcCenters = PXSelectorAttribute.SelectAll<ARPayment.processingCenterID>(cache, doc)
					.RowCast<CCProcessingCenter>();
				bool exists = availableProcCenters.Any(i => i.ProcessingCenterID == doc.ProcessingCenterID);
				if (!exists)
				{
					throw new PXException(ErrorMessages.ElementDoesntExist, nameof(ARPayment.ProcessingCenterID));
				}
			}

			bool docIsMemoOrBalanceWO = doc.DocType == ARDocType.CreditMemo || doc.DocType == ARDocType.SmallBalanceWO;
			bool validCreditCardPM = pm?.PaymentType == PaymentMethodType.CreditCard
				&& pm?.IsAccountNumberRequired == true;
			if (doc.DocType == ARDocType.Refund && doc.CCTransactionRefund == false && validCreditCardPM)
			{
				PXDefaultAttribute.SetPersistingCheck<ARPayment.pMInstanceID>(cache, doc, PXPersistingCheck.NullOrBlank);
			}
			else if (doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile && validCreditCardPM && !docIsMemoOrBalanceWO)
			{
				PXDefaultAttribute.SetPersistingCheck<ARPayment.processingCenterID>(cache, doc, PXPersistingCheck.NullOrBlank);
			}
			else
			{
				bool isAccountNumberRequired = Base.paymentmethod.Current?.IsAccountNumberRequired ?? false;
				PXDefaultAttribute.SetPersistingCheck<ARPayment.pMInstanceID>(cache, doc, !docIsMemoOrBalanceWO && isAccountNumberRequired ? PXPersistingCheck.NullOrBlank
					: PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<ARPayment.processingCenterID>(cache, doc, PXPersistingCheck.Nothing);
			}
		}

		private CustomerPaymentMethod GetCustomerPaymentMethodById(int? pmInstanceId)
		{
			CustomerPaymentMethodRepository repo = new CustomerPaymentMethodRepository(Base);
			CustomerPaymentMethod cpm = repo.GetCustomerPaymentMethod(pmInstanceId);
			return cpm;
		}

		private void CheckSyncLock(ARPayment payment)
		{
			bool paymentCreatedByApiWithSyncLock = Base.IsContractBasedAPI &&
				Base.Document.Cache.GetStatus(payment) == PXEntryStatus.Inserted &&
				payment?.SyncLockReason == ARPayment.syncLockReason.NeedValidation;

			if (CheckSyncLockOnPersist && payment.SyncLock == true && !paymentCreatedByApiWithSyncLock)
			{
				if (CCProcessingHelper.IntegratedProcessingActivated(Base.arsetup.Current))
				{
					throw new PXException(Messages.ERR_CCProcessingARPaymentSyncLock);
				}
				else
				{
					WebDialogResult result = Base.Document.Ask(Messages.CCProcessingARPaymentSyncWarning, MessageButtons.YesNo);
					if (result == WebDialogResult.Yes)
					{
						payment.SyncLock = false;
					}
					else
					{
						throw new PXException(Messages.CCProcessingOperationCancelled);
					}
				}
			}
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.paymentMethodID> e)
		{
			PXCache cache = e.Cache;
			ARPayment payment = e.Row as ARPayment;
			if (payment == null) return;
			cache.SetDefaultExt<ARPayment.cCTransactionRefund>(payment);
			cache.SetValueExt<ARPayment.saveCard>(payment, false);
			cache.SetValueExt<ARPayment.processingCenterID>(payment, null);
			if (payment.DocType == ARDocType.Refund)
			{
				cache.SetValueExt<ARPayment.refTranExtNbr>(payment, null);
			}
			else
			{
				object retVal;
				cache.RaiseFieldDefaulting<ARPayment.pMInstanceID>(payment, out retVal);
				if (retVal == null)
				{
					PaymentMethod pm = Base.paymentmethod?.Select();
					CCProcessingCenter procCenter = Base.processingCenter?.Select();
					if (payment != null && pm != null && procCenter != null && Base.ShowCardChck(payment))
					{
						int availableCnt = PXSelectorAttribute.SelectAll<ARPayment.pMInstanceID>(cache, payment).Count;
						if (availableCnt == 0)
						{
							cache.SetValuePending<ARPayment.newCard>(payment, true);
						}
					}
				}
			}
			SetPendingProcessingIfNeeded(cache, payment);
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.processingCenterID> e)
		{
			ARPayment payment = e.Row as ARPayment;
			if (payment == null) return;
			if (payment.ProcessingCenterID != null && e.ExternalCall)
			{
				e.Cache.SetValueExt<ARPayment.pMInstanceID>(payment, PaymentTranExtConstants.NewPaymentProfile);
			}
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.pMInstanceID> e)
		{
			ARPayment payment = e.Row as ARPayment;
			if (payment == null) return;
			if (payment.PMInstanceID != null && e.ExternalCall)
			{
				e.Cache.SetValueExt<ARPayment.processingCenterID>(payment, null);
			}
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.newCard> e)
		{
			ARPayment payment = e.Row as ARPayment;
			PXCache cache = e.Cache;
			bool? newValue = e.NewValue as bool?;
			if (payment == null) return;

			if (e.ExternalCall == true && payment != null)
			{
				if (newValue == true)
				{
					EnableNewCardMode(payment, cache);
					if (ForceSaveCard(payment))
					{
						cache.SetValueExt<ARPayment.saveCard>(payment, true);
					}
				}
				else
				{
					DisableNewCardMode(payment, cache);
				}
			}
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.refTranExtNbr> e)
		{
			ARPayment payment = e.Row as ARPayment;
			PXCache cache = e.Cache;
			if (payment == null) return;

			var val = e.NewValue as string;
			if (!string.IsNullOrEmpty(val))
			{
				var extTran = RefTranExtNbrAttribute.GetStoredTran(payment.RefTranExtNbr, Base, cache);
				if (extTran == null)
				{
					EnableNewCardMode(payment, cache);
				}
				else
				{
					if (extTran.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
					{
						DisableNewCardMode(payment, cache);
						cache.SetValueExt<ARPayment.pMInstanceID>(payment, extTran.PMInstanceID);
					}
					else
					{
						EnableNewCardMode(payment, cache);
						cache.SetValueExt<ARPayment.processingCenterID>(payment, extTran.ProcessingCenterID);
					}
				}

			}
			else
			{
				EnableNewCardMode(payment, cache);
			}
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARPayment.cCTransactionRefund> e)
		{
			ARPayment payment = e.Row as ARPayment;
			PXCache cache = e.Cache;
			bool? newVal = e.NewValue as bool?;
			if (payment == null) return;

			if (payment.DocType == ARDocType.Refund)
			{
				if (newVal == true)
				{
					EnableNewCardMode(payment, cache);
				}
				else
				{
					cache.SetValueExt<ARPayment.refTranExtNbr>(payment, null);
					DisableNewCardMode(payment, cache);
				}
			}
		}

		protected virtual void FieldVerifying(Events.FieldVerifying<ARPayment.adjDate> e)
		{
			ARPayment doc = e.Row as ARPayment;
			if (doc == null) return;
			if (e.ExternalCall && doc.AdjDate.HasValue && doc.Released == false
				&& doc.AdjDate.Value.CompareTo(e.NewValue) != 0)
			{
				IExternalTransaction extTran = Base.ExternalTran.SelectSingle();
				if (extTran != null)
				{
					ExternalTransactionState state = ExternalTranHelper.GetTransactionState(Base, extTran);
					if (IsDocTypePayment(doc) && state.IsSettlementDue)
					{
						throw new PXSetPropertyException(Messages.ApplicationAndCaptureDatesDifferent);
					}
					if (doc.DocType == ARDocType.Refund && state.IsSettlementDue)
					{
						throw new PXSetPropertyException(Messages.ApplicationAndVoidRefundDatesDifferent);
					}
					if (doc.DocType == ARDocType.VoidPayment && state.IsVoided)
					{
						throw new PXSetPropertyException(Messages.ApplicationAndVoidRefundDatesDifferent);
					}
				}
			}
		}

		private void DisableNewCardMode(ARPayment payment, PXCache cache)
		{
			cache.SetDefaultExt<ARPayment.pMInstanceID>(payment);
			cache.SetValueExt<ARPayment.processingCenterID>(payment, null);
			cache.SetValueExt<ARPayment.saveCard>(payment, false);
		}

		private void EnableNewCardMode(ARPayment payment, PXCache cache)
		{
			cache.SetValueExt<ARPayment.pMInstanceID>(payment, PaymentTranExtConstants.NewPaymentProfile);
			cache.SetDefaultExt<ARPayment.processingCenterID>(payment);
		}

		#region CacheAttached
		[PXDBDefault(typeof(ARRegister.docType))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void CCProcTran_DocType_CacheAttached(PXCache sender) { }


		[PXDBDefault(typeof(ARRegister.refNbr))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void CCProcTran_RefNbr_CacheAttached(PXCache sender) { }

		[PXDBDefault(typeof(ARRegister.docType))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void ExternalTransaction_DocType_CacheAttached(PXCache sender) { }

		[PXDBDefault(typeof(ARRegister.refNbr))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void ExternalTransaction_RefNbr_CacheAttached(PXCache sender) { }

		[PXDBDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void ExternalTransaction_VoidDocType_CacheAttached(PXCache sender) { }

		[PXDBDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void ExternalTransaction_VoidRefNbr_CacheAttached(PXCache sender) { }

		[PXDBString(15, IsUnicode = true)]
		[PXDBDefault(typeof(ARRegister.refNbr))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void CCBatchTransaction_RefNbr_CacheAttached(PXCache sender) { }

		[PXDBDefault(typeof(ExternalTransaction.transactionID))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void CCBatchTransaction_TransactionID_CacheAttached(PXCache sender) { }
		#endregion
	}
}
