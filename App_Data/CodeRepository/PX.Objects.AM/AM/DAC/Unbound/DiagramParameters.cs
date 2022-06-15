using System;
using PX.Data;

namespace PX.Objects.AM
{
	[PXCacheName("Diagram parameters")]
	public class DiagramParameters : IBqlTable
	{
		#region StartDate

		public virtual DateTime? StartDate { get; set; }

		public abstract class startDate : Data.BQL.BqlDateTime.Field<startDate>
		{
		}

		#endregion

		#region EndDate

		public virtual DateTime? EndDate { get; set; }

		public abstract class endDate : Data.BQL.BqlDateTime.Field<endDate>
		{
		}

		#endregion

		#region DisplayNonWorkingDays

		public virtual bool? DisplayNonWorkingDays { get; set; }

		public abstract class displayNonWorkingDays : Data.BQL.BqlBool.Field<displayNonWorkingDays>
		{
		}

		#endregion

		#region ColorCodingOrders

		public virtual string ColorCodingOrders { get; set; }

		public abstract class colorCodingOrders : Data.BQL.BqlString.Field<colorCodingOrders>
		{
		}

		#endregion

		#region BlockSizeInMinutes

		/// <summary>
		/// Value from <see cref="AMPSetup.schdBlockSize"/>
		/// </summary>
		public virtual int? BlockSizeInMinutes { get; set; }

		public abstract class blockSizeInMinutes : Data.BQL.BqlInt.Field<blockSizeInMinutes>	{	}

		#endregion

		#region WorkCentreCalendarType

		/// <summary>
		/// Defines how the Histogram data is displayed
		/// </summary>
		public virtual string WorkCentreCalendarType { get; set; }

		public abstract class workCentreCalendarType : Data.BQL.BqlString.Field<workCentreCalendarType>	{	}

		#endregion
	}
}
