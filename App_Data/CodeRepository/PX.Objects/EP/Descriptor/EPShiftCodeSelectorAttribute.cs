using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;
using System.Linq;

namespace PX.Objects.EP
{
	public abstract class EPShiftCodeSelectorAttribute : PXSelectorAttribute, IPXRowInsertingSubscriber, IPXRowPersistingSubscriber
	{
		protected Type _DateField;

		protected EPShiftCodeSelectorAttribute(Type compareDateField) : base(BqlTemplate.OfCommand<
				SelectFrom<EPShiftCode>
					.LeftJoin<EPShiftCodeRate>.On<EPShiftCodeRate.shiftID.IsEqual<EPShiftCode.shiftID>
						.And<EPShiftCodeRate.effectiveDate.IsLessEqual<BqlPlaceholder.A.AsField.AsOptional>.Or<BqlPlaceholder.A.AsField.AsOptional.IsNull>>>
					.Where<EPShiftCode.isManufacturingShift.IsEqual<False>>
					.AggregateTo<GroupBy<EPShiftCode.shiftID>>
					.SearchFor<EPShiftCode.shiftID>>
				.Replace<BqlPlaceholder.A>(compareDateField)
				.ToType())
		{
			SubstituteKey = typeof(EPShiftCode.shiftCD);
			DescriptionField = typeof(EPShiftCode.description);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (_DateField != null)
			{
				sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _DateField.Name, (cache, e) =>
				{
					object shiftCodeValue = cache.GetValue(e.Row, _FieldName);
					if (e.OldValue == null && shiftCodeValue == null)
					{
						SetDefaultShiftCode(cache, e.Row);
						shiftCodeValue = cache.GetValue(e.Row, _FieldName);
					}

					try
					{
						cache.RaiseFieldVerifying(_FieldName, e.Row, ref shiftCodeValue);
					}
					catch (Exception ex)
					{
						cache.RaiseExceptionHandling(_FieldName, e.Row, shiftCodeValue, ex);
					}
				});
			}
		}

		public void RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			object shiftCodeValue = sender.GetValue(e.Row, _FieldName);
			if (shiftCodeValue == null)
			{
				SetDefaultShiftCode(sender, e.Row);
			}
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object shiftCodeValue = sender.GetValue(e.Row, _FieldName);
			try
			{
				sender.RaiseFieldVerifying(_FieldName, e.Row, ref shiftCodeValue);
			}
			catch (Exception ex)
			{
				sender.RaiseExceptionHandling(_FieldName, e.Row, shiftCodeValue, ex);
			}
		}

		protected virtual void SetDefaultShiftCode(PXCache cache, object row)
		{
			EPEmployee employee = GetEmployee(cache, row);
			if (employee?.ShiftID != null && IsShiftCodeEffective(cache, row, employee.ShiftID))
			{
				cache.SetValue(row, _FieldName, employee.ShiftID);
			}
		}

		protected virtual bool IsShiftCodeEffective(PXCache cache, object row, int? shiftID)
		{
			return new PXView(cache.Graph, true, _LookupSelect).SelectMulti(GetQueryParameters(cache, row))
				.Select(x => new { ShiftCode = (EPShiftCode)((PXResult)x)[typeof(EPShiftCode)], Rate = (EPShiftCodeRate)((PXResult)x)[typeof(EPShiftCodeRate)] })
				.Any(x => x.ShiftCode?.IsActive == true && x.Rate?.ShiftID == shiftID);
		}

		protected abstract EPEmployee GetEmployee(PXCache cache, object row);
		protected abstract object[] GetQueryParameters(PXCache cache, object row);
	}

	public abstract class EPShiftCodeSelectorWithEmployeeIDAttribute : EPShiftCodeSelectorAttribute
	{
		private Type _EmployeeIDField;

		protected EPShiftCodeSelectorWithEmployeeIDAttribute(Type employeeIDField, Type compareDateField) : base(compareDateField)
		{
			_EmployeeIDField = employeeIDField;
		}

		protected override EPEmployee GetEmployee(PXCache cache, object row)
		{
			int? employeeID = (int?)cache.GetValue(row, _EmployeeIDField.Name);
			return new SelectFrom<EPEmployee>.Where<EPEmployee.bAccountID.IsEqual<P.AsInt>>.View(cache.Graph).SelectSingle(employeeID);
		}
	}

	public class DetailShiftCodeSelectorAttribute : EPShiftCodeSelectorWithEmployeeIDAttribute
	{
		public DetailShiftCodeSelectorAttribute(Type employeeIDField, Type dateField) : base(employeeIDField, dateField)
		{
			_DateField = dateField;
		}

		protected override object[] GetQueryParameters(PXCache cache, object row)
		{
			return new[] { cache.GetValue(row, _DateField.Name) };
		}
	}

	public class TimeActivityShiftCodeSelectorAttribute : EPShiftCodeSelectorAttribute
	{
		private Type _OwnerIDField;

		public TimeActivityShiftCodeSelectorAttribute(Type ownerIDField, Type dateField) : base(dateField)
		{
			_OwnerIDField = ownerIDField;
			_DateField = dateField;
		}

		protected override EPEmployee GetEmployee(PXCache cache, object row)
		{
			int? ownerID = (int?)cache.GetValue(row, _OwnerIDField.Name);
			return new SelectFrom<EPEmployee>.Where<EPEmployee.defContactID.IsEqual<P.AsInt>>.View(cache.Graph).SelectSingle(ownerID);
		}

		protected override object[] GetQueryParameters(PXCache cache, object row)
		{
			return new[] { cache.GetValue(row, _DateField.Name) };
		}
	}

	public class TimeCardShiftCodeSelectorAttribute : EPShiftCodeSelectorWithEmployeeIDAttribute
	{
		private Type _WeekEndField;

		public TimeCardShiftCodeSelectorAttribute(Type employeeIDField, Type weekEndField) : base(employeeIDField, weekEndField)
		{
			_WeekEndField = weekEndField;
		}

		protected override object[] GetQueryParameters(PXCache cache, object row)
		{
			return new[] { CacheHelper.GetCurrentValue(cache.Graph, _WeekEndField) };
		}
	}
}
