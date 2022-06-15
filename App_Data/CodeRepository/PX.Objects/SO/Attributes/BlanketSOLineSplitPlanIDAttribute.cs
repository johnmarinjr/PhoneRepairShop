using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO.DAC.Projections;

namespace PX.Objects.SO.Attributes
{
	public class BlanketSOLineSplitPlanIDAttribute : INItemPlanIDAttribute
	{
		#region Ctor
		public BlanketSOLineSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry)
			: base(ParentNoteID, ParentHoldEntry)
		{
		}
		#endregion
		#region Implementation

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan planRow, object origRow)
		{
			var splitRow = (BlanketSOLineSplit)origRow;

			bool initPlan = string.IsNullOrEmpty(planRow.PlanType);
			if (initPlan)
			{
				if (splitRow.Completed == true
					|| splitRow.POCompleted == true
					|| splitRow.LineType == SOLineType.MiscCharge)
				{
					return null;
				}
				var line = PXParentAttribute.SelectParent<BlanketSOLine>(sender, splitRow);
				var order = PXParentAttribute.SelectParent<BlanketSOOrder>(sender.Graph.Caches<BlanketSOLine>(), line);
				if (order == null)
				{
					throw new Common.Exceptions.RowNotFoundException(sender.Graph.Caches<BlanketSOOrder>(),
						splitRow.OrderType, splitRow.OrderNbr);
				}
				planRow.PlanType = CalcPlanType(sender, order, splitRow);
				if (string.IsNullOrEmpty(planRow.PlanType)) return null;

				if (splitRow.POCreate == true)
				{
					planRow.FixedSource = INReplenishmentSource.Purchased;
					if (splitRow.POType != PO.POOrderType.Blanket && splitRow.POType != PO.POOrderType.DropShip && splitRow.POSource == INReplenishmentSource.PurchaseToOrder)
						planRow.SourceSiteID = splitRow.SiteID;
					else
						planRow.SourceSiteID = splitRow.SiteID;

					planRow.VendorID = splitRow.VendorID;
					if (planRow.VendorID != null)
					{
						planRow.VendorLocationID = PO.POItemCostManager.FetchLocation(
							sender.Graph,
							splitRow.VendorID,
							splitRow.InventoryID,
							splitRow.SubItemID,
							splitRow.SiteID);
					}
				}
				else
				{
					planRow.FixedSource = (splitRow.SiteID != splitRow.ToSiteID ? INReplenishmentSource.Transfer : INReplenishmentSource.None);
					planRow.SourceSiteID = splitRow.SiteID;
				}
				planRow.BAccountID = line.CustomerID;
				planRow.InventoryID = splitRow.InventoryID;
				planRow.SubItemID = splitRow.SubItemID;
				planRow.SiteID = splitRow.SiteID;
				planRow.LocationID = splitRow.LocationID;
				planRow.ProjectID = line.ProjectID;
				planRow.TaskID = line.TaskID;
				planRow.PlanDate = splitRow.ShipDate;
				planRow.UOM = line.UOM;
				planRow.LotSerialNbr = splitRow.LotSerialNbr;

				planRow.Hold = (order.Hold ?? false) || (!order.Approved ?? false);
				planRow.RefNoteID = order.NoteID;
			}

			planRow.PlanQty = (splitRow.POCreate == true ? splitRow.BaseUnreceivedQty : splitRow.BaseQty) - splitRow.BaseQtyOnOrders;

			return planRow;
		}

		protected virtual string CalcPlanType(PXCache sender, BlanketSOOrder order, BlanketSOLineSplit split)
		{
			if (split.POCreate == true && split.POSource == INReplenishmentSource.BlanketPurchaseToOrder)
				return order.IsExpired == true && string.IsNullOrEmpty(split.PONbr) ? null : INPlanConstants.Plan6B;

			if (split.POCreate == true && split.POSource == INReplenishmentSource.PurchaseToOrder)
				return order.IsExpired == true && string.IsNullOrEmpty(split.PONbr) ? null : INPlanConstants.Plan66;

			return (split.IsAllocated == true) ? INPlanConstants.Plan61 : null;
		}

		#endregion
	}
}
