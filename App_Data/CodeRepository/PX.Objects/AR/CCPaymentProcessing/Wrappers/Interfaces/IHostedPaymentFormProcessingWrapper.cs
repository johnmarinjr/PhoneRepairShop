using PX.Data;
using PX.Objects.AR.CCPaymentProcessing.Common;

using V2 = PX.CCProcessingBase.Interfaces.V2;
namespace PX.Objects.AR.CCPaymentProcessing.Wrappers
{
	public interface IHostedPaymentFormProcessingWrapper
	{
		void GetPaymentForm(V2.ProcessingInput inputData);
		HostedFormResponse ParsePaymentFormResponse(string response);
		PXPluginRedirectOptions PreparePaymentForm(V2.PaymentFormPrepareOptions inputData);
		V2.PaymentFormResponseProcessResult ProcessPaymentFormResponse(V2.PaymentFormPrepareOptions inputData, string response);
	}
}
