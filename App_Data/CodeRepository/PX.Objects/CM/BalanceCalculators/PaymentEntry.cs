using PX.Data;
using System;

namespace PX.Objects.CM
{
	public static class PaymentEntry
	{
		public static void CuryConvCury(decimal? BaseAmt, out decimal? CuryAmt, decimal CuryRate, string CuryMultDiv, int CuryPrecision)
		{
			if (CuryMultDiv == "D" && BaseAmt != null)
			{
				CuryAmt = Math.Round((decimal)BaseAmt * CuryRate, CuryPrecision, MidpointRounding.AwayFromZero);
			}
			else if (CuryRate != 0m && BaseAmt != null)
			{
				CuryAmt = Math.Round((decimal)BaseAmt / CuryRate, CuryPrecision, MidpointRounding.AwayFromZero);
			}
			else
			{
				CuryAmt = BaseAmt;
			}
		}

		public static void CuryConvBase(decimal? CuryAmt, out decimal? BaseAmt, decimal CuryRate, string CuryMultDiv, int BasePrecision)
		{
			if (CuryMultDiv == "M" && CuryAmt != null)
			{
				BaseAmt = Math.Round((decimal)CuryAmt * CuryRate, BasePrecision, MidpointRounding.AwayFromZero);
			}
			else if (CuryRate != 0m && CuryAmt != null)
			{
				BaseAmt = Math.Round((decimal)CuryAmt / CuryRate, BasePrecision, MidpointRounding.AwayFromZero);
			}
			else
			{
				BaseAmt = CuryAmt;
			}
		}

		public static decimal? CalcBalances(decimal? CuryDocBal, decimal? DocBal, string PayCuryID, string DocCuryID, string BaseCuryID, decimal PayCuryRate, string PayCuryMultDiv, decimal DocCuryRate, string DocCuryMultDiv, int CuryPrecision, int BasePrecision)
		{
			decimal? payment_curydocbal;
			decimal? payment_docbal;

			if (object.Equals(PayCuryID, DocCuryID))
			{
				payment_curydocbal = CuryDocBal;
			}
			else if (object.Equals(BaseCuryID, DocCuryID))
			{
				CuryConvCury(DocBal, out payment_curydocbal, PayCuryRate, PayCuryMultDiv, CuryPrecision);
			}
			else
			{
				CuryConvBase(CuryDocBal, out payment_docbal, DocCuryRate, DocCuryMultDiv, BasePrecision);
				CuryConvCury(payment_docbal, out payment_curydocbal, PayCuryRate, PayCuryMultDiv, CuryPrecision);
			}

			return payment_curydocbal;
		}

		public static void CalcDiscount(DateTime? PayDate, IInvoice voucher, IAdjustment adj)
		{
			if (PayDate != null && voucher.DiscDate != null && ((DateTime)PayDate).CompareTo((DateTime)voucher.DiscDate) > 0)
			{
				adj.CuryDiscBal = 0m;
				adj.DiscBal = 0m;
			}
		}

		public static void WarnDiscount<TInvoice, TAdjust>(PXGraph graph, DateTime? PayDate, TInvoice invoice, TAdjust adj)
			where TInvoice : IInvoice
			where TAdjust : class, IBqlTable, IAdjustment
		{
			if (adj.Released != true && invoice.DiscDate != null && adj.AdjgDocDate != null &&
				((DateTime)adj.AdjgDocDate).CompareTo((DateTime)invoice.DiscDate) > 0 && adj.CuryAdjgDiscAmt > 0m)
			{
				graph.Caches[typeof(TAdjust)].RaiseExceptionHandling("CuryAdjgDiscAmt", adj, adj.CuryAdjgDiscAmt, new PXSetPropertyException(AR.Messages.DiscountOutOfDate, PXErrorLevel.Warning, invoice.DiscDate));
			}

		}

		public static void WarnPPDiscount<TInvoice, TAdjust>(PXGraph graph, DateTime? PayDate, TInvoice invoice, TAdjust adj, decimal? CuryAdjgPPDAmt)
			where TInvoice : IInvoice
			where TAdjust : class, IBqlTable, IAdjustment
		{
			if (adj.Released != true && invoice.DiscDate != null && adj.AdjgDocDate != null &&
				((DateTime)adj.AdjgDocDate).CompareTo((DateTime)invoice.DiscDate) > 0 && CuryAdjgPPDAmt > 0m)
			{
				graph.Caches[typeof(TAdjust)].RaiseExceptionHandling("CuryAdjgPPDAmt", adj, CuryAdjgPPDAmt, new PXSetPropertyException(AR.Messages.DiscountOutOfDate, PXErrorLevel.Warning, invoice.DiscDate));
			}
		}
	}
}