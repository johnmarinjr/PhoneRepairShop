using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;

namespace PX.Objects.AM.Attributes
{
	public class ShiftCodeSelector : PXSelectorAttribute
	{
		public ShiftCodeSelector() : base(typeof(SearchFor<EPShiftCode.shiftCD>.Where<EPShiftCode.isManufacturingShift.IsEqual<True>>))
		{
			DescriptionField = typeof(EPShiftCode.description);
		}
	}
}
