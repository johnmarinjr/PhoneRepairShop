using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.GraphExtensions.Abstract;
using PX.Objects.Common.GraphExtensions.Abstract.DAC;
using PX.Objects.Common.GraphExtensions.Abstract.Mapping;
using System.Linq;

namespace PX.Objects.AP
{
	public partial class APPaymentEntry
	{
		public class APPaymentEntryDocumentExtension : PaymentGraphExtension<APPaymentEntry, APPayment, APAdjust, APInvoice, APTran>
		{
			#region Override 

			protected override bool DiscOnDiscDate => !Base.TakeDiscAlways;

			protected override bool InternalCall => Base.UnattendedMode;

			public override PXSelectBase<APAdjust> Adjustments => Base.Adjustments_Raw;

			protected override AbstractPaymentBalanceCalculator<APAdjust, APTran> GetAbstractBalanceCalculator()
				=> new APPaymentBalanceCalculator(Base.GetExtension<MultiCurrency>());

			public override void Initialize()
			{
				base.Initialize();

				Documents = new PXSelectExtension<Payment>(Base.Document);
			}

			protected override PaymentMapping GetPaymentMapping()
			{
				return new PaymentMapping(typeof(APPayment));
			}

			public override void CalcBalancesFromAdjustedDocument(APAdjust adj, bool isCalcRGOL, bool DiscOnDiscDate)
			{
				if (Base.balanceCache == null || !Base.balanceCache.TryGetValue(adj, out var source))
					source = Base.APInvoice_VendorID_DocType_RefNbr.Select(adj.AdjdLineNbr, adj.VendorID, adj.AdjdDocType, adj.AdjdRefNbr);

				foreach (PXResult<APInvoice, APTran> res in source)
				{
					APInvoice voucher = res;
					APTran tran = res;
					CalcBalances(adj, voucher, isCalcRGOL, DiscOnDiscDate, tran);
					return;
				}

				foreach (APPayment payment in Base.APPayment_VendorID_DocType_RefNbr.Select(adj.VendorID, adj.AdjdDocType, adj.AdjdRefNbr))
				{
					CalcBalances(adj, payment, isCalcRGOL, DiscOnDiscDate, null);
				}
			}

			#endregion

			#region Handlers

			protected virtual void _(Events.FieldUpdated<APAdjust, APAdjust.curyAdjgPPDAmt> e)
			{
				if (e.Row == null) return;

				if (e.OldValue != null && e.Row.CuryDocBal == 0m && e.Row.CuryAdjgAmt < (decimal)e.OldValue)
				{
					e.Row.CuryAdjgDiscAmt = 0m;
				}
				e.Row.FillDiscAmts();
				CalcBalancesFromAdjustedDocument(e.Row, true, !Base.TakeDiscAlways);
			}


			protected virtual void _(Events.FieldUpdating<APAdjust, APAdjust.curyWhTaxBal> e)
			{
				e.Cancel = true;
				if (InternalCall || e.Row == null) return;


				if (e.Row.AdjdCuryInfoID != null && e.Row.CuryWhTaxBal == null && e.Cache.GetStatus(e.Row) != PXEntryStatus.Deleted)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
				}
				e.NewValue = e.Row.CuryWhTaxBal;
			}

			protected virtual void _(Events.FieldVerifying<APAdjust, APAdjust.curyAdjgAmt> e)
			{
				APAdjust adj = e.Row;

				foreach (string key in e.Cache.Keys.Where(key => e.Cache.GetValue(adj, key) == null))
				{
					throw new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, key));
				}

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
				}

				if (adj.CuryDocBal == null)
				{
					e.Cache.RaiseExceptionHandling<APAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<APAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, ((int)0).ToString());
				}

				if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgAmt - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgAmt).ToString());
				}
			}

			protected virtual void _(Events.FieldUpdated<APAdjust, APAdjust.curyAdjgAmt> e)
			{
				CalcBalancesFromAdjustedDocument(e.Row, true, false);
			}

			protected virtual void _(Events.FieldVerifying<APAdjust, APAdjust.curyAdjgPPDAmt> e)
			{
				APAdjust adj = e.Row;

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
				}

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null)
				{
					e.Cache.RaiseExceptionHandling<APAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<APAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, 0.ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, 0.ToString());
				}

				if ((decimal)adj.CuryDiscBal + (decimal)adj.CuryAdjgPPDAmt - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(
						(decimal)adj.CuryDiscBal + (decimal)adj.CuryAdjgDiscAmt == 0 ? CS.Messages.Entry_EQ : Messages.Entry_LE,
						((decimal)adj.CuryDiscBal + (decimal)adj.CuryAdjgPPDAmt).ToString()
						);
				}

				if (adj.CuryAdjgAmt != null && (e.Cache.GetValuePending<APAdjust.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (decimal?)e.Cache.GetValuePending<APAdjust.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgPPDAmt - (decimal)e.NewValue < 0)
					{
						throw new PXSetPropertyException(
							(decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgDiscAmt == 0 ? CS.Messages.Entry_EQ : Messages.Entry_LE,
							((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgPPDAmt).ToString()
							);
					}
				}

				if (adj.AdjdHasPPDTaxes == true &&
					adj.AdjgDocType == APDocType.DebitAdj)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_EQ, 0.ToString());
				}
			}

			protected virtual void _(Events.FieldVerifying<APAdjust, APAdjust.curyAdjgWhTaxAmt> e)
			{
				APAdjust adj = e.Row;

				if (adj.CuryDocBal == null || adj.CuryDiscBal == null || adj.CuryWhTaxBal == null)
				{
					CalcBalancesFromAdjustedDocument(e.Row, false, DiscOnDiscDate);
				}

				if (adj.CuryDocBal == null || adj.CuryWhTaxBal == null)
				{
					e.Cache.RaiseExceptionHandling<APAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<APAdjust.adjdRefNbr>(e.Cache)));
					return;
				}

				if (adj.VoidAdjNbr == null && (decimal)e.NewValue < 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, ((int)0).ToString());
				}

				if (adj.VoidAdjNbr != null && (decimal)e.NewValue > 0m)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_LE, ((int)0).ToString());
				}
				if ((decimal)adj.CuryWhTaxBal + (decimal)adj.CuryAdjgWhTaxAmt - (decimal)e.NewValue < 0)
				{
					throw new PXSetPropertyException(Messages.Entry_LE, ((decimal)adj.CuryWhTaxBal + (decimal)adj.CuryAdjgWhTaxAmt).ToString());
				}

				if (adj.CuryAdjgAmt != null && (e.Cache.GetValuePending<APAdjust.curyAdjgAmt>(e.Row) == PXCache.NotSetValue || (decimal?)e.Cache.GetValuePending<APAdjust.curyAdjgAmt>(e.Row) == adj.CuryAdjgAmt))
				{
					if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWhTaxAmt - (decimal)e.NewValue < 0)
					{
						throw new PXSetPropertyException(Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjgWhTaxAmt).ToString());
					}
				}
			}

			protected virtual void _(Events.FieldUpdated<APAdjust, APAdjust.curyAdjgWhTaxAmt> e)
			{
				CalcBalancesFromAdjustedDocument(e.Row, true, DiscOnDiscDate);
			}
			#endregion

		}
	}
}
