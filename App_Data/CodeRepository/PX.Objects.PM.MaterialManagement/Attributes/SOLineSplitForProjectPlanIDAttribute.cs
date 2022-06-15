using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using System;

namespace PX.Objects.PM.MaterialManagement
{
    public class SOLineSplitForProjectPlanIDAttribute : SOLineSplitPlanIDAttribute
	{
		public SOLineSplitForProjectPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate) :
			base(ParentNoteID, ParentHoldEntry, ParentOrderDate)
		{ }

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldDefaulting.AddHandler<PMSiteStatusAccum.negAvailQty>(base.SiteStatus_NegAvailQty_FieldDefaulting);
			sender.Graph.FieldDefaulting.AddHandler<PMSiteSummaryStatusAccum.negAvailQty>(base.SiteStatus_NegAvailQty_FieldDefaulting);
		}

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan plan_Row, object orig_Row)
		{
			INItemPlan plan;
			if (PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>())
			{
				plan = ManufacturingDefaultValues(sender, plan_Row, orig_Row);
			}
			else
			{
				plan = base.DefaultValues(sender, plan_Row, orig_Row);
			}

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


		protected override bool InitPlanRequired(PXCache cache, PXRowUpdatedEventArgs e)
		{
			if (PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>())
			{
				return base.InitPlanRequired(cache, e) || !cache.ObjectsEqual<SOLineSplit.aMProdCreate>(e.Row, e.OldRow);
			}
			else
			{
				return base.InitPlanRequired(cache, e);
			}
		}

		protected override bool IsLineLinked(SOLineSplit soLineSplit)
		{
			if (PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>())
			{
				return base.IsLineLinked(soLineSplit) ||  PXCache<SOLineSplit>.GetExtension<AM.CacheExtensions.SOLineSplitExt>(soLineSplit)?.AMProdOrdID != null;
			}
			else
			{
				return base.IsLineLinked(soLineSplit);
			}
		}

		protected override string CalcPlanType(INItemPlan plan, SOLineSplit splitRow, SOOrderType ordertype, bool isOrderOnHold)
		{
			var planType = base.CalcPlanType(plan, splitRow, ordertype, isOrderOnHold);
			if (splitRow?.IsAllocated == false && splitRow?.AMProdCreate == true)
			{
				planType = INPlanConstants.PlanM8;
			}

			return planType;
		}

		private  INItemPlan ManufacturingDefaultValues(PXCache sender, INItemPlan planRow, object origRow)
		{
			var splitRow = (SOLineSplit)origRow;
			var splitRowExt = PXCache<SOLineSplit>.GetExtension<AM.CacheExtensions.SOLineSplitExt>(splitRow);
			var isProductionLinked = splitRowExt != null && splitRow.AMProdCreate.GetValueOrDefault() && !string.IsNullOrWhiteSpace(splitRowExt.AMProdOrdID);

			var planRowReturn = base.DefaultValues(sender, planRow, origRow);

			if (planRowReturn == null || splitRowExt == null)
			{
				return planRowReturn;
			}

			if (AM.INPlanTypeHelper.IsMfgPlanType(planRowReturn.PlanType) || isProductionLinked)
			{
				//It is possible during production creation the order gets marked as linked row however...
				//  this doesn't give the plan type enough time to set as M8 due to IsLineLinked(SOLineSplit) reporting back a linked row
				planRowReturn.PlanType = INPlanConstants.PlanM8;

				planRowReturn.FixedSource = INReplenishmentSource.Manufactured;
				planRowReturn.PlanQty = splitRow.BaseQty.GetValueOrDefault() - splitRow.BaseReceivedQty.GetValueOrDefault() - splitRow.BaseShippedQty.GetValueOrDefault();

				if (planRowReturn.PlanQty.GetValueOrDefault() <= 0)
				{
					return null;
				}
			}

			return planRowReturn;
		}
	}
}
