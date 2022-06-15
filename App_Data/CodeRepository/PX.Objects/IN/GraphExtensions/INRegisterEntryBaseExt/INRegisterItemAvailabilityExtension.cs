using System.Collections.Generic;
using PX.Data;
using PX.Objects.Common.Exceptions;

namespace PX.Objects.IN.GraphExtensions
{
	public abstract class INRegisterItemAvailabilityExtension<TRegisterGraph> : ItemAvailabilityExtension<TRegisterGraph, INTran, INTranSplit>
		where TRegisterGraph : INRegisterEntryBase
	{
		protected override INTranSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<INRegisterLineSplittingExtension<TRegisterGraph>>().EnsureSplit(row);

		protected override decimal GetUnitRate(INTran line) => GetUnitRate<INTran.inventoryID, INTran.uOM>(line);

		protected override string GetStatus(INTran line)
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
				Messages.Availability_ActualInfo,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(availability.QtyActual));
		}

		protected override IEnumerable<PXExceptionInfo> GetCheckErrors(ILSMaster row, IStatus availability)
		{
			foreach (var errorInfo in base.GetCheckErrors(row, availability))
				yield return errorInfo;

			foreach (var errorInfo in GetCheckErrorsQtyOnHand(row, availability))
				yield return errorInfo;
		}

		protected virtual IEnumerable<PXExceptionInfo> GetCheckErrorsQtyOnHand(ILSMaster row, IStatus availability)
		{
			if (!IsAvailableOnHandQty(row, availability))
			{
				string message = GetErrorMessageQtyOnHand(GetStatusLevel(availability));

				if (message != null)
					yield return new PXExceptionInfo(PXErrorLevel.RowWarning, message);
			}
		}

		protected virtual bool IsAvailableOnHandQty(ILSMaster row, IStatus availability)
		{
			if (row.InvtMult == -1 && row.BaseQty > 0m && availability != null)
				if (availability.QtyOnHand - row.Qty < 0m && Base.INRegisterDataMember.Current?.Released == false)
					return false;

			return true;
		}

		protected override void RaiseQtyExceptionHandling(INTran line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<INTran.qty>(line, newValue,
				new PXSetPropertyException(ei.MessageFormat, ei.ErrorLevel ?? PXErrorLevel.Warning,
					LineCache.GetStateExt<INTran.inventoryID>(line),
					LineCache.GetStateExt<INTran.subItemID>(line),
					LineCache.GetStateExt<INTran.siteID>(line),
					LineCache.GetStateExt<INTran.locationID>(line),
					LineCache.GetValue<INTran.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(INTranSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<INTranSplit.qty>(split, newValue,
				new PXSetPropertyException(ei.MessageFormat, ei.ErrorLevel ?? PXErrorLevel.Warning,
					SplitCache.GetStateExt<INTranSplit.inventoryID>(split),
					SplitCache.GetStateExt<INTranSplit.subItemID>(split),
					SplitCache.GetStateExt<INTranSplit.siteID>(split),
					SplitCache.GetStateExt<INTranSplit.locationID>(split),
					SplitCache.GetValue<INTranSplit.lotSerialNbr>(split)));
		}
	}
}
