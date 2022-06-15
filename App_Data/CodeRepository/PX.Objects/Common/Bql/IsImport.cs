using PX.Data;
using PX.Data.BQL;
using PX.Data.SQLTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Bql
{
	[Obsolete]
    public class IsImport: BqlOperand<IsImport, IBqlBool>, IBqlCreator
	{
		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			value = cache.Graph.IsImport == true;
		}

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			if (graph != null && info.BuildExpression) 
				exp = SQLExpression.IsTrue(graph.IsImport);
			return true;
		}
    }
}