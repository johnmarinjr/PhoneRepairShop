using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO.DAC.Projections;
using PX.Objects.TX;

namespace PX.Objects.SO.Attributes
{
	public class BlanketSOUnbilledTaxAttribute : SOUnbilledTax2Attribute
	{
		public BlanketSOUnbilledTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this.CuryTranAmt = typeof(BlanketSOLine.curyUnbilledAmt);
			this.GroupDiscountRate = typeof(BlanketSOLine.groupDiscountRate);
			this.DocumentDiscountRate = typeof(BlanketSOLine.documentDiscountRate);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<BlanketSOLine.lineType, NotEqual<SOLineType.miscCharge>>, BlanketSOLine.curyUnbilledAmt>, decimal0>),
				typeof(SumCalc<SOOrder.curyUnbilledLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<BlanketSOLine.lineType, Equal<SOLineType.miscCharge>>, BlanketSOLine.curyUnbilledAmt>, decimal0>),
				typeof(SumCalc<SOOrder.curyUnbilledMiscTot>)));
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<BlanketSOLine.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}
	}
}
