using System;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.AP.Services.CalculationServices;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class VendorPaymentAmountValidationService : ValidationServiceBase
    {
        private readonly JointAmountToPayCalculationService jointAmountToPayCalculationService;

        public VendorPaymentAmountValidationService(APPaymentEntry graph, IJointCheckErrorHandlingStrategy jointCheckErrorHandlingStrategy)
            : base(graph, jointCheckErrorHandlingStrategy)
        {
            jointAmountToPayCalculationService = new JointAmountToPayCalculationService(graph);
        }

        public void Validate(string errorMessage)
        {
            foreach (var adjustment in ActualBillAdjustments)
            {
                var invoice = InvoiceDataProvider.GetInvoice(Graph, adjustment.AdjdDocType, adjustment.AdjdRefNbr);
                var invoiceExt = invoice.GetExtension<APInvoiceJCExt>();
                if (invoiceExt.IsJointPayees == true)
                {
                    ValidateVendorPaymentAmount(adjustment, errorMessage);
                }
            }
        }

        public void ValidateVendorPaymentAmount(APAdjust adjustment, string errorMessage)
        {
            InitializeServices(adjustment.AdjdLineNbr != 0);
            var totalJointAmountToPay = jointAmountToPayCalculationService.GetTotalJointAmountToPay(adjustment);
            var vendorPaymentAmount = adjustment.CuryAdjgAmt - totalJointAmountToPay;
            var vendorPreparedBalance = VendorPreparedBalanceCalculationService.GetVendorPreparedBalance(adjustment);
            if (vendorPaymentAmount > vendorPreparedBalance)
            {
                var totalNonReleasedCashDiscountTaken =
                    CashDiscountCalculationService.GetNonReleasedCashDiscountTakenExceptCurrentAdjustment(adjustment) +
                    adjustment.CuryAdjgPPDAmt;
                var allowableAmountPaid =
                    vendorPreparedBalance + totalJointAmountToPay - totalNonReleasedCashDiscountTaken;
                allowableAmountPaid = Math.Max(allowableAmountPaid.GetValueOrDefault(), 0);

                var errorHandlingParams = new ShowAndThrowErrorParams(errorMessage, adjustment, 
                    Graph.Adjustments.Cache.DisplayName, allowableAmountPaid);
                errorHandlingStrategy.HandleError<APAdjust.curyAdjgAmt>(errorHandlingParams);
            }
        }
    }
}
