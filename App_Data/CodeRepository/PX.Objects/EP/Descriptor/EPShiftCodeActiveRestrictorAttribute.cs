using PX.Data;

namespace PX.Objects.EP
{
	public class EPShiftCodeActiveRestrictorAttribute : PXRestrictorAttribute
	{
		public EPShiftCodeActiveRestrictorAttribute() : base(
			typeof(Where<EPShiftCode.isActive.IsEqual<True>.And<EPShiftCodeRate.shiftID.IsNotNull>>),
			Messages.ShiftCodeNotEffective)
		{ }
	}
}
