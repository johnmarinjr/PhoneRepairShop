using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using System;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select5<
		CuryARHistory,
		LeftJoin<CuryARHistoryTran,
			On<CuryARHistoryTran.customerID, Equal<CuryARHistory.customerID>,
				And<CuryARHistoryTran.branchID, Equal<CuryARHistory.branchID>,
				And<CuryARHistoryTran.accountID, Equal<CuryARHistory.accountID>,
				And<CuryARHistoryTran.subID, Equal<CuryARHistory.subID>,
				And<CuryARHistoryTran.finPeriodID, Equal<CuryARHistory.finPeriodID>,
				And<CuryARHistoryTran.curyID, Equal<CuryARHistory.curyID>>>>>>>>,
		Aggregate<
			GroupBy<CuryARHistory.branchID,
			GroupBy<CuryARHistory.accountID,
			GroupBy<CuryARHistory.subID,
			GroupBy<CuryARHistory.curyID,
			GroupBy<CuryARHistory.customerID,
			GroupBy<CuryARHistory.finPeriodID,

			GroupBy<CuryARHistory.finPtdSales,
			GroupBy<CuryARHistory.finPtdPayments,
			GroupBy<CuryARHistory.finPtdDrAdjustments,
			GroupBy<CuryARHistory.finPtdCrAdjustments,
			GroupBy<CuryARHistory.finPtdDiscounts,
			GroupBy<CuryARHistory.finPtdRGOL,
			GroupBy<CuryARHistory.finPtdFinCharges,
			GroupBy<CuryARHistory.finPtdDeposits,
			GroupBy<CuryARHistory.finPtdRetainageWithheld,
			GroupBy<CuryARHistory.finPtdRetainageReleased,

			GroupBy<CuryARHistory.curyFinPtdSales,
			GroupBy<CuryARHistory.curyFinPtdPayments,
			GroupBy<CuryARHistory.curyFinPtdDrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdCrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdDiscounts,
			GroupBy<CuryARHistory.curyFinPtdFinCharges,
			GroupBy<CuryARHistory.curyFinPtdDeposits,
			GroupBy<CuryARHistory.curyFinPtdRetainageWithheld,
			GroupBy<CuryARHistory.curyFinPtdRetainageReleased,

			Sum<CuryARHistoryTran.ptdSales,
			Sum<CuryARHistoryTran.ptdPayments,
			Sum<CuryARHistoryTran.ptdDrAdjustments,
			Sum<CuryARHistoryTran.ptdCrAdjustments,
			Sum<CuryARHistoryTran.ptdDiscounts,
			Sum<CuryARHistoryTran.ptdRGOL,
			Sum<CuryARHistoryTran.ptdFinCharges,
			Sum<CuryARHistoryTran.ptdDeposits,
			Sum<CuryARHistoryTran.ptdRetainageWithheld,
			Sum<CuryARHistoryTran.ptdRetainageReleased,

			Sum<CuryARHistoryTran.curyPtdSales,
			Sum<CuryARHistoryTran.curyPtdPayments,
			Sum<CuryARHistoryTran.curyPtdDrAdjustments,
			Sum<CuryARHistoryTran.curyPtdCrAdjustments,
			Sum<CuryARHistoryTran.curyPtdDiscounts,
			Sum<CuryARHistoryTran.curyPtdFinCharges,
			Sum<CuryARHistoryTran.curyPtdDeposits,
			Sum<CuryARHistoryTran.curyPtdRetainageWithheld,
			Sum<CuryARHistoryTran.curyPtdRetainageReleased
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class CuryARHistoryFinGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<CuryARHistoryFinGrouped>.By<branchID, accountID, subID, curyID, customerID, finPeriodID>
		{
			public static CuryARHistoryFinGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, string curyID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, curyID, customerID, finPeriodID);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? BranchID { get; set; }
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? AccountID { get; set; }
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? SubID { get; set; }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual string CuryID { get; set; }
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[Customer(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? CustomerID { get; set; }
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodID(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual string FinPeriodID { get; set; }
		#endregion

		#region FinPtdSales
		public abstract class finPtdSales : PX.Data.BQL.BqlDecimal.Field<finPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdSales { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdPayments { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdDrAdjustments { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdCrAdjustments { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<finPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdDiscounts { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdRGOL { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdFinCharges { get; set; }
		#endregion
		#region FinPtdDeposits
		public abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdDeposits { get; set; }
		#endregion
		#region FinPtdRetainageWithheld
		public abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdRetainageWithheld { get; set; }
		#endregion
		#region FinPtdRetainageReleased
		public abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdRetainageReleased { get; set; }
		#endregion

		#region CuryFinPtdSales
		public abstract class curyFinPtdSales : PX.Data.BQL.BqlDecimal.Field<curyFinPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdSales { get; set; }
		#endregion
		#region CuryFinPtdPayments
		public abstract class curyFinPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdPayments { get; set; }
		#endregion
		#region CuryFinPtdDrAdjustments
		public abstract class curyFinPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdDrAdjustments { get; set; }
		#endregion
		#region CuryFinPtdCrAdjustments
		public abstract class curyFinPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdCrAdjustments { get; set; }
		#endregion
		#region CuryFinPtdDiscounts
		public abstract class curyFinPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdDiscounts { get; set; }
		#endregion
		#region CuryFinPtdFinCharges
		public abstract class curyFinPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyFinPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdFinCharges { get; set; }
		#endregion
		#region CuryFinPtdDeposits
		public abstract class curyFinPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdDeposits { get; set; }
		#endregion
		#region CuryFinPtdRetainageWithheld
		public abstract class curyFinPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdRetainageWithheld { get; set; }
		#endregion
		#region CuryFinPtdRetainageReleased
		public abstract class curyFinPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdRetainageReleased { get; set; }
		#endregion

		#region FinPtdSales
		public abstract class finPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<finPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdSales))]
		public virtual decimal? FinPtdSalesSum { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdPayments))]
		public virtual decimal? FinPtdPaymentsSum { get; set; }
		#endregion
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDrAdjustments))]
		public virtual decimal? FinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdCrAdjustments))]
		public virtual decimal? FinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDiscounts))]
		public virtual decimal? FinPtdDiscountsSum { get; set; }
		#endregion
		#region FinPtdRGOL
		public abstract class finPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<finPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRGOL))]
		public virtual decimal? FinPtdRGOLSum { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<finPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdFinCharges))]
		public virtual decimal? FinPtdFinChargesSum { get; set; }
		#endregion
		#region FinPtdDeposits
		public abstract class finPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDeposits))]
		public virtual decimal? FinPtdDepositsSum { get; set; }
		#endregion
		#region FinPtdRetainageWithheld
		public abstract class finPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRetainageWithheld))]
		public virtual decimal? FinPtdRetainageWithheldSum { get; set; }
		#endregion
		#region FinPtdRetainageReleased
		public abstract class finPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRetainageReleased))]
		public virtual decimal? FinPtdRetainageReleasedSum { get; set; }
		#endregion

		#region CuryFinPtdSales
		public abstract class curyFinPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdSales))]
		public virtual decimal? CuryFinPtdSalesSum { get; set; }
		#endregion
		#region CuryFinPtdPayments
		public abstract class curyFinPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdPayments))]
		public virtual decimal? CuryFinPtdPaymentsSum { get; set; }
		#endregion
		#region CuryFinPtdDrAdjustments
		public abstract class curyFinPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDrAdjustments))]
		public virtual decimal? CuryFinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region CuryFinPtdCrAdjustments
		public abstract class curyFinPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdCrAdjustments))]
		public virtual decimal? CuryFinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region CuryFinPtdDiscounts
		public abstract class curyFinPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDiscounts))]
		public virtual decimal? CuryFinPtdDiscountsSum { get; set; }
		#endregion
		#region CuryFinPtdFinCharges
		public abstract class curyFinPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdFinCharges))]
		public virtual decimal? CuryFinPtdFinChargesSum { get; set; }
		#endregion
		#region CuryFinPtdDeposits
		public abstract class curyFinPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDeposits))]
		public virtual decimal? CuryFinPtdDepositsSum { get; set; }
		#endregion
		#region CuryFinPtdRetainageWithheld
		public abstract class curyFinPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdRetainageWithheld))]
		public virtual decimal? CuryFinPtdRetainageWithheldSum { get; set; }
		#endregion
		#region CuryFinPtdRetainageReleased
		public abstract class curyFinPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdRetainageReleased))]
		public virtual decimal? CuryFinPtdRetainageReleasedSum { get; set; }
		#endregion
	}
	
	[PXProjection(typeof(Select5<
		CuryARHistory,
		LeftJoin<CuryARHistoryTran,
			On<CuryARHistoryTran.customerID, Equal<CuryARHistory.customerID>,
				And<CuryARHistoryTran.branchID, Equal<CuryARHistory.branchID>,
				And<CuryARHistoryTran.accountID, Equal<CuryARHistory.accountID>,
				And<CuryARHistoryTran.subID, Equal<CuryARHistory.subID>,
				And<CuryARHistoryTran.tranPeriodID, Equal<CuryARHistory.finPeriodID>,
				And<CuryARHistoryTran.curyID, Equal<CuryARHistory.curyID>>>>>>>>,
		Aggregate<
			GroupBy<CuryARHistory.branchID,
			GroupBy<CuryARHistory.accountID,
			GroupBy<CuryARHistory.subID,
			GroupBy<CuryARHistory.curyID,
			GroupBy<CuryARHistory.customerID,
			GroupBy<CuryARHistory.finPeriodID,

			GroupBy<CuryARHistory.finPtdSales,
			GroupBy<CuryARHistory.finPtdPayments,
			GroupBy<CuryARHistory.finPtdDrAdjustments,
			GroupBy<CuryARHistory.finPtdCrAdjustments,
			GroupBy<CuryARHistory.finPtdDiscounts,
			GroupBy<CuryARHistory.finPtdRGOL,
			GroupBy<CuryARHistory.finPtdFinCharges,
			GroupBy<CuryARHistory.finPtdDeposits,
			GroupBy<CuryARHistory.finPtdRetainageWithheld,
			GroupBy<CuryARHistory.finPtdRetainageReleased,

			GroupBy<CuryARHistory.curyFinPtdSales,
			GroupBy<CuryARHistory.curyFinPtdPayments,
			GroupBy<CuryARHistory.curyFinPtdDrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdCrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdDiscounts,
			GroupBy<CuryARHistory.curyFinPtdFinCharges,
			GroupBy<CuryARHistory.curyFinPtdDeposits,
			GroupBy<CuryARHistory.curyFinPtdRetainageWithheld,
			GroupBy<CuryARHistory.curyFinPtdRetainageReleased,

			Sum<CuryARHistoryTran.ptdSales,
			Sum<CuryARHistoryTran.ptdPayments,
			Sum<CuryARHistoryTran.ptdDrAdjustments,
			Sum<CuryARHistoryTran.ptdCrAdjustments,
			Sum<CuryARHistoryTran.ptdDiscounts,
			Sum<CuryARHistoryTran.ptdRGOL,
			Sum<CuryARHistoryTran.ptdFinCharges,
			Sum<CuryARHistoryTran.ptdDeposits,
			Sum<CuryARHistoryTran.ptdRetainageWithheld,
			Sum<CuryARHistoryTran.ptdRetainageReleased,

			Sum<CuryARHistoryTran.curyPtdSales,
			Sum<CuryARHistoryTran.curyPtdPayments,
			Sum<CuryARHistoryTran.curyPtdDrAdjustments,
			Sum<CuryARHistoryTran.curyPtdCrAdjustments,
			Sum<CuryARHistoryTran.curyPtdDiscounts,
			Sum<CuryARHistoryTran.curyPtdFinCharges,
			Sum<CuryARHistoryTran.curyPtdDeposits,
			Sum<CuryARHistoryTran.curyPtdRetainageWithheld,
			Sum<CuryARHistoryTran.curyPtdRetainageReleased
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), Persistent = false)]
	[PXHidden]
	public class CuryARHistoryTranGrouped : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<CuryARHistoryTranGrouped>.By<branchID, accountID, subID, curyID, customerID, finPeriodID>
		{
			public static CuryARHistoryTranGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, string curyID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, curyID, customerID, finPeriodID);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? BranchID { get; set; }
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? AccountID { get; set; }
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? SubID { get; set; }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual string CuryID { get; set; }
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[Customer(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? CustomerID { get; set; }
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodID(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual string FinPeriodID { get; set; }
		#endregion

		#region TranPtdSales
		public abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdSales { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdPayments { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdDrAdjustments { get; set; }
		#endregion
		#region TranPtdCrAdjustments
		public abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdCrAdjustments { get; set; }
		#endregion
		#region TranPtdDiscounts
		public abstract class tranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdDiscounts { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdRGOL { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdFinCharges { get; set; }
		#endregion
		#region TranPtdDeposits
		public abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdDeposits { get; set; }
		#endregion
		#region TranPtdRetainageWithheld
		public abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdRetainageWithheld { get; set; }
		#endregion
		#region TranPtdRetainageReleased
		public abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdRetainageReleased { get; set; }
		#endregion

		#region CuryTranPtdSales
		public abstract class curyTranPtdSales : PX.Data.BQL.BqlDecimal.Field<curyTranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdSales { get; set; }
		#endregion
		#region CuryTranPtdPayments
		public abstract class curyTranPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdPayments { get; set; }
		#endregion
		#region CuryTranPtdDrAdjustments
		public abstract class curyTranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdDrAdjustments { get; set; }
		#endregion
		#region CuryTranPtdCrAdjustments
		public abstract class curyTranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdCrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdCrAdjustments { get; set; }
		#endregion
		#region CuryTranPtdDiscounts
		public abstract class curyTranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDiscounts> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdDiscounts { get; set; }
		#endregion
		#region CuryTranPtdFinCharges
		public abstract class curyTranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyTranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdFinCharges { get; set; }
		#endregion
		#region CuryTranPtdDeposits
		public abstract class curyTranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDeposits> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdDeposits { get; set; }
		#endregion
		#region CuryTranPtdRetainageWithheld
		public abstract class curyTranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageWithheld> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdRetainageWithheld { get; set; }
		#endregion
		#region CuryTranPtdRetainageReleased
		public abstract class curyTranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageReleased> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdRetainageReleased { get; set; }
		#endregion

		#region TranPtdSales
		public abstract class tranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdSales))]
		public virtual decimal? TranPtdSalesSum { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdPayments))]
		public virtual decimal? TranPtdPaymentsSum { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDrAdjustments))]
		public virtual decimal? TranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdCrAdjustments
		public abstract class tranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdCrAdjustments))]
		public virtual decimal? TranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdDiscounts
		public abstract class tranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDiscounts))]
		public virtual decimal? TranPtdDiscountsSum { get; set; }
		#endregion
		#region TranPtdRGOL
		public abstract class tranPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRGOL))]
		public virtual decimal? TranPtdRGOLSum { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdFinCharges))]
		public virtual decimal? TranPtdFinChargesSum { get; set; }
		#endregion
		#region TranPtdDeposits
		public abstract class tranPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdDeposits))]
		public virtual decimal? TranPtdDepositsSum { get; set; }
		#endregion
		#region TranPtdRetainageWithheld
		public abstract class tranPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRetainageWithheld))]
		public virtual decimal? TranPtdRetainageWithheldSum { get; set; }
		#endregion
		#region TranPtdRetainageReleased
		public abstract class tranPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.ptdRetainageReleased))]
		public virtual decimal? TranPtdRetainageReleasedSum { get; set; }
		#endregion

		#region CuryTranPtdSales
		public abstract class curyTranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdSales))]
		public virtual decimal? CuryTranPtdSalesSum { get; set; }
		#endregion
		#region CuryTranPtdPayments
		public abstract class curyTranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdPayments))]
		public virtual decimal? CuryTranPtdPaymentsSum { get; set; }
		#endregion
		#region CuryTranPtdDrAdjustments
		public abstract class curyTranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDrAdjustments))]
		public virtual decimal? CuryTranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region CuryTranPtdCrAdjustments
		public abstract class curyTranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdCrAdjustments))]
		public virtual decimal? CuryTranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region CuryTranPtdDiscounts
		public abstract class curyTranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDiscounts))]
		public virtual decimal? CuryTranPtdDiscountsSum { get; set; }
		#endregion
		#region CuryTranPtdFinCharges
		public abstract class curyTranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdFinCharges))]
		public virtual decimal? CuryTranPtdFinChargesSum { get; set; }
		#endregion
		#region CuryTranPtdDeposits
		public abstract class curyTranPtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdDeposits))]
		public virtual decimal? CuryTranPtdDepositsSum { get; set; }
		#endregion
		#region CuryTranPtdRetainageWithheld
		public abstract class curyTranPtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdRetainageWithheld))]
		public virtual decimal? CuryTranPtdRetainageWithheldSum { get; set; }
		#endregion
		#region CuryTranPtdRetainageReleased
		public abstract class curyTranPtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryTran.curyPtdRetainageReleased))]
		public virtual decimal? CuryTranPtdRetainageReleasedSum { get; set; }
		#endregion
	}
}
