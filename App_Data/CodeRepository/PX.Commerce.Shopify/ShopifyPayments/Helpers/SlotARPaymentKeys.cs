using PX.Common;

namespace PX.Commerce.Shopify.ShopifyPayments.Extensions
{
    public class SlotARPaymentKeys
    {
        protected string _DocType;
        protected string _DocRefNbr;

        public SlotARPaymentKeys(string docType, string docRefNbr)
        {
            _DocType = docType;
            _DocRefNbr = docRefNbr;

            PXContext.SetSlot<SlotARPaymentKeys>(this);
        }

        public static void SaveKeys(string docType, string docRefNbr)
        {
            new SlotARPaymentKeys(docType, docRefNbr);
        }

        public static void GetKeys(out string docType, out string docRefNbr, bool clearSlot)
        {
            SlotARPaymentKeys slot = PXContext.GetSlot<SlotARPaymentKeys>();

            if (slot == null)
            {
                docType = null;
                docRefNbr = null;

                return;
            }

            docType = slot._DocType;
            docRefNbr = slot._DocRefNbr;

            if (clearSlot == true
                    && (docType != null || docRefNbr != null))
            {
                SaveKeys(null, null);
            }
        }
    }
}
