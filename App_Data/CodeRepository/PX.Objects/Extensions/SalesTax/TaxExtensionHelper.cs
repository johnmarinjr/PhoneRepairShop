using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.TX;

namespace PX.Objects.Extensions.SalesTax
{
    public static class TaxExtensionHelper
    {
        public static void AdjustMinMaxTaxableAmt(
                  this PXGraph graph,
                   TaxRev taxrev,
                   ref decimal curyTaxableAmt,
                   ref decimal taxableAmt)
        {
            CurrencyInfo currencyInfo = graph.FindImplementation<IPXCurrencyHelper>().GetDefaultCurrencyInfo();
            taxableAmt = currencyInfo.CuryConvBase(curyTaxableAmt);

            if (taxrev.TaxableMin != 0.0m)
            {
                if (taxableAmt < taxrev.TaxableMin)
                {
                    curyTaxableAmt = 0.0m;
                    taxableAmt = 0.0m;
                }
            }

            if (taxrev.TaxableMax != 0.0m)
            {
                if (taxableAmt > taxrev.TaxableMax)
                {
                    curyTaxableAmt = currencyInfo.CuryConvCury((decimal)taxrev.TaxableMax);
                    taxableAmt = (decimal)taxrev.TaxableMax;
                }
            }
        }

        public static void SetExpenseAmountsForDeductibleVAT(this TaxDetail taxdet, TaxRev taxrev, decimal CuryTaxAmt, CurrencyInfo currencyInfo)
        {
            taxdet.CuryExpenseAmt = currencyInfo.RoundCury(CuryTaxAmt * (1 - (taxrev.NonDeductibleTaxRate ?? 0m) / 100));
            taxdet.ExpenseAmt = currencyInfo.CuryConvBase(taxdet.CuryExpenseAmt.Value);
        }
    }
}
