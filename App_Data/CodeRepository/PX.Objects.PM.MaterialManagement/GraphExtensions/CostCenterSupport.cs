using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.PM.MaterialManagement
{
    public class CostCenterSupport<T> : PXGraphExtension<T> where T : INRegisterEntryBase
	{
		[PXCopyPasteHiddenView()]
		public PXSelect<PMCostCenter> ProjectCostCenter;

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(PMCostCenter.costCenterID), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INTran.costCenterID> e) { }

		public bool IsShipmentPosting { get; set; }

		public override void Initialize()
		{
			base.Initialize();
			MoveProjectCostCenterViewCacheToTop();
			Base.OnBeforePersist += Base_OnBeforePersist;

			VerifyProjectStockIsInitialized();
		}

		private void VerifyProjectStockIsInitialized()
        {
			PMSetup setup = PXSelect<PMSetup>.Select(Base);
			if (setup != null && setup.StockInitRequired == true)
            {
				throw new PXSetupNotEnteredException<PMValidationFilter>(Messages.StockNotInitialized);
			}
        }

		private void MoveProjectCostCenterViewCacheToTop()
        {
			int index = Base.Views.Caches.IndexOf(typeof(PMCostCenter));
			if (index > 0)
			{
				Base.Views.Caches.RemoveAt(index);
				Base.Views.Caches.Insert(0, typeof(PMCostCenter));
			}
		}


		private void Base_OnBeforePersist(PXGraph obj)
        {
			foreach (INTran split in Base.INTranDataMember.Cache.Inserted)
            {
				SetCostCenter(split);
			}

			foreach (INTran split in Base.INTranDataMember.Cache.Updated)
			{
				SetCostCenter(split);
			}
		}

		protected virtual void _(Events.FieldVerifying<INTranSplit, INTranSplit.locationID> e)
		{
			if (e.Row != null)
			{
				INTran tran = (INTran)PXParentAttribute.SelectParent(Base.INTranSplitDataMember.Cache, e.Row, typeof(INTran));
				if (tran != null &&
					e.NewValue != null &&
					tran.LocationID != (int?)e.NewValue &&
					GetAccountingMode(tran.ProjectID) == ProjectAccountingModes.ProjectSpecific &&
					!IsShipmentPosting)
                {
					INLocation location = INLocation.PK.Find(Base, (int?)e.NewValue);
					PXSetPropertyException ex = new PXSetPropertyException(Messages.MixedLocationsAreNotAllowed);
					if (location != null)
						ex.ErrorValue = location.LocationCD;

					throw ex;
                }
			}
		}

		protected virtual void SetCostCenter(INTran row)
		{
			if (row != null && 
				row.SiteID != null && 
				row.LocationID != null && 
				row.TaskID != null &&
				GetAccountingMode(row.ProjectID) == ProjectAccountingModes.ProjectSpecific)
			{
				row.CostCenterID = FindOrCreateProjectCostSite(row.SiteID, row.LocationID, row.ProjectID, row.TaskID);
			}
			else
            {
				row.CostCenterID = null;
            }
		}

		protected string GetAccountingMode(int? projectID)
		{
			if (projectID != null)
			{
				PMProject project = PMProject.PK.Find(Base, projectID);
				if (project != null && project.NonProject != true)
				{
					return project.AccountingMode;
				}
			}

			return ProjectAccountingModes.Valuated;
		}

		protected int? FindOrCreateProjectCostSite(int? siteID, int? locationID, int? projectID, int? taskID)
		{
			var select = new PXSelect<PMCostCenter,
				Where<PMCostCenter.siteID, Equal<Required<PMCostCenter.siteID>>,
				And<PMCostCenter.locationID, Equal<Required<PMCostCenter.locationID>>,
				And<PMCostCenter.projectID, Equal<Required<PMCostCenter.projectID>>,
				And<PMCostCenter.taskID, Equal<Required<PMCostCenter.taskID>>>>>>>(Base);

			PMCostCenter existing = select.Select(siteID, locationID, projectID, taskID);

			if (existing != null)
			{
				return existing.CostCenterID;
			}
			else
			{
				return InsertNewCostSite(siteID, locationID, projectID, taskID);
			}
		}

		private int? InsertNewCostSite(int? siteID, int? locationID, int? projectID, int? taskID)
		{
			PMCostCenter costSite = new PMCostCenter();
			costSite.SiteID = siteID;
			costSite.LocationID = locationID;
			costSite.ProjectID = projectID;
			costSite.TaskID = taskID;
			costSite.CostCenterCD = BuildCostSiteCD(projectID, taskID, locationID);

			costSite = ProjectCostCenter.Insert(costSite);
			if (costSite != null)
			{
				return costSite.CostCenterID;
			}

			throw new PXException("Failed to insert new PMCostCenter");
		}

		private string BuildCostSiteCD(int? projectID, int? taskID, int? locationID)
		{
			PMProject project = PMProject.PK.Find(Base, projectID);
			PMTask task = PMTask.PK.Find(Base, taskID);
			INLocation location = INLocation.PK.Find(Base, locationID);

			if (project != null && task != null && location != null)
			{
				return string.Format("{0}/{1}/{2}", project.ContractCD.Trim(), task.TaskCD.Trim(), location.LocationCD.Trim());
			}

			return null;
		}


	}
}
