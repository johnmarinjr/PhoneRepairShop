using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.PO
{
	[PXProjection(typeof(Select<POLine>), Persistent = true)]
	[Serializable]
	public partial class POLineR : IBqlTable, ISortOrder
	{
		#region Keys
		public class PK : PrimaryKeyOf<POLineR>.By<orderType, orderNbr, lineNbr>
		{
			public static POLineR Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr) => FindBy(graph, orderType, orderNbr, lineNbr);
		}
		public static class FK
		{
			public class OrderR : POOrderEntry.POOrderR.PK.ForeignKeyOf<POLineR>.By<orderType, orderNbr> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<POLineR>.By<inventoryID> { }
		}
		#endregion

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		protected String _OrderType;
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(POLine.orderType))]
		[PXDefault()]
		[PXUIField(DisplayName = "Order Type", Visibility = PXUIVisibility.Visible, Visible = false)]
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
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		protected String _OrderNbr;

		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POLine.orderNbr))]
		[PXDefault()]
		[PXParent(typeof(FK.OrderR))]
		[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.Invisible, Visible = false)]
		public virtual String OrderNbr
		{
			get
			{
				return this._OrderNbr;
			}
			set
			{
				this._OrderNbr = value;
			}
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;
		[PXDBInt(IsKey = true, BqlField = typeof(POLine.lineNbr))]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
		public virtual Int32? LineNbr
		{
			get
			{
				return this._LineNbr;
			}
			set
			{
				this._LineNbr = value;
			}
		}
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		protected Int32? _SortOrder;
		[PXDBInt(BqlField = typeof(POLine.sortOrder))]
		public virtual Int32? SortOrder
		{
			get
			{
				return this._SortOrder;
			}
			set
			{
				this._SortOrder = value;
			}
		}
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }
		protected String _LineType;
		[PXDBString(2, IsFixed = true, BqlField = typeof(POLine.lineType))]
		[PXUIField(DisplayName = "Line Type")]
		public virtual String LineType
		{
			get
			{
				return this._LineType;
			}
			set
			{
				this._LineType = value;
			}
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }
		protected Decimal? _ReceivedQty;
		[PXDBQuantity(BqlField = typeof(POLine.receivedQty))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Received Qty.", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? ReceivedQty
		{
			get
			{
				return this._ReceivedQty;
			}
			set
			{
				this._ReceivedQty = value;
			}
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }
		protected Decimal? _BaseReceivedQty;

		[PXDBBaseQuantity(typeof(POLineR.uOM), typeof(POLineR.receivedQty), BqlField = typeof(POLine.baseReceivedQty), HandleEmptyKey = true)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Base Received Qty.", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? BaseReceivedQty
		{
			get
			{
				return this._BaseReceivedQty;
			}
			set
			{
				this._BaseReceivedQty = value;
			}
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		protected Int64? _CuryInfoID;
		[PXDBLong(BqlField = typeof(POLine.curyInfoID))]
		public virtual Int64? CuryInfoID
		{
			get
			{
				return this._CuryInfoID;
			}
			set
			{
				this._CuryInfoID = value;
			}
		}
		#endregion
		#region CuryBLOrderedCost
		public abstract class curyBLOrderedCost : PX.Data.BQL.BqlDecimal.Field<curyBLOrderedCost> { }
		[PXDBCurrency(typeof(POLineR.curyInfoID), typeof(POLineR.bLOrderedCost), BqlField = typeof(POLine.curyBLOrderedCost))]
		[PXDefault]
		public virtual decimal? CuryBLOrderedCost
		{
			get;
			set;
		}
		#endregion
		#region BLOrderedCost
		public abstract class bLOrderedCost : PX.Data.BQL.BqlDecimal.Field<bLOrderedCost> { }
		[PXDBDecimal(6, BqlField = typeof(POLine.bLOrderedCost))]
		[PXDefault]
		public virtual decimal? BLOrderedCost
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;
		[INUnit(typeof(POLineR.inventoryID), DisplayName = "UOM", BqlField = typeof(POLine.uOM))]
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
		#region Completed
		public abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }
		protected Boolean? _Completed;
		[PXDBBool(BqlField = typeof(POLine.completed))]
		[PXDefault(false)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<lineType, NotEqual<POLineType.description>, And<completed, Equal<False>>>, int1>, int0>),
			typeof(SumCalc<POOrderEntry.POOrderR.linesToCompleteCntr>))]
		public virtual Boolean? Completed
		{
			get
			{
				return this._Completed;
			}
			set
			{
				this._Completed = value;
			}
		}
		#endregion
		#region Cancelled
		public abstract class cancelled : PX.Data.BQL.BqlBool.Field<cancelled> { }
		protected Boolean? _Cancelled;
		[PXDBBool(BqlField = typeof(POLine.cancelled))]
		[PXDefault(false)]
		public virtual Boolean? Cancelled
		{
			get
			{
				return this._Cancelled;
			}
			set
			{
				this._Cancelled = value;
			}
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }
		protected Decimal? _OrderQty;
		[PXDBDecimal(BqlField = typeof(POLine.orderQty))]
		public virtual Decimal? OrderQty
		{
			get
			{
				return this._OrderQty;
			}
			set
			{
				this._OrderQty = value;
			}
		}
		#endregion
		#region OpenQty
		public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }
		protected Decimal? _OpenQty;
		[PXDBQuantity(typeof(POLineR.uOM), typeof(POLineR.baseOpenQty), HandleEmptyKey = true, BqlField = typeof(POLine.openQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Open Qty.", Enabled = false)]
		public virtual Decimal? OpenQty
		{
			get
			{
				return this._OpenQty;
			}
			set
			{
				this._OpenQty = value;
			}
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : PX.Data.BQL.BqlDecimal.Field<baseOpenQty> { }
		protected Decimal? _BaseOpenQty;
		[PXDBDecimal(6, BqlField = typeof(POLine.baseOpenQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseOpenQty
		{
			get
			{
				return this._BaseOpenQty;
			}
			set
			{
				this._BaseOpenQty = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[PXDBInt(BqlField = typeof(POLine.inventoryID))]
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

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp(BqlField = typeof(POLine.Tstamp), RecordComesFirst = true)]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(POLine.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(POLine.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(POLine.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion

		#region AllowComplete
		public abstract class allowComplete : PX.Data.BQL.BqlBool.Field<allowComplete> { }
		protected Boolean? _AllowComplete;
		[PXDBBool(BqlField = typeof(POLine.allowComplete))]
		[PXUIField(DisplayName = "Allow Complete", Visibility = PXUIVisibility.Visible)]
		[PXDefault(false)]
		public virtual Boolean? AllowComplete
		{
			get
			{
				return this._AllowComplete;
			}
			set
			{
				this._AllowComplete = value;
			}
		}
		#endregion
		#region DRTermStartDate
		public abstract class dRTermStartDate : PX.Data.BQL.BqlDateTime.Field<dRTermStartDate> { }

		protected DateTime? _DRTermStartDate;

		[PXDBDate(BqlField = typeof(POLine.dRTermStartDate))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Term Start Date")]
		public DateTime? DRTermStartDate
		{
			get { return _DRTermStartDate; }
			set { _DRTermStartDate = value; }
		}
		#endregion
		#region DRTermEndDate
		public abstract class dRTermEndDate : PX.Data.BQL.BqlDateTime.Field<dRTermEndDate> { }

		protected DateTime? _DRTermEndDate;

		[PXDBDate(BqlField = typeof(POLine.dRTermEndDate))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Term End Date")]
		public DateTime? DRTermEndDate
		{
			get { return _DRTermEndDate; }
			set { _DRTermEndDate = value; }
		}
		#endregion
	}
}
