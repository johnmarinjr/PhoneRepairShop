using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Extensions;
using PX.Objects.GL;

namespace PX.Objects.CA
{
	public class TransferCashAccountAttribute : CashAccountAttribute, IPXFieldUpdatedSubscriber
	{
		public Type PairCashAccount { get; set; }

		public void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var cashAccountID = (int?)sender.GetValue(e.Row, _FieldName);
			var pairCashAccountID = (int?)sender.GetValue(e.Row, PairCashAccount.Name);

			if (pairCashAccountID != null && pairCashAccountID == cashAccountID)
			{
				var cashAccountCD = GetCashAccount(sender.Graph, cashAccountID).CashAccountCD;
				var exception = new PXSetPropertyException(Messages.TransferInCAAreEquals);
				sender.RaiseExceptionHandling(_FieldName, e.Row, cashAccountCD, exception);
			}

			VerifyAccount(sender, e);
			VerifySubaccount(sender, e);
		}

		private void VerifyAccount(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			int? cashAccountID = (int?)sender.GetValue(e.Row, _FieldName);
			CashAccount cashAccount = GetCashAccount(sender.Graph, cashAccountID);
			Account currenctAccount = GetAccount(sender.Graph, cashAccount.AccountID);
			if (currenctAccount != null && currenctAccount.Active == false)
			{
				PXSetPropertyException exception = new PXSetPropertyException(Messages.CashAccountUsesInactiveGLAccount, PXErrorLevel.Error, cashAccount.CashAccountCD, currenctAccount.AccountCD);
				sender.RaiseExceptionHandling(_FieldName, e.Row, cashAccount.CashAccountCD, exception);
			}
		}

		private void VerifySubaccount(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			int? cashAccountID = (int?)sender.GetValue(e.Row, _FieldName);
			CashAccount cashAccount = GetCashAccount(sender.Graph, cashAccountID);
			Sub sub = GetSub(sender.Graph, cashAccount.SubID);
			if (sub != null && sub.Active == false)
			{
				string subCD = sender.Graph.Caches[typeof(Sub)].GetFormatedMaskField<Sub.subCD>(sub);
				PXSetPropertyException exception = new PXSetPropertyException(Messages.SubaccountIsInactive, PXErrorLevel.Error, subCD);
				sender.RaiseExceptionHandling(_FieldName, e.Row, cashAccount.CashAccountCD, exception);
			}
		}

		private static CashAccount GetCashAccount(PXGraph graph, int? cashAccountID)
		{
			return SelectFrom<CashAccount>.Where<CashAccount.cashAccountID.IsEqual<@P.AsInt>>.View.Select(graph, cashAccountID);
		}

		private static Account GetAccount(PXGraph graph, int? accountID)
		{
			return SelectFrom<Account>.Where<Account.accountID.IsEqual<@P.AsInt>>.View.Select(graph, accountID);
		}

		private static Sub GetSub(PXGraph graph, int? subID)
		{
			return SelectFrom<Sub>.Where<Sub.subID.IsEqual<@P.AsInt>>.View.Select(graph, subID);
		}
	}
}
