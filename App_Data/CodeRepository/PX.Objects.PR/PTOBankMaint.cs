using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PTOBankMaint : PXGraph<PTOBankMaint, PRPTOBank>
	{
		private const int _NonLeapYear = 1900;

		public PXFilter<PTOBankFilter> Filter;
		public SelectFrom<PRPTOBank>.View Bank;
		public SelectFrom<PRPTOBank>.Where<PRPTOBank.bankID.IsEqual<PRPTOBank.bankID.FromCurrent>>.View CurrentBank;
		public SelectFrom<PRPaymentPTOBank>
			.InnerJoin<PRPayment>.On<PRPayment.refNbr.IsEqual<PRPaymentPTOBank.refNbr>
				.And<PRPayment.docType.IsEqual<PRPaymentPTOBank.docType>>>
			.LeftJoin<PRPTODetail>.On<PRPTODetail.paymentDocType.IsEqual<PRPayment.docType>
					.And<PRPTODetail.paymentRefNbr.IsEqual<PRPayment.refNbr>>
					.And<PRPTODetail.bankID.IsEqual<PRPaymentPTOBank.bankID>>>
			.Where<PRPayment.paid.IsEqual<False>
				.And<PRPayment.released.IsEqual<False>>
				.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>
				.And<PRPaymentPTOBank.bankID.IsEqual<P.AsString>>>.View EditablePaymentPTOBanks;

		// These views are necessary to persist PRPTODetail and PRPayment records when saving
		public SelectFrom<PRPTODetail>.View DummyPTODetailView;
		public SelectFrom<PRPayment>.View DummyPaymentView;

		#region Events
		protected virtual void _(Events.FieldUpdated<PRPTOBank.isActive> e)
		{
			PRPTOBank row = e.Row as PRPTOBank;
			if (row == null)
			{
				return;
			}

			if (!e.NewValue.Equals(true))
			{
				PXCache paymentCache = this.Caches<PRPayment>();
				PXCache paymentPTOBankCache = this.Caches<PRPaymentPTOBank>();
				PXCache ptoDetailCache = this.Caches<PRPTODetail>();
				foreach (PXResult<PRPaymentPTOBank, PRPayment, PRPTODetail> result in EditablePaymentPTOBanks.Select(row.BankID))
				{
					PRPayment payment = result;
					PRPaymentPTOBank paymentPTOBank = result;
					PRPTODetail ptoDetail = result;

					paymentPTOBank.IsActive = false;
					paymentPTOBank.AccrualAmount = 0m;
					paymentPTOBank.AccrualMoney = 0m;
					paymentPTOBankCache.Update(paymentPTOBank);

					ptoDetailCache.Delete(ptoDetail);

					payment.Calculated = false;
					paymentCache.Update(payment);
				}
			}
		}
		
		public void _(Events.FieldUpdated<PRPTOBank.allowNegativeBalance> e)
		{
			var row = (PRPTOBank)e.Row;
			if (row == null || e.NewValue.Equals(false))
			{
				return;
			}

			if (row.DisburseFromCarryover == true)
			{
				row.DisburseFromCarryover = false;
				PXUIFieldAttribute.SetWarning<PRPTOBank.disburseFromCarryover>(e.Cache, row, Messages.CantUseSimultaneously);
			}
		}

		public void _(Events.FieldUpdated<PRPTOBank.disburseFromCarryover> e)
		{
			var row = (PRPTOBank)e.Row;
			if (row == null || e.NewValue.Equals(false))
			{
				return;
			}

			if (row.AllowNegativeBalance == true)
			{
				row.AllowNegativeBalance = false;
				PXUIFieldAttribute.SetWarning<PRPTOBank.allowNegativeBalance>(e.Cache, row, Messages.CantUseSimultaneously);
			}
		}

		public void _(Events.FieldUpdating<PRPTOBank.createFinancialTransaction> e)
		{
			var row = (PRPTOBank)e.Row;
			if (row == null || e.NewValue.Equals(e.OldValue))
			{
				return;
			}

			if (BankHasBeenUsed(row.BankID))
			{
				throw new PXSetPropertyException<PRPTOBank.createFinancialTransaction>(Messages.PTOBankInUse);
			}
		}

		public void _(Events.FieldUpdated<PRPTOBank.createFinancialTransaction> e)
		{
			var row = (PRPTOBank)e.Row;
			if (row == null)
			{
				return;
			}

			foreach (PXResult<PRPaymentPTOBank, PRPayment> result in EditablePaymentPTOBanks.Select(row.BankID))
			{
				PRPayment payment = result;
				payment.Calculated = false;
				Caches[typeof(PRPayment)].Update(payment);
			}
		}

		public void _(Events.RowPersisting<PRPTOBank> e)
		{
			bool? originalValue = (bool?)e.Cache.GetValueOriginal<PRPTOBank.allowNegativeBalance>(e.Row);
			if (e.Row.AllowNegativeBalance == false && e.Operation != PXDBOperation.Delete
				&& originalValue != false)
			{
				IEnumerable<string> negativeBalanceEmployees = GetNegativeBalanceEmployees(e.Row);
				if (negativeBalanceEmployees.Any())
				{
					var errorMessage = string.Format(Messages.NegativePTOBalanceError, string.Join(",", negativeBalanceEmployees));
					PXUIFieldAttribute.SetError<PRPTOBank.allowNegativeBalance>(e.Cache, e.Row, errorMessage);
					e.Row.AllowNegativeBalance = true;
				}
			}
		}

		public virtual void _(Events.RowSelected<PRPTOBank> e)
		{
			if (e.Row?.StartDate != null && Filter.Current != null)
			{
				Filter.Current.StartDateDay = e.Row.StartDate.Value.Day;
				Filter.Current.StartDateMonth = e.Row.StartDate.Value.Month;
			}
		}

		public virtual void _(Events.RowUpdating<PTOBankFilter> e)
		{
			if (e.NewRow?.StartDateMonth == null || e.NewRow?.StartDateDay == null)
			{
				return;
			}

			try
			{
				var newDate = new DateTime(_NonLeapYear, e.NewRow.StartDateMonth.Value, e.NewRow.StartDateDay.Value);
				Bank.SetValueExt<PRPTOBank.startDate>(Bank.Current, newDate);
			}
			catch (ArgumentOutOfRangeException)
			{
				var errorMessage = PXMessages.LocalizeFormat(Messages.InvalidStartDate, nameof(PRPTOBank.StartDate));
				PXUIFieldAttribute.SetWarning<PRPTOBank.startDate>(Bank.Cache, Bank.Current, errorMessage);
			}
		}

		#endregion Events

		protected IEnumerable<string> GetNegativeBalanceEmployees(PRPTOBank row)
		{
			var paymentsGroupedByEmployee =
				SelectFrom<PRPaymentPTOBank>
					.InnerJoin<PRPayment>.On<PRPayment.docType.IsEqual<PRPaymentPTOBank.docType>
						.And<PRPayment.refNbr.IsEqual<PRPaymentPTOBank.refNbr>>>
					.InnerJoin<BAccount>.On<BAccount.bAccountID.IsEqual<PRPayment.employeeID>>
					.Where<PRPaymentPTOBank.bankID.IsEqual<PRPTOBank.bankID.FromCurrent>
						.And<PRPayment.released.IsEqual<True>>
						.And<PRPayment.voided.IsEqual<False>>>
					.OrderBy<PRPayment.transactionDate.Desc>
					.View.Select(this)
					.Cast<PXResult<PRPaymentPTOBank, PRPayment, BAccount>>()
					.GroupBy(x => ((PRPayment)x).EmployeeID);

			var negativeBalanceEmployees = new HashSet<string>();
			foreach (IGrouping<int?, PXResult<PRPaymentPTOBank, PRPayment, BAccount>> employeePayments in paymentsGroupedByEmployee)
			{
				var latestResult = employeePayments.First();
				BAccount employee = latestResult;
				foreach (var result in employeePayments)
				{
					PRPayment payment = result;
					var yearSummary = PTOHelper.GetPTOYearSummary(this, payment.TransactionDate.Value, payment.EmployeeID.Value, Bank.Current.StartDate.Value, Bank.Current.BankID);
					if (yearSummary.BalanceHours < 0 || yearSummary.BalanceMoney < 0)
					{
						negativeBalanceEmployees.Add(employee.AcctCD);
						break;
					}
				}
			}

			return negativeBalanceEmployees;
		}

		protected virtual bool BankHasBeenUsed(string bankID)
		{
			return SelectFrom<PRPaymentPTOBank>
				.InnerJoin<PRPayment>.On<PRPaymentPTOBank.FK.Payment>
				.Where<PRPaymentPTOBank.bankID.IsEqual<P.AsString>
					.And<PRPayment.paid.IsEqual<True>
						.Or<PRPayment.released.IsEqual<True>>>>.View.Select(this, bankID).Any();
		}

		public class PTOBankFilter : IBqlTable
		{
			#region StartDateMonth
			[PXInt]
			[PXUIField(DisplayName = "Start Date")]
			[Month.List]
			public virtual int? StartDateMonth { get; set; }
			public abstract class startDateMonth : PX.Data.BQL.BqlInt.Field<startDateMonth> { }
			#endregion

			#region StartDateDay
			[PXInt(MinValue = 1, MaxValue = 31)]
			[PXUIField(DisplayName = "Start Date")]
			[PXUnboundDefault(1)]
			public virtual int? StartDateDay { get; set; }
			public abstract class startDateDay : PX.Data.BQL.BqlInt.Field<startDateDay> { }
			#endregion
		}
	}
}
