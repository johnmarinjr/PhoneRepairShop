using PX.Data;
using PX.Objects.DR;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.TX;
using PX.Objects.PO;

using Amount = PX.Objects.AR.ARReleaseProcess.Amount;
using System.Collections.Generic;

namespace PX.Objects.AP
{
	public class InvoiceTransactionReleasingArgs
	{
		public PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem, APTaxTran> TransactionResult { get; set; }

		public virtual APTax TaxDetail { get => TransactionResult; }
		public virtual APTaxTran TaxTransaction { get => TransactionResult; }
		public virtual Tax Tax { get => TransactionResult; }
		public virtual DRDeferredCode DeferredCode { get => TransactionResult; }
		public virtual LandedCostCode LandedCostCode { get => TransactionResult; }
		public virtual InventoryItem Inventory { get => TransactionResult; }

		protected APTran _transaction;
		public virtual APTran Transaction
		{
			get => _transaction ?? TransactionResult;
			set =>  _transaction = value;
		}

		public GLTran GLTransaction { get; set; }

		public Amount PostedAmount { get; set; }

		public APRegister Register { get; set; }

		public APInvoice Invoice { get; set; }
		public virtual bool IsPrebookVoiding
		{
			get => Invoice.DocType == APDocType.VoidQuickCheck && !string.IsNullOrEmpty(Invoice.PrebookBatchNbr);
		}

		public JournalEntry JournalEntry { get; set; }

		public CM.Extensions.CurrencyInfo CurrencyInfo { get; set; }

		public bool IsPrebooking { get; set; }
	}

	public class InvoiceTransactionsReleasedArgs
	{
		public List<INRegister> INDocuments { get; set; } = new List<INRegister>();

		public APInvoice Invoice { get; set; }

		public bool IsPrebooking { get; set; }
	}
}
