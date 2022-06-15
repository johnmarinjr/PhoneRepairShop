using System;
using PX.Data;

namespace PX.Objects.FS
{
	[Serializable]
	public partial class FSContractForecast : IBqlTable
	{
		#region ForecastID
		public abstract class forecastID : PX.Data.BQL.BqlGuid.Field<forecastID> { }

		[PXDBGuidNotNull(IsKey = true)]
		public virtual Guid? ForecastID { get; set; }
		#endregion
		#region ServiceContractID
		public abstract class serviceContractID : PX.Data.BQL.BqlInt.Field<serviceContractID> { }

		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(FSServiceContract.serviceContractID))]
		public virtual int? ServiceContractID { get; set; }
		#endregion
		#region Active
		public abstract class active : PX.Data.BQL.BqlBool.Field<active> { }

		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? Active { get; set; }
		#endregion
		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }

		[PXDBInt]
		[PXDefault(0)]
		public virtual int? LineCntr { get; set; }
		#endregion
		#region DateTimeBegin
		public abstract class dateTimeBegin : PX.Data.BQL.BqlDateTime.Field<dateTimeBegin> { }

		[PXDefault]
		[PXDBDateAndTime(UseTimeZone = true, PreserveTime = true, DisplayNameDate = "Date", DisplayNameTime = "Start Time")]
		[PXUIField(DisplayName = "Start Time")]
		public virtual DateTime? DateTimeBegin { get; set; }
		#endregion
		#region DateTimeEnd
		public abstract class dateTimeEnd : PX.Data.BQL.BqlDateTime.Field<dateTimeEnd> { }

		[PXDefault]
		[PXDBDateAndTime(UseTimeZone = true, PreserveTime = true, DisplayNameDate = "Date", DisplayNameTime = "End Time")]
		[PXUIField(DisplayName = "End Time")]
		public virtual DateTime? DateTimeEnd { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID]
		[PXUIField(DisplayName = "CreatedByID")]
		public virtual Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		[PXUIField(DisplayName = "CreatedByScreenID")]
		public virtual string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = "CreatedDateTime")]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		[PXUIField(DisplayName = "LastModifiedByID")]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		[PXUIField(DisplayName = "LastModifiedByScreenID")]
		public virtual string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = "LastModifiedDateTime")]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}
