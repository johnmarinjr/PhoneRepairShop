using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.CS;
using System.Collections;
using PX.Data.BQL;

namespace PX.Objects.CA
{
	public class CABankTransactionsProcess : PXGraph<CABankTransactionsProcess>
	{
		public CABankTransactionsProcess()
		{
			BankAccountSummary.SetProcessCaption(Messages.AutoMatch);
			BankAccountSummary.SetProcessAllCaption(Messages.AutoMatchAll);
		}


		#region Cancel
		public PXAction<CashAccount> cancel;

		[PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select)]
		[PXCancelButton]
		protected virtual IEnumerable Cancel(PXAdapter adapter)
		{
			BankAccountSummary.Cache.Clear();
			TimeStamp = null;
			PXLongOperation.ClearStatus(this.UID);
			return adapter.Get();
		}
		#endregion


		public static void DoMatch(List<CashAccount> list, object processorID)
		{
			var graph = CreateInstance<CABankMatchingProcess>();

			foreach (var cashAccount in list)
			{
				PXProcessing<CashAccount>.SetCurrentItem(cashAccount);
				IEnumerable<CABankTran> tranList = graph.UnMatchedDetails.Select(cashAccount.CashAccountID).RowCast<CABankTran>();

				try
				{
					graph.DoMatch(tranList, cashAccount, (Guid?)processorID);
					graph.Persist();

					if (PXProcessing<CashAccount>.GetItemMessage() == null)
					{
						PXProcessing<CashAccount>.SetProcessed();
					}
				}
				catch (Exception ex)
				{
					PXProcessing<CashAccount>.SetError(ex);
				}
			}
		}

		[PXFilterable]
		public PXProcessingJoin<CashAccount,
			LeftJoin<CABankTranByCashAccount,
		On<CashAccount.cashAccountID, Equal<CABankTranByCashAccount.cashAccountID>>>,
			Where2<Where<CashAccount.restrictVisibilityWithBranch, Equal<False>,
					Or<CashAccount.branchID, Equal<Current<AccessInfo.branchID>>>>,
				And<Where<CashAccount.baseCuryID, Equal<Current<AccessInfo.baseCuryID>>,
					Or<FeatureInstalled<FeaturesSet.multipleBaseCurrencies>>>>>> BankAccountSummary;

		public PXAction<CashAccount> ViewUnmatchedTrans;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewUnmatchedTrans(PXAdapter adapter)
		{
			RedirectToMatchingOf(BankAccountSummary.Current?.CashAccountID);
			return adapter.Get();
		}

		private void RedirectToMatchingOf(int? cashAccountID)
		{
			if(!cashAccountID.HasValue)
			{
				return;
			}

			var graph = PXGraph.CreateInstance<CABankTransactionsMaint>();
			graph.TranMatch.Cache.Clear();
			graph.TranFilter.Cache.SetValueExt<CABankTransactionsMaint.Filter.cashAccountID>(graph.TranFilter.Current, cashAccountID);

			throw new PXRedirectRequiredException(graph, Messages.ProcessTransactions);
		}

		#region Events
		protected virtual void _(Events.RowSelected<CashAccount> e)
		{
			var processorID = (Guid?)UID;
			BankAccountSummary.SetProcessDelegate(list => DoMatch(list, processorID));
		}
		#endregion
	}
}
