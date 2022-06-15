using PX.Data;
using PX.Objects.AP;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
	internal class APPaymentBalanceCalculator : AbstractPaymentBalanceCalculator<APAdjust,APTran>
	{
		public APPaymentBalanceCalculator(PXSelectBase<CM.CurrencyInfo> curyInfoSelect) : this(new CM.CuryHelper(curyInfoSelect))
		{
		}

		public APPaymentBalanceCalculator(IPXCurrencyHelper curyhelper) : base(curyhelper)
		{
		}

		protected override void AfterBalanceCalculatedBeforeBalanceAjusted<T>(APAdjust adj, T voucher, bool DiscOnDiscDate, APTran tran)
		{
			if (DiscOnDiscDate) PaymentEntry.CalcDiscount(adj.AdjgDocDate, voucher, adj);

			base.AfterBalanceCalculatedBeforeBalanceAjusted(adj, voucher, DiscOnDiscDate, tran);
		}

		protected override bool ShouldRgolBeResetInZero(APAdjust adj) =>
			(adj.AdjgDocType == APDocType.Check || adj.AdjgDocType == APDocType.VoidCheck || adj.AdjgDocType == APDocType.Prepayment)
			&& adj.AdjdDocType == APDocType.Prepayment;
	}
}
