using PX.Data;
using PX.Data.BQL.Fluent;

namespace PX.Objects.SO.GraphExtensions
{
	public static class ShowPickListPopup
	{
		public abstract class On<TGraph, TPrimary>
			where TGraph : PXGraph
			where TPrimary : class, IBqlTable, new()
		{
			public abstract class FilteredBy<TWhere> : PXGraphExtension<TGraph>
				where TWhere : IBqlWhere, new()
			{
				public
					SelectFrom<SOPickerListEntry>.
					InnerJoin<SOPicker>.On<SOPickerListEntry.FK.Picker>.
					InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
					InnerJoin<SOPickingJob>.On<SOPickingJob.FK.Picker>.
					InnerJoin<IN.INLocation>.On<SOPickerListEntry.FK.Location>.
					InnerJoin<IN.InventoryItem>.On<SOPickerListEntry.FK.InventoryItem>.
					LeftJoin<SOPickerToShipmentLink>.On<
						SOPickerToShipmentLink.FK.Picker.
						And<SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>>>.
					Where<TWhere>.
					OrderBy<
						IN.INLocation.pathPriority.Asc,
						IN.INLocation.locationCD.Asc,
						IN.InventoryItem.inventoryCD.Asc,
						SOPickerListEntry.lotSerialNbr.Asc>.
					View PickListEntries;

				[PXButton(CommitChanges = true), PXUIField(DisplayName = "Show Pick List", MapEnableRights = PXCacheRights.Select)]
				protected virtual void showPickList() => PickListEntries.AskExt();
				public PXAction<TPrimary> ShowPickList;

				[PXButton(CommitChanges = true, DisplayOnMainToolbar = false), PXUIField(DisplayName = "View Source Document", MapEnableRights = PXCacheRights.Select)]
				protected virtual void viewPickListSource()
				{
					var sheet = SOPickerListEntry.FK.Worksheet.FindParent(Base, PickListEntries.Current);
					if (sheet != null)
					{
						if (sheet.WorksheetType == SOPickingWorksheet.worksheetType.Single)
						{
							var shipmentEntry = PXGraph.CreateInstance<SOShipmentEntry>();
							shipmentEntry.Document.Current = shipmentEntry.Document.Search<SOShipment.shipmentNbr>(sheet.SingleShipmentNbr);
							throw new PXRedirectRequiredException(shipmentEntry, newWindow: true, "");
						}
						else
						{
							var worksheetEntry = PXGraph.CreateInstance<SOPickingWorksheetReview>();
							worksheetEntry.worksheet.Current = worksheetEntry.worksheet.Search<SOPickingWorksheet.worksheetNbr>(sheet.WorksheetNbr);
							throw new PXRedirectRequiredException(worksheetEntry, newWindow: true, "");
						}
					}
				}
				public PXAction<TPrimary> ViewPickListSource;

				#region DAC Overrides
				#region SOPickerToShipmentLink
				public SelectFrom<SOPickerToShipmentLink>.View DummyPickerToShipmentLink;

				[PXMergeAttributes]
				[PXUIVisible(typeof(SOPickerToShipmentLink.toteID.IsNotNull))]
				protected virtual void _(Events.CacheAttached<SOPickerToShipmentLink.toteID> args) { }
				#endregion
				#endregion
			}
		}
	}
}