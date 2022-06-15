using PX.Data;
using PX.Objects.Common.GraphExtensions.Abstract.DAC;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CM.Extensions
{
    public abstract class AbstractPaymentBalanceCalculator<TAdjust, TTran>
            where TAdjust : class, IBqlTable, IFinAdjust, new()
            where TTran : class, IBqlTable, IDocumentTran, new()
    {
        public IPXCurrencyHelper curyHelper { get; }
        public virtual bool DiscOnDiscDate => false;

        private readonly PaymentBalanceCalculator paymentBalanceCalculator;

        protected AbstractPaymentBalanceCalculator(IPXCurrencyHelper curyHelper)
        {
            this.curyHelper = curyHelper;
            paymentBalanceCalculator = new PaymentBalanceCalculator(curyHelper);
        }

        protected virtual bool ShouldRgolBeResetInZero(TAdjust adj) => false;

        public void CalcBalances<T>(TAdjust adj, T originalInvoice, bool isCalcRGOL, bool DiscOnDiscDate, TTran tran)
            where T : class, IInvoice, IBqlTable, new()
        {
            //Conditional additional reset
            bool isPendingPPD = adj.CuryAdjgPPDAmt != null && adj.CuryAdjgPPDAmt != 0m && adj.AdjdHasPPDTaxes == true;
            if (isPendingPPD)
            {
                adj.CuryAdjgDiscAmt = 0m;
                adj.CuryAdjdDiscAmt = 0m;
                adj.AdjDiscAmt = 0m;
            }

            //Auto-apply (AR only)
            T voucher = AjustInvoiceBalanceForAutoApply(adj, originalInvoice);

            //Balance Calcualtion
            paymentBalanceCalculator.CalcBalances(adj.AdjgCuryInfoID, adj.AdjdCuryInfoID, voucher, adj, tran);

            AfterBalanceCalculatedBeforeBalanceAjusted(adj, voucher, DiscOnDiscDate, tran);

            PaymentBalanceAjuster balanceAjuster = new PaymentBalanceAjuster(curyHelper);
            balanceAjuster.AdjustBalance(adj);

            if (isPendingPPD && adj.AdjPPDAmt == null && adj.Released != true)
            {
                TAdjust adjPPD = PXCache<TAdjust>.CreateCopy(adj);
                adjPPD.FillDiscAmts();
                balanceAjuster.AdjustBalance(adjPPD);
                adj.AdjPPDAmt = adjPPD.AdjDiscAmt;
            }

            if (isCalcRGOL && (adj.Voided == null || adj.Voided == false))
            {
                new PaymentRGOLCalculator(curyHelper, adj, adj.ReverseGainLoss).Calculate(voucher, tran);

                if (ShouldRgolBeResetInZero(adj)) adj.RGOLAmt = 0m;

                decimal? CuryAdjdPPDAmt = adj.CuryAdjdDiscAmt;
                if (isPendingPPD)
                {
                    TAdjust adjPPD = PXCache<TAdjust>.CreateCopy(adj);
                    adjPPD.FillDiscAmts();
                    new PaymentRGOLCalculator(curyHelper, adjPPD, false).Calculate(voucher, tran);
                    CuryAdjdPPDAmt = adjPPD.CuryAdjdDiscAmt;
                }

                adj.CuryAdjdPPDAmt = CuryAdjdPPDAmt;
            }

            if (isPendingPPD && adj.Voided != true)
            {
                adj.CuryDocBal -= adj.CuryAdjgPPDAmt;
                adj.DocBal -= adj.AdjPPDAmt;
                adj.CuryDiscBal -= adj.CuryAdjgPPDAmt;
                adj.DiscBal -= adj.AdjPPDAmt;
            }
        }

        /// <summary>
        /// Behavior by default: adj.AdjdCuryRate = paymentBalanceCalculator.GetAdjdCuryRate(adj);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adj"></param>
        /// <param name="voucher"></param>
        /// <param name="DiscOnDiscDate"></param>
		protected virtual void AfterBalanceCalculatedBeforeBalanceAjusted<T>(TAdjust adj, T voucher, bool DiscOnDiscDate, TTran tran) where T : class, IInvoice, IBqlTable, new()
        {
            adj.AdjdCuryRate = paymentBalanceCalculator.GetAdjdCuryRate(adj);
        }

        protected virtual T AjustInvoiceBalanceForAutoApply<T>(TAdjust adj, T invoice)
            where T : class, IInvoice, IBqlTable, new()
        {
            return invoice;
        }
    }
}