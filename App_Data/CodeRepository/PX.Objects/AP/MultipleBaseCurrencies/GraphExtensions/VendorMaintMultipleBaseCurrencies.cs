using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CA;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.AP
{
	public class VendorMaintMultipleBaseCurrencies : PXGraphExtension<VendorMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.RowSelected<Vendor> e)
		{
			if (e.Row == null)
				return;

			Vendor vendor = e.Row;

			PXUIFieldAttribute.SetRequired<Vendor.vOrgBAccountID>(e.Cache,
				PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>()
				&& e.Row.IsBranch == false);

			#region Balances
			var showBalances = true;

			if (e.Row.BaseCuryID == null)
			{
				var availableBranches = CommonServiceLocator.ServiceLocator.Current.GetInstance<ICurrentUserInformationProvider>().GetAllBranches().ToList();
				var currencies = availableBranches.Select(b => PXAccess.GetBranch(b.Id).BaseCuryID).Distinct().ToList();
				showBalances = currencies.Count <= 1;
			}

			PXUIFieldAttribute.SetVisible<VendorMaint.VendorBalanceSummary.balance>(Base.VendorBalance.Cache, null, showBalances);
			PXUIFieldAttribute.SetVisible<VendorMaint.VendorBalanceSummary.depositsBalance>(Base.VendorBalance.Cache, null, showBalances);
			PXUIFieldAttribute.SetVisible<VendorMaint.VendorBalanceSummary.retainageBalance>(Base.VendorBalance.Cache, null, showBalances);

			Base.VendorBalanceByBaseCurrency.AllowSelect = !showBalances;
			#endregion

			#region Restrict Visibility for Vendors, extended from Branch
			PXUIFieldAttribute.SetEnabled<Vendor.vOrgBAccountID>(Base.CurrentVendor.Cache, vendor, true);

			if (vendor.IsBranch == true)
			{
				using (new PXReadBranchRestrictedScope())
				{
					// searching for AP History with different base currencies
					APHistory diffCurrencyAPHistory = SelectFrom<APHistory>
						.InnerJoin<Branch>
							.On<Branch.branchID.IsEqual<APHistory.branchID>>
						.InnerJoin<APHistoryAlias>
							.On<APHistoryAlias.vendorID.IsEqual<APHistory.vendorID>>
						.InnerJoin<BranchAlias>
							.On<BranchAlias.branchID.IsEqual<APHistoryAlias.branchID>
								.And<BranchAlias.baseCuryID.IsNotEqual<Branch.baseCuryID>>>
						.Where<APHistory.vendorID.IsEqual<@P.AsInt>>
						.View
						.SelectSingleBound(Base, null, new object[] { vendor.BAccountID });

					if (diffCurrencyAPHistory != null)
					{
						PXUIFieldAttribute.SetEnabled<Vendor.vOrgBAccountID>(Base.CurrentVendor.Cache, vendor, false);
					}
					else if (vendor.Type == BAccountType.CombinedType)
					{
						// searching for AR History with different base currencies
						ARHistory diffCurrencyARHistory = SelectFrom<ARHistory>
							.InnerJoin<Branch>
								.On<Branch.branchID.IsEqual<ARHistory.branchID>>
							.InnerJoin<ARHistoryAlias>
								.On<ARHistoryAlias.customerID.IsEqual<ARHistory.customerID>>
							.InnerJoin<BranchAlias>
								.On<BranchAlias.branchID.IsEqual<ARHistoryAlias.branchID>
									.And<BranchAlias.baseCuryID.IsNotEqual<Branch.baseCuryID>>>
							.Where<ARHistory.customerID.IsEqual<@P.AsInt>>
							.View
							.SelectSingleBound(Base, null, new object[] { vendor.BAccountID });

						if (diffCurrencyARHistory != null)
						{
							PXUIFieldAttribute.SetEnabled<Vendor.vOrgBAccountID>(Base.CurrentVendor.Cache, vendor, false);
						}
						else
						{
							// searching for combined AP/AR History with different base currencies
							APHistory diffCurrencyAPARHistory =
								SelectFrom<APHistory>
									.InnerJoin<Branch>
										.On<Branch.branchID.IsEqual<APHistory.branchID>>
									.InnerJoin<ARHistory>
										.On<ARHistory.customerID.IsEqual<APHistory.vendorID>>
									.InnerJoin<BranchAlias>
										.On<BranchAlias.branchID.IsEqual<ARHistory.branchID>
											.And<BranchAlias.baseCuryID.IsNotEqual<Branch.baseCuryID>>>
									.Where<APHistory.vendorID.IsEqual<@P.AsInt>>
								.View
								.SelectSingleBound(Base, null, new object[] { vendor.BAccountID });

							if (diffCurrencyAPARHistory != null)
							{
								PXUIFieldAttribute.SetEnabled<Vendor.vOrgBAccountID>(Base.CurrentVendor.Cache, vendor, false);
							}
						}
					}
				}
			}
			#endregion
		}

		protected virtual void _(Events.RowDeleted<Vendor> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.Type == BAccountType.VendorType && e.Row.IsBranch == true)
			{
				BAccountItself baccount = Base.CurrentBAccountItself.SelectSingle(e.Row.BAccountID);
				if (baccount != null)
				{
					baccount.BaseCuryID = null;
					baccount.VOrgBAccountID = 0;
					Base.CurrentBAccountItself.Update(baccount);
				}
			}
		}

		protected virtual void _(Events.RowSelected<VendorMaint.VendorBalanceSummary> e)
		{
			if (e.Row == null)
				return;

			e.Cache
				.GetAttributesReadonly(null)
				.OfType<PX.Objects.CM.CurySymbolAttribute>()
				.ToList()
				.ForEach(attr => attr.SetSymbol(null));

			Vendor vendor = e.Cache.Graph.Caches[typeof(Vendor)].Current as Vendor;

			if (vendor != null && vendor.BaseCuryID == null)
			{
				var availableBranches = CommonServiceLocator.ServiceLocator.Current.GetInstance<ICurrentUserInformationProvider>().GetAllBranches().ToList();
				var currencies = availableBranches.Select(b => PXAccess.GetBranch(b.Id).BaseCuryID).Distinct().ToList();

				if (currencies.Count <= 1)
				{
					var curr = PX.Objects.CM.CurrencyCollection.GetCurrency(currencies.FirstOrDefault());

					e.Cache
						.GetAttributesReadonly(null)
						.OfType<PX.Objects.CM.CurySymbolAttribute>()
						.ToList()
						.ForEach(attr => attr.SetSymbol(curr?.CurySymbol));
				}
			}
		}

		#region VOrgBAccountID

		protected virtual void _(Events.FieldVerifying<Vendor, Vendor.vOrgBAccountID> e)
		{
			if (e.Row == null || (int?)e.NewValue == (int?)e.OldValue) return;

			Vendor vendor = e.Row;

			var newBaseCuryID = PXOrgAccess.GetBaseCuryID((int)e.NewValue);

			if (vendor.BaseCuryID != newBaseCuryID)
			{
				APHistory apHist = null;
				using (new PXReadBranchRestrictedScope())
				{
					apHist = SelectFrom<APHistory>
						.InnerJoin<Branch>
							.On<Branch.branchID.IsEqual<APHistory.branchID>>
						.Where<APHistory.vendorID.IsEqual<@P.AsInt>
							.And<Branch.baseCuryID.IsNotEqual<@P.AsString>>>
						.View
						.SelectSingleBound(Base, null, new object[] { vendor.BAccountID, newBaseCuryID });
				}

				if (apHist != null)
				{
					e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

					var branch = PXAccess.GetBranch(apHist.BranchID);

					throw new PXSetPropertyException(Messages.EntityCannotBeAssociated, PXErrorLevel.Error,
							branch.BaseCuryID,
							vendor.AcctCD.Trim());
				}
				else if (vendor.Type == BAccountType.CombinedType && (int?)e.NewValue != vendor.COrgBAccountID)
				{
					ARHistory arHist = null;
					using (new PXReadBranchRestrictedScope())
					{
						arHist = SelectFrom<ARHistory>
							.InnerJoin<Branch>
								.On<Branch.branchID.IsEqual<ARHistory.branchID>>
							.Where<ARHistory.customerID.IsEqual<@P.AsInt>
								.And<Branch.baseCuryID.IsNotEqual<@P.AsString>>>
							.View
							.SelectSingleBound(Base, null, new object[] { vendor.BAccountID, newBaseCuryID });
					}

					if (arHist != null)
					{
						e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

						var branch = PXAccess.GetBranch(arHist.BranchID);

						if (CS.VisibilityRestriction.IsEmpty(vendor.COrgBAccountID))
						{
							throw new PXSetPropertyException(Messages.EntityCannotBeAssociatedBecauseOfARHistoryNoRestrictForCustomer, PXErrorLevel.Error,
									branch.BaseCuryID,
									vendor.AcctCD.Trim());
						}
						else
						{
							var customerBranch = PXAccess.GetBranchByBAccountID(vendor.COrgBAccountID);
							var customerOrg = PXAccess.GetOrganizationByBAccountID(vendor.COrgBAccountID);

							throw new PXSetPropertyException(Messages.EntityCannotBeAssociatedBecauseOfARHistory, PXErrorLevel.Error,
									branch.BaseCuryID,
									vendor.AcctCD.Trim(),
									customerBranch?.BranchCD?.Trim() ?? customerOrg?.OrganizationCD?.Trim(),
									customerBranch?.BaseCuryID ?? customerOrg?.BaseCuryID);
						}
					}
					else if (CS.VisibilityRestriction.IsNotEmpty(vendor.COrgBAccountID)
						&& (vendor.IsBranch == true || CS.VisibilityRestriction.IsNotEmpty((int?)e.NewValue)))
					{
						var customerBranch = PXAccess.GetBranchByBAccountID(vendor.COrgBAccountID);
						var customerOrg = PXAccess.GetOrganizationByBAccountID(vendor.COrgBAccountID);
						var localizedMsg = PXMessages.LocalizeFormatNoPrefix(Messages.ChangeVisibilityForVendor,
							vendor.AcctCD.Trim(),
							customerBranch?.BranchCD?.Trim() ?? customerOrg?.OrganizationCD?.Trim(),
							customerBranch?.BaseCuryID ?? customerOrg?.BaseCuryID);
						if (Base.BAccount.View.Ask(nameof(Messages.ChangeVisibilityForVendor), localizedMsg, MessageButtons.YesNo) == WebDialogResult.Yes)
						{
							object newValue = e.NewValue;
							e.Cache.RaiseFieldVerifying<Vendor.cOrgBAccountID>(e.Row, ref newValue);
							// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs
							// [This is almost independed field, related to the customers. We have to either modify both fields,
							// or revert changing of the vendor related one, because of complicated logic]
							vendor.COrgBAccountID = (int?)e.NewValue;
						}
						else
						{
							e.NewValue = e.OldValue;
							e.Cancel = true;
							return;
						}
					}
					else if (vendor.IsBranch == true || CS.VisibilityRestriction.IsNotEmpty((int?)e.NewValue))
					{
						var localizedMsg = PXMessages.LocalizeFormatNoPrefix(Messages.SetVisibilityForVendor,
							vendor.AcctCD);
						if (Base.BAccount.View.Ask(nameof(Messages.SetVisibilityForVendor), localizedMsg, MessageButtons.YesNo) == WebDialogResult.Yes)
						{
							object newValue = e.NewValue;
							e.Cache.RaiseFieldVerifying<Vendor.cOrgBAccountID>(e.Row, ref newValue);
							// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs
							// [This is almost independed field, related to the customers. We have to either modify both fields,
							// or revert changing of the vendor related one, because of complicated logic]
							vendor.COrgBAccountID = (int?)e.NewValue;
						}
						else
						{
							e.NewValue = e.OldValue;
							e.Cancel = true;
							return;
						}
					}
				}

				Vendor suppliedByVendor = PXSelect<Vendor,
														Where<Vendor.payToVendorID, Equal<Current<Vendor.bAccountID>>,
															And<Vendor.bAccountID, NotEqual<Current<Vendor.bAccountID>>>>,
														OrderBy<Asc<Vendor.bAccountID>>>
														.SelectSingleBound(Base, new object[] { vendor });

				if (suppliedByVendor != null)
				{
					e.NewValue = PXOrgAccess.GetCD(e.NewValue as int?);
					throw new PXSetPropertyException(Messages.SuppliedByvendorsDiffrentBaseCurrency,
						vendor.BaseCuryID,
						suppliedByVendor.AcctCD,
						PXOrgAccess.GetCD(suppliedByVendor.VOrgBAccountID),
						suppliedByVendor.BaseCuryID);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<Vendor, Vendor.vOrgBAccountID> e)
		{
			if ((int?)e.OldValue == (int?)e.NewValue) return;

			if (!((int?)e.NewValue == CS.VisibilityRestriction.EmptyBAccountID
				&& e.Row.Type == BAccountType.CombinedType
				&& e.Row.COrgBAccountID != CS.VisibilityRestriction.EmptyBAccountID))
			{
				e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.VOrgBAccountID)
				?? ((e.Row.IsBranch == true) ? null : Base.Accessinfo.BaseCuryID);
			}

			object newName = e.Row.PayToVendorID;
			try
			{
				e.Cache.RaiseFieldVerifying<Vendor.payToVendorID>(e.Row, ref newName);
			}
			catch (PXSetPropertyException ex)
			{
				e.Cache.RaiseExceptionHandling<Vendor.payToVendorID>(e.Row, newName, ex);
			}
		}

		#endregion

		public void _(Events.RowUpdated<Vendor> e)
		{
			if (e.OldRow.BaseCuryID != e.Row.BaseCuryID)
			{
				foreach (CRLocation location in Base.GetExtension<VendorMaint.LocationDetailsExt>().Locations.Select())
				{
					location.VCashAccountID = null;
					if (Base.Caches<CRLocation>().GetStatus(location) == PXEntryStatus.Notchanged)
						Base.Caches<CRLocation>().MarkUpdated(location);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<Vendor.payToVendorID> e)
		{
			if (e.Row == null || e.NewValue == null)
				return;

			Vendor vendor = e.Row as Vendor;
			BAccount payToVendor = PXSelectorAttribute.Select<Vendor.payToVendorID>(e.Cache, e.Row, e.NewValue) as BAccount;
			BAccount restrictedBAccount = PXSelectorAttribute.Select<Vendor.vOrgBAccountID>(e.Cache, e.Row) as BAccount;

			if (vendor == null || payToVendor == null || restrictedBAccount == null)
				return;

			if (payToVendor.BaseCuryID != vendor.BaseCuryID)
			{
				e.NewValue = payToVendor.AcctCD;
				throw new PXSetPropertyException(Messages.PayToVendorDiffrentBaseCurrency,
					payToVendor.AcctCD,
					restrictedBAccount.AcctCD,
					vendor.AcctCD);
			}
		}

		public delegate IEnumerable vendorBalanceByBaseCurrencyDelegate();
		[PXOverride]
		public IEnumerable vendorBalanceByBaseCurrency(vendorBalanceByBaseCurrencyDelegate baseMethod)
		{
			Vendor vendor = (Vendor)Base.BAccountAccessor.Current;
			List<VendorBalanceSummaryByBaseCurrency> list = new List<VendorBalanceSummaryByBaseCurrency>(1);
			bool isInserted = (Base.BAccountAccessor.Cache.GetStatus(vendor) == PXEntryStatus.Inserted);
			if (!isInserted)
			{
				PXSelectBase<APVendorBalanceEnq.APLatestHistory> sel = new PXSelectJoinGroupBy<APVendorBalanceEnq.APLatestHistory,
					LeftJoin<CuryAPHistory, On<APVendorBalanceEnq.APLatestHistory.branchID, Equal<CuryAPHistory.branchID>,
						And<APVendorBalanceEnq.APLatestHistory.accountID, Equal<CuryAPHistory.accountID>,
						And<APVendorBalanceEnq.APLatestHistory.vendorID, Equal<CuryAPHistory.vendorID>,
						And<APVendorBalanceEnq.APLatestHistory.subID, Equal<CuryAPHistory.subID>,
						And<APVendorBalanceEnq.APLatestHistory.curyID, Equal<CuryAPHistory.curyID>,
						And<APVendorBalanceEnq.APLatestHistory.lastActivityPeriod, Equal<CuryAPHistory.finPeriodID>>>>>>>,
					InnerJoin<Branch, On<Branch.branchID, Equal<CuryAPHistory.branchID>>>>,
					Where<APVendorBalanceEnq.APLatestHistory.vendorID, Equal<Current<Vendor.bAccountID>>>,
					Aggregate<
						GroupBy<GL.Branch.baseCuryID,
						Sum<CuryAPHistory.finYtdBalance,
						Sum<CuryAPHistory.finYtdDeposits,
						Sum<CuryAPHistory.finYtdRetainageWithheld,
						Sum<CuryAPHistory.finYtdRetainageReleased
						>>>>>>>(Base);

				foreach (PXResult<APVendorBalanceEnq.APLatestHistory, CuryAPHistory> it in sel.Select())
				{
					CuryAPHistory iHst = it;
					Aggregate(list, iHst);
				}
			}

			var foundCurrencies = list.Select(_ => _.BaseCuryID).Distinct().ToList();

			var availableBranches = CommonServiceLocator.ServiceLocator.Current.GetInstance<ICurrentUserInformationProvider>().GetAllBranches().ToList();
			var currencies = availableBranches.Select(b => PXAccess.GetBranch(b.Id).BaseCuryID).Distinct().ToList();
			foreach (var currency in currencies.Where(c => !foundCurrencies.Contains(c)))
			{
				list.Add(new VendorBalanceSummaryByBaseCurrency()
				{
					VendorID = Base.BAccount.Current.BAccountID,
					BaseCuryID = currency,
					Balance = 0,
					DepositsBalance = 0,
					RetainageBalance = 0
				});
			}

			return list.OrderBy(_ => _.BaseCuryID);
		}

		protected virtual void Aggregate(List<VendorBalanceSummaryByBaseCurrency> aRes, CuryAPHistory aSrc)
		{
			var baseCuryID = PXAccess.GetBranch(aSrc.BranchID)?.BaseCuryID;

			var item = aRes.Where(_ => _.BaseCuryID == baseCuryID).FirstOrDefault();
			if (item == null)
			{
				item = new VendorBalanceSummaryByBaseCurrency();

				if (!item.Balance.HasValue) item.Balance = Decimal.Zero;
				if (!item.DepositsBalance.HasValue) item.DepositsBalance = Decimal.Zero;
				if (!item.RetainageBalance.HasValue) item.RetainageBalance = Decimal.Zero;

				item.BaseCuryID = baseCuryID;
				aRes.Add(item);
			}

			item.VendorID = aSrc.VendorID;
			item.Balance += aSrc.FinYtdBalance ?? Decimal.Zero;
			item.DepositsBalance += aSrc.FinYtdDeposits ?? Decimal.Zero;
			item.RetainageBalance += aSrc.FinYtdRetainageWithheld - aSrc.FinYtdRetainageReleased;
		}
	}
}
