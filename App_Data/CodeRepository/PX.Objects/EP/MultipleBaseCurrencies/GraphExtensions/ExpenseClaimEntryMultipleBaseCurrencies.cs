using System.Collections;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.CM;
using System;
using CommonServiceLocator;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Data.BQL;
using PX.Objects.IN;

namespace PX.Objects.EP
{
    public sealed class ExpenseClaimEntryMultipleBaseCurrencies : PXGraphExtension<ExpenseClaimEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }

        protected void _(Events.FieldVerifying<EPExpenseClaim, EPExpenseClaim.branchID> e)
        {
            if (Base.ExpenseClaimCurrent.Current == null || Base.ExpenseClaimCurrent.Current.EmployeeID == null)
            {
                return;
            }

            int? newVal = e.NewValue as int?;
            if (newVal > 0)
            {
                PXUIFieldAttribute.SetError<EPExpenseClaim.branchID>(Base.ExpenseClaimCurrent.Cache, Base.ExpenseClaimCurrent.Current, null);

                var branchInfo = PXAccess.GetBranch(newVal);
                EPEmployee employeeRow = PXSelect<EPEmployee>.Search<EPEmployee.bAccountID>(Base, Base.ExpenseClaimCurrent.Current.EmployeeID);
                if (employeeRow.BaseCuryID != branchInfo.BaseCuryID)
                {
                    e.NewValue = branchInfo.BranchCD;
                    e.Cancel = true;

                    throw new PXSetPropertyException(CS.Messages.BranchBaseCurrencyDifferFromEmployee, employeeRow.AcctCD);
                }
            }
        }

        protected void _(Events.FieldVerifying<EPExpenseClaim, EPExpenseClaim.customerID> e)
        {
            EPExpenseClaim claim = e.Row;
            int? newVal = e.NewValue as int?;
            if (newVal > 0)
            {
                PXUIFieldAttribute.SetError<EPExpenseClaim.customerID>(Base.ExpenseClaimCurrent.Cache, Base.ExpenseClaimCurrent.Current, null);

                var billable = Base.ExpenseClaimDetails.Select<EPExpenseClaimDetails>().AsEnumerable().Any(a => a.Billable == true);
                if (!billable)
                {
                    return;
                }

                var branchInfo = PXAccess.GetBranch(claim.BranchID);
                var customer = Customer.PK.Find(Base, newVal);
                if (customer.BaseCuryID != branchInfo.BaseCuryID)
                {
                    e.NewValue = customer.AcctCD;
                    e.Cancel = true;

                    throw new PXSetPropertyException(CS.Messages.BranchBaseCurrencyDifferFromCustomer, customer.AcctCD);
                }
            }
        }

        protected void _(Events.FieldVerifying<EPExpenseClaimDetails, EPExpenseClaimDetails.customerID> e)
        {
            if (e.Row == null)
            {
                return;
            }
            EPExpenseClaimDetails claimDetail = e.Row;
            int? newVal = e.NewValue as int?;
            if (newVal > 0)
            {
                PXUIFieldAttribute.SetError<EPExpenseClaimDetails.customerID>(Base.ExpenseClaimDetailsCurrent.Cache, Base.ExpenseClaimDetailsCurrent.Current, null);

                if (claimDetail.Billable != true)
                {
                    return;
                }

                var branchInfo = PXAccess.GetBranch(claimDetail.BranchID);
                var customer = Customer.PK.Find(Base, newVal);
                if (customer.BaseCuryID != branchInfo.BaseCuryID)
                {
                    e.NewValue = customer.AcctCD;
                    e.Cancel = true;

                    PXUIFieldAttribute.SetError<EPExpenseClaimDetails.customerID>(Base.ExpenseClaimDetailsCurrent.Cache, Base.ExpenseClaimDetailsCurrent.Current,
                        string.Format(CS.Messages.BranchBaseCurrencyDifferFromCustomerOnExpenseReceipt, customer.AcctCD), customer.AcctCD);
                }
            }
        }

        protected void _(Events.FieldUpdated<EPExpenseClaimDetails.billable> e)
        {
            var row = e.Row as EPExpenseClaimDetails;
            var isBillable = e.NewValue as bool?;
            if (isBillable == true && row.CustomerID != null)
            {
                var branchInfo = PXAccess.GetBranch(row.BranchID);
                var customer = Customer.PK.Find(Base, row.CustomerID);
                if (customer.BaseCuryID != branchInfo.BaseCuryID)
                {
                    PXUIFieldAttribute.SetError<EPExpenseClaim.customerID>(e.Cache, e.Row,
                        string.Format(CS.Messages.BranchBaseCurrencyDifferFromCustomerOnExpenseReceipt, customer.AcctCD), customer.AcctCD);
                }
            }
		}

		protected void _(Events.FieldUpdated<EPExpenseClaimDetails.paidWith> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetError<EPExpenseClaimDetails.corpCardID>(Base.ExpenseClaimDetailsCurrent.Cache, Base.ExpenseClaimDetailsCurrent.Current, null);
			e.Cache.SetValueExt<EPExpenseClaimDetails.corpCardID>(Base.ExpenseClaimDetailsCurrent.Current, null);
		}
	}
}
