using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;

using System;
using System.Collections;

namespace PX.Objects.FA
{ 
	public abstract class AdditionsViewExtensionBase<TGraph> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		public virtual BqlCommand GetSelectCommand(GLTranFilter filter)
		{
			BqlCommand cmd = new PXSelectJoin<FAAccrualTran,
				LeftJoin<Account, On<Account.accountID, Equal<FAAccrualTran.gLTranAccountID>>>>(Base)
				.View
				.BqlSelect;

			if (filter.AccountID != null)
			{
				cmd = cmd.WhereAnd<Where<FAAccrualTran.gLTranAccountID, Equal<Current<GLTranFilter.accountID>>>>();
			}
			else
			{
				cmd = cmd.WhereAnd<Where2<Match<Account, Current<AccessInfo.userName>>,
									And<Account.active, Equal<True>,
									And2<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
										Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>,
									And<Where<Account.curyID, IsNull,
										Or<Account.curyID, Equal<Current<Company.baseCuryID>>>>>>>>>();
			}
			cmd = cmd.WhereAnd<Where<FAAccrualTran.closedAmt, Less<FAAccrualTran.gLTranAmt>, Or<FAAccrualTran.tranID, IsNull>>>();
			cmd = cmd.WhereAnd<Where<Current<GLTranFilter.showReconciled>, Equal<True>, Or<FAAccrualTran.reconciled, NotEqual<True>, Or<FAAccrualTran.reconciled, IsNull>>>>();
			if (filter.ReconType == GLTranFilter.reconType.Addition)
			{
				cmd = cmd.WhereAnd<Where<FAAccrualTran.gLTranDebitAmt, Greater<decimal0>>>();
			}
			else
			{
				cmd = cmd.WhereAnd<Where<FAAccrualTran.gLTranCreditAmt, Greater<decimal0>>>();
			}

			if (filter.SubID != null)
			{
				cmd = cmd.WhereAnd<Where<FAAccrualTran.gLTranSubID, Equal<Current<GLTranFilter.subID>>>>();
			}

			return cmd;
		}

		public virtual IEnumerable GetFAAccrualTransactions(GLTranFilter filter, PXCache accrualCache)
		{
			int startRow = PXView.StartRow;
			int totalRows = 0;

			BqlCommand cmd = GetSelectCommand(filter);

			foreach (PXResult<FAAccrualTran> res in cmd
				.CreateView(Base, mergeCache: true)
				.Select(
					PXView.Currents,
					null,
					PXView.Searches,
					null,
					PXView.Descendings,
					PXView.Filters,
					ref startRow,
					PXView.MaximumRows,
					ref totalRows))
			{
				FAAccrualTran ext = res;
				if (ext.GLTranAmt == null)
				{
					ext.GLTranAmt = ext.GLTranDebitAmt + ext.GLTranCreditAmt;
					ext.GLTranQty = ext.GLTranOrigQty;
					ext.SelectedAmt = 0m;
					ext.SelectedQty = 0m;
					ext.OpenAmt = ext.GLTranAmt;
					ext.OpenQty = ext.GLTranOrigQty;
					ext.ClosedAmt = 0m;
					ext.ClosedQty = 0m;
					ext.UnitCost = ext.GLTranOrigQty > 0 ? ext.GLTranAmt / ext.GLTranOrigQty : ext.GLTranAmt;
					ext.Reconciled = false;

					accrualCache.SetStatus(ext, PXEntryStatus.Inserted);
					accrualCache.RaiseRowInserting(ext);
				}
				yield return ext;
			}
			PXView.StartRow = 0;
		}
	}
}
