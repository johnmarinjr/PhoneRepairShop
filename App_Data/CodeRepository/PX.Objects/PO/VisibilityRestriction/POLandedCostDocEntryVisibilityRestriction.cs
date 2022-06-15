using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PO.LandedCosts;
using System.Collections.Generic;

namespace PX.Objects.PO
{
	public class POLandedCostDocEntryVisibilityRestriction: PXGraphExtension<POLandedCostDocEntry>
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
				script, containers, nameof(Base.CurrentDocument), nameof(POLandedCostDoc.BranchID));
		}

		public override void Initialize()
		{
			base.Initialize();

			Base.poReceiptSelectionView.Join<InnerJoin<BAccount2, On<POReceipt.vendorID.IsEqual<BAccount2.bAccountID>>>>();
			Base.poReceiptSelectionView.WhereAnd<Where<BAccount2.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();

			Base.poReceiptLinesSelectionView.Join<InnerJoin<BAccount2, On<POReceiptLineAdd.vendorID.IsEqual<BAccount2.bAccountID>>>>();
			Base.poReceiptLinesSelectionView.WhereAnd<Where<BAccount2.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
		}
	}
}
