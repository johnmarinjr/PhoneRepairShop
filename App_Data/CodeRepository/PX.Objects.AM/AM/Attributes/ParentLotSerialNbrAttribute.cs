using PX.Common;
using PX.Data;
using PX.Objects.AM.Attributes;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM.Attributes
{
    [PXDBString(INLotSerialStatus.lotSerialNbr.LENGTH, IsUnicode =true, InputMask ="")]
    [PXUIField(DisplayName = "Parent Lot/Serial Nbr.", FieldClass ="LotSerial")]
    [PXDefault("")]
    public class ParentLotSerialNbrAttribute : AcctSubAttribute//, IPXFieldVerifyingSubscriber
    {
        public ParentLotSerialNbrAttribute(Type ProdOrderType, Type ProdOrdIDType) : base()
        {
            var prodOrderType = BqlCommand.GetItemType(ProdOrderType);
            if (!typeof(ILSMaster).IsAssignableFrom(prodOrderType))
            {
                throw new PXArgumentException(nameof(itemType), IN.Messages.TypeMustImplementInterface, prodOrderType.GetLongName(), typeof(ILSMaster).GetLongName());
            }

            Type SearchType = BqlCommand.Compose(
                typeof(Search<,>),
                typeof(AMProdItemSplit.lotSerialNbr),
                typeof(Where<,,>),
                typeof(AMProdItemSplit.orderType),
                typeof(Equal<>),
                typeof(Optional<>),
                ProdOrderType,
                typeof(And<,>),
                typeof(AMProdItemSplit.prodOrdID),
                typeof(Equal<>),
                typeof(Optional<>),
                ProdOrdIDType
                );

            {
                PXSelectorAttribute attr = new PXSelectorAttribute(SearchType,
                                                                     typeof(AMProdItemSplit.lotSerialNbr),
                                                                     typeof(AMProdItemSplit.qty),
                                                                     typeof(AMProdItemSplit.qtyComplete),
                                                                     typeof(AMProdItemSplit.qtyScrapped),
                                                                     typeof(AMProdItemSplit.qtyRemaining));
                _Attributes.Add(attr);
                _SelAttrIndex = _Attributes.Count - 1;
            }
        }
    }
}
