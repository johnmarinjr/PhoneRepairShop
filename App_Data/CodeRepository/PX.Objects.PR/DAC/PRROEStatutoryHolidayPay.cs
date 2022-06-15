using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// Stores the Statutory Holiday Pay records related to the Record of Employment.
	/// </summary>
	[PXCacheName(Messages.PRROEStatutoryHolidayPay)]
	[Serializable]
	public class PRROEStatutoryHolidayPay : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRROEStatutoryHolidayPay>.By<refNbr, lineNbr>
		{
			public static PRROEStatutoryHolidayPay Find(PXGraph graph, string refNbr, int? lineNbr) => FindBy(graph, refNbr, lineNbr);
		}

		public static class FK
		{
			public class RecordOfEmployment : PRRecordOfEmployment.PK.ForeignKeyOf<PRROEStatutoryHolidayPay>.By<refNbr> { }
		}
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr.")]
		[PXDBDefault(typeof(PRRecordOfEmployment.refNbr))]
		public string RefNbr { get; set; }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(PRRecordOfEmployment))]
		[PXParent(typeof(Select<PRRecordOfEmployment, Where<PRRecordOfEmployment.refNbr, Equal<Current<refNbr>>>>))]
		[PXUIField(DisplayName = "Line Nbr.", Visible = false)]
		public virtual int? LineNbr { get; set; }
		#endregion

		#region Date
		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }
		[PXDBDate(UseSmallDateTime = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Date")]
		public DateTime? Date { get; set; }
		#endregion

		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public virtual Decimal? Amount { get; set; }
		#endregion

		#region System Columns
		#region TStamp
		public abstract class tStamp : PX.Data.BQL.BqlByteArray.Field<tStamp> { }
		[PXDBTimestamp]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
