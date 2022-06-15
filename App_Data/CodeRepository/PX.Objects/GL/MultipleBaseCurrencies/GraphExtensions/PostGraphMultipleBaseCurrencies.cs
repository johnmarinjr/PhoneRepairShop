using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL.DAC;
using PX.Objects.GL.Standalone;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Data.BQL;
using PX.Objects.CM;

namespace PX.Objects.GL
{
	public sealed class PostGraphMultipleBaseCurrencies : PXGraphExtension<PostGraph>
	{

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate void PostBatchProcDelegate(Batch b, bool createintercompany);
		[PXOverride]
		public void PostBatchProc(Batch b, bool createintercompany, PostBatchProcDelegate baseMethod)
		{
			CheckBatchAndLedgerBaseCurrency(Base.Caches<Batch>(), b);
			baseMethod(b, createintercompany);
		}

		public delegate void ReleaseBatchProcDelegate(Batch b, bool unholdBatch = false);
		[PXOverride]
		public void ReleaseBatchProc(Batch b, bool unholdBatch = false, ReleaseBatchProcDelegate baseMethod = null)
		{
			CheckBatchAndLedgerBaseCurrency(Base.Caches<Batch>(), b);
			baseMethod(b, unholdBatch);
		}

		protected void CheckBatchAndLedgerBaseCurrency(PXCache cache, Batch batch)
		{
			CurrencyInfo tranCurrencyInfo = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>
											.Select(cache.Graph, batch.CuryInfoID);
			Ledger ledger = PXSelectorAttribute.Select<Ledger.ledgerID>(cache, batch, batch.LedgerID) as Ledger;

			if (tranCurrencyInfo != null && ledger != null
				&& !tranCurrencyInfo.BaseCuryID.Equals(ledger.BaseCuryID))
			{
				throw new PXException(Messages.IncorrectBaseCurrency, ledger.LedgerCD);
			}
		}
	}
}
