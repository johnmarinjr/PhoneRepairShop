using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Attributes;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.PO;
using PX.Objects.PO.DAC.Unbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.POOrderEntryExt
{
	public class MultipleBaseCurrencyExt :
		MultipleBaseCurrencyExtBase<POOrderEntry, POOrder, POLine,
			POOrder.branchID, POLine.branchID, POLine.siteID>
	{
		#region DAC extensions

		public sealed class POOrderMultipleBaseCurrenciesRestriction : PXCacheExtension<POOrderVisibilityRestriction, POOrder>
		{
			public static bool IsActive()
				=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

			#region ShipToBAccountID
			[PXMergeAttributes(Method = MergeMethod.Append)]
			[PXRestrictor(typeof(Where<Current2<POOrder.branchID>, IsNull,
				Or<Customer.baseCuryID, EqualBaseCuryID<Current2<POOrder.branchID>>,
				Or<Customer.baseCuryID, IsNull,
				Or<Current<POOrder.shipDestType>, NotEqual<POShippingDestination.customer>,
				Or<Current<POOrder.orderType>, NotEqual<POOrderType.dropShip>>>>>>),
				Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Customer.cOrgBAccountID), typeof(Customer.acctCD))]
			public int? ShipToBAccountID { get; set; }
			#endregion
		}

		#endregion // DAC extensions

		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		#region Event handlers
		#region CacheAttached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(POOrder.branchID))]
		protected virtual void _(Events.CacheAttached<POLine.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(POOrder.branchID))]
		protected virtual void _(Events.CacheAttached<EntryHeader.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<POOrder.branchID>, IsNull,
			Or<Customer.baseCuryID, EqualBaseCuryID<Current2<POOrder.branchID>>,
			Or<Customer.baseCuryID, IsNull>>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Customer.cOrgBAccountID), typeof(Customer.acctCD))]
		protected virtual void _(Events.CacheAttached<CreateSOOrderFilter.customerID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<POOrder.branchID>, IsNull,
			Or<Vendor.baseCuryID, EqualBaseCuryID<Current2<POOrder.branchID>>,
			Or<Vendor.baseCuryID, IsNull>>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Vendor.vOrgBAccountID), typeof(Vendor.acctCD))]
		protected virtual void _(Events.CacheAttached<POOrder.vendorID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Vendor.baseCuryID, EqualBaseCuryID<Current2<POOrder.branchID>>,
			Or<Vendor.baseCuryID, IsNull>>),
			Messages.CustomerOrVendorHasDifferentBaseCurrency, typeof(Vendor.vOrgBAccountID), typeof(Vendor.acctCD))]
		protected virtual void _(Events.CacheAttached<POOrder.payToVendorID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(POOrder.branchID))]
		protected virtual void _(Events.CacheAttached<POSiteStatusFilter.siteID> e)
		{
		}

		#endregion // CacheAttached

		protected virtual void _(Events.RowPersisting<POOrder> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<POOrder.branchID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<POOrder.payToVendorID>(e.Row);
		}

		#endregion // Event handlers

		#region Overrides

		protected override void OnLineBaseCuryChanged(PXCache cache, POLine row)
		{
			base.OnLineBaseCuryChanged(cache, row);
			cache.SetDefaultExt<POLine.curyUnitCost>(row);
		}

		protected override PXSelectBase<POLine> GetTransactionView()
			=> Base.Transactions;

		#endregion // Overrides
	}
}
