using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FilterProductVariants : Filter
    {
        [Description("product_id")]
        public int? ProductId { get; set; }

        [Description("variant_id")]
        public int? VariantId { get; set; }
    }
}
