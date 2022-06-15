using PX.Data;
using PX.Objects.Extensions.MultiCurrency.AP;

namespace PX.Objects.TX
{
	public partial class TaxAdjustmentEntry
	{
		public class MultiCurrency : APMultiCurrencyGraph<TaxAdjustmentEntry, TaxAdjustment>
		{
			protected override string DocumentStatus => Base.Document.Current?.Status;

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(TaxAdjustment))
				{
					DocumentDate = typeof(TaxAdjustment.docDate),
					BAccountID = typeof(TaxAdjustment.vendorID)
				};
			}

			protected override PXSelectBase[] GetChildren()
			{
				return new PXSelectBase[]
				{
					Base.Document,
					Base.Transactions,
				};
			}
		}
	}
}
