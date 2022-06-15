using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;

namespace PX.Objects.SO.GraphExtensions.ARReleaseProcessExt
{
	public class Correction : PXGraphExtension<IN.GraphExtensions.ARReleaseProcessExt.ProcessInventory, ARReleaseProcess>
	{
		/// <summary>
		/// Overrides <see cref="ARReleaseProcess.CloseInvoiceAndClearBalances(ARRegister, int?)"/>
		/// </summary>
		[PXOverride]
		public virtual void CloseInvoiceAndClearBalances(ARRegister ardoc, Action<ARRegister> baseMethod)
		{
			if (ardoc.IsUnderCorrection == true)
			{
				ardoc.Canceled = true;
				ARInvoice.Events
					.Select(ev=>ev.CancelDocument)
					.FireOn(Base, (ARInvoice)ardoc);

				PXDatabase.Update<ARTran>(
					new PXDataFieldAssign<ARTran.canceled>(PXDbType.Bit, true),
					new PXDataFieldRestrict<ARTran.tranType>(PXDbType.Char, ardoc.DocType),
					new PXDataFieldRestrict<ARTran.refNbr>(PXDbType.NVarChar, ardoc.RefNbr),
					new PXDataFieldRestrict<ARTran.canceled>(PXDbType.Bit, false));
			}

			baseMethod(ardoc);
		}

		/// <summary>
		/// Overrides <see cref="ARReleaseProcess.OpenInvoiceAndRecoverBalances(ARRegister)"/>
		/// </summary>
		[PXOverride]
		public virtual void OpenInvoiceAndRecoverBalances(ARRegister ardoc, Action<ARRegister> baseMethod)
		{
			if (ardoc.IsUnderCorrection == true && !Base.IsIntegrityCheck)
			{
				throw new PXException(Messages.OnlyCancelCreditMemoCanBeApplied, ardoc.RefNbr);
			}

			baseMethod(ardoc);
		}

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.ARReleaseProcessExt.ProcessInventory.ProcessARTranInventory(ARTran, ARInvoice, JournalEntry)"/>
		/// </summary>
		[PXOverride]
		public virtual void ProcessARTranInventory(ARTran n, ARInvoice ardoc, JournalEntry je, Action<ARTran, ARInvoice, JournalEntry> baseMethod)
		{
			if (ardoc.IsCancellation == true)
			{
				if (Base.IsIntegrityCheck || n?.LineType == SOLineType.Discount) return;

				foreach (INTran intran in Base1.intranselect.View.SelectMultiBound(new object[] { n }))
				{
					intran.ARDocType = null;
					intran.ARRefNbr = null;
					intran.ARLineNbr = null;

					Base1.intranselect.Cache.MarkUpdated(intran);

					Base1.PostShippedNotInvoiced(intran, n, ardoc, je);
				}
			}
			else
			{
				baseMethod(n, ardoc, je);
			}
		}

		public delegate List<ARRegister> ReleaseInvoiceDelegate(
			JournalEntry je,
			ARRegister doc,
			PXResult<ARInvoice, CurrencyInfo, Terms, Customer, Account> res,
			List<PMRegister> pmDocs);

		/// <summary>
		/// Overrides <see cref="ARReleaseProcess.ReleaseInvoice"/>
		/// </summary>
		[PXOverride]
		public virtual List<ARRegister> ReleaseInvoice(
			JournalEntry je,
			ARRegister doc,
			PXResult<ARInvoice, CurrencyInfo, Terms, Customer, Account> res,
			List<PMRegister> pmDocs,
			ReleaseInvoiceDelegate baseMethod)
		{
			// special handling for zero invoice correction
			ARInvoice ardoc = res;
			bool zeroCancellationInv = (ardoc.IsCancellation == true
				&& ardoc.CuryOrigDocAmt == 0m
				&& !string.IsNullOrEmpty(ardoc.OrigRefNbr));

			var ret = baseMethod(je, doc, res, pmDocs);

			if (zeroCancellationInv && ardoc.OpenDoc == false)
			{
				ARRegister origInvoice = PXSelect<ARRegister,
					Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
						And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>
					.Select(Base, ardoc.OrigDocType, ardoc.OrigRefNbr);

				if (origInvoice.IsUnderCorrection == true)
				{
					origInvoice.Canceled = true;
					origInvoice = Base.ARDocument.Update(origInvoice);

					PXDatabase.Update<ARTran>(
						new PXDataFieldAssign<ARTran.canceled>(PXDbType.Bit, true),
						new PXDataFieldRestrict<ARTran.tranType>(PXDbType.Char, origInvoice.DocType),
						new PXDataFieldRestrict<ARTran.refNbr>(PXDbType.NVarChar, origInvoice.RefNbr),
						new PXDataFieldRestrict<ARTran.canceled>(PXDbType.Bit, false));
				}
			}

			return ret;
		}
	}
}
