using System;
using PX.Objects.Localizations.CA.TX;

namespace PX.Objects.Localizations.CA.Reports
{
    public class CanadaCustomReportFunctions
    {
        private readonly Lazy<TaxPrintingLabelsService> taxPrintingLabelsService = new Lazy<TaxPrintingLabelsService>(() => new TaxPrintingLabelsService());

        public string GetSOTaxShortPrintingLabels(string orderType, string orderNbr, int? lineNbr)
        {
            return taxPrintingLabelsService.Value.GetSOTaxShortPrintingLabels(orderType, orderNbr, lineNbr);
        }

        public string GetARTaxShortPrintingLabels(string tranType, string refNbr, int? lineNbr)
        {
            return taxPrintingLabelsService.Value.GetARTaxShortPrintingLabels(tranType, refNbr, lineNbr);
        }
    }
}
