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

namespace PX.Objects.CA.Repositories
{
	public interface ICABankTransactionsRepository
	{
		PXResultset<CATranExt> SearchForMatchingTransactions(PXGraph graph, CABankTran aDetail, IMatchSettings aSettings, Pair<DateTime, DateTime> tranDateRange, string curyID);
		PXResultset<CABatch> SearchForMatchingCABatches(PXGraph graph, CABankTran aDetail, Pair<DateTime, DateTime> tranDateRange, string curyID, bool allowUnreleased);
		PXResultset<CABatchDetail> SearchForMatchesInCABatches(PXGraph graph, string tranType, string batchNbr);
		PXResultset<CABankTranMatch> SearchForTranMatchForCABatch(PXGraph graph, string batchNbr);

		PXResult<ARInvoice, ARAdjust> FindARInvoiceByInvoiceInfo(PXGraph graph, CABankTran aRow);
		PXResult<APInvoice, APAdjust, APPayment> FindAPInvoiceByInvoiceInfo(PXGraph graph, CABankTran aRow);
		
		decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CATran aTran, IMatchSettings aSettings);
		decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, IMatchSettings aSettings);
		decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch matchRow, IMatchSettings aSettings);
		decimal EvaluateMatching(PXGraph graph, string aStr1, string aStr2, bool aCaseSensitive, bool matchEmpty = true);
		decimal EvaluateTideMatching(PXGraph graph, string aStr1, string aStr2, bool aCaseSensitive, bool matchEmpty = true);
		
		decimal CompareDate(PXGraph graph, CABankTran aDetail, CATran aTran, double meanValue, double sigma);
		decimal CompareDate(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, double meanValue, double sigma);
		decimal CompareDate(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch aTran, double meanValue, double sigma);

		decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CATran aTran, bool looseCompare, IMatchSettings settings);
		decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, bool looseCompare);
		decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch aTran, bool looseCompare, IMatchSettings matchSettings);

		decimal ComparePayee(PXGraph graph, CABankTran aDetail, CATran aTran);
		decimal ComparePayee(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran);

		decimal CompareExpenseReceiptAmount(PXGraph graph, CABankTran bankTran, CABankTranExpenseDetailMatch receipt, IMatchSettings settings);
	}
}
