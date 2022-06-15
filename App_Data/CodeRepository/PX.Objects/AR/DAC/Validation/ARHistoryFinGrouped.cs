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
		LeftJoin<ARHistoryTran,
			On<ARHistoryTran.customerID, Equal<ARHistory.customerID>,
				And<ARHistoryTran.branchID, Equal<ARHistory.branchID>,
				And<ARHistoryTran.accountID, Equal<ARHistory.accountID>,
				And<ARHistoryTran.subID, Equal<ARHistory.subID>,
				And<ARHistoryTran.finPeriodID, Equal<ARHistory.finPeriodID>>>>>>>,
		Aggregate<
			GroupBy<ARHistory.branchID,
			GroupBy<ARHistory.accountID,
			GroupBy<ARHistory.subID,
			GroupBy<ARHistory.customerID,
			GroupBy<ARHistory.finPeriodID,

			GroupBy<ARHistory.finPtdSales,
			GroupBy<ARHistory.finPtdPayments,
			GroupBy<ARHistory.finPtdDrAdjustments,
			GroupBy<ARHistory.finPtdCrAdjustments,
			GroupBy<ARHistory.finPtdDiscounts,
			GroupBy<ARHistory.finPtdItemDiscounts,
			GroupBy<ARHistory.finPtdRGOL,
			GroupBy<ARHistory.finPtdFinCharges,
			GroupBy<ARHistory.finPtdDeposits,
			GroupBy<ARHistory.finPtdRetainageWithheld,
			GroupBy<ARHistory.finPtdRetainageReleased,

			Sum<ARHistoryTran.ptdSales,
			Sum<ARHistoryTran.ptdPayments,
			Sum<ARHistoryTran.ptdDrAdjustments,
			Sum<ARHistoryTran.ptdCrAdjustments,
			Sum<ARHistoryTran.ptdDiscounts,
			Sum<ARHistoryTran.ptdItemDiscounts,
			Sum<ARHistoryTran.ptdRGOL,
			Sum<ARHistoryTran.ptdFinCharges,
			Sum<ARHistoryTran.ptdDeposits,
			Sum<ARHistoryTran.ptdRetainageWithheld,
			Sum<ARHistoryTran.ptdRetainageReleased
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARHistoryFinGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARHistoryFinGrouped>.By<branchID, accountID, subID, customerID, finPeriodID>
		{
			public static ARHistoryFinGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, customerID, finPeriodID);
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

		#region FinPtdSales
		public abstract class finPtdSales : PX.Data.BQL.BqlDecimal.Field<finPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdSales { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdPayments { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdDrAdjustments { get; set; }
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
		#region FinPtdItemDiscounts
		public abstract class finPtdItemDiscounts : PX.Data.BQL.BqlDecimal.Field<finPtdItemDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdItemDiscounts { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdRGOL { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdFinCharges { get; set; }
		#endregion
		#region FinPtdDeposits
		public abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdDeposits { get; set; }
		#endregion
		#region FinPtdRetainageWithheld
		public abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdRetainageWithheld { get; set; }
		#endregion
		#region FinPtdRetainageReleased
		public abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? FinPtdRetainageReleased { get; set; }
		#endregion

		#region FinPtdSales
		public abstract class finPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<finPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdSales))]
		public virtual decimal? FinPtdSalesSum { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdPayments))]
		public virtual decimal? FinPtdPaymentsSum { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDrAdjustments))]
		public virtual decimal? FinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdCrAdjustments))]
		public virtual decimal? FinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDiscounts))]
		public virtual decimal? FinPtdDiscountsSum { get; set; }
		#endregion
		#region FinPtdItemDiscounts
		public abstract class finPtdItemDiscountsSum : PX.Data.BQL.BqlDecimal.Field<finPtdItemDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdItemDiscounts))]
		public virtual decimal? FinPtdItemDiscountsSum { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<finPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRGOL))]
		public virtual decimal? FinPtdRGOLSum { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<finPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdFinCharges))]
		public virtual decimal? FinPtdFinChargesSum { get; set; }
		#endregion
		#region FinPtdDeposits
		public abstract class finPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDeposits))]
		public virtual decimal? FinPtdDepositsSum { get; set; }
		#endregion
		#region FinPtdRetainageWithheld
		public abstract class finPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRetainageWithheld))]
		public virtual decimal? FinPtdRetainageWithheldSum { get; set; }
		#endregion
		#region FinPtdRetainageReleased
		public abstract class finPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRetainageReleased))]
		public virtual decimal? FinPtdRetainageReleasedSum { get; set; }
		#endregion
	}
	
	[PXProjection(typeof(Select5<
		ARHistory,
		LeftJoin<ARHistoryTran,
			On<ARHistoryTran.customerID, Equal<ARHistory.customerID>,
				And<ARHistoryTran.branchID, Equal<ARHistory.branchID>,
				And<ARHistoryTran.accountID, Equal<ARHistory.accountID>,
				And<ARHistoryTran.subID, Equal<ARHistory.subID>,
				And<ARHistoryTran.tranPeriodID, Equal<ARHistory.finPeriodID>>>>>>>,
		Aggregate<
			GroupBy<ARHistory.branchID,
			GroupBy<ARHistory.accountID,
			GroupBy<ARHistory.subID,
			GroupBy<ARHistory.customerID,
			GroupBy<ARHistory.finPeriodID,

			GroupBy<ARHistory.finPtdSales,
			GroupBy<ARHistory.finPtdPayments,
			GroupBy<ARHistory.finPtdDrAdjustments,
			GroupBy<ARHistory.finPtdCrAdjustments,
			GroupBy<ARHistory.finPtdDiscounts,
			GroupBy<ARHistory.finPtdItemDiscounts,
			GroupBy<ARHistory.finPtdRGOL,
			GroupBy<ARHistory.finPtdFinCharges,
			GroupBy<ARHistory.finPtdDeposits,
			GroupBy<ARHistory.finPtdRetainageWithheld,
			GroupBy<ARHistory.finPtdRetainageReleased,

			Sum<ARHistoryTran.ptdSales,
			Sum<ARHistoryTran.ptdPayments,
			Sum<ARHistoryTran.ptdDrAdjustments,
			Sum<ARHistoryTran.ptdCrAdjustments,
			Sum<ARHistoryTran.ptdDiscounts,
			Sum<ARHistoryTran.ptdItemDiscounts,
			Sum<ARHistoryTran.ptdRGOL,
			Sum<ARHistoryTran.ptdFinCharges,
			Sum<ARHistoryTran.ptdDeposits,
			Sum<ARHistoryTran.ptdRetainageWithheld,
			Sum<ARHistoryTran.ptdRetainageReleased
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARHistoryTranGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARHistoryTranGrouped>.By<branchID, accountID, subID, customerID, finPeriodID>
		{
			public static ARHistoryTranGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String finPeriodID) 
				=> FindBy(graph, branchID, accountID, subID, customerID, finPeriodID);
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

		#region TranPtdSales
		public abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdSales { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdPayments { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdDrAdjustments { get; set; }
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
		#region TranPtdItemDiscounts
		public abstract class tranPtdItemDiscounts : PX.Data.BQL.BqlDecimal.Field<tranPtdItemDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdItemDiscounts { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdRGOL { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdFinCharges { get; set; }
		#endregion
		#region TranPtdDeposits
		public abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdDeposits { get; set; }
		#endregion
		#region TranPtdRetainageWithheld
		public abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdRetainageWithheld { get; set; }
		#endregion
		#region TranPtdRetainageReleased
		public abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(ARHistory))]
		public virtual decimal? TranPtdRetainageReleased { get; set; }
		#endregion

		#region TranPtdSales
		public abstract class tranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdSales))]
		public virtual decimal? TranPtdSalesSum { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdPayments))]
		public virtual decimal? TranPtdPaymentsSum { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDrAdjustments))]
		public virtual decimal? TranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdCrAdjustments
		public abstract class tranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdCrAdjustments))]
		public virtual decimal? TranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdDiscounts
		public abstract class tranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDiscounts))]
		public virtual decimal? TranPtdDiscountsSum { get; set; }
		#endregion
		#region TranPtdItemDiscounts
		public abstract class tranPtdItemDiscountsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdItemDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdItemDiscounts))]
		public virtual decimal? TranPtdItemDiscountsSum { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRGOL))]
		public virtual decimal? TranPtdRGOLSum { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdFinCharges))]
		public virtual decimal? TranPtdFinChargesSum { get; set; }
		#endregion
		#region TranPtdDeposits
		public abstract class tranPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdDeposits))]
		public virtual decimal? TranPtdDepositsSum { get; set; }
		#endregion
		#region TranPtdRetainageWithheld
		public abstract class tranPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRetainageWithheld))]
		public virtual decimal? TranPtdRetainageWithheldSum { get; set; }
		#endregion
		#region TranPtdRetainageReleased
		public abstract class tranPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(ARHistoryTran.ptdRetainageReleased))]
		public virtual decimal? TranPtdRetainageReleasedSum { get; set; }
		#endregion
	}
}
