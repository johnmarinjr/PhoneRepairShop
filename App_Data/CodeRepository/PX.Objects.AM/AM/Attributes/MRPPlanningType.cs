using PX.Data;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// MRP Types
    /// </summary>
    public class MRPPlanningType
    {
        public const int Unknown = 0;

        /// <summary>
        /// Sales Order = SO (10)
        /// </summary>
        public const int SalesOrder = 10;

        /// <summary>
        /// SO Shipment
        /// </summary>
        public const int Shipment = 12;

        /// <summary>
        /// Purchase Order = PU (15)
        /// </summary>
        public const int PurchaseOrder = 15;

        /// <summary>
        /// Forecast Demand = FC (20)
        /// </summary>
        public const int ForecastDemand = 20;

        /// <summary>
        /// Production Order = PI (25)
        /// </summary>
        public const int ProductionOrder = 25;

        /// <summary>
        /// Production Material = PM (30)
        /// </summary>
        public const int ProductionMaterial = 30;

        /// <summary>
        /// Stock Adjustment = IN (35)
        /// </summary>
        public const int StockAdjustment = 35;

        /// <summary>
        /// Safety Stock = SS (40)
        /// </summary>
        public const int SafetyStock = 40;
		// treated same as SafetyStock but just for difference in display
		public const int ReorderPoint = 41;

        /// <summary>
        /// MPS = MS (45)
        /// </summary>
        public const int MPS = 45;

        /// <summary>
        /// MRP Plan = MP (50)
        /// </summary>
        public const int MrpPlan = 50;

        /// <summary>
        /// MRP Requirement (Blowdown) = BD (55)
        /// </summary>
        public const int MrpRequirement = 55;

        /// <summary>
        /// MRP SO Transfer Demand
        /// </summary>
        public const int TransferDemand = 60;

        /// <summary>
        /// MRP SO Transfer Supply
        /// </summary>
        public const int TransferSupply = 65;

        /// <summary>
        /// MRP KIT Demand
        /// </summary>
        public const int AssemblyDemand = 66;

        /// <summary>
        /// MRP KIT Supply
        /// </summary>
        public const int AssemblySupply = 67;

        /// <summary>
        /// MRP Generic Inventory Demand
        /// </summary>
        public const int InventoryDemand = 68;

        /// <summary>
        /// MRP Generic Inventory Supply
        /// </summary>
        public const int InventorySupply = 69;

        /// <summary>
        /// MRP Field Service
        /// </summary>
        public const int FieldService = 70;

		/// <summary>
		/// Consolidated plan
		/// </summary>
		public const int Consolidated = 75;

        public static string GetDescription(int? id)
        {
            if (id == null)
            {
                return Messages.GetLocal(Messages.Unknown);
            }

            try
            {
                return new ListAttribute().ValueLabelDic[id.GetValueOrDefault()];
            }
            catch
            {
                return Messages.GetLocal(Messages.Unknown);
            }
        }

        //BQL constants declaration
        public class salesOrder : PX.Data.BQL.BqlInt.Constant<salesOrder>
        {
            public salesOrder() : base(SalesOrder) {}
        }
        public class shipment : PX.Data.BQL.BqlInt.Constant<shipment>
        {
            public shipment() : base(Shipment) { }
        }
        public class purchaseOrder : PX.Data.BQL.BqlInt.Constant<purchaseOrder>
        {
            public purchaseOrder() : base(PurchaseOrder) {}
        }
        public class forecastDemand : PX.Data.BQL.BqlInt.Constant<forecastDemand>
        {
            public forecastDemand() : base(ForecastDemand) {}
        }
        public class productionOrder : PX.Data.BQL.BqlInt.Constant<productionOrder>
        {
            public productionOrder() : base(ProductionOrder) {}
        }
        public class productionMaterial : PX.Data.BQL.BqlInt.Constant<productionMaterial>
        {
            public productionMaterial() : base(ProductionMaterial) {}
        }
        public class stockAdjustment : PX.Data.BQL.BqlInt.Constant<stockAdjustment>
        {
            public stockAdjustment() : base(StockAdjustment) {}
        }
        public class safetyStock : PX.Data.BQL.BqlInt.Constant<safetyStock>
        {
            public safetyStock() : base(SafetyStock) {}
        }
		public class reorderPoint : PX.Data.BQL.BqlInt.Constant<reorderPoint>
        {
            public reorderPoint() : base(ReorderPoint) {}
        }
        public class mps : PX.Data.BQL.BqlInt.Constant<mps>
        {
            public mps() : base(MPS) {}
        }
        public class mrpPlan : PX.Data.BQL.BqlInt.Constant<mrpPlan>
        {
            public mrpPlan() : base(MrpPlan) {}
        }
        public class mrpRequirement : PX.Data.BQL.BqlInt.Constant<mrpRequirement>
        {
            public mrpRequirement() : base(MrpRequirement) {}
        }
        public class transferDemand : PX.Data.BQL.BqlInt.Constant<transferDemand>
        {
            public transferDemand() : base(TransferDemand) {}
        }
        public class transferSupply : PX.Data.BQL.BqlInt.Constant<transferSupply>
        {
            public transferSupply() : base(TransferSupply) {}
        }
        public class assemblyDemand : PX.Data.BQL.BqlInt.Constant<assemblyDemand>
        {
            public assemblyDemand() : base(AssemblyDemand) { }
        }
        public class assemblySupply : PX.Data.BQL.BqlInt.Constant<assemblySupply>
        {
            public assemblySupply() : base(AssemblySupply) { }
        }
        public class inventoryDemand : PX.Data.BQL.BqlInt.Constant<inventoryDemand>
        {
            public inventoryDemand() : base(InventoryDemand) { }
        }
        public class inventorySupply : PX.Data.BQL.BqlInt.Constant<inventorySupply>
        {
            public inventorySupply() : base(InventorySupply) { }
        }
        public class fieldService : PX.Data.BQL.BqlInt.Constant<fieldService>
        {
            public fieldService() : base(FieldService) { }
        }
		public class consolidated : PX.Data.BQL.BqlInt.Constant<consolidated>
        {
            public consolidated() : base(Consolidated) { }
        }

        public class ListAttribute : PXIntListAttribute
        {
            public ListAttribute()
                : base(
                    new[]
                    {
                        Unknown,
                        SalesOrder,
                        Shipment,
                        PurchaseOrder,
                        ForecastDemand,
                        ProductionOrder,
                        ProductionMaterial,
                        StockAdjustment,
                        SafetyStock,
						ReorderPoint,
                        MPS,
                        MrpPlan,
                        MrpRequirement,
                        TransferDemand,
                        TransferSupply,
                        AssemblyDemand,
                        AssemblySupply,
                        InventoryDemand,
                        InventorySupply,
                        FieldService,
						Consolidated
                    },
                    new[]
                    {
                        Messages.Unknown,
                        Messages.SalesOrder,
                        Messages.Shipment,
                        Messages.PurchaseOrder,
                        Messages.Forecast,
                        Messages.ProductionOrder,
                        Messages.ProductionMaterial,
                        Messages.StockAdjustment,
                        Messages.StockingMethodSafetyStock,
						Messages.StockingMethodReorderPoint,
                        Messages.MPS,
                        Messages.MRPPlan,
                        Messages.MrpRequirement,
                        Messages.TransferDemand,
                        Messages.TransferSupply,
                        Messages.AssemblyDemand,
                        Messages.AssemblySupply,
                        Messages.InventoryDemand,
                        Messages.InventorySupply,
                        Messages.FieldService,
						Messages.Consolidated
                    })
            {
            }
        }
    }
}
