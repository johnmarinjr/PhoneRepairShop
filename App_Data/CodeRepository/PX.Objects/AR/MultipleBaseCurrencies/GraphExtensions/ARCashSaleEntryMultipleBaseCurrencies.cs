using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using ARCashSale = PX.Objects.AR.Standalone.ARCashSale;

namespace PX.Objects.AR
{
	public sealed class ARCashSaleEntryMultipleBaseCurrencies : PXGraphExtension<ARCashSaleEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXRestrictor(typeof(Where<Customer.baseCuryID, IsNull, 
			Or<Customer.baseCuryID, EqualBaseCuryID<ARCashSale.branchID.FromCurrent>>>),
			"",
		SuppressVerify = false
		)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public void _(Events.CacheAttached<ARCashSale.customerID> e) { }

		protected void _(Events.FieldVerifying<ARCashSale.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<ARCashSale.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			BAccountR customer = PXSelectorAttribute.Select<ARCashSale.customerID>(e.Cache, e.Row) as BAccountR;

			if (branch != null && customer != null
				&& customer.BaseCuryID != null && branch.BaseCuryID != customer.BaseCuryID)
			{
				e.NewValue = branch.BranchCD;
				throw new PXSetPropertyException(Messages.BranchCustomerDifferentBaseCury, PXOrgAccess.GetCD(customer.COrgBAccountID), customer.AcctCD);
			}
		}
	}
}
