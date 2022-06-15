using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	public abstract class INRegisterLineSplittingProjectExtension<TRegisterGraph, TRegisterLSExt> : LineSplittingProjectExtension<TRegisterGraph, TRegisterLSExt, INRegister, INTran, INTranSplit>
		where TRegisterGraph : INRegisterEntryBase
		where TRegisterLSExt : INRegisterLineSplittingExtension<TRegisterGraph>
	{
	}

	[PXProtectedAccess(typeof(INIssueEntry.LineSplittingExtension))]
	public abstract class INIssueLineSplittingProjectExtension
		: INRegisterLineSplittingProjectExtension<INIssueEntry, INIssueEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INReceiptEntry.LineSplittingExtension))]
	public abstract class INReceiptLineSplittingProjectExtension
		: INRegisterLineSplittingProjectExtension<INReceiptEntry, INReceiptEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INAdjustmentEntry.LineSplittingExtension))]
	public abstract class INAdjustmentLineSplittingProjectExtension
		: INRegisterLineSplittingProjectExtension<INAdjustmentEntry, INAdjustmentEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}

	[PXProtectedAccess(typeof(INTransferEntry.LineSplittingExtension))]
	public abstract class INTransferLineSplittingProjectExtension
		: INRegisterLineSplittingProjectExtension<INTransferEntry, INTransferEntry.LineSplittingExtension>
	{
		public static bool IsActive() => UseProjectAvailability;
	}
}
