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
    public sealed class ExpenseClaimDetailEntryMultipleBaseCurrencies : PXGraphExtension<ExpenseClaimDetailEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }

        protected void _(Events.FieldVerifying<EPExpenseClaimDetails, EPExpenseClaimDetails.branchID> e)
        {
            if (Base.ClaimDetails.Current == null || Base.ClaimDetails.Current.EmployeeID == null)
            {
                return;
            }

            int? newVal = e.NewValue as int?;
            if (newVal > 0)
            {
                PXUIFieldAttribute.SetError<EPExpenseClaimDetails.branchID>(Base.ClaimDetails.Cache, Base.ClaimDetails.Current, null);

                var branchInfo = PXAccess.GetBranch(newVal);
                EPEmployee employeeRow = PXSelect<EPEmployee>.Search<EPEmployee.bAccountID>(Base, Base.ClaimDetails.Current?.EmployeeID);
                if (employeeRow.BaseCuryID != branchInfo.BaseCuryID)
                {
                    e.NewValue = branchInfo.BranchCD;
                    throw new PXSetPropertyException(CS.Messages.BranchBaseCurrencyDifferFromEmployee, employeeRow.AcctCD);
                }
            }
        }

        protected void _(Events.FieldUpdated<EPExpenseClaimDetails.branchID> e)
        {
            var row = e.Row as EPExpenseClaimDetails;
            if(row.BranchID == null) 
            {
                return;
            }

            var branchInfo = PXAccess.GetBranch(row.BranchID);
            if (row.CuryID == branchInfo.BaseCuryID)
            {
                CurrencyInfo info = CurrencyInfoAttribute.SetDefaults<EPExpenseClaim.curyInfoID>(e.Cache, row);
                if (info != null)
                {
                    row.CuryID = info.CuryID;
                }
            }
            else
            {
                EPEmployee employeeRow = PXSelect<EPEmployee>.Search<EPEmployee.bAccountID>(Base, Base.ClaimDetails.Current?.EmployeeID);
                CurrencyInfo baseCuryInfo = CreateRate(Base, row.CuryID, branchInfo.BaseCuryID, row.ExpenseDate, employeeRow);
                if (baseCuryInfo != null)
                {
                    row.CuryID = baseCuryInfo.CuryID;
                    row.CuryInfoID = baseCuryInfo.CuryInfoID;
                }
            }
        }

        protected void _(Events.FieldUpdated<EPExpenseClaimDetails.corpCardID> e)
        {
            var row = e.Row as EPExpenseClaimDetails;
            CalculateCorpCurrencyInfo(e.Cache, row);
        }

        protected void _(Events.FieldUpdated<EPExpenseClaimDetails.employeeID> e)
        {
            var row = e.Row as EPExpenseClaimDetails;
            CalculateCorpCurrencyInfo(e.Cache, row);
        }

        protected void _(Events.FieldUpdated<EPExpenseClaimDetails.billable> e)
        {
            var row = e.Row as EPExpenseClaimDetails;
            var isBillable = e.NewValue as bool?;
            if(isBillable == true && row.CustomerID != null)
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

        protected void _(Events.FieldVerifying<EPExpenseClaimDetails, EPExpenseClaimDetails.customerID> e)
        {
            if(e.Row == null)
            {
                return;
            }
            EPExpenseClaimDetails claimDetail = e.Row;
            int? newVal = e.NewValue as int?;
            if (newVal > 0)
            {
                PXUIFieldAttribute.SetError<EPExpenseClaim.customerID>(Base.ClaimDetails.Cache, Base.ClaimDetails.Current, null);

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

                    PXUIFieldAttribute.SetError<EPExpenseClaim.customerID>(e.Cache, e.Row,
                        string.Format(CS.Messages.BranchBaseCurrencyDifferFromCustomerOnExpenseReceipt, customer.AcctCD), customer.AcctCD);
                }
            }
        }

        private static CurrencyInfo CreateRate(PXGraph graph, string curyID, string baseCuryID, DateTime? date, EPEmployee employee)
        {
            CM.Extensions.IPXCurrencyService currencyService = ServiceLocator.Current.GetInstance<Func<PXGraph, CM.Extensions.IPXCurrencyService>>()(graph);

            CurrencyInfo result = new CurrencyInfo();
            result.ModuleCode = GL.BatchModule.EP;
            result.BaseCuryID = baseCuryID;
            result.CuryID = curyID;
            result.CuryRateTypeID = employee != null ? employee.CuryRateTypeID ?? currencyService.DefaultRateTypeID(GL.BatchModule.CA) : currencyService.DefaultRateTypeID(GL.BatchModule.CA);
            result.CuryEffDate = date;

            var rate = currencyService.GetRate(result.CuryID, result.BaseCuryID, result.CuryRateTypeID, result.CuryEffDate);
            if (rate == null)
            {
                CurrencyInfo dflt = new CurrencyInfo();
                graph.Caches[typeof(CurrencyInfo)].SetDefaultExt<CurrencyInfo.curyRate>(dflt);
                graph.Caches[typeof(CurrencyInfo)].SetDefaultExt<CurrencyInfo.curyMultDiv>(dflt);
                graph.Caches[typeof(CurrencyInfo)].SetDefaultExt<CurrencyInfo.recipRate>(dflt);
                result.CuryRate = Math.Round((decimal)dflt.CuryRate, 8);
                result.CuryMultDiv = dflt.CuryMultDiv;
                result.RecipRate = Math.Round((decimal)dflt.RecipRate, 8);
            }
            else
            {
                result.CuryRate = rate.CuryRate;
                result.CuryMultDiv = rate.CuryMultDiv;
                result.RecipRate = rate.RateReciprocal;
            }

            result = (CurrencyInfo)graph.Caches[typeof(CurrencyInfo)].Insert(result);
            return result;
        }

        private void CalculateCorpCurrencyInfo(PXCache cache, EPExpenseClaimDetails row)
        {
            if(row.BranchID == null || row.CardCuryID == null)
            {
                return;
            }

            var branchInfo = PXAccess.GetBranch(row.BranchID);
            EPEmployee employeeRow = PXSelect<EPEmployee>.Search<EPEmployee.bAccountID>(Base, Base.ClaimDetails.Current?.EmployeeID);
            CurrencyInfo baseCuryInfo = CreateRate(Base, row.CardCuryID, branchInfo.BaseCuryID, row.ExpenseDate, employeeRow);
            if (baseCuryInfo != null)
            {
                cache.SetValueExt<EPExpenseClaimDetails.cardCuryID>(row, baseCuryInfo.CuryID);
                cache.SetValueExt<EPExpenseClaimDetails.cardCuryInfoID>(row, baseCuryInfo.CuryInfoID);
                if (row.IsPaidWithCard)
                {
                    cache.SetValueExt<EPExpenseClaimDetails.claimCuryInfoID>(row, baseCuryInfo.CuryInfoID);
                }
            }
        }
    }
}