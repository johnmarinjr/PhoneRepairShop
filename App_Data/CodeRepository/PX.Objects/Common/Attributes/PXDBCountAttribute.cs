using PX.Data;
using PX.Data.SQLTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Attributes
{
	public class PXDBCountAttribute : PXDBIntAttribute
	{
		protected Type DistinctByField { get; }

		public PXDBCountAttribute() { }

		public PXDBCountAttribute(Type distinctByField)
		{
			DistinctByField = distinctByField;
		}

		protected override void PrepareFieldName(string dbFieldName, PXCommandPreparingEventArgs e)
		{
			base.PrepareFieldName(dbFieldName, e);

			if (DistinctByField == null)
				e.Expr = SQLExpression.Count();
			else
				e.Expr = SQLExpression.CountDistinct(new Column(DistinctByField.Name, new SimpleTable(BqlCommand.GetItemType(DistinctByField).Name)));
		}
	}
}
