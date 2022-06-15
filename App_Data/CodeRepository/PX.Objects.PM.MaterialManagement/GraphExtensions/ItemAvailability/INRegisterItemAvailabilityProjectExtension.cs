using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability
{
	public abstract class INRegisterItemAvailabilityProjectExtension<TRegisterGraph, TRegisterItemAvailExt> : ItemAvailabilityProjectExtension<TRegisterGraph, TRegisterItemAvailExt, INTran, INTranSplit>
		where TRegisterGraph : INRegisterEntryBase
		where TRegisterItemAvailExt : INRegisterItemAvailabilityExtension<TRegisterGraph>
	{
		protected override string GetStatusProject(INTran line)
		{
			string status = string.Empty;

			bool excludeCurrent = line?.Released != true;

			if (FetchWithLineUOM(line, excludeCurrent) is IStatus availability &&
				FetchWithLineUOMProject(line, excludeCurrent) is IStatus availabilityProject)
			{
				status = FormatStatusProject(availability, availabilityProject, line.UOM);
				Check(line, availabilityProject);
			}

			return status;
		}

		private string FormatStatusProject(IStatus availability, IStatus availabilityProject, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				IN.Messages.Availability_ActualInfo_Project,
				uom,
				FormatQty(availabilityProject.QtyOnHand),
				FormatQty(availabilityProject.QtyAvail),
				FormatQty(availabilityProject.QtyHardAvail),
				FormatQty(availabilityProject.QtyActual),
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(availability.QtyActual));
		}
	}

	[PXProtectedAccess(typeof(INIssueEntry.ItemAvailabilityExtension))]
	public abstract class INIssueItemAvailabilityProjectExtension
		: INRegisterItemAvailabilityProjectExtension<INIssueEntry, INIssueEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INReceiptEntry.ItemAvailabilityExtension))]
	public abstract class INReceiptItemAvailabilityProjectExtension
		: INRegisterItemAvailabilityProjectExtension<INReceiptEntry, INReceiptEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INAdjustmentEntry.ItemAvailabilityExtension))]
	public abstract class INAdjustmentItemAvailabilityProjectExtension
		: INRegisterItemAvailabilityProjectExtension<INAdjustmentEntry, INAdjustmentEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INTransferEntry.ItemAvailabilityExtension))]
	public abstract class INTransferItemAvailabilityProjectExtension
		: INRegisterItemAvailabilityProjectExtension<INTransferEntry, INTransferEntry.ItemAvailabilityExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
