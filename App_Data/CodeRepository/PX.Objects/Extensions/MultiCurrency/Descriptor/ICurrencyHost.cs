using PX.Data;
using PX.Objects.CM.Extensions;
using System;

namespace PX.Objects.Extensions.MultiCurrency
{
	public interface ICurrencyHost
	{
		bool IsTrackedType(Type dacType);
		CurrencyInfo GetCurrencyInfo(PXCache sender, object row, string curyInfoIDField);
	}
}
