using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.PM.MaterialManagement
{
    public class SOOrderEntryMaterialExt : PXGraphExtension<SOOrderEntry>
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

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBLong(IsImmutable = true)]
		[SOLineSplitForProjectPlanID(typeof(SOOrder.noteID), typeof(SOOrder.hold), typeof(SOOrder.orderDate))]
		protected virtual void SOLineSplit_PlanID_CacheAttached(PXCache sender)
		{
		}

		public static bool IsActive()
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
	}
}
