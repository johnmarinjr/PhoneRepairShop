using System;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.AP;
using PX.Objects.CN.JointChecks.AP.DAC;
using PX.Objects.CN.JointChecks.AP.Services.DataProviders;
using PX.Objects.CN.JointChecks.Descriptor;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class ReversedJointPayeePaymentsValidationService : ValidationServiceBase
    {
        public ReversedJointPayeePaymentsValidationService(APPaymentEntry graph, IJointCheckErrorHandlingStrategy jointCheckErrorHandlingStrategy)
            : base(graph, jointCheckErrorHandlingStrategy)
        {
        }

        public void Validate()
        {
            var atLeastOneVoidAdjustmentExist = Adjustments.Any(adjustment => adjustment.Voided == true);
            if (atLeastOneVoidAdjustmentExist)
            {
                var showErrorOnPersist = JointPayeePaymentDataProvider
                    .GetCurrentJointPayeePaymentsByVendorGroups(Graph, Graph.Document.Current)
                    .Select(ValidateReversedJointPayeePayments).Any(isValid => !isValid);
                if (showErrorOnPersist)
                {
                    var errorHandlingParams = new ThrowErrorOnlyParams(JointCheckMessages.JointPayeeAmountIsNotEqualToTheOriginalAmount, 
                        Graph.Caches[typeof(JointPayeePayment)].DisplayName);
                    errorHandlingStrategy.HandleError<JointPayeePayment.jointAmountToPay>(errorHandlingParams);
                }
            }
        }

        private bool ValidateReversedJointPayeePayments(List<JointPayeePayment> jointPayeePayments)
        {
            if (jointPayeePayments.Sum(jpp => jpp.JointAmountToPay) == 0)
            {
                return true;
            }
            foreach (var jointPayeePayment in jointPayeePayments)
            {
                var errorHandlingParams = new ShowErrorOnlyParams(JointCheckMessages.JointPayeeAmountIsNotEqualToTheOriginalAmount, 
                    jointPayeePayment);
                errorHandlingStrategy.HandleError<JointPayeePayment.jointAmountToPay>(errorHandlingParams);
            }
            return false;
        }
    }
}
