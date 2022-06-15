using PX.Objects.CM.Extensions;

namespace PX.Objects.Extensions.MultiCurrency
{
	public interface IPXCurrencyHelper
	{
		CurrencyInfo GetCurrencyInfo(long? key);
		CurrencyInfo GetDefaultCurrencyInfo();
	}
}
