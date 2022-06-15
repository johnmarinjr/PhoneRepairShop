using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.EP
{
	public sealed class ShiftDifferentialGLTranExt : PXCacheExtension<GLTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.shiftDifferential>();
		}

		#region ShiftID
		public abstract class shiftID : PX.Data.BQL.BqlInt.Field<shiftID> { }
		[PXDBInt]
		public int? ShiftID { get; set; }
		#endregion
	}
}
