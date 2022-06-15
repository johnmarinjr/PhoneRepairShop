using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PM.MaterialManagement
{
    public class SOShipmentEntryMaterialExt : PXGraphExtension<SOShipmentEntry>
    {
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteStatusAccum> projectsitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLocationStatusAccum> projectlocationstatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatus> dummy; //To Prevent Cache-Inheritance-Clash in LSSelect  
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		[PXMergeAttributes(Method = MergeMethod.Merge)]
        [SOShipLineSplitForProjectPlanID(typeof(SOShipment.noteID), typeof(SOShipment.hold), typeof(SOShipment.shipDate))]
        protected virtual void _(Events.CacheAttached<SOShipLineSplit.planID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [SOUnassignedShipLineSplitForProjectPlanID(typeof(SOShipment.noteID), typeof(SOShipment.hold), typeof(SOShipment.shipDate))]
        protected virtual void _(Events.CacheAttached<PX.Objects.SO.Unassigned.SOShipLineSplit.planID> e) { }

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
        }

        [PXOverride]
        public virtual List<PXResult> SelectLocationStatus(SOShipLine newline,
            Func<SOShipLine, List<PXResult>> baseMethod)
        {
            var select = new PXSelectReadonly2<PMLocationStatus,
                    InnerJoin<INLocation, On<PMLocationStatus.FK.Location>,
                    LeftJoin<PMSiteStatus, On<PMSiteStatus.inventoryID, Equal<PMLocationStatus.inventoryID>,
						And<PMSiteStatus.subItemID, Equal<PMLocationStatus.subItemID>,
						And<PMSiteStatus.siteID, Equal<PMLocationStatus.siteID>,
						And<PMSiteStatus.projectID, Equal<PMLocationStatus.projectID>,
						And<PMSiteStatus.taskID, Equal<PMLocationStatus.taskID>>>>>>>>,
                    Where<PMLocationStatus.inventoryID, Equal<Required<PMLocationStatus.inventoryID>>,
                    And<PMLocationStatus.siteID, Equal<Required<PMLocationStatus.siteID>>,
                    And<INLocation.salesValid, Equal<boolTrue>,
					And<PMLocationStatus.taskID, Equal<Required<PMLocationStatus.taskID>>,
               And<INLocation.inclQtyAvail, Equal<boolTrue>>>>>>,
               OrderBy<Asc<INLocation.pickPriority, Asc<INLocation.locationCD>>>>(Base);

            var pars = new List<object>(capacity: 8) { newline.InventoryID, newline.SiteID, newline.TaskID.GetValueOrDefault() };
            if (PXAccess.FeatureInstalled<FeaturesSet.subItem>())
            {
                select.WhereAnd<Where<PMLocationStatus.subItemID, Equal<Required<PMLocationStatus.subItemID>>>>();
                pars.Add(newline.SubItemID);
            }

            if (newline.ProjectID != null && newline.TaskID != null)
            {
				PMProject project = PMProject.PK.Find(Base, newline.ProjectID);
				if (project != null && project.AccountingMode == ProjectAccountingModes.Linked)
				{
					//Reset TaskID to 0.
					pars[2] = 0;

					select.WhereAnd<Where<INLocation.projectID, Equal<Required<INLocation.projectID>>,
						Or<INLocation.projectID, IsNull>>>();
					pars.Add(newline.ProjectID);
				}
            }
            else
            {
				select.WhereAnd<Where<INLocation.projectID, IsNull>>();
			}

            return select.Select(pars.ToArray()).Cast<PXResult>().ToList();
        }

        [PXOverride]
		public virtual decimal? CreateSplitsForAvailableNonLots(
			decimal? PlannedQty, string origPlanType,
			SOShipLine newline, INLotSerClass lotserclass,
			Func<decimal?, string, SOShipLine, INLotSerClass, decimal?> baseMethod)
		{
            return Base.CreateSplitsForAvailableNonLotsImpl<PMLocationStatus, PMLocationStatusAccum, PMSiteStatus, PMSiteStatusAccum>(PlannedQty, origPlanType, newline, lotserclass);
        }

		[PXOverride]
		public virtual List<PXResult> SelectLotSerialStatus(string origLotSerialNbr, SOShipLine newline, INLotSerClass lotserclass,
			Func<string, SOShipLine, INLotSerClass, List<PXResult>> baseMethod)
		{
			PXSelectBase<PMLotSerialStatus> cmd;
			if (!string.IsNullOrEmpty(origLotSerialNbr))
			{
				cmd = new PXSelectReadonly2<PMLotSerialStatus,
				InnerJoin<INLocation, On<PMLotSerialStatus.FK.Location>,
				LeftJoin<PMSiteStatus, On<PMSiteStatus.inventoryID, Equal<PMLotSerialStatus.inventoryID>,
					And<PMSiteStatus.subItemID, Equal<PMLotSerialStatus.subItemID>,
					And<PMSiteStatus.siteID, Equal<PMLotSerialStatus.siteID>,
					And<PMSiteStatus.projectID, Equal<PMLotSerialStatus.projectID>,
					And<PMSiteStatus.taskID, Equal<PMLotSerialStatus.taskID>>>>>>>>,
				Where<PMLotSerialStatus.inventoryID, Equal<Required<PMLotSerialStatus.inventoryID>>,
					And<PMLotSerialStatus.subItemID, Equal<Required<PMLotSerialStatus.subItemID>>,
					And<PMLotSerialStatus.siteID, Equal<Required<PMLotSerialStatus.siteID>>,
					And<PMLotSerialStatus.taskID, Equal<Required<PMLotSerialStatus.taskID>>,
					And<INLocation.salesValid, Equal<boolTrue>,
					And<INLocation.inclQtyAvail, Equal<boolTrue>>>>>>>>(Base);
			}
			else
			{
				cmd = new PXSelectReadonly2<PMLotSerialStatus,
				InnerJoin<INLocation, On<PMLotSerialStatus.FK.Location>,
				LeftJoin<PMSiteStatus, On<PMSiteStatus.inventoryID, Equal<PMLotSerialStatus.inventoryID>,
					And<PMSiteStatus.subItemID, Equal<PMLotSerialStatus.subItemID>,
					And<PMSiteStatus.siteID, Equal<PMLotSerialStatus.siteID>,
					And<PMSiteStatus.projectID, Equal<PMLotSerialStatus.projectID>,
					And<PMSiteStatus.taskID, Equal<PMLotSerialStatus.taskID>>>>>>,
				InnerJoin<INSiteLotSerial, On<INSiteLotSerial.inventoryID, Equal<PMLotSerialStatus.inventoryID>,
				And<INSiteLotSerial.siteID, Equal<PMLotSerialStatus.siteID>, And<INSiteLotSerial.lotSerialNbr, Equal<PMLotSerialStatus.lotSerialNbr>>>>>>>,
				Where<PMLotSerialStatus.inventoryID, Equal<Required<PMLotSerialStatus.inventoryID>>,
				And<PMLotSerialStatus.subItemID, Equal<Required<PMLotSerialStatus.subItemID>>,
				And<PMLotSerialStatus.siteID, Equal<Required<PMLotSerialStatus.siteID>>,
				And<PMLotSerialStatus.taskID, Equal<Required<PMLotSerialStatus.taskID>>,
				And<INLocation.salesValid, Equal<boolTrue>,
				And<INLocation.inclQtyAvail, Equal<boolTrue>,
				And<PMLotSerialStatus.qtyOnHand, Greater<decimal0>, And<INSiteLotSerial.qtyHardAvail, Greater<decimal0>>>>>>>>>>(Base);
			}

			var pars = new List<object>(capacity: 8) { newline.InventoryID, newline.SubItemID, newline.SiteID, newline.TaskID.GetValueOrDefault() };

			if (!string.IsNullOrEmpty(origLotSerialNbr))
			{
				cmd.WhereAnd<Where<PMLotSerialStatus.lotSerialNbr, Equal<Required<PMLotSerialStatus.lotSerialNbr>>>>();
				pars.Add(origLotSerialNbr);
			}
			
			if (newline.ProjectID != null && newline.TaskID != null)
			{
				PMProject project = PMProject.PK.Find(Base, newline.ProjectID);
				if (project != null && project.AccountingMode == ProjectAccountingModes.Linked)
				{
					//Reset TaskID to 0.
					pars[3] = 0;

					cmd.WhereAnd<Where<INLocation.projectID, Equal<Required<INLocation.projectID>>,
						Or<INLocation.projectID, IsNull>>>();
					pars.Add(newline.ProjectID);
				}
			}
			else
			{
				cmd.WhereAnd<Where<INLocation.projectID, IsNull>>();
			}

			Base.FindImplementation<GraphExtensions.LineSplitting.SOShipmentLineSplittingProjectExtension>()
				.AppendSerialStatusCmdOrderByProject(cmd, newline, lotserclass);

			return cmd.Select(pars.ToArray()).Cast<PXResult>().ToList();
		}

		[PXOverride]
		public virtual decimal? CreateSplitsForAvailableLots(
		decimal? PlannedQty, string origPlanType, string origLotSerialNbr,
		SOShipLine newline, INLotSerClass lotserclass,
		Func<decimal?, string, string, SOShipLine, INLotSerClass, decimal?> baseMethod)
		{
			return Base.CreateSplitsForAvailableLotsImpl<PMLotSerialStatus, PMLotSerialStatusAccum, PMSiteStatus, PMSiteStatusAccum>(
				PlannedQty, origPlanType, origLotSerialNbr, newline, lotserclass);
		}

		[PXOverride]
		public virtual void MergeStatusCachesBetweenGraphs(PXGraph source, PXGraph target,
			Action<PXGraph, PXGraph> baseMethod)
		{
			baseMethod(source, target);

			target.Caches[typeof(PMSiteSummaryStatusAccum)] = source.Caches[typeof(PMSiteSummaryStatusAccum)];
			target.Caches[typeof(PMSiteStatusAccum)] = source.Caches[typeof(PMSiteStatusAccum)];
			target.Caches[typeof(PMLocationStatusAccum)] = source.Caches[typeof(PMLocationStatusAccum)];
			target.Caches[typeof(PMLotSerialStatusAccum)] = source.Caches[typeof(PMLotSerialStatusAccum)];

			target.Views.Caches.Remove(typeof(PMSiteSummaryStatusAccum));
			target.Views.Caches.Remove(typeof(PMSiteStatusAccum));
			target.Views.Caches.Remove(typeof(PMLocationStatusAccum));
			target.Views.Caches.Remove(typeof(PMLotSerialStatusAccum));
		}

		[PXOverride]
		public virtual decimal? GetQtyHardAvailFromSiteStatus(PXGraph docgraph, SOLineSplit split,
			Func<PXGraph, SOLineSplit, decimal?> baseMethod)
		{
			decimal? fallback = baseMethod(docgraph, split);
			SOLine soline = (SOLine)PXParentAttribute.SelectParent(docgraph.Caches[typeof(SOLineSplit)], split, typeof(SOLine));

			if (soline != null) 
			{
				PMSiteSummaryStatusAccum accum = new PMSiteSummaryStatusAccum();
				accum.InventoryID = split.InventoryID;
				accum.SiteID = split.SiteID;
				accum.SubItemID = split.SubItemID;
				accum.ProjectID = soline.ProjectID;
				
				accum = (PMSiteSummaryStatusAccum)docgraph.Caches[typeof(PMSiteSummaryStatusAccum)].Insert(accum);
				accum = PXCache<PMSiteSummaryStatusAccum>.CreateCopy(accum);

				PMSiteSummaryStatus stat = PMSiteSummaryStatus.PK.Find(docgraph, split.InventoryID, split.SubItemID, split.SiteID, soline.ProjectID);
				if (stat != null)
				{
					accum.QtyHardAvail += stat.QtyHardAvail;
				}

				return accum.QtyHardAvail;
			}

			return fallback;
		}

		[PXOverride]
		public virtual void CreateShipment(CreateShipmentArgs args,
			Action<CreateShipmentArgs> baseMethod)
		{
			try
			{
				baseMethod(args);
			}
			catch(SOShipmentException ex)
			{
				if (ex.Code == SOShipmentException.ErrorCode.NothingToShipTraced && ex.Item != null)
				{
					PMProject project = PMProject.PK.Find(Base, ex.Item.ProjectID);
					INSite site = INSite.PK.Find(Base, ex.Item.SiteID);

					throw new SOShipmentException(ex.Code, ex.Item,
						project.AccountingMode == ProjectAccountingModes.Linked ? Messages.NothingToShipTraced_Linked : Messages.NothingToShipTraced_NotLinked
						, ex.Item.OrderType, ex.Item.OrderNbr, project.ContractCD.Trim(), site.SiteCD.Trim());
				}
				else
				{
					throw;
				}
			}
		}

		[PXOverride]
		public virtual void PostShipment(INRegisterEntryBase docgraph, PXResult<SOOrderShipment, SOOrder> sh, DocumentList<INRegister> list, ARInvoice invoice,
			Action<INRegisterEntryBase, PXResult<SOOrderShipment, SOOrder>, DocumentList<INRegister>, ARInvoice> baseMethod)
		{
			INIssueEntryMaterialExt issueExt = docgraph.GetExtension<INIssueEntryMaterialExt>();
			if (issueExt != null)
			{
				issueExt.IsShipmentPosting = true;

				try
				{
					baseMethod(docgraph, sh, list, invoice);
				}
				finally
				{
					issueExt.IsShipmentPosting = false;
				}
			}
		}
	}
}
