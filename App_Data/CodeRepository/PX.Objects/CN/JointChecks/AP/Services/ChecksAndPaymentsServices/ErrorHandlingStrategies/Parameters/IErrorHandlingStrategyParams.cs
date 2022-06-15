using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public interface IErrorHandlingStrategyParams
    {
        bool ShowError { get; }
        bool ThrowError { get; }
        bool WithFieldValue { get; }
        string ErrorMessage { get; }
    }
}
