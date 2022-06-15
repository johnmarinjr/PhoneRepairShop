using System;
using PX.Data;
using PX.Objects.EP;

namespace PX.Objects.SO
{
	public class SOOrderApprovalAutomation : EPApprovalAutomation<SOOrder, SOOrder.approved, SOOrder.rejected, SOOrder.hold, SOSetupApproval>
	{
		public SOOrderApprovalAutomation(PXGraph graph, Delegate handler) : base(graph, handler) { }

		public SOOrderApprovalAutomation(PXGraph graph) : base(graph) { }

		protected override bool AllowAssign(PXCache cache, SOOrder oldDoc, SOOrder doc)
		{
			var oldHold = oldDoc.Hold == true;
			var oldCancelled = oldDoc.Cancelled == true;
			var cancelled = doc.Cancelled == true;

			if (oldHold)
			{
				if (cancelled)
					return false;//Hold -> Cancelled
				return true;
			}

			return oldCancelled && !cancelled; //Cancelled -> Open(Pending Approval and other)
		}
	}
}
