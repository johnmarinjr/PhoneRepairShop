using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.SQLTree;
using PX.Objects.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
	[PXHidden]
	[PXProjection(typeof(
		SelectFrom<POReceiptLine>
		.Where<
			POReceiptLine.lineType.IsNotIn<POLineType.service, POLineType.freight>>
		.AggregateTo<
			GroupBy<POReceiptLine.receiptType,
			GroupBy<POReceiptLine.receiptNbr>>>))]
	public class POReceiptLinesCount: IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<POReceiptLinesCount>.By<receiptType, receiptNbr>
		{
			public static POReceiptLinesCount Find(PXGraph graph, string receiptType, string receiptNbr) => FindBy(graph, receiptType, receiptNbr);
		}
		public static class FK
		{
			public class Receipt : POReceipt.PK.ForeignKeyOf<POReceiptLinesCount>.By<receiptType, receiptNbr> { }
		}
		#endregion

		#region ReceiptType
		[PXDBString(POReceiptLine.receiptType.Length, IsFixed = true, IsKey = true, BqlField = typeof(POReceiptLine.receiptType))]
		public virtual string ReceiptType { get; set; }
		public abstract class receiptType : BqlString.Field<receiptType> { }
		#endregion

		#region ReceiptNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POReceiptLine.receiptNbr))]
		public virtual string ReceiptNbr { get; set; }
		public abstract class receiptNbr : BqlString.Field<receiptNbr> { }
		#endregion

		#region LinesCount
		[PXDBCount]
		public virtual int? LinesCount { get; set; }
		public abstract class linesCount : BqlInt.Field<linesCount> { }
		#endregion
	}
}
