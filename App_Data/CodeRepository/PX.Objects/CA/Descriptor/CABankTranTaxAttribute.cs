using System;
using System.Linq;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;
using PX.Objects.Common;
using PX.Objects.CM;

namespace PX.Objects.CA
{
	public class CABankTranTaxAttribute : ManualVATAttribute
	{
		protected override bool CalcGrossOnDocumentLevel { get => true; set => base.CalcGrossOnDocumentLevel = value; }
		protected override bool AskRecalculationOnCalcModeChange { get => true; set => base.AskRecalculationOnCalcModeChange = value; }

		public CABankTranTaxAttribute(Type parentType, Type taxType, Type taxSumType, Type calcMode = null, Type parentBranchIDField = null)
			: base(parentType, taxType, taxSumType, calcMode, parentBranchIDField)
		{
			Init();
		}
		
		private void Init()
		{
			this.CuryDocBal = typeof(CABankTran.curyTranAmt);
			this.CuryLineTotal = typeof(CABankTran.curyApplAmtCA);
			this.DocDate = typeof(CABankTran.tranDate);
			this.CuryTaxTotal = typeof(CABankTran.curyTaxTotal);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			IComparer<Tax> taxByCalculationLevelComparer = GetTaxByCalculationLevelComparer();
			taxByCalculationLevelComparer.ThrowOnNull(nameof(taxByCalculationLevelComparer));

			Dictionary<string, PXResult<Tax, TaxRev>> tail = new Dictionary<string, PXResult<Tax, TaxRev>>();
			object[] currents = new object[] { row, GetCurrent(graph) };

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
					foreach (CABankTax record in PXSelect<CABankTax,
													Where<CABankTax.bankTranType, Equal<Current<CABankTranDetail.bankTranType>>,
													  And<CABankTax.bankTranID, Equal<Current<CABankTranDetail.bankTranID>>,
													  And<CABankTax.lineNbr, Equal<Current<CABankTranDetail.lineNbr>>>>>>
													.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankTax>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankTax, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				case PXTaxCheck.RecalcLine:
					foreach (CABankTax record in PXSelect<CABankTax,
						Where<CABankTax.bankTranType, Equal<Current<CABankTran.tranType>>,
						And<CABankTax.bankTranID, Equal<Current<CABankTran.tranID>>>>>
						.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankTax>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankTax, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				case PXTaxCheck.RecalcTotals:
					foreach (CABankTaxTran record in PXSelect<CABankTaxTran,
						Where<CABankTaxTran.bankTranType, Equal<Current<CABankTran.tranType>>,
							And<CABankTaxTran.bankTranID, Equal<Current<CABankTran.tranID>>>>>
						.SelectMultiBound(graph, currents))
					{
						PXResult<Tax, TaxRev> line;
						if (record.TaxID != null && tail.TryGetValue(record.TaxID, out line))
						{
							int idx = CalculateIndex<CABankTaxTran>(ret, line, taxByCalculationLevelComparer);
							ret.Insert(idx, new PXResult<CABankTaxTran, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					return ret;

				default:
					return ret;
			}
		}

		protected virtual object GetCurrent(PXGraph graph)
		{
			return ((CABankTransactionsMaint)graph).Details.Current;
		}

		protected override List<object> SelectDocumentLines(PXGraph graph, object row)
		{
			List<object> ret = PXSelect<CABankTranDetail,
								Where<CABankTranDetail.bankTranID, Equal<Current<CABankTran.tranID>>>>
									.SelectMultiBound(graph, new object[] { row })
									.RowCast<CABankTranDetail>()
									.Select(_ => (object)_)
									.ToList();
			return ret;
		}

		private int CalculateIndex<T>(List<object> ret, PXResult<Tax, TaxRev> line, IComparer<Tax> taxByCalculationLevelComparer)
			where T : class, IBqlTable, new()
		{
			int idx;
			for (idx = ret.Count;
				(idx > 0) && taxByCalculationLevelComparer.Compare((PXResult<T, Tax, TaxRev>)ret[idx - 1], line) > 0;
				idx--) ;
			return idx;
		}

		public override void CacheAttached(PXCache sender)
		{
			if (sender.Graph is CABankTransactionsMaint || sender.Graph is CABankMatchingProcess)
			{
				base.CacheAttached(sender);
			}
			else
			{
				this.TaxCalc = TaxCalc.NoCalc;
			}
		}

		protected override decimal CalcLineTotal(PXCache sender, object row)
		{
			if (sender.Graph is CABankTransactionsMaint)
			{
				decimal curyLineTotal = 0m;
				foreach (CABankTranDetail detrow in ((CABankTransactionsMaint)sender.Graph).TranSplit.View.SelectMultiBound(new object[1] { row }))
				{
					curyLineTotal += detrow.CuryTranAmt.GetValueOrDefault();
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
			sender.SetValue<CABankTranDetail.curyTaxableAmt>(row, value);
		}

		protected override void SetTaxAmt(PXCache sender, object row, decimal? value)
		{
			sender.SetValue<CABankTranDetail.curyTaxAmt>(row, value);
		}

		protected override decimal? GetTaxableAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CABankTranDetail.curyTaxableAmt>(row);
		}

		protected override decimal? GetTaxAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CABankTranDetail.curyTaxAmt>(row);
		}

		protected override void SetExtCostExt(PXCache sender, object child, decimal? value)
		{
			CABankTranDetail row = child as CABankTranDetail;
			if (row != null)
			{
				row.CuryTranAmt = value;
				sender.Update(row);
			}
		}

		protected override string GetExtCostLabel(PXCache sender, object row)
		{
			return ((PXDecimalState) sender.GetValueExt<CABankTranDetail.curyTranAmt>(row)).DisplayName;
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

		protected override void DefaultTaxes(PXCache sender, object row, bool DefaultExisting)
		{
			base.DefaultTaxes(sender, row, DefaultExisting);
			CalculateRawAmount(sender, row);
		}

		protected virtual void CalculateRawAmount(PXCache sender, object row)
		{
			if (sender.Current == null) return;

			decimal? inclusiveSumRates = 0m;
			decimal? exclusiveSumRates = 0m;

			var inclusiveTaxRates = new List<RateWithMultiplier>();
			var exclusiveTaxRates = new List<RateWithMultiplier>();

			string calcMode = _isTaxCalcModeEnabled ? GetTaxCalcMode(sender.Graph) : TaxCalculationMode.TaxSetting;
			decimal? curyTranAmt = ((CABankTranDetail)sender.Current).CuryTranAmt;

			foreach (object taxrow in SelectTaxes(sender, row, PXTaxCheck.Line))
			{
				Tax tax = PXResult.Unwrap<Tax>(taxrow);
				TaxRev taxRev = PXResult.Unwrap<TaxRev>(taxrow);

				if ((!_isTaxCalcModeEnabled || calcMode == TaxCalculationMode.TaxSetting) && tax.TaxCalcLevel == CSTaxCalcLevel.Inclusive || calcMode == TaxCalculationMode.Gross)
				{
					inclusiveSumRates += taxRev.TaxRate ?? 0;
					inclusiveTaxRates.Add(new RateWithMultiplier(taxRev.TaxRate, tax.ReverseTax == true ? Decimal.MinusOne : Decimal.One));
				}

				if ((!_isTaxCalcModeEnabled || calcMode == TaxCalculationMode.TaxSetting) && tax.TaxCalcLevel != CSTaxCalcLevel.Inclusive || calcMode == TaxCalculationMode.Net)
				{
					exclusiveSumRates += taxRev.TaxRate ?? 0;
					exclusiveTaxRates.Add(new RateWithMultiplier(taxRev.TaxRate, tax.ReverseTax == true ? Decimal.MinusOne : Decimal.One));
				}
			}

			decimal? taxableAmountAfterGross = (curyTranAmt * (1m + inclusiveSumRates / 100)) / (1m + inclusiveSumRates / 100 + exclusiveSumRates / 100);
			decimal? inclusiveTaxTotalAmt = CalculateTaxTotalAmtWithRates(sender, row, inclusiveTaxRates, taxableAmountAfterGross);

			decimal? taxableAmt = taxableAmountAfterGross - inclusiveTaxTotalAmt;
			decimal? exclusiveTaxTotalAmt = CalculateTaxTotalAmtWithRates(sender, row, exclusiveTaxRates, taxableAmt);

			decimal? rawAmount = curyTranAmt - exclusiveTaxTotalAmt;

			PXCache childCache = sender.Graph.Caches[typeof(CABankTranDetail)];
			CABankTranDetail copy = sender.Graph.Caches[typeof(CABankTranDetail)].CreateCopy(row) as CABankTranDetail;
			copy.CuryTranAmt = rawAmount;
			childCache.Update(copy);
		}

		protected virtual decimal? CalculateTaxTotalAmtWithRates(PXCache sender, object row, List<RateWithMultiplier> taxRates, decimal? amount)
		{
			decimal? taxTotalAmt = 0m;
			foreach (var taxrow in taxRates)
			{
				decimal? curyCurrentTaxAmount = (amount * taxrow.TaxRate / 100m) ?? 0m;
				curyCurrentTaxAmount = PXDBCurrencyAttribute.RoundCury(sender, row, curyCurrentTaxAmount.Value, Precision) * taxrow.Multiplier;
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

		protected override void _CalcDocTotals(
			PXCache sender,
			object row,
			decimal CuryTaxTotal,
			decimal CuryInclTaxTotal,
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			decimal CuryLineTotal = CalcLineTotal(sender, row);

			decimal CuryDocTotal = CuryLineTotal + CuryTaxTotal - CuryInclTaxTotal;

			decimal doc_CuryLineTotal = (decimal)(ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m);
			decimal doc_CuryTaxTotal = (decimal)(ParentGetValue(sender.Graph, _CuryTaxTotal) ?? 0m);
			decimal doc_CuryTranAmt = Math.Abs((decimal)(ParentGetValue<CABankTran.curyTranAmt>(sender.Graph) ?? 0m));

			ParentSetValue<CABankTran.curyUnappliedBalCA>(sender.Graph, doc_CuryTranAmt - CuryDocTotal);
			ParentSetValue<CABankTran.curyApplAmtCA>(sender.Graph, CuryLineTotal);

			if (!Equals(CuryLineTotal, doc_CuryLineTotal) ||
				!Equals(CuryTaxTotal, doc_CuryTaxTotal))
			{
				ParentSetValue<CABankTran.curyTaxTotal>(sender.Graph, CuryTaxTotal);
				ParentSetValue<CABankTran.curyDetailsWithTaxesTotal>(sender.Graph, CuryDocTotal);
			}
		}

		protected override void ZoneUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var originalTaxCalc = TaxCalc;
			try
			{
				//old Tax Zone
				if (IsExternalTax(sender.Graph, (string)e.OldValue))
				{
					TaxCalc = TaxCalc.Calc;
				}
				//new Tax Zone
				if (IsExternalTax(sender.Graph, (string)sender.GetValue(e.Row, _TaxZoneID)) || (bool?)sender.GetValue(e.Row, "ExternalTaxesImportInProgress") == true)
				{
					TaxCalc = TaxCalc.ManualCalc;
				}


				if (e.OldValue == null && ( _TaxCalc == TaxCalc.Calc || _TaxCalc == TaxCalc.ManualLineCalc))
				{
					PXCache cache = sender.Graph.Caches[_ChildType];
					if (!CompareZone(sender.Graph, (string)e.OldValue, (string)sender.GetValue(e.Row, _TaxZoneID)) || sender.GetValue(e.Row, _TaxZoneID) == null)
					{
						Preload(sender);

						List<object> details = this.ChildSelect(cache, e.Row);
						ReDefaultTaxes(cache, details);

						_ParentRow = e.Row;
						CalcTaxes(cache, null);
						_ParentRow = null;
					}
				}
			}
			finally
			{
				TaxCalc = originalTaxCalc;
			}
		}
	} 
}
