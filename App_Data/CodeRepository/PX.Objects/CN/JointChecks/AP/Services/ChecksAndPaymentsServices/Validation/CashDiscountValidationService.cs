using PX.Objects.AP;
using PX.Objects.CN.JointChecks.Descriptor;
using System;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class CashDiscountValidationService : ValidationServiceBase
    {
        public CashDiscountValidationService(APPaymentEntry graph, IJointCheckErrorHandlingStrategy jointCheckErrorHandlingStrategy)
            : base(graph, jointCheckErrorHandlingStrategy)
        {
        }
		       
        public void Validate()
        {
            foreach (var adjustment in ActualAdjustments)
            {
                InitializeServices(adjustment.AdjdLineNbr != 0);
                var allowableCashDiscount = CashDiscountCalculationService.GetAllowableCashDiscount(adjustment);
                if (adjustment.CuryAdjgPPDAmt > allowableCashDiscount)
                {
                    var errorHandlingParams = 
                        new ShowAndThrowErrorParams(JointCheckMessages.AmountPaidWithCashDiscountTakenExceedsVendorBalance, 
                        adjustment, Graph.Adjustments.Cache.DisplayName, allowableCashDiscount);
                    errorHandlingStrategy.HandleError<APAdjust.curyAdjgAmt>(errorHandlingParams);
                }
            }
        }
    }
}
