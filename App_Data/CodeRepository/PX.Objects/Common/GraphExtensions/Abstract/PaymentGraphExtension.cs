using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.GraphExtensions.Abstract.DAC;
using PX.Objects.Common.GraphExtensions.Abstract.Mapping;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.Common.GraphExtensions.Abstract
{
	public abstract class PaymentGraphExtension<TGraph, TPayment, TAdjust, TInvoice, TTran> : PXGraphExtension<TGraph>,
        IDocumentWithFinDetailsGraphExtension
        where TGraph : PXGraph
        where TPayment : class, IBqlTable, CM.IInvoice, new()
        where TAdjust : class, IBqlTable, IFinAdjust, new()
        where TInvoice : class, IBqlTable, CM.IInvoice, new()
        where TTran : class, IBqlTable, CM.IDocumentTran, new()
    {
        protected abstract PaymentMapping GetPaymentMapping();

        /// <summary>A mapping-based view of the <see cref="Payment" /> data.</summary>
        public PXSelectExtension<Payment> Documents;

        public abstract PXSelectBase<TAdjust> Adjustments { get; }

        protected abstract AbstractPaymentBalanceCalculator<TAdjust, TTran> GetAbstractBalanceCalculator();

        private AbstractPaymentBalanceCalculator<TAdjust, TTran> _balanceCalculator = null;
        private AbstractPaymentBalanceCalculator<TAdjust, TTran> BalanceClaculator => _balanceCalculator ?? (_balanceCalculator = GetAbstractBalanceCalculator());

        protected abstract bool InternalCall { get; }

        protected IPXCurrencyHelper curyHelper => BalanceClaculator.curyHelper;

        protected virtual bool DiscOnDiscDate => false;

        #region AC-120642 - period setting by master and validation on DataEntry

        public List<int?> GetOrganizationIDsInDetails()
        {
            return Adjustments
                .Select()
                .AsEnumerable()
                .SelectMany(row => GetAdjustBranchIDs(row))
                .Select(PXAccess.GetParentOrganizationID)
                .Distinct()
                .ToList();
        }

        protected virtual IEnumerable<int?> GetAdjustBranchIDs(TAdjust adjust)
        {
            yield return adjust.AdjdBranchID;
            yield return adjust.AdjgBranchID;
        }

        protected virtual void _(Events.RowUpdated<Payment> e)
        {
            if (!e.Cache.ObjectsEqual<Payment.adjDate, Payment.adjTranPeriodID, Payment.curyID, Payment.branchID>(e.Row, e.OldRow))
            {
                foreach (TAdjust adjust in Adjustments.Select())
                {
                    if (!e.Cache.ObjectsEqual<Payment.branchID>(e.Row, e.OldRow))
                    {
                        Adjustments.Cache.SetDefaultExt<Adjust.adjgBranchID>(adjust);
                    }

                    if (!e.Cache.ObjectsEqual<Payment.adjTranPeriodID>(e.Row, e.OldRow))
                    {
                        FinPeriodIDAttribute.DefaultPeriods<Adjust.adjgFinPeriodID>(Adjustments.Cache, adjust);
                    }

                    (Adjustments.Cache as PXModelExtension<Adjust>)?.UpdateExtensionMapping(adjust);

                    Adjustments.Cache.MarkUpdated(adjust);
                }
            }
        }

        #endregion

        #region Balance calculations

        public void CalcBalances<T>(TAdjust adj, T voucher, bool isCalcRGOL, bool DiscOnDiscDate, TTran tran)
            where T : class, CM.IInvoice, IBqlTable, new()
        {
            BalanceClaculator.CalcBalances(adj, voucher, isCalcRGOL, DiscOnDiscDate, tran);
        }

        public abstract void CalcBalancesFromAdjustedDocument(TAdjust adj, bool isCalcRGOL, bool DiscOnDiscDate);

        protected virtual void _(Events.FieldUpdating<TAdjust, Adjust.curyDocBal> e)
        {
            e.Cancel = true;
            if (InternalCall || e.Row == null) return;

            if (e.Row.AdjdCuryInfoID != null && e.Row.CuryDocBal == null && e.Cache.GetStatus(e.Row) != PXEntryStatus.Deleted)
            {
                CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
            }
            e.NewValue = e.Row.CuryDocBal;
        }

        protected virtual void _(Events.FieldUpdating<TAdjust, Adjust.curyDiscBal> e)
        {
            e.Cancel = true;
            if (InternalCall || e.Row == null) return;


            if (e.Row.AdjdCuryInfoID != null && e.Row.CuryDiscBal == null && e.Cache.GetStatus(e.Row) != PXEntryStatus.Deleted)
            {
                CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
            }
            e.NewValue = e.Row.CuryDiscBal;
        }

        protected virtual void _(Events.FieldUpdated<TAdjust, Adjust.adjdCuryRate> e)
        {
            TAdjust adj = e.Row;

            if (adj.VoidAppl == true || adj.Voided == true) return;

            CurrencyInfo pay_info = curyHelper.GetCurrencyInfo(adj.AdjgCuryInfoID);
            CurrencyInfo vouch_info = curyHelper.GetCurrencyInfo(adj.AdjdCuryInfoID);

            decimal payment_docbal = (decimal)adj.CuryAdjgAmt;
            decimal discount_docbal = (decimal)adj.CuryAdjgDiscAmt;

            if (string.Equals(pay_info.CuryID, vouch_info.CuryID) && adj.AdjdCuryRate != 1m)
            {
                adj.AdjdCuryRate = 1m;
            }
            else if (string.Equals(vouch_info.CuryID, vouch_info.BaseCuryID))
            {
                adj.AdjdCuryRate = pay_info.CuryMultDiv == "M" ? 1 / pay_info.CuryRate : pay_info.CuryRate;
            }
            else
            {
                vouch_info.CuryRate = adj.AdjdCuryRate;
                vouch_info.RecipRate = Math.Round(1m / (decimal)adj.AdjdCuryRate, 8, MidpointRounding.AwayFromZero);
                vouch_info.CuryMultDiv = "M";
                payment_docbal = vouch_info.CuryConvBase(adj.CuryAdjdAmt.Value);
                discount_docbal = vouch_info.CuryConvBase(adj.CuryAdjdDiscAmt.Value);
                decimal invoice_amount = vouch_info.CuryConvBase((adj.CuryAdjdAmt + adj.CuryAdjdDiscAmt).Value);

                vouch_info.CuryRate = Math.Round((decimal)adj.AdjdCuryRate * (pay_info.CuryMultDiv == "M" ? (decimal)pay_info.CuryRate : 1m / (decimal)pay_info.CuryRate), 8, MidpointRounding.AwayFromZero);
                vouch_info.RecipRate = Math.Round((pay_info.CuryMultDiv == "M" ? 1m / (decimal)pay_info.CuryRate : (decimal)pay_info.CuryRate) / (decimal)adj.AdjdCuryRate, 8, MidpointRounding.AwayFromZero);

                if (payment_docbal + discount_docbal != invoice_amount)
                    discount_docbal += invoice_amount - discount_docbal - payment_docbal;
            }

            Base.Caches[typeof(CurrencyInfo)].MarkUpdated(vouch_info);

            if (payment_docbal != adj.CuryAdjgAmt)
                e.Cache.SetValue<Adjust.curyAdjgAmt>(e.Row, payment_docbal);

            if (discount_docbal != adj.CuryAdjgDiscAmt)
                e.Cache.SetValue<Adjust.curyAdjgDiscAmt>(e.Row, discount_docbal);

            FillPPDAmts(adj);//Was not present in AP. Does it make sence?
            CalcBalancesFromAdjustedDocument(adj, true, true);
        }

        protected void FillPPDAmts(TAdjust adj)
        {
            adj.CuryAdjgPPDAmt = adj.CuryAdjgDiscAmt;
            adj.CuryAdjdPPDAmt = adj.CuryAdjdDiscAmt;
            adj.AdjPPDAmt = adj.AdjDiscAmt;
        }

        protected virtual void _(Events.FieldUpdated<TAdjust, Adjust.voided> e)
        {
            CalcBalancesFromAdjustedDocument(e.Row, true, false);
        }

        protected virtual void _(Events.FieldUpdated<TAdjust, Adjust.curyAdjgDiscAmt> e)
        {
            if (e.Row == null) return;
            FillPPDAmts(e.Row);
            CalcBalancesFromAdjustedDocument(e.Row, true, DiscOnDiscDate);
        }

        #endregion
    }
}
