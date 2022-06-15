using System.Collections.Generic;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.Exceptions;

namespace PX.Objects.IN.GraphExtensions.KitAssemblyEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class INComponentItemAvailabilityExtension : ItemAvailabilityExtension<KitAssemblyEntry, INComponentTran, INComponentTranSplit>
	{
		protected override INComponentTranSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<INComponentLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(INComponentTran line) => GetUnitRate<INComponentTran.inventoryID, INComponentTran.uOM>(line);

		public override void Initialize()
		{
			base.Initialize();
			ManualEvent.Row<INComponentTran>.Updated.Subscribe(Base, EventHandler);
		}

		protected override string GetStatus(INComponentTran line)
		{
			string status = string.Empty;

			if (FetchWithLineUOM(line, excludeCurrent: line?.Released != true) is IStatus availability)
				status = FormatStatus(availability, line.UOM);

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

		protected virtual void EventHandler(ManualEvent.Row<INComponentTran>.Updated.Args e) // former LSINComponentTran.INComponentTran_RowUpdated
		{
			if (e.Row == null || PXLongOperation.Exists(Base.UID))
				return;

			if (FetchWithLineUOM(e.Row, excludeCurrent: true) is IStatus availability)
				Check(e.Row, availability);
		}

		protected override IEnumerable<PXExceptionInfo> GetCheckErrors(ILSMaster row, IStatus availability)
		{
			foreach (var errorInfo in base.GetCheckErrors(row, availability))
				yield return errorInfo;

			if (row.InvtMult == -1 && row.BaseQty > 0m && availability != null)
				if (availability.QtyAvail < row.Qty)
				{
					string message = GetErrorMessageQtyAvail(GetStatusLevel(availability));

					if (message != null)
						yield return new PXExceptionInfo(message);
				}
		}

		protected override void RaiseQtyExceptionHandling(INComponentTran line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<INComponentTran.qty>(line, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<INComponentTran.inventoryID>(line),
					LineCache.GetStateExt<INComponentTran.subItemID>(line),
					LineCache.GetStateExt<INComponentTran.siteID>(line),
					LineCache.GetStateExt<INComponentTran.locationID>(line),
					LineCache.GetValue<INTran.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(INComponentTranSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<INComponentTranSplit.qty>(split, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<INComponentTranSplit.inventoryID>(split),
					SplitCache.GetStateExt<INComponentTranSplit.subItemID>(split),
					SplitCache.GetStateExt<INComponentTranSplit.siteID>(split),
					SplitCache.GetStateExt<INComponentTranSplit.locationID>(split),
					SplitCache.GetValue<INComponentTranSplit.lotSerialNbr>(split)));
		}
	}
}
