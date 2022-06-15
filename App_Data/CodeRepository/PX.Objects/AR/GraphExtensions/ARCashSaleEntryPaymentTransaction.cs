using PX.Data;
using PX.Objects;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.Standalone;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.Extensions.PaymentTransaction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AR.GraphExtensions
{
	public class ARCashSaleEntryPaymentTransaction : PaymentTransactionGraph<ARCashSaleEntry, ARCashSale>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.integratedCardProcessing>();

		public PXSelect<ExternalTransaction> externalTran;

		protected override PaymentTransactionDetailMapping GetPaymentTransactionMapping()
		{
			return new PaymentTransactionDetailMapping(typeof(CCProcTran));
		}

		protected override ExternalTransactionDetailMapping GetExternalTransactionMapping()
		{
			return new ExternalTransactionDetailMapping(typeof(ExternalTransaction));
		}

		protected override PaymentMapping GetPaymentMapping()
		{
			return new PaymentMapping(typeof(ARCashSale));
		}

		protected override void MapViews(ARCashSaleEntry graph)
		{
			this.PaymentTransaction = new PXSelectExtension<PaymentTransactionDetail>(Base.ccProcTran);
			this.ExternalTransaction = new PXSelectExtension<ExternalTransactionDetail>(Base.ExternalTran);
		}

		protected override void BeforeVoidPayment(ARCashSale doc)
		{
			base.BeforeVoidPayment(doc);
			ReleaseDoc = doc.VoidAppl == true && doc.Released == false && this.ARSetup.Current.IntegratedCCProcessing == true;
		}

		protected override void BeforeCapturePayment(ARCashSale doc)
		{
			base.BeforeCapturePayment(doc);
			ReleaseDoc = doc.Released == false && ARSetup.Current.IntegratedCCProcessing == true;
		}

		protected override void BeforeCreditPayment(ARCashSale doc)
		{
			base.BeforeCreditPayment(doc);
			ReleaseDoc = doc.Released == false && ARSetup.Current.IntegratedCCProcessing == true;
		}

		protected override AfterProcessingManager GetAfterProcessingManager(ARCashSaleEntry graph)
		{
			var manager = GetARCashSaleAfterProcessingManager();
			manager.Graph = graph;
			return manager;
		}

		protected override AfterProcessingManager GetAfterProcessingManager()
		{
			return GetARCashSaleAfterProcessingManager();
		}

		private ARCashSaleAfterProcessingManager GetARCashSaleAfterProcessingManager()
		{
			return new ARCashSaleAfterProcessingManager() { ReleaseDoc = true };
		}

		protected override void RowSelected(Events.RowSelected<ARCashSale> e)
		{
			base.RowSelected(e);
			ARCashSale doc = e.Row;
			if (doc == null)
				return;
			TranHeldwarnMsg = AR.Messages.CCProcessingARPaymentTranHeldWarning;
			PXCache cache = e.Cache;
			bool docTypePayment = IsDocTypePayment(doc);

			bool isPMInstanceRequired = false;
			if (!string.IsNullOrEmpty(doc.PaymentMethodID))
			{
				isPMInstanceRequired = Base.paymentmethod.Current?.IsAccountNumberRequired ?? false;
			}

			ExternalTransactionState tranState = GetActiveTransactionState();
			bool canAuthorize = doc.Hold == false && docTypePayment && !(tranState.IsPreAuthorized || tranState.IsCaptured);
			bool canCapture = doc.Hold == false && docTypePayment && !tranState.IsCaptured;
			bool canVoid = doc.Hold == false && (doc.DocType == ARDocType.CashReturn && (tranState.IsCaptured || tranState.IsPreAuthorized)) ||
						   (tranState.IsPreAuthorized && docTypePayment);
			bool canCredit = doc.Hold == false && doc.DocType == ARDocType.CashReturn && doc.Status != ARDocStatus.Closed && !tranState.IsRefunded;

			CCProcessingCenter procCenter = GetPaymentRepository().GetCCProcessingCenter(doc.ProcessingCenterID);
			bool canAuthorizeIfExtAuthOnly = procCenter?.IsExternalAuthorizationOnly == false;
			bool canCaptureIfExtAuthOnly = canAuthorizeIfExtAuthOnly || procCenter?.IsExternalAuthorizationOnly == true && tranState.IsActive == true;

			SelectedProcessingCenterType = procCenter?.ProcessingTypeName;
			bool enableCCProcess = EnableCCProcess(doc);

			this.authorizeCCPayment.SetEnabled(enableCCProcess && canAuthorize && canAuthorizeIfExtAuthOnly);
			this.captureCCPayment.SetEnabled(enableCCProcess && canCapture && canCaptureIfExtAuthOnly);
			this.voidCCPayment.SetEnabled(enableCCProcess && canVoid);
			this.creditCCPayment.SetEnabled(enableCCProcess && canCredit);
			doc.CCPaymentStateDescr = GetPaymentStateDescr(tranState);

			bool canValidate = false;
			if (enableCCProcess)
			{
				canValidate = CanValidate(doc);
			}
			this.validateCCPayment.SetEnabled(canValidate);

			this.recordCCPayment.SetEnabled(false);
			this.recordCCPayment.SetVisible(false);
			this.captureOnlyCCPayment.SetEnabled(false);
			this.captureOnlyCCPayment.SetVisible(false);

			PXUIFieldAttribute.SetRequired<ARCashSale.extRefNbr>(cache, enableCCProcess || ARSetup.Current.RequireExtRef == true);
			PXUIFieldAttribute.SetVisible<ARCashSale.cCPaymentStateDescr>(cache, doc, enableCCProcess && doc.CCPaymentStateDescr != null);
			PXUIFieldAttribute.SetVisible<ARCashSale.refTranExtNbr>(cache, doc, ((doc.DocType == ARDocType.CashReturn) && enableCCProcess));
			PXUIFieldAttribute.SetRequired<ARPayment.pMInstanceID>(cache, isPMInstanceRequired);
			PXDefaultAttribute.SetPersistingCheck<ARPayment.pMInstanceID>(cache, doc, isPMInstanceRequired ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			if (doc.Released == true || doc.Voided == true)
			{
				cache.AllowUpdate = enableCCProcess;
			}
			else if (enableCCProcess && (tranState.IsPreAuthorized || tranState.IsCaptured
				|| (doc.DocType == ARDocType.CashReturn && (tranState.IsRefunded || CheckLastProcessedTranIsVoided(doc)))))
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				if (doc.Status != ARDocStatus.PendingApproval)
				{
					PXUIFieldAttribute.SetEnabled<ARCashSale.adjDate>(cache, doc, true);
					PXUIFieldAttribute.SetEnabled<ARCashSale.adjFinPeriodID>(cache, doc, true);
				}
				PXUIFieldAttribute.SetEnabled<ARCashSale.hold>(cache, doc, true);
				//calculate only on data entry, differences from the applications will be moved to RGOL upon closure
				PXDBCurrencyAttribute.SetBaseCalc<ARCashSale.curyDocBal>(cache, null, true);
				PXDBCurrencyAttribute.SetBaseCalc<ARCashSale.curyDiscBal>(cache, null, true);

				cache.AllowDelete = false;
				cache.AllowUpdate = true;
				Base.Transactions.Cache.AllowDelete = true;
				Base.Transactions.Cache.AllowUpdate = true;
				Base.Transactions.Cache.AllowInsert = doc.CustomerID != null && doc.CustomerLocationID != null;
				Base.release.SetEnabled(doc.Hold == false);
				Base.voidCheck.SetEnabled(false);
			}
			else
			{

				PXUIFieldAttribute.SetEnabled<ARCashSale.refTranExtNbr>(cache, doc, enableCCProcess && ((doc.DocType == ARDocType.CashReturn) && !tranState.IsRefunded));
				PXUIFieldAttribute.SetEnabled<ARPayment.pMInstanceID>(cache, doc, isPMInstanceRequired);
				cache.AllowDelete = !ExternalTranHelper.HasTransactions(Base.ExternalTran);
			}

			#region CCProcessing integrated with doc
			if (enableCCProcess && CCProcessingHelper.IntegratedProcessingActivated(ARSetup.Current))
			{
				if (doc.Released == false)
				{
					bool releaseActionEnabled = doc.Hold == false &&
												doc.OpenDoc == true &&
											   (doc.DocType == ARDocType.CashReturn ? tranState.IsRefunded : tranState.IsCaptured);

					Base.release.SetEnabled(releaseActionEnabled);
				}
			}
			#endregion

			PXUIFieldAttribute.SetEnabled<ARCashSale.docType>(cache, doc, true);
			PXUIFieldAttribute.SetEnabled<ARCashSale.refNbr>(cache, doc, true);
			ShowWarningIfExternalAuthorizationOnly(e, doc);
		}

		protected virtual void ShowWarningIfExternalAuthorizationOnly(Events.RowSelected<ARCashSale> e, ARCashSale doc)
		{
			ExternalTransactionState state = GetActiveTransactionState();
			CCProcessingCenter procCenter = GetPaymentRepository().GetCCProcessingCenter(doc.ProcessingCenterID);
			CustomerPaymentMethod cpm = GetPaymentRepository().GetCustomerPaymentMethod(doc.PMInstanceID);

			bool IsExternalAuthorizationOnly = procCenter?.IsExternalAuthorizationOnly == true && !state.IsActive
												&& doc.Status == ARDocStatus.CCHold && doc.DocType == Standalone.ARCashSaleType.CashSale;

			UIState.RaiseOrHideErrorByErrorLevelPriority<ARCashSale.pMInstanceID>(e.Cache, e.Row, IsExternalAuthorizationOnly,
				Messages.CardAssociatedWithExternalAuthorizationOnlyProcessingCenter, PXErrorLevel.Warning, cpm?.Descr, procCenter?.ProcessingCenterID);
		}

		protected virtual void FieldUpdated(Events.FieldUpdated<ARCashSale.paymentMethodID> e)
		{
			PXCache cache = e.Cache;
			ARCashSale cashSale = e.Row as ARCashSale;
			if (cashSale == null) return;
			SetPendingProcessingIfNeeded(cache, cashSale);
		}

		public static bool IsDocTypeSuitableForCC(ARCashSale doc)
		{
			bool isDocTypeSuitableForCC = (doc.DocType == ARDocType.CashSale) || (doc.DocType == ARDocType.CashReturn);
			return isDocTypeSuitableForCC;
		}

		public static bool IsDocTypePayment(ARCashSale doc)
		{
			bool docTypePayment = doc.DocType == ARDocType.CashSale;
			return docTypePayment;
		}

		public bool EnableCCProcess(ARCashSale doc)
		{
			bool enableCCProcess = false;

			if (doc.IsMigratedRecord != true &&
				Base.paymentmethod.Current != null &&
				Base.paymentmethod.Current.PaymentType == CA.PaymentMethodType.CreditCard)
			{
				enableCCProcess = IsDocTypeSuitableForCC(doc);
			}
			enableCCProcess &= !doc.Voided.Value;

			bool disabledProcCenter = IsProcCenterDisabled(SelectedProcessingCenterType);
			enableCCProcess &= !disabledProcCenter;

			return enableCCProcess;
		}

		public bool CanValidate(ARCashSale doc)
		{
			bool enableCCProcess = EnableCCProcess(doc);

			if (!enableCCProcess)
				return false;

			ExternalTransactionState tranState = GetActiveTransactionState();
			bool canValidate = doc.Hold == false && IsDocTypePayment(doc) && tranState.IsActive;
			if (!canValidate)
				return false;

			if (!canValidate)
			{
				var manager = GetAfterProcessingManager(Base);
				canValidate = manager != null && !manager.CheckDocStateConsistency(doc);
			}

			canValidate = canValidate && GettingDetailsByTranSupported();

			return canValidate;
		}

		private string GetPaymentStateDescr(ExternalTransactionState state)
		{
			return GetLastTransactionDescription();
		}

		private bool GettingDetailsByTranSupported()
		{
			CCProcessingCenter procCenter = Base.ProcessingCenter.SelectSingle();
			return CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.TransactionGetter, false);
		}

		protected void SetPendingProcessingIfNeeded(PXCache sender, ARCashSale document)
		{
			PaymentMethod pm = new PXSelect<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>(Base)
				.SelectSingle(document.PaymentMethodID);
			bool pendingProc = false;
			if (CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(pm) && document.Released == false)
			{
				pendingProc = true;
			}
			sender.SetValue<ARRegister.pendingProcessing>(document, pendingProc);
		}

		protected override ARCashSale SetCurrentDocument(ARCashSaleEntry graph, ARCashSale doc)
		{
			var document = graph.Document;
			document.Current = document.Search<ARCashSale.refNbr>(doc.RefNbr, doc.DocType);
			return document.Current;
		}

		protected override PaymentTransactionGraph<ARCashSaleEntry, ARCashSale> GetPaymentTransactionExt(ARCashSaleEntry graph)
		{
			return graph.GetExtension<ARCashSaleEntryPaymentTransaction>();
		}

		private bool CheckLastProcessedTranIsVoided(ARCashSale cashSale)
		{
			var extTrans = GetExtTrans();
			var externalTran = ExternalTranHelper.GetLastProcessedExtTran(extTrans, GetProcTrans());
			
			bool ret = false;
			var transaction = extTrans.Where(i => i.TransactionID == externalTran.TransactionID).FirstOrDefault();
			if (transaction != null)
			{
				var state = ExternalTranHelper.GetTransactionState(Base, transaction);
				ret = state.IsVoided;
			}
			return ret;
		}

	}
}
