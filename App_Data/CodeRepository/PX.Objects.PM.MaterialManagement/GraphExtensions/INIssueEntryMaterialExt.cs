using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM.MaterialManagement
{

    public class INIssueEntryMaterialExt : CostCenterSupport<INIssueEntry>
	{
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteStatusAccum> projectsitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLocationStatusAccum> projectlocationstatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatus> dummy; //To Prevent Cache-Inheritance-Clash in LSSelect while IssueNumbers  
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}

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
				if (!e.Cancel)
					e.NewValue = true;
				e.Cancel = true;
			});

			Base.FieldDefaulting.AddHandler<PMSiteSummaryStatusAccum.negAvailQty>((sender, e) =>
			{
				if (!e.Cancel)
					e.NewValue = true;
				e.Cancel = true;
			});
		}
	}
}
