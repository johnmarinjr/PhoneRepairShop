using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CA.BankStatementHelpers;
using PX.Objects.CA.BankStatementProtoHelpers;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.AP.MigrationMode;
using PX.Objects.AR.MigrationMode;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.EP;
using static PX.Objects.Common.UIState;
using PX.Objects.TX;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Objects.CA.Repositories;

namespace PX.Objects.CA
{
	public class CABankMatchingProcess : PXGraph<CABankMatchingProcess>, ICABankTransactionsDataProvider
	{
		public PXSelect<CABankTran> CABankTran;
		public PXSelect<CABankTranMatch, Where<CABankTranMatch.matchType, Equal<CABankTranMatch.matchType.match>, And<CABankTranMatch.tranID, Equal<Required<CABankTran.tranID>>>>> TranMatch;
		public PXSelect<CABankTranMatch, Where<CABankTranMatch.matchType, Equal<CABankTranMatch.matchType.charge>, And<CABankTranMatch.tranID, Equal<Required<CABankTran.tranID>>>>> TranMatchCharge;
		public PXSelect<EPExpenseClaimDetails> ExpenseReceipts;
		public PXSelectJoin<CABankTranAdjustment,
			LeftJoin<ARInvoice, On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>,
								And<CABankTranAdjustment.adjdRefNbr, Equal<ARInvoice.refNbr>,
								And<CABankTranAdjustment.adjdDocType, Equal<ARInvoice.docType>>>>>,
			Where<CABankTranAdjustment.tranID, Equal<Required<CABankTran.tranID>>>>
			Adjustments;

		public PXSelect<
			CABankTran,
			Where<CABankTran.processed, Equal<False>,
				And<CABankTran.cashAccountID, Equal<Required<CashAccount.cashAccountID>>,
				And<CABankTran.documentMatched, Equal<False>>>>>
			UnMatchedDetails;

		public PXSelect<CABankTran,
			Where<CABankTran.tranID, Equal<Required<CABankTran.tranID>>>> CABankTranSelection;
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CABankTran.curyInfoID>>>> currencyinfo;

		public PXSelect<CABankTranDetail, Where<CABankTranDetail.bankTranID, Equal<Required<CABankTran.tranID>>>> TranSplit;

		public PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>> cashAccount;
		public SelectFrom<TaxZone>.Where<TaxZone.taxZoneID.IsEqual<@P.AsString>>.View Taxzone;
		public SelectFrom<CABankTaxTran>
					.LeftJoin<Tax>.On<Tax.taxID.IsEqual<CABankTaxTran.taxID>>
					.Where<CABankTaxTran.bankTranID.IsEqual<@P.AsInt>
					  .And<CABankTaxTran.bankTranType.IsEqual<@P.AsString>>>.View TaxTrans;

		public SelectFrom<CABankTax>
				   .Where<CABankTax.bankTranID.IsEqual<CABankTran.tranID.FromCurrent>>.View Taxes;

		[PXHidden]
		public PXSelect<CABankTranRule, Where<CABankTranRule.isActive, Equal<True>>> Rules;

		public PXSetup<CASetup> CASetup;
		public PXSetup<APSetup> APSetup;
		public CMSetupSelect CMSetup;

		[PXHidden]
		public PXSelect<CAMatchProcess, Where<CAMatchProcess.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>> MatchProcess;
		[PXHidden]
		public PXSelectReadonly<CAMatchProcess, Where<CAMatchProcess.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>> MatchProcessSelect;

		[InjectDependency]
		public ICABankTransactionsRepository CABankTransactionsRepository { get; set; }

		#region MatchingMethods
		public virtual void DoMatch(CABankTransactionsMaint graph, int? cashAccountID)
		{
			IEnumerable<CABankTran> tranList = UnMatchedDetails.Select(cashAccountID).RowCast<CABankTran>();
			var cashAcc = cashAccount.SelectSingle(cashAccountID);
			PXLongOperation.StartOperation(graph.UID, delegate () {
				DoMatch(tranList, cashAcc, (Guid?)graph.UID);
				this.Persist();
			});
		}

		public virtual void DoMatch(IEnumerable<CABankTran> aRows, CashAccount cashAccount, Guid? processorID)
		{
			using (new CAMatchProcessContext(this, cashAccount.CashAccountID, processorID))
			{
				ClearCachedMatches();

				DoCAMatch(aRows, cashAccount);
				DoInvMatch(aRows, cashAccount);
				DoExpenseReceiptMatch(aRows, cashAccount);

				foreach (CABankTran det in aRows)
				{
					if (det.DocumentMatched != true && (det.CreateDocument != true
						|| (det.OrigModule == BatchModule.CA && det.TranDate == det.MatchingPaymentDate && det.EntryTypeID == null)))
					{
						if (det.CountMatches.HasValue && det.CountMatches == 0
							&& det.CountInvoiceMatches.HasValue && det.CountInvoiceMatches == 0
							&& (det.CountExpenseReceiptDetailMatches == null || det.CountExpenseReceiptDetailMatches == 0))
						{
							if (det.CreateDocument == true)
							{
								det.CreateDocument = false;
								CABankTran.Cache.Update(det);

							}
							det.CreateDocument = true;
							this.cashAccount.Current = cashAccount;
							CABankTran.Cache.Update(det);
							CreateDocument(det, cashAccount);
						}
					}
				}

				ClearCachedMatches();
			}
		}

		protected virtual List<CABankTran> DoCAMatchInner(IEnumerable<CABankTran> aRows, CashAccount cashAccount)
		{
			Dictionary<string, List<CABankTranDocRef>> cross = new Dictionary<string, List<CABankTranDocRef>>();
			Dictionary<int, CABankTran> rows = new Dictionary<int, CABankTran>();
			int rowCount = 0;
			decimal paymentMatchTreshold = ((cashAccount as IMatchSettings).MatchThreshold ?? 0m) / 100m;
			decimal paymentRelevanceTreshold = ((cashAccount as IMatchSettings).RelativeMatchThreshold ?? 0m) / 100m;

			foreach (object en in aRows)
			{
				rowCount++;
				CABankTran iDet = en as CABankTran;
				if (iDet.DocumentMatched == true || (iDet.CreateDocument == true
					&& (iDet.OrigModule != BatchModule.CA || iDet.TranDate != iDet.MatchingPaymentDate || iDet.EntryTypeID != null)))
				{
					continue;
				}

				if (!rows.ContainsKey(iDet.TranID.Value))
				{
					rows.Add(iDet.TranID.Value, iDet);
				}
				List<PXResult<CATranExt, Light.BAccount>> list = null;
				CATranExt[] bestMatches = { null, null };
				list = (List<PXResult<CATranExt, Light.BAccount>>)FindDetailMatches(this, iDet, cashAccount, bestMatches);
				if (bestMatches[0] != null
					&& (bestMatches[1] == null && bestMatches[0].MatchRelevance >= paymentRelevanceTreshold
					|| bestMatches[1] != null && (bestMatches[0].MatchRelevance - bestMatches[1].MatchRelevance) > paymentRelevanceTreshold
					|| bestMatches[0].MatchRelevance > paymentMatchTreshold))
				{
					CATranExt matchCandidate = bestMatches[0];
					CABankTranDocRef xref = new CABankTranDocRef();
					xref.Copy(iDet);
					xref.Copy(matchCandidate);
					if (xref.CATranID == null)
					{
						xref.DocModule = matchCandidate.OrigModule;
						xref.DocRefNbr = matchCandidate.OrigRefNbr;
						xref.DocType = matchCandidate.OrigTranType;
					}
					xref.MatchRelevance = matchCandidate.MatchRelevance;
					string key;
					if (matchCandidate.TranID.HasValue)
						key = matchCandidate.TranID.Value.ToString();
					else key = matchCandidate.OrigModule + matchCandidate.OrigTranType + matchCandidate.OrigRefNbr;

					if (cross.ContainsKey(key) == false)
					{
						cross.Add(key, new List<CABankTranDocRef>());
					}
					cross[key].Add(xref);
				}
			}

			return DoMatch(aRows, cross, rows);
		}

		public virtual void DoCAMatch(IEnumerable<CABankTran> aRows, CashAccount cashAccount)
		{
			List<CABankTran> spareDetails = new List<CABankTran>();
			spareDetails.AddRange(aRows);

			while (spareDetails.Count > 0)
			{
				spareDetails = DoCAMatchInner(spareDetails, cashAccount);
			}

			foreach (CABankTran iRow in aRows)
			{
				if (iRow.DocumentMatched == false
					&& (iRow.CreateDocument == false
						|| (iRow.OrigModule == BatchModule.CA && iRow.TranDate == iRow.MatchingPaymentDate && iRow.EntryTypeID == null))
					&& iRow.CountMatches > 0)
				{
					CATranExt[] bestMatches = { null, null };
					FindDetailMatches(this, iRow, cashAccount, bestMatches);
				}
			}
		}

		protected virtual List<CABankTran> DoDocumentMatchInner<TMatchRow>(
			IEnumerable<CABankTran> aRows,
			CashAccount cashAccount,
			Func<CABankMatchingProcess, CABankTran, CashAccount, decimal, TMatchRow[], IEnumerable> findDetailMatching)
			where TMatchRow : CABankTranDocumentMatch
		{
			var currentMatchSesstings = (IMatchSettings)cashAccount;
			Dictionary<string, List<CABankTranDocRef>> cross = new Dictionary<string, List<CABankTranDocRef>>();
			Dictionary<int, CABankTran> rows = new Dictionary<int, CABankTran>();
			decimal invoiceMatchTreshold = (currentMatchSesstings.MatchThreshold ?? 0m) / 100m;
			decimal invoiceRelevanceTreshold = (currentMatchSesstings.RelativeMatchThreshold ?? 0m) / 100m;

			foreach (object en in aRows)
			{
				CABankTran iDet = en as CABankTran;
				if (iDet.DocumentMatched == true || (iDet.CreateDocument == true
					&& (iDet.OrigModule != BatchModule.CA || iDet.TranDate != iDet.MatchingPaymentDate || iDet.EntryTypeID != null)))
				{
					continue;
				}

				if (!rows.ContainsKey(iDet.TranID.Value))
				{
					rows.Add(iDet.TranID.Value, iDet);
				}

				TMatchRow[] bestMatches = { null, null };

				findDetailMatching(this, iDet, cashAccount, Decimal.Zero, bestMatches);

				if (bestMatches[0] != null
					&& (bestMatches[1] == null && bestMatches[0].MatchRelevance >= invoiceRelevanceTreshold
						|| bestMatches[1] != null && (bestMatches[0].MatchRelevance - bestMatches[1].MatchRelevance) > invoiceRelevanceTreshold
						|| bestMatches[0].MatchRelevance > invoiceMatchTreshold))
				{
					TMatchRow matchCandidate = bestMatches[0];
					CABankTranDocRef xref = new CABankTranDocRef();
					xref.Copy(iDet);
					matchCandidate.BuildDocRef(xref);
					xref.MatchRelevance = matchCandidate.MatchRelevance;
					string key = matchCandidate.GetDocumentKey();
					if (cross.ContainsKey(key) == false)
					{
						cross.Add(key, new List<CABankTranDocRef>());
					}
					cross[key].Add(xref);
				}
			}

			return DoMatch(aRows, cross, rows);
		}

		public virtual void DoInvMatch(IEnumerable<CABankTran> aRows, CashAccount cashAccount)
		{
			List<CABankTran> spareDetails = new List<CABankTran>();
			spareDetails.AddRange(aRows);

			while (spareDetails.Count > 0)
			{
				spareDetails = DoDocumentMatchInner<CABankTranInvoiceMatch>(spareDetails, cashAccount, FindDetailMatchingInvoices);
			}

			foreach (CABankTran iRow in aRows)
			{
				if (iRow.DocumentMatched == false
					&& (iRow.CreateDocument == false
						|| (iRow.OrigModule == BatchModule.CA && iRow.TranDate == iRow.MatchingPaymentDate && iRow.EntryTypeID == null))
					&& iRow.CountInvoiceMatches > 0)
				{
					this.matchingInvoices.Remove(iRow);
					FindDetailMatchingInvoices(this, iRow, cashAccount);
				}
			}
		}

		public virtual void DoExpenseReceiptMatch(IEnumerable<CABankTran> aRows, CashAccount cashAccount)
		{
			List<CABankTran> spareDetails = new List<CABankTran>();
			spareDetails.AddRange(aRows);

			while (spareDetails.Count > 0)
			{
				spareDetails = DoDocumentMatchInner<CABankTranExpenseDetailMatch>(
					spareDetails,
					cashAccount,
					(aGraph, bankTran, settings, relevanceTreshold, bestMatches) => GetExpenseReceiptDetailMatches(aGraph, bankTran, settings, relevanceTreshold, bestMatches));
			}

			foreach (CABankTran iRow in aRows)
			{
				if (iRow.DocumentMatched == false
					&& (iRow.CreateDocument == false
						|| (iRow.OrigModule == BatchModule.CA && iRow.TranDate == iRow.MatchingPaymentDate && iRow.EntryTypeID == null))
					&& iRow.CountExpenseReceiptDetailMatches > 0)
				{
					matchingExpenseReceiptDetails.Remove(iRow);
					FindExpenseReceiptDetailMatches(this, iRow, cashAccount);
				}
			}
		}

		public virtual List<CABankTran> DoMatch(IEnumerable<CABankTran> aRows, Dictionary<string, List<CABankTranDocRef>> cross, Dictionary<int, CABankTran> rows)
		{
			Dictionary<int, CABankTran> spareDetails = new Dictionary<int, CABankTran>();
			foreach (KeyValuePair<string, List<CABankTranDocRef>> iCandidate in cross)
			{
				CABankTranDocRef bestMatch = null;
				foreach (CABankTranDocRef iRef in iCandidate.Value)
				{
					if (bestMatch == null || bestMatch.MatchRelevance < iRef.MatchRelevance)
					{
						bestMatch = iRef;
					}
				}
				if (bestMatch != null && bestMatch.TranID != null)
				{
					CABankTran detail;
					if (!rows.TryGetValue(bestMatch.TranID.Value, out detail))
					{
						detail = CABankTran.SearchAll<Asc<CABankTran.tranID,
							Asc<CABankTran.tranID>>>(new object[] { bestMatch.TranID });
						rows.Add(detail.TranID.Value, detail);
					}

					if (detail != null && TranMatch.Select(detail.TranID).Count == 0)
					{
						CABankTranMatch match = new CABankTranMatch()
						{
							TranID = detail.TranID,
							TranType = detail.TranType,
						};
						match.Copy(bestMatch);
						if (detail.DrCr == CADrCr.CACredit && (match.CATranID != null || match.DocType == CATranType.CABatch))
							match.CuryApplAmt = -1 * match.CuryApplAmt;
						TranMatch.Insert(match);
						detail.CreateDocument = false;
						detail = CABankTran.Cache.Update(detail) as CABankTran;
						detail.DocumentMatched = true;
						CABankTran.Cache.Update(detail);

						if (match.DocModule == BatchModule.EP && match.DocType == EPExpenseClaimDetails.DocType)
						{
							EPExpenseClaimDetails receipt =
								PXSelect<EPExpenseClaimDetails,
										Where<EPExpenseClaimDetails.claimDetailCD,
											Equal<Required<EPExpenseClaimDetails.claimDetailCD>>>>
										.Select(this, match.DocRefNbr);

							receipt.BankTranDate = detail.TranDate;

							ExpenseReceipts.Update(receipt);
						}
					}

					spareDetails.Remove(bestMatch.TranID.Value);
					foreach (CABankTranDocRef iMatch in iCandidate.Value)
					{
						if (Object.ReferenceEquals(iMatch, bestMatch)) continue;
						spareDetails[iMatch.TranID.Value] = null;
					}
				}
			}
			cross.Clear();
			List<CABankTran> spareDetails1 = new List<CABankTran>(spareDetails.Keys.Count);
			foreach (KeyValuePair<int, CABankTran> iDet in spareDetails)
			{
				CABankTran detail;
				if (!rows.TryGetValue(iDet.Key, out detail))
				{
					detail = CABankTran
						.SearchAll<
						Asc<CABankTran.tranID,
						Asc<CABankTran.tranID>>>(new object[] { iDet.Key });
					rows.Add(detail.TranID.Value, detail);
				}
				if (detail != null)
					spareDetails1.Add(detail);
			}
			return spareDetails1;
		}

		public virtual IEnumerable FindDetailMatches(CABankMatchingProcess graph, CABankTran aDetail, CashAccount cashAccount, CATranExt[] aBestMatches)
		{
			List<PXResult<CATranExt, Light.BAccount>> matchList = null;
			if (matchingTrans == null) matchingTrans = new Dictionary<Object, List<PXResult<CATranExt, Light.BAccount>>>();
			if (aBestMatches == null)
			{
				if (matchingTrans.TryGetValue(aDetail, out matchList))
				{
					aDetail.CountMatches = matchList.Count;
					if (aDetail.MatchedToExisting == true && matchList.Find((PXResult<CATranExt, Light.BAccount> result) => { CATranExt tran = result; return tran.IsMatched == true; }) == null)
					{
						matchList.ForEach((PXResult<CATranExt, Light.BAccount> result) =>
						{
							CATranExt tran = result;

							tran.IsMatched =
							PXSelect<
								CABankTranMatch,
								Where<CABankTranMatch.tranID, Equal<Required<CABankTranMatch.tranID>>,
									And<CABankTranMatch.cATranID, Equal<Required<CABankTranMatch.cATranID>>>>>
								.Select(graph, aDetail.TranID, tran.TranID)
								.Count > 0
							||
							(tran.OrigModule == BatchModule.AP &&
							tran.OrigTranType == CATranType.CABatch &&
							PXSelect<
								CABankTranMatch,
								Where<CABankTranMatch.tranID, Equal<Required<CABankTranMatch.tranID>>,
									And<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>,
									And<CABankTranMatch.docModule, Equal<Required<CABankTranMatch.docModule>>,
									And<CABankTranMatch.docType, Equal<Required<CABankTranMatch.docType>>>>>>>
								.Select(graph, aDetail.TranID, tran.OrigRefNbr, tran.OrigModule, tran.OrigTranType)
								.Count > 0);
						});
					}
					return matchList;
				}
			}
			decimal minRelevance = decimal.Zero; //It shoild be "cashAccount.RelativeMatchThreshold.Value;", but current implementation depends on decimal.Zero
			matchList = (List<PXResult<CATranExt, Light.BAccount>>)FindDetailMatches(graph, aDetail, cashAccount, aBestMatches, minRelevance);
			matchingTrans[aDetail] = matchList;

			return matchList;
		}

		protected virtual IEnumerable<PXResult<CATranExt, Light.BAccount>> FindDetailMatches(CABankMatchingProcess graph, CABankTran aDetail, CashAccount cashAccount, CATranExt[] aBestMatches, decimal minRelevance)
		{
			return StatementsMatchingProto.FindDetailMatches(graph, aDetail, cashAccount, minRelevance, aBestMatches);
		}

		public virtual IEnumerable FindDetailMatchingInvoices(CABankMatchingProcess graph, CABankTran aDetail, CashAccount cashAccount)
		{
			decimal relevanceTreshold = decimal.Zero; //It shoild be "cashAccount.RelativeMatchThreshold.Value;", but current implementation depends on decimal.Zero
			return FindDetailMatchingInvoicesProc(graph, aDetail, cashAccount, relevanceTreshold, null);
		}

		public virtual IList<CABankTranExpenseDetailMatch> FindExpenseReceiptDetailMatches(CABankMatchingProcess graph, CABankTran detail, CashAccount cashAccount)
		{
			decimal relevanceTreshold = decimal.Zero; //It shoild be "cashAccount.RelativeMatchThreshold.Value;", but current implementation depends on decimal.Zero
			return GetExpenseReceiptDetailMatches(graph, detail, cashAccount, relevanceTreshold, null);
		}

		public virtual IEnumerable FindDetailMatchingInvoices(CABankMatchingProcess graph, CABankTran aDetail, CashAccount cashAccount, decimal aRelevanceTreshold, CABankTranInvoiceMatch[] aBestMatches)
		{
			List<CABankTranInvoiceMatch> result = null;
			if (matchingInvoices == null) matchingInvoices = new Dictionary<object, List<CABankTranInvoiceMatch>>(1);
			bool recalculateCounts = (aBestMatches != null);
			if (!recalculateCounts && matchingInvoices.TryGetValue(aDetail, out result))
			{
				aDetail.CountInvoiceMatches = result.Count;
				return result;
			}
			else
			{
				result = FindDetailMatchingInvoicesProc(graph, aDetail, cashAccount, aRelevanceTreshold, aBestMatches);
				matchingInvoices[aDetail] = result;
			}

			aDetail.CountInvoiceMatches = result.Count;
			return result;
		}

		public static List<CABankTranInvoiceMatch> FindDetailMatchingInvoicesProc(PXGraph graph, CABankTran aDetail, CashAccount cashAccount, decimal aRelevanceTreshold, CABankTranInvoiceMatch[] aBestMatches)
		{
			var result = new List<CABankTranInvoiceMatch>();
			var aSettings = (IMatchSettings)cashAccount;

			Decimal? amount = aDetail.CuryTranAmt > 0 ? aDetail.CuryTranAmt : -aDetail.CuryTranAmt;
			CABankTranInvoiceMatch bestMatch = null;
			int bestMatchesNumber = aBestMatches != null ? aBestMatches.Length : 0;

			bool clearingAccount = cashAccount.ClearingAccount == true;
			if ((aDetail.DrCr == CA.CADrCr.CADebit ||
				aDetail.DrCr == CA.CADrCr.CACredit && aSettings.AllowMatchingCreditMemo == true) &&
				amount > 0)
			{
				List<object> bqlParams;
				PXSelectBase<Light.ARInvoice> sel = CreateARInvoiceQuery(graph, aDetail, cashAccount, amount, out bqlParams);

				foreach (PXResult<Light.ARInvoice, Light.BAccount, CABankTranMatch, Light.ARAdjust, Light.CABankTranAdjustment> it in
					sel.Select(bqlParams.ToArray()))
				{
					Light.ARInvoice iDoc = it;
					Light.BAccount iCustomer = it;
					CABankTranMatch iMatch = it;
					Light.CABankTranAdjustment iAdj = it;
					CABankTranInvoiceMatch tran = new CABankTranInvoiceMatch();
					tran.Copy(iDoc);
					tran.ReferenceName = iCustomer.AcctName;
					if (tran.DrCr != aDetail.DrCr)
					{
						tran.CuryTranAmt = -1 * tran.CuryTranAmt;
						tran.TranAmt = -1 * tran.TranAmt;
						tran.CuryDiscAmt = -1 * tran.CuryDiscAmt;
						tran.DiscAmt = -1 * tran.DiscAmt;
					}

					if (IsAlreadyMatched(graph, tran, aDetail, iMatch) || IsAlreadyInAdjustment(graph, iAdj, tran.OrigTranType, tran.OrigRefNbr, tran.OrigModule)) continue;
					tran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, tran, aSettings);
					bool isMatchedToCurrent = (iMatch != null && iMatch.TranID.HasValue);
					if (isMatchedToCurrent == false && tran.MatchRelevance < aRelevanceTreshold)
						continue;
					if (bestMatchesNumber > 0)
					{
						for (int i = 0; i < bestMatchesNumber; i++)
						{
							if (aBestMatches[i] == null || aBestMatches[i].MatchRelevance < tran.MatchRelevance)
							{
								for (int j = bestMatchesNumber - 1; j > i; j--)
								{
									aBestMatches[j] = aBestMatches[j - 1];
								}
								aBestMatches[i] = tran;
								break;
							}
						}
					}
					else
					{
						if (bestMatch == null || bestMatch.MatchRelevance < tran.MatchRelevance)
						{
							bestMatch = tran;
						}
					}
					tran.IsBestMatch = false;
					result.Add(tran);
				}
			}
			if (aDetail.DrCr == CA.CADrCr.CACredit && !clearingAccount)
			{
				List<object> bqlParams;

				PXSelectBase<Light.APInvoice> sel = CreateAPInvoiceQuery(graph, aDetail, cashAccount, amount, out bqlParams);

				foreach (PXResult<Light.APInvoice, Light.BAccount, CABankTranMatch, Light.APAdjust, Light.APPayment, Light.CABankTranAdjustment> it in
					sel.Select(bqlParams.ToArray()))
				{
					Light.APInvoice iDoc = it;
					Light.BAccount iPayee = it;
					CABankTranMatch iMatch = it;
					Light.CABankTranAdjustment iAdj = it;
					CABankTranInvoiceMatch tran = new CABankTranInvoiceMatch();
					tran.Copy(iDoc);
					tran.ReferenceName = iPayee.AcctName;
					if (tran.DrCr != aDetail.DrCr)
					{
						tran.CuryTranAmt = -1 * tran.CuryTranAmt;
						tran.TranAmt = -1 * tran.TranAmt;
						tran.CuryDiscAmt = -1 * tran.CuryDiscAmt;
						tran.DiscAmt = -1 * tran.DiscAmt;
					}

					if (IsAlreadyMatched(graph, tran, aDetail, iMatch) || IsAlreadyInAdjustment(graph, iAdj, tran.OrigTranType, tran.OrigRefNbr, tran.OrigModule)) continue;
					tran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, tran, aSettings);
					bool isMatchedToCurrent = (iMatch != null && iMatch.TranID.HasValue);
					if (isMatchedToCurrent == false && tran.MatchRelevance < aRelevanceTreshold)
						continue;
					if (bestMatchesNumber > 0)
					{
						for (int i = 0; i < bestMatchesNumber; i++)
						{
							if (aBestMatches[i] == null || aBestMatches[i].MatchRelevance < tran.MatchRelevance)
							{
								for (int j = bestMatchesNumber - 1; j > i; j--)
								{
									aBestMatches[j] = aBestMatches[j - 1];
								}
								aBestMatches[i] = tran;
								break;
							}
						}
					}
					else
					{
						if (bestMatch == null || bestMatch.MatchRelevance < tran.MatchRelevance)
						{
							bestMatch = tran;
						}
					}
					tran.IsBestMatch = false;
					result.Add(tran);
				}
			}
			if (bestMatchesNumber > 0)
				bestMatch = aBestMatches[0];
			if (bestMatch != null)
			{
				bestMatch.IsBestMatch = true;
			}

			return result;
		}

		protected static PXSelectBase<Light.ARInvoice> CreateARInvoiceQuery(PXGraph graph, CABankTran aDetail, CashAccount cashAccount, decimal? tranAmount, out List<object> bqlParams)
		{
			bqlParams = new List<object>() { aDetail.TranType, cashAccount.CuryID };
			var aSettings = (IMatchSettings)cashAccount;

			PXSelectBase<Light.ARInvoice> sel = new PXSelectJoin<Light.ARInvoice,
				InnerJoin<Light.BAccount, On<Light.BAccount.bAccountID, Equal<Light.ARInvoice.customerID>>,
				LeftJoin<CABankTranMatch, On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAR>,
					And<CABankTranMatch.docType, Equal<Light.ARInvoice.docType>,
					And<CABankTranMatch.docRefNbr, Equal<Light.ARInvoice.refNbr>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTranMatch.tranType>>>>>>,
				LeftJoin<Light.ARAdjust, On<Light.ARAdjust.adjdDocType, Equal<Light.ARInvoice.docType>,
					And<Light.ARAdjust.adjdRefNbr, Equal<Light.ARInvoice.refNbr>,
					And<Light.ARAdjust.released, Equal<boolFalse>,
					And<Light.ARAdjust.voided, Equal<boolFalse>>>>>,
				LeftJoin<Light.CABankTranAdjustment, On<Light.CABankTranAdjustment.adjdDocType, Equal<Light.ARInvoice.docType>,
					And<Light.CABankTranAdjustment.adjdRefNbr, Equal<Light.ARInvoice.refNbr>,
					And<Light.CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>,
					And<Light.CABankTranAdjustment.released, Equal<boolFalse>>>>>>>>>,
					Where<Light.ARAdjust.adjgRefNbr, IsNull,
						And<Light.ARInvoice.openDoc, Equal<True>,
						And<Light.ARInvoice.released, Equal<True>,
						And<Light.ARInvoice.curyDocBal, NotEqual<decimal0>,
						And<Light.ARInvoice.curyID, Equal<Required<ARInvoice.curyID>>>>>>>>(graph);

			if (aDetail.PayeeBAccountID == null || aDetail.MultipleMatching == false)
			{
				if (aDetail.DrCr == CA.CADrCr.CADebit)
				{
					sel
						.WhereAnd<
						Where<Light.ARInvoice.docType, Equal<ARDocType.invoice>>>();
				}
				else
				{
					sel
						.WhereAnd<
						Where<Light.ARInvoice.docType, Equal<ARDocType.creditMemo>>>();
				}
			}

			if (aDetail.MultipleMatching == true)
			{
				bool hasCustomerOrCharge = !(aDetail.PayeeBAccountID == null && (aDetail.ChargeTypeID == null || aDetail.DrCr == aDetail.ChargeDrCr || aDetail.ChargeDrCr == null));
				if (aSettings.InvoiceFilterByDate == true)
				{
					Triplet<DateTime, DateTime, DateTime> tranDateRange = StatementsMatchingProto.GetInvoiceDateRangeForMatch(aDetail, aSettings);

					if (!hasCustomerOrCharge)
					{
						sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.ARInvoice.discDate, Less<Required<Light.ARInvoice.discDate>>,
											Or<Light.ARInvoice.discDate, IsNull>>,
									And<Light.ARInvoice.curyDocBal, LessEqual<Required<ARInvoice.curyDocBal>>,
									And<Where<Light.ARInvoice.dueDate, GreaterEqual<Required<ARInvoice.dueDate>>,
										And<Light.ARInvoice.dueDate, LessEqual<Required<ARInvoice.dueDate>>,
										Or<Light.ARInvoice.dueDate, IsNull>>>>>>,
								Or<Where<Light.ARInvoice.discDate, GreaterEqual<Required<Light.ARInvoice.discDate>>,
									And<Sub<Light.ARInvoice.curyDocBal, Light.ARInvoice.curyDiscBal>, LessEqual<Required<ARInvoice.curyDocBal>>,
									And<Light.ARInvoice.discDate, LessEqual<Required<ARInvoice.discDate>>>>>>>>();

						bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranAmount, tranDateRange.third });
					}
					else
					{
						sel.WhereAnd<
						Where2<
							Where<Light.ARInvoice.dueDate, GreaterEqual<Required<ARInvoice.dueDate>>,
								And<Light.ARInvoice.dueDate, LessEqual<Required<ARInvoice.dueDate>>,
								Or<Light.ARInvoice.dueDate, IsNull>>>,
							Or<Where<Light.ARInvoice.discDate, GreaterEqual<Required<Light.ARInvoice.discDate>>,
								And<Light.ARInvoice.discDate, LessEqual<Required<ARInvoice.discDate>>>>>>>();

						bqlParams.AddRange(new object[] { tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranDateRange.third });
					}
				}
				else
				{
					if (!hasCustomerOrCharge)
					{
						sel.WhereAnd<
							Where2<
								Where2<
									Where<Light.ARInvoice.discDate, Less<Required<Light.ARInvoice.discDate>>,
										Or<Light.ARInvoice.discDate, IsNull>>,
									And<Light.ARInvoice.curyDocBal, LessEqual<Required<ARInvoice.curyDocBal>>>>,
								Or<Where<Light.ARInvoice.discDate, GreaterEqual<Required<Light.ARInvoice.discDate>>,
									And<Sub<Light.ARInvoice.curyDocBal, Light.ARInvoice.curyDiscBal>, LessEqual<Required<ARInvoice.curyDocBal>>>>>>>();

						bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, aDetail.TranDate, tranAmount });
					}
				}
			}
			else
			{
				if (aSettings.InvoiceFilterByDate == true)
				{
					Triplet<DateTime, DateTime, DateTime> tranDateRange = StatementsMatchingProto.GetInvoiceDateRangeForMatch(aDetail, aSettings);

					sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.ARInvoice.discDate, Less<Required<ARInvoice.discDate>>,
											Or<Light.ARInvoice.discDate, IsNull>>,
									And<Light.ARInvoice.curyDocBal, Equal<Required<ARInvoice.curyDocBal>>,
									And<Where<Light.ARInvoice.dueDate, GreaterEqual<Required<ARInvoice.dueDate>>,
										And<Light.ARInvoice.dueDate, LessEqual<Required<ARInvoice.dueDate>>,
										Or<Light.ARInvoice.dueDate, IsNull>>>>>>,
								Or<Where<Light.ARInvoice.discDate, GreaterEqual<Required<ARInvoice.discDate>>,
									And<Sub<Light.ARInvoice.curyDocBal, ARInvoice.curyDiscBal>, Equal<Required<ARInvoice.curyDocBal>>,
									And<Light.ARInvoice.discDate, LessEqual<Required<ARInvoice.discDate>>>>>>>>();

					bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranAmount, tranDateRange.third });
				}
				else
				{
					sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.ARInvoice.discDate, Less<Required<Light.ARInvoice.discDate>>,
											Or<Light.ARInvoice.discDate, IsNull>>,
									And<Light.ARInvoice.curyDocBal, Equal<Required<ARInvoice.curyDocBal>>>>,
								Or<Where<Light.ARInvoice.discDate, GreaterEqual<Required<Light.ARInvoice.discDate>>,
									And<Sub<Light.ARInvoice.curyDocBal, Light.ARInvoice.curyDiscBal>, Equal<Required<ARInvoice.curyDocBal>>>>>>>();

					bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, aDetail.TranDate, tranAmount });
				}
			}
			
			if (aDetail.PayeeBAccountID != null)
			{
				sel.WhereAnd<
					Where<Light.ARInvoice.customerID,
						In2<Search<AR.Override.BAccount.bAccountID,
										Where<AR.Override.BAccount.bAccountID, Equal<Required<ARInvoice.customerID>>,
										Or<AR.Override.BAccount.parentBAccountID, Equal<Required<ARInvoice.customerID>>>>>>>>();

				bqlParams.Add(aDetail.PayeeBAccountID);
				bqlParams.Add(aDetail.PayeeBAccountID);
			}

			CashAccount cashAcc = null;
			if (aSettings.InvoiceFilterByCashAccount == true)
			{
				cashAcc = CashAccount.PK.Find(graph, aDetail.CashAccountID);
				sel.WhereAnd<
					Where<Light.ARInvoice.cashAccountID,
							Equal<Required<Light.ARInvoice.cashAccountID>>,
						Or<Where<Light.ARInvoice.cashAccountID, IsNull,
							And<Light.ARInvoice.branchID, Equal<Required<Light.ARInvoice.branchID>>>>>>>();

				bqlParams.Add(aDetail.CashAccountID);
				bqlParams.Add(cashAcc.BranchID);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.interBranch>() == false)
			{
				cashAcc = cashAcc ?? CashAccount.PK.Find(graph, aDetail.CashAccountID);
				sel.WhereAnd<Where<Light.ARInvoice.branchID, Equal<Required<Light.ARInvoice.branchID>>>>();
				bqlParams.Add(cashAcc.BranchID);
			}

			return sel;
		}

		protected static PXSelectBase<Light.APInvoice> CreateAPInvoiceQuery(PXGraph graph, CABankTran aDetail, CashAccount cashAccount, decimal? tranAmount, out List<object> bqlParams)
		{
			bqlParams = new List<object>() { aDetail.TranType, cashAccount.CuryID, aDetail.TranDate, aDetail.TranPeriodID };
			var aSettings = (IMatchSettings)cashAccount;

			PXSelectBase<Light.APInvoice> sel = new PXSelectJoin<
					Light.APInvoice,
					InnerJoin<Light.BAccount,
						On<Light.BAccount.bAccountID, Equal<Light.APInvoice.vendorID>>,
					LeftJoin<CABankTranMatch,
						On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAP>,
						And<CABankTranMatch.docType, Equal<Light.APInvoice.docType>,
						And<CABankTranMatch.docRefNbr, Equal<Light.APInvoice.refNbr>,
						And<CABankTranMatch.tranType, Equal<Required<CABankTranMatch.tranType>>>>>>,
					LeftJoin<Light.APAdjust,
						On<Light.APAdjust.adjdDocType, Equal<Light.APInvoice.docType>,
						And<Light.APAdjust.adjdRefNbr, Equal<Light.APInvoice.refNbr>,
						And<Light.APAdjust.released, Equal<boolFalse>>>>,
					LeftJoin<Light.APPayment,
						On<Light.APPayment.docType, Equal<Light.APInvoice.docType>,
						And<Light.APPayment.refNbr, Equal<Light.APInvoice.refNbr>>>,
					LeftJoin<Light.CABankTranAdjustment,
						On<Light.CABankTranAdjustment.adjdDocType, Equal<Light.APInvoice.docType>,
						And<Light.CABankTranAdjustment.adjdRefNbr, Equal<Light.APInvoice.refNbr>,
						And<Light.CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAP>,
						And<Light.CABankTranAdjustment.released, Equal<boolFalse>>>>>>>>>>,
					Where<Light.APAdjust.adjgRefNbr, IsNull,
						And<Light.APInvoice.openDoc, Equal<True>,
						And<Light.APInvoice.released, Equal<True>,
						And<Light.APInvoice.curyDocBal, NotEqual<decimal0>,
						And<Light.APInvoice.curyID, Equal<Required<APInvoice.curyID>>,
						And<Where<Light.APInvoice.docDate, LessEqual<Required<Light.APInvoice.docDate>>,
							And<Light.APInvoice.finPeriodID, LessEqual<Required<Light.APInvoice.finPeriodID>>,
							Or<Current<APSetup.earlyChecks>, Equal<True>>>>>>>>>>>(graph);

			if (aDetail.PayeeBAccountID == null || aDetail.MultipleMatching == false)
			{
				sel.WhereAnd<Where<Light.APInvoice.docType, Equal<APDocType.invoice>,
						And<Light.APPayment.refNbr, IsNull>>>();
			}
			else
			{
				sel.WhereAnd<
					Where<Light.APInvoice.docType, Equal<APDocType.invoice>,
						Or<Light.APInvoice.docType, Equal<APDocType.debitAdj>,
						Or<Light.APInvoice.docType, Equal<APDocType.creditAdj>>>>>();
			}

			if (aDetail.MultipleMatching == true)
			{
				bool hasCustomerOrCharge = !(aDetail.PayeeBAccountID == null && (aDetail.ChargeTypeID == null || aDetail.DrCr == aDetail.ChargeDrCr || aDetail.ChargeDrCr == null));

				if (aSettings.InvoiceFilterByDate == true)
				{
					Triplet<DateTime, DateTime, DateTime> tranDateRange = StatementsMatchingProto.GetInvoiceDateRangeForMatch(aDetail, aSettings);

					if (!hasCustomerOrCharge)
					{
						sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.APInvoice.discDate, Less<Required<Light.APInvoice.discDate>>,
											Or<Light.APInvoice.discDate, IsNull>>,
									And<Light.APInvoice.curyDocBal, LessEqual<Required<Light.APInvoice.curyDocBal>>,
									And<Where<Light.APInvoice.dueDate, GreaterEqual<Required<Light.APInvoice.dueDate>>,
										And<Light.APInvoice.dueDate, LessEqual<Required<Light.APInvoice.dueDate>>,
										Or<Light.APInvoice.dueDate, IsNull>>>>>>,
								Or<Where<Light.APInvoice.discDate, GreaterEqual<Required<Light.APInvoice.discDate>>,
									And<Sub<Light.APInvoice.curyDocBal, Light.APInvoice.curyDiscBal>, LessEqual<Required<Light.APInvoice.curyDocBal>>,
									And<Light.APInvoice.discDate, LessEqual<Required<Light.APInvoice.discDate>>>>>>>>();

						bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranAmount, tranDateRange.third });
					}
					else
					{
						sel.WhereAnd<
						Where2<
							Where<Light.APInvoice.dueDate, GreaterEqual<Required<Light.APInvoice.dueDate>>,
										And<Light.APInvoice.dueDate, LessEqual<Required<Light.APInvoice.dueDate>>,
										Or<Light.APInvoice.dueDate, IsNull>>>,
								Or<Where<Light.APInvoice.discDate, GreaterEqual<Required<Light.APInvoice.discDate>>,
									And<Light.APInvoice.discDate, LessEqual<Required<Light.APInvoice.discDate>>>>>>>();

						bqlParams.AddRange(new object[] { tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranDateRange.third });
					}
				}
				else
				{
					if (!hasCustomerOrCharge)
					{
						sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.APInvoice.discDate, Less<Required<Light.APInvoice.discDate>>,
											Or<Light.APInvoice.discDate, IsNull>>,
									And<Light.APInvoice.curyDocBal, LessEqual<Required<Light.APInvoice.curyDocBal>>>>,
								Or<Where<Light.APInvoice.discDate, GreaterEqual<Required<Light.APInvoice.discDate>>,
									And<Sub<Light.APInvoice.curyDocBal, Light.APInvoice.curyDiscBal>, LessEqual<Required<Light.APInvoice.curyDocBal>>>>>>>();

						bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, aDetail.TranDate, tranAmount });
					}
				}
			}
			else
			{
				if (aSettings.InvoiceFilterByDate == true)
				{
					Triplet<DateTime, DateTime, DateTime> tranDateRange = StatementsMatchingProto.GetInvoiceDateRangeForMatch(aDetail, aSettings);

					sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.APInvoice.discDate, Less<Required<Light.APInvoice.discDate>>,
											Or<Light.APInvoice.discDate, IsNull>>,
									And<Light.APInvoice.curyDocBal, Equal<Required<Light.APInvoice.curyDocBal>>,
									And<Where<Light.APInvoice.dueDate, GreaterEqual<Required<Light.APInvoice.dueDate>>,
										And<Light.APInvoice.dueDate, LessEqual<Required<Light.APInvoice.dueDate>>,
										Or<Light.APInvoice.dueDate, IsNull>>>>>>,
								Or<Where<Light.APInvoice.discDate, GreaterEqual<Required<Light.APInvoice.discDate>>,
									And<Sub<Light.APInvoice.curyDocBal, Light.APInvoice.curyDiscBal>, Equal<Required<Light.APInvoice.curyDocBal>>,
									And<Light.APInvoice.discDate, LessEqual<Required<APInvoice.discDate>>>>>>>>();

					bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, tranDateRange.first, tranDateRange.second, aDetail.TranDate, tranAmount, tranDateRange.third });
				}
				else
				{
					sel.WhereAnd<
						Where2<
							Where2<
								Where<Light.APInvoice.discDate, Less<Required<Light.APInvoice.discDate>>,
											Or<Light.APInvoice.discDate, IsNull>>,
									And<Light.APInvoice.curyDocBal, Equal<Required<Light.APInvoice.curyDocBal>>>>,
								Or<Where<Light.APInvoice.discDate, GreaterEqual<Required<Light.APInvoice.discDate>>,
									And<Sub<Light.APInvoice.curyDocBal, Light.APInvoice.curyDiscBal>, Equal<Required<Light.APInvoice.curyDocBal>>>>>>>();

					bqlParams.AddRange(new object[] { aDetail.TranDate, tranAmount, aDetail.TranDate, tranAmount });
				}
			}
			if (aDetail.PayeeBAccountID != null)
			{
				sel.WhereAnd<
					Where<Light.APInvoice.vendorID,
						Equal<Required<APInvoice.vendorID>>>>();

				bqlParams.Add(aDetail.PayeeBAccountID);
			}

			CashAccount cashAcc = null;
			if (aSettings.InvoiceFilterByCashAccount == true)
			{
				cashAcc = CashAccount.PK.Find(graph, aDetail.CashAccountID);
				sel.WhereAnd<
					Where<APInvoice.payAccountID,
							Equal<Required<Light.APInvoice.payAccountID>>,
						Or<Where<Light.APInvoice.payAccountID, IsNull,
							And<Light.APInvoice.branchID, Equal<Required<Light.APInvoice.branchID>>>>>>>();

				bqlParams.Add(aDetail.CashAccountID);
				bqlParams.Add(cashAcc.BranchID);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.interBranch>() == false)
			{
				cashAcc = cashAcc ?? CashAccount.PK.Find(graph, aDetail.CashAccountID);
				sel.WhereAnd<Where<Light.APInvoice.branchID, Equal<Required<Light.APInvoice.branchID>>>>();

				bqlParams.Add(cashAcc.BranchID);
			}

			return sel;
		}

		public virtual List<CABankTranExpenseDetailMatch> GetExpenseReceiptDetailMatches(
			CABankMatchingProcess graph,
			CABankTran bankTran,
			CashAccount cashAccount,
			decimal relevanceTreshold,
			CABankTranExpenseDetailMatch[] bestMatches)
		{
			if (cashAccount.UseForCorpCard != true)
			{
				return new List<CABankTranExpenseDetailMatch>();
			}

			List<CABankTranExpenseDetailMatch> result = null;

			if (matchingExpenseReceiptDetails == null)
			{
				matchingExpenseReceiptDetails = new Dictionary<object, List<CABankTranExpenseDetailMatch>>(1);
			}

			bool recalculateCounts = (bestMatches != null);

			if (!recalculateCounts && matchingExpenseReceiptDetails.TryGetValue(bankTran, out result))
			{
				bankTran.CountExpenseReceiptDetailMatches = result.Count;
				return result;
			}
			else
			{

				if(result == null)
				{
					result = new List<CABankTranExpenseDetailMatch>();
				}

				result.AddRange(FindExpenseReceiptDetailMatches(graph, bankTran, cashAccount, relevanceTreshold, bestMatches));
				matchingExpenseReceiptDetails[bankTran] = result;
			}

			bankTran.CountExpenseReceiptDetailMatches = result.Count;

			return result;
		}

		public static IList<CABankTranExpenseDetailMatch> FindExpenseReceiptDetailMatches(PXGraph graph, CABankTran bankTran, CashAccount cashAccount, decimal relevanceTreshold, CABankTranExpenseDetailMatch[] bestMatches)
		{
			var result = new List<CABankTranExpenseDetailMatch>();
			var settings = (IMatchSettings)cashAccount;

			Pair<DateTime, DateTime> tranDateRange = StatementsMatchingProto.GetDateRangeForMatch(bankTran, settings);

			Decimal amount = Math.Abs(bankTran.CuryTranAmt.Value);

			decimal curyDiffThresholdValue = 0m;

			if (settings.CuryDiffThreshold != null)
			{
				curyDiffThresholdValue = amount / 100 * settings.CuryDiffThreshold.Value;
			}

			decimal amountFrom = amount - curyDiffThresholdValue;
			decimal amountTo = amount + curyDiffThresholdValue;

			int bestMatchesNumber = bestMatches != null ? bestMatches.Length : 0;

			if (bankTran.CuryTranAmt < 0)
			{
				CABankTranMatch existingBankTranMatch =
					PXSelect<CABankTranMatch,
						Where<CABankTranMatch.tranID, Equal<Required<CABankTranMatch.tranID>>,
								And<CABankTranMatch.docModule, Equal<BatchModule.moduleEP>,
								And<CABankTranMatch.docType, Equal<EPExpenseClaimDetails.docType>>>>>
						.Select(graph, bankTran.TranID);

				var receipts = PXSelectJoin<
										EPExpenseClaimDetails,
										InnerJoin<Light.BAccount,
											On<EPExpenseClaimDetails.employeeID, Equal<Light.BAccount.bAccountID>>,
										InnerJoin<CACorpCard,
											On<CACorpCard.corpCardID, Equal<EPExpenseClaimDetails.corpCardID>>,
										LeftJoin<CABankTranMatch,
											On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleEP>,
												And<CABankTranMatch.docType, Equal<EPExpenseClaimDetails.docType>,
												And<CABankTranMatch.docRefNbr, Equal<EPExpenseClaimDetails.claimDetailCD>>>>,
										LeftJoin<CurrencyInfo,
											On<EPExpenseClaimDetails.claimCuryInfoID, Equal<CurrencyInfo.curyInfoID>>,
										LeftJoin<CATran,
											On<CATran.origModule, Equal<BatchModule.moduleAP>,
												And<EPExpenseClaimDetails.aPDocType, Equal<CATran.origTranType>,
												And<EPExpenseClaimDetails.aPRefNbr, Equal<CATran.origRefNbr>>>>,
										LeftJoin<GLTran,
											On<GLTran.module, Equal<BatchModule.moduleAP>,
												And<EPExpenseClaimDetails.aPDocType, Equal<GLTran.tranType>,
												And<EPExpenseClaimDetails.aPRefNbr, Equal<GLTran.refNbr>,
												And<EPExpenseClaimDetails.aPLineNbr, Equal<GLTran.tranLineNbr>>>>>,
										LeftJoin<CATran2,
											On<CATran2.origModule, Equal<BatchModule.moduleAP>,
												And<CATran2.origTranType, Equal<GLTranType.gLEntry>,
												And<CATran2.origRefNbr, Equal<GLTran.batchNbr>,
												And<CATran2.origLineNbr, Equal<GLTran.lineNbr>>>>>>>>>>>>,
										Where2<
											Where<EPExpenseClaimDetails.claimCuryTranAmtWithTaxes, Between<Required<EPExpenseClaimDetails.claimCuryTranAmtWithTaxes>, Required<EPExpenseClaimDetails.claimCuryTranAmtWithTaxes>>,
												And<EPExpenseClaimDetails.curyID, NotEqual<EPExpenseClaimDetails.cardCuryID>,
												Or<EPExpenseClaimDetails.claimCuryTranAmtWithTaxes, Equal<Required<EPExpenseClaimDetails.claimCuryTranAmtWithTaxes>>>>>,
											And<EPExpenseClaimDetails.hold, NotEqual<True>,
											And<EPExpenseClaimDetails.rejected, NotEqual<True>,
											And<EPExpenseClaimDetails.paidWith, NotEqual<EPExpenseClaimDetails.paidWith.cash>,
											And<CACorpCard.cashAccountID, Equal<Required<CACorpCard.cashAccountID>>,
											And<EPExpenseClaimDetails.expenseDate, GreaterEqual<Required<EPExpenseClaimDetails.expenseDate>>,
											And<EPExpenseClaimDetails.expenseDate, LessEqual<Required<EPExpenseClaimDetails.expenseDate>>,
											And<CATran.tranID, IsNull,
											And<CATran2.tranID, IsNull,
											Or<EPExpenseClaimDetails.claimDetailCD, Equal<Required<EPExpenseClaimDetails.claimDetailCD>>>>>>>>>>>>>
										.Select(graph,
												amountFrom,
												amountTo,
												amount,
												cashAccount.CashAccountID,
												tranDateRange.first,
												tranDateRange.second,
												existingBankTranMatch?.DocRefNbr);


				foreach (PXResult<EPExpenseClaimDetails, Light.BAccount, CACorpCard, CABankTranMatch, CurrencyInfo> row in receipts)
				{
					EPExpenseClaimDetails receipt = row;
					Light.BAccount employeeBAccount = row;
					CACorpCard card = row;
					CABankTranMatch matchLink = row;
					CurrencyInfo claimCurrencyInfo = row;

					CABankTranExpenseDetailMatch matchRow = new CABankTranExpenseDetailMatch
					{
						RefNbr = receipt.ClaimDetailCD,
						ExtRefNbr = receipt.ExpenseRefNbr,
						CuryDocAmt = receipt.ClaimCuryTranAmtWithTaxes,
						ClaimCuryID = claimCurrencyInfo?.CuryID ?? cashAccount.CuryID,
						CuryDocAmtDiff = Math.Abs(amount - receipt.ClaimCuryTranAmtWithTaxes.Value),
						PaidWith = receipt.PaidWith,
						ReferenceID = receipt.EmployeeID,
						ReferenceName = employeeBAccount.AcctName,
						DocDate = receipt.ExpenseDate,
						CardNumber = card.CardNumber,
						TranDesc = receipt.TranDesc
					};

					if (IsAlreadyMatched(graph, BatchModule.EP, EPExpenseClaimDetails.DocType, matchRow.RefNbr, bankTran, matchLink)
						|| !IsCardNumberMatch(bankTran.CardNumber, matchRow.CardNumber))
						continue;

					matchRow.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, bankTran, matchRow, settings);

					bool isMatchedToCurrent = (matchLink != null && matchLink.TranID.HasValue);

					if (isMatchedToCurrent == false && matchRow.MatchRelevance < relevanceTreshold)
						continue;

					if (bestMatchesNumber > 0)
					{
						for (int i = 0; i < bestMatchesNumber; i++)
						{
							if (bestMatches[i] == null || bestMatches[i].MatchRelevance < matchRow.MatchRelevance)
							{
								for (int j = bestMatchesNumber - 1; j > i; j--)
								{
									bestMatches[j] = bestMatches[j - 1];
								}
								bestMatches[i] = matchRow;
								break;
							}
						}
					}

					result.Add(matchRow);
				}
			}

			return result;
		}

		public static string ExtractCardNumber(string cardNumberRaw)
		{
			if (String.IsNullOrEmpty(cardNumberRaw))
				return cardNumberRaw;

			int startPos = 0;
			int endPos = -1;

			for (int i = cardNumberRaw.Length - 1; i >= 0; i--)
			{
				if (endPos == -1 && Char.IsDigit(cardNumberRaw[i]))
				{
					endPos = i;

					continue;
				}

				if (endPos != -1 && !Char.IsDigit(cardNumberRaw[i]))
				{
					startPos = i + 1;
					break;
				}
			}

			return endPos == -1
				? string.Empty
				: cardNumberRaw.Substring(startPos, endPos - startPos + 1);
		}

		public static bool IsCardNumberMatch(string bankTranCardNumberRaw, string receiptCardNumberRaw)
		{
			string bankTranCardNumber = ExtractCardNumber(bankTranCardNumberRaw);
			string receiptCardNumber = ExtractCardNumber(receiptCardNumberRaw);

			if (String.IsNullOrEmpty(bankTranCardNumber))
				return true;

			if (String.IsNullOrEmpty(receiptCardNumber))
				return false;

			return bankTranCardNumber.Length > receiptCardNumber.Length
				? bankTranCardNumber.Contains(receiptCardNumber)
				: receiptCardNumber.Contains(bankTranCardNumber);
		}


		protected static bool IsAlreadyInAdjustment(PXGraph graph, Light.CABankTranAdjustment aAdjust, String aTranDocType, String aTranRefNbr, String aTranModule)
		{
			if (aAdjust != null)
				return IsAlreadyInAdjustment(graph, aAdjust.TranID, aAdjust.AdjdRefNbr, aAdjust.AdjdDocType, aAdjust.AdjdModule, aTranDocType, aTranRefNbr, aTranModule);
			else return IsAlreadyInAdjustment(graph, null, null, null, null, aTranDocType, aTranRefNbr, aTranModule);
		}
		protected static bool IsAlreadyInAdjustment(PXGraph graph, int? tranID, String adjdRefNbr, string adjdDocType, String adjdModule, String aTranDocType, String aTranRefNbr, String aTranModule)
		{
			PXCache cache = graph.Caches[typeof(CABankTranAdjustment)];
			bool isInAdjustment = adjdRefNbr != null;
			if (!isInAdjustment)
			{
				foreach (CABankTranAdjustment adj in cache.Inserted)
				{
					if (adj.AdjdDocType == aTranDocType && adj.AdjdRefNbr == aTranRefNbr && adj.AdjdModule == aTranModule)
					{
						isInAdjustment = true;
						break;
					}
				}
			}
			else
			{
				foreach (CABankTranAdjustment adj in cache.Deleted)
				{
					if (adj.TranID == tranID && adj.AdjdDocType == adjdDocType && adj.AdjdRefNbr == adjdRefNbr && adj.AdjdModule == adjdModule)
					{
						isInAdjustment = false;
						break;
					}
				}
			}
			return isInAdjustment;
		}

		public static bool IsAlreadyMatched(PXGraph graph, CABankTranInvoiceMatch aMatch, CABankTran aTran, CABankTranMatch aTranMatch)
		{
			return IsAlreadyMatched(graph, aMatch.OrigModule, aMatch.OrigTranType, aMatch.OrigRefNbr, aTran, aTranMatch);
		}

		public static bool IsAlreadyMatched(PXGraph graph, string module, string docType, string refNbr, CABankTran aTran, CABankTranMatch aTranMatch)
		{
			PXCache cache = graph.Caches[typeof(CABankTranMatch)];
			bool isMatched = aTranMatch.TranID != null;
			bool mathedToCurrent = aTranMatch.TranID == aTran.TranID;
			if (isMatched)
			{
				switch (cache.GetStatus(aTranMatch))
				{
					case PXEntryStatus.Deleted:
					case PXEntryStatus.InsertedDeleted:
						isMatched = false;
						mathedToCurrent = false;
						break;
					default:
						CABankTranMatch cached = (CABankTranMatch)cache.Locate(aTranMatch);
						if (cached != null && !cache.ObjectsEqual<CABankTranMatch.tranType,
							CABankTranMatch.docModule, CABankTranMatch.docType, CABankTranMatch.docRefNbr>(cached, aTranMatch))
						{
							isMatched = false;
							mathedToCurrent = false;
						}

						break;
				}
			}
			if (!isMatched)
			{
				foreach (CABankTranMatch iMatch in cache.Inserted)
				{
					if (iMatch.TranType == aTran.TranType &&
						iMatch.DocModule == module &&
						iMatch.DocType == docType &&
						iMatch.DocRefNbr == refNbr)
					{
						if (iMatch.TranID != aTran.TranID)
						{
							isMatched = true;
							mathedToCurrent = iMatch.TranID == aTran.TranID;
							break;
						}
					}
				}
			}
			if (!isMatched)
			{
				foreach (CABankTranMatch iMatch in cache.Updated)
				{
					if (iMatch.TranType == aTran.TranType &&
						iMatch.DocModule == module &&
						iMatch.DocType == docType &&
						iMatch.DocRefNbr == refNbr)
					{
						if (iMatch.TranID != aTran.TranID)
						{
							isMatched = true;
							mathedToCurrent = iMatch.TranID == aTran.TranID;
							break;
						}
					}
				}
			}

			return isMatched && !mathedToCurrent;
		}

		protected virtual void ClearCachedMatches()
		{
			if (this.matchingInvoices != null) this.matchingInvoices.Clear();
			if (this.matchingTrans != null) this.matchingTrans.Clear();
			if (this.matchingExpenseReceiptDetails != null) this.matchingExpenseReceiptDetails.Clear();
		}

		#endregion
		#region Internal Variables
		protected Dictionary<Object, List<CABankTranInvoiceMatch>> matchingInvoices;
		protected Dictionary<Object, List<PXResult<CATranExt, Light.BAccount>>> matchingTrans;
		protected Dictionary<Object, List<CABankTranExpenseDetailMatch>> matchingExpenseReceiptDetails;

		public const decimal RelevanceTreshold = 0.2m;
		#endregion
		#region CABankTran Events
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(CA.CABankTran.matchReason.AutoMatch, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CABankTran.matchReason> e) { }

		protected virtual void _(Events.FieldDefaulting<CABankTran.lastAutoMatchDate> e)
		{
			e.NewValue = PXTimeZoneInfo.Now;
		}

		protected virtual void CABankTran_DocumentMatched_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row == null) return;
			if (row.DocumentMatched == true)
			{
				CABankTranMatch match = TranMatch.SelectSingle(row.TranID);
				if (match != null && !string.IsNullOrEmpty(match.DocRefNbr) && !CABankTransactionsMaint.IsMatchedToExpenseReceipt(match))
				{
					CABankTran.Cache.SetValue<CABankTran.origModule>(row, match.DocModule);
					if (row.PayeeBAccountIDCopy == null)
					{
						CABankTran.Cache.SetValue<CABankTran.payeeBAccountIDCopy>(row, match.ReferenceID);
						object refNbr = row.PayeeBAccountIDCopy;
						CABankTran.Cache.RaiseFieldUpdating<CABankTran.payeeBAccountIDCopy>(row, ref refNbr);
						CABankTran.Cache.RaiseFieldUpdated<CABankTran.payeeBAccountIDCopy>(row, null);
					}
					else
					{
						CABankTran.Cache.SetDefaultExt<CABankTran.payeeLocationID>(row);
						CABankTran.Cache.SetDefaultExt<CABankTran.paymentMethodID>(row);
						CABankTran.Cache.SetDefaultExt<CABankTran.pMInstanceID>(row);
					}
				}

				CABankTran.Cache.SetDefaultExt<CABankTran.matchReason>(row);
				CABankTran.Cache.SetDefaultExt<CABankTran.lastAutoMatchDate>(row);
			}
		}

		protected virtual void CABankTran_PayeeBAccountIDCopy_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row == null) return;

			bool needUnmatch = row.PayeeBAccountIDCopy == null && row.DocumentMatched == true && row.MatchedToInvoice == true;
			if (!needUnmatch && row.PayeeBAccountIDCopy != null && e.OldValue != null)
			{
				int? oldAccountID = (int?)e.OldValue;
				BAccount oldAccount = BAccount.PK.Find(this, oldAccountID);
				if (oldAccount?.ParentBAccountID != row.PayeeBAccountIDCopy)
				{
					needUnmatch = true;
					foreach (CABankTranMatch match in TranMatch.Select(row.TranID))
					{
						if (match.ReferenceID == row.PayeeBAccountIDCopy)
						{
							needUnmatch = false;
							break;
						}
					}
				}
			}

			if (needUnmatch)
			{
				CABankTran.Cache.SetValue<CABankTran.documentMatched>(row, false);
				CABankTran.Cache.SetValue<CABankTran.matchedToExisting>(row, null);
				CABankTran.Cache.SetValue<CABankTran.matchedToInvoice>(row, null);
				CABankTran.Cache.SetValue<CABankTran.matchedToExpenseReceipt>(row, null);
				CABankTran.Cache.SetValue<CABankTran.origModule>(row, null);
			}

			sender.SetDefaultExt<CABankTran.payeeLocationID>(e.Row);
			sender.SetDefaultExt<CABankTran.paymentMethodID>(e.Row);
			sender.SetDefaultExt<CABankTran.pMInstanceID>(e.Row);
			sender.SetDefaultExt<CABankTran.entryTypeID>(e.Row);
		}

		protected virtual string GetUnrecognizedReceiptDefaultEntryType(CABankTran row)
		{
			string entryTypeID = CASetup.Current.UnknownPaymentEntryTypeID;
			CAEntryType entryType = PXSelectJoin<
				CAEntryType,
				InnerJoin<CashAccountETDetail,
					On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
				Where<CashAccountETDetail.accountID, Equal<Required<CABankTran.cashAccountID>>,
					And<CAEntryType.module, Equal<GL.BatchModule.moduleCA>,
					And<CAEntryType.drCr, Equal<Required<CABankTran.drCr>>,
					And<CAEntryType.entryTypeId, Equal<Required<CAEntryType.entryTypeId>>>>>>>
				.Select(this, row.CashAccountID, row.DrCr, entryTypeID);

			return (entryType != null) ? entryTypeID : null;
		}

		protected virtual string GetDefaultCashAccountEntryType(CABankTran row)
		{
			CAEntryType entryType = PXSelectJoin<CAEntryType,
									   InnerJoin<CashAccountETDetail,
											  On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
										   Where<CashAccountETDetail.accountID, Equal<Required<CABankTran.cashAccountID>>,
											 And<CashAccountETDetail.isDefault, Equal<True>,
											 And<CAEntryType.module, Equal<GL.BatchModule.moduleCA>,
											 And<CAEntryType.drCr, Equal<Required<CABankTran.drCr>>>>>>>
									.Select(this, row.CashAccountID, row.DrCr);

			return entryType?.EntryTypeId;
		}
		
		#endregion
		#region Create Document
		protected virtual void CreateDocument(CABankTran row, CashAccount cashAccount)
		{
			if (row.CreateDocument != true)
			{
				return;
			}

			bool isInvoiceFound = false;

			try
			{
				isInvoiceFound = ApplyInvoiceInfo(CABankTran.Cache, row);
			}
			catch
			{
				foreach (CABankTranAdjustment adj in Adjustments.Select(row.TranID))
				{
					Adjustments.Delete(adj);
				}
			}

			bool clearingAccount = cashAccount?.ClearingAccount == true;
			CABankTranDetail newSplit = null;

			if (row.CreateDocument == true && isInvoiceFound == false && !clearingAccount)
			{
				if(!AttemptApplyRules(row))
				{
					CABankTran.Current = row;
					row.OrigModule = GetDefaultOrigModule(row, cashAccount);
					row.EntryTypeID = GetDefaultEntryTypeId(row);
				}

				row.UserDesc = CutOffTranDescTo256(row.TranDesc);

				TryToSetDefaultTaxInfo(row, cashAccount);
				row = CABankTran.Update(row);

				if (!string.IsNullOrEmpty(row.EntryTypeID))
				{
					var tranAnt = (row.DrCr == DrCr.Debit ? decimal.One : decimal.MinusOne) * row.CuryTranAmt;

					newSplit = new CABankTranDetail
					{
						Qty = 1.0m,
						CuryUnitPrice = tranAnt,
						CuryTranAmt = tranAnt,
						TranDesc = row.UserDesc,
					};
					newSplit = TranSplit.Insert(newSplit);
				}

			}

			bool isCADetailValid = row.OrigModule == GL.BatchModule.CA ? newSplit?.AccountID != null && newSplit?.SubID != null : true;
			bool isMatched = row.CreateDocument == true && !string.IsNullOrEmpty(row.EntryTypeID) && row.CuryUnappliedBalCA == 0m && isCADetailValid;
			row.DocumentMatched = isMatched;
			CABankTran.Cache.Update(row);
			CABankTran.Cache.SetDefaultExt<CABankTran.matchingPaymentDate>(row);
			CABankTran.Cache.SetDefaultExt<CABankTran.matchingfinPeriodID>(row);
		}

		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R2)]
		private string CutOffTranDescTo256(string description)
		{
			return description?.Length > 256 ? description.Substring(0, 255) : description;
		}

		private void TryToSetDefaultTaxInfo(CABankTran row, CashAccount cashAccount)
		{
			if (!string.IsNullOrEmpty(row.EntryTypeID))
			{
				CashAccountETDetail entryType = PXSelect<CashAccountETDetail, Where<CashAccountETDetail.accountID, Equal<Required<CABankTran.cashAccountID>>,
							And<CashAccountETDetail.entryTypeID, Equal<Required<CABankTran.entryTypeID>>>>>.Select(this, cashAccount.CashAccountID, row.EntryTypeID);

				row.TaxZoneID = entryType.TaxZoneID;
				row.TaxCalcMode = entryType.TaxCalcMode;
			}
		}

		private string GetDefaultEntryTypeId(CABankTran row)
		{
			string entryTypeID = null;

			{
				if (row.OrigModule == GL.BatchModule.CA)
				{
					string defaultEntryType = GetUnrecognizedReceiptDefaultEntryType(row);

					if (string.IsNullOrEmpty(defaultEntryType))
					{
						defaultEntryType = GetDefaultCashAccountEntryType(row);
					}

					entryTypeID = defaultEntryType;
				}
			}

			return entryTypeID;
		}

		private string GetDefaultOrigModule(CABankTran row, CashAccount cashAccount)
		{
			if (cashAccount?.ClearingAccount == true)
			{
				return GL.BatchModule.AR;
			}
			else if (!String.IsNullOrEmpty(row.InvoiceInfo))
			{
				if (row.DrCr == CADrCr.CACredit)
					return GL.BatchModule.AP;
				else if (row.DrCr == CADrCr.CADebit)
					return GL.BatchModule.AR;
				else
					throw new NotImplementedException();
			}
			else
			{
				return GL.BatchModule.CA;
			}
		}


		protected virtual object FindInvoiceByInvoiceInfo(CABankTran row, out string module)
		{
			return StatementsMatchingProto.FindInvoiceByInvoiceInfo(this, row, out module);
		}

		protected virtual bool ApplyInvoiceInfo(PXCache sender, CABankTran row)
		{
			if (row.CreateDocument == true && row.InvoiceInfo != null)
			{
				string Module;
				object linkedInvoice = FindInvoiceByInvoiceInfo(row, out Module);
				if (linkedInvoice != null)
				{
					int? payeeBAccountID;
					string paymentMethodID;
					int? pmInstanceID = null;
					string RefNbr;

					switch (Module)
					{
						case GL.BatchModule.AP:
							APInvoice APinvoice = linkedInvoice as APInvoice;
							if (APinvoice == null)
							{
								throw new PXSetPropertyException(Messages.WrongInvoiceType);
							}
							payeeBAccountID = APinvoice.VendorID;
							paymentMethodID = APinvoice.PayTypeID;
							RefNbr = APinvoice.RefNbr;
							break;
						case GL.BatchModule.AR:
							ARInvoice ARinvoice = linkedInvoice as ARInvoice;
							if (ARinvoice == null)
							{
								throw new PXSetPropertyException(Messages.WrongInvoiceType);
							}
							payeeBAccountID = ARinvoice.CustomerID;
							paymentMethodID = ARinvoice.PaymentMethodID;
							pmInstanceID = ARinvoice.PMInstanceID;
							RefNbr = ARinvoice.RefNbr;
							break;
						default:
							throw new PXSetPropertyException(Messages.UnknownModule);
					}
					sender.SetValueExt<CABankTran.origModule>(row, Module);
					sender.SetValue<CABankTran.payeeBAccountID>(row, payeeBAccountID);
					object refNbr = row.PayeeBAccountID;
					sender.RaiseFieldUpdating<CABankTran.payeeBAccountID>(row, ref refNbr);
					sender.RaiseFieldUpdated<CABankTran.payeeBAccountID>(row, null);
					if (paymentMethodID != null)
					{
						try
						{
							sender.SetValueExt<CABankTran.paymentMethodID>(row, paymentMethodID);
							sender.SetValueExt<CABankTran.pMInstanceID>(row, pmInstanceID);
						}
						catch (PXSetPropertyException)
						{
						}
					}

					try
					{
						CABankTranAdjustment adj = new CABankTranAdjustment()
						{
							TranID = row.TranID
						};

						adj = Adjustments.Insert(adj);

						adj.AdjdDocType = APInvoiceType.Invoice;
						adj.AdjdRefNbr = RefNbr;

						Adjustments.Update(adj);
					}
					catch
					{
						throw new PXSetPropertyException(Messages.CouldNotAddApplication, row.InvoiceInfo);
					}

					return true;
				}
				else
				{
					sender.SetValue<CABankTran.invoiceNotFound>(row, true);
				}
			}
			return false;
		}

		protected virtual bool AttemptApplyRules(CABankTran transaction)
		{
			if (transaction == null || transaction.RuleID != null) return false;

			foreach (CABankTranRule rule in Rules.Select())
			{
				if (CheckRuleMatches(transaction, rule))
				{
					try
					{
						ApplyRule(transaction, rule);
						CABankTran.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetError<CABankTran.ruleID>(CABankTran.Cache, transaction, null);

						return true;
					}
					catch (PXException)
					{
						CABankTran.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetWarning<CABankTran.ruleID>(CABankTran.Cache, transaction, Messages.BankRuleFailedToApply);
						CABankTran.Cache.SetDefaultExt<CABankTran.ruleID>(transaction);
						CABankTran.Cache.SetDefaultExt<CABankTran.origModule>(transaction);
						CABankTran.Cache.SetDefaultExt<CABankTran.curyTotalAmt>(transaction);
					}
				}
			}

			return false;
		}

		protected virtual bool CheckRuleMatches(CABankTran transaction, CABankTranRule rule) => StatementsMatchingProto.CheckRuleMatches(transaction, rule);

		protected virtual void ApplyRule(CABankTran transaction, CABankTranRule rule)
		{
			if (rule.Action == RuleAction.CreateDocument)
			{
				transaction.OrigModule = rule.DocumentModule;
				transaction.EntryTypeID = rule.DocumentEntryTypeID;
			}
			else if (rule.Action == RuleAction.HideTransaction)
			{
				transaction.CreateDocument = false;
				transaction.DocumentMatched = false;
				transaction.Hidden = true;
				transaction.Processed = true;
			}

			transaction.RuleID = rule.RuleID;
		}

		protected virtual void CABankTran_OrigModule_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null && row.CreateDocument == true)
			{
				CashAccount cashaccount = cashAccount.Select(row.CashAccountID);
				bool clearingAccount = cashaccount?.ClearingAccount == true;
				if (clearingAccount)
				{
					e.NewValue = GL.BatchModule.AR;
				}
				else if (!String.IsNullOrEmpty(row.InvoiceInfo))
				{
					if (row.DrCr == CADrCr.CACredit)
						e.NewValue = GL.BatchModule.AP;
					else if (row.DrCr == CADrCr.CADebit)
						e.NewValue = GL.BatchModule.AR;
				}
				else
				{
					e.NewValue = GL.BatchModule.CA;
				}
			}
			else
			{
				e.NewValue = null;
			}
			e.Cancel = true;
		}
		#endregion
		#region CABankTranAdjustment Events

		protected virtual void CABankTranAdjustment_AdjdRefNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			var current = CABankTranSelection.SelectSingle(adj.TranID);

			adj.AdjgDocDate = current?.TranDate;
			adj.Released = false;
			adj.CuryDocBal = null;
			adj.CuryDiscBal = null;
			adj.CuryWhTaxBal = null;

			if (String.IsNullOrEmpty(adj.AdjdRefNbr))
			{
				sender.SetValueExt<CABankTranAdjustment.curyAdjgAmt>(adj, null);
				sender.SetValueExt<CABankTranAdjustment.curyAdjgDiscAmt>(adj, null);
				sender.SetValueExt<CABankTranAdjustment.curyAdjgWhTaxAmt>(adj, null);
				sender.SetValueExt<CABankTranAdjustment.curyAdjgWOAmt>(adj, null);
			}
			if (adj.CuryAdjgAmt == null || adj.CuryAdjgAmt == 0.0m)
			{
				adj.CuryAdjgAmt = null;

				if (current.OrigModule == GL.BatchModule.AP)
				{
					PopulateAdjustmentFieldsAP(current, adj);
					sender.SetDefaultExt<CABankTranAdjustment.adjdTranPeriodID>(e.Row);
				}
				else if (current.OrigModule == GL.BatchModule.AR)
				{
					PopulateAdjustmentFieldsAR(current, adj);
					sender.SetDefaultExt<CABankTranAdjustment.adjdTranPeriodID>(e.Row);
				}
			}
			else
			{
				sender.SetValueExt<CABankTranAdjustment.curyAdjgAmt>(adj, adj.CuryAdjgAmt);
			}
			if (adj.CuryAdjgDiscAmt != null && adj.CuryAdjgDiscAmt != 0.0m)
			{
				sender.SetValueExt<CABankTranAdjustment.curyAdjgDiscAmt>(adj, adj.CuryAdjgDiscAmt);
			}
			if (adj.CuryAdjgWhTaxAmt != null && adj.CuryAdjgWhTaxAmt != 0.0m)
			{
				sender.SetValueExt<CABankTranAdjustment.curyAdjgWhTaxAmt>(adj, adj.CuryAdjgWhTaxAmt);
			}
		}

		protected virtual void PopulateAdjustmentFieldsAR(CABankTran tran, CABankTranAdjustment adj)
		{
			GetExtension<StatementApplicationBalancesProto>().PopulateAdjustmentFieldsAR(tran, adj);
		}

		protected virtual void PopulateAdjustmentFieldsAP(CABankTran tran, CABankTranAdjustment adj)
		{
			GetExtension<StatementApplicationBalancesProto>().PopulateAdjustmentFieldsAP(tran, adj);
		}

		protected virtual void CABankTranAdjustment_AdjdRefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj == null)
				return;

			foreach (CABankTranAdjustment other in Adjustments.Select(adj.TranID))
			{
				if (object.ReferenceEquals(adj, other))
					continue;

				if (other.AdjdDocType == adj.AdjdDocType && other.AdjdRefNbr == (string)e.NewValue && other.AdjdModule == adj.AdjdModule)
					throw new PXSetPropertyException<CABankTranAdjustment.adjdRefNbr>(Messages.PaymentAlreadyAppliedToThisDocument);
			}
		}

		protected virtual void CABankTranAdjustment_AdjdCuryRate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal)e.NewValue <= 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GT, ((int)0).ToString());
			}
		}

		protected virtual void CABankTranAdjustment_AdjdCuryRate_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;

			CurrencyInfo pay_info = PXSelect<
				CurrencyInfo,
				Where<CurrencyInfo.curyInfoID, Equal<Current<CABankTranAdjustment.adjgCuryInfoID>>>>
				.SelectSingleBound(this, new object[] { e.Row });
			CurrencyInfo vouch_info = PXSelect<
				CurrencyInfo,
				Where<CurrencyInfo.curyInfoID, Equal<Current<CABankTranAdjustment.adjdCuryInfoID>>>>
				.SelectSingleBound(this, new object[] { e.Row });

			decimal payment_docbal = (decimal)adj.CuryAdjgAmt;
			decimal discount_docbal = (decimal)adj.CuryAdjgDiscAmt;
			decimal invoice_amount;

			if (string.Equals(pay_info.CuryID, vouch_info.CuryID) && adj.AdjdCuryRate != 1m)
			{
				adj.AdjdCuryRate = 1m;
				var current = CABankTranSelection.SelectSingle(adj.TranID);
				vouch_info.SetCuryEffDate(currencyinfo.Cache, current?.TranDate);
			}
			else if (string.Equals(vouch_info.CuryID, vouch_info.BaseCuryID))
			{
				adj.AdjdCuryRate = pay_info.CuryMultDiv == "M" ? 1 / pay_info.CuryRate : pay_info.CuryRate;
			}
			else
			{
				vouch_info.CuryRate = adj.AdjdCuryRate;
				vouch_info.RecipRate = Math.Round(1m / (decimal)adj.AdjdCuryRate, 8, MidpointRounding.AwayFromZero);
				vouch_info.CuryMultDiv = "M";
				PXCurrencyAttribute.CuryConvBase(sender, vouch_info, (decimal)adj.CuryAdjdAmt, out payment_docbal);
				PXCurrencyAttribute.CuryConvBase(sender, vouch_info, (decimal)adj.CuryAdjdDiscAmt, out discount_docbal);
				PXCurrencyAttribute.CuryConvBase(sender, vouch_info, (decimal)adj.CuryAdjdAmt + (decimal)adj.CuryAdjdDiscAmt, out invoice_amount);

				vouch_info.CuryRate = Math.Round((decimal)adj.AdjdCuryRate * (pay_info.CuryMultDiv == "M" ? (decimal)pay_info.CuryRate : 1m / (decimal)pay_info.CuryRate), 8, MidpointRounding.AwayFromZero);
				vouch_info.RecipRate = Math.Round((pay_info.CuryMultDiv == "M" ? 1m / (decimal)pay_info.CuryRate : (decimal)pay_info.CuryRate) / (decimal)adj.AdjdCuryRate, 8, MidpointRounding.AwayFromZero);

				if (payment_docbal + discount_docbal != invoice_amount)
					discount_docbal += invoice_amount - discount_docbal - payment_docbal;
			}

			Caches[typeof(CurrencyInfo)].MarkUpdated(vouch_info);

			if (payment_docbal != (decimal)adj.CuryAdjgAmt)
				sender.SetValue<CABankTranAdjustment.curyAdjgAmt>(e.Row, payment_docbal);

			if (discount_docbal != (decimal)adj.CuryAdjgDiscAmt)
				sender.SetValue<CABankTranAdjustment.curyAdjgDiscAmt>(e.Row, discount_docbal);

			UpdateBalance((CABankTranAdjustment)e.Row, true);
		}

		protected virtual void CABankTranAdjustment_CuryAdjgAmt_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj != null && adj.AdjdRefNbr != null)
			{
				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					UpdateBalance((CABankTranAdjustment)e.Row, false);
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, ((int)0).ToString());
				}
			}
			else
			{
				e.NewValue = 0m;
			}
		}

		protected virtual void CABankTranAdjustment_CuryAdjgAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.OldValue != null && ((CABankTranAdjustment)e.Row).CuryDocBal == 0m && ((CABankTranAdjustment)e.Row).CuryAdjgAmt < (decimal)e.OldValue)
			{
				((CABankTranAdjustment)e.Row).CuryAdjgDiscAmt = 0m;
			}
			UpdateBalance((CABankTranAdjustment)e.Row, true);
		}

		protected virtual void CABankTranAdjustment_CuryAdjgDiscAmt_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj != null && adj.AdjdRefNbr != null)
			{
				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					UpdateBalance((CABankTranAdjustment)e.Row, false);
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, ((int)0).ToString());
				}

				if ((decimal)adj.CuryDiscBal + (decimal)adj.CuryAdjgDiscAmt - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(AP.Messages.Entry_LE, ((decimal)adj.CuryDiscBal + (decimal)adj.CuryAdjgDiscAmt).ToString());
				}

				if (adj.CuryAdjgAmt != null && (sender.GetValuePending<CABankTranAdjustment.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (Decimal?)sender.GetValuePending<CABankTranAdjustment.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgDiscAmt - (decimal)e.NewValue < 0)
					{
						throw new PXSetPropertyException(AP.Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgDiscAmt).ToString());
					}
				}
			}
			else
			{
				e.NewValue = 0m;
			}
		}

		protected virtual void CABankTranAdjustment_CuryAdjgDiscAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			UpdateBalance((CABankTranAdjustment)e.Row, true);
		}

		protected virtual void CABankTranAdjustment_CuryAdjgWhTaxAmt_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj != null && adj.AdjdRefNbr != null)
			{
				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					UpdateBalance((CABankTranAdjustment)e.Row, false);
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, ((int)0).ToString());
				}

				if ((decimal)adj.CuryWhTaxBal + (decimal)adj.CuryAdjgWhTaxAmt - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(AP.Messages.Entry_LE, ((decimal)adj.CuryWhTaxBal + (decimal)adj.CuryAdjgWhTaxAmt).ToString());
				}

				if (adj.CuryAdjgAmt != null && (sender.GetValuePending<CABankTranAdjustment.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (Decimal?)sender.GetValuePending<CABankTranAdjustment.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWhTaxAmt - (decimal)e.NewValue < 0)
					{
						throw new PXSetPropertyException(AP.Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWhTaxAmt).ToString());
					}
				}
			}
			else
			{
				e.NewValue = 0m;
			}
		}

		protected virtual void CABankTranAdjustment_WriteOffReasonCode_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj?.AdjdRefNbr != null)
			{
				ReasonCode reasonCode = ReasonCode.PK.Find(this, (string)e.NewValue);

				if (reasonCode?.Usage == ReasonCodeUsages.BalanceWriteOff && ((decimal)adj.CuryAdjgWOAmt) < 0m)
				{
					throw new PXSetPropertyException(Messages.ForNegativeBalanceWriteOffAmountCreditWriteOffReasonCodeShouldBeUsed);
				}

				if (reasonCode?.Usage == ReasonCodeUsages.CreditWriteOff && ((decimal)adj.CuryAdjgWOAmt) > 0m)
				{
					throw new PXSetPropertyException(Messages.ForPositiveBalanceWriteOffAmountCreditWriteOffReasonCodeShouldBeUsed);
				}
			}
		}


		protected virtual void CABankTranAdjustment_CuryAdjgWOAmt_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj?.AdjdRefNbr != null)
			{
				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					UpdateBalance(adj, false);
				}

				if ((adj.CuryWhTaxBal ?? 0m) + (adj.CuryAdjgWOAmt ?? 0m) - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(AR.Messages.ApplicationWOLimitExceeded, ((adj.CuryWhTaxBal ?? 0m) + (adj.CuryAdjgWOAmt ?? 0m)).ToString());
				}
			}
			else
			{
				e.NewValue = 0m;
			}
		}

		protected virtual void CABankTranAdjustment_CuryAdjgWOAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			UpdateBalance((CABankTranAdjustment)e.Row, false);

			string writeOffCodeFieldName = typeof(CABankTranAdjustment.writeOffReasonCode).Name;
			var currentWriteOffCode = sender.GetValue(sender.Current, writeOffCodeFieldName);

			try
			{
				sender.RaiseFieldVerifying(writeOffCodeFieldName, sender.Current, ref currentWriteOffCode);
			}
			catch (PXSetPropertyException ex)
			{
				sender.RaiseExceptionHandling(writeOffCodeFieldName, sender.Current, currentWriteOffCode, ex);
			}
		}

		protected virtual void CABankTranAdjustment_CuryDocBal_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.Row != null && ((CABankTranAdjustment)e.Row).AdjdCuryInfoID != null && ((CABankTranAdjustment)e.Row).CuryDocBal == null && sender.GetStatus(e.Row) != PXEntryStatus.Deleted)
			{
				UpdateBalance((CABankTranAdjustment)e.Row, false);
			}
			if (e.Row != null)
			{
				e.NewValue = ((CABankTranAdjustment)e.Row).CuryDocBal;
			}
			e.Cancel = true;
		}

		protected virtual void CABankTranAdjustment_CuryDiscBal_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.Row != null && ((CABankTranAdjustment)e.Row).AdjdCuryInfoID != null && ((CABankTranAdjustment)e.Row).CuryDiscBal == null && sender.GetStatus(e.Row) != PXEntryStatus.Deleted)
			{
				UpdateBalance((CABankTranAdjustment)e.Row, false);
			}
			if (e.Row != null)
			{
				e.NewValue = ((CABankTranAdjustment)e.Row).CuryDiscBal;
			}
			e.Cancel = true;
		}

		protected virtual void CABankTranAdjustment_CuryWhTaxBal_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.Row != null && ((CABankTranAdjustment)e.Row).AdjdCuryInfoID != null && ((CABankTranAdjustment)e.Row).CuryWhTaxBal == null && sender.GetStatus(e.Row) != PXEntryStatus.Deleted)
			{
				UpdateBalance((CABankTranAdjustment)e.Row, false);
			}
			if (e.Row != null)
			{
				e.NewValue = ((CABankTranAdjustment)e.Row).CuryWhTaxBal;
			}
			e.Cancel = true;
		}

		protected virtual void CABankTranAdjustment_AdjdDocType_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTranAdjustment row = e.Row as CABankTranAdjustment;
			if (row == null) return;
			if (row.AdjdDocType != (string)e.OldValue)
			{
				sender.SetValueExt<CABankTranAdjustment.adjdRefNbr>(row, null);
				sender.SetValueExt<CABankTranAdjustment.curyAdjgAmt>(row, Decimal.Zero);
			}

		}

		protected virtual void CABankTranAdjustment_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			CABankTranAdjustment row = (CABankTranAdjustment)e.Row;
			if (row != null)
			{
				foreach (CABankTranAdjustment adjustmentRecord in this.Adjustments.Select())
				{
					if (row.AdjdRefNbr != null && adjustmentRecord.AdjdRefNbr == row.AdjdRefNbr && adjustmentRecord.AdjdDocType == row.AdjdDocType)
					{
						PXEntryStatus status = this.Adjustments.Cache.GetStatus(adjustmentRecord);
						if (!(status == PXEntryStatus.InsertedDeleted || status == PXEntryStatus.Deleted))
						{
							sender.RaiseExceptionHandling<CABankTranAdjustment.adjdRefNbr>(e.Row, null, new PXException(Messages.DuplicatedKeyForRow));
							e.Cancel = true;
							break;
						}
					}
				}
			}
		}

		protected virtual void CABankTranAdjustment_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			CABankTranAdjustment row = (CABankTranAdjustment)e.Row;
			if (row == null) return;
			CABankTran det = CABankTranSelection.SelectSingle(row.TranID);

			sender.SetValue<CABankTranAdjustment.adjdModule>(row, det.OrigModule);
			CurrencyInfoAttribute.SetDefaults<CABankTran.curyInfoID>(CABankTran.Cache, det);
			CurrencyInfoAttribute.SetDefaults<CABankTranAdjustment.adjgCuryInfoID>(sender, row);
		}

		public virtual void UpdateBalance(CABankTranAdjustment adj, bool isCalcRGOL)
		{
			if (adj.AdjdDocType != null && adj.AdjdRefNbr != null)
			{
				CABankTran current = CABankTranSelection.SelectSingle(adj.TranID);
				GetExtension<StatementApplicationBalancesProto>().UpdateBalance(current, adj, isCalcRGOL);
			}
		}
		#endregion
		#region CABankTranDetails
		#region CABankTranDetail Events
		protected virtual void CABankTranDetail_AccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			CABankTran tran = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, split.BankTranID);

			if (tran == null || tran.EntryTypeID == null || split == null)
				return;

			e.NewValue = GetDefaultAccountValues(this, tran.CashAccountID, tran.EntryTypeID).AccountID;
			e.Cancel = e.NewValue != null;
		}

		protected virtual void CABankTranDetail_AccountID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			CATranDetailHelper.OnAccountIdFieldUpdatedEvent(cache, e);

			CABankTranDetail row = (CABankTranDetail)e.Row;

			if (row.InventoryID == null)
				cache.SetDefaultExt<CABankTranDetail.taxCategoryID>(e.Row);
		}

		protected virtual void CABankTranDetail_BranchID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			CABankTran adj = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, split.BankTranID);

			if (adj == null || adj.EntryTypeID == null || split == null)
				return;

			e.NewValue = GetDefaultAccountValues(this, adj.CashAccountID, adj.EntryTypeID).BranchID;
			e.Cancel = e.NewValue != null;
		}

		protected virtual void CABankTranDetail_CashAccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CATranDetailHelper.OnCashAccountIdFieldDefaultingEvent(sender, e);
		}

		protected virtual void CABankTranDetail_CashAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CATranDetailHelper.OnCashAccountIdFieldUpdatedEvent(sender, e);
		}


		protected virtual void CABankTranDetail_InventoryId_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			CABankTran adj = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, split.BankTranID);

			if (split != null && split.InventoryID != null)
			{
				InventoryItem invItem = PXSelect<
					InventoryItem,
					Where<InventoryItem.inventoryID, Equal<Required<CABankTranDetail.inventoryID>>>>
					.Select(this, split.InventoryID);

				if (invItem != null && adj != null)
				{
					if (adj.DrCr == CADrCr.CADebit)
					{
						split.AccountID = invItem.SalesAcctID;
						split.SubID = invItem.SalesSubID;
					}
					else
					{
						split.AccountID = invItem.COGSAcctID;
						split.SubID = invItem.COGSSubID;
					}
				}

				sender.SetDefaultExt<CABankTranDetail.taxCategoryID>(split);
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTranDetail, CABankTranDetail.taxCategoryID> e)
		{
			CABankTranDetail row = e.Row;

			if (row == null)
				return;

			if (TaxAttribute.GetTaxCalc<CABankTranDetail.taxCategoryID>(e.Cache, row) != TaxCalc.Calc || row.InventoryID != null)
				return;

			Account account = null;
			if (row.AccountID != null)
			{
				account = Account.PK.Find(this, row.AccountID);
			}

			TaxZone taxZone = Taxzone.Select(CABankTran?.Current?.TaxZoneID);

			if (account?.TaxCategoryID != null)
			{
				e.NewValue = account.TaxCategoryID;
			}
			else if (taxZone != null && !string.IsNullOrEmpty(taxZone.DfltTaxCategoryID))
			{
				e.NewValue = taxZone.DfltTaxCategoryID;
			}
			else
			{
				e.NewValue = row.TaxCategoryID;
			}
		}

		protected virtual void CABankTranDetail_Qty_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			e.NewValue = 1.0m;
		}

		protected virtual void CABankTranDetail_SubID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			CABankTran adj = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, split.BankTranID);

			if (adj == null || adj.EntryTypeID == null || split == null)
				return;

			e.NewValue = GetDefaultAccountValues(this, adj.CashAccountID, adj.EntryTypeID).SubID;
			e.Cancel = e.NewValue != null;
		}

		protected virtual void CABankTranDetail_TranDesc_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;

			CABankTran tran = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, split.BankTranID);

			if (tran != null && tran.EntryTypeID != null)
			{
				CAEntryType entryType = PXSelect<
					CAEntryType,
					Where<CAEntryType.entryTypeId, Equal<Required<CAEntryType.entryTypeId>>>>
					.Select(this, tran.EntryTypeID);

				if (entryType != null)
				{
					e.NewValue = entryType.Descr;
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTran, CABankTaxTran.taxType> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTran, CABankTaxTran.accountID> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTran, CABankTaxTran.subID> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTran, CABankTran.chargeTaxCalcMode> e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.netGrossEntryMode>())
			{
				e.NewValue = TaxCalculationMode.Gross;
			}
			else
			{
				e.NewValue = TaxCalculationMode.TaxSetting;
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTranMatch, CABankTaxTranMatch.taxType> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTranMatch, CABankTaxTranMatch.accountID> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<CABankTaxTranMatch, CABankTaxTranMatch.subID> e)
		{
			CABankTran current = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, e.Row.BankTranID);

			if (e.Row != null && current != null)
			{
				if (current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(e.Cache.Graph, ((CABankTaxTranMatch)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
			}
		}
		#endregion


		protected virtual void CABankTranDetail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			CABankTranDetail newSplit = e.Row as CABankTranDetail;
			CurrencyInfoAttribute.SetDefaults<CABankTranDetail.curyInfoID>(sender, newSplit);
			CATranDetailHelper.VerifyOffsetCashAccount(sender, newSplit, TranSplit.Current?.CashAccountID);
		}

		protected virtual void CABankTranDetail_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			CATranDetailHelper.OnCATranDetailRowUpdatingEvent(sender, e);
			CABankTran tran = PXSelect<CA.CABankTran, Where<CA.CABankTran.tranID, Equal<Required<CABankTranDetail.bankTranID>>>>.Select(this, (e.NewRow as CABankTranDetail).BankTranID);
			CATranDetailHelper.UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(sender, e.Row as ICATranDetail, e.NewRow as ICATranDetail);
			if (CATranDetailHelper.VerifyOffsetCashAccount(sender, e.NewRow as CABankTranDetail, tran?.CashAccountID))
			{
				e.Cancel = true;
			}
		}


		protected virtual void CABankTranDetail_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTranDetail row = (CABankTranDetail)e.Row;

			if (row == null)
				return;


			CABankTran tranRow = PXSelect<CABankTran, Where<CABankTran.tranID, Equal<Required<CABankTranAdjustment.tranID>>>>.Select(this, row.BankTranID);
			var accountFieldState = sender.GetStateExt<CABankTranDetail.accountID>(row) as PXFieldState;

			PXSetPropertyException exception = null;
			PXSetPropertyException isControlAccountException = null;

			var accountIsNotValid = true;
			try
			{
				var account = (Account)PXSelectorAttribute.Select<CABankTranDetail.accountID>(sender, e.Row);
				AccountAttribute.VerifyAccountIsNotControl(account);
			}
			catch (PXSetPropertyException ex)
			{
				accountIsNotValid = false;
				isControlAccountException = ex;
			}

			if (row.AccountID == null)
			{
				exception = new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.RowWarning, PXUIFieldAttribute.GetDisplayName<CABankTranDetail.accountID>(sender));
			}
			else if (row.SubID == null)
			{
				exception = new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.RowWarning, PXUIFieldAttribute.GetDisplayName<CABankTranDetail.subID>(sender));
			}
			else if (accountIsNotValid != true)
			{
				exception = isControlAccountException;
			}

			CABankTran.Cache.RaiseExceptionHandling<CABankTran.createDocument>(tranRow, tranRow.CreateDocument, exception);
			sender.RaiseExceptionHandling<CABankTranDetail.accountID>(row, row.AccountID, exception);
		}

		private CABankTranDetail GetDefaultAccountValues(PXGraph graph, int? cashAccountID, string entryTypeID)
		{
			return CATranDetailHelper.CreateCATransactionDetailWithDefaultAccountValues<CABankTranDetail>(graph, cashAccountID, entryTypeID);
		}

		public virtual void updateAmountPrice(CABankTranDetail oldSplit, CABankTranDetail newSplit)
		{
			CATranDetailHelper.UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(TranSplit.Cache, oldSplit, newSplit);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[CABankTranTaxMatch(typeof(CABankTran), typeof(CABankTax), typeof(CABankTaxTran), typeof(CABankTran.taxCalcMode),
			CuryOrigDocAmt = typeof(CABankTran.curyTranAmt), CuryLineTotal = typeof(CABankTran.curyApplAmtCA))]
		protected virtual void _(Events.CacheAttached<CABankTranDetail.taxCategoryID> e) { }
		#endregion
		#region CurrencyInfo
		protected virtual void CurrencyInfo_CuryID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				CashAccount cacct = cashAccount.Current;

				if (cacct != null && !string.IsNullOrEmpty(cacct.CuryID))
				{
					e.NewValue = cacct.CuryID;
					e.Cancel = true;
				}
			}
		}

		protected virtual void CurrencyInfo_CuryRateTypeID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				CashAccount cacct = cashAccount.Current;
				if (cacct != null && !string.IsNullOrEmpty(cacct.CuryRateTypeID))
				{
					e.NewValue = cacct.CuryRateTypeID;
					e.Cancel = true;
				}
				else
				{
					CMSetup setup = CMSetup.Current;
					CABankTran det = CABankTran.Current;

					if (setup != null && det != null)
					{
						switch (det.OrigModule)
						{
							case GL.BatchModule.CA:
								e.NewValue = setup.CARateTypeDflt;
								break;
							case GL.BatchModule.AP:
								e.NewValue = setup.APRateTypeDflt;
								break;
							case GL.BatchModule.AR:
								e.NewValue = setup.ARRateTypeDflt;
								break;
						}

						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void CurrencyInfo_CuryEffDate_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (CABankTran.Current != null)
			{
				e.NewValue = CABankTran.Current.TranDate;
				e.Cancel = true;
			}
		}
		#endregion
		#region DataProvider
		public virtual PXResultset<CATranExt> SearchForMatchingTransactions(CABankTran aDetail, IMatchSettings aSettings, Pair<DateTime, DateTime> tranDateRange, string curyID)
		{
			return CABankTransactionsRepository.SearchForMatchingTransactions(this, aDetail, aSettings, tranDateRange, curyID);
		}

		public virtual PXResultset<CABatch> SearchForMatchingCABatches(CABankTran aDetail, Pair<DateTime, DateTime> tranDateRange, string curyID, bool allowUnreleased)
		{
			return CABankTransactionsRepository.SearchForMatchingCABatches(this, aDetail, tranDateRange, curyID, allowUnreleased);
		}

		public virtual PXResultset<CABatchDetail> SearchForMatchesInCABatches(string tranType, string batchNbr)
		{
			return CABankTransactionsRepository.SearchForMatchesInCABatches(this, tranType, batchNbr);
		}

		public virtual PXResultset<CABankTranMatch> SearchForTranMatchForCABatch(string batchNbr)
		{
			return CABankTransactionsRepository.SearchForTranMatchForCABatch(this, batchNbr);
		}

		public virtual PXResult<ARInvoice, ARAdjust> FindARInvoiceByInvoiceInfo(CABankTran aRow)
		{
			return CABankTransactionsRepository.FindARInvoiceByInvoiceInfo(this, aRow);
		}

		public virtual PXResult<APInvoice, APAdjust, APPayment> FindAPInvoiceByInvoiceInfo(CABankTran aRow)
		{
			return CABankTransactionsRepository.FindAPInvoiceByInvoiceInfo(this, aRow);
		}
		#endregion
	}
}
