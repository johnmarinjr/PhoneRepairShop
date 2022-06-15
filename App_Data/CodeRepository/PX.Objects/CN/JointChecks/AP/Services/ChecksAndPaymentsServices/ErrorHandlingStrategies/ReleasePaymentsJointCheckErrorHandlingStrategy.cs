using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class ReleasePaymentsJointCheckErrorHandlingStrategy : IJointCheckErrorHandlingStrategy
    {
        private readonly APPaymentEntry graph;

        public ReleasePaymentsJointCheckErrorHandlingStrategy(APPaymentEntry graph)
        {
            this.graph = graph;
        }

        public APPaymentEntry Graph => graph;

        public void HandleError<TField>(IErrorHandlingStrategyParams parameters) where TField : IBqlField
        {
            ThrowError(parameters.ErrorMessage);
        }

        private static void ThrowError(string errorMessage)
        {
            throw new PXException(errorMessage, typeof(APPayment.refNbr).Name);
        }
    }
}
