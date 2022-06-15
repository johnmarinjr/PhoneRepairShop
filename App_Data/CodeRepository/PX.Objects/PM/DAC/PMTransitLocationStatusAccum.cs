using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.Overrides.INDocumentRelease;
using PX.Objects.SO;

namespace PX.Objects.PM
{
    [PXHidden]
    [PMTransitLocationStatusAccumulator]
    [Serializable]
    public partial class PMTransitLocationStatusAccum : PMLocationStatus, IQtyAllocated
    {
        #region Keys
        public class PK : PrimaryKeyOf<PMTransitLocationStatusAccum>.By<inventoryID, subItemID, siteID, locationID, projectID, taskID>
        {
            public static PMTransitLocationStatusAccum Find(PXGraph graph, int? inventoryID, int? subItemID, int? siteID, int? locationID, int? projectID, int? taskID)
                => FindBy(graph, inventoryID, subItemID, siteID, locationID, projectID, taskID);
        }
        public static class FK
        {
            public class Location : INLocation.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<locationID> { }
            public class LocationStatus : INLocationStatus.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<inventoryID, subItemID, siteID, locationID> { }

            public class ProjectLocationStatus : PMLocationStatus.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<inventoryID, subItemID, siteID, locationID, projectID, taskID> { }

            public class SubItem : INSubItem.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<subItemID> { }
            public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<inventoryID> { }
           
            public class Site : INSite.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<siteID> { }
            public class Project : PMProject.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<PMTransitLocationStatusAccum>.By<taskID> { }
        }
        #endregion
        #region InventoryID
        public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
        [PXDBInt(IsKey = true)]
        [PXForeignSelector(typeof(INTran.inventoryID))]
        [PXSelectorMarker(typeof(Search<InventoryItem.inventoryID, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>), CacheGlobal = true)]
        [PXDefault()]
        public override Int32? InventoryID
        {
            get;
            set;
        }
        #endregion
        #region SubItemID
        public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
        [SubItem(IsKey = true)]
        [PXForeignSelector(typeof(INTran.subItemID))]
        [PXDefault()]
        public override Int32? SubItemID
        {
            get;
            set;
        }
        #endregion
        #region SiteID
        public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
        //[PXDBInt(IsKey = true)]
        [IN.Site(true, IsKey = true)]
        [PXDefault()]
        public override Int32? SiteID
        {
            get;
            set;
        }
        #endregion
        #region LocationID
        public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
        [PXDBInt(IsKey = true)]
        [PXDBDefault(typeof(INTransitLine.costSiteID))]
        public override Int32? LocationID
        {
            get;
            set;
        }
        #endregion
        #region ProjectID
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
        {
        }
        [PXDefault]
        [PXDBInt(IsKey = true)]
        public override Int32? ProjectID
        {
            get;
            set;
        }
        #endregion
        #region TaskID
        public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID>
        {
        }
        [PXDefault]
        [PXDBInt(IsKey = true)]
        public override Int32? TaskID
        {
            get;
            set;
        }
        #endregion
        #region ItemClassID
        public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
        protected int? _ItemClassID;
        [PXInt]
        [PXFormula(typeof(Selector<inventoryID, InventoryItem.itemClassID>))]
        [PXSelectorMarker(typeof(Search<INItemClass.itemClassID, Where<INItemClass.itemClassID, Equal<Current<itemClassID>>>>), CacheGlobal = true)]
        public virtual int? ItemClassID
        {
            get
            {
                return this._ItemClassID;
            }
            set
            {
                this._ItemClassID = value;
            }
        }
        #endregion
        #region LotSerClassID
        public abstract class lotSerClassID : IBqlField { }
        [PXString(10, IsUnicode = true)]
        [PXDefault(typeof(Select<InventoryItem, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>),
            SourceField = typeof(InventoryItem.lotSerClassID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string LotSerClassID
        {
            get;
            set;
        }
        #endregion
        #region QtyOnHand
        public new abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
        #endregion
        #region QtyNotAvail
        public new abstract class qtyNotAvail : PX.Data.BQL.BqlDecimal.Field<qtyNotAvail> { }
        #endregion
        #region NegQty
        public abstract class negQty : PX.Data.BQL.BqlBool.Field<negQty> { }
        protected bool? _NegQty;
        [PXBool()]
        [PXFormula(typeof(Selector<itemClassID, INItemClass.negQty>))]
        public virtual bool? NegQty
        {
            get
            {
                return this._NegQty;
            }
            set
            {
                this._NegQty = value;
            }
        }
        #endregion
        #region InclQtyAvail
        public abstract class inclQtyAvail : PX.Data.BQL.BqlBool.Field<inclQtyAvail> { }
        protected Boolean? _InclQtyAvail;
        [PXBool()]
        [PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyAvail
        {
            get
            {
                return this._InclQtyAvail;
            }
            set
            {
                this._InclQtyAvail = value;
            }
        }
        #endregion
        #region AvailabilitySchemeID
        public abstract class availabilitySchemeID : PX.Data.BQL.BqlString.Field<availabilitySchemeID> { }
        [PXString(10, IsUnicode = true)]
        [PXDefault(typeof(Select<INItemClass, Where<INItemClass.itemClassID, Equal<Current<itemClassID>>>>),
            CacheGlobal = true, SourceField = typeof(INItemClass.availabilitySchemeID), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string AvailabilitySchemeID { get; set; }
        #endregion

        #region InclQtyFSSrvOrdPrepared
        public abstract class inclQtyFSSrvOrdPrepared : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdPrepared> { }
        protected Boolean? _InclQtyFSSrvOrdPrepared;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdPrepared
        {
            get
            {
                return this._InclQtyFSSrvOrdPrepared;
            }
            set
            {
                this._InclQtyFSSrvOrdPrepared = value;
            }
        }
        #endregion
        #region InclQtyFSSrvOrdBooked
        public abstract class inclQtyFSSrvOrdBooked : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdBooked> { }
        protected Boolean? _InclQtyFSSrvOrdBooked;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdBooked), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdBooked
        {
            get
            {
                return this._InclQtyFSSrvOrdBooked;
            }
            set
            {
                this._InclQtyFSSrvOrdBooked = value;
            }
        }
        #endregion
        #region InclQtyFSSrvOrdAllocated
        public abstract class inclQtyFSSrvOrdAllocated : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdAllocated> { }
        protected Boolean? _InclQtyFSSrvOrdAllocated;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdAllocated), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdAllocated
        {
            get
            {
                return this._InclQtyFSSrvOrdAllocated;
            }
            set
            {
                this._InclQtyFSSrvOrdAllocated = value;
            }
        }
        #endregion

        #region InclQtySOReverse
        public abstract class inclQtySOReverse : PX.Data.BQL.BqlBool.Field<inclQtySOReverse> { }
        protected Boolean? _InclQtySOReverse;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOReverse), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOReverse
        {
            get
            {
                return this._InclQtySOReverse;
            }
            set
            {
                this._InclQtySOReverse = value;
            }
        }
        #endregion
        #region InclQtySOBackOrdered
        public abstract class inclQtySOBackOrdered : PX.Data.BQL.BqlBool.Field<inclQtySOBackOrdered> { }
        protected Boolean? _InclQtySOBackOrdered;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOBackOrdered), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOBackOrdered
        {
            get
            {
                return this._InclQtySOBackOrdered;
            }
            set
            {
                this._InclQtySOBackOrdered = value;
            }
        }
        #endregion
        #region InclQtySOPrepared
        public abstract class inclQtySOPrepared : PX.Data.BQL.BqlBool.Field<inclQtySOPrepared> { }
        protected Boolean? _InclQtySOPrepared;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOPrepared
        {
            get
            {
                return this._InclQtySOPrepared;
            }
            set
            {
                this._InclQtySOPrepared = value;
            }
        }
        #endregion
        #region InclQtySOBooked
        public abstract class inclQtySOBooked : PX.Data.BQL.BqlBool.Field<inclQtySOBooked> { }
        protected Boolean? _InclQtySOBooked;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOBooked), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOBooked
        {
            get
            {
                return this._InclQtySOBooked;
            }
            set
            {
                this._InclQtySOBooked = value;
            }
        }
        #endregion
        #region InclQtySOShipped
        public abstract class inclQtySOShipped : PX.Data.BQL.BqlBool.Field<inclQtySOShipped> { }
        protected Boolean? _InclQtySOShipped;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOShipped), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOShipped
        {
            get
            {
                return this._InclQtySOShipped;
            }
            set
            {
                this._InclQtySOShipped = value;
            }
        }
        #endregion
        #region InclQtySOShipping
        public abstract class inclQtySOShipping : PX.Data.BQL.BqlBool.Field<inclQtySOShipping> { }
        protected Boolean? _InclQtySOShipping;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOShipping), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOShipping
        {
            get
            {
                return this._InclQtySOShipping;
            }
            set
            {
                this._InclQtySOShipping = value;
            }
        }
        #endregion
        #region InclQtyInTransit
        public abstract class inclQtyInTransit : PX.Data.BQL.BqlBool.Field<inclQtyInTransit> { }
        protected Boolean? _InclQtyInTransit;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyInTransit), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyInTransit
        {
            get
            {
                return this._InclQtyInTransit;
            }
            set
            {
                this._InclQtyInTransit = value;
            }
        }
        #endregion
        #region InclQtyPOReceipts
        public abstract class inclQtyPOReceipts : PX.Data.BQL.BqlBool.Field<inclQtyPOReceipts> { }
        protected Boolean? _InclQtyPOReceipts;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOReceipts), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOReceipts
        {
            get
            {
                return this._InclQtyPOReceipts;
            }
            set
            {
                this._InclQtyPOReceipts = value;
            }
        }
        #endregion
        #region InclQtyPOPrepared
        public abstract class inclQtyPOPrepared : PX.Data.BQL.BqlBool.Field<inclQtyPOPrepared> { }
        protected Boolean? _InclQtyPOPrepared;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOPrepared
        {
            get
            {
                return this._InclQtyPOPrepared;
            }
            set
            {
                this._InclQtyPOPrepared = value;
            }
        }
        #endregion
        #region InclQtyPOOrders
        public abstract class inclQtyPOOrders : PX.Data.BQL.BqlBool.Field<inclQtyPOOrders> { }
        protected Boolean? _InclQtyPOOrders;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOOrders), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOOrders
        {
            get
            {
                return this._InclQtyPOOrders;
            }
            set
            {
                this._InclQtyPOOrders = value;
            }
        }
        #endregion
        #region InclQtyINIssues
        public abstract class inclQtyINIssues : PX.Data.BQL.BqlBool.Field<inclQtyINIssues> { }
        protected Boolean? _InclQtyINIssues;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINIssues), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINIssues
        {
            get
            {
                return this._InclQtyINIssues;
            }
            set
            {
                this._InclQtyINIssues = value;
            }
        }
        #endregion
        #region InclQtyINReceipts
        public abstract class inclQtyINReceipts : PX.Data.BQL.BqlBool.Field<inclQtyINReceipts> { }
        protected Boolean? _InclQtyINReceipts;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINReceipts), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINReceipts
        {
            get
            {
                return this._InclQtyINReceipts;
            }
            set
            {
                this._InclQtyINReceipts = value;
            }
        }
        #endregion
        #region InclQtyINAssemblyDemand
        public abstract class inclQtyINAssemblyDemand : PX.Data.BQL.BqlBool.Field<inclQtyINAssemblyDemand> { }
        protected Boolean? _InclQtyINAssemblyDemand;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINAssemblyDemand), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINAssemblyDemand
        {
            get
            {
                return this._InclQtyINAssemblyDemand;
            }
            set
            {
                this._InclQtyINAssemblyDemand = value;
            }
        }
        #endregion
        #region InclQtyINAssemblySupply
        public abstract class inclQtyINAssemblySupply : PX.Data.BQL.BqlBool.Field<inclQtyINAssemblySupply> { }
        protected Boolean? _InclQtyINAssemblySupply;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINAssemblySupply), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINAssemblySupply
        {
            get
            {
                return this._InclQtyINAssemblySupply;
            }
            set
            {
                this._InclQtyINAssemblySupply = value;
            }
        }
        #endregion
        #region InclQtyProductionDemandPrepared
        public abstract class inclQtyProductionDemandPrepared : PX.Data.BQL.BqlBool.Field<inclQtyProductionDemandPrepared> { }
        protected Boolean? _InclQtyProductionDemandPrepared;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionDemandPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionDemandPrepared
        {
            get
            {
                return this._InclQtyProductionDemandPrepared;
            }
            set
            {
                this._InclQtyProductionDemandPrepared = value;
            }
        }
        #endregion
        #region InclQtyProductionDemand
        public abstract class inclQtyProductionDemand : PX.Data.BQL.BqlBool.Field<inclQtyProductionDemand> { }
        protected Boolean? _InclQtyProductionDemand;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionDemand), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionDemand
        {
            get
            {
                return this._InclQtyProductionDemand;
            }
            set
            {
                this._InclQtyProductionDemand = value;
            }
        }
        #endregion
        #region InclQtyProductionAllocated
        public abstract class inclQtyProductionAllocated : PX.Data.BQL.BqlBool.Field<inclQtyProductionAllocated> { }
        protected Boolean? _InclQtyProductionAllocated;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionAllocated), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionAllocated
        {
            get
            {
                return this._InclQtyProductionAllocated;
            }
            set
            {
                this._InclQtyProductionAllocated = value;
            }
        }
        #endregion
        #region InclQtyProductionSupplyPrepared
        public abstract class inclQtyProductionSupplyPrepared : PX.Data.BQL.BqlBool.Field<inclQtyProductionSupplyPrepared> { }
        protected Boolean? _InclQtyProductionSupplyPrepared;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionSupplyPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionSupplyPrepared
        {
            get
            {
                return this._InclQtyProductionSupplyPrepared;
            }
            set
            {
                this._InclQtyProductionSupplyPrepared = value;
            }
        }
        #endregion
        #region InclQtyProductionSupply
        public abstract class inclQtyProductionSupply : PX.Data.BQL.BqlBool.Field<inclQtyProductionSupply> { }
        protected Boolean? _InclQtyProductionSupply;
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionSupply), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionSupply
        {
            get
            {
                return this._InclQtyProductionSupply;
            }
            set
            {
                this._InclQtyProductionSupply = value;
            }
        }
        #endregion
        #region InclQtyPOFixedReceipt
        public abstract class inclQtyPOFixedReceipt : PX.Data.BQL.BqlBool.Field<inclQtyPOFixedReceipt> { }
        protected Boolean? _InclQtyPOFixedReceipt;
        [PXBool()]
        [PXDefault(typeof(False), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOFixedReceipt
        {
            get
            {
                return this._InclQtyPOFixedReceipt;
            }
            set
            {
                this._InclQtyPOFixedReceipt = value;
            }
        }
        #endregion
    }
}
