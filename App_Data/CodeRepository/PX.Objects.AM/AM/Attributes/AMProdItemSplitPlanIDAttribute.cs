using PX.Data;
using System;
using PX.Objects.IN;
using System.Linq;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Handles the Allocation of Production orders - The manufactured item
    /// </summary>
    public class AMProdItemSplitPlanIDAttribute : INItemPlanIDAttribute
    {
        #region Ctor
        public AMProdItemSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry)
            : base(ParentNoteID, ParentHoldEntry)
        {
        }
        #endregion

        #region Implementation

        protected virtual string GetPlanType(AMProdItem parent, bool hold)
        {
            // Determine the Plan Type based on Parent Status and the Supply type
            if (parent == null || parent.StatusID == ProductionOrderStatus.Cancel || parent.StatusID == ProductionOrderStatus.Closed
				|| parent.StatusID == ProductionOrderStatus.Completed)
            {
                return null;
            }

            if (parent.Function == OrderTypeFunction.Disassemble)
            {
                return hold || parent.StatusID == ProductionOrderStatus.Planned ? INPlanConstants.PlanM5 : INPlanConstants.PlanM6;
            }

            switch (parent.SupplyType)
            {
                case ProductionSupplyType.Inventory:
                    return hold || parent.StatusID == ProductionOrderStatus.Planned ? INPlanConstants.PlanM1 : INPlanConstants.PlanM2;
                case ProductionSupplyType.Production:
                    return hold || parent.StatusID == ProductionOrderStatus.Planned ? INPlanConstants.PlanMB : INPlanConstants.PlanMC;
                case ProductionSupplyType.SalesOrder:
                    return hold || parent.StatusID == ProductionOrderStatus.Planned ? INPlanConstants.PlanMD : INPlanConstants.PlanME;
            }

            return null;
        }

        protected virtual AMProdItem GetParentProdItem(PXCache sender, AMProdItemSplit split)
        {
            return (AMProdItem)PXParentAttribute.SelectParent(sender, split, typeof(AMProdItem));
        }

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan planRow, object origRow)
		{
			var splitRow = (AMProdItemSplit)origRow;
			if(!string.IsNullOrWhiteSpace(splitRow.LotSerialNbr) && InventoryHelper.IsLotSerialTempAssigned(splitRow))
			{
				// Wait for split persisting to correctly set the defaulting LotSerialNbr
				return null;
			}

			planRow.InventoryID = splitRow.InventoryID;
			planRow.SubItemID = splitRow.SubItemID;
			planRow.SiteID = splitRow.SiteID;
			planRow.LocationID = splitRow.LocationID;
			planRow.LotSerialNbr = splitRow.LotSerialNbr;
			planRow.UOM = splitRow.UOM;
			// Covers late evaluation to QtyRemaining which impacts allocations
			var qtyRemaining = PXFormulaAttribute.Evaluate<AMProdItemSplit.baseQtyRemaining>(sender, splitRow) as decimal?;
			planRow.PlanQty = qtyRemaining;

			if(splitRow.StatusID == ProductionOrderStatus.Closed || splitRow.StatusID == ProductionOrderStatus.Cancel)
			{
				planRow.PlanQty = 0m;
			}

			var parent = GetParentProdItem(sender, splitRow);
			if(parent == null)
			{
				// During insert persist of lot/serial tracked items with preassigned true the AMProdItem will have the ProductionNbr set where the split does not yet have
				//	it set so we need to grab in cache the best we can for that correct item
				parent = (AMProdItem)sender.Graph.Caches[typeof(AMProdItem)].Current ?? sender.Graph.Caches[typeof(AMProdItem)].Cached.RowCast<AMProdItem>()
					.Where(r => r.OrderType == splitRow?.OrderType && r.InventoryID == splitRow?.InventoryID).FirstOrDefault();
			}

			planRow.Hold = parent?.Hold ?? false;
			planRow.PlanDate = parent?.EndDate ?? splitRow?.TranDate;
			if(parent != null)
			{
				planRow.PlanType = GetPlanType(parent, planRow.Hold.GetValueOrDefault());
				planRow.BAccountID = parent.CustomerID;
				planRow.RefNoteID = parent.NoteID;
				planRow.DemandPlanID = parent.DemandPlanID;
			}

			if (planRow.RefNoteID == Guid.Empty)
			{
				planRow.RefNoteID = null;
			}

			return string.IsNullOrEmpty(planRow.PlanType) || planRow.PlanQty.GetValueOrDefault() == 0 ? null : planRow;
		}

		#endregion
	}
}
