using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOShipmentEntryExt
{
    public class ValidateRequiredRelatedItems: ValidateRequiredRelatedItems<SOShipmentEntry, SOOrder, SOLine>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>();

        /// <summary>
        /// Overrides <see cref="SOShipmentEntry.ValidateLineBeforeShipment"/>
        /// </summary>
        [PXOverride]
        public virtual bool ValidateLineBeforeShipment(SOLine line, Func<SOLine, bool> baseImpl)
        {
            if (!Validate(line))
                return false;

            return baseImpl(line);
        }

        public override void ThrowError() 
        {
            if (IsMassProcessing)
                throw new PXException(IN.RelatedItems.Messages.ShipmentCannotBeCreatedOnProcessingScreen);
            throw new PXException(IN.RelatedItems.Messages.ShipmentCannotBeCreated);
        }
    }
}
