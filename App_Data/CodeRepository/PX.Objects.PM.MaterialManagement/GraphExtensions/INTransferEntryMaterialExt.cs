using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.PM.MaterialManagement
{
    public class INTransferEntryMaterialExt : CostCenterSupport<INTransferEntry>
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

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(PMCostCenter.costCenterID), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INTran.toCostCenterID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[INTranSplitForProjectPlanID(typeof(INRegister.noteID), typeof(INRegister.hold), typeof(INRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INTranSplit.planID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PMLotSerialNbr(typeof(INTran.inventoryID), typeof(INTran.subItemID), typeof(INTran.locationID), typeof(INTran.projectID), typeof(INTran.taskID), null, PersistingCheck = PXPersistingCheck.Nothing, FieldClass = "LotSerial")]
		protected virtual void _(Events.CacheAttached<INTran.lotSerialNbr> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PMLotSerialNbr(typeof(INTranSplit.inventoryID), typeof(INTranSplit.subItemID), typeof(INTranSplit.locationID), typeof(INTranSplit.projectID), typeof(INTranSplit.taskID), typeof(INTran), PersistingCheck = PXPersistingCheck.Nothing, FieldClass = "LotSerial")]
		protected virtual void _(Events.CacheAttached<INTranSplit.lotSerialNbr> e) { }

		public override void Initialize()
		{
			base.Initialize();

			Base.FieldDefaulting.AddHandler<PMSiteStatusAccum.negAvailQty>((sender, e) =>
			{
				e.NewValue = true;
				e.Cancel = true;
			});

			Base.FieldDefaulting.AddHandler<PMSiteSummaryStatusAccum.negAvailQty>((sender, e) =>
			{
				e.NewValue = true;
				e.Cancel = true;
			});
		}

		protected override void SetCostCenter(INTran row)
		{
			base.SetCostCenter(row);

			if (row != null &&
				row.SiteID != null &&
				row.ToLocationID != null &&
				row.ToTaskID != null &&
				GetAccountingMode(row.ToProjectID) == ProjectAccountingModes.ProjectSpecific)
			{
				row.ToCostCenterID = FindOrCreateProjectCostSite(row.SiteID, row.ToLocationID, row.ToProjectID, row.ToTaskID);
			}
			else
			{
				row.ToCostCenterID = null;
			}
		}
	}
}
