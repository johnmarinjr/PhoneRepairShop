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
    [PXCacheName(Messages.RelatedItemsFilter)]
    public class RelatedItemsFilter : IBqlTable
    {
        #region LineNbr
        [PXInt]
        public virtual int? LineNbr { get; set; }
        public abstract class lineNbr : BqlInt.Field<lineNbr> { }
        #endregion

        #region InventoryID
        [Inventory(Enabled = false)]
        public virtual int? InventoryID { get; set; }
        public abstract class inventoryID : BqlInt.Field<inventoryID> { }
        #endregion

        #region SubItemID
        [PXInt]
        public virtual int? SubItemID { get; set; }
        public abstract class subItemID : BqlInt.Field<subItemID> { }
        #endregion

        #region DocumentDate
        [PXDate]
        public virtual DateTime? DocumentDate { get; set; }
        public abstract class documentDate : BqlDateTime.Field<documentDate> { }
        #endregion

        #region CuryID
        [PXString]
        [PXUIField(DisplayName = "Currency", Enabled = false)]
        public virtual string CuryID { get; set; }
        public abstract class curyID : BqlString.Field<curyID> { }
        #endregion

        #region CuryUnitPrice
        [PXPriceCost]
        [PXUIField(DisplayName = "Unit Price", Enabled = false)]
        public virtual decimal? CuryUnitPrice { get; set; }
        public abstract class curyUnitPrice : BqlDecimal.Field<curyUnitPrice> { }
        #endregion

        #region OriginalCuryExtPrice
        public abstract class originalCuryExtPrice : BqlDecimal.Field<originalCuryExtPrice> { }
        [PXCury(typeof(curyID))]
        public virtual decimal? OriginalCuryExtPrice { get; set; }
        #endregion

        #region CuryExtPrice
        public abstract class curyExtPrice : BqlDecimal.Field<curyExtPrice> { }
        [PXCury(typeof(curyID))]
        [PXFormula(typeof(qty.Multiply<
            curyUnitPrice
                .When<originalQty.IsEqual<decimal0>>
                .Else<originalCuryExtPrice.Divide<originalQty>>>))]
        [PXUIField(DisplayName = "Ext. Price", Enabled = false)]
        public virtual decimal? CuryExtPrice { get; set; }
        #endregion

        #region Uom
        [INUnit(typeof(inventoryID), Enabled = false)]
        public virtual string UOM { get; set; }
        public abstract class uom : BqlString.Field<uom> { }
        #endregion

        #region OriginalQty
        [PXQuantity]
        public virtual decimal? OriginalQty { get; set; }
        public abstract class originalQty : BqlDecimal.Field<originalQty> { }
        #endregion

        #region Qty
        [PXQuantity]
        [PXUIField(DisplayName = "Quantity")]
        public virtual decimal? Qty { get; set; }
        public abstract class qty : BqlDecimal.Field<qty> { }
        #endregion

        #region BaseUnitMultDiv
        [PXString(1, IsFixed = true)]
        [MultDiv.List]
        public virtual string BaseUnitMultDiv { get; set; }
        public abstract class baseUnitMultDiv : BqlString.Field<baseUnitMultDiv> { }
        #endregion

        #region BaseUnitRate
        [PXDecimal(6)]
        public virtual decimal? BaseUnitRate { get; set; }
        public abstract class baseUnitRate : BqlDecimal.Field<baseUnitRate> { }
        #endregion

        #region AvailableQty
        [PXQuantity]
        [PXUIField(DisplayName = "Qty. Available", Enabled = false)]
        public virtual decimal? AvailableQty { get; set; }
        public abstract class availableQty : BqlDecimal.Field<availableQty> { }
        #endregion

        #region BranchID
        [PXInt]
        public virtual int? BranchID { get; set; }
        public abstract class branchID : BqlInt.Field<branchID> { }
        #endregion

        #region SiteID
        [Site(Enabled = false)]
        public virtual int? SiteID { get; set; }
        public abstract class siteID : BqlInt.Field<siteID> { }
        #endregion

        #region OnlyAvailableItems
        [PXBool]
        [PXUnboundDefault(typeof(Search<SOSetup.showOnlyAvailableRelatedItems>))]
        [PXUIField(DisplayName = "Show Only Available Items")]
        public virtual bool? OnlyAvailableItems { get; set; }
        public abstract class onlyAvailableItems : BqlBool.Field<onlyAvailableItems> { }
        #endregion

        #region KeepOriginalPrice
        [PXBool]
        [PXUIField(DisplayName = "Keep Original Price")]
        public virtual bool? KeepOriginalPrice { get; set; }
        public abstract class keepOriginalPrice : BqlBool.Field<keepOriginalPrice> { }
        #endregion

        #region OrderBehavior
        [PXString(2, IsFixed = true, InputMask = ">aa")]
        [SOBehavior.List()]
        public virtual string OrderBehavior { get; set; }
        public abstract class orderBehavior : BqlString.Field<orderBehavior> { }
        #endregion

        #region RelatedItemsRelation
        [PXInt]
        public virtual int? RelatedItemsRelation { get; set; }
        public abstract class relatedItemsRelation : BqlInt.Field<relatedItemsRelation> { }
        #endregion

        #region ShowSubstituteItems
        [PXBool]
        public virtual bool? ShowSubstituteItems { get; set; }
        public abstract class showSubstituteItems : BqlBool.Field<showSubstituteItems> { }
        #endregion

        #region ShowUpSellItems
        [PXBool]
        public virtual bool? ShowUpSellItems { get; set; }
        public abstract class showUpSellItems : BqlBool.Field<showUpSellItems> { }
        #endregion

        #region ShowCrossSellItems
        [PXBool]
        public virtual bool? ShowCrossSellItems { get; set; }
        public abstract class showCrossSellItems : BqlBool.Field<showCrossSellItems> { }
        #endregion

        #region ShowOtherRelatedItems
        [PXBool]
        public virtual bool? ShowOtherRelatedItems { get; set; }
        public abstract class showOtherRelatedItems : BqlBool.Field<showOtherRelatedItems> { }
        #endregion

        #region ShowAllRelatedItems
        [PXBool]
        public virtual bool? ShowAllRelatedItems { get; set; }
        public abstract class showAllRelatedItems : BqlBool.Field<showAllRelatedItems> { }
        #endregion
    }
}
