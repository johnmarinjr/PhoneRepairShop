﻿using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using System;

namespace PX.Objects.PM.MaterialManagement
{
    public class SOShipLineSplitForProjectPlanIDAttribute : SOShipLineSplitPlanIDAttribute
	{
		public SOShipLineSplitForProjectPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate) :
			base(ParentNoteID, ParentHoldEntry, ParentOrderDate)
		{ }

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan plan_Row, object orig_Row)
		{
			INItemPlan plan = base.DefaultValues(sender, plan_Row, orig_Row);
			if (plan != null && INTranSplitForProjectPlanIDAttribute.IsLinkedProject(sender.Graph, plan.ProjectID))
			{
				plan.ProjectID = ProjectDefaultAttribute.NonProject();
				plan.TaskID = null;
			}

			return plan;
		}

		protected override void UpdateAllocatedQuantitiesWithPlan(PXCache sender, IN.INItemPlan plan, bool revert = false)
		{
			base.UpdateAllocatedQuantitiesWithPlan(sender, plan, revert);

			if (CanUpdateAllocatedQuantitiesWithPlan(sender, plan))
			{
				INPlanType plantype = INPlanType.PK.Find(sender.Graph, plan.PlanType);
				plantype = revert ? -plantype : plantype;

				if (plan.LocationID != null && IsPlanProjectKeyValid(plan))
				{
					PMLocationStatusAccum item = UpdateAllocatedQuantities<PMLocationStatusAccum>(sender.Graph, plan, plantype, true);
					UpdateAllocatedQuantities<PMSiteStatusAccum>(sender.Graph, plan, plantype, (bool)item.InclQtyAvail);
					UpdateAllocatedQuantities<PMSiteSummaryStatusAccum>(sender.Graph, plan, plantype, (bool)item.InclQtyAvail);

					if (!string.IsNullOrEmpty(plan.LotSerialNbr))
					{
						UpdateAllocatedQuantities<PMLotSerialStatusAccum>(sender.Graph, plan, plantype, true);
					}
				}
				else
				{
					if (IsPlanProjectKeyValid(plan))
						UpdateAllocatedQuantities<PMSiteStatusAccum>(sender.Graph, plan, plantype, true);
					UpdateAllocatedQuantities<PMSiteSummaryStatusAccum>(sender.Graph, plan, plantype, true);
				}
			}
		}

		private bool IsPlanProjectKeyValid(INItemPlan plan)
		{
			if (plan.ProjectID != null &&
				plan.ProjectID != ProjectDefaultAttribute.NonProject() &&
				plan.TaskID == null)
			{
				return false;
			}

			return true;
		}

		protected override INPlanType GetTargetPlanType<TNode>(PXGraph graph, INItemPlan plan, INPlanType plantype)
		{
			return GetTargetPlanTypeBase<TNode>(graph, plan, plantype);
		}

		public override void Accumulator_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			base.Accumulator_RowPersisted(sender, e);

			if (e.Operation != PXDBOperation.Delete && e.TranStatus == PXTranStatus.Completed)
			{
				if (sender.GetItemType() == typeof(PM.PMLocationStatusAccum))
				{
					Clear<PM.PMLocationStatus>(sender.Graph);
				}
				if (sender.GetItemType() == typeof(PM.PMLotSerialStatusAccum))
				{
					Clear<PMLotSerialStatus>(sender.Graph);
				}
				if (sender.GetItemType() == typeof(PM.PMSiteStatusAccum))
				{
					Clear<PMSiteStatusAccum>(sender.Graph);
				}
				if (sender.GetItemType() == typeof(PM.PMSiteSummaryStatusAccum))
				{
					Clear<PMSiteSummaryStatusAccum>(sender.Graph);
				}
			}
		}
	}
}
