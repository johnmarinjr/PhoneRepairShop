using CommonServiceLocator;
using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.GL;
using System;

namespace PX.Objects.Extensions.MultiCurrency
{

	/// <summary>
	/// An implementation of IPXCurrencyHelper for a screen on which the same currency is always expected
	/// and CurrencyInfo is never persisted.
	/// </summary>
	public abstract class SingleCurrencyGraph<TGraph, TPrimary> : PXGraphExtension<TGraph>, IPXCurrencyHelper
			where TGraph : PXGraph
			where TPrimary : class, IBqlTable, new()
	{
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>> currencyinfobykey;

		public CurrencyInfo GetCurrencyInfo(long? key) => currencyinfobykey.Select(key);

		public CurrencyInfo GetDefaultCurrencyInfo()
		{
			IPXCurrencyService pXCurrencyService = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base);
			string baseCuryID = pXCurrencyService.BaseCuryID();
			short precision = Convert.ToInt16(pXCurrencyService.CuryDecimalPlaces(baseCuryID));
			return new CurrencyInfo
			{
				CuryID = baseCuryID,
				BaseCuryID = baseCuryID,
				CuryRate = 1m,
				RecipRate = 1m,
				CuryPrecision = precision,
				BasePrecision = precision
			};
		}
	}
}
