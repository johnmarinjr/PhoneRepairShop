using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.AR;
using PX.Objects.EP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.TM;

namespace PX.Objects.AM
{
    public class ManufacturingDiagram : PXGraph<ManufacturingDiagram>
    {
        #region ViewsUsedInWebApi

        public SelectFrom<AMProdItem>.View AMProdItems;
        public SelectFrom<AMSchdItem>.View AMSchdItems;

        public SelectFrom<ProductionOrderResource>.View.ReadOnly ProductionOrderResourceView;
        public SelectFrom<ProductionOrderEvent>.View.ReadOnly ProductionOrderEventView;

        public SelectFrom<WorkCenterResource>.View.ReadOnly WorkCenterResourceView;
        public SelectFrom<WorkCenterEvent>.View.ReadOnly WorkCenterEventView;
		public SelectFrom<WorkCenterShiftCalendarResource>.View.ReadOnly WorkCenterShiftCalendarResourceView;

        public SelectFrom<MachineResource>.View.ReadOnly MachineResourceView;
        public SelectFrom<MachineEvent>.View.ReadOnly MachineEventView;
		public SelectFrom<MachineCalendarResource>.View.ReadOnly MachineCalendarResourceView;

        public SelectFrom<DiagramParameters>.View.ReadOnly GeneralParametersView;

        [PXHidden]
        public PXSetup<AMPSetup> ProductionSetup;

        #endregion

        public override bool IsDirty => false;

        public PXFilter<ManufacturingDiagramFilter> Filter;



		#region Buttons

		public PXAction<ManufacturingDiagramFilter> RoughCutSchedule;
		[PXUIField(DisplayName = "Schedule", MapEnableRights = PXCacheRights.Insert, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = true)]
		public IEnumerable roughCutSchedule(PXAdapter adapter)
		{
			RunRoughCutProcess(AMSchdItems.Cache.Cached.RowCast<AMSchdItem>().Where(r => r.Selected == true).ToList(), APSRoughCutProcessActions.Schedule);

			return adapter.Get();
		}

		public PXAction<ManufacturingDiagramFilter> RoughCutFirm;
		[PXUIField(DisplayName = "Firm", MapEnableRights = PXCacheRights.Insert, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = true)]
		public IEnumerable roughCutFirm(PXAdapter adapter)
		{
			RunRoughCutProcess(AMSchdItems.Cache.Cached.RowCast<AMSchdItem>().Where(r => r.Selected == true).ToList(), APSRoughCutProcessActions.Firm);

			return adapter.Get();
		}

		public PXAction<ManufacturingDiagramFilter> RoughCutUndoFirm;
		[PXUIField(DisplayName = "Undo Firm", MapEnableRights = PXCacheRights.Insert, MapViewRights = PXCacheRights.Select)]
		[PXButton(DisplayOnMainToolbar = true)]
		public IEnumerable roughCutUndoFirm(PXAdapter adapter)
		{
			RunRoughCutProcess(AMSchdItems.Cache.Cached.RowCast<AMSchdItem>().Where(r => r.Selected == true).ToList(), APSRoughCutProcessActions.UndoFirm);

			return adapter.Get();
		}

		protected virtual void RunRoughCutProcess(List<AMSchdItem> selectedOrders, string roughCutProcessAction)
		{
			if(selectedOrders == null || selectedOrders.Count == 0)
			{
				return;
			}

			PXLongOperation.StartOperation(this, delegate ()
			{
				APSRoughCutProcess.ProcessSchedule(selectedOrders, new APSRoughCutProcessFilter { ReleaseOrders = false, ProcessAction = roughCutProcessAction }, false);
			});
		}

		#endregion

		public static void RedirectTo(ManufacturingDiagramFilter filter)
		{
			var graph = CreateInstance<ManufacturingDiagram>();
			if(filter != null)
			{
				graph.Filter.Current = filter;
			}
			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.New);
		}

		protected virtual IEnumerable productionOrderResourceView()
        {
            ActualizeProductionOrderCaches();
            return ProductionOrderResourceView.Cache.Cached;
        }

        protected virtual IEnumerable productionOrderEventView()
        {
            ActualizeProductionOrderCaches();
            return ProductionOrderEventView.Cache.Cached;
        }

        protected virtual IEnumerable workCenterResourceView()
        {
            ActualizeWorkCenterCaches();
            return WorkCenterResourceView.Cache.Cached;
        }

        protected virtual IEnumerable workCenterEventView()
        {
            ActualizeWorkCenterCaches();
            return WorkCenterEventView.Cache.Cached;
        }

        protected virtual IEnumerable machineResourceView()
        {
            ActualizeMachineCaches();
            return MachineResourceView.Cache.Cached;
        }
		
        protected virtual IEnumerable machineEventView()
        {
            ActualizeMachineCaches();
            return MachineEventView.Cache.Cached;
        }

        protected virtual IEnumerable workCenterShiftCalendarResourceView()
        {
			ActualizeWorkCenterCaches();
            return WorkCenterShiftCalendarResourceView.Cache.Cached;
        }

		protected virtual IEnumerable machineCalendarResourceView()
        {
            ActualizeMachineCaches();
            return MachineCalendarResourceView.Cache.Cached;
        }

        protected virtual IEnumerable generalParametersView()
        {
			yield return new DiagramParameters
            {
                DisplayNonWorkingDays = Filter.Current.DisplayNonWorkingDays,
                StartDate = (Filter.Current.DateFrom ?? CalcMinDateFrom())?.Date,
                EndDate = (Filter.Current.DateTo ?? CalcMaxDateTo())?.Date.AddDays(1).AddSeconds(-1),
                ColorCodingOrders = Filter.Current.ColorCodingOrders,
                BlockSizeInMinutes = this.ProductionSetup?.Current?.SchdBlockSize ?? 30,
                WorkCentreCalendarType = Filter.Current.WorkCenterCalendarType
            };
        }

        private DateTime? CalcMinDateFrom()
        {
            var productionOrderEventMinDate = ProductionOrderEventView.Select().Select(i => i.GetItem<ProductionOrderEvent>().StartDate).Min();
            var workCenterEventMinDate = WorkCenterEventView.Select().Select(i => i.GetItem<WorkCenterEvent>().StartDate).Min();
            var machineEventMinDate = MachineEventView.Select().Select(i => i.GetItem<MachineEvent>().StartDate).Min();
            return new[] { productionOrderEventMinDate, workCenterEventMinDate, machineEventMinDate }.Min();
        }

        private DateTime? CalcMaxDateTo()
        {
            var productionOrderEventMaxDate = ProductionOrderEventView.Select().Select(i => i.GetItem<ProductionOrderEvent>().EndDate).Max();
            var workCenterEventMaxDate = WorkCenterEventView.Select().Select(i => i.GetItem<WorkCenterEvent>().EndDate).Max();
            var machineEventMaxDate = MachineEventView.Select().Select(i => i.GetItem<MachineEvent>().EndDate).Max();
            return new[] { productionOrderEventMaxDate, workCenterEventMaxDate, machineEventMaxDate }.Max();
        }

        private void ActualizeProductionOrderCaches(bool force = false)
        {
            var controlTimeStamps = ControlTimeStamps;
            if (force || controlTimeStamps.Production.NeedToRefillCache)
            {
                RefillProductionOrderResourceCache();
                RefillProductionOrderEventCache();
                controlTimeStamps.Production.UpdateLastGraphTimeStamp();
            }
        }

        private void ActualizeWorkCenterCaches(bool force = false)
        {
            var controlTimeStamps = ControlTimeStamps;
            if (force || controlTimeStamps.WorkCenter.NeedToRefillCache)
            {
                RefillWorkCenterResourceCache();
                RefillWorkCenterEventCache();
				RefillWorkCenterShiftCalendarResourceCache();
                controlTimeStamps.WorkCenter.UpdateLastGraphTimeStamp();
            }
        }

        private void ActualizeMachineCaches(bool force = false)
        {
            var controlTimeStamps = ControlTimeStamps;
            if (force || controlTimeStamps.Machine.NeedToRefillCache)
            {
                RefillMachineResourceCache();
                RefillMachineEventCache();
				RefillMachineCalendarResourceCache();
                controlTimeStamps.Machine.UpdateLastGraphTimeStamp();
            }
        }

        private void ActualizeAllCaches(bool force = false)
        {
            ActualizeProductionOrderCaches(force);
            ActualizeWorkCenterCaches(force);
            ActualizeMachineCaches(force);
        }

        private void RefillProductionOrderResourceCache()
        {
            ProductionOrderResourceView.Cache.Clear();
			var cacheRight = PXAccess.Provider.GetRights(ProductionOrderResourceView.Cache);
			// Using the "Edit" permission as the so called View Only option so we can still edit the filter to show results
			var isReadOnly = cacheRight != PXCacheRights.Delete && cacheRight != PXCacheRights.Insert;

            foreach (PXResult<AMSchdItem> record in
                SelectFrom<AMSchdItem>
                    .InnerJoin<AMProdItem>.On<AMSchdItem.FK.ProductionOrder>
                    .InnerJoin<AMProdTotal>.On<AMProdTotal.FK.ProductionOrder>
                    .InnerJoin<InventoryItem>.On<AMSchdItem.FK.InventoryItem>
                    .InnerJoin<AMOrderType>.On<AMSchdItem.FK.OrderType>
                    .LeftJoin<Customer>.On<AMProdItem.customerID.IsEqual<Customer.bAccountID>>
			        .InnerJoin<INSite>.On<AMSchdItem.FK.Site>
			        .LeftJoin<SOOrder>.On<AMProdItem.ordNbr.IsEqual<SOOrder.orderNbr>
				        .And<AMProdItem.ordTypeRef.IsEqual<SOOrder.orderType>>>
			        .LeftJoin<EPCompanyTree>.On<AMProdItem.productWorkgroupID.IsEqual<EPCompanyTree.workGroupID>>
			        .LeftJoin<Contact>.On<AMProdItem.productManagerID.IsEqual<Contact.contactID>>
			        .Where<
				        Brackets<ManufacturingDiagramFilter.siteID.FromCurrent.IsNull.Or<AMSchdItem.siteID.IsEqual<ManufacturingDiagramFilter.siteID.FromCurrent>>>
				        .And<Brackets<ManufacturingDiagramFilter.orderType.FromCurrent.IsNull.Or<AMSchdItem.orderType.IsEqual<ManufacturingDiagramFilter.orderType.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.prodOrdId.FromCurrent.IsNull.Or<AMSchdItem.prodOrdID.IsEqual<ManufacturingDiagramFilter.prodOrdId.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.inventoryID.FromCurrent.IsNull.Or<AMSchdItem.inventoryID.IsEqual<ManufacturingDiagramFilter.inventoryID.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.orderStatus.FromCurrent.IsNull.Or<AMProdItem.statusID.IsEqual<ManufacturingDiagramFilter.orderStatus.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateFrom.FromCurrent.IsNull.Or<AMSchdItem.startDate.IsGreaterEqual<ManufacturingDiagramFilter.dateFrom.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateTo.FromCurrent.IsNull.Or<AMSchdItem.endDate.IsLessEqual<ManufacturingDiagramFilter.dateTo.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soOrderType.FromCurrent.IsNull.Or<AMProdItem.ordTypeRef.IsEqual<ManufacturingDiagramFilter.soOrderType.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soNumber.FromCurrent.IsNull.Or<AMProdItem.ordNbr.IsEqual<ManufacturingDiagramFilter.soNumber.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.customerID.FromCurrent.IsNull.Or<AMProdItem.customerID.IsEqual<ManufacturingDiagramFilter.customerID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.scheduleStatus.FromCurrent.IsEqual<ManufacturingDiagramFilter.ScheduleStatusFilterListAttribute.bothStatus>
                            .Or<AMSchdItem.scheduleStatus.IsEqual<ManufacturingDiagramFilter.scheduleStatus.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.includeOnHold.FromCurrent.IsEqual<True>.Or<AMProdItem.hold.IsEqual<False>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent.IsNull.Or<AMProdItem.productWorkgroupID.IsEqual<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productManagerId.FromCurrent.IsNull.Or<AMProdItem.productManagerID.IsEqual<ManufacturingDiagramFilter.productManagerId.FromCurrent>>>>
                        .And<AMProdItem.function.IsNotEqual<OrderTypeFunction.planning>>
                        .And<AMSchdItem.scheduleStatus.IsNotEqual<ProductionScheduleStatus.unscheduled>>>
                    .View.Select(this))
            {
                var schdItem = record.GetItem<AMSchdItem>();
                var prodItem = record.GetItem<AMProdItem>();
                var orderType = record.GetItem<AMOrderType>();
                var invItem = record.GetItem<InventoryItem>();
                var cust = record.GetItem<Customer>();
                var site = record.GetItem<INSite>();
                var soOrder = record.GetItem<SOOrder>();
                var companyTree = record.GetItem<EPCompanyTree>();
                var contact = record.GetItem<Contact>();
                var item = new ProductionOrderResource
                {
                    Id = $"{schdItem.ProdOrdID}-{schdItem.OrderType}",
                    OrdType = schdItem.OrderType,
                    OrdNum = schdItem.ProdOrdID,
                    Descr = invItem.Descr,
                    InvId = invItem.InventoryCD,
                    Priority = schdItem.SchPriority,
                    Constraint = schdItem.ConstDate,
                    FirmSchedule = prodItem.FirmSchedule,
                    Selected = schdItem.Selected,
                    ScheduleStatus = schdItem.ScheduleStatus,
                    Hold = prodItem.Hold,
                    OrdTypeDescr = orderType.Descr,
                    OrdStatus = prodItem.StatusID,
                    CustomerId = cust.AcctCD,
                    CustomerName = cust.AcctName,
                    WorkgroupDsc = companyTree.Description,
                    ProductManager = contact.DisplayName,
                    StartDate = schdItem.StartDate,
                    EndDate = schdItem.EndDate,
                    QtyP = schdItem.QtytoProd,
                    QtyR = schdItem.QtyRemaining,
                    Uom = prodItem.UOM,
                    OrdDate = prodItem.ProdDate,
                    Warehouse = site.SiteCD,
                    SoOrderType = prodItem.OrdTypeRef,
                    SoOrderNumber = prodItem.OrdNbr,
                    RequestedOn = soOrder.RequestDate,
                    TotalCost = prodItem.WIPTotal,
					SchedulingMethod = schdItem.SchedulingMethod,
					ReadOnly = (isReadOnly || prodItem.StatusID != ProductionOrderStatus.Planned)
                };

                ProductionOrderResourceView.Cache.Hold(item);
            }
        }

        private void RefillProductionOrderEventCache()
        {
            ProductionOrderEventView.Cache.Clear();

	        var cnt = 1;
         	foreach (PXResult<AMSchdOper> record in
		        SelectFrom<AMSchdOper>
                    .InnerJoin<AMSchdItem>.On<AMSchdOper.FK.SchdItem>
			        .InnerJoin<AMProdItem>.On<AMSchdItem.FK.ProductionOrder>
			        .InnerJoin<AMProdOper>.On<AMSchdOper.FK.Operation>
			        .Where<
				        Brackets<ManufacturingDiagramFilter.siteID.FromCurrent.IsNull.Or<AMProdItem.siteID.IsEqual<ManufacturingDiagramFilter.siteID.FromCurrent>>>
				        .And<Brackets<ManufacturingDiagramFilter.orderType.FromCurrent.IsNull.Or<AMSchdItem.orderType.IsEqual<ManufacturingDiagramFilter.orderType.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.prodOrdId.FromCurrent.IsNull.Or<AMSchdItem.prodOrdID.IsEqual<ManufacturingDiagramFilter.prodOrdId.FromCurrent>>>>
						.And<Brackets<ManufacturingDiagramFilter.inventoryID.FromCurrent.IsNull.Or<AMSchdItem.inventoryID.IsEqual<ManufacturingDiagramFilter.inventoryID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.orderStatus.FromCurrent.IsNull.Or<AMProdItem.statusID.IsEqual<ManufacturingDiagramFilter.orderStatus.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateFrom.FromCurrent.IsNull.Or<AMSchdItem.startDate.IsGreaterEqual<ManufacturingDiagramFilter.dateFrom.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateTo.FromCurrent.IsNull.Or<AMSchdItem.endDate.IsLessEqual<ManufacturingDiagramFilter.dateTo.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soOrderType.FromCurrent.IsNull.Or<AMProdItem.ordTypeRef.IsEqual<ManufacturingDiagramFilter.soOrderType.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.soNumber.FromCurrent.IsNull.Or<AMProdItem.ordNbr.IsEqual<ManufacturingDiagramFilter.soNumber.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.customerID.FromCurrent.IsNull.Or<AMProdItem.customerID.IsEqual<ManufacturingDiagramFilter.customerID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.scheduleStatus.FromCurrent.IsEqual<ManufacturingDiagramFilter.ScheduleStatusFilterListAttribute.bothStatus>
                            .Or<AMSchdItem.scheduleStatus.IsEqual<ManufacturingDiagramFilter.scheduleStatus.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.includeOnHold.FromCurrent.IsEqual<True>.Or<AMProdItem.hold.IsEqual<False>>>>
				        .And<Brackets<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent.IsNull.Or<AMProdItem.productWorkgroupID.IsEqual<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.productManagerId.FromCurrent.IsNull.Or<AMProdItem.productManagerID.IsEqual<ManufacturingDiagramFilter.productManagerId.FromCurrent>>>>
                        .And<AMProdItem.function.IsNotEqual<OrderTypeFunction.planning>>
                        .And<AMSchdItem.scheduleStatus.IsNotEqual<ProductionScheduleStatus.unscheduled>>>
                    .View.Select(this))
	        {
		        var prodOrder = record.GetItem<AMProdOper>();
		        var prodItem = record.GetItem<AMProdItem>();
                var schdOper = record.GetItem<AMSchdOper>();
                var item = new ProductionOrderEvent
                {
                    Id = cnt.ToString(),
                    ResourceId = $"{prodOrder.ProdOrdID}-{prodOrder.OrderType}",
                    Name = prodOrder.OperationCD,
                    Outside = prodOrder.OutsideProcess,
                    StartDate = schdOper.StartDate,
                    EndDate = schdOper.MoveEndDate,
                    Descr = prodOrder.Descr,
                    LackOfMaterials = false
                };

                item.LackOfMaterials = CalculateLackOfMaterialsSign(prodOrder, prodItem);

                cnt++;
                ProductionOrderEventView.Cache.Hold(item);
            }
        }

        private bool? CalculateLackOfMaterialsSign(AMProdOper prodOrder, AMProdItem amProdItem)
        {
            // Note: see original algorithm in CriticalMaterialsInq.BuildSelectedProdMatl. When any QtyShort > 0 then LackOfMaterials=true for current prodOrder

            List<AMProdMatl> prevMatlList = new List<AMProdMatl>();

            foreach (PXResult<AMProdMatl> record in
                SelectFrom<AMProdMatl>
                    .InnerJoin<INSiteStatus>.On<AMProdMatl.inventoryID.IsEqual<INSiteStatus.inventoryID>
                        .And<AMProdMatl.subItemID.IsEqual<INSiteStatus.subItemID>>
                        .And<AMProdMatl.siteID.IsEqual<INSiteStatus.siteID>>>
                    .Where<AMProdMatl.orderType.IsEqual<@P.AsString>
                        .And<AMProdMatl.prodOrdID.IsEqual<@P.AsString>>
                        .And<AMProdMatl.operationID.IsEqual<@P.AsInt>>>
                    .View.Select(this, prodOrder.OrderType, prodOrder.ProdOrdID, prodOrder.OperationID))
            {
                var amProdMatl = record.GetItem<AMProdMatl>();
                var inSiteStatus = record.GetItem<INSiteStatus>();

                if (amProdMatl.IsStockItem.GetValueOrDefault() && !amProdMatl.IsByproduct.GetValueOrDefault() && amProdMatl.SubcontractSource != AMSubcontractSource.VendorSupplied)
                {
                    var multiplier = amProdItem.Function == OrderTypeFunction.Disassemble ? -1 : 1;
                    var uomConversion = amProdMatl.BaseTotalQtyRequired.GetValueOrDefault() == 0m
                        ? 1m
                        : amProdMatl.TotalQtyRequired.GetValueOrDefault() / amProdMatl.BaseTotalQtyRequired.GetValueOrDefault();
                    decimal? qtyRemaining = amProdMatl.QtyRemaining.GetValueOrDefault() * multiplier;
                    decimal qtyOnHand = (inSiteStatus?.QtyOnHand ?? 0m) * uomConversion;
                    var previousQty = GetPreviousMaterialQty(prevMatlList, amProdMatl.InventoryID, amProdMatl.SiteID, amProdMatl.SubItemID);
                    // Need to account for negative qty on hand
                    var adjustedQtyOnHand = Math.Max(qtyOnHand - previousQty, 0m);
                    if (adjustedQtyOnHand < qtyRemaining.GetValueOrDefault() && qtyRemaining.GetValueOrDefault() > 0)
                    {
                        return true;
                    }
                }

                prevMatlList.Add(amProdMatl);
            }

            return false;
        }

        private decimal GetPreviousMaterialQty(List<AMProdMatl> prevMatlList, int? inventoryId, int? siteId, int? subItemId)
        {
            var subItemEnabled = PXAccess.FeatureInstalled<FeaturesSet.subItem>();
            return prevMatlList
                .Where(prodMatl => prodMatl.InventoryID == inventoryId && prodMatl.SiteID == siteId && (prodMatl.SubItemID == subItemId || !subItemEnabled))
                .Sum(prodMatl => prodMatl.Qty.GetValueOrDefault());
        }

        private void RefillWorkCenterResourceCache()
        {
            WorkCenterResourceView.Cache.Clear();

	        foreach (PXResult<AMShift> record in
		        SelectFrom<AMShift>
			        .InnerJoin<AMWC>.On<AMShift.FK.WorkCenter>
			        .Where<Brackets<ManufacturingDiagramFilter.siteID.FromCurrent.IsNull.Or<AMWC.siteID.IsEqual<ManufacturingDiagramFilter.siteID.FromCurrent>>>>
		        .View.Select(this))
	        {
		        var shift = record.GetItem<AMShift>();
				var id = $"{shift.WcID}-{shift.ShiftCD}";
		        var item = new WorkCenterResource
		        {
			        Id = id,
					ShiftCode = id,
			        Code = shift.WcID,
			        Shift = shift.ShiftCD,
			        CrewSize = shift.CrewSize,
			        Machines = shift.MachNbr
		        };
		        WorkCenterResourceView.Cache.Hold(item);
	        }
        }

        private void RefillWorkCenterEventCache()
        {
            WorkCenterEventView.Cache.Clear();

            var cnt = 1;
            foreach (PXResult<AMWCSchdDetail> record in
                SelectFrom<AMWCSchdDetail>
                    .LeftJoin<AMSchdOperDetail>.On<AMWCSchdDetail.schdKey.IsEqual<AMSchdOperDetail.schdKey>>
                    .LeftJoin<AMProdOper>.On<AMSchdOperDetail.FK.Operation>
                    .LeftJoin<AMProdItem>.On<AMSchdOperDetail.FK.ProductionOrder>
                    .LeftJoin<AMSchdItem>.On<AMProdItem.orderType.IsEqual<AMSchdItem.orderType>
                        .And<AMProdItem.prodOrdID.IsEqual<AMSchdItem.prodOrdID>>>
			        .Where<
				        Brackets<ManufacturingDiagramFilter.siteID.FromCurrent.IsNull.Or<AMWCSchdDetail.siteID.IsEqual<ManufacturingDiagramFilter.siteID.FromCurrent>>>
				        .And<Brackets<ManufacturingDiagramFilter.orderType.FromCurrent.IsNull.Or<AMSchdOperDetail.orderType.IsEqual<ManufacturingDiagramFilter.orderType.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.prodOrdId.FromCurrent.IsNull.Or<AMSchdOperDetail.prodOrdID.IsEqual<ManufacturingDiagramFilter.prodOrdId.FromCurrent>>>>
						.And<Brackets<ManufacturingDiagramFilter.inventoryID.FromCurrent.IsNull.Or<AMSchdItem.inventoryID.IsEqual<ManufacturingDiagramFilter.inventoryID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.orderStatus.FromCurrent.IsNull.Or<AMProdOper.statusID.IsEqual<ManufacturingDiagramFilter.orderStatus.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.dateFrom.FromCurrent.IsNull.Or<AMWCSchdDetail.schdDate.IsGreaterEqual<ManufacturingDiagramFilter.dateFrom.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.dateTo.FromCurrent.IsNull.Or<AMWCSchdDetail.schdDate.IsLessEqual<ManufacturingDiagramFilter.dateTo.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soOrderType.FromCurrent.IsNull.Or<AMProdItem.ordTypeRef.IsEqual<ManufacturingDiagramFilter.soOrderType.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soNumber.FromCurrent.IsNull.Or<AMProdItem.ordNbr.IsEqual<ManufacturingDiagramFilter.soNumber.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.customerID.FromCurrent.IsNull.Or<AMProdItem.customerID.IsEqual<ManufacturingDiagramFilter.customerID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.scheduleStatus.FromCurrent.IsEqual<ManufacturingDiagramFilter.ScheduleStatusFilterListAttribute.bothStatus>
                            .Or<AMSchdItem.scheduleStatus.IsEqual<ManufacturingDiagramFilter.scheduleStatus.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.includeOnHold.FromCurrent.IsEqual<True>.Or<AMProdItem.hold.IsEqual<False>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent.IsNull.Or<AMProdItem.productWorkgroupID.IsEqual<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productManagerId.FromCurrent.IsNull.Or<AMProdItem.productManagerID.IsEqual<ManufacturingDiagramFilter.productManagerId.FromCurrent>>>>
                        .And<AMProdItem.function.IsNotEqual<OrderTypeFunction.planning>>
                        .And<AMSchdItem.scheduleStatus.IsNotEqual<ProductionScheduleStatus.unscheduled>>>
                    .View.Select(this))
            {
                var schdDetail = record.GetItem<AMWCSchdDetail>();
                var operDetail = record.GetItem<AMSchdOperDetail>();
                var prodOper = record.GetItem<AMProdOper>();

		        var item = new WorkCenterEvent
		        {
			        Id = cnt.ToString(),
			        ResourceId = $"{schdDetail.WcID}-{schdDetail.ShiftCD}",
			        Name = prodOper.OperationCD,
			        StartDate = schdDetail.SchdDate.GetValueOrDefault().Date + schdDetail.StartTime.GetValueOrDefault().TimeOfDay,
			        EndDate = schdDetail.SchdDate.GetValueOrDefault().Date + schdDetail.EndTime.GetValueOrDefault().TimeOfDay,
			        OrdNum = operDetail.ProdOrdID,
			        OrdRef = $"{operDetail.ProdOrdID}-{operDetail.OrderType}"
		        };
		        cnt++;
		        WorkCenterEventView.Cache.Hold(item);
	        }
        }

        private void RefillMachineResourceCache()
        {
            MachineResourceView.Cache.Clear();

            foreach (PXResult<AMMach> record in SelectFrom<AMMach>.View.Select(this))
            {
                var mach = record.GetItem<AMMach>();
                var item = new MachineResource
                {
                    Id = mach.MachID
                };
                MachineResourceView.Cache.Hold(item);
            }
        }

        private void RefillMachineEventCache()
        {
            MachineEventView.Cache.Clear();

            var cnt = 1;
            foreach (PXResult<AMMachSchdDetail> record in
                SelectFrom<AMMachSchdDetail>
                    .LeftJoin<AMSchdOperDetail>.On<AMMachSchdDetail.schdKey.IsEqual<AMSchdOperDetail.schdKey>>
                    .LeftJoin<AMProdOper>.On<AMSchdOperDetail.FK.Operation>
                    .LeftJoin<AMProdItem>.On<AMSchdOperDetail.FK.ProductionOrder>
                    .LeftJoin<AMSchdItem>.On<AMProdItem.orderType.IsEqual<AMSchdItem.orderType>
                        .And<AMProdItem.prodOrdID.IsEqual<AMSchdItem.prodOrdID>>>
                    .Where<
				        Brackets<ManufacturingDiagramFilter.siteID.FromCurrent.IsNull.Or<AMMachSchdDetail.siteID.IsEqual<ManufacturingDiagramFilter.siteID.FromCurrent>>>
				        .And<Brackets<ManufacturingDiagramFilter.orderType.FromCurrent.IsNull.Or<AMSchdOperDetail.orderType.IsEqual<ManufacturingDiagramFilter.orderType.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.prodOrdId.FromCurrent.IsNull.Or<AMSchdOperDetail.prodOrdID.IsEqual<ManufacturingDiagramFilter.prodOrdId.FromCurrent>>>>
						.And<Brackets<ManufacturingDiagramFilter.inventoryID.FromCurrent.IsNull.Or<AMSchdItem.inventoryID.IsEqual<ManufacturingDiagramFilter.inventoryID.FromCurrent>>>>
						.And<Brackets<ManufacturingDiagramFilter.orderStatus.FromCurrent.IsNull.Or<AMProdOper.statusID.IsEqual<ManufacturingDiagramFilter.orderStatus.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateFrom.FromCurrent.IsNull.Or<AMMachSchdDetail.schdDate.IsGreaterEqual<ManufacturingDiagramFilter.dateFrom.FromCurrent>>>>
				        .And<Brackets<ManufacturingDiagramFilter.dateTo.FromCurrent.IsNull.Or<AMMachSchdDetail.schdDate.IsLessEqual<ManufacturingDiagramFilter.dateTo.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soOrderType.FromCurrent.IsNull.Or<AMProdItem.ordTypeRef.IsEqual<ManufacturingDiagramFilter.soOrderType.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.soNumber.FromCurrent.IsNull.Or<AMProdItem.ordNbr.IsEqual<ManufacturingDiagramFilter.soNumber.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.customerID.FromCurrent.IsNull.Or<AMProdItem.customerID.IsEqual<ManufacturingDiagramFilter.customerID.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.scheduleStatus.FromCurrent.IsEqual<ManufacturingDiagramFilter.ScheduleStatusFilterListAttribute.bothStatus>
                            .Or<AMSchdItem.scheduleStatus.IsEqual<ManufacturingDiagramFilter.scheduleStatus.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.includeOnHold.FromCurrent.IsEqual<True>.Or<AMProdItem.hold.IsEqual<False>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent.IsNull.Or<AMProdItem.productWorkgroupID.IsEqual<ManufacturingDiagramFilter.productWorkgroupId.FromCurrent>>>>
                        .And<Brackets<ManufacturingDiagramFilter.productManagerId.FromCurrent.IsNull.Or<AMProdItem.productManagerID.IsEqual<ManufacturingDiagramFilter.productManagerId.FromCurrent>>>>
                        .And<AMProdItem.function.IsNotEqual<OrderTypeFunction.planning>>
                        .And<AMSchdItem.scheduleStatus.IsNotEqual<ProductionScheduleStatus.unscheduled>>>
                    .View.Select(this))
            {
                var schdDetail = record.GetItem<AMMachSchdDetail>();
                var operDetail = record.GetItem<AMSchdOperDetail>();
                var prodOper = record.GetItem<AMProdOper>();
                var item = new MachineEvent
                {
                    Id = cnt.ToString(),
                    ResourceId = schdDetail.ResourceID,
                    Name = prodOper.OperationCD,
                    StartDate = schdDetail.SchdDate.GetValueOrDefault().Date + schdDetail.StartTime.GetValueOrDefault().TimeOfDay,
                    EndDate = schdDetail.SchdDate.GetValueOrDefault().Date + schdDetail.EndTime.GetValueOrDefault().TimeOfDay,
                    OrdNum = operDetail.ProdOrdID,
                    OrdRef = $"{operDetail.ProdOrdID}-{operDetail.OrderType}"
                };
                cnt++;
                MachineEventView.Cache.Hold(item);
            }
        }

        private void RefillWorkCenterShiftCalendarResourceCache()
        {
            WorkCenterShiftCalendarResourceView.Cache.Clear();

			foreach (PXResult<AMShift> record in
				SelectFrom<AMShift>
				.InnerJoin<CSCalendar>.On<AMShift.FK.WorkCalendar>
				.View.Select(this))
			{
				var shift = record.GetItem<AMShift>();
				var calendar = record.GetItem<CSCalendar>();

				var calendarIntervals = BuildCalendarInterval(calendar);
				if(calendarIntervals == null)
				{
					continue;
				}

				var item = new WorkCenterShiftCalendarResource
				{
					Id = $"{shift.WcID}-{shift.ShiftCD}",
					Name = string.Empty,
					UnspecifiedTimeIsWorking = false,
					Intervals = calendarIntervals.ToArray()
				};
				WorkCenterShiftCalendarResourceView.Cache.Hold(item);
			}
        }

		private void RefillMachineCalendarResourceCache()
        {
            MachineCalendarResourceView.Cache.Clear();

			foreach (PXResult<AMMach> record in
				SelectFrom<AMMach>
				.InnerJoin<CSCalendar>.On<AMMach.FK.MachineCalendar>
				.View.Select(this))
			{
				var machine = record.GetItem<AMMach>();
				var calendar = record.GetItem<CSCalendar>();

				var calendarIntervals = BuildCalendarInterval(calendar);
				if(calendarIntervals == null)
				{
					continue;
				}

				var item = new MachineCalendarResource
				{
					Id = machine.MachID,
					Name = machine.Descr,
					UnspecifiedTimeIsWorking = false,
					Intervals = calendarIntervals.ToArray()
				};
				MachineCalendarResourceView.Cache.Hold(item);
			}
        }

		// Example output: "on Mon at 10:00"
		private string FormatCalendarIntervalDateString(DayOfWeek dayOfWeek, DateTime? date)
		{
			return $"on {dayOfWeek.ToShortString()} at {date.To24HourString()}";
		}

		protected virtual IEnumerable<CalendarInterval> BuildCalendarInterval(CSCalendar calendar)
		{
			var breakTimes = SelectFrom<AMCalendarBreakTime>
					.Where<AMCalendarBreakTime.calendarID.IsEqual<@P.AsString>>
					.View.Select(this, calendar?.CalendarID).ToFirstTable<AMCalendarBreakTime>().ToList();

			return BuildCalendarInterval(calendar, breakTimes);
		}

		protected virtual IEnumerable<CalendarInterval> BuildCalendarInterval(CSCalendar calendar, List<AMCalendarBreakTime> breakTimes)
		{
			if(calendar?.CalendarID == null)
			{
				yield break;
			}

			foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
			{
				var workTimes = CalendarHelper.GetGenericWorkingTimes(dayOfWeek, calendar, breakTimes);
				if(workTimes == null)
				{
					continue;
				}

				foreach (var workTime in workTimes)
				{
					yield return BuildCalendarInterval(calendar, workTime, dayOfWeek);
				}
			}

			yield break;
		}

		private CalendarInterval BuildCalendarInterval(CSCalendar calendar, DateRange dateRange, DayOfWeek dayOfWeek)
		{
			// could be overnight
			var endDateDayOfWeek = DateTime.Compare(dateRange.StartDate.Date, dateRange.EndDate.Date) == 0
				? dayOfWeek
				: dayOfWeek.NextDay();

			return new CalendarInterval
			{
				RecurrentStartDate = FormatCalendarIntervalDateString(dayOfWeek, dateRange.StartDate),
				RecurrentEndDate = FormatCalendarIntervalDateString(endDateDayOfWeek, dateRange.EndDate),
				IsWorking = true
			};
		}

		[PXHidden]
        public class ManufacturingDiagramFilter : IBqlTable
        {
			#region InventoryID
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

        [StockItem]
        public virtual Int32? InventoryID { get; set; }

        #endregion

            #region Warehouse
            [IN.Site(DisplayName = "Warehouse", DescriptionField = typeof(INSite.descr))]
            public virtual int? SiteID { get; set; }
	        public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID>
	        { }
            #endregion

            #region OrderType
            [AMOrderTypeField]
            [AMOrderTypeSelector]
            public virtual string OrderType { get; set; }
            public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType>
            { }
            #endregion

            #region ProdOrdId
            [ProductionNbr(DisplayName = "Production Nbr.")]
            [ProductionOrderSelector(typeof(ManufacturingDiagramFilter.orderType), true)]
            public virtual string ProdOrdId { get; set; }
            public abstract class prodOrdId : PX.Data.BQL.BqlString.Field<prodOrdId>
            { }
            #endregion

            #region OrderStatus
            [PXUIField(DisplayName = "Production Order Status")]
            [PXString(1, IsFixed = true)]
            [ProductionOrderStatus.ListAll]
            public virtual string OrderStatus { get; set; }
            public abstract class orderStatus : PX.Data.BQL.BqlString.Field<orderStatus>
            { }
            #endregion

            #region DateFrom
            [PXDate()]
            [PXUIField(DisplayName = "From")]
            public virtual DateTime? DateFrom { get; set; }
            public abstract class dateFrom : PX.Data.BQL.BqlDateTime.Field<dateFrom>
            { }
            #endregion

            #region DateTo
            [PXDate()]
            [PXUIField(DisplayName = "To")]
            public virtual DateTime? DateTo { get; set; }
            public abstract class dateTo : PX.Data.BQL.BqlDateTime.Field<dateTo>
            { }
            #endregion

            #region OrderType
            [PXString]
            [PXUIField(DisplayName = "Sales Order Type")]
            [PXSelector(typeof(Search5<SOOrderType.orderType,
                InnerJoin<SOOrderTypeOperation, On2<SOOrderTypeOperation.FK.OrderType, And<SOOrderTypeOperation.operation, Equal<SOOrderType.defaultOperation>>>,
                LeftJoin<SOSetupApproval, On<SOOrderType.orderType, Equal<SOSetupApproval.orderType>>>>,
                Aggregate<GroupBy<SOOrderType.orderType>>>))]
            [PXRestrictor(typeof(Where<SOOrderTypeOperation.iNDocType, NotEqual<INTranType.transfer>, Or<FeatureInstalled<FeaturesSet.warehouse>>>), ErrorMessages.ElementDoesntExist, typeof(SOOrderType.orderType))]
            [PXRestrictor(typeof(Where<SOOrderType.requireAllocation, NotEqual<True>, Or<AllocationAllowed>>), ErrorMessages.ElementDoesntExist, typeof(SOOrderType.orderType))]
            [PXRestrictor(typeof(Where<SOOrderType.active,Equal<True>>), null)]
            public virtual string SoOrderType { get; set; }
            public abstract class soOrderType : PX.Data.BQL.BqlString.Field<soOrderType>
            { }
            #endregion

            #region SO number
            [PXString(15, IsUnicode = true, InputMask = "")]
            [PXUIField(DisplayName = "Sales Order Nbr.")]
            [PXSelector(typeof(SearchFor<SOOrder.orderNbr>.Where<SOOrder.orderType.IsEqual<soOrderType.FromCurrent>>),
	            typeof(SOOrder.orderNbr),
	            typeof(SOOrder.orderDesc),
	            typeof(SOOrder.status),
	            DescriptionField = typeof(SOOrder.orderDesc))]
            public virtual string SoNumber { get; set; }
            public abstract class soNumber : PX.Data.BQL.BqlString.Field<soNumber>
            { }
            #endregion

            #region CustomerID
            [PXUIField(DisplayName = "Customer")]
            [Customer(DescriptionField = typeof(Customer.acctName))]
            public virtual int? CustomerID { get; set; }
            public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
            #endregion

            #region DisplayNonWorkingDays
            [PXBool()]
            [PXUnboundDefault(false)]
            [PXUIField(DisplayName = "Display Non-Working Days")]
            public virtual bool? DisplayNonWorkingDays { get; set; }
            public abstract class displayNonWorkingDays : PX.Data.BQL.BqlBool.Field<displayNonWorkingDays>
            { }
            #endregion

            #region ColorCodingOrders
            [PXUIField(DisplayName = "Color Coding")]
            [PXString(2, IsFixed = true)]
            [PXUnboundDefault(ColorCodingForOrders.Status)]
            [ColorCodingForOrders.List]
            public virtual string ColorCodingOrders { get; set; }
            public abstract class colorCodingOrders : PX.Data.BQL.BqlString.Field<colorCodingOrders>
            { }
            #endregion

            #region ScheduleStatus
            [PXUIField(DisplayName = "Schedule Status")]	
            [PXString]
            [ScheduleStatusFilterList]
            [PXUnboundDefault(ScheduleStatusFilterListAttribute.BothStatus)]
            public virtual string ScheduleStatus { get; set; }
            public abstract class scheduleStatus : PX.Data.BQL.BqlString.Field<scheduleStatus>
            { }

            public class ScheduleStatusFilterListAttribute : PXStringListAttribute
            {
                public ScheduleStatusFilterListAttribute()
                    : base(
                        new string[] { 
                            BothStatus,
                            ProductionScheduleStatus.Scheduled, 
                            ProductionScheduleStatus.Firm},
                        new string[] {
                            PX.Objects.FA.Messages.BothType,
                            Messages.Scheduled,
                            Messages.Firm}) 
                {
                }

                public const string BothStatus = "B";

                public class bothStatus : PX.Data.BQL.BqlString.Constant<bothStatus>
                {
                    public bothStatus() : base(BothStatus) { }
                }
            }

            #endregion

            #region IncludeOnHold
            [PXBool()]
            [PXUnboundDefault(false)]
            [PXUIField(DisplayName = "Include Orders on Hold")]
            public virtual bool? IncludeOnHold { get; set; }
            public abstract class includeOnHold : PX.Data.BQL.BqlBool.Field<includeOnHold>
            { }
            #endregion

            #region ProductWorkgroupId
            [PXInt()]
            [PXUIField(DisplayName = "Product Workgroup")]
            [PXWorkgroupSelector]
            public virtual int? ProductWorkgroupId { get; set; }
            public abstract class productWorkgroupId : PX.Data.BQL.BqlInt.Field<productWorkgroupId>
            { }
            #endregion

            #region ProductManagerId
            [Owner(typeof(ManufacturingDiagramFilter.productWorkgroupId), DisplayName = "Product Manager")]
            public virtual int? ProductManagerId { get; set; }
            public abstract class productManagerId : PX.Data.BQL.BqlInt.Field<productManagerId>
            { }
            #endregion

            #region WorkCenterCalendarType

		    /// <summary>
		    /// Defines how the Histogram data is displayed
		    /// </summary>
            [PXString]
            [PXUnboundDefault("ByShifts")]
            [PXUIField(DisplayName = "Work Center Calendar Type")]
            [PXStringList(
                new string[] {"ByShifts", "Common"}, 
                new string[] {"By Shifts", "Common"})]
		    public virtual string WorkCenterCalendarType { get; set; }

		    public abstract class workCenterCalendarType : Data.BQL.BqlString.Field<workCenterCalendarType>	{	}

		    #endregion
        }

        protected virtual void _(Events.RowUpdated<ManufacturingDiagramFilter> e)
        {
	        if (e.Row == null || e.OldRow == null) return;

	        if (!e.Cache.ObjectsEqual<
		            ManufacturingDiagramFilter.siteID,
		            ManufacturingDiagramFilter.orderType,
		            ManufacturingDiagramFilter.prodOrdId,
		            ManufacturingDiagramFilter.orderStatus,
		            //ManufacturingDiagramFilter.weekId,
		            ManufacturingDiagramFilter.dateFrom,
		            ManufacturingDiagramFilter.dateTo,
		            ManufacturingDiagramFilter.soNumber,
		            ManufacturingDiagramFilter.customerID>(e.Row, e.OldRow)
	            || !e.Cache.ObjectsEqual<
		            ManufacturingDiagramFilter.scheduleStatus,
		            ManufacturingDiagramFilter.includeOnHold,
		            ManufacturingDiagramFilter.scheduleStatus>(e.Row, e.OldRow))
	        {
		        ActualizeAllCaches(true);
	        }
        }

        protected virtual void _(Events.RowInserted<ManufacturingDiagramFilter> e)
        {
            Filter.Current.DateFrom = Accessinfo.BusinessDate;
            Filter.Current.DateTo = Accessinfo.BusinessDate?.AddDays(7);
        }

        protected virtual void _(Events.FieldVerifying<ManufacturingDiagramFilter, ManufacturingDiagramFilter.dateFrom> e)
        {
	        if (e.Row == null || e.NewValue == null || e.Row.DateTo == null)
		        return;
            
	        if ((DateTime)e.NewValue > e.Row.DateTo)
		        throw new PXSetPropertyException(Messages.DateToMustBeGreaterThanDateFrom, PXErrorLevel.Error);
        }

        protected virtual void _(Events.FieldVerifying<ManufacturingDiagramFilter, ManufacturingDiagramFilter.dateTo> e)
        {
	        if (e.Row == null || e.NewValue == null || e.Row.DateFrom == null)
		        return;

	        if ((DateTime)e.NewValue < e.Row.DateFrom)
		        throw new PXSetPropertyException(Messages.DateToMustBeGreaterThanDateFrom, PXErrorLevel.Error);
        }

        #region Aging strategy
        private bool _timestampSelected;

        private DiagramDataAgingParams ControlTimeStamps
        {
            get
            {
                if (!_timestampSelected)
                {
                    PXDatabase.SelectTimeStamp();
                    _timestampSelected = true;
                }

                DiagramDataAgingParams result = PXContext.GetSlot<DiagramDataAgingParams>();
                if (result == null)
                {
                    var productionTimeStamps = PXDatabase.GetSlot<Definition>($"{nameof(ManufacturingDiagram)}${nameof(DiagramDataAgingParams.Production)}$ControlTimeStampDefinition",
                        new[] {typeof(AMSchdItem), typeof(AMProdItem), typeof(InventoryItem), typeof(Customer), typeof(INSite), typeof(AMProdOper)});
                    var workCenterTimeStamps = PXDatabase.GetSlot<Definition>($"{nameof(ManufacturingDiagram)}${nameof(DiagramDataAgingParams.WorkCenter)}$ControlTimeStampDefinition",
                        new[] { typeof(AMShift), typeof(AMWCSchdDetail), typeof(AMProdOper) });
                    var machineTimeStamps = PXDatabase.GetSlot<Definition>($"{nameof(ManufacturingDiagram)}${nameof(DiagramDataAgingParams.Machine)}$ControlTimeStampDefinition",
                        new[] { typeof(AMMach), typeof(AMMachSchdDetail), typeof(AMSchdOperDetail), typeof(AMProdOper) });
                    var toolTimeStamps = PXDatabase.GetSlot<Definition>($"{nameof(ManufacturingDiagram)}${nameof(DiagramDataAgingParams.Tool)}$ControlTimeStampDefinition",
                        new[] { typeof(AMToolMst), typeof(AMToolSchdDetail), typeof(AMSchdOperDetail), typeof(AMProdOper) });

                    result = new DiagramDataAgingParams
                    {
	                    Production = new DataAgingParams {DbTimeStampDefinition = productionTimeStamps},
	                    WorkCenter = new DataAgingParams {DbTimeStampDefinition = workCenterTimeStamps},
	                    Machine = new DataAgingParams {DbTimeStampDefinition = machineTimeStamps},
	                    Tool = new DataAgingParams {DbTimeStampDefinition = toolTimeStamps}
                    };
                    PXContext.SetSlot(result);
                }
                return result;
            }
        }

        public class Definition : IPrefetchable
        {
            public string DbTimeStamp { get; private set; }

            public void Prefetch()
            {
                DbTimeStamp = System.Text.Encoding.Default.GetString(PXDatabase.Provider.SelectTimeStamp());
            }
	    }

        public class DiagramDataAgingParams
        {
            public DataAgingParams Production { get; set; }
            public DataAgingParams WorkCenter { get; set; }
            public DataAgingParams Machine { get; set; }
            public DataAgingParams Tool { get; set; }
        }

        public class DataAgingParams
        {
	        public Definition DbTimeStampDefinition { get; set; }
	        public string LastGraphTimeStamp { get; set; }

	        public bool NeedToRefillCache => LastGraphTimeStamp == null || LastGraphTimeStamp != DbTimeStampDefinition.DbTimeStamp;

	        public void UpdateLastGraphTimeStamp()
	        {
		        LastGraphTimeStamp = DbTimeStampDefinition.DbTimeStamp;
	        }
        }
        #endregion
    }
}
