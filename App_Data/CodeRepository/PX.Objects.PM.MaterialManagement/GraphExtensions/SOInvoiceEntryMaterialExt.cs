using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.PM.MaterialManagement
{
    public class SOInvoiceEntryMaterialExt : PXGraphExtension<SOInvoiceEntry>
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
        [ARTranForProjectPlanID(typeof(ARRegister.noteID), typeof(ARRegister.hold))]
        protected virtual void _(Events.CacheAttached<ARTran.planID> e) { }

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
        }
    }
}
