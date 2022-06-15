using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
    public interface ISubstitutableLine
    {
        int? LineNbr { get; set; }

        int? BranchID { get; set; }

        int? CustomerID { get; set; }

        int? SiteID { get; set; }

        int? InventoryID { get; set; }

        int? SubItemID { get; set; }

        string UOM { get; set; }

        decimal? Qty { get; set; }

        decimal? BaseQty { get; set; }

        decimal? UnitPrice { get; set; }

        decimal? CuryUnitPrice { get; set; }

		decimal? CuryExtPrice { get; set; }

        bool? ManualPrice { get; set; }

        bool? SubstitutionRequired { get; set; }
    }
}
