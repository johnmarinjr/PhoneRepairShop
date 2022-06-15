using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.Common.Attributes;
using PX.Objects.CS;
using System;


namespace PX.Objects.SO.DAC.Projections
{
	[PXCacheName(Messages.BlanketSOAdjustment)]
	[PXProjection(typeof(Select<SOAdjust>), Persistent = true)]
	public class BlanketSOAdjust : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BlanketSOAdjust>.By<recordID, adjdOrderType, adjdOrderNbr, adjgDocType, adjgRefNbr>
		{
			public static BlanketSOAdjust Find(PXGraph graph, int recordID, string adjdOrderType, string adjdOrderNbr, string adjgDocType, string adjgRefNbr)
				=> FindBy(graph, recordID, adjdOrderType, adjdOrderNbr, adjgDocType, adjgRefNbr);
		}
		public static class FK
		{
			public class BlanketOrder : BlanketSOOrder.PK.ForeignKeyOf<BlanketSOAdjust>.By<adjdOrderType, adjdOrderNbr> { }
		}
		#endregion
		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }

		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField This projection has the same keys as SOAdjust has
		[PXDBIdentity(IsKey = true, BqlField = typeof(SOAdjust.recordID))]
		public virtual Int32? RecordID
		{
			get;
			set;
		}
		#endregion
		#region AdjgDocType
		public abstract class adjgDocType : PX.Data.BQL.BqlString.Field<adjgDocType> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField This projection has the same keys as SOAdjust has
		[PXDBString(SOAdjust.AdjgDocTypeLength, IsKey = true, IsFixed = true, InputMask = "", BqlField = typeof(SOAdjust.adjgDocType))]
		[ARPaymentType.List()]
		[PXDefault]
		public virtual String AdjgDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjgRefNbr
		public abstract class adjgRefNbr : PX.Data.BQL.BqlString.Field<adjgRefNbr> { }

		public const int AdjgRefNbrLength = 15;
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField This projection has the same keys as SOAdjust has
		[PXDBString(SOAdjust.AdjgRefNbrLength, IsKey = true, IsUnicode = true, BqlField = typeof(SOAdjust.adjgRefNbr))]
		[PXParent(typeof(Select<ARPaymentTotals,
			Where<ARPaymentTotals.docType, Equal<Current<BlanketSOAdjust.adjgDocType>>,
				And<ARPaymentTotals.refNbr, Equal<Current<BlanketSOAdjust.adjgRefNbr>>>>>), ParentCreate = true)]
		public virtual String AdjgRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjdOrderType
		public abstract class adjdOrderType : PX.Data.BQL.BqlString.Field<adjdOrderType> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField This projection has the same keys as SOAdjust has
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(SOAdjust.adjdOrderType))]
		public virtual String AdjdOrderType
		{
			get;
			set;
		}
		#endregion
		#region AdjdOrderNbr
		public abstract class adjdOrderNbr : PX.Data.BQL.BqlString.Field<adjdOrderNbr> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField This projection has the same keys as SOAdjust has
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(SOAdjust.adjdOrderNbr))]
		[PXDefault()]
		[PXParent(typeof(FK.BlanketOrder))]
		public virtual String AdjdOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjgAmt
		public abstract class curyAdjgAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOAdjust.curyAdjgAmt))]
		[PXFormula(typeof(Maximum<Sub<BlanketSOAdjust.curyAdjgOrigBlanketAmt, BlanketSOAdjust.curyAdjgTransferredToChildrenAmt>, decimal0>))]
		public virtual Decimal? CuryAdjgAmt
		{
			get;
			set;
		}
		#endregion
		#region AdjAmt
		public abstract class adjAmt : PX.Data.BQL.BqlDecimal.Field<adjAmt> { }
		[PXFormula(typeof(Maximum<Sub<BlanketSOAdjust.adjOrigBlanketAmt, BlanketSOAdjust.adjTransferredToChildrenAmt>, decimal0>))]
		[PXDBDecimal(4, BqlField = typeof(SOAdjust.adjAmt))]
		[PXDefault]
		public virtual Decimal? AdjAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjdAmt
		public abstract class curyAdjdAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdAmt> { }

		[PXFormula(typeof(Maximum<Sub<BlanketSOAdjust.curyAdjdOrigBlanketAmt, BlanketSOAdjust.curyAdjdTransferredToChildrenAmt>, decimal0>))]
		[PXDBDecimal(4, BqlField = typeof(SOAdjust.curyAdjdAmt))]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<False>, And<isCCAuthorized, Equal<False>>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyUnreleasedPaymentAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<False>, And<isCCAuthorized, Equal<True>>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyCCAuthorizedAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>, And<paymentReleased, Equal<True>>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyPaidAmt>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<voided, Equal<False>>,
				curyAdjdAmt>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyPaymentTotal>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(IIf<Where<voided, Equal<False>,
				And<Where<isCCPayment, Equal<False>,
					Or<isCCAuthorized, Equal<True>,
					Or<isCCCaptured, Equal<True>,
					Or<paymentReleased, Equal<True>>>>>>>,
				Add<curyAdjdAmt, curyAdjdBilledAmt>, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyPaymentOverall>),
			ForceAggregateRecalculation = true)]
		[CopyChildLink(typeof(ARPaymentTotals.orderCntr), typeof(curyAdjdAmt),
			new Type[] { typeof(adjdOrderType), typeof(adjdOrderNbr) },
			new Type[] { typeof(ARPaymentTotals.adjdOrderType), typeof(ARPaymentTotals.adjdOrderNbr) })]
		[PXDefault]
		public virtual Decimal? CuryAdjdAmt
		{
			get;
			set;
		}
		#endregion

		#region CuryAdjdBilledAmt
		public abstract class curyAdjdBilledAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdBilledAmt> { }
		[PXDBDecimal(4, BqlField = typeof(SOAdjust.curyAdjdBilledAmt))]
		[PXDefault]
		public virtual Decimal? CuryAdjdBilledAmt
		{
			get;
			set;
		}
		#endregion

		#region IsCCPayment
		public abstract class isCCPayment : Data.BQL.BqlBool.Field<isCCPayment> { }
		[PXDBBool(BqlField = typeof(SOAdjust.isCCPayment))]
		public virtual bool? IsCCPayment
		{
			get;
			set;
		}
		#endregion
		#region PaymentReleased
		public abstract class paymentReleased : Data.BQL.BqlBool.Field<paymentReleased> { }
		[PXDBBool(BqlField = typeof(SOAdjust.paymentReleased))]
		public virtual bool? PaymentReleased
		{
			get;
			set;
		}
		#endregion
		#region IsCCAuthorized
		public abstract class isCCAuthorized : Data.BQL.BqlBool.Field<isCCAuthorized> { }
		[PXDBBool(BqlField = typeof(SOAdjust.isCCAuthorized))]
		public virtual bool? IsCCAuthorized
		{
			get;
			set;
		}
		#endregion
		#region IsCCCaptured
		public abstract class isCCCaptured : Data.BQL.BqlBool.Field<isCCCaptured> { }
		[PXDBBool(BqlField = typeof(SOAdjust.isCCCaptured))]
		public virtual bool? IsCCCaptured
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		[PXDBBool(BqlField = typeof(SOAdjust.voided))]
		public virtual Boolean? Voided
		{
			get;
			set;
		}
		#endregion




		#region CuryAdjgTransferredToChildrenAmt
		public abstract class curyAdjgTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgTransferredToChildrenAmt> { }

		[PXDBDecimal(4, BqlField = typeof(SOAdjust.curyAdjgTransferredToChildrenAmt))]
		[PXDefault]
		public virtual decimal? CuryAdjgTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion
		#region AdjTransferredToChildrenAmt
		public abstract class adjTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<adjTransferredToChildrenAmt> { }

		[PXDBDecimal(4, BqlField = typeof(SOAdjust.adjTransferredToChildrenAmt))]
		[PXDefault]
		public virtual Decimal? AdjTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjdTransferredToChildrenAmt
		public abstract class curyAdjdTransferredToChildrenAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdTransferredToChildrenAmt> { }

		[PXDBDecimal(4, BqlField = typeof(SOAdjust.curyAdjdTransferredToChildrenAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Transferred to Child Orders", Enabled = false)]
		[PXUnboundFormula(
			typeof(IIf<Where<voided, Equal<False>>, curyAdjdTransferredToChildrenAmt, decimal0>),
			typeof(SumCalc<BlanketSOOrder.curyTransferredToChildrenPaymentTotal>),
			ForceAggregateRecalculation = true)]
		public virtual decimal? CuryAdjdTransferredToChildrenAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjdOrigBlanketAmt
		public abstract class curyAdjdOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjdOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<BlanketSOAdjust.curyAdjdAmt, BlanketSOAdjust.curyAdjdTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? CuryAdjdOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion
		#region AdjOrigBlanketAmt
		public abstract class adjOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<adjOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<BlanketSOAdjust.adjAmt, BlanketSOAdjust.adjTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? AdjOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryAdjgOrigBlanketAmt
		public abstract class curyAdjgOrigBlanketAmt : PX.Data.BQL.BqlDecimal.Field<curyAdjgOrigBlanketAmt> { }

		[PXDBCalced(typeof(Add<BlanketSOAdjust.curyAdjgAmt, BlanketSOAdjust.curyAdjgTransferredToChildrenAmt>), typeof(decimal), Persistent = true)]
		[PXDecimal(4)]
		public virtual decimal? CuryAdjgOrigBlanketAmt
		{
			get;
			set;
		}
		#endregion

	}
}
