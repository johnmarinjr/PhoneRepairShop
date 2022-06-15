using System;
using PX.Data;

namespace PX.Objects.FS
{
	[System.SerializableAttribute]
	public class FSContractForecastFilter : IBqlTable
	{
		#region StartDate
		public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }

		[PXDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Start Date")]
		public virtual DateTime? StartDate { get; set; }
		#endregion
		#region EndDate
		public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }

		[PXDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "End Date")]
		public virtual DateTime? EndDate { get; set; }
		#endregion
	}
}
