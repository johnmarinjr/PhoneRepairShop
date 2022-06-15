using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    [Serializable]
    [PXCacheName(Messages.ProductionMatlLotSerial)]
    public class AMProdMatlLotSerial : IBqlTable
    {
        internal string DebuggerDisplay => $"[{OrderType}:{ProdOrdID}] OperationID = {OperationID}, LineID = {LineID}, " +
            $"LotSerialNbr = {LotSerialNbr}, ParentLotSerialNbr = {ParentLotSerialNbr}";

        #region Keys

        public class PK : PrimaryKeyOf<AMProdMatlLotSerial>.By<orderType, prodOrdID, operationID, lineID, lotSerialNbr, parentLotSerialNbr>
        {
            public static AMProdMatlLotSerial Find(PXGraph graph, string orderType, string prodOrdID, int? operationID, int? lineID, 
                string lotSerialNbr, string parentLotSerialNbr) 
                => FindBy(graph, orderType, prodOrdID, operationID, lineID, lotSerialNbr, parentLotSerialNbr);
            public static AMProdMatlLotSerial FindDirty(PXGraph graph, string orderType, string prodOrdID, int? operationID, int? lineID,
                string lotSerialNbr, string parentLotSerialNbr)
                => PXSelect<AMProdMatlLotSerial,
                    Where<orderType, Equal<Required<orderType>>,
                        And<prodOrdID, Equal<Required<prodOrdID>>,
                        And<operationID, Equal<Required<operationID>>,
                        And<lineID, Equal<Required<lineID>>,
                            And<lotSerialNbr, Equal<Required<lotSerialNbr>>,
                                And<parentLotSerialNbr, Equal<Required<parentLotSerialNbr>>>>>>>>>
                    .SelectWindowed(graph, 0, 1, orderType, prodOrdID, operationID, lineID, lotSerialNbr, parentLotSerialNbr);
        }

		public static class FK
        {
            public class OrderType : AMOrderType.PK.ForeignKeyOf<AMProdMatlLotSerial>.By<orderType> { }
            public class ProductionOrder : AMProdItem.PK.ForeignKeyOf<AMProdMatlLotSerial>.By<orderType, prodOrdID> { }
            public class Operation : AMProdOper.PK.ForeignKeyOf<AMProdMatlLotSerial>.By<orderType, prodOrdID, operationID> { }
            public class Material : AMProdMatl.PK.ForeignKeyOf<AMProdMatlLotSerial>.By<orderType, prodOrdID, operationID, lineID> { }
        }

        #endregion

        #region OrderType
        public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

        protected String _OrderType;
        [AMOrderTypeField(IsKey = true, Visible = false, Enabled = false)]
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
        [ProductionNbr(IsKey = true, Visible = false, Enabled = false)]
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
        [OperationIDField(IsKey = true, Visible = false, Enabled = false)]
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
        [PXDBInt(IsKey = true)]
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
        #region LotSerialNbr

        public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }

        protected String _LotSerialNbr;
        [PXDBString(100, IsUnicode = true, IsKey = true)]
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
        #region ParentLotSerialNbr

        public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }

        protected String _ParentLotSerialNbr;

        [PXDBString(INLotSerialStatus.lotSerialNbr.LENGTH, IsUnicode = true, IsKey = true)]
        [PXDefault("")]
        [PXUIField(DisplayName = "Parent Lot/Serial Nbr.")]
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
        #region QtyIssued
        public abstract class qtyIssued : PX.Data.BQL.BqlDecimal.Field<qtyIssued> { }

        protected Decimal? _QtyIssued;
        [PXDBDecimal()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Issued Qty.", Enabled = false)]
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
        #region CreatedByID

        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

        protected Guid? _CreatedByID;
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID
        {
            get
            {
                return this._CreatedByID;
            }
            set
            {
                this._CreatedByID = value;
            }
        }
        #endregion
        #region CreatedByScreenID

        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

        protected String _CreatedByScreenID;
        [PXDBCreatedByScreenID]
        public virtual String CreatedByScreenID
        {
            get
            {
                return this._CreatedByScreenID;
            }
            set
            {
                this._CreatedByScreenID = value;
            }
        }
        #endregion
        #region CreatedDateTime

        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

        protected DateTime? _CreatedDateTime;
        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime
        {
            get
            {
                return this._CreatedDateTime;
            }
            set
            {
                this._CreatedDateTime = value;
            }
        }
        #endregion
        #region LastModifiedByID

        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

        protected Guid? _LastModifiedByID;
        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID
        {
            get
            {
                return this._LastModifiedByID;
            }
            set
            {
                this._LastModifiedByID = value;
            }
        }
        #endregion
        #region LastModifiedByScreenID

        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

        protected String _LastModifiedByScreenID;
        [PXDBLastModifiedByScreenID]
        public virtual String LastModifiedByScreenID
        {
            get
            {
                return this._LastModifiedByScreenID;
            }
            set
            {
                this._LastModifiedByScreenID = value;
            }
        }
        #endregion
        #region LastModifiedDateTime

        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

        protected DateTime? _LastModifiedDateTime;
        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime
        {
            get
            {
                return this._LastModifiedDateTime;
            }
            set
            {
                this._LastModifiedDateTime = value;
            }
        }
        #endregion       
    }

    /// <summary>
    /// Material Lot Serial Assigned
    /// </summary>
    [PXProjection(typeof(Select2<AMProdMatlLotSerial,
        InnerJoin<AMProdMatl, 
            On<AMProdMatl.orderType, Equal<AMProdMatlLotSerial.orderType>,
                And<AMProdMatl.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>,
                And<AMProdMatl.operationID, Equal<AMProdMatlLotSerial.operationID>,
                And<AMProdMatl.lineID, Equal<AMProdMatlLotSerial.lineID>>>>>,
        InnerJoin<InventoryItem,
            On<InventoryItem.inventoryID, Equal<AMProdMatl.inventoryID>>>>>), Persistent = false)]
    [Serializable]
    [PXCacheName("Material Lot Serial Assigned")]
    public class AMProdMatlLotSerialAssigned : IBqlTable
    {
        #region OrderType
        public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

        protected String _OrderType;
        [AMOrderTypeField(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.orderType))]
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
        [ProductionNbr(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.prodOrdID))]
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
        [PXDBInt(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.operationID))]
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
        [PXDBInt(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.lineID))]  
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
        #region InventoryID
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

        protected Int32? _InventoryID;
        [StockItem(Enabled = false, BqlField = typeof(AMProdMatl.inventoryID))]
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
        [PXDBString(256, IsUnicode = true, BqlField = typeof(AMProdMatl.descr))]
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
        [PXDBString(100, IsKey = true, IsUnicode = true, BqlField = typeof(AMProdMatlLotSerial.lotSerialNbr))]
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
        #region ParentLotSerialNbr

        public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }

        protected String _ParentLotSerialNbr;
        [PXDBString(100, IsKey = true, IsUnicode =true, BqlField = typeof(AMProdMatlLotSerial.parentLotSerialNbr))]
        [PXUIField(DisplayName = "Parent Lot/Serial Nbr.")]
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
        #region QtyIssued
        public abstract class qtyIssued : PX.Data.BQL.BqlString.Field<qtyIssued> { }

        protected Decimal? _QtyIssued;
        [PXDBDecimal(BqlField = typeof(AMProdMatlLotSerial.qtyIssued))]
        [PXUIField(DisplayName = "Qty. Allocated")]
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
		#region BaseUnit
		public abstract class baseUnit : PX.Data.BQL.BqlString.Field<baseUnit> { }
        protected String _BaseUnit;
        [PXUIField(DisplayName = "UOM")]
        [INUnit(BqlField = typeof(InventoryItem.baseUnit))]
        public virtual String BaseUnit
        {
            get
            {
                return this._BaseUnit;
            }
            set
            {
                this._BaseUnit = value;
            }
        }
		#endregion
		#region BatchSize
		public abstract class batchSize : PX.Data.BQL.BqlDecimal.Field<batchSize> { }

		protected Decimal? _BatchSize;
		[BatchSize(BqlField = typeof(AMProdMatl.batchSize))]
		//[PXDefault(TypeCode.Decimal, "1.0")]
		public virtual Decimal? BatchSize
		{
			get
			{
				return this._BatchSize;
			}
			set
			{
				this._BatchSize = value;
			}
		}
		#endregion
		#region QtyReq
		public abstract class qtyReq : PX.Data.BQL.BqlDecimal.Field<qtyReq> { }

		protected Decimal? _QtyReq;
		[PXDBQuantity(BqlField = typeof(AMProdMatl.qtyReq))]
		//[PXDefault(TypeCode.Decimal, "1.0")]
		[PXUIField(DisplayName = "Qty Required")]
		public virtual Decimal? QtyReq
		{
			get
			{
				return this._QtyReq;
			}
			set
			{
				this._QtyReq = value;
			}
		}
		#endregion
		#region ScrapFactor
		public abstract class scrapFactor : PX.Data.BQL.BqlDecimal.Field<scrapFactor> { }

		protected Decimal? _ScrapFactor;
		[PXDBDecimal(6, MinValue = 0.0, BqlField = typeof(AMProdMatl.scrapFactor))]
		//[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Scrap Factor")]
		public virtual Decimal? ScrapFactor
		{
			get
			{
				return this._ScrapFactor;
			}
			set
			{
				this._ScrapFactor = value;
			}
		}
		#endregion
		#region CreatedByID

		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

        protected Guid? _CreatedByID;
        [PXDBCreatedByID(BqlField = typeof(AMProdMatlLotSerial.createdByID))]
        public virtual Guid? CreatedByID
        {
            get
            {
                return this._CreatedByID;
            }
            set
            {
                this._CreatedByID = value;
            }
        }
        #endregion
        #region CreatedByScreenID

        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

        protected String _CreatedByScreenID;
        [PXDBCreatedByScreenID(BqlField = typeof(AMProdMatlLotSerial.createdByScreenID))]
        public virtual String CreatedByScreenID
        {
            get
            {
                return this._CreatedByScreenID;
            }
            set
            {
                this._CreatedByScreenID = value;
            }
        }
        #endregion
        #region CreatedDateTime

        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

        protected DateTime? _CreatedDateTime;
        [PXDBCreatedDateTime(BqlField = typeof(AMProdMatlLotSerial.createdDateTime))]
        public virtual DateTime? CreatedDateTime
        {
            get
            {
                return this._CreatedDateTime;
            }
            set
            {
                this._CreatedDateTime = value;
            }
        }
        #endregion
        #region LastModifiedByID

        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

        protected Guid? _LastModifiedByID;
        [PXDBLastModifiedByID(BqlField = typeof(AMProdMatlLotSerial.lastModifiedByID))]
        public virtual Guid? LastModifiedByID
        {
            get
            {
                return this._LastModifiedByID;
            }
            set
            {
                this._LastModifiedByID = value;
            }
        }
        #endregion
        #region LastModifiedByScreenID

        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

        protected String _LastModifiedByScreenID;
        [PXDBLastModifiedByScreenID(BqlField = typeof(AMProdMatlLotSerial.lastModifiedByScreenID))]
        public virtual String LastModifiedByScreenID
        {
            get
            {
                return this._LastModifiedByScreenID;
            }
            set
            {
                this._LastModifiedByScreenID = value;
            }
        }
        #endregion
        #region LastModifiedDateTime

        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

        protected DateTime? _LastModifiedDateTime;
        [PXDBLastModifiedDateTime(BqlField = typeof(AMProdMatlLotSerial.lastModifiedDateTime))]
        public virtual DateTime? LastModifiedDateTime
        {
            get
            {
                return this._LastModifiedDateTime;
            }
            set
            {
                this._LastModifiedDateTime = value;
            }
        }
		#endregion
		#region QtyRequired
		public abstract class qtyRequired : PX.Data.BQL.BqlDecimal.Field<qtyRequired> { }

		protected Decimal? _QtyRequired;
		[PXQuantity()]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Required")]
		public virtual Decimal? QtyRequired
		{
			get
			{
				return this._QtyRequired;
			}
			set
			{
				this._QtyRequired = value;
			}
		}
		#endregion

		public static explicit operator AMProdMatlLotSerial(AMProdMatlLotSerialAssigned prodMatlAssigned)
        {
            return Convert(prodMatlAssigned);
        }

        public static AMProdMatlLotSerial Convert(AMProdMatlLotSerialAssigned prodMatlAssigned)
        {
            return new AMProdMatlLotSerial
            {
                OrderType = prodMatlAssigned.OrderType,
                ProdOrdID = prodMatlAssigned.ProdOrdID,
                OperationID = prodMatlAssigned.OperationID,
                LineID = prodMatlAssigned.LineID,
                LotSerialNbr = prodMatlAssigned.LotSerialNbr,
                ParentLotSerialNbr = prodMatlAssigned.ParentLotSerialNbr,
                QtyIssued = prodMatlAssigned.QtyIssued,
                CreatedByID = prodMatlAssigned.CreatedByID,
                CreatedByScreenID = prodMatlAssigned.CreatedByScreenID,
                CreatedDateTime = prodMatlAssigned.CreatedDateTime,
                LastModifiedByID = prodMatlAssigned.LastModifiedByID,
                LastModifiedByScreenID = prodMatlAssigned.LastModifiedByScreenID,
                LastModifiedDateTime = prodMatlAssigned.LastModifiedDateTime,
            };
        }

		public static explicit operator AMProdMatlLotSerialAssigned(AMProdMatlLotSerial prodMatlLot)
        {
            return new AMProdMatlLotSerialAssigned
            {
                OrderType = prodMatlLot.OrderType,
                ProdOrdID = prodMatlLot.ProdOrdID,
                OperationID = prodMatlLot.OperationID,
                LineID = prodMatlLot.LineID,
                LotSerialNbr = prodMatlLot.LotSerialNbr,
                ParentLotSerialNbr = prodMatlLot.ParentLotSerialNbr,
                QtyIssued = prodMatlLot.QtyIssued,
                CreatedByID = prodMatlLot.CreatedByID,
                CreatedByScreenID = prodMatlLot.CreatedByScreenID,
                CreatedDateTime = prodMatlLot.CreatedDateTime,
                LastModifiedByID = prodMatlLot.LastModifiedByID,
                LastModifiedByScreenID = prodMatlLot.LastModifiedByScreenID,
                LastModifiedDateTime = prodMatlLot.LastModifiedDateTime,
            };
        }
    }


    /// <summary>
    /// Material Lot Serial Unassigned
    /// </summary>
    [PXProjection(typeof(Select2<AMProdMatlLotSerial,
        InnerJoin<AMProdMatl, 
            On<AMProdMatl.orderType, Equal<AMProdMatlLotSerial.orderType>,
                And<AMProdMatl.prodOrdID, Equal<AMProdMatlLotSerial.prodOrdID>,
                And<AMProdMatl.operationID, Equal<AMProdMatlLotSerial.operationID>,
                And<AMProdMatl.lineID, Equal<AMProdMatlLotSerial.lineID>>>>>,
        InnerJoin<InventoryItem,
            On<InventoryItem.inventoryID, Equal<AMProdMatl.inventoryID>>>>>), Persistent = false)]
    [Serializable]
    [PXCacheName("Material Lot Serial Unassigned")]
    public class AMProdMatlLotSerialUnassigned : IBqlTable
    {
        #region OrderType
        public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

        protected String _OrderType;
        [AMOrderTypeField(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.orderType))]
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
        [ProductionNbr(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.prodOrdID))]
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
        [PXDBInt(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.operationID))]
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
        [PXDBInt(IsKey = true, BqlField = typeof(AMProdMatlLotSerial.lineID))]
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
        #region InventoryID
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

        protected Int32? _InventoryID;
        [StockItem(Enabled = false, BqlField = typeof(AMProdMatl.inventoryID))]
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
        [PXDBString(256, IsUnicode = true, BqlField = typeof(AMProdMatl.descr))]
        //[PXDefault(typeof(Search<InventoryItem.descr, Where<InventoryItem.inventoryID, 
        //    Equal<Current<AMProdMatl.inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
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
        [PXDBString(100, IsKey = true, IsUnicode = true, BqlField = typeof(AMProdMatlLotSerial.lotSerialNbr))]
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
        #region ParentLotSerialNbr

        public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }

        protected String _ParentLotSerialNbr;
        [PXDBString(100, IsKey = true, IsUnicode = true, BqlField = typeof(AMProdMatlLotSerial.parentLotSerialNbr))]
        [PXUIField(DisplayName = "Parent Lot/Serial Nbr.")]
		[PXDefault("")]
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
        #region QtyIssued
        public abstract class qtyIssued : PX.Data.BQL.BqlDecimal.Field<qtyIssued> { }

        protected Decimal? _QtyIssued;
        [PXDBDecimal(BqlField = typeof(AMProdMatlLotSerial.qtyIssued))]
        [PXUIField(DisplayName = "Qty. Unallocated")]
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
		#region BaseUnit
		public abstract class baseUnit : PX.Data.BQL.BqlString.Field<baseUnit> { }
        protected String _BaseUnit;
        [PXUIField(DisplayName = "UOM")]
		[INUnit(BqlField = typeof(InventoryItem.baseUnit))]
        public virtual String BaseUnit
        {
            get
            {
                return this._BaseUnit;
            }
            set
            {
                this._BaseUnit = value;
            }
        }
        #endregion
        #region CreatedByID

        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

        protected Guid? _CreatedByID;
        [PXDBCreatedByID(BqlField = typeof(AMProdMatlLotSerial.createdByID))]
        public virtual Guid? CreatedByID
        {
            get
            {
                return this._CreatedByID;
            }
            set
            {
                this._CreatedByID = value;
            }
        }
        #endregion
        #region CreatedByScreenID

        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

        protected String _CreatedByScreenID;
        [PXDBCreatedByScreenID(BqlField = typeof(AMProdMatlLotSerial.createdByScreenID))]
        public virtual String CreatedByScreenID
        {
            get
            {
                return this._CreatedByScreenID;
            }
            set
            {
                this._CreatedByScreenID = value;
            }
        }
        #endregion
        #region CreatedDateTime

        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

        protected DateTime? _CreatedDateTime;
        [PXDBCreatedDateTime(BqlField = typeof(AMProdMatlLotSerial.createdDateTime))]
        public virtual DateTime? CreatedDateTime
        {
            get
            {
                return this._CreatedDateTime;
            }
            set
            {
                this._CreatedDateTime = value;
            }
        }
        #endregion
        #region LastModifiedByID

        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

        protected Guid? _LastModifiedByID;
        [PXDBLastModifiedByID(BqlField = typeof(AMProdMatlLotSerial.lastModifiedByID))]
        public virtual Guid? LastModifiedByID
        {
            get
            {
                return this._LastModifiedByID;
            }
            set
            {
                this._LastModifiedByID = value;
            }
        }
        #endregion
        #region LastModifiedByScreenID

        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

        protected String _LastModifiedByScreenID;
        [PXDBLastModifiedByScreenID(BqlField = typeof(AMProdMatlLotSerial.lastModifiedByScreenID))]
        public virtual String LastModifiedByScreenID
        {
            get
            {
                return this._LastModifiedByScreenID;
            }
            set
            {
                this._LastModifiedByScreenID = value;
            }
        }
        #endregion
        #region LastModifiedDateTime

        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

        protected DateTime? _LastModifiedDateTime;
        [PXDBLastModifiedDateTime(BqlField = typeof(AMProdMatlLotSerial.lastModifiedDateTime))]
        public virtual DateTime? LastModifiedDateTime
        {
            get
            {
                return this._LastModifiedDateTime;
            }
            set
            {
                this._LastModifiedDateTime = value;
            }
        }
		#endregion
		#region BatchSize
		public abstract class batchSize : PX.Data.BQL.BqlDecimal.Field<batchSize> { }

		protected Decimal? _BatchSize;
		[BatchSize(BqlField = typeof(AMProdMatl.batchSize))]
		//[PXDefault(TypeCode.Decimal, "1.0")]
		public virtual Decimal? BatchSize
		{
			get
			{
				return this._BatchSize;
			}
			set
			{
				this._BatchSize = value;
			}
		}
		#endregion
		#region QtyReq
		public abstract class qtyReq : PX.Data.BQL.BqlDecimal.Field<qtyReq> { }

		protected Decimal? _QtyReq;
		[PXDBQuantity(BqlField = typeof(AMProdMatl.qtyReq))]
		[PXUIField(DisplayName = "Qty Required")]
		public virtual Decimal? QtyReq
		{
			get
			{
				return this._QtyReq;
			}
			set
			{
				this._QtyReq = value;
			}
		}
		#endregion
		#region ScrapFactor
		public abstract class scrapFactor : PX.Data.BQL.BqlDecimal.Field<scrapFactor> { }

		protected Decimal? _ScrapFactor;
		[PXDBDecimal(6, MinValue = 0.0, BqlField = typeof(AMProdMatl.scrapFactor))]
		//[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Scrap Factor")]
		public virtual Decimal? ScrapFactor
		{
			get
			{
				return this._ScrapFactor;
			}
			set
			{
				this._ScrapFactor = value;
			}
		}
		#endregion
		#region QtyRequired
		public abstract class qtyRequired : PX.Data.BQL.BqlDecimal.Field<qtyRequired> { }

		protected Decimal? _QtyRequired;
		[PXQuantity()]
		[PXUIField(DisplayName = "Qty. Required")]
		public virtual Decimal? QtyRequired
		{
			get
			{
				return this._QtyRequired;
			}
			set
			{
				this._QtyRequired = value;
			}
		}
		#endregion
		#region QtyToAllocate
		public abstract class qtyToAllocate : PX.Data.BQL.BqlDecimal.Field<qtyToAllocate> { }

		protected Decimal? _QtyToAllocate;
		[PXQuantity()]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		//[PXUnboundFormula(typeof(Switch<Case<Where<AMProdMatl.batchSize, Equal<decimal0>>, Mult<AMProdMatl.qtyReq, Add<decimal1, AMProdMatl.scrapFactor>>>,
		//	Mult<Mult<AMProdMatl.qtyReq, Add<decimal1, AMProdMatl.scrapFactor>>, Div<Parent<AMProdItemSplit.baseQty>, AMProdMatl.batchSize>>>))]
		[PXUIField(DisplayName = "Qty. to Allocate")]
		public virtual Decimal? QtyToAllocate
		{
			get
			{
				return this._QtyToAllocate;
			}
			set
			{
				this._QtyToAllocate = value;
			}
		}
		#endregion

		public static explicit operator AMProdMatlLotSerial(AMProdMatlLotSerialUnassigned prodMatlUnassigned)
        {
            return Convert(prodMatlUnassigned);
        }

        public static AMProdMatlLotSerial Convert(AMProdMatlLotSerialUnassigned prodMatlUnassigned)
        {
            return new AMProdMatlLotSerial
            {
                OrderType = prodMatlUnassigned.OrderType,
                ProdOrdID = prodMatlUnassigned.ProdOrdID,
                OperationID = prodMatlUnassigned.OperationID,
                LineID = prodMatlUnassigned.LineID,
                LotSerialNbr = prodMatlUnassigned.LotSerialNbr,
                ParentLotSerialNbr = string.Empty,
                QtyIssued = prodMatlUnassigned.QtyIssued,
                CreatedByID = prodMatlUnassigned.CreatedByID,
                CreatedByScreenID = prodMatlUnassigned.CreatedByScreenID,
                CreatedDateTime = prodMatlUnassigned.CreatedDateTime,
                LastModifiedByID = prodMatlUnassigned.LastModifiedByID,
                LastModifiedByScreenID = prodMatlUnassigned.LastModifiedByScreenID,
                LastModifiedDateTime = prodMatlUnassigned.LastModifiedDateTime,
            };
        }

		public static explicit operator AMProdMatl(AMProdMatlLotSerialUnassigned prodMatlUnassigned)
		{
			return new AMProdMatl
			{
				OrderType = prodMatlUnassigned.OrderType,
				ProdOrdID = prodMatlUnassigned.ProdOrdID,
				OperationID = prodMatlUnassigned.OperationID,
				LineID = prodMatlUnassigned.LineID,
				InventoryID = prodMatlUnassigned.InventoryID,
				QtyReq = prodMatlUnassigned.QtyReq,
				ScrapFactor = prodMatlUnassigned.ScrapFactor,
				BatchSize = prodMatlUnassigned.BatchSize,
				UOM = prodMatlUnassigned.BaseUnit,
				QtyRoundUp = false
			};
		}
    }
}
