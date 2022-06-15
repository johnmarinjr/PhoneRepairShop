using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM.Attributes
{
    public class AMMoveLotSerialNbrAttribute : AMLotSerialNbrAttribute
    {
        public AMMoveLotSerialNbrAttribute(Type OrderTypeType, Type ProdOrderType, Type InventoryType, Type SubItemType, Type LocationType) : base(InventoryType, SubItemType, LocationType)
        {

            Type selType = typeof(Search2<,,>);
            Type field = typeof(INLotSerialStatus.lotSerialNbr);
            Type join = typeof(InnerJoin<AMProdItem, On<INLotSerialStatus.inventoryID, Equal<AMProdItem.inventoryID>>,
                LeftJoin<AMProdItemSplit, On<AMProdItem.orderType, Equal<AMProdItemSplit.orderType>,
                And<AMProdItem.prodOrdID, Equal<AMProdItemSplit.prodOrdID>
                ,And<Sub<AMProdItemSplit.qty,AMProdItemSplit.qtyComplete>, Greater<decimal0>
                ,And<AMProdItemSplit.lotSerialNbr, Equal<INLotSerialStatus.lotSerialNbr>>
                >
                >>>>);
            //Type where = BqlCommand.Compose(typeof(Where<,,>), typeof(AMProdItem.orderType), typeof(Equal<>), typeof(Optional<>), OrderTypeType
            //    ,typeof(And<,,>), typeof(AMProdItem.prodOrdID), typeof(Equal<>), typeof(Optional<>), ProdOrderType
            //    , typeof(And2<Where<AMProdItem.preassignLotSerial, Equal<boolFalse>>, Or<AMProdItemSplit.lotSerialNbr, IsNotNull>>)
            //    );
            Type where = BqlCommand.Compose(typeof(Where<,,>), typeof(AMProdItem.orderType), typeof(Equal<>), typeof(Optional<>), OrderTypeType
            , typeof(And<,,>), typeof(AMProdItem.prodOrdID), typeof(Equal<>), typeof(Optional<>), ProdOrderType
            , typeof(And<Where2<Where<AMProdItem.preassignLotSerial, Equal<boolFalse>>, Or<AMProdItemSplit.lotSerialNbr, IsNotNull>>>)
            );

            var SearchType = BqlCommand.Compose(selType, field, join, where);

            PXSelectorAttribute attr = new PXSelectorAttribute(SearchType,
                                                    typeof(INLotSerialStatus.inventoryID),
                                                     typeof(INLotSerialStatus.lotSerialNbr),
                                                     typeof(INLotSerialStatus.siteID),
                                                     typeof(INLotSerialStatus.locationID),
                                                     typeof(INLotSerialStatus.qtyOnHand),
                                                     typeof(INLotSerialStatus.qtyAvail),
                                                     typeof(INLotSerialStatus.expireDate));
            _Attributes.Add(attr);
            _SelAttrIndex = _Attributes.Count - 1;
        }
    }
}
