using PX.Data;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.SO;
using PX.Web.UI;
using System.Collections;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
    public class AsBuiltConfigInq : PXGraph<AsBuiltConfigInq>
    {
        public PXFilter<AsBuiltConfigFilter> Filter;
        public SelectFrom<AsBuiltTreeNode> 
            .Where<AsBuiltTreeNode.parentID.IsEqual<Argument.AsString>
                .And<AsBuiltTreeNode.matlLine.IsEqual<Argument.AsString>
                .And<AsBuiltTreeNode.level.IsEqual<Argument.AsInt>>>>
            .OrderBy<Asc<AsBuiltTreeNode.sortOrder>>.View Tree;

        public PXCancel<AsBuiltConfigFilter> Cancel;

        public AsBuiltConfigInq()
        {
            ProdLotSerialRecs.AllowDelete = false;
            ProdLotSerialRecs.AllowInsert = false;
            ProdLotSerialRecs.AllowUpdate = false;
        }

        protected virtual IEnumerable tree([PXString]string parentID, [PXString]string matlLine, [PXInt]int? level)
        {
            var filter = Filter.Current;
            if ((filter.LotSerialNbr == null && filter.InventoryID == null
                && filter.OrdNbr == null && filter.ProdOrdID == null) || 
                (filter.LevelsToDisplay != null && level.HasValue && filter.LevelsToDisplay < level))
            {
                yield break;
            }

            if(level == null)
            {
                var filterResults = SelectFrom<AMProdItem>
                    .InnerJoin<AMProdItemSplit>
                        .On<AMProdItem.orderType.IsEqual<AMProdItemSplit.orderType>
                            .And<AMProdItem.prodOrdID.IsEqual<AMProdItemSplit.prodOrdID>>>
                    .Where<Brackets<AMProdItemSplit.lotSerialNbr.IsEqual<AsBuiltConfigFilter.lotSerialNbr.FromCurrent>
                        .Or<AsBuiltConfigFilter.lotSerialNbr.FromCurrent.IsNull>>
                    .And<Brackets<AMProdItem.inventoryID.IsEqual<AsBuiltConfigFilter.inventoryID.FromCurrent>
                        .Or<AsBuiltConfigFilter.inventoryID.FromCurrent.IsNull>>
                    .And<Brackets<AMProdItem.ordNbr.IsEqual<AsBuiltConfigFilter.ordNbr.FromCurrent>
                        .Or<AsBuiltConfigFilter.ordNbr.FromCurrent.IsNull>>
                    .And<Brackets<AMProdItem.prodOrdID.IsEqual<AsBuiltConfigFilter.prodOrdID.FromCurrent>
                        .Or<AsBuiltConfigFilter.prodOrdID.FromCurrent.IsNull>>
                    .And<AMProdItem.statusID.IsNotEqual<ProductionOrderStatus.cancel>>
                        >>>>.View.Select(this).RowCast<AMProdItem>().Distinct();

                foreach(AMProdItem item in filterResults)
                {
                    var initem = InventoryItem.PK.Find(this, item.InventoryID);
                    yield return new AsBuiltTreeNode
                    {
                        ParentID = $"{item.OrderType};{item.ProdOrdID};{filter.LotSerialNbr}",
                        Label = $"{item.OrderType} - {item.ProdOrdID} - {initem.InventoryCD}",
                        Icon = Sprite.Tree.GetFullUrl(Sprite.Tree.Folder),
                        Level = 0,
                        SelectedValue = $"{item.OrderType};{item.ProdOrdID};{filter.LotSerialNbr}"
                    };
                }
            }
            else if(matlLine == null)
            {
                var prodItem = parentID.Split(';');
                var materials = SelectFrom<AMProdMatl>
                    .InnerJoin<InventoryItem>
                        .On<AMProdMatl.inventoryID.IsEqual<InventoryItem.inventoryID>>
                    .LeftJoin<AMProdMatlLotSerial>
                        .On<AMProdMatl.orderType.IsEqual<AMProdMatlLotSerial.orderType>
                            .And<AMProdMatl.prodOrdID.IsEqual<AMProdMatlLotSerial.prodOrdID>
                            .And<AMProdMatl.operationID.IsEqual<AMProdMatlLotSerial.operationID>
                            .And<AMProdMatl.lineID.IsEqual<AMProdMatlLotSerial.lineID>>>>>
                    .Where<AMProdMatl.orderType.IsEqual<@P.AsString>
                        .And<AMProdMatl.prodOrdID.IsEqual<@P.AsString>
                        .And<Brackets<AMProdMatlLotSerial.parentLotSerialNbr.IsEqual<@P.AsString>
                            .Or<@P.AsString.IsEqual<StringEmpty>>>>>>.View.Select(this, prodItem[0], prodItem[1], prodItem[2], prodItem[2]);

                if(!string.IsNullOrEmpty(prodItem[2]))
                {
                    foreach(PXResult<AMProdMatl, InventoryItem, AMProdMatlLotSerial> result in materials)
                    {
                        var matl = (AMProdMatl)result;
                        var initem = (InventoryItem)result;
                        var lotserial = (AMProdMatlLotSerial)result;
                        yield return new AsBuiltTreeNode
                        {
                            MatlLine = $"{matl.OrderType};{matl.ProdOrdID};{matl.OperationID};{matl.LineID};{lotserial.LotSerialNbr}",
                            Label = INDescrDisplay(initem.InventoryCD, matl.Descr),
                            Icon = Sprite.Tree.GetFullUrl(Sprite.Tree.Leaf),
                            Level = level + 1,
                            SelectedValue = $"{matl.OrderType};{matl.ProdOrdID};{matl.OperationID};{matl.LineID};{lotserial.LotSerialNbr}"
                        };
                    }
                }
                else
                {
                    foreach (AMProdMatl result in materials.RowCast<AMProdMatl>().Distinct())
                    {
                        var matl = (AMProdMatl)result;
                        var initem = InventoryItem.PK.Find(this, matl.InventoryID);
                        yield return new AsBuiltTreeNode
                        {
                            MatlLine = $"{matl.OrderType};{matl.ProdOrdID};{matl.OperationID};{matl.LineID};",
                            Label = INDescrDisplay(initem.InventoryCD, matl.Descr),
                            Icon = Sprite.Tree.GetFullUrl(Sprite.Tree.Leaf),
                            Level = level + 1,
                            SelectedValue = $"{matl.OrderType};{matl.ProdOrdID};{matl.OperationID};{matl.LineID};"
                        };
                    }
                }

            }
            else if(parentID == null)
            {
                var matl = matlLine.Split(';');
                var subOrders = SelectFrom<AMProdItem>
                    .InnerJoin<AMProdItemSplit>
                        .On<AMProdItem.orderType.IsEqual<AMProdItemSplit.orderType>
                            .And<AMProdItem.prodOrdID.IsEqual<AMProdItemSplit.prodOrdID>>>
                    .InnerJoin<AMProdMatlLotSerial>
                        .On<AMProdMatlLotSerial.lotSerialNbr.IsEqual<AMProdItemSplit.lotSerialNbr>>
                    .InnerJoin<AMProdMatl>
                        .On<AMProdMatl.orderType.IsEqual<AMProdMatlLotSerial.orderType>
                            .And<AMProdMatl.prodOrdID.IsEqual<AMProdMatlLotSerial.prodOrdID>
                            .And<AMProdMatl.operationID.IsEqual<AMProdMatlLotSerial.operationID>
                            .And<AMProdMatl.lineID.IsEqual<AMProdMatlLotSerial.lineID>
                            .And<AMProdMatl.inventoryID.IsEqual<AMProdItem.inventoryID>>>>>>
                    .Where<AMProdMatl.orderType.IsEqual<@P.AsString>
                        .And<AMProdMatl.prodOrdID.IsEqual<@P.AsString>
                        .And<AMProdMatl.operationID.IsEqual<@P.AsInt>
                        .And<AMProdMatl.lineID.IsEqual<@P.AsInt>
                        .And<AMProdItem.statusID.IsNotEqual<ProductionOrderStatus.cancel>
                        .And<Brackets<AMProdItemSplit.lotSerialNbr.IsEqual<@P.AsString>
                            .Or<@P.AsString.IsEqual<StringEmpty>>>>>>>>>.View.Select(this, matl[0], matl[1], matl[2], matl[3], matl[4], matl[4]);

                if(!string.IsNullOrEmpty(matl[4]))
                {
                    foreach (PXResult<AMProdItem, AMProdItemSplit, AMProdMatlLotSerial, AMProdMatl> result in subOrders)
                    {
                        var item = (AMProdItem)result;
                        var lotserial = (AMProdMatlLotSerial)result;
                        var initem = InventoryItem.PK.Find(this, item.InventoryID);
                        yield return new AsBuiltTreeNode
                        {
                            ParentID = $"{item.OrderType};{item.ProdOrdID};{lotserial.LotSerialNbr}",
                            Label = $"{item.OrderType} - {item.ProdOrdID} - {initem.InventoryCD}",
                            Icon = Sprite.Tree.GetFullUrl(Sprite.Tree.Folder),
                            Level = level + 1,
                            SelectedValue = $"{item.OrderType};{item.ProdOrdID};{lotserial.LotSerialNbr}"
                        };
                    }
                }
                else
                {
                    foreach (AMProdItem result in subOrders.RowCast<AMProdItem>().Distinct())
                    {
                        var item = (AMProdItem)result;
                        var initem = InventoryItem.PK.Find(this, item.InventoryID);
                        yield return new AsBuiltTreeNode
                        {
                            ParentID = $"{item.OrderType};{item.ProdOrdID};",
                            Label = $"{item.OrderType} - {item.ProdOrdID} - {initem.InventoryCD}",
                            Icon = Sprite.Tree.GetFullUrl(Sprite.Tree.Folder),
                            Level = level + 1,
                            SelectedValue = $"{item.OrderType};{item.ProdOrdID};"
                        };
                    }
                }

            }
            

        }

        public PXSelect<
        	AMProdLotSerial, 
        	Where<AMProdLotSerial.descr, Equal<Argument<string>>>> 
        	ProdLotSerialRecs;

        protected virtual IEnumerable prodLotSerialRecs([PXString]string selectedValue)
        {
            var list = new List<AMProdLotSerial>();
            if (selectedValue == null)
                return list;

            var keys = selectedValue.Split(';');
            //if there are 5 keys get material lots, if there's 3 get proditemsplits
            if (keys.Count() == 5)
            {                
                var prodMatls = SelectFrom<AMProdMatlLotSerial>
                    .InnerJoin<AMProdItem>
                        .On<AMProdItem.orderType.IsEqual<AMProdMatlLotSerial.orderType>
                            .And<AMProdItem.prodOrdID.IsEqual<AMProdMatlLotSerial.prodOrdID>>>
                    .InnerJoin<AMProdMatl>
                        .On<AMProdMatl.orderType.IsEqual<AMProdMatlLotSerial.orderType>
                            .And<AMProdMatl.prodOrdID.IsEqual<AMProdMatlLotSerial.prodOrdID>
                            .And<AMProdMatl.operationID.IsEqual<AMProdMatlLotSerial.operationID>
                            .And<AMProdMatl.lineID.IsEqual<AMProdMatlLotSerial.lineID>>>>>
					.InnerJoin<InventoryItem>
						.On<AMProdItem.inventoryID.IsEqual<InventoryItem.inventoryID>>
					.Where<AMProdMatlLotSerial.orderType.IsEqual<@P.AsString>
                        .And<AMProdMatlLotSerial.prodOrdID.IsEqual<@P.AsString>
                        .And<AMProdMatlLotSerial.operationID.IsEqual<@P.AsInt>
                        .And<AMProdMatlLotSerial.lineID.IsEqual<@P.AsInt>
                        .And<Brackets<AMProdMatlLotSerial.lotSerialNbr.IsEqual<@P.AsString>
                            .Or<@P.AsString.IsEqual<StringEmpty>>>>>>>>.View.Select(this, 
                        keys[0], keys[1], keys[2], keys[3], keys[4], keys[4]);
                foreach(PXResult<AMProdMatlLotSerial, AMProdItem, AMProdMatl, InventoryItem> prodMatl in prodMatls)
                {
					var matl = (AMProdMatl)prodMatl;
					var matlLot = (AMProdMatlLotSerial)prodMatl;
					var item = (AMProdItem)prodMatl;
					var invent = (InventoryItem)prodMatl;
					//if a lot/serial number is specified in the filter, only show those records in the grid
					var filter = Filter.Current;
					if (filter != null && ((filter.LotSerialNbr != null && filter.LotSerialNbr == matlLot.ParentLotSerialNbr) || filter.ProdOrdID != null ||
						filter.InventoryID != null || filter.OrdNbr != null))

					{
						list.Add(new AMProdLotSerial
						{
							InventoryID = matl.InventoryID,
							Descr = matl.Descr,
							LotSerialNbr = matlLot.LotSerialNbr,
							Qty = matlLot.QtyIssued,
							UOM = matl.UOM,
							ParentLotSerialNbr = matlLot.ParentLotSerialNbr,
							ParentInventoryID = item.InventoryID,
							ParentDescr = invent.Descr
						});
					}
                }
            }
            else if (keys.Count() == 3)
            {
                var prodSplits = SelectFrom<AMProdItemSplit>
                    .InnerJoin<AMProdItem>
                        .On<AMProdItemSplit.orderType.IsEqual<AMProdItem.orderType>
                            .And<AMProdItemSplit.prodOrdID.IsEqual<AMProdItem.prodOrdID>>>
                    .Where<AMProdItemSplit.orderType.IsEqual<@P.AsString>
                        .And<AMProdItemSplit.prodOrdID.IsEqual<@P.AsString>
                        .And<Brackets<AMProdItemSplit.lotSerialNbr.IsEqual<@P.AsString>
                            .Or<@P.AsString.IsEqual<StringEmpty>>>>>>.View.Select(this, keys[0], keys[1], keys[2], keys[2]);
                foreach (PXResult<AMProdItemSplit, AMProdItem> prodSplit in prodSplits)
                {
                    var item = (AMProdItem)prodSplit;
                    var split = (AMProdItemSplit)prodSplit;
                    list.Add(new AMProdLotSerial
                    {
                        InventoryID = item.InventoryID,
                        Descr = item.Descr,
                        LotSerialNbr = split.LotSerialNbr,
                        Qty = split.QtyComplete,
                        UOM = split.UOM
                    });
                }
            }
            return list;
        }

        private string INDescrDisplay(string inventoryCD, string descr)
        {
            var display = inventoryCD.TrimIfNotNullEmpty();
            if (!string.IsNullOrEmpty(descr))
                display += " - " + descr.Trim();
            return display;
        }
    }

    [Serializable]
    [PXCacheName("As Built Config Filter")]
    public class AsBuiltConfigFilter : IBqlTable
    {
        #region LotSerialNbr
        public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
        
        protected String _LotSerialNbr;
        [PXString]
        [PXSelector(typeof(Search4<AMProdItemSplit.lotSerialNbr, Where<AMProdItemSplit.lotSerialNbr, IsNotNull>, Aggregate<GroupBy<AMProdItemSplit.lotSerialNbr>>>))]
        [PXUIField(DisplayName = "Lot/Serial Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
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
        #region InventoryID
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

        protected Int32? _InventoryID;
        [StockItem(Visibility = PXUIVisibility.SelectorVisible, Required = false)]
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
        #region OrdNbr
        public abstract class ordNbr : PX.Data.BQL.BqlString.Field<ordNbr> { }

        protected String _OrdNbr;
        [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Sales Order Nbr.")]
        [PXSelector(typeof(Search<SOOrder.orderNbr>))]
        public virtual String OrdNbr
        {
            get
            {
                return this._OrdNbr;
            }
            set
            {
                this._OrdNbr = value;
            }
        }
        #endregion
        #region ProdOrdID
        public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

        protected String _ProdOrdID;
        [PXUIField(DisplayName = "Prod. Order Nbr.")]
        [PXString]
        [PXSelector(typeof(Search<AMProdItem.prodOrdID>))]
        public virtual String ProdOrdID
        {
            get
            {
                return this._ProdOrdID;
            }
            set
            {
                this._ProdOrdID = value;
            }
        }
        #endregion
        #region LevelsToDisplay
        public abstract class levelsToDisplay : PX.Data.BQL.BqlInt.Field<levelsToDisplay> { }

        protected Int32? _LevelsToDisplay;
        [PXInt]
        [PXUnboundDefault(1)]
        [PXUIField(DisplayName = "Levels to Display")]
        public virtual Int32? LevelsToDisplay
        {
            get
            {
                return this._LevelsToDisplay;
            }
            set
            {
                this._LevelsToDisplay = value;
            }
        }
        #endregion
    }

    [Serializable]
    [PXHidden]
    public class AsBuiltTreeNode : IBqlTable
    {
        #region ParentID
        public abstract class parentID : PX.Data.BQL.BqlString.Field<parentID> { }

        //OrderType;ProdOrdID
        protected string _ParentID;
        [PXUIField(DisplayName = "Parent ID")]
        public virtual string ParentID
        {
            get
            {
                return this._ParentID;
            }
            set
            {
                this._ParentID = value;
            }
        }
        #endregion

        #region MatlLine
        public abstract class matlLine : PX.Data.BQL.BqlString.Field<matlLine> { }

        //OrderType;ProdordID;OperationID;LineID
        [PXString()]
        [PXUIField(DisplayName = "Matl Line")]
        public virtual string MatlLine { get; set; }
        #endregion

        #region Label
        public abstract class label : PX.Data.BQL.BqlString.Field<label> { }

        [PXString()]
        [PXUIField(DisplayName = "Label")]
        public virtual string Label { get; set; }
        #endregion

        #region ToolTip
        public abstract class toolTip : PX.Data.BQL.BqlString.Field<toolTip> { }

        [PXString]
        [PXUIField(DisplayName = "ToolTip")]
        public virtual string ToolTip { get; set; }
        #endregion

        #region SortOrder
        public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }

        [PXInt]
        [PXUIField(DisplayName = "Sort Order")]
        public virtual int? SortOrder { get; set; }
        #endregion

        #region Icon
        public abstract class icon : PX.Data.BQL.BqlString.Field<icon> { }

        [PXString(250)]
        public virtual String Icon { get; set; }
        #endregion
        #region Level
        public abstract class level : PX.Data.BQL.BqlInt.Field<level> { }

        protected Int32? _Level;
        [PXInt]
        [PXUnboundDefault(1)]
        [PXUIField(DisplayName = "Level")]
        public virtual Int32? Level
        {
            get
            {
                return this._Level;
            }
            set
            {
                this._Level = value;
            }
        }
        #endregion
        #region SelectedValue
        public abstract class selectedValue : PX.Data.BQL.BqlString.Field<selectedValue> { }

        //This is the key passed to the grid on click
        [PXString()]
        [PXUIField()]
        public virtual string SelectedValue { get; set; }
        #endregion
    }

    [PXHidden]
    public class AMProdLotSerial : IBqlTable
    {
        #region InventoryID
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

        protected Int32? _InventoryID;
        [StockItem(Visibility = PXUIVisibility.SelectorVisible, Required = false)]
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
        #region Descr
        public abstract class descr : PX.Data.BQL.BqlString.Field<descr> { }

        protected String _Descr;
        [PXString(256, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String Descr
        {
            get
            {
                return this._Descr;
            }
            set
            {
                this._Descr = value;
            }
        }
        #endregion
        #region LotSerialNbr
        public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }

        protected String _LotSerialNbr;
        [PXString]
        [PXUIField(DisplayName = "Lot/Serial Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
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
        #region ParentLotSerialNbr
        public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }

        protected String _ParentLotSerialNbr;
        [PXString]
        [PXUIField(DisplayName = "Parent Lot/Serial Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String ParentLotSerialNbr
        {
            get
            {
                return this._ParentLotSerialNbr;
            }
            set
            {
                this._ParentLotSerialNbr = value;
            }
        }
        #endregion
        #region Qty
        public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }

        protected Decimal? _Qty;
        [PXQuantity]
        [PXUIField(DisplayName = "Qty")]
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
        #region UOM
        public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

        protected String _UOM;
        
        [INUnit(typeof(inventoryID))]
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
        #region ParentInventoryID
        public abstract class parentInventoryID : PX.Data.BQL.BqlInt.Field<parentInventoryID> { }

        protected Int32? _ParentInventoryID;
        [StockItem(Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Parent Inventory ID", Required = false)]
        public virtual Int32? ParentInventoryID
        {
            get
            {
                return this._ParentInventoryID;
            }
            set
            {
                this._ParentInventoryID = value;
            }
        }
        #endregion
        #region ParentDescr
        public abstract class parentDescr : PX.Data.BQL.BqlString.Field<parentDescr> { }

        protected String _ParentDescr;
        [PXString(256, IsUnicode = true)]
        [PXUIField(DisplayName = "Parent Description", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String ParentDescr
        {
            get
            {
                return this._ParentDescr;
            }
            set
            {
                this._ParentDescr = value;
            }
        }
        #endregion
    }

}
