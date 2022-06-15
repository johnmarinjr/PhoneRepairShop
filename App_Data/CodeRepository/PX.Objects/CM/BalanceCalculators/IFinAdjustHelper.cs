using PX.Objects.Common.GraphExtensions.Abstract.DAC;

namespace PX.Objects.CM.Extensions
{
    public static class IFinAdjustHelper
    {
        public static void FillDiscAmts(this IFinAdjust adj)
        {
            adj.CuryAdjgDiscAmt = adj.CuryAdjgPPDAmt;
            adj.CuryAdjdDiscAmt = adj.CuryAdjdPPDAmt;
            adj.AdjDiscAmt = adj.AdjPPDAmt;
        }
    }
}
