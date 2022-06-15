using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Attributes;
using PX.Objects.PO;
using PX.Objects.PO.DAC.Unbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.POReceiptEntryExt
{
	public class MultipleBaseCurrencyExt :
		MultipleBaseCurrencyExtBase<POReceiptEntry, POReceipt, POReceiptLine,
			POReceipt.branchID, POReceiptLine.branchID, POReceiptLine.siteID>
	{
		#region DAC extensions

		public sealed class POReceiptMultipleBaseCurrenciesRestriction : PXCacheExtension<POReceiptVisibilityRestriction, POReceipt>
		{
			public static bool IsActive()
				=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

			#region BranchBaseCuryID
			public abstract class branchBaseCuryID : Data.BQL.BqlString.Field<branchBaseCuryID> { }

			[PXString(5, IsUnicode = true)]
			[PXFormula(typeof(Selector<POReceipt.branchID, Branch.baseCuryID>))]
			[PXUIVisible(typeof(Where<POReceipt.receiptType.IsEqual<POReceiptType.transferreceipt>>))]
			[PXUIField(DisplayName = "Base Currency", Enabled = false, FieldClass = nameof(FeaturesSet.MultipleBaseCurrencies))]
			public string BranchBaseCuryID { get; set; }
			#endregion
		}

		#endregion // DAC extensions

		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		#region Event handlers
		#region CacheAttached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(POReceipt.branchID))]
		protected virtual void _(Events.CacheAttached<POReceiptLine.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<POReceipt.branchID>, IsNull,
			Or<Current2<POReceipt.receiptType>, Equal<POReceiptType.transferreceipt>,
			Or<Vendor.baseCuryID, EqualBaseCuryID<Current2<POReceipt.branchID>>,
			Or<Vendor.baseCuryID, IsNull>>>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Vendor.vOrgBAccountID), typeof(Vendor.acctCD))]
		protected virtual void _(Events.CacheAttached<POReceipt.vendorID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(where: typeof(Where<Current2<POReceipt.branchID>, IsNull,
			Or<Current2<POReceipt.receiptType>, NotEqual<POReceiptType.transferreceipt>,
			Or<INSite.baseCuryID, EqualBaseCuryID<Current2<POReceipt.branchID>>>>>),
			branchField: typeof(POReceipt.branchID))]
		protected virtual void _(Events.CacheAttached<POReceipt.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(POReceipt.branchID))]
		protected virtual void _(Events.CacheAttached<POReceiptEntry.POOrderFilter.shipFromSiteID> e)
		{
		}

		#endregion // CacheAttached

		protected virtual void _(Events.RowSelected<POReceipt> e)
		{
			PXUIFieldAttribute.SetEnabled<POReceiptMultipleBaseCurrenciesRestriction.branchBaseCuryID>(e.Cache, null, false);
		}

		protected virtual void _(Events.RowPersisting<POReceipt> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<POReceipt.branchID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<POReceipt.siteID>(e.Row);
		}

		#endregion // Event handlers

		#region Overrides

		protected override void OnDocumentBaseCuryChanged(PXCache cache, POReceipt row)
		{
			base.OnDocumentBaseCuryChanged(cache, row);
			cache.VerifyFieldAndRaiseException<POReceipt.siteID>(row);
		}

		protected override PXSelectBase<POReceiptLine> GetTransactionView()
			=> Base.transactions;

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.InsertNewLandedCostDoc"/>
		/// </summary>
		[PXOverride]
		public virtual void InsertNewLandedCostDoc(POLandedCostDocEntry lcGraph, POReceipt receipt,
			Action<POLandedCostDocEntry, POReceipt> baseMethod)
		{
			baseMethod.Invoke(lcGraph, receipt);
			lcGraph.Document.Current.BranchID = receipt.BranchID;
			lcGraph.Document.Current.CuryID = Branch.PK.Find(Base, receipt.BranchID).BaseCuryID;
			lcGraph.Document.UpdateCurrent();
		}

		#endregion // Overrides
	}
}
