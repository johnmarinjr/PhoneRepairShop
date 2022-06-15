using System.Collections.Generic;

using PX.CCProcessingBase.Interfaces.V2;

using V2 = PX.CCProcessingBase.Interfaces.V2;

namespace PX.Objects.AR.CCPaymentProcessing.Wrappers
{
	public interface IHostedFromProcessingWrapper
	{
		void GetCreateForm();
		IEnumerable<V2.CreditCardData> GetMissingPaymentProfiles();
		void GetManageForm();
		ProfileFormResponseProcessResult ProcessProfileFormResponse(string response);
		void PrepareProfileForm();
	}
}
