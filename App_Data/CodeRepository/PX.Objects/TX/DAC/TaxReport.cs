using System;
using System.Collections.Generic;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.Common;
using PX.Objects.TX.Descriptor;

namespace PX.Objects.TX
{
	[System.SerializableAttribute()]
	[PXCacheName(Messages.TaxReport)]
	public partial class TaxReport : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<TaxReport>.By<vendorID, revisionID>
		{
			public static TaxReport Find(PXGraph graph, int? vendorID, int? revisionID) => FindBy(graph, vendorID, revisionID);
		}
		public static class FK
		{
			public class Vendor : AP.Vendor.PK.ForeignKeyOf<TaxReport>.By<vendorID> { }
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[TaxAgencyActive(IsKey = true)]
		[PXDefault]
		public virtual int? VendorID { get; set; }
		#endregion
		#region RevisionID
		public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Report Version")]
		[PXDefault()]
		[PXSelector(typeof(Search<TaxReport.revisionID, Where<TaxReport.vendorID, Equal<Current<vendorID>>>, OrderBy<Desc<TaxReport.revisionID>>>),
			new Type[] { typeof(TaxReport.revisionID), typeof(TaxReport.validFrom) })]
		public virtual int? RevisionID { get; set; }
		#endregion
		#region ValidFrom
		public abstract class validFrom : PX.Data.BQL.BqlDateTime.Field<validFrom> { }
		[PXDBDate]
		[PXUIField(DisplayName = "Valid From")]
		[PXDefault()]
		public virtual DateTime? ValidFrom { get; set; }
		#endregion
		#region ValidTo
		public abstract class validTo : PX.Data.BQL.BqlDateTime.Field<validTo> { }
		[PXDBDate]
		[PXUIField(DisplayName = "Valid To", Visible = false)]
		public virtual DateTime? ValidTo { get; set; }
		#endregion

		#region ShowNoTemp
		public abstract class showNoTemp : PX.Data.BQL.BqlBool.Field<showNoTemp> { }
		[PXBool]
		[PXUIField(DisplayName = "Show Tax Zones")]
		[PXUnboundDefault(false)]
		public virtual bool? ShowNoTemp { get; set; }
		#endregion

		#region LineCntr
		public abstract class lineCntr : IBqlField { }
		[PXInt]
		[PXDefault(0)]
		[PXDBScalar(typeof(Search4<TaxReportLine.lineNbr,
			Where<TaxReportLine.vendorID, Equal<vendorID>, And<TaxReportLine.taxReportRevisionID, Equal<revisionID>>>,
			Aggregate<Max<TaxReportLine.lineNbr>>>))]
		public virtual int? LineCntr { get; set; }
		#endregion

		#region NoteID
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXNote(PopupTextEnabled = true)]
		public Guid? NoteID { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp()]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}
