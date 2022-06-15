using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;

namespace PX.Objects.AR
{
	public sealed class ARTranMultipleBaseCurrencies : PXCacheExtension<ARTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchBaseCuryID
		public new abstract class branchBaseCuryID : PX.Data.BQL.BqlString.Field<branchBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<ARTran.branchID, Branch.baseCuryID>))]
		public string BranchBaseCuryID { get; set; }
		#endregion

		#region CuryInventoryID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<InventoryItemCurySettings.inventoryID,
			Where<InventoryItemCurySettings.inventoryID, Equal<ARTran.inventoryID.FromCurrent>,
				And<InventoryItemCurySettings.curyID, Equal<branchBaseCuryID.FromCurrent>>>>),
			ValidateValue = false)]
		public int? CuryInventoryID { get; set; }
		#endregion
	}
}
