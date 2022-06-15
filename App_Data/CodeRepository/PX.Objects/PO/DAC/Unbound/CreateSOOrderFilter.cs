using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using CustomerOrderNbrLightAttribute = PX.Objects.SO.Attributes.CustomerOrderNbrLightAttribute;

namespace PX.Objects.PO.DAC.Unbound
{
	[PXHidden]
	public class CreateSOOrderFilter : IBqlTable
	{
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		[PXDefault]
		[PXDBString(2, IsFixed = true)]
		[PXSelector(typeof(Search<SOOrderType.orderType,
			Where<SOOrderType.active, Equal<True>,
				And<SOOrderType.behavior, In3<SOBehavior.sO, SOBehavior.tR>,
				And<SOOrderType.aRDocType, Equal<ARDocType.invoice>>>>>))]
		[PXUIField(DisplayName = "Sales Order Type", Required = true)]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion

		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }

		[PXDefault]
		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
		public virtual String OrderNbr
		{
			get;
			set;
		}
		#endregion

		#region FixedCustomer
		public abstract class fixedCustomer : PX.Data.BQL.BqlBool.Field<fixedCustomer> { }
		[PXBool]
		public virtual bool? FixedCustomer
		{
			get;
			set;
		}
		#endregion

		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[PXDefault]
		[CustomerActive(typeof(Search<BAccountR.bAccountID, Where<BAccountR.type, Equal<BAccountType.customerType>>>),
			Visibility = PXUIVisibility.SelectorVisible,
			DescriptionField = typeof(Customer.acctName),
			Filterable = true,
			Required = true)]
		public virtual int? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region CustomerLocationID
		public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }

		[LocationID(typeof(Where<Location.bAccountID, Equal<Current<customerID>>>), DescriptionField = typeof(Location.descr))]
		[PXDefault((object)null, typeof(Search<BAccount2.defLocationID, Where<BAccount2.bAccountID, Equal<Optional<customerID>>>>))]
		[PXUIField(DisplayName = "Location", Required = true)]
		public virtual Int32? CustomerLocationID
		{
			get;
			set;
		}
		#endregion
		#region CustomerOrderNbr
		public abstract class customerOrderNbr : PX.Data.BQL.BqlString.Field<customerOrderNbr> { }

		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Customer Order Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[CustomerOrderNbrLight]
		public virtual String CustomerOrderNbr
		{
			get;
			set;
		}
		#endregion
	}
}