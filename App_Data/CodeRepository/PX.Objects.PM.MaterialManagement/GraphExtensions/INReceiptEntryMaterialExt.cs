using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.PM.MaterialManagement
{

    public class INReceiptEntryMaterialExt : CostCenterSupport<INReceiptEntry>
	{
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteStatusAccum> projectsitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatus> dummy; //To Prevent Cache-Inheritance-Clash in LSSelect  
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLocationStatusAccum> projectlocationstatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[INTranSplitForProjectPlanID(typeof(INRegister.noteID), typeof(INRegister.hold), typeof(INRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INTranSplit.planID> e) { }

		
	}
}
