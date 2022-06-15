using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
    public interface ISubstitutableDocument
    {
        int? CustomerID { get; set; }

        string CuryID { get; set; }

        bool? SuggestRelatedItems { get; set; }
    }
}
