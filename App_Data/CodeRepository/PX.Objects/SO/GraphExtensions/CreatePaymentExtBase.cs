using PX.CCProcessingBase;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.AR.GraphExtensions;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.Common.Abstractions;
using PX.Objects.CS;
using PX.Objects.Extensions.PaymentTransaction;
using PX.Objects.SO.DAC;
using PX.Objects.SO.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO.GraphExtensions
{
	public abstract class CreatePaymentExtBase<TGraph, TDocument, TAdjust> : CreatePaymentExtBase<TGraph, TGraph, TDocument, TAdjust, TAdjust>
		where TGraph : PXGraph<TGraph, TDocument>, new()
		where TDocument : class, IBqlTable, ICreatePaymentDocument, new()
		where TAdjust : class, IBqlTable, ICreatePaymentAdjust, new()
	{
	}

	public abstract class CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust> : PXGraphExtension<TGraph>
		where TGraph : PXGraph<TFirstGraph, TDocument>, new()
		where TFirstGraph : PXGraph
		where TDocument : class, IBqlTable, ICreatePaymentDocument, new()
		where TDocumentAdjust : class, IBqlTable, ICreatePaymentAdjust, new()
		where TPaymentAdjust : class, IBqlTable, new()
	{
		#region Types

		protected class EndPaymentOperationCustomInfo : IPXCustomInfo
		{
			protected string _adjgDocType;
			protected string _adjgRefNbr;
			protected PXBaseRedirectException _redirectException;
			protected Exception _processPaymentException;
			protected TDocumentAdjust _failedPayment;

			public EndPaymentOperationCustomInfo()
			{
			}

			public EndPaymentOperationCustomInfo(string adjgDocType, string adjgRefNbr, PXBaseRedirectException redirectException)
			{
				_adjgDocType = adjgDocType;
				_adjgRefNbr = adjgRefNbr;
				_redirectException = redirectException;
			}

			public EndPaymentOperationCustomInfo(TDocumentAdjust failedPayment, Exception processPaymentException)
			{
				_failedPayment = failedPayment;
				_processPaymentException = processPaymentException;
			}

			public void Complete(PXLongRunStatus status, PXGraph graph)
			{
				if (status.IsNotIn(PXLongRunStatus.Completed, PXLongRunStatus.Aborted))
					return;

				var createPaymentExt = graph.FindImplementation
					<CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust>>();

				if (createPaymentExt == null)
				{
					if (_redirectException != null)
						throw _redirectException;

					if (_processPaymentException != null)
						throw _processPaymentException;

					return;
				}

				// Clear adjust query cache to show payments correctly after void / capture.
				createPaymentExt.GetAdjustView().Cache.ClearQueryCache();

				if (status == PXLongRunStatus.Completed)
				{
					if (_adjgDocType != null && _adjgRefNbr != null)
					{
						createPaymentExt.QuickPayment.Current.AdjgDocType = _adjgDocType;
						createPaymentExt.QuickPayment.Current.AdjgRefNbr = _adjgRefNbr;
					}

					if (_redirectException != null)
						throw _redirectException;
				}
				else if (_failedPayment != null && _processPaymentException != null)
				{
					createPaymentExt.CopyError(_failedPayment, _processPaymentException);
				}
			}
		}

		public class LongOperationParameterBase
		{
			public TDocument Document { get; set; }
		}

		public class SyncPaymentTransactionParameter : LongOperationParameterBase
		{
			public string CancelStr { get; set; }
			public string TranResponseStr { get; set; }
			public string AdjgRefNbr { get; set; }
			public string AdjgDocType { get; set; }
		}

		public class CreatePaymentParameter : LongOperationParameterBase
		{
			public string CcPaymentConnectorUrl { get; set; }
			public SOQuickPayment QuickPayment { get; set; }
			public string PaymentType { get; set; }
			public bool ThrowErrors { get; set; }
		}

		public class ImportPaymentParameter : LongOperationParameterBase
		{
			public SOImportExternalTran Panel { get; set; }
		}

		public class ExecutePaymentTransactionActionParameter : LongOperationParameterBase
		{
			public TDocumentAdjust Adjustment { get; set; }

			public Action<CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust>,
				TDocumentAdjust, ARPaymentEntry, ARPaymentEntryPaymentTransaction> PaymentAction { get; set; }
		}

		#endregion

		[PXCopyPasteHiddenView]
		public PXFilter<SOQuickPayment> QuickPayment;

		[PXCopyPasteHiddenView]
		public PXFilter<SOImportExternalTran> ImportExternalTran;

		[PXCopyPasteHiddenView]
		public PXSelect<ARPaymentTotals> PaymentTotals;

		#region Helpers

		protected virtual DAC GetCurrent<DAC>() where DAC : class, IBqlTable, new()
			=> (DAC)Base.Caches<DAC>().Current;

		protected static CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust> CreateNewGraph(TDocument document)
		{
			var createPaymentExt = PXGraph.CreateInstance<TGraph>().FindImplementation
				<CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust>>();

			createPaymentExt.SetCurrentDocument(document);
			return createPaymentExt;
		}

		#endregion // Helpers

		#region Initialize

		public override void Initialize()
		{
			base.Initialize();

			Base.Views.Caches.Remove(QuickPayment.GetItemType());
			Base.Views.Caches.Remove(ImportExternalTran.GetItemType());

			if (!GetAdjustView().Cache.Fields.Contains(nameof(CanVoid)))
			{
				GetAdjustView().Cache.Fields.Add(nameof(CanVoid));

				Base.FieldSelecting.AddHandler(typeof(TDocumentAdjust), nameof(CanVoid),
					CreateVoidCaptureFieldSelecting(nameof(CanVoid), CanVoid));
			}

			if (!GetAdjustView().Cache.Fields.Contains(nameof(CanCapture)))
			{
				GetAdjustView().Cache.Fields.Add(nameof(CanCapture));

				Base.FieldSelecting.AddHandler(typeof(TDocumentAdjust), nameof(CanCapture),
					CreateVoidCaptureFieldSelecting(nameof(CanCapture), CanCapture));
			}

			Type paymentMethodField = GetPaymentMethodField();
			Base.FieldVerifying.AddHandler(BqlCommand.GetItemType(paymentMethodField), paymentMethodField.Name, PaymentMethodFieldVerifying);
		}

		protected virtual void InitializeQuickPaymentPanel(PXGraph graph, string viewName)
		{
			SetDefaultValues(QuickPayment.Current, GetCurrent<TDocument>());
			QuickPayment.Cache.RaiseRowSelected(QuickPayment.Current);
		}

		public virtual void SetDefaultValues(SOQuickPayment payment, TDocument document)
		{
			ClearQuickPayment(payment);

			if (document != null)
			{
				PXCache paymentCache = QuickPayment.Cache;
				paymentCache.SetValueExt<SOQuickPayment.paymentMethodID>(payment, document.PaymentMethodID);
				paymentCache.SetValueExt<SOQuickPayment.cashAccountID>(payment, document.CashAccountID);
				paymentCache.SetValueExt<SOQuickPayment.docDesc>(payment, GetDocumentDescr(document));
				if (document.PMInstanceID != null)
					paymentCache.SetValueExt<SOQuickPayment.pMInstanceID>(payment, document.PMInstanceID);
				paymentCache.SetDefaultExt<SOQuickPayment.curyOrigDocAmt>(payment);
				paymentCache.SetDefaultExt<SOQuickPayment.curyRefundAmt>(payment);
				paymentCache.SetDefaultExt<SOQuickPayment.newCard>(payment);
				paymentCache.SetDefaultExt<SOQuickPayment.saveCard>(payment);
				paymentCache.SetDefaultExt<SOQuickPayment.refTranExtNbr>(payment);
			}
		}

		protected virtual void ClearQuickPayment(SOQuickPayment quickPayment)
		{
			quickPayment.CashAccountID = null;
			quickPayment.CuryOrigDocAmt = null;
			quickPayment.CuryRefundAmt = null;
			quickPayment.DocDesc = null;
			quickPayment.ExtRefNbr = null;
			quickPayment.NewCard = null;
			quickPayment.SaveCard = null;
			quickPayment.OrigDocAmt = null;
			quickPayment.RefundAmt = null;
			quickPayment.PaymentMethodID = null;
			quickPayment.PaymentMethodProcCenterID = null;
			quickPayment.RefTranExtNbr = null;
			quickPayment.PMInstanceID = null;
			quickPayment.ProcessingCenterID = null;
			quickPayment.UpdateNextNumber = null;

			quickPayment.Authorize = null;
			quickPayment.Capture = null;
			quickPayment.Refund = null;
			quickPayment.AdjgDocType = null;
			quickPayment.AdjgRefNbr = null;
			
			QuickPayment.Update(quickPayment);
		}

		protected virtual void InitializeImportPaymentPanel(PXGraph graph, string viewName)
		{
			SetDefaultValues(ImportExternalTran.Current, GetCurrent<TDocument>());
			ImportExternalTran.Cache.RaiseRowSelected(ImportExternalTran.Current);
		}

		public virtual void SetDefaultValues(SOImportExternalTran panel, TDocument document)
		{
			ClearImportPayment(panel);

			if (document != null)
			{
				PXCache panelCache = ImportExternalTran.Cache;
				panelCache.SetValueExt<SOImportExternalTran.paymentMethodID>(panel, document.PaymentMethodID);
				// we do not want to validate transaction against the PM instance specified in the document
				//panelCache.SetValueExt<SOImportExternalTran.pMInstanceID>(panel, document.PMInstanceID);
				panelCache.SetDefaultExt<SOImportExternalTran.processingCenterID>(panel);
			}
		}

		protected virtual void ClearImportPayment(SOImportExternalTran panel)
		{
			panel.PaymentMethodID = null;
			panel.PMInstanceID = null;
			panel.TranNumber = null;
			panel.ProcessingCenterID = null;
		}

		#endregion // Initialize

		#region Buttons

		public PXAction<TDocument> createDocumentPayment;
		[PXUIField(DisplayName = "Create Payment", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.AddNew, Tooltip = "Create Payment", DisplayOnMainToolbar = false, PopupCommand = nameof(syncPaymentTransaction))]
		protected virtual IEnumerable CreateDocumentPayment(PXAdapter adapter)
		{
			if (AskCreatePaymentDialog(Messages.CreatePayment) == WebDialogResult.OK)
				CreatePayment(QuickPayment.Current, ARPaymentType.Payment, false);

			return adapter.Get();
		}

		public PXAction<TDocument> createDocumentRefund;
		[PXUIField(DisplayName = "Create Refund", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.AddNew, Tooltip = "Create Refund", VisibleOnDataSource = false, PopupCommand = nameof(syncPaymentTransaction))]
		protected virtual IEnumerable CreateDocumentRefund(PXAdapter adapter)
		{
			Base.Save.Press();	// we have to save SOLines, otherwise orig. transaction selector may not work
			if (AskCreatePaymentDialog(Messages.CreateRefund) == WebDialogResult.OK)
				CreatePayment(QuickPayment.Current, ARPaymentType.Refund, false);

			return adapter.Get();
		}

		public PXAction<TDocument> createPaymentOK;
		[PXUIField(DisplayName = "OK", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		protected virtual IEnumerable CreatePaymentOK(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<TDocument> createPaymentCapture;
		[PXUIField(DisplayName = "Capture", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		protected virtual IEnumerable CreatePaymentCapture(PXAdapter adapter)
		{
			AssignCapture();

			return adapter.Get();
		}

		public PXAction<TDocument> createPaymentAuthorize;
		[PXUIField(DisplayName = "Authorize", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		protected virtual IEnumerable CreatePaymentAuthorize(PXAdapter adapter)
		{
			AssignAuthorize();

			return adapter.Get();
		}

		public PXAction<TDocument> createPaymentRefund;
		[PXUIField(DisplayName = "Refund", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false)]
		protected virtual IEnumerable CreatePaymentRefund(PXAdapter adapter)
		{
			AssignRefund();

			return adapter.Get();
		}


		public PXAction<TDocument> createAndAuthorizePayment;
		[PXUIField(DisplayName = "Create and Authorize", Visible = false, Enabled = false)]
		[PXButton]
		protected virtual IEnumerable CreateAndAuthorizePayment(PXAdapter adapter)
		{
			List<TDocument> list = adapter.Get<TDocument>().ToList();
			foreach (TDocument doc in list)
			{
				CreateCCPayment(doc, AssignAuthorize);
			}
			return list;
		}

		public PXAction<TDocument> createAndCapturePayment;
		[PXUIField(DisplayName = "Create and Capture", Visible = false, Enabled = false)]
		[PXButton]
		protected virtual IEnumerable CreateAndCapturePayment(PXAdapter adapter)
		{
			List<TDocument> list = adapter.Get<TDocument>().ToList();
			foreach (TDocument doc in list)
			{
				CreateCCPayment(doc, AssignCapture);
			}
			return list;
		}

		public PXAction<TDocument> syncPaymentTransaction;
		[PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXButton]
		public virtual IEnumerable SyncPaymentTransaction(PXAdapter adapter)
		{
			if (!string.IsNullOrEmpty(QuickPayment.Current.AdjgDocType)
				&& !string.IsNullOrEmpty(QuickPayment.Current.AdjgRefNbr)
				&& QuickPayment.Current.NewCard == true
				&& (QuickPayment.Current.Authorize == true || QuickPayment.Current.Capture == true))
			{
				var request = System.Web.HttpContext.Current.Request;

				var syncParameter = new SyncPaymentTransactionParameter
				{
					Document = GetCurrent<TDocument>(),
					AdjgDocType = QuickPayment.Current.AdjgDocType,
					AdjgRefNbr = QuickPayment.Current.AdjgRefNbr
				};

				string commandArguments = adapter.CommandArguments;
				if (!string.IsNullOrEmpty(commandArguments))
				{
					if (commandArguments == "__CLOSECCHFORM")
						syncParameter.CancelStr = true.ToString();
					else
						syncParameter.TranResponseStr = commandArguments;
				}
				else
				{
					string cancelStr = request.Form.Get("__CLOSECCHFORM");
					if (bool.TryParse(cancelStr, out bool isCancel) && isCancel)
						syncParameter.CancelStr = true.ToString();
					else
						syncParameter.TranResponseStr = request.Form.Get("__TRANID");
				}

				PXLongOperation.StartOperation(Base, () =>
				{
					CreateNewGraph(syncParameter.Document).ProcessSyncPaymentTransaction(syncParameter);
				});

				return adapter.Get();
			}
			else
			{
				return Base.Cancel.Press(adapter);
			}
		}

		protected virtual void ProcessSyncPaymentTransaction(SyncPaymentTransactionParameter parameter)
		{
			var paymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();

			var paymentTransaction = paymentEntry.GetExtension<ARPaymentEntryPaymentTransaction>();
			paymentTransaction.SetContextString("__CLOSECCHFORM", parameter.CancelStr);
			paymentTransaction.SetContextString("__TRANID", parameter.TranResponseStr);

			paymentEntry.Document.Current = paymentEntry.Document.Search<ARPayment.refNbr>(
				parameter.AdjgRefNbr, parameter.AdjgDocType);

			try
			{
				PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.syncPaymentTransaction));
			}
			catch (Exception exception)
			{
				RedirectToNewGraph(paymentEntry, exception);
			}
			finally
			{
				paymentTransaction.SetContextString("__CLOSECCHFORM", null);
				paymentTransaction.SetContextString("__TRANID", null);
			}
		}

		public PXAction<TDocument> captureDocumentPayment;
		[PXUIField(DisplayName = Messages.CaptureCCPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable CaptureDocumentPayment(PXAdapter adapter)
		{
			if (GetCurrent<TDocumentAdjust>()?.CuryAdjdAmt == 0m)
				throw new PXException(Messages.AmountToCaptureMustBeGreaterThanZero);

			ExecutePaymentTransactionAction((createPaymentExt, adjustment, payment, transaction)
				=> createPaymentExt.CapturePayment(adjustment, payment, transaction));

			return adapter.Get();
		}

		public PXAction<TDocument> voidDocumentPayment;
		[PXUIField(DisplayName = Messages.VoidCCPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable VoidDocumentPayment(PXAdapter adapter)
		{
			ExecutePaymentTransactionAction((createPaymentExt, adjustment, payment, transaction)
				=> createPaymentExt.VoidPayment(adjustment, payment, transaction));

			return adapter.Get();
		}

		public PXAction<TDocument> viewPayment;
		[PXUIField(DisplayName = "View Payment", Visible = false,
			MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton(Tooltip = "View Payment", VisibleOnDataSource = false, OnClosingPopup = PXSpecialButtonType.Cancel)]
		public virtual IEnumerable ViewPayment(PXAdapter adapter)
		{
			TDocumentAdjust adj = GetCurrent<TDocumentAdjust>();
			if (GetCurrent<TDocument>() != null && adj != null)
			{
				ARPaymentEntry pe = PXGraph.CreateInstance<ARPaymentEntry>();
				pe.Document.Current = pe.Document.Search<ARPayment.refNbr>(adj.AdjgRefNbr, adj.AdjgDocType);

				throw new PXRedirectRequiredException(pe, true, "Payment")
				{
					Mode = PXBaseRedirectException.WindowMode.NewWindow
				};
			}
			return adapter.Get();
		}

		public PXAction<TDocument> importDocumentPayment;
		[PXUIField(DisplayName = "Import Card Payment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable ImportDocumentPayment(PXAdapter adapter)
		{
			if (ImportExternalTran.AskExt(InitializeImportPaymentPanel, true) == WebDialogResult.OK)
				ImportPayment(ImportExternalTran.Current);

			return adapter.Get();
		}

		public PXAction<TDocument> importDocumentPaymentCreate;
		[PXUIField(DisplayName = "Create", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = false)]
		protected virtual IEnumerable ImportDocumentPaymentCreate(PXAdapter adapter)
		{
			return adapter.Get();
		}

		#endregion // Buttons

		#region SOQuickPayment events

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.paymentMethodID> eventArgs)
		{
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.paymentMethodProcCenterID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.pMInstanceID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.cashAccountID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.extRefNbr>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.refTranExtNbr>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.newCard>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.processingCenterID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.saveCard>(eventArgs.Row);
		}

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.refTranExtNbr> eventArgs)
		{
			var trans = (ExternalTransaction)PXSelectorAttribute.Select<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, eventArgs.Row);
			if (trans != null)
			{
				eventArgs.Cache.SetValueExt<SOQuickPayment.pMInstanceID>(eventArgs.Row, trans.PMInstanceID);
				object refundAmt = eventArgs.Row.CuryRefundAmt;
				eventArgs.Cache.RaiseFieldVerifying<SOQuickPayment.curyRefundAmt>(eventArgs.Row, ref refundAmt);
			}
		}

		protected virtual void _(Events.RowSelected<SOQuickPayment> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			PaymentMethod paymentMethod = PaymentMethod.PK.Find(Base, eventArgs.Row.PaymentMethodID);

			bool isCreditCardRequired = paymentMethod?.PaymentType == PaymentMethodType.CreditCard &&
				paymentMethod.IsAccountNumberRequired == true;
			bool newCard = eventArgs.Row.NewCard == true;
			bool isRefund = eventArgs.Row.IsRefund == true;
			bool allowUnlinkedRefund = eventArgs.Row.AllowUnlinkedRefund == true;
			bool origTranPopulated = !string.IsNullOrEmpty(eventArgs.Row.RefTranExtNbr);

			PXUIFieldAttribute.SetVisible<SOQuickPayment.curyOrigDocAmt>(eventArgs.Cache, eventArgs.Row, !isRefund);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.curyOrigDocAmt>(eventArgs.Cache, eventArgs.Row, !isRefund);
			PXUIFieldAttribute.SetRequired<SOQuickPayment.curyOrigDocAmt>(eventArgs.Cache, !isRefund);
			PXUIFieldAttribute.SetVisible<SOQuickPayment.curyRefundAmt>(eventArgs.Cache, eventArgs.Row, isRefund);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.curyRefundAmt>(eventArgs.Cache, eventArgs.Row, isRefund);
			PXUIFieldAttribute.SetRequired<SOQuickPayment.curyRefundAmt>(eventArgs.Cache, isRefund);

			PXUIFieldAttribute.SetVisible<SOQuickPayment.pMInstanceID>(eventArgs.Cache, eventArgs.Row, isCreditCardRequired);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.pMInstanceID>(eventArgs.Cache, eventArgs.Row,
				isCreditCardRequired && !newCard && !origTranPopulated && (!isRefund || allowUnlinkedRefund));
			PXUIFieldAttribute.SetRequired<SOQuickPayment.pMInstanceID>(eventArgs.Cache, isCreditCardRequired && !newCard && !origTranPopulated);

			PXUIFieldAttribute.SetVisible<SOQuickPayment.processingCenterID>(eventArgs.Cache, eventArgs.Row, isCreditCardRequired);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.processingCenterID>(eventArgs.Cache, eventArgs.Row, isCreditCardRequired && newCard);
			PXUIFieldAttribute.SetRequired<SOQuickPayment.processingCenterID>(eventArgs.Cache, isCreditCardRequired);

			PXUIFieldAttribute.SetVisible<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, eventArgs.Row, isCreditCardRequired && isRefund);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, eventArgs.Row, isCreditCardRequired && isRefund);
			PXUIFieldAttribute.SetRequired<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, isCreditCardRequired && isRefund && !allowUnlinkedRefund);

			ARSetup aRSetup = GetARSetup();
			PXUIFieldAttribute.SetVisible<SOQuickPayment.extRefNbr>(eventArgs.Cache, eventArgs.Row, !isCreditCardRequired);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.extRefNbr>(eventArgs.Cache, eventArgs.Row, !isCreditCardRequired);
			PXUIFieldAttribute.SetRequired<SOQuickPayment.extRefNbr>(eventArgs.Cache,
				!isCreditCardRequired && aRSetup.RequireExtRef == true);

			createPaymentOK.SetVisible(!CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(paymentMethod));
			createPaymentCapture.SetVisible(CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(paymentMethod) && !isRefund);
			createPaymentAuthorize.SetVisible(CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(paymentMethod) && !isRefund);
			createPaymentRefund.SetVisible(CCProcessingHelper.PaymentMethodSupportsIntegratedProcessing(paymentMethod) && isRefund);

			bool hasError = PaymentHasError(eventArgs.Row);
			CCProcessingCenter cCProcessingCenter = CCProcessingCenter.PK.Find(Base, eventArgs.Row.ProcessingCenterID);
			bool externalAuthorizationOnly = cCProcessingCenter?.IsExternalAuthorizationOnly == true;			

			Common.UIState.RaiseOrHideError<SOQuickPayment.processingCenterID>
			(
				eventArgs.Cache, eventArgs.Row, externalAuthorizationOnly,
				AR.Messages.ProcessingCenterIsExternalAuthorizationOnly, PXErrorLevel.Warning, eventArgs.Row.ProcessingCenterID
			);

			createPaymentOK.SetEnabled(!hasError);
			createPaymentCapture.SetEnabled(!(hasError || externalAuthorizationOnly));
			createPaymentAuthorize.SetEnabled(!(hasError || externalAuthorizationOnly));
			createPaymentRefund.SetEnabled(!(hasError || externalAuthorizationOnly));

			// TODO: support new card for refunds - waiting for support on AR302000 screen
			bool useNewCard = isCreditCardRequired && !isRefund
				&& PXSelectorAttribute.SelectAll<SOQuickPayment.processingCenterID>(eventArgs.Cache, eventArgs.Row)
					.RowCast<CCProcessingCenter>().Where(i => i.UseAcceptPaymentForm == true).Any();

			PXUIFieldAttribute.SetVisible<SOQuickPayment.newCard>(eventArgs.Cache, eventArgs.Row, useNewCard);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.newCard>(eventArgs.Cache, eventArgs.Row, useNewCard);

			bool useSaveCard = useNewCard && GetSavePaymentProfileCode(eventArgs.Row) == SavePaymentProfileCode.Allow;
			PXUIFieldAttribute.SetVisible<SOQuickPayment.saveCard>(eventArgs.Cache, eventArgs.Row, useSaveCard);
			PXUIFieldAttribute.SetEnabled<SOQuickPayment.saveCard>(eventArgs.Cache, eventArgs.Row, useSaveCard);
		}

		protected virtual bool PaymentHasError(SOQuickPayment payment)
		{
			foreach (string field in QuickPayment.Cache.Fields)
			{
				var uiFieldAttribute = QuickPayment.Cache.GetAttributesReadonly(payment, field).OfType<PXUIFieldAttribute>().FirstOrDefault();

				if (uiFieldAttribute?.ErrorLevel.IsIn(PXErrorLevel.Error, PXErrorLevel.RowError) == true ||
					(uiFieldAttribute?.Required == true && QuickPayment.Cache.GetValue(payment, field).IsIn(null, 0m)))
				{
					return true;
				}
			}

			return false;
		}

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.newCard> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			if (eventArgs.Row.NewCard == true)
			{
				eventArgs.Row.PMInstanceID = null;
			}
			else
			{
				eventArgs.Cache.SetDefaultExt<SOQuickPayment.processingCenterID>(eventArgs.Row);
				eventArgs.Cache.SetDefaultExt<SOQuickPayment.saveCard>(eventArgs.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.cashAccountID> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			eventArgs.Cache.SetDefaultExt<SOQuickPayment.extRefNbr>(eventArgs.Row);

			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				var previousCuryID = eventArgs.Row.CuryID;
				eventArgs.Cache.SetDefaultExt<SOQuickPayment.curyID>(eventArgs.Row);

				var cashaccount = CashAccount.PK.Find(Base, QuickPayment.Current?.CashAccountID);

				var currencyInfoCache = Base.Caches<CurrencyInfo>();
				var currencyInfoRow = currencyInfoCache.Locate(new CurrencyInfo() { CuryInfoID = eventArgs.Row.CuryInfoID });
				if (!string.IsNullOrEmpty(cashaccount?.CuryRateTypeID))
				{
					currencyInfoCache.SetValueExt<CurrencyInfo.curyRateTypeID>(currencyInfoRow, cashaccount?.CuryRateTypeID);
				}
				else
				{
					currencyInfoCache.SetDefaultExt<CurrencyInfo.curyRateTypeID>(currencyInfoRow);
				}

				if(previousCuryID != eventArgs.Row.CuryID)
				eventArgs.Cache.SetDefaultExt<SOQuickPayment.curyOrigDocAmt>(eventArgs.Row);
				eventArgs.Cache.SetDefaultExt<SOQuickPayment.curyRefundAmt>(eventArgs.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.pMInstanceID> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			eventArgs.Cache.SetDefaultExt<SOQuickPayment.processingCenterID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<SOQuickPayment.saveCard>(eventArgs.Row);
		}

		protected virtual void _(Events.FieldUpdated<SOQuickPayment, SOQuickPayment.processingCenterID> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			if (eventArgs.Row.ProcessingCenterID != null &&
				CCProcessingCenter.PK.Find(Base, eventArgs.Row.ProcessingCenterID)?.AllowSaveProfile != true)
			{
				eventArgs.Cache.SetValueExt<SOQuickPayment.saveCard>(eventArgs.Row, false);
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOQuickPayment, SOQuickPayment.curyOrigDocAmt> eventArgs)
		{
			eventArgs.NewValue = GetDefaultPaymentAmount(eventArgs.Row) ?? eventArgs.NewValue;
		}

		protected virtual void _(Events.FieldDefaulting<SOQuickPayment, SOQuickPayment.curyRefundAmt> eventArgs)
		{
			eventArgs.NewValue = GetDefaultPaymentAmount(eventArgs.Row) ?? eventArgs.NewValue;
		}

		protected virtual decimal? GetDefaultPaymentAmount(SOQuickPayment qp)
		{
			decimal? amt = null;
			if (qp == null || GetCurrent<TDocument>() == null)
				return amt;

			TDocument document = GetCurrent<TDocument>();

			if (qp.CuryID == document.CuryID)
			{
				amt = document.CuryUnpaidBalance;
			}
			else if (qp.CuryID != null)
			{
				PXCurrencyAttribute.CuryConvCury(QuickPayment.Cache, qp, document.UnpaidBalance ?? 0m, out decimal curyUnpaidBalance);
				amt = curyUnpaidBalance;
			}
			return amt;
		}

		protected virtual void _(Events.FieldVerifying<SOQuickPayment, SOQuickPayment.curyOrigDocAmt> eventArgs)
		{
			if (eventArgs.Row.IsRefund != true)
				ValidateAmount<SOQuickPayment.curyOrigDocAmt>(eventArgs.Row, (decimal?)eventArgs.NewValue);
		}

		protected virtual void _(Events.FieldVerifying<SOQuickPayment, SOQuickPayment.curyRefundAmt> eventArgs)
		{
			if (eventArgs.Row.IsRefund == true)
				ValidateAmount<SOQuickPayment.curyRefundAmt>(eventArgs.Row, (decimal?)eventArgs.NewValue);
		}

		protected virtual void ValidateAmount<AmountField>(SOQuickPayment qp, decimal? value)
			where AmountField: IBqlField
		{
			if (value == null || qp?.CuryID == null) return;

			PXException exc = null;
			QuickPayment.Cache.RaiseFieldDefaulting<AmountField>(qp, out object maxValue);

			if (value > (decimal?)maxValue)
			{
				exc = new PXSetPropertyException<AmountField>(Messages.PaymentShouldBeLessUnpaidAmount, maxValue);
			}
			else if (value <= 0m)
			{
				exc = new PXSetPropertyException<AmountField>(Messages.PaymentShouldBeMoreZero);
			}

			if (qp.RefTranExtNbr != null && exc == null)
			{
				var trans = (ExternalTransaction)PXSelectorAttribute.Select<SOQuickPayment.refTranExtNbr>(QuickPayment.Cache, qp);
				if (trans != null)
				{
					var origPayment = ARPayment.PK.Find(Base, trans.DocType, trans.RefNbr);
					decimal? maxTranAmt;
					if (origPayment == null || origPayment.CuryID == qp.CuryID)
					{
						maxTranAmt = trans.Amount;
					}
					else
					{
						PXCurrencyAttribute.CuryConvCury(QuickPayment.Cache, qp, trans.Amount ?? 0m, out decimal tranAmt);
						maxTranAmt = tranAmt;
					}
					if (maxTranAmt != null && value > maxTranAmt)
					{
						exc = new PXSetPropertyException<AmountField>(Messages.RefundAmtExceedsOrigTranAmt);
					}
				}
			}

			QuickPayment.Cache.RaiseExceptionHandling<AmountField>(qp, value, exc);
		}

		protected virtual void _(Events.FieldVerifying<SOQuickPayment, SOQuickPayment.extRefNbr> eventArgs)
		{
			string newValue = (string)eventArgs.NewValue;

			if (eventArgs.Row == null || string.IsNullOrEmpty(newValue))
				return;

			ARPayment duplicate = FindARPaymentByPaymentRef(newValue, eventArgs.Row.PaymentMethodID);
			if (duplicate != null)
			{
				// TODO: SOCreatePayment: Investigate why RaiseExceptionHandling in FieldVerifying doesn't work when SmartPanel is showing first time. (PXDialogRequiredException).
				eventArgs.Cache.RaiseExceptionHandling<SOQuickPayment.extRefNbr>(eventArgs.Row, newValue, new PXSetPropertyException(
					AR.Messages.DuplicateCustomerPayment, PXErrorLevel.Warning, duplicate.ExtRefNbr, duplicate.DocDate, duplicate.DocType, duplicate.RefNbr));
			}
		}

		protected virtual ARPayment FindARPaymentByPaymentRef(string paymentRef, string paymentMethodID)
		{
			if (string.IsNullOrEmpty(paymentMethodID))
			{
				return null;
			}

			PaymentMethod paymentMethod = PaymentMethod.PK.Find(Base, paymentMethodID);

			if (paymentMethod.IsAccountNumberRequired == true)
			{
				return PXSelectReadonly<ARPayment,
					Where<ARPayment.customerID, Equal<Required<Customer.bAccountID>>,
						And<ARPayment.pMInstanceID, Equal<Current<SOQuickPayment.pMInstanceID>>,
						And<ARPayment.extRefNbr, Equal<Required<SOQuickPayment.extRefNbr>>,
						And<ARPayment.voided, Equal<False>>>>>>.Select(Base, GetCurrent<TDocument>().CustomerID, paymentRef);
			}
			else
			{
				return PXSelectReadonly<ARPayment,
					Where<ARPayment.customerID, Equal<Required<Customer.bAccountID>>,
						And<ARPayment.paymentMethodID, Equal<Current<SOQuickPayment.paymentMethodID>>,
						And<ARPayment.extRefNbr, Equal<Required<SOQuickPayment.extRefNbr>>,
						And<ARPayment.voided, Equal<False>>>>>>.Select(Base, GetCurrent<TDocument>().CustomerID, paymentRef);
			}
		}

		protected virtual void FieldDefaulting(Events.FieldDefaulting<SOQuickPayment, SOQuickPayment.newCard> e)
		{
			if (e.Row == null)
				return;

			if (e.Row?.PaymentMethodID == null || e.Row.ProcessingCenterID == null || e.Row.IsRefund == true)
			{
				e.NewValue = false;
				return;
			}

			PaymentMethod paymentMethod = PaymentMethod.PK.Find(Base, e.Row.PaymentMethodID);
			CCProcessingCenter procCenter = CCProcessingCenter.PK.Find(Base, e.Row.ProcessingCenterID);
			if (procCenter != null)
			{
				bool useNewCard = paymentMethod.PaymentType == PaymentMethodType.CreditCard && procCenter.UseAcceptPaymentForm == true;
				if (useNewCard && e.Row.PMInstanceID == null)
				{
					e.NewValue = (PXSelectorAttribute.SelectAll<SOQuickPayment.pMInstanceID>(e.Cache, e.Row).Count == 0);
				}
				else
				{
					e.NewValue = false;
				}
			}
			else
			{
				e.NewValue = false;
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOQuickPayment, SOQuickPayment.refTranExtNbr> eventArgs)
		{
			if (eventArgs.Row.IsRefund == true)
			{
				var trans = PXSelectorAttribute.SelectAll<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, eventArgs.Row)
					.RowCast<ExternalTransaction>().ToList();
				if (trans.Count == 1)
				{
					eventArgs.NewValue = trans[0].TranNumber;
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<SOQuickPayment, SOQuickPayment.refTranExtNbr> eventArgs)
		{
			// need to imitate PXRestrictorAttribute behavior because Exists<Select> does not support Verify method properly
			var trans = (ExternalTransaction)PXSelectorAttribute.Select<SOQuickPayment.refTranExtNbr>(eventArgs.Cache, eventArgs.Row,
				(string)eventArgs.NewValue);
			if (!string.IsNullOrEmpty(trans?.TranNumber) 
				&& !HasReturnLineForOrigTran(trans.ProcessingCenterID, trans.TranNumber)
				&& ValidateCCRefundOrigTransaction())
			{
				throw new PXSetPropertyException(Messages.OrigTranNumberNotRelatedToReturnedInvoices, trans.TranNumber);
			}
		}

		#endregion // SOQuickPayment events

		#region TDocumentEvents

		protected virtual void _(Events.RowDeleting<TDocument> eventArgs)
			=> ThrowExceptionIfDocumentHasLegacyCCTran();

		protected virtual void PaymentMethodFieldVerifying(PXCache sender, PXFieldVerifyingEventArgs eventArgs)
		{
			if (IsCashSale() && eventArgs.NewValue != null)
			{
				var paymentMethod = PaymentMethod.PK.Find(Base, (string)eventArgs.NewValue);
				if (paymentMethod == null)
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<PaymentMethod>(), eventArgs.NewValue);

				if (paymentMethod.PaymentType == PaymentMethodType.CreditCard)
					throw new PXSetPropertyException(GetCCPaymentIsNotSupportedMessage());
			}
		}

		#endregion // TDocumentEvents

		#region TDocumentAdjust events

		protected virtual void _(Events.RowSelected<TDocumentAdjust> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			PXSetPropertyException exception = null;

			if (PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() &&
				eventArgs.Row.IsCCPayment == true &&
				eventArgs.Row.IsCCAuthorized != true &&
				eventArgs.Row.IsCCCaptured != true &&
				eventArgs.Row.Voided != true &&
				eventArgs.Row.Released != true &&
				ARPaymentType.DrCr(eventArgs.Row.AdjgDocType) == GL.DrCr.Debit)
			{
				var payment = ARPayment.PK.Find(Base, eventArgs.Row.AdjgDocType, eventArgs.Row.AdjgRefNbr);
				if (payment == null)
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<ARPayment>(), eventArgs.Row.AdjgDocType, eventArgs.Row.AdjgRefNbr);

				string errorMessage = (payment.CCReauthTriesLeft > 0) ?
					Messages.PaymentHasNoActiveAuthorizedOrCapturedTransactionsTriesMoreZero : Messages.PaymentHasNoActiveAuthorizedOrCapturedTransactions;

				exception = new PXSetPropertyException(errorMessage, PXErrorLevel.RowWarning, eventArgs.Row.AdjgRefNbr);
			}

			eventArgs.Cache.RaiseExceptionHandling(GetPaymentErrorFieldName(), eventArgs.Row, null, exception);
		}

		#endregion // TDocumentAdjust events

		#region CurrencyInfo events

		protected virtual void _(Events.RowPersisting<CurrencyInfo> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			bool quickPaymentCurrentIsNull = !QuickPayment.Cache.Cached.Any_(); // The PXFilter inserts a new row on getting the Current property.
			if (!quickPaymentCurrentIsNull && eventArgs.Row.CuryInfoID == QuickPayment.Current.CuryInfoID)
			{
				eventArgs.Cancel = true;
			}
		}

		#endregion // CurrencyInfo events

		#region Create Payment/Prepayment

		protected virtual WebDialogResult AskCreatePaymentDialog(string header)
		{
			ThrowExceptionIfDocumentHasLegacyCCTran();

			const WebDialogResult Authorize = WebDialogResult.No;
			const WebDialogResult Capture = WebDialogResult.Yes;
			const WebDialogResult Refund = WebDialogResult.Abort;

			try
			{
				string localizedHeader = PXMessages.LocalizeNoPrefix(header);
				var dialogResult = QuickPayment.View.AskExtWithHeader(localizedHeader, InitializeQuickPaymentPanel);
				switch (dialogResult)
				{
					case Authorize:
						AssignAuthorize();
						return WebDialogResult.OK;
					case Capture:
						AssignCapture();
						return WebDialogResult.OK;
					case Refund:
						AssignRefund();
						return WebDialogResult.OK;
					default:
						return dialogResult;
				}
			}
			catch (PXBaseRedirectException exc)
			{
				exc.RepaintControls = true;
				throw exc;
			}
		}

		protected virtual void PrepareForCreateCCPayment(TDocument doc)
		{
			if (doc.PMInstanceID == null)
			{
				throw new Common.Exceptions.FieldIsEmptyException(
					Base.Caches<TDocument>(), doc, GetDocumentPMInstanceIDField(), true);
			}

			var pmInstance = CustomerPaymentMethod.PK.Find(Base, doc.PMInstanceID);
			if (pmInstance == null)
			{
				throw new Common.Exceptions.RowNotFoundException(
					Base.Caches[typeof(CustomerPaymentMethod)], doc.PMInstanceID);
			}

			if (pmInstance.IsActive != true)
				throw new PXException(AR.Messages.InactiveCreditCardMayNotBeProcessed, pmInstance.Descr);
		}

		protected virtual void CreateCCPayment(TDocument doc, Action onBeforeCreatePayment)
		{
			Base.Clear();
			SetCurrentDocument(doc);
			PrepareForCreateCCPayment(doc);
			InitializeQuickPaymentPanel(Base, QuickPayment.View.Name);
			onBeforeCreatePayment?.Invoke();
			CreatePayment(QuickPayment.Current, ARPaymentType.Payment, true);
		}

		protected virtual void CreatePayment(SOQuickPayment quickPayment, string paymentType, bool throwErrors)
		{
			Base.Save.Press();

			if (quickPayment.Capture == true || quickPayment.Authorize == true || quickPayment.Refund == true)
			{
				if (quickPayment.NewCard == true)
				{
					AR.CCPaymentProcessing.Helpers.CCProcessingHelper.CheckHttpsConnection();
				}

				var createParameter = new CreatePaymentParameter
				{
					Document = GetCurrent<TDocument>(),
					CcPaymentConnectorUrl = System.Web.HttpContext.Current.GetPaymentConnectorUrl(),
					QuickPayment = quickPayment,
					PaymentType = paymentType,
					ThrowErrors = throwErrors
				};
				PXLongOperation.StartOperation(Base, () =>
				{
					CreateNewGraph(createParameter.Document).ProcessCreatePayment(createParameter);
				});

			}
			else
			{
				ARPaymentEntry paymentEntry = CreatePayment(quickPayment, GetCurrent<TDocument>(), paymentType);
				paymentEntry.Save.Press();
				Base.Cancel.Press();
			}
		}

		protected virtual void ProcessCreatePayment(CreatePaymentParameter parameter)
		{
			PXContext.SetSlot(nameof(Extentions.GetPaymentConnectorUrl), parameter.CcPaymentConnectorUrl);

			using (new ForcePaymentAppScope())
			{
				ARPaymentEntry paymentEntry = CreatePayment(parameter.QuickPayment, parameter.Document, parameter.PaymentType);
				paymentEntry.Save.Press();

				try
				{
					if (parameter.QuickPayment.Capture == true)
					{
						PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.captureCCPayment));
					}
					else if (parameter.QuickPayment.Authorize == true)
					{
						PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.authorizeCCPayment));
					}
					else if (parameter.QuickPayment.Refund == true)
					{
						// the refund might be on hold when approval is turned on
						if (paymentEntry.Document.Current.Hold == false)
						{
							PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.creditCCPayment));
						}
					}
				}
				catch (PXBaseRedirectException redirectException)
				{
					PXLongOperation.SetCustomInfo(new EndPaymentOperationCustomInfo(
						paymentEntry.Document.Current.DocType, paymentEntry.Document.Current.RefNbr, redirectException));
				}
				catch (Exception exception)
				{
					if (parameter.ThrowErrors)
						throw;
					else
						RedirectToNewGraph(paymentEntry, exception);
				}
				finally
				{
					PXContext.SetSlot(nameof(Extentions.GetPaymentConnectorUrl), null);
				}
			}
		}

		public virtual ARPaymentEntry CreatePayment(SOQuickPayment quickPayment, TDocument document, string paymentType)
		{
			ARPaymentEntry paymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();

			paymentEntry.AutoPaymentApp = true;

			if (quickPayment.Capture == true || quickPayment.Authorize == true || quickPayment.Refund == true)
				paymentEntry.arsetup.Current.HoldEntry = false;

			var payment = paymentEntry.Document.Insert(new ARPayment() { DocType = paymentType });
			FillARPayment(payment, quickPayment, document);
			payment = paymentEntry.Document.Update(payment);
			if (quickPayment.NewCard == true)
			{
				paymentEntry.Document.Cache.SetValue<ARPayment.pMInstanceID>(payment, PaymentTranExtConstants.NewPaymentProfile);
			}
			if (string.IsNullOrEmpty(quickPayment.RefTranExtNbr))
			{
				paymentEntry.Document.Cache.SetValue<ARPayment.cCTransactionRefund>(payment, false);
			}

			AddAdjust(paymentEntry, document);

			return paymentEntry;
		}


		protected virtual void FillARPayment(ARPayment arpayment, SOQuickPayment quickPayment, TDocument document)
		{
			arpayment.CustomerID = document.CustomerID;
			arpayment.CustomerLocationID = document.CustomerLocationID;
			arpayment.PaymentMethodID = quickPayment.PaymentMethodID;
			arpayment.RefTranExtNbr = quickPayment.RefTranExtNbr;
			arpayment.PMInstanceID = quickPayment.PMInstanceID;
			arpayment.CuryOrigDocAmt = (quickPayment.IsRefund == true) ? quickPayment.CuryRefundAmt : quickPayment.CuryOrigDocAmt;
			arpayment.DocDesc = quickPayment.DocDesc;
			arpayment.CashAccountID = quickPayment.CashAccountID;
			arpayment.ProcessingCenterID = quickPayment.ProcessingCenterID;
			arpayment.ExtRefNbr = quickPayment.ExtRefNbr;
			arpayment.UpdateNextNumber = quickPayment.UpdateNextNumber;

			switch (GetSavePaymentProfileCode(quickPayment))
			{
				case SavePaymentProfileCode.Allow:
					arpayment.SaveCard = quickPayment.SaveCard;
					break;
				case SavePaymentProfileCode.Force:
					arpayment.SaveCard = true;
					break;
			}
		}

		protected virtual string GetSavePaymentProfileCode(SOQuickPayment quickPayment)
		{
			return GetSavePaymentProfileCode(quickPayment.NewCard, quickPayment.ProcessingCenterID);
		}

		public virtual string GetSavePaymentProfileCode(bool? newCard, string ProcessingCenterID)
		{
			if (newCard == true)
			{
				CCProcessingCenter procCenter = CCProcessingCenter.PK.Find(Base, ProcessingCenterID);
				if (procCenter?.AllowSaveProfile == true)
					return GetCustomerClass()?.SavePaymentProfiles;
			}

			return null;
		}

		#endregion // Create Payment/Prepayment

		#region SOImportExternalTran events

		protected virtual void _(Events.RowSelected<SOImportExternalTran> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			PXUIFieldAttribute.SetEnabled<SOImportExternalTran.processingCenterID>(eventArgs.Cache, eventArgs.Row,
				eventArgs.Row.PMInstanceID == null);
			importDocumentPaymentCreate.SetEnabled(
				!string.IsNullOrEmpty(eventArgs.Row.ProcessingCenterID));
		}

		#endregion

		#region Import CC Payment

		protected virtual void ImportPayment(SOImportExternalTran panel)
		{
			Base.Save.Press();

			var importParameter = new ImportPaymentParameter
			{
				Document = GetCurrent<TDocument>(),
				Panel = panel
			};

			PXLongOperation.StartOperation(Base, () =>
			{
				CreateNewGraph(importParameter.Document).ProcessImportPayment(importParameter);
			});
		}

		protected virtual void ProcessImportPayment(ImportPaymentParameter parameter)
		{
			var paymentCreator = new PaymentDocCreator();
			var inputParams = new PaymentDocCreator.InputParams()
			{
				Customer = parameter.Document.CustomerID,
				CashAccountID = parameter.Document.CashAccountID,
				PaymentMethodID = parameter.Panel.PaymentMethodID,
				PMInstanceID = parameter.Panel.PMInstanceID,
				ProcessingCenterID = parameter.Panel.ProcessingCenterID,
				TransactionID = parameter.Panel.TranNumber
			};

			using (var txscope = new PXTransactionScope())
			{
				IDocumentKey payment = paymentCreator.CreateDoc(inputParams);

				var paymentEntry = ApplyPayment(payment, GetCurrent<TDocument>());
				paymentEntry.Save.Press();
				txscope.Complete();
			}
		}

		protected virtual ARPaymentEntry ApplyPayment(IDocumentKey payment, TDocument document)
		{
			var paymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();
			paymentEntry.AutoPaymentApp = true;

			paymentEntry.Document.Current = paymentEntry.Document.Search<ARPayment.refNbr>(payment.RefNbr, payment.DocType);
			AddAdjust(paymentEntry, document);

			return paymentEntry;
		}

		#endregion

		#region Autorize/Capture/Void actions

		protected virtual void ExecutePaymentTransactionAction(
			Action<CreatePaymentExtBase<TGraph, TFirstGraph, TDocument, TDocumentAdjust, TPaymentAdjust>,
				TDocumentAdjust, ARPaymentEntry, ARPaymentEntryPaymentTransaction> paymentAction)
		{
			Base.Save.Press();

			TDocumentAdjust adjustment = GetAdjustView().Current;
			if (adjustment == null)
				return;

			var executeParameter = new ExecutePaymentTransactionActionParameter
			{
				Document = GetCurrent<TDocument>(),
				PaymentAction = paymentAction,
				Adjustment = adjustment
			};

			PXLongOperation.StartOperation(Base, () =>
			{
				CreateNewGraph(executeParameter.Document).ProcessExecutePaymentTransactionAction(executeParameter);
			});
		}

		protected virtual void ProcessExecutePaymentTransactionAction(ExecutePaymentTransactionActionParameter parameter)
		{
			ARPaymentEntry paymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();
			paymentEntry.Clear();
			paymentEntry.Document.Current = paymentEntry.Document.Search
				<ARPayment.refNbr>(parameter.Adjustment.AdjgRefNbr, parameter.Adjustment.AdjgDocType);

			var paymentTransaction = paymentEntry.GetExtension<ARPaymentEntryPaymentTransaction>();

			try
			{
				parameter.PaymentAction(this, parameter.Adjustment, paymentEntry, paymentTransaction);
				PXLongOperation.SetCustomInfo(new EndPaymentOperationCustomInfo());
			}
			catch (Exception exception)
			{
				PXTrace.WriteError(Messages.PaymentProcessingError, parameter.Adjustment.AdjgRefNbr);
				PXTrace.WriteError(exception);
				RedirectToNewGraph(parameter.Adjustment, exception);
			}
		}

		public virtual void RedirectToNewGraph(TDocumentAdjust adjustment, Exception exception)
		{
			if (exception == null)
				return;

			PXLongOperation.SetCustomInfo(new EndPaymentOperationCustomInfo(adjustment, exception));

			throw new PXException(Messages.PaymentProcessingError, adjustment.AdjgRefNbr);
		}

		public virtual void RedirectToNewGraph(ARPaymentEntry paymentEntry, Exception exception)
		{
			PXTrace.WriteError(Messages.PaymentProcessingError, paymentEntry.Document.Current?.RefNbr);
			PXTrace.WriteError(exception);

			var paymentAdjustView = GetAdjustView(paymentEntry);
			TPaymentAdjust adjust = paymentAdjustView.Current ?? paymentAdjustView.SelectSingle();
			if (adjust != null)
			{
				TDocumentAdjust newAdjust;

				if (typeof(TPaymentAdjust) == typeof(TDocumentAdjust))
				{
					newAdjust = adjust as TDocumentAdjust;
				}
				else
				{
					newAdjust = (TDocumentAdjust)GetAdjustView().Cache.CreateInstance();

					var adjustCache = GetAdjustView().Cache;
					foreach (var key in adjustCache.Keys)
					{
						adjustCache.SetValue(newAdjust, key, paymentAdjustView.Cache.GetValue(adjust, key));
					}
				}

				if (newAdjust == null)
					throw exception;

				RedirectToNewGraph(newAdjust, exception);
			}
			else
			{
				throw exception;
			}
		}

		public virtual void CopyError(TDocumentAdjust errorAdjustment, Exception exception)
		{
			CopyError<TDocumentAdjust>(nameof(errorAdjustment.AdjgDocType), errorAdjustment, exception);
		}

		protected virtual void CopyError<TAdjust>(string fieldName, ICreatePaymentAdjust errorAdjustment, Exception exception)
			where TAdjust : class, ICreatePaymentAdjust
		{
			Base.RowSelected.AddHandler<TAdjust>((sender, e) =>
			{
				var adjustment = e.Row as TAdjust;

				if (errorAdjustment.AdjgDocType == adjustment?.AdjgDocType && errorAdjustment.AdjgRefNbr == adjustment?.AdjgRefNbr)
				{
					sender.RaiseExceptionHandling(fieldName, adjustment, sender.GetValue(adjustment, fieldName), exception);
				}
			});
		}

		public virtual void CapturePayment(TDocumentAdjust adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			VerifyAdjustments(paymentEntry, nameof(CaptureDocumentPayment));

			if (!CanCapture(adjustment, paymentEntry.Document.Current))
				throw new PXActionDisabledException(nameof(ARPaymentEntryPaymentTransaction.captureCCPayment));

			if (adjustment.CuryAdjdAmt == 0m)
				throw new PXException(Messages.AmountToCaptureMustBeGreaterThanZero);

			RemoveUnappliedBalance(paymentEntry);

			PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.captureCCPayment));
		}

		protected virtual void RemoveUnappliedBalance(ARPaymentEntry paymentEntry)
		{
			var payment = paymentEntry.Document.Current;
			var paymentCache = paymentEntry.Document.Cache;

			if (payment.CuryUnappliedBal > 0m)
			{
				paymentCache.SetValueExt<ARPayment.curyOrigDocAmt>(payment, payment.CuryOrigDocAmt - payment.CuryUnappliedBal);
				payment = (ARPayment)paymentCache.Update(payment);
			}
		}

		public virtual void VoidPayment(TDocumentAdjust adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			VerifyAdjustments(paymentEntry, nameof(VoidDocumentPayment));

			ARPayment payment = paymentEntry.Document.Current;

			if (!CanVoid(adjustment, payment))
				throw new PXActionDisabledException(nameof(ARPaymentEntryPaymentTransaction.voidCCPayment));

			var extTranState = paymentTransaction.GetActiveTransactionState();

			if (payment.PendingProcessing == true && extTranState.IsPreAuthorized)
			{
				string paymentRef = paymentEntry.Document.Current.ExtRefNbr; // TODO: SOCreatePayment: Temporary fix ARPayment bug

				PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.voidCCPayment));

				paymentEntry.Clear();
				paymentEntry.Clear(PXClearOption.ClearQueriesOnly);
				paymentEntry.Document.Current = paymentEntry.Document.Search<ARPayment.refNbr>(adjustment.AdjgRefNbr, adjustment.AdjgDocType);
				paymentEntry.Document.Current.ExtRefNbr = paymentRef; // TODO: SOCreatePayment: Temporary fix ARPayment bug

				PressButtonIfEnabled(paymentEntry.voidCheck);
			}
			else if (payment.Released == true && payment.OpenDoc == true && extTranState.IsCaptured)
			{
				PressButtonIfEnabled(paymentEntry.voidCheck, true);

				paymentEntry.Document.Current = paymentEntry.Document.Search<ARPayment.refNbr>(paymentEntry.Document.Current.RefNbr, ARDocType.VoidPayment);
				if (paymentEntry.Document.Current == null)
					throw new Common.Exceptions.RowNotFoundException(paymentEntry.Document.Cache, paymentEntry.Document.Current.RefNbr, ARDocType.VoidPayment);

				if (paymentEntry.Document.Current.Hold == true)
				{
					paymentEntry.Document.Cache.SetValueExt<ARPayment.hold>(paymentEntry.Document.Current, false);
					paymentEntry.Document.UpdateCurrent();
				}

				PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.voidCCPayment));

				paymentEntry.Save.Press();
			}
		}

		public virtual void VoidCCTransactionForReAuthorization(TDocumentAdjust adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			const string VoidButtonName = nameof(ARPaymentEntryPaymentTransaction.voidCCPaymentForReAuthorization);

			VerifyAdjustments(paymentEntry, nameof(VoidDocumentPayment));

			ARPayment payment = paymentEntry.Document.Current;

			if (!CanVoid(adjustment, payment))
				throw new PXActionDisabledException(VoidButtonName);

			var extTranState = paymentTransaction.GetActiveTransactionState();

			if (payment.PendingProcessing == true && extTranState.IsPreAuthorized)
			{
				PressButtonIfEnabled(paymentEntry, VoidButtonName);
			}
			else
			{
				throw new PXActionDisabledException(VoidButtonName);
			}
		}

		public virtual void ValidatePayment(TDocumentAdjust adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			VerifyAdjustments(paymentEntry, nameof(CaptureDocumentPayment));
			PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.validateCCPayment));
		}

		public virtual void AuthorizePayment(TDocumentAdjust adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			VerifyAdjustments(paymentEntry, nameof(CaptureDocumentPayment));
			RemoveUnappliedBalance(paymentEntry);
			PressButtonIfEnabled(paymentEntry, nameof(ARPaymentEntryPaymentTransaction.authorizeCCPayment));
		}

		public virtual void PressButtonIfEnabled(PXGraph graph, string actionName)
		{
			if (!graph.Actions.Contains(actionName))
				throw new PXException(ErrorMessages.ActionNotFound, actionName);

			PressButtonIfEnabled(graph.Actions[actionName]);
		}

		protected virtual void PressButtonIfEnabled(PXAction action)
		{
			PressButtonIfEnabled(action, false);
		}

		protected virtual void PressButtonIfEnabled(PXAction action, bool suppressRedirectExc)
		{
			try
			{
				if (action.GetEnabled())
				{
					action.Press();
				}
				else
				{
					PXButtonState bs = action.GetState((object)null) as PXButtonState;
					throw new PXActionDisabledException(bs?.DisplayName ?? bs?.Name);
				}
			}
			catch (PXBaseRedirectException) when (suppressRedirectExc)
			{
				// it is a temporary fix waiting for AC-187429
			}
		}

		protected virtual PXFieldSelecting CreateVoidCaptureFieldSelecting(string fieldName, Func<TDocumentAdjust, ARPayment, bool> funcGetValue)
		{
			return (c, e) =>
			{
				PXFieldState state = PXFieldState.CreateInstance(e.ReturnState, typeof(bool), false, false, 0, null, null, null, fieldName);
				state.Visible = false;
				e.ReturnState = state;

				if (e.Row == null)
					return;

				var row = (TDocumentAdjust)e.Row;
				var payment = ARPayment.PK.Find(Base, row.AdjgDocType, row.AdjgRefNbr);
				e.ReturnValue = funcGetValue(row, payment);
			};
		}

		protected virtual bool CanVoid(TDocumentAdjust adjust, ARPayment payment)
		{
			if (adjust?.IsCCPayment != true)
				return false;

			return (payment?.PendingProcessing == true && payment.IsCCAuthorized == true) ||
				(payment.Released == true && payment.OpenDoc == true && payment.IsCCCaptured == true);
		}

		protected virtual bool CanCapture(TDocumentAdjust adjust, ARPayment payment)
		{
			if (adjust?.IsCCPayment != true)
				return false;

			return payment?.PendingProcessing == true && payment.IsCCAuthorized == true;
		}

		protected virtual void AssignAuthorize()
		{
			QuickPayment.Current.Authorize = true;
		}

		protected virtual void AssignCapture()
		{
			QuickPayment.Current.Capture = true;
		}

		protected virtual void AssignRefund()
		{
			QuickPayment.Current.Refund = true;
		}

		#endregion // Autorize/Capture/Void actions

		#region Payment Information

		protected virtual bool IsMultipleApplications(ARPaymentEntry paymentEntry)
			=> IsMultipleApplications(paymentEntry, out ARPaymentTotals paymentTotals, out SOInvoice invoice);

		protected virtual bool IsMultipleApplications(ARPaymentEntry paymentEntry, out ARPaymentTotals paymentTotals, out SOInvoice invoice)
		{
			invoice = null;

			var payment = paymentEntry.Document.Current;
			paymentTotals = ARPaymentTotals.PK.Find(paymentEntry, payment.DocType, payment.RefNbr);
			if (paymentTotals == null || (paymentTotals.OrderCntr == 0 && paymentTotals.InvoiceCntr == 0))
				return false; // No related documents

			if (paymentTotals.OrderCntr > 0 && paymentTotals.AdjdOrderNbr != null && paymentTotals.InvoiceCntr == 0)
				return false; // Only links to unique order

			if (paymentTotals.InvoiceCntr > 0 && paymentTotals.AdjdRefNbr != null && paymentTotals.OrderCntr == 0)
				return false; // Only links to unique invoice

			if (paymentTotals.AdjdRefNbr != null && paymentTotals.AdjdOrderNbr != null) // Only links to unique order and unique invoice
			{
				invoice = SOInvoice.PK.Find(paymentEntry, paymentTotals.AdjdDocType, paymentTotals.AdjdRefNbr);
				if (invoice == null)
					return true; // It's ARInvoice doc, not SOInvoice

				return paymentTotals.AdjdOrderType != invoice.SOOrderType || paymentTotals.AdjdOrderNbr != invoice.SOOrderNbr; // Invoice has only links to unique order but it is not the same order which payment refers to
			}

			return true;
		}

		protected virtual string GetPaymentErrorFieldName()
			=> nameof(ICreatePaymentAdjust.AdjgRefNbr);

		#endregion // Payment Information

		#region Abstract

		protected abstract PXSelectBase<TDocumentAdjust> GetAdjustView();
		protected abstract PXSelectBase<TPaymentAdjust> GetAdjustView(ARPaymentEntry paymentEntry);

		protected abstract CustomerClass GetCustomerClass();

		protected abstract void SetCurrentDocument(TDocument document);

		protected abstract void AddAdjust(ARPaymentEntry paymentEntry, TDocument document);

		protected abstract void VerifyAdjustments(ARPaymentEntry paymentEntry, string actionName);

		protected abstract string GetDocumentDescr(TDocument document);

		protected abstract ARSetup GetARSetup();

		protected abstract void ThrowExceptionIfDocumentHasLegacyCCTran();

		protected abstract Type GetPaymentMethodField();

		protected abstract bool IsCashSale();

		protected abstract string GetCCPaymentIsNotSupportedMessage();

		protected abstract Type GetDocumentPMInstanceIDField();

		protected abstract bool HasReturnLineForOrigTran(string procCenterID, string tranNumber);

		protected abstract bool ValidateCCRefundOrigTransaction();

		#endregion // Abstract
	}
}
