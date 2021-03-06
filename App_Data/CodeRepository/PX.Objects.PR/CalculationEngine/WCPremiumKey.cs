using PX.Data;
using PX.Objects.PR;
using System;

namespace PX.Objects.PR
{
	[PXHidden]
	public partial class PRCalculationEngine : PXGraph<PRCalculationEngine>
	{
		protected struct WCPremiumKey
		{
			public WCPremiumKey(string workCodeID, string state, int? branchID)
			{
				this.WorkCodeID = workCodeID;
				this.State = state;
				this.BranchID = branchID;
			}

			public string WorkCodeID;
			public string State;
			public int? BranchID;
		}
	}
}
