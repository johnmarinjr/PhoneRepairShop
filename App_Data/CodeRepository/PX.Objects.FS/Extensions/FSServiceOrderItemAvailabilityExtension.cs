using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;
using PX.Objects.SO.GraphExtensions;

namespace PX.Objects.FS
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class FSServiceOrderItemAvailabilityExtension : SOBaseItemAvailabilityExtension<ServiceOrderEntry, FSSODet, FSSODetSplit>
	{
		protected override FSSODetSplit EnsureSplit(ILSMaster row)
			=> Base.FindImplementation<FSServiceOrderLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(FSSODet line) => GetUnitRate<FSSODet.inventoryID, FSSODet.uOM>(line);


		protected override string GetStatus(FSSODet line)
		{
			string status = string.Empty;

			if (FetchWithLineUOM(line, excludeCurrent: line?.Completed != true) is IStatus availability)
			{
				status = FormatStatus(availability, line.UOM);
				Check(line, availability);
			}

			return status;
		}

		private string FormatStatus(IStatus availability, string uom)
		{
			return string.Format(
				PXMessages.LocalizeFormatNoPrefix(SO.Messages.Availability_Info),
				uom,
				FormatQty(availability.QtyOnHand),
				FormatQty(availability.QtyAvail),
				FormatQty(availability.QtyHardAvail));
		}


		protected override IStatus Fetch(ILSDetail split, bool excludeCurrent)
		{
			int? locationID = split.LocationID;
			try
			{
				split.LocationID = null;
				return base.Fetch(split, excludeCurrent);
			}
			finally
			{
				split.LocationID = locationID;
			}
		}


		public override void Check(ILSMaster row)
		{
			base.Check(row);
			MemoCheck(row);
		}

		protected virtual void MemoCheck(ILSMaster row)
		{
			if (row is FSSODet line)
			{
				MemoCheck(line);

				FSSODetSplit split = EnsureSplit(line);
				MemoCheck(line, split, triggeredBySplit: false);

				if (split.LotSerialNbr == null)
					row.LotSerialNbr = null;
			}
			else if (row is FSSODetSplit split)
			{
				line = PXParentAttribute.SelectParent<FSSODet>(SplitCache, split);
				MemoCheck(line);
				MemoCheck(line, split, triggeredBySplit: true);
			}
		}

		public virtual bool MemoCheck(FSSODet line) => MemoCheckQty(line);
		protected virtual bool MemoCheckQty(FSSODet row) => true;
		protected virtual bool MemoCheck(FSSODet line, FSSODetSplit split, bool triggeredBySplit) => true;

		protected override int DetailsCountToEnableOptimization => 50;
		protected override void Optimize()
		{
			base.Optimize();

			foreach (PXResult<FSSODet, INUnit, INSiteStatus> res in
				SelectFrom<FSSODet>.
				InnerJoin<INUnit>.On<
					INUnit.inventoryID.IsEqual<FSSODet.inventoryID>.
					And<INUnit.fromUnit.IsEqual<FSSODet.uOM>>>.
				InnerJoin<INSiteStatus>.On<
					FSSODet.inventoryID.IsEqual<INSiteStatus.inventoryID>.
					And<FSSODet.subItemID.IsEqual<INSiteStatus.subItemID>>.
					And<FSSODet.siteID.IsEqual<INSiteStatus.siteID>>>.
				Where<FSSODet.FK.ServiceOrder.SameAsCurrent>.
				View.ReadOnly.Select(Base))
			{
				INUnit.UK.ByInventory.StoreResult(Base, res);
				INSiteStatus.PK.StoreResult(Base, res);
			}
		}

		protected override void RaiseQtyExceptionHandling(FSSODet line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<FSSODet.orderQty>(line, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<FSSODet.inventoryID>(line),
					LineCache.GetStateExt<FSSODet.subItemID>(line),
					LineCache.GetStateExt<FSSODet.siteID>(line),
					LineCache.GetStateExt<FSSODet.locationID>(line),
					LineCache.GetValue<FSSODet.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(FSSODetSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<FSSODetSplit.qty>(split, newValue,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<FSSODetSplit.inventoryID>(split),
					SplitCache.GetStateExt<FSSODetSplit.subItemID>(split),
					SplitCache.GetStateExt<FSSODetSplit.siteID>(split),
					SplitCache.GetStateExt<FSSODetSplit.locationID>(split),
					SplitCache.GetValue<FSSODetSplit.lotSerialNbr>(split)));
		}
	}
}
