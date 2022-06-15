﻿using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services;
using PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation;
using PX.Objects.CN.JointChecks.Descriptor;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;

namespace PX.Objects.CN.JointChecks.AP.GraphExtensions.PaymentEntry
{
    public class ApPaymentEntryValidationExt : PXGraphExtension<APPaymentEntry>
    {
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.construction>();
		}

        private AdjustmentCurrencyValidationService adjustmentCurrencyValidationService;
        private AdjustmentAmountPaidValidationService adjustmentAmountPaidValidationService;
        private CashDiscountValidationService cashDiscountValidationService;
        private DebitAdjustmentsValidationService debitAdjustmentsValidationService;
        private JointAmountToPayValidationService jointAmountToPayValidationService;
        private PaymentCycleWorkflowValidationService paymentCycleWorkflowValidationService;
        private ReversedJointPayeePaymentsValidationService reversedJointPayeePaymentsValidationService;
        private VendorPaymentAmountValidationService vendorPaymentAmountValidationService;

        public override void Initialize()
        {
            var errorHandlingStrategy = InitializeErrorHanldingStrategy();

            adjustmentCurrencyValidationService = new AdjustmentCurrencyValidationService(Base, errorHandlingStrategy);
            adjustmentAmountPaidValidationService = new AdjustmentAmountPaidValidationService(Base, errorHandlingStrategy);
            cashDiscountValidationService = new CashDiscountValidationService(Base, errorHandlingStrategy);
            debitAdjustmentsValidationService = new DebitAdjustmentsValidationService(Base, errorHandlingStrategy);
            jointAmountToPayValidationService = new JointAmountToPayValidationService(Base, errorHandlingStrategy);
            paymentCycleWorkflowValidationService = new PaymentCycleWorkflowValidationService(Base, errorHandlingStrategy);
            reversedJointPayeePaymentsValidationService = new ReversedJointPayeePaymentsValidationService(Base, errorHandlingStrategy);
            vendorPaymentAmountValidationService = new VendorPaymentAmountValidationService(Base, errorHandlingStrategy);
        }

        public virtual void APPayment_RowPersisting(PXCache cache, PXRowPersistingEventArgs args)
        {
            if (args.Operation != PXDBOperation.Delete && isValidScreenToProcess)
            {
                switch (Base.Document.Current?.DocType)
                {
                    case APDocType.Check:
                        ValidateCheck();
                        break;
                    case APDocType.DebitAdj:
                        ValidateDebitAdjustment();
                        break;
                    case APDocType.Prepayment:
                        ValidatePrepayment();
                        break;
                }
            }
        }

        public virtual void _(Events.RowInserted<APAdjust> args)
        {
            if (args.Row?.AdjdDocType == APDocType.Invoice && IsCheck())
            {
                paymentCycleWorkflowValidationService.Validate(args.Row.SingleToList());
            }
        }

        public virtual void APAdjust_CuryAdjdAmt_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs args)
        {
            if (args.Row is APAdjust adjust && adjust.Voided == false && IsCheck())
            {
                adjustmentAmountPaidValidationService.ValidateAmountPaid(adjust, (decimal) args.NewValue, false);
            }
        }

        private bool IsCheck()
        {
            return Base.Document.Current?.DocType == APDocType.Check;
        }

        private void ValidateCheck()
        {
            adjustmentCurrencyValidationService.Validate();
            paymentCycleWorkflowValidationService.Validate();
            reversedJointPayeePaymentsValidationService.Validate();
            jointAmountToPayValidationService.ValidateJointAmountToPayExceedBalance();
            cashDiscountValidationService.Validate();
            vendorPaymentAmountValidationService.Validate(JointCheckMessages.AmountPaidExceedsVendorBalance);
            adjustmentAmountPaidValidationService.ValidateAmountsPaid();
            debitAdjustmentsValidationService.ValidateDebitAdjustmentsIfRequired();
        }

        private void ValidatePrepayment()
        {
            adjustmentCurrencyValidationService.Validate();
            vendorPaymentAmountValidationService.Validate(JointCheckMessages.AmountPaidExceedsVendorBalanceForPrepayment);
        }

        private void ValidateDebitAdjustment()
        {
            adjustmentCurrencyValidationService.Validate();
            vendorPaymentAmountValidationService.Validate(JointCheckMessages.AmountPaidExceedsVendorBalanceForDebitAdjustment);
            cashDiscountValidationService.Validate();
        }

        private IJointCheckErrorHandlingStrategy InitializeErrorHanldingStrategy()
        {
            if (SiteMapExtension.IsChecksAndPaymentsScreenId())
            {
                return new ChecksAndPaymentsJointCheckErrorHandlingStrategy(Base);
            }
            return new ReleasePaymentsJointCheckErrorHandlingStrategy(Base);
        }

        private bool isValidScreenToProcess => SiteMapExtension.IsChecksAndPaymentsScreenId() ||
            SiteMapExtension.IsReleasePaymentsScreenId();

    }
}
