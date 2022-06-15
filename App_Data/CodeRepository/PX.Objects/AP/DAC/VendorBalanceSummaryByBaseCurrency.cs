using System;
using PX.Data;
using PX.Objects.CM;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	[Serializable]
	[PXCacheName(Messages.VendorBalanceSummaryByBaseCurrency)]
	public partial class VendorBalanceSummaryByBaseCurrency : IBqlTable
	{
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt()]
		[PXDefault()]
		public virtual Int32? VendorID { get; set; }
		#endregion
		#region BaseCuryID
		public abstract class baseCuryID : PX.Data.BQL.BqlString.Field<baseCuryID> { }
		[PXDBString(5, IsKey = true, IsUnicode = true, BqlTable = typeof(GL.Branch))]
		[PXUIField(DisplayName = "Currency")]
		public virtual string BaseCuryID { get; set; }
		#endregion
		#region Balance
		public abstract class balance : PX.Data.BQL.BqlDecimal.Field<balance> { }
		[CurySymbol(curyID: typeof(Vendor.baseCuryID))]
		[PXBaseCury(curyID: typeof(Vendor.baseCuryID))]
		[PXUIField(DisplayName = "Balance", Visible = true, Enabled = false)]
		public virtual Decimal? Balance { get; set; }
		#endregion
		#region DepositsBalance
		public abstract class depositsBalance : PX.Data.BQL.BqlDecimal.Field<depositsBalance> { }
		[CurySymbol(curyID: typeof(Vendor.baseCuryID))]
		[PXBaseCury(curyID: typeof(Vendor.baseCuryID))]
		[PXUIField(DisplayName = "Prepayment Balance", Enabled = false)]
		public virtual Decimal? DepositsBalance { get; set; }
		#endregion
		#region RetainageBalance
		public abstract class retainageBalance : PX.Data.BQL.BqlDecimal.Field<retainageBalance> { }
		[CurySymbol(curyID: typeof(Vendor.baseCuryID))]
		[PXBaseCury(curyID: typeof(Vendor.baseCuryID))]
		[PXUIField(DisplayName = "Retained Balance", Visibility = PXUIVisibility.Visible, Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual decimal? RetainageBalance { get; set; }
		#endregion
	}
}
