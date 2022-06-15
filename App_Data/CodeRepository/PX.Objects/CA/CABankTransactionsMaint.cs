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
using PX.Objects.CR;
using PX.Objects.EP;
using static PX.Objects.Common.UIState;
using PX.Objects.TX;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Objects.CA.Repositories;

namespace PX.Objects.CA
{
	public partial class CABankTransactionsMaint : PXGraph<CABankTransactionsMaint>, ICABankTransactionsDataProvider
	{
		#region Constructor
		public CABankTransactionsMaint()
		{
			Details.Cache.AllowInsert = false;
			Details.Cache.AllowDelete = false;
			Details.Cache.AllowUpdate = false;

			DetailMatchesCA.Cache.AllowInsert = false;
			DetailMatchesCA.Cache.AllowDelete = false;

			Adjustments.Cache.AllowInsert = false;
			Adjustments.Cache.AllowDelete = false;

			DetailMatchingInvoices.Cache.AllowInsert = false;
			DetailMatchingInvoices.Cache.AllowDelete = false;

			Details.AllowUpdate = false;
			TranSplit.AllowInsert = false;

			APSetupNoMigrationMode.EnsureMigrationModeDisabled(this);
			ARSetupNoMigrationMode.EnsureMigrationModeDisabled(this);

			matchSettingsPanel.StateSelectingEvents += StateSelectingEventsHandler;
			processMatched.StateSelectingEvents += StateSelectingEventsHandler;
			uploadFile.StateSelectingEvents += StateSelectingEventsHandler;
			autoMatch.StateSelectingEvents += StateSelectingEventsHandler;

			FieldDefaulting.AddHandler<CR.BAccountR.type>(SetDefaultBaccountType);

			PXUIFieldAttribute.SetVisible<CABankTranDetail.projectID>(TranSplit.Cache, null, false);
			EnableCreateTab(Details.Cache, null, false);
		}

		private void StateSelectingEventsHandler(PXCache sender, PXFieldSelectingEventArgs e)
		{
			TimeSpan timespan;
			Exception message;
			PXLongRunStatus status = PXLongOperation.GetStatus(this.UID, out timespan, out message);

			if (status == PXLongRunStatus.NotExists)
				return;

			PXButtonState state = PXButtonState.CreateInstance(e.ReturnState, null, null, null, null, null, false,
															   PXConfirmationType.Unspecified, null, null, null, null, null,
															   null, null, null, null, null, null, typeof(Filter));
			state.Enabled = false;
			e.ReturnState = state;
		}

		/// <summary>
		/// Sets default baccount type. Method is used as a workaround for the redirection problem with the edit button of the empty Business Account field.
		/// </summary>
		private void SetDefaultBaccountType(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTran bankTransaction = DetailsForPaymentCreation.Current;

			if (e.Row == null || bankTransaction == null)
				return;

			if (bankTransaction.OrigModule == BatchModule.AP)
				e.NewValue = CR.BAccountType.VendorType;
			else if (bankTransaction.OrigModule == BatchModule.AR)
				e.NewValue = CR.BAccountType.CustomerType;
		}
		#endregion

		#region Internal Classes definitions
		public class MatchInvoiceContext : IDisposable
		{
			private CABankTransactionsMaint Graph { get; set; }

			public MatchInvoiceContext(CABankTransactionsMaint graph)
			{
				graph.MatchInvoiceProcess = true;
				Graph = graph;
			}

			public void Dispose()
			{
				Graph.MatchInvoiceProcess = false;
			}
		}

		internal bool MatchInvoiceProcess { get; set; } = false;

		[Serializable]
		public partial class Filter : IBqlTable
		{
			#region CashAccountID
			public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
			protected Int32? _CashAccountID;
			[CashAccount(typeof(AccessInfo.branchID), typeof(Search<
				CashAccount.cashAccountID,
				Where<Match<Current<AccessInfo.userName>>>>))]
			public virtual int? CashAccountID
			{
				get
				{
					return this._CashAccountID;
				}
				set
				{
					this._CashAccountID = value;
				}
			}
			#endregion
			#region TranType
			public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
			protected String _TranType;
			[PXDBString(1, IsFixed = true)]
			[PXDefault(typeof(CABankTranType.statement))]
			[CABankTranType.List()]
			public virtual String TranType
			{
				get
				{
					return this._TranType;
				}
				set
				{
					this._TranType = value;
				}
			}
			#endregion
			#region IsCorpCardCashAccount
			public abstract class isCorpCardCashAccount : PX.Data.BQL.BqlBool.Field<isCorpCardCashAccount> { }

			[PXUIField(DisplayName = "IsCorpCardCashAccount", Visible = false)]
			[PXBool]
			[PXFormula(typeof(Selector<Filter.cashAccountID, CashAccount.useForCorpCard>))]
			public bool? IsCorpCardCashAccount { get; set; }
			#endregion
			#region BaseCuryID
			public abstract class baseCuryID : PX.Data.BQL.BqlString.Field<baseCuryID> { }
			[PXDBString(5, IsUnicode = true)]
			[PXFormula(typeof(Selector<Filter.cashAccountID, CashAccount.baseCuryID>))]
			public virtual string BaseCuryID { get; set; }
			#endregion
		}

		#endregion
		#region Public Memebrs
		public PXFilter<Filter> TranFilter;
		public PXSelect<AR.Standalone.ARRegister> dummyARRegister;
		public PXSelect<AP.Standalone.APRegister> dummyAPRegister;
		[PXFilterable]
		public PXSelect<CABankTran,
			Where<True, Equal<True>>,
			OrderBy<Asc<CABankTran.sortOrder>>> Details;
		public PXSelect<CABankTran, Where<CABankTran.tranID, Equal<Current<CABankTran.tranID>>>> CurrentCABankTran;

		public SelectFrom<CABankTax>
				   .Where<CABankTax.bankTranID.IsEqual<CABankTran.tranID.FromCurrent>>.View Taxes;

		public SelectFrom<CABankTaxTran>
					.LeftJoin<Tax>.On<Tax.taxID.IsEqual<CABankTaxTran.taxID>>
					.Where<CABankTaxTran.bankTranID.IsEqual<CABankTran.tranID.FromCurrent>
					  .And<CABankTaxTran.bankTranType.IsEqual<CABankTran.tranType.FromCurrent>>>.View TaxTrans;

		public SelectFrom<TaxZone>
				   .Where<TaxZone.taxZoneID.IsEqual<CABankTran.taxZoneID.FromCurrent>>.View Taxzone;

		public PXSelect<
			CABankTran,
			Where<CABankTran.processed, Equal<False>,
				And<CABankTran.cashAccountID, Equal<Current<Filter.cashAccountID>>,
				And<CABankTran.tranType, Equal<Current<Filter.tranType>>,
				And<CABankTran.documentMatched, Equal<False>>>>>>
			UnMatchedDetails;
		public PXSelect<
			CABankTran,
			Where<CABankTran.tranID, Equal<Current<CABankTran.tranID>>>>
			DetailsForPaymentCreation;
		public PXSelect<
			CABankTran,
			Where<CABankTran.tranID, Equal<Current<CABankTran.tranID>>>>
			DetailsForInvoiceApplication;
		public PXSelect<
			CABankTran,
			Where<CABankTran.tranID, Equal<Current<CABankTran.tranID>>>>
			DetailsForPaymentMatching;

		public PXSetup<CASetup> CASetup;
		public CMSetupSelect CMSetup;
		public PXSetup<APSetup> APSetup;
		public PXSetup<ARSetup> arsetup;

		//these view are here for correct StatementsMatchingProto.UpdateSourceDoc work
		public PXSelect<CATran, Where<CATran.tranID, IsNull>> caTran;
		public PXSelect<APPayment, Where<APPayment.docType, IsNull>> apPayment;
		public PXSelect<ARPayment, Where<ARPayment.docType, IsNull>> arPayment;
		public PXSelect<CADeposit, Where<CADeposit.tranType, IsNull>> caDeposit;
		public PXSelect<CAAdj, Where<CAAdj.adjRefNbr, IsNull>> caAdjustment;
		public PXSelect<CATransfer, Where<CATransfer.transferNbr, IsNull>> caTransfer;

		public PXSelectJoin<CATranExt,
			LeftJoin<Light.BAccount, On<Light.BAccount.bAccountID, Equal<CATranExt.referenceID>>>,
			Where<CATranExt.matchRelevance, IsNotNull>, OrderBy<Desc<CATranExt.matchRelevance>>> DetailMatchesCA;
		public PXSelect<CABankTranMatch, Where<CABankTranMatch.matchType, Equal<CABankTranMatch.matchType.match>, And<CABankTranMatch.tranID, Equal<Required<CABankTran.tranID>>>>> TranMatch;
		public PXSelect<
			CABankTranMatch,
			Where<CABankTranMatch.tranID, Equal<Required<CABankTran.tranID>>,
				And<CABankTranMatch.tranType, Equal<Required<CABankTranMatch.tranType>>,
				And<CABankTranMatch.docModule, Equal<Required<CABankTranMatch.docModule>>,
				And<CABankTranMatch.docType, Equal<Required<CABankTranMatch.docType>>,
				And<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>>>>>>>
			TranMatchInvoices;
		public PXSelect<CABankTranMatch, Where<CABankTranMatch.matchType, Equal<CABankTranMatch.matchType.charge>, And<CABankTranMatch.tranID, Equal<Required<CABankTran.tranID>>>>> TranMatchCharge;
		public PXSelectJoin<
			CABankTranAdjustment,
			LeftJoin<ARInvoice,
				On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>,
				And<CABankTranAdjustment.adjdRefNbr, Equal<ARInvoice.refNbr>,
				And<CABankTranAdjustment.adjdDocType, Equal<ARInvoice.docType>>>>>,
			Where<CABankTranAdjustment.tranID, Equal<Optional<CABankTran.tranID>>>>
			Adjustments;
		public PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Current<Filter.cashAccountID>>>> cashAccount;
		public PXSelect<GeneralInvoice> invoices;

		public SelectFrom<CABankChargeTax>
				   .Where<CABankChargeTax.bankTranID.IsEqual<CABankTran.tranID.FromCurrent>>.View ChargeTaxes;

		public SelectFrom<CABankTaxTranMatch>
					.LeftJoin<Tax>.On<Tax.taxID.IsEqual<CABankTaxTranMatch.taxID>>
					.Where<CABankTaxTranMatch.bankTranID.IsEqual<CABankTran.tranID.FromCurrent>
					  .And<CABankTaxTranMatch.bankTranType.IsEqual<CABankTran.tranType.FromCurrent>>>.View ChargeTaxTrans;

		[PXCopyPasteHiddenView]
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>> CurrencyInfo_CuryInfoID;
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Optional<CABankTran.curyInfoID>>>> currencyinfo;
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Optional<CABankTranAdjustment.adjgCuryInfoID>>>> currencyinfo_adjustment;
		public PXSelectReadonly<CashAccount, Where<CashAccount.extRefNbr, Equal<Optional<CashAccount.extRefNbr>>>> cashAccountByExtRef;

		public PXSelect<CABankTranInvoiceMatch, Where<True, Equal<False>>, OrderBy<Desc<CABankTranInvoiceMatch.matchRelevance>>> DetailMatchingInvoices;

		public PXSelect<CABankTranExpenseDetailMatch,
					Where<True, Equal<True>>,
					OrderBy<Desc<CABankTranExpenseDetailMatch.matchRelevance,
							Asc<CABankTranExpenseDetailMatch.curyDocAmtDiff>>>>
			ExpenseReceiptDetailMatches;

		[PXCopyPasteHiddenView]
		public PXSelect<CAExpense> cAExpense;

		[PXHidden]
		public PXSelect<CABankTranRule, Where<CABankTranRule.isActive, Equal<True>>> Rules;

		public PXSelect<CABankTranDetail,
						Where<CABankTranDetail.bankTranID, Equal<Optional<CABankTran.tranID>>,
							And<CABankTranDetail.bankTranType, Equal<Optional<CABankTran.tranType>>>>>
						TranSplit;

		public PXSelect<CR.BAccountR> BaccountCache;
		public PXSelect<Light.BAccount> LightBAccountCache;

		public PXSelect<EPExpenseClaimDetails> ExpenseReceipts;

		[InjectDependency]
		public ICABankTransactionsRepository CABankTransactionsRepository { get; set; }

		public virtual IMatchSettings CurrentMatchSesstings
		{
			get { return cashAccount.Current ?? cashAccount.Select(); }
		}

		#endregion
		#region Delegates
		protected virtual IEnumerable detailMatchingInvoices()
		{
			CABankTran detail = this.Details.Current;
			if (detail == null) yield break;
			PXCache cache = this.DetailMatchingInvoices.Cache;
			cache.Clear();
			detail.CountInvoiceMatches = 0;
			IEnumerable matches = CABankMatchingProcess.FindDetailMatchingInvoicesProc(this, detail, cashAccount.Current ?? cashAccount.Select(), 0m, null);
			if (matches.Any_())
			{
				List<CABankTranMatch> existingMatches = new List<CABankTranMatch>();
				foreach (CABankTranMatch match in TranMatch.Select(detail.TranID))
				{
					if (match.DocModule != null && match.DocType != null && match.DocRefNbr != null)
					{
						existingMatches.Add(match);
					}
				}

				foreach (CABankTranInvoiceMatch it in matches)
				{
					CABankTranInvoiceMatch invMatch = cache.Insert(it) as CABankTranInvoiceMatch;
					if (invMatch != null)
					{
						bool matched = false;
						if (existingMatches.Any(existingMatch => existingMatch.DocModule == invMatch.OrigModule
													&& existingMatch.DocType == invMatch.OrigTranType
													&& existingMatch.DocRefNbr == invMatch.OrigRefNbr))
						{
							matched = true;
						}
						cache.SetValue<CABankTranInvoiceMatch.isMatched>(invMatch, matched);
						yield return invMatch;
					}
				}
			}
			cache.IsDirty = false;
			yield break;
		}

		protected virtual IEnumerable expenseReceiptDetailMatches()
		{
			CABankTran detail = Details.Current;

			if (detail == null)
				yield break;

			PXCache matchesCache = ExpenseReceiptDetailMatches.Cache;

			matchesCache.Clear();

			detail.CountExpenseReceiptDetailMatches = 0;

			IList<CABankTranExpenseDetailMatch> matches = CABankMatchingProcess.FindExpenseReceiptDetailMatches(this, detail, cashAccount.Current ?? cashAccount.Select(), 0m, null);

			if (matches.Any())
			{
				CABankTranMatch existingMatch = null;
				foreach (CABankTranMatch match in TranMatch.Select(detail.TranID))
				{
					if (match.DocModule != null && match.DocType != null && match.DocRefNbr != null)
					{
						existingMatch = match;
						break;
					}
				}

				foreach (CABankTranExpenseDetailMatch it in matches)
				{
					CABankTranExpenseDetailMatch expenseMatch = (CABankTranExpenseDetailMatch)matchesCache.Insert(it);

					if (expenseMatch != null)
					{
						bool matched = false;
						if (existingMatch != null
							&& existingMatch.DocModule == BatchModule.EP
							&& existingMatch.DocType == EPExpenseClaimDetails.DocType
							&& existingMatch.DocRefNbr == expenseMatch.RefNbr)
						{
							matched = true;
							existingMatch = null;//we've already found a match. There can be only one match in current implementation.
						}
						matchesCache.SetValue<CABankTranExpenseDetailMatch.isMatched>(expenseMatch, matched);
						yield return expenseMatch;
					}
				}
			}
			matchesCache.IsDirty = false;
			yield break;
		}

		protected virtual IEnumerable details()
		{
			Filter current = TranFilter.Current;
			if (current == null || current.CashAccountID == null) yield break;

			TimeSpan timespan;
			Exception ex;

			PXLongRunStatus status = PXLongOperation.GetStatus(this.UID, out timespan, out ex);

			IEnumerable<CABankTran> recordsInProcessing = null;
			if (status != PXLongRunStatus.NotExists)
			{
				object[] processingList;
				var customInfo = PXLongOperation.GetCustomInfo(this.UID, out processingList);

				if (processingList != null)
				{
					recordsInProcessing = processingList.Cast<CABankTran>();
				}
			}

			foreach (CABankTran det in recordsInProcessing ?? GetUnprocessedTransactions())
			{
				yield return det;
			}
		}

		protected virtual IEnumerable<PXResult<CATranExt, Light.BAccount>> FindDetailMatches(CABankTran aDetail, IMatchSettings cashAccount, decimal minRelevance)
		{
			return StatementsMatchingProto.FindDetailMatches(this, aDetail, cashAccount, minRelevance, null);
		}

		protected virtual IEnumerable<CABankTran> GetUnprocessedTransactions()
		{
			Filter current = TranFilter.Current;
			if (current == null || current.CashAccountID == null) yield break;

			foreach (CABankTran det in PXSelect<
				CABankTran,
				Where<CABankTran.processed, Equal<False>,
					And<CABankTran.cashAccountID, Equal<Required<CABankTran.cashAccountID>>,
					And<CABankTran.tranType, Equal<Required<CABankTran.tranType>>>>>>
				.Select(this, current.CashAccountID, current.TranType)
				.RowCast<CABankTran>())
			{
				yield return det;
			}
		}

		protected virtual IEnumerable detailMatchesCA()
		{
			CABankTran detail = this.Details.Current;
			if (detail == null) yield break;
			PXCache cache = this.DetailMatchesCA.Cache;
			var items = cache.Cached.ToArray<CATranExt>();
			detail.CountMatches = 0;
			cache.Clear();
			foreach (PXResult<CATranExt, Light.BAccount> result in FindDetailMatches(detail, CurrentMatchSesstings, decimal.Zero))
			{
				CATranExt CATran = result;
				Light.BAccount bAccount = result;

				CATranExt det = null;
				bool matched = CATran.IsMatched == true;
				CATran.IsMatched = null;
				if (CATran.OrigModule == BatchModule.AP && CATran.OrigTranType == CATranType.CABatch)
				{
					foreach (CATranExt inserted in items)
					{
						if (inserted.OrigModule == BatchModule.AP && inserted.OrigTranType == CATranType.CABatch && inserted.OrigRefNbr == CATran.OrigRefNbr)
						{
							CATran.TranID = inserted.TranID;
							det = DetailMatchesCA.Update(CATran);
							break;
						}
					}
				}
				if (det == null)
					det = DetailMatchesCA.Insert(CATran);
				det.IsMatched = matched;
				yield return new PXResult<CATranExt, Light.BAccount>(det, bAccount);
			}
			cache.IsDirty = false;
		}
		#endregion
		#region
		protected virtual bool IsMatchedOnCreateTab(string refNbr, string docType, string module)
		{
			CABankTranAdjustment tranAdj = PXSelect<
				CABankTranAdjustment,
				Where<CABankTranAdjustment.adjdRefNbr, Equal<Required<CABankTranAdjustment.adjdRefNbr>>,
					And<CABankTranAdjustment.adjdModule, Equal<Required<CABankTranAdjustment.adjdModule>>,
					And<CABankTranAdjustment.adjdDocType, Equal<Required<CABankTranAdjustment.adjdDocType>>>>>>
				.Select(this, refNbr, module, docType);
		
			return tranAdj != null;
		}
		#endregion
		#region Internal Variables
		protected Dictionary<Object, List<CABankTranInvoiceMatch>> matchingInvoices;
		protected Dictionary<Object, List<PXResult<CATranExt, Light.BAccount>>> matchingTrans;
		protected Dictionary<Object, List<CABankTranExpenseDetailMatch>> matchingExpenseReceiptDetails;

		protected virtual bool IsInvoiceMatched(string refNbr, string docType, string module)
		{
			CABankTranMatch match = PXSelect<
				CABankTranMatch,
				Where<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>,
					And<CABankTranMatch.docModule, Equal<Required<CABankTranMatch.docModule>>,
					And<CABankTranMatch.docType, Equal<Required<CABankTranMatch.docType>>>>>>
				.Select(this, refNbr, module, docType);

			return match != null;
		}

		protected virtual void PopulateAdjustmentFieldsAR(CABankTranAdjustment adj)
		{
			GetExtension<StatementApplicationBalancesProto>().PopulateAdjustmentFieldsAR(DetailsForPaymentCreation.Current, adj);
		}

		protected virtual void PopulateAdjustmentFieldsAP(CABankTranAdjustment adj)
		{
			GetExtension<StatementApplicationBalancesProto>().PopulateAdjustmentFieldsAP(DetailsForPaymentCreation.Current, adj);
		}

		public virtual void UpdateBalance(CABankTranAdjustment adj, bool isCalcRGOL)
		{
			if (adj.AdjdDocType != null && adj.AdjdRefNbr != null)
			{
				GetExtension<StatementApplicationBalancesProto>().UpdateBalance(DetailsForPaymentCreation.Current, adj, isCalcRGOL);
			}
		}
		#endregion
		#region Buttons
		public PXSave<Filter> Save;

		public PXAction<Filter> cancel;
		[PXCancelButton]
		[PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable Cancel(PXAdapter adapter)
		{
			int? cashAccount = TranFilter.Current.CashAccountID;
			Clear();
			TranFilter.Cache.SetValueExt<Filter.cashAccountID>(TranFilter.Current, cashAccount);
			return adapter.Get();
		}

		#region LoadOptions
		[PXHidden]
		public class MatchingLoadOptions : ARPaymentEntry.LoadOptions
		{
			#region StartRefNbr
			public new abstract class startRefNbr : PX.Data.BQL.BqlString.Field<startRefNbr> { }
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "From Ref. Nbr.")]
			[PXSelector(typeof(Search2<ARAdjust.ARInvoice.refNbr,
									LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARAdjust.ARInvoice.docType>, And<ARAdjust.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<ARAdjust.released, Equal<boolFalse>, And<ARAdjust.voided, Equal<boolFalse>, And<Where<ARAdjust.adjgDocType, NotEqual<Current<CABankTranAdjustment.adjdDocType>>>>>>>>,
									LeftJoin<CABankTranAdjustment, On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>,
									And<CABankTranAdjustment.adjdDocType, Equal<ARAdjust.ARInvoice.docType>,
										And<CABankTranAdjustment.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<CABankTranAdjustment.released, Equal<boolFalse>,
										And<Where<CABankTranAdjustment.tranID,
											NotEqual<Current<CABankTranAdjustment.tranID>>,
											Or<Current<CABankTranAdjustment.adjNbr>, IsNull,
											Or<CABankTranAdjustment.adjNbr, NotEqual<Current<CABankTranAdjustment.adjNbr>>>>>>>>>>,
									LeftJoin<CABankTran, On<CABankTran.tranID, Equal<CABankTranAdjustment.tranID>>>>>,
									Where<ARAdjust.ARInvoice.customerID, In2<Search<PX.Objects.AR.Override.BAccount.bAccountID,
										Where<PX.Objects.AR.Override.BAccount.bAccountID, Equal<Current<CABankTran.payeeBAccountID>>,
										Or<PX.Objects.AR.Override.BAccount.consolidatingBAccountID, Equal<Current<CABankTran.payeeBAccountID>>>>>>,
									And<ARAdjust.ARInvoice.released, Equal<boolTrue>,
									And<ARAdjust.ARInvoice.openDoc, Equal<boolTrue>,
									And<ARAdjust.adjgRefNbr, IsNull,
									And<ARAdjust.ARInvoice.pendingPPD, NotEqual<True>,
									And<Where<CABankTranAdjustment.adjdRefNbr, IsNull, Or<CABankTran.origModule, NotEqual<BatchModule.moduleAR>>>>>>>>>>))]
			public override string StartRefNbr
			{
				get;
				set;
			}
			#endregion
			#region EndRefNbr
			public new abstract class endRefNbr : PX.Data.BQL.BqlString.Field<endRefNbr> { }
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "To Ref. Nbr.")]
			[PXSelector(typeof(Search2<ARAdjust.ARInvoice.refNbr,
									LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARAdjust.ARInvoice.docType>, And<ARAdjust.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<ARAdjust.released, Equal<boolFalse>, And<ARAdjust.voided, Equal<boolFalse>, And<Where<ARAdjust.adjgDocType, NotEqual<Current<CABankTranAdjustment.adjdDocType>>>>>>>>,
									LeftJoin<CABankTranAdjustment, On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>,
									And<CABankTranAdjustment.adjdDocType, Equal<ARAdjust.ARInvoice.docType>,
										And<CABankTranAdjustment.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<CABankTranAdjustment.released, Equal<boolFalse>,
										And<Where<CABankTranAdjustment.tranID,
											NotEqual<Current<CABankTranAdjustment.tranID>>,
											Or<Current<CABankTranAdjustment.adjNbr>, IsNull,
											Or<CABankTranAdjustment.adjNbr, NotEqual<Current<CABankTranAdjustment.adjNbr>>>>>>>>>>,
									LeftJoin<CABankTran, On<CABankTran.tranID, Equal<CABankTranAdjustment.tranID>>>>>,
									Where<ARAdjust.ARInvoice.customerID, In2<Search<PX.Objects.AR.Override.BAccount.bAccountID,
										Where<PX.Objects.AR.Override.BAccount.bAccountID, Equal<Current<CABankTran.payeeBAccountID>>,
										Or<PX.Objects.AR.Override.BAccount.consolidatingBAccountID, Equal<Current<CABankTran.payeeBAccountID>>>>>>,
									And<ARAdjust.ARInvoice.released, Equal<boolTrue>,
									And<ARAdjust.ARInvoice.openDoc, Equal<boolTrue>,
									And<ARAdjust.adjgRefNbr, IsNull,
									And<ARAdjust.ARInvoice.pendingPPD, NotEqual<True>,
									And<Where<CABankTranAdjustment.adjdRefNbr, IsNull, Or<CABankTran.origModule, NotEqual<BatchModule.moduleAR>>>>>>>>>>))]
			public override string EndRefNbr
			{
				get;
				set;
			}
			#endregion
		}
		#endregion

		public PXFilter<MatchingLoadOptions> loadOpts;

		public PXAction<Filter> loadInvoices;
		[PXUIField(DisplayName = "Load Documents", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		public virtual IEnumerable LoadInvoices(PXAdapter adapter)
		{
			CABankTran currentDoc = Details.Current;
			bool receipt = currentDoc.DrCr == CADrCr.CADebit;
			if (currentDoc.OrigModule == GL.BatchModule.AR)
			{
				var dialogResult = loadOpts.AskExt();
				if (dialogResult == WebDialogResult.OK || dialogResult == WebDialogResult.Yes)
				{
					if (dialogResult == WebDialogResult.Yes)
					{
						foreach (PXResult<CABankTranAdjustment> item in Adjustments.Select())
						{
							Adjustments.Cache.Delete((CABankTranAdjustment)item);
						}
					}
					PXResultset<ARInvoice> custdocs = ARPaymentEntry.GetCustDocs(new ARPaymentEntry.LoadOptions
					{
						LoadChildDocuments = ARPaymentEntry.LoadOptions.loadChildDocuments.IncludeCRM
					},
					new ARPayment
					{
						Released = false,
						OpenDoc = true,
						Hold = false,
						Status = ARDocStatus.Balanced,
						CustomerID = Details.Current.PayeeBAccountID,
						CashAccountID = Details.Current.CashAccountID,
						PaymentMethodID = Details.Current.PaymentMethodID,
						CuryOrigDocAmt = Details.Current.CuryOrigDocAmt,
						DocType = ARDocType.Payment,
						CuryID = Details.Current.CuryID,
						AdjDate = currentDoc.TranDate,
						AdjTranPeriodID = currentDoc.MatchingFinPeriodID
					}, arsetup.Current, this);
					List<string> existing = new List<string>();
					foreach (PXResult<CABankTranAdjustment> res in Adjustments.Select())
					{
						CABankTranAdjustment bankAdj = (CABankTranAdjustment)res;
						existing.Add(string.Format("{0}_{1}", bankAdj.AdjdDocType, bankAdj.AdjdRefNbr));
					}
					foreach (ARInvoice invoice in custdocs.AsEnumerable().Where(row => ((ARInvoice)row).PaymentsByLinesAllowed != true))
					{
						if (!receipt && (invoice.DocType == ARDocType.Invoice || invoice.DocType == ARDocType.DebitMemo || invoice.DocType == ARDocType.FinCharge))
							continue;
						if (receipt && (invoice.DocType == ARDocType.Prepayment || invoice.DocType == ARDocType.Payment))
							continue;
						string s = string.Format("{0}_{1}", invoice.DocType, invoice.RefNbr);
						if (existing.Contains(s) == false &&
							!IsMatchedOnCreateTab(invoice.RefNbr, invoice.DocType, BatchModule.AR) &&
							!IsInvoiceMatched(invoice.RefNbr, invoice.DocType, BatchModule.AR) &&
							BankStatementProtoHelpers.PXInvoiceSelectorAttribute.GetRecordsAR(invoice.DocType, currentDoc.TranID, null, currentDoc, Adjustments.Cache, this)
								.Where(inv => inv.DocType == invoice.DocType && inv.RefNbr == invoice.RefNbr)
								.Any())
						{
							if (currentDoc.CuryUnappliedBal == 0m && currentDoc.CuryOrigDocAmt > 0m)
							{
								break;
							}
							CABankTranAdjustment bankAdj = new CABankTranAdjustment();

							bankAdj = Adjustments.Insert(bankAdj);
							bankAdj.AdjdDocType = invoice.DocType;
							bankAdj.AdjdRefNbr = invoice.RefNbr;
							Adjustments.Update(bankAdj);
						}
					}
					if (currentDoc.CuryApplAmt < 0m)
					{
						List<CABankTranAdjustment> credits = new List<CABankTranAdjustment>();

						foreach (CABankTranAdjustment adj in Adjustments.Select())
						{
							if (adj.AdjdDocType == ARDocType.CreditMemo)
							{
								credits.Add(adj);
							}
						}

						credits.Sort((a, b) =>
						{
							return ((IComparable)a.CuryAdjgAmt).CompareTo(b.CuryAdjgAmt);
						});

						foreach (CABankTranAdjustment adj in credits)
						{
							if (adj.CuryAdjgAmt <= -currentDoc.CuryApplAmt)
							{
								Adjustments.Delete(adj);
							}
							else
							{
								CABankTranAdjustment copy = PXCache<CABankTranAdjustment>.CreateCopy(adj);
								copy.CuryAdjgAmt += currentDoc.CuryApplAmt;
								Adjustments.Update(copy);
							}
						}
					}
				}


			}
			else if (currentDoc.OrigModule == GL.BatchModule.AP)
			{
				APPayment payment = new APPayment();
				payment.Released = false;
				payment.OpenDoc = true;
				payment.Hold = false;
				payment.Status = APDocStatus.Balanced;
				payment.VendorID = currentDoc.PayeeBAccountID;
				payment.CashAccountID = currentDoc.CashAccountID;
				payment.PaymentMethodID = currentDoc.PaymentMethodID;
				payment.CuryOrigDocAmt = currentDoc.CuryOrigDocAmt;
				payment.DocType = APDocType.Check;
				payment.CuryID = currentDoc.CuryID;
				payment.AdjDate = currentDoc.TranDate;
				payment.AdjTranPeriodID = currentDoc.MatchingFinPeriodID;
				PXResultset<APInvoice> venddocs = APPaymentEntry.GetVendDocs(payment, this, APSetup.Current);
				List<string> existing = new List<string>();
				foreach (PXResult<CABankTranAdjustment> res in Adjustments.Select())
				{
					CABankTranAdjustment bankAdj = (CABankTranAdjustment)res;
					existing.Add(string.Format("{0}_{1}", bankAdj.AdjdDocType, bankAdj.AdjdRefNbr));
				}
				foreach (APInvoice invoice in venddocs.AsEnumerable().Where(row => ((APInvoice)row).PaymentsByLinesAllowed != true))
				{
					if (receipt && (invoice.DocType == APDocType.CreditAdj || invoice.DocType == ARDocType.Invoice))
						continue;
					string s = string.Format("{0}_{1}", invoice.DocType, invoice.RefNbr);
					if (existing.Contains(s) == false &&
						!IsInvoiceMatched(invoice.RefNbr, invoice.DocType, BatchModule.AP) &&
						!IsMatchedOnCreateTab(invoice.RefNbr, invoice.DocType, BatchModule.AP) &&
						BankStatementProtoHelpers.PXInvoiceSelectorAttribute.GetRecordsAP(invoice.DocType, currentDoc.TranID, null, currentDoc, Adjustments.Cache, this)
							.Where(inv => inv.DocType == invoice.DocType && inv.RefNbr == invoice.RefNbr)
							.Any())
					{
						if (currentDoc.CuryUnappliedBal == 0m && currentDoc.CuryOrigDocAmt > 0m)
						{
							break;
						}
						CABankTranAdjustment bankAdj = new CABankTranAdjustment();

						bankAdj = Adjustments.Insert(bankAdj);
						bankAdj.AdjdDocType = invoice.DocType;
						bankAdj.AdjdRefNbr = invoice.RefNbr;
						Adjustments.Update(bankAdj);
					}
				}
				//removung debit adjustments that sets balance below 0
				if (currentDoc.CuryApplAmt < 0m)
				{
					List<CABankTranAdjustment> debits = new List<CABankTranAdjustment>();

					foreach (CABankTranAdjustment adj in Adjustments.Select())
					{
						if (adj.AdjdDocType == APDocType.DebitAdj)
						{
							debits.Add(adj);
						}
					}

					debits.Sort((a, b) =>
					{
						return ((IComparable)a.CuryAdjgAmt).CompareTo(b.CuryAdjgAmt);
					});

					foreach (CABankTranAdjustment adj in debits)
					{
						if (adj.CuryAdjgAmt <= -currentDoc.CuryApplAmt)
						{
							Adjustments.Delete(adj);
						}
						else
						{
							CABankTranAdjustment copy = PXCache<CABankTranAdjustment>.CreateCopy(adj);
							copy.CuryAdjgAmt += currentDoc.CuryApplAmt;
							Adjustments.Update(copy);
						}
					}
				}
			}

			return adapter.Get();
		}

		#region AutoMatch
		public PXAction<Filter> autoMatch;
		[PXUIField(DisplayName = Messages.AutoMatch, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXProcessButton]
		public virtual IEnumerable AutoMatch(PXAdapter adapter)
		{
			Save.Press();
			var graph = PXGraph.CreateInstance<CABankMatchingProcess>();
			graph.DoMatch(this, (cashAccount.Current ?? cashAccount.Select()).CashAccountID);

			return adapter.Get();
		}
		#endregion

		public PXAction<Filter> processMatched;
		[PXUIField(DisplayName = AR.Messages.Process, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public virtual IEnumerable ProcessMatched(PXAdapter adapter)
		{
			Save.Press();
			PXResultset<CABankTran> list = Details.Select();
			var toProcess = list.RowCast<CABankTran>().Where(t => t.DocumentMatched == true && t.Processed != true).ToList();
			if (toProcess.Count < 1)
				return adapter.Get();
			PXLongOperation.ClearStatus(this.UID);
			PXLongOperation.StartOperation(this, delegate () { DoProcessing(toProcess); });
			Caches[typeof(Light.ARInvoice)].ClearQueryCache();
			Caches[typeof(Light.APInvoice)].ClearQueryCache();
			return adapter.Get();
		}

		public PXAction<Filter> matchSettingsPanel;
		[PXUIField(DisplayName = Messages.MatchSettings, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public virtual IEnumerable MatchSettingsPanel(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXSelect<Filter> NewRevisionPanel;
		public PXAction<Filter> uploadFile;
		[PXUIField(DisplayName = Messages.UploadFile, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton()]
		public virtual IEnumerable UploadFile(PXAdapter adapter)
		{
			this.Save.Press();
			bool doImport = true;
			Filter row = TranFilter.Current;
			if (CASetup.Current.ImportToSingleAccount == true)
			{
				if (row == null || row.CashAccountID == null)
				{
					throw new PXException(Messages.CashAccountMustBeSelectedToImportStatement);
				}
				else
				{
					CashAccount acct = PXSelect<
						CashAccount,
						Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>
						.Select(this, row.CashAccountID);
					if (acct != null && string.IsNullOrEmpty(acct.StatementImportTypeName))
					{
						throw new PXException(Messages.StatementImportServiceMustBeConfiguredForTheCashAccount);
					}
				}
			}
			else
			{
				if (string.IsNullOrEmpty(CASetup.Current.StatementImportTypeName))
				{
					throw new PXException(Messages.StatementImportServiceMustBeConfiguredInTheCASetup);
				}
			}

			if (Details.Current != null && this.IsDirty == true)
			{
				if (CASetup.Current.ImportToSingleAccount != true)
				{
					if (Details.Ask(Messages.ImportConfirmationTitle, Messages.UnsavedDataInThisScreenWillBeLostConfirmation, MessageButtons.YesNo) != WebDialogResult.Yes)
					{
						doImport = false;
					}
				}
				else
				{
					doImport = true;
				}
			}

			if (doImport)
			{
				if (this.NewRevisionPanel.AskExt() == WebDialogResult.OK)
				{
					Filter currFilter = (Filter)TranFilter.Cache.CreateCopy(TranFilter.Current);
					const string PanelSessionKey = "ImportStatementProtoFile";
					PX.SM.FileInfo info = PXContext.SessionTyped<PXSessionStatePXData>().FileInfo[PanelSessionKey] as PX.SM.FileInfo;
					System.Web.HttpContext.Current.Session.Remove(PanelSessionKey);
					CABankTransactionsImport importGraph = PXGraph.CreateInstance<CABankTransactionsImport>();
					CABankTranHeader newCurrent = new CABankTranHeader() { CashAccountID = row.CashAccountID };
					if (CASetup.Current.ImportToSingleAccount == true)
					{
						newCurrent = importGraph.Header.Insert(newCurrent);
					}
					importGraph.Header.Current = newCurrent;
					importGraph.ImportStatement(info, false);
					importGraph.Save.Press();
					this.Clear();
					Caches[typeof(CABankTran)].ClearQueryCacheObsolete();
					this.TranFilter.Current = currFilter;
					if (!currFilter.CashAccountID.HasValue)
					{
						currFilter.CashAccountID = importGraph.Header.Current.CashAccountID;
						List<Filter> result = new List<Filter>();
						result.Add(currFilter);
						return result;
					}
				}
			}
			return adapter.Get();
		}


		public PXAction<Filter> clearMatch;
		[PXUIField(DisplayName = Messages.ClearMatch, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton()]
		public virtual IEnumerable ClearMatch(PXAdapter adapter)
		{
			CABankTran detail = Details.Current;
			ClearMatchProc(detail);
			return adapter.Get();
		}

		public PXAction<Filter> clearAllMatches;
		[PXUIField(DisplayName = Messages.ClearAllMatches, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton()]
		public virtual IEnumerable ClearAllMatches(PXAdapter adapter)
		{
			foreach (CABankTran detail in Details.Select())
			{
				ClearMatchProc(detail);
			}
			return adapter.Get();
		}

		protected virtual void ClearMatchProc(CABankTran detail)
		{
			if (detail.Processed == false && (detail.DocumentMatched == true || detail.CreateDocument == true))
			{
				foreach (CABankTranMatch match in TranMatch.Select(detail.TranID))
				{
					if (IsMatchedToExpenseReceipt(match))
					{
						EPExpenseClaimDetails receipt =
							PXSelect<EPExpenseClaimDetails,
									Where<EPExpenseClaimDetails.claimDetailCD,
										Equal<Required<EPExpenseClaimDetails.claimDetailCD>>>>
								.Select(this, match.DocRefNbr);

						receipt.BankTranDate = null;

						ExpenseReceipts.Update(receipt);
					}

					TranMatch.Delete(match);
				}

				ClearFields(detail);
				Details.Update(detail);
			}
		}

		public static void ClearFields(CABankTran detail)
		{
			detail.CreateDocument = false;
			detail.DocumentMatched = false;
			detail.MultipleMatching = false;
			detail.BAccountID = null;
			detail.OrigModule = null;
			detail.PaymentMethodID = null;
			detail.PMInstanceID = null;
			detail.LocationID = null;
			detail.EntryTypeID = null;
			detail.UserDesc = null;
			detail.InvoiceNotFound = null;
			detail.RuleID = null;
		}

		public static void ClearChargeFields(CABankTran detail)
		{
			detail.ChargeTypeID = null;
			detail.CuryChargeAmt = null;
		}

		public PXAction<Filter> hide;
		[PXUIField(DisplayName = Messages.HideTran, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton()]
		public virtual IEnumerable Hide(PXAdapter adapter)
		{
			CABankTran detail = Details.Current;
			if (detail.Processed == false && Details.Ask(Messages.HideTran, Messages.HideTranMsg, MessageButtons.YesNo) == WebDialogResult.Yes)
			{
				ClearMatchProc(detail);
				detail.Hidden = true;
				detail.Processed = true;
				Details.Update(detail);
			}
			return adapter.Get();
		}

		[Serializable]
		public class CreateRuleSettings : IBqlTable
		{
			public abstract class ruleName : PX.Data.BQL.BqlString.Field<ruleName> { }

			[PXDBString(30, IsUnicode = true, InputMask = ">AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
			[PXUIField(DisplayName = "Rule ID", Required = true)]
			public virtual string RuleName { get; set; }
		}

		public PXFilter<CreateRuleSettings> RuleCreation;

		public PXAction<Filter> createRule;

		[PXUIField(DisplayName = Messages.CreateRule, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton]
		public virtual void CreateRule()
		{
			var currentTran = DetailsForPaymentCreation.Current as CABankTran;

			if (currentTran == null || currentTran.CreateDocument != true)
				return;

			if (currentTran.OrigModule != BatchModule.CA)
				throw new PXException(Messages.BankRuleOnlyCADocumentsSupported);

			var rulesGraph = PXGraph.CreateInstance<CABankTranRuleMaintPopup>();
			var rule = new CABankTranRulePopup
			{
				BankDrCr = currentTran.DrCr,
				BankTranCashAccountID = currentTran.CashAccountID,
				TranCode = currentTran.TranCode,
				BankTranDescription = currentTran.TranDesc,
				AmountMatchingMode = MatchingMode.Equal,
				CuryTranAmt = Math.Abs(currentTran.CuryTranAmt ?? 0.0m),
				DocumentModule = currentTran.OrigModule,
				DocumentEntryTypeID = currentTran.EntryTypeID,
				TranCuryID = currentTran.CuryID
			};
			rulesGraph.Rule.Cache.Insert(rule);

			PXRedirectHelper.TryRedirect(rulesGraph, PXRedirectHelper.WindowMode.Popup);
		}

		public PXAction<Filter> unapplyRule;

		[PXUIField(DisplayName = Messages.ClearRule, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton]
		public virtual void UnapplyRule()
		{
			var currentTran = DetailsForPaymentCreation.Current as CABankTran;

			if (currentTran == null || currentTran.CreateDocument != true || currentTran.RuleID == null)
				return;

			ClearRule(DetailsForPaymentCreation.Cache, currentTran);
		}

		public PXAction<Filter> viewPayment;

		[PXUIField(DisplayName = Messages.ViewPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void ViewPayment()
		{
			var currentPayment = DetailMatchesCA.Current;
			if (currentPayment == null)
				return;

			PXRedirectHelper.TryRedirect(DetailMatchesCA.Cache, currentPayment, "Document", PXRedirectHelper.WindowMode.NewWindow);
		}

		public PXAction<Filter> viewInvoice;

		[PXUIField(DisplayName = Messages.ViewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void ViewInvoice()
		{
			var currentInvoice = DetailMatchingInvoices.Current;
			if (currentInvoice == null)
				return;

			PXCache cache = null;
			object document = null;

			switch (currentInvoice.OrigModule)
			{
				case BatchModule.AP:
					{
						document = (APInvoice)PXSelect<
							APInvoice,
							Where<APInvoice.docType, Equal<Required<APInvoice.docType>>,
								And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>
							.Select(this, currentInvoice.OrigTranType, currentInvoice.OrigRefNbr);

						if (document != null)
						{
							cache = this.Caches[typeof(APInvoice)];
						}
					}
					break;
				case BatchModule.AR:
					{
						document = (ARInvoice)PXSelect<
							ARInvoice,
							Where<ARInvoice.docType, Equal<Required<ARInvoice.docType>>,
								And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>
							.Select(this, currentInvoice.OrigTranType, currentInvoice.OrigRefNbr);
						if (document != null)
						{
							cache = this.Caches[typeof(ARInvoice)];
						}
					}
					break;
			}

			if (cache != null && document != null)
			{
				PXRedirectHelper.TryRedirect(cache, document, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
		}
		public PXAction<Filter> refreshAfterRuleCreate;
		[PXButton]
		public virtual void RefreshAfterRuleCreate()
		{
			CABankTran tran = (CABankTran)Details.Current;
			this.cancel.Press();
			Details.Select();
			Details.Current = (CABankTran)Details.Cache.Locate(tran);
			AttemptApplyRules((CABankTran)Details.Current, false);
		}

		public PXAction<Filter> viewDocumentToApply;
		[PXUIField(DisplayName = "View Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewDocumentToApply(PXAdapter adapter)
		{
			CABankTran header = DetailsForPaymentCreation.Current;
			CABankTranAdjustment row = Adjustments.Current;

			if (header?.OrigModule == GL.BatchModule.AP)
			{
				APRegister doc = PXSelect<
					APRegister,
					Where<APRegister.docType, Equal<Required<APRegister.docType>>,
						And<APRegister.refNbr, Equal<Required<APRegister.refNbr>>>>>
					.Select(this, row.AdjdDocType, row.AdjdRefNbr);

				PXRedirectHelper.TryRedirect(dummyAPRegister.Cache, doc, Messages.ViewDocument, PXRedirectHelper.WindowMode.NewWindow);
			}

			if (header.OrigModule == GL.BatchModule.AR)
			{
				ARRegister doc = PXSelect<
					ARRegister,
					Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
						And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>
					.Select(this, row.AdjdDocType, row.AdjdRefNbr);

				PXRedirectHelper.TryRedirect(dummyARRegister.Cache, doc, Messages.ViewDocument, PXRedirectHelper.WindowMode.NewWindow);
			}

			return adapter.Get();
		}

		public PXAction<Filter> ViewExpenseReceipt;

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		protected virtual IEnumerable viewExpenseReceipt(PXAdapter adapter)
		{
			RedirectionToOrigDoc.TryRedirect(EPExpenseClaimDetails.DocType, ExpenseReceiptDetailMatches.Current.RefNbr, BatchModule.EP);

			return adapter.Get();
		}

		public PXAction<Filter> ResetMatchSettingsToDefault;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, DisplayName = "Reset to Default")]
		[PXButton]
		protected virtual IEnumerable resetMatchSettingsToDefault(PXAdapter adapter)
		{
			PXCache cache = cashAccount.Cache;
			CashAccount row = cashAccount.Current;

			cache.SetDefaultExt<CashAccount.receiptTranDaysBefore>(row);
			cache.SetDefaultExt<CashAccount.receiptTranDaysAfter>(row);
			cache.SetDefaultExt<CashAccount.disbursementTranDaysBefore>(row);
			cache.SetDefaultExt<CashAccount.disbursementTranDaysAfter>(row);
			cache.SetDefaultExt<CashAccount.allowMatchingCreditMemo>(row);
			cache.SetDefaultExt<CashAccount.refNbrCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.dateCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.payeeCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.dateMeanOffset>(row);
			cache.SetDefaultExt<CashAccount.dateSigma>(row);
			cache.SetDefaultExt<CashAccount.curyDiffThreshold>(row);
			cache.SetDefaultExt<CashAccount.amountWeight>(row);
			cache.SetDefaultExt<CashAccount.emptyRefNbrMatching>(row);
			cache.SetDefaultExt<CashAccount.skipVoided>(row);
			cache.SetDefaultExt<CashAccount.matchThreshold>(row);
			cache.SetDefaultExt<CashAccount.relativeMatchThreshold>(row);

			cache.SetDefaultExt<CashAccount.invoiceFilterByCashAccount>(row);
			cache.SetDefaultExt<CashAccount.invoiceFilterByDate>(row);
			cache.SetDefaultExt<CashAccount.daysBeforeInvoiceDiscountDate>(row);
			cache.SetDefaultExt<CashAccount.daysBeforeInvoiceDueDate>(row);
			cache.SetDefaultExt<CashAccount.daysAfterInvoiceDueDate>(row);
			cache.SetDefaultExt<CashAccount.invoiceRefNbrCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.invoiceDateCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.invoicePayeeCompareWeight>(row);
			cache.SetDefaultExt<CashAccount.averagePaymentDelay>(row);
			cache.SetDefaultExt<CashAccount.invoiceDateSigma>(row);

			row = (CashAccount)cache.Update(row);

			row.MatchSettingsPerAccount = false;

			return adapter.Get();
		}
		#endregion
		#region Events

		#region CABankTranInvoiceMatch Events

		protected virtual void CABankTranInvoiceMatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTranInvoiceMatch row = e.Row as CABankTranInvoiceMatch;
			if (row == null) return;
			PXUIFieldAttribute.SetEnabled(sender, row, false);
			PXUIFieldAttribute.SetEnabled<CABankTranInvoiceMatch.isMatched>(sender, row, true);
		}

		protected virtual void CABankTranInvoiceMatch_IsMatched_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranInvoiceMatch row = e.Row as CABankTranInvoiceMatch;
			if ((bool?)e.NewValue == true
					&& ((Details.Current.DocumentMatched == true && (Details.Current.MultipleMatching != true || Details.Current.MatchedToInvoice != true))
						|| Details.Current.CreateDocument == true))
			{
				throw new PXSetPropertyException(Messages.AnotherOptionChosen, PXErrorLevel.RowWarning);
			}
		}

		protected virtual void CABankTranInvoiceMatch_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CABankTran currentTran = Details.Current;
			CABankTranInvoiceMatch row = e.Row as CABankTranInvoiceMatch;
			CABankTranInvoiceMatch oldRow = e.OldRow as CABankTranInvoiceMatch;
			if (!sender.ObjectsEqual<CABankTranInvoiceMatch.isMatched>(row, oldRow))
			{
				if (row.IsMatched == true)
				{
					bool cashDiscIsApplicable = row.CuryDiscAmt != null
												&& currentTran.TranDate != null
												&& row.DiscDate != null
												&& (DateTime)currentTran.TranDate <= (DateTime)row.DiscDate;

					CABankTranMatch match = new CABankTranMatch()
					{
						TranID = currentTran.TranID,
						TranType = currentTran.TranType,
						DocModule = row.OrigModule,
						DocType = row.OrigTranType,
						DocRefNbr = row.OrigRefNbr,
						ReferenceID = row.ReferenceID,
						CuryApplAmt = row.CuryTranAmt - (cashDiscIsApplicable ? row.CuryDiscAmt : 0)
					};
					TranMatch.Insert(match);
				}
				else
				{
					foreach (var match in TranMatch.Select(currentTran.TranID).RowCast<CABankTranMatch>()
												.Where(item => item.DocModule == row.OrigModule
													&& item.DocType == row.OrigTranType
													&& item.DocRefNbr == row.OrigRefNbr))
					{
						TranMatch.Delete(match);
					}
				}
				bool documentMatched = TranMatch.Select(currentTran.TranID).Any_();
				currentTran.DocumentMatched = documentMatched;

				if (!documentMatched)
				{
					Details.Cache.SetValueExt<CABankTran.origModule>(currentTran, null);
				}
				Details.Cache.Update(currentTran);
			}
			sender.IsDirty = false;
		}

		protected virtual void CABankTranInvoiceMatch_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CABankTranInvoiceMatch_OrigTranType_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		#endregion

		#region CABankTranExpenseClaimDetailMatch Events
		protected virtual void _(Events.RowSelected<CABankTranExpenseDetailMatch> e)
		{
			if (e.Row == null)
				return;
			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<CABankTranExpenseDetailMatch.isMatched>(e.Cache, e.Row, true);
		}

		protected virtual void _(Events.RowUpdated<CABankTranExpenseDetailMatch> e)
		{
			CABankTran currentTran = Details.Current;

			if (!e.Cache.ObjectsEqual<CABankTranExpenseDetailMatch.isMatched>(e.Row, e.OldRow))
			{
				EPExpenseClaimDetails receipt =
					PXSelect<EPExpenseClaimDetails,
							Where<EPExpenseClaimDetails.claimDetailCD,
								Equal<Required<EPExpenseClaimDetails.claimDetailCD>>>>
						.Select(this, e.Row.RefNbr);

				if (e.Row.IsMatched == true)
				{
					CABankTranMatch match = new CABankTranMatch()
					{
						TranID = currentTran.TranID,
						TranType = currentTran.TranType,
						DocModule = BatchModule.EP,
						DocRefNbr = e.Row.RefNbr,
						DocType = EPExpenseClaimDetails.DocType,
						ReferenceID = e.Row.ReferenceID
					};

					TranMatch.Insert(match);

					receipt.BankTranDate = currentTran.TranDate;

					ExpenseReceipts.Update(receipt);
				}
				else
				{
					foreach (var match in TranMatch.Select(currentTran.TranID))
					{
						TranMatch.Delete(match);
					}

					receipt.BankTranDate = null;

					ExpenseReceipts.Update(receipt);

					Details.Cache.SetValueExt<CABankTran.origModule>(currentTran, null);
				}
				currentTran.DocumentMatched = e.Row.IsMatched;
				Details.Cache.Update(currentTran);
			}

			e.Cache.IsDirty = false;
		}

		protected virtual void _(Events.RowPersisting<CABankTranExpenseDetailMatch> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldVerifying<CABankTranExpenseDetailMatch.isMatched> e)
		{
			CABankTranExpenseDetailMatch row = e.Row as CABankTranExpenseDetailMatch;
			if ((bool?)e.NewValue == true && (Details.Current.DocumentMatched == true || Details.Current.CreateDocument == true))
			{
				throw new PXSetPropertyException(Messages.AnotherOptionChosen, PXErrorLevel.RowWarning);
			}
		}

		#endregion

		#region CABankTranAdjustment Events

		protected virtual void CABankTranAdjustment_AdjdRefNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			adj.AdjgDocDate = DetailsForPaymentCreation.Current.TranDate;
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

				if (DetailsForPaymentCreation.Current.OrigModule == GL.BatchModule.AP)
				{
					PopulateAdjustmentFieldsAP(adj);
					sender.SetDefaultExt<CABankTranAdjustment.adjdTranPeriodID>(e.Row);
				}
				else if (DetailsForPaymentCreation.Current.OrigModule == GL.BatchModule.AR)
				{
					PopulateAdjustmentFieldsAR(adj);
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

		protected virtual void CABankTranAdjustment_AdjdRefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj == null)
				return;

			foreach (CABankTranAdjustment other in Adjustments.Select())
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
				vouch_info.SetCuryEffDate(currencyinfo.Cache, DetailsForPaymentCreation.Current.TranDate);
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
			CABankTran det = Details.Current;
			sender.SetValue<CABankTranAdjustment.adjdModule>(row, det.OrigModule);
			CurrencyInfoAttribute.SetDefaults<CABankTran.curyInfoID>(Details.Cache, det);
			CurrencyInfoAttribute.SetDefaults<CABankTranAdjustment.adjgCuryInfoID>(sender, row);
		}

		protected virtual void CABankTranAdjustment_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (adj?.AdjdRefNbr == null)
			{
				Details.Cache.RaiseExceptionHandling<CABankTran.createDocument>(Details.Current, Details.Current.CreateDocument, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.RowError, PXUIFieldAttribute.GetDisplayName<CABankTranAdjustment.adjdRefNbr>(sender)));
			}
			if (adj?.CuryDocBal < 0m)
			{
				sender.RaiseExceptionHandling<CABankTranAdjustment.curyDocBal>(adj, adj.CuryDocBal, new PXSetPropertyException(Messages.DocumentOutOfBalance));
			}
		}
		protected virtual void CABankTranAdjustment_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTranAdjustment adj = (CABankTranAdjustment)e.Row;
			if (Details.Current != null && adj != null)
			{
				bool isAP = adj.AdjdRefNbr != null && DetailsForPaymentCreation.Current?.OrigModule == BatchModule.AP;

				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.adjdRefNbr>(sender, adj, adj.AdjdRefNbr == null);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.adjdDocType>(sender, adj, adj.AdjdRefNbr == null);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.curyAdjgAmt>(sender, adj, adj.AdjdRefNbr != null);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.curyAdjgDiscAmt>(sender, adj, adj.AdjdRefNbr != null);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.curyAdjgWhTaxAmt>(sender, adj, isAP);

				Customer customer = Customer.PK.Find(this, Details.Current.PayeeBAccountID);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.curyAdjgWOAmt>(sender, adj, canBeWrittenOff(customer, adj) && adj.Released != true && adj.Voided == false);
				PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.writeOffReasonCode>(sender, adj, canBeWrittenOff(customer, adj));

				if (Details.Current.InvoiceInfo != null && adj.AdjdRefNbr == Details.Current.InvoiceInfo
					&& (adj.CuryAdjgAmt != Details.Current.CuryTotalAmt || adj.CuryDocBal != decimal.Zero))
				{
					sender.RaiseExceptionHandling<CABankTranAdjustment.curyAdjgAmt>(adj, adj.CuryAdjgAmt, new PXSetPropertyException(Messages.NotExactAmount, PXErrorLevel.Warning));
				}
			}
			PXUIFieldAttribute.SetVisible<ARInvoice.customerID>(Caches[typeof(ARInvoice)], null, PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>() && DetailsForPaymentCreation.Current?.OrigModule == BatchModule.AR);
		}

		Func<Customer, CABankTranAdjustment, bool> canBeWrittenOff = (customer, adj) =>
			customer != null
			&& adj.AdjdRefNbr != null
			&& customer.SmallBalanceAllow == true
			&& customer.SmallBalanceLimit > 0
			&& adj.AdjdDocType != ARDocType.CreditMemo
			&& adj.AdjdModule == BatchModule.AR;

		protected virtual void CABankTranAdjustment_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			CABankTranAdjustment row = (CABankTranAdjustment)e.Row;
			if (row == null)
				return;
			CABankTran tranRow = PXSelect<
				CABankTran,
				Where<CABankTran.tranID, Equal<Required<CABankTranAdjustment.tranID>>>>
				.Select(this, row.TranID);

			if (row.AdjdRefNbr == null)
			{
				Details.Cache.RaiseExceptionHandling<CABankTran.createDocument>(tranRow, tranRow.CreateDocument, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.RowError, PXUIFieldAttribute.GetDisplayName<CABankTranAdjustment.adjdRefNbr>(sender)));
			}
			if (row.CuryWhTaxBal < 0m)
			{
				sender.RaiseExceptionHandling<CABankTran.createDocument>(e.Row, row.CuryAdjgWhTaxAmt, new PXSetPropertyException(AR.Messages.DocumentBalanceNegative));
			}

			if (tranRow.OrigModule == BatchModule.AR && row.CuryAdjgWhTaxAmt != 0 && string.IsNullOrEmpty(row.WriteOffReasonCode))
			{
				Details.Cache.RaiseExceptionHandling<CABankTran.createDocument>(tranRow, tranRow.CreateDocument, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<CABankTranAdjustment.writeOffReasonCode>(sender), PXErrorLevel.RowError));
			}

			if (!string.IsNullOrEmpty(row.WriteOffReasonCode))
			{
				ReasonCode reasonCode = ReasonCode.PK.Find(this, row.WriteOffReasonCode);

				if (reasonCode?.Usage == ReasonCodeUsages.BalanceWriteOff && ((decimal)row.CuryAdjgWOAmt) < 0m)
				{
					sender.RaiseExceptionHandling<CABankTranAdjustment.writeOffReasonCode>(row, row.WriteOffReasonCode, new PXSetPropertyException(Messages.ForNegativeBalanceWriteOffAmountCreditWriteOffReasonCodeShouldBeUsed));
				}

				if (reasonCode?.Usage == ReasonCodeUsages.CreditWriteOff && ((decimal)row.CuryAdjgWOAmt) > 0m)
				{
					sender.RaiseExceptionHandling<CABankTranAdjustment.writeOffReasonCode>(row, row.WriteOffReasonCode, new PXSetPropertyException(Messages.ForPositiveBalanceWriteOffAmountCreditWriteOffReasonCodeShouldBeUsed));
				}
			}

			if (row.CuryDocBal < 0m)
			{
				sender.RaiseExceptionHandling<CABankTranAdjustment.curyDocBal>(row, row.CuryDocBal, new PXSetPropertyException(Messages.DocumentOutOfBalance));
			}
		}

		#endregion
		#region CABankTran Events

		protected virtual void CABankTran_CreateDocument_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row.DocumentMatched == true && (bool?)e.NewValue == true)
			{
				throw new PXSetPropertyException("", PXErrorLevel.RowInfo);
			}
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

		protected virtual void CABankTran_CreateDocument_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row == null) return;
			if (row.CreateDocument == false)
			{
				Details.Cache.SetValueExt<CABankTran.documentMatched>(row, row.CreateDocument);
			}
			ResetTranFields(sender, row);

			if (row.CreateDocument == true)
			{
				bool isInvoiceFound = false;

				try
				{
					isInvoiceFound = ApplyInvoiceInfo(sender, row);
				}
				catch (PXSetPropertyException ex)
				{
					sender.RaiseExceptionHandling<CABankTran.invoiceInfo>(row, row.CreateDocument, new PXSetPropertyException(ex.Message, PXErrorLevel.Warning));
					foreach (CABankTranAdjustment adj in Adjustments.Select())
					{
						Adjustments.Delete(adj);
					}
				}

				bool clearingAccount = ((CashAccount)cashAccount.Select())?.ClearingAccount == true;

				row.UserDesc = CutOffTranDescTo256(row.TranDesc);

				if (row.CreateDocument == true && isInvoiceFound == false && !clearingAccount)
				{
					AttemptApplyRules(row, e.ExternalCall == false);
				}

				row.UserDesc = CutOffTranDescTo256(row.TranDesc);

				Details.Cache.SetValueExt<CABankTran.documentMatched>(row, row.CreateDocument == true && ValidateTranFields(this, sender, row, Adjustments));
				Details.Cache.SetDefaultExt<CABankTran.matchingPaymentDate>(row);
				Details.Cache.SetDefaultExt<CABankTran.matchingfinPeriodID>(row);
			}
			else
			{
				sender.SetValue<CABankTran.invoiceNotFound>(row, false);
			}
		}

		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R2)]
		private string CutOffTranDescTo256(string description)
		{
			return description?.Length > 256 ? description.Substring(0, 255) : description;
		}

		protected virtual void ResetTranFields(PXCache cache, CABankTran transaction)
		{
			cache.SetDefaultExt<CABankTran.ruleID>(transaction);
			cache.SetDefaultExt<CABankTran.origModule>(transaction);
			cache.SetDefaultExt<CABankTran.curyTotalAmt>(transaction);
		}

		protected virtual void ClearRule(PXCache cache, CABankTran transaction)
		{
			ResetTranFields(cache, transaction);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(CA.CABankTran.matchReason.Manual, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CABankTran.matchReason> e) { }

		protected virtual void CABankTran_MultipleMatching_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row == null) return;

			if (row.MultipleMatching != true && row.DocumentMatched == true && row.MatchedToInvoice == true)
			{
				Details.Cache.SetValue<CABankTran.documentMatched>(row, false);
				Details.Cache.SetValue<CABankTran.matchedToExisting>(row, null);
				Details.Cache.SetValue<CABankTran.matchedToInvoice>(row, null);
				Details.Cache.SetValue<CABankTran.matchedToExpenseReceipt>(row, null);
				Details.Cache.SetValueExt<CABankTran.origModule>(row, null);
				Details.Cache.SetValue<CABankTran.chargeTypeID>(row, null);
				Details.Cache.SetValue<CABankTran.curyChargeAmt>(row, null);
				Details.Cache.SetValue<CABankTran.curyChargeTaxAmt>(row, null);
			}

			if (((bool?)e.OldValue) == true && row.MultipleMatching != true)
			{
				Details.Cache.SetValue<CABankTran.chargeTypeID>(row, null);
				Details.Cache.SetValue<CABankTran.curyChargeAmt>(row, null);
				Details.Cache.SetValue<CABankTran.curyChargeTaxAmt>(row, null);
			}
		}

		protected virtual void CABankTran_MultipleMatchingToPayments_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row == null) return;

			if (row.MultipleMatchingToPayments != true && row.MatchedToInvoice == false && row.MatchedToExpenseReceipt == false)
			{
				Details.Cache.SetValue<CABankTran.documentMatched>(row, false);
				Details.Cache.SetValue<CABankTran.matchReceiptsAndDisbursements>(row, false);
			}
		}

		protected virtual void CABankTran_MatchReceiptsAndDisbursements_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row == null) return;

			if (row.MultipleMatchingToPayments != true)
			{
				Details.Cache.SetValue<CABankTran.documentMatched>(row, false);
			}
		}

		protected virtual void CABankTran_OrigModule_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (string.IsNullOrEmpty((string)e.NewValue))
			{
				return;
			}
			CashAccount cashaccount = cashAccount.Select();
			bool clearingAccount = cashaccount?.ClearingAccount == true;
			string newModule = (string)e.NewValue;
			if (clearingAccount &&
				(newModule == GL.BatchModule.CA ||
				(newModule == GL.BatchModule.AP && ((CABankTran)e.Row).DrCr == DrCr.Credit)))
			{
				throw new PXSetPropertyException<CABankTran.origModule>(Messages.NotAllDocumentsAllowedForClearingAccount);
			}
		}
		protected virtual void CABankTran_OrigModule_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null && row.CreateDocument == true)
			{
				CashAccount cashaccount = cashAccount.Select();
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

		protected virtual void CABankTran_OrigModule_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null)
			{
				sender.SetDefaultExt<CABankTran.payeeBAccountID>(e.Row);
				sender.SetDefaultExt<CABankTran.entryTypeID>(e.Row);
			}
		}

		protected virtual void CABankTran_ChargeTypeID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row == null)
				return;

			sender.SetDefaultExt<CABankTran.chargeDrCr>(e.Row);
			if (row.ChargeTypeID == null)
			{
				sender.SetDefaultExt<CABankTran.curyChargeAmt>(e.Row);
			}
		}

		protected virtual void CABankTran_PayeeBAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null)
			{
				sender.SetDefaultExt<CABankTran.payeeLocationID>(e.Row);
				sender.SetDefaultExt<CABankTran.paymentMethodID>(e.Row);
				sender.SetDefaultExt<CABankTran.pMInstanceID>(e.Row);
				DetailMatchingInvoices.View.RequestRefresh();
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
				Details.Cache.SetValue<CABankTran.documentMatched>(row, false);
				Details.Cache.SetValue<CABankTran.matchedToExisting>(row, null);
				Details.Cache.SetValue<CABankTran.matchedToInvoice>(row, null);
				Details.Cache.SetValue<CABankTran.matchedToExpenseReceipt>(row, null);
				Details.Cache.SetValue<CABankTran.origModule>(row, null);
			}

			sender.SetDefaultExt<CABankTran.payeeLocationID>(e.Row);
			sender.SetDefaultExt<CABankTran.paymentMethodID>(e.Row);
			sender.SetDefaultExt<CABankTran.pMInstanceID>(e.Row);
			sender.SetDefaultExt<CABankTran.entryTypeID>(e.Row);
			DetailMatchingInvoices.View.RequestRefresh();
		}

		protected virtual void CABankTran_DocumentMatched_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row == null) return;
			if (row.DocumentMatched == true)
			{
				CABankTranMatch match = TranMatch.SelectSingle(row.TranID);
				if (match != null && !string.IsNullOrEmpty(match.DocRefNbr) && !IsMatchedToExpenseReceipt(match))
				{
					Details.Cache.SetValue<CABankTran.origModule>(row, match.DocModule);
					if (row.PayeeBAccountIDCopy == null)
					{
						Details.Cache.SetValue<CABankTran.payeeBAccountIDCopy>(row, match.ReferenceID);
						object refNbr = row.PayeeBAccountIDCopy;
						Details.Cache.RaiseFieldUpdating<CABankTran.payeeBAccountIDCopy>(row, ref refNbr);
						Details.Cache.RaiseFieldUpdated<CABankTran.payeeBAccountIDCopy>(row, null);
					}
					else
					{
						Details.Cache.SetDefaultExt<CABankTran.payeeLocationID>(row);
						Details.Cache.SetDefaultExt<CABankTran.paymentMethodID>(row);
						Details.Cache.SetDefaultExt<CABankTran.pMInstanceID>(row);
					}
				}

				Details.Cache.SetDefaultExt<CABankTran.matchReason>(row);
			}
		}
		protected virtual void CABankTran_EntryTypeID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row.OrigModule == GL.BatchModule.CA && row.EntryTypeID != (string)e.OldValue)
			{
				foreach (CABankTranDetail split in TranSplit.Select())
				{
					TranSplit.Delete(split);
				}
				if (!string.IsNullOrEmpty(row.EntryTypeID))
				{
					CABankTranDetail newSplit = new CABankTranDetail();
					TranSplit.Insert(newSplit);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<CABankTran, CABankTran.taxZoneID> e)
		{
			CABankTran row = e.Row;
			if (row.OrigModule == GL.BatchModule.CA && row.TaxZoneID != (string)e.OldValue && (string)e.OldValue != null)
			{
				foreach (CABankTranDetail split in TranSplit.Select())
				{
					TranSplit.Delete(split);
				}
				CABankTranDetail newSplit = new CABankTranDetail();
				TranSplit.Insert(newSplit);
			}
		}

		protected virtual void CABankTran_MatchingPaymentDate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null || e.NewValue == null)
			{
				return;
			}

			CABankTran row = (CABankTran)e.Row;
			DateTime newDate = (DateTime)e.NewValue;
			DateTime originalTranDate = (DateTime)row.TranDate;
			int? tranDaysBefore = ((CashAccount)cashAccount.Select())?.DisbursementTranDaysBefore;

			if (newDate > originalTranDate)
			{
				if (sender.RaiseExceptionHandling<CABankTran.matchingPaymentDate>(row, newDate, new PXSetPropertyException(Messages.TranDateIsLaterThanTransactionDate)))
				{
					throw new PXSetPropertyException(Messages.TranDateIsLaterThanTransactionDate);
				}
			}
		}

		protected virtual void CABankTran_EntryTypeID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row == null) return;

			if (row.OrigModule == GL.BatchModule.CA)
			{
				string defaultEntryType = GetUnrecognizedReceiptDefaultEntryType(row);

				if (string.IsNullOrEmpty(defaultEntryType))
				{
					defaultEntryType = GetDefaultCashAccountEntryType(row);
				}

				e.NewValue = defaultEntryType;
			}
			else
			{
				e.NewValue = null;
			}
			e.Cancel = true;
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

		protected virtual void CABankTran_PaymentMethodID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null)
			{
				sender.SetDefaultExt<CABankTran.pMInstanceID>(e.Row);
			}
		}

		protected virtual void CABankTran_PaymentMethodIDCopy_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null)
			{
				sender.SetDefaultExt<CABankTran.pMInstanceID>(e.Row);
			}
		}

		protected virtual void CABankTran_EntryTypeId_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row != null)
			{
				CAEntryType entryType = PXSelect<
					CAEntryType,
					Where<CAEntryType.entryTypeId, Equal<Required<CAEntryType.entryTypeId>>>>
					.Select(this, row.EntryTypeID);
				if (entryType != null)
				{
					row.DrCr = entryType.DrCr;
					if (entryType.UseToReclassifyPayments == true && row.CashAccountID.HasValue)
					{
						CashAccount availableAccount = PXSelect<
							CashAccount,
							Where<CashAccount.cashAccountID, NotEqual<Required<CashAccount.cashAccountID>>,
								And<CashAccount.curyID, Equal<Required<CashAccount.curyID>>>>>
							.SelectWindowed(sender.Graph, 0, 1, row.CashAccountID, row.CuryID);
						if (availableAccount == null)
						{
							sender.RaiseExceptionHandling<CABankTran.entryTypeID>(row, null, new PXSetPropertyException(Messages.EntryTypeRequiresCashAccountButNoOneIsConfigured, PXErrorLevel.Warning, row.CuryID));
						}
					}
				}

				if (row.CreateDocument == true)
				{
					sender.SetDefaultExt<CAAdj.taxCalcMode>(row);
					sender.SetDefaultExt<CAAdj.taxZoneID>(row);
				}
			}
		}

		protected virtual void CABankTran_MatchStatsInfo_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			if (row != null)
			{
				String message = null;
				if (row.DocumentMatched == true || row.CreateDocument == true)
				{
					if (row.MatchedToExisting == true && row.CreateDocument == false)
					{
						if (row.MatchedToInvoice == true)
						{
							message = PXMessages.LocalizeFormatNoPrefix(Messages.TransactionWillPayInvoice);
						}
						else if (row.MatchedToExpenseReceipt == true)
						{
							message = PXMessages.LocalizeFormatNoPrefix(Messages.TransactionMatchedToExistingExpenseReceipt);
						}
						else
						{
							message = PXMessages.LocalizeFormatNoPrefix(Messages.TransactionMatchedToExistingDocument);
						}
					}
					else
					{
						if (row.RuleID != null)
						{
							message = PXMessages.LocalizeFormatNoPrefix(Messages.TransactionWillCreateNewDocumentBasedOnRuleDefined);
						}
						else
						{
							message = PXMessages.LocalizeFormatNoPrefix(Messages.TransactionWillCreateNewDocument);
						}
					}
				}
				else
				{
					message = PXMessages.LocalizeFormatNoPrefix(Messages.TRansactionNotMatched);
				}
				e.ReturnValue = message;
			}
		}

		protected virtual void CABankTran_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CABankTranRowUpdated(this, sender, e, TranSplit, TranMatch, Adjustments, TranMatchCharge, MatchInvoiceProcess);
		}

		public static void CABankTranRowUpdated(CABankTransactionsMaint graph, PXCache sender, PXRowUpdatedEventArgs e, PXSelectBase<CABankTranDetail> tranSplit, PXSelectBase<CABankTranMatch> tranMatch, PXSelectBase<CABankTranAdjustment> adjustments, PXSelectBase<CABankTranMatch> tranMatchCharge, bool matchInvoiceProcess)
		{
			CABankTran row = (CABankTran)e.Row;
			CABankTran oldRow = (CABankTran)e.OldRow;

			if (row.CreateDocument == true)
			{
				if (oldRow.CreateDocument != true)
				{
					CurrencyInfoAttribute.SetDefaults<CABankTran.curyInfoID>(sender, row);
				}
				sender.SetValueExt<CABankTran.documentMatched>(row, ValidateTranFields(graph, sender, row, adjustments));
			}
			if (row.CreateDocument == false || (row.OrigModule != GL.BatchModule.CA && row.OrigModule != GL.BatchModule.EP))
			{
				foreach (CABankTranDetail split in tranSplit.Select())
				{
					tranSplit.Delete(split);
				}
			}
			if ((oldRow != null && oldRow.CreateDocument == true)
				&& (row.CreateDocument == false || (row.OrigModule != GL.BatchModule.AR
														&& row.OrigModule != GL.BatchModule.AP
														&& row.OrigModule != GL.BatchModule.EP)))
			{
				foreach (CABankTranAdjustment adj in adjustments.Select())
				{
					adjustments.Delete(adj);
				}
			}
			if (oldRow != null && oldRow.MultipleMatching == true
							&& row.MultipleMatching == false)
			{
				foreach (CABankTranMatch match in tranMatch.Select(row.TranID))
				{
					if (!String.IsNullOrEmpty(match.DocRefNbr) && !IsMatchedToExpenseReceipt(match))
					{
						tranMatch.Delete(match);
					}
				}
			}
			if (oldRow != null && row.MatchedToInvoice != true
				&& oldRow.MultipleMatchingToPayments == true && row.MultipleMatchingToPayments == false)
			{
				foreach (CABankTranMatch match in tranMatch.Select(row.TranID))
				{
					if (match.CATranID != null)
					{
						tranMatch.Delete(match);
					}
				}
			}
			if (oldRow != null && oldRow.MatchedToInvoice == true && row.PayeeBAccountID != oldRow.PayeeBAccountID)
			{
				if (row.PayeeBAccountID == null)
				{
					foreach (CABankTranMatch match in tranMatch.Select(row.TranID))
					{
						if (!String.IsNullOrEmpty(match.DocRefNbr) && !IsMatchedToExpenseReceipt(match))
						{
							tranMatch.Delete(match);
						}
					}
				}
				else
				{
					BAccount oldAccount = BAccount.PK.Find(graph, oldRow.PayeeBAccountID);
					if (oldAccount?.ParentBAccountID != row.PayeeBAccountIDCopy)
					{
						foreach (CABankTranMatch match in tranMatch.Select(row.TranID))
						{
							if (match.ReferenceID != row.PayeeBAccountIDCopy)
							{
								tranMatch.Delete(match);
							}
						}
					}
				}
			}

			ProcessChangeOnRowUpdated(row, oldRow, tranMatchCharge, matchInvoiceProcess);
		}

		//private void ProcessChangeOnRowUpdated(CABankTran row, CABankTran oldRow)
		private static void ProcessChangeOnRowUpdated(CABankTran row, CABankTran oldRow, PXSelectBase<CABankTranMatch> tranMatchCharge, bool matchInvoiceProcess)
		{
			if (matchInvoiceProcess == true)
			{
				return;
			}

			if (row.ChargeTypeID != oldRow.ChargeTypeID || (oldRow.CuryChargeAmt ?? 0m) != (row.CuryChargeAmt ?? 0m))
			{
				foreach (CABankTranMatch imatch in tranMatchCharge.Select(row.TranID))
				{
					tranMatchCharge.Delete(imatch);
				}

				var amount = (row.ChargeDrCr == row.DrCr ? 1 : -1) * (row.CuryChargeAmt ?? 0m);

				if (amount != 0m)
				{
				CABankTranMatch match = new CABankTranMatch()
				{
					TranID = row.TranID,
					MatchType = CABankTranMatch.matchType.Charge,
					TranType = row.TranType,
					CuryApplAmt = amount,
					CuryApplTaxableAmt = amount,
					IsCharge = true,
				};

				match = tranMatchCharge.Insert(match);
			}
		}
		}

		protected virtual void EnableTranFields(PXCache sender, CABankTran row)
		{
			bool matchedToCA = false;
			bool matchedToInv = false;
			bool matchedToReceipt = false;

			List<CABankTranMatch> matches = TranMatch.Select(row.TranID).RowCast<CABankTranMatch>().ToList();

			if (matches.Count != 0)
			{
				matchedToCA = matches.Any(match => (match.CATranID.HasValue && match.IsCharge != true) || match.DocType == CATranType.CABatch);
				matchedToReceipt = matches.Any(match => IsMatchedToExpenseReceipt(match));
				matchedToInv = matches.Any(match => !String.IsNullOrEmpty(match.DocRefNbr) && !IsMatchedToExpenseReceipt(match) && match.DocType != CATranType.CABatch);

				int matchesTypeCount = 0;

				if (matchedToCA)
					matchesTypeCount++;
				if (matchedToReceipt)
					matchesTypeCount++;
				if (matchedToInv)
					matchesTypeCount++;

				if (matchesTypeCount > 1)
				{
					throw new PXException(Messages.ErrorInMatchTable, row.TranID);
				}
			}

			bool needsPMInstance = false;
			if (row.OrigModule == BatchModule.AR)
			{
				var pm = (PaymentMethod)PXSelectorAttribute.Select<CABankTran.paymentMethodID>(sender, row);
				needsPMInstance = pm != null && pm.ARIsOnePerCustomer == false;
			}

			bool isMultipleMatchingToInv = row.MultipleMatching == true;
			bool isMultipleMatchingToPayment = row.MultipleMatchingToPayments == true;
			PXUIFieldAttribute.SetEnabled(sender, row, false);
			PXUIFieldAttribute.SetEnabled<CABankTran.multipleMatching>(sender, row, true);
			PXUIFieldAttribute.SetEnabled<CABankTran.multipleMatchingToPayments>(sender, row, true);
			PXUIFieldAttribute.SetEnabled<CABankTran.matchReceiptsAndDisbursements>(sender, row, isMultipleMatchingToPayment);
			PXUIFieldAttribute.SetEnabled<CABankTran.payeeBAccountIDCopy>(sender, row, !matchedToCA && !matchedToReceipt);
			PXUIFieldAttribute.SetVisible<CABankTran.payeeLocationIDCopy>(sender, row, matchedToInv);
			PXUIFieldAttribute.SetEnabled<CABankTran.payeeLocationIDCopy>(sender, row, matchedToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.paymentMethodIDCopy>(sender, row, matchedToInv);
			PXUIFieldAttribute.SetEnabled<CABankTran.paymentMethodIDCopy>(sender, row, matchedToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.pMInstanceIDCopy>(sender, row, matchedToInv && needsPMInstance);
			PXUIFieldAttribute.SetEnabled<CABankTran.pMInstanceIDCopy>(sender, row, matchedToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.curyTotalAmtCopy>(sender, row, matchedToInv || isMultipleMatchingToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.curyTotalAmtDisplay>(sender, row, matchedToCA || isMultipleMatchingToPayment);
			PXUIFieldAttribute.SetVisible<CABankTran.curyApplAmtMatchToInvoice>(sender, row, matchedToInv || isMultipleMatchingToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.curyApplAmtMatchToPayment>(sender, row, matchedToCA || isMultipleMatchingToPayment);
			PXUIFieldAttribute.SetVisible<CABankTran.curyUnappliedBalMatchToInvoice>(sender, row, matchedToInv || isMultipleMatchingToInv);
			PXUIFieldAttribute.SetVisible<CABankTran.curyUnappliedBalMatchToPayment>(sender, row, matchedToCA || isMultipleMatchingToPayment);
			PXUIFieldAttribute.SetVisible<CABankTran.chargeTypeID>(sender, row, row.MultipleMatching == true);
			PXUIFieldAttribute.SetEnabled<CABankTran.chargeTypeID>(sender, row, row.MultipleMatching == true);
			PXUIFieldAttribute.SetVisible<CABankTran.curyChargeAmt>(sender, row, row.MultipleMatching == true);
			PXUIFieldAttribute.SetEnabled<CABankTran.curyChargeAmt>(sender, row, row.MultipleMatching == true && row.ChargeTypeID != null);
			PXUIFieldAttribute.SetVisible<CABankTran.curyChargeTaxAmt>(sender, row, row.MultipleMatching == true);

			bool notMatched = !matchedToCA && !matchedToInv && !matchedToReceipt;

			PXUIFieldAttribute.SetEnabled<CABankTran.createDocument>(sender, row, notMatched);

			CAEntryType entryType = PXSelect<
				CAEntryType,
				Where<CAEntryType.entryTypeId, Equal<Required<CAEntryType.entryTypeId>>>>
				.Select(this, row.EntryTypeID);
			if (entryType != null)
			{
				bool isReclassification = entryType.UseToReclassifyPayments ?? false;

				PXUIFieldAttribute.SetEnabled<CABankTranDetail.accountID>(TranSplit.Cache, null, !isReclassification);
				PXUIFieldAttribute.SetEnabled<CABankTranDetail.subID>(TranSplit.Cache, null, !isReclassification);
				PXUIFieldAttribute.SetEnabled<CABankTranDetail.branchID>(TranSplit.Cache, null, !isReclassification);
				PXUIFieldAttribute.SetEnabled<CABankTranDetail.cashAccountID>(TranSplit.Cache, null, isReclassification);
				PXUIFieldAttribute.SetVisible<CABankTranDetail.cashAccountID>(TranSplit.Cache, null, isReclassification);
				TranSplit.AllowInsert = true;
			}
			else
			{
				TranSplit.AllowInsert = false;
			}

			PXUIFieldAttribute.SetEnabled<CABankTranAdjustment.adjdCuryRate>(Adjustments.Cache, null, row.OrigModule == GL.BatchModule.AR || row.OrigModule == GL.BatchModule.AP);
			EnableCreateTab(sender, row, needsPMInstance);
		}
		private void VerifyMatchingPaymentDate(PXCache sender, CABankTran row)
		{
			if (row.MatchingPaymentDate == null)
			{
				sender.RaiseExceptionHandling<CABankTran.matchingPaymentDate>(row, null, null);
				return;
			}

			DateTime matchingDate = (DateTime)row.MatchingPaymentDate;
			DateTime originalTranDate = (DateTime)row.TranDate;
			int? tranDaysBefore = ((CashAccount)cashAccount.Select()).DisbursementTranDaysBefore;

			if (matchingDate < originalTranDate.AddDays((double)-tranDaysBefore))
			{
				sender.RaiseExceptionHandling<CABankTran.matchingPaymentDate>(row, matchingDate, new PXSetPropertyException(Messages.TranDateIsMoreThanDaysBeforeBankTransactionDate, PXErrorLevel.Warning, tranDaysBefore));
			}
			else if (matchingDate == originalTranDate)
			{
				sender.RaiseExceptionHandling<CABankTran.matchingPaymentDate>(row, matchingDate, null);
			}
		}
		private void EnableCreateTab(PXCache sender, CABankTran row, bool needsPMInstance)
		{
			bool CreatingDocument = row != null && row.CreateDocument == true;

			bool ruleApplied = row != null && row.RuleApplied == true;

			bool isAR = row != null && row.OrigModule == GL.BatchModule.AR && CreatingDocument;
			bool isAP = row != null && row.OrigModule == GL.BatchModule.AP && CreatingDocument;
			bool isCA = row != null && row.OrigModule == GL.BatchModule.CA && CreatingDocument;
			bool isARorAP = isAR || isAP;
			bool noAdjustmentsYet = row?.HasAdjustments == false;
			bool isReceipt = row != null && row.DrCr == DrCr.Debit;

			PXUIFieldAttribute.SetVisible<CABankTran.ruleID>(sender, row, CreatingDocument && ruleApplied);
			PXUIFieldAttribute.SetVisible<CABankTran.origModule>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetEnabled<CABankTran.origModule>(sender, row, CreatingDocument && noAdjustmentsYet && row.RuleID == null);
			PXUIFieldAttribute.SetVisible<CABankTran.entryTypeID>(sender, row, isCA);
			PXUIFieldAttribute.SetEnabled<CABankTran.entryTypeID>(sender, row, isCA && !ruleApplied);
			PXUIFieldAttribute.SetEnabled<CABankTran.taxZoneID>(sender, row, isCA);
			PXUIFieldAttribute.SetEnabled<CABankTran.taxCalcMode>(sender, row, isCA);
			PXUIFieldAttribute.SetVisible<CABankTran.payeeBAccountID>(sender, row, isARorAP);
			PXUIFieldAttribute.SetEnabled<CABankTran.payeeBAccountID>(sender, row, isARorAP && noAdjustmentsYet);
			PXUIFieldAttribute.SetVisible<CABankTran.payeeLocationID>(sender, row, isARorAP);
			PXUIFieldAttribute.SetEnabled<CABankTran.payeeLocationID>(sender, row, isARorAP);
			PXUIFieldAttribute.SetVisible<CABankTran.paymentMethodID>(sender, row, isARorAP);
			PXUIFieldAttribute.SetEnabled<CABankTran.paymentMethodID>(sender, row, isARorAP);
			PXUIFieldAttribute.SetVisible<CABankTran.pMInstanceID>(sender, row, needsPMInstance);
			PXUIFieldAttribute.SetEnabled<CABankTran.pMInstanceID>(sender, row, isAR);
			PXUIFieldAttribute.SetVisible<CABankTran.invoiceInfo>(sender, row, isARorAP);
			PXUIFieldAttribute.SetEnabled<CABankTran.invoiceInfo>(sender, row, false);
			PXUIFieldAttribute.SetVisible<CABankTran.curyTotalAmt>(sender, row, isARorAP);
			PXUIFieldAttribute.SetVisible<CABankTran.curyDetailsWithTaxesTotal>(sender, row, isCA);
			PXUIFieldAttribute.SetVisible<CABankTran.curyApplAmt>(sender, row, isARorAP);
			PXUIFieldAttribute.SetVisible<CABankTran.curyUnappliedBal>(sender, row, isARorAP);
			PXUIFieldAttribute.SetVisible<CABankTran.curyWOAmt>(sender, row, isAR && isReceipt);
			PXUIFieldAttribute.SetVisible<CABankTranAdjustment.curyAdjgWOAmt>(Adjustments.Cache, null, isAR && isReceipt);
			PXUIFieldAttribute.SetVisible<CABankTranAdjustment.writeOffReasonCode>(Adjustments.Cache, null, isAR && isReceipt);
			PXUIFieldAttribute.SetVisible<CABankTran.curyApplAmtCA>(sender, row, isCA);
			PXUIFieldAttribute.SetVisible<CABankTran.curyUnappliedBalCA>(sender, row, isCA);
			PXUIFieldAttribute.SetVisible<CABankTran.curyTaxTotal>(sender, row, isCA);
			PXUIFieldAttribute.SetVisible<CABankTran.userDesc>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetEnabled<CABankTran.userDesc>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetVisible<GeneralInvoice.apExtRefNbr>(invoices.Cache, null, isAP);
			PXUIFieldAttribute.SetVisible<GeneralInvoice.arExtRefNbr>(invoices.Cache, null, isAR);
			PXUIFieldAttribute.SetVisible<GeneralInvoice.vendorBAccountID>(invoices.Cache, null, isAP);
			PXUIFieldAttribute.SetVisible<GeneralInvoice.customerBAccountID>(invoices.Cache, null, isAR);

			PXUIFieldAttribute.SetEnabled<CABankTran.matchingPaymentDate>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetVisible<CABankTran.matchingPaymentDate>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetEnabled<CABankTran.matchingfinPeriodID>(sender, row, CreatingDocument);
			PXUIFieldAttribute.SetVisible<CABankTran.matchingfinPeriodID>(sender, row, CreatingDocument);

			if (isAR && isReceipt)
				PXUIFieldAttribute.SetVisible<CABankTranAdjustment.curyAdjgWhTaxAmt>(Adjustments.Cache, null, false);

			TranSplit.View.AllowSelect = isCA;
			Adjustments.View.AllowSelect = isARorAP;
		}

		public static bool ValidateTranFields(CABankTransactionsMaint graph, PXCache sender, CABankTran row, PXSelectBase<CABankTranAdjustment> adjustments)
		{
			bool creatingDocument = row.CreateDocument == true;
			bool moduleCA = creatingDocument && row.OrigModule == GL.BatchModule.CA;
			bool moduleAR = creatingDocument && row.OrigModule == GL.BatchModule.AR;
			bool moduleAP = creatingDocument && row.OrigModule == GL.BatchModule.AP;
			bool matchedToInv = row.MatchedToInvoice == true;
			bool matchedToExpenseReceipt = row.MatchedToExpenseReceipt == true;
			bool matchedToExisting = row.MatchedToExisting == true;

			bool missingBAccount = (moduleAP || moduleAR) && row.BAccountID == null;
			bool missingLocation = (moduleAP || moduleAR) && row.LocationID == null;
			bool missingPaymentMethod = (moduleAP || moduleAR) && string.IsNullOrEmpty(row.PaymentMethodID);
			bool missingPaymentMethodInvoiceTab = row.MatchedToInvoice == true && String.IsNullOrEmpty(row.PaymentMethodIDCopy);
			bool missingEntryType = moduleCA && string.IsNullOrEmpty(row.EntryTypeID);
			bool unappliedBalAP = moduleAP && row.DrCr == DrCr.Debit && row.CuryUnappliedBal != null && row.CuryUnappliedBal > 0;
			bool unappliedBalCA = moduleCA && row.CuryUnappliedBalCA != null && row.CuryUnappliedBalCA != 0;
			bool unappliedBalMatchToInv = matchedToInv && row.CuryUnappliedBalMatch != null && row.CuryUnappliedBalMatch != 0;
			bool unappliedBalMatchToPayment = matchedToExisting && !matchedToInv && !matchedToExpenseReceipt && row.CuryUnappliedBalMatch != null && row.CuryUnappliedBalMatch != 0;
			bool missingPMInstance = false;
			if (row.PMInstanceID == null && moduleAR)
			{
				PaymentMethod pm = PXSelect<
					PaymentMethod,
					Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>
					.Select(graph, row.PaymentMethodID);
				missingPMInstance = pm != null && pm.IsAccountNumberRequired == true;
			}
			CABankTranAdjustment adj = null;
			string errorMessage = null;
			if (creatingDocument && row.InvoiceInfo != null)
			{
				if (row.HasAdjustments == true)
				{
					adj = adjustments.Search<CABankTranAdjustment.adjdRefNbr, CABankTranAdjustment.adjdModule>(row.InvoiceInfo, row.OrigModule);
				}
				else
				{
					string Module;
					try
					{
						object linkedInvoice = graph.FindInvoiceByInvoiceInfo(row, out Module);
					}
					catch (Exception ex)
					{
						errorMessage = ex.Message;
					}
				}
			}
			bool missingInvoice = creatingDocument && (row.InvoiceNotFound == true || (adj == null && row.InvoiceInfo != null));
			bool notExactAmount = adj != null && (adj.CuryAdjgAmt != (sender.Current as CABankTran).CuryTotalAmt || adj.CuryDocBal != decimal.Zero);

			CheckExternalTaxProviders(sender, row);
			RaiseOrHideError<CABankTran.entryTypeID>(sender, row, missingEntryType, Messages.EntryTypeIsRequiredToCreateCADocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.curyUnappliedBalCA>(sender, row, unappliedBalCA, Messages.AmountDiscrepancy, PXErrorLevel.RowWarning, row.CuryApplAmtCA, row.CuryTotalAmt);
			RaiseOrHideError<CABankTran.curyUnappliedBalMatchToInvoice>(sender, row, unappliedBalMatchToInv, Messages.MatchToInvoiceAmountDiscrepancy, PXErrorLevel.RowWarning, row.CuryApplAmtMatch, row.CuryTotalAmt);
			RaiseOrHideError<CABankTran.curyUnappliedBalMatchToPayment>(sender, row, unappliedBalMatchToPayment, Messages.MatchToInvoiceAmountDiscrepancy, PXErrorLevel.RowWarning, row.CuryApplAmtMatch, row.CuryTotalAmt);
			RaiseOrHideError<CABankTran.payeeBAccountID>(sender, row, missingBAccount, Messages.PayeeIsRequiredToCreateDocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.payeeLocationID>(sender, row, missingLocation, Messages.PayeeLocationIsRequiredToCreateDocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.curyUnappliedBal>(sender, row, unappliedBalAP, Messages.DocumentMustByAppliedInFullBeforeItMayBeCreated, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.paymentMethodID>(sender, row, missingPaymentMethod, Messages.PaymentMethodIsRequiredToCreateDocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.paymentMethodIDCopy>(sender, row, missingPaymentMethodInvoiceTab, Messages.PaymentMethodIsRequiredToCreateDocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.pMInstanceID>(sender, row, missingPMInstance, Messages.PaymentMethodIsRequiredToCreateDocument, PXErrorLevel.RowWarning);
			RaiseOrHideError<CABankTran.curyApplAmt>(sender, row, notExactAmount, Messages.NotExactAmount, PXErrorLevel.Warning);
			RaiseOrHideError<CABankTran.invoiceInfo>(sender, row, missingInvoice, row.InvoiceNotFound == true ? Messages.InvoiceNotFound : errorMessage ?? Messages.ApplicationremovedByUser, PXErrorLevel.Warning, row.InvoiceInfo);


			bool readyToProcess = true;
			sender.RaiseExceptionHandling<CABankTran.documentMatched>(row, row.DocumentMatched, null);
			Dictionary<string, string> errors = PXUIFieldAttribute.GetErrors(sender, row, PXErrorLevel.Error, PXErrorLevel.RowError);
			if (errors.Count != 0)
			{
				readyToProcess = false;
				sender.RaiseExceptionHandling<CABankTran.documentMatched>(row, row.DocumentMatched, new PXSetPropertyException(errors.Values.First(), PXErrorLevel.RowError));
			}
			else
			{
				Dictionary<string, string> rowWarnings = PXUIFieldAttribute.GetErrors(sender, row, PXErrorLevel.RowWarning);
				if (rowWarnings.Count != 0)
				{
					readyToProcess = false;
					sender.RaiseExceptionHandling<CABankTran.documentMatched>(row, row.DocumentMatched, new PXSetPropertyException(rowWarnings.Values.First(), PXErrorLevel.RowWarning));
				}
				else
				{
					Dictionary<string, string> warnings = PXUIFieldAttribute.GetErrors(sender, row, PXErrorLevel.Warning);
					if (warnings.Count != 0)
					{
						sender.RaiseExceptionHandling<CABankTran.documentMatched>(row, row.DocumentMatched, new PXSetPropertyException(warnings.Values.First(), PXErrorLevel.RowWarning));
					}
				}
			}
			return readyToProcess;
		}

		//protected virtual void CheckExternalTaxProviders(PXCache sender, CABankTran row)
		public static void CheckExternalTaxProviders(PXCache sender, CABankTran row)
		{
			TaxZone taxZone = SelectFrom<TaxZone>.Where<TaxZone.taxZoneID.IsEqual<@P.AsString>>.View.Select(sender.Graph, row.TaxZoneID);
			bool IsCABankTranExternalTaxProvider = taxZone?.IsExternal == true;
			bool IsChargeTypeExternalTaxProvider = false;

			if (row.ChargeTypeID != null)
			{
				string defaultTaxZoneID = ((CashAccountETDetail)SelectFrom<CashAccountETDetail>
															.Where<CashAccountETDetail.accountID.IsEqual<@P.AsInt>
															.And<CashAccountETDetail.entryTypeID.IsEqual<@P.AsString>>>
															.View
															.Select(sender.Graph, row.CashAccountID, row.ChargeTypeID))?.TaxZoneID;

				IsChargeTypeExternalTaxProvider = ((TaxZone)SelectFrom<TaxZone>
															.Where<TaxZone.taxZoneID.IsEqual<@P.AsString>>
															.View
															.Select(sender.Graph, defaultTaxZoneID))?.IsExternal == true;
			}

			RaiseOrHideError<CABankTran.taxZoneID>(sender, row, IsCABankTranExternalTaxProvider, Messages.ExternalTaxCannotBeCalculated, PXErrorLevel.RowError);
			RaiseOrHideError<CABankTran.curyTaxTotal>(sender, row, IsCABankTranExternalTaxProvider, Messages.ExternalTaxCannotBeCalculated, PXErrorLevel.RowError);
			RaiseOrHideError<CABankTran.chargeTypeID>(sender, row, IsChargeTypeExternalTaxProvider, Messages.ExternalTaxCannotBeCalculated, PXErrorLevel.RowError);
		}

		protected virtual void CABankTran_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			CABankTran adj = (CABankTran)e.Row;
			if (adj.CashAccountID == null)
			{
				sender.RaiseExceptionHandling<CABankTran.cashAccountID>(adj, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(CABankTran.cashAccountID)}]"));
			}

			var cashAccount = this.cashAccount.SelectSingle();
			var process = CAMatchProcess.PK.Find(this, cashAccount?.CashAccountID);
			if (process?.CashAccountID != null)
			{
				throw new PXRowPersistedException(typeof(Filter.cashAccountID).Name, cashAccount?.CashAccountID, Messages.CashAccountIsInMatchingProcessChangesWillBeLost, cashAccount.CashAccountCD);
			}
		}

		protected virtual void CABankTran_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTran row = e.Row as CABankTran;
			if (row == null) return;

			if (TranFilter.Current?.CashAccountID == null)
				return;

			ValidateRunningMatchingProcesses(sender, row);
			ValidateTranFields(this, sender, row, Adjustments);
			EnableTranFields(sender, row);
			VerifyMatchingPaymentDate(sender, row);

			bool allowMatchReceiptsAndDisbursements = row.MatchReceiptsAndDisbursements == true;

			PXUIFieldAttribute.SetVisible<CATranExt.curyTranAmtCalc>(DetailMatchesCA.Cache, null, allowMatchReceiptsAndDisbursements);
			PXUIFieldAttribute.SetVisible<CATranExt.curyTranAbsAmt>(DetailMatchesCA.Cache, null, !allowMatchReceiptsAndDisbursements);

			if (row.MatchedToExisting == null)
			{
				row.MatchedToExisting = TranMatch.Select(row.TranID).Count != 0;
				if (row.MatchedToExisting == true)
				{
					CABankTranMatch match = ((CABankTranMatch)TranMatch.SelectSingle(row.TranID));

					row.MatchedToExpenseReceipt = IsMatchedToExpenseReceipt(match);
					row.MatchedToInvoice = IsMatchedToInvoice(row, match);

					PXFormulaAttribute.CalcAggregate<CABankTranMatch.curyApplAmt>(TranMatch.Cache, e.Row);
				}
			}

			StatementsMatchingProto.SetDocTypeList(Adjustments.Cache, row);

			Dictionary<int, PXSetPropertyException> listMessages = PXLongOperation.GetCustomInfo(this.UID) as Dictionary<int, PXSetPropertyException>;
			TimeSpan timespan;
			Exception ex;
			PXLongRunStatus status = PXLongOperation.GetStatus(this.UID, out timespan, out ex);
			if ((status == PXLongRunStatus.Aborted || status == PXLongRunStatus.Completed) && listMessages != null)
			{
				int key = row.TranID.Value;
				if (listMessages.ContainsKey(key))
				{
					sender.RaiseExceptionHandling<CABankTran.documentMatched>(row, row.DocumentMatched, listMessages[key]);
				}
			}
		}

		private void ValidateRunningMatchingProcesses(PXCache sender, CABankTran row)
		{
			var process = CAMatchProcess.PK.Find(this, row.CashAccountID);
			PXSetPropertyException exception = null;

			if (process?.CashAccountID != null)
			{
				CashAccount cashAccount = CashAccount.PK.Find(this, row.CashAccountID);
				exception = new PXSetPropertyException(Messages.CashAccountIsInMatchingProcessChangesMayBeLost, PXErrorLevel.Warning, cashAccount.CashAccountCD);
			}

			sender.RaiseExceptionHandling<Filter.cashAccountID>(row, row.CashAccountID, exception);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXVendorCustomerSelectorAttribute))]
		[PXVendorCustomerSelector(typeof(CABankTran.origModule))]
		[PXRestrictor(typeof(
				Where<BAccountR.type, In3<BAccountType.customerType, BAccountType.combinedType, BAccountType.empCombinedType>,
					And<BAccountR.status, In3<
						CustomerStatus.active,
						CustomerStatus.oneTime,
						CustomerStatus.creditHold>,
				Or<BAccountR.type, In3<BAccountType.vendorType, BAccountType.employeeType, BAccountType.combinedType, BAccountType.empCombinedType>,
					And<BAccountR.vStatus, In3<
						VendorStatus.active,
						VendorStatus.oneTime,
						VendorStatus.holdPayments>>>>>),
					Messages.IncorrectBAccountStatus,
					typeof(CABankTran.payeeBAccountID))]
		protected virtual void CABankTran_PayeeBAccountID_CacheAttached(PXCache sender) { }

		private void FieldsDisableOnProcessing(PXLongRunStatus status)
		{
			bool noProcessing = status == PXLongRunStatus.NotExists;
			Details.Cache.AllowUpdate = noProcessing;
			DetailMatchesCA.Cache.AllowUpdate = noProcessing;
			DetailsForPaymentCreation.Cache.AllowUpdate = noProcessing;
			DetailMatchingInvoices.Cache.AllowUpdate = noProcessing;
			ExpenseReceiptDetailMatches.Cache.AllowUpdate = noProcessing;
			Adjustments.Cache.AllowInsert = noProcessing;
			Adjustments.Cache.AllowUpdate = noProcessing;
			Adjustments.Cache.AllowDelete = noProcessing;

			TranSplit.Cache.AllowInsert = noProcessing;
			TranSplit.Cache.AllowUpdate = noProcessing;
			TranSplit.Cache.AllowDelete = noProcessing;
			autoMatch.SetEnabled(noProcessing);
			processMatched.SetEnabled(noProcessing);
			matchSettingsPanel.SetEnabled(noProcessing);
			uploadFile.SetEnabled(noProcessing);
			clearMatch.SetEnabled(noProcessing);
			clearAllMatches.SetEnabled(noProcessing);
			hide.SetEnabled(noProcessing);
		}

		#endregion
		#region Filter Events
		protected virtual void Filter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			Filter row = e.Row as Filter;

			TimeSpan timespan;
			Exception ex;

			PXLongRunStatus status = PXLongOperation.GetStatus(this.UID, out timespan, out ex);

			PXUIFieldAttribute.SetEnabled(sender, null, status == PXLongRunStatus.NotExists);

			bool enableDetails = (status == PXLongRunStatus.NotExists) && row != null && row.CashAccountID.HasValue;
			if (!enableDetails)
			{
				PXUIFieldAttribute.SetEnabled(this.Details.Cache, null, false);
			}

			FieldsDisableOnProcessing(status);
			autoMatch.SetEnabled(row.CashAccountID != null);
			processMatched.SetEnabled(row.CashAccountID != null);
			matchSettingsPanel.SetEnabled(row.CashAccountID != null);
		}


		protected virtual void _(Events.FieldVerifying<Filter.cashAccountID> e)
		{
			if (e == null) return;

			CashAccount cashAccount = CashAccount.PK.Find(this, (int?)e.NewValue);
			if (cashAccount?.Active == false)
			{
				e.Cache.RaiseExceptionHandling<Filter.cashAccountID>(e.Row, e.NewValue,
					new PXSetPropertyException(Messages.CashAccountInactive, PXErrorLevel.RowError, cashAccount.CashAccountCD));
				e.Cancel = true;
			}
		}
		#endregion

		#region CashAccount Events

		protected virtual void _(Events.RowSelected<CashAccount> e)
		{
			if (e.Row == null)
				return;

			bool isCorpCard = TranFilter.Current.IsCorpCardCashAccount == true;

			PXUIFieldAttribute.SetVisible<CashAccount.curyDiffThreshold>(e.Cache, null, isCorpCard);
			PXUIFieldAttribute.SetVisible<CashAccount.amountWeight>(e.Cache, null, isCorpCard);
			PXUIFieldAttribute.SetVisible<CashAccount.ratioInRelevanceCalculationLabel>(e.Cache, null, isCorpCard);

			bool invoiceFilterByDate = e.Row.InvoiceFilterByDate == true;
			PXUIFieldAttribute.SetEnabled<CashAccount.daysBeforeInvoiceDiscountDate>(e.Cache, null, invoiceFilterByDate);
			PXUIFieldAttribute.SetEnabled<CashAccount.daysBeforeInvoiceDueDate>(e.Cache, null, invoiceFilterByDate);
			PXUIFieldAttribute.SetEnabled<CashAccount.daysAfterInvoiceDueDate>(e.Cache, null, invoiceFilterByDate);

			PXUIFieldAttribute.SetRequired<CashAccount.daysBeforeInvoiceDiscountDate>(e.Cache, invoiceFilterByDate);
			PXUIFieldAttribute.SetRequired<CashAccount.daysBeforeInvoiceDueDate>(e.Cache, invoiceFilterByDate);
			PXUIFieldAttribute.SetRequired<CashAccount.daysAfterInvoiceDueDate>(e.Cache, invoiceFilterByDate);
		}

		protected virtual void _(Events.RowUpdated<CashAccount> e)
		{
			if (!e.Cache.ObjectsEqual<
					CashAccount.receiptTranDaysBefore,
					CashAccount.receiptTranDaysAfter,
					CashAccount.disbursementTranDaysBefore,
					CashAccount.disbursementTranDaysAfter,
					CashAccount.allowMatchingCreditMemo,
					CashAccount.refNbrCompareWeight,
					CashAccount.matchThreshold,
					CashAccount.relativeMatchThreshold>(e.OldRow, e.Row)
				|| !e.Cache.ObjectsEqual<
					CashAccount.dateCompareWeight,
					CashAccount.payeeCompareWeight,
					CashAccount.dateMeanOffset,
					CashAccount.dateSigma,
					CashAccount.skipVoided,
					CashAccount.curyDiffThreshold,
					CashAccount.amountWeight,
					CashAccount.emptyRefNbrMatching>(e.OldRow, e.Row)
				|| !e.Cache.ObjectsEqual<
					CashAccount.invoiceFilterByCashAccount,
					CashAccount.invoiceFilterByDate,
					CashAccount.daysAfterInvoiceDueDate,
					CashAccount.daysBeforeInvoiceDiscountDate,
					CashAccount.daysBeforeInvoiceDueDate,
					CashAccount.invoiceRefNbrCompareWeight,
					CashAccount.invoiceDateCompareWeight,
					CashAccount.invoicePayeeCompareWeight>(e.OldRow, e.Row)
				|| !e.Cache.ObjectsEqual<
					CashAccount.averagePaymentDelay,
					CashAccount.invoiceDateSigma>(e.OldRow, e.Row))
			{
				e.Row.MatchSettingsPerAccount = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<CashAccount.invoiceFilterByDate> e)
		{
			CashAccount row = (CashAccount)e.Row;

			if (row == null) return;

			if (row.InvoiceFilterByDate != true)
			{
				row.DaysBeforeInvoiceDiscountDate = 0;
				row.DaysBeforeInvoiceDueDate = 0;
				row.DaysAfterInvoiceDueDate = 0;
			}
		}
		#endregion
		#region CATranExt Events

		protected virtual void CATranExt_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CATranExt row = e.Row as CATranExt;

			if (row == null)
				return;

			PXUIFieldAttribute.SetVisible<CATranExt.finPeriodID>(sender, null, false);
			PXUIFieldAttribute.SetEnabled(sender, row, false);
			PXUIFieldAttribute.SetEnabled<CATranExt.isMatched>(sender, row, true);
		}

		protected virtual void CATranExt_IsMatched_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CATranExt row = e.Row as CATranExt;

			if ((bool?)e.NewValue == true &&
					((Details.Current.DocumentMatched == true
						&& (Details.Current.MultipleMatchingToPayments != true || Details.Current.MatchedToInvoice == true))
						|| Details.Current.CreateDocument == true))
			{
				throw new PXSetPropertyException(Messages.AnotherOptionChosen, PXErrorLevel.RowWarning);
			}
		}

		protected virtual void CATranExt_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CATranExt row = e.Row as CATranExt;
			CABankTran currentTran = Details.Current;

			if (!sender.ObjectsEqual<CATranExt.isMatched>(e.Row, e.OldRow))
			{
				if (row.IsMatched == true)
				{
					CABankTranMatch match = null;

					if (row.OrigTranType == CATranType.CABatch)
					{
						match = new CABankTranMatch()
						{
							TranID = currentTran.TranID,
							TranType = currentTran.TranType,
							DocModule = BatchModule.AP,
							DocType = CATranType.CABatch,
							DocRefNbr = row.OrigRefNbr,
							ReferenceID = row.ReferenceID,
							CuryApplAmt = (currentTran.DrCr == DrCr.Debit ? 1 : -1) * row.CuryTranAmt
						};
					}
					else
					{
						match = new CABankTranMatch()
						{
							TranID = currentTran.TranID,
							TranType = currentTran.TranType,
							CATranID = row.TranID,
							ReferenceID = row.ReferenceID,
							CuryApplAmt = (currentTran.DrCr == DrCr.Debit ? 1 : -1) * row.CuryTranAmt
						};
					}

					TranMatch.Insert(match);
				}
				else
				{
					foreach (var match in TranMatch.Select(currentTran.TranID).RowCast<CABankTranMatch>()
												.Where(item => item.CATranID == row.TranID || (row.OrigTranType == CATranType.CABatch
												&& item.DocModule == BatchModule.AP && item.DocType == CATranType.CABatch && item.DocRefNbr == row.OrigRefNbr)))
					{
						TranMatch.Delete(match);
					}
				}

				bool documentMatched = TranMatch.Select(currentTran.TranID).Any_();

				Details.Cache.SetValueExt<CABankTran.documentMatched>(currentTran, documentMatched);
				Details.Cache.SetStatus(Details.Current, PXEntryStatus.Updated);
			}

			sender.IsDirty = false;
		}

		protected virtual void CATranExt_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			e.Cancel = true;
		}

		#endregion
		#region CurrencyInfo
		protected virtual void CurrencyInfo_CuryID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				CashAccount cacct = cashAccount.Select();

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
				CashAccount cacct = cashAccount.Select();
				if (cacct != null && !string.IsNullOrEmpty(cacct.CuryRateTypeID))
				{
					e.NewValue = cacct.CuryRateTypeID;
					e.Cancel = true;
				}
				else
				{
					CMSetup setup = CMSetup.Current;
					CABankTran det = Details.Current;

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
			if (Details.Current != null)
			{
				e.NewValue = Details.Current.TranDate;
				e.Cancel = true;
			}
		}

		#endregion

		#region CABankTranDetail Events
		protected virtual void CABankTranDetail_AccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;
			CABankTran tran = Details.Current;

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
			CABankTran adj = Details.Current;
			CABankTranDetail split = e.Row as CABankTranDetail;

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
			CABankTran adj = Details.Current;

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

			TaxZone taxZone = Taxzone.Select();

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
			CABankTran adj = Details.Current;

			if (adj == null || adj.EntryTypeID == null || split == null)
				return;

			e.NewValue = GetDefaultAccountValues(this, adj.CashAccountID, adj.EntryTypeID).SubID;
			e.Cancel = e.NewValue != null;
		}

		protected virtual void CABankTranDetail_TranDesc_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			CABankTranDetail split = e.Row as CABankTranDetail;

			CABankTran tran = Details.Current;

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

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[CABankTranTax(typeof(CABankTran), typeof(CABankTax), typeof(CABankTaxTran), typeof(CABankTran.taxCalcMode),
			CuryOrigDocAmt = typeof(CABankTran.curyTranAmt), CuryLineTotal = typeof(CABankTran.curyApplAmtCA))]
		protected virtual void _(Events.CacheAttached<CABankTranDetail.taxCategoryID> e) { }

		protected virtual void _(Events.FieldDefaulting<CABankTaxTran, CABankTaxTran.taxType> e)
		{
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			if (e.Row != null && Details.Current != null)
			{
				if (Details.Current.DrCr == CADrCr.CACredit)
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
			CABankTran tran = Details.Current;
			CABankTranDetail newSplit = e.Row as CABankTranDetail;
			CurrencyInfoAttribute.SetDefaults<CABankTranDetail.curyInfoID>(sender, newSplit);
			newSplit.Qty = 1.0m;
			var tranAmt = (tran.DrCr == DrCr.Debit ? decimal.One : decimal.MinusOne) * tran.CuryTranAmt;

			decimal? taxAmt = 0;
			foreach (PXResult<CABankTaxTran, Tax> item in TaxTrans.Select())
			{
				CABankTaxTran taxTran = (CABankTaxTran)item;
				Tax tax = (Tax)item;

				bool isNotInclusive = (!PXAccess.FeatureInstalled<FeaturesSet.netGrossEntryMode>() || tran.TaxCalcMode == TaxCalculationMode.TaxSetting) &&
					tax.TaxCalcLevel != CSTaxCalcLevel.Inclusive || tran.TaxCalcMode == TaxCalculationMode.Net;

				if (isNotInclusive)
				{
					taxAmt += taxTran.CuryTaxAmt + taxTran.CuryExpenseAmt;
				}
			}

			decimal? balanceLeft = tranAmt - tran.CuryApplAmtCA - taxAmt;
			newSplit.CuryUnitPrice = balanceLeft > 0 ? balanceLeft : 0;
			sender.SetValueExt<CABankTranDetail.curyTranAmt>(newSplit, newSplit.Qty * newSplit.CuryUnitPrice);
			newSplit.TranDesc = tran.UserDesc;
			CATranDetailHelper.VerifyOffsetCashAccount(sender, newSplit, TranSplit.Current?.CashAccountID);
		}

		protected virtual void CABankTranDetail_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			CATranDetailHelper.OnCATranDetailRowUpdatingEvent(sender, e);
			//TODO: Move the CATranDetailHelper.UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice to the CABankTransactionsMaint currency extention
			CATranDetailHelper.UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(sender, e.Row as ICATranDetail, e.NewRow as ICATranDetail);
			if (CATranDetailHelper.VerifyOffsetCashAccount(sender, e.NewRow as CABankTranDetail, TranFilter.Current?.CashAccountID))
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

			Details.Cache.RaiseExceptionHandling<CABankTran.createDocument>(tranRow, tranRow.CreateDocument, exception);
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

		#endregion

		#region CABankTranMatch
		protected virtual void CABankTranMatch_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			CABankTranMatch row = e.Row as CABankTranMatch;
			CABankTran currtran = PXSelect<
				CABankTran,
				Where<CABankTran.tranID, Equal<Required<CABankTran.tranID>>>>
				.Select(this, row.TranID);
			Details.Cache.SetValue<CABankTran.matchedToExisting>(currtran, null);
			Details.Cache.SetValue<CABankTran.matchedToInvoice>(currtran, null);
			Details.Cache.SetValue<CABankTran.matchedToExpenseReceipt>(currtran, null);
		}

		protected virtual void CABankTranMatch_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			CABankTranMatch row = e.Row as CABankTranMatch;
			CABankTran currtran = PXSelect<
				CABankTran,
				Where<CABankTran.tranID, Equal<Required<CABankTran.tranID>>>>
				.Select(this, row.TranID);
			Details.Cache.SetValue<CABankTran.matchedToExisting>(currtran, null);
			Details.Cache.SetValue<CABankTran.matchedToInvoice>(currtran, null);
			Details.Cache.SetValue<CABankTran.matchedToExpenseReceipt>(currtran, null);
		}

		protected virtual void CABankTranMatch_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Operation != PXDBOperation.Insert)
			{
				return;
			}

			var row = (CABankTranMatch)e.Row;

			bool isAlreadyMatched = PXSelectReadonly2<
				CABankTranMatch,
				InnerJoin<CABankTran,
					On<CABankTran.tranID, Equal<CABankTranMatch.tranID>>>,
				Where<CABankTranMatch.cATranID, Equal<Required<CABankTranMatch.cATranID>>,
					And<CABankTran.tranType, Equal<Current<CABankTran.tranType>>>>>
				.Select(this, row.CATranID)
				.AsEnumerable()
				.Any(bankTran => Caches["CABankTranMatch"].GetStatus((CABankTranMatch)bankTran) != PXEntryStatus.Deleted);

			if (isAlreadyMatched)
			{
				CABankTran tranRow = PXSelect<
					CABankTran,
					Where<CABankTran.tranID, Equal<Required<CABankTranMatch.tranID>>>>
					.Select(this, row.TranID);

				Details.Cache.RaiseExceptionHandling<CABankTran.extRefNbr>(tranRow, tranRow.ExtRefNbr,
					new PXSetPropertyException(Messages.DocumentIsAlreadyMatched, PXErrorLevel.RowError));
			}
		}
		#endregion

		#region Light.BAccount
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = CR.Messages.BusinessAccount, Visibility = PXUIVisibility.Visible)]
		protected virtual void _(Events.CacheAttached<Light.BAccount.acctCD> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = CR.Messages.BAccountName, Visibility = PXUIVisibility.Visible)]
		protected virtual void _(Events.CacheAttached<Light.BAccount.acctName> e) { }
		#endregion
		#region Processing
		public static void DoProcessing(IEnumerable<CABankTran> list, Dictionary<int, PXSetPropertyException> listMessages)
		{
			CABankTransactionsMaint graph = PXGraph.CreateInstance<CABankTransactionsMaint>();
			bool hasErrors = false;
			List<Batch> toPost = new List<Batch>();
			foreach (CABankTran iDet in list)
			{
				graph.Clear();
				if (iDet.DocumentMatched == false) continue;
				CABankTran det = (CABankTran)graph.Details.Cache.CreateCopy(iDet);
				graph.Details.Current = det;
				graph.DetailsForPaymentCreation.Current = det;
				var filter = graph.TranFilter.Current;
				graph.TranFilter.SetValueExt<Filter.cashAccountID>(filter, det.CashAccountID);
				graph.TranFilter.SetValueExt<Filter.tranType>(filter, det.TranType);

				try
				{
					//assuming that all matches are of one type
					CABankTranMatch match = graph.TranMatch.SelectSingle(det.TranID);
					using (PXTransactionScope ts = new PXTransactionScope())
					{
						graph.ValidateBeforeProcessing(iDet);

						if (match != null && IsMatchedToExpenseReceipt(match))
						{
							graph.MatchExpenseReceipt(iDet, match);
						}
						else
						{
							if (match != null && !String.IsNullOrEmpty(match.DocRefNbr) && match.DocType != CATranType.CABatch)
							{
								det = graph.MatchInvoices(det);

								if (det.ChargeTypeID != null)
								{
									det = graph.CreateChargesProc(det);
								}
							}
							else
							{
								det.ChargeTypeID = null;
								det = (CABankTran)graph.Details.Update(det);
							}

							if (det.CreateDocument == true)
							{
								graph.CreateDocumentProc(det, false);
							}

							graph.MatchCATran(det, toPost);
						}

						det = (CABankTran)graph.Details.Cache.CreateCopy(det);
						det.Processed = true;
						det.DocumentMatched = true;
						det.RuleID = iDet.RuleID;
						graph.Details.Update(det);
						graph.Save.Press();
						ts.Complete(graph);
					}
					listMessages[det.TranID.Value] = new PXSetPropertyException(Messages.DeatsilProcess, PXErrorLevel.RowInfo);
				}
				catch (PXOuterException e)
				{
					listMessages[det.TranID.Value] = new PXSetPropertyException(e, PXErrorLevel.RowError, e.Message + " " + e.InnerMessages[0]);
					hasErrors = true;
				}
				catch (Exception e)
				{
					listMessages[det.TranID.Value] = new PXSetPropertyException(e, PXErrorLevel.RowError, e.Message);
					hasErrors = true;
				}
			}
			List<Batch> postFailedList = new List<Batch>();
			if (toPost.Count > 0)
			{
				PostGraph pg = PXGraph.CreateInstance<PostGraph>();
				foreach (Batch iBatch in toPost)
				{
					try
					{
						//if (rg.AutoPost)
						{
							pg.Clear();
							pg.PostBatchProc(iBatch);
						}
					}
					catch (Exception)
					{
						postFailedList.Add(iBatch);
					}
				}
			}
			if (postFailedList.Count > 0)
			{
				throw new PXException(GL.Messages.PostingOfSomeOfTheIncludedDocumentsFailed, postFailedList.Count, toPost.Count);
			}
			if (hasErrors)
			{
				throw new PXException(Messages.ErrorsInProcessing);
			}
		}

		public static void DoProcessing(IEnumerable<CABankTran> list)
		{
			Dictionary<int, PXSetPropertyException> listMessages = new Dictionary<int, PXSetPropertyException>();
			PXLongOperation.SetCustomInfo(listMessages, list.Cast<object>().ToArray());
			DoProcessing(list, listMessages);
		}

		protected virtual void VerifyBeforeMatchInvoices(CABankTran det)
		{
			if ((det.CuryUnappliedBalMatch ?? 0) != 0)
			{
				throw new PXSetPropertyException(Messages.MatchToInvoiceAmountDiscrepancy, det.CuryApplAmtMatch, det.CuryTotalAmt);
			}
		}

		protected virtual void VerifyBeforeMatchCATran(CABankTran det)
		{
			if ((det.CuryUnappliedBalMatch ?? 0) != 0 && det.DocumentMatched == true)
			{
				throw new PXSetPropertyException(Messages.MatchToPaymentAmountDiscrepancy, det.CuryApplAmtMatch, det.CuryTotalAmt);
			}
		}

		protected virtual void ValidateBeforeProcessing(CABankTran det)
		{
			
		}

		protected virtual CABankTran MatchInvoices(CABankTran det)
		{
			VerifyBeforeMatchInvoices(det);

			var matches = TranMatch.Select(det.TranID).RowCast<CABankTranMatch>().ToList();

			var BAccountID = det.BAccountID;
			var LocationID = det.LocationID;
			var OrigModule = det.OrigModule;
			var PaymentMethodID = det.PaymentMethodID;
			var ChargeTypeID = det.ChargeTypeID;
			var CuryChargeAmt = det.CuryChargeAmt;

			using (new MatchInvoiceContext(this))
			{
				ClearMatchProc(det);
				Details.Cache.SetValue<CABankTran.createDocument>(det, true);
				det = Details.Update(det);

				ClearRule(Details.Cache, det);
				foreach (CABankTranAdjustment adj in Adjustments.Select(det.TranID))
				{
					Adjustments.Delete(adj);
				}

				Details.Cache.SetValue<CABankTran.origModule>(det, OrigModule);
				Details.Cache.SetValue<CABankTran.payeeBAccountID>(det, BAccountID);
				Details.Cache.SetValue<CABankTran.payeeLocationID>(det, LocationID);
				Details.Cache.SetValue<CABankTran.paymentMethodID>(det, PaymentMethodID);
				Details.Cache.SetValue<CABankTran.chargeTypeID>(det, ChargeTypeID);
				Details.Cache.SetValue<CABankTran.curyChargeAmt>(det, CuryChargeAmt);
				det = Details.Update(det);
			}

			foreach (CABankTranMatch match in matches)
			{
				try
				{
					CABankTranAdjustment adj = new CABankTranAdjustment()
					{
						TranID = det.TranID
					};

					adj = Adjustments.Insert(adj);

					adj.AdjdDocType = match.DocType;
					adj.AdjdRefNbr = match.DocRefNbr;
					adj.AdjdModule = match.DocModule;
					adj.CuryAdjgAmt = Math.Abs(match.CuryApplAmt ?? 0m);

					Adjustments.Update(adj);
				}
				catch
				{
					throw new PXSetPropertyException(Messages.CouldNotAddApplication, match.DocRefNbr);
				}
			}

			return det;
		}

		protected virtual void MatchCATran(CABankTran det, List<Batch> externalPostList)
		{
			VerifyBeforeMatchCATran(det);

			bool matchFound = false;
			foreach (CABankTranMatch match in TranMatch.Select(det.TranID))
			{
				matchFound = ProcessCABankTranMatches(det, externalPostList, match);
			}

			foreach (CABankTranMatch match in TranMatchCharge.Select(det.TranID))
			{
				matchFound = ProcessCABankTranMatches(det, externalPostList, match);
			}

			if (!matchFound)
				throw new PXException(Messages.MatchNotFound, det.TranID);
		}

		private bool ProcessCABankTranMatches(CABankTran det, List<Batch> externalPostList, CABankTranMatch match)
		{
			bool matchFound;
			if (match == null)
				throw new PXException(Messages.MatchNotFound, det.TranID);

			matchFound = true;

			if (match.DocModule == BatchModule.AP && match.DocType == CATranType.CABatch)
			{
				bool cleared = true;
				foreach (CATranExt tran in PXSelectJoin<
					CATranExt,
					InnerJoin<CABatchDetail,
						On<CATranExt.origTranType, Equal<CABatchDetail.origDocType>,
						And<CATranExt.origRefNbr, Equal<CABatchDetail.origRefNbr>,
						And<CATranExt.origModule, Equal<CABatchDetail.origModule>>>>>,
					Where<CABatchDetail.batchNbr, Equal<Required<CABatchDetail.batchNbr>>>>
					.Select(this, match.DocRefNbr))
				{
					if (ProcessCATran(det, externalPostList, tran.TranID, false) != true)
					{
						cleared = false;
					}
				}

				if (cleared == true)
				{
					PXDatabase.Update<CABatch>(new PXDataFieldAssign<CABatch.cleared>(true),
												new PXDataFieldAssign<CABatch.clearDate>(det.TranDate),
												   new PXDataFieldRestrict<CABatch.batchNbr>(PXDbType.VarChar, 15, match.DocRefNbr, PXComp.EQ));
				}
				CABatch batch = PXSelectReadonly<
					CABatch,
					Where<CABatch.batchNbr, Equal<Required<CABatch.batchNbr>>>>
					.Select(this, match.DocRefNbr);
				if (batch.Released != true)
				{
					CABatchEntry batchEntryGraph = PXGraph.CreateInstance<CABatchEntry>();
					batchEntryGraph.Document.Current = batch;
					batchEntryGraph.SelectTimeStamp();
					batchEntryGraph.release.Press();
				}
			}
			else
			{
				ProcessCATran(det, externalPostList, match.CATranID);
			}

			return matchFound;
		}

		private bool ProcessCATran(CABankTran det, List<Batch> externalPostList, Int64? tranID, bool checkAmt = true)
		{
			Func<CATranExt> getCATran = () => (CATranExt)PXSelectReadonly<
				CATranExt,
				Where<CATranExt.tranID, Equal<Required<CATranExt.tranID>>>>
				.Select(this, tranID);
			CATranExt tran = getCATran();
			if (tran != null)
			{
				if ((tran.CuryTranAmt != det.CuryTranAmt && (det.MultipleMatchingToPayments != true || (det.CuryUnappliedBalMatch ?? 0) != 0)) && checkAmt)
				{
					throw new PXException(Messages.AmountDiscrepancy, tran.CuryTranAmt, det.CuryTranAmt);
				}

				TryToSetPaymentDateToBankDate(this, det, tran);

				if (tran.Released != true)
				{
					PXGraph searchGraph = null;
					//errors when we cannot release?
					switch (tran.OrigModule)
					{
						case GL.BatchModule.AP:
							if (CASetup.Current.ReleaseAP == true)
							{
								CATrxRelease.ReleaseCATran(tran, ref searchGraph, externalPostList);
							}
							break;
						case GL.BatchModule.AR:
							if (CASetup.Current.ReleaseAR == true)
							{
								CATrxRelease.ReleaseCATran(tran, ref searchGraph, externalPostList);
							}
							break;
						case GL.BatchModule.CA:
							CATrxRelease.ReleaseCATran(tran, ref searchGraph, externalPostList);
							break;
						default:
							throw new Exception(Messages.ThisDocTypeNotAvailableForRelease);
					}
				}
				Caches[typeof(CATranExt)].ClearQueryCache();
				tran = getCATran();
				if (tran == null)
				{
					throw new PXException(Messages.ProcessCannotBeCompleted);
				}

				if (tran.Released == true && tran.Cleared == false)
				{
					this.SelectTimeStamp();
					StatementsMatchingProto.UpdateSourceDoc(this, tran, det.TranDate);
				}
				return tran.Cleared ?? false;
			}
			else
			{
				throw new PXException(Messages.CATranNotFound);
			}
		}

		protected virtual bool TryToSetPaymentDateToBankDate(PXGraph graph, CABankTran bankTransaction, CATranExt caTransaction)
		{
			if (caTransaction.Released != false ||
			    bankTransaction.TranDate == null ||
			    bankTransaction.TranDate == caTransaction.TranDate) return false;

			switch (caTransaction.OrigModule)
			{
				case GL.BatchModule.AP:
					if (caTransaction.OrigTranType != GLTranType.GLEntry)
					{
						var payment = APPayment.PK.Find(graph, caTransaction.OrigTranType, caTransaction.OrigRefNbr);
						var pmt = PaymentMethod.PK.Find(graph, payment.PaymentMethodID);

						if (pmt?.PaymentDateToBankDate != true) return false;

						var paymentGraph = PXGraph.CreateInstance<APPaymentEntry>();
						payment = paymentGraph.Document.Search<APPayment.refNbr>(caTransaction.OrigRefNbr, caTransaction.OrigTranType);
						paymentGraph.Document.Current = payment;

						payment.AdjDate = bankTransaction.TranDate;
						payment.DocDate = bankTransaction.TranDate;
						paymentGraph.Document.Update(payment);

						foreach (APAdjust adjustment in paymentGraph.Adjustments_Raw.Select())
						{
							adjustment.AdjgDocDate = bankTransaction.TranDate;
							paymentGraph.Adjustments_Raw.Update(adjustment);
						}

						paymentGraph.Persist();
						return true;
					}

					break;
				case GL.BatchModule.AR:
					if (caTransaction.OrigTranType != GLTranType.GLEntry)
					{
						var payment = ARPayment.PK.Find(graph, caTransaction.OrigTranType, caTransaction.OrigRefNbr);
						var pmt = PaymentMethod.PK.Find(graph, payment.PaymentMethodID);

						if (pmt?.PaymentDateToBankDate != true) return false;

						var paymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
						payment = paymentGraph.Document.Search<ARPayment.refNbr>(caTransaction.OrigRefNbr, caTransaction.OrigTranType);
						paymentGraph.Document.Current = payment;

						payment.AdjDate = bankTransaction.TranDate;
						payment.DocDate = bankTransaction.TranDate;
						paymentGraph.Document.Update(payment);

						foreach (ARAdjust adjustment in paymentGraph.Adjustments_Raw.Select())
						{
							adjustment.AdjgDocDate = bankTransaction.TranDate;
							paymentGraph.Adjustments_Raw.Update(adjustment);
						}

						paymentGraph.Persist();
						return true;
					}

					break;
			}

			return false;
		}

		public void MatchExpenseReceipt(CABankTran bankTran, CABankTranMatch match)
		{
			EPExpenseClaimDetails receipt = PXSelect<EPExpenseClaimDetails,
													Where<EPExpenseClaimDetails.claimDetailCD, Equal<Required<EPExpenseClaimDetails.claimDetailCD>>>>
													.Select(this, match.DocRefNbr);
			this.SelectTimeStamp();

			//concurrency matching and release
			ExpenseReceipts.Update(receipt);

			if (receipt.Released != true)
				return;

			CATran caTran = null;

			if (receipt.PaidWith == EPExpenseClaimDetails.paidWith.CardCompanyExpense)
			{
				caTran = PXSelect<CATran,
							Where<CATran.origModule, Equal<BatchModule.moduleAP>,
								And<CATran.origTranType, Equal<Required<CATran.origTranType>>,
								And<CATran.origRefNbr, Equal<Required<CATran.origRefNbr>>>>>>
							.Select(this, receipt.APDocType, receipt.APRefNbr);
			}
			else if (receipt.PaidWith == EPExpenseClaimDetails.paidWith.CardPersonalExpense)
			{
				caTran =
					PXSelectJoin<CATran,
						InnerJoin<GLTran,
							On<CATran.tranID, Equal<GLTran.cATranID>>>,
						Where<GLTran.module, Equal<BatchModule.moduleAP>,
								And<GLTran.tranType, Equal<Required<GLTran.tranType>>,
								And<GLTran.refNbr, Equal<Required<GLTran.refNbr>>,
								And<GLTran.tranLineNbr, Equal<Required<GLTran.tranLineNbr>>>>>>>
						.Select(this, receipt.APDocType, receipt.APRefNbr, receipt.APLineNbr);
			}
			else
			{
				throw new InvalidOperationException();
			}

			if (caTran == null)
				return;

			if (caTran.Released == true && caTran.Cleared == false)
			{
				if (receipt.PaidWith == EPExpenseClaimDetails.paidWith.CardCompanyExpense)
				{
					StatementsMatchingProto.UpdateSourceDoc(this, caTran, bankTran.TranDate);
				}
				else
				{
					caTran.Cleared = true;
					caTran.ClearDate = bankTran.TranDate ?? caTran.TranDate;

					Caches[typeof(CATran)].Update(caTran);
				}
			}

			match.CATranID = caTran.TranID;

			TranMatch.Update(match);
		}

		protected virtual void ValidateDataForDocumentCreation(CABankTran aRow)
		{
			if (TranMatch.Select(aRow.TranID).Where(t => ((CABankTranMatch)t).IsCharge != true).Count() != 0)
			{
				throw new PXSetPropertyException(Messages.DocumentIsAlreadyCreatedForThisDetail);
			}
			if (aRow.BAccountID == null && aRow.OrigModule != GL.BatchModule.CA)
			{
				throw new PXSetPropertyException(Messages.PayeeIsRequiredToCreateDocument);
			}
			if (aRow.OrigModule == GL.BatchModule.CA && aRow.EntryTypeID == null)
			{
				throw new PXRowPersistingException(typeof(CABankTranDetail).Name, null, Messages.EntryTypeIsRequiredToCreateCADocument);
			}
			if (aRow.LocationID == null && aRow.OrigModule != GL.BatchModule.CA)
			{
				throw new PXSetPropertyException(Messages.PayeeLocationIsRequiredToCreateDocument);
			}

			if (aRow.OrigModule == GL.BatchModule.AR)
			{
				if (string.IsNullOrEmpty(aRow.PaymentMethodID))
				{
					throw new PXSetPropertyException(Messages.PaymentMethodIsRequiredToCreateDocument);
				}
				if (aRow.PMInstanceID == null)
				{
					PaymentMethod pm = PXSelect<
						PaymentMethod,
						Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>
						.Select(this, aRow.PaymentMethodID);
					if (pm != null && pm.IsAccountNumberRequired == true)
					{
						throw new PXSetPropertyException(Messages.PaymentMethodIsRequiredToCreateDocument);
					}
				}
			}
			if (aRow.OrigModule == GL.BatchModule.AP && string.IsNullOrEmpty(aRow.PaymentMethodID))
			{
				throw new PXSetPropertyException(Messages.PaymentMethodIsRequiredToCreateDocument);
			}
		}

		protected virtual void ValidateDataForChargeCreation(CABankTran aRow)
		{
			if (aRow.ChargeTypeID == null)
			{
				throw new PXSetPropertyException(Messages.ToCreateChargeSpecifyChargeType);
			}
		}

		protected virtual object FindInvoiceByInvoiceInfo(CABankTran aRow, out string Module)
		{
			return StatementsMatchingProto.FindInvoiceByInvoiceInfo(this, aRow, out Module);
		}

		protected virtual void CreateDocumentProc(CABankTran aRow, bool doPersist)
		{
			CATran result = null;
			PXCache sender = this.Details.Cache;
			ValidateDataForDocumentCreation(aRow);
			CurrencyInfo curyInfo = PXSelect<
				CurrencyInfo,
				Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>
				.Select(this, aRow.CuryInfoID);

			if (aRow.CuryTaxRoundDiff != 0)
			{
				throw new PXException(Messages.CannotEditTaxAmtOnBankTran);
			}

			if (aRow.OrigModule == GL.BatchModule.AR)
			{
				List<ARAdjust> adjustmentsAR = new List<ARAdjust>();

				foreach (CABankTranAdjustment adj in Adjustments.Select(aRow.TranID))
				{
					if (adj.PaymentsByLinesAllowed == true)
					{
						foreach (ARTran tran in
								PXSelect<ARTran,
									Where<ARTran.tranType, Equal<Current<CABankTranAdjustment.adjdDocType>>,
										And<ARTran.refNbr, Equal<Current<CABankTranAdjustment.adjdRefNbr>>,
										And<ARTran.curyTranBal, NotEqual<Zero>>>>,
									OrderBy<Desc<ARTran.curyTranBal>>>
									.SelectMultiBound(this, new object[] { adj }))
						{
							ARAdjust adjAR = new ARAdjust();
							adjAR.AdjdRefNbr = adj.AdjdRefNbr;
							adjAR.AdjdDocType = adj.AdjdDocType;
							adjAR.AdjdLineNbr = tran.LineNbr;
							adjustmentsAR.Add(adjAR);
						}
					}
					else
					{
						ARAdjust adjAR = new ARAdjust();
						adjAR.AdjdRefNbr = adj.AdjdRefNbr;
						adjAR.AdjdDocType = adj.AdjdDocType;
						adjAR.CuryAdjgAmt = adj.CuryAdjgAmt;
						adjAR.CuryAdjgDiscAmt = adj.CuryAdjgDiscAmt;
						adjAR.AdjdCuryRate = adj.AdjdCuryRate;
						adjAR.WOBal = adj.WhTaxBal;
						adjAR.AdjWOAmt = adj.AdjWhTaxAmt;
						adjAR.CuryAdjdWOAmt = adj.CuryAdjdWhTaxAmt;
						adjAR.CuryAdjgWOAmt = adj.CuryAdjgWhTaxAmt;
						adjAR.CuryWOBal = adj.CuryWhTaxBal;
						adjAR.WriteOffReasonCode = adj.WriteOffReasonCode;
						adjustmentsAR.Add(adjAR);
					}
				}
				bool OnHold = (this.CASetup.Current.ReleaseAR == false);
				result = AddARTransaction(aRow, curyInfo, adjustmentsAR, OnHold);
			}

			if (aRow.OrigModule == GL.BatchModule.AP)
			{
				List<ICADocAdjust> adjustments = new List<ICADocAdjust>();

				foreach (CABankTranAdjustment adj in Adjustments.Select(aRow.TranID))
				{
					adjustments.Add(adj);
				}
				bool OnHold = (this.CASetup.Current.ReleaseAP == false);
				result = AddAPTransaction(aRow, curyInfo, adjustments, OnHold);
			}

			if (aRow.OrigModule == GL.BatchModule.CA)
			{
				List<CASplit> splits = new List<CASplit>();
				foreach (PXResult<CABankTranDetail> res in TranSplit.Select(aRow.TranID, aRow.TranType))
				{
					CABankTranDetail det = (CABankTranDetail)res;
					CASplit split = new CASplit();
					split.LineNbr = det.LineNbr;
					split.AccountID = det.AccountID;
					split.BranchID = det.BranchID;
					split.CashAccountID = det.CashAccountID;
					split.CuryTranAmt = det.CuryTranAmt;
					split.CuryUnitPrice = det.CuryUnitPrice;
					split.InventoryID = det.InventoryID;
					split.NonBillable = det.NonBillable;
					split.NoteID = det.NoteID;
					split.ProjectID = det.ProjectID;
					split.Qty = det.Qty;
					split.SubID = det.SubID;
					split.TaskID = det.TaskID;
					split.CostCodeID = det.CostCodeID;
					split.TranDesc = det.TranDesc;
					split.TaxCategoryID = det.TaxCategoryID;
					splits.Add(split);
				}

				List<CATaxTran> caTaxTrans = new List<CATaxTran>();
				foreach (PXResult<CABankTaxTran> res in TaxTrans.Select())
				{
					CABankTaxTran bankTaxTran = (CABankTaxTran)res;
					CATaxTran caTaxTran = new CATaxTran();
					caTaxTran.BranchID = bankTaxTran.BranchID;
					caTaxTran.VendorID = bankTaxTran.VendorID;
					caTaxTran.AccountID = bankTaxTran.AccountID;
					caTaxTran.TaxPeriodID = bankTaxTran.TaxPeriodID;
					caTaxTran.FinPeriodID = bankTaxTran.FinPeriodID;
					caTaxTran.FinDate = bankTaxTran.FinDate;
					caTaxTran.Module = bankTaxTran.Module;
					caTaxTran.TranType = bankTaxTran.Module;
					caTaxTran.Released = bankTaxTran.Released;
					caTaxTran.Voided = bankTaxTran.Voided;
					caTaxTran.TaxID = bankTaxTran.TaxID;
					caTaxTran.TaxRate = bankTaxTran.TaxRate;
					caTaxTran.CuryOrigTaxableAmt = bankTaxTran.CuryOrigTaxableAmt;
					caTaxTran.OrigTaxableAmt = bankTaxTran.OrigTaxableAmt;
					caTaxTran.CuryTaxableAmt = bankTaxTran.CuryTaxableAmt;
					caTaxTran.TaxableAmt = bankTaxTran.TaxableAmt;
					caTaxTran.CuryExemptedAmt = bankTaxTran.CuryExemptedAmt;
					caTaxTran.ExemptedAmt = bankTaxTran.ExemptedAmt;
					caTaxTran.CuryTaxAmt = bankTaxTran.CuryTaxAmt;
					caTaxTran.TaxAmt = bankTaxTran.TaxAmt;
					caTaxTran.BAccountID = bankTaxTran.BAccountID;
					caTaxTran.CuryTaxAmtSumm = bankTaxTran.CuryTaxAmtSumm;
					caTaxTran.TaxAmtSumm = bankTaxTran.TaxAmtSumm;
					caTaxTran.NonDeductibleTaxRate = bankTaxTran.NonDeductibleTaxRate;
					caTaxTran.CuryExpenseAmt = bankTaxTran.CuryExpenseAmt;
					caTaxTran.ExpenseAmt = bankTaxTran.ExpenseAmt;
					caTaxTran.CuryID = bankTaxTran.CuryID;
					caTaxTran.TaxUOM = bankTaxTran.TaxUOM;
					caTaxTran.TaxableQty = bankTaxTran.TaxableQty;
					caTaxTran.TaxBucketID = bankTaxTran.TaxBucketID;
					caTaxTran.TaxType = bankTaxTran.TaxType;
					caTaxTran.TaxZoneID = bankTaxTran.TaxZoneID;
					caTaxTrans.Add(caTaxTran);
				}

				if (splits.Count > 0)
				{
					result = AddCATransaction(aRow, curyInfo, splits, caTaxTrans, false);
				}
				else
				{
					throw new PXRowPersistingException(typeof(CABankTranDetail).Name, null, Messages.UnableToProcessWithoutDetails);
				}
			}

			if (result != null)
			{
				CABankTranMatch match = new CABankTranMatch()
				{
					TranID = aRow.TranID,
					TranType = aRow.TranType,
					CATranID = result.TranID,
					CuryApplAmt = (aRow.DrCr == DrCr.Debit ? 1 : -1) * result.CuryTranAmt
				};
				TranMatch.Insert(match);
				aRow.CreateDocument = false;
				sender.Update(aRow);
			}

			if (doPersist)
				this.Save.Press();
		}

		protected virtual CABankTran CreateChargesProc(CABankTran aRow)
		{
			CATran tran = null;
			PXCache sender = this.Details.Cache;
			ValidateDataForChargeCreation(aRow);
			decimal? curyTaxableAmount = null;
			string taxCategotyID = null;

			foreach (CABankTranMatch match in TranMatchCharge.Select(aRow.TranID))
			{
				curyTaxableAmount = match.CuryApplAmt;
				taxCategotyID = match.TaxCategoryID;
				TranMatch.Delete(match);
			}

			CurrencyInfo curyInfo = PXSelect<
				CurrencyInfo,
				Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>
				.Select(this, aRow.CuryInfoID);

			tran = AddChargeTransaction(aRow, curyInfo, curyTaxableAmount, taxCategotyID);

			if (tran != null)
			{
				CABankTranMatch match = new CABankTranMatch()
				{
					TranID = aRow.TranID,
					MatchType = CABankTranMatch.matchType.Charge,
					TranType = aRow.TranType,
					CATranID = tran.TranID,
					CuryApplAmt = (aRow.ChargeDrCr == aRow.DrCr ? 1 : -1) * aRow.CuryChargeAmt,
				};

				match = TranMatch.Insert(match);

				aRow.MultipleMatchingToPayments = true;
				aRow = (CABankTran)sender.Update(aRow);
			}

			return aRow;
		}

		protected virtual CATran AddARTransaction(ICADocSource parameters, CurrencyInfo aCuryInfo, IEnumerable<ICADocAdjust> aAdjustments, bool aOnHold)
		{
			PaymentReclassifyProcess.CheckARTransaction(parameters);
			return PaymentReclassifyProcess.AddARTransaction(parameters, aCuryInfo, aAdjustments, aOnHold);
		}
		protected virtual CATran AddARTransaction(ICADocSource parameters, CurrencyInfo aCuryInfo, IEnumerable<ARAdjust> aAdjustments, bool aOnHold)
		{
			PaymentReclassifyProcess.CheckARTransaction(parameters);
			return PaymentReclassifyProcess.AddARTransaction(parameters, aCuryInfo, aAdjustments, aOnHold);
		}

		protected virtual CATran AddAPTransaction(ICADocSource parameters, CurrencyInfo aCuryInfo, IList<ICADocAdjust> aAdjustments, bool aOnHold)
		{
			PaymentReclassifyProcess.CheckAPTransaction(parameters);
			return PaymentReclassifyProcess.AddAPTransaction(parameters, aCuryInfo, aAdjustments, aOnHold);
		}

		protected virtual CATran AddCATransaction(ICADocWithTaxesSource parameters, CurrencyInfo aCuryInfo, IEnumerable<CASplit> splits, IEnumerable<CATaxTran> taxTrans, bool IsTransferExpense)
		{
			CheckCATransaction(parameters, CASetup.Current);
			return AddCATransaction(this, parameters, aCuryInfo, splits, taxTrans, IsTransferExpense);
		}

		protected virtual CATran AddChargeTransaction(CABankTran bankTran, CurrencyInfo aCuryInfo, decimal? amount, string taxCategotyID)
		{
			CheckChargeTransaction(bankTran, CASetup.Current);
			return AddChargeTransaction(this, bankTran, aCuryInfo, amount, taxCategotyID);
		}

		protected virtual bool IsARInvoiceSearchNeeded(CABankTran aRow)
		{
			return (aRow.OrigModule == GL.BatchModule.AR && String.IsNullOrEmpty(aRow.InvoiceInfo) == false);
		}

		protected virtual bool IsAPInvoiceSearchNeeded(CABankTran aRow)
		{
			return (aRow.OrigModule == GL.BatchModule.AP && String.IsNullOrEmpty(aRow.InvoiceInfo) == false);
		}
		#endregion

		#region Rules Matching
		protected virtual bool AttemptApplyRules(CABankTran transaction, bool applyHiding)
		{
			if (transaction == null || transaction.RuleID != null) return false;

			foreach (CABankTranRule rule in Rules.Select())
			{
				if ((applyHiding == true || rule.Action != RuleAction.HideTransaction)
					&& CheckRuleMatches(transaction, rule))
				{
					try
					{
						ApplyRule(transaction, rule);
						Details.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetError<CABankTran.ruleID>(Details.Cache, transaction, null);

						return true;
					}
					catch (PXException)
					{
						Details.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetWarning<CABankTran.ruleID>(Details.Cache, transaction, Messages.BankRuleFailedToApply);
						ResetTranFields(Details.Cache, transaction);
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
				Details.Cache.SetValueExt<CABankTran.origModule>(transaction, rule.DocumentModule);
				Details.Cache.SetValueExt<CABankTran.entryTypeID>(transaction, rule.DocumentEntryTypeID);
			}
			else if (rule.Action == RuleAction.HideTransaction)
			{
				transaction.CreateDocument = false;
				transaction.DocumentMatched = false;
				transaction.Hidden = true;
				transaction.Processed = true;
			}

			Details.Cache.SetValue<CABankTran.ruleID>(transaction, rule.RuleID);
		}

		public virtual void ApplyRule(CABankTranRule rule)
		{
			PXCache cache = this.Details.Cache;
			IEnumerable<CABankTran> tranList = UnMatchedDetails.Select().RowCast<CABankTran>();
			ApplyRule(cache, tranList, rule);
		}
		public virtual void ApplyRule(PXCache aUpdateCache, IEnumerable<CABankTran> aRows, CABankTranRule rule)
		{
			//this.ClearCachedMatches();

			foreach (CABankTran row in aRows)
			{
				if (row.CreateDocument == true && (row.OrigModule != BatchModule.CA || row.EntryTypeID != null)) continue;

				if (CheckRuleMatches(row, rule))
				{
					aUpdateCache.Current = row;
					CABankTran transaction = aUpdateCache.CreateCopy(row) as CABankTran;
					try
					{
						if (transaction.CreateDocument == true)
						{
							transaction.CreateDocument = false;
							transaction = aUpdateCache.Update(transaction) as CABankTran;
						}
						transaction.CreateDocument = true;
						ApplyRule(transaction, rule);
						Details.Cache.SetValueExt<CABankTran.documentMatched>(transaction, transaction.CreateDocument == true && ValidateTranFields(this, aUpdateCache, transaction, Adjustments));
						aUpdateCache.Update(transaction);

						Details.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetError<CABankTran.ruleID>(aUpdateCache, transaction, null);
					}
					catch (PXException)
					{
						Details.Cache.RaiseExceptionHandling<CABankTran.entryTypeID>(transaction, transaction.EntryTypeID, null);
						PXUIFieldAttribute.SetWarning<CABankTran.ruleID>(aUpdateCache, transaction, Messages.BankRuleFailedToApply);
						ResetTranFields(aUpdateCache, transaction);
					}
				}
			}
		}
		#endregion
		#region ModuleTranTypeSelector to CATranExt
		protected virtual void CATranExt_OrigTranType_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CATranExt_CuryTranAmtCalc_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			CABankTran bankTran = Details.Current;
			CATranExt row = (CATranExt)e.Row;
			if (Details == null || row == null)
				return;

			bool needRevert = bankTran.CuryTranAmt <= 0m;
			e.ReturnValue = needRevert ? -1 * row.CuryTranAmt.Value : row.CuryTranAmt.Value;
		}
		#endregion

		public PXAction<CABankTran> ViewTaxDetails;
		[PXUIField(DisplayName = "View Taxes", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true, Visible = false)]
		[PXLookupButton]
		protected virtual IEnumerable viewTaxDetails(PXAdapter adapter)
		{
			TaxTrans.AskExt(true);
			return adapter.Get();
		}

		[PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CashAccount.acctSettingsAllowed> e) { }

		[PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), "PersistingCheck", PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CashAccount.pTInstancesAllowed> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "DisplayName", CR.Messages.BAccountName)]
		protected virtual void BAccountR_AcctName_CacheAttached(PXCache sender) { }

		[Branch(null, typeof(Search2<Branch.branchID,
					InnerJoin<GL.DAC.Organization,
						On<Branch.organizationID, Equal<GL.DAC.Organization.organizationID>>>,
					Where2<MatchWithBranch<Branch.branchID>,
						And<Branch.baseCuryID.IsEqual<Filter.baseCuryID.FromCurrent>>>>))]
		protected virtual void _(Events.CacheAttached<CABankTranDetail.branchID> e) { }

		public static void CheckCATransaction(ICADocSource parameters, CASetup setup)
		{
			if (parameters.OrigModule == GL.BatchModule.CA)
			{
				if (parameters.CashAccountID == null)
				{
					throw new PXRowPersistingException(typeof(AddTrxFilter.cashAccountID).Name, null, ErrorMessages.FieldIsEmpty, typeof(AddTrxFilter.cashAccountID).Name);
				}

				if (string.IsNullOrEmpty(parameters.EntryTypeID))
				{
					throw new PXRowPersistingException(typeof(AddTrxFilter.entryTypeID).Name, null, ErrorMessages.FieldIsEmpty, typeof(AddTrxFilter.entryTypeID).Name);
				}

				if (string.IsNullOrEmpty(parameters.ExtRefNbr) && setup.RequireExtRefNbr == true)
				{
					throw new PXRowPersistingException(typeof(AddTrxFilter.extRefNbr).Name, null, ErrorMessages.FieldIsEmpty, typeof(AddTrxFilter.extRefNbr).Name);
				}
			}
		}

		public static void CheckChargeTransaction(CABankTran bankTran, CASetup setup)
		{
			if (bankTran.OrigModule == GL.BatchModule.CA)
			{
				if (bankTran.CashAccountID == null)
				{
					throw new PXRowPersistingException(typeof(CABankTran.cashAccountID).Name, null, ErrorMessages.FieldIsEmpty, typeof(CABankTran.cashAccountID).Name);
				}

				if (string.IsNullOrEmpty(bankTran.ChargeTypeID))
				{
					throw new PXRowPersistingException(typeof(CABankTran.entryTypeID).Name, null, ErrorMessages.FieldIsEmpty, typeof(CABankTran.entryTypeID).Name);
				}

				if (string.IsNullOrEmpty(bankTran.ExtRefNbr) && setup.RequireExtRefNbr == true)
				{
					throw new PXRowPersistingException(typeof(CABankTran.extRefNbr).Name, null, ErrorMessages.FieldIsEmpty, typeof(CABankTran.extRefNbr).Name);
				}
			}
		}

		public static CATran AddCATransaction(PXGraph graph, ICADocWithTaxesSource parameters, CurrencyInfo aCuryInfo, IEnumerable<CASplit> splits, IEnumerable<CATaxTran> taxTrans, bool IsTransferExpense)
		{
			if (parameters.OrigModule == GL.BatchModule.CA)
			{
				CATranEntry te = PXGraph.CreateInstance<CATranEntry>();
				var teMultiCurrency = te.GetExtension<MultiCurrency.CATranEntryMultiCurrency>();
				CashAccount cashacct = (CashAccount)PXSelect<
					CashAccount,
					Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>
					.Select(graph, parameters.CashAccountID);

				CM.Extensions.CurrencyInfo refInfo = CM.Extensions.CurrencyInfo.GetEX(aCuryInfo);
				refInfo.CuryInfoID = aCuryInfo.CuryInfoID;
				if (refInfo == null)
				{
					refInfo = PXSelectReadonly<
						CM.Extensions.CurrencyInfo,
						Where<CM.Extensions.CurrencyInfo.curyInfoID, Equal<Required<CM.Extensions.CurrencyInfo.curyInfoID>>>>
						.Select(te, parameters.CuryInfoID);
				}
				if (refInfo != null)
				{
					foreach (CurrencyInfo info in PXSelect<
						CurrencyInfo,
						Where<CurrencyInfo.curyInfoID, Equal<Current<CAAdj.curyInfoID>>>>
						.Select(te))
					{
						CM.Extensions.CurrencyInfo new_info = PXCache<CM.Extensions.CurrencyInfo>.CreateCopy(refInfo);
						new_info.CuryInfoID = info.CuryInfoID;
						teMultiCurrency.currencyinfo.Cache.Update(new_info);
					}
				}
				else if ((cashacct != null) && (cashacct.CuryRateTypeID != null))
				{
					refInfo = new CM.Extensions.CurrencyInfo();
					refInfo.CuryID = cashacct.CuryID;
					refInfo.CuryRateTypeID = cashacct.CuryRateTypeID;
					refInfo = teMultiCurrency.currencyinfo.Insert(refInfo);
				}

				CAAdj adj = new CAAdj();
				adj.AdjTranType = (IsTransferExpense ? CATranType.CATransferExp : CATranType.CAAdjustment);
				if (IsTransferExpense)
				{
					adj.TransferNbr = (graph as CashTransferEntry).Transfer.Current.TransferNbr;
				}
				adj.CashAccountID = parameters.CashAccountID;
				adj.CuryID = parameters.CuryID;
				adj.CuryInfoID = refInfo.CuryInfoID;
				adj.DrCr = parameters.DrCr;
				adj.ExtRefNbr = parameters.ExtRefNbr;
				adj.Released = false;
				adj.Cleared = parameters.Cleared;
				adj.TranDate = parameters.MatchingPaymentDate;
				adj.FinPeriodID = parameters.FinPeriodID;
				adj.TranDesc = parameters.TranDesc;
				adj.EntryTypeID = parameters.EntryTypeID;
				adj.CuryControlAmt = parameters.CuryOrigDocAmt;
				adj.CABankTranRefNoteID = parameters.NoteID;
				adj.Hold = true;
				adj.TaxZoneID = parameters.TaxZoneID;
				adj.TaxCalcMode = parameters.TaxCalcMode;
				adj = te.CAAdjRecords.Insert(adj);

				if (splits == null)
				{
					CASplit split = new CASplit();
					split.AdjTranType = adj.AdjTranType;
					split.CuryInfoID = refInfo.CuryInfoID;
					split.Qty = (decimal)1.0;
					split.CuryUnitPrice = parameters.CuryOrigDocAmt;
					split.CuryTranAmt = parameters.CuryOrigDocAmt;
					split.TranDesc = parameters.TranDesc;
					te.CASplitRecords.Insert(split);
				}
				else
				{
					foreach (CASplit split in splits)
					{
						split.AdjTranType = adj.AdjTranType;
						split.AdjRefNbr = adj.RefNbr;
						te.CASplitRecords.Insert(split);
					}
				}

				Dictionary<string, CATaxTran> appliedTaxes = taxTrans.ToDictionary(x => x.TaxID);

				foreach (CATaxTran taxTran in te.Taxes.Select())
				{
					if (appliedTaxes.ContainsKey(taxTran.TaxID))
					{
						if (!Equals(taxTran.CuryTaxAmt, appliedTaxes[taxTran.TaxID].CuryTaxAmt))
						{
							taxTran.CuryTaxAmt = appliedTaxes[taxTran.TaxID].CuryTaxAmt;
							te.Taxes.Update(taxTran);
						}
					}
					else
					{
						te.Taxes.Delete(taxTran);
					}
				}

				var tranAmt = (parameters.DrCr == DrCr.Debit ? decimal.One : decimal.MinusOne) * parameters.CuryTranAmt;

				adj.CuryTaxAmt = parameters.CuryTaxTotal;
				adj.CuryTaxTotal = parameters.CuryTaxTotal;
				adj.CuryOrigDocAmt = tranAmt;
				adj.CuryTranAmt = tranAmt;
				adj.Hold = false;
				adj = te.CAAdjRecords.Update(adj);
				te.releaseFromHold.Press();
				te.Save.Press();
				adj = (CAAdj)te.Caches[typeof(CAAdj)].Current;
				return (CATran)PXSelect<
					CATran,
					Where<CATran.tranID, Equal<Required<CAAdj.tranID>>>>
					.Select(te, adj.TranID);
			}
			return null;
		}

		public static CATran AddChargeTransaction(PXGraph graph, ICADocSource bankTran, CurrencyInfo aCuryInfo, decimal? amount, string taxCategotyID)
		{
			CATranEntry te = PXGraph.CreateInstance<CATranEntry>();
			var teMultiCurrency = te.GetExtension<MultiCurrency.CATranEntryMultiCurrency>();
			CashAccount cashacct = (CashAccount)PXSelect<
				CashAccount,
				Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>
				.Select(graph, bankTran.CashAccountID);

			CM.Extensions.CurrencyInfo refInfo = CM.Extensions.CurrencyInfo.GetEX(aCuryInfo);
			if (refInfo == null)
			{
				refInfo = PXSelectReadonly<
						CM.Extensions.CurrencyInfo,
						Where<CM.Extensions.CurrencyInfo.curyInfoID, Equal<Required<CM.Extensions.CurrencyInfo.curyInfoID>>>>
						.Select(te, bankTran.CuryInfoID);
			}
			if (refInfo != null)
			{
				foreach (CurrencyInfo info in PXSelect<
					CurrencyInfo,
					Where<CurrencyInfo.curyInfoID, Equal<Current<CAAdj.curyInfoID>>>>
					.Select(te))
				{
					CM.Extensions.CurrencyInfo new_info = PXCache<CM.Extensions.CurrencyInfo>.CreateCopy(refInfo);
					new_info.CuryInfoID = info.CuryInfoID;
					teMultiCurrency.currencyinfo.Cache.Update(new_info);
				}
			}
			else if ((cashacct != null) && (cashacct.CuryRateTypeID != null))
			{
				refInfo = new CM.Extensions.CurrencyInfo();
				refInfo.CuryID = cashacct.CuryID;
				refInfo.CuryRateTypeID = cashacct.CuryRateTypeID;
				refInfo = teMultiCurrency.currencyinfo.Insert(refInfo);
			}

			CAAdj adj = new CAAdj();
			adj.AdjTranType = CATranType.CAAdjustment;

			adj.CashAccountID = bankTran.CashAccountID;
			adj.CuryID = bankTran.CuryID;
			adj.DrCr = bankTran.DrCr;
			adj.ExtRefNbr = bankTran.ExtRefNbr;
			adj.Released = false;
			adj.Cleared = bankTran.Cleared;
			adj.TranDate = bankTran.MatchingPaymentDate;
			adj.FinPeriodID = ((ICADocSource)bankTran).FinPeriodID;
			adj.TranDesc = bankTran.TranDesc;
			adj.EntryTypeID = bankTran.ChargeTypeID;
			adj.CuryControlAmt = bankTran.CuryChargeAmt;
			adj.CABankTranRefNoteID = bankTran.NoteID;
			adj.Hold = true;
			adj.TaxZoneID = bankTran.ChargeTaxZoneID;
			adj.TaxCalcMode = bankTran.ChargeTaxCalcMode;
			adj = te.CAAdjRecords.Insert(adj);

			CASplit split = new CASplit();
			split.AdjTranType = adj.AdjTranType;
			split.Qty = (decimal)1.0;
			split.CuryUnitPrice = (bankTran.ChargeDrCr == bankTran.DrCr ? 1 : -1) * amount ?? bankTran.CuryChargeAmt;
			split.CuryTranAmt = (bankTran.ChargeDrCr == bankTran.DrCr ? 1 : -1) * amount ?? bankTran.CuryChargeAmt;
			split.TranDesc = bankTran.TranDesc;
			split.TaxCategoryID = taxCategotyID;
			te.CASplitRecords.Insert(split);

			var discrepancy = te.CAAdjRecords.Current.CuryTranAmt - bankTran.CuryChargeAmt;
			if (discrepancy != 0m)
			{
				ProcessDiscrepancy(te, adj, discrepancy);
			}

			adj.Hold = false;
			adj = te.CAAdjRecords.Update(adj);
			te.releaseFromHold.Press();
			te.Save.Press();
			adj = (CAAdj)te.Caches[typeof(CAAdj)].Current;
			return (CATran)PXSelect<
				CATran,
				Where<CATran.tranID, Equal<Required<CAAdj.tranID>>>>
				.Select(te, adj.TranID);
		}

		private static void ProcessDiscrepancy(CATranEntry graph, CAAdj adj, decimal? discrepancy)
		{
			adj.CuryTranAmt -= discrepancy;
			adj.CuryTaxTotal -= discrepancy;

			foreach (CATaxTran taxTran in graph.Taxes.Select())
			{
				taxTran.CuryTaxAmt -= discrepancy;
				graph.Taxes.Update(taxTran);
				break;
			}
		}

		public static void RematchFromExpenseReceipt(
			PXGraph graph,
			CABankTranMatch bankTranMatch, long? catranID, int? referenceID,
			EPExpenseClaimDetails receipt)
		{
			bankTranMatch.CATranID = catranID;
			bankTranMatch.ReferenceID = referenceID;
			bankTranMatch.DocModule = null;
			bankTranMatch.DocType = null;
			bankTranMatch.DocRefNbr = null;

			graph.Caches[typeof(CABankTranMatch)].Update(bankTranMatch);

			CABankTran bankTran = PXSelect<CABankTran,
					Where<CABankTran.tranID, Equal<Required<CABankTran.tranID>>>>
				.Select(graph, bankTranMatch.TranID);

			graph.Caches[typeof(CABankTran)].Update(bankTran);

			receipt.BankTranDate = null;
		}

		public static bool IsMatchedToExpenseReceipt(CABankTranMatch match)
		{
			return match != null && match.DocModule == BatchModule.EP && match.DocType == EPExpenseClaimDetails.DocType;
		}

		public static bool IsMatchedToInvoice(CABankTran tran, CABankTranMatch match)
		{
			return !(match != null && (match.CATranID != null
									   || (match.DocType == CATranType.CABatch && match.DocModule == GL.BatchModule.AP)
									   || IsMatchedToExpenseReceipt(match)));
		}

		#region Helpers

		public class GLCATranToExpenseReceiptMatchingGraphExtension<TGraph> : PXGraphExtension<TGraph>
			where TGraph : PXGraph
		{
			protected void _(Events.RowInserted<CATran> e)
			{
				if (e.Row.OrigTranType == GLTranType.GLEntry && e.Row.OrigModule == BatchModule.AP)
				{
					PXResult<EPExpenseClaimDetails, CABankTranMatch> row = GetExpenseReceiptWithBankTranMatching(e.Row);

					CABankTranMatch bankTranMatch = (CABankTranMatch)row;

					if (bankTranMatch?.TranID != null)
					{
						CABankTran bankTran = CABankTran.PK.Find(Base, bankTranMatch.TranID);

						e.Row.Cleared = true;
						e.Row.ClearDate = bankTran.TranDate;
					}
				}
			}

			protected void _(Events.RowPersisted<CATran> e)
			{
				if (e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open
					&& e.Row.OrigTranType == GLTranType.GLEntry && e.Row.OrigModule == BatchModule.AP)
				{
					PXResult<EPExpenseClaimDetails, CABankTranMatch> row = GetExpenseReceiptWithBankTranMatching(e.Row);

					if (row != null)
					{
						CABankTranMatch bankTranMatch = (CABankTranMatch)row;

						if (bankTranMatch?.DocRefNbr != null)
						{
							EPExpenseClaimDetails receipt = row;

							RematchFromExpenseReceipt(Base, bankTranMatch, e.Row.TranID, e.Row.ReferenceID, receipt);

							Base.Caches[typeof(CABankTranMatch)].PersistUpdated(bankTranMatch);

							PXCache receiptCache = Base.Caches[typeof(EPExpenseClaimDetails)];

							PXDBTimestampAttribute timestampAttribute = receiptCache
								.GetAttributesOfType<PXDBTimestampAttribute>(null, nameof(EPExpenseClaimDetails.tstamp))
								.First();

							timestampAttribute.RecordComesFirst = true;

							receiptCache.PersistUpdated(receipt);
						}
					}
				}
			}

			private PXResult<EPExpenseClaimDetails, CABankTranMatch> GetExpenseReceiptWithBankTranMatching(CATran caTran)
			{
				GLTran glTran =
					PXSelect<GLTran,
						Where<GLTran.module, Equal<Required<GLTran.module>>,
							And<GLTran.batchNbr, Equal<Required<GLTran.batchNbr>>,
							And<GLTran.lineNbr, Equal<Required<GLTran.lineNbr>>,
							And<GLTran.tranType, Equal<APDocType.debitAdj>>>>>>
						.Select(Base, caTran.OrigModule, caTran.OrigRefNbr, caTran.OrigLineNbr);

				if (glTran == null)
					return null;

				return PXSelectJoin<EPExpenseClaimDetails,
						   LeftJoin<CABankTranMatch,
							   On<EPExpenseClaimDetails.docType, Equal<CABankTranMatch.docType>,
								   And<EPExpenseClaimDetails.claimDetailCD, Equal<CABankTranMatch.docRefNbr>,
								   And<CABankTranMatch.docModule, Equal<BatchModule.moduleEP>>>>>,
						   Where<EPExpenseClaimDetails.aPDocType, Equal<Required<EPExpenseClaimDetails.aPDocType>>,
							   And<EPExpenseClaimDetails.aPRefNbr, Equal<Required<EPExpenseClaimDetails.aPRefNbr>>,
							   And<EPExpenseClaimDetails.aPLineNbr, Equal<Required<EPExpenseClaimDetails.aPLineNbr>>>>>>
						   .Select(Base, glTran.TranType, glTran.RefNbr, glTran.TranLineNbr)
						   .AsEnumerable()
						   .Cast<PXResult<EPExpenseClaimDetails, CABankTranMatch>>()
						   .SingleOrDefault();
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

	public class CABankIncomingPaymentsMaint : CABankTransactionsMaint
	{
		public PXFilter<MatchSettings> matchSettings;

		public override IMatchSettings CurrentMatchSesstings
		{
			get { return matchSettings.Current; }
		}

		[PXDBString(1, IsFixed = true)]
		[PXDefault(typeof(CABankTranType.paymentImport))]
		[CABankTranType.List()]
		protected virtual void Filter_TranType_CacheAttached(PXCache sender) { }
	}
}
