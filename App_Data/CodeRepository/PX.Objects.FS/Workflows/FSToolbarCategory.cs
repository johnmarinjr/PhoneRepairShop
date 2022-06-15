using PX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.FS
{
    public static class FSToolbarCategory
    {
        public const string CorrectionCategoryID = "Corrections Category";
        public const string SchedulingCategoryID = "Scheduling Category";
        public const string ReplenishmentCategoryID = "Replenishment Category";
        public const string InquiriesCategoryID = "Inquiries Category";
        public const string TravelingID = "Traveling Category";

        [PXLocalizable]
        public static class CategoryNames
        {
            public const string Corrections = "Corrections";
            public const string Scheduling = "Scheduling";
            public const string Replenishment = "Replenishment";
            public const string Inquiries = "Inquiries";
            public const string Traveling = "Traveling";
        }
    }
}
