using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.SO;
using System;

namespace PX.Objects.AR
{
	[PXProjection(typeof(Select2<
		ARBalances,
		InnerJoin<SOOrder,
			On<SOOrder.branchID, Equal<ARBalances.branchID>,
				And<SOOrder.customerID, Equal<ARBalances.customerID>,
				And<SOOrder.customerLocationID, Equal<ARBalances.customerLocationID>,
				And<SOOrder.inclCustOpenOrders, Equal<True>,
				And<SOOrder.cancelled, Equal<False>,
				And<SOOrder.hold, Equal<False>,
				And<SOOrder.creditHold, Equal<False>,
				And<SOOrder.unbilledOrderTotal, NotEqual<Zero>>>>>>>>>,
			InnerJoin<SOOrderType,
				On<SOOrderType.orderType, Equal<SOOrder.orderType>>>>>), Persistent = false)]
	[PXHidden]
	public class ARCurrentBalanceOpenOrders : IBqlTable
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARCurrentBalanceOpenOrders>.By<branchID, customerID, customerLocationID>
		{
			public static ARCurrentBalanceOpenOrders Find(PXGraph graph, int branchID, int? customerID, int? customerLocationID) =>
				FindBy(graph, branchID, customerID, customerLocationID);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		[PXDBInt(BqlTable = typeof(ARBalances))]
		public virtual int? BranchID { get; set; }
		#endregion

		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[PXDBInt(BqlTable = typeof(ARBalances))]
		public virtual int? CustomerID { get; set; }
		#endregion

		#region CustomerLocationID
		public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }

		[PXDBInt(BqlTable = typeof(ARBalances))]
		public virtual int? CustomerLocationID { get; set; }
		#endregion


		#region BalanceSign
		public abstract class balanceSign : PX.Data.BQL.BqlDecimal.Field<balanceSign> { }

		[PXDecimal]
		[PXDBCalced(typeof(
				Switch<
					Case<Where<SOOrderType.aRDocType.IsIn<ARDocType.refund, ARDocType.voidRefund, ARDocType.invoice, ARDocType.debitMemo, ARDocType.finCharge, ARDocType.smallCreditWO>>,
					decimal1,
					Case<Where<SOOrderType.aRDocType.IsIn<ARDocType.creditMemo, ARDocType.payment, ARDocType.prepayment, ARDocType.voidPayment, ARDocType.smallBalanceWO>>,
					decimal_1>>,
					decimal0>), typeof(decimal))]
		public virtual decimal? BalanceSign { get; set; }
		#endregion

		#region UnbilledOrderTotal
		public abstract class unbilledOrderTotal : PX.Data.BQL.BqlDecimal.Field<unbilledOrderTotal> { }

		[PXDecimal]
		[PXDBCalced(typeof(SOOrder.unbilledOrderTotal.Multiply<balanceSign>), typeof(decimal))]
		public virtual decimal? UnbilledOrderTotal { get; set; }
		#endregion
	}

	[PXProjection(typeof(Select4<ARCurrentBalanceOpenOrders,
		Aggregate<
			GroupBy<ARCurrentBalanceOpenOrders.branchID,
			GroupBy<ARCurrentBalanceOpenOrders.customerID,
			GroupBy<ARCurrentBalanceOpenOrders.customerLocationID,

			Sum<ARCurrentBalanceOpenOrders.unbilledOrderTotal>>>>>>), Persistent = false)]
	[PXHidden]
	public class ARCurrentBalanceOpenOrdersSum : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<ARCurrentBalanceOpenOrdersSum>.By<branchID, customerID, customerLocationID>
		{
			public static ARCurrentBalanceOpenOrdersSum Find(PXGraph graph, int branchID, int? customerID, int? customerLocationID) =>
				FindBy(graph, branchID, customerID, customerLocationID);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		[PXDBInt(BqlTable = typeof(ARCurrentBalanceOpenOrders))]
		public virtual int? BranchID { get; set; }
		#endregion

		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[PXDBInt(BqlTable = typeof(ARCurrentBalanceOpenOrders))]
		public virtual int? CustomerID { get; set; }
		#endregion

		#region CustomerLocationID
		public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }

		[PXDBInt(BqlTable = typeof(ARCurrentBalanceOpenOrders))]
		public virtual int? CustomerLocationID { get; set; }
		#endregion


		#region UnbilledOrderTotal
		public abstract class unbilledOrderTotal : PX.Data.BQL.BqlDecimal.Field<unbilledOrderTotal> { }

		[PXDBDecimal(BqlField = typeof(ARCurrentBalanceOpenOrders.unbilledOrderTotal))]
		public virtual Decimal? UnbilledOrderTotal { get; set; }
		#endregion
	}
}
