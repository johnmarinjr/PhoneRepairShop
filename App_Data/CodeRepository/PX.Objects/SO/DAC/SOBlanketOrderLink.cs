using PX.Data.ReferentialIntegrity.Attributes;
using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.SO.Attributes;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.BlanketOrderLink)]
	public partial class SOBlanketOrderLink : PX.Data.IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<SOBlanketOrderLink>.By<blanketType, blanketNbr, orderType, orderNbr>
		{
			public static SOBlanketOrderLink Find(PXGraph graph, string blanketType, string blanketNbr, string orderType, string orderNbr) =>
				FindBy(graph, blanketType, blanketNbr, orderType, orderNbr);
		}
		public static class FK
		{
			public class BlanketOrder : SOOrder.PK.ForeignKeyOf<SOBlanketOrderLink>.By<blanketType, blanketNbr> { }
			public class ChildOrder : SOOrder.PK.ForeignKeyOf<SOBlanketOrderLink>.By<orderType, orderNbr> { }
		}
		#endregion

		#region BlanketType
		public abstract class blanketType : Data.BQL.BqlString.Field<blanketType> { }
		[PXDefault()]
		[PXDBString(2, IsFixed = true, IsKey = true)]
		public virtual string BlanketType
		{
			get;
			set;
		}
		#endregion
		#region BlanketNbr
		public abstract class blanketNbr : Data.BQL.BqlString.Field<blanketNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDefault()]
		[PXParent(typeof(FK.BlanketOrder))]
		[PXUIField(DisplayName = "Blanket Order Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<blanketType>>>>), ValidateValue = false)]
		public virtual string BlanketNbr
		{
			get;
			set;
		}
		#endregion
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		[PXDBString(2, IsKey = true, IsFixed = true)]
		[PXDefault(typeof(SOOrder.orderType))]
		[PXUIField(DisplayName = "Order Type", Visible = true, Enabled = false)]
		[PXSelector(typeof(Search<SOOrderType.orderType>), CacheGlobal = true)]
		public virtual String OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<SOBlanketOrderLink.orderType>>>>))]
		[PXDBDefault(typeof(SOOrder.orderNbr))]
		[PXParent(typeof(FK.ChildOrder))]
		[PXUIField(DisplayName = "Order Nbr.", Visible = true, Enabled = false)]
		public virtual String OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region OrderedQty
		public abstract class orderedQty : PX.Data.BQL.BqlDecimal.Field<orderedQty> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ordered Qty.", Enabled = false)]
		public virtual Decimal? OrderedQty
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong()]
		[CurrencyInfo(typeof(SOOrder.curyInfoID))]
		public virtual Int64? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryOrderedAmt
		public abstract class curyOrderedAmt : PX.Data.BQL.BqlDecimal.Field<curyOrderedAmt> { }
		[PXDBCurrency(typeof(SOBlanketOrderLink.curyInfoID), typeof(SOBlanketOrderLink.orderedAmt))]
		[PXUIField(DisplayName = "Ordered Amount", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryOrderedAmt
		{
			get;
			set;
		}
		#endregion
		#region OrderedAmt
		public abstract class orderedAmt : PX.Data.BQL.BqlDecimal.Field<orderedAmt> { }
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? OrderedAmt
		{
			get;
			set;
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp(RecordComesFirst = true)]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
	}

	[PXProjection(typeof(Select5<SOBlanketOrderLink,
			InnerJoin<SOOrder, On<FK.ChildOrder>,
			LeftJoin<SOShipLine, On<SOShipLine.FK.BlanketOrderLink>,
			LeftJoin<SOOrderShipment, On<SOShipLine.FK.OrderShipment>,
			LeftJoin<SOShipment, On<SOOrderShipment.FK.Shipment>,
			LeftJoin<ARRegister, On<SOOrderShipment.FK.ARRegister>>>>>>,
			Aggregate<
				GroupBy<SOBlanketOrderLink.blanketType,
				GroupBy<SOBlanketOrderLink.blanketNbr,
				GroupBy<SOBlanketOrderLink.orderType,
				GroupBy<SOBlanketOrderLink.orderNbr,
				GroupBy<SOOrderShipment.shippingRefNoteID,
				Sum<SOShipLine.shippedQty>>>>>>>>),
			Persistent = false)]
	[PXCacheName(Messages.BlanketOrderDisplayLink)]
	public partial class SOBlanketOrderDisplayLink : SOBlanketOrderLink
	{
		#region Keys
		public new class PK : PrimaryKeyOf<SOBlanketOrderDisplayLink>.By<blanketType, blanketNbr, orderType, orderNbr, shippingRefNoteID>
		{
			public static SOBlanketOrderDisplayLink Find(PXGraph graph, string blanketType, string blanketNbr, string orderType, string orderNbr, Guid? shippingRefNoteID) =>
				FindBy(graph, blanketType, blanketNbr, orderType, orderNbr, shippingRefNoteID);
		}
		#endregion

		#region BlanketType
		public new abstract class blanketType : Data.BQL.BqlString.Field<blanketType> { }
		#endregion
		#region BlanketNbr
		public new abstract class blanketNbr : Data.BQL.BqlString.Field<blanketNbr> { }
		#endregion
		#region OrderType
		public new abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		#endregion
		#region OrderNbr
		public new abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(SOBlanketOrderLink.orderNbr))]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<SOBlanketOrderDisplayLink.orderType>>>>))]
		[PXUIField(DisplayName = "Order Nbr.", Visible = true, Enabled = false)]
		public override String OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region CustomerLocationID
		public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }
		protected Int32? _CustomerLocationID;
		[LocationActive(typeof(Where<Location.bAccountID, Equal<Current<SOOrder.customerID>>,
			And<MatchWithBranch<Location.cBranchID>>>), DescriptionField = typeof(Location.descr),
			Visibility = PXUIVisibility.SelectorVisible, BqlField = typeof(SOOrder.customerLocationID), DisplayName = "Ship-To Location", Enabled = false)]
		public virtual Int32? CustomerLocationID
		{
			get;
			set;
		}
		#endregion
		#region OrderDate
		public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }
		[PXDBDate(BqlField = typeof(SOOrder.orderDate))]
		[PXUIField(DisplayName = "Order Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? OrderDate
		{
			get;
			set;
		}
		#endregion
		#region OrderStatus
		public abstract class orderStatus : PX.Data.BQL.BqlString.Field<orderStatus> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOOrder.status))]
		[PXUIField(DisplayName = "Order Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[SOOrderStatus.List()]
		public virtual String OrderStatus
		{
			get;
			set;
		}
		#endregion
		#region OrderedQty
		public new abstract class orderedQty : PX.Data.BQL.BqlString.Field<orderedQty> { }
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlString.Field<curyInfoID> { }
		#endregion
		#region CuryOrderedAmt
		public new abstract class curyOrderedAmt : PX.Data.BQL.BqlString.Field<curyOrderedAmt> { }
		#endregion
		#region OrderedAmt
		public new abstract class orderedAmt : PX.Data.BQL.BqlString.Field<orderedAmt> { }
		#endregion
		#region Operation
		public abstract class operation : PX.Data.BQL.BqlString.Field<operation> { }
		[PXDBString(1, IsFixed = true, InputMask = ">a", BqlField = typeof(SOOrderShipment.operation))]
		[PXUIField(DisplayName = "Operation", Enabled = false)]
		[SOOperation.List]
		public virtual String Operation
		{
			get;
			set;
		}
		#endregion
		#region ShippingRefNoteID
		public abstract class shippingRefNoteID : PX.Data.BQL.BqlGuid.Field<shippingRefNoteID> { }
		[PXDBGuid(IsKey = true, BqlField = typeof(SOOrderShipment.shippingRefNoteID))]
		public virtual Guid? ShippingRefNoteID
		{
			get;
			set;
		}

		#endregion
		#region DisplayShippingRefNoteID
		public abstract class displayShippingRefNoteID : PX.Data.BQL.BqlGuid.Field<displayShippingRefNoteID> { }
		[ShippingRefNote]
		[PXFormula(typeof(shippingRefNoteID))]
		[PXUIField(DisplayName = "Document Nbr.", Enabled = false)]
		public virtual Guid? DisplayShippingRefNoteID
		{
			get;
			set;
		}
		#endregion
		#region ShipmentType
		public abstract class shipmentType : PX.Data.BQL.BqlString.Field<shipmentType> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOOrderShipment.shipmentType))]
		[SOShipmentType.List()]
		[PXUIField(DisplayName = "Shipment Type", Enabled = false)]
		public virtual String ShipmentType
		{
			get;
			set;
		}
		#endregion
		#region ShipmentNbr
		public abstract class shipmentNbr : PX.Data.BQL.BqlString.Field<shipmentNbr> { }
		[PXDBString(15, InputMask = "", IsUnicode = true, BqlField = typeof(SOOrderShipment.shipmentNbr))]
		[PXUIField(DisplayName = "Shipment Nbr.", Visible = false, Enabled = false)]
		public virtual String ShipmentNbr
		{
			get;
			set;
		}
		#endregion
		#region ShipmentDate
		public abstract class shipmentDate : PX.Data.BQL.BqlDateTime.Field<shipmentDate> { }
		[PXDBDate(BqlField = typeof(SOShipment.shipDate))]
		[PXUIField(DisplayName = "Shipment Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? ShipmentDate
		{
			get;
			set;
		}
		#endregion
		#region ShipmentStatus
		public abstract class shipmentStatus : PX.Data.BQL.BqlString.Field<shipmentStatus> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOShipment.status))]
		[PXUIField(DisplayName = "Shipment Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[SOShipmentStatus.List()]
		public virtual String ShipmentStatus
		{
			get;
			set;
		}
		#endregion
		#region ShippedQty
		public abstract class shippedQty : PX.Data.BQL.BqlDecimal.Field<shippedQty> { }
		[PXDBQuantity(BqlField = typeof(SOShipLine.shippedQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Shipped Qty.", Enabled = false)]
		public virtual Decimal? ShippedQty
		{
			get;
			set;
		}
		#endregion
		#region InvoiceType
		public abstract class invoiceType : PX.Data.BQL.BqlString.Field<invoiceType> { }
		protected String _InvoiceType;
		[PXDBString(3, IsFixed = true, BqlField = typeof(SOOrderShipment.invoiceType))]
		[PXUIField(DisplayName = "Invoice Type", Enabled = false)]
		[ARDocType.List()]
		public virtual String InvoiceType
		{
			get
			{
				return this._InvoiceType;
			}
			set
			{
				this._InvoiceType = value;
			}
		}
		#endregion
		#region InvoiceNbr
		public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		protected String _InvoiceNbr;
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOOrderShipment.invoiceNbr))]
		[PXUIField(DisplayName = "Invoice Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<SOInvoice.refNbr, Where<SOInvoice.docType, Equal<Current<SOBlanketOrderDisplayLink.invoiceType>>>>), DirtyRead = true)]
		public virtual String InvoiceNbr
		{
			get
			{
				return this._InvoiceNbr;
			}
			set
			{
				this._InvoiceNbr = value;
			}
		}
		#endregion
		#region InvoiceDate
		public abstract class invoiceDate : PX.Data.BQL.BqlDateTime.Field<invoiceDate> { }
		[PXDBDate(BqlField = typeof(ARRegister.docDate))]
		[PXUIField(DisplayName = "Invoice Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? InvoiceDate
		{
			get;
			set;
		}
		#endregion
		#region InvoiceStatus
		public abstract class invoiceStatus : PX.Data.BQL.BqlString.Field<invoiceStatus> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(ARRegister.status))]
		[PXUIField(DisplayName = "Invoice Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[ARDocStatus.List()]
		public virtual String InvoiceStatus
		{
			get;
			set;
		}
		#endregion
		#region InvtDocType
		public abstract class invtDocType : PX.Data.BQL.BqlString.Field<invtDocType> { }
		protected String _InvtDocType;
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOOrderShipment.invtDocType))]
		[PXUIField(DisplayName = "Inventory Doc. Type", Enabled = false)]
		[INDocType.List()]
		public virtual String InvtDocType
		{
			get
			{
				return this._InvtDocType;
			}
			set
			{
				this._InvtDocType = value;
			}
		}
		#endregion
		#region InvtRefNbr
		public abstract class invtRefNbr : PX.Data.BQL.BqlString.Field<invtRefNbr> { }
		protected String _InvtRefNbr;
		[PXDBString(15, IsUnicode = true, InputMask = "", BqlField = typeof(SOOrderShipment.invtRefNbr))]
		[PXUIField(DisplayName = "Inventory Ref. Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<INRegister.refNbr, Where<INRegister.docType, Equal<Current<SOOrderShipment.invtDocType>>>>))]
		public virtual String InvtRefNbr
		{
			get
			{
				return this._InvtRefNbr;
			}
			set
			{
				this._InvtRefNbr = value;
			}
		}
		#endregion
		#region tstamp
		public new abstract class Tstamp : PX.Data.BQL.BqlString.Field<Tstamp> { }
		#endregion
	}

	[PXProjection(typeof(Select5<SOBlanketOrderLink,
			InnerJoin<SOOrder, On<FK.ChildOrder>,
			InnerJoin<ARTran, On<ARTran.FK.BlanketOrderLink>,
			InnerJoin<SOOrderShipment, On<ARTran.FK.SOOrderShipment>,
			InnerJoin<ARRegister, On<SOOrderShipment.FK.ARRegister>>>>>,
			Where<SOOrderShipment.shipmentNoteID.IsNull>,
			Aggregate<
				GroupBy<SOBlanketOrderLink.blanketType,
				GroupBy<SOBlanketOrderLink.blanketNbr,
				GroupBy<SOBlanketOrderLink.orderType,
				GroupBy<SOBlanketOrderLink.orderNbr,
				GroupBy<SOOrderShipment.shippingRefNoteID>>>>>>>),
			Persistent = false)]
	[PXHidden]
	public partial class SOBlanketOrderMiscLink : SOBlanketOrderDisplayLink
	{
		#region BlanketType
		public new abstract class blanketType : Data.BQL.BqlString.Field<blanketType> { }
		#endregion
		#region BlanketNbr
		public new abstract class blanketNbr : Data.BQL.BqlString.Field<blanketNbr> { }
		#endregion
		#region OrderType
		public new abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		#endregion
		#region OrderNbr
		public new abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		#endregion
		#region CustomerLocationID
		public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }
		#endregion
		#region OrderDate
		public new abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }
		#endregion
		#region OrderStatus
		public new abstract class orderStatus : PX.Data.BQL.BqlString.Field<orderStatus> { }
		#endregion
		#region OrderedQty
		public new abstract class orderedQty : PX.Data.BQL.BqlString.Field<orderedQty> { }
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlString.Field<curyInfoID> { }
		#endregion
		#region CuryOrderedAmt
		public new abstract class curyOrderedAmt : PX.Data.BQL.BqlString.Field<curyOrderedAmt> { }
		#endregion
		#region OrderedAmt
		public new abstract class orderedAmt : PX.Data.BQL.BqlString.Field<orderedAmt> { }
		#endregion
		#region Operation
		public new abstract class operation : PX.Data.BQL.BqlString.Field<operation> { }
		[PXString(1, IsFixed = true, InputMask = ">a")]
		[PXUIField(DisplayName = "Operation", Enabled = false)]
		[SOOperation.List]
		public override String Operation
		{
			get;
			set;
		}
		#endregion
		#region ShippingRefNoteID
		public new abstract class shippingRefNoteID : PX.Data.BQL.BqlGuid.Field<shippingRefNoteID> { }
		#endregion
		#region DisplayShippingRefNoteID
		public new abstract class displayShippingRefNoteID : PX.Data.BQL.BqlGuid.Field<displayShippingRefNoteID> { }
		#endregion
		#region ShipmentType
		public new abstract class shipmentType : PX.Data.BQL.BqlString.Field<shipmentType> { }
		#endregion
		#region ShipmentNbr
		public new abstract class shipmentNbr : PX.Data.BQL.BqlString.Field<shipmentNbr> { }
		#endregion
		#region ShipmentDate
		public new abstract class shipmentDate : PX.Data.BQL.BqlDateTime.Field<shipmentDate> { }
		[PXDate]
		[PXUIField(DisplayName = "Shipment Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public override DateTime? ShipmentDate
		{
			get;
			set;
		}
		#endregion
		#region ShipmentStatus
		public new abstract class shipmentStatus : PX.Data.BQL.BqlString.Field<shipmentStatus> { }
		[PXString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Shipment Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[SOShipmentStatus.List()]
		public override String ShipmentStatus
		{
			get;
			set;
		}
		#endregion
		#region ShippedQty
		public new abstract class shippedQty : PX.Data.BQL.BqlDecimal.Field<shippedQty> { }
		[PXQuantity]
		[PXUIField(DisplayName = "Shipped Qty.", Enabled = false)]
		public override Decimal? ShippedQty
		{
			get => base.ShippedQty;
			set => base.ShippedQty = value;
		}
		#endregion
		#region InvoiceType
		public new abstract class invoiceType : PX.Data.BQL.BqlString.Field<invoiceType> { }
		#endregion
		#region InvoiceNbr
		public new abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		#endregion
		#region InvoiceDate
		public new abstract class invoiceDate : PX.Data.BQL.BqlDateTime.Field<invoiceDate> { }
		#endregion
		#region InvoiceStatus
		public new abstract class invoiceStatus : PX.Data.BQL.BqlString.Field<invoiceStatus> { }
		#endregion
		#region InvtDocType
		public new abstract class invtDocType : PX.Data.BQL.BqlString.Field<invtDocType> { }
		#endregion
		#region InvtRefNbr
		public new abstract class invtRefNbr : PX.Data.BQL.BqlString.Field<invtRefNbr> { }
		#endregion
		#region tstamp
		public new abstract class Tstamp : PX.Data.BQL.BqlString.Field<Tstamp> { }
		#endregion
	}
}
