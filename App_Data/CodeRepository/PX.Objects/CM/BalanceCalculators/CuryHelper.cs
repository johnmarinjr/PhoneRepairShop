using PX.Data;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM
{
	public class CuryHelper : IPXCurrencyHelper
	{
		private PXSelectBase<CurrencyInfo> Currencyinfoselect { get; }

		public CuryHelper(PXSelectBase<CurrencyInfo> currencyinfoselect)
		{
			Currencyinfoselect = currencyinfoselect;
		}

		public CurrencyInfo GetCurrencyInfo(long? currencyInfoID) => CurrencyInfoCache.GetInfo(Currencyinfoselect, currencyInfoID);

		Extensions.CurrencyInfo IPXCurrencyHelper.GetCurrencyInfo(long? key) => Extensions.CurrencyInfo.GetEX(GetCurrencyInfo(key));

		public Extensions.CurrencyInfo GetDefaultCurrencyInfo() => Extensions.CurrencyInfo.GetEX(Currencyinfoselect.SelectSingle());
	}
}
