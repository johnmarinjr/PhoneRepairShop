using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class POReceiptItemAvailabilityExtension : ItemAvailabilityExtension<POReceiptEntry, POReceiptLine, POReceiptLineSplit>
	{
		protected override POReceiptLineSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<POReceiptLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(POReceiptLine line) => GetUnitRate<POReceiptLine.inventoryID, POReceiptLine.uOM>(line);

		protected override string GetStatus(POReceiptLine line)
		{
			string status = string.Empty;

			bool excludeCurrent = PXParentAttribute.SelectParent<POReceipt>(LineCache, line)?.Released != true;
			
			if (FetchWithLineUOM(line, excludeCurrent) is IStatus availability)
			{
				status = FormatStatus(availability, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		private string FormatStatus(IStatus availability, string uom)
		{
			return PXMessages.LocalizeFormatNoPrefixNLA(
				Messages.Availability_Info,
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail),
				FormatQty(availability.QtyActual));
		}

		protected override void Optimize()
		{
			base.Optimize();

			//package loading and caching
			var select = new
				SelectFrom<POReceiptLine>.
				InnerJoin<INSiteStatus>.On<POReceiptLine.FK.SiteStatus>.
				LeftJoin<INLocationStatus>.On<POReceiptLine.FK.LocationStatus>.
				LeftJoin<INLotSerialStatus>.On<POReceiptLine.FK.LotSerialStatus>.
				Where<
					POReceiptLine.receiptType.IsEqual<POReceipt.receiptType.FromCurrent>.
					And<POReceiptLine.receiptNbr.IsEqual<POReceipt.receiptNbr.FromCurrent>>>.
				View.ReadOnly(Base);
			using (new PXFieldScope(select.View, typeof(INSiteStatus), typeof(INLocationStatus), typeof(INLotSerialStatus)))
			{
				foreach (PXResult<POReceiptLine, INSiteStatus, INLocationStatus, INLotSerialStatus> res in select.Select())
				{
					(var _, var siteStatus, var locationStatus, var lotSerialStatus) = res;

					INSiteStatus.PK.StoreResult(Base, siteStatus);

					if (locationStatus.LocationID != null)
						INLocationStatus.PK.StoreResult(Base, locationStatus);

					if (lotSerialStatus?.LotSerialNbr != null)
						INLotSerialStatus.PK.StoreResult(Base, lotSerialStatus);
				}
			}
		}

		protected override void RaiseQtyExceptionHandling(POReceiptLine line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<POReceiptLine.receiptQty>(line, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetValueExt<POReceiptLine.inventoryID>(line),
					LineCache.GetValueExt<POReceiptLine.subItemID>(line),
					LineCache.GetValueExt<POReceiptLine.siteID>(line),
					LineCache.GetValueExt<POReceiptLine.locationID>(line),
					LineCache.GetValue<POReceiptLine.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(POReceiptLineSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<POReceiptLineSplit.qty>(split, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetValueExt<POReceiptLineSplit.inventoryID>(split),
					SplitCache.GetValueExt<POReceiptLineSplit.subItemID>(split),
					SplitCache.GetValueExt<POReceiptLineSplit.siteID>(split),
					SplitCache.GetValueExt<POReceiptLineSplit.locationID>(split),
					SplitCache.GetValue<POReceiptLineSplit.lotSerialNbr>(split)));
		}
	}
}
