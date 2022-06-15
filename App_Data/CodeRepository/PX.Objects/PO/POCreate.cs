using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AP.MigrationMode;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common.DAC;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO.GraphExtensions.POOrderEntryExt;
using PX.Objects.SO;
using PX.TM;
using CRLocation = PX.Objects.CR.Standalone.Location;
using SOLine5 = PX.Objects.PO.POOrderEntry.SOLine5;
using SOLineSplit3 = PX.Objects.PO.POOrderEntry.SOLineSplit3;

namespace PX.Objects.PO
{
	[PX.Objects.GL.TableAndChartDashboardType]
	[Serializable]
	public class POCreate : PXGraph<POCreate>
	{

		public PXCancel<POCreateFilter> Cancel;
		public PXAction<POCreateFilter> viewDocument;
		public PXFilter<POCreateFilter> Filter;
		[PXFilterable]
		public PXFilteredProcessingJoin<POFixedDemand, POCreateFilter,
			LeftJoin<Vendor, On<Vendor.bAccountID, Equal<POFixedDemand.vendorID>>,
			LeftJoin<POVendorInventory,
				  On<POVendorInventory.recordID, Equal<POFixedDemand.recordID>>,
			LeftJoin<CRLocation, On<CRLocation.bAccountID, Equal<POFixedDemand.vendorID>, And<CRLocation.locationID, Equal<POFixedDemand.vendorLocationID>>>,
			LeftJoin<SOOrder, On<SOOrder.noteID, Equal<POFixedDemand.refNoteID>, And<SOOrder.status.IsIn<SOOrderStatus.backOrder, SOOrderStatus.open, SOOrderStatus.shipping>>>,
			LeftJoin<SOLine, On<SOLine.orderType, Equal<POFixedDemand.orderType>, And<SOLine.orderNbr, Equal<POFixedDemand.orderNbr>, And<SOLine.lineNbr, Equal<POFixedDemand.lineNbr>>>>,
			LeftJoin<DropShipLink, On<DropShipLink.FK.SOLine>>>>>>>,
			Where2<Where<POFixedDemand.vendorID, Equal<Current<POCreateFilter.vendorID>>, Or<Current<POCreateFilter.vendorID>, IsNull>>,
				And2<Where<POFixedDemand.inventoryID, Equal<Current<POCreateFilter.inventoryID>>, Or<Current<POCreateFilter.inventoryID>, IsNull>>,
				And2<Where<POFixedDemand.siteID, Equal<Current<POCreateFilter.siteID>>, Or<Current<POCreateFilter.siteID>, IsNull>>,
				And2<Where<SOOrder.customerID, Equal<Current<POCreateFilter.customerID>>, Or<Current<POCreateFilter.customerID>, IsNull, Or<SOOrder.orderNbr, IsNull>>>,
				And2<Where<SOOrder.orderType, Equal<Current<POCreateFilter.orderType>>, Or<Current<POCreateFilter.orderType>, IsNull>>,
				And2<Where<SOOrder.orderNbr, Equal<Current<POCreateFilter.orderNbr>>, Or<Current<POCreateFilter.orderNbr>, IsNull>>,
				And2<Where<POFixedDemand.planDate, LessEqual<Current<POCreateFilter.requestedOnDate>>, Or<Current<POCreateFilter.requestedOnDate>, IsNull>>,
				And2<Where<POFixedDemand.orderType, IsNull, Or<POFixedDemand.behavior, NotEqual<SOBehavior.bL>, Or<POFixedDemand.pOCreateDate, LessEqual<Current<POCreateFilter.purchDate>>>>>,
				And2<Where<POFixedDemand.itemClassCD, Like<Current<POCreateFilter.itemClassCDWildcard>>, Or<Current<POCreateFilter.itemClassCDWildcard>, IsNull>>,
				And<POFixedDemand.planQty, NotEqual<decimal0>,
				And<Where<POFixedDemand.planType, NotIn3<INPlanConstants.plan6D, INPlanConstants.plan6E>,
						Or<POFixedDemand.baseShippedQty, Equal<decimal0>,
							And<DropShipLink.sOLineNbr, IsNull,
							And<SOLine.isLegacyDropShip, Equal<boolFalse>>>>>>>>>>>>>>>>,
			OrderBy<Asc<POFixedDemand.inventoryID>>> FixedDemand;
		public POCreate()
		{
			APSetupNoMigrationMode.EnsureMigrationModeDisabled(this);

			PXUIFieldAttribute.SetEnabled<POFixedDemand.orderQty>(FixedDemand.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.fixedSource>(FixedDemand.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.sourceSiteID>(FixedDemand.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.vendorID>(FixedDemand.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.vendorLocationID>(FixedDemand.Cache, null, true);

			PXUIFieldAttribute.SetDisplayName<InventoryItem.descr>(this.Caches[typeof(InventoryItem)], Messages.InventoryItemDescr);
			PXUIFieldAttribute.SetDisplayName<INSite.descr>(this.Caches[typeof(INSite)], Messages.SiteDescr);
			PXUIFieldAttribute.SetDisplayName<Vendor.acctName>(this.Caches[typeof(Vendor)], Messages.VendorAcctName);
			PXUIFieldAttribute.SetDisplayName<Customer.acctName>(this.Caches[typeof(Customer)], Messages.CustomerAcctName);
			PXUIFieldAttribute.SetDisplayName<SOOrder.customerLocationID>(this.Caches[typeof(SOOrder)], Messages.CustomerLocationID);
			PXUIFieldAttribute.SetDisplayName<INPlanType.descr>(this.Caches[typeof(INPlanType)], Messages.PlanTypeDescr);

			PXUIFieldAttribute.SetDisplayName<SOLine.curyUnitPrice>(this.Caches[typeof(SOLine)], Messages.CustomerPrice);
			PXUIFieldAttribute.SetDisplayName<SOLine.unitPrice>(this.Caches[typeof(SOLine)], Messages.CustomerPrice);
			PXUIFieldAttribute.SetDisplayName<SOLine.uOM>(this.Caches[typeof(SOLine)], Messages.CustomerPriceUOM);
			PXUIFieldAttribute.SetRequired<SOLine.uOM>(this.Caches[typeof(SOLine)], false);

			PXUIFieldAttribute.SetDisplayName<POLine.orderNbr>(this.Caches[typeof(POLine)], Messages.POLineOrderNbr);
		}

		protected IEnumerable filter()
		{
			POCreateFilter filter = this.Filter.Current;
			filter.OrderVolume = 0;
			filter.OrderWeight = 0;
			filter.OrderTotal = 0;
			foreach (POFixedDemand demand in this.FixedDemand.Cache.Updated)
				if (demand.Selected == true)
				{
					filter.OrderVolume += demand.ExtVolume ?? 0m;
					filter.OrderWeight += demand.ExtWeight ?? 0m;
					filter.OrderTotal += demand.ExtCost ?? 0m;
				}
			yield return filter;
		}

		protected virtual IEnumerable fixedDemand()
		{
			PXResultset<POFixedDemand> fixedDemands = SelectFromFixedDemandView();
			return EnumerateAndPrepareFixedDemands(fixedDemands);
		}

		public virtual PXResultset<POFixedDemand> SelectFromFixedDemandView()
		{
			PXView query = new PXView(this, false, FixedDemand.View.BqlSelect);

			var fixedDemands = new PXResultset<POFixedDemand>();
			var startRow = PXView.StartRow;
			var totalRows = 0;
			object[] parameters = null;

			if (PXView.MaximumRows == 1 && PXView.SortColumns != null && PXView.Searches != null
				&& Array.FindIndex(PXView.SortColumns,
					s => s.Equals(nameof(POFixedDemand.planID), StringComparison.OrdinalIgnoreCase))
					is int planIDIndex
				&& planIDIndex >= 0
				&& PXView.Searches[planIDIndex] != null)
			{
				long planID = Convert.ToInt64(PXView.Searches[planIDIndex]);
				query.WhereAnd<Where<POFixedDemand.planID.IsEqual<@P.AsLong>>>();
				parameters = parameters.Append(planID);
			}

			using (new PXFieldScope(query, GetFixedDemandFieldScope()))
			{
				foreach (PXResult<POFixedDemand> demand in query.Select(PXView.Currents, parameters,
					PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters,
					ref startRow, PXView.MaximumRows, ref totalRows))
				{
					fixedDemands.Add(demand);
				}
			}

			PXView.StartRow = 0;

			return fixedDemands;
		}

		public virtual IEnumerable<Type> GetFixedDemandFieldScope()
		{
			yield return typeof(POFixedDemand);
			yield return typeof(Vendor.bAccountID);
			yield return typeof(Vendor.curyID);
			yield return typeof(Vendor.termsID);
			yield return typeof(POVendorInventory.recordID);
			yield return typeof(POVendorInventory.lastPrice);
			yield return typeof(POVendorInventory.addLeadTimeDays);
			yield return typeof(CRLocation.locationID);
			yield return typeof(CRLocation.vLeadTime);
			yield return typeof(CRLocation.vCarrierID);
			yield return typeof(SOOrder.orderType);
			yield return typeof(SOOrder.orderNbr);
			yield return typeof(SOOrder.customerID);
			yield return typeof(SOOrder.customerLocationID);
			yield return typeof(SOOrder.noteID);
			yield return typeof(SOLine.orderType);
			yield return typeof(SOLine.orderNbr);
			yield return typeof(SOLine.lineNbr);
			yield return typeof(SOLine.unitPrice);
			yield return typeof(SOLine.inventoryID);
			yield return typeof(SOLine.uOM);
			yield return typeof(SOLine.noteID);
		}

		/// <summary>
		/// Enumerates the and prepares fixed demands for the view delegate. This is an extension point used by Lexware PriceUnit customization.
		/// </summary>
		/// <param name="fixedDemands">The fixed demands.</param>
		/// <returns/>
		public virtual IEnumerable EnumerateAndPrepareFixedDemands(PXResultset<POFixedDemand> fixedDemands)
		{
			foreach (PXResult<POFixedDemand> rec in fixedDemands)
			{
				EnumerateAndPrepareFixedDemandRow(rec);

				yield return rec;
			}
		}

		public virtual void EnumerateAndPrepareFixedDemandRow(PXResult<POFixedDemand> rec)
		{
			var demand = (POFixedDemand)rec;
			var vendor = PXResult.Unwrap<Vendor>(rec);
			var price = PXResult.Unwrap<POVendorInventory>(rec);

			if (demand?.InventoryID != null && demand.UOM != null && demand.VendorID != null && vendor?.CuryID != null &&
				Filter.Current.PurchDate != null && demand.EffPrice == null)
			{
				demand.EffPrice = APVendorPriceMaint.CalculateCuryUnitCost(
					sender: FixedDemand.Cache,
					vendorID: demand.VendorID,
					vendorLocationID: demand.VendorLocationID,
					inventoryID: demand.InventoryID,
					siteID: demand.SiteID,
					curyID: vendor.CuryID,
					UOM: demand.UOM,
					quantity: demand.OrderQty,
					date: Filter.Current.PurchDate.Value,
					currentUnitCost: 0m);
			}

			if (demand.RecordID != null)
			{
				if (demand.EffPrice == null)
					demand.EffPrice = price.LastPrice;

				demand.AddLeadTimeDays = price.AddLeadTimeDays;
			}

			if (demand.EffPrice != null && demand.OrderQty != null && demand.ExtCost == null)
				demand.ExtCost = demand.OrderQty * demand.EffPrice;
		}

		protected virtual void POCreateFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			POCreateFilter filter = Filter.Current;

			if (filter == null) return;

			FixedDemand.SetProcessDelegate(delegate (List<POFixedDemand> list)
			{
				POCreate graphPOCreate = PXGraph.CreateInstance<POCreate>();
				graphPOCreate.Filter.Cache.RestoreCopy(graphPOCreate.Filter.Current, filter);

				// Acuminator disable once PX1086 SetupNotEnteredExceptionInLongRunOperation Legacy
				graphPOCreate.CreateProc(list, filter.PurchDate, filter.OrderNbr != null, filter.BranchID);
			});

			TimeSpan span;
			Exception message;
			PXLongRunStatus status = PXLongOperation.GetStatus(this.UID, out span, out message);

			PXUIFieldAttribute.SetVisible<POLine.orderNbr>(Caches[typeof(POLine)], null, (status == PXLongRunStatus.Completed || status == PXLongRunStatus.Aborted));
			PXUIFieldAttribute.SetVisible<POCreateFilter.orderTotal>(sender, null, filter.VendorID != null);
		}

		public virtual void POCreateFilter_ItemClassCDWildCard_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void POFixedDemand_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			POFixedDemand row = (POFixedDemand)e.Row;
			if (row != null && row.Selected != true
				&& sender.ObjectsEqual<POFixedDemand.selected>(e.Row, e.OldRow))
			{
				row.Selected = true;
			}
		}
		protected virtual void POFixedDemand_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			POFixedDemand row = e.Row as POFixedDemand;
			if (row == null) return;

			PXUIFieldAttribute.SetEnabled<POFixedDemand.orderQty>(sender, row, row.PlanType == INPlanConstants.Plan90);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.fixedSource>(FixedDemand.Cache, row, row.PlanType == INPlanConstants.Plan90);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.pOSiteID>(sender, row, row.FixedSource == INReplenishmentSource.Purchased);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.sourceSiteID>(sender, row, row.FixedSource == INReplenishmentSource.Transfer);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.vendorID>(sender, row, row.FixedSource == INReplenishmentSource.Purchased);
			PXUIFieldAttribute.SetEnabled<POFixedDemand.vendorLocationID>(sender, row, row.FixedSource == INReplenishmentSource.Purchased);
		}

		protected virtual void POFixedDemand_VendorLocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			POFixedDemand row = (POFixedDemand)e.Row;
			if (row != null)
			{
				e.NewValue =
					PX.Objects.PO.POItemCostManager.FetchLocation(
						this,
						row.VendorID,
						row.InventoryID,
						row.SubItemID,
						row.SiteID);
				e.Cancel = true;
			}
		}

		protected virtual void POFixedDemand_OrderQty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			POFixedDemand row = (POFixedDemand)e.Row;
			if (row != null && row.PlanUnitQty < (Decimal?)e.NewValue)
			{
				e.NewValue = row.PlanUnitQty;
				sender.RaiseExceptionHandling<POFixedDemand.orderQty>(row, null,
																	  new PXSetPropertyException<POFixedDemand.orderQty>(
																		  Messages.POOrderQtyValidation, PXErrorLevel.Warning));
			}
		}

		protected virtual void POFixedDemand_RecordID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			POFixedDemand row = (POFixedDemand)e.Row;
			POVendorInventory result = null;
			if (row == null) return;
			foreach (PXResult<POVendorInventory, BAccountR, InventoryItem> rec in
				PXSelectJoin<POVendorInventory,
				InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<POVendorInventory.vendorID>>,
				InnerJoin<InventoryItem,
					On<POVendorInventory.FK.InventoryItem>>>,
				Where<POVendorInventory.vendorID, Equal<Current<POFixedDemand.vendorID>>,
				And<POVendorInventory.inventoryID, Equal<Current<POFixedDemand.inventoryID>>,
				And<POVendorInventory.active, Equal<boolTrue>,
				And2<Where<POVendorInventory.vendorLocationID, Equal<Current<POFixedDemand.vendorLocationID>>,
						Or<POVendorInventory.vendorLocationID, IsNull>>,
					  And<Where<POVendorInventory.subItemID, Equal<Current<POFixedDemand.subItemID>>,
							 Or<POVendorInventory.subItemID, Equal<InventoryItem.defaultSubItemID>>>>>>>>>
				.SelectMultiBound(this, new object[] { e.Row }))
			{
				POVendorInventory price = rec;
				InventoryItem item = rec;
				if (price.VendorLocationID == row.VendorLocationID &&
					price.SubItemID == row.SubItemID)
				{
					result = price;
					break;
				}

				if (price.VendorLocationID == row.VendorLocationID)
					result = price;

				if (result != null && result.VendorLocationID != row.VendorLocationID &&
					price.SubItemID == row.SubItemID)
					result = price;

				if (result == null)
					result = price;
			}
			if (result != null)
			{
				e.NewValue = result.RecordID;
				e.Cancel = true;
			}


		}

		protected virtual void POFixedDemand_RecordID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			POFixedDemand demand = e.Row as POFixedDemand;

			if (demand == null || Filter.Current == null)
				return;

			decimal? vendorUnitCost = null;

			if (demand.InventoryID != null && demand.UOM != null && demand.VendorID != null && Filter.Current.PurchDate != null)
			{
				Vendor vendor = PXSelect<Vendor,
								   Where<Vendor.bAccountID, Equal<Required<Vendor.bAccountID>>>>
								.Select(this, demand.VendorID);

				if (vendor?.CuryID != null)
				{
					vendorUnitCost = APVendorPriceMaint.CalculateCuryUnitCost(sender, demand.VendorID, demand.VendorLocationID, demand.InventoryID,
																			  demand.SiteID, vendor.CuryID, demand.UOM, demand.OrderQty,
																			  (DateTime)Filter.Current.PurchDate, 0m);
				}

				demand.EffPrice = vendorUnitCost;
			}

			POVendorInventory price =
				PXSelect<POVendorInventory,
				   Where<POVendorInventory.recordID, Equal<Required<POVendorInventory.recordID>>>>
				.SelectSingleBound(this, null, demand.RecordID);

			if (vendorUnitCost == null)
			{
				demand.EffPrice = price?.LastPrice ?? 0m;
			}

			demand.AddLeadTimeDays = price?.AddLeadTimeDays;
			FixedDemand.Cache.RaiseFieldUpdated<POFixedDemand.effPrice>(demand, null);
		}

		#region Actions
		public PXAction<POCreateFilter> inventorySummary;

		[PXUIField(DisplayName = "Inventory Summary", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnProcessingResults = true)]
		public virtual IEnumerable InventorySummary(PXAdapter adapter)
		{
			PXCache tCache = FixedDemand.Cache;
			POFixedDemand line = FixedDemand.Current;
			if (line == null) return adapter.Get();

			InventoryItem item = InventoryItem.PK.Find(this, line.InventoryID);
			if (item != null && item.StkItem == true)
			{
				INSubItem sbitem = (INSubItem)PXSelectorAttribute.Select<POFixedDemand.subItemID>(tCache, line);
				InventorySummaryEnq.Redirect(item.InventoryID,
											 ((sbitem != null) ? sbitem.SubItemCD : null),
											 line.SiteID,
											 line.LocationID);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXEditDetailButton]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			POFixedDemand line = FixedDemand.Current;
			if (line == null || line.RefNoteID == null) return adapter.Get();

			SOOrder doc = PXSelect<SOOrder, Where<SOOrder.noteID, Equal<Required<POFixedDemand.refNoteID>>>>.Select(this, line.RefNoteID);

			if (doc != null)
			{
				SOOrderEntry graph = PXGraph.CreateInstance<SOOrderEntry>();
				graph.Document.Current = doc;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
			}
			return adapter.Get();
		}
		#endregion

		public virtual void CreateProc(List<POFixedDemand> list, DateTime? orderDate, bool extSort, int? branchID = null)
		{
			PXRedirectRequiredException poredirect = CreatePOOrders(list, orderDate, extSort, branchID);

			if (poredirect != null)
				throw poredirect;
		}

		public virtual PXRedirectRequiredException CreatePOOrders(List<POFixedDemand> list, DateTime? PurchDate, bool extSort, int? branchID = null)
		{
			POOrderEntry docgraph = PXGraph.CreateInstance<POOrderEntry>();
			docgraph.Views.Caches.Add(typeof(SOLineSplit3));
			POSetup setup = docgraph.POSetup.Current;

			DocumentList<POOrder> created = new DocumentList<POOrder>(docgraph);
			Dictionary<String, DocumentList<POLine>> orderedByPlantype = new Dictionary<String, DocumentList<POLine>>();
			DocumentList<POLine> ordered;

			list = docgraph.SortPOFixDemandList(list);

			POOrder order = null;
			bool hasErrors = false;

			foreach (POFixedDemand demand in list)
			{
				if (demand.FixedSource != INReplenishmentSource.Purchased)
					continue;

				if (demand.VendorID == null || demand.VendorLocationID == null)
				{
					PXProcessing<POFixedDemand>.SetWarning(list.IndexOf(demand), Messages.MissingVendorOrLocation);
					continue;
				}

				PXErrorLevel ErrorLevel = PXErrorLevel.RowInfo;
				string ErrorText = string.Empty;

				try
				{
					SOOrder soorder = PXSelect<SOOrder, Where<SOOrder.noteID, Equal<Required<SOOrder.noteID>>>>.Select(docgraph, demand.RefNoteID);
					SOLineSplit3 soline = PXSelect<SOLineSplit3, Where<SOLineSplit3.planID, Equal<Required<SOLineSplit3.planID>>>>.Select(docgraph, demand.PlanID);

					if (soline?.POSource.IsIn(INReplenishmentSource.DropShipToOrder, INReplenishmentSource.BlanketDropShipToOrder) == true && soline.IsValidForDropShip != true)
					{
						throw new PXException(SO.Messages.DropShipSOLineCantHaveMultipleSplitsOrAllocation);
					}

					bool requireSingleProject = docgraph.apsetup.Current.RequireSingleProjectPerDocument == true;

					order = FindOrCreatePOOrder(created, order, demand, soorder, soline, requireSingleProject);

					//we do not want vendor inventory updated in this case
					order.UpdateVendorCost = false;

					if (order.OrderNbr == null)
					{
						docgraph.Clear();
						order = docgraph.FillPOOrderFromDemand(order, demand, soorder, PurchDate, extSort, branchID);
					}
					else if (docgraph.Document.Cache.ObjectsEqual(docgraph.Document.Current, order) == false)
					{
						order = docgraph.Document.Current = docgraph.Document.Search<POOrder.orderNbr>(order.OrderNbr, order.OrderType);
					}

					//Sales Orders to Blanket should not be grouped together
					//Drop Ships to Blankets are not grouped either
					if (!orderedByPlantype.TryGetValue(demand.PlanType, out ordered))
					{
						ordered = orderedByPlantype[demand.PlanType] = new DocumentList<POLine>(docgraph);
					}

					POLine line = FindOrCreatePOLine(docgraph, ordered, order.OrderType, demand, soline);
					if (line.OrderNbr == null)
					{
						docgraph.FillPOLineFromDemand(line, demand, order.OrderType, soline);
						line = docgraph.Transactions.Insert(line);

						if (setup.CopyLineNoteSO == true && soline != null)
						{
							PXNoteAttribute.SetNote(docgraph.Transactions.Cache, line,
								PXNoteAttribute.GetNote(docgraph.Caches[typeof(SOLineSplit3)], soline));
						}

						docgraph.onCopyPOLineFields?.Invoke(demand, line);

						line = PXCache<POLine>.CreateCopy(line);
						ordered.Add(line);
					}
					else
					{
						line = (POLine)
							PXSelect<POLine,
							   Where<POLine.orderType, Equal<Current<POOrder.orderType>>,
							   And<POLine.orderNbr, Equal<Current<POOrder.orderNbr>>,
							   And<POLine.lineNbr, Equal<Current<POLine.lineNbr>>>>>>
							   .SelectSingleBound(docgraph, new object[] { line });

						line = PXCache<POLine>.CreateCopy(line);
						line.OrderQty += demand.OrderQty;
					}

					string replanType = LinkPOLineToBlanket(line, docgraph, demand, soline, ref ErrorLevel, ref ErrorText);
					line = docgraph.Transactions.Update(line);

					var dsLinksExt = docgraph.GetExtension<DropShipLinksExt>();
					dsLinksExt?.InsertDropShipLink(line, soline);

					PXCache cache = docgraph.Caches[typeof(INItemPlan)];
					CreateSplitDemand(cache, demand);

					cache.SetStatus(demand, PXEntryStatus.Updated);
					demand.SupplyPlanID = line.PlanID;
					// TODO: it is the temporary fix against clearing the fields
					((INItemPlan)demand).UOM = demand.PlanUOM;
					((INItemPlan)demand).ProjectID = demand.PlanProjectID;

					if (replanType != null)
					{
						cache.RaiseRowDeleted(demand);
						demand.PlanType = replanType;
						cache.RaiseRowInserted(demand);
					}

					if (soline != null)
					{
						LinkPOLineToSOLineSplit(docgraph, soline, line);

						docgraph.UpdateSOLine(soline, docgraph.Document.Current.VendorID, true);
						docgraph.FixedDemand.Cache.SetStatus(soline, PXEntryStatus.Updated);
					}

					if (docgraph.Transactions.Cache.IsInsertedUpdatedDeleted)
					{
						using (PXTransactionScope scope = new PXTransactionScope())
						{
							docgraph.Save.Press();

							if (demand.PlanType == INPlanConstants.Plan90)
							{
								docgraph.Replenihment.Current = docgraph.Replenihment.Search<INReplenishmentOrder.noteID>(demand.RefNoteID);
								InsertReplenishmentLine(docgraph, demand, line);
							}
							scope.Complete();
						}

						if (ErrorLevel == PXErrorLevel.RowInfo)
						{
							PXProcessing<POFixedDemand>.SetInfo(list.IndexOf(demand), PXMessages.LocalizeFormatNoPrefixNLA(Messages.PurchaseOrderCreated, docgraph.Document.Current.OrderNbr) + "\r\n" + ErrorText);
						}
						else
						{
							PXProcessing<POFixedDemand>.SetWarning(list.IndexOf(demand), PXMessages.LocalizeFormatNoPrefixNLA(Messages.PurchaseOrderCreated, docgraph.Document.Current.OrderNbr) + "\r\n" + ErrorText);
						}

						if (created.Find(docgraph.Document.Current) == null)
						{
							created.Add(docgraph.Document.Current);
						}
					}
				}
				catch (Exception e)
				{
					PXProcessing<POFixedDemand>.SetError(list.IndexOf(demand), e);
					PXTrace.WriteError(e);
					hasErrors = true;
				}
			}

			if (!hasErrors && created.Count == 1)
			{
				using (new PXTimeStampScope(null))
				{
					docgraph.Clear();
					docgraph.Document.Current = docgraph.Document.Search<POOrder.orderNbr>(created[0].OrderNbr, created[0].OrderType);
					return new PXRedirectRequiredException(docgraph, Messages.POOrder);
				}
			}

			return null;
		}

		protected virtual void InsertReplenishmentLine(POOrderEntry docgraph, POFixedDemand demand, POLine line)
		{
			if (docgraph.Replenihment.Current != null)
			{
				INReplenishmentLine rLine =
					PXCache<INReplenishmentLine>.CreateCopy(docgraph.ReplenishmentLines.Insert(new INReplenishmentLine()));

				rLine.InventoryID = line.InventoryID;
				rLine.SubItemID = line.SubItemID;
				rLine.UOM = line.UOM;
				rLine.VendorID = line.VendorID;
				rLine.VendorLocationID = line.VendorLocationID;
				rLine.Qty = line.OrderQty;
				rLine.POType = line.OrderType;
				rLine.PONbr = docgraph.Document.Current.OrderNbr;
				rLine.POLineNbr = line.LineNbr;
				rLine.SiteID = demand.POSiteID;
				rLine.PlanID = demand.PlanID;
				docgraph.ReplenishmentLines.Update(rLine);
				docgraph.Caches[typeof(INItemPlan)].Delete(demand);
				docgraph.Save.Press();
			}
		}

		protected virtual void LinkPOLineToSOLineSplit(POOrderEntry docgraph, SOLineSplit3 soline, POLine line)
		{
			soline.POType = line.OrderType;
			soline.PONbr = line.OrderNbr;
			soline.POLineNbr = line.LineNbr;
			soline.RefNoteID = docgraph.Document.Current.NoteID;

			string targetPOSource = soline.POSource == INReplenishmentSource.BlanketDropShipToOrder ? INReplenishmentSource.DropShipToOrder
				: soline.POSource == INReplenishmentSource.BlanketPurchaseToOrder ? INReplenishmentSource.PurchaseToOrder
				: null;

			if (targetPOSource != null)
			{
				soline.POSource = targetPOSource;

				SOLine5 origsoline = docgraph.FixedDemandOrigSOLine.Select(soline.OrderType, soline.OrderNbr, soline.LineNbr);
				if (origsoline != null)
				{
					origsoline.POSource = targetPOSource;
					docgraph.FixedDemandOrigSOLine.Cache.MarkUpdated(origsoline);
				}
			}
		}

		protected virtual string LinkPOLineToBlanket(POLine line, POOrderEntry docgraph, POFixedDemand demand, SOLineSplit3 soline, ref PXErrorLevel ErrorLevel, ref string ErrorText)
		{
			string replanType = null;

			if (demand.PlanType == INPlanConstants.Plan6B ||
				demand.PlanType == INPlanConstants.Plan6E)
			{
				replanType = demand.PlanType == INPlanConstants.Plan6B
						? INPlanConstants.Plan66
						: INPlanConstants.Plan6D;
				demand.FixedSource = INReplenishmentSource.Purchased;

				line.POType = soline.POType;
				line.PONbr = soline.PONbr;
				line.POLineNbr = soline.POLineNbr;

				POLine blanket_line =
					PXSelect<POLine,
						Where<POLine.orderType, Equal<Current<POLine.pOType>>,
						  And<POLine.orderNbr, Equal<Current<POLine.pONbr>>,
						  And<POLine.lineNbr, Equal<Current<POLine.pOLineNbr>>>>>>
					 .SelectSingleBound(docgraph, new object[] { line });

				if (blanket_line != null)
				{
					//POOrderEntry() is persisted on each loop, BaseOpenQty will include everything in List<POLine> ordered
					if (demand.PlanQty > blanket_line.BaseOpenQty)
					{
						line.OrderQty -= demand.OrderQty;

						if (string.Equals(line.UOM, blanket_line.UOM))
						{
							line.OrderQty += blanket_line.OpenQty;
						}
						else
						{
							PXDBQuantityAttribute.CalcBaseQty<POLine.orderQty>(docgraph.Transactions.Cache, line);
							line.BaseOrderQty += blanket_line.BaseOpenQty;
							PXDBQuantityAttribute.CalcTranQty<POLine.orderQty>(docgraph.Transactions.Cache, line);
						}

						ErrorLevel = PXErrorLevel.RowWarning;
						ErrorText += PXMessages.LocalizeFormatNoPrefixNLA(Messages.QuantityReducedToBlanketOpen, line.PONbr);
					}

					line.CuryUnitCost = blanket_line.CuryUnitCost;
					line.UnitCost = blanket_line.UnitCost;
				}
			}

			return replanType;
		}

		protected virtual POLine FindOrCreatePOLine(POOrderEntry docgraph, DocumentList<POLine> ordered, string orderType, POFixedDemand demand, SOLineSplit3 soline)
		{
			POLine line = null;
			POSetup poSetup = docgraph.POSetup.Current;

			if (orderType == POOrderType.RegularOrder && demand.PlanType != INPlanConstants.Plan6B)
			{
				var lineSearchValues = new List<FieldLookup>()
				{
					new FieldLookup<POLine.vendorID>(demand.VendorID),
					new FieldLookup<POLine.vendorLocationID>(demand.VendorLocationID),
					new FieldLookup<POLine.siteID>(demand.POSiteID),
					new FieldLookup<POLine.inventoryID>(demand.InventoryID),
					new FieldLookup<POLine.subItemID>(demand.SubItemID),
					new FieldLookup<POLine.requestedDate>(soline?.ShipDate),
					new FieldLookup<POLine.projectID>(soline?.ProjectID),
					new FieldLookup<POLine.taskID>(soline?.TaskID),
					new FieldLookup<POLine.costCodeID>(soline?.CostCodeID),
				};
				if (poSetup.CopyLineDescrSO == true && soline != null)
				{
					lineSearchValues.Add(new FieldLookup<POLine.tranDesc>(soline.TranDesc));
					line = ordered.Find(lineSearchValues.ToArray());
					if (line != null && poSetup.CopyLineNoteSO == true &&
						(PXNoteAttribute.GetNote(docgraph.Caches[typeof(POLine)], line) != null || PXNoteAttribute.GetNote(docgraph.Caches[typeof(SOLineSplit3)], soline) != null))
					{
						line = null;
					}
				}
				else
				{
					line = ordered.Find(lineSearchValues.ToArray());
				}
			}

			return line ?? new POLine();
		}

		protected virtual POOrder FindOrCreatePOOrder(DocumentList<POOrder> created, POOrder previousOrder, POFixedDemand demand, SOOrder soorder, SOLineSplit3 soline, bool requireSingleProject)
		{
			string OrderType = demand.PlanType.IsIn(INPlanConstants.Plan6D, INPlanConstants.Plan6E) ? POOrderType.DropShip : POOrderType.RegularOrder;
			bool linkToBlanket = demand.PlanType == INPlanConstants.Plan6B || demand.PlanType == INPlanConstants.Plan6E;

			var orderSearchValues = new List<FieldLookup>()
			{
				new FieldLookup<POOrder.orderType>(OrderType),
				new FieldLookup<POOrder.vendorID>(demand.VendorID),
				new FieldLookup<POOrder.vendorLocationID>(demand.VendorLocationID),
				new FieldLookup<POOrder.bLOrderNbr>(linkToBlanket ? soline.PONbr : null),
			};

			if (OrderType == POOrderType.RegularOrder)
			{
				if (requireSingleProject)
				{
					int? project = demand.ProjectID ?? PM.ProjectDefaultAttribute.NonProject();
					orderSearchValues.Add(new FieldLookup<POOrder.projectID>(project));
				}

				if (previousOrder != null && previousOrder.ShipDestType == POShippingDestination.CompanyLocation && previousOrder.SiteID == null)
				{
					//When previous order was shipped to Company then we would never find it if we search by POSiteID 
				}
				else
				{
					orderSearchValues.Add(new FieldLookup<POOrder.siteID>(demand.POSiteID));
				}
			}
			else if (OrderType == POOrderType.DropShip)
			{
				orderSearchValues.Add(new FieldLookup<POOrder.sOOrderType>(soline.OrderType));
				orderSearchValues.Add(new FieldLookup<POOrder.sOOrderNbr>(soline.OrderNbr));
			}
			else
			{
				orderSearchValues.Add(new FieldLookup<POOrder.shipToBAccountID>(soorder.CustomerID));
				orderSearchValues.Add(new FieldLookup<POOrder.shipToLocationID>(soorder.CustomerLocationID));
				orderSearchValues.Add(new FieldLookup<POOrder.siteID>(demand.POSiteID));
			}

			return created.Find(orderSearchValues.ToArray()) ?? new POOrder
			{
				OrderType = OrderType,
				BLType = linkToBlanket ? POOrderType.Blanket : null,
				BLOrderNbr = linkToBlanket ? soline.PONbr : null
			};
		}

		protected virtual void CreateSplitDemand(PXCache cache, POFixedDemand demand)
		{
			if (demand.OrderQty != demand.PlanUnitQty)
			{
				INItemPlan orig_demand = PXSelectReadonly<INItemPlan,
					Where<INItemPlan.planID, Equal<Current<INItemPlan.planID>>>>
					.SelectSingleBound(cache.Graph, new object[] { demand });

				INItemPlan split = PXCache<INItemPlan>.CreateCopy(orig_demand);
				split.PlanID = null;
				split.PlanQty = demand.PlanUnitQty - demand.OrderQty;
				if (demand.UnitMultDiv == MultDiv.Multiply)
					split.PlanQty *= demand.UnitRate;
				else
					split.PlanQty /= demand.UnitRate;
				cache.Insert(split);
				cache.RaiseRowDeleted(demand);
				demand.PlanQty = orig_demand.PlanQty - split.PlanQty;
				cache.RaiseRowInserted(demand);
			}
		}


		[Serializable()]
		public partial class POCreateFilter : IBqlTable
		{
			#region BranchID
			public abstract class branchID : BqlInt.Field<branchID> { }
			[Branch(FieldClass = nameof(FeaturesSet.MultipleBaseCurrencies), DisplayName = "PO Creation Branch")]
			public virtual Int32? BranchID { get; set; }
			#endregion
			#region CurrentOwnerID
			public abstract class currentOwnerID : PX.Data.BQL.BqlInt.Field<currentOwnerID> { }

			[PXDBInt]
			[CRCurrentOwnerID]
			public virtual int? CurrentOwnerID { get; set; }
			#endregion
			#region MyOwner
			public abstract class myOwner : PX.Data.BQL.BqlBool.Field<myOwner> { }
			protected Boolean? _MyOwner;
			[PXDBBool]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Me")]
			public virtual Boolean? MyOwner
			{
				get
				{
					return _MyOwner;
				}
				set
				{
					_MyOwner = value;
				}
			}
			#endregion
			#region OwnerID
			public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
			protected int? _OwnerID;
			[PX.TM.SubordinateOwner(DisplayName = "Product Manager")]
			public virtual int? OwnerID
			{
				get
				{
					return (_MyOwner == true) ? CurrentOwnerID : _OwnerID;
				}
				set
				{
					_OwnerID = value;
				}
			}
			#endregion
			#region WorkGroupID
			public abstract class workGroupID : PX.Data.BQL.BqlInt.Field<workGroupID> { }
			protected Int32? _WorkGroupID;
			[PXDBInt]
			[PXUIField(DisplayName = "Product  Workgroup")]
			[PXSelector(typeof(Search<EPCompanyTree.workGroupID,
				Where<EPCompanyTree.workGroupID, IsWorkgroupOrSubgroupOfContact<Current<AccessInfo.contactID>>>>),
			 SubstituteKey = typeof(EPCompanyTree.description))]
			public virtual Int32? WorkGroupID
			{
				get
				{
					return (_MyWorkGroup == true) ? null : _WorkGroupID;
				}
				set
				{
					_WorkGroupID = value;
				}
			}
			#endregion
			#region MyWorkGroup
			public abstract class myWorkGroup : PX.Data.BQL.BqlBool.Field<myWorkGroup> { }
			protected Boolean? _MyWorkGroup;
			[PXDefault(false)]
			[PXDBBool]
			[PXUIField(DisplayName = "My", Visibility = PXUIVisibility.Visible)]
			public virtual Boolean? MyWorkGroup
			{
				get
				{
					return _MyWorkGroup;
				}
				set
				{
					_MyWorkGroup = value;
				}
			}
			#endregion
			#region FilterSet
			public abstract class filterSet : PX.Data.BQL.BqlBool.Field<filterSet> { }
			[PXDefault(false)]
			[PXDBBool]
			public virtual Boolean? FilterSet
			{
				get
				{
					return
						this.OwnerID != null ||
						this.WorkGroupID != null ||
						this.MyWorkGroup == true;
				}
			}
			#endregion
			#region VendorID
			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
			protected Int32? _VendorID;
			[Vendor(
				typeof(Search<BAccountR.bAccountID, Where<True, Equal<True>>>), // TODO: remove fake Where after AC-101187
				CacheGlobal = true,
				Filterable = true)]
			[VerndorNonEmployeeOrOrganizationRestrictor]
			[PXRestrictor(
				typeof(Where<Vendor.vStatus, IsNull,
					Or<Vendor.vStatus, In3<VendorStatus.active, VendorStatus.oneTime, VendorStatus.holdPayments>>>),
				AP.Messages.VendorIsInStatus, typeof(Vendor.vStatus))]
			public virtual Int32? VendorID
			{
				get
				{
					return this._VendorID;
				}
				set
				{
					this._VendorID = value;
				}
			}
			#endregion
			#region SiteID
			public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
			protected Int32? _SiteID;
			[IN.Site(DisplayName = "Warehouse ID")]
			public virtual Int32? SiteID
			{
				get
				{
					return this._SiteID;
				}
				set
				{
					this._SiteID = value;
				}
			}
			#endregion
			#region SourceSiteID
			public abstract class sourceSiteID : PX.Data.BQL.BqlInt.Field<sourceSiteID> { }
			protected Int32? _SourceSiteID;
			[IN.Site(DisplayName = "Source Warehouse", DescriptionField = typeof(INSite.descr))]
			public virtual Int32? SourceSiteID
			{
				get
				{
					return this._SourceSiteID;
				}
				set
				{
					this._SourceSiteID = value;
				}
			}
			#endregion
			#region EndDate
			public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }
			protected DateTime? _EndDate;
			[PXDBDate()]
			[PXUIField(DisplayName = "Date Promised")]
			[PXDefault(typeof(AccessInfo.businessDate))]
			public virtual DateTime? EndDate
			{
				get
				{
					return this._EndDate;
				}
				set
				{
					this._EndDate = value;
				}
			}
			#endregion
			#region PurchDate
			public abstract class purchDate : PX.Data.BQL.BqlDateTime.Field<purchDate> { }
			protected DateTime? _PurchDate;
			[PXDBDate()]
			[PXUIField(DisplayName = "Creation Date")]
			[PXDefault(typeof(AccessInfo.businessDate))]
			public virtual DateTime? PurchDate
			{
				get
				{
					return this._PurchDate;
				}
				set
				{
					this._PurchDate = value;
				}
			}
			#endregion
			#region RequestedOnDate
			public abstract class requestedOnDate : PX.Data.BQL.BqlDateTime.Field<requestedOnDate> { }
			protected DateTime? _RequestedOnDate;
			[PXDBDate()]
			[PXUIField(DisplayName = "Requested On")]
			public virtual DateTime? RequestedOnDate
			{
				get
				{
					return this._RequestedOnDate;
				}
				set
				{
					this._RequestedOnDate = value;
				}
			}
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
			protected Int32? _CustomerID;
			[Customer()]
			public virtual Int32? CustomerID
			{
				get
				{
					return this._CustomerID;
				}
				set
				{
					this._CustomerID = value;
				}
			}
			#endregion
			#region InventoryID
			public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
			protected Int32? _InventoryID;
			[StockItem()]
			public virtual Int32? InventoryID
			{
				get
				{
					return this._InventoryID;
				}
				set
				{
					this._InventoryID = value;
				}
			}
			#endregion
			#region ItemClassCD
			public abstract class itemClassCD : PX.Data.BQL.BqlString.Field<itemClassCD> { }
			protected string _ItemClassCD;

			[PXDBString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "Item Class ID", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDimensionSelector(INItemClass.Dimension, typeof(INItemClass.itemClassCD), DescriptionField = typeof(INItemClass.descr), ValidComboRequired = true)]
			public virtual string ItemClassCD
			{
				get { return this._ItemClassCD; }
				set { this._ItemClassCD = value; }
			}
			#endregion
			#region ItemClassCDWildcard
			public abstract class itemClassCDWildcard : PX.Data.BQL.BqlString.Field<itemClassCDWildcard> { }
			[PXString(IsUnicode = true)]
			[PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
			[PXDimension(INItemClass.Dimension, ParentSelect = typeof(Select<INItemClass>), ParentValueField = typeof(INItemClass.itemClassCD))]
			public virtual string ItemClassCDWildcard
			{
				get { return ItemClassTree.MakeWildcard(ItemClassCD); }
				set { }
			}
			#endregion
			#region OrderType
			public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
			protected String _OrderType;
			[PXDBString(2, IsFixed = true, InputMask = ">aa")]
			[PXSelector(typeof(Search<SOOrderType.orderType, Where<SOOrderType.active, Equal<True>>>))]
			[PXUIField(DisplayName = "Order Type", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual String OrderType
			{
				get
				{
					return this._OrderType;
				}
				set
				{
					this._OrderType = value;
				}
			}
			#endregion
			#region OrderNbr
			public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
			protected String _OrderNbr;
			[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
			[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
			[SO.SO.RefNbr(typeof(Search2<SOOrder.orderNbr,
				LeftJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>,
						And<Where<Match<Customer, Current<AccessInfo.userName>>>>>>,
				Where<SOOrder.orderType, Equal<Optional<POCreateFilter.orderType>>,
				And<Where<SOOrder.orderType, Equal<SOOrderTypeConstants.transferOrder>,
				 Or<Customer.bAccountID, IsNotNull>>>>,
				 OrderBy<Desc<SOOrder.orderNbr>>>))]
			[PXFormula(typeof(Default<POCreateFilter.orderType>))]
			public virtual String OrderNbr
			{
				get
				{
					return this._OrderNbr;
				}
				set
				{
					this._OrderNbr = value;
				}
			}
			#endregion
			#region OrderWeight
			public abstract class orderWeight : PX.Data.BQL.BqlDecimal.Field<orderWeight> { }
			protected Decimal? _OrderWeight;
			[PXDBDecimal(6)]
			[PXUIField(DisplayName = "Weight", Enabled = false)]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual Decimal? OrderWeight
			{
				get
				{
					return this._OrderWeight;
				}
				set
				{
					this._OrderWeight = value;
				}
			}
			#endregion
			#region OrderVolume
			public abstract class orderVolume : PX.Data.BQL.BqlDecimal.Field<orderVolume> { }
			protected Decimal? _OrderVolume;
			[PXDBDecimal(6)]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Volume", Enabled = false)]
			public virtual Decimal? OrderVolume
			{
				get
				{
					return this._OrderVolume;
				}
				set
				{
					this._OrderVolume = value;
				}
			}
			#endregion
			#region OrderTotal
			public abstract class orderTotal : PX.Data.BQL.BqlDecimal.Field<orderTotal> { }
			protected Decimal? _OrderTotal;
			[PXDBDecimal(typeof(Search<Currency.decimalPlaces, Where<Currency.curyID, Equal<Selector<Current<POCreate.POCreateFilter.vendorID>, Vendor.curyID>>>>))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Total", Enabled = false)]
			public virtual Decimal? OrderTotal
			{
				get
				{
					return this._OrderTotal;
				}
				set
				{
					this._OrderTotal = value;
				}
			}
			#endregion

		}
	}
}
