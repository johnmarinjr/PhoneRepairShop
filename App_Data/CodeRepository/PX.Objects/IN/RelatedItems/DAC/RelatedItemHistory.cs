using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
	[PXCacheName(Messages.RelatedItemHistory, PXDacType.History)]
	public class RelatedItemHistory : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<RelatedItemHistory>.By<lineID>
		{
			public static RelatedItemHistory Find(PXGraph graph, int? lineID) => FindBy(graph, lineID);

			public new class Dirty : PrimaryKeyOf<RelatedItemHistory>.By<lineID>.Dirty
			{
				public static RelatedItemHistory Find(PXGraph graph, int? lineID) => FindBy(graph, lineID);
			}
		}
		public static class FK
		{
			public class OriginalInventoryItem : InventoryItem.PK.ForeignKeyOf<RelatedItemHistory>.By<originalInventoryID> { }
			public class RelatedInventoryItem : InventoryItem.PK.ForeignKeyOf<RelatedItemHistory>.By<relatedInventoryID> { }

			public class SalesOrder : SOOrder.PK.ForeignKeyOf<RelatedItemHistory>.By<orderType, orderNbr> { }
			public class OriginalSalesOrderLine : SOLine.PK.ForeignKeyOf<RelatedItemHistory>.By<orderType, orderNbr, originalOrderLineNbr> { }
			public class RelatedSalesOrderLine : SOLine.PK.ForeignKeyOf<RelatedItemHistory>.By<orderType, orderNbr, relatedOrderLineNbr> { }

			public class Invoice : SOInvoice.PK.ForeignKeyOf<RelatedItemHistory>.By<invoiceDocType, invoiceRefNbr> { }
			public class ARInvoice : AR.ARInvoice.PK.ForeignKeyOf<RelatedItemHistory>.By<invoiceDocType, invoiceRefNbr> { }
			public class OriginalInvoiceLine : ARTran.PK.ForeignKeyOf<RelatedItemHistory>.By<invoiceDocType, invoiceRefNbr, originalInvoiceLineNbr> { }
			public class RelatedInvoiceLine : ARTran.PK.ForeignKeyOf<RelatedItemHistory>.By<invoiceDocType, invoiceRefNbr, relatedInvoiceLineNbr> { }
		}
		#endregion

		#region LineID
		[PXDBIdentity(IsKey = true)]
		public virtual int? LineID { get; set; }
		public abstract class lineID : BqlInt.Field<lineID> { }
		#endregion

		#region IsDraft
		[PXDBBool]
		[PXDefault(true)]
		public virtual bool? IsDraft { get; set; }
		public abstract class isDraft : BqlBool.Field<isDraft> { }
		#endregion

		#region OriginalInventoryID
		[Inventory(DisplayName = "Original Item ID", Required = false)]
		[PXDefault]
		[PXForeignReference(typeof(FK.OriginalInventoryItem))]
		public virtual int? OriginalInventoryID { get; set; }
		public abstract class originalInventoryID : BqlInt.Field<originalInventoryID> { }
		#endregion

		#region OriginalInventoryDesc
		[PXString]
		[PXUIField(DisplayName = "Original Item Description")]
		[PXFormula(typeof(Selector<originalInventoryID, InventoryItem.descr>))]
		public virtual string OriginalInventoryDesc { get; set; }
		public abstract class originalInventoryDesc : BqlInt.Field<originalInventoryDesc> { }
		#endregion

		#region OriginalInventoryUOM
		[INUnit(typeof(originalInventoryID), DisplayName = "Original Item UOM", Required = false)]
		[PXDefault]
		public virtual string OriginalInventoryUOM { get; set; }
		public abstract class originalInventoryUOM : BqlString.Field<originalInventoryUOM> { }
		#endregion

		#region OriginalInventoryQty
		[PXDBQuantity]
		[PXDefault]
		[PXUIField(DisplayName = "Original Item Qty.")]
		public virtual decimal? OriginalInventoryQty { get; set; }
		public abstract class originalInventoryQty : BqlDecimal.Field<originalInventoryQty> { }
		#endregion

		#region RelatedInventoryID
		[Inventory(DisplayName = "Related Item ID", Required = false)]
		[PXDefault]
		[PXForeignReference(typeof(FK.RelatedInventoryItem))]
		public virtual int? RelatedInventoryID { get; set; }
		public abstract class relatedInventoryID : BqlInt.Field<relatedInventoryID> { }
		#endregion

		#region RelatedInventoryDesc
		[PXString]
		[PXUIField(DisplayName = "Related Item Description", Enabled = false)]
		[PXFormula(typeof(Selector<relatedInventoryID, InventoryItem.descr>))]
		public virtual string RelatedInventoryDesc { get; set; }
		public abstract class relatedInventoryDesc : BqlInt.Field<relatedInventoryDesc> { }
		#endregion

		#region RelatedInventoryUOM
		[INUnit(typeof(relatedInventoryID), DisplayName = "Related Item UOM", Required = false)]
		[PXDefault]
		public virtual string RelatedInventoryUOM { get; set; }
		public abstract class relatedInventoryUOM : BqlString.Field<relatedInventoryUOM> { }
		#endregion

		#region RelatedInventoryQty
		[PXDBQuantity]
		[PXDefault]
		[PXUIField(DisplayName = "Related Item Qty.")]
		public virtual decimal? RelatedInventoryQty { get; set; }
		public abstract class relatedInventoryQty : BqlDecimal.Field<relatedInventoryQty> { }
		#endregion

		#region SoldQty
		[PXDBQuantity]
		[PXUIField(DisplayName = "Qty. Sold")]
		public virtual decimal? SoldQty { get; set; }
		public abstract class soldQty : BqlDecimal.Field<soldQty> { }
		#endregion

		#region Relation
		[PXDBString(5, IsFixed = true)]
		[PXUIField(DisplayName = "Relation")]
		[PXDefault]
		[InventoryRelation.ListAttribute.WithAll]
		public virtual string Relation { get; set; }
		public abstract class relation : BqlString.Field<relation> { }
		#endregion

		#region Tag
		[PXDBString(4, IsFixed = true)]
		[PXUIField(DisplayName = "Tag")]
		[PXDefault]
		[InventoryRelationTag.ListAttribute.WithAll]
		public virtual string Tag { get; set; }
		public abstract class tag : BqlString.Field<tag> { }
		#endregion

		#region DocumentDate
		[PXDBDate]
		[PXDefault]
		[PXUIField(DisplayName = "Document Date", Visible = false, Required = false)]
		public virtual DateTime? DocumentDate { get; set; }
		public abstract class documentDate : BqlDateTime.Field<documentDate> { }
		#endregion

		#region OrderType
		[PXDBString(2, IsFixed = true)]
		public virtual string OrderType { get; set; }
		public abstract class orderType : BqlString.Field<orderType> { }
		#endregion

		#region OrderNbr
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXParent(typeof(FK.SalesOrder))]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<orderType.FromCurrent>>>))]
		[PXUIField(DisplayName = "Order Nbr.")]
		public virtual string OrderNbr { get; set; }
		public abstract class orderNbr : BqlString.Field<orderNbr> { }
		#endregion

		#region OriginalOrderLineNbr
		[PXDBInt]
		[PXParent(typeof(FK.OriginalSalesOrderLine), LeaveChildren = true)]
		public virtual int? OriginalOrderLineNbr { get; set; }
		public abstract class originalOrderLineNbr : BqlInt.Field<originalOrderLineNbr> { }
		#endregion

		#region RelatedOrderLineNbr
		[PXDBInt]
		[PXParent(typeof(FK.RelatedSalesOrderLine))]
		public virtual int? RelatedOrderLineNbr { get; set; }
		public abstract class relatedOrderLineNbr : BqlInt.Field<relatedOrderLineNbr> { }
		#endregion

		#region InvoiceDocType
		[PXDBString(3, IsFixed = true)]
		public virtual string InvoiceDocType { get; set; }
		public abstract class invoiceDocType : BqlString.Field<invoiceDocType> { }
		#endregion

		#region InvoiceRefNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Invoice Nbr.")]
		[PXParent(typeof(FK.ARInvoice))]
		[PXParent(typeof(FK.Invoice))]
		[PXSelector(typeof(Search<SOInvoice.refNbr, Where<SOInvoice.docType, Equal<invoiceDocType.FromCurrent>>>))]
		public virtual string InvoiceRefNbr{ get; set; }
		public abstract class invoiceRefNbr : BqlString.Field<invoiceRefNbr> { }
		#endregion

		#region OriginalInvoiceLineNbr
		[PXDBInt]
		[PXParent(typeof(FK.OriginalInvoiceLine), LeaveChildren = true)]
		public virtual int? OriginalInvoiceLineNbr { get; set; }
		public abstract class originalInvoiceLineNbr : BqlInt.Field<originalInvoiceLineNbr> { }
		#endregion

		#region RelatedInvoiceLineNbr
		[PXDBInt]
		[PXParent(typeof(FK.RelatedInvoiceLine))]
		public virtual int? RelatedInvoiceLineNbr { get; set; }
		public abstract class relatedInvoiceLineNbr : BqlInt.Field<relatedInvoiceLineNbr> { }
		#endregion

		#region System fields

		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion

		#region tstamp
		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion

		#endregion
	}
}
