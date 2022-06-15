using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using System;
using System.Linq;

namespace PX.Objects.EP
{
	public class EPShiftCodeSetup : PXGraph<EPShiftCodeSetup>
	{
		public PXSave<EPShiftCode> Save;
		public PXCancel<EPShiftCode> Cancel;

		#region Data Views
		public SelectFrom<EPShiftCode>
			.Where<EPShiftCode.isManufacturingShift.IsEqual<False>>.View Codes;

		public SelectFrom<EPShiftCodeRate>
			.Where<EPShiftCodeRate.FK.ShiftCode.SameAsCurrent>.View Rates;
		#endregion Data Views

		#region Event Handlers
		public virtual void _(Events.FieldUpdated<EPShiftCodeRate, EPShiftCodeRate.wageAmount> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var newWageAmount = e.NewValue as decimal?;
			if (newWageAmount > e.Row.CostingAmount)
			{
				e.Row.CostingAmount = newWageAmount;
				PXUIFieldAttribute.SetWarning<EPShiftCodeRate.costingAmount>(e.Cache, e.Row, Messages.CostingTooLow);
			}
		}

		public virtual void _(Events.FieldUpdated<EPShiftCodeRate, EPShiftCodeRate.costingAmount> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var newCostingAmount = e.NewValue as decimal?;
			if (newCostingAmount < e.Row.WageAmount)
			{
				e.Row.WageAmount = newCostingAmount;
				PXUIFieldAttribute.SetWarning<EPShiftCodeRate.wageAmount>(e.Cache, e.Row, Messages.WageTooHigh);
			}
		}

		public virtual void _(Events.RowSelected<EPShiftCodeRate> e)
		{
			if (e.Row == null)
			{
				return;
			}

			bool enable = e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted;
			PXUIFieldAttribute.SetEnabled<EPShiftCodeRate.effectiveDate>(e.Cache, e.Row, enable);
			PXUIFieldAttribute.SetEnabled<EPShiftCodeRate.type>(e.Cache, e.Row, enable);
			PXUIFieldAttribute.SetEnabled<EPShiftCodeRate.percent>(e.Cache, e.Row, enable && e.Row.Type == EPShiftCodeType.Percent);
			PXUIFieldAttribute.SetEnabled<EPShiftCodeRate.wageAmount>(e.Cache, e.Row, enable && e.Row.Type == EPShiftCodeType.Amount);
			PXUIFieldAttribute.SetEnabled<EPShiftCodeRate.costingAmount>(e.Cache, e.Row, enable && e.Row.Type == EPShiftCodeType.Amount);
		}

		public virtual void _(Events.FieldVerifying<EPShiftCodeRate, EPShiftCodeRate.effectiveDate> e)
		{
			var newValue = (DateTime)e.NewValue;
			var lastDate = Rates.Select().FirstTableItems.OrderByDescending(x => x.EffectiveDate).FirstOrDefault()?.EffectiveDate;
			if (newValue < lastDate)
			{
				e.Cache.RaiseExceptionHandling<EPShiftCodeRate.effectiveDate>(e.Row, e.OldValue, new PXSetPropertyException(Messages.DateMustBeLatest, lastDate));
				e.NewValue = null;
			}
		}

		public virtual void _(Events.RowPersisting<EPShiftCodeRate> e)
		{
			if (e.Row.Type == EPShiftCodeType.Amount)
			{
				if (!PXAccess.FeatureInstalled<FeaturesSet.payrollModule>())
				{
					e.Row.WageAmount = e.Row.CostingAmount;
				}

				if (e.Row.WageAmount == null && PXAccess.FeatureInstalled<FeaturesSet.payrollModule>())
				{
					e.Cache.RaiseExceptionHandling<EPShiftCodeRate.wageAmount>(e.Row, null,
					   new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeRate.wageAmount>(e.Cache)));
				}
				if (e.Row.CostingAmount == null)
				{
					e.Cache.RaiseExceptionHandling<EPShiftCodeRate.costingAmount>(e.Row, null,
					   new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeRate.costingAmount>(e.Cache)));
				}
			}
			else if (e.Row.Percent == null)
			{
				e.Cache.RaiseExceptionHandling<EPShiftCodeRate.percent>(e.Row, null,
				   new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeRate.percent>(e.Cache)));
			}
		}
		#endregion Event Handlers

		#region Helpers
		public static decimal CalculateShiftWage(PXGraph graph, int? shiftID, DateTime? date, decimal originalWage, decimal otMultiplier)
		{
			EPShiftCodeRate shiftRate = GetEffectiveRate(graph, shiftID, date);
			if (shiftRate == null)
			{
				return originalWage;
			}
			else if (shiftRate.Type == EPShiftCodeType.Amount)
			{
				return originalWage += shiftRate.WageAmount.GetValueOrDefault() * otMultiplier;
			}
			else
			{
				return originalWage += originalWage * shiftRate.Percent.GetValueOrDefault() / 100;
			}
		}

		public static decimal CalculateShiftCosting(PXGraph graph, int? shiftID, DateTime? date, decimal originalCost, decimal otMultiplier)
		{
			EPShiftCodeRate shiftRate = GetEffectiveRate(graph, shiftID, date);
			if (shiftRate == null)
			{
				return originalCost;
			}
			else if (shiftRate.Type == EPShiftCodeType.Amount)
			{
				return originalCost += shiftRate.CostingAmount.GetValueOrDefault() * otMultiplier;
			}
			else
			{
				return originalCost += originalCost * shiftRate.Percent.GetValueOrDefault() / 100;
			}
		}

		private static EPShiftCodeRate GetEffectiveRate(PXGraph graph, int? shiftID, DateTime? date)
		{
			return new SelectFrom<EPShiftCodeRate>
				.Where<EPShiftCodeRate.shiftID.IsEqual<P.AsInt>
					.And<EPShiftCodeRate.effectiveDate.IsLessEqual<P.AsDateTime>>>
				.OrderBy<EPShiftCodeRate.effectiveDate.Desc>.View(graph).Select(shiftID, date);
		}
		#endregion Helpers
	}
}
