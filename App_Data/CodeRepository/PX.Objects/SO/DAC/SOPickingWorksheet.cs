using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.SOPickingWorksheet, PXDacType.Document)]
	[PXPrimaryGraph(typeof(SOPickingWorksheetReview))]
	public class SOPickingWorksheet : IBqlTable
	{
		public class PK : PrimaryKeyOf<SOPickingWorksheet>.By<worksheetNbr>
		{
			public static SOPickingWorksheet Find(PXGraph graph, string groupNbr) => FindBy(graph, groupNbr);
		}
		public static class FK
		{
			public class Site : INSite.PK.ForeignKeyOf<SOPickingWorksheet>.By<siteID> { }
			public class SingleShipment : SOShipment.PK.ForeignKeyOf<SOPickingWorksheet>.By<singleShipmentNbr> { }
		}

		#region WorksheetNbr
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault]
		[PXUIField(DisplayName = "Worksheet Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[CS.AutoNumber(typeof(SOSetup.pickingWorksheetNumberingID), typeof(pickDate))]
		[PX.Data.EP.PXFieldDescription]
		[PXReferentialIntegrityCheck]
		[PXSelector(typeof(
			SearchFor<worksheetNbr>.In<
				SelectFrom<SOPickingWorksheet>.
				InnerJoin<INSite>.On<FK.Site>.
				Where<
					worksheetType.IsNotEqual<worksheetType.single>.
					And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
				OrderBy<SOPickingWorksheet.worksheetNbr.Desc>
			>))]
		public virtual String WorksheetNbr { get; set; }
		public abstract class worksheetNbr : BqlString.Field<worksheetNbr> { }
		#endregion
		#region WorksheetType
		[PXDBString(2, IsFixed = true)]
		[PXExtraKey]
		[PXDefault]
		[worksheetType.List]
		[PXUIField(DisplayName = "Type", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String WorksheetType { get; set; }
		public abstract class worksheetType : BqlString.Field<worksheetType>
		{
			public const string Wave = "WV";
			public const string Batch = "BT";
			public const string Single = "SS";

			[PX.Common.PXLocalizable]
			public static class DisplayNames
			{
				public const string Wave = "Wave";
				public const string Batch = "Batch";
				public const string Single = "Single-Shipment";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base
				(
					Pair(Single, DisplayNames.Single),
					Pair(Wave, DisplayNames.Wave),
					Pair(Batch, DisplayNames.Batch)
				) { }
			}

			public class wave : BqlString.Constant<wave> { public wave() : base(Wave) { } }
			public class batch : BqlString.Constant<batch> { public batch() : base(Batch) { } }
			public class single : BqlString.Constant<single> { public single() : base(Single) { } }
		}
		#endregion
		#region PickDate
		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Picking Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? PickDate { get; set; }
		public abstract class pickDate : BqlDateTime.Field<pickDate> { }
		#endregion
		#region Status
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[status.List]
		[PXFormula(typeof(status.hold.When<hold.IsEqual<True>>.Else<status.open>))]
		public virtual String Status { get; set; }
		public abstract class status : BqlString.Field<status>
		{
			public const string Open = SOShipmentStatus.Open;
			public const string Hold = SOShipmentStatus.Hold;
			public const string Picking = "I";
			public const string Picked = "P";
			public const string Completed = SOShipmentStatus.Completed;

			[PX.Common.PXLocalizable]
			public static class DisplayNames
			{
				public const string Open = Messages.Open;
				public const string Hold = Messages.Hold;
				public const string Picking = "Picking";
				public const string Picked = "Picked";
				public const string Completed = Messages.Completed;
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base
				(
					Pair(Open, DisplayNames.Open),
					Pair(Hold, DisplayNames.Hold),
					Pair(Picking, DisplayNames.Picking),
					Pair(Picked, DisplayNames.Picked),
					Pair(Completed, DisplayNames.Completed)
				) { }
			}

			public class open : BqlString.Constant<open> { public open() : base(Open) { } }
			public class hold : BqlString.Constant<hold> { public hold() : base(Hold) { } }
			public class picking : BqlString.Constant<picking> { public picking() : base(Picking) { } }
			public class picked : BqlString.Constant<picked> { public picked() : base(Picked) { } }
			public class completed : BqlString.Constant<completed> { public completed() : base(Completed) { } }
		}
		#endregion
		#region Hold
		[PXDBBool]
		[PXDefault(typeof(SOSetup.holdShipments))]
		[PXUIField(DisplayName = "Hold")]
		[PXUIEnabled(typeof(status.IsIn<status.open, status.hold>))]
		public virtual Boolean? Hold { get; set; }
		public abstract class hold : BqlBool.Field<hold> { }
		#endregion
		#region SiteID
		[Site(DisplayName = "Warehouse ID", DescriptionField = typeof(INSite.descr), Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXDefault]
		[PXForeignReference(typeof(FK.Site))]
		[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, AccessInfo.branchID.FromCurrent>>))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region ShipmentQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Shipped Quantity", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region ShipmentWeight
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Shipped Weight", Enabled = false)]
		public virtual Decimal? ShipmentWeight { get; set; }
		public abstract class shipmentWeight : BqlDecimal.Field<shipmentWeight> { }
		#endregion
		#region ShipmentVolume
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Shipped Volume", Enabled = false)]
		public virtual Decimal? ShipmentVolume { get; set; }
		public abstract class shipmentVolume : BqlDecimal.Field<shipmentVolume> { }
		#endregion
		#region PackageWeight
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Package Weight", Enabled = false)]
		public virtual Decimal? PackageWeight { get; set; }
		public abstract class packageWeight : BqlDecimal.Field<packageWeight> { }
		#endregion
		#region PackageCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Packages", Enabled = false)]
		public virtual int? PackageCount { get; set; }
		public abstract class packageCount : BqlInt.Field<packageCount> { }
		#endregion
		#region SingleShipmentNbr
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		[PXSelector(typeof(SOShipment.shipmentNbr))]
		[PXForeignReference(typeof(FK.SingleShipment))]
		public virtual String SingleShipmentNbr { get; set; }
		public abstract class singleShipmentNbr : BqlString.Field<singleShipmentNbr> { }
		#endregion
		#region NoteID
		[PXSearchable(
			SM.SearchCategory.SO, "{0}: {1}", new Type[] { typeof(worksheetType), typeof(worksheetNbr) }, new Type[0],
			NumberFields = new Type[] { typeof(worksheetNbr) },
			Line1Format = "{0:d}{1}{2}", Line1Fields = new Type[] { typeof(pickDate), typeof(status), typeof(qty) }
		)]
		[PXNote(ShowInReferenceSelector = true, Selector = typeof(worksheetNbr))]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion

		#region Audit Fields
		#region tstamp
		[PXDBTimestamp(RecordComesFirst = true)]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		[PXUIField(DisplayName = "Created At", Enabled = false, IsReadOnly = true)]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedByID, Enabled = false, IsReadOnly = true)]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		[PXUIField(DisplayName = "Last Modified At", Enabled = false, IsReadOnly = true)]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion
	}
}
