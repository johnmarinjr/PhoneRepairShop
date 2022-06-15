using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.PM.MaterialManagement
{
    public class POOrderEntryMaterialExt : PXGraphExtension<POOrderEntry>
	{		
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteStatusAccum> projectsitestatus;

		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatus> dummy; //To Prevent Cache-Inheritance-Clash in LSSelect  
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		public static new bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
		}

		public override void Initialize()
		{
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

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[POLineForProjectPlanID(typeof(POOrder.noteID), typeof(POOrder.hold))]
		protected virtual void _(Events.CacheAttached<POLine.planID> e) { }
	}
}
