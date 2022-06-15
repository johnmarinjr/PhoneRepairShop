using System;
using System.Linq;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;

namespace PX.Objects.CA
{
	public class CATaxAttribute : ManualVATAttribute
	{
		protected override bool CalcGrossOnDocumentLevel { get => true; set => base.CalcGrossOnDocumentLevel = value; }
		protected override bool AskRecalculationOnCalcModeChange { get => true; set => base.AskRecalculationOnCalcModeChange = value; }

		public CATaxAttribute(Type parentType, Type taxType, Type taxSumType, Type calcMode = null, Type parentBranchIDField = null)
			: base(parentType, taxType, taxSumType, calcMode, parentBranchIDField)
		{
			Init();
		}
		
		private void Init()
		{
			this.CuryDocBal = typeof(CAAdj.curyTranAmt);
			this.CuryLineTotal = typeof(CAAdj.curySplitTotal);
			this.DocDate = typeof(CAAdj.tranDate);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			IComparer<Tax> taxByCalculationLevelComparer = GetTaxByCalculationLevelComparer();
			taxByCalculationLevelComparer.ThrowOnNull(nameof(taxByCalculationLevelComparer));

			List<ITaxDetail> taxDetails;
			IDictionary<string, PXResult<Tax, TaxRev>> tails;

			List<object> ret = new List<object>();

			BqlCommand selectTaxes = new Select2<Tax,
				LeftJoin<TaxRev, On<TaxRev.taxID, Equal<Tax.taxID>,
					And<TaxRev.outdated, Equal<boolFalse>,
					And2<
						Where<Current<CAAdj.drCr>, Equal<CADrCr.cACredit>, And<TaxRev.taxType, Equal<TaxType.purchase>, And<Tax.reverseTax, Equal<boolFalse>,
						Or<Current<CAAdj.drCr>, Equal<CADrCr.cACredit>, And<TaxRev.taxType, Equal<TaxType.sales>, And2<Where<Tax.reverseTax, Equal<boolTrue>,
							Or<Tax.taxType, Equal<CSTaxType.use>>>,
						Or<Current<CAAdj.drCr>, Equal<CADrCr.cADebit>, And<TaxRev.taxType, Equal<TaxType.sales>, And<Tax.reverseTax, Equal<boolFalse>,
							And<Tax.taxType, NotEqual<CSTaxType.withholding>, And<Tax.taxType, NotEqual<CSTaxType.use>>>>>>>>>>>>,
					And<Current<CAAdj.tranDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>>();

			object[] currents = new object[] { row, ((CATranEntry)graph).CAAdjRecords.Current };

			switch (taxchk)
			{
				case PXTaxCheck.Line:
					taxDetails = PXSelect<CATax,
						Where<CATax.adjTranType, Equal<Current<CASplit.adjTranType>>,
							And<CATax.adjRefNbr, Equal<Current<CASplit.adjRefNbr>>,
							And<CATax.lineNbr, Equal<Current<CASplit.lineNbr>>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<CATax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (CATax record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				case PXTaxCheck.RecalcLine:
					taxDetails = PXSelect<CATax,
						Where<CATax.adjTranType, Equal<Current<CAAdj.adjTranType>>,
							And<CATax.adjRefNbr, Equal<Current<CAAdj.adjRefNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<CATax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (CATax record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				case PXTaxCheck.RecalcTotals:
					taxDetails = PXSelect<CATaxTran,
						Where<CATaxTran.module, Equal<BatchModule.moduleCA>,
							And<CATaxTran.tranType, Equal<Current<CAAdj.adjTranType>>,
							And<CATaxTran.refNbr, Equal<Current<CAAdj.adjRefNbr>>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<CATaxTran>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (CATaxTran record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				default:
					return ret;
			}
		}
		
		protected override List<object> SelectDocumentLines(PXGraph graph, object row)
		{
			List<object> ret = PXSelect<CASplit,
								Where<CASplit.adjTranType, Equal<Current<CAAdj.adjTranType>>,
									And<CASplit.adjRefNbr, Equal<Current<CAAdj.adjRefNbr>>>>>
									.SelectMultiBound(graph, new object[] { row })
									.RowCast<CASplit>()
									.Select(_ => (object)_)
									.ToList();
			return ret;
		}

		public override void CacheAttached(PXCache sender)
		{
			if (sender.Graph is CATranEntry)
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
			if (sender.Graph is CATranEntry)
			{
				decimal curyLineTotal = 0m;
				foreach (CASplit detrow in ((CATranEntry)sender.Graph).CASplitRecords.View.SelectMultiBound(new object[1] { row }))
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
			sender.SetValue<CASplit.curyTaxableAmt>(row, value);
		}

		protected override void SetTaxAmt(PXCache sender, object row, decimal? value)
		{
			sender.SetValue<CASplit.curyTaxAmt>(row, value);
		}

		protected override decimal? GetTaxableAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CASplit.curyTaxableAmt>(row);
		}

		protected override decimal? GetTaxAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CASplit.curyTaxAmt>(row);
		}

		protected override void SetExtCostExt(PXCache sender, object child, decimal? value)
		{
			CASplit row = child as CASplit;
			if (row != null)
			{
				row.CuryTranAmt = value;
				sender.Update(row);
			}
		}

		protected override string GetExtCostLabel(PXCache sender, object row)
		{
			return ((PXDecimalState) sender.GetValueExt<CASplit.curyTranAmt>(row)).DisplayName;
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
