using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.Extensions.PaymentTransaction;
using PX.Objects.AR.GraphExtensions;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt
{
	public class CreatePaymentExt : CreatePaymentExtBase<SOInvoiceEntry, ARInvoiceEntry, ARInvoice, ARAdjust2, ARAdjust>
	{
		public override void Initialize()
		{
			base.Initialize();

			PXAction action = Base.Actions["action"];
			if (action != null)
			{
				// this action is never used for invoices
				action.AddMenuAction(createAndAuthorizePayment);
				action.SetVisible(nameof(CreateAndAuthorizePayment), false);
			}
		}

		#region Document events

		protected virtual void _(Events.RowSelected<ARInvoice> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			bool paymentsAllowed = eventArgs.Row.DocType.IsIn(ARDocType.Invoice, ARDocType.DebitMemo);
			bool refundsAllowed = eventArgs.Row.DocType == ARDocType.CreditMemo;
			bool inserted = eventArgs.Cache.GetStatus(eventArgs.Row) == PXEntryStatus.Inserted;
			bool docStatusAllowsPayment = eventArgs.Row.Status.IsIn(ARDocStatus.Hold, ARDocStatus.CCHold, ARDocStatus.CreditHold, ARDocStatus.Balanced);
			bool docStatusAllowsRefund = docStatusAllowsPayment || eventArgs.Row.Status.IsIn(ARDocStatus.PendingEmail, ARDocStatus.PendingPrint);
			bool createPaymentEnabled = paymentsAllowed && !inserted && docStatusAllowsPayment && eventArgs.Row.CuryUnpaidBalance > 0m;
			bool createRefundEnabled = refundsAllowed && !inserted && docStatusAllowsRefund && eventArgs.Row.CuryUnpaidBalance > 0m;

			bool importPaymentEnabled = createPaymentEnabled && eventArgs.Row.PaymentMethodID != null &&
				PaymentMethod.PK.Find(Base, eventArgs.Row.PaymentMethodID)?.PaymentType == PaymentMethodType.CreditCard;

			createDocumentPayment.SetVisible(paymentsAllowed);
			createDocumentPayment.SetEnabled(createPaymentEnabled);
			createDocumentRefund.SetVisible(refundsAllowed);
			createDocumentRefund.SetEnabled(createRefundEnabled);
			importDocumentPayment.SetVisible(paymentsAllowed);
			importDocumentPayment.SetEnabled(importPaymentEnabled);

			PXSetPropertyException hasLegacyCCTranException = null;
			var invoice = (SOInvoice)Base.SODocument.View.SelectMultiBound(new[] { eventArgs.Row }).FirstOrDefault();

			if (invoice?.HasLegacyCCTran == true)
			{
				hasLegacyCCTranException = new PXSetPropertyException(
					Messages.CantProcessSOInvoiceBecauseItHasLegacyCCTran, PXErrorLevel.Warning, eventArgs.Row.RefNbr);
			}

			eventArgs.Cache.RaiseExceptionHandling<ARInvoice.curyPaymentTotal>(
				eventArgs.Row, eventArgs.Row.CuryPaymentTotal, hasLegacyCCTranException);
		}

		protected virtual void _(Events.RowSelected<SOInvoice> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			bool allowPaymentInfo = Base.Document.Cache.AllowUpdate
				&& eventArgs.Row.DocType.IsIn(ARDocType.CashSale, ARDocType.CashReturn, ARDocType.Invoice, ARDocType.CreditMemo);
			bool isPMInstanceRequired = false;

			if (allowPaymentInfo && (String.IsNullOrEmpty(eventArgs.Row.PaymentMethodID) == false))
			{
				PaymentMethod pm = PaymentMethod.PK.Find(Base, eventArgs.Row.PaymentMethodID);
				isPMInstanceRequired = (pm?.IsAccountNumberRequired == true);
			}

			PXUIFieldAttribute.SetEnabled<SOInvoice.paymentMethodID>(Base.SODocument.Cache, eventArgs.Row, allowPaymentInfo);
			PXUIFieldAttribute.SetEnabled<SOInvoice.pMInstanceID>(Base.SODocument.Cache, eventArgs.Row, allowPaymentInfo && isPMInstanceRequired);
		}

		#endregion // Document events

		#region ARTran events

		protected virtual void _(Events.RowDeleted<ARTran> eventArgs)
		{
			MarkRefundAdjUpdatedForValidation(eventArgs.Row);
		}

		protected virtual void _(Events.RowUpdated<ARTran> eventArgs)
		{
			if (!eventArgs.Cache.ObjectsEqual<ARTran.qty>(eventArgs.OldRow, eventArgs.Row)
				&& eventArgs.Row.Qty == 0m)
			{
				MarkRefundAdjUpdatedForValidation(eventArgs.Row);
			}
		}

		#endregion // ARTran events

		#region ARAdjust events

		protected virtual void _(Events.RowSelected<ARAdjust> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			PXSetPropertyException exception = null;

			if (eventArgs.Row.AdjdDocType == ARDocType.CreditMemo &&
				eventArgs.Row.AdjgDocType == ARDocType.Refund &&
				PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() &&
				eventArgs.Row.IsCCPayment == true &&
				eventArgs.Row.IsCCAuthorized != true &&
				eventArgs.Row.IsCCCaptured != true &&
				eventArgs.Row.Voided != true &&
				eventArgs.Row.Released != true &&
				eventArgs.Row.PaymentReleased != true &&
				eventArgs.Row.PaymentPendingProcessing == true)
			{
				exception = new PXSetPropertyException(Messages.PaymentHasNoActiveRefundedTransaction, PXErrorLevel.RowWarning, eventArgs.Row.AdjgRefNbr);
				eventArgs.Cache.RaiseExceptionHandling<ARAdjust.displayRefNbr>(eventArgs.Row, null, exception);
			}
		}

		protected virtual void _(Events.RowPersisting<ARAdjust> eventArgs)
		{
			if (eventArgs.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update)
				|| eventArgs.Row.AdjgDocType != ARPaymentType.Refund
				|| !string.IsNullOrEmpty(eventArgs.Row.AdjdOrderNbr))
				return;

			ARPayment refund = ARPayment.PK.Find(Base, eventArgs.Row.AdjgDocType, eventArgs.Row.AdjgRefNbr);
			if (!string.IsNullOrEmpty(refund?.RefTranExtNbr)
				&& !HasReturnLineForOrigTran(refund.ProcessingCenterID, refund.RefTranExtNbr))
			{
				eventArgs.Cache.RaiseExceptionHandling<ARAdjust.displayDocType>(
					eventArgs.Row, eventArgs.Row.AdjgDocType,
					new PXSetPropertyException(Messages.OrigTranNumberNotRelatedToReturnedInvoices,
					PXErrorLevel.RowError, refund.RefTranExtNbr));
			}
		}

		#endregion // ARAdjust events

		#region SOQuickPayment events

		#region SOQuickPayment CacheAttached

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Switch<Case<Where<Current<ARInvoice.docType>, Equal<ARDocType.creditMemo>>, True>, False>))]
		protected virtual void _(Events.CacheAttached<SOQuickPayment.isRefund> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search2<ExternalTransaction.tranNumber,
			InnerJoin<ARPayment, On<ExternalTransaction.docType, Equal<ARPayment.docType>, And<ExternalTransaction.refNbr, Equal<ARPayment.refNbr>>>>,
			Where<ExternalTransaction.procStatus, Equal<ExtTransactionProcStatusCode.captureSuccess>,
				And<ARPayment.customerID, Equal<Current2<ARInvoice.customerID>>,
				And<ARPayment.paymentMethodID, Equal<Current2<SOQuickPayment.paymentMethodID>>>>>,
			OrderBy<Desc<ExternalTransaction.tranNumber>>>),
			typeof(ExternalTransaction.refNbr),
			typeof(ARPayment.docDate),
			typeof(ExternalTransaction.amount),
			typeof(ExternalTransaction.tranNumber))]
		[PXRestrictor(typeof(Where<Exists<Select2<ARAdjust,
			InnerJoin<ARTran, On<ARTran.origInvoiceType, Equal<ARAdjust.adjdDocType>, And<ARTran.origInvoiceNbr, Equal<ARAdjust.adjdRefNbr>>>>,
			Where<ARAdjust.adjgDocType, Equal<ARPayment.docType>, And<ARAdjust.adjgRefNbr, Equal<ARPayment.refNbr>,
				And<ARTran.tranType, Equal<Current2<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current2<ARInvoice.refNbr>>,
				And<ARAdjust.voided, NotEqual<True>, And<ARAdjust.curyAdjdAmt, NotEqual<decimal0>,
				And<ARTran.curyTranAmt, NotEqual<decimal0>>>>>>>>>>>),
			Messages.OrigTranNumberNotRelatedToReturnedInvoices, typeof(ExternalTransaction.tranNumber))]
		protected virtual void _(Events.CacheAttached<SOQuickPayment.refTranExtNbr> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Coalesce<
						Search2<Customer.defPMInstanceID, InnerJoin<CustomerPaymentMethod, On<CustomerPaymentMethod.pMInstanceID, Equal<Customer.defPMInstanceID>,
								And<CustomerPaymentMethod.bAccountID, Equal<Customer.bAccountID>>>>,
								Where<Customer.bAccountID, Equal<Current<ARInvoice.customerID>>,
								  And<CustomerPaymentMethod.isActive, Equal<True>,
								  And<CustomerPaymentMethod.paymentMethodID, Equal<Current2<SOQuickPayment.paymentMethodID>>>>>>,
						Search<CustomerPaymentMethod.pMInstanceID,
								Where<CustomerPaymentMethod.bAccountID, Equal<Current<ARInvoice.customerID>>,
									And<CustomerPaymentMethod.paymentMethodID, Equal<Current2<SOQuickPayment.paymentMethodID>>,
									And<CustomerPaymentMethod.isActive, Equal<True>>>>, OrderBy<Desc<CustomerPaymentMethod.expirationDate, Desc<CustomerPaymentMethod.pMInstanceID>>>>>)
						, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(Search<CustomerPaymentMethod.pMInstanceID, Where<CustomerPaymentMethod.bAccountID, Equal<Current2<ARInvoice.customerID>>,
			And<CustomerPaymentMethod.paymentMethodID, Equal<Current2<SOQuickPayment.paymentMethodID>>,
			And<Where<CustomerPaymentMethod.isActive, Equal<boolTrue>, Or<CustomerPaymentMethod.pMInstanceID,
					Equal<Current<SOQuickPayment.pMInstanceID>>>>>>>>), DescriptionField = typeof(CustomerPaymentMethod.descr))]
		protected virtual void _(Events.CacheAttached<SOQuickPayment.pMInstanceID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Coalesce<Search2<CustomerPaymentMethod.cashAccountID,
									InnerJoin<PaymentMethodAccount, On<PaymentMethodAccount.cashAccountID, Equal<CustomerPaymentMethod.cashAccountID>,
										And<PaymentMethodAccount.paymentMethodID, Equal<CustomerPaymentMethod.paymentMethodID>,
										And<PaymentMethodAccount.useForAR, Equal<True>>>>>,
									Where<CustomerPaymentMethod.bAccountID, Equal<Current<ARInvoice.customerID>>,
										And<CustomerPaymentMethod.pMInstanceID, Equal<Current2<SOQuickPayment.pMInstanceID>>>>>,
								Search2<CashAccount.cashAccountID,
								InnerJoin<PaymentMethodAccount, On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
									And<PaymentMethodAccount.useForAR, Equal<True>,
									And<PaymentMethodAccount.aRIsDefault, Equal<True>,
									And<PaymentMethodAccount.paymentMethodID, Equal<Current2<SOQuickPayment.paymentMethodID>>>>>>>,
									Where<CashAccount.branchID, Equal<Current<ARInvoice.branchID>>,
										And<Match<Current<AccessInfo.userName>>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[CashAccount(typeof(ARInvoice.branchID), typeof(Search2<CashAccount.cashAccountID,
				InnerJoin<PaymentMethodAccount,
					On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
						And<PaymentMethodAccount.useForAR, Equal<True>,
						And<PaymentMethodAccount.paymentMethodID,
						Equal<Current2<SOQuickPayment.paymentMethodID>>>>>>,
						Where<Match<Current<AccessInfo.userName>>>>), SuppressCurrencyValidation = false, Required = true)]
		protected virtual void _(Events.CacheAttached<SOQuickPayment.cashAccountID> eventArgs)
		{
		}

		#endregion // SOQuickPayment CacheAttached

		#endregion // SOQuickPayment events

		#region Override methods

		public override void AuthorizePayment(ARAdjust2 adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			paymentEntry.ForcePaymentApp = true;
			using (new ForcePaymentAppScope())
				base.AuthorizePayment(adjustment, paymentEntry, paymentTransaction);
		}

		public override void VoidPayment(ARAdjust2 adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			paymentEntry.IgnoreNegativeOrderBal = true; /// TODO: SOCreatePayment: Temporary fix ARPayment bug (AC-159389), after fix we should remove this code.

			// TODO: SOCreatePayment: Temporary fix ARPayment bug (AC-163765), after fix we should remove this code.
			if (adjustment.IsCCPayment == true && paymentEntry.Document.Current?.IsCCAuthorized == true)
			{
				ARAdjust paymentAdjust = paymentEntry.Adjustments.Locate(adjustment);
				if (paymentAdjust != null)
				{
					paymentEntry.Adjustments.Cache.SetValueExt<ARAdjust.curyAdjgAmt>(paymentAdjust, 0m);
					paymentEntry.Adjustments.Update(paymentAdjust);
				}
			}

			base.VoidPayment(adjustment, paymentEntry, paymentTransaction);
		}

		public override void CapturePayment(ARAdjust2 adjustment, ARPaymentEntry paymentEntry, ARPaymentEntryPaymentTransaction paymentTransaction)
		{
			paymentEntry.IgnoreNegativeOrderBal = true; // TODO: SOCreatePayment: Temporary fix ARPayment bug (AC-159389), after fix we should remove this method.
			paymentEntry.ForcePaymentApp = true;
			using (new ForcePaymentAppScope())
				base.CapturePayment(adjustment, paymentEntry, paymentTransaction);
		}

		protected override void RemoveUnappliedBalance(ARPaymentEntry paymentEntry)
		{
			var ordersToApplyTabExtension = paymentEntry.GetOrdersToApplyTabExtension(true);
			SOAdjust soadjust = ordersToApplyTabExtension.SOAdjustments.Select().AsEnumerable()
				.Where(a => a.GetItem<SOAdjust>().CuryAdjgAmt > 0m)
				.SingleOrDefault();

			if (soadjust != null)
			{
				ordersToApplyTabExtension.SOAdjustments.SetValueExt<SOAdjust.curyAdjgAmt>(soadjust, 0m);
				ordersToApplyTabExtension.SOAdjustments.Update(soadjust);
			}

			PXFormulaAttribute.CalcAggregate<SOAdjust.curyAdjgAmt>(ordersToApplyTabExtension.SOAdjustments.Cache, paymentEntry.Document.Current, false);

			base.RemoveUnappliedBalance(paymentEntry);
		}

		protected override PXSelectBase<ARAdjust2> GetAdjustView()
			=> Base.Adjustments;

		protected override PXSelectBase<ARAdjust> GetAdjustView(ARPaymentEntry paymentEntry)
			=> paymentEntry.Adjustments;

		protected override ARSetup GetARSetup()
			=> Base.arsetup.Current;

		protected override CustomerClass GetCustomerClass()
			=> Base.customerclass.SelectSingle();

		protected override void SetCurrentDocument(ARInvoice document)
		{
			Base.Document.Current = Base.Document.Search<ARInvoice.refNbr>(document.RefNbr, document.DocType);
		}

		protected override void AddAdjust(ARPaymentEntry paymentEntry, ARInvoice document)
		{
			var newAdjust = new ARAdjust2()
			{
				AdjdRefNbr = document.RefNbr,
				AdjdDocType = document.DocType
			};

			paymentEntry.Adjustments.Insert(newAdjust);
			ARInvoice updatedInvoice = (ARInvoice)paymentEntry.Caches[typeof(ARInvoice)].Locate(document);
			ARInvoice.Events.FireOnPropertyChanged<ARInvoice.pendingProcessing>(
				paymentEntry, updatedInvoice);
		}

		protected override void VerifyAdjustments(ARPaymentEntry paymentEntry, string actionName)
		{
			ARPayment payment = paymentEntry.Document.Current;

			if (IsMultipleApplications(paymentEntry))
			{
				if (actionName == nameof(CaptureDocumentPayment))
				{
					if (payment.DocType == ARDocType.Payment)
						throw new PXException(Messages.CapturePaymentWithMultipleApplicationsError, payment.RefNbr);
					else
						throw new PXException(Messages.CapturePrepaymentWithMultipleApplicationsError, payment.RefNbr);
				}
				else
				{
					if (payment.DocType == ARDocType.Payment)
						throw new PXException(Messages.VoidPaymentWithMultipleApplicationsError, payment.RefNbr);
					else
						throw new PXException(Messages.VoidPrepaymentWithMultipleApplicationsError, payment.RefNbr);
				}
			}
		}

		protected override string GetDocumentDescr(ARInvoice document) => document.DocDesc;

		protected override bool CanVoid(ARAdjust2 adjust, ARPayment payment)
			=> base.CanVoid(adjust, payment) && (payment.Released != true || adjust.Hold != true);

		protected override void ThrowExceptionIfDocumentHasLegacyCCTran()
		{
			SOInvoice doc = Base.SODocument.Select();

			if (doc?.HasLegacyCCTran == true)
				throw new PXException(Messages.CantProcessSOInvoiceBecauseItHasLegacyCCTran, doc.RefNbr);
		}

		[PXOverride]
		public virtual ARInvoiceState GetDocumentState(PXCache cache, ARInvoice doc, Func<PXCache, ARInvoice, ARInvoiceState> baseMethod)
		{
			ARInvoiceState state = baseMethod(cache, doc);
			SOInvoice invoice = Base.SODocument.Select();
			state.LoadDocumentsEnabled &= invoice?.HasLegacyCCTran != true;
			state.AllowUpdateAdjustments &= invoice?.HasLegacyCCTran != true;

			return state;
		}

		protected override Type GetPaymentMethodField()
			=> typeof(SOInvoice.paymentMethodID);

		protected override bool IsCashSale()
			=> Base.Document.Current?.DocType.IsIn(ARInvoiceType.CashSale, ARInvoiceType.CashReturn) == true;

		protected override string GetCCPaymentIsNotSupportedMessage()
			=> Messages.CCPaymentMethodIsNotSupportedInSOInvoiceCashSale;

		protected override Type GetDocumentPMInstanceIDField()
			=> typeof(ARInvoice.pMInstanceID);

		public override void CopyError(ARAdjust2 errorAdjustment, Exception exception)
		{
			if (GetCurrent<ARInvoice>()?.DocType == ARDocType.CreditMemo)
			{
				base.CopyError<ARAdjust>(nameof(ARAdjust2.DisplayDocType), errorAdjustment, exception);
			}
			else
			{
				base.CopyError(errorAdjustment, exception);
			}
		}

		protected override bool HasReturnLineForOrigTran(string procCenterID, string tranNumber)
		{
			if (string.IsNullOrEmpty(procCenterID))
			{
				throw new PXArgumentException(nameof(procCenterID));
			}
			if (string.IsNullOrEmpty(tranNumber))
			{
				throw new PXArgumentException(nameof(tranNumber));
			}
			ARTran returnLine = SelectFrom<ARTran>
				.InnerJoin<ARAdjust>.On<ARAdjust.adjdDocType.IsEqual<ARTran.origInvoiceType>
					.And<ARAdjust.adjdRefNbr.IsEqual<ARTran.origInvoiceNbr>>>
				.InnerJoin<ExternalTransaction>.On<ExternalTransaction.docType.IsEqual<ARAdjust.adjgDocType>
					.And<ExternalTransaction.refNbr.IsEqual<ARAdjust.adjgRefNbr>>>
				.Where<ARTran.tranType.IsEqual<ARInvoice.docType.FromCurrent>
					.And<ARTran.refNbr.IsEqual<ARInvoice.refNbr.FromCurrent>>
					.And<ExternalTransaction.processingCenterID.IsEqual<@P.AsString>>
					.And<ExternalTransaction.tranNumber.IsEqual<@P.AsString>>
					.And<ARAdjust.voided.IsNotEqual<True>>.And<ARAdjust.curyAdjdAmt.IsNotEqual<decimal0>>
					.And<ARTran.curyTranAmt.IsNotEqual<decimal0>>>
				.View.SelectWindowed(Base, 0, 1, procCenterID, tranNumber);
			return returnLine != null;
		}

		protected override bool ValidateCCRefundOrigTransaction()
		{
			//Validation can only be ignored on Sales Order level for now.
			return true;
		}
		protected virtual void MarkRefundAdjUpdatedForValidation(ARTran line)
		{
			if (line.TranType == ARDocType.CreditMemo && !string.IsNullOrEmpty(line.OrigInvoiceNbr))
			{
				foreach (ARAdjust adj in Base.Adjustments_1.Select())
				{
					Base.Adjustments_1.Cache.MarkUpdated(adj);
				}
			}
		}

		#endregion // Override methods
	}
}
