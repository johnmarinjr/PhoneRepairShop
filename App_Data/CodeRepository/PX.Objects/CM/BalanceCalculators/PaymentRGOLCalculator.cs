using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
	public class PaymentRGOLCalculator
	{
		private readonly RGOLCalculator rGOLCalculator;
		private readonly IAdjustment adj;
		private readonly bool? reverseGainLoss;

		public PaymentRGOLCalculator(IPXCurrencyHelper curyHelper, IAdjustment adj, bool? reverseGainLoss)
		{
			this.adj = adj;
			this.reverseGainLoss = reverseGainLoss;

			CurrencyInfo payment_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);
			CurrencyInfo voucher_info = curyHelper.GetCurrencyInfo(adj.AdjdCuryInfoID);
			CurrencyInfo voucher_originfo = curyHelper.GetCurrencyInfo(adj.AdjdOrigCuryInfoID);

			rGOLCalculator = new RGOLCalculator(
				payment_info,
				voucher_info,
				voucher_originfo
				);
		}

		public void Calculate(IInvoice voucher, IDocumentTran tran = null)
		{
			decimal? documentCuryDiscBal = tran?.CuryCashDiscBal ?? voucher.CuryDiscBal;
			decimal? documentBaseDiscBal = tran?.CashDiscBal ?? voucher.DiscBal;

			RGOLCalculationResult CuryAdjdDiscAmtRgol = rGOLCalculator.CalcRGOL(
				adj.CuryDiscBal,
				documentCuryDiscBal,
				documentBaseDiscBal,
				adj.CuryAdjgDiscAmt,
				adj.AdjDiscAmt);
			adj.CuryAdjdDiscAmt = CuryAdjdDiscAmtRgol.ToCuryAdjAmt;

			RGOLCalculationResult CuryAdjdWhTaxAmtRgol = rGOLCalculator.CalcRGOL(adj.CuryAdjgWhTaxAmt, adj.AdjWhTaxAmt);
			adj.CuryAdjdWhTaxAmt = CuryAdjdWhTaxAmtRgol.ToCuryAdjAmt;

			decimal? curyAvailableBal = tran?.CuryTranBal ?? voucher.CuryDocBal;
			decimal? baseAvailableBal = tran?.TranBal ?? voucher.DocBal;

			RGOLCalculationResult CuryAdjdAmtRgol;

			if (adj.CuryDocBal == decimal.Zero)
			{
				CuryAdjdAmtRgol = new RGOLCalculationResult
				{
					ToCuryAdjAmt = curyAvailableBal - adj.CuryAdjdDiscAmt - adj.CuryAdjdWhTaxAmt,
					RgolAmt = (decimal)baseAvailableBal
					- (decimal)adj.AdjDiscAmt
					- (decimal)adj.AdjWhTaxAmt
					- (decimal)adj.AdjAmt
					- CuryAdjdDiscAmtRgol.RgolAmt
					- CuryAdjdWhTaxAmtRgol.RgolAmt
				};
			}
			else
			{
				CuryAdjdAmtRgol = rGOLCalculator.CalcRGOL(
					adj.CuryDocBal,
					curyAvailableBal,
					baseAvailableBal,
					adj.CuryAdjgAmt,
					adj.AdjAmt);
			}
			adj.CuryAdjdAmt = CuryAdjdAmtRgol.ToCuryAdjAmt;
			adj.RGOLAmt = CuryAdjdDiscAmtRgol.RgolAmt + CuryAdjdWhTaxAmtRgol.RgolAmt + CuryAdjdAmtRgol.RgolAmt;
			
			if (reverseGainLoss == true) adj.RGOLAmt *= -1m;
		}
	}
}
