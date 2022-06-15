using PX.Common;
using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.GraphExtensions.Abstract;
using PX.Objects.Common.GraphExtensions.Abstract.DAC;
using PX.Objects.Common.GraphExtensions.Abstract.Mapping;
using System;
using System.Linq;

namespace PX.Objects.AR
{
	partial class ARPaymentEntry
	{
		public class ARPaymentEntryDocumentExtension : PaymentGraphExtension<ARPaymentEntry, ARPayment, ARAdjust, ARInvoice, ARTran>
		{
			#region Overrides

			protected override AbstractPaymentBalanceCalculator<ARAdjust, ARTran> GetAbstractBalanceCalculator()
				=> new ARPaymentBalanceCalculator(Base);

			protected override bool InternalCall => Base.internalCall;

			public override PXSelectBase<ARAdjust> Adjustments => Base.Adjustments_Raw;

			public override void Initialize()
			{
				base.Initialize();

				Documents = new PXSelectExtension<Payment>(Base.Document);
			}

			protected override PaymentMapping GetPaymentMapping()
			{
				return new PaymentMapping(typeof(ARPayment));
			}

			public override void CalcBalancesFromAdjustedDocument(ARAdjust adj, bool isCalcRGOL, bool DiscOnDiscDate)
			{
				if (Base.balanceCache == null || !Base.balanceCache.TryGetValue(adj, out var source))
					source = Base.ARInvoice_DocType_RefNbr.Select(adj.AdjdLineNbr, adj.AdjdDocType, adj.AdjdRefNbr);

				foreach (PXResult<ARInvoice> res in source)
				{
					ARInvoice voucher = res;
					ARTran tran = PXResult.Unwrap<ARTran>(res);

					CalcBalances(adj, voucher, isCalcRGOL, DiscOnDiscDate, tran);
					return;
				}

				foreach (ARPayment payment in Base.ARPayment_DocType_RefNbr.Select(adj.AdjdDocType, adj.AdjdRefNbr))
				{
					CalcBalances(adj, payment, isCalcRGOL, DiscOnDiscDate, null);
				}
			}

			#endregion

			#region Handlers

			protected virtual void _(Events.FieldUpdated<ARAdjust, ARAdjust.curyAdjgPPDAmt> e)
			{
				if (e.Row == null) return;
				e.Row.FillDiscAmts();
				CalcBalancesFromAdjustedDocument(e.Row, true, false);
			}


			protected virtual void _(Events.FieldVerifying<ARAdjust, ARAdjust.curyAdjgAmt> e)
			{
				ARAdjust adj = e.Row;

				foreach (string key in e.Cache.Keys.Where(key => e.Cache.GetValue(adj, key) == null))
				{
					throw new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, key));
				}

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWOBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, false);
				}

				if (adj.CuryDocBal == null)
				{
					e.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<ARAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				Sign balanceSign = adj.CuryOrigDocAmt < 0m ? Sign.Minus : Sign.Plus;

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue * balanceSign < 0m)
				{
					throw new PXSetPropertyException(balanceSign == Sign.Plus
						? CS.Messages.Entry_GE
						: CS.Messages.Entry_LE, 0.ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue * balanceSign > 0m)
				{
					throw new PXSetPropertyException(balanceSign == Sign.Plus
						? CS.Messages.Entry_LE
						: CS.Messages.Entry_GE, 0.ToString());
				}

				if (((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgAmt - (decimal)e.NewValue) * balanceSign < 0m)
				{
					throw new PXSetPropertyException(balanceSign == Sign.Plus
						? CS.Messages.Entry_LE
						: CS.Messages.Entry_GE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgAmt).ToString());
				}
			}

			protected virtual void _(Events.FieldUpdated<ARAdjust, ARAdjust.curyAdjgAmt> e)
			{
				CalcBalancesFromAdjustedDocument(e.Row, true, Base.InternalCall);
				e.Row.Selected = e.Row.CuryAdjgAmt != 0m || e.Row.CuryAdjgPPDAmt != 0m;
			}

			protected virtual void _(Events.FieldVerifying<ARAdjust, ARAdjust.curyAdjgPPDAmt> e)
			{
				ARAdjust adj = e.Row;

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWOBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, false);
				}

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null)
				{
					e.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<ARAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				Sign balanceSign = adj.CuryOrigDocAmt < 0m ? Sign.Minus : Sign.Plus;


				if ((adj.VoidAdjNbr == null && balanceSign * Math.Sign((decimal)e.NewValue) < 0)
					|| (adj.VoidAdjNbr != null && balanceSign * Math.Sign((decimal)e.NewValue) > 0))
				{
					throw new PXSetPropertyException(
						(decimal)e.NewValue < 0m ? CS.Messages.Entry_GE : CS.Messages.Entry_LE,
						0);
				}

				decimal remainingCashDiscountBalance = (adj.CuryDiscBal ?? 0m) + (adj.CuryAdjgPPDAmt ?? 0m);

				if (adj.VoidAdjNbr == null && (adj.CuryDiscBal == 0 || Math.Sign((decimal)adj.CuryDiscBal) * Math.Sign((decimal)e.NewValue) > 0) &&
						Math.Abs(remainingCashDiscountBalance) < Math.Abs((decimal)e.NewValue))
				{
					throw new PXSetPropertyException(
						Messages.AmountEnteredExceedsRemainingCashDiscountBalance,
						remainingCashDiscountBalance.ToString());
				}

				if (adj.CuryAdjgAmt != null && (e.Cache.GetValuePending<ARAdjust.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (decimal?)e.Cache.GetValuePending<ARAdjust.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if (Math.Abs((decimal)adj.CuryDocBal) + Math.Abs((decimal)adj.CuryAdjgPPDAmt) < Math.Abs((decimal)e.NewValue))
					{
						throw new PXSetPropertyException(
							CS.Messages.Entry_LE,
							((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgPPDAmt).ToString()
							);
					}
				}

				if (adj.AdjdHasPPDTaxes == true && adj.AdjgDocType == ARDocType.CreditMemo)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_EQ, 0.ToString());
				}
			}

			protected virtual void _(Events.FieldVerifying<ARAdjust, ARAdjust.curyAdjgWOAmt> e)
			{
				ARAdjust adj = e.Row;

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWOBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, false);
				}

				if (adj.CuryDocBal == null || adj.CuryWOBal == null)
				{
					e.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.Error, PXUIFieldAttribute.GetDisplayName<ARAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				// We should use absolute values here, because wo amount 
				// may have positive or negative sign.
				// 
				if ((decimal)adj.CuryWOBal + Math.Abs((decimal)adj.CuryAdjgWOAmt) - Math.Abs((decimal)e.NewValue) < 0)
				{
					throw new PXSetPropertyException(Messages.ApplicationWOLimitExceeded, ((decimal)adj.CuryWOBal + Math.Abs((decimal)adj.CuryAdjgWOAmt)).ToString());
				}

				if (adj.CuryAdjgAmt != null &&
					(e.Cache.GetValuePending<ARAdjust.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (decimal?)e.Cache.GetValuePending<ARAdjust.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWOAmt - (decimal)e.NewValue < 0)
					{
						throw new PXSetPropertyException(CS.Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWOAmt).ToString());
					}
				}
			}

			protected virtual void _(Events.FieldUpdated<ARAdjust, ARAdjust.curyAdjgWOAmt> e)
			{
				CalcBalancesFromAdjustedDocument(e.Row, true, false);
			}

			#endregion

		}
	}
}
