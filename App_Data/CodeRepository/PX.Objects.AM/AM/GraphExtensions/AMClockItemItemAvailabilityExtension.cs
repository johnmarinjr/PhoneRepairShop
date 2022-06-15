using PX.Data;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMClockItemItemAvailabilityExtension : IN.GraphExtensions.ItemAvailabilityExtension<ClockEntry, AMClockItem, AMClockItemSplit>
	{
		protected override AMClockItemSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<AMClockItemLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(AMClockItem line) => GetUnitRate<AMClockItem.inventoryID, AMClockItem.uOM>(line);

		protected override string GetStatus(AMClockItem line)
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

		protected override void RaiseQtyExceptionHandling(AMClockItem line, PXExceptionInfo ei, decimal? newValue)
		{
#if DEBUG
			AMDebug.TraceWriteMethodName(ei.MessageFormat,
				LineCache.GetStateExt<AMClockItem.inventoryID>(line),
				LineCache.GetStateExt<AMClockItem.subItemID>(line),
				LineCache.GetStateExt<AMClockItem.siteID>(line),
				LineCache.GetStateExt<AMClockItem.locationID>(line),
				LineCache.GetValue<AMClockItem.lotSerialNbr>(line));
#endif
			LineCache.RaiseExceptionHandling<AMClockItem.qty>(line, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<AMClockItem.inventoryID>(line),
					LineCache.GetStateExt<AMClockItem.subItemID>(line),
					LineCache.GetStateExt<AMClockItem.siteID>(line),
					LineCache.GetStateExt<AMClockItem.locationID>(line),
					LineCache.GetValue<AMClockItem.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(AMClockItemSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<AMClockItemSplit.qty>(split, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<AMClockItemSplit.inventoryID>(split),
					SplitCache.GetStateExt<AMClockItemSplit.subItemID>(split),
					SplitCache.GetStateExt<AMClockItemSplit.siteID>(split),
					SplitCache.GetStateExt<AMClockItemSplit.locationID>(split),
					SplitCache.GetValue<AMClockItemSplit.lotSerialNbr>(split)));
		}
	}
}
