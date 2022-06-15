using CommonServiceLocator;
using PX.Data;
using System;

namespace PX.Objects.CM.Extensions
{
	public static class CurrencyServiceHelper
	{
		public static IPXCurrencyRate SearchForNewRate(this CurrencyInfo info, PXGraph graph) => ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(graph)
				.GetRate(info.CuryID, info.BaseCuryID, info.CuryRateTypeID, info.CuryEffDate);

		public static void Populate(this IPXCurrencyRate rate, CurrencyInfo info)
		{
			info.CuryEffDate = rate.CuryEffDate;
			info.CuryRate = Math.Round((decimal)rate.CuryRate, 8);
			info.CuryMultDiv = rate.CuryMultDiv;
			info.RecipRate = Math.Round((decimal)rate.RateReciprocal, 8);
		}
	}
}
