using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// Stores the Insurable Earnings By Pay Period records related to the Record of Employment.
	/// </summary>
	[PXCacheName(Messages.PRROEInsurableEarningsByPayPeriod)]
	[Serializable]
	public class PRROEInsurableEarningsByPayPeriod : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRROEInsurableEarningsByPayPeriod>.By<refNbr, payPeriodID>
		{
			public static PRROEInsurableEarningsByPayPeriod Find(PXGraph graph, string refNbr, string payPeriodID) => FindBy(graph, refNbr, payPeriodID);
		}

		public static class FK
		{
			public class RecordOfEmployment : PRRecordOfEmployment.PK.ForeignKeyOf<PRROEInsurableEarningsByPayPeriod>.By< refNbr> { }
		}
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Reference Nbr.")]
		[PXDBDefault(typeof(PRRecordOfEmployment.refNbr))]
		[PXParent(typeof(FK.RecordOfEmployment))]
		public string RefNbr { get; set; }
		#endregion

		#region PayPeriodID
		public abstract class payPeriodID : PX.Data.BQL.BqlString.Field<payPeriodID> { }
		[PXDBString(6, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Pay Period ID")]
		public string PayPeriodID { get; set; }
		#endregion

		#region InsurableHours
		public abstract class insurableHours : PX.Data.BQL.BqlDecimal.Field<insurableHours> { }
		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.00")]
		[PXUIField(DisplayName = "Insurable Hours")]
		public virtual decimal? InsurableHours { get; set; }
		#endregion

		#region InsurableEarnings
		public abstract class insurableEarnings : PX.Data.BQL.BqlDecimal.Field<insurableEarnings> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.00")]
		[PXUIField(DisplayName = "Insurable Earnings")]
		public virtual decimal? InsurableEarnings { get; set; }
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
