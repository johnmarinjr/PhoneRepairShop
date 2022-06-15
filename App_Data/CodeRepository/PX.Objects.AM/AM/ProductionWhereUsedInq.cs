using System;
using System.Collections;
using System.Collections.Generic;
using PX.Objects.AM.Attributes;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.SO;

namespace PX.Objects.AM
{
	/// <summary>
	/// Manufacturing Production Material Where Used Inquiry
	/// </summary>
	public class ProductionWhereUsedInq : PXGraph<ProductionWhereUsedInq>
	{
		public PXFilter<ProductionWhereUsedFilter> Filter;
		public PXCancel<ProductionWhereUsedFilter> Cancel;

		public PXSelect<WhereUsedProductionDetail> ProductionWhereUsed;

		public ProductionWhereUsedInq()
		{
			this.ProductionWhereUsed.Cache.AllowInsert = false;
			this.ProductionWhereUsed.Cache.AllowDelete = false;
			this.ProductionWhereUsed.Cache.AllowUpdate = false;

			PXUIFieldAttribute.SetVisible<ProductionWhereUsedFilter.lotSerialNbr>(Filter.Cache, null, InventoryHelper.LotSerialTrackingFeatureEnabled);
			PXUIFieldAttribute.SetVisible<ProductionWhereUsedFilter.siteID>(Filter.Cache, null, InventoryHelper.MultiWarehousesFeatureEnabled);
			PXUIFieldAttribute.SetVisible<ProductionWhereUsedFilter.locationID>(Filter.Cache, null, InventoryHelper.MultiWarehouseLocationFeatureEnabled);
			PXUIFieldAttribute.SetVisible<ProductionWhereUsedFilter.multiLevel>(Filter.Cache, null, InventoryHelper.LotSerialTrackingFeatureEnabled);
		}

		protected virtual IEnumerable productionWhereUsed()
		{
			return LoadAllData();
		}

		protected virtual void _(Events.FieldUpdated<ProductionWhereUsedFilter, ProductionWhereUsedFilter.inventoryID> e)
		{
			if (e.Row == null || e.Row.InventoryID == null || e.NewValue == e.OldValue)
			{
				return;
			}

			e.Cache.SetValueExt<ProductionWhereUsedFilter.lotSerialNbr>(e.Row, null);
		}

		protected virtual void _(Events.FieldUpdated<ProductionWhereUsedFilter, ProductionWhereUsedFilter.siteID> e)
		{
			if (e.Row == null || e.Row.SiteID == null || e.NewValue == e.OldValue)
			{
				return;
			}

			e.Cache.SetValueExt<ProductionWhereUsedFilter.locationID>(e.Row, null);
		}

		protected virtual List<WhereUsedProductionDetail> LoadAllData()
		{
			List<WhereUsedProductionDetail> productionWhereUsedList = new List<WhereUsedProductionDetail>();

			if (Filter.Current.InventoryID == null)
			{
				return productionWhereUsedList;
			}

			INLotSerClass inLotSerClass = InventoryHelper.GetItemLotSerClass(this, Filter.Current.InventoryID);
			bool isLotTrackedItem = inLotSerClass.LotSerTrack != "N";

			if (Filter.Current.LotSerialNbr == null && isLotTrackedItem)
			{
				return productionWhereUsedList;
			}

			PXSelectBase<AMProdMatl> slctdmatl = new PXSelectJoin<AMProdMatl,
				InnerJoin<AMProdMatlLotSerial, On<AMProdMatlLotSerial.orderType, Equal<AMProdMatl.orderType>,
					And<AMProdMatlLotSerial.prodOrdID, Equal<AMProdMatl.prodOrdID>,
					And<AMProdMatlLotSerial.operationID, Equal<AMProdMatl.operationID>,
					And<AMProdMatlLotSerial.lineID, Equal<AMProdMatl.lineID>>>>>,
				InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<AMProdMatl.inventoryID>>,
				InnerJoin<AMProdItem, On<AMProdItem.orderType, Equal<AMProdMatlLotSerial.orderType>,
					And<AMProdItem.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>>>,
				LeftJoin<AMProdItemSplit, On<AMProdItemSplit.orderType, Equal<AMProdMatlLotSerial.orderType>,
					And<AMProdItemSplit.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>,
					And<AMProdItemSplit.lotSerialNbr, Equal<AMProdMatlLotSerial.parentLotSerialNbr>>>>,
				LeftJoin<BAccount, On<BAccount.bAccountID, Equal<AMProdItem.customerID>>,
				InnerJoin<ParentInventoryItem, On<ParentInventoryItem.parentInventoryID, Equal<AMProdItem.inventoryID>>,
				InnerJoin<INLotSerClass, On<INLotSerClass.lotSerClassID, Equal<ParentInventoryItem.parentLotSerClassID>>>>>>>>>>(this);

			if (Filter.Current.InventoryID != null)
			{
				slctdmatl.WhereAnd<Where<AMProdMatl.inventoryID, Equal<Current<ProductionWhereUsedFilter.inventoryID>>>>();
			}

			if (Filter.Current?.LotSerialNbr != null && isLotTrackedItem)
			{
				slctdmatl.WhereAnd<Where<AMProdMatlLotSerial.lotSerialNbr, Equal<Current<ProductionWhereUsedFilter.lotSerialNbr>>>>();
			}

			if (Filter.Current.SiteID != null)
			{
				slctdmatl.WhereAnd<Where<AMProdMatl.siteID, Equal<Current<ProductionWhereUsedFilter.siteID>>,
					Or<Where<AMProdMatl.siteID, IsNull,
					And<AMProdItem.siteID, Equal<Current<ProductionWhereUsedFilter.siteID>>>>>>>();
			}

			if (Filter.Current.LocationID != null)
			{
				slctdmatl.WhereAnd<Where<AMProdMatl.locationID, Equal<Current<ProductionWhereUsedFilter.locationID>>,
					Or<Where<AMProdMatl.locationID, IsNull,
					And<AMProdItem.locationID, Equal<Current<ProductionWhereUsedFilter.locationID>>>>>>>();
			}

			if (Filter.Current.ProductionStatusID != null)
			{
				slctdmatl.WhereAnd<Where<AMProdItem.statusID, Equal<Current<ProductionWhereUsedFilter.productionStatusID>>>>();
			}

			foreach (PXResult<AMProdMatl, AMProdMatlLotSerial, InventoryItem, AMProdItem, AMProdItemSplit, BAccount, ParentInventoryItem, INLotSerClass> result in slctdmatl.Select())
			{
				var matlItem = (AMProdMatl)result;
				var invtItem = (InventoryItem)result;
				var matlLotSerial = (AMProdMatlLotSerial)result;
				var prodItem = (AMProdItem)result;
				var prodItemSplit = (AMProdItemSplit)result;
				var account = (BAccount)result;
				var invtItemParent = (ParentInventoryItem)result;
				var lotSerClass = (INLotSerClass)result;

				if (string.IsNullOrWhiteSpace(matlItem?.OrderType) || string.IsNullOrWhiteSpace(matlItem.ProdOrdID) || invtItem?.InventoryID == null)
				{
					continue;
				}

				var whereUsedRecord = WriteWhereUsedRecord(1, matlItem, invtItem, matlLotSerial, prodItem, prodItemSplit, account, lotSerClass,
					invtItemParent);

				whereUsedRecord.ComponentLotSerialNbr = matlLotSerial.LotSerialNbr;
				whereUsedRecord.ComponentInventoryID = matlItem.InventoryID;
				whereUsedRecord.ComponentDescr = invtItem.Descr;
				whereUsedRecord.InventoryID = matlItem.InventoryID;
				whereUsedRecord.LotSerialNbr = matlLotSerial.LotSerialNbr;
				whereUsedRecord.Descr = invtItem.Descr;

				productionWhereUsedList.Add(whereUsedRecord);

				if (Filter.Current.MultiLevel.GetValueOrDefault() && isLotTrackedItem)
				{
					try
					{
						LoadDataRecords(2, whereUsedRecord, productionWhereUsedList);
					}
					catch (Exception)
					{
						throw new PXArgumentException(nameof(prodItem));
					}
				}
			}

			return productionWhereUsedList;
		}

		protected virtual void LoadDataRecords(int level, WhereUsedProductionDetail componentWhereUsed, List<WhereUsedProductionDetail> productionWhereUsedList)
		{
			if (Filter?.Current == null)
			{
				throw new PXArgumentException(nameof(Filter));
			}

			if(level >= LowLevel.MaxLowLevel)
			{
				throw new PXArgumentException(nameof(level));
			}

			foreach (PXResult<AMProdMatl, AMProdMatlLotSerial, InventoryItem, AMProdItem, AMProdItemSplit, BAccount, ParentInventoryItem, INLotSerClass> result in PXSelectJoin<AMProdMatl,
				InnerJoin<AMProdMatlLotSerial, On<AMProdMatlLotSerial.orderType, Equal<AMProdMatl.orderType>,
					And<AMProdMatlLotSerial.prodOrdID, Equal<AMProdMatl.prodOrdID>,
					And<AMProdMatlLotSerial.operationID, Equal<AMProdMatl.operationID>,
					And<AMProdMatlLotSerial.lineID, Equal<AMProdMatl.lineID>>>>>,
				InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<AMProdMatl.inventoryID>>,
				InnerJoin<AMProdItem, On<AMProdItem.orderType, Equal<AMProdMatlLotSerial.orderType>,
					And<AMProdItem.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>>>,
				LeftJoin<AMProdItemSplit, On<AMProdItemSplit.orderType, Equal<AMProdMatlLotSerial.orderType>,
					And<AMProdItemSplit.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>,
					And<AMProdItemSplit.lotSerialNbr, Equal<AMProdMatlLotSerial.parentLotSerialNbr>>>>,
				LeftJoin<BAccount, On<BAccount.bAccountID, Equal<AMProdItem.customerID>>,
				InnerJoin<ParentInventoryItem, On<ParentInventoryItem.parentInventoryID, Equal<AMProdItem.inventoryID>>,
				InnerJoin<INLotSerClass, On<INLotSerClass.lotSerClassID, Equal<ParentInventoryItem.parentLotSerClassID>>>>>>>>>,
				Where<AMProdMatlLotSerial.lotSerialNbr, Equal<Required<AMProdMatlLotSerial.lotSerialNbr>>,
					And<AMProdMatl.inventoryID, Equal<Required<AMProdMatl.inventoryID>>>
					>>.Select(this, componentWhereUsed.ParentLotSerialNbr, componentWhereUsed.ParentInventoryID))
			{
				var matlItem = (AMProdMatl)result;
				var invtItem = (InventoryItem)result;
				var matlLotSerial = (AMProdMatlLotSerial)result;
				var prodItem = (AMProdItem)result;
				var prodItemSplit = (AMProdItemSplit)result;
				var account = (BAccount)result;
				var parentInvItem = (ParentInventoryItem)result;
				var lotSerClass = new INLotSerClass();

				if (string.IsNullOrWhiteSpace(matlItem?.OrderType) || string.IsNullOrWhiteSpace(matlItem.ProdOrdID) || invtItem?.InventoryID == null)
				{
					continue;
				}

				var productionWhereUsed = WriteWhereUsedRecord(level, matlItem, invtItem, matlLotSerial, prodItem, prodItemSplit, account, lotSerClass, parentInvItem, componentWhereUsed);
				productionWhereUsedList.Add(productionWhereUsed);

				if (Filter.Current.MultiLevel.GetValueOrDefault())
				{
					try
					{
						LoadDataRecords(level+1, productionWhereUsed, productionWhereUsedList);
					}
					catch (Exception e)
					{
						throw e;
					}
				}
			}
		}

		protected virtual WhereUsedProductionDetail WriteWhereUsedRecord(int level, AMProdMatl matlItem, InventoryItem invtItem, AMProdMatlLotSerial matlLotSerial, AMProdItem prodItem,
			AMProdItemSplit prodItemSplit, BAccount custAccount, INLotSerClass lotSerClass, ParentInventoryItem parentInvItem, WhereUsedProductionDetail whereUsedComponentDetail)
		{
			var record = WriteWhereUsedRecord(level, matlItem, invtItem, matlLotSerial, prodItem,
				prodItemSplit, custAccount, lotSerClass, parentInvItem);
			record.ComponentLotSerialNbr = whereUsedComponentDetail.ParentLotSerialNbr;
			record.ComponentInventoryID = whereUsedComponentDetail.ParentInventoryID;
			record.ComponentDescr = whereUsedComponentDetail.ParentDescr;
			record.InventoryID = whereUsedComponentDetail.InventoryID;
			record.LotSerialNbr = whereUsedComponentDetail.LotSerialNbr;
			record.Descr = whereUsedComponentDetail.Descr;

			return record;
		}

		protected virtual WhereUsedProductionDetail WriteWhereUsedRecord(int level, AMProdMatl matlItem, InventoryItem invtItem, AMProdMatlLotSerial matlLotSerial, AMProdItem prodItem,
			AMProdItemSplit prodItemSplit, BAccount custAccount, INLotSerClass lotSerClass, ParentInventoryItem parentInvItem)
		{
			var record = new WhereUsedProductionDetail();
			record.ParentInventoryID = prodItem.InventoryID;
			record.ParentLotSerialNbr = matlLotSerial.ParentLotSerialNbr;
			record.Level = level;
			record.OrderType = matlItem.OrderType;
			record.ProdOrdID = matlItem.ProdOrdID;
			record.OperationID = matlItem.OperationID;
			record.QtyIssued = matlLotSerial.QtyIssued;
			record.UOM = invtItem.BaseUnit;
			record.SiteID = matlItem.SiteID;
			record.LocationID = matlItem.LocationID;
			record.SalesOrderType = prodItem.OrdTypeRef;
			record.SalesOrderNbr = prodItem.OrdNbr;
			record.ProductionStatusID = prodItem.StatusID;
			record.CustomerID = custAccount.BAccountID;
			record.CustomerName = custAccount.AcctName;
			record.ScheduleStatus = prodItem.ScheduleStatus;
			record.ProdDate = prodItem.ProdDate;
			record.ConstDate = prodItem.ConstDate;
			record.StartDate = prodItem.StartDate;
			record.EndDate = prodItem.EndDate;
			record.ParentDescr = parentInvItem.ParentDescription;
			record.ParentUOM = parentInvItem.ParentBaseUOM;

			bool isLotTrackedParent = lotSerClass.LotSerTrack != "N";

			record.ParentQty = isLotTrackedParent ? prodItemSplit.BaseQty : prodItem.BaseQtytoProd;
			record.ParentQtyComplete = isLotTrackedParent ? prodItemSplit.BaseQtyComplete : prodItem.BaseQtyComplete;
			record.ParentQtyScrapped = isLotTrackedParent ? prodItemSplit.BaseQtyScrapped : prodItem.BaseQtyScrapped;
			record.ParentQtyRemaining = isLotTrackedParent ? prodItemSplit.BaseQtyRemaining : prodItem.BaseQtyRemaining;

			return record;
		}

	}

	/// <summary>
	/// Where used inquiry Parent Inventory
	/// </summary>
	[PXProjection(typeof(Select<InventoryItem>), Persistent = false)]
	[Serializable]
	[PXCacheName("Parent Inventory Item")]
	public class ParentInventoryItem : IBqlTable
	{
		#region ParentLotSerClassID
		public abstract class parentLotSerClassID : PX.Data.BQL.BqlString.Field<parentLotSerClassID> { }
		protected string _ParentLotSerClassID;
		[PXDBString(10, IsUnicode = true, BqlField = typeof(InventoryItem.lotSerClassID))]
		[PXSelector(typeof(INLotSerClass.lotSerClassID), DescriptionField = typeof(INLotSerClass.descr), CacheGlobal = true)]
		[PXUIField(DisplayName = "Lot/Serial Class")]
		public virtual String ParentLotSerClassID 
		{
			get
			{
				return this._ParentLotSerClassID;
			}
			set
			{
				this._ParentLotSerClassID = value;
			}
		}
		#endregion
		#region ParentInventoryID
		public abstract class parentInventoryID : PX.Data.BQL.BqlInt.Field<parentInventoryID> { }

		protected Int32? _ParentInventoryID;
		[Inventory(DisplayName = "Parent Inventory ID", BqlField = typeof(InventoryItem.inventoryID),
			Visibility = PXUIVisibility.SelectorVisible)]
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
		#region ParentDescription
		public abstract class parentDescription : PX.Data.BQL.BqlString.Field<parentDescription> { }

		protected String _ParentDescription;
		[PXDBString(256, BqlField = typeof(InventoryItem.descr))]
		[PXUIField(DisplayName = "Parent Description")]
		public virtual String ParentDescription
		{
			get
			{
				return this._ParentDescription;
			}
			set
			{
				this._ParentDescription = value;
			}
		}
		#endregion
		#region ParentBaseUOM
		public abstract class parentBaseUOM : PX.Data.BQL.BqlString.Field<parentBaseUOM> { }

		protected String _ParentBaseUOM;
		[PXDBString(6, BqlField = typeof(InventoryItem.baseUnit))]
		[PXUIField(DisplayName = "Parent UOM")]
		public virtual String ParentBaseUOM
		{
			get
			{
				return this._ParentBaseUOM;
			}
			set
			{
				this._ParentBaseUOM = value;
			}
		}
		#endregion
	}

	/// <summary>
	/// Production Where used inquiry filter
	/// </summary>
	[Serializable]
	[PXCacheName("Production Where Used Filter")]
	public class ProductionWhereUsedFilter : IBqlTable
	{
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		protected Int32? _InventoryID;
		[Inventory]
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
		#region LotSerialNbr
		public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
		protected String _LotSerialNbr;
		[PXSelector(typeof(Search5<AMProdMatlLotSerial.lotSerialNbr,
			InnerJoin<AMProdMatl, On<AMProdMatl.orderType, Equal<AMProdMatlLotSerial.orderType>,
				And<AMProdMatl.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>,
				And<AMProdMatl.operationID, Equal<AMProdMatlLotSerial.operationID>,
				And<AMProdMatl.lineID, Equal<AMProdMatlLotSerial.lineID>>>>>>,
			Where<AMProdMatl.inventoryID, Equal<Current<ProductionWhereUsedFilter.inventoryID>>>,
			Aggregate<GroupBy<AMProdMatlLotSerial.lotSerialNbr>>>), ValidateValue = false)]
		[PXString(100)]
		[PXUIField(DisplayName = "Lot/Serial Nbr.")]
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
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }

		protected Int32? _SiteID;
		[Site]
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
		[Location(typeof(ProductionWhereUsedFilter.siteID), KeepEntry = false, DescriptionField = typeof(INLocation.descr))]
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
		#region ProductionStatusID
		public abstract class productionStatusID : PX.Data.BQL.BqlString.Field<productionStatusID> { }

		protected String _ProductionStatusID;
		[PXString(1)]
		[PXUIField(DisplayName = "Production Status")]
		[ProductionOrderStatus.List]
		public virtual String ProductionStatusID
		{
			get
			{
				return this._ProductionStatusID;
			}
			set
			{
				this._ProductionStatusID = value;
			}
		}
		#endregion
		#region MultiLevel
		public abstract class multiLevel : PX.Data.BQL.BqlBool.Field<multiLevel> { }

		protected Boolean? _MultiLevel;
		[PXBool]
		[PXUnboundDefault(true)]
		[PXUIField(DisplayName = "Multi-Level")]
		public virtual Boolean? MultiLevel
		{
			get
			{
				return this._MultiLevel;
			}
			set
			{
				this._MultiLevel = value;
			}
		}
		#endregion
	}

	/// <summary>
	/// Production Where used inquiry
	/// </summary>
	[Serializable]
	[PXCacheName("Where Used Production Detail")]
	public class WhereUsedProductionDetail : IBqlTable
	{
		#region ParentInventoryID
		public abstract class parentInventoryID : PX.Data.BQL.BqlInt.Field<parentInventoryID> { }

		protected Int32? _ParentInventoryID;
		[AnyInventory(DisplayName = "Parent Inventory ID")]
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
		[PXString(256)]
		[PXUIField(DisplayName = "Parent Description")]
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
		#region ParentLotSerialNbr

		public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }

		protected String _ParentLotSerialNbr;
		[PXString(100)]
		[PXUIField(DisplayName = "Parent Lot/Serial Nbr.", FieldClass = "LotSerial")]
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
		#region ComponentInventoryID
		public abstract class componentInventoryID : PX.Data.BQL.BqlInt.Field<componentInventoryID> { }

		protected Int32? _ComponentInventoryID;
		[AnyInventory(DisplayName = "Component Inventory ID")]
		public virtual Int32? ComponentInventoryID
		{
			get
			{
				return this._ComponentInventoryID;
			}
			set
			{
				this._ComponentInventoryID = value;
			}
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }

		protected Int32? _SubItemID;
		[SubItem(typeof(WhereUsedProductionDetail.inventoryID))]
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
		#region ComponentDescr
		public abstract class componentDescr : PX.Data.BQL.BqlString.Field<componentDescr> { }

		protected String _ComponentDescr;
		[PXString(256)]
		[PXUIField(DisplayName = "Component Description", Visible = false)]
		public virtual String ComponentDescr
		{
			get
			{
				return this._ComponentDescr;
			}
			set
			{
				this._ComponentDescr = value;
			}
		}
		#endregion
		#region ComponentLotSerialNbr

		public abstract class componentLotSerialNbr : PX.Data.BQL.BqlString.Field<componentLotSerialNbr> { }

		protected String _ComponentLotSerialNbr;
		[PXString(100)]
		[PXUIField(DisplayName = "Component Lot/Serial Nbr.", FieldClass = "LotSerial")]
		public virtual String ComponentLotSerialNbr
		{
			get
			{
				return this._ComponentLotSerialNbr;
			}
			set
			{
				this._ComponentLotSerialNbr = value;
			}
		}
		#endregion
		#region Level
		public abstract class level : PX.Data.BQL.BqlString.Field<level> { }

		protected int? _Level;
		[PXInt(IsKey = true)]
		[PXUIField(DisplayName = "Level")]
		public virtual int? Level
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
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		protected String _OrderType;
		[AMOrderTypeField(IsKey = true)]
		[AMOrderTypeSelector(ValidateValue = false)]
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
		#region ProdOrdID
		public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

		protected String _ProdOrdID;
		[ProductionNbr(IsKey = true)]
		[PXUIField(DisplayName = "Production Nbr")]
		[ProductionOrderSelector(typeof(WhereUsedProductionDetail.orderType), true, ValidateValue = false)]
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
		#region OperationID
		public abstract class operationID : PX.Data.BQL.BqlInt.Field<operationID> { }

		protected int? _OperationID;
		[PXSelector(typeof(Search<AMProdOper.operationID,
				Where<AMProdOper.orderType, Equal<Current<WhereUsedProductionDetail.orderType>>,
					And<AMProdOper.prodOrdID, Equal<Current<WhereUsedProductionDetail.prodOrdID>>>>>),
			SubstituteKey = typeof(AMProdOper.operationCD))]
		[OperationIDField(IsKey = true)]
		[PXUIField(DisplayName = "Operation ID")]
		public virtual int? OperationID
		{
			get
			{
				return this._OperationID;
			}
			set
			{
				this._OperationID = value;
			}
		}
		#endregion
		#region LineID
		public abstract class lineID : PX.Data.BQL.BqlInt.Field<lineID> { }

		protected Int32? _LineID;
		[PXInt(IsKey = true)]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, Enabled = false)]
		public virtual Int32? LineID
		{
			get
			{
				return this._LineID;
			}
			set
			{
				this._LineID = value;
			}
		}
		#endregion
		#region QtyIssued
		public abstract class qtyIssued : PX.Data.BQL.BqlDecimal.Field<qtyIssued> { }

		protected Decimal? _QtyIssued;
		[PXDecimal()]
		[PXUIField(DisplayName = "Qty. Issued")]
		public virtual Decimal? QtyIssued
		{
			get
			{
				return this._QtyIssued;
			}
			set
			{
				this._QtyIssued = value;
			}
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		protected String _UOM;
		[PXString(6)]
		[PXUIField(DisplayName = "UOM")]
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
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }

		protected Int32? _SiteID;
		[Site(DescriptionField = typeof(INSite.descr), ValidateValue = false)]
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
		[Location(typeof(WhereUsedProductionDetail.siteID), KeepEntry = false, DescriptionField = typeof(INLocation.descr), ValidateValue= false)]
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
		#region SalesOrderType
		public abstract class salesOrderType : PX.Data.BQL.BqlString.Field<salesOrderType> { }

		protected String _SalesOrderType;
		[PXDBString(2)]
		[PXUIField(DisplayName = "Sales Order Type")]
		public virtual String SalesOrderType
		{
			get
			{
				return this._SalesOrderType;
			}
			set
			{
				this._SalesOrderType = value;
			}
		}
		#endregion
		#region SalesOrderNbr
		public abstract class salesOrderNbr : PX.Data.BQL.BqlString.Field<salesOrderNbr> { }

		protected String _SalesOrderNbr;
		[PXUIField(DisplayName = "Sales Order Nbr.") ]
		[PXDBString(15)]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<WhereUsedProductionDetail.salesOrderType>>>>))]
		public virtual String SalesOrderNbr
		{
			get
			{
				return this._SalesOrderNbr;
			}
			set
			{
				this._SalesOrderNbr = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		protected Int32? _InventoryID;
		[AnyInventory(DisplayName = "Inventory ID", Visible = false)]
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
		[PXString(256)]
		[PXUIField(DisplayName = "Description")]
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
		[PXString(100)]
		[PXUIField(DisplayName = "Lot/Serial Nbr.", FieldClass = "LotSerial", Visible = false)]
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
		#region ProductionStatusID
		public abstract class productionStatusID : PX.Data.BQL.BqlString.Field<productionStatusID> { }

		protected String _ProductionStatusID;
		[PXString(1)]
		[PXUIField(DisplayName = "Production Status", Visible = false)]
		[ProductionOrderStatus.List]
		public virtual String ProductionStatusID
		{
			get
			{
				return this._ProductionStatusID;
			}
			set
			{
				this._ProductionStatusID = value;
			}
		}
		#endregion
		#region ComponentOrderType
		public abstract class componentOrderType : PX.Data.BQL.BqlString.Field<componentOrderType> { }

		protected String _ComponentOrderType;
		[AMOrderTypeField]
		[PXUIField(DisplayName = "Component Order Type", Visible = false)]
		[AMOrderTypeSelector(ValidateValue = false)]
		public virtual String ComponentOrderType
		{
			get
			{
				return this._ComponentOrderType;
			}
			set
			{
				this._ComponentOrderType = value;
			}
		}
		#endregion
		#region ComponentProdOrdID
		public abstract class componentProdOrdID : PX.Data.BQL.BqlString.Field<componentProdOrdID> { }

		protected String _ComponentProdOrdID;
		[ProductionNbr]
		[PXUIField(DisplayName = "Production Nbr", Visible = false)]
		[ProductionOrderSelector(typeof(WhereUsedProductionDetail.componentOrderType), true, ValidateValue = false)]
		public virtual String ComponentProdOrdID
		{
			get
			{
				return this._ComponentProdOrdID;
			}
			set
			{
				this._ComponentProdOrdID = value;
			}
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		protected Int32? _CustomerID;
		[Customer(ValidateValue = false, Visible = false)]
		public virtual Int32? CustomerID
		{
			get
			{
				return this._CustomerID;
			}
			set
			{
				this._CustomerID = value;
			}
		}
		#endregion
		#region CustomerName
		public abstract class customerName : PX.Data.BQL.BqlString.Field<customerName> { }

		protected String _CustomerName;
		[PXString(255)]
		[PXUIField(DisplayName = "Customer Name", Visible = false)]
		public virtual String CustomerName
		{
			get
			{
				return this._CustomerName;
			}
			set
			{
				this._CustomerName = value;
			}
		}
		#endregion
		#region ScheduleStatus
		public abstract class scheduleStatus : PX.Data.BQL.BqlString.Field<scheduleStatus> { }

		[PXString(1)]
		[PXUIField(DisplayName = "Schedule Status", FieldClass = Features.ADVANCEDPLANNINGFIELDCLASS)]
		[ProductionScheduleStatus.List]
		public virtual string ScheduleStatus { get; set; }
		#endregion
		#region ProdDate
		public abstract class prodDate : PX.Data.BQL.BqlDateTime.Field<prodDate> { }

		protected DateTime? _ProdDate;
		[PXDate]
		[PXUIField(DisplayName = "Order Date", Visible = false)]
		public virtual DateTime? ProdDate
		{
			get
			{
				return this._ProdDate;
			}
			set
			{
				this._ProdDate = value;
			}
		}
		#endregion
		#region ConstDate
		public abstract class constDate : PX.Data.BQL.BqlDateTime.Field<constDate> { }

		protected DateTime? _ConstDate;
		[PXDate]
		[PXUIField(DisplayName = "Constraint", Visible = false)]
		public virtual DateTime? ConstDate
		{
			get
			{
				return this._ConstDate;
			}
			set
			{
				this._ConstDate = value;
			}
		}
		#endregion
		#region StartDate
		public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }

		protected DateTime? _StartDate;
		[PXDate]
		[PXUIField(DisplayName = "Start Date", Visible = false)]
		public virtual DateTime? StartDate
		{
			get
			{
				return this._StartDate;
			}
			set
			{
				this._StartDate = value;
			}
		}
		#endregion
		#region EndDate
		public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }

		protected DateTime? _EndDate;
		[PXDate]
		[PXUIField(DisplayName = "End Date", Visible = false)]
		public virtual DateTime? EndDate
		{
			get
			{
				return this._EndDate;
			}
			set
			{
				this._EndDate = value;
			}
		}
		#endregion
		#region ParentQty
		public abstract class parentQty : PX.Data.BQL.BqlDecimal.Field<parentQty> { }

		protected Decimal? _ParentQty;
		[PXDecimal]
		[PXUIField(DisplayName = "Parent Qty.", Visible = false)]
		public virtual Decimal? ParentQty
		{
			get
			{
				return this._ParentQty;
			}
			set
			{
				this._ParentQty = value;
			}
		}
		#endregion
		#region ParentQtyComplete
		public abstract class parentQtyComplete : PX.Data.BQL.BqlDecimal.Field<parentQtyComplete> { }

		protected Decimal? _ParentQtyComplete;
		[PXDecimal]
		[PXUIField(DisplayName = "Parent Complete Qty.", Visible = false)]
		public virtual Decimal? ParentQtyComplete
		{
			get
			{
				return this._ParentQtyComplete;
			}
			set
			{
				this._ParentQtyComplete = value;
			}
		}
		#endregion
		#region ParentQtyScrapped
		public abstract class parentQtyScrapped : PX.Data.BQL.BqlDecimal.Field<parentQtyScrapped> { }

		protected Decimal? _ParentQtyScrapped;
		[PXDecimal]
		[PXUIField(DisplayName = "Parent Scrapped Qty.", Visible = false)]
		public virtual Decimal? ParentQtyScrapped
		{
			get
			{
				return this._ParentQtyScrapped;
			}
			set
			{
				this._ParentQtyScrapped = value;
			}
		}
		#endregion
		#region ParentQtyRemaining
		public abstract class parentQtyRemaining : PX.Data.BQL.BqlDecimal.Field<parentQtyRemaining> { }

		protected Decimal? _ParentQtyRemaining;
		[PXDecimal]
		[PXUIField(DisplayName = "Parent Remaining Qty.", Visible = false)]
		public virtual Decimal? ParentQtyRemaining
		{
			get
			{
				return this._ParentQtyRemaining;
			}
			set
			{
				this._ParentQtyRemaining = value;
			}
		}
		#endregion
		#region ParentUOM
		public abstract class parentUOM : PX.Data.BQL.BqlString.Field<parentUOM> { }

		protected String _ParentUOM;
		[PXString(6)]
		[PXUIField(DisplayName = "Parent UOM", Visible = false)]
		public virtual String ParentUOM
		{
			get
			{
				return this._ParentUOM;
			}
			set
			{
				this._ParentUOM = value;
			}
		}
		#endregion
	}
}
