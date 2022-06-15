using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Attributes;

namespace PX.Objects.IN.GraphExtensions.INRegisterEntryBaseExt
{
	public class MultipleBaseCurrencyExt :
		MultipleBaseCurrencyExtBase<INRegisterEntryBase, INRegister, INTran,
			INRegister.branchID, INTran.branchID, INTran.siteID>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		#region Event handlers
		#region CacheAttached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(INRegister.branchID))]
		protected virtual void _(Events.CacheAttached<INTran.siteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(INRegister.branchID))]
		protected virtual void _(Events.CacheAttached<INRegister.toSiteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(INRegister.branchID))]
		protected virtual void _(Events.CacheAttached<INSiteStatusFilter.siteID> e)
		{
		}

		#endregion // CacheAttached

		protected virtual void _(Events.RowSelected<INRegister> e)
			=> PXUIFieldAttribute.SetEnabled<INRegister.branchBaseCuryID>(e.Cache, null, false);

		protected virtual void _(Events.RowPersisting<INRegister> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<INRegister.branchID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<INRegister.siteID>(e.Row);
			e.Cache.VerifyFieldAndRaiseException<INRegister.toSiteID>(e.Row);
		}

		#endregion // Event handlers

		#region Overrides

		protected override void OnLineBaseCuryChanged(PXCache cache, INTran row)
		{
			base.OnLineBaseCuryChanged(cache, row);
			Base.DefaultUnitCost(cache, row, true);
		}

		protected override PXSelectBase<INTran> GetTransactionView()
			=> Base.INTranDataMember;

		#endregion // Overrides
	}
}
