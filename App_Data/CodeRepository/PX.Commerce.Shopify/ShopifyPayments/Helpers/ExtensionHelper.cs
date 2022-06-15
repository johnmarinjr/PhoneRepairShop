using PX.Data;
using PX.Objects.CA;

namespace PX.Commerce.Shopify.ShopifyPayments
{
    public static class ExtensionHelper
    {
        public static bool IsShopifyPaymentsPlugin(PXGraph graph, string processingCenterID)
        {
            CCProcessingCenter processingCenter = CCProcessingCenter.PK.Find(graph, processingCenterID);
            return IsShopifyPaymentsPlugin(processingCenter);
        }

        public static bool IsShopifyPaymentsPlugin(CCProcessingCenter row) => IsShopifyPaymentsPlugin(row?.ProcessingTypeName);

        public static bool IsShopifyPaymentsPlugin(string processingTypeName) => (processingTypeName == typeof(ShopifyPaymentsProcessingPlugin).FullName);
    }
}
