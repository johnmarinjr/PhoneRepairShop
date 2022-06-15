using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Objects.PM.GraphExtensions;

namespace PX.Objects.EP
{
	public class ShiftDifferentialJournalEntryTranRefExt : PXGraphExtension<JournalEntryTranRef>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.shiftDifferential>();
		}

		public delegate void AssignAdditionalFieldsDelegate(GLTran glTran, PMTran pmTran);
		[PXOverride]
		public virtual void AssignAdditionalFields(GLTran glTran, PMTran pmTran, AssignAdditionalFieldsDelegate baseMethod)
		{
			baseMethod(glTran, pmTran);

			ShiftDifferentialGLTranExt glTranExt = PXCache<GLTran>.GetExtension<ShiftDifferentialGLTranExt>(glTran);
			ShiftDifferentialPMTranExt pmTranExt = PXCache<PMTran>.GetExtension<ShiftDifferentialPMTranExt>(pmTran);
			pmTranExt.ShiftID = glTranExt.ShiftID;
		}
	}
}
