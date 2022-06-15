using PX.Data;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;

namespace PX.Objects.PR
{
	public class ShiftDifferentialPRReleaseProcessExt : PXGraphExtension<PRReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.shiftDifferential>();
		}

		public delegate GLTran WriteEarningDelegate(PRPayment payment, PREarningDetail earningDetail, CurrencyInfo info, Batch batch);
		[PXOverride]
		public virtual GLTran WriteEarning(PRPayment payment, PREarningDetail earningDetail, CurrencyInfo info, Batch batch, WriteEarningDelegate baseMethod)
		{
			GLTran tran = baseMethod(payment, earningDetail, info, batch);
			Base.Caches<GLTran>().SetValue<ShiftDifferentialGLTranExt.shiftID>(tran, earningDetail.ShiftID);
			return tran;
		}
	}
}
