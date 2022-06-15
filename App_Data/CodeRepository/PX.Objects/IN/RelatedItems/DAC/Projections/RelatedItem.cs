using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.Common.Bql;
using PX.Objects.CS;
using PX.Objects.SO;
using System;

namespace PX.Objects.IN.RelatedItems
{
    [PXCacheName(Messages.RelatedItem)]
    [PXProjection(typeof(
        SelectFrom<INRelatedInventory>
            .InnerJoin<InventoryItem>
                .On<INRelatedInventory.inventoryID.IsEqual<RelatedItemsFilter.inventoryID.FromCurrent.Value>
                .And<INRelatedInventory.isActive.IsEqual<True>>
                .And<InventoryItem.inventoryID.IsEqual<INRelatedInventory.relatedInventoryID>>
                .And<InventoryItem.isTemplate.IsNotEqual<True>>
                .And<InventoryItem.itemStatus.IsNotIn<InventoryItemStatus.unknown, InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion, InventoryItemStatus.noSales>>
                .And<CurrentMatch<InventoryItem, AccessInfo.userName>>>
            .InnerJoin<INUnit>
                .On<INUnit.unitType.IsEqual<INUnitType.inventoryItem>
                .And<INUnit.inventoryID.IsEqual<InventoryItem.inventoryID>>
                .And<INUnit.fromUnit.IsEqual<INRelatedInventory.uom>>>
            .LeftJoin<INSiteStatus>
                .On<INSiteStatus.inventoryID.IsEqual<INRelatedInventory.relatedInventoryID>
                .And<INSiteStatus.siteID.IsNotEqual<SiteAttribute.transitSiteID>>>
            .LeftJoin<INSubItem>
                    .On<INSiteStatus.FK.SubItem>
            .LeftJoin<INSite>
                .On<INSiteStatus.FK.Site>
            .Where<
                Brackets<RelatedItemsFilter.onlyAvailableItems.FromCurrent.Value.IsNotEqual<True>
                    .Or<InventoryItem.stkItem.IsNotEqual<True>>
                    .Or<INRelatedInventory.relation.IsEqual<InventoryRelation.substitute>.And<INRelatedInventory.required.IsEqual<True>>>
                    .Or<INSiteStatus.qtyAvail.IsGreater<decimal0>>>
                .And<RelatedItemsFilter.documentDate.FromCurrent.Value.IsNull
                    .Or<Brackets<INRelatedInventory.effectiveDate.IsNull
                            .Or<INRelatedInventory.effectiveDate.IsLessEqual<RelatedItemsFilter.documentDate.FromCurrent.Value>>>
                        .And<Brackets<INRelatedInventory.expirationDate.IsNull
                            .Or<INRelatedInventory.expirationDate.IsGreaterEqual<RelatedItemsFilter.documentDate.FromCurrent.Value>>>>>>
                .And<INSite.siteID.IsNull
                    .Or<CurrentMatch<INSite, AccessInfo.userName>
                        .And<FeatureInstalled<FeaturesSet.interBranch>
                            .Or<SameOrganizationBranch<INSite.branchID, RelatedItemsFilter.branchID.FromCurrent>>
                            .Or<RelatedItemsFilter.orderBehavior.FromCurrent.Value.IsNotNull
								.And<RelatedItemsFilter.orderBehavior.FromCurrent.Value.IsEqual<SO.SOBehavior.qT>>>>>>
                .And<INSubItem.subItemID.IsNull.Or<CurrentMatch<INSubItem, AccessInfo.userName>>>
                >
        ), Persistent = false)]
    public class RelatedItem: IBqlTable
    {
        #region Keys
        public class PK : PrimaryKeyOf<RelatedItem>.By<inventoryID, lineID, subItemCD, siteCD>
        {
            public static RelatedItem Find(PXGraph graph, int? inventoryID, int? lineID, string subItemCD, string siteCD)
                => FindBy(graph, inventoryID, lineID, subItemCD, siteCD);
        }
        public static class FK
        {
            public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INRelatedInventory>.By<inventoryID> { }
            public class RelatedInventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INRelatedInventory>.By<relatedInventoryID> { }
            //todo public class UnitOfMeasure : INUnit.PK.ForeignKeyOf<INRelatedInventory>.By<relatedInventoryID, uom> { }
        }
        #endregion

        #region InventoryID
        [PXDBInt(IsKey = true, BqlField = typeof(INRelatedInventory.inventoryID))]
        public int? InventoryID { get; set; }
        public abstract class inventoryID : BqlInt.Field<inventoryID> { }
        #endregion

        #region LineID
        [PXDBInt(IsKey = true, BqlField = typeof(INRelatedInventory.lineID))]
        public virtual int? LineID { get; set; }
        public abstract class lineID : BqlInt.Field<lineID> { }
        #endregion

        #region RelatedInventoryID
        [Inventory(BqlField = typeof(INRelatedInventory.relatedInventoryID), Enabled = false)]
        public virtual int? RelatedInventoryID { get; set; }
        public abstract class relatedInventoryID : BqlInt.Field<relatedInventoryID> { }
        #endregion

        #region Desc
        [PXString]
        [PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXFormula(typeof(Selector<relatedInventoryID, InventoryItem.descr>))]
        public virtual string Desc { get; set; }
        public abstract class desc : BqlInt.Field<relatedInventoryID> { }
        #endregion

        #region Relation
        [PXDBString(5, IsFixed = true, BqlField = typeof(INRelatedInventory.relation))]
        [PXUIField(DisplayName = "Relation", Enabled = false)]
        [InventoryRelation.List]
        public virtual string Relation { get; set; }
        public abstract class relation : BqlString.Field<relation> { }
        #endregion

        #region Rank
        [PXDBInt(BqlField = typeof(INRelatedInventory.rank))]
        [PXUIField(DisplayName = "Rank", Enabled = false)]
        public virtual int? Rank { get; set; }
        public abstract class rank : BqlInt.Field<rank> { }
        #endregion

        #region Tag
        [PXDBString(4, IsFixed = true, BqlField = typeof(INRelatedInventory.tag))]
        [PXUIField(DisplayName = "Tag", Enabled = false)]
        [InventoryRelationTag.List]
        public virtual string Tag { get; set; }
        public abstract class tag : BqlString.Field<tag> { }
        #endregion

        #region UOM
        [INUnit(typeof(relatedInventoryID), BqlField = typeof(INRelatedInventory.uom), Enabled = false)]
        public virtual string UOM { get; set; }
        public abstract class uom : BqlString.Field<uom> { }
        #endregion

        #region Qty
        [PXDBQuantity(BqlField = typeof(INRelatedInventory.qty))]
        public virtual decimal? Qty { get; set; }
        public abstract class qty : BqlDecimal.Field<qty> { }
        #endregion

        #region BaseQty
        [PXDBDecimal(BqlField = typeof(INRelatedInventory.baseQty))]
        public virtual decimal? BaseQty { get; set; }
        public abstract class baseQty : BqlDecimal.Field<baseQty> { }
        #endregion

        #region Interchangeable
        [PXDBBool(BqlField = typeof(INRelatedInventory.interchangeable))]
        [PXUIField(DisplayName = "Customer Approval Not Needed", Enabled = false)]
        public virtual bool? Interchangeable { get; set; }
        public abstract class interchangeable : BqlBool.Field<interchangeable> { }
        #endregion

        #region Required
        [PXDBBool(BqlField = typeof(INRelatedInventory.required))]
        [PXUIField(DisplayName = "Required", Enabled = false)]
        public virtual bool? Required { get; set; }
        public abstract class required : BqlBool.Field<required> { }
        #endregion

        #region NoteID
        [PXNote(PopupTextEnabled = true, BqlField = typeof(INRelatedInventory.noteID))]
        public virtual Guid? NoteID { get; set; }
        public abstract class noteID : BqlGuid.Field<noteID> { }
        #endregion

        #region SubItemID
        [SubItem(typeof(inventoryID), BqlField = typeof(INSubItem.subItemID))]
        public virtual int? SubItemID { get; set; }
        public abstract class subItemID : BqlInt.Field<subItemID> { }
        #endregion

        #region SubItemCD
        [PXString(IsUnicode = true, IsKey = true)]
        [PXDBCalced(typeof(IsNull<Data.RTrim<INSubItem.subItemCD>, Empty>), typeof(string))]
        public virtual string SubItemCD { get; set; }
        public abstract class subItemCD : BqlString.Field<subItemCD> { }
        #endregion

        #region SiteID
        [Site(BqlField = typeof(INSiteStatus.siteID), Enabled = false)]
        public virtual int? SiteID { get; set; }
        public abstract class siteID : BqlInt.Field<siteID> { }
        #endregion

        #region SiteCD
        [PXString(IsUnicode = true, IsKey = true)]
        [PXDBCalced(typeof(IsNull<Data.RTrim<INSite.siteCD>, Empty>), typeof(string))]
        public virtual string SiteCD { get; set; }
        public abstract class siteCD : BqlString.Field<siteCD> { }
        #endregion

        #region Selected
        [PXBool]
        [PXUnboundDefault(false)]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected { get; set; }
        public abstract class selected : BqlBool.Field<selected> { }
        #endregion

        #region QtySelected
        [PXQuantity]
        [PXFormula(typeof(RelatedItemsFilter.qty.FromCurrent
            .Multiply<RelatedItemsFilter.baseUnitRate.FromCurrent
                .When<RelatedItemsFilter.baseUnitMultDiv.FromCurrent.IsEqual<MultDiv.multiply>>
                .Else<decimal1.Divide<RelatedItemsFilter.baseUnitRate.FromCurrent>>>
            .Multiply<qty>))]
        [PXUIField(DisplayName = "Qty. Selected", Enabled = false)]
        public virtual decimal? QtySelected { get; set; }
        public abstract class qtySelected : BqlDecimal.Field<qtySelected> { }
        #endregion

        #region BaseAvailableQty
        [PXDBDecimal(BqlField = typeof(INSiteStatus.qtyAvail))]
        public virtual decimal? BaseAvailableQty { get; set; }
        public abstract class baseAvailableQty : BqlDecimal.Field<baseAvailableQty> { }
        #endregion

        #region AvailableQty
        [PXQuantity]
        [PXDBCalced(typeof(
            Null
                .When<InventoryItem.stkItem.IsNotEqual<True>>
                .Else<decimal0
                    .When<INSiteStatus.qtyAvail.IsNull>
                    .Else<
                        INSiteStatus.qtyAvail.Multiply<INUnit.unitRate>
                        .When<INUnit.unitMultDiv.IsEqual<MultDiv.divide>>
                        .Else<INSiteStatus.qtyAvail.Divide<INUnit.unitRate>>>>), typeof(decimal))]
        [PXUIField(DisplayName = "Qty. Available", Enabled = false)]
        public virtual decimal? AvailableQty { get; set; }
        public abstract class availableQty : BqlDecimal.Field<availableQty> { }
        #endregion

        #region CuryUnitPrice
        [PXPriceCost]
        [PXUIField(DisplayName = "Unit Price", Enabled = false)]
        public virtual decimal? CuryUnitPrice { get; set; }
        public abstract class curyUnitPrice : BqlDecimal.Field<curyUnitPrice> { }
        #endregion

        #region CuryExtPrice
        [PXCury(typeof(RelatedItemsFilter.curyID))]
        [PXFormula(typeof(curyUnitPrice.Multiply<qtySelected>))]
        [PXUIField(DisplayName = "Ext. Price", Enabled = false)]
        public virtual decimal? CuryExtPrice { get; set; }
        public abstract class curyExtPrice : BqlDecimal.Field<curyExtPrice> { }
        #endregion

        #region PriceDiff
        [PXCury(typeof(RelatedItemsFilter.curyID))]
        [PXFormula(typeof(Case<Where<relation.IsNotEqual<InventoryRelation.crossSell>>, curyExtPrice.Subtract<RelatedItemsFilter.curyExtPrice.FromCurrent>>))]
        [PXUIField(DisplayName = "Ext. Price Difference", Enabled = false)]
        public virtual decimal? PriceDiff { get; set; }
        public abstract class priceDiff : BqlDecimal.Field<priceDiff> { }
        #endregion
    }
}
