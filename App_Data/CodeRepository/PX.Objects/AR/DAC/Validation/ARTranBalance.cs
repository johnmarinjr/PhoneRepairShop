using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select2<
		ARRegister,
		InnerJoin<ARTran,
			On<ARTran.tranType, Equal<ARRegister.docType>,
			And<ARTran.refNbr, Equal<ARRegister.refNbr>>>,
		InnerJoin<ARTranPostGL,
			On<ARTranPostGL.docType, Equal<ARTran.tranType>,
			And<ARTranPostGL.refNbr, Equal<ARTran.refNbr>,
			And<ARTranPostGL.lineNbr, Equal<ARTran.lineNbr>>>>>>,
		Where<ARRegister.paymentsByLinesAllowed, Equal<True>,
			And<ARRegister.released, Equal<True>
			>>>), Persistent = false)]
	[PXHidden]
	public class ARTranApplications : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARTranApplications>.By<tranType, refNbr, lineNbr>
		{
			public static ARTranApplications Find(PXGraph graph, string tranType, string refNbr, int? lineNbr) =>
				FindBy(graph, tranType, refNbr, lineNbr);
		}
		#endregion

		#region TranType
		public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARTran.tranType))]
		public virtual string TranType { get; set; }
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARTran.refNbr))]
		public virtual string RefNbr { get; set; }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true, BqlField = typeof(ARTran.lineNbr))]
		public virtual int? LineNbr { get; set; }
		#endregion

		#region CuryOrigTranAmt
		public abstract class curyOrigTranAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigTranAmt> { }

		[PXDBDecimal(BqlField = typeof(ARTran.curyOrigTranAmt))]
		public virtual decimal? CuryOrigTranAmt { get; set; }
		#endregion

		#region OrigTranAmt
		public abstract class origTranAmt : PX.Data.BQL.BqlDecimal.Field<origTranAmt> { }

		[PXDBDecimal(BqlField = typeof(ARTran.origTranAmt))]
		public virtual decimal? OrigTranAmt { get; set; }
		#endregion

		#region CuryAppBalanceSigned
		public abstract class curyAppBalanceSigned : PX.Data.BQL.BqlDecimal.Field<curyAppBalanceSigned> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.curyBalanceAmt.Multiply<ARTranPostGL.balanceSign.Multiply<decimal_1>>), typeof(decimal))]
		public virtual decimal? CuryAppBalanceSigned { get; set; }
		#endregion

		#region AppBalanceSigned
		public abstract class appBalanceSigned : PX.Data.BQL.BqlDecimal.Field<appBalanceSigned> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.balanceAmt.Multiply<ARTranPostGL.balanceSign.Multiply<decimal_1>>), typeof(decimal))]
		public virtual decimal? AppBalanceSigned { get; set; }
		#endregion
	}

	[PXProjection(typeof(Select4<
		ARTranApplications,
		Aggregate<
			GroupBy<ARTranApplications.tranType,
			GroupBy<ARTranApplications.refNbr,
			GroupBy<ARTranApplications.lineNbr,
			GroupBy<ARTranApplications.curyOrigTranAmt,
			GroupBy<ARTranApplications.origTranAmt,

			Sum<ARTranApplications.curyAppBalanceSigned,
			Sum<ARTranApplications.appBalanceSigned
			>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARTranApplicationsTotal : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARTranApplicationsTotal>.By<tranType, refNbr, lineNbr>
		{
			public static ARTranApplicationsTotal Find(PXGraph graph, string tranType, string refNbr, int? lineNbr) =>
				FindBy(graph, tranType, refNbr, lineNbr);
		}
		#endregion

		#region TranType
		public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARTranApplications.tranType))]
		public virtual string TranType { get; set; }
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARTranApplications.refNbr))]
		public virtual string RefNbr { get; set; }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true, BqlField = typeof(ARTranApplications.lineNbr))]
		public virtual int? LineNbr { get; set; }
		#endregion

		#region CuryOrigTranAmt
		public abstract class curyOrigTranAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigTranAmt> { }

		[PXDBDecimal(BqlField = typeof(ARTranApplications.curyOrigTranAmt))]
		public virtual decimal? CuryOrigTranAmt { get; set; }
		#endregion

		#region OrigTranAmt
		public abstract class origTranAmt : PX.Data.BQL.BqlDecimal.Field<origTranAmt> { }

		[PXDBDecimal(BqlField = typeof(ARTranApplications.origTranAmt))]
		public virtual decimal? OrigTranAmt { get; set; }
		#endregion

		#region CuryAppBalanceTotal
		public abstract class curyAppBalanceTotal : PX.Data.BQL.BqlDecimal.Field<curyAppBalanceTotal> { }

		[PXDBDecimal(BqlField = typeof(ARTranApplications.curyAppBalanceSigned))]
		public virtual decimal? CuryAppBalanceTotal { get; set; }
		#endregion

		#region AppBalanceTotal
		public abstract class appBalanceTotal : PX.Data.BQL.BqlDecimal.Field<appBalanceTotal> { }

		[PXDBDecimal(BqlField = typeof(ARTranApplications.appBalanceSigned))]
		public virtual decimal? AppBalanceTotal { get; set; }
		#endregion
	}
}
