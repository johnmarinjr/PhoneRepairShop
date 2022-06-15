using PX.Data;
using PX.Objects.CR;
using PX.Objects.PM;
using System;
using PX.Objects.AM.CacheExtensions;

namespace PX.Objects.AM.GraphExtensions
{
    public class RegisterEntryAMExtension : PXGraphExtension<RegisterEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
        }

        [PXOverride]
        public virtual PMTran CreateTransaction(RegisterEntry.CreatePMTran createPMTran, Func<RegisterEntry.CreatePMTran, PMTran> baseMethod)
        {
            var timeActExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(createPMTran.TimeActivity);
            if(timeActExt != null && timeActExt.AMIsProd == true)
            {
                return null;
            }

            return baseMethod?.Invoke(createPMTran);
        }
    }
}
