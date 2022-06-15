using PX.Data;
using PX.Objects.FS;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	// Added for formal backward compatibility - remove if project availability is not applicable for AppointmentEntry
	[PXProtectedAccess(typeof(FSAppointmentLineSplittingExtension))]
	public abstract class FSAppointmentLineSplittingProjectExtension
		: LineSplittingProjectExtension<AppointmentEntry, FSAppointmentLineSplittingExtension, FSAppointment, FSAppointmentDet, FSApptLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
