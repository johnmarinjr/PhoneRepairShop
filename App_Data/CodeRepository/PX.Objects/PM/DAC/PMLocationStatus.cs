using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	[Serializable]
	[PXCacheName(Messages.PMLocationStatus)]
	public class PMLocationStatus : PX.Data.IBqlTable, IStatus
	{
		#region Keys
		public class PK : PrimaryKeyOf<PMLocationStatus>.By<inventoryID, subItemID, siteID, locationID, projectID, taskID>
		{
			public static PMLocationStatus Find(PXGraph graph, int? inventoryID, int? subItemID, int? siteID, int? locationID, int? projectID, int? taskID)
				=> FindBy(graph, inventoryID, subItemID, siteID, locationID, projectID, taskID);
		}
		public static class FK
		{
			public class Location : INLocation.PK.ForeignKeyOf<PMLocationStatus>.By<locationID> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<PMLocationStatus>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<PMLocationStatus>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<PMLocationStatus>.By<siteID> { }
			public class ItemSite : INItemSite.PK.ForeignKeyOf<PMLocationStatus>.By<inventoryID, siteID> { }
			public class Project : PMProject.PK.ForeignKeyOf<PMLocationStatus>.By<projectID> { }
			public class Task : PMTask.PK.ForeignKeyOf<PMLocationStatus>.By<taskID> { }
		}
		#endregion
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		protected Boolean? _Selected = false;
		[PXBool()]
		[PXUIField(DisplayName = "Selected")]
		public virtual Boolean? Selected
		{
			get
			{
				return this._Selected;
			}
			set
			{
				this._Selected = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[StockItem(IsKey = true)]
		[PXDefault()]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[SubItem(IsKey = true)]
		[PXDefault()]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[Site(IsKey = true)]
		[PXDefault()]
		public virtual Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[Location(IsKey = true)]
		[PXDefault()]
		[PXForeignReference(typeof(FK.Location))]
		public virtual Int32? LocationID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
		{
		}
		[PXDefault]
		[Project(IsKey = true)]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		public virtual Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID>
		{
		}
		[PXDefault]
		[BaseProjectTask(typeof(projectID), AllowInactive = true, IsKey = true)]
		[PXForeignReference(typeof(Field<taskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual Int32? TaskID
		{
			get;
			set;
		}
		#endregion
		#region Active
		public abstract class active : PX.Data.BQL.BqlBool.Field<active> { }
		[PXExistance()]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? Active
		{
			get;
			set;
		}
		#endregion
		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Hand")]
		public virtual Decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region QtyAvail
		public abstract class qtyAvail : PX.Data.BQL.BqlDecimal.Field<qtyAvail> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available")]
		public virtual Decimal? QtyAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyNotAvail
		public abstract class qtyNotAvail : PX.Data.BQL.BqlDecimal.Field<qtyNotAvail> { }
		[PXDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? QtyNotAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyExpired
		public abstract class qtyExpired : PX.Data.BQL.BqlDecimal.Field<qtyExpired> { }
		[PXDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? QtyExpired
		{
			get;
			set;
		}
		#endregion
		#region QtyHardAvail
		public abstract class qtyHardAvail : PX.Data.BQL.BqlDecimal.Field<qtyHardAvail> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Hard Available")]
		public virtual Decimal? QtyHardAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyActual
		public abstract class qtyActual : PX.Data.BQL.BqlDecimal.Field<qtyActual> { }
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available for Issue")]
		public virtual decimal? QtyActual
		{
			get;
			set;
		}
		#endregion
		#region QtyInTransit
		public abstract class qtyInTransit : PX.Data.BQL.BqlDecimal.Field<qtyInTransit> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. In-Transit")]
		public virtual Decimal? QtyInTransit
		{
			get;
			set;
		}
		#endregion
		#region QtyInTransitToSO
		public abstract class qtyInTransitToSO : PX.Data.BQL.BqlDecimal.Field<qtyInTransitToSO> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? QtyInTransitToSO
		{
			get;
			set;
		}
		#endregion
		#region QtyINReplaned
		public decimal? QtyINReplaned
		{
			get { return 0m; }
			set { }
		}
		#endregion
		#region QtyPOPrepared
		public abstract class qtyPOPrepared : PX.Data.BQL.BqlDecimal.Field<qtyPOPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? QtyPOPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyPOOrders
		public abstract class qtyPOOrders : PX.Data.BQL.BqlDecimal.Field<qtyPOOrders> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Purchase Orders")]
		public virtual Decimal? QtyPOOrders
		{
			get;
			set;
		}
		#endregion
		#region QtyPOReceipts
		public abstract class qtyPOReceipts : PX.Data.BQL.BqlDecimal.Field<qtyPOReceipts> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Purchase Receipts")]
		public virtual Decimal? QtyPOReceipts
		{
			get;
			set;
		}
		#endregion

		#region QtyFSSrvOrdBooked
		public abstract class qtyFSSrvOrdBooked : PX.Data.BQL.BqlDecimal.Field<qtyFSSrvOrdBooked> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. FS Booked", FieldClass = "SERVICEMANAGEMENT")]
		public virtual Decimal? QtyFSSrvOrdBooked
		{
			get;
			set;
		}
		#endregion
		#region QtyFSSrvOrdAllocated
		public abstract class qtyFSSrvOrdAllocated : PX.Data.BQL.BqlDecimal.Field<qtyFSSrvOrdAllocated> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. FS Allocated", FieldClass = "SERVICEMANAGEMENT")]
		public virtual Decimal? QtyFSSrvOrdAllocated
		{
			get;
			set;
		}
		#endregion
		#region QtyFSSrvOrdPrepared
		public abstract class qtyFSSrvOrdPrepared : PX.Data.BQL.BqlDecimal.Field<qtyFSSrvOrdPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. FS Prepared", FieldClass = "SERVICEMANAGEMENT")]
		public virtual Decimal? QtyFSSrvOrdPrepared
		{
			get;
			set;
		}
		#endregion

		#region QtySOBackOrdered
		public abstract class qtySOBackOrdered : PX.Data.BQL.BqlDecimal.Field<qtySOBackOrdered> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? QtySOBackOrdered
		{
			get;
			set;
		}
		#endregion
		#region QtySOPrepared
		public abstract class qtySOPrepared : PX.Data.BQL.BqlDecimal.Field<qtySOPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? QtySOPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtySOBooked
		public abstract class qtySOBooked : PX.Data.BQL.BqlDecimal.Field<qtySOBooked> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. SO Booked")]
		public virtual Decimal? QtySOBooked
		{
			get;
			set;
		}
		#endregion
		#region QtySOShipped
		public abstract class qtySOShipped : PX.Data.BQL.BqlDecimal.Field<qtySOShipped> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. SO Shipped")]
		public virtual Decimal? QtySOShipped
		{
			get;
			set;
		}
		#endregion
		#region QtySOShipping
		public abstract class qtySOShipping : PX.Data.BQL.BqlDecimal.Field<qtySOShipping> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. SO Shipping")]
		public virtual Decimal? QtySOShipping
		{
			get;
			set;
		}
		#endregion
		#region QtyINIssues
		public abstract class qtyINIssues : PX.Data.BQL.BqlDecimal.Field<qtyINIssues> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Inventory Issues")]
		public virtual Decimal? QtyINIssues
		{
			get;
			set;
		}
		#endregion
		#region QtyINReceipts
		public abstract class qtyINReceipts : PX.Data.BQL.BqlDecimal.Field<qtyINReceipts> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Inventory Receipts")]
		public virtual Decimal? QtyINReceipts
		{
			get;
			set;
		}
		#endregion
		#region QtyINAssemblyDemand
		public abstract class qtyINAssemblyDemand : PX.Data.BQL.BqlDecimal.Field<qtyINAssemblyDemand> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty Demanded by Kit Assembly")]
		public virtual Decimal? QtyINAssemblyDemand
		{
			get;
			set;
		}
		#endregion
		#region QtyINAssemblySupply
		public abstract class qtyINAssemblySupply : PX.Data.BQL.BqlDecimal.Field<qtyINAssemblySupply> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Kit Assembly")]
		public virtual Decimal? QtyINAssemblySupply
		{
			get;
			set;
		}
		#endregion
		#region QtyInTransitToProduction
		public abstract class qtyInTransitToProduction : PX.Data.BQL.BqlDecimal.Field<qtyInTransitToProduction> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity In Transit to Production.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty In Transit to Production")]
		public virtual Decimal? QtyInTransitToProduction
		{
			get;
			set;
		}
		#endregion
		#region QtyProductionSupplyPrepared
		public abstract class qtyProductionSupplyPrepared : PX.Data.BQL.BqlDecimal.Field<qtyProductionSupplyPrepared> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity Production Supply Prepared.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty Production Supply Prepared")]
		public virtual Decimal? QtyProductionSupplyPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyProductionSupply
		public abstract class qtyProductionSupply : PX.Data.BQL.BqlDecimal.Field<qtyProductionSupply> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity Production Supply.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production Supply")]
		public virtual Decimal? QtyProductionSupply
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedProductionPrepared
		public abstract class qtyPOFixedProductionPrepared : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedProductionPrepared> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Purchase for Prod. Prepared.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Purchase for Prod. Prepared")]
		public virtual Decimal? QtyPOFixedProductionPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedProductionOrders
		public abstract class qtyPOFixedProductionOrders : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedProductionOrders> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Purchase for Production.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Purchase for Production")]
		public virtual Decimal? QtyPOFixedProductionOrders
		{
			get;
			set;
		}
		#endregion
		#region QtyProductionDemandPrepared
		public abstract class qtyProductionDemandPrepared : PX.Data.BQL.BqlDecimal.Field<qtyProductionDemandPrepared> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production Demand Prepared.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production Demand Prepared")]
		public virtual Decimal? QtyProductionDemandPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyProductionDemand
		public abstract class qtyProductionDemand : PX.Data.BQL.BqlDecimal.Field<qtyProductionDemand> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production Demand.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production Demand")]
		public virtual Decimal? QtyProductionDemand
		{
			get;
			set;
		}
		#endregion
		#region QtyProductionAllocated
		public abstract class qtyProductionAllocated : PX.Data.BQL.BqlDecimal.Field<qtyProductionAllocated> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production Allocated.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production Allocated")]
		public virtual Decimal? QtyProductionAllocated
		{
			get;
			set;
		}
		#endregion
		#region QtySOFixedProduction
		public abstract class qtySOFixedProduction : PX.Data.BQL.BqlDecimal.Field<qtySOFixedProduction> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On SO to Production.  
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On SO to Production")]
		public virtual Decimal? QtySOFixedProduction
		{
			get;
			set;
		}
		#endregion

		#region QtyFixedFSSrvOrd
		public abstract class qtyFixedFSSrvOrd : PX.Data.BQL.BqlDecimal.Field<qtyFixedFSSrvOrd> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyFixedFSSrvOrd
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedFSSrvOrd
		public abstract class qtyPOFixedFSSrvOrd : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedFSSrvOrd> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedFSSrvOrd
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedFSSrvOrdPrepared
		public abstract class qtyPOFixedFSSrvOrdPrepared : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedFSSrvOrdPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedFSSrvOrdPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedFSSrvOrdReceipts
		public abstract class qtyPOFixedFSSrvOrdReceipts : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedFSSrvOrdReceipts> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedFSSrvOrdReceipts
		{
			get;
			set;
		}
		#endregion

		#region QtyProdFixedPurchase
		// M9
		public abstract class qtyProdFixedPurchase : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedPurchase> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production to Purchase.  
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production to Purchase", Enabled = false)]
		public virtual Decimal? QtyProdFixedPurchase
		{
			get;
			set;
		}
		#endregion
		#region QtyProdFixedProduction
		// MA
		public abstract class qtyProdFixedProduction : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedProduction> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production to Production
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production to Production", Enabled = false)]
		public virtual Decimal? QtyProdFixedProduction
		{
			get;
			set;
		}
		#endregion
		#region QtyProdFixedProdOrdersPrepared
		// MB
		public abstract class qtyProdFixedProdOrdersPrepared : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedProdOrdersPrepared> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production for Prod. Prepared
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production for Prod. Prepared", Enabled = false)]
		public virtual Decimal? QtyProdFixedProdOrdersPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyProdFixedProdOrders
		// MC
		public abstract class qtyProdFixedProdOrders : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedProdOrders> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production for Production
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production for Production", Enabled = false)]
		public virtual Decimal? QtyProdFixedProdOrders
		{
			get;
			set;
		}
		#endregion
		#region QtyProdFixedSalesOrdersPrepared
		// MD
		public abstract class qtyProdFixedSalesOrdersPrepared : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedSalesOrdersPrepared> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production for SO Prepared
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production for SO Prepared", Enabled = false)]
		public virtual Decimal? QtyProdFixedSalesOrdersPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyProdFixedSalesOrders
		// ME
		public abstract class qtyProdFixedSalesOrders : PX.Data.BQL.BqlDecimal.Field<qtyProdFixedSalesOrders> { }
		/// <summary>
		/// Production / Manufacturing 
		/// Specifies the quantity On Production for SO
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty On Production for SO", Enabled = false)]
		public virtual Decimal? QtyProdFixedSalesOrders
		{
			get;
			set;
		}
		#endregion
		#region QtySOFixed
		public abstract class qtySOFixed : PX.Data.BQL.BqlDecimal.Field<qtySOFixed> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtySOFixed
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedOrders
		public abstract class qtyPOFixedOrders : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedOrders> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedOrders
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedPrepared
		public abstract class qtyPOFixedPrepared : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyPOFixedReceipts
		public abstract class qtyPOFixedReceipts : PX.Data.BQL.BqlDecimal.Field<qtyPOFixedReceipts> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPOFixedReceipts
		{
			get;
			set;
		}
		#endregion
		#region QtySODropShip
		public abstract class qtySODropShip : PX.Data.BQL.BqlDecimal.Field<qtySODropShip> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtySODropShip
		{
			get;
			set;
		}
		#endregion
		#region QtyPODropShipOrders
		public abstract class qtyPODropShipOrders : PX.Data.BQL.BqlDecimal.Field<qtyPODropShipOrders> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPODropShipOrders
		{
			get;
			set;
		}
		#endregion
		#region QtyPODropShipPrepared
		public abstract class qtyPODropShipPrepared : PX.Data.BQL.BqlDecimal.Field<qtyPODropShipPrepared> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPODropShipPrepared
		{
			get;
			set;
		}
		#endregion
		#region QtyPODropShipReceipts
		public abstract class qtyPODropShipReceipts : PX.Data.BQL.BqlDecimal.Field<qtyPODropShipReceipts> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyPODropShipReceipts
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}

	[PXProjection(typeof(Select4<PMLocationStatus,
		Where<PMLocationStatus.taskID, NotEqual<int0>>,
		Aggregate<Sum<PMLocationStatus.qtyOnHand,
			Sum<PMLocationStatus.qtyNotAvail,
			Sum<PMLocationStatus.qtyHardAvail,
			Sum<PMLocationStatus.qtyActual,
			GroupBy<PMLocationStatus.inventoryID,
			GroupBy<PMLocationStatus.subItemID,
			GroupBy<PMLocationStatus.siteID,
			GroupBy<PMLocationStatus.locationID>>>>>>>>>>), Persistent = false)]
    [PXHidden]
    public class PMLocationStatusProject : IBqlTable
    {
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[StockItem(IsKey = true, BqlField = typeof(PMLocationStatus.inventoryID))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[SubItem(IsKey = true, BqlField = typeof(PMLocationStatus.subItemID))]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[Site(IsKey = true, BqlField = typeof(PMLocationStatus.siteID))]
		public virtual Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[Location(IsKey = true, BqlField = typeof(PMLocationStatus.locationID))]
		public virtual Int32? LocationID
		{
			get;
			set;
		}
		#endregion

		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Hand")]
		public virtual Decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region QtyAvail
		public abstract class qtyAvail : PX.Data.BQL.BqlDecimal.Field<qtyAvail> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available")]
		public virtual Decimal? QtyAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyHardAvail
		public abstract class qtyHardAvail : PX.Data.BQL.BqlDecimal.Field<qtyHardAvail> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyHardAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Hard Available")]
		public virtual Decimal? QtyHardAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyActual
		public abstract class qtyActual : PX.Data.BQL.BqlDecimal.Field<qtyActual> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyActual))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available for Issue")]
		public virtual decimal? QtyActual
		{
			get;
			set;
		}
		#endregion
	}

	[PXProjection(typeof(Select4<PMLocationStatus,
		Where<PMLocationStatus.taskID, Equal<int0>>,
		Aggregate<Sum<PMLocationStatus.qtyOnHand,
			Sum<PMLocationStatus.qtyNotAvail,
			Sum<PMLocationStatus.qtyHardAvail,
			Sum<PMLocationStatus.qtyActual,
			GroupBy<PMLocationStatus.inventoryID,
			GroupBy<PMLocationStatus.subItemID,
			GroupBy<PMLocationStatus.siteID,
			GroupBy<PMLocationStatus.locationID>>>>>>>>>>), Persistent = false)]
    [PXHidden]
    public class PMLocationStatusNonProject : IBqlTable
	{
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[StockItem(IsKey = true, BqlField = typeof(PMLocationStatus.inventoryID))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[SubItem(IsKey = true, BqlField = typeof(PMLocationStatus.subItemID))]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[Site(IsKey = true, BqlField = typeof(PMLocationStatus.siteID))]
		public virtual Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[Location(IsKey = true, BqlField = typeof(PMLocationStatus.locationID))]
		public virtual Int32? LocationID
		{
			get;
			set;
		}
		#endregion

		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Hand")]
		public virtual Decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region QtyAvail
		public abstract class qtyAvail : PX.Data.BQL.BqlDecimal.Field<qtyAvail> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available")]
		public virtual Decimal? QtyAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyHardAvail
		public abstract class qtyHardAvail : PX.Data.BQL.BqlDecimal.Field<qtyHardAvail> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyHardAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Hard Available")]
		public virtual Decimal? QtyHardAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyActual
		public abstract class qtyActual : PX.Data.BQL.BqlDecimal.Field<qtyActual> { }
		[PXDBQuantity(BqlField = typeof(PMLocationStatus.qtyActual))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available for Issue")]
		public virtual decimal? QtyActual
		{
			get;
			set;
		}
		#endregion
	}
}
