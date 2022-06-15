using PX.Objects.CM.Extensions;
using System;

namespace PX.Objects.Extensions.MultiCurrency
{
	public interface ICurrencyHelperEx : IPXCurrencyHelper
	{
		CurrencyInfo CloneCurrencyInfo(CurrencyInfo currencyInfo);
		CurrencyInfo CloneCurrencyInfo(CurrencyInfo currencyInfo, DateTime? currencyEffectiveDate);
	}
}
