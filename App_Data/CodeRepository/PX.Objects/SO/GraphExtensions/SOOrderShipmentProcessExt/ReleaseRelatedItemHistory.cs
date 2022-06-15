using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderShipmentProcessExt
{
    public class ReleaseRelatedItemHistory: ReleaseRelatedItemHistory<SOOrderShipmentProcess>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>();

        /// <summary>
        /// Overrides <see cref="SOOrderShipmentProcess.OnInvoiceReleased(ARRegister, List{PXResult{SOOrderShipment, SOOrder}})"/>
        /// </summary>
        [PXOverride]
        public virtual void OnInvoiceReleased(ARRegister ardoc, List<PXResult<SOOrderShipment, SOOrder>> orderShipments, Action<ARRegister, List<PXResult<SOOrderShipment, SOOrder>>> baseImpl)
        {
            baseImpl(ardoc, orderShipments);

            if (PXAccess.FeatureInstalled<FeaturesSet.advancedSOInvoices>())
                ReleaseRelatedItemHistoryFromInvoice(ardoc);

            if (orderShipments.Any())
                ReleaseRelatedItemHistoryFromOrder(ardoc);

            if (Base.IsDirty)
                Base.Save.Press();
        }
    }
}
