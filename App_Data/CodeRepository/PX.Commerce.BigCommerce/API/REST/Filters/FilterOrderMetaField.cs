using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class FilterOrderMetaField : Filter
    {
        [Description("key")]
        public string Key { get; set; }

        [Description("namespace")]
        public string NameSpace { get; set; }
    }
}
