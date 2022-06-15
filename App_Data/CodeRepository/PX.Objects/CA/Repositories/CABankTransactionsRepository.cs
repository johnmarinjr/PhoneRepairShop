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
using PX.Objects.CA.BankStatementHelpers;

namespace PX.Objects.CA.Repositories
{
	public class CABankTransactionsRepository : ICABankTransactionsRepository
	{
		public virtual PXResultset<CATranExt> SearchForMatchingTransactions(PXGraph graph, CABankTran aDetail, IMatchSettings aSettings, Pair<DateTime, DateTime> tranDateRange, string curyID)
		{
			var cmd = new PXSelectReadonly2<CATranExt,
					LeftJoin<Light.BAccount, On<Light.BAccount.bAccountID, Equal<CATranExt.referenceID>>,
					LeftJoin<CATran2, On<CATran2.cashAccountID, Equal<CATranExt.cashAccountID>,
						And<CATran2.voidedTranID, Equal<CATranExt.tranID>,
						And<True, Equal<Required<CASetup.skipVoided>>>>>,
					LeftJoin<CABankTranMatch2, On<CABankTranMatch2.cATranID, Equal<CATranExt.tranID>,
						And<CABankTranMatch2.tranType, Equal<Required<CABankTran.tranType>>>>,
					LeftJoin<CABatchDetail, On<CABatchDetail.origModule, Equal<CATranExt.origModule>,
						And<CABatchDetail.origDocType, Equal<CATranExt.origTranType>,
						And<CABatchDetail.origRefNbr, Equal<CATranExt.origRefNbr>,
						And<CATranExt.isPaymentChargeTran, Equal<False>>>>>,
					LeftJoin<CABankTranMatch, On<CABankTranMatch.docModule, Equal<BatchModule.moduleAP>,
						And<CABankTranMatch.docType, Equal<CATranType.cABatch>,
						And<CABankTranMatch.docRefNbr, Equal<CABatchDetail.batchNbr>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>>>>>>>>,
					Where<CATranExt.cashAccountID, Equal<Required<CABankTran.cashAccountID>>,
						And<CATranExt.tranDate, Between<Required<CATranExt.tranDate>, Required<CATranExt.tranDate>>,
						And<CATranExt.curyID, Equal<Required<CATranExt.curyID>>>>>>(graph);

			if (aDetail.MultipleMatchingToPayments == true && aDetail.MatchReceiptsAndDisbursements != true)
			{
				if (aDetail.CuryTranAmt.Value > 0)
				{
					cmd.WhereAnd<Where<CATranExt.curyTranAmt, LessEqual<Required<CATranExt.curyTranAmt>>,
										And<CATranExt.curyTranAmt, GreaterEqual<Zero>>>>();
				}
				else
				{
					cmd.WhereAnd<Where<CATranExt.curyTranAmt, GreaterEqual<Required<CATranExt.curyTranAmt>>,
										And<CATranExt.curyTranAmt, LessEqual<Zero>>>>();
				}
			}
			else if (aDetail.MatchReceiptsAndDisbursements != true)
			{
				cmd.WhereAnd<Where<CATranExt.curyTranAmt, Equal<Required<CATranExt.curyTranAmt>>>>();
			}

			if (aSettings.SkipVoided == true)
			{
				cmd.WhereAnd<Where<CATranExt.voidedTranID, IsNull, And<CATran2.tranID, IsNull>>>();
			}
			if ((graph.Caches[typeof(CASetup)].Current as CASetup).SkipReconciled == true)
			{
				cmd.WhereAnd<Where<CATranExt.reconciled, Equal<False>>>();
			}

			return cmd.Select(aSettings.SkipVoided, aDetail.TranType, aDetail.TranType, aDetail.CashAccountID, tranDateRange.first, tranDateRange.second, curyID, aDetail.CuryTranAmt.Value);
		}

		public virtual PXResultset<CABatch> SearchForMatchingCABatches(PXGraph graph, CABankTran aDetail, Pair<DateTime, DateTime> tranDateRange, string curyID, bool allowUnreleased)
		{
			var cmd = new PXSelectJoin<CABatch,
							LeftJoin<CABatchDetail, On<CABatchDetail.batchNbr, Equal<CABatch.batchNbr>,
								And<CABatchDetail.origModule, Equal<BatchModule.moduleAP>>>,
							LeftJoin<Light.APPayment, On<Light.APPayment.docType, Equal<CABatchDetail.origDocType>,
								And<Light.APPayment.refNbr, Equal<CABatchDetail.origRefNbr>>>,
							LeftJoin<Light.BAccount, On<Light.BAccount.bAccountID, Equal<APPayment.vendorID>>,
							LeftJoin<CABankTranMatch, On<CABankTranMatch.cATranID, Equal<Light.APPayment.cATranID>,
								And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>>>>>,
							 Where<CABatch.cashAccountID, Equal<Required<CABatch.cashAccountID>>,
								And2<Where<CABatch.released, Equal<True>, Or<Required<CASetup.allowMatchingToUnreleasedBatch>, Equal<True>>>,
								And<Where<CABatch.tranDate, Between<Required<CABatch.tranDate>, Required<CABatch.tranDate>>,
								And<CABatch.curyID, Equal<Required<CABatch.curyID>>,
								And<Where<CABatch.reconciled, Equal<False>, Or<Required<CASetup.skipReconciled>, Equal<False>>>>>>>>>>(graph);

			if (aDetail.MultipleMatchingToPayments == true && aDetail.MatchReceiptsAndDisbursements != true)
			{
				cmd.WhereAnd<Where<CABatch.curyDetailTotal, LessEqual<Required<CABatch.curyDetailTotal>>>>();
			}
			else if (aDetail.MatchReceiptsAndDisbursements != true)
			{
				cmd.WhereAnd<Where<CABatch.curyDetailTotal, Equal<Required<CABatch.curyDetailTotal>>>>();
			}

			return cmd.Select(aDetail.TranType, aDetail.CashAccountID, allowUnreleased, tranDateRange.first, tranDateRange.second,
								 curyID, (graph.Caches[typeof(CASetup)].Current as CASetup).SkipReconciled ?? false, -1 * aDetail.CuryTranAmt.Value);
		}

		public virtual PXResultset<CABatchDetail> SearchForMatchesInCABatches(PXGraph graph, string tranType, string batchNbr)
		{
			return PXSelectJoin<CABatchDetail,
					InnerJoin<CATran, On<CATran.origModule, Equal<CABatchDetail.origModule>,
						And<CATran.origTranType, Equal<CABatchDetail.origDocType>,
						And<CATran.origRefNbr, Equal<CABatchDetail.origRefNbr>,
						And<CATran.isPaymentChargeTran, Equal<False>>>>>,
					InnerJoin<CABankTranMatch, On<CABankTranMatch.cATranID, Equal<CATran.tranID>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>>>,
					Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>>>.Select(graph, tranType, batchNbr);
		}

		public virtual PXResultset<CABankTranMatch> SearchForTranMatchForCABatch(PXGraph graph, string batchNbr)
		{
			return PXSelect<CABankTranMatch, Where<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>,
					And<CABankTranMatch.docType, Equal<CATranType.cABatch>,
					And<CABankTranMatch.docModule, Equal<BatchModule.moduleAP>>>>>.Select(graph, batchNbr);
		}

		/// <summary>
		/// Searches in database AR invoices, based on the the information in CABankTran record.
		/// The field used for the search are  - BAccountID and InvoiceInfo. First it is searching a invoice by it RefNbr, 
		/// then (if not found) - by invoiceNbr. 
		/// </summary>
		/// <param name="aRow">parameters for the search. The field used for the search are  - BAccountID and InvoiceInfo.</param>
		///	<returns>Returns null if nothing is found and PXResult<ARInvoice,ARAdjust> in the case of success.
		///		ARAdjust record represents unreleased adjustment (payment), applied to this Invoice
		///	</returns>
		public virtual PXResult<ARInvoice, ARAdjust> FindARInvoiceByInvoiceInfo(PXGraph graph, CABankTran aRow)
		{
			PXResult<ARInvoice, ARAdjust> invResult = (PXResult<ARInvoice, ARAdjust>)PXSelectJoin<
				ARInvoice,
				LeftJoin<ARAdjust,
					On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
					And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust.released, Equal<boolFalse>>>>>,
				Where<ARInvoice.docType, Equal<AR.ARInvoiceType.invoice>,
					And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>
				.Select(graph, aRow.InvoiceInfo);

			if (invResult == null)
			{
				invResult = (PXResult<ARInvoice, ARAdjust>)PXSelectJoin<
					ARInvoice,
					LeftJoin<ARAdjust,
						On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
						And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
						And<ARAdjust.released, Equal<boolFalse>>>>>,
					Where<ARInvoice.docType, Equal<AR.ARInvoiceType.invoice>,
						And<ARInvoice.invoiceNbr, Equal<Required<ARInvoice.invoiceNbr>>>>>
					.Select(graph, aRow.InvoiceInfo);
			}
			return invResult;
		}

		/// <summary>
		/// Searches in database AR invoices, based on the the information in the CABankTran record.
		/// The field used for the search are  - BAccountID and InvoiceInfo. First it is searching a invoice by it RefNbr, 
		/// then (if not found) - by invoiceNbr. 
		/// </summary>
		/// <param name="aRow">Parameters for the search. The field used for the search are  - BAccountID and InvoiceInfo.</param>
		/// <returns>Returns null if nothing is found and PXResult<APInvoice,APAdjust,APPayment> in the case of success.
		/// APAdjust record represents unreleased adjustment (payment), applied to this APInvoice</returns>
		public virtual PXResult<APInvoice, APAdjust, APPayment> FindAPInvoiceByInvoiceInfo(PXGraph graph, CABankTran aRow)
		{

			PXResult<APInvoice, APAdjust, APPayment> invResult = (PXResult<APInvoice, APAdjust, APPayment>)PXSelectJoin<
				APInvoice,
				LeftJoin<APAdjust,
					On<APAdjust.adjdDocType, Equal<APInvoice.docType>,
					And<APAdjust.adjdRefNbr, Equal<APInvoice.refNbr>,
					And<APAdjust.released, Equal<boolFalse>>>>,
				LeftJoin<APPayment,
					On<APPayment.docType, Equal<APInvoice.docType>,
					And<APPayment.refNbr, Equal<APInvoice.refNbr>,
					And<Where<APPayment.docType, Equal<APDocType.prepayment>,
						Or<APPayment.docType, Equal<APDocType.debitAdj>>>>>>>>,
				Where<APInvoice.docType, Equal<AP.APInvoiceType.invoice>,
					And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>
				.Select(graph, aRow.InvoiceInfo);


			if (invResult == null)
			{
				invResult = (PXResult<APInvoice, APAdjust, APPayment>)PXSelectJoin<
					APInvoice,
					LeftJoin<APAdjust,
						On<APAdjust.adjdDocType, Equal<APInvoice.docType>,
						And<APAdjust.adjdRefNbr, Equal<APInvoice.refNbr>,
						And<APAdjust.released, Equal<boolFalse>>>>,
					LeftJoin<APPayment,
						On<APPayment.docType, Equal<APInvoice.docType>,
						And<APPayment.refNbr, Equal<APInvoice.refNbr>,
						And<Where<APPayment.docType, Equal<APDocType.prepayment>,
							Or<APPayment.docType, Equal<APDocType.debitAdj>>>>>>>>,
					Where<APInvoice.docType, Equal<AP.APInvoiceType.invoice>,
						And<APInvoice.invoiceNbr, Equal<Required<APInvoice.invoiceNbr>>>>>
					.Select(graph, aRow.InvoiceInfo);

			}
			return invResult;
		}

		public virtual decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch matchRow, IMatchSettings aSettings)
		{
			return StatementsMatchingProto.EvaluateMatching(graph, aDetail, matchRow, aSettings);
		}

		public virtual decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CATran aTran, IMatchSettings aSettings)
		{
			return StatementsMatchingProto.EvaluateMatching(graph, aDetail, aTran, aSettings);
		}

		public virtual decimal EvaluateMatching(PXGraph graph, string aStr1, string aStr2, bool aCaseSensitive, bool matchEmpty = true)
		{
			return StatementMatching.EvaluateMatching(aStr1, aStr2, aCaseSensitive, matchEmpty);
		}

		public virtual decimal EvaluateTideMatching(PXGraph graph, string aStr1, string aStr2, bool aCaseSensitive, bool matchEmpty = true)
		{
			return StatementMatching.EvaluateTideMatching(aStr1, aStr2, aCaseSensitive, matchEmpty);
		}

		public virtual decimal CompareDate(PXGraph graph, CABankTran aDetail, CATran aTran, double meanValue, double sigma)
		{
			return StatementsMatchingProto.CompareDate(aDetail, aTran, meanValue, sigma);
		}

		public virtual decimal CompareDate(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, double meanValue, double sigma)
		{
			DateTime? comparedDate = (aTran?.DiscDate >= aDetail.TranDate.Value) ? aTran?.DiscDate : aTran?.DueDate;
			return StatementsMatchingProto.CompareDate(aDetail, comparedDate, meanValue, sigma);
		}

		public virtual decimal CompareDate(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch aTran, double meanValue, double sigma)
		{
			return StatementsMatchingProto.CompareDate(aDetail, aTran.DocDate, meanValue, sigma);
		}

		public virtual decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CATran aTran, bool looseCompare, IMatchSettings settings)
		{
			return StatementsMatchingProto.CompareRefNbr(graph, aDetail, aTran, looseCompare, settings);
		}

		public virtual decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, bool looseCompare)
		{
			return StatementsMatchingProto.CompareRefNbr(graph, aDetail, aTran, looseCompare);
		}

		public virtual decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch aTran, bool looseCompare, IMatchSettings matchSettings)
		{
			return StatementsMatchingProto.CompareRefNbr(graph, aDetail, aTran.ExtRefNbr, looseCompare, matchSettings);
		}

		public virtual decimal ComparePayee(PXGraph graph, CABankTran aDetail, CATran aTran)
		{
			return StatementsMatchingProto.ComparePayee(graph, aDetail, aTran);
		}

		public virtual decimal ComparePayee(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran)
		{
			return StatementsMatchingProto.ComparePayee(graph, aDetail, aTran);
		}

		public virtual decimal CompareExpenseReceiptAmount(PXGraph graph, CABankTran bankTran, CABankTranExpenseDetailMatch receipt, IMatchSettings settings)
		{
			double diff = Convert.ToDouble(Math.Abs(bankTran.CuryTranAmt.Value) - receipt.CuryDocAmt.Value);

			double sigma = Convert.ToDouble(settings.CuryDiffThreshold.Value * bankTran.CuryTranAmt.Value) / 100;

			decimal res = (decimal)Math.Exp(-(diff * diff / (2 * sigma * sigma)));

			return res > 0 ? res : 0.0m;
		}

		public virtual decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, IMatchSettings aSettings)
		{
			return StatementsMatchingProto.EvaluateMatching(graph, aDetail, aTran, aSettings);
		}
	}
}
