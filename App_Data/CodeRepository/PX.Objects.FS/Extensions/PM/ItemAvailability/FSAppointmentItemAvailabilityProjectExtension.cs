using PX.Data;
using PX.Objects.FS;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	// TODO: ensure this class is even needed - could project availability be used in AppointmentEntry?
	// if yes, then the GetStatusProject's meaningful implementation is missing, otherwise this class should be removed
	[PXProtectedAccess(typeof(FSAppointmentItemAvailabilityExtension))]
	public abstract class FSAppointmentItemAvailabilityProjectExtension
		: ItemAvailabilityProjectExtension<AppointmentEntry, FSAppointmentItemAvailabilityExtension, FSAppointmentDet, FSApptLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;
		protected override string GetStatusProject(FSAppointmentDet line) => null;
	}
}
