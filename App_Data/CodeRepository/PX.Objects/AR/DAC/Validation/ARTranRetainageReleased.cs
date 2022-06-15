using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using PX.Data.BQL;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select2<
		ARRegister,
		InnerJoin<ARTran,
			On<ARTran.tranType, Equal<ARRegister.docType>,
			And<ARTran.refNbr, Equal<ARRegister.refNbr>>>>,
		Where<ARRegister.paymentsByLinesAllowed, Equal<True>,
			And<ARRegister.released, Equal<True>,
			And<Where<ARRegister.isRetainageDocument, Equal<True>,
					Or<ARRegister.isRetainageReversing, Equal<True>>>
			>>>>), Persistent = false)]
	[PXHidden]
	public class ARTranRetainageReleased : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARTranRetainageReleased>.By<origDocType, origRefNbr, origLineNbr>
		{
			public static ARTranRetainageReleased Find(PXGraph graph, string tranType, string refNbr, int? lineNbr) =>
				FindBy(graph, tranType, refNbr, lineNbr);
		}
		#endregion

		#region OrigDocType
		public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARRegister.origDocType))]
		public virtual string OrigDocType { get; set; }
		#endregion

		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

		[PXDBString(IsKey = true, BqlField = typeof(ARRegister.origRefNbr))]
		public virtual string OrigRefNbr { get; set; }
		#endregion

		#region OrigLineNbr
		public abstract class origLineNbr : PX.Data.BQL.BqlInt.Field<origLineNbr> { }

		[PXDBInt(IsKey = true, BqlField = typeof(ARTran.origLineNbr))]
		public virtual int? OrigLineNbr { get; set; }
		#endregion

		#region BalanceSign
		public abstract class balanceSign : PX.Data.BQL.BqlDecimal.Field<balanceSign> { }

		[PXDecimal]
		[PXDBCalced(typeof(
				Switch<Case<Where<ARRegister.docType.IsIn<ARDocType.refund, ARDocType.voidRefund, ARDocType.invoice, ARDocType.debitMemo, ARDocType.finCharge, ARDocType.smallCreditWO, ARDocType.cashSale>>,
					decimal1>,
					decimal_1>), typeof(decimal))]
		public virtual decimal? BalanceSign { get; set; }
		#endregion

		#region CuryRetainageReleased
		public abstract class curyRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyRetainageReleased> { }

		[PXDecimal]
		[PXDBCalced(typeof(
			Multiply<
			IIf<ARRegister.isRetainageDocument.IsEqual<True>,ARTran.curyOrigTranAmt, 
				PX.Data.BQL.Minus<ARTran.curyRetainageAmt.Add<ARTran.curyRetainedTaxAmt>>>,
			balanceSign>), typeof(decimal))]
		public virtual Decimal? CuryRetainageReleased { get; set; }
		#endregion

		#region RetainageReleased
		public abstract class retainageReleased : PX.Data.BQL.BqlDecimal.Field<retainageReleased> { }

		[PXDecimal]
		[PXDBCalced(typeof(
			Multiply<
				IIf<ARRegister.isRetainageDocument.IsEqual<True>,ARTran.origTranAmt, 
					PX.Data.BQL.Minus<ARTran.retainageAmt.Add<ARTran.retainedTaxAmt>>>,
				balanceSign>), typeof(decimal))]
		public virtual Decimal? RetainageReleased { get; set; }
		#endregion
	}

	[PXProjection(typeof(Select4<
		ARTranRetainageReleased,
		Aggregate<
			GroupBy<ARTranRetainageReleased.origDocType,
			GroupBy<ARTranRetainageReleased.origRefNbr,
			GroupBy<ARTranRetainageReleased.origLineNbr,

			Sum<ARTranRetainageReleased.curyRetainageReleased,
			Sum<ARTranRetainageReleased.retainageReleased
			>>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARTranRetainageReleasedTotal : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARTranRetainageReleasedTotal>.By<origDocType, origRefNbr, origLineNbr>
		{
			public static ARTranRetainageReleasedTotal Find(PXGraph graph, string tranType, string refNbr, int? lineNbr) =>
				FindBy(graph, tranType, refNbr, lineNbr);
		}
		#endregion

		#region OrigTranType
		public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARTranRetainageReleased))]
		public virtual string OrigDocType { get; set; }
		#endregion

		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARTranRetainageReleased))]
		public virtual string OrigRefNbr { get; set; }
		#endregion

		#region OrigLineNbr
		public abstract class origLineNbr : PX.Data.BQL.BqlInt.Field<origLineNbr> { }

		[PXDBInt(IsKey = true, BqlTable = typeof(ARTranRetainageReleased))]
		public virtual int? OrigLineNbr { get; set; }
		#endregion


		#region CuryRetainageReleased
		public abstract class curyRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyRetainageReleased> { }

		[PXDBDecimal(BqlField = typeof(ARTranRetainageReleased.curyRetainageReleased))]
		public virtual Decimal? CuryRetainageReleased { get; set; }
		#endregion

		#region RetainageReleased
		public abstract class retainageReleased : PX.Data.BQL.BqlDecimal.Field<retainageReleased> { }

		[PXDBDecimal(BqlField = typeof(ARTranRetainageReleased.retainageReleased))]
		public virtual Decimal? RetainageReleased { get; set; }
		#endregion
	}
}
