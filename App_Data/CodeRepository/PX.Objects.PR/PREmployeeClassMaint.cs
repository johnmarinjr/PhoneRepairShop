using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.PM;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PREmployeeClassMaint : PXGraph<PREmployeeClassMaint, PREmployeeClass>
	{
		#region Views
		public SelectFrom<PREmployeeClass>
			.Where<MatchPRCountry<PREmployeeClass.countryID>>.View EmployeeClass;
		public PXSelect<PREmployeeClass, Where<PREmployeeClass.employeeClassID, Equal<Current<PREmployeeClass.employeeClassID>>>> CurEmployeeClassRecord;

		public SelectFrom<PREmployeeClassPTOBank>
			.Where<PREmployeeClassPTOBank.employeeClassID.IsEqual<PREmployeeClass.employeeClassID.FromCurrent>>.View EmployeeClassPTOBanks;

		public SelectFrom<PREmployee>
			.LeftJoin<PREmployeePTOBank>.On<PREmployee.bAccountID.IsEqual<PREmployeePTOBank.bAccountID>
				.And<PREmployeePTOBank.bankID.IsEqual<P.AsString>>>
			.Where<PREmployee.employeeClassID.IsEqual<PREmployeeClass.employeeClassID.FromCurrent>
			.And<PREmployee.usePTOBanksFromClass.IsEqual<True>
			.And<PREmployeePTOBank.bankID.IsNull>>>.View ClassEmployeesWithoutBank;

		public SelectFrom<PREmployeePTOBank>
			.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PREmployeePTOBank.bAccountID>>
			.Where<PREmployeePTOBank.bankID.IsEqual<P.AsString>
				.And<PREmployee.employeeClassID.IsEqual<PREmployeeClass.employeeClassID.FromCurrent>>
				.And<PREmployee.usePTOBanksFromClass.IsEqual<True>>
				.And<PREmployeePTOBank.useClassDefault.IsEqual<True>
					.Or<PREmployeePTOBank.useClassDefault.IsEqual<P.AsBool>>>>.View EmployeePTOBanks;

		public SelectFrom<PREmployeeClassWorkLocation>
			.InnerJoin<PRLocation>.On<PRLocation.locationID.IsEqual<PREmployeeClassWorkLocation.locationID>>
			.InnerJoin<Address>.On<Address.addressID.IsEqual<PRLocation.addressID>>
			.Where<PREmployeeClassWorkLocation.employeeClassID.IsEqual<PREmployeeClass.employeeClassID.FromCurrent>
				.And<Address.countryID.IsEqual<PREmployeeClass.countryID.FromCurrent>>>.View WorkLocations;
		#endregion

		#region Event Handlers
		public void _(Events.FieldUpdated<PREmployeeClassPTOBank.bankID> e)
		{
			var row = (PREmployeeClassPTOBank)e.Row;
			if (row?.BankID == null)
			{
				return;
			}

			var bank = (PRPTOBank)PXSelectorAttribute.Select<PREmployeeClassPTOBank.bankID>(e.Cache, row);
			if (bank == null)
			{
				return;
			}

			row.IsActive = bank.IsActive;
			row.AccrualMethod = bank.AccrualMethod;
			row.AccrualRate = bank.AccrualRate;
			row.HoursPerYear = bank.HoursPerYear;
			row.AccrualLimit = bank.AccrualLimit;
			row.StartDate = bank.StartDate;
			row.CarryoverType = bank.CarryoverType;
			row.CarryoverAmount = bank.CarryoverAmount;
			row.FrontLoadingAmount = bank.FrontLoadingAmount;
		}

		public void _(Events.FieldVerifying<PREmployeeClassPTOBank.bankID> e)
		{
			if (e.Row == null || e.NewValue == null)
			{
				return;
			}

			if (PXSelectorAttribute.Select<PREmployeeClassPTOBank.bankID>(e.Cache, e.Row, e.NewValue) == null)
			{
				throw new PXSetPropertyException<PREmployeeClassPTOBank.bankID>(ErrorMessages.ValueDoesntExist, nameof(PREmployeeClassPTOBank.bankID), e.NewValue);
			}
		}

		public void _(Events.RowSelected<PREmployeeClassPTOBank> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PREmployeeClassPTOBank.bankID>(e.Cache, e.Row, e.Row.BankID == null);
		}

		public void _(Events.RowInserted<PREmployeeClassPTOBank> e)
		{
			var row = (PREmployeeClassPTOBank)e.Row;
			if (row?.BankID == null)
			{
				return;
			}

			foreach (PREmployee employee in ClassEmployeesWithoutBank.Select(row.BankID))
			{
				var newBank = new PREmployeePTOBank();
				newBank.BAccountID = employee.BAccountID;
				newBank.BankID = row.BankID;
				newBank.UseClassDefault = true;
				newBank.AccrualMethod = row.AccrualMethod;
				newBank.AccrualRate = row.AccrualRate;
				newBank.HoursPerYear = row.HoursPerYear;
				newBank.AccrualLimit = row.AccrualLimit;
				newBank.CarryoverType = row.CarryoverType;
				newBank.CarryoverAmount = row.CarryoverAmount;
				newBank.FrontLoadingAmount = row.FrontLoadingAmount;
				newBank.StartDate = row.StartDate;
				newBank.DisbursingType = row.DisbursingType;

				EmployeePTOBanks.Insert(newBank);
			}

			foreach (PREmployeePTOBank employeeBank in EmployeePTOBanks.Select(row.BankID, false))
			{
				employeeBank.IsActive = true;
				EmployeePTOBanks.Update(employeeBank);
			}
		}

		public void _(Events.FieldUpdated<PREmployeeClassPTOBank.isActive> e)
		{
			PREmployeeClassPTOBank row = e.Row as PREmployeeClassPTOBank;
			if (row == null)
			{
				return;
			}

			foreach (PREmployeePTOBank employeeBank in EmployeePTOBanks.Select(row.BankID, false))
			{
				bool isActive = (e.NewValue as bool?) ?? false;

				if (isActive || employeeBank.UseClassDefault == true)
				{
					employeeBank.IsActive = e.NewValue as bool?;
					EmployeePTOBanks.Update(employeeBank);
				}
			}
		}

		public void _(Events.RowDeleted<PREmployeeClassPTOBank> e)
		{
			foreach (PREmployeePTOBank employeeBank in EmployeePTOBanks.Select(e.Row.BankID))
			{
				employeeBank.IsActive = false;
				employeeBank.UseClassDefault = false;
				EmployeePTOBanks.Update(employeeBank);
			}
		}

		public virtual void _(Events.FieldVerifying<PREmployeeClassWorkLocation.isDefault> e)
		{
			if (!e.ExternalCall)
			{
				return;
			}

			bool? newValueBool = e.NewValue as bool?;
			bool requestRefresh = false;
			if (newValueBool == true)
			{
				WorkLocations.Select().FirstTableItems.Where(x => x.IsDefault == true).ForEach(x =>
				{
					x.IsDefault = false;
					WorkLocations.Update(x);
					requestRefresh = true;
				});
			}
			else if (newValueBool == false && !WorkLocations.Select().FirstTableItems.Any(x => x.IsDefault == true && !x.LocationID.Equals(e.Cache.GetValue<PREmployeeClassWorkLocation.locationID>(e.Row))))
			{
				e.NewValue = true;
			}

			if (requestRefresh)
			{
				WorkLocations.View.RequestRefresh();
			}
		}

		public virtual void _(Events.RowInserting<PREmployeeClassWorkLocation> e)
		{
			if (!WorkLocations.Select().FirstTableItems.Any(x => x.IsDefault == true))
			{
				e.Row.IsDefault = true;
			}
		}

		public virtual void _(Events.RowDeleted<PREmployeeClassWorkLocation> e)
		{
			IEnumerable<PREmployeeClassWorkLocation> remainingWorkLocations = WorkLocations.Select().FirstTableItems;
			if (!remainingWorkLocations.Any(x => x.IsDefault == true))
			{
				PREmployeeClassWorkLocation newDefault = remainingWorkLocations.FirstOrDefault();
				if (newDefault != null)
				{
					newDefault.IsDefault = true;
					WorkLocations.Update(newDefault);
					WorkLocations.View.RequestRefresh();
				}
			}
		}
		public void _(Events.FieldUpdated<PREmployeeClass, PREmployeeClass.empType> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var newValue = (string)e.NewValue;
			if (newValue == EmployeeType.SalariedExempt)
			{
				e.Row.ExemptFromOvertimeRules = true;
			}
			else if (newValue == EmployeeType.SalariedNonExempt)
			{
				e.Row.ExemptFromOvertimeRules = false;
			}
		}

		public void _(Events.RowSelected<PREmployeeClass> e)
		{
			if (e.Row == null)
			{
				return;
			}

			bool isSalaried = EmployeeType.IsSalaried(e.Row.EmpType);
			PXUIFieldAttribute.SetEnabled<PREmployeeClass.exemptFromOvertimeRules>(e.Cache, e.Row, !isSalaried);
		}

		public virtual void _(Events.FieldSelecting<PREmployeeClass.workCodeID> e)
		{
			PREmployeeClass row = e.Row as PREmployeeClass;
			if (row == null || row.WorkCodeID == null)
			{
				return;
			}

			PMWorkCode workCode = PXSelectorAttribute.Select<PREmployeeClass.workCodeID>(e.Cache, row) as PMWorkCode;
			if (workCode == null)
			{
				e.ReturnValue = null;
				row.WorkCodeID = null;
				e.Cache.Update(row);
			}
		}
		#endregion
	}
}