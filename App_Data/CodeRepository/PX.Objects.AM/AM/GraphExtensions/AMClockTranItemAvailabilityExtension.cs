using PX.Data;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMClockTranItemAvailabilityExtension : IN.GraphExtensions.ItemAvailabilityExtension<ClockApprovalProcess, AMClockTran, AMClockTranSplit>
	{
		protected override AMClockTranSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<AMClockTranLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(AMClockTran line) => GetUnitRate<AMClockTran.inventoryID, AMClockTran.uOM>(line);

		protected override string GetStatus(AMClockTran line)
		{
			string status = string.Empty;

			if (FetchWithLineUOM(line, excludeCurrent: line?.Released != true) is IStatus availability)
			{
				status = FormatStatus(availability, line.UOM);
			}

			return status;
		}

		private string FormatStatus(IStatus availability, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				Messages.LSTranStatus,
				uom,
				FormatQty(availability.QtyOnHand.GetValueOrDefault()),
				FormatQty(availability.QtyAvail.GetValueOrDefault()),
				FormatQty(availability.QtyHardAvail.GetValueOrDefault()));
		}

		protected override void RaiseQtyExceptionHandling(AMClockTran line, PXExceptionInfo ei, decimal? newValue)
		{
#if DEBUG
			AMDebug.TraceWriteMethodName(ei.MessageFormat,
				LineCache.GetStateExt<AMClockTran.inventoryID>(line),
				LineCache.GetStateExt<AMClockTran.subItemID>(line),
				LineCache.GetStateExt<AMClockTran.siteID>(line),
				LineCache.GetStateExt<AMClockTran.locationID>(line),
				LineCache.GetValue<AMClockTran.lotSerialNbr>(line));
#endif
			LineCache.RaiseExceptionHandling<AMClockTran.qty>(line, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<AMClockTran.inventoryID>(line),
					LineCache.GetStateExt<AMClockTran.subItemID>(line),
					LineCache.GetStateExt<AMClockTran.siteID>(line),
					LineCache.GetStateExt<AMClockTran.locationID>(line),
					LineCache.GetValue<AMClockTran.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(AMClockTranSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<AMClockTranSplit.qty>(split, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<AMClockTranSplit.inventoryID>(split),
					SplitCache.GetStateExt<AMClockTranSplit.subItemID>(split),
					SplitCache.GetStateExt<AMClockTranSplit.siteID>(split),
					SplitCache.GetStateExt<AMClockTranSplit.locationID>(split),
					SplitCache.GetValue<AMClockTranSplit.lotSerialNbr>(split)));
		}
	}
}
