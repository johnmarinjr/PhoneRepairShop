using PX.Data;
using PX.Objects.GL;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Manufacturing Shift field attribute
    /// </summary>
    [PXDBString(15, InputMask = "####")]
    [PXUIField(DisplayName = "Shift")]
    public class ShiftCDFieldAttribute : AcctSubAttribute 
    {
    }
}