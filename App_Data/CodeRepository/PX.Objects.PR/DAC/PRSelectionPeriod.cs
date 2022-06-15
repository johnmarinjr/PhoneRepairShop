using PX.Data;
using PX.Data.BQL;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PREmployeeClass)]
	[Serializable]
	public class PRSelectionPeriod : IBqlTable
	{
		#region Period
		public abstract class period : BqlString.Field<period> { }
		[PXString]
		[PXUIField(DisplayName = "Period")]
		[PXStringList(
			new string[] { PRSelectionPeriodIDs.LastMonth, PRSelectionPeriodIDs.Last12Months, PRSelectionPeriodIDs.CurrentQuarter, PRSelectionPeriodIDs.CurrentCalYear, PRSelectionPeriodIDs.CurrentFinYear },
			new string[] { Messages.LastMonth, Messages.Last12Months, Messages.CurrentQuarter, Messages.CurrentCalYear, Messages.CurrentFinYear })]
		public string Period { get; set; }
		#endregion
	}
}
