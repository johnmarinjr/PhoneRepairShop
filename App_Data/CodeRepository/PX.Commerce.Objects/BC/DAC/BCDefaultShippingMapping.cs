using PX.Data;

namespace PX.Commerce.Objects
{
	[PXHidden]
	[PXCacheName("BCDefaultShippingMapping")]
	public class BCDefaultShippingMapping : IBqlTable
	{
		#region ConnectorType
		[PXDBString(IsKey = true)]
		public virtual string ConnectorType { get; set; }
		public abstract class connectorType : PX.Data.BQL.BqlString.Field<connectorType> { }
		#endregion
		#region BC Shipping zone
		[PXDBString(IsKey = true)]
		public virtual string ShippingZone { get; set; }
		public abstract class shippingZone : PX.Data.BQL.BqlString.Field<shippingZone> { }
		#endregion
		#region BC Shipping method
		[PXDBString(IsKey = true)]
		public virtual string ShippingMethod { get; set; }
		public abstract class shippingMethod : PX.Data.BQL.BqlString.Field<shippingMethod> { }
		#endregion
	}
}
