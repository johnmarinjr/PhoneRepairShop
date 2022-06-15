using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Bql;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
    [PXProjection(typeof(
        SelectFrom<INRelatedInventory>
            .InnerJoin<InventoryItem>
                .On<INRelatedInventory.FK.RelatedInventoryItem
                .And<INRelatedInventory.isActive.IsEqual<True>>
                .And<InventoryItem.isTemplate.IsNotEqual<True>>
                .And<InventoryItem.itemStatus.IsNotIn<InventoryItemStatus.unknown, InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion, InventoryItemStatus.noSales>>
                .And<CurrentMatch<InventoryItem, AccessInfo.userName>>>
            .LeftJoin<INSiteStatus>
                .On<INSiteStatus.inventoryID.IsEqual<INRelatedInventory.relatedInventoryID>
                .And<INSiteStatus.siteID.IsNotEqual<SiteAttribute.transitSiteID>>>
            .LeftJoin<INSubItem>
                .On<INSiteStatus.FK.SubItem>
            .LeftJoin<INSite>
                .On<INSiteStatus.FK.Site>
            .Where<
                Brackets<INSubItem.subItemID.IsNull.Or<CurrentMatch<INSubItem, AccessInfo.userName>>>
                .And<INSite.siteID.IsNull.Or<CurrentMatch<INSite, AccessInfo.userName>>>>),
        Persistent = false)]
    [PXHidden]
    public class INAvailableRelatedItems : IBqlTable
    {
        #region OriginalInventoryID
        [PXDBInt(BqlField = typeof(INRelatedInventory.inventoryID))]
        public int? OriginalInventoryID { get; set; }
        public abstract class originalInventoryID : BqlInt.Field<originalInventoryID> { }
        #endregion

        #region InventoryID 
        [PXDBInt(BqlField = typeof(INRelatedInventory.relatedInventoryID))]
        public int? InventoryID { get; set; }
        public abstract class relatedInventoryID : BqlInt.Field<relatedInventoryID> { }
        #endregion

        #region StkItem 
        [PXDBBool(BqlField = typeof(InventoryItem.stkItem))]
        public virtual bool? StkItem { get; set; }
        public abstract class stkItem : BqlBool.Field<stkItem> { }
        #endregion

        #region EffectiveDate
        [PXDBDate(BqlField = typeof(INRelatedInventory.effectiveDate))]
        public DateTime? EffectiveDate { get; set; }
        public abstract class effectiveDate : BqlDateTime.Field<effectiveDate> { }
        #endregion

        #region ExpirationDate
        [PXDBDate(BqlField = typeof(INRelatedInventory.expirationDate))]
        public DateTime? ExpirationDate { get; set; }
        public abstract class expirationDate : BqlDateTime.Field<expirationDate> { }
        #endregion

        #region SubItemID
        [PXDBInt(BqlField = typeof(INSiteStatus.subItemID))]
        public virtual int? SubItemID { get; set; }
        public abstract class subItemID : BqlInt.Field<subItemID> { }
        #endregion

        #region SiteID
        [PXDBInt(BqlField = typeof(INSiteStatus.siteID))]
        public virtual int? SiteID { get; set; }
        public abstract class siteID : BqlInt.Field<siteID> { }
        #endregion

        #region BranchID
        [PXDBInt(BqlField = typeof(INSite.branchID))]
        public virtual int? BranchID { get; set; }
        public abstract class branchID : BqlInt.Field<branchID> { }
        #endregion

        #region QtyAvail
        public abstract class qtyAvail : BqlDecimal.Field<qtyAvail> { }
        [PXDBQuantity(BqlField = typeof(INSiteStatus.qtyAvail))]
        public virtual decimal? QtyAvail { get; set; }
        #endregion

        #region Relation
        [PXDBString(5, IsFixed = true, BqlField = typeof(INRelatedInventory.relation))]
        public string Relation { get; set; }
        public abstract class relation : BqlString.Field<relation> { }
        #endregion

        #region Required
        [PXDBBool(BqlField = typeof(INRelatedInventory.required))]
        public bool? Required { get; set; }
        public abstract class required : BqlBool.Field<required> { }
        #endregion
    }
}
