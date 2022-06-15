﻿using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.SO
{
	using System;
    using System.Text;
	using PX.Data;
	using PX.Objects.IN;
	using PX.Objects.CS;
	using PX.Objects.GL;
	using PX.Objects.PO;
	using PX.Objects.Common.Bql;
	using PX.Objects.Common;

	[System.SerializableAttribute()]
	[PXCacheName(Messages.SOLineSplit)]
	public partial class SOLineSplit : PX.Data.IBqlTable, ILSDetail
	{
		#region Keys
		public class PK : PrimaryKeyOf<SOLineSplit>.By<orderType, orderNbr, lineNbr, splitLineNbr>
		{
			public static SOLineSplit Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr, int? splitLineNbr)
				=> FindBy(graph, orderType, orderNbr, lineNbr, splitLineNbr);
		}
		public static class FK
		{
			public class Order : SOOrder.PK.ForeignKeyOf<SOLineSplit>.By<orderType, orderNbr> { }
			public class OrderType : SOOrderType.PK.ForeignKeyOf<SOLineSplit>.By<orderType> { }
			public class OrderTypeOperation : SOOrderTypeOperation.PK.ForeignKeyOf<SOLineSplit>.By<orderType, operation> { }
			public class OrderLine : SOLine.PK.ForeignKeyOf<SOLineSplit>.By<orderType, orderNbr, lineNbr> { }
			public class ParentLineSplit : SOLineSplit.PK.ForeignKeyOf<SOLineSplit>.By<orderType, orderNbr, lineNbr, parentSplitLineNbr> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<SOLineSplit>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<SOLineSplit>.By<siteID> { }
			public class SiteStatus : IN.INSiteStatus.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID, subItemID, siteID> { }
			public class ToSite : INSite.PK.ForeignKeyOf<SOLineSplit>.By<toSiteID> { }
			public class ToSiteStatus : IN.INSiteStatus.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID, subItemID, toSiteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<SOLineSplit>.By<locationID> { }
			public class LocationStatus : IN.INLocationStatus.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID, subItemID, siteID, locationID> { }
			public class LotSerialStatus : IN.INLotSerialStatus.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID, subItemID, siteID, locationID, lotSerialNbr> { }
			public class Shipment : SOShipment.PK.ForeignKeyOf<SOLineSplit>.By<shipmentNbr> { }
			public class Vendor : AP.Vendor.PK.ForeignKeyOf<SOLineSplit>.By<vendorID> { }
			public class POSite : INSite.PK.ForeignKeyOf<SOLineSplit>.By<pOSiteID> { }
			public class POOrder : Objects.PO.POOrder.PK.ForeignKeyOf<SOLineSplit>.By<pOType, pONbr> { }
			public class POLine : Objects.PO.POLine.PK.ForeignKeyOf<SOLineSplit>.By<pOType, pONbr, pOLineNbr> { }
			public class POReceipt : Objects.PO.POReceipt.PK.ForeignKeyOf<SOLineSplit>.By<pOReceiptType, pOReceiptNbr> { }
			public class RelatedOrder : SOOrder.PK.ForeignKeyOf<SOLineSplit>.By<sOOrderType, sOOrderNbr> { }
			public class RelatedOrderType : SOOrderType.PK.ForeignKeyOf<SOLineSplit>.By<sOOrderType> { }
			public class RelatedOrderLine : SOLine.PK.ForeignKeyOf<SOLineSplit>.By<sOOrderType, sOOrderNbr, sOLineNbr> { }
			public class RelatedOrderLineSplit : SOLineSplit.PK.ForeignKeyOf<SOLineSplit>.By<sOOrderType, sOOrderNbr, sOLineNbr, sOSplitLineNbr> { }
			public class ItemPlan : INItemPlan.PK.ForeignKeyOf<SOLineSplit>.By<planID> { }
			//todo public class UnitOfMeasure : INUnit.PK.ForeignKeyOf<SOLineSplit>.By<inventoryID, uOM> { }

			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use OrderLine instead.")]
			public class Line : OrderLine { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use ParentLineSplit instead.")]
			public class ParenLineSplit : ParentLineSplit { }

			public class SupplyLine : SupplyPOLine.PK.ForeignKeyOf<SOLineSplit>.By<pOType, pONbr, pOLineNbr> { }
		}
		#endregion

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		protected String _OrderType;
		[PXDBString(2, IsKey = true, IsFixed = true)]
		[PXDefault(typeof(SOOrder.orderType))]
        [PXSelector(typeof(Search<SOOrderType.orderType>), CacheGlobal = true)]
		public virtual String OrderType
		{
			get
			{
				return this._OrderType;
			}
			set
			{
				this._OrderType = value;
			}
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		protected String _OrderNbr;
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDBDefault(typeof(SOOrder.orderNbr))]
		[PXParent(typeof(FK.Order))]
		[PXParent(typeof(FK.OrderLine))]
		public virtual String OrderNbr
		{
			get
			{
				return this._OrderNbr;
			}
			set
			{
				this._OrderNbr = value;
			}
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(SOLine.lineNbr))]
		public virtual Int32? LineNbr
		{
			get
			{
				return this._LineNbr;
			}
			set
			{
				this._LineNbr = value;
			}
		}
		#endregion
		#region SplitLineNbr
		public abstract class splitLineNbr : PX.Data.BQL.BqlInt.Field<splitLineNbr> { }
		protected Int32? _SplitLineNbr;
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		[PXLineNbr(typeof(SOOrder.lineCntr))]
		[PXUIField(DisplayName = "Allocation ID", Visible = false, IsReadOnly = true, Enabled = false)]
		public virtual Int32? SplitLineNbr
		{
			get
			{
				return this._SplitLineNbr;
			}
			set
			{
				this._SplitLineNbr = value;
			}
		}
		#endregion
		#region ParentSplitLineNbr
		public abstract class parentSplitLineNbr : PX.Data.BQL.BqlInt.Field<parentSplitLineNbr> { }
		protected Int32? _ParentSplitLineNbr;
		[PXDBInt()]
		[PXUIField(DisplayName = "Parent Allocation ID", Visible = false, IsReadOnly = true, Enabled = false)]
		public virtual Int32? ParentSplitLineNbr
		{
			get
			{
				return this._ParentSplitLineNbr;
			}
			set
			{
				this._ParentSplitLineNbr = value;
			}
		}
		#endregion
		#region Behavior
		public abstract class behavior : Data.BQL.BqlString.Field<behavior> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault(typeof(SOLine.behavior))]
		public virtual string Behavior
		{
			get;
			set;
		}
		#endregion
		#region Operation
		public abstract class operation : PX.Data.BQL.BqlString.Field<operation> { }
		protected String _Operation;
		[PXDBString(1, IsFixed = true)]
		[PXDefault(typeof(SOLine.operation))]
		[PXSelectorMarker(typeof(Search<SOOrderTypeOperation.operation, Where<SOOrderTypeOperation.orderType, Equal<Current<SOLineSplit.orderType>>>>))]
		public virtual String Operation
		{
			get
			{
				return this._Operation;
			}
			set
			{
				this._Operation = value;
			}
		}
		#endregion
		#region InvtMult
		public abstract class invtMult : PX.Data.BQL.BqlShort.Field<invtMult> { }
		protected Int16? _InvtMult;
		[PXDBShort()]
		[PXDefault(typeof(INTran.invtMult))]
		public virtual Int16? InvtMult
		{
			get
			{
				return this._InvtMult;
			}
			set
			{
				this._InvtMult = value;
			}
		}
		#endregion
		#region RequireShipping
		public abstract class requireShipping : PX.Data.BQL.BqlBool.Field<requireShipping> { }
		protected bool? _RequireShipping;
		[PXBool()]
		[PXFormula(typeof(Selector<SOLineSplit.orderType, SOOrderType.requireShipping>))]
		public virtual bool? RequireShipping
		{
			get
			{
				return this._RequireShipping;
			}
			set
			{
				this._RequireShipping = value;
			}
		}
		#endregion
		#region RequireAllocation
		public abstract class requireAllocation : PX.Data.BQL.BqlBool.Field<requireAllocation> { }
		protected bool? _RequireAllocation;
		[PXBool()]
		[PXFormula(typeof(Selector<SOLineSplit.orderType, SOOrderType.requireAllocation>))]
		public virtual bool? RequireAllocation
		{
			get
			{
				return this._RequireAllocation;
			}
			set
			{
				this._RequireAllocation = value;
			}
		}
		#endregion
		#region RequireLocation
		public abstract class requireLocation : PX.Data.BQL.BqlBool.Field<requireLocation> { }
		protected bool? _RequireLocation;
		[PXBool()]
		[PXFormula(typeof(Selector<SOLineSplit.orderType, SOOrderType.requireLocation>))]
		public virtual bool? RequireLocation
		{
			get
			{
				return this._RequireLocation;
			}
			set
			{
				this._RequireLocation = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[Inventory(Enabled = false, Visible = true)]
		[PXDefault(typeof(SOLine.inventoryID))]
		[PXForeignReference(typeof(FK.InventoryItem))]
		public virtual Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }
		protected String _LineType;
		[PXDBString(2, IsFixed = true)]
		[PXDefault(typeof(SOLine.lineType))]
		public virtual String LineType
		{
			get
			{
				return this._LineType;
			}
			set
			{
				this._LineType = value;
			}
		}
		#endregion
		#region IsStockItem
		public abstract class isStockItem : PX.Data.BQL.BqlBool.Field<isStockItem> { }
		[PXDBBool()]
		[PXFormula(typeof(Selector<SOLineSplit.inventoryID, InventoryItem.stkItem>))]
		public bool? IsStockItem
		{
			get;
			set;
		}
		#endregion
        #region IsAllocated
        public abstract class isAllocated : PX.Data.BQL.BqlBool.Field<isAllocated> { }
        protected Boolean? _IsAllocated;
        [PXDBBool()]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Allocated")]
        public virtual Boolean? IsAllocated
        {
            get
            {
                return this._IsAllocated;
            }
            set
            {
                this._IsAllocated = value;
            }
        }
        #endregion
        #region IsMergeable
        public abstract class isMergeable : PX.Data.BQL.BqlBool.Field<isMergeable> { }
        protected Boolean? _IsMergeable;
        [PXBool()]
        [PXFormula(typeof(True))]
        public virtual Boolean? IsMergeable
        {
            get
            {
                return this._IsMergeable;
            }
            set
            {
                this._IsMergeable = value;
            }
        }
        #endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		protected Int32? _SiteID;
        [SiteAvail(typeof(SOLineSplit.inventoryID), typeof(SOLineSplit.subItemID),
			new Type[] { typeof(INSite.siteCD), typeof(INSiteStatus.qtyOnHand), typeof(INSiteStatus.qtyAvail), typeof(INSiteStatus.active), typeof(INSite.descr) },
			DisplayName = "Alloc. Warehouse", DocumentBranchType = typeof(SOOrder.branchID))]
        [PXFormula(typeof(Switch<Case<Where<SOLineSplit.isAllocated, Equal<False>>, Current<SOLine.siteID>>, SOLineSplit.siteID>))]
		[PXDefault]
		[PXUIRequired(typeof(IIf<Where<lineType, NotEqual<SOLineType.miscCharge>>, True, False>))]
		[PXForeignReference(typeof(Field<siteID>.IsRelatedTo<INSite.siteID>))]
		[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, Current<SOOrder.branchID>>>))]
		public virtual Int32? SiteID
		{
			get
			{
				return this._SiteID;
			}
			set
			{
				this._SiteID = value;
			}
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		protected Int32? _LocationID;
		[SOLocationAvail(typeof(SOLineSplit.inventoryID), typeof(SOLineSplit.subItemID), typeof(SOLineSplit.siteID), typeof(SOLineSplit.tranType), typeof(SOLineSplit.invtMult))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? LocationID
		{
			get
			{
				return this._LocationID;
			}
			set
			{
				this._LocationID = value;
			}
		}
		#endregion
		#region ToSiteID
		public abstract class toSiteID : PX.Data.BQL.BqlInt.Field<toSiteID> { }
		protected Int32? _ToSiteID;
		[IN.Site(DisplayName = "Orig. Warehouse")]
		[PXDefault(typeof(SOLine.siteID))]
		[PXUIRequired(typeof(IIf<Where<lineType, NotEqual<SOLineType.miscCharge>>, True, False>))]
		public virtual Int32? ToSiteID
		{
			get
			{
				return this._ToSiteID;
			}
			set
			{
				this._ToSiteID = value;
			}
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		protected Int32? _SubItemID;
		[IN.SubItem(typeof(SOLineSplit.inventoryID))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[SubItemStatusVeryfier(typeof(SOLineSplit.inventoryID), typeof(SOLineSplit.siteID), InventoryItemStatus.Inactive, InventoryItemStatus.NoSales)]
		public virtual Int32? SubItemID
		{
			get
			{
				return this._SubItemID;
			}
			set
			{
				this._SubItemID = value;
			}
		}
		#endregion
		#region ShipDate
		public abstract class shipDate : PX.Data.BQL.BqlDateTime.Field<shipDate> { }
		protected DateTime? _ShipDate;
		[PXDBDate()]
		[PXDefault(typeof(SOLine.shipDate), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Ship On", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? ShipDate
		{
			get
			{
				return this._ShipDate;
			}
			set
			{
				this._ShipDate = value;
			}
		}
		#endregion
        #region ShipComplete
        public abstract class shipComplete : PX.Data.BQL.BqlString.Field<shipComplete> { }
        protected String _ShipComplete;
        [PXDBString(1, IsFixed = true)]
        [PXDefault(typeof(SOLine.shipComplete), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual String ShipComplete
        {
            get
            {
                return this._ShipComplete;
            }
            set
            {
                this._ShipComplete = value;
            }
        }
        #endregion
		#region Completed
		public abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }
		protected Boolean? _Completed;
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Completed", Enabled = false)]
		public virtual Boolean? Completed
		{
			get
			{
				return this._Completed;
			}
			set
			{
				this._Completed = value;
			}
		}
		#endregion
		#region ShipmentNbr
		public abstract class shipmentNbr : PX.Data.BQL.BqlString.Field<shipmentNbr> { }
		protected string _ShipmentNbr;
		[PXDBString(IsUnicode = true)]
		[PXUIFieldAttribute(DisplayName="Shipment Nbr.", Enabled = false)]
		public virtual string ShipmentNbr
		{
			get
			{
				return this._ShipmentNbr;
			}
			set
			{
				this._ShipmentNbr = value;
			}
		}
		#endregion
		#region LotSerialNbr
		public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
		protected String _LotSerialNbr;
        [SOLotSerialNbrAttribute.SOAllocationLotSerialNbr(typeof(SOLineSplit.inventoryID), typeof(SOLineSplit.subItemID), typeof(SOLineSplit.siteID), typeof(SOLineSplit.locationID), typeof(SOLine.lotSerialNbr), FieldClass = "LotSerial")]
		public virtual String LotSerialNbr
		{
			get
			{
				return this._LotSerialNbr;
			}
			set
			{
				this._LotSerialNbr = value;
			}
		}
		#endregion
		#region LotSerClassID
		public abstract class lotSerClassID : PX.Data.BQL.BqlString.Field<lotSerClassID> { }
		protected String _LotSerClassID;
		[PXString(10, IsUnicode = true)]
		public virtual String LotSerClassID
		{
			get
			{
				return this._LotSerClassID;
			}
			set
			{
				this._LotSerClassID = value;
			}
		}
		#endregion
		#region AssignedNbr
		public abstract class assignedNbr : PX.Data.BQL.BqlString.Field<assignedNbr> { }
		protected String _AssignedNbr;
		[PXString(30, IsUnicode = true)]
		public virtual String AssignedNbr
		{
			get
			{
				return this._AssignedNbr;
			}
			set
			{
				this._AssignedNbr = value;
			}
		}
		#endregion
		#region ExpireDate
		public abstract class expireDate : PX.Data.BQL.BqlDateTime.Field<expireDate> { }
		protected DateTime? _ExpireDate;
		[INExpireDate(typeof(SOLineSplit.inventoryID))]
		public virtual DateTime? ExpireDate
		{
			get
			{
				return this._ExpireDate;
			}
			set
			{
				this._ExpireDate = value;
			}
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;
		[INUnit(typeof(SOLineSplit.inventoryID), DisplayName = "UOM", Enabled = false)]
		[PXDefault(typeof(SOLine.uOM))]
		public virtual String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region Qty
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
		protected Decimal? _Qty;
		[PXDBQuantity(typeof(SOLineSplit.uOM), typeof(SOLineSplit.baseQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public virtual Decimal? Qty
		{
			get
			{
				return this._Qty;
			}
			set
			{
				this._Qty = value;
			}
		}
		#endregion
		#region BaseQty
		public abstract class baseQty : PX.Data.BQL.BqlDecimal.Field<baseQty> { }
		protected Decimal? _BaseQty;
		[PXDBDecimal(6)]
		public virtual Decimal? BaseQty
		{
			get
			{
				return this._BaseQty;
			}
			set
			{
				this._BaseQty = value;
			}
		}
		#endregion
		#region ShippedQty
		public abstract class shippedQty : PX.Data.BQL.BqlDecimal.Field<shippedQty> { }
		protected Decimal? _ShippedQty;
		[PXDBQuantity(typeof(SOLineSplit.uOM), typeof(SOLineSplit.baseShippedQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Shipments", Enabled = false)]
		public virtual Decimal? ShippedQty
		{
			get
			{
				return this._ShippedQty;
			}
			set
			{
				this._ShippedQty = value;
			}
		}
		#endregion
		#region BaseShippedQty
		public abstract class baseShippedQty : PX.Data.BQL.BqlDecimal.Field<baseShippedQty> { }
		protected Decimal? _BaseShippedQty;
		[PXDBDecimal(6, MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseShippedQty
		{
			get
			{
				return this._BaseShippedQty;
			}
			set
			{
				this._BaseShippedQty = value;
			}
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }
		protected Decimal? _ReceivedQty;
		[PXDBQuantity(typeof(SOLineSplit.uOM), typeof(SOLineSplit.baseReceivedQty), MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Received", Enabled = false)]
		public virtual Decimal? ReceivedQty
		{
			get
			{
				return this._ReceivedQty;
			}
			set
			{
				this._ReceivedQty = value;
			}
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }
		protected Decimal? _BaseReceivedQty;
		[PXDBDecimal(6, MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseReceivedQty
		{
			get
			{
				return this._BaseReceivedQty;
			}
			set
			{
				this._BaseReceivedQty = value;
			}
		}
		#endregion
		#region UnreceivedQty
		public abstract class unreceivedQty : PX.Data.BQL.BqlDecimal.Field<unreceivedQty> { }
		protected Decimal? _UnreceivedQty;
		[PXQuantity(typeof(SOLineSplit.uOM), typeof(SOLineSplit.baseUnreceivedQty), MinValue = 0)]
		[PXFormula(typeof(Sub<SOLineSplit.qty, SOLineSplit.receivedQty>))]
		public virtual Decimal? UnreceivedQty
		{
			get
			{
				return this._UnreceivedQty;
			}
			set
			{
				this._UnreceivedQty = value;
			}
		}
		#endregion
		#region BaseUnreceivedQty
		public abstract class baseUnreceivedQty : PX.Data.BQL.BqlDecimal.Field<baseUnreceivedQty> { }
		protected Decimal? _BaseUnreceivedQty;
		[PXDecimal(6, MinValue = 0)]
		[PXFormula(typeof(Sub<SOLineSplit.baseQty, SOLineSplit.baseReceivedQty>))]
		public virtual Decimal? BaseUnreceivedQty
		{
			get
			{
				return this._BaseUnreceivedQty;
			}
			set
			{
				this._BaseUnreceivedQty = value;
			}
		}
		#endregion
		#region OpenQty
		public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }
		protected Decimal? _OpenQty;
		[PXQuantity(typeof(SOLineSplit.uOM), typeof(SOLineSplit.baseOpenQty), MinValue = 0)]
		[PXFormula(typeof(Sub<SOLineSplit.qty, SOLineSplit.shippedQty>))]
		public virtual Decimal? OpenQty
		{
			get
			{
				return this._OpenQty;
			}
			set
			{
				this._OpenQty = value;
			}
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : PX.Data.BQL.BqlDecimal.Field<baseOpenQty> { }
		protected Decimal? _BaseOpenQty;
		[PXDecimal(6, MinValue = 0)]
		[PXFormula(typeof(Sub<SOLineSplit.baseQty, SOLineSplit.baseShippedQty>))]
		public virtual Decimal? BaseOpenQty
		{
			get
			{
				return this._BaseOpenQty;
			}
			set
			{
				this._BaseOpenQty = value;
			}
		}
		#endregion
		#region OrderDate
		public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }
		protected DateTime? _OrderDate;
		[PXDBDate()]
		[PXDBDefault(typeof(SOOrder.orderDate))]
		public virtual DateTime? OrderDate
		{
			get
			{
				return this._OrderDate;
			}
			set
			{
				this._OrderDate = value;
			}
		}
		#endregion
		#region TranType
		public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
		protected String _TranType;
		[PXFormula(typeof(Selector<SOLineSplit.operation, SOOrderTypeOperation.iNDocType>))]
		[PXString(SOOrderTypeOperation.iNDocType.Length, IsFixed = true)]
		public virtual String TranType
		{
			get
			{
				return this._TranType;
			}
			set
			{
				this._TranType = value;
			}
		}
		#endregion
		#region TranDate
		public virtual DateTime? TranDate
		{
			get { return this._OrderDate; }
		}
		#endregion
		#region PlanType
		public abstract class planType : PX.Data.BQL.BqlString.Field<planType> { }
		protected String _PlanType;
		[PXFormula(typeof(Selector<SOLineSplit.operation, SOOrderTypeOperation.orderPlanType>))]
		[PXString(SOOrderTypeOperation.orderPlanType.Length, IsFixed = true)]
		public virtual String PlanType
		{
			get
			{
				return this._PlanType;
			}
			set
			{
				this._PlanType = value;
			}
		}
		#endregion
		#region AllocatedPlanType
		public abstract class allocatedPlanType : PX.Data.BQL.BqlString.Field<allocatedPlanType> { }
		[PXFormula(typeof(INPlanConstants.plan61))]
		public virtual String AllocatedPlanType
		{
			get;
			set;
		}
		#endregion
		#region BackOrderPlanType
		public abstract class backOrderPlanType : PX.Data.BQL.BqlString.Field<backOrderPlanType> { }
		protected String _BackOrderPlanType;
		[PXFormula(typeof(INPlanConstants.plan68))]
		public virtual String BackOrderPlanType
		{
			get
			{
				return this._BackOrderPlanType;
			}
			set
			{
				this._BackOrderPlanType = value;
			}
		}
		#endregion
		#region OrigPlanType
		public abstract class origPlanType : PX.Data.BQL.BqlString.Field<origPlanType> { }
		[PXDBString(2, IsFixed = true)]
		[PXSelector(typeof(Search<INPlanType.planType>), CacheGlobal = true)]
		public virtual String OrigPlanType
		{
			get;
			set;
		}
		#endregion   

		#region POCreate
		public abstract class pOCreate : PX.Data.BQL.BqlBool.Field<pOCreate> { }
		protected Boolean? _POCreate;
		[PXDBBool()]
		[PXDefault()]
        [PXFormula(typeof(Switch<Case<Where<SOLineSplit.isAllocated, Equal<False>, And<SOLineSplit.pOReceiptNbr, IsNull>>, Current<SOLine.pOCreate>>, False>))]
		[PXUIField(DisplayName = "Mark for PO", Visible = true, Enabled = false)]
		public virtual Boolean? POCreate
		{
			get
			{
				return this._POCreate;
			}
			set
			{
				this._POCreate = value ?? false;
			}
		}
		#endregion
		#region POCompleted
		public abstract class pOCompleted : PX.Data.BQL.BqlBool.Field<pOCompleted> { }
		protected Boolean? _POCompleted;
		[PXDBBool()]
		[PXDefault(false)]
		public virtual Boolean? POCompleted
		{
			get
			{
				return this._POCompleted;
			}
			set
			{
				this._POCompleted = value;
			}
		}
		#endregion
		#region POCancelled
		public abstract class pOCancelled : PX.Data.BQL.BqlBool.Field<pOCancelled> { }
		protected Boolean? _POCancelled;
		[PXDBBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? POCancelled
		{
			get
			{
				return this._POCancelled;
			}
			set
			{
				this._POCancelled = value;
			}
		}
		#endregion
		#region POSource
		public abstract class pOSource : PX.Data.BQL.BqlString.Field<pOSource> { }
		protected string _POSource;
		[PXDBString()]
        [PXFormula(typeof(Switch<Case<Where<SOLineSplit.isAllocated, Equal<False>>, Current<SOLine.pOSource>>, Null>))]
		public virtual string POSource
		{
			get
			{
				return this._POSource;
			}
			set
			{
				this._POSource = value;
			}
		}
		#endregion
        #region FixedSource
        public abstract class fixedSource : PX.Data.BQL.BqlString.Field<fixedSource> { }
        protected String _FixedSource;
        [PXString(1, IsFixed = true)]
        [PXDBCalced(typeof(Switch<Case<Where<SOLineSplit.pOCreate, Equal<True>>, INReplenishmentSource.purchased, Case<Where<SOLineSplit.siteID, NotEqual<SOLineSplit.toSiteID>>, INReplenishmentSource.transfer>>, INReplenishmentSource.none>), typeof(string))]
        public virtual String FixedSource
        {
            get
            {
                return this._FixedSource;
            }
            set
            {
                this._FixedSource = value;
            }
        }
        #endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		protected Int32? _VendorID;
		[PXDBInt()]
        [PXFormula(typeof(Switch<Case<Where<SOLineSplit.isAllocated, Equal<False>>, Current<SOLine.vendorID>>, Null>))]
        public virtual Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
        #region POSiteID
        public abstract class pOSiteID : PX.Data.BQL.BqlInt.Field<pOSiteID> { }
        protected Int32? _POSiteID;
        [PXDBInt()]
        [PXFormula(typeof(Switch<Case<Where<SOLineSplit.isAllocated, Equal<False>>, Current<SOLine.pOSiteID>>, Null>))]
        public virtual Int32? POSiteID
        {
            get
            {
                return this._POSiteID;
            }
            set
            {
                this._POSiteID = value;
            }
        }
        #endregion
		#region POType
		public abstract class pOType : PX.Data.BQL.BqlString.Field<pOType> { }
		protected String _POType;
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "PO Type", Enabled = false)]
		[POOrderType.RBDList]
		public virtual String POType
		{
			get
			{
				return this._POType;
			}
			set
			{
				this._POType = value;
			}
		}
		#endregion
		#region PONbr
		public abstract class pONbr : PX.Data.BQL.BqlString.Field<pONbr> { }
		protected String _PONbr;
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "PO Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<POOrder.orderNbr, Where<POOrder.orderType, Equal<Current<SOLineSplit.pOType>>>>), DescriptionField = typeof(POOrder.orderDesc))]
		public virtual String PONbr
		{
			get
			{
				return this._PONbr;
			}
			set
			{
				this._PONbr = value;
			}
		}
		#endregion
		#region POLineNbr
		public abstract class pOLineNbr : PX.Data.BQL.BqlInt.Field<pOLineNbr> { }
		protected Int32? _POLineNbr;
		[PXDBInt()]
		[PXUIField(DisplayName = "PO Line Nbr.", Enabled = false)]
		public virtual Int32? POLineNbr
		{
			get
			{
				return this._POLineNbr;
			}
			set
			{
				this._POLineNbr = value;
			}
		}
		#endregion
        #region POReceiptType
        public abstract class pOReceiptType : PX.Data.BQL.BqlString.Field<pOReceiptType> { }
        protected String _POReceiptType;
        [PXDBString(2, IsFixed = true)]
        [PXUIField(DisplayName = "PO Receipt Type", Enabled = false)]
        public virtual String POReceiptType
        {
            get
            {
                return this._POReceiptType;
            }
            set
            {
                this._POReceiptType = value;
            }
        }
        #endregion
		#region POReceiptNbr
		public abstract class pOReceiptNbr : PX.Data.BQL.BqlString.Field<pOReceiptNbr> { }
		protected String _POReceiptNbr;
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "PO Receipt Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<POReceipt.receiptNbr, Where<POReceipt.receiptType, Equal<Current<SOLineSplit.pOReceiptType>>>>), DescriptionField = typeof(POReceipt.invoiceNbr))]
		public virtual String POReceiptNbr
		{
			get
			{
				return this._POReceiptNbr;
			}
			set
			{
				this._POReceiptNbr = value;
			}
		}
		#endregion
		#region SOOrderType
		public abstract class sOOrderType : PX.Data.BQL.BqlString.Field<sOOrderType> { }
		protected String _SOOrderType;
		[PXDBString(2, IsFixed = true)]
		public virtual String SOOrderType
		{
			get
			{
				return this._SOOrderType;
			}
			set
			{
				this._SOOrderType = value;
			}
		}
		#endregion
		#region SOOrderNbr
		public abstract class sOOrderNbr : PX.Data.BQL.BqlString.Field<sOOrderNbr> { }
		protected String _SOOrderNbr;
		[PXDBString(15, IsUnicode = true)]
		public virtual String SOOrderNbr
		{
			get
			{
				return this._SOOrderNbr;
			}
			set
			{
				this._SOOrderNbr = value;
			}
		}
		#endregion
		#region SOLineNbr
		public abstract class sOLineNbr : PX.Data.BQL.BqlInt.Field<sOLineNbr> { }
		protected Int32? _SOLineNbr;
		[PXDBInt()]
		public virtual Int32? SOLineNbr
		{
			get
			{
				return this._SOLineNbr;
			}
			set
			{
				this._SOLineNbr = value;
			}
		}
		#endregion
		#region SOSplitLineNbr
		public abstract class sOSplitLineNbr : PX.Data.BQL.BqlInt.Field<sOSplitLineNbr> { }
		protected Int32? _SOSplitLineNbr;
		[PXDBInt()]
		public virtual Int32? SOSplitLineNbr
		{
			get
			{
				return this._SOSplitLineNbr;
			}
			set
			{
				this._SOSplitLineNbr = value;
			}
		}
		#endregion
        #region RefNoteID
        public abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }
        protected Guid? _RefNoteID;
        [PXUIField(DisplayName = "Related Document", Enabled = false)]
        [PXRefNote()]
        public virtual Guid? RefNoteID
        {
            get
            {
                return this._RefNoteID;
            }
            set
            {
                this._RefNoteID = value;
            }
        }
        public class PXRefNoteAttribute : Common.PXRefNoteBaseAttribute
        {
            public PXRefNoteAttribute()
                :base()
            { 
            }

            public override void CacheAttached(PXCache sender)
            {
                base.CacheAttached(sender);

                PXButtonDelegate del = delegate(PXAdapter adapter)
                {
                    PXCache cache = adapter.View.Graph.Caches[typeof(SOLineSplit)];
                    if (cache.Current != null)
                    {
                        object val = cache.GetValueExt(cache.Current, _FieldName);

                        PXLinkState state = val as PXLinkState;
                        if (state != null)
                        {
                            helper.NavigateToRow(state.target.FullName, state.keys,
                                PXRedirectHelper.WindowMode.NewWindow);
                        }
                        else
                        {
                            helper.NavigateToRow((Guid?) cache.GetValue(cache.Current, _FieldName),
                                PXRedirectHelper.WindowMode.NewWindow);
                        }
                    }

                    return adapter.Get();
                };

                string ActionName = sender.GetItemType().Name + "$" + _FieldName + "$Link";
                sender.Graph.Actions[ActionName] = (PXAction) Activator.CreateInstance(
                    typeof(PXNamedAction<>).MakeGenericType(typeof(SOOrder)),
                    new object[]
                    {
                        sender.Graph, ActionName, del,
                        new PXEventSubscriberAttribute[]
                            {new PXUIFieldAttribute {MapEnableRights = PXCacheRights.Select}, new PXButtonAttribute() { DisplayOnMainToolbar = false} }
                    });
            }

            public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
            {
                SOLineSplit row = e.Row as SOLineSplit;

                if (row != null && !string.IsNullOrEmpty(row.PONbr))
                {
                    e.ReturnValue = GetEntityRowID(sender.Graph.Caches[typeof(POOrder)], new object[] { row.POType, row.PONbr });
                    e.ReturnState = PXLinkState.CreateInstance(e.ReturnState, typeof(POOrder), new object[] { row.POType, row.PONbr });
                }
                else if (row != null && !string.IsNullOrEmpty(row.ShipmentNbr))
                {
                    e.ReturnValue = GetEntityRowID(sender.Graph.Caches[typeof(SOShipment)], new object[] { row.ShipmentNbr });
                    e.ReturnState = PXLinkState.CreateInstance(e.ReturnState, typeof(SOShipment), new object[] { row.ShipmentNbr });
                }
                else if (row != null && !string.IsNullOrEmpty(row.SOOrderNbr))
                {
                    e.ReturnValue = GetEntityRowID(sender.Graph.Caches[typeof(SOOrder)], new object[] { row.SOOrderType, row.SOOrderNbr });
                    e.ReturnState = PXLinkState.CreateInstance(e.ReturnState, typeof(SOOrder), new object[] { row.SOOrderType, row.SOOrderNbr });
                }
                else if (row != null && !string.IsNullOrEmpty(row.POReceiptNbr))
                {
                    e.ReturnValue = GetEntityRowID(sender.Graph.Caches[typeof(POReceipt)], new object[] { row.POReceiptType, row.POReceiptNbr });
                    e.ReturnState = PXLinkState.CreateInstance(e.ReturnState, typeof(POReceipt), new object[] { row.POReceiptType, row.POReceiptNbr });
                }
                else
                {
                    base.FieldSelecting(sender, e);
                }
            }
        }

        #endregion
		#region PlanID
		public abstract class planID : PX.Data.BQL.BqlLong.Field<planID> { }
		protected Int64? _PlanID;
		[PXDBLong(IsImmutable = true)]
		public virtual Int64? PlanID
		{
			get
			{
				return this._PlanID;
			}
			set
			{
				this._PlanID = value;
			}
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;
		[PXInt]
		public virtual Int32? ProjectID
		{
			get
			{
				return this._ProjectID;
			}
			set
			{
				this._ProjectID = value;
			}
		}
		#endregion
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		protected Int32? _TaskID;
		[PXInt]
		public virtual Int32? TaskID
		{
			get
			{
				return this._TaskID;
			}
			set
			{
				this._TaskID = value;
			}
		}
		#endregion
		bool? ILSMaster.IsIntercompany => false;
		#region AMProdCreate
		public abstract class aMProdCreate : PX.Data.BQL.BqlBool.Field<aMProdCreate> { }

		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Mark for Production", Enabled = false)]
		public Boolean? AMProdCreate { get; set; }
		#endregion

		#region CustomerOrderNbr
		public abstract class customerOrderNbr : Data.BQL.BqlString.Field<customerOrderNbr> { }
		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Customer Order Nbr.")]
		public virtual string CustomerOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region SchedOrderDate
		public abstract class schedOrderDate : Data.BQL.BqlDateTime.Field<schedOrderDate> { }
		[PXDBDate]
		[PXDefault(typeof(IsNull<Current<SOLine.schedOrderDate>, Current<AccessInfo.businessDate>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Sched. Order Date")]
		public virtual DateTime? SchedOrderDate
		{
			get;
			set;
		}
		#endregion
		#region SchedShipDate
		public abstract class schedShipDate : Data.BQL.BqlDateTime.Field<schedShipDate> { }
		[PXDBDate]
		[PXDefault(typeof(SOLine.schedShipDate), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Sched. Shipment Date")]
		public virtual DateTime? SchedShipDate
		{
			get;
			set;
		}
		#endregion
		#region POCreateDate
		public abstract class pOCreateDate : Data.BQL.BqlDateTime.Field<pOCreateDate> { }
		[PXDBDate]
		[PXDefault(typeof(IsNull<Current<SOLine.pOCreateDate>, Current<AccessInfo.businessDate>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "PO Creation Date")]
		public virtual DateTime? POCreateDate
		{
			get;
			set;
		}
		#endregion
		#region QtyOnOrders
		public abstract class qtyOnOrders : Data.BQL.BqlDecimal.Field<qtyOnOrders> { }
		[PXDBQuantity(typeof(uOM), typeof(baseQtyOnOrders))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Orders", Enabled = false)]
		public virtual decimal? QtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region BaseQtyOnOrders
		public abstract class baseQtyOnOrders : Data.BQL.BqlDecimal.Field<baseQtyOnOrders> { }
		[PXDBDecimal(6, MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? BaseQtyOnOrders
		{
			get;
			set;
		}
		#endregion
		#region BlanketOpenQty
		public abstract class blanketOpenQty : Data.BQL.BqlDecimal.Field<blanketOpenQty> { }
		[PXQuantity]
		[PXDBCalced(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>, And<completed, Equal<False>>>, Sub<qty, Add<qtyOnOrders, receivedQty>>>, decimal0>), typeof(decimal))]
		[PXFormula(typeof(Switch<Case<Where<lineType, NotEqual<SOLineType.miscCharge>, And<completed, Equal<False>>>, Sub<qty, Add<qtyOnOrders, receivedQty>>>, decimal0>))]
		[PXDefault]
		[PXUIField(DisplayName = "Blanket Open Qty.", Enabled = false)]
		public virtual decimal? BlanketOpenQty
		{
			get;
			set;
		}
		#endregion
		#region ChildLineCntr
		public abstract class childLineCntr : Data.BQL.BqlInt.Field<childLineCntr> { }
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? ChildLineCntr
		{
			get;
			set;
		}
		#endregion
		#region EffectiveChildLineCntr
		public abstract class effectiveChildLineCntr : Data.BQL.BqlInt.Field<effectiveChildLineCntr> { }
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? EffectiveChildLineCntr
		{
			get;
			set;
		}
		#endregion
		#region OpenChildLineCntr
		public abstract class openChildLineCntr : Data.BQL.BqlInt.Field<openChildLineCntr> { }
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? OpenChildLineCntr
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
}
