using PX.Data;

namespace PX.Commerce.Objects
{
	[PXHidden]
	[PXCacheName("BCDefaultShippingMapping")]
	public class BCDefaultPaymentMapping : IBqlTable
	{
		#region ConnectorType
		[PXDBString(IsKey = true)]
		public virtual string ConnectorType { get; set; }
		public abstract class connectorType : PX.Data.BQL.BqlString.Field<connectorType> { }
		#endregion
		#region StorePaymentMethod
		[PXDBString(IsKey = true)]
		public virtual string StorePaymentMethod { get; set; }
		public abstract class storePaymentMethod : PX.Data.BQL.BqlString.Field<storePaymentMethod> { }
		#endregion
		#region CreatePaymentfromOrder
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? CreatePaymentfromOrder { get; set; }
		public abstract class createPaymentfromOrder : PX.Data.BQL.BqlBool.Field<createPaymentfromOrder> { }
		#endregion
	}
}
