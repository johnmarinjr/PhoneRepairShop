﻿using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class PayRateAttribute : PXBaseConditionAttribute, IPXRowSelectedSubscriber, IPXRowUpdatedSubscriber, IPXRowInsertedSubscriber, IPXFieldVerifyingSubscriber, IPXFieldSelectingSubscriber
	{
		private const decimal ZeroRate = 0m;

		private Type _EnableCondition;

		public PayRateAttribute(Type enableCondition)
		{
			_EnableCondition = enableCondition;
		}

		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			PREarningDetail currentRecord = e.Row as PREarningDetail;

			if (currentRecord == null || currentRecord.IsRegularRate != true)
				return;

			e.ReturnValue = null;
		}

		public void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetWarning<PREarningDetail.rate>(sender, e.Row, null);
			PREarningDetail currentRecord = e.Row as PREarningDetail;
			if (currentRecord == null || currentRecord.IsAmountBased == true || currentRecord.IsRegularRate == true)
				return;

			decimal? payRate = e.NewValue as decimal?;
			if (payRate > 0 || currentRecord.TypeCD == null)
				return;

			string errorMessage;
			if (payRate == 0)
				errorMessage = Messages.ZeroPayRate;
			else if (payRate < 0)
				errorMessage = Messages.NegativePayRate;
			else
				errorMessage = Messages.EmptyPayRate;

			sender.RaiseExceptionHandling<PREarningDetail.rate>(e.Row, e.NewValue, 
				new PXSetPropertyException(errorMessage, PXErrorLevel.Warning));
		}

		public virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!GetConditionResult(sender, e.Row, _EnableCondition))
			{
				return;
			}

			PREarningDetail oldRow = e.OldRow as PREarningDetail;
			PREarningDetail newRow = e.Row as PREarningDetail;

			if (newRow == null)
			{
				return;
			}

			if (e.ExternalCall && !sender.ObjectsEqual<PREarningDetail.rate>(oldRow, newRow))
			{
				if (newRow.IsRegularRate != true)
				{
					newRow.ManualRate = true;
					return;
				}

				newRow.ManualRate = false;
			}

			//The Rate should not be updated if the fields the Rate depends on were not updated.
			if (oldRow != null &&
				sender.ObjectsEqual<
					PREarningDetail.manualRate, 
					PREarningDetail.employeeID, 
					PREarningDetail.typeCD, 
					PREarningDetail.date,
					PREarningDetail.labourItemID, 
					PREarningDetail.projectID,
					PREarningDetail.projectTaskID,
					PREarningDetail.unionID>(oldRow, newRow) &&
				sender.ObjectsEqual<
					PREarningDetail.isRegularRate,
					PREarningDetail.shiftID,
					PREarningDetail.certifiedJob>(oldRow, newRow))
			{
				return;
			}

			SetRate(sender, newRow);
		}

		public virtual void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if (!GetConditionResult(sender, e.Row, _EnableCondition))
			{
				return;
			}

			SetRate(sender, e.Row as PREarningDetail);
		}

		public static void SetRate(PXCache sender, PREarningDetail currentRecord)
		{
			if (currentRecord == null || currentRecord.ManualRate == true || currentRecord.PaymentDocType == PayrollType.VoidCheck)
				return;

			if (currentRecord.IsRegularRate == true || currentRecord.SourceType == EarningDetailSourceType.SalesCommission)
				return;

			if (currentRecord.IsAmountBased != true)
			{
				decimal maxRate = GetMaxRate(sender.Graph, currentRecord);

				if ((sender.Graph.IsImportFromExcel || sender.Graph.IsImport) && currentRecord.Rate != null)
				{
					int ratePrecision = PRCurrencyAttribute.GetPrecision(sender, currentRecord, nameof(PREarningDetail.rate)) ?? 2;

					if (currentRecord.Rate != Math.Round(maxRate, ratePrecision, MidpointRounding.AwayFromZero))
					{
						currentRecord.ManualRate = true;
						sender.SetDefaultExt<PREarningDetail.amount>(currentRecord);
						return;
					}
				}

				sender.SetValueExt<PREarningDetail.rate>(currentRecord, maxRate);
				sender.SetDefaultExt<PREarningDetail.amount>(currentRecord);
			}
			else
			{
				currentRecord.Rate = null;
			}
		}

		private static decimal GetMaxRate(PXGraph graph, PREarningDetail currentRecord)
		{
			decimal employeeEarningRate = GetEmployeeEarningRate(graph, currentRecord);
			if (currentRecord.UnitType == UnitType.Misc)
				return employeeEarningRate;

			decimal employeeLaborCostRate = GetEmployeeLaborCostRate(graph, currentRecord);
			decimal unionLocalRate = GetUnionLocalRate(graph, currentRecord);
			decimal certifiedProjectRate = GetCertifiedProjectRate(graph, currentRecord);
			decimal projectRate = GetProjectRate(graph, currentRecord);
			decimal laborItemRate = GetLaborItemRate(graph, currentRecord);

			decimal rate = Math.Max(Math.Max(
				Math.Max(employeeEarningRate, employeeLaborCostRate),
				Math.Max(unionLocalRate, certifiedProjectRate)),
				Math.Max(projectRate, laborItemRate));

			if (currentRecord.ShiftID != null)
			{
				decimal otMultiplier = GetOvertimeMultiplier(graph, currentRecord).GetValueOrDefault(1);
				rate = EPShiftCodeSetup.CalculateShiftWage(graph, currentRecord.ShiftID, currentRecord.Date, rate, otMultiplier);
			}
			return rate;
		}

		private static decimal GetEmployeeEarningRate(PXGraph graph, PREarningDetail earningDetailRecord)
		{
			return GetEmployeeEarningRate(graph, earningDetailRecord.TypeCD, earningDetailRecord.EmployeeID, earningDetailRecord.Date);
		}

		public static decimal GetEmployeeEarningRate(PXGraph graph, string earningTypeCD, int? employeeID, DateTime? date)
		{
			EPEarningType earningDetailType = GetEarningTypeRecord(graph, earningTypeCD);
			PREarningType prEarningDetailType = earningDetailType?.GetExtension<PREarningType>();

			if (earningDetailType?.IsOvertime == true || prEarningDetailType?.IsPTO == true)
			{
				if (prEarningDetailType == null)
					PXTrace.WriteWarning(Messages.EarningTypeNotFound, earningTypeCD, typeof(PREarningType).Name);

				if (string.IsNullOrWhiteSpace(prEarningDetailType?.RegularTypeCD))
					return ZeroRate;

				earningTypeCD = prEarningDetailType.RegularTypeCD;
			}
			
			PXResult<PREmployeeEarning, PREmployee> employeeEarningQuery = 
				(PXResult<PREmployeeEarning, PREmployee>)
				SelectFrom<PREmployeeEarning>.
				InnerJoin<PREmployee>.On<PREmployeeEarning.bAccountID.IsEqual<PREmployee.bAccountID>>.
				Where<PREmployeeEarning.isActive.IsEqual<True>.
					And<PREmployeeEarning.bAccountID.IsEqual<P.AsInt>>.
					And<PREmployeeEarning.typeCD.IsEqual<P.AsString>>.
					And<PREmployeeEarning.startDate.IsLessEqual<P.AsDateTime>.
						And<PREmployeeEarning.endDate.IsNull.
							Or<PREmployeeEarning.endDate.IsGreaterEqual<P.AsDateTime>>>>>.
				OrderBy<PREmployeeEarning.startDate.Desc>.View.
				Select(graph, employeeID, earningTypeCD, date, date);

			PREmployeeEarning employeeEarning = employeeEarningQuery;

			if (employeeEarning == null || employeeEarning.PayRate == null)
				return ZeroRate;

			decimal currentPayRate = employeeEarning.PayRate.Value;

			if (earningDetailType?.IsOvertime == true && earningDetailType?.OvertimeMultiplier > 0)
				currentPayRate *= earningDetailType.OvertimeMultiplier.Value;

			if (employeeEarning.UnitType != UnitType.Year)
				return currentPayRate;

			PREmployee currentEmployee = employeeEarningQuery;

			decimal hoursPerYear = currentEmployee?.HoursPerYear ?? 0m;
			if (hoursPerYear == 0)
				return ZeroRate;

			return currentPayRate / hoursPerYear;
		}

		private static decimal GetEmployeeLaborCostRate(PXGraph graph, PREarningDetail earningDetail)
		{
			PMLaborCostRate employeeLaborCostRate =
				SelectFrom<PMLaborCostRate>.
				Where<PMLaborCostRate.employeeID.IsEqual<PREarningDetail.employeeID.FromCurrent>.
					And<PMLaborCostRate.type.IsEqual<PMLaborCostRateType.employee>>.
					And<PMLaborCostRate.inventoryID.IsNull.Or<PMLaborCostRate.inventoryID.IsEqual<PREarningDetail.labourItemID.FromCurrent>>>.
					And<PMLaborCostRate.effectiveDate.IsLessEqual<PREarningDetail.date.FromCurrent>>>.
				OrderBy<PMLaborCostRate.effectiveDate.Desc>.View.
				SelectSingleBound(graph, new object[] { earningDetail });

			if (employeeLaborCostRate?.WageRate == null)
				return ZeroRate;

			return employeeLaborCostRate.WageRate.Value * (GetOvertimeMultiplier(graph, earningDetail) ?? 1);
		}

		private static decimal GetUnionLocalRate(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail.UnionID == null || earningDetail.LabourItemID == null)
				return ZeroRate;

			PMLaborCostRate unionLocalRate =
				SelectFrom<PMLaborCostRate>.
				Where<PMLaborCostRate.inventoryID.IsEqual<PREarningDetail.labourItemID.FromCurrent>.
					And<PMLaborCostRate.effectiveDate.IsLessEqual<PREarningDetail.date.FromCurrent>>.
					And<PMLaborCostRate.employeeID.IsNull.Or<PMLaborCostRate.employeeID.IsEqual<PREarningDetail.employeeID.FromCurrent>>>.
					And<PMLaborCostRate.unionID.IsEqual<PREarningDetail.unionID.FromCurrent>>>.
				OrderBy<PMLaborCostRate.effectiveDate.Desc>.View.
				SelectSingleBound(graph, new object[] { earningDetail });

			if (unionLocalRate?.WageRate == null)
				return ZeroRate;

			return unionLocalRate.WageRate.Value * (GetOvertimeMultiplier(graph, earningDetail) ?? 1);
		}

		private static decimal GetCertifiedProjectRate(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail.ProjectID == null || earningDetail.CertifiedJob != true || earningDetail.LabourItemID == null)
				return ZeroRate;

			PREmployee employee =
				SelectFrom<PREmployee>.Where<PREmployee.bAccountID.IsEqual<PREarningDetail.employeeID.FromCurrent>>.View.
				SelectSingleBound(graph, new object[] { earningDetail });

			if (employee?.ExemptFromCertifiedReporting == true)
				return ZeroRate;

			PMLaborCostRate certifiedProjectRate =
				SelectFrom<PMLaborCostRate>.
				Where<PMLaborCostRate.inventoryID.IsEqual<PREarningDetail.labourItemID.FromCurrent>.
					And<PMLaborCostRate.effectiveDate.IsLessEqual<PREarningDetail.date.FromCurrent>>.
					And<PMLaborCostRate.employeeID.IsNull.Or<PMLaborCostRate.employeeID.IsEqual<PREarningDetail.employeeID.FromCurrent>>>.
					And<PMLaborCostRate.projectID.IsEqual<PREarningDetail.projectID.FromCurrent>.
					And<PMLaborCostRate.type.IsEqual<PMLaborCostRateType.certified>>.
					And<PMLaborCostRate.taskID.IsNull.Or<PMLaborCostRate.taskID.IsEqual<PREarningDetail.projectTaskID.FromCurrent>>>>>.
				OrderBy<PMLaborCostRate.effectiveDate.Desc>.View.
				SelectSingleBound(graph, new[] { earningDetail });

			if (certifiedProjectRate?.WageRate == null)
				return ZeroRate;

			return certifiedProjectRate.WageRate.Value * (GetOvertimeMultiplier(graph, earningDetail) ?? 1);
		}

		private static decimal GetProjectRate(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail.ProjectID == null)
				return ZeroRate;

			PMLaborCostRate projectRate =
				SelectFrom<PMLaborCostRate>.
				Where<PMLaborCostRate.effectiveDate.IsLessEqual<PREarningDetail.date.FromCurrent>.
					And<PMLaborCostRate.inventoryID.IsNull.Or<PMLaborCostRate.inventoryID.IsEqual<PREarningDetail.labourItemID.FromCurrent>>>.
					And<PMLaborCostRate.employeeID.IsNull.Or<PMLaborCostRate.employeeID.IsEqual<PREarningDetail.employeeID.FromCurrent>>>.
					And<PMLaborCostRate.projectID.IsEqual<PREarningDetail.projectID.FromCurrent>.
					And<PMLaborCostRate.type.IsEqual<PMLaborCostRateType.project>>.
					And<PMLaborCostRate.taskID.IsNull.Or<PMLaborCostRate.taskID.IsEqual<PREarningDetail.projectTaskID.FromCurrent>>>>>.
				OrderBy<PMLaborCostRate.effectiveDate.Desc>.View.
				SelectSingleBound(graph, new[] { earningDetail });

			if (projectRate?.WageRate == null)
				return ZeroRate;

			return projectRate.WageRate.Value * (GetOvertimeMultiplier(graph, earningDetail) ?? 1);
		}

		private static decimal GetLaborItemRate(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail.LabourItemID == null)
				return ZeroRate;

			PMLaborCostRate laborItemRate =
				SelectFrom<PMLaborCostRate>.
				Where<PMLaborCostRate.effectiveDate.IsLessEqual<PREarningDetail.date.FromCurrent>.
					And<PMLaborCostRate.inventoryID.IsEqual<PREarningDetail.labourItemID.FromCurrent>>.
					And<PMLaborCostRate.type.IsEqual<PMLaborCostRateType.item>>.
					And<PMLaborCostRate.employeeID.IsNull.Or<PMLaborCostRate.employeeID.IsEqual<PREarningDetail.employeeID.FromCurrent>>>.
					And<PMLaborCostRate.projectID.IsNull.Or<PMLaborCostRate.projectID.IsEqual<PREarningDetail.projectID.FromCurrent>>>.					
					And<PMLaborCostRate.taskID.IsNull.Or<PMLaborCostRate.taskID.IsEqual<PREarningDetail.projectTaskID.FromCurrent>>>>.
				OrderBy<PMLaborCostRate.effectiveDate.Desc>.View.
				SelectSingleBound(graph, new[] { earningDetail });

			if (laborItemRate?.WageRate == null)
				return ZeroRate;

			return laborItemRate.WageRate.Value * (GetOvertimeMultiplier(graph, earningDetail) ?? 1);
		}

		private static decimal? GetOvertimeMultiplier(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail.IsOvertime != true)
			{
				return null;
			}

			EPEarningType overTimeEarningType = GetEarningTypeRecord(graph, earningDetail.TypeCD);

			return overTimeEarningType?.OvertimeMultiplier;
		}

		private static EPEarningType GetEarningTypeRecord(PXGraph graph, string typeCD)
		{
			EPEarningType record = SelectFrom<EPEarningType>.
				Where<EPEarningType.isActive.IsEqual<True>.
					And<EPEarningType.typeCD.IsEqual<P.AsString>>>.View.SelectSingleBound(graph, null, typeCD);

			if (record == null)
			{
				PXTrace.WriteWarning(Messages.EarningTypeNotFound, typeCD, typeof(EPEarningType).Name);
			}

			return record;
		}

		#region Avoid breaking changes for 2020R2

		// The handler is not deleted to avoid breaking changes in the Minor Update
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
		}

		//[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)] does not compile with this attribute
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
		}

		#endregion
	}
}
