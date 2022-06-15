using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common;

namespace PX.Objects.CM.Extensions
{
	internal class ARPaymentBalanceCalculator : AbstractPaymentBalanceCalculator<ARAdjust, ARTran>
	{
		private readonly ARPaymentEntry Base;

		public ARPaymentBalanceCalculator(ARPaymentEntry graph) : base(graph.GetExtension<ARPaymentEntry.MultiCurrency>())
		{
			Base = graph;
		}

		protected override T AjustInvoiceBalanceForAutoApply<T>(ARAdjust adj, T originalInvoice)
		{
			if (!Base.AutoPaymentApp) return originalInvoice;
			try
			{
				ARAdjust unreleased = GetSisterAdjustmentsJustCreatedByAutoApplicationAggregated(adj);
				if (unreleased?.AdjdRefNbr == null) return originalInvoice;
				else
				{
					FullBalanceDelta balanceDelta = BalanceCalculation.GetFullBalanceDelta(unreleased);

					T invoice = PXCache<T>.CreateCopy(originalInvoice);
					invoice.CuryDocBal -= balanceDelta.CurrencyAdjustedBalanceDelta;
					invoice.DocBal -= balanceDelta.BaseAdjustedBalanceDelta;
					invoice.CuryDiscBal -= unreleased.CuryAdjdDiscAmt;
					invoice.DiscBal -= unreleased.AdjDiscAmt;
					invoice.CuryWhTaxBal -= unreleased.CuryAdjdWOAmt;
					invoice.WhTaxBal -= unreleased.AdjWOAmt;
					return invoice;
				}
			}
			finally
			{
				Base.AutoPaymentApp = false;
			}
		}

		private ARAdjust GetSisterAdjustmentsJustCreatedByAutoApplicationAggregated(ARAdjust adj)
		{
			Base.internalCall = true;
			try
			{
				return PXSelectGroupBy<
					ARAdjust,
					Where<ARAdjust.adjdDocType, Equal<Required<ARAdjust.adjdDocType>>,
						And<ARAdjust.adjdRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>,
						And<ARAdjust.released, Equal<False>,
						And<ARAdjust.voided, Equal<False>,
						And<Where<ARAdjust.adjgDocType, NotEqual<Required<ARAdjust.adjgDocType>>,
							Or<ARAdjust.adjgRefNbr, NotEqual<Required<ARAdjust.adjgRefNbr>>>>>>>>>,
					Aggregate<
						GroupBy<ARAdjust.adjdDocType,
						GroupBy<ARAdjust.adjdRefNbr,
							Sum<ARAdjust.curyAdjdAmt,
							Sum<ARAdjust.adjAmt,
							Sum<ARAdjust.curyAdjdDiscAmt,
							Sum<ARAdjust.adjDiscAmt>>>>>>>>
					.Select(Base, adj.AdjdDocType, adj.AdjdRefNbr, adj.AdjgDocType, adj.AdjgRefNbr);
			}
			finally
			{
				Base.internalCall = false;
			}
		}

		protected override void AfterBalanceCalculatedBeforeBalanceAjusted<T>(ARAdjust adj, T invoice, bool DiscOnDiscDate, ARTran tran)
		{
			adj.CuryOrigDocAmt = tran?.CuryOrigTranAmt ?? invoice.CuryOrigDocAmt;
			adj.OrigDocAmt = tran?.OrigTranAmt ?? invoice.OrigDocAmt;

			if (DiscOnDiscDate)
			{
				PaymentEntry.CalcDiscount(adj.AdjgDocDate, invoice, adj);
			}
			PaymentEntry.WarnPPDiscount(Base, adj.AdjgDocDate, invoice, adj, adj.CuryAdjgPPDAmt);

			base.AfterBalanceCalculatedBeforeBalanceAjusted(adj, invoice, DiscOnDiscDate, tran);

			Customer invoiceCustomer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(Base, adj.AdjdCustomerID);
			if (invoiceCustomer != null && invoiceCustomer.SmallBalanceAllow == true && adj.AdjgDocType != ARDocType.Refund && adj.AdjdDocType != ARDocType.CreditMemo)
			{
				CurrencyInfo payment_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);
				decimal payment_smallbalancelimit = payment_info.CuryConvCury(invoiceCustomer.SmallBalanceLimit.Value);

				int sign = adj.CuryOrigDocAmt < 0m ? -1 : 1;
				adj.CuryWOBal = sign * payment_smallbalancelimit;
				adj.WOBal = sign * invoiceCustomer.SmallBalanceLimit;

				invoice.CuryWhTaxBal = payment_smallbalancelimit;
				invoice.WhTaxBal = invoiceCustomer.SmallBalanceLimit;
			}
			else
			{
				adj.CuryWOBal = 0m;
				adj.WOBal = 0m;

				invoice.CuryWhTaxBal = 0m;
				invoice.WhTaxBal = 0m;
			}
		}
	}
}
