using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.PushNotifications;
using System.Threading;
using System.Reflection;
using CommonServiceLocator;
using PX.Api.ContractBased.Models;
using PX.Licensing;
using PX.Objects.IN;
using PX.Objects.Common;
using PX.Data.BQL.Fluent;
using PX.Commerce.Core;
using PX.Commerce.Objects.Availability;
using PX.Commerce.Core.API;
using PX.Objects.CS;

namespace PX.Commerce.Objects
{
	public abstract class AvailabilityProcessorBase<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorBulkBase<TGraph, TEntityBucket, TPrimaryMapped>
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		public delegate PXView FetchAvailabilityDelegate(BCBindingExt bindingExt, DateTime? startTime, DateTime? endTime, ref List<object> parameters);

		#region Fetch Product Availability

		public virtual PXView FetchAvailabilityBaseCommand(BCBindingExt bindingExt, Type join, Type where, ref List<object> parameters)
		{
			List<Type> typesList = new List<Type> {
				typeof(Select5<,,,,>), typeof(BCSyncStatus),
				join,
				typeof(Where<BCSyncStatus.connectorType.IsEqual<P.AsString>
						.And<BCSyncStatus.bindingID.IsEqual<P.AsInt>>
						.And<BCSyncStatus.localID.IsNotNull>
						.And<BCSyncStatus.externID.IsNotNull>
						.And<BCSyncStatus.deleted.IsNotEqual<True>>
						.And<PX.Objects.IN.InventoryItem.stkItem.IsEqual<True>>
						.And<PX.Objects.IN.InventoryItem.exportToExternal.IsEqual<True>>
						>),
				typeof(Aggregate<>),
				typeof(GroupBy<PX.Objects.IN.InventoryItem.inventoryID>),
				typeof(OrderBy<PX.Objects.IN.InventoryItem.inventoryID.Asc>)
			};

			Type select = BqlCommand.Compose(typesList.ToArray());
			BqlCommand cmd = BqlCommand.CreateInstance(select);
			PXView view = new PXView(this, true, cmd);

			if (parameters == null)
				parameters = new List<object>();

			parameters.Add(Operation.ConnectorType);
			parameters.Add(bindingExt?.BindingID);

			if(where != null) view.WhereAnd(where);

			return view;
		}
		public virtual PXView FetchAvailabilityBaseCommandForStockItem(BCBindingExt bindingExt, DateTime? startTime, DateTime? endTime, ref List<object> parameters)
		{
			var commandBQL = FetchAvailabilityBaseCommand(bindingExt,
				typeof(InnerJoin<PX.Objects.IN.InventoryItem,
						On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncStatus.localID>,
						And<BCSyncStatus.entityType, Equal<BCEntitiesAttribute.stockItem>>>,
					LeftJoin<PX.Objects.IN.INSiteStatus,
						On<PX.Objects.IN.InventoryItem.inventoryID, Equal<PX.Objects.IN.INSiteStatus.inventoryID>>>>),
				typeof(Where<BCSyncStatus.entityType, Equal<BCEntitiesAttribute.stockItem>>),
				ref parameters);

			if (startTime != null)
			{
				commandBQL.WhereAnd(BqlCommand.Compose(typeof(Where<,,>), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime), typeof(GreaterEqual<>), typeof(P.AsDateTime),
					typeof(Or<,>), typeof(PX.Objects.IN.InventoryItem.lastModifiedDateTime), typeof(GreaterEqual<>), typeof(P.AsDateTime)));
				parameters.Add(startTime);
				parameters.Add(startTime);
			}

			if (endTime != null)
			{
				commandBQL.WhereAnd(BqlCommand.Compose(typeof(Where<,,>), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime), typeof(LessEqual<>), typeof(P.AsDateTime),
					typeof(Or<,>), typeof(PX.Objects.IN.InventoryItem.lastModifiedDateTime), typeof(LessEqual<>), typeof(P.AsDateTime)));
				parameters.Add(endTime);
				parameters.Add(endTime);
			}
			return commandBQL;
		}

		public virtual PXView FetchAvailabilityBaseCommandForTemplateItem(BCBindingExt bindingExt, DateTime? startTime, DateTime? endTime, ref List<object> parameters)
		{
			var commandBQL = FetchAvailabilityBaseCommand(bindingExt,
				typeof(LeftJoin<BCSyncDetail,
						On<BCSyncStatus.syncID, Equal<BCSyncDetail.syncID>,
						And<BCSyncDetail.entityType, Equal<BCEntitiesAttribute.variant>>>,
					LeftJoin<ChildInventoryItem,
						On<ChildInventoryItem.noteID, Equal<BCSyncDetail.localID>>,
					InnerJoin<PX.Objects.IN.InventoryItem,
						On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncStatus.localID>,
						And<BCSyncStatus.entityType, Equal<BCEntitiesAttribute.productWithVariant>>>,
					LeftJoin<PX.Objects.IN.INSiteStatus,
						On<ChildInventoryItem.inventoryID, Equal<PX.Objects.IN.INSiteStatus.inventoryID>>>>>>),
				typeof(Where<BCSyncStatus.entityType, Equal<BCEntitiesAttribute.productWithVariant>>),
				ref parameters);

			if (startTime != null)
			{
				commandBQL.WhereAnd(BqlCommand.Compose(typeof(Where<,,>), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime), typeof(GreaterEqual<>), typeof(P.AsDateTime),
					typeof(Or<,,>), typeof(PX.Objects.IN.InventoryItem.lastModifiedDateTime), typeof(GreaterEqual<>), typeof(P.AsDateTime),
					typeof(Or<,>), typeof(ChildInventoryItem.lastModifiedDateTime), typeof(GreaterEqual<>), typeof(P.AsDateTime)));
				parameters.Add(startTime);
				parameters.Add(startTime);
				parameters.Add(startTime);
			}

			if (endTime != null)
			{
				commandBQL.WhereAnd(BqlCommand.Compose(typeof(Where<,,>), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime), typeof(LessEqual<>), typeof(P.AsDateTime),
					typeof(Or<,,>), typeof(PX.Objects.IN.InventoryItem.lastModifiedDateTime), typeof(LessEqual<>), typeof(P.AsDateTime),
					typeof(Or<,>), typeof(ChildInventoryItem.lastModifiedDateTime), typeof(LessEqual<>), typeof(P.AsDateTime)));
				parameters.Add(endTime);
				parameters.Add(endTime);
				parameters.Add(endTime);
			}
			return commandBQL;
		}

		public StorageDetailsResult ParseResult(PXResult row)
		{
			PX.Objects.IN.InventoryItem item = row.GetItem<PX.Objects.IN.InventoryItem>();
			ChildInventoryItem childItem = row.GetItem<ChildInventoryItem>();
			BCSyncStatus parentStatus = row.GetItem<BCSyncStatus>();
			PX.Objects.IN.INSiteStatus siteStatus = row.GetItem<PX.Objects.IN.INSiteStatus>();

			StorageDetailsResult result = new StorageDetailsResult();
			result.ParentSyncId = parentStatus.SyncID.ValueField();
			result.InventoryID = item.InventoryID.ValueField();
			result.InventoryCD = item.InventoryCD.Trim().ValueField();
			result.InventoryNoteID = item.NoteID.ValueField();
			result.InventoryLastModifiedDate = childItem?.LastModifiedDateTime != null && childItem?.LastModifiedDateTime > item.LastModifiedDateTime ? childItem?.LastModifiedDateTime.ValueField() : item.LastModifiedDateTime.ValueField();
			result.SiteLastModifiedDate = siteStatus?.LastModifiedDateTime.ValueField();
			result.IsTemplate = item.IsTemplate.ValueField();

			return result;
		}

		public virtual PXView FetchStorageDetailsBaseCommand(BCBindingExt bindingExt, DateTime? startTime, DateTime? endTime, ref List<object> parameters, FetchAvailabilityDelegate availabilityDelegate)
		{
			PXView commandBQL = availabilityDelegate(bindingExt, startTime, endTime, ref parameters);
			if (commandBQL == null) throw new NotSupportedException();

			var siteLocations = BCLocationSlot.GetWarehouseLocations(bindingExt.BindingID);
			if (siteLocations != null || siteLocations.Count > 0)
			{
				Type condition = null;
				foreach (var siteId in siteLocations)
				{
					if (condition == null)
						condition = BqlCommand.Compose(typeof(Where<,>), typeof(PX.Objects.IN.INSiteStatus.siteID), typeof(IsNull));

					Type siteConditions = BqlCommand.Compose(typeof(Where<,>), typeof(PX.Objects.IN.INSiteStatus.siteID), typeof(Equal<>), typeof(P.AsInt));
					parameters.Add(siteId.Key);
					
					condition = BqlCommand.Compose(typeof(Where2<,>), condition, typeof(Or<>), siteConditions);
				}
				commandBQL.WhereAnd(BqlCommand.Compose(typeof(Brackets<>), condition));
			}

			return commandBQL;
		}

		public virtual IEnumerable<StorageDetailsResult> FetchStorageDetails(BCBindingExt bindingExt, DateTime? startTime, DateTime? endTime, FetchAvailabilityDelegate availabilityDelegate)
		{
			var totalRows = cbapi.CommerceInquiryPageSize;
			List<object> parameters = new List<object>();
			int startRow = 0;
			var commandBQL = FetchStorageDetailsBaseCommand(bindingExt, startTime, endTime, ref parameters, availabilityDelegate);
			while (true)
			{
				int resultCount = 0;

				using (new PXFieldScope(commandBQL, typeof(PX.Objects.IN.InventoryItem.noteID), typeof(PX.Objects.IN.InventoryItem.inventoryCD), typeof(PX.Objects.IN.InventoryItem.isTemplate), 
					typeof(PX.Objects.IN.InventoryItem.inventoryID), typeof(PX.Objects.IN.InventoryItem.lastModifiedDateTime), typeof(ChildInventoryItem.lastModifiedDateTime), typeof(BCSyncStatus.syncID), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime)))
				{
					foreach (PXResult row in commandBQL.SelectWindowed(null, parameters.ToArray(), null, null, startRow, totalRows))
					{
						resultCount++;
						
						yield return ParseResult(row);
					}

					startRow += resultCount;
					if (resultCount != totalRows)
						break;
				}
			}
			yield break;
		}

		#endregion Fetch Product Availability

		#region Get Product Availability

		public virtual IEnumerable<StorageDetailsResult> GetStorageDetailsResults(BCBindingExt bindingExt, List<BCSyncStatus> statuses)
		{
			if (statuses == null || statuses?.Count == 0) yield break;

			List<object> parameters = new List<object>();
			var warehouses = BCLocationSlot.GetWarehouses(bindingExt.BindingID);
			var locations = BCLocationSlot.GetLocations(bindingExt.BindingID);
			var commandBQL = GetAvailabilityBaseCommand(bindingExt, statuses, out parameters);

			using (new PXFieldScope(commandBQL, typeof(ChildInventoryItem.noteID), typeof(ChildInventoryItem.inventoryID), typeof(ChildInventoryItem.inventoryCD), typeof(ChildInventoryItem.templateItemID), 
				 typeof(ChildInventoryItem.descr), typeof(ChildInventoryItem.isTemplate), typeof(ChildInventoryItem.availability), typeof(ChildInventoryItem.lastModifiedDateTime),
				typeof(ChildInventoryItem.itemStatus), typeof(ChildInventoryItem.notAvailMode), typeof(ChildInventoryItem.exportToExternal), typeof(BCSyncDetail.externID), typeof(BCSyncStatus.localID), typeof(BCSyncStatus.externID), typeof(BCSyncStatus.syncID), 
				typeof(PX.Objects.IN.INSiteStatus.siteID), typeof(PX.Objects.IN.INSiteStatus.qtyAvail), typeof(PX.Objects.IN.INSiteStatus.qtyActual), typeof(PX.Objects.IN.INSiteStatus.qtyHardAvail), typeof(PX.Objects.IN.INSiteStatus.qtyOnHand), typeof(PX.Objects.IN.INSiteStatus.lastModifiedDateTime),
				typeof(PX.Objects.IN.INSiteLotSerial.qtyActual), typeof(PX.Objects.IN.INSiteLotSerial.qtyAvail), typeof(PX.Objects.IN.INSiteLotSerial.qtyHardAvail), typeof(PX.Objects.IN.INSiteLotSerial.qtyOnHand), typeof(PX.Objects.IN.INLocationStatus.locationID),
				typeof(PX.Objects.IN.INLocationStatus.qtyAvail), typeof(PX.Objects.IN.INLocationStatus.qtyActual), typeof(PX.Objects.IN.INLocationStatus.qtyHardAvail), typeof(PX.Objects.IN.INLocationStatus.qtyOnHand), typeof(PX.Objects.IN.INLocationStatus.lastModifiedDateTime)))
			{
				foreach (PXResult row in commandBQL.SelectMulti(parameters.ToArray()))
				{
					yield return ParseResult(bindingExt, warehouses, locations, row);
				}
			}
			yield break;
		}

		public virtual PXView GetAvailabilityBaseCommand(BCBindingExt bindingExt, List<BCSyncStatus> statuses, out List<object> parameters)
		{
			parameters = new List<object>();
			var siteLocations = BCLocationSlot.GetWarehouseLocations(bindingExt.BindingID);
			List<Type> typesList = new List<Type> {
				typeof(Select5<,,,,>),
				typeof(BCSyncStatus),
				typeof(LeftJoin<,,>), typeof(BCSyncDetail),
				typeof(On<BCSyncStatus.syncID, Equal<BCSyncDetail.syncID>,
						And<BCSyncDetail.entityType, Equal<BCEntitiesAttribute.variant>>>),
				typeof(InnerJoin<,,>), typeof(ChildInventoryItem),
				typeof(On<ChildInventoryItem.noteID, Equal<BCSyncDetail.localID>,
						Or<ChildInventoryItem.noteID, Equal<BCSyncStatus.localID>>>),
				typeof(InnerJoin<,,>), typeof(PX.Objects.IN.InventoryItem),
				typeof(On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncStatus.localID>>),
				typeof(LeftJoin<,,>), typeof(PX.Objects.IN.INSite),
				typeof(On<PX.Objects.IN.INSite.active, Equal<True>>)
			};

			Type groupBy = typeof(Aggregate<GroupBy<ChildInventoryItem.inventoryID, GroupBy<PX.Objects.IN.INSiteStatus.siteID>>>);
			if (BCLocationSlot.GetBCLocations(bindingExt.BindingID).Any(x => x.LocationID != null))
			{
				groupBy = typeof(Aggregate<GroupBy<ChildInventoryItem.inventoryID, GroupBy<PX.Objects.IN.INSiteStatus.siteID, GroupBy<INLocationStatus.locationID>>>>);
				typesList.Add(typeof(LeftJoin<,,>));
				typesList.Add(typeof(PX.Objects.IN.INSiteStatus));
				typesList.Add(typeof(On<PX.Objects.IN.INSiteStatus.inventoryID, Equal<ChildInventoryItem.inventoryID>,
					And<PX.Objects.IN.INSite.siteID, Equal<PX.Objects.IN.INSiteStatus.siteID>>>));
				typesList.Add(typeof(LeftJoin<PX.Objects.IN.INLocationStatus,
						On<PX.Objects.IN.INSiteStatus.inventoryID.IsEqual<PX.Objects.IN.INLocationStatus.inventoryID>
							.And<PX.Objects.IN.INSiteStatus.siteID.IsEqual<PX.Objects.IN.INLocationStatus.siteID>>>>));
			}
			else
			{
				typesList.Add(typeof(LeftJoin<,>));
				typesList.Add(typeof(PX.Objects.IN.INSiteStatus));
				typesList.Add(typeof(On<PX.Objects.IN.INSiteStatus.inventoryID, Equal<ChildInventoryItem.inventoryID>,
					And<PX.Objects.IN.INSite.siteID, Equal<PX.Objects.IN.INSiteStatus.siteID>>>));
			}

			Type where = typeof(Where<ChildInventoryItem.stkItem.IsEqual<True>
					.And<ChildInventoryItem.exportToExternal.IsEqual<True>>
					.And<BCSyncStatus.connectorType.IsEqual<P.AsString>>
					.And<BCSyncStatus.bindingID.IsEqual<P.AsInt>>
					.And<Brackets<BCSyncStatus.entityType.IsEqual<BCEntitiesAttribute.stockItem>
						.Or<BCSyncStatus.entityType.IsEqual<BCEntitiesAttribute.productWithVariant>>>>
					.And<BCSyncStatus.localID.IsNotNull>
					.And<BCSyncStatus.externID.IsNotNull>
					.And<BCSyncStatus.deleted.IsNotEqual<True>>>);

			typesList.Add(where);
			typesList.Add(groupBy);
			typesList.Add(typeof(OrderBy<Asc<BCSyncStatus.syncID, Asc<ChildInventoryItem.inventoryID>>>));

			Type select = BqlCommand.Compose(typesList.ToArray());
			BqlCommand cmd = BqlCommand.CreateInstance(select);
			PXView view = new PXView(this, true, cmd);

			parameters.Add(Operation.ConnectorType);
			parameters.Add(bindingExt?.BindingID);

			Type siteConditions = null;
			foreach (var key in siteLocations.Keys)
			{
				siteConditions = siteConditions == null ? BqlCommand.Compose(typeof(Where<,>), typeof(PX.Objects.IN.INSite.siteID), typeof(Equal<>), typeof(P.AsInt)) :
					BqlCommand.Compose(typeof(Where2<,>), siteConditions, typeof(Or<,>), typeof(PX.Objects.IN.INSite.siteID), typeof(Equal<>), typeof(P.AsInt));
				parameters.Add(key);
			}
			if (siteConditions != null)
				view.WhereAnd(siteConditions);

			view = ComposeStatusConditions(view, statuses, ref parameters);

			return view;
		}

		public virtual PXView ComposeStatusConditions(PXView commandBQL, List<BCSyncStatus> statuses, ref List<object> parameters)
		{
			if (parameters == null)
				parameters = new List<object>();

			Type condition = null;
			foreach (var stat in statuses)
			{
				if (stat?.LocalID == null) continue;
				if (condition == null)
					condition = BqlCommand.Compose(typeof(Where<,,>), typeof(PX.Objects.IN.InventoryItem.noteID), typeof(Equal<>), typeof(P.AsGuid),
						typeof(Or<,>), typeof(ChildInventoryItem.noteID), typeof(Equal<>), typeof(P.AsGuid));
				else
					condition = BqlCommand.Compose(typeof(Where2<,>), condition, typeof(Or<,,>), typeof(PX.Objects.IN.InventoryItem.noteID), typeof(Equal<>), typeof(P.AsGuid),
						typeof(Or<,>), typeof(ChildInventoryItem.noteID), typeof(Equal<>), typeof(P.AsGuid));
				parameters.Add(stat.LocalID);
				parameters.Add(stat.LocalID);
			}

			if(condition != null)
				commandBQL.WhereAnd(condition);

			return commandBQL;
		}

		public StorageDetailsResult ParseResult(BCBindingExt bindingExt, Dictionary<int, INSite> warehouses, Dictionary<int, INLocation> locations, PXResult row)
		{
			ChildInventoryItem inventoryItem = row.GetItem<ChildInventoryItem>();
			BCSyncStatus parentStatus = row.GetItem<BCSyncStatus>();
			BCSyncDetail syncDetail = row.GetItem<BCSyncDetail>();
			PX.Objects.IN.INSiteStatus siteStatus = row.GetItem<PX.Objects.IN.INSiteStatus>();
			PX.Objects.IN.INSiteLotSerial siteLotSerial = row.GetItem<PX.Objects.IN.INSiteLotSerial>();
			PX.Objects.IN.INLocationStatus locationStatus = row.GetItem<PX.Objects.IN.INLocationStatus>();
			warehouses.TryGetValue(siteStatus != null && siteStatus.SiteID.HasValue ? siteStatus.SiteID.Value : 0, out PX.Objects.IN.INSite site);
			PX.Objects.IN.INLocation location = null;
			if (locationStatus != null && locationStatus.LocationID.HasValue)
				locations.TryGetValue(locationStatus.LocationID.Value, out location);

			StorageDetailsResult result = new StorageDetailsResult();
			result.ParentSyncId = parentStatus.SyncID.ValueField();
			result.Availability = (inventoryItem.Availability == null || string.Equals(inventoryItem.Availability.Trim(), BCItemAvailabilities.StoreDefault) ? bindingExt.Availability : inventoryItem.Availability.Trim()).ValueField();
			result.InventoryDescription = inventoryItem.Descr?.Trim().ValueField();
			result.InventoryID = inventoryItem.InventoryID.ValueField();
			result.InventoryCD = inventoryItem.InventoryCD.Trim().ValueField();
			result.InventoryNoteID = inventoryItem.NoteID.ValueField();
			result.InventoryLastModifiedDate = inventoryItem?.LastModifiedDateTime.ValueField();
			result.IsTemplate = inventoryItem.IsTemplate.ValueField();
			result.ItemStatus = inventoryItem?.ItemStatus.ValueField();
			result.LocationDescription = inventoryItem.IsTemplate == true ? null : location?.Descr?.Trim().ValueField();
			result.LocationID = inventoryItem.IsTemplate == true ? null : location?.LocationID.ValueField();
			result.LocationCD = inventoryItem.IsTemplate == true ? null : location?.LocationCD?.Trim().ValueField();
			result.LocationLastModifiedDate = locationStatus?.LastModifiedDateTime.ValueField();
			result.LocationAvailableforIssue = locationStatus?.QtyActual.ValueField();
			result.LocationAvailable = locationStatus?.QtyAvail.ValueField();
			result.LocationAvailableforShipping = locationStatus?.QtyHardAvail.ValueField();
			result.LocationOnHand = locationStatus?.QtyOnHand.ValueField();
			result.NotAvailMode = (inventoryItem.NotAvailMode == null || string.Equals(inventoryItem.NotAvailMode.Trim(), BCItemAvailabilities.StoreDefault) ? bindingExt.NotAvailMode : inventoryItem.NotAvailMode.Trim()).ValueField();
			result.SiteDescription = inventoryItem.IsTemplate == true ? null : site?.Descr?.Trim().ValueField();
			result.SiteID = inventoryItem.IsTemplate == true ? null : site?.SiteID.ValueField();
			result.SiteCD = inventoryItem.IsTemplate == true ? null : site?.SiteCD?.Trim().ValueField();
			result.SiteAvailable = (siteLotSerial?.QtyAvail ?? siteStatus?.QtyAvail ?? 0).ValueField();
			result.SiteAvailableforIssue = (siteLotSerial?.QtyActual ?? siteStatus?.QtyActual ?? 0).ValueField();
			result.SiteAvailableforShipping = (siteLotSerial?.QtyHardAvail ?? siteStatus?.QtyHardAvail ?? 0).ValueField();
			result.SiteOnHand = (siteLotSerial?.QtyOnHand ?? siteStatus?.QtyOnHand ?? 0).ValueField();
			result.SiteLastModifiedDate = siteStatus?.LastModifiedDateTime.ValueField();
			result.TemplateItemID = inventoryItem.TemplateItemID.ValueField();
			result.ExportToExternal = inventoryItem.ExportToExternal.ValueField();
			result.ProductExternID = parentStatus.ExternID.ValueField();
			result.VariantExternID = syncDetail?.ExternID == null && inventoryItem.IsTemplate == false && inventoryItem.TemplateItemID == null ? parentStatus.ExternID.ValueField() : syncDetail?.ExternID.ValueField();

			return result;
		}

		#endregion Get Product Availability
	}
}
