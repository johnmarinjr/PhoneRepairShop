using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.PM;

namespace PX.Objects.EP
{
	public sealed class ShiftDifferentialPMTranExt : PXCacheExtension<PMTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.shiftDifferential>();
		}

		#region ShiftID
		[PXDBInt]
		[PXUIField(DisplayName = "Shift Code")]
		[PXSelector(typeof(SearchFor<EPShiftCode.shiftID>.Where<EPShiftCode.isManufacturingShift.IsEqual<False>>),
			SubstituteKey = typeof(EPShiftCode.shiftCD),
			DescriptionField = typeof(EPShiftCode.description))]
		public int? ShiftID { get; set; }
		public abstract class shiftID : BqlInt.Field<shiftID> { }
		#endregion
	}
}
