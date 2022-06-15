using System;
using PX.Data;

namespace PX.Objects.AR.CCPaymentProcessing.Common
{
	public sealed class ClearTransactionCache : IPXCustomInfo
	{
		public void Complete(PXLongRunStatus status, PXGraph graph)
		{
			if (status == PXLongRunStatus.Completed && graph is ARPaymentEntry paymentGraph)
			{
				paymentGraph.ExternalTran.Cache.Clear();
				paymentGraph.ExternalTran.Cache.ClearQueryCache();
			}
		}
	}
}
