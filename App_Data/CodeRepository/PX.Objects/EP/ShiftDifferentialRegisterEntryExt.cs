using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PM;
using System;

namespace PX.Objects.EP
{
	public class ShiftDifferentialRegisterEntryExt : PXGraphExtension<RegisterEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.shiftDifferential>();
		}
				
		[PXOverride]
		public virtual PMTran CreateTransaction(RegisterEntry.CreatePMTran createPMTran, Func<RegisterEntry.CreatePMTran, PMTran> baseMethod)
		{
			PMTran tran = baseMethod(createPMTran);
			Base.Transactions.Cache.SetValue<ShiftDifferentialPMTranExt.shiftID>(tran, createPMTran.TimeActivity.ShiftID);
			return Base.Transactions.Update(tran);
		}
	}
}
