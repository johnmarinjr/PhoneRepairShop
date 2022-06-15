using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FilterProductCategories : Filter
    {
        [Description("parent_id")]
        public int? ParentId { get; set; }

        [Description("name")]
        public string Name { get; set; }

        [Description("is_visible")]
        public int? IsVisible { get; set; }
    }

}
