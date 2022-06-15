using PX.Data;
using System;

namespace PX.Objects.GL
{
	public abstract class GLVoucherEntryBase<TGraph, TDocType, TRefNbrType,TModule> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
		where TDocType : IBqlField
		where TRefNbrType : IBqlField
		where TModule : IBqlOperand, IConstant
	{
		public PXSelect<GLVoucher,
					Where<GLVoucher.docType, Equal<Current<TDocType>>,
						And<GLVoucher.refNbr, Equal<Current<TRefNbrType>>,
						And<GLVoucher.module, Equal<TModule>>>>> Voucher;

		public PXSelect<GLVoucherBatch,
					Where<GLVoucherBatch.workBookID, Equal<Optional<GLVoucher.workBookID>>,
						And<GLVoucherBatch.voucherBatchNbr, Equal<Optional<GLVoucher.voucherBatchNbr>>>>> VoucherBatch;
		protected void RowInserted<TNoteID, TDesc, TBAccount, TBAccountLocation>(PXCache sender, PXRowInsertedEventArgs e, PXRowInserted bs)
		where TNoteID : IBqlField
			where TDesc : IBqlField
			where TBAccount : IBqlField
			where TBAccountLocation : IBqlField
		{
			if (bs != null)
				bs(sender, e);
			if (Base.IsWithinContext)
			{
				string vb = Base.GetContextValue<GLVoucherBatch.voucherBatchNbr>();
				string wbID = Base.GetContextValue<GLVoucherBatch.workBookID>();
				this.VoucherBatch.Current = this.VoucherBatch.Select(wbID, vb);
				GLWorkBook wb = PXSelect<GLWorkBook,
					Where<GLWorkBook.workBookID, Equal<Required<GLVoucherBatch.workBookID>>>>.Select(this.Base, Base.GetContextValue<GLVoucherBatch.workBookID>());
				sender.Remove(e.Row);
				sender.SetValueExt<TDocType>(e.Row, wb.DocType);
				sender.SetStatus(e.Row, PXEntryStatus.Inserted);
				sender.Normalize();

				if (!String.IsNullOrEmpty(vb))
				{
					Guid? noteID = PXNoteAttribute.GetNoteID<TNoteID>(sender, e.Row);
					this.Voucher.Insert(new GLVoucher());
					this.Voucher.Cache.IsDirty = false;
					Base.Caches[typeof(Note)].IsDirty = false;
				}

				if (wb.DefaultDescription != null && sender.GetValue<TDesc>(e.Row) == null)
					sender.SetValueExt<TDesc>(e.Row, wb.DefaultDescription);

				if (wb.DefaultBAccountID != null && sender.GetValue<TBAccount>(e.Row) == null)
				{
					sender.SetValueExt<TBAccount>(e.Row, wb.DefaultBAccountID);
					if (wb.DefaultLocationID != null)
						sender.SetValueExt<TBAccountLocation>(e.Row, wb.DefaultLocationID);
				}
			}
		}
	}
}