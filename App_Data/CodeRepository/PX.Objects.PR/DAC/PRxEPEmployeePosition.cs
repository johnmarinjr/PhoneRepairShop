using PX.Data;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	public sealed class PRxEPEmployeePosition : PXCacheExtension<EPEmployeePosition>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region SettlementPaycheckRefNoteID
		public abstract class settlementPaycheckRefNoteID : PX.Data.BQL.BqlGuid.Field<settlementPaycheckRefNoteID> { }
		[PXUIField(DisplayName = "Final Payment", Enabled = false, Visible = false)]
		[PXRefNote]
		public Guid? SettlementPaycheckRefNoteID { get; set; }
		#endregion
	}
}
