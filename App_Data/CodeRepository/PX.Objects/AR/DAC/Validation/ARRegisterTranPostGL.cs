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
		InnerJoin<ARTranPostGL,
			On<ARTranPostGL.customerID, Equal<ARRegister.customerID>,
				And<ARTranPostGL.docType, Equal<ARRegister.docType>,
				And<ARTranPostGL.refNbr, Equal<ARRegister.refNbr>>>>>>), 
		Persistent = false)]
	[PXHidden]
	public class ARRegisterTranPostGL : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARRegisterTranPostGL>.By<docType, refNbr>
		{
			public static ARRegisterTranPostGL Find(PXGraph graph, string docType, string refNbr) =>
				FindBy(graph, docType, refNbr);
		}
		#endregion

		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARRegister))]
		public virtual string DocType { get; set; }
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARRegister))]
		public virtual string RefNbr { get; set; }
		#endregion

		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[Customer(BqlTable = typeof(ARRegister))]
		public virtual int? CustomerID { get; set; }
		#endregion

		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		[FinPeriodID(BqlTable = typeof(ARRegister))]
		public virtual string FinPeriodID { get; set; }
		#endregion

		#region TranPeriodID
		public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

		[PeriodID(BqlTable = typeof(ARRegister))]
		public virtual string TranPeriodID { get; set; }
		#endregion

		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		[PXDBString(1, IsFixed = true, BqlTable = typeof(ARRegister))]
		[ARDocStatus.List]
		public virtual string Status { get; set; }
		#endregion

		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }

		[PXDBBool(BqlTable = typeof(ARRegister))]
		public virtual bool? Hold { get; set; }
		#endregion

		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

		[PXDBBool(BqlTable = typeof(ARRegister))]
		public virtual bool? Voided { get; set; }
		#endregion

		#region Canceled
		public abstract class canceled : PX.Data.BQL.BqlBool.Field<canceled> { }

		[PXDBBool(BqlTable = typeof(ARRegister))]
		public virtual bool? Canceled { get; set; }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlLong.Field<curyID> { }

		[PXDBString(BqlTable = typeof(ARRegister))]
		public virtual string CuryID { get; set; }

		#endregion

		#region IsMigratedRecord
		public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

		[PXDBBool(BqlTable = typeof(ARRegister))]
		public virtual bool? IsMigratedRecord { get; set; }
		#endregion
		
		#region BalanceSign
		public abstract class balanceSign : PX.Data.BQL.BqlShort.Field<balanceSign> { }
		[PXDBShort(BqlTable=typeof(ARTranPostGL))]
		public virtual short? BalanceSign { get; set; }
		#endregion
		
		#region Type

		public abstract class type : PX.Data.BQL.BqlString.Field<type>
		{
		}

		[PXDBString(BqlTable = typeof(ARTranPostGL))]
		[ARTranPost.type.List]
		public virtual string Type { get; set; }

		#endregion

		#region CuryOrigDocAmt
		public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

		[PXDBDecimal(BqlTable = typeof(ARRegister))]
		public virtual Decimal? CuryOrigDocAmt { get; set; }
		#endregion

		#region CuryDocBal
		public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

		[PXDBDecimal(BqlTable = typeof(ARRegister))]
		public virtual Decimal? CuryDocBal { get; set; }
		#endregion

		#region DocBal
		public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

		[PXDBDecimal(BqlTable = typeof(ARRegister))]
		public virtual Decimal? DocBal { get; set; }
		#endregion

		#region RGOLAmt

		public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }

		[PXDBBaseCury(BqlTable = typeof(ARRegister))]
		public virtual decimal? RGOLAmt { get; set; }
		#endregion

		#region CalcCuryBalance
		public abstract class calcCuryBalance : PX.Data.BQL.BqlDecimal.Field<calcCuryBalance> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.curyBalanceAmt.Multiply<ARTranPostGL.balanceSign>), typeof(decimal))]
		public virtual decimal? CalcCuryBalance { get; set; }
		#endregion

		#region CalcCuryBalanceGL
		public abstract class calcCuryBalanceGL : PX.Data.BQL.BqlDecimal.Field<calcCuryBalanceGL> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.curyDebitARAmt.Subtract<ARTranPostGL.curyCreditARAmt>), typeof(decimal))]
		public virtual decimal? CalcCuryBalanceGL { get; set; }
		#endregion

		#region CalcBalance
		public abstract class calcBalance : PX.Data.BQL.BqlDecimal.Field<calcBalance> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.balanceAmt.Multiply<ARTranPostGL.balanceSign>), typeof(decimal))]
		public virtual decimal? CalcBalance { get; set; }
		#endregion

		#region CalcBalanceGL
		public abstract class calcBalanceGL : PX.Data.BQL.BqlDecimal.Field<calcBalanceGL> { }

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.debitARAmt.Subtract<ARTranPostGL.creditARAmt>), typeof(decimal))]
		public virtual decimal? CalcBalanceGL { get; set; }
		#endregion

		#region CalcRGOL
		public abstract class calcRGOL : PX.Data.BQL.BqlDecimal.Field<calcRGOL> { }

		[PXDecimal]
		[PXDBCalced(typeof(IIf<
			Where<ARTranPostGL.type, Equal<ARTranPost.type.application>>,
				ARTranPostGL.rGOLAmt,
				Zero>), typeof(decimal))]
		public virtual decimal? CalcRGOL { get; set; }
		#endregion

		#region MaxFinPeriodID
		public abstract class maxFinPeriodID : PX.Data.BQL.BqlString.Field<maxFinPeriodID> { }

		// Acuminator disable once PX1095 NoUnboundTypeAttributeWithPXDBCalced [Type field define with FinPeriod attribue]
		[FinPeriodID(IsDBField = false)]
		[PXDBCalced(typeof(IIf<Where<ARTranPostGL.type, Equal<ARTranPost.type.rgol>>
			, Null
			, ARTranPostGL.finPeriodID>), typeof(string))]
		public virtual string MaxFinPeriodID { get; set; }
		#endregion

		#region MaxTranPeriodID
		public abstract class maxTranPeriodID : PX.Data.BQL.BqlString.Field<maxTranPeriodID> { }

		// Acuminator disable once PX1095 NoUnboundTypeAttributeWithPXDBCalced [Type field define with FinPeriod attribue]
		[FinPeriodID(IsDBField = false)]
		[PXDBCalced(typeof(IIf<Where<ARTranPostGL.type, Equal<ARTranPost.type.rgol>>
			, Null
			, ARTranPostGL.tranPeriodID>), typeof(string))]
		public virtual string MaxTranPeriodID { get; set; }
		#endregion

		#region MaxDocDate
		public abstract class maxDocDate : PX.Data.BQL.BqlString.Field<maxDocDate> { }

		[PXDBDate(BqlField = typeof(ARTranPostGL.docDate))]
		public virtual DateTime? MaxDocDate { get; set; }
		#endregion
		
		#region CalcCuryRetainageReleased
		public abstract class calcCuryRetainageReleased : PX.Data.BQL.BqlDecimal.Field<calcCuryRetainageReleased>
		{
		}

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.curyRetainageReleasedAmt), typeof(decimal))]
		public virtual decimal? CalcCuryRetainageReleased { get; set; }

		#endregion
        
		#region CalcRetainageReleased
		public abstract class calcRetainageReleased : PX.Data.BQL.BqlDecimal.Field<calcRetainageReleased>
		{
		}

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.retainageReleasedAmt), typeof(decimal))]
		public virtual decimal? CalcRetainageReleased { get; set; }
		#endregion
		
		#region CalcCuryRetainageUnreleased
		public abstract class calcCuryRetainageUnreleased : PX.Data.BQL.BqlDecimal.Field<calcCuryRetainageUnreleased>
		{
		}

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.curyRetainageUnreleasedAmt), typeof(decimal))]
		public virtual decimal? CalcCuryRetainageUnreleased { get; set; }

		#endregion
        
		#region CalcRetainageUnreleased
		public abstract class calcRetainageUnreleased : PX.Data.BQL.BqlDecimal.Field<calcRetainageUnreleased>
		{
		}

		[PXDecimal]
		[PXDBCalced(typeof(ARTranPostGL.retainageUnreleasedAmt), typeof(decimal))]
		public virtual decimal? CalcRetainageUnreleased { get; set; }
		#endregion
	}

	[PXProjection(typeof(Select4<
		ARRegisterTranPostGL,
		Aggregate<
			GroupBy<ARRegisterTranPostGL.docType,
			GroupBy<ARRegisterTranPostGL.refNbr,
			GroupBy<ARRegisterTranPostGL.customerID,
			GroupBy<ARRegisterTranPostGL.finPeriodID,
			GroupBy<ARRegisterTranPostGL.tranPeriodID,
			GroupBy<ARRegisterTranPostGL.status,
			GroupBy<ARRegisterTranPostGL.hold,
			GroupBy<ARRegisterTranPostGL.voided,
			GroupBy<ARRegisterTranPostGL.canceled,
			GroupBy<ARRegisterTranPostGL.curyID,
			GroupBy<ARRegisterTranPostGL.isMigratedRecord,
			GroupBy<ARRegisterTranPostGL.curyOrigDocAmt,
			GroupBy<ARRegisterTranPostGL.curyDocBal,
			GroupBy<ARRegisterTranPostGL.docBal,
			GroupBy<ARRegisterTranPostGL.rGOLAmt,

			Sum<ARRegisterTranPostGL.calcCuryBalance,
			Sum<ARRegisterTranPostGL.calcCuryBalanceGL,
			Sum<ARRegisterTranPostGL.calcBalance,
			Sum<ARRegisterTranPostGL.calcBalanceGL,
			Sum<ARRegisterTranPostGL.calcRGOL,
			Sum<ARRegisterTranPostGL.calcCuryRetainageReleased,
			Sum<ARRegisterTranPostGL.calcRetainageReleased,
			Sum<ARRegisterTranPostGL.calcCuryRetainageUnreleased,
			Sum<ARRegisterTranPostGL.calcRetainageUnreleased,	
			Sum<ARRegisterTranPostGL.calcRGOL,
			Max<ARRegisterTranPostGL.maxFinPeriodID,
			Max<ARRegisterTranPostGL.maxTranPeriodID,
			Max<ARRegisterTranPostGL.maxDocDate
		>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARRegisterTranPostGLGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARRegisterTranPostGLGrouped>.By<docType, refNbr>
		{
			public static ARRegisterTranPostGLGrouped Find(PXGraph graph, string docType, string refNbr) =>
				FindBy(graph, docType, refNbr);
		}
		#endregion

		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string DocType { get; set; }
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(IsKey = true, BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string RefNbr { get; set; }
		#endregion

		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[Customer(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual int? CustomerID { get; set; }
		#endregion

		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		[FinPeriodID(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string FinPeriodID { get; set; }
		#endregion

		#region TranPeriodID
		public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

		[PeriodID(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string TranPeriodID { get; set; }
		#endregion

		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		[PXDBString(1, IsFixed = true, BqlTable = typeof(ARRegisterTranPostGL))]
		[ARDocStatus.List]
		public virtual string Status { get; set; }
		#endregion

		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }

		[PXDBBool(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual bool? Hold { get; set; }
		#endregion
		
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

		[PXDBBool(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual bool? Voided { get; set; }
		#endregion
		
		#region Canceled
		public abstract class canceled : PX.Data.BQL.BqlBool.Field<canceled> { }

		[PXDBBool(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual bool? Canceled { get; set; }
		#endregion

		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlLong.Field<curyID> { }

		[PXDBString(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string CuryID { get; set; }

		#endregion

		#region IsMigratedRecord
		public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

		[PXDBBool(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual bool? IsMigratedRecord { get; set; }
		#endregion

		#region CuryOrigDocAmt
		public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual Decimal? CuryOrigDocAmt { get; set; }
		#endregion

		#region CuryDocBal
		public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual Decimal? CuryDocBal { get; set; }
		#endregion

		#region DocBal
		public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual Decimal? DocBal { get; set; }
		#endregion

		#region RGOLAmt

		public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }

		[PXDBBaseCury(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? RGOLAmt { get; set; }
		#endregion


		#region CalcCuryBalance
		public abstract class calcCuryBalance : PX.Data.BQL.BqlDecimal.Field<calcCuryBalance> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcCuryBalance { get; set; }
		#endregion

		#region CalcCuryBalanceGL
		public abstract class calcCuryBalanceGL : PX.Data.BQL.BqlDecimal.Field<calcCuryBalanceGL> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcCuryBalanceGL { get; set; }
		#endregion

		#region CalcBalance
		public abstract class calcBalance : PX.Data.BQL.BqlDecimal.Field<calcBalance> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcBalance { get; set; }
		#endregion

		#region CalcBalanceGL
		public abstract class calcBalanceGL : PX.Data.BQL.BqlDecimal.Field<calcBalanceGL> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcBalanceGL { get; set; }
		#endregion

		#region CalcRGOL
		public abstract class calcRGOL : PX.Data.BQL.BqlDecimal.Field<calcRGOL> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcRGOL { get; set; }
		#endregion

		#region MaxFinPeriodID
		public abstract class maxFinPeriodID : PX.Data.BQL.BqlString.Field<maxFinPeriodID> { }

		[FinPeriodID(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string MaxFinPeriodID { get; set; }
		#endregion

		#region MaxTranPeriodID
		public abstract class maxTranPeriodID : PX.Data.BQL.BqlString.Field<maxTranPeriodID> { }

		[FinPeriodID(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual string MaxTranPeriodID { get; set; }
		#endregion

		#region MaxDocDate
		public abstract class maxDocDate : PX.Data.BQL.BqlString.Field<maxDocDate> { }

		[PXDBDate(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual DateTime? MaxDocDate { get; set; }
		#endregion
		
		#region CalcCuryRetainageReleased
		public abstract class calcCuryRetainageReleased : PX.Data.BQL.BqlDecimal.Field<calcCuryRetainageReleased> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcCuryRetainageReleased { get; set; }
		#endregion
		
		#region CalcRetainageReleased
		public abstract class calcRetainageReleased : PX.Data.BQL.BqlDecimal.Field<calcRetainageReleased> { }
		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcRetainageReleased { get; set; }
		#endregion

		#region CalcCuryRetainageUnreleased
		public abstract class calcCuryRetainageUnreleased : PX.Data.BQL.BqlDecimal.Field<calcCuryRetainageUnreleased> { }

		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcCuryRetainageUnreleased { get; set; }
		#endregion
		
		#region CalcRetainageUnreleased
		public abstract class calcRetainageUnreleased : PX.Data.BQL.BqlDecimal.Field<calcRetainageUnreleased> { }
		[PXDBDecimal(BqlTable = typeof(ARRegisterTranPostGL))]
		public virtual decimal? CalcRetainageUnreleased { get; set; }
		#endregion
	}
}
