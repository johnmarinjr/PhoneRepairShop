using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
    public interface ISubstitutableLineExt
    {
        bool? SuggestRelatedItems { get; set; }

        string RelatedItems { get; set; }

        int? RelatedItemsRelation { get; set; }

        int? RelatedItemsRequired { get; set; }

        int? HistoryLineID { get; set; }
    }
}
