using CommonServiceLocator;
using PX.Data;
using PX.Objects.Extensions.MultiCurrency;
using System;
using System.Collections.Generic;

namespace PX.Objects.CM.Extensions
{
	internal static class CurrencyAttributeHelper
	{
		internal static int? GetPrecision(this PXGraph graph, PXCache sender, object row, string fieldName, Dictionary<long, string> _Matches)
		{
			return ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(graph).CuryDecimalPlaces(graph.GetCuryID(sender, row, fieldName, _Matches));
		}

		private static string GetCuryID(this PXGraph graph, PXCache sender, object row, string fieldName, Dictionary<long, string> _Matches)
		{
			if (_Matches == null)//same as ask "If IPXCurrencyHelper exists"
			{
				long? currencyInfoID = (long?)sender.GetValue(row ?? sender.InternalCurrent, fieldName);

				IPXCurrencyHelper pXCurrencyHelper = graph.FindImplementation<IPXCurrencyHelper>();
				CurrencyInfo currencyInfo = pXCurrencyHelper?.GetCurrencyInfo(currencyInfoID) ?? pXCurrencyHelper.GetDefaultCurrencyInfo();

				if (graph.Accessinfo.CuryViewState) return currencyInfo?.BaseCuryID;
				else return currencyInfo?.CuryID;
			}
			else
			{
				long? currencyInfoID = (long?)sender.GetValue(row, fieldName);

				if (currencyInfoID != null && _Matches.TryGetValue((long)currencyInfoID, out string cury))
					return cury;
				else return string.Empty;
			}
		}
	}
}
