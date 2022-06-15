using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
	public class CalculatedBalance
	{
		public decimal Balance { get; set; }
		public decimal CuryBalance { get; set; }
	}

	public class PaymentBalanceCalculator
	{
		private readonly IPXCurrencyHelper curyHelper;

		public PaymentBalanceCalculator(IPXCurrencyHelper curyHelper) => this.curyHelper = curyHelper;

		/// <summary>
		/// The method to initialize application
		/// balances in Payment currency.
		/// </summary>
		public void CalcBalances(
			long? PaymentCuryInfoID,
			long? VoucherPayCuryInfoID,
			IInvoice voucher,
			IAdjustment adj,
			IDocumentTran tran = null)
		{
			CalculatedBalance DocBalance = CalcBalance(
				PaymentCuryInfoID,
				VoucherPayCuryInfoID,
				tran?.CuryInfoID ?? voucher.CuryInfoID,
				tran?.CuryTranBal ?? voucher.CuryDocBal,
				tran?.TranBal ?? voucher.DocBal);

			adj.CuryDocBal = DocBalance.CuryBalance;
			adj.DocBal = DocBalance.Balance;

			CalculatedBalance DiscBalance = CalcBalance(
				PaymentCuryInfoID,
				VoucherPayCuryInfoID,
				tran?.CuryInfoID ?? voucher.CuryInfoID,
				tran?.CuryCashDiscBal ?? voucher.CuryDiscBal,
				tran?.CashDiscBal ?? voucher.DiscBal);

			adj.CuryDiscBal = DiscBalance.CuryBalance;
			adj.DiscBal = DiscBalance.Balance;

			CalculatedBalance WHTaxBalance = CalcBalance(
				PaymentCuryInfoID,
				VoucherPayCuryInfoID,
				voucher.CuryInfoID,
				voucher.CuryWhTaxBal,
				voucher.WhTaxBal);

			adj.CuryWhTaxBal = WHTaxBalance.CuryBalance;
			adj.WhTaxBal = WHTaxBalance.Balance;
		}

		public CalculatedBalance CalcBalance(
			long? toCuryInfoID,
			long? fromCuryInfoID,
			long? fromOrigInfoID,
			decimal? fromCuryDocBal,
			decimal? fromDocBal)
		{
			CurrencyInfo to_info = curyHelper.GetCurrencyInfo(toCuryInfoID);
			CurrencyInfo from_originfo = curyHelper.GetCurrencyInfo(fromOrigInfoID);

			if (Equals(to_info.CuryID, from_originfo.CuryID)) return new CalculatedBalance
			{
				CuryBalance = fromCuryDocBal.Value,
				Balance = to_info.CuryConvBase(fromCuryDocBal.Value)
			};
			else if (Equals(from_originfo.CuryID, from_originfo.BaseCuryID)) return new CalculatedBalance
			{
				CuryBalance = to_info.CuryConvCury(fromDocBal.Value),
				Balance = fromDocBal.Value
			};
			else
			{
				CurrencyInfo from_info = curyHelper.GetCurrencyInfo(fromCuryInfoID);
				decimal toDocBal = from_info.CuryConvBaseRaw(fromCuryDocBal.Value);
				decimal toCuryDocBal = to_info.CuryConvCury(toDocBal);

				return new CalculatedBalance
				{
					CuryBalance = toCuryDocBal,
					Balance = to_info.CuryConvBase(toCuryDocBal)
				};
			}
		}

		public decimal GetAdjdCuryRate(IAdjustment adj)
		{
			CurrencyInfo pay_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);
			CurrencyInfo vouch_info = curyHelper.GetCurrencyInfo(adj.AdjdCuryInfoID);

			if (vouch_info != null && string.Equals(pay_info.CuryID, vouch_info.CuryID) == false)
			{
				decimal voucherCuryRateMultiplier = vouch_info.CuryMultDiv == CuryMultDivType.Mult
					? vouch_info.CuryRate.Value
					: 1 / vouch_info.CuryRate.Value;
				decimal payInfoCuryRateMultiplier = pay_info.CuryMultDiv == CuryMultDivType.Mult
					? 1 / pay_info.CuryRate.Value
					: pay_info.CuryRate.Value;
				return Math.Round(voucherCuryRateMultiplier * payInfoCuryRateMultiplier, 8, MidpointRounding.AwayFromZero);
			}
			else return 1m;
		}
	}
}
