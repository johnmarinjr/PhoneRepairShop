using PX.CloudServices.DAC;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[PXInternalUseOnly]
	[PXHidden]
	public class RecognizedRecordDetail : IBqlTable
	{
		[PXParent(typeof(
			SelectFrom<RecognizedRecord>.
			Where<RecognizedRecord.refNbr.IsEqual<RecognizedRecordDetail.refNbr.FromCurrent>.And<
				  RecognizedRecord.entityType.IsEqual<RecognizedRecordDetail.entityType.FromCurrent>>>))]
		[PXDefault]
		[PXDBGuid(IsKey = true)]
		public virtual Guid? RefNbr { get; set; }
		public abstract class refNbr : BqlGuid.Field<refNbr> { }

		[PXDefault]
		[RecognizedRecordEntityTypeList]
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string EntityType { get; set; }
		public abstract class entityType : BqlString.Field<entityType> { }

		[Vendor]
		public int? VendorID { get; set; }
		public abstract class vendorID : BqlInt.Field<vendorID> { }

		[PXDBString(IsUnicode = true)]
		public string VendorName { get; set; }
		public abstract class vendorName : BqlString.Field<vendorName> { }

		[PXDBInt]
		public int? VendorTermIndex { get; set; }
		public abstract class vendorTermIndex : BqlInt.Field<vendorTermIndex> { }

		[PXUIField(DisplayName = "Recognized Amount")]
		[PXDBDecimal]
		public decimal? Amount { get; set; }
		public abstract class amount : BqlDecimal.Field<amount> { }

		[PXUIField(DisplayName = "Recognized Date", Visible = false)]
		[PXDBDate]
		public DateTime? Date { get; set; }
		public abstract class date : BqlDateTime.Field<date> { }

		[PXUIField(DisplayName = "Recognized Due Date", Visible = false)]
		[PXDBDate]
		public DateTime? DueDate { get; set; }
		public abstract class dueDate : BqlDateTime.Field<dueDate> { }

		[PXUIField(DisplayName = "Recognized Vendor Ref.", Visible = false)]
		[PXDBString(40, IsUnicode = true)]
		public string VendorRef { get; set; }
		public abstract class vendorRef : BqlString.Field<vendorRef> { }

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }

		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }

		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBTimestamp]
		public virtual byte[] TStamp { get; set; }
		public abstract class tStamp : BqlByteArray.Field<tStamp> { }
	}
}
