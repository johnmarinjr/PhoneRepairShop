using System;
using System.Linq;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.GL.Reclassification.UI
{
	public class ReclassifyTransactionsProcessMultipleBaseCurrencies : PXGraphExtension<ReclassifyTransactionsProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected void _(Events.FieldUpdated<ReclassifyTransactionsProcess.ReplaceOptions, ReclassifyTransactionsProcess.ReplaceOptions.withBranchID> e)
		{
			ReclassifyTransactionsProcess.ReplaceOptions row = e.Row;
			bool hasSameBaseCurrency = PXSelect<Branch, Where<Branch.baseCuryID, EqualBaseCuryID<ReclassifyTransactionsProcess.ReplaceOptions.withBranchID.FromCurrent>,
				And<Branch.branchID.IsEqual<ReclassifyTransactionsProcess.ReplaceOptions.newBranchID.FromCurrent>>>>.SelectSingleBound(Base, null).Any();

			if (!hasSameBaseCurrency)
			{
				e.Cache.SetValueExt<ReclassifyTransactionsProcess.ReplaceOptions.newBranchID>(row, null);
			}
		}
	}
}
