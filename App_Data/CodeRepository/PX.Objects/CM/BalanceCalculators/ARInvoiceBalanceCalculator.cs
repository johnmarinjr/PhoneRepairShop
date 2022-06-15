using PX.Data;
using PX.Objects.AR;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
	public class ARInvoiceBalanceCalculator
	{
		private readonly IPXCurrencyHelper curyHelper;
		private readonly PXGraph Graph;

		public ARInvoiceBalanceCalculator(IPXCurrencyHelper curyHelper, PXGraph Graph) 
		{
			this.curyHelper = curyHelper;
			this.Graph = Graph;
		}


		/// <summary>
		/// The base method to calculate application
		/// balances in Invoice currency. Both invoice
		/// and payment documents should be set.
		/// </summary>
		public  void CalcBalancesFromInvoiceSide(
			ARAdjust2 adj,
			ARInvoice invoice,
			ARPayment payment,
			bool isCalcRGOL,
			bool DiscOnDiscDate,
			ARAdjust2 others = null)
		{
			if (invoice == null) return;

			InitBalancesFromInvoiceSide(adj, invoice, payment, others);

			if (DiscOnDiscDate)
			{
				PaymentEntry.CalcDiscount(adj.AdjgDocDate, invoice, adj);
			}

			new PaymentBalanceAjuster(curyHelper).AdjustBalance(adj, adj.AdjdCuryInfoID, adj.CuryAdjdAmt, adj.CuryAdjdDiscAmt, adj.CuryAdjdWOAmt, false);

			if (isCalcRGOL && (adj.Voided != true))
			{
				//TODO: move outside this method
				CalcRGOLFromInvoiceSide(payment, adj);
			}
			adj.Selected = adj.CuryAdjdAmt != 0;
		}

		/// <summary>
		/// The method to initialize application
		/// balances in Invoice currency.
		/// </summary>
		public  void InitBalancesFromInvoiceSide(
			ARAdjust2 adj,
			ARInvoice invoice,
			ARPayment payment,
			ARAdjust2 others = null)
		{
			// Payment balance should be calculated 
			// in Invoice currency.
			//

			CalculatedBalance calculatedBalance = new PaymentBalanceCalculator(curyHelper).CalcBalance(
				adj.AdjdCuryInfoID,
				adj.AdjgCuryInfoID,
				payment.CuryInfoID,
				(payment.Released == true ? payment.CuryDocBal : payment.CuryOrigDocAmt) - (others?.CuryAdjgAmt ?? 0m),
				(payment.Released == true ? payment.DocBal : payment.OrigDocAmt) - (others?.AdjAmt ?? 0m)
				);

			adj.CuryDocBal = calculatedBalance.CuryBalance;
			adj.DocBal = calculatedBalance.Balance;

			// Discount balance can be taken 
			// from the Invoice as is.
			//
			adj.CuryDiscBal = invoice.CuryDiscBal;
			adj.DiscBal = invoice.DiscBal;

			// WO balance should be taken from 
			// the customer in Invoice currency.
			//
			adj.CuryWhTaxBal = 0m;
			adj.WhTaxBal = 0m;

			invoice.CuryWhTaxBal = 0m;
			invoice.WhTaxBal = 0m;

			if (adj.AdjgDocType != ARDocType.Refund &&
				adj.AdjdDocType != ARDocType.CreditMemo)
			{
				Customer invoiceCustomer = PXSelect<
					Customer,
					Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
					.Select(Graph, adj.AdjdCustomerID);

				if (invoiceCustomer?.SmallBalanceAllow == true)
				{
					CurrencyInfo invoice_info = curyHelper.GetCurrencyInfo(adj.AdjdCuryInfoID);
					decimal invoice_smallbalancelimit = invoice_info.CuryConvCury(invoiceCustomer.SmallBalanceLimit ?? 0m);

					adj.CuryWhTaxBal = invoice_smallbalancelimit;
					adj.WhTaxBal = invoiceCustomer.SmallBalanceLimit;

					invoice.CuryWhTaxBal = invoice_smallbalancelimit;
					invoice.WhTaxBal = invoiceCustomer.SmallBalanceLimit;
				}
			}
		}

		/// <summary>
		/// The method to calculate application RGOL
		/// from the Invoice document side.
		/// </summary>
		public  void CalcRGOLFromInvoiceSide<TInvoice, TAdjustment>(TInvoice document, TAdjustment adj)
			where TInvoice : IInvoice
			where TAdjustment : class, IBqlTable, IAdjustment
		{
			if (adj.CuryAdjdAmt == null || adj.CuryAdjdDiscAmt == null || adj.CuryAdjdWhTaxAmt == null) return;

			CurrencyInfo invoice_info = curyHelper.GetCurrencyInfo(adj.AdjdCuryInfoID);
			CurrencyInfo payment_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);
			CurrencyInfo payment_originfo = curyHelper.GetCurrencyInfo(document.CuryInfoID);

			RGOLCalculator rGOLCalculator = new RGOLCalculator(
				invoice_info,
				payment_info,
				payment_originfo);

			RGOLCalculationResult CuryAdjgDiscAmtRgol = rGOLCalculator.CalcRGOL(adj.CuryAdjdDiscAmt, adj.AdjDiscAmt);
			adj.CuryAdjgDiscAmt = CuryAdjgDiscAmtRgol.ToCuryAdjAmt;

			RGOLCalculationResult CuryAdjgWhTaxAmtRgol = rGOLCalculator.CalcRGOL(adj.CuryAdjdWhTaxAmt, adj.AdjWhTaxAmt);
			adj.CuryAdjgWhTaxAmt = CuryAdjgWhTaxAmtRgol.ToCuryAdjAmt;

			RGOLCalculationResult CuryAdjgAmtRgol = rGOLCalculator.CalcRGOL(adj.CuryAdjdAmt, adj.AdjAmt);
			adj.CuryAdjgAmt = CuryAdjgAmtRgol.ToCuryAdjAmt;

			adj.RGOLAmt = CuryAdjgDiscAmtRgol.RgolAmt + CuryAdjgWhTaxAmtRgol.RgolAmt + CuryAdjgAmtRgol.RgolAmt;
			adj.RGOLAmt = adj.ReverseGainLoss == true ? -1m * adj.RGOLAmt : adj.RGOLAmt;
		}
	}
}
