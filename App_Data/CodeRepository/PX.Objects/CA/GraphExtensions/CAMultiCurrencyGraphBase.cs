using PX.Data;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.CM.Extensions;

namespace PX.Objects.CA.MultiCurrency
{
	/// <summary>The generic graph extension that defines the multi-currency functionality.</summary>
	/// <typeparam name="TGraph">A <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TPrimary">A DAC (a <see cref="PX.Data.IBqlTable" /> type).</typeparam>
	public abstract class CAMultiCurrencyGraph<TGraph, TPrimary> : MultiCurrencyGraph<TGraph, TPrimary>
			where TGraph : PXGraph
			where TPrimary : class, IBqlTable, new()
	{
		internal virtual void UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(PXCache tranDetailsCache, ICATranDetail oldTranDetail, ICATranDetail newTranDetail)
		{
			if (tranDetailsCache == null || newTranDetail == null)
			{
				return;
			}

			bool priceChanged = (oldTranDetail?.CuryUnitPrice ?? 0m) != (newTranDetail.CuryUnitPrice ?? 0m);
			bool amtChanged = (oldTranDetail?.CuryTranAmt ?? 0m) != (newTranDetail.CuryTranAmt ?? 0m);
			bool qtyChanged = (oldTranDetail?.Qty ?? 0m) != (newTranDetail.Qty ?? 0m);

			if (amtChanged)
			{
				if (newTranDetail.Qty != null && newTranDetail.Qty != 0m)
				{
					decimal curyUnitPriceToRound = (newTranDetail.CuryTranAmt ?? 0m) / newTranDetail.Qty.Value;
					CurrencyInfo currencyInfo = GetDefaultCurrencyInfo();
					newTranDetail.CuryUnitPrice = currencyInfo.RoundCury(curyUnitPriceToRound);
				}
				else
				{
					newTranDetail.CuryUnitPrice = newTranDetail.CuryTranAmt;
					newTranDetail.Qty = 1.0m;
				}
			}
			else if (priceChanged || qtyChanged)
			{
				newTranDetail.CuryTranAmt = newTranDetail.Qty * newTranDetail.CuryUnitPrice;
			}
		}
	}

}