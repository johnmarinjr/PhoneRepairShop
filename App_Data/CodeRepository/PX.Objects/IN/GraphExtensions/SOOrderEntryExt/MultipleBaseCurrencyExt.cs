using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Attributes;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.SOOrderEntryExt
{
	public class MultipleBaseCurrencyExt :
		MultipleBaseCurrencyExtBase<SOOrderEntry, SOOrder, SOLine,
			SOOrder.branchID, SOLine.branchID, SOLine.siteID>
	{
		#region DAC extensions

		public sealed class SOOrderMultipleBaseCurrenciesRestriction : PXCacheExtension<SOOrderVisibilityRestriction, SOOrder>
		{
			public static bool IsActive()
				=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

			#region BranchBaseCuryID
			public abstract class branchBaseCuryID : Data.BQL.BqlString.Field<branchBaseCuryID> { }

			[PXString(5, IsUnicode = true)]
			[PXFormula(typeof(Selector<SOOrder.branchID, Branch.baseCuryID>))]
			[PXUIVisible(typeof(Where<SOOrder.behavior.IsEqual<SOBehavior.tR>.And<SOOrder.aRDocType.IsEqual<ARDocType.noUpdate>>>))]
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
		[RestrictSiteByBranch(typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<SOLineSplit.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<SOLine.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<SOLine.pOSiteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(where: typeof(Where<Current2<SOOrder.branchID>, IsNull,
			Or<Current2<SOOrder.behavior>, NotEqual<SOBehavior.tR>,
			Or<Current2<SOOrder.aRDocType>, NotEqual<ARDocType.noUpdate>,
			Or<INSite.baseCuryID, EqualBaseCuryID<Current2<SOOrder.branchID>>>>>>),
			branchField: typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<SOOrder.destinationSiteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<EntryHeader.siteID> e)
		{
		}

		// We need this FieldVerifying event handler because the SelectorFieldVerifying method of
		// BranchScopeDimensionSelector class contains "Cancel = true", the restrictor doesn't work.
		// TODO: Replace this code with a better solution.
		protected virtual void _(Events.FieldVerifying<SOOrder, SOOrder.destinationSiteID> e)
		{
			e.Cache.GetAttributesReadonly<SOOrder.destinationSiteID>()
				.OfType<PXRestrictorAttribute>()
				.ForEach((r,i) => r.FieldVerifying(e.Cache, e.Args));
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<SOOrder.branchID>, IsNull,
			Or2<Where<Current2<SOOrder.behavior>, Equal<SOBehavior.tR>, And<Current2<SOOrder.aRDocType>, Equal<ARDocType.noUpdate>>>,
			Or<Customer.baseCuryID, EqualBaseCuryID<Current2<SOOrder.branchID>>,
			Or<Customer.baseCuryID, IsNull>>>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Customer.cOrgBAccountID), typeof(Customer.acctCD))]
		protected virtual void _(Events.CacheAttached<SOOrder.customerID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<SOOrder.branchID>, IsNull,
			Or<Vendor.baseCuryID, EqualBaseCuryID<Current2<SOOrder.branchID>>,
			Or<Vendor.baseCuryID, IsNull>>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Vendor.vOrgBAccountID), typeof(Vendor.acctCD))]
		protected virtual void _(Events.CacheAttached<SOLine.vendorID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(SOOrder.branchID))]
		protected virtual void _(Events.CacheAttached<SOSiteStatusFilter.siteID> e)
		{
		}

		#endregion // CacheAttached

		protected virtual void _(Events.RowSelected<SOOrder> e)
		{
			PXUIFieldAttribute.SetEnabled<SOOrderMultipleBaseCurrenciesRestriction.branchBaseCuryID>(e.Cache, null, false);
		}

		protected virtual void _(Events.FieldUpdated<SOOrder, SOOrder.branchID> e)
		{
			bool resetCuryID = !Base.IsCopyPasteContext &&
				((e.Row?.CustomerID == null && e.ExternalCall) || e.Row?.IsTransferOrder == true);

			SetDefaultBaseCurrency<SOOrder.curyID, SOOrder.curyInfoID, SOOrder.orderDate>(e.Cache, e.Row, resetCuryID);
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyID> e,
			PXFieldDefaulting baseMethod)
		{
			var doc = Base.Document.Current;

			if (doc?.IsTransferOrder == true && !string.IsNullOrEmpty(e.Row?.BaseCuryID))
			{
				e.NewValue = e.Row.BaseCuryID;
				e.Cancel = true;
				return;
			}

			if (doc?.BranchID != null && !Base.IsCopyOrder &&
				(doc.CustomerID == null || Customer.PK.Find(Base, doc.CustomerID)?.CuryID == null))
			{
				e.NewValue = Branch.PK.Find(Base, doc.BranchID)?.BaseCuryID;
				e.Cancel = (e.NewValue != null);
			}

			baseMethod?.Invoke(e.Cache, e.Args);
		}

		protected virtual void _(Events.RowPersisting<SOOrder> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<SOOrder.branchID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<SOOrder.destinationSiteID>(e.Row);
		}

		protected virtual void _(Events.RowPersisting<SOLineSplit> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<SOLineSplit.siteID>(e.Row);
		}

		#endregion // Event handlers

		#region Overrides

		protected override void OnDocumentBaseCuryChanged(PXCache cache, SOOrder row)
		{
			base.OnDocumentBaseCuryChanged(cache, row);

			cache.VerifyFieldAndRaiseException<SOOrder.customerID>(row);
			cache.VerifyFieldAndRaiseException<SOOrder.destinationSiteID>(row);

			foreach (SOLineSplit tran in Base.splits.Select())
			{
				var splitCache = Base.splits.Cache;
				splitCache.MarkUpdated(tran);
				splitCache.VerifyFieldAndRaiseException<SOLineSplit.siteID>(tran);
			}
		}

		protected override void OnLineBaseCuryChanged(PXCache cache, SOLine row)
		{
			base.OnLineBaseCuryChanged(cache, row);
			cache.SetDefaultExt<SOLine.curyUnitCost>(row);
			cache.VerifyFieldAndRaiseException<SOLine.vendorID>(row);
			cache.VerifyFieldAndRaiseException<SOLine.pOSiteID>(row);
		}

		protected override PXSelectBase<SOLine> GetTransactionView()
			=> Base.Transactions;

		protected override void _(Events.RowPersisting<SOLine> e)
		{
			base._(e);

			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<SOLine.vendorID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<SOLine.pOSiteID>(e.Row);
		}

		#endregion // Overrides
	}
}
