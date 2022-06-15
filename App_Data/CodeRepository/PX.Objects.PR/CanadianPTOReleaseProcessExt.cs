using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class CanadianPTOReleaseProcessExt : PXGraphExtension<PRReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>();
		}

		private JournalEntry _JournalEntryGraph;

		#region Data views
		public SelectFrom<PRPTODetail>.
			Where<PRPTODetail.paymentDocType.IsEqual<P.AsString>
				.And<PRPTODetail.paymentRefNbr.IsEqual<P.AsString>>>.View PTODetails;
		#endregion Data views

		#region Base graph overrides
		public delegate void ReleaseDocProcDelegate(JournalEntry je, PRPayment doc);
		[PXOverride]
		public virtual void ReleaseDocProc(JournalEntry je, PRPayment doc, ReleaseDocProcDelegate baseMethod)
		{
			_JournalEntryGraph = je;
			baseMethod(je, doc);
		}

		public delegate void ProcessPTODelegate(PRPayment doc);
		[PXOverride]
		public virtual void ProcessPTO(PRPayment doc, ProcessPTODelegate baseMethod)
		{
			baseMethod(doc);

			if (!Base.PaymentUpdatesGL(doc))
			{
				return;
			}

			List<PRPTODetail> ptoDetails = PTODetails.Select(doc.DocType, doc.RefNbr).FirstTableItems.ToList();
			Dictionary<AccountKey, decimal> assetAccountBalances = null;
			Dictionary<AccountKey, decimal> liabilityAccountBalances = null;
			if (doc.DocType != PayrollType.VoidCheck)
			{
				assetAccountBalances = ptoDetails.GroupBy(x => new AccountKey(x.AssetAccountID, x.BranchID))
					.ToDictionary(k => k.Key, v => GetAccountBalance(v.Key.AccountID, doc.FinPeriodID, v.Key.BranchID));
				liabilityAccountBalances = ptoDetails.GroupBy(x => new AccountKey(x.LiabilityAccountID, x.BranchID))
					.ToDictionary(k => k.Key, v => GetAccountBalance(v.Key.AccountID, doc.FinPeriodID, v.Key.BranchID));
			}

			CurrencyInfo currencyInfo = Base.GetCurrencyInfo(doc);
			foreach (PRPTODetail ptoDetail in ptoDetails.Where(x => x.Amount != 0))
			{
				GLTran expenseTran = WritePTOExpense(doc, ptoDetail, currencyInfo, _JournalEntryGraph.BatchModule.Current);
				_JournalEntryGraph.GLTranModuleBatNbr.Insert(expenseTran);

				// For the credit transaction, the following logic applies:
				// 1. For void checks, post to the same account as the original paycheck.
				// 2. If the PTO Detail amount is positive:
				//    a. If the asset account has a positive balance, post to asset account.
				//    b. Otherwise, post to liability account.
				// 3. If the PTO Detail amount is negative:
				//    a. If the liability account has a positive balance, post to liability account.
				//    b. Otherwise, post to asset account.
				if (doc.DocType == PayrollType.VoidCheck)
				{
					if (ptoDetail.CreditAmountFromAssetAccount != 0)
					{
						GLTran assetTran = WritePTOAsset(doc, ptoDetail, ptoDetail.CreditAmountFromAssetAccount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(assetTran);
					}

					decimal? liabilityAmount = ptoDetail.Amount - ptoDetail.CreditAmountFromAssetAccount;
					if (liabilityAmount != 0)
					{
						GLTran liabilityTran = WritePTOLiability(doc, ptoDetail, liabilityAmount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(liabilityTran);
					}
				}
				else if (ptoDetail.Amount > 0)
				{
					ptoDetail.CreditAmountFromAssetAccount = 0;
					AccountKey accountKey = new AccountKey(ptoDetail.AssetAccountID, ptoDetail.BranchID);
					if (assetAccountBalances[accountKey] > 0)
					{
						ptoDetail.CreditAmountFromAssetAccount = Math.Min(assetAccountBalances[accountKey], ptoDetail.Amount.GetValueOrDefault());
						GLTran assetTran = WritePTOAsset(doc, ptoDetail, ptoDetail.CreditAmountFromAssetAccount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(assetTran);
						assetAccountBalances[accountKey] -= ptoDetail.CreditAmountFromAssetAccount.GetValueOrDefault();
					}					

					decimal? liabilityAmount = ptoDetail.Amount - ptoDetail.CreditAmountFromAssetAccount;
					if (liabilityAmount != 0)
					{
						GLTran liabilityTran = WritePTOLiability(doc, ptoDetail, liabilityAmount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(liabilityTran);
					}
				}
				else
				{
					decimal liabilityAmount = 0;
					AccountKey accountKey = new AccountKey(ptoDetail.LiabilityAccountID, ptoDetail.BranchID);
					if (liabilityAccountBalances[accountKey] > 0)
					{
						liabilityAmount = Math.Min(liabilityAccountBalances[accountKey], ptoDetail.Amount.GetValueOrDefault());
						GLTran liabilityTran = WritePTOLiability(doc, ptoDetail, liabilityAmount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(liabilityTran);
						liabilityAccountBalances[accountKey] -= liabilityAmount;
					}

					ptoDetail.CreditAmountFromAssetAccount = ptoDetail.Amount - liabilityAmount;
					if (ptoDetail.CreditAmountFromAssetAccount != 0)
					{
						GLTran assetTran = WritePTOAsset(doc, ptoDetail, ptoDetail.CreditAmountFromAssetAccount, currencyInfo, _JournalEntryGraph.BatchModule.Current);
						_JournalEntryGraph.GLTranModuleBatNbr.Insert(assetTran);
					}
				}

				PTODetails.Update(ptoDetail);				
			}
		}
		#endregion Base graph overrides

		#region Helpers
		protected virtual GLTran WritePTOExpense(PRPayment payment, PRPTODetail ptoDetail, CurrencyInfo info, Batch batch)
		{
			var isDebit = payment.DrCr == DrCr.Debit;

			GLTran tran = new GLTran();
			tran.SummPost = Base.PRSetup.Current.SummPost;
			tran.BranchID = ptoDetail.BranchID;
			tran.AccountID = ptoDetail.ExpenseAccountID;
			tran.SubID = ptoDetail.ExpenseSubID;
			tran.ReclassificationProhibited = true;
			tran.CuryDebitAmt = isDebit ? ptoDetail.Amount : 0m;
			tran.DebitAmt = isDebit ? ptoDetail.Amount : 0m;
			tran.CuryCreditAmt = isDebit ? 0m : ptoDetail.Amount;
			tran.CreditAmt = isDebit ? 0m : ptoDetail.Amount;
			tran.TranType = payment.DocType;
			tran.RefNbr = payment.RefNbr;
			tran.TranDesc = PXMessages.LocalizeFormatNoPrefix(Messages.PTOExpenseFormat, ptoDetail.BankID);
			tran.TranPeriodID = batch.TranPeriodID;
			tran.FinPeriodID = batch.FinPeriodID;
			tran.TranDate = payment.TransactionDate;
			tran.CuryInfoID = info.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = Base.PRSetup.Current.HideEmployeeInfo == true ? null : payment.EmployeeID;
			tran.ProjectID = CostAssignmentType.GetSetting(payment.PTOCostSplitType?.FirstOrDefault()).AssignCostToProject && ptoDetail.ProjectID != null ? ptoDetail.ProjectID : ProjectDefaultAttribute.NonProject();
			tran.TaskID = CostAssignmentType.GetSetting(payment.PTOCostSplitType?.FirstOrDefault()).AssignCostToProject ? ptoDetail.ProjectTaskID : null;
			tran.CostCodeID = ptoDetail.CostCodeID;
			tran.InventoryID = ptoDetail.LabourItemID;
			tran.Qty = 1;
			return tran;
		}

		protected virtual GLTran WritePTOAsset(PRPayment payment, PRPTODetail ptoDetail, decimal? amount, CurrencyInfo info, Batch batch)
		{
			var isDebit = payment.DrCr == GL.DrCr.Debit;

			GLTran tran = new GLTran();
			tran.SummPost = Base.PRSetup.Current.SummPost;
			tran.BranchID = ptoDetail.BranchID;
			tran.AccountID = ptoDetail.AssetAccountID;
			tran.SubID = ptoDetail.AssetSubID;
			tran.ReclassificationProhibited = true;
			tran.CuryDebitAmt = isDebit ? 0m : amount;
			tran.DebitAmt = isDebit ? 0m : amount;
			tran.CuryCreditAmt = isDebit ? amount : 0m;
			tran.CreditAmt = isDebit ? amount : 0m;
			tran.TranType = payment.DocType;
			tran.RefNbr = payment.RefNbr;
			tran.TranDesc = PXMessages.LocalizeFormatNoPrefix(Messages.PTOAssetFormat, ptoDetail.BankID);
			tran.TranPeriodID = batch.TranPeriodID;
			tran.FinPeriodID = batch.FinPeriodID;
			tran.TranDate = payment.TransactionDate;
			tran.CuryInfoID = info.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = Base.PRSetup.Current.HideEmployeeInfo == true ? null : payment.EmployeeID;
			tran.Qty = 1;
			return tran;
		}

		protected virtual GLTran WritePTOLiability(PRPayment payment, PRPTODetail ptoDetail, decimal? amount, CurrencyInfo info, Batch batch)
		{
			var isDebit = payment.DrCr == GL.DrCr.Debit;

			GLTran tran = new GLTran();
			tran.SummPost = Base.PRSetup.Current.SummPost;
			tran.BranchID = ptoDetail.BranchID;
			tran.AccountID = ptoDetail.LiabilityAccountID;
			tran.SubID = ptoDetail.LiabilitySubID;
			tran.ReclassificationProhibited = true;
			tran.CuryDebitAmt = isDebit ? 0m : amount;
			tran.DebitAmt = isDebit ? 0m : amount;
			tran.CuryCreditAmt = isDebit ? amount : 0m;
			tran.CreditAmt = isDebit ? amount : 0m;
			tran.TranType = payment.DocType;
			tran.RefNbr = payment.RefNbr;
			tran.TranDesc = PXMessages.LocalizeFormatNoPrefix(Messages.PTOLiabilityFormat, ptoDetail.BankID);
			tran.TranPeriodID = batch.TranPeriodID;
			tran.FinPeriodID = batch.FinPeriodID;
			tran.TranDate = payment.TransactionDate;
			tran.CuryInfoID = info.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = Base.PRSetup.Current.HideEmployeeInfo == true ? null : payment.EmployeeID;
			tran.Qty = 1;
			return tran;
		}

		protected virtual decimal GetAccountBalance(int? accountID, string finPeriodID, int? branchID)
		{
			AccountByPeriodEnq graphGL = PXGraph.CreateInstance<AccountByPeriodEnq>();
			AccountByPeriodFilter filterGL = PXCache<AccountByPeriodFilter>.CreateCopy(graphGL.Filter.Current);

			filterGL.BranchID = branchID;
			graphGL.Filter.Cache.SetDefaultExt<AccountByPeriodFilter.ledgerID>(filterGL);
			filterGL.StartPeriodID = finPeriodID;
			filterGL.EndPeriodID = finPeriodID;
			filterGL.AccountID = accountID;
			graphGL.Filter.Update(filterGL);
			filterGL = graphGL.Filter.Select(); // to calculate totals
			return filterGL.EndBal.GetValueOrDefault();
		}
		#endregion Helpers

		protected struct AccountKey
		{
			public int? AccountID;
			public int? BranchID;
			public AccountKey(int? accountID, int? branchID) => (AccountID, BranchID) = (accountID, branchID);
		}
	}
}
