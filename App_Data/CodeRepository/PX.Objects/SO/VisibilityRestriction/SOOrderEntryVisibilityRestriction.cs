using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Formula;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO
{
	public class SOOrderEntryVisibilityRestriction : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		public delegate void CopyPasteGetScriptDelegate(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers);

		[PXOverride]
		public void CopyPasteGetScript(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers,
			CopyPasteGetScriptDelegate baseMethod)
		{
			baseMethod.Invoke(isImportSimple, script, containers);

			Common.Utilities.SetFieldCommandToTheTop(
				script, containers, nameof(Base.CurrentDocument), nameof(SOOrder.BranchID));
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public void _(Events.CacheAttached<SOLine.vendorID> e)
		{
		}
	}


	public sealed class SOOrderVisibilityRestriction : PXCacheExtension<SOOrder>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BranchID
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
				Case<Where<IsCopyPasteContext, Equal<True>, And<Current2<SOOrder.branchID>, IsNotNull>>, Current2<SOOrder.branchID>,
				Case<Where<SOOrder.customerLocationID, IsNotNull,
						And<Selector<SOOrder.customerLocationID, Location.cBranchID>, IsNotNull>>,
					Selector<SOOrder.customerLocationID, Location.cBranchID>,
				Case<Where<SOOrder.customerID, IsNotNull,
						And<Selector<SOOrder.customerID, Customer.cOrgBAccountID>, IsNotNull,
						And<Not<Selector<SOOrder.customerID, Customer.cOrgBAccountID>, RestrictByBranch<Current2<SOOrder.branchID>>>>>>,
					Null,
				Case<Where<Current2<SOOrder.branchID>, IsNotNull>,
					Current2<SOOrder.branchID>>>>>,
				Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(SOOrder.branchID))]
		public int? CustomerID { get; set; }
		#endregion
	}
}
