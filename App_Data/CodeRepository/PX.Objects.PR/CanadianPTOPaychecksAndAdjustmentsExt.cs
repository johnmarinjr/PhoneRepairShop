using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class CanadianPTOPaychecksAndAdjustmentsExt : PXGraphExtension<PRPayChecksAndAdjustments>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>();
		}

		#region Data views
		[PXCopyPasteHiddenView]
		public SelectFrom<PRPTODetail>
			.Where<PRPTODetail.FK.Payment.SameAsCurrent>
			.OrderBy<PRPTODetail.bankID.Asc>.View PTODetails;
		#endregion Data views

		#region Actions
		public PXAction<PRPayment> ViewPTODetails;
		[PXUIField(DisplayName = "PTO Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewPTODetails()
		{
			PTODetails.AskExt();
		}

		public PXAction<PRPayment> RevertPTOSplitCalculation;
		[PXUIField(DisplayName = "Revert PTO earning split calculation", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable revertPTOSplitCalculation(PXAdapter adapter)
		{
			RevertPaymentSplitPTOEarnings(Base, Base.Document.Current, Base.Earnings.View);
			return adapter.Get();
		}
		#endregion Actions

		#region Event handlers
		public virtual void _(Events.RowSelected<PRPayment> e)
		{
			bool notPaid = !Base.IsPaid(e.Row);
			bool enableDetailEdit = Base.ShouldEnableDetailEdit(e.Row);
			bool isReadyForInput = Base.IsReadyForInput(e.Row);

			PTODetails.Cache.AllowInsert = enableDetailEdit;
			PTODetails.Cache.AllowUpdate = e.Row.Released == false;
			PTODetails.Cache.AllowDelete = enableDetailEdit;

			RevertPTOSplitCalculation.SetEnabled(notPaid && isReadyForInput);

			if (e.Row.DocType == PayrollType.VoidCheck)
			{
				PTODetails.Cache.AllowUpdate = false;
				RevertPTOSplitCalculation.SetEnabled(false);
			}
		}

		public virtual void _(Events.FieldUpdated<PRPaymentPTOBank, PRPaymentPTOBank.isActive> e)
		{
			// If e.ExternalCall is false, the PRPaymentDeduct row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (e.Row == null || !e.ExternalCall || Base.CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [PRCalculationEngineUtils will be created]
			RecreatePTODetails(e.Row);
		}

		public virtual void _(Events.FieldUpdated<PRPaymentPTOBank, PRPaymentPTOBank.accrualMoney> e)
		{
			// If e.ExternalCall is false, the PRPaymentDeduct row was created by creation of detail line, so we can skip this event to not recreate the same detail line.
			if (e.Row == null || !e.ExternalCall || Base.CurrentDocument.Current.DocType != PayrollType.Adjustment)
			{
				return;
			}

			// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [PRCalculationEngineUtils will be created]
			RecreatePTODetails(e.Row);
		}

		public virtual void _(Events.RowSelected<PRPTODetail> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRPTODetail.bankID>(e.Cache, e.Row, string.IsNullOrEmpty(e.Row.BankID));

			if (Base.Document.Current.Paid == true && Base.Document.Current.Released == false)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.liabilityAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.liabilitySubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.expenseAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.expenseSubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.assetAccountID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.assetSubID>(e.Cache, e.Row, true);
				PXUIFieldAttribute.SetEnabled<PRPTODetail.costCodeID>(e.Cache, e.Row, true);
			}
		}

		public virtual void _(Events.FieldUpdated<PRPTODetail, PRPTODetail.amount> e)
		{
			if (e.Row == null || Base.Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustPTOSummary(e.Row.BankID);
		}

		public virtual void _(Events.RowInserted<PRPTODetail> e)
		{
			if (e.Row == null || Base.Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustPTOSummary(e.Row.BankID);
		}

		public virtual void _(Events.RowDeleted<PRPTODetail> e)
		{
			if (e.Row == null || Base.Document.Current.DocType != PayrollType.Adjustment || !e.ExternalCall)
			{
				return;
			}

			AdjustPTOSummary(e.Row.BankID);
		}

		public virtual void _(Events.FieldUpdated<PRPTODetail, PRPTODetail.labourItemID> e)
		{
			if (e.Row == null)
			{
				return;
			}

			DefaultPTOExpenseAcctSub(e.Cache, e.Row);
		}

		public virtual void _(Events.FieldUpdated<PRPTODetail, PRPTODetail.earningTypeCD> e)
		{
			if (e.Row == null)
			{
				return;
			}

			DefaultPTOExpenseAcctSub(e.Cache, e.Row);
		}
		#endregion Event handlers

		#region Base graph overrides
		public delegate void DeleteCalculatedDataDelegate();
		[PXOverride]
		public virtual void DeleteCalculatedData(DeleteCalculatedDataDelegate baseMethod)
		{
			baseMethod();
			PTODetails.Select().FirstTableItems.ForEach(x => PTODetails.Delete(x));
		}

		public delegate void VoidCheckProcDelegate(PRPayment doc);
		[PXOverride]
		public virtual void VoidCheckProc(PRPayment doc, VoidCheckProcDelegate baseMethod)
		{
			baseMethod(doc);

			foreach (PRPTODetail ptoDetail in SelectFrom<PRPTODetail>.
				Where<PRPTODetail.paymentDocType.IsEqual<P.AsString>.
					And<PRPTODetail.paymentRefNbr.IsEqual<P.AsString>>>.View.Select(Base, doc.DocType, doc.RefNbr))
			{
				PRPTODetail copy = PXCache<PRPTODetail>.CreateCopy(ptoDetail);
				copy.RecordID = null;
				copy.PaymentDocType = PayrollType.VoidCheck;
				copy.Released = false;
				copy.Amount = -1 * copy.Amount;
				copy.CreditAmountFromAssetAccount = -1 * copy.CreditAmountFromAssetAccount;

				PTODetails.Update(copy);
			}
		}
		#endregion Base graph overrides

		#region Helpers
		protected virtual void RecreatePTODetails(PRPaymentPTOBank row)
		{
			if (row.CreateFinancialTransaction != true)
			{
				return;
			}

			PTODetails.Select().FirstTableItems.Where(x => x.BankID == row.BankID)
				.ForEach(x => PTODetails.Delete(x));

			CanadianPTOCalculationEngineExt.CreatePTODetail(Base, Base.Document.Current, PTODetails.Cache, row.BankID, Base.Earnings.Select().FirstTableItems);
		}

		protected virtual void AdjustPTOSummary(string bankID)
		{
			if (string.IsNullOrEmpty(bankID))
			{
				return;
			}

			PRPaymentPTOBank paymentPTOBank = Base.PaymentPTOBanks.Select().FirstTableItems.FirstOrDefault(x => x.BankID == bankID);
			decimal? detailTotalAmount = PTODetails.Select().FirstTableItems.Where(x => x.BankID == bankID).Sum(x => x.Amount);
			if (detailTotalAmount != paymentPTOBank?.AccrualMoney)
			{
				paymentPTOBank = paymentPTOBank ??
					new PRPaymentPTOBank()
					{
						BankID = bankID
					};
				paymentPTOBank.IsActive = true;
				paymentPTOBank.AccrualMoney = detailTotalAmount;
				Base.PaymentPTOBanks.Update(paymentPTOBank);
			}
		}

		protected virtual void DefaultPTOExpenseAcctSub(PXCache cache, PRPTODetail row)
		{
			if (row.ExpenseAccountID == null)
			{
				cache.SetDefaultExt<PRPTODetail.expenseAccountID>(row);
			}

			if (row.ExpenseSubID == null)
			{
				cache.SetDefaultExt<PRPTODetail.expenseSubID>(row);
			}
		}

		public static void RevertPaymentSplitPTOEarnings(PXGraph graph, PRPayment document, PXView earningDetailView)
		{
			Dictionary<int?, PREarningDetail> paymentEarningDetails = earningDetailView.SelectMulti()
				.Select(x => (PREarningDetail)(x is PXResult pxResult ? pxResult[0] : x))
				.ToDictionary(x => x.RecordID, x => x);
			bool splitPTORecordsExist = false;

			foreach (PREarningDetail ptoSplitEarningDetail in paymentEarningDetails.Values)
			{
				int? basePTORecordID = ptoSplitEarningDetail.BasePTORecordID;

				if (basePTORecordID == null)
				{
					continue;
				}

				if (!paymentEarningDetails.TryGetValue(basePTORecordID, out PREarningDetail baseEarningDetail))
				{
					earningDetailView.Cache.Delete(ptoSplitEarningDetail);
					PXTrace.WriteWarning(Messages.InconsistentBaseEarningDetailRecord, basePTORecordID, ptoSplitEarningDetail.RecordID);
					continue;
				}

				using (PXTransactionScope transactionScope = new PXTransactionScope())
				{
					if (ptoSplitEarningDetail.IsFringeRateEarning != true)
					{
						baseEarningDetail.Hours += ptoSplitEarningDetail.Hours;
						earningDetailView.Cache.Update(baseEarningDetail);
					}

					earningDetailView.Cache.Delete(ptoSplitEarningDetail);
					transactionScope.Complete(graph);
				}
				splitPTORecordsExist = true;
			}

			if (splitPTORecordsExist)
			{
				document.Calculated = false;
				graph.Actions.PressSave();
			}
		}
		#endregion Helpers
	}
}
