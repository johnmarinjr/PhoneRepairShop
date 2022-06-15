using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
	class PaymentBalanceAjuster
	{
		private readonly IPXCurrencyHelper curyHelper;

		public PaymentBalanceAjuster(IPXCurrencyHelper curyHelper) => this.curyHelper = curyHelper;

		public void AdjustBalance(IAdjustment adj) => AdjustBalance(
				adj,
				adj.AdjgCuryInfoID,
				adj.CuryAdjgAmt,
				adj.CuryAdjgDiscAmt,
				adj.CuryAdjgWhTaxAmt,
				true);

		public void AdjustBalance(
			IAdjustment adj,
			long? toCuryInfoID,
			decimal? curyAdjAmt,
			decimal? curyAdjDiscAmt,
			decimal? curyAdjWhTaxAmt,
			bool adjustBalanceByExtraAmounts)
		{
			if (adj.Released == true) return;

			CurrencyInfo to_info = curyHelper.GetCurrencyInfo(toCuryInfoID);
			if (curyAdjAmt != null)
			{
				decimal decimalValue = (decimal)curyAdjAmt;
				decimal to_adjamt = to_info.CuryConvBase(decimalValue);

				adj.AdjAmt = to_adjamt;
				adj.CuryDocBal -= decimalValue;

				if (adj.CuryDocBal == 0m)
				{
					adj.AdjAmt = adj.DocBal;
					adj.DocBal = 0m;
				}
				else
				{
					adj.DocBal = AdjustWhenTheSameCuryAndRate(adj, adj.CuryDocBal, adj.DocBal - to_adjamt);
				}
			}

			if (curyAdjWhTaxAmt != null)
			{
				decimal decimalValue = (decimal)curyAdjWhTaxAmt;
				decimal to_adjwhtaxamt = to_info.CuryConvBase(decimalValue);

				adj.AdjWhTaxAmt = to_adjwhtaxamt;
				adj.CuryWhTaxBal -= decimalValue;
				adj.WhTaxBal = adj.CuryWhTaxBal == 0m ? 0m : adj.WhTaxBal - to_adjwhtaxamt;

				if (adjustBalanceByExtraAmounts)
				{
					adj.CuryDocBal -= decimalValue;

					if (adj.CuryDocBal == 0m)
					{
						adj.DocBal = 0m;
					}
					else
					{
						adj.DocBal = AdjustWhenTheSameCuryAndRate(adj, adj.CuryDocBal, adj.DocBal - to_adjwhtaxamt);
					}
				}
			}

			if (curyAdjDiscAmt != null)
			{
				decimal decimalValue = (decimal)curyAdjDiscAmt;
				decimal to_adjdiscamt = to_info.CuryConvBase( decimalValue);

				adj.AdjDiscAmt = to_adjdiscamt;
				adj.CuryDiscBal -= decimalValue;
				adj.DiscBal = adj.CuryDiscBal == 0m ? 0m : adj.DiscBal - to_adjdiscamt;

				if (adjustBalanceByExtraAmounts)
				{
					adj.CuryDocBal -= decimalValue;

					if (adj.CuryDocBal == 0m)
					{
						adj.DocBal = 0m;
					}
					else
					{
						adj.DocBal = AdjustWhenTheSameCuryAndRate(adj, adj.CuryDocBal, adj.DocBal - to_adjdiscamt);
					}
				}
			}
		}

		/// <summary>
		/// prevent discrepancy due rounding (unexpected for user in that case)
		/// </summary>
		public decimal? AdjustWhenTheSameCuryAndRate(IAdjustment adj, decimal? curyValue, decimal? value)
		{
			CurrencyInfo invoice_info = curyHelper.GetCurrencyInfo(adj.AdjdOrigCuryInfoID);
			CurrencyInfo payment_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);

			bool isSameCuryAndRate =
				invoice_info.CuryID == payment_info.CuryID &&
				invoice_info.CuryRate == payment_info.CuryRate &&
				invoice_info.RecipRate == payment_info.RecipRate;

			if (isSameCuryAndRate) return invoice_info.CuryConvBase((decimal)curyValue);
			else return value;
		}
	}
}
