using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.Common.Formula
{
	public class IsCopyPasteContext : BqlFormulaEvaluator, IBqlOperand
    {
        public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> pars)
        {
            return cache.Graph.IsCopyPasteContext;
        }
    }
}