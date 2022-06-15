using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using CATranExt = PX.Objects.CA.BankStatementHelpers.CATranExt;
using CATran2 = PX.Objects.CA.BankStatementHelpers.CATran2;
using IMatchSettings = PX.Objects.CA.BankStatementHelpers.IMatchSettings;


namespace PX.Objects.PR
{
	public class PRxCABankTransactionsMaint : PXGraphExtension<CABankTransactionsMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		[PXOverride]
		public virtual PXResultset<CATranExt> SearchForMatchingTransactions(CABankTran aDetail, IMatchSettings aSettings, PX.Objects.AP.Pair<DateTime, DateTime> tranDateRange, string curyID)
		{
			var cmd = new PXSelectReadonly2<CATranExt,
					LeftJoin<CA.Light.BAccount, On<CA.Light.BAccount.bAccountID, Equal<CATranExt.referenceID>>,
					LeftJoin<CATran2, On<CATran2.cashAccountID, Equal<CATranExt.cashAccountID>,
						And<CATran2.voidedTranID, Equal<CATranExt.tranID>,
						And<True, Equal<Required<CASetup.skipVoided>>>>>,
					LeftJoin<CABankTranMatch2, On<CABankTranMatch2.cATranID, Equal<CATranExt.tranID>,
						And<CABankTranMatch2.tranType, Equal<Required<CABankTran.tranType>>>>,
					LeftJoin<CABatchDetail, On<CABatchDetail.origModule, Equal<CATranExt.origModule>,
						And<CABatchDetail.origDocType, Equal<CATranExt.origTranType>,
						And<CABatchDetail.origRefNbr, Equal<CATranExt.origRefNbr>,
						And<CATranExt.isPaymentChargeTran, Equal<False>>>>>,
					LeftJoin<CABankTranMatch, On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAP>,
						And<CABankTranMatch.docType, Equal<CATranType.cABatch>,
						And<CABankTranMatch.docRefNbr, Equal<CABatchDetail.batchNbr>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>>>,
					LeftJoin<PRDirectDepositSplit, On<PRDirectDepositSplit.docType, Equal<CABatchDetail.origDocType>,
						And<PRDirectDepositSplit.refNbr, Equal<CABatchDetail.origRefNbr>>>>>>>>>,
					Where<CATranExt.cashAccountID, Equal<Required<CABankTran.cashAccountID>>,
						And<CATranExt.tranDate, Between<Required<CATranExt.tranDate>, Required<CATranExt.tranDate>>,
						And<CATranExt.curyID, Equal<Required<CATranExt.curyID>>,
						And<Where<PRDirectDepositSplit.caTranID, Equal<CATranExt.tranID>,
											And<PRDirectDepositSplit.lineNbr, Equal<CABatchDetail.origLineNbr>,
											Or<CABatchDetail.origLineNbr, Equal<CABatchDetail.origLineNbr.defaultValue>,
											Or<CABatchDetail.origLineNbr, IsNull>>>>>>>>>(Base);

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
			if (Base.CASetup.Current.SkipReconciled == true)
			{
				cmd.WhereAnd<Where<CATranExt.reconciled, Equal<False>>>();
			}

			return cmd.Select(aSettings.SkipVoided, aDetail.TranType, aDetail.TranType, aDetail.CashAccountID, tranDateRange.first, tranDateRange.second, curyID, aDetail.CuryTranAmt.Value);
		}

		[PXOverride]
		public virtual PXResultset<CABatch> SearchForMatchingCABatches(CABankTran aDetail, PX.Objects.AP.Pair<DateTime, DateTime> tranDateRange, string curyID, bool allowUnreleased)
		{
			return PXSelectJoin<CABatch,
							LeftJoin<CABatchDetail, On<CABatchDetail.batchNbr, Equal<CABatch.batchNbr>>,
							LeftJoin<PRDirectDepositSplit, On<PRDirectDepositSplit.docType, Equal<CABatchDetail.origDocType>,
								And<PRDirectDepositSplit.refNbr, Equal<CABatchDetail.origRefNbr>>>,
							LeftJoin <CA.Light.APPayment, On<CA.Light.APPayment.docType, Equal<CABatchDetail.origDocType>,
								And<CA.Light.APPayment.refNbr, Equal<CABatchDetail.origRefNbr>>>,
							LeftJoin<CABankTranMatch, On<CABankTranMatch.cATranID, Equal<CA.Light.APPayment.cATranID>,
								And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>>>>>,
							 Where<CABatch.cashAccountID, Equal<Required<CABatch.cashAccountID>>,
								And2<Where<CABatch.released, Equal<True>, Or<Required<CASetup.allowMatchingToUnreleasedBatch>, Equal<True>>>,
								And<Where<CABatch.tranDate, Between<Required<CABatch.tranDate>, Required<CABatch.tranDate>>,
								And<CABatch.curyID, Equal<Required<CABatch.curyID>>,
								And<CABatch.curyDetailTotal, Equal<Required<CABatch.curyDetailTotal>>,
								And2<Where<CABatch.reconciled, Equal<False>,
											Or<Required<CASetup.skipReconciled>, Equal<False>>>,
								And<Where<PRDirectDepositSplit.lineNbr, Equal<CABatchDetail.origLineNbr>,
											Or<CABatchDetail.origLineNbr, Equal<CABatchDetail.origLineNbr.defaultValue>>>>>>>>>>>>.
							Select(Base, aDetail.TranType, aDetail.CashAccountID, allowUnreleased, tranDateRange.first, tranDateRange.second,
								 curyID, -1 * aDetail.CuryTranAmt.Value, Base.CASetup.Current.SkipReconciled ?? false);

			
		}

		[PXOverride]
		public virtual PXResultset<CABatchDetail> SearchForMatchesInCABatches(string tranType, string batchNbr)
		{
			return PXSelectJoin<CABatchDetail,
					InnerJoin<CATran, On<CATran.origModule, Equal<CABatchDetail.origModule>,
						And<CATran.origTranType, Equal<CABatchDetail.origDocType>,
						And<CATran.origRefNbr, Equal<CABatchDetail.origRefNbr>,
						And<CATran.isPaymentChargeTran, Equal<False>>>>>,
					InnerJoin<CABankTranMatch, On<CABankTranMatch.cATranID, Equal<CATran.tranID>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTran.tranType>>>>,
					LeftJoin<PRDirectDepositSplit, On<PRDirectDepositSplit.docType, Equal<CABatchDetail.origDocType>,
							And<PRDirectDepositSplit.refNbr, Equal<CABatchDetail.origRefNbr>,
							And<Where<PRDirectDepositSplit.caTranID, Equal<CATran.tranID>,
											And<PRDirectDepositSplit.lineNbr, Equal<CABatchDetail.origLineNbr>,
											Or<CABatchDetail.origLineNbr, Equal<CABatchDetail.origLineNbr.defaultValue>>>>>>>>>>,
					Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>>>.Select(Base, tranType, batchNbr);
		}

		[PXOverride]
		public virtual PXResultset<CABankTranMatch> SearchForTranMatchForCABatch(string batchNbr)
		{
			return PXSelect<CABankTranMatch,
					Where<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>,
					And<CABankTranMatch.docType, Equal<CATranType.cABatch>>>>.Select(Base, batchNbr);
		}
	}
}
