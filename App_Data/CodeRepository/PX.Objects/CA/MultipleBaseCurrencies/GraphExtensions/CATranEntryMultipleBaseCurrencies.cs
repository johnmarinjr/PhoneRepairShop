using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.CA.MultipleBaseCurrencies.GraphExtensions
{
	public class CATranEntryMultipleBaseCurrencies : PXGraphExtension<CATranEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<CAAdj.cashAccountID> e)
		{
			if (e.NewValue == null)
				return;

			CashAccount cahsAccount = PXSelectorAttribute.Select<CAAdj.cashAccountID>(e.Cache, e.Row, e.NewValue) as CashAccount;
			Branch branch = PXSelect<Branch, Where<Branch.branchID, Equal<AccessInfo.branchID.FromCurrent>>>.Select(Base);

			if (cahsAccount != null && branch != null && cahsAccount.BaseCuryID != branch.BaseCuryID)
			{
				e.NewValue = cahsAccount.CashAccountCD;
				throw new PXSetPropertyException(Messages.CashAccountBaseCurrencyDiffersCurrentBranch,
					PXAccess.GetBranchCD(cahsAccount.BranchID),
					cahsAccount.CashAccountCD);
			}
		}
	}
}
