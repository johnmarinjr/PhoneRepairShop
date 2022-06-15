using PX.Data;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class AMDisassembleMasterTranItemAvailabilityExtension : IN.GraphExtensions.ItemAvailabilityExtension<DisassemblyEntry, AMDisassembleBatch, AMDisassembleBatchSplit>
	{
		protected override AMDisassembleBatchSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<AMDisassembleMasterTranLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(AMDisassembleBatch line) => GetUnitRate<AMDisassembleBatch.inventoryID, AMDisassembleBatch.uOM>(line);

		protected override string GetStatus(AMDisassembleBatch line)
		{
			string status = string.Empty;

			if (FetchWithLineUOM(line, excludeCurrent: line?.Released != true) is IStatus availability)
			{
				status = FormatStatus(availability, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		private string FormatStatus(IStatus availability, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				IN.Messages.Availability_ActualInfo,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(availability.QtyActual));
		}

		protected override void RaiseQtyExceptionHandling(AMDisassembleBatch line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<AMDisassembleBatch.qty>(line, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<AMDisassembleBatch.inventoryID>(line),
					LineCache.GetStateExt<AMDisassembleBatch.subItemID>(line),
					LineCache.GetStateExt<AMDisassembleBatch.siteID>(line),
					LineCache.GetStateExt<AMDisassembleBatch.locationID>(line),
					LineCache.GetValue<AMDisassembleBatch.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(AMDisassembleBatchSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<AMDisassembleBatchSplit.qty>(split, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<AMDisassembleBatchSplit.inventoryID>(split),
					SplitCache.GetStateExt<AMDisassembleBatchSplit.subItemID>(split),
					SplitCache.GetStateExt<AMDisassembleBatchSplit.siteID>(split),
					SplitCache.GetStateExt<AMDisassembleBatchSplit.locationID>(split),
					SplitCache.GetValue<AMDisassembleBatchSplit.lotSerialNbr>(split)));
		}
	}
}
