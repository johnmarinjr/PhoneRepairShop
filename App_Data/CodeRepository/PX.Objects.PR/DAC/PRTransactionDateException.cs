using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRTransactionDateException)]
	[Serializable]
	public class PRTransactionDateException : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRTransactionDateException>.By<recordID>
		{
			public static PRTransactionDateException Find(PXGraph graph, string field) => FindBy(graph, field);
		}

		public static class FK
		{
			public class Country : CS.Country.PK.ForeignKeyOf<PRTransactionDateException>.By<countryID> { }
		}
		#endregion

		#region RecordID
		public abstract class recordID : BqlInt.Field<recordID> { }
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID { get; set; }
		#endregion
		#region Date
		public abstract class date : BqlDateTime.Field<date> { }
		[PXDBDate]
		[PXDefault]
		[PXUIField(DisplayName = "Date")]
		public virtual DateTime? Date { get; set; }
		#endregion
		#region Description
		public abstract class description : BqlString.Field<description> { }
		[PXDBString(255)]
		[PXDefault]
		[PXUIField(DisplayName = "Description")]
		public virtual string Description { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault]
		[PRCountry]
		[PXUIField(Visible = false)]
		public virtual string CountryID { get; set; }
		#endregion

		#region DayOfWeek
		public abstract class dayOfWeek : BqlString.Field<dayOfWeek> { }
		[PXString(20)]
		[PXUIField(DisplayName = "Day of Week", Enabled = false)]
		[DayOfWeek(typeof(date))]
		public virtual string DayOfWeek { get; set; }
		#endregion
		#region System Columns
		#region TStamp
		public class tStamp : IBqlField { }
		[PXDBTimestamp()]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public class createdByID : IBqlField { }
		[PXDBCreatedByID()]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public class createdByScreenID : IBqlField { }
		[PXDBCreatedByScreenID()]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public class createdDateTime : IBqlField { }
		[PXDBCreatedDateTime()]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public class lastModifiedByID : IBqlField { }
		[PXDBLastModifiedByID()]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public class lastModifiedByScreenID : IBqlField { }
		[PXDBLastModifiedByScreenID()]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public class lastModifiedDateTime : IBqlField { }
		[PXDBLastModifiedDateTime()]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
