using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;

namespace PX.Objects.PM.MaterialManagement
{
    public class POReceiptEntryMaterialExt : PXGraphExtension<
		PO.GraphExtensions.POReceiptEntryExt.UpdatePOOnRelease,
		POReceiptEntry.MultiCurrency,
		POReceiptEntry>
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
        [POReceiptLineSplitForProjectPlanID(typeof(POReceipt.noteID), typeof(POReceipt.hold), typeof(POReceipt.receiptDate))]
        protected virtual void _(Events.CacheAttached<POReceiptLineSplit.planID> e){}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[POLineRForProjectPlanID(typeof(POOrder.noteID), typeof(POOrder.hold))]
		protected virtual void _(Events.CacheAttached<POLineUOpen.planID> e)
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

		[PXOverride]
		public virtual IN.INIssueEntry CreateIssueEntry(Func<IN.INIssueEntry> baseMethod)
        {
			var ie = baseMethod();

			ie.FieldDefaulting.AddHandler<PMSiteStatusAccum.negAvailQty>((sender, e) =>
			{
				INItemClass itemclass = INItemClass.PK.Find(sender.Graph, ((PMSiteStatusAccum)e.Row)?.ItemClassID);
				e.NewValue = itemclass != null && itemclass.NegQty == true;
				e.Cancel = true;
			});

			ie.FieldDefaulting.AddHandler<PMSiteSummaryStatusAccum.negAvailQty>((sender, e) =>
			{
				INItemClass itemclass = INItemClass.PK.Find(sender.Graph, ((PMSiteSummaryStatusAccum)e.Row)?.ItemClassID);
				e.NewValue = itemclass != null && itemclass.NegQty == true;
				e.Cancel = true;
			});

			return ie;
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
