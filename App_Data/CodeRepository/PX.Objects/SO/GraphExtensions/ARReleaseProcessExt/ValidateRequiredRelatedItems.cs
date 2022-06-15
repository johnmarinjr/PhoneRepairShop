using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.DR;
using PX.Objects.GL;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;
using PX.Objects.TX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.ARReleaseProcessExt
{
    public class ValidateRequiredRelatedItems : ValidateRequiredRelatedItems<ARReleaseProcess, SOInvoice, ARTran>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>() && PXAccess.FeatureInstalled<FeaturesSet.advancedSOInvoices>();

        public delegate void ReleaseInvoiceTransactionPostProcessingHandler(JournalEntry je, ARInvoice ardoc, PXResult<ARTran, ARTax, Tax, DRDeferredCode, SOOrderType, ARTaxTran> r, GLTran tran);

        /// <summary>
        /// Overrides <see cref="ARReleaseProcess.ReleaseInvoiceTransactionPostProcessing(JournalEntry, ARInvoice, PXResult{ARTran, ARTax, Tax, DRDeferredCode, SOOrderType, ARTaxTran}, GLTran)"/>
        /// </summary>
        [PXOverride]
        public virtual void ReleaseInvoiceTransactionPostProcessing(
            JournalEntry je,
            ARInvoice ardoc, 
            PXResult<ARTran, ARTax, TX.Tax, DRDeferredCode, SOOrderType, ARTaxTran> r, 
            GLTran tran,
            ReleaseInvoiceTransactionPostProcessingHandler baseImpl)
        {
            if (ardoc.OrigModule == BatchModule.SO)
            {
                if (!Validate(r))
                    return;
            }

            baseImpl(je, ardoc, r, tran);
        }

        public override void ThrowError()
        {
            if (IsMassProcessing)
                throw new PXException(IN.RelatedItems.Messages.InvoiceCannotBeReleasedOnProcessingScreen);
            throw new PXException(IN.RelatedItems.Messages.InvoiceCannotBeReleased);
        }
    }
}
