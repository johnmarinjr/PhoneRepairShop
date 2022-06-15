using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public interface IJointCheckErrorHandlingStrategy
    {
        APPaymentEntry Graph { get; }
        void HandleError<TField>(IErrorHandlingStrategyParams parameters) where TField : IBqlField;
    }
}
