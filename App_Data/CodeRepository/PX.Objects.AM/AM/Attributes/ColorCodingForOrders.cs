using PX.Data;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Color coding for Production Schedule Board. Production Schedule part.
    /// </summary>
    public class ColorCodingForOrders
    {
        public const string ByOrderType = "OT";
        public const string WorkCenter = "WC";
        public const string Status = "ST";
        public const string DispatchPriority = "DP";
        public const string FirmSchedule = "FS";

        #pragma warning disable
        public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute()
                : base(
                    new string[] {
	                    ByOrderType,
	                    WorkCenter,
	                    Status,
	                    DispatchPriority,
	                    FirmSchedule
                    },
                    new string[] {
                        Messages.ByOrderTypeColorCoding,
                        Messages.WorkCenterColorCoding,
                        Messages.StatusColorCoding,
                        Messages.DispatchPriorityColorCoding,
                        Messages.FirmScheduleColorCoding}) { }
        }
    }
}