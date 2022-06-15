using PX.Data;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.CA
{
	public class DirectDepositType
	{
		public string Code;
		public string Description;
	}

	public interface IDirectDepositType
	{
		bool IsActive();
		DirectDepositType GetDirectDepositType();
		IEnumerable<PaymentMethodDetail> GetDefaults();
		void SetPaymentMethodDefaults(PXCache cache);
	}
}
