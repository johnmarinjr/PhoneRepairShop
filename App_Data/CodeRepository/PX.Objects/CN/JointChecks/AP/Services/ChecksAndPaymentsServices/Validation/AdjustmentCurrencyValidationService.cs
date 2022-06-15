using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.Descriptor;
using System;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class AdjustmentCurrencyValidationService : ValidationServiceBase
    {
        public AdjustmentCurrencyValidationService(APPaymentEntry graph, IJointCheckErrorHandlingStrategy jointCheckErrorHandlingStrategy)
            : base(graph, jointCheckErrorHandlingStrategy)
        {
        }

        
        public void Validate()
        {
            var payment = Graph.Document.Current;
            if (payment.Hold == true) return;
            var paymentExt = PXCache<APPayment>.GetExtension<ApPaymentExt>(payment);
            foreach (var adjustment in ActualBillAdjustments)
            {
                var invoice = InvoiceDataProvider.GetInvoice(Graph, adjustment.AdjdDocType, adjustment.AdjdRefNbr);
                if (paymentExt.IsJointCheck == true && paymentExt.JointPaymentAmount > 0 && payment.CuryID != invoice.CuryID)
                {
                    var errorHandlingParams = new ShowAndThrowErrorParams(JointCheckMessages.PaymentCurrencyDiffersFromBill, 
                        adjustment, Graph.Adjustments.Cache.DisplayName);
                    errorHandlingStrategy.HandleError<APAdjust.curyAdjgAmt>(errorHandlingParams);
                }
            }
        }
    }
}
