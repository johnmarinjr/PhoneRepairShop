using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common.Exceptions;
using PX.Objects.IN;

namespace PX.Objects.AM
{
	public abstract class AMProdMatlItemAvailabilityExtension<TGraph> : IN.GraphExtensions.ItemAvailabilityExtension<TGraph, AMProdMatl, AMProdMatlSplit>
		where TGraph : PXGraph
	{
		protected override AMProdMatlSplit EnsureSplit(ILSMaster row) => Base.FindImplementation<AMProdMatlLineSplittingExtension<TGraph>>().EnsureSplit(row);

		protected override decimal GetUnitRate(AMProdMatl line) => GetUnitRate<AMProdMatl.inventoryID, AMProdMatl.uOM>(line);

		protected override string GetStatus(AMProdMatl line) => string.Empty;

		protected override void Optimize()
		{
			base.Optimize();

			foreach (PXResult<AMProdMatl, INUnit, INSiteStatus> res in
				SelectFrom<AMProdMatl>.
				InnerJoin<INUnit>.On<
					INUnit.inventoryID.IsEqual<AMProdMatl.inventoryID>.
					And<INUnit.fromUnit.IsEqual<AMProdMatl.uOM>>>.
				InnerJoin<INSiteStatus>.On<
					AMProdMatl.inventoryID.IsEqual<INSiteStatus.inventoryID>.
					And<AMProdMatl.subItemID.IsEqual<INSiteStatus.subItemID>>.
					And<AMProdMatl.siteID.IsEqual<INSiteStatus.siteID>>>.
				Where<
					AMProdMatl.orderType.IsEqual<AMProdMatl.orderType.FromCurrent>.
					And<AMProdMatl.prodOrdID.IsEqual<AMProdMatl.prodOrdID.FromCurrent>>>.
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

		protected override void RaiseQtyExceptionHandling(AMProdMatl line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<AMProdMatl.qtyRemaining>(line, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<AMProdMatl.inventoryID>(line),
					LineCache.GetStateExt<AMProdMatl.subItemID>(line),
					LineCache.GetStateExt<AMProdMatl.siteID>(line),
					LineCache.GetStateExt<AMProdMatl.locationID>(line),
					LineCache.GetValue<AMProdMatl.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(AMProdMatlSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<AMProdMatlSplit.qty>(split, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<AMProdMatlSplit.inventoryID>(split),
					SplitCache.GetStateExt<AMProdMatlSplit.subItemID>(split),
					SplitCache.GetStateExt<AMProdMatlSplit.siteID>(split),
					SplitCache.GetStateExt<AMProdMatlSplit.locationID>(split),
					SplitCache.GetValue<AMProdMatlSplit.lotSerialNbr>(split)));
		}
	}
}
