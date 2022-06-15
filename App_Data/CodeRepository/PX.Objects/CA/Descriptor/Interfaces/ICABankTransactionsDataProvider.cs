using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using CATranExt = PX.Objects.CA.BankStatementHelpers.CATranExt;
using CATran2 = PX.Objects.CA.BankStatementHelpers.CATran2;
using IMatchSettings = PX.Objects.CA.BankStatementHelpers.IMatchSettings;
using PX.Objects.CA.BankStatementProtoHelpers;

namespace PX.Objects.CA
{
	public interface ICABankTransactionsDataProvider
	{
		PXResultset<CATranExt> SearchForMatchingTransactions(CABankTran aDetail, IMatchSettings aSettings, Pair<DateTime, DateTime> tranDateRange, string curyID);
		PXResultset<CABatch> SearchForMatchingCABatches(CABankTran aDetail, Pair<DateTime, DateTime> tranDateRange, string curyID, bool allowUnreleased);
		PXResultset<CABatchDetail> SearchForMatchesInCABatches(string tranType, string batchNbr);
		PXResultset<CABankTranMatch> SearchForTranMatchForCABatch(string batchNbr);

		PXResult<ARInvoice, ARAdjust> FindARInvoiceByInvoiceInfo(CABankTran aRow);
		PXResult<APInvoice, APAdjust, APPayment> FindAPInvoiceByInvoiceInfo(CABankTran aRow);
	}
}
