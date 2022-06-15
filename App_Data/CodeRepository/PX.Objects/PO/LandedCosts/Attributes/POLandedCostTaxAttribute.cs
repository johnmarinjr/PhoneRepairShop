using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.TX;

namespace PX.Objects.PO.LandedCosts.Attributes
{
	public class POLandedCostTaxAttribute : TaxAttribute
	{
		protected virtual short SortOrder
		{
			get
			{
				return 0;
			}
		}
		public POLandedCostTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this.CuryDocBal = typeof(POLandedCostDoc.curyDocTotal);
			this.CuryLineTotal = typeof(POLandedCostDoc.curyLineTotal);
			this.DocDate = typeof(POLandedCostDoc.docDate);
			this.CuryTaxTotal = typeof(POLandedCostDoc.curyTaxTotal);
			//this.CuryDiscTot = typeof(POLandedCostDoc.curyDiscTot);
			this.CuryOrigDiscAmt = typeof(POLandedCostDoc.curyDiscAmt);
			this.CuryTranAmt = typeof(POLandedCostDetail.curyLineAmt);
			//this.GroupDiscountRate = typeof(POLandedCostDetail.groupDiscountRate);

			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(POLandedCostDetail.curyLineAmt), typeof(SumCalc<POLandedCostDoc.curyLineTotal>)));
		}

		public override int CompareTo(object other)
		{
			return this.SortOrder.CompareTo(((POLandedCostTaxAttribute)other).SortOrder);
		}

		protected override decimal? GetCuryTranAmt(PXCache sender, object row, string TaxCalcType)
		{
			var curyTranAmt = base.GetCuryTranAmt(sender, row);
			//var groupDiscountRate = (decimal?)sender.GetValue(row, _GroupDiscountRate);
			//var documentDiscountRate = (decimal?)sender.GetValue(row, _DocumentDiscountRate);
			//decimal? CuryTranAmt = curyTranAmt * groupDiscountRate * documentDiscountRate;
			if (curyTranAmt != 0m)
			{
				CurrencyInfo currencyInfo = sender.Graph.FindImplementation<IPXCurrencyHelper>().GetDefaultCurrencyInfo();
				return currencyInfo.RoundCury((decimal)curyTranAmt);
			}
			else
			{
				return curyTranAmt;
			}
		}

		protected override decimal CalcLineTotal(PXCache sender, object row)
		{
			return (decimal?)ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m;
		}

		protected override List<object> SelectTaxes<TWhere>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			var result = SelectTaxes<TWhere, Current<POLandedCostDetail.lineNbr>>(graph, new object[] { row, ((POLandedCostDocEntry)graph).Document.Current }, taxchk, parameters);
			return result;
		}

		protected List<object> SelectTaxes<TWhere, TLineNbr>(PXGraph graph, object[] currents, PXTaxCheck taxchk, params object[] parameters)
			where TWhere : IBqlWhere, new()
			where TLineNbr : IBqlOperand
		{
			List<ITaxDetail> taxDetails;
			IDictionary<string, PXResult<Tax, TaxRev>> tails;

			List<object> ret = new List<object>();

			BqlCommand selectTaxes = new Select2<Tax,
				LeftJoin<TaxRev, On<TaxRev.taxID, Equal<Tax.taxID>,
					And<TaxRev.outdated, Equal<boolFalse>,
					And2<Where<TaxRev.taxType, Equal<TaxType.purchase>, And<Tax.reverseTax, Equal<False>,
						Or<TaxRev.taxType, Equal<TaxType.sales>, And<Where<Tax.reverseTax, Equal<True>,
						Or<Tax.taxType, Equal<CSTaxType.use>, Or<Tax.taxType, Equal<CSTaxType.withholding>>>>>>>>,
					And<Current<POLandedCostDoc.docDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>>();

			switch (taxchk)
			{
				case PXTaxCheck.Line:
					taxDetails = PXSelect<POLandedCostTax,
						Where<POLandedCostTax.docType, Equal<Current<POLandedCostDoc.docType>>,
							And<POLandedCostTax.refNbr, Equal<Current<POLandedCostDoc.refNbr>>,
							And<POLandedCostTax.lineNbr, Equal<TLineNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<POLandedCostTax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<TWhere>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (POLandedCostTax record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				case PXTaxCheck.RecalcLine:
					taxDetails = PXSelect<POLandedCostTax,
						Where<POLandedCostTax.docType, Equal<Current<POLandedCostDoc.docType>>,
							And<POLandedCostTax.refNbr, Equal<Current<POLandedCostDoc.refNbr>>,
							And<POLandedCostTax.lineNbr, Less<intMax>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<POLandedCostTax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<TWhere>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (POLandedCostTax record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				case PXTaxCheck.RecalcTotals:
					taxDetails = PXSelect<POLandedCostTaxTran,
						Where<POLandedCostTaxTran.docType, Equal<Current<POLandedCostDoc.docType>>, And<POLandedCostTaxTran.refNbr, Equal<Current<POLandedCostDoc.refNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<POLandedCostTaxTran>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<TWhere>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (POLandedCostTaxTran record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				default:
					return ret;
			}
		}

		private Dictionary<object, Dictionary<string, object>> totals;

		/// <summary>
		/// The purpose of this handler is to workaround situations where exception was thrown in RowUpdated 
		/// event chain and formulas that are embedded inside SOTaxAttribute will fail to calculate totals.
		/// Resetting totals to null will force formulas to calculate on all the details rather then calculate
		/// on the difference from previous incorrect result.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (sender.GetItemType() != typeof(POLandedCostDetail))
			{
				base.RowUpdated(sender, e);
				return;
			}

			var FieldsTotal = _Attributes.OfType<PXUnboundFormulaAttribute>()
				.Where(a => a.ParentField.DeclaringType == typeof(POLandedCostDoc))
				.Select(a => a.ParentField);

			Dictionary<string, object> header;

			//Entry point from Attribute style event handler
			if (_TaxCalc == TaxCalc.ManualLineCalc)
			{
				if (!totals.TryGetValue(e.Row, out header))
				{
					totals[e.Row] = header = new Dictionary<string, object>();

					FieldsTotal.ForEach(a =>
					{
						PXCache cache = ParentCache(sender.Graph);
						object value = cache.GetValue(cache.Current, a.Name);

						if (value != null)
						{
							header[a.Name] = value;
							cache.SetValue(cache.Current, a.Name, null);
						}
					});
				}
			}

			//Entry point from Calculate method invoked from graph.POLandedCostDetail_RowUpdated handler
			if (_TaxCalc == TaxCalc.Calc)
			{
				if (totals.TryGetValue(e.Row, out header))
				{
					FieldsTotal.Where(a => header.ContainsKey(a.Name)).ForEach(a =>
					{
						PXCache cache = ParentCache(sender.Graph);
						cache.SetValue(cache.Current, a.Name, header[a.Name]);
					});
					totals.Remove(e.Row);
				}
			}

			base.RowUpdated(sender, e);
		}

		public override void CacheAttached(PXCache sender)
		{
			inserted = new Dictionary<object, object>();
			updated = new Dictionary<object, object>();
			totals = new Dictionary<object, Dictionary<string, object>>();

			if (this.EnableTaxCalcOn(sender.Graph))
			{
				base.CacheAttached(sender);
				sender.Graph.FieldUpdated.AddHandler(typeof(POLandedCostDoc), _CuryTaxTotal, POLandedCostDoc_CuryTaxTot_FieldUpdated);
			}
			else
			{
				this.TaxCalc = TaxCalc.NoCalc;
			}
		}

		protected virtual void POLandedCostDoc_CuryDiscTot_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			bool calc = true;
			if (IsExternalTax(sender.Graph, (string)sender.GetValue(e.Row, _TaxZoneID)))
				calc = false;

			this._ParentRow = e.Row;
			if (!(_TaxCalc == TaxCalc.ManualLineCalc && totals.Any()))
				CalcTotals(sender, e.Row, calc);
			this._ParentRow = null;
		}

		virtual protected bool EnableTaxCalcOn(PXGraph aGraph)
		{
			return (aGraph is POLandedCostDocEntry);
		}

		protected override void CalcDocTotals(
			PXCache sender,
			object row,
			decimal CuryTaxTotal,
			decimal CuryInclTaxTotal,
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			base.CalcDocTotals(sender, row, CuryTaxTotal, CuryInclTaxTotal, CuryWhTaxTotal, CuryTaxDiscountTotal);

			if (ParentGetStatus(sender.Graph) != PXEntryStatus.Deleted)
			{
				decimal doc_CuryWhTaxTotal = (decimal)(ParentGetValue(sender.Graph, _CuryWhTaxTotal) ?? 0m);

				if (object.Equals(CuryWhTaxTotal, doc_CuryWhTaxTotal) == false)
				{
					ParentSetValue(sender.Graph, _CuryWhTaxTotal, CuryWhTaxTotal);
				}
			}
		}

		protected override void _CalcDocTotals(
			PXCache sender,
			object row,
			decimal CuryTaxTotal,
			decimal CuryInclTaxTotal,
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			decimal CuryDiscountTotal = (decimal)(ParentGetValue(sender.Graph, _CuryDiscTot) ?? 0m);

			decimal CuryLineTotal = CalcLineTotal(sender, row);

			decimal CuryDocTotal = CuryLineTotal + CuryTaxTotal - CuryInclTaxTotal - CuryDiscountTotal;

			decimal doc_CuryLineTotal = (decimal)(ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m);
			decimal doc_CuryTaxTotal = (decimal)(ParentGetValue(sender.Graph, _CuryTaxTotal) ?? 0m);

			if (object.Equals(CuryLineTotal, doc_CuryLineTotal) == false ||
				object.Equals(CuryTaxTotal, doc_CuryTaxTotal) == false)
			{
				ParentSetValue(sender.Graph, _CuryLineTotal, CuryLineTotal);
				ParentSetValue(sender.Graph, _CuryTaxTotal, CuryTaxTotal);
				if (!string.IsNullOrEmpty(_CuryDocBal))
				{
					ParentSetValue(sender.Graph, _CuryDocBal, CuryDocTotal);
					return;
				}
			}

			if (!string.IsNullOrEmpty(_CuryDocBal))
			{
				decimal doc_CuryDocBal = (decimal)(ParentGetValue(sender.Graph, _CuryDocBal) ?? 0m);

				if (object.Equals(CuryDocTotal, doc_CuryDocBal) == false)
				{
					ParentSetValue(sender.Graph, _CuryDocBal, CuryDocTotal);
				}
			}
		}

		protected virtual void POLandedCostDoc_CuryTaxTot_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			decimal? curyTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryTaxTotal);
			decimal? curyWhTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryWhTaxTotal);

			CalcDocTotals(sender, e.Row, curyTaxTotal.GetValueOrDefault(), 0, curyWhTaxTotal.GetValueOrDefault(), 0);
		}
	}
}
