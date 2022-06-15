using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PTOHelper
	{
		public static void GetPTOBankYear(DateTime targetDate, DateTime bankStartDate, out DateTime startDate, out DateTime endDate)
		{
			startDate = new DateTime(targetDate.Year, bankStartDate.Month, bankStartDate.Day);
			if (startDate > targetDate)
			{
				startDate = startDate.AddYears(-1);
			}
			endDate = startDate.AddYears(1).AddSeconds(-1);
		}

		public static IPTOBank GetSourceBank(PRPTOBank bank, PREmployeeClassPTOBank classBank, PREmployeePTOBank employeeBank)
		{
			IPTOBank sourceBank;
			if (employeeBank != null)
			{
				if (employeeBank.UseClassDefault == true)
				{
					sourceBank = classBank;
				}
				else
				{
					sourceBank = employeeBank;
				}
			}
			else
			{
				sourceBank = bank;
			}

			return sourceBank;
		}

		public static IPTOBank GetBankSettings(PXGraph graph, string bankID, int employeeID, DateTime targetDate)
		{
			var result = PTOHelper.PTOBankSelect.View.Select(graph, employeeID, bankID)
					.Select(x => (PXResult<PRPTOBank, PREmployee, PREmployeeClassPTOBank, PREmployeePTOBank>)x).ToList();

			return result.Select(x => GetSourceBank(x, x, x))
				.Where(x => x.StartDate.Value.Date <= targetDate.Date)
				.OrderBy(x => x.StartDate).Last();
		}

		public static PRPaymentPTOBank GetEffectivePaymentBank(IEnumerable<PRPaymentPTOBank> paymentBanks, DateTime targetDate, string bankID)
		{
			return paymentBanks.SingleOrDefault(x => x.BankID == bankID && x.EffectiveStartDate <= targetDate && targetDate < x.EffectiveEndDate);
		}

		/// <summary>
		/// Returns the hour amount that should be carried over from last year's PTO.
		/// </summary>
		public static decimal CalculateHoursToCarryover(PXGraph graph, int? employeeID, PRPayment currentPayment, IPTOBank sourceBank, DateTime ptoYearStartDate, DateTime ptoYearEndDate)
		{
			IEnumerable<PRPaymentPTOBank> pastYearHistory;
			decimal? carryoverAmount = null;
			switch (sourceBank.CarryoverType)
			{
				case CarryoverType.Total:
				case CarryoverType.PaidOnTimeLimit:
				case CarryoverType.Partial:
					pastYearHistory = EmployeePTOHistory.Select(graph, ptoYearStartDate.AddYears(-1), currentPayment.EmployeeID.Value, sourceBank).FirstTableItems;
					carryoverAmount = pastYearHistory.Sum(x => x.TotalAccrual.GetValueOrDefault() - x.TotalDisbursement.GetValueOrDefault());
					break;
				case CarryoverType.None:
				default:
					return 0;
			}

			if (currentPayment != null)
			{
				// Add amount that could be accrued on same paycheck but previous PTO year.
				foreach (PRPaymentPTOBank bank in PaymentPTOBanks.View.Select(graph, currentPayment.DocType, currentPayment.RefNbr, sourceBank.BankID))
				{
					if (bank.EffectiveStartDate < ptoYearStartDate)
					{
						carryoverAmount += bank.TotalAccrual.Value - bank.TotalDisbursement.Value;
					}
				}
			}

			if (sourceBank.CarryoverType == CarryoverType.Partial)
			{
				carryoverAmount = Math.Min(carryoverAmount.GetValueOrDefault(), sourceBank.CarryoverAmount.GetValueOrDefault());
			}
			return carryoverAmount ?? 0;
		}

		/// <summary>
		/// Returns the dollar amount that should be carried over from last year's PTO.
		/// </summary>
		public static decimal CalculateMoneyToCarryover(PXGraph graph, int? employeeID, PRPayment currentPayment, IPTOBank sourceBank, DateTime ptoYearStartDate, DateTime ptoYearEndDate)
		{
			if (sourceBank?.CreateFinancialTransaction != true)
			{
				return 0m;
			}

			decimal carryoverMoney = EmployeePTOHistory.Select(graph, ptoYearStartDate.AddYears(-1), employeeID.Value, sourceBank).FirstTableItems
				.Sum(x => x.TotalAccrualMoney.GetValueOrDefault() - x.DisbursementMoney.GetValueOrDefault());

			if (currentPayment != null)
			{
				foreach (PRPaymentPTOBank bank in PaymentPTOBanks.View.Select(graph, currentPayment.DocType, currentPayment.RefNbr, sourceBank.BankID))
				{
					if (bank.EffectiveStartDate < ptoYearStartDate)
					{
						carryoverMoney += bank.TotalAccrualMoney.Value - bank.TotalDisbursementMoney.Value;
					}
				}
			}

			return carryoverMoney;
		}

		/// <summary>
		/// Calculate accumulated, used and available PTO amounts at specified date, for an employee and a bank.
		/// Make sure you pass the right IPTOBank for your needs, either the PTO bank itself, the Class bank or the Employee Bank.
		/// </summary>
		public static PTOHistoricalAmounts GetPTOHistory(PXGraph graph, DateTime targetDate, int employeeID, IPTOBank bank)
		{
			VerifyBankIsValid(bank);

			GetPTOBankYear(targetDate, bank.PTOYearStartDate.Value, out DateTime startDate, out DateTime endDate);
			IEnumerable<PRPaymentPTOBank> historyRecords = EmployeePTOHistory.Select(graph, startDate, employeeID, bank).FirstTableItems;

			PTOHistoricalAmounts history = new PTOHistoricalAmounts();
			history.AccumulatedHours = historyRecords.Sum(x => x.TotalAccrual.GetValueOrDefault());
			history.AccumulatedMoney = historyRecords.Sum(x => x.TotalAccrualMoney.GetValueOrDefault());
			history.UsedHours = historyRecords.Sum(x => x.TotalDisbursement.GetValueOrDefault());
			history.UsedMoney = historyRecords.Sum(x => x.DisbursementMoney.GetValueOrDefault());
			if (bank.DisburseFromCarryover == true)
			{
				history.AvailableHours = historyRecords.Sum(x => x.CarryoverAmount.GetValueOrDefault());
				history.AvailableMoney = historyRecords.Sum(x => x.CarryoverMoney.GetValueOrDefault());
				history.AvailableHours -= history.UsedHours;
				history.AvailableMoney -= history.UsedMoney;
			}
			else
			{
				history.AvailableHours = history.AccumulatedHours;
				history.AvailableMoney = history.AccumulatedMoney;
				history.AvailableHours -= history.UsedHours;
				history.AvailableMoney -= history.UsedMoney;

				PRPaymentPTOBank result = EmployeePTOHistory.Select(graph, targetDate, employeeID, bank)
					.Select(x => (PXResult<PRPaymentPTOBank, PRPayment>)x)
					.Where(x => ((PRPayment)x).DocType != PayrollType.VoidCheck && ((PRPayment)x).Voided == false)
					.Select(x => (PRPaymentPTOBank)x)
					.OrderBy(x => x.RefNbr)
					.LastOrDefault();

				if (result != null)
				{
					decimal hoursUsed = result.AvailableAmount.GetValueOrDefault();
					history.AvailableHours = Math.Min(history.AvailableHours, hoursUsed);
				}
			}

			return history;
		}

		public static PTOYearSummary GetPTOYearSummary(PXGraph graph, DateTime targetDate, int employeeID, IPTOBank bank)
		{
			return GetPTOYearSummary(graph, targetDate, employeeID, bank.PTOYearStartDate.Value, bank.BankID);
		}

		public static PTOYearSummary GetPTOYearSummary(PXGraph graph, DateTime targetDate, int employeeID, DateTime bankStartDate, string bankID)
		{
			GetPTOBankYear(targetDate, bankStartDate, out DateTime startDate, out DateTime endDate);
			var results = EmployeePTOHistory.Select(graph, targetDate, employeeID, bankStartDate, bankID);
			var history = results.Select(x => (PXResult<PRPaymentPTOBank, PRPayment>)x).ToList();

			var summary = new PTOYearSummary();
			summary.StartDate = startDate;
			summary.EndDate = endDate;
			summary.AccrualAmount = history.Sum(x => ((PRPaymentPTOBank)x).AccrualAmount.GetValueOrDefault());
			summary.AccrualMoney = history.Sum(x => ((PRPaymentPTOBank)x).AccrualMoney.GetValueOrDefault());
			summary.DisbursementAmount = history.Sum(x => ((PRPaymentPTOBank)x).DisbursementAmount.GetValueOrDefault());
			summary.DisbursementMoney = history.Sum(x => ((PRPaymentPTOBank)x).DisbursementMoney.GetValueOrDefault());
			summary.FrontLoadingAmount = history.Sum(x => ((PRPaymentPTOBank)x).FrontLoadingAmount.GetValueOrDefault());
			summary.CarryoverAmount = history.Sum(x => ((PRPaymentPTOBank)x).CarryoverAmount.GetValueOrDefault());
			summary.CarryoverMoney = history.Sum(x => ((PRPaymentPTOBank)x).CarryoverMoney.GetValueOrDefault());
			summary.PaidCarryoverAmount = history.Sum(x => ((PRPaymentPTOBank)x).PaidCarryoverAmount.GetValueOrDefault());
			summary.SettlementDiscardAmount = history.Sum(x => ((PRPaymentPTOBank)x).SettlementDiscardAmount.GetValueOrDefault());

			summary.ProcessedFrontLoading = history.Any(x => ((PRPaymentPTOBank)x).ProcessedFrontLoading == true && ((PRPayment)x).Voided == false && ((PRPayment)x).DocType != PayrollType.VoidCheck);
			summary.ProcessedCarryover = history.Any(x => ((PRPaymentPTOBank)x).ProcessedCarryover == true && ((PRPayment)x).Voided == false && ((PRPayment)x).DocType != PayrollType.VoidCheck);
			summary.ProcessedPaidCarryover = history.Any(x => ((PRPaymentPTOBank)x).ProcessedPaidCarryover == true && ((PRPayment)x).Voided == false && ((PRPayment)x).DocType != PayrollType.VoidCheck);

			return summary;
		}

		protected static void VerifyBankIsValid(IPTOBank bank)
		{
			if (bank.PTOYearStartDate == null)
			{
				throw new PXException(Messages.InvalidBankStartDate);
			}
		}

		public static bool IsFirstBankProcessOfPTOYear(PXGraph graph, DateTime targetDate, int employeeID, IPTOBank bank, PRPayment paymentToSkip = null)
		{
			GetPTOBankYear(targetDate, bank.StartDate.Value, out DateTime startDate, out DateTime endDate);
			var records = SelectFrom<PRPaymentPTOBank>
				.InnerJoin<PRPayment>.On<PRPaymentPTOBank.FK.Payment>
				.InnerJoin<PREarningDetail>.On<PREarningDetail.FK.Payment>
				.Where<PRPayment.employeeID.IsEqual<P.AsInt>
					.And<PRPayment.docType.IsEqual<PayrollType.regular>>
					.And<PREarningDetail.date.IsBetween<P.AsDateTime, P.AsDateTime>>
					.And<PRPaymentPTOBank.bankID.IsEqual<P.AsString>>>
				.AggregateTo<
					GroupBy<PRPaymentPTOBank.docType>,
					GroupBy<PRPaymentPTOBank.refNbr>,
					GroupBy<PRPaymentPTOBank.bankID>,
					GroupBy<PRPaymentPTOBank.effectiveStartDate>>.View
				.Select(graph, employeeID, startDate, endDate, bank.BankID);

			var paymentBanks = records.Select(x => (PRPaymentPTOBank)x).ToList();
			if (paymentToSkip != null)
			{
				paymentBanks.RemoveAll(x => x.DocType == paymentToSkip.DocType && x.RefNbr == paymentToSkip.RefNbr);
			}

			return !paymentBanks.Any(x => startDate <= x.EffectiveStartDate && x.EffectiveStartDate < endDate);
		}

		private static List<IPTOBank> GetAllEmployeeBanksQuery(PXGraph graph, PRPayment payment, Func<IPTOBank, bool> condition)
		{
			return SelectFrom<PREmployeePTOBank>
				.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PREmployeePTOBank.bAccountID>>
				.LeftJoin<PREmployeeClassPTOBank>.On<PREmployeeClassPTOBank.employeeClassID.IsEqual<PREmployee.employeeClassID>
					.And<PREmployeeClassPTOBank.bankID.IsEqual<PREmployeePTOBank.bankID>>>
				.InnerJoin<PRPTOBank>.On<PRPTOBank.bankID.IsEqual<PREmployeePTOBank.bankID>>
				.Where<PREmployeePTOBank.bAccountID.IsEqual<P.AsInt>>
				.View.Select(graph, payment.EmployeeID)
				.Select(x => (PXResult<PREmployeePTOBank, PREmployee, PREmployeeClassPTOBank, PRPTOBank>)x)
				.Select(x => GetSourceBank(x, x, x))
				.Where(x => condition(x))
				.OrderBy(x => x.StartDate)
				.ToList();
		}

		public static IEnumerable<IPTOBank> GetEmployeeBanks(PXGraph graph, PRPayment payment)
		{
			return GetAllEmployeeBanksQuery(graph, payment, x => x.StartDate.Value.Date <= payment.EndDate.Value.Date);
		}

		public static IEnumerable<IPTOBank> GetEmployeeBanksPerBankIDAndYear(PXGraph graph, PRPayment payment, string bankId, DateTime ptoYearStart)
		{
			return GetAllEmployeeBanksQuery(graph, payment, x => x.StartDate.Value.Date.Year == ptoYearStart.Year && x.BankID == bankId);
		}

		public static IEnumerable<PRPaymentPTOBank> GetLastEffectiveBanks(IEnumerable<PRPaymentPTOBank> banks)
		{
			return banks.OrderBy(x => x.EffectiveStartDate).GroupBy(x => x.BankID).Select(x => x.Last());
		}

		public static PRPaymentPTOBank GetLastEffectiveBank(IEnumerable<PRPaymentPTOBank> banks, string bankID)
		{
			return GetLastEffectiveBanks(banks).Single(x => x.BankID == bankID);
		}

		public static IEnumerable<IPTOBank> GetFirstEffectiveBanks(IEnumerable<IPTOBank> banks)
		{
			return banks.OrderBy(x => x.StartDate).GroupBy(x => x.BankID).Select(x => x.First());
		}

		public static IEnumerable<IPTOBank> GetLastEffectiveBanks(IEnumerable<IPTOBank> banks)
		{
			return banks.OrderBy(x => x.StartDate).GroupBy(x => x.BankID).Select(x => x.Last());
		}

		public static bool SpansTwoPTOYears(DateTime bankEffectiveDate, DateTime periodStartDate, DateTime periodEndDate)
		{
			GetPTOBankYear(periodStartDate, bankEffectiveDate, out DateTime paymentStartPTOYearStart, out DateTime paymentStartPTOYearEnd);
			GetPTOBankYear(periodEndDate, bankEffectiveDate, out DateTime paymentEndPTOYearStart, out DateTime paymentEndPTOYearEnd);
			return paymentStartPTOYearStart != paymentEndPTOYearStart && paymentStartPTOYearEnd != paymentEndPTOYearEnd;
		}

		public static class EmployeePTOHistory
		{
			protected class EmployeePTOHistorySelect : SelectFrom<PRPaymentPTOBank>
			.InnerJoin<PRPayment>.On<PRPaymentPTOBank.FK.Payment>
			.LeftJoin<PREarningDetail>.On<PREarningDetail.FK.Payment>
			.Where<PRPayment.employeeID.IsEqual<P.AsInt>
				.And<PRPaymentPTOBank.effectiveStartDate.IsBetween<P.AsDateTime, P.AsDateTime>>
				.And<PRPaymentPTOBank.bankID.IsEqual<P.AsString>>
				.And<PRPayment.released.IsEqual<True>>>
			.AggregateTo<
				GroupBy<PRPaymentPTOBank.docType>,
				GroupBy<PRPaymentPTOBank.refNbr>,
				GroupBy<PRPaymentPTOBank.bankID>,
				GroupBy<PRPaymentPTOBank.effectiveStartDate>>
			{ }

			public static PXResultset<PRPaymentPTOBank> Select(PXGraph graph, DateTime targetDate, int employeeID, DateTime bankStartDate, string bankID)
			{
				var results = new PXResultset<PRPaymentPTOBank>();
				GetPTOBankYear(targetDate, bankStartDate, out DateTime ptoYearStartDate, out DateTime ptoYearEndDate);
				foreach(PXResult<PRPaymentPTOBank, PRPayment, PREarningDetail> result in EmployeePTOHistorySelect.View.Select(graph, employeeID, ptoYearStartDate, ptoYearEndDate, bankID))
				{
					PRPaymentPTOBank paymentBank = result;
					// In previous versions that didn't have overlaping PTO year in the same payment, bank start date are in year 1900 instead of paycheck's year.
					if(paymentBank.EffectiveStartDate?.Year == 1900 || ptoYearStartDate <= paymentBank.EffectiveStartDate && paymentBank.EffectiveStartDate <= ptoYearEndDate)
					{
						results.Add(result);
					}
				}

				return results;
			}

			public static PXResultset<PRPaymentPTOBank> Select(PXGraph graph, DateTime targetDate, int employeeID, IPTOBank bank)
			{
				return Select(graph, targetDate, employeeID, bank.PTOYearStartDate.Value, bank.BankID);
			}
		}


		public class PTOBankSelect : SelectFrom<PRPTOBank>
			.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<P.AsInt>>
			.LeftJoin<PREmployeeClassPTOBank>.On<PREmployeeClassPTOBank.bankID.IsEqual<PRPTOBank.bankID>
				   .And<PREmployeeClassPTOBank.employeeClassID.IsEqual<PREmployee.employeeClassID>>>
			.LeftJoin<PREmployeePTOBank>.On<PREmployeePTOBank.bankID.IsEqual<PRPTOBank.bankID>
				   .And<PREmployeePTOBank.bAccountID.IsEqual<PREmployee.bAccountID>>>
			.Where<PRPTOBank.bankID.IsEqual<P.AsString>>
		{ }

		public class PaymentPTOBanks : SelectFrom<PRPaymentPTOBank>
			.Where<PRPaymentPTOBank.docType.IsEqual<P.AsString>
				.And<PRPaymentPTOBank.refNbr.IsEqual<P.AsString>>
				.And<PRPaymentPTOBank.bankID.IsEqual<P.AsString>>>
		{ }

		public class PTOYearSummary : IPTOHistory
		{
			public DateTime StartDate { get; set; }

			public DateTime EndDate { get; set; }

			public decimal? AccrualAmount { get; set; }

			public decimal? AccrualMoney { get; set; }

			public decimal? DisbursementAmount { get; set; }

			public decimal? DisbursementMoney { get; set; }

			public bool? ProcessedFrontLoading { get; set; }

			public decimal? FrontLoadingAmount { get; set; }

			public bool? ProcessedCarryover { get; set; }

			public decimal? CarryoverAmount { get; set; }

			public decimal? CarryoverMoney { get; set; }

			public bool? ProcessedPaidCarryover { get; set; }

			public decimal? PaidCarryoverAmount { get; set; }

			public decimal? SettlementDiscardAmount { get; set; }
			
			public decimal TotalIncreasedHours => AccrualAmount.GetValueOrDefault() + FrontLoadingAmount.GetValueOrDefault() + CarryoverAmount.GetValueOrDefault();

			public decimal TotalDecreasedHours => DisbursementAmount.GetValueOrDefault() + PaidCarryoverAmount.GetValueOrDefault() + SettlementDiscardAmount.GetValueOrDefault();

			public decimal BalanceHours => TotalIncreasedHours - TotalDecreasedHours;

			public decimal TotalIncreasedMoney => AccrualMoney.GetValueOrDefault() + CarryoverMoney.GetValueOrDefault();

			public decimal TotalDecreasedMoney => DisbursementMoney.GetValueOrDefault();

			public decimal BalanceMoney => TotalIncreasedMoney - TotalDecreasedMoney;
		}

		public interface IPTOHistory
		{
			decimal? AccrualAmount { get; set; }

			decimal? AccrualMoney { get; set; }

			decimal? DisbursementAmount { get; set; }

			decimal? DisbursementMoney { get; set; }

			bool? ProcessedFrontLoading { get; set; }

			decimal? FrontLoadingAmount { get; set; }

			bool? ProcessedCarryover { get; set; }

			decimal? CarryoverAmount { get; set; }

			decimal? CarryoverMoney { get; set; }

			bool? ProcessedPaidCarryover { get; set; }

			decimal? PaidCarryoverAmount { get; set; }

			decimal? SettlementDiscardAmount { get; set; }

		}

		public class PTOHistoricalAmounts
		{
			public decimal AccumulatedHours = 0;
			public decimal AccumulatedMoney = 0;
			public decimal UsedHours = 0;
			public decimal UsedMoney = 0;
			public decimal AvailableHours = 0;
			public decimal AvailableMoney = 0;
		}
	}
}
