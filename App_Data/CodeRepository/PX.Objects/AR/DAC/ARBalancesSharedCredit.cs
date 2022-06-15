using PX.Data;
using PX.Objects.CM;
using System;

namespace PX.Objects.AR
{
	//Introduced in scope of AC-183277

	[Serializable]
	[PXCacheName(Messages.ARBalancesSharedCredit)]
	[PXProjection(typeof(Select5<ARBalances,
						InnerJoin<CustomerSharedCredit, On<CustomerSharedCredit.bAccountID, Equal<ARBalances.customerID>>>,
					Where<CustomerSharedCredit.sharedCreditCustomerID, Equal<CustomerSharedCredit.sharedCreditCustomerID>,
							Or<CustomerSharedCredit.bAccountID, Equal<ARBalances.customerID>>>,
					Aggregate<
					GroupBy<CustomerSharedCredit.sharedCreditCustomerID,
					GroupBy<CustomerSharedCredit.creditLimit,
					Sum<ARBalances.currentBal,
					Sum<ARBalances.totalOpenOrders,
					Sum<ARBalances.totalPrepayments,
					Sum<ARBalances.totalShipped,
					Sum<ARBalances.unreleasedBal>>>>>>>>>))]
	public class ARBalancesSharedCredit : IBqlTable
	{
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(ARBalances))]
		public virtual int? BranchID { get; set; }
		#endregion
		#region SharedCreditCustomerID
		public abstract class sharedCreditCustomerID : PX.Data.BQL.BqlInt.Field<sharedCreditCustomerID> { }
		[PXDBInt(BqlTable = typeof(CustomerSharedCredit))]
		public virtual int? SharedCreditCustomerID { get; set; }
		#endregion
		#region CreditRule
		public abstract class creditRule : PX.Data.BQL.BqlString.Field<creditRule> { }
		[PXDBString(1, IsFixed = true, BqlTable = typeof(CustomerSharedCredit))]
		[CreditRule()]
		[PXUIField(DisplayName = "Credit Verification")]
		public virtual string CreditRule { get; set; }
		#endregion
		#region CreditLimit
		public abstract class creditLimit : PX.Data.BQL.BqlDecimal.Field<creditLimit> { }
		[PXDBBaseCury(BqlTable = typeof(CustomerSharedCredit))]
		[PXUIField(DisplayName = "Credit Limit")]
		public virtual decimal? CreditLimit { get; set; }
		#endregion
		#region CurrentBal
		public abstract class currentBal : PX.Data.BQL.BqlDecimal.Field<currentBal> { }
		[PXDBDecimal(4, BqlTable = typeof(ARBalances))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CurrentBal { get; set; }
		#endregion
		#region UnreleasedBal
		public abstract class unreleasedBal : PX.Data.BQL.BqlDecimal.Field<unreleasedBal> { }
		[PXDBDecimal(4, BqlTable = typeof(ARBalances))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? UnreleasedBal { get; set; }
		#endregion
		#region TotalPrepayments
		public abstract class totalPrepayments : PX.Data.BQL.BqlDecimal.Field<totalPrepayments> { }
		[PXDBDecimal(4, BqlTable = typeof(ARBalances))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TotalPrepayments { get; set; }
		#endregion
		#region TotalOpenOrders
		public abstract class totalOpenOrders : PX.Data.BQL.BqlDecimal.Field<totalOpenOrders> { }
		[PXDBDecimal(4, BqlTable = typeof(ARBalances))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TotalOpenOrders { get; set; }
		#endregion
		#region TotalShipped
		public abstract class totalShipped : PX.Data.BQL.BqlDecimal.Field<totalShipped> { }
		[PXDBDecimal(4, BqlTable = typeof(ARBalances))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TotalShipped { get; set; }
		#endregion
	}
}
