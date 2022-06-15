using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using System.Linq;

namespace PX.Objects.AM.GraphExtensions
{
	public class EPShiftCodeSetupExtension : PXGraphExtension<EPShiftCodeSetup>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
		}

		public virtual void _(Events.FieldUpdating<EPShiftCode.shiftCD> e)
		{
			if (SelectFrom<EPShiftCode>
				.Where<EPShiftCode.isManufacturingShift.IsEqual<True>
					.And<EPShiftCode.shiftCD.IsEqual<P.AsString>>>.View.Select(Base, e.NewValue).Any())
			{
				throw new PXSetPropertyException<EPShiftCode.shiftCD>(Messages.ShiftCodeExistsInManufacturing);
			}
		}
	}
}
