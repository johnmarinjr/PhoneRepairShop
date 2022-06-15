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
		LeftJoin<CuryARHistoryYtd,
			On<CuryARHistoryYtd.customerID, Equal<CuryARHistory.customerID>,
				And<CuryARHistoryYtd.branchID, Equal<CuryARHistory.branchID>,
				And<CuryARHistoryYtd.accountID, Equal<CuryARHistory.accountID>,
				And<CuryARHistoryYtd.subID, Equal<CuryARHistory.subID>,
				And<CuryARHistoryYtd.curyID, Equal<CuryARHistory.curyID>,
				And<CuryARHistoryYtd.finPeriodID, LessEqual<CuryARHistory.finPeriodID>>>>>>>>,
		Aggregate<
			GroupBy<CuryARHistory.branchID,
			GroupBy<CuryARHistory.accountID,
			GroupBy<CuryARHistory.subID,
			GroupBy<CuryARHistory.customerID,
			GroupBy<CuryARHistory.curyID,
			GroupBy<CuryARHistory.finPeriodID,

			GroupBy<CuryARHistory.finBegBalance,
			GroupBy<CuryARHistory.finPtdSales,
			GroupBy<CuryARHistory.finPtdDrAdjustments,
			GroupBy<CuryARHistory.finPtdFinCharges,
			GroupBy<CuryARHistory.finPtdPayments,
			GroupBy<CuryARHistory.finPtdCrAdjustments,
			GroupBy<CuryARHistory.finPtdDiscounts,
			GroupBy<CuryARHistory.finPtdRGOL,
			GroupBy<CuryARHistory.finYtdBalance,
			GroupBy<CuryARHistory.finYtdDeposits,
			GroupBy<CuryARHistory.finYtdRetainageReleased,
			GroupBy<CuryARHistory.finYtdRetainageWithheld,

			GroupBy<CuryARHistory.curyFinBegBalance,
			GroupBy<CuryARHistory.curyFinPtdSales,
			GroupBy<CuryARHistory.curyFinPtdDrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdFinCharges,
			GroupBy<CuryARHistory.curyFinPtdPayments,
			GroupBy<CuryARHistory.curyFinPtdCrAdjustments,
			GroupBy<CuryARHistory.curyFinPtdDiscounts,
			GroupBy<CuryARHistory.curyFinYtdBalance,
			GroupBy<CuryARHistory.curyFinYtdDeposits,
			GroupBy<CuryARHistory.curyFinYtdRetainageReleased,
			GroupBy<CuryARHistory.curyFinYtdRetainageWithheld,
				
			GroupBy<CuryARHistory.tranBegBalance,
			GroupBy<CuryARHistory.tranPtdSales,
			GroupBy<CuryARHistory.tranPtdDrAdjustments,
			GroupBy<CuryARHistory.tranPtdFinCharges,
			GroupBy<CuryARHistory.tranPtdPayments,
			GroupBy<CuryARHistory.tranPtdCrAdjustments,
			GroupBy<CuryARHistory.tranPtdDiscounts,
			GroupBy<CuryARHistory.tranPtdRGOL,
			GroupBy<CuryARHistory.tranYtdBalance,
			GroupBy<CuryARHistory.tranYtdDeposits,
			GroupBy<CuryARHistory.tranYtdRetainageReleased,
			GroupBy<CuryARHistory.tranYtdRetainageWithheld,
			
				
			GroupBy<CuryARHistory.curyTranBegBalance,
			GroupBy<CuryARHistory.curyTranPtdSales,
			GroupBy<CuryARHistory.curyTranPtdDrAdjustments,
			GroupBy<CuryARHistory.curyTranPtdFinCharges,
			GroupBy<CuryARHistory.curyTranPtdPayments,
			GroupBy<CuryARHistory.curyTranPtdCrAdjustments,
			GroupBy<CuryARHistory.curyTranPtdDiscounts,
			GroupBy<CuryARHistory.curyTranYtdBalance,
			GroupBy<CuryARHistory.curyTranYtdDeposits,
			GroupBy<CuryARHistory.curyTranYtdRetainageReleased,
			GroupBy<CuryARHistory.curyTranYtdRetainageWithheld,	

			Sum<CuryARHistoryYtd.finPtdSales,
			Sum<CuryARHistoryYtd.finPtdDrAdjustments,
			Sum<CuryARHistoryYtd.finPtdFinCharges,
			Sum<CuryARHistoryYtd.finPtdPayments,
			Sum<CuryARHistoryYtd.finPtdCrAdjustments,
			Sum<CuryARHistoryYtd.finPtdDiscounts,
			Sum<CuryARHistoryYtd.finPtdDeposits,
			Sum<CuryARHistoryYtd.finPtdRetainageReleased,
			Sum<CuryARHistoryYtd.finPtdRetainageWithheld,
			Sum<CuryARHistoryYtd.finPtdRGOL,
				
			Sum<CuryARHistoryYtd.curyFinPtdSales,
			Sum<CuryARHistoryYtd.curyFinPtdDrAdjustments,
			Sum<CuryARHistoryYtd.curyFinPtdFinCharges,
			Sum<CuryARHistoryYtd.curyFinPtdPayments,
			Sum<CuryARHistoryYtd.curyFinPtdCrAdjustments,
			Sum<CuryARHistoryYtd.curyFinPtdDiscounts,
			Sum<CuryARHistoryYtd.curyFinPtdDeposits,
			Sum<CuryARHistoryYtd.curyFinPtdRetainageReleased,
			Sum<CuryARHistoryYtd.curyFinPtdRetainageWithheld,
			

			Sum<CuryARHistoryYtd.tranPtdSales,
			Sum<CuryARHistoryYtd.tranPtdDrAdjustments,
			Sum<CuryARHistoryYtd.tranPtdFinCharges,
			Sum<CuryARHistoryYtd.tranPtdPayments,
			Sum<CuryARHistoryYtd.tranPtdCrAdjustments,
			Sum<CuryARHistoryYtd.tranPtdDiscounts,
			Sum<CuryARHistoryYtd.tranPtdDeposits,
			Sum<CuryARHistoryYtd.tranPtdRetainageReleased,
			Sum<CuryARHistoryYtd.tranPtdRetainageWithheld,
			Sum<CuryARHistoryYtd.tranPtdRGOL,
			
			Sum<CuryARHistoryYtd.curyTranPtdSales,
			Sum<CuryARHistoryYtd.curyTranPtdDrAdjustments,
			Sum<CuryARHistoryYtd.curyTranPtdFinCharges,
			Sum<CuryARHistoryYtd.curyTranPtdPayments,
			Sum<CuryARHistoryYtd.curyTranPtdCrAdjustments,
			Sum<CuryARHistoryYtd.curyTranPtdDiscounts,
			Sum<CuryARHistoryYtd.curyTranPtdDeposits,
			Sum<CuryARHistoryYtd.curyTranPtdRetainageReleased,
			Sum<CuryARHistoryYtd.curyTranPtdRetainageWithheld	
			>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		), Persistent = false)]
	[PXHidden]
	public class CuryARHistoryYtdGrouped : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CuryARHistoryYtdGrouped>.By<branchID, accountID, subID, customerID, curyID, finPeriodID>
		{
			public static CuryARHistoryYtdGrouped Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String curyID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, customerID, curyID, finPeriodID);
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
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[Customer(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual int? CustomerID { get; set; }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(IsKey = true, BqlTable = typeof(CuryARHistory))]
		public virtual string CuryID { get; set; }
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
		#region FinPtdDrAdjustments
		public abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdDrAdjustments { get; set; }
		#endregion
		#region FinPtdFinCharges
		public abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdFinCharges { get; set; }
		#endregion
		#region FinPtdPayments
		public abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? FinPtdPayments { get; set; }
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

		#region CuryFinPtdSales
		public abstract class curyFinPtdSales : PX.Data.BQL.BqlDecimal.Field<curyFinPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdSales { get; set; }
		#endregion
		#region CuryFinPtdDrAdjustments
		public abstract class curyFinPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdDrAdjustments { get; set; }
		#endregion
		#region CuryFinPtdFinCharges
		public abstract class curyFinPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyFinPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdFinCharges { get; set; }
		#endregion
		#region CuryFinPtdPayments
		public abstract class curyFinPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryFinPtdPayments { get; set; }
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

		
		#region TranPtdSales
		public abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdSales { get; set; }
		#endregion
		#region TranPtdDrAdjustments
		public abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdDrAdjustments { get; set; }
		#endregion
		#region TranPtdFinCharges
		public abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdFinCharges { get; set; }
		#endregion
		#region TranPtdPayments
		public abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? TranPtdPayments { get; set; }
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
		
		#region CuryTranPtdSales
		public abstract class curyTranPtdSales : PX.Data.BQL.BqlDecimal.Field<curyTranPtdSales> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdSales { get; set; }
		#endregion
		#region CuryTranPtdDrAdjustments
		public abstract class curyTranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdDrAdjustments { get; set; }
		#endregion
		#region CuryTranPtdFinCharges
		public abstract class curyTranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyTranPtdFinCharges> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdFinCharges { get; set; }
		#endregion
		#region CuryTranPtdPayments
		public abstract class curyTranPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPayments> { }
		[PXDBDecimal(4, BqlTable = typeof(CuryARHistory))]
		public virtual decimal? CuryTranPtdPayments { get; set; }
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


		#region FinPtdSalesSum
		public abstract class finPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<finPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdSales))]
		public virtual decimal? FinPtdSalesSum { get; set; }
		#endregion
		#region FinPtdDrAdjustmentsSum
		public abstract class finPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdDrAdjustments))]
		public virtual decimal? FinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdFinChargesSum
		public abstract class finPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<finPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdFinCharges))]
		public virtual decimal? FinPtdFinChargesSum { get; set; }
		#endregion
		#region FinPtdPaymentsSum
		public abstract class finPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdPayments))]
		public virtual decimal? FinPtdPaymentsSum { get; set; }
		#endregion
		#region FinPtdCrAdjustments
		public abstract class finPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdCrAdjustments))]
		public virtual decimal? FinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region FinPtdDiscounts
		public abstract class finPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<finPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdDiscounts))]
		public virtual decimal? FinPtdDiscountsSum { get; set; }
		#endregion
		#region FinYtdDepositsSum
		public abstract class finYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<finYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdDeposits))]
		public virtual decimal? FinYtdDepositsSum { get; set; }
		#endregion
		#region FinYtdRetainageReleasedSum
		public abstract class finYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdRetainageReleased))]
		public virtual decimal? FinYtdRetainageReleasedSum { get; set; }
		#endregion
		#region FinYtdRetainageWithheldSum
		public abstract class finYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdRetainageWithheld))]
		public virtual decimal? FinYtdRetainageWithheldSum { get; set; }
		#endregion
		#region FinPtdRGOLSum
		public abstract class finPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<finPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.finPtdRGOL))]
		public virtual decimal? FinPtdRGOLSum { get; set; }
		#endregion

		#region CuryFinPtdSalesSum
		public abstract class curyFinPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdSales))]
		public virtual decimal? CuryFinPtdSalesSum { get; set; }
		#endregion
		#region CuryFinPtdDrAdjustmentsSum
		public abstract class curyFinPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdDrAdjustments))]
		public virtual decimal? CuryFinPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region CuryFinPtdFinChargesSum
		public abstract class curyFinPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdFinCharges))]
		public virtual decimal? CuryFinPtdFinChargesSum { get; set; }
		#endregion
		#region CuryFinPtdPaymentsSum
		public abstract class curyFinPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdPayments))]
		public virtual decimal? CuryFinPtdPaymentsSum { get; set; }
		#endregion
		#region CuryFinPtdCrAdjustments
		public abstract class curyFinPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdCrAdjustments))]
		public virtual decimal? CuryFinPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region CuryFinPtdDiscounts
		public abstract class curyFinPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdDiscounts))]
		public virtual decimal? CuryFinPtdDiscountsSum { get; set; }
		#endregion
		#region CuryFinYtdDepositsSum
		public abstract class curyFinYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<curyFinYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdDeposits))]
		public virtual decimal? CuryFinYtdDepositsSum { get; set; }
		#endregion
		#region CuryFinYtdRetainageReleasedSum
		public abstract class curyFinYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<curyFinYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdRetainageReleased))]
		public virtual decimal? CuryFinYtdRetainageReleasedSum { get; set; }
		#endregion
		#region CuryFinYtdRetainageWithheldSum
		public abstract class curyFinYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<curyFinYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyFinPtdRetainageWithheld))]
		public virtual decimal? CuryFinYtdRetainageWithheldSum { get; set; }
		#endregion

		#region TranPtdSalesSum
		public abstract class tranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdSales))]
		public virtual decimal? TranPtdSalesSum { get; set; }
		#endregion
		#region TranPtdDrAdjustmentsSum
		public abstract class tranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdDrAdjustments))]
		public virtual decimal? TranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdFinChargesSum
		public abstract class tranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<tranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdFinCharges))]
		public virtual decimal? TranPtdFinChargesSum { get; set; }
		#endregion
		#region TranPtdPaymentsSum
		public abstract class tranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdPayments))]
		public virtual decimal? TranPtdPaymentsSum { get; set; }
		#endregion
		#region TranPtdCrAdjustmentsSum
		public abstract class tranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdCrAdjustments))]
		public virtual decimal? TranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region TranPtdDiscountsSum
		public abstract class tranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdDiscounts))]
		public virtual decimal? TranPtdDiscountsSum { get; set; }
		#endregion
		#region TranYtdDepositsSum
		public abstract class tranYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<tranYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdDeposits))]
		public virtual decimal? TranYtdDepositsSum { get; set; }
		#endregion
		#region TranYtdRetainageReleasedSum
		public abstract class tranYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdRetainageReleased))]
		public virtual decimal? TranYtdRetainageReleasedSum { get; set; }
		#endregion
		#region TranYtdRetainageWithheldSum
		public abstract class tranYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdRetainageWithheld))]
		public virtual decimal? TranYtdRetainageWithheldSum { get; set; }
		#endregion
		#region TranPtdRGOLSum
		public abstract class tranPtdRGOLSum : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOLSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.tranPtdRGOL))]
		public virtual decimal? TranPtdRGOLSum { get; set; }
		#endregion
		
		#region CuryTranPtdSalesSum
		public abstract class curyTranPtdSalesSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdSalesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdSales))]
		public virtual decimal? CuryTranPtdSalesSum { get; set; }
		#endregion
		#region CuryTranPtdDrAdjustmentsSum
		public abstract class curyTranPtdDrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdDrAdjustments))]
		public virtual decimal? CuryTranPtdDrAdjustmentsSum { get; set; }
		#endregion
		#region CuryTranPtdFinChargesSum
		public abstract class curyTranPtdFinChargesSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdFinChargesSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdFinCharges))]
		public virtual decimal? CuryTranPtdFinChargesSum { get; set; }
		#endregion
		#region CuryTranPtdPaymentsSum
		public abstract class curyTranPtdPaymentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPaymentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdPayments))]
		public virtual decimal? CuryTranPtdPaymentsSum { get; set; }
		#endregion
		#region CuryTranPtdCrAdjustments
		public abstract class curyTranPtdCrAdjustmentsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdCrAdjustmentsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdCrAdjustments))]
		public virtual decimal? CuryTranPtdCrAdjustmentsSum { get; set; }
		#endregion
		#region CuryTranPtdDiscounts
		public abstract class curyTranPtdDiscountsSum : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDiscountsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdDiscounts))]
		public virtual decimal? CuryTranPtdDiscountsSum { get; set; }
		#endregion
		#region CuryTranYtdDepositsSum
		public abstract class curyTranYtdDepositsSum : PX.Data.BQL.BqlDecimal.Field<curyTranYtdDepositsSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdDeposits))]
		public virtual decimal? CuryTranYtdDepositsSum { get; set; }
		#endregion
		#region CuryTranYtdRetainageReleasedSum
		public abstract class curyTranYtdRetainageReleasedSum : PX.Data.BQL.BqlDecimal.Field<curyTranYtdRetainageReleasedSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdRetainageReleased))]
		public virtual decimal? CuryTranYtdRetainageReleasedSum { get; set; }
		#endregion
		#region CuryTranYtdRetainageWithheldSum
		public abstract class curyTranYtdRetainageWithheldSum : PX.Data.BQL.BqlDecimal.Field<curyTranYtdRetainageWithheldSum> { }
		[PXDBDecimal(4, BqlField = typeof(CuryARHistoryYtd.curyTranPtdRetainageWithheld))]
		public virtual decimal? CuryTranYtdRetainageWithheldSum { get; set; }
		#endregion

	}

	[PXHidden]
	public class CuryARHistoryYtd : CuryARHistory
	{
		#region Keys
		public new class PK : PrimaryKeyOf<CuryARHistoryYtd>.By<branchID, accountID, subID, customerID, finPeriodID>
		{
			public static CuryARHistoryYtd Find(PXGraph graph, Int32? branchID, Int32? accountID, Int32? subID, Int32? customerID, String finPeriodID) => FindBy(graph, branchID, accountID, subID, customerID, finPeriodID);
		}
		#endregion

		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		public new abstract class curyID : PX.Data.BQL.BqlInt.Field<curyID> { }
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		public new abstract class finPtdSales : PX.Data.BQL.BqlDecimal.Field<finPtdSales> { }
		public new abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		public new abstract class finPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<finPtdFinCharges> { }
		public new abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		public new abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		public new abstract class finPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<finPtdDiscounts> { }
		public new abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		public new abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		public new abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }
		public new abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }

		public new abstract class curyFinPtdSales : PX.Data.BQL.BqlDecimal.Field<curyFinPtdSales> { }
		public new abstract class curyFinPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustments> { }
		public new abstract class curyFinPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyFinPtdFinCharges> { }
		public new abstract class curyFinPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPayments> { }
		public new abstract class curyFinPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdCrAdjustments> { }
		public new abstract class curyFinPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDiscounts> { }
		public new abstract class curyFinPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDeposits> { }
		public new abstract class curyFinPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageReleased> { }
		public new abstract class curyFinPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageWithheld> { }
		public new abstract class curyFinPtdRGOL : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRGOL> { }
		public new abstract class tranPtdSales : PX.Data.BQL.BqlDecimal.Field<tranPtdSales> { }
		public new abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		public new abstract class tranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<tranPtdFinCharges> { }
		public new abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		public new abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		public new abstract class tranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscounts> { }
		public new abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		public new abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		public new abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
		public new abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		public new abstract class curyTranPtdSales : PX.Data.BQL.BqlDecimal.Field<curyTranPtdSales> { }
		public new abstract class curyTranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustments> { }
		public new abstract class curyTranPtdFinCharges : PX.Data.BQL.BqlDecimal.Field<curyTranPtdFinCharges> { }
		public new abstract class curyTranPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPayments> { }
		public new abstract class curyTranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdCrAdjustments> { }
		public new abstract class curyTranPtdDiscounts : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDiscounts> { }
		public new abstract class curyTranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDeposits> { }
		public new abstract class curyTranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageReleased> { }
		public new abstract class curyTranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageWithheld> { }
		public new abstract class curyTranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRGOL> { }
	}
}
