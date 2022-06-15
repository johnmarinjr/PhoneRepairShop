using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.Common.Exceptions;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	public abstract class AMProdItemAvailabilityExtension<TGraph> : IN.GraphExtensions.ItemAvailabilityExtension<TGraph, AMProdItem, AMProdItemSplit>
		where TGraph : PXGraph
	{
		protected override AMProdItemSplit EnsureSplit(ILSMaster row) => Base.FindImplementation<AMProdItemLineSplittingExtension<TGraph>>().EnsureSplit(row);

		protected override decimal GetUnitRate(AMProdItem line) => GetUnitRate<AMProdItem.inventoryID ,AMProdItem.uOM>(line);

		protected override string GetStatus(AMProdItem line) => string.Empty;

		protected override void EventHandlerStatusField(ManualEvent.FieldOf<AMProdItem>.Selecting.Args e) => e.Cancel = true;

		protected override void Optimize()
		{
			base.Optimize();

			foreach (PXResult<AMProdItem, INUnit, INSiteStatus> res in
				SelectFrom<AMProdItem>.
				InnerJoin<INUnit>.On<
					INUnit.inventoryID.IsEqual<AMProdItem.inventoryID>.
					And<INUnit.fromUnit.IsEqual<AMProdItem.uOM>>>.
				InnerJoin<INSiteStatus>.On<
					AMProdItem.inventoryID.IsEqual<INSiteStatus.inventoryID>.
					And<AMProdItem.subItemID.IsEqual<INSiteStatus.subItemID>>.
					And<AMProdItem.siteID.IsEqual<INSiteStatus.siteID>>>.
				Where<
					AMProdItem.orderType.IsEqual<AMProdItem.orderType.FromCurrent>.
					And<AMProdItem.prodOrdID.IsEqual<AMProdItem.prodOrdID.FromCurrent>>>.
				View.ReadOnly.Select(Base))
			{
				SelectFrom<INUnit>.
				Where<
					INUnit.unitType.IsEqual<INUnitType.inventoryItem>.
					And<INUnit.inventoryID.IsEqual<@P.AsInt>>.
					And<INUnit.toUnit.IsEqual<@P.AsString>>.
					And<INUnit.fromUnit.IsEqual<@P.AsString>>>.
				View.ReadOnly.StoreResult(Base, (INUnit)res);

				INSiteStatus.PK.StoreResult(Base, res);
			}
		}

		protected override void RaiseQtyExceptionHandling(AMProdItem line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<AMProdItem.qtytoProd>(line, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<AMProdItem.inventoryID>(line),
					LineCache.GetStateExt<AMProdItem.subItemID>(line),
					LineCache.GetStateExt<AMProdItem.siteID>(line),
					LineCache.GetStateExt<AMProdItem.locationID>(line),
					LineCache.GetValue<AMProdItem.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(AMProdItemSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<AMProdItemSplit.qty>(split, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<AMProdItemSplit.inventoryID>(split),
					SplitCache.GetStateExt<AMProdItemSplit.subItemID>(split),
					SplitCache.GetStateExt<AMProdItemSplit.siteID>(split),
					SplitCache.GetStateExt<AMProdItemSplit.locationID>(split),
					SplitCache.GetValue<AMProdItemSplit.lotSerialNbr>(split)));
		}
	}
}
