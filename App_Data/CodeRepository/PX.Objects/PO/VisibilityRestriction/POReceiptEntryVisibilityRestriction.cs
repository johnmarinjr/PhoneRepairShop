using PX.Data;
using PX.Objects.AP;
using PX.Objects.Common.Formula;
using PX.Objects.CR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using System.Collections.Generic;

namespace PX.Objects.PO
{
	public class POReceiptEntryVisibilityRestriction : PXGraphExtension<POReceiptEntry>
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
				script, containers, nameof(Base.CurrentDocument), nameof(POReceipt.BranchID));
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
			Case<Where<IsCopyPasteContext, Equal<True>, And<Current2<POReceipt.branchID>, IsNotNull>>, Current2<POReceipt.branchID>,
			Case<Where<POReceipt.receiptType, Equal<POReceiptType.transferreceipt>>,
				Selector<POReceipt.siteID, INSite.branchID>,
			Case<Where<POReceipt.vendorLocationID, IsNotNull,
					And<Selector<POReceipt.vendorLocationID, Location.vBranchID>, IsNotNull>>,
				Selector<POReceipt.vendorLocationID, Location.vBranchID>,
			Case<Where<POReceipt.vendorID, IsNotNull,
					And<Not<Selector<POReceipt.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<POReceipt.branchID>>>>>,
				Null,
			Case<Where<Current2<POReceipt.branchID>, IsNotNull>,
				Current2<POReceipt.branchID>>>>>>,
			Current<AccessInfo.branchID>>))]
		public virtual void POReceipt_BranchID_CacheAttached(PXCache sender)
		{
		}
	}
}
