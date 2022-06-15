using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select5<
		ARHistory,
		LeftJoin<ARHistoryYtd,
			On<ARHistoryYtd.customerID, Equal<ARHistory.customerID>,
				And<ARHistoryYtd.branchID, Equal<ARHistory.branchID>,
				And<ARHistoryYtd.accountID, Equal<ARHistory.accountID>,
				And<ARHistoryYtd.subID, Equal<ARHistory.subID>,
				And<ARHistoryYtd.finPeriodID, LessEqual<ARHistory.finPeriodID>>>>>>>,
		Aggregate<
			GroupBy<ARHistory.branchID,
			GroupBy<ARHistory.accountID,
			GroupBy<ARHistory.subID,
			GroupBy<ARHistory.customerID,
			GroupBy<ARHistory.finPeriodID,

			GroupBy<ARHistory.finBegBalance,
			GroupBy<ARHistory.finPtdSales,
			GroupBy<ARHistory.finPtdDrAdjustments,
			GroupBy<ARHistory.finPtdFinCharges,
			GroupBy<ARHistory.finPtdPayments,
			GroupBy<ARHistory.finPtdCrAdjustments,
			GroupBy<ARHistory.finPtdDiscounts,
			GroupBy<ARHistory.finPtdRGOL,
			GroupBy<ARHistory.finYtdBalance,
			GroupBy<ARHistory.finYtdDeposits,
			GroupBy<ARHistory.finYtdRetainageReleased,
			GroupBy<ARHistory.finYtdRetainageWithheld,

			GroupBy<ARHistory.tranBegBalance,
			GroupBy<ARHistory.tranPtdSales,
			GroupBy<ARHistory.tranPtdDrAdjustments,
			GroupBy<ARHistory.tranPtdFinCharges,
			GroupBy<ARHistory.tranPtdPayments,
			GroupBy<ARHistory.tranPtdCrAdjustments,
			GroupBy<ARHistory.tranPtdDiscounts,
			GroupBy<ARHistory.tranPtdRGOL,
			GroupBy<ARHistory.tranYtdBalance,
			GroupBy<ARHistory.tranYtdDeposits,
			GroupBy<ARHistory.tranYtdRetainageReleased,
			GroupBy<ARHistory.tranYtdRetainageWithheld,

			Sum<ARHistoryYtd.finPtdSales,
			Sum<ARHistoryYtd.finPtdDrAdjustments,
			Sum<ARHistoryYtd.finPtdFinCharges,
			Sum<ARHistoryYtd.finPtdPayments,
			Sum<ARHistoryYtd.finPtdCrAdjustments,
			Sum<ARHistoryYtd.finPtdDiscounts,
			Sum<ARHistoryYtd.finPtdRGOL,
			Sum<ARHistoryYtd.finPtdDeposits,
			Sum<ARHistoryYtd.finPtdRetainageReleased,
			Sum<ARHistoryYtd.finPtdRetainageWithheld,

			Sum<ARHistoryYtd.tranPtdSales,
			Sum<ARHistoryYtd.tranPtdDrAdjustments,
			Sum<ARHistoryYtd.tranPtdFinCharges,
			Sum<ARHistoryYtd.tranPtdPayments,
			Sum<ARHistoryYtd.tranPtdCrAdjustments,
			Sum<ARHistoryYtd.tranPtdDiscounts,
			Sum<ARHistoryYtd.tranPtdRGOL,
			Sum<ARHistoryYtd.tranPtdDeposits,
			Sum<ARHistoryYtd.tranPtdRetainageReleased,
			Sum<ARHistoryYtd.tranPtdRetainageWithheld
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		), Persistent = false)]
	[PXHidden]
	public class ARHistoryYtdGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARHistoryYtdGrouped>.By<branchID, accountID, subID, customerID, finPeriodID>
		{
			public static ARHistoryYtdGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, customerID, finPeriodID);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(ARHistory))]
		public virtual int? BranchID { get; set; }
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(ARHistory))]
		public virtual int? AccountID { get; set; }
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(ARHistory))]
		public virtual int? SubID { get; set; }
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[Customer(IsKey = true, BqlTable = typeof(ARHistory))]
		public virtual int? CustomerID { get; set; }
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodID(IsKey = true, BqlTable = typeof(ARHistory))]
		public virtual string FinPeriodID { get; set; }
		#endregion

		#region FinBegBalance
		public abstract class finBegBalance : PX.Data.BQL.BqlDecimal.Field<finBegBalance> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinBegBalance { get; set; }
		#endregion
		#region FinPtdSales
		public abstract class finPtdSales : PX.Data.BQL.BqlDecimal.Field<finPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdSales { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdDrAdjustments { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdFinCharges { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdPayments { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdCrAdjustments { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<finPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdDiscounts { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdRGOL { get; set; }
		#endregion
		#region FinYtdBalance
		public abstract class finYtdBalance : PX.Data.BQL.BqlDecimal.Field<finYtdBalance> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinYtdBalance { get; set; }
		#endregion
		#region FinYtdDeposits
		public abstract class finYtdDeposits : PX.Data.BQL.BqlDecimal.Field<finYtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinYtdDeposits { get; set; }
		#endregion
		#region FinYtdRetainageReleased
		public abstract class finYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinYtdRetainageReleased { get; set; }
		#endregion
		#region FinYtdRetainageWithheld
		public abstract class finYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinYtdRetainageWithheld { get; set; }
		#endregion

		#region TranBegBalance
		public abstract class tranBegBalance : PX.Data.BQL.BqlDecimal.Field<tranBegBalance> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranBegBalance { get; set; }
		#endregion
		#region TranPtdSales
		public abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdSales { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdDrAdjustments { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdFinCharges { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdPayments { get; set; }
		#endregion
		#region TranPtdCrAdjustments
		public abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdCrAdjustments { get; set; }
		#endregion
		#region TranPtdDiscounts
		public abstract class tranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdDiscounts { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdRGOL { get; set; }
		#endregion
		#region TranYtdBalance
		public abstract class tranYtdBalance : PX.Data.BQL.BqlDecimal.Field<tranYtdBalance> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranYtdBalance { get; set; }
		#endregion
		#region TranYtdDeposits
		public abstract class tranYtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranYtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranYtdDeposits { get; set; }
		#endregion
		#region TranYtdRetainageReleased
		public abstract class tranYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranYtdRetainageReleased { get; set; }
		#endregion
		#region TranYtdRetainageWithheld
		public abstract class tranYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranYtdRetainageWithheld { get; set; }
		#endregion

		#region FinPtdSales
		public abstract class finPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<finPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdSales))]
		public virtual decimal? FinPtdSalesSum { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdDrAdjustments))]
		public virtual decimal? FinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<finPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdFinCharges))]
		public virtual decimal? FinPtdFinChargesSum { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdPayments))]
		public virtual decimal? FinPtdPaymentsSum { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdCrAdjustments))]
		public virtual decimal? FinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdDiscounts))]
		public virtual decimal? FinPtdDiscountsSum { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<finPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdRGOL))]
		public virtual decimal? FinPtdRGOLSum { get; set; }
		#endregion
		#region FinYtdDepositsSum
		public abstract class finYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<finYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdDeposits))]
		public virtual decimal? FinYtdDepositsSum { get; set; }
		#endregion
		#region FinYtdRetainageReleasedSum
		public abstract class finYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdRetainageReleased))]
		public virtual decimal? FinYtdRetainageReleasedSum { get; set; }
		#endregion
		#region FinYtdRetainageWithheldSum
		public abstract class finYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.finPtdRetainageWithheld))]
		public virtual decimal? FinYtdRetainageWithheldSum { get; set; }
		#endregion

		#region TranPtdSales
		public abstract class tranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdSales))]
		public virtual decimal? TranPtdSalesSum { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdDrAdjustments))]
		public virtual decimal? TranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdFinCharges))]
		public virtual decimal? TranPtdFinChargesSum { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdPayments))]
		public virtual decimal? TranPtdPaymentsSum { get; set; }
		#endregion
		#region TranPtdCrAdjustments
		public abstract class tranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdCrAdjustments))]
		public virtual decimal? TranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdDiscounts
		public abstract class tranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdDiscounts))]
		public virtual decimal? TranPtdDiscountsSum { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdRGOL))]
		public virtual decimal? TranPtdRGOLSum { get; set; }
		#endregion
		#region TranYtdDepositsSum
		public abstract class tranYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<tranYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdDeposits))]
		public virtual decimal? TranYtdDepositsSum { get; set; }
		#endregion
		#region TranYtdRetainageReleasedSum
		public abstract class tranYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdRetainageReleased))]
		public virtual decimal? TranYtdRetainageReleasedSum { get; set; }
		#endregion
		#region TranYtdRetainageWithheldSum
		public abstract class tranYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryYtd.tranPtdRetainageWithheld))]
		public virtual decimal? TranYtdRetainageWithheldSum { get; set; }
		#endregion
	}

	[PXHidden]
	public class ARHistoryYtd : ARHistory
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARHistoryYtd>.By<branchID, accountID, subID, customerID, finPeriodID>
		{
			public static ARHistoryYtd Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, customerID, finPeriodID);
		}
		#endregion

		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		public new abstract class finPtdSales : PX.Data.BQL.BqlDecimal.Field<finPtdSales> { }
		public new abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		public new abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		public new abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		public new abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		public new abstract class finPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<finPtdDiscounts> { }
		public new abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		public new abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		public new abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		public new abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }

		public new abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		public new abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		public new abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		public new abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		public new abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		public new abstract class tranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscounts> { }
		public new abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		public new abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		public new abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		public new abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
	}
}
