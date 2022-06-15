using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.Overrides.INDocumentRelease;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    [PMSiteSummaryStatusAccumulatorAttribute]
    [Serializable]
    [PXCacheName(Messages.PMSiteStatus)]
    public class PMSiteSummaryStatusAccum : PMSiteSummaryStatus, IQtyAllocated, ICostStatus
    {
        #region Keys
        public new class PK : PrimaryKeyOf<PMSiteSummaryStatusAccum>.By<inventoryID, subItemID, siteID, projectID>
        {
            public static PMSiteSummaryStatusAccum Find(PXGraph graph, int? inventoryID, int? subItemID, int? siteID, int? projectID)
                => FindBy(graph, inventoryID, subItemID, siteID, projectID);
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
        [PXForeignSelector(typeof(INTran.siteID))]
        [IN.Site(IsKey = true)]
        [PXRestrictor(typeof(Where<True, Equal<True>>), "", ReplaceInherited = true)]
        [PXDefault()]
        public override Int32? SiteID
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
        #region ItemClassID
        public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
        protected int? _ItemClassID;
        [PXInt]
        [PXFormula(typeof(Selector<inventoryID, InventoryItem.itemClassID>))]
        [PXSelectorMarker(typeof(Search<INItemClass.itemClassID, Where<INItemClass.itemClassID, Equal<Current<itemClassID>>>>), CacheGlobal = true)]
        public virtual int? ItemClassID
        {
            get;
            set;
        }
        #endregion
        #region QtyOnHand
        public new abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
        #endregion
        #region TotalCost
        public abstract class totalCost : PX.Data.BQL.BqlDecimal.Field<totalCost> { }
        [PXDecimal(4)]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Decimal? TotalCost
        {
            get;
            set;
        }
        #endregion
        #region QtyNotAvail
        public new abstract class qtyNotAvail : PX.Data.BQL.BqlDecimal.Field<qtyNotAvail> { }
        #endregion
        #region NegQty
        public abstract class negQty : PX.Data.BQL.BqlBool.Field<negQty> { }
        [PXBool()]
        [PXFormula(typeof(Selector<itemClassID, INItemClass.negQty>))]
        public virtual bool? NegQty
        {
            get;
            set;
        }
        #endregion
        #region NegAvailQty
        public abstract class negAvailQty : PX.Data.BQL.BqlBool.Field<negAvailQty> { }
        protected bool? _NegAvailQty;
        [PXBool()]
        [PXDefault(typeof(Select<INItemClass, Where<INItemClass.itemClassID, Equal<Current<itemClassID>>>>), CacheGlobal = true, SourceField = typeof(INItemClass.negQty), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual bool? NegAvailQty
        {
            get
            {
                return this._NegAvailQty;
            }
            set
            {
                this._NegAvailQty = value;
            }
        }
        #endregion
        #region InclQtyAvail
        public abstract class inclQtyAvail : PX.Data.BQL.BqlBool.Field<inclQtyAvail> { }
        [PXBool()]
        [PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyAvail
        {
            get;
            set;
        }
        #endregion
        #region AvailabilitySchemeID
        public abstract class availabilitySchemeID : PX.Data.BQL.BqlString.Field<availabilitySchemeID> { }
        [PXString(10, IsUnicode = true)]
        [PXDefault(typeof(Select<INItemClass, Where<INItemClass.itemClassID, Equal<Current<itemClassID>>>>),
            CacheGlobal = true, SourceField = typeof(INItemClass.availabilitySchemeID), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string AvailabilitySchemeID { get; set; }
        #endregion
        #region RelatedPIID
        public abstract class relatedPIID : PX.Data.BQL.BqlString.Field<relatedPIID> { }
        [PXString(IsUnicode = true)]
        public virtual String RelatedPIID
        {
            get;
            set;
        }
        #endregion

        #region InclQtyFSSrvOrdAllocated
        public abstract class inclQtyFSSrvOrdPrepared : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdPrepared> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdPrepared
        {
            get;
            set;
        }
        #endregion
        #region InclQtyFSSrvOrdBooked
        public abstract class inclQtyFSSrvOrdBooked : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdBooked> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdBooked), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdBooked
        {
            get;
            set;
        }
        #endregion
        #region InclQtyFSSrvOrdAllocated
        public abstract class inclQtyFSSrvOrdAllocated : PX.Data.BQL.BqlBool.Field<inclQtyFSSrvOrdAllocated> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyFSSrvOrdAllocated), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyFSSrvOrdAllocated
        {
            get;
            set;
        }
        #endregion

        #region InclQtySOReverse
        public abstract class inclQtySOReverse : PX.Data.BQL.BqlBool.Field<inclQtySOReverse> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOReverse), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOReverse
        {
            get;
            set;
        }
        #endregion
        #region InclQtySOBackOrdered
        public abstract class inclQtySOBackOrdered : PX.Data.BQL.BqlBool.Field<inclQtySOBackOrdered> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOBackOrdered), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOBackOrdered
        {
            get;
            set;
        }
        #endregion
        #region InclQtySOPrepared
        public abstract class inclQtySOPrepared : PX.Data.BQL.BqlBool.Field<inclQtySOPrepared> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOPrepared
        {
            get;
            set;
        }
        #endregion
        #region InclQtySOBooked
        public abstract class inclQtySOBooked : PX.Data.BQL.BqlBool.Field<inclQtySOBooked> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOBooked), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOBooked
        {
            get;
            set;
        }
        #endregion
        #region InclQtySOShipped
        public abstract class inclQtySOShipped : PX.Data.BQL.BqlBool.Field<inclQtySOShipped> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOShipped), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOShipped
        {
            get;
            set;
        }
        #endregion
        #region InclQtySOShipping
        public abstract class inclQtySOShipping : PX.Data.BQL.BqlBool.Field<inclQtySOShipping> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtySOShipping), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtySOShipping
        {
            get;
            set;
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
            get;
            set;
        }
        #endregion
        #region InclQtyPOReceipts
        public abstract class inclQtyPOReceipts : PX.Data.BQL.BqlBool.Field<inclQtyPOReceipts> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOReceipts), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOReceipts
        {
            get;
            set;
        }
        #endregion
        #region InclQtyPOPrepared
        public abstract class inclQtyPOPrepared : PX.Data.BQL.BqlBool.Field<inclQtyPOPrepared> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOPrepared
        {
            get;
            set;
        }
        #endregion
        #region InclQtyPOOrders
        public abstract class inclQtyPOOrders : PX.Data.BQL.BqlBool.Field<inclQtyPOOrders> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyPOOrders), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOOrders
        {
            get;
            set;
        }
        #endregion
        #region InclQtyINIssues
        public abstract class inclQtyINIssues : PX.Data.BQL.BqlBool.Field<inclQtyINIssues> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINIssues), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINIssues
        {
            get;
            set;
        }
        #endregion
        #region InclQtyINReceipts
        public abstract class inclQtyINReceipts : PX.Data.BQL.BqlBool.Field<inclQtyINReceipts> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINReceipts), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINReceipts
        {
            get;
            set;
        }
        #endregion
        #region InclQtyINAssemblyDemand
        public abstract class inclQtyINAssemblyDemand : PX.Data.BQL.BqlBool.Field<inclQtyINAssemblyDemand> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINAssemblyDemand), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINAssemblyDemand
        {
            get;
            set;
        }
        #endregion
        #region InclQtyINAssemblySupply
        public abstract class inclQtyINAssemblySupply : PX.Data.BQL.BqlBool.Field<inclQtyINAssemblySupply> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyINAssemblySupply), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyINAssemblySupply
        {
            get;
            set;
        }
        #endregion
        #region InclQtyProductionDemandPrepared
        public abstract class inclQtyProductionDemandPrepared : PX.Data.BQL.BqlBool.Field<inclQtyProductionDemandPrepared> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionDemandPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionDemandPrepared
        {
            get;
            set;
        }
        #endregion
        #region InclQtyProductionDemand
        public abstract class inclQtyProductionDemand : PX.Data.BQL.BqlBool.Field<inclQtyProductionDemand> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionDemand), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionDemand
        {
            get;
            set;
        }
        #endregion
        #region InclQtyProductionAllocated
        public abstract class inclQtyProductionAllocated : PX.Data.BQL.BqlBool.Field<inclQtyProductionAllocated> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionAllocated), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionAllocated
        {
            get;
            set;
        }
        #endregion
        #region InclQtyProductionSupplyPrepared
        public abstract class inclQtyProductionSupplyPrepared : PX.Data.BQL.BqlBool.Field<inclQtyProductionSupplyPrepared> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionSupplyPrepared), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionSupplyPrepared
        {
            get;
            set;
        }
        #endregion
        #region InclQtyProductionSupply
        public abstract class inclQtyProductionSupply : PX.Data.BQL.BqlBool.Field<inclQtyProductionSupply> { }
        [PXBool()]
        [PXDefault(typeof(Select<INAvailabilityScheme, Where<INAvailabilityScheme.availabilitySchemeID, Equal<Current<availabilitySchemeID>>>>),
            CacheGlobal = true, SourceField = typeof(INAvailabilityScheme.inclQtyProductionSupply), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyProductionSupply
        {
            get;
            set;
        }
        #endregion
        #region InclQtyPOFixedReceipt
        public abstract class inclQtyPOFixedReceipt : PX.Data.BQL.BqlBool.Field<inclQtyPOFixedReceipt> { }
        [PXBool()]
        [PXDefault(typeof(False), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? InclQtyPOFixedReceipt
        {
            get;
            set;
        }
        #endregion

        #region SkipQtyValidation
        [PXBool, PXUnboundDefault(false)]
        public virtual Boolean? SkipQtyValidation { get; set; }
        public abstract class skipQtyValidation : PX.Data.BQL.BqlBool.Field<skipQtyValidation> { }
        #endregion
        #region PersistEvenZero
        public abstract class persistEvenZero : PX.Data.IBqlField
        {
        }

        [PXBool()]
        public virtual bool? PersistEvenZero
        {
            get;
            set;
        }
        #endregion
    }
}
