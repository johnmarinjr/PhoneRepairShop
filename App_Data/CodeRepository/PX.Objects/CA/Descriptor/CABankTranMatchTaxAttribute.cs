using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.TX;
using PX.Common;
using PX.Objects.CS;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CM;

namespace PX.Objects.CA
{
	public class CABankTranMatchTaxAttribute : ManualVATAttribute
	{
		public CABankTranMatchTaxAttribute(Type parentType, Type taxType, Type taxSumType, Type calcMode = null, Type parentBranchIDField = null) : base(parentType, taxType, taxSumType, calcMode, parentBranchIDField)
		{
			Init();
		}

		private void Init()
		{
			this.CuryDocBal = typeof(CABankTran.curyChargeAmt);
			this.CuryLineTotal = typeof(CABankTran.curyChargeAmt);
			this.DocDate = typeof(CABankTran.tranDate);
			this.CuryTaxTotal = typeof(CABankTran.curyChargeTaxAmt);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			IComparer<Tax> taxByCalculationLevelComparer = GetTaxByCalculationLevelComparer();
			taxByCalculationLevelComparer.ThrowOnNull(nameof(taxByCalculationLevelComparer));

			Dictionary<string, PXResult<Tax, TaxRev>> tail = new Dictionary<string, PXResult<Tax, TaxRev>>();
			object[] currents = new object[] { row, graph.Caches[typeof(CABankTran)].Current };

			foreach (PXResult<Tax, TaxRev> record in PXSelectReadonly2<Tax,
				LeftJoin<TaxRev, On<TaxRev.taxID, Equal<Tax.taxID>,
					And<TaxRev.outdated, Equal<boolFalse>,
					And2<
						Where<Current<CABankTran.drCr>, Equal<CADrCr.cACredit>, And<TaxRev.taxType, Equal<TaxType.purchase>, And<Tax.reverseTax, Equal<boolFalse>,
						Or<Current<CABankTran.drCr>, Equal<CADrCr.cACredit>, And<TaxRev.taxType, Equal<TaxType.sales>, And2<Where<Tax.reverseTax, Equal<boolTrue>,
							Or<Tax.taxType, Equal<CSTaxType.use>>>,
						Or<Current<CABankTran.drCr>, Equal<CADrCr.cADebit>, And<TaxRev.taxType, Equal<TaxType.sales>, And<Tax.reverseTax, Equal<boolFalse>,
							And<Tax.taxType, NotEqual<CSTaxType.withholding>, And<Tax.taxType, NotEqual<CSTaxType.use>>>>>>>>>>>>,
					And<Current<CABankTran.tranDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>,
				Where>
				.SelectMultiBound(graph, currents, parameters))
			{
				Tax adjdTax = AdjustTaxLevel(graph, (Tax)record);
				tail[((Tax)record).TaxID] = new PXResult<Tax, TaxRev>(adjdTax, (TaxRev)record);
			}

			List<object> ret = new List<object>();

			switch (taxchk)
			{
				case PXTaxCheck.Line:
					foreach (CABankChargeTax record in PXSelect<CABankChargeTax,
													Where<CABankChargeTax.matchType, Equal<Current<CABankTranMatch.matchType>>,
													  And<CABankChargeTax.bankTranID, Equal<Current<CABankTranMatch.tranID>>,
													  And<CABankChargeTax.lineNbr, Equal<Current<CABankTranMatch.lineNbr>>>>>>
													.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankChargeTax>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankChargeTax, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				case PXTaxCheck.RecalcLine:
					foreach (CABankChargeTax record in PXSelect<CABankChargeTax,
						Where<CABankChargeTax.bankTranID, Equal<Current<CABankTran.tranID>>>>
						.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankChargeTax>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankChargeTax, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				case PXTaxCheck.RecalcTotals:
					foreach (CABankTaxTranMatch record in PXSelect<CABankTaxTranMatch,
						Where<CABankTaxTranMatch.bankTranType, Equal<Current<CABankTran.tranType>>,
							And<CABankTaxTranMatch.bankTranID, Equal<Current<CABankTran.tranID>>>>>
						.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (record.TaxID != null && tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankTaxTranMatch>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankTaxTranMatch, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				default:
					return ret;
			}
		}

		protected override List<object> SelectDocumentLines(PXGraph graph, object row)
		{
			List<object> ret = PXSelect<CABankTranMatch,
								Where<CABankTranMatch.tranID, Equal<Current<CABankTran.tranID>>>>
									.SelectMultiBound(graph, new object[] { row })
									.RowCast<CABankTranMatch>()
									.Select(_ => (object)_)
									.ToList();
			return ret;
		}

		protected int CalculateIndex<T>(List<object> ret, PXResult<Tax, TaxRev> line, IComparer<Tax> taxByCalculationLevelComparer)
			where T : class, IBqlTable, new()
		{
			int idx;
			for (idx = ret.Count;
				(idx > 0) && taxByCalculationLevelComparer.Compare((PXResult<T, Tax, TaxRev>)ret[idx - 1], line) > 0;
				idx--) ;
			return idx;
		}

		protected override decimal CalcLineTotal(PXCache sender, object row)
		{
			if (sender.Graph is CABankTransactionsMaint)
			{
				decimal curyLineTotal = 0m;
				foreach (CABankTranMatch detrow in ((CABankTransactionsMaint)sender.Graph).TranSplit.View.SelectMultiBound(new object[1] { row }))
				{
					curyLineTotal += detrow.CuryApplAmt.GetValueOrDefault();
				}
				return curyLineTotal;
			}
			else
			{
				return base.CalcLineTotal(sender, row);
			}
		}

		protected override void SetTaxableAmt(PXCache sender, object row, decimal? value)
		{
			sender.SetValue<CABankTranMatch.curyApplTaxableAmt>(row, value);
		}

		protected override void SetTaxAmt(PXCache sender, object row, decimal? value)
		{
			sender.SetValue<CABankTranMatch.curyApplTaxAmt>(row, value);
		}

		protected override decimal? GetTaxableAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CABankTranMatch.curyApplTaxableAmt>(row);
		}

		protected override decimal? GetTaxAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CABankTranMatch.curyApplTaxAmt>(row);
		}

		protected override void SetExtCostExt(PXCache sender, object child, decimal? value)
		{
			CABankTranMatch row = child as CABankTranMatch;
			if (row != null)
			{
				row.CuryApplAmt = value;
				sender.Update(row);
			}
		}

		protected override string GetExtCostLabel(PXCache sender, object row)
		{
			return ((PXDecimalState)sender.GetValueExt<CABankTranMatch.curyApplAmt>(row)).DisplayName;
		}

		protected override void DefaultTaxes(PXCache sender, object row, bool DefaultExisting)
		{
			base.DefaultTaxes(sender, row, DefaultExisting);
			CalculateRawAmount(sender, row);
		}

		protected virtual void CalculateRawAmount(PXCache sender, object row)
		{
			var aRow = (CABankTranMatch)row;
			if (aRow.MatchType != CABankTranMatch.matchType.Charge)
			{
				return;
			}

			CalculateRawAmount<CABankTranMatch>(sender, aRow);
		}

		public virtual void CalculateRawAmount<T>(PXCache sender, object row)
			where T : IBqlTable
		{
			if (sender.Current == null) return;

			decimal? inclusiveSumRates = 0m;
			decimal? exclusiveSumRates = 0m;

			var inclusiveTaxRates = new List<RateWithMultiplier>();
			var exclusiveTaxRates = new List<RateWithMultiplier>();

			string calcMode = _isTaxCalcModeEnabled ? GetTaxCalcMode(sender.Graph) : TaxCalculationMode.TaxSetting;
			decimal? curyTranAmt = GetCuryTranAmount(sender);

			foreach (object taxrow in SelectTaxes(sender, row, PXTaxCheck.Line))
			{
				Tax tax = PXResult.Unwrap<Tax>(taxrow);
				TaxRev taxRev = PXResult.Unwrap<TaxRev>(taxrow);

				if ((!_isTaxCalcModeEnabled || calcMode == TaxCalculationMode.TaxSetting) && tax.TaxCalcLevel == CSTaxCalcLevel.Inclusive || calcMode == TaxCalculationMode.Gross)
				{
					inclusiveSumRates += taxRev.TaxRate;
					inclusiveTaxRates.Add(new RateWithMultiplier(taxRev.TaxRate, tax.ReverseTax == true ? Decimal.MinusOne : Decimal.One));
				}

				if ((!_isTaxCalcModeEnabled || calcMode == TaxCalculationMode.TaxSetting) && tax.TaxCalcLevel != CSTaxCalcLevel.Inclusive || calcMode == TaxCalculationMode.Net)
				{
					exclusiveSumRates += taxRev.TaxRate;
					exclusiveTaxRates.Add(new RateWithMultiplier(taxRev.TaxRate, tax.ReverseTax == true ? Decimal.MinusOne : Decimal.One));
				}
			}

			decimal? taxableAmountAfterGross = (curyTranAmt * (1m + inclusiveSumRates / 100)) / (1m + inclusiveSumRates / 100 + exclusiveSumRates / 100);
			decimal? inclusiveTaxTotalAmt = CalculateTaxTotalAmtWithRates(sender, row, inclusiveTaxRates, taxableAmountAfterGross);

			decimal? taxableAmt = taxableAmountAfterGross - inclusiveTaxTotalAmt;
			decimal? exclusiveTaxTotalAmt = CalculateTaxTotalAmtWithRates(sender, row, exclusiveTaxRates, taxableAmt);

			decimal? rawAmount = curyTranAmt - exclusiveTaxTotalAmt;

			PXCache childCache = sender.Graph.Caches[typeof(T)];
			T copy = (T)sender.Graph.Caches[typeof(T)].CreateCopy(row);
			copy = (T)SetCuryTranAmount(copy, rawAmount);
			childCache.Update(copy);
		}

		protected virtual decimal? CalculateTaxTotalAmtWithRates(PXCache sender, object row, List<RateWithMultiplier> taxRates, decimal? amount)
		{
			decimal? taxTotalAmt = 0m;
			foreach (var taxrow in taxRates)
			{
				decimal? curyCurrentTaxAmount = (amount * taxrow.TaxRate / 100m) ?? 0m;
				curyCurrentTaxAmount = ApplyRounding(sender, row, curyCurrentTaxAmount.Value) * taxrow.Multiplier;
				taxTotalAmt += curyCurrentTaxAmount;
			}

			return taxTotalAmt;
		}

		protected class RateWithMultiplier
		{
			public decimal? TaxRate { get; private set; }
			public decimal? Multiplier { get; private set; }

			public RateWithMultiplier(decimal? taxRate, decimal? multiplier)
			{
				TaxRate = taxRate;
				Multiplier = multiplier;
			}
		}

		protected virtual decimal? GetCuryTranAmount(PXCache sender)
		{
			return ((CABankTranMatch)sender.Current).CuryApplAmt;
		}

		protected virtual object SetCuryTranAmount(object row, decimal? amount)
		{
			var aRow = (CABankTranMatch)row;
			aRow.CuryApplAmt = amount;
			return aRow;
		}

		protected virtual decimal ApplyRounding(PXCache sender, object row, decimal taxAmount)
		{
			var prec = GetCuryPrecision(sender);
			return Math.Round(taxAmount, prec, MidpointRounding.AwayFromZero);
		}

		protected virtual int GetCuryPrecision(PXCache sender)
		{
			CurrencyList cury = SelectFrom<CurrencyList>.Where<CurrencyList.curyID.IsEqual<CABankTran.curyID.FromCurrent>>.View.Select(sender.Graph);
			return cury?.DecimalPlaces ?? Precision ?? 0;
		}

		protected override decimal? GetCuryTranAmt(PXCache sender, object row, string TaxCalcType = "I")
		{
			return (decimal?)sender.GetValue<CABankTranMatch.curyApplAmt>(row) ?? 0m;
		}

		protected override decimal? GetDocLineFinalAmtNoRounding(PXCache sender, object row, string TaxCalcType = "I")
		{
			return (decimal?)sender.GetValue<CABankTranMatch.curyApplAmt>(row) ?? 0m;
		}

		protected override void _CalcDocTotals(
			PXCache sender,
			object row,
			decimal CuryTaxTotal,
			decimal CuryInclTaxTotal,
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			decimal doc_CuryTaxTotal = (decimal)(ParentGetValue(sender.Graph, _CuryTaxTotal) ?? 0m);
			decimal curyChargeAmt = (decimal)(ParentGetValue<CABankTran.curyChargeAmt>(sender.Graph) ?? 0m);

			var curyChargeAmtSign = curyChargeAmt > 0m ? 1 : -1;
			decimal discrepancy = CalculateDiscrepancy(row, CuryTaxTotal, curyChargeAmt, CuryInclTaxTotal);

			if (!Equals(CuryTaxTotal, doc_CuryTaxTotal))
			{
				ParentSetValue<CABankTran.curyChargeTaxAmt>(sender.Graph, curyChargeAmtSign * Math.Abs(CuryTaxTotal) + discrepancy);
			}
		}

		private static decimal CalculateDiscrepancy(object row, decimal CuryTaxTotal, decimal curyChargeAmt, decimal curyInclTaxTotal)
		{
			var bankMatch = (CABankTranMatch)row;

			decimal discrepancy = Math.Abs(curyChargeAmt) - Math.Min(Math.Abs(bankMatch?.CuryApplTaxableAmt ?? 0m) + Math.Abs(curyInclTaxTotal), Math.Abs(bankMatch?.CuryApplAmt ?? 0m)) - (Math.Abs(CuryTaxTotal) - Math.Abs(curyInclTaxTotal));
			return discrepancy;
		}

		protected override bool isControlTaxTotalRequired(PXCache sender)
		{
			CASetup setup = new PXSetup<CASetup>(sender.Graph).Select();
			return setup != null && setup.RequireControlTaxTotal == true;
		}

		protected override bool isControlTotalRequired(PXCache sender)
		{
			CASetup setup = new PXSetup<CASetup>(sender.Graph).Select();
			return setup != null && setup.RequireControlTotal == true;
		}

	}
}
