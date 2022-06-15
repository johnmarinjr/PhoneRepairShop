using PX.Data;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PM.MaterialManagement
{
    public class INIntegrityCheckMaterialExt : PXGraphExtension<INIntegrityCheck>
    {
		public PXSelect<PMSiteStatusAccum> projectsitestatus;
		public PXSelect<PMSiteSummaryStatusAccum> projectsitesummarystatus;
		public PXSelect<PMLocationStatusAccum> projectlocationstatus;
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
        }

		[PXOverride]
		public virtual void ClearSiteStatusAllocatedQuantities(INItemSiteSummary itemsite, Action<INItemSiteSummary> baseMethod)
		{
			baseMethod(itemsite);

			PXDatabase.Update<PMSiteStatus>(
				Base.AssignAllDBDecimalFieldsToZeroCommand(projectsitestatus.Cache,
					excludeFields: new string[]
					{
						nameof(PMLocationStatus.qtyOnHand)
					})
				.Append(new PXDataFieldRestrict<PMSiteStatus.inventoryID>(PXDbType.Int, 4, itemsite.InventoryID, PXComp.EQ))
				.Append(new PXDataFieldRestrict<PMSiteStatus.siteID>(PXDbType.Int, 4, itemsite.SiteID, PXComp.EQ))
				.ToArray());

			PXDatabase.Update<PMSiteSummaryStatus>(
				Base.AssignAllDBDecimalFieldsToZeroCommand(projectsitestatus.Cache,
					excludeFields: new string[]
					{
						nameof(PMLocationStatus.qtyOnHand)
					})
				.Append(new PXDataFieldRestrict<PMSiteSummaryStatus.inventoryID>(PXDbType.Int, 4, itemsite.InventoryID, PXComp.EQ))
				.Append(new PXDataFieldRestrict<PMSiteSummaryStatus.siteID>(PXDbType.Int, 4, itemsite.SiteID, PXComp.EQ))
				.ToArray());
		}

		[PXOverride]
		public virtual void ClearLocationStatusAllocatedQuantities(INItemSiteSummary itemsite, Action<INItemSiteSummary> baseMethod)
		{
			baseMethod(itemsite);

			PXDatabase.Update<PMLocationStatus>(
				Base.AssignAllDBDecimalFieldsToZeroCommand(projectlocationstatus.Cache,
					excludeFields: new string[]
					{
						nameof(PMLocationStatus.qtyOnHand),
						nameof(PMLocationStatus.qtyAvail),
						nameof(PMLocationStatus.qtyHardAvail),
						nameof(PMLocationStatus.qtyActual)
					})
				.Append(new PXDataFieldAssign<PMLocationStatus.qtyAvail>(PXDbType.DirectExpression, nameof(PMLocationStatus.QtyOnHand)))
				.Append(new PXDataFieldAssign<PMLocationStatus.qtyHardAvail>(PXDbType.DirectExpression, nameof(PMLocationStatus.QtyOnHand)))
				.Append(new PXDataFieldAssign<PMLocationStatus.qtyActual>(PXDbType.DirectExpression, nameof(PMLocationStatus.QtyOnHand)))
				.Append(new PXDataFieldRestrict<PMLocationStatus.inventoryID>(PXDbType.Int, 4, itemsite.InventoryID, PXComp.EQ))
				.Append(new PXDataFieldRestrict<PMLocationStatus.siteID>(PXDbType.Int, 4, itemsite.SiteID, PXComp.EQ))
				.ToArray());
		}

		[PXOverride]
		public virtual void ClearLotSerialStatusAllocatedQuantities(INItemSiteSummary itemsite, Action<INItemSiteSummary> baseMethod)
		{
			baseMethod(itemsite);

			PXDatabase.Update<PMLotSerialStatus>(
				Base.AssignAllDBDecimalFieldsToZeroCommand(projectlotserialstatus.Cache,
					excludeFields: new string[]
					{
						nameof(INLotSerialStatus.qtyOnHand),
						nameof(INLotSerialStatus.qtyAvail),
						nameof(INLotSerialStatus.qtyHardAvail),
						nameof(INLotSerialStatus.qtyActual)
					})
				.Append(new PXDataFieldAssign<INLotSerialStatus.qtyAvail>(PXDbType.DirectExpression, nameof(INLotSerialStatus.QtyOnHand)))
				.Append(new PXDataFieldAssign<INLotSerialStatus.qtyHardAvail>(PXDbType.DirectExpression, nameof(INLotSerialStatus.QtyOnHand)))
				.Append(new PXDataFieldAssign<INLotSerialStatus.qtyActual>(PXDbType.DirectExpression, nameof(INLotSerialStatus.QtyOnHand)))
				.Append(new PXDataFieldRestrict<INLotSerialStatus.inventoryID>(PXDbType.Int, 4, itemsite.InventoryID, PXComp.EQ))
				.Append(new PXDataFieldRestrict<INLotSerialStatus.siteID>(PXDbType.Int, 4, itemsite.SiteID, PXComp.EQ))
				.ToArray());
		}

		[PXOverride]
		public virtual void DeleteLotSerialStatusForNotTrackedItemsByItem(int? inventoryID, Action<int?> baseMethod)
		{
			baseMethod(inventoryID);
			PXDatabase.Delete<PMLotSerialStatus>(
					new PXDataFieldRestrict<PMLotSerialStatus.inventoryID>(PXDbType.Int, 4, inventoryID, PXComp.EQ),
					new PXDataFieldRestrict<PMLotSerialStatus.qtyOnHand>(PXDbType.Decimal, 4, 0m, PXComp.EQ)
				);
		}

		[PXOverride]
		public virtual void PopulateSiteAvailQtyByLocationStatus(INItemSiteSummary itemsite, Action<INItemSiteSummary> baseMethod)
		{
			baseMethod(itemsite);

			foreach (PXResult<ReadOnlyPMLocationStatus, INLocation> res in PXSelectJoinGroupBy<ReadOnlyPMLocationStatus,
				InnerJoin<INLocation, On<INLocation.locationID, Equal<ReadOnlyPMLocationStatus.locationID>>>,
				Where<ReadOnlyPMLocationStatus.inventoryID, Equal<Current<INItemSiteSummary.inventoryID>>,
					And<ReadOnlyPMLocationStatus.siteID, Equal<Current<INItemSiteSummary.siteID>>,
					And<ReadOnlyPMLocationStatus.projectID, NotEqual<Required<ReadOnlyPMLocationStatus.projectID>>>>>,
				Aggregate<GroupBy<ReadOnlyPMLocationStatus.inventoryID,
					GroupBy<ReadOnlyPMLocationStatus.siteID,
					GroupBy<ReadOnlyPMLocationStatus.subItemID,
					GroupBy<ReadOnlyPMLocationStatus.projectID,
					GroupBy<ReadOnlyPMLocationStatus.taskID,
					Sum<ReadOnlyPMLocationStatus.qtyOnHand>>>>>>>>
				.SelectMultiBound(Base, new object[] { itemsite }, ProjectDefaultAttribute.NonProject()))
			{
				PMSiteStatusAccum status = new PMSiteStatusAccum();
				status.InventoryID = ((ReadOnlyPMLocationStatus)res).InventoryID;
				status.SubItemID = ((ReadOnlyPMLocationStatus)res).SubItemID;
				status.SiteID = ((ReadOnlyPMLocationStatus)res).SiteID;
				status.ProjectID = ((ReadOnlyPMLocationStatus)res).ProjectID;
				status.TaskID = ((ReadOnlyPMLocationStatus)res).TaskID;
				status = (PMSiteStatusAccum)projectsitestatus.Cache.Insert(status);

				if (((INLocation)res).InclQtyAvail == true)
				{
					status.QtyAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
					status.QtyHardAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
					status.QtyActual += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
				}
				else
				{
					status.QtyNotAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
				}

				PMSiteSummaryStatusAccum statusSummary = new PMSiteSummaryStatusAccum();
				statusSummary.InventoryID = ((ReadOnlyPMLocationStatus)res).InventoryID;
				statusSummary.SubItemID = ((ReadOnlyPMLocationStatus)res).SubItemID;
				statusSummary.SiteID = ((ReadOnlyPMLocationStatus)res).SiteID;
				statusSummary.ProjectID = ((ReadOnlyPMLocationStatus)res).ProjectID;
				statusSummary = (PMSiteSummaryStatusAccum)projectsitesummarystatus.Cache.Insert(statusSummary);

				if (((INLocation)res).InclQtyAvail == true)
				{
					statusSummary.QtyAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
					statusSummary.QtyHardAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
					statusSummary.QtyActual += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
				}
				else
				{
					statusSummary.QtyNotAvail += ((ReadOnlyPMLocationStatus)res).QtyOnHand;
				}
			}
		}

		[PXOverride]
		public virtual void UpdateAllocatedQuantitiesWithPlans(INItemSiteSummary itemsite, INItemPlan plan, INPlanType plantype, Action<INItemSiteSummary, INItemPlan, INPlanType> baseMethod)
		{
			baseMethod(itemsite, plan, plantype);

			if (plan.InventoryID != null &&
								plan.SubItemID != null &&
								plan.SiteID != null &&
								plan.ProjectID != null &&
								plan.TaskID != null)
			{
				if (plan.LocationID != null)
				{
					PMLocationStatusAccum item = Base.UpdateAllocatedQuantities<PMLocationStatusAccum>(plan, plantype, true);
					Base.UpdateAllocatedQuantities<PMSiteStatusAccum>(plan, plantype, (bool)item.InclQtyAvail);
					Base.UpdateAllocatedQuantities<PMSiteSummaryStatusAccum>(plan, plantype, (bool)item.InclQtyAvail);
					if (!string.IsNullOrEmpty(plan.LotSerialNbr))
					{
						Base.UpdateAllocatedQuantities<PMLotSerialStatusAccum>(plan, plantype, true);
					}
				}
				else
				{
					Base.UpdateAllocatedQuantities<PMSiteStatusAccum>(plan, plantype, true);
					Base.UpdateAllocatedQuantities<PMSiteSummaryStatusAccum>(plan, plantype, true);
				}
			}
		}

		[PXOverride]
		public virtual void DeleteZeroStatusRecords(INItemSiteSummary itemsite, Action<INItemSiteSummary> baseMethod)
		{
			baseMethod(itemsite);
			var restrictions = new List<PXDataFieldRestrict>
			{
				new PXDataFieldRestrict(nameof(PMLocationStatus.InventoryID), PXDbType.Int, 4, itemsite.InventoryID, PXComp.EQ),
				new PXDataFieldRestrict(nameof(PMLocationStatus.SiteID), PXDbType.Int, 4, itemsite.SiteID, PXComp.EQ),
				new PXDataFieldRestrict(nameof(PMLocationStatus.QtyOnHand), PXDbType.Decimal, decimal.Zero),
				new PXDataFieldRestrict(nameof(PMLocationStatus.QtyAvail), PXDbType.Decimal, decimal.Zero),
				new PXDataFieldRestrict(nameof(PMLocationStatus.QtyHardAvail), PXDbType.Decimal, decimal.Zero),
			};
			restrictions.AddRange(
				projectlocationstatus.Cache.GetAllDBDecimalFields()
				.Select(f => new PXDataFieldRestrict(f, PXDbType.Decimal, decimal.Zero)));
			PXDatabase.Delete<PMLocationStatus>(restrictions.ToArray());
		}

		[PXOverride]
		public virtual void PersistCaches(Action baseMethod)
		{
			baseMethod();

			projectsitestatus.Cache.Persist(PXDBOperation.Insert);
			projectsitestatus.Cache.Persist(PXDBOperation.Update);

			projectsitesummarystatus.Cache.Persist(PXDBOperation.Insert);
			projectsitesummarystatus.Cache.Persist(PXDBOperation.Update);

			projectlocationstatus.Cache.Persist(PXDBOperation.Insert);
			projectlocationstatus.Cache.Persist(PXDBOperation.Update);

			projectlotserialstatus.Cache.Persist(PXDBOperation.Insert);
			projectlotserialstatus.Cache.Persist(PXDBOperation.Update);
		}

		[PXOverride]
		public virtual void OnCachePersisted(Action baseMethod)
		{
			baseMethod();

			projectsitestatus.Cache.Persisted(false);
			projectsitesummarystatus.Cache.Persisted(false);
			projectlocationstatus.Cache.Persisted(false);
			projectlotserialstatus.Cache.Persisted(false);
		}
	}

	[Serializable]
	[PXHidden]
	public partial class ReadOnlyPMSiteStatus : PMSiteStatus
	{
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
		{
		}
		[PXDefault]
		[PXDBInt(IsKey = true)]
		public override Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region TaskID
		public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID>
		{
		}
		[PXDefault]
		[PXDBInt(IsKey = true)]
		public override Int32? TaskID
		{
			get;
			set;
		}
		#endregion
	}

	[Serializable]
	[PXHidden]
	public partial class ReadOnlyPMLocationStatus : PMLocationStatus
	{
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? LocationID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
		{
		}
		[PXDefault]
		[PXDBInt(IsKey = true)]
		public override Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region TaskID
		public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID>
		{
		}
		[PXDefault]
		[PXDBInt(IsKey = true)]
		public override Int32? TaskID
		{
			get;
			set;
		}
		#endregion
		
		#region QtyOnHand
		public new abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		#endregion
	}
}
