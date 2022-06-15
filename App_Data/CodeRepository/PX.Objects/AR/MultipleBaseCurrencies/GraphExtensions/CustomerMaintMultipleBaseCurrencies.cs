using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CS;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System.Linq;
using PX.Objects.Common;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.GL;
using PX.Objects.CA;

namespace PX.Objects.AR
{
	public class CustomerMaintMultipleBaseCurrencies : PXGraphExtension<CustomerMaint.PaymentDetailsExt, CustomerMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.RowSelected<Customer> e)
		{
			if (e.Row == null)
				return;

			Customer customer = e.Row;

			PXUIFieldAttribute.SetRequired<Customer.cOrgBAccountID>(e.Cache,
				PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>()
				&& e.Row.IsBranch == false);

			#region Balances

			var showCreditVerifications = e.Row.BaseCuryID != null && e.Row.IsBranch != true;
			PXUIFieldAttribute.SetVisible<Customer.creditRule>(Base.CurrentCustomer.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<Customer.creditLimit>(Base.CurrentCustomer.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<Customer.creditDaysPastDue>(Base.CurrentCustomer.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.unreleasedBalance>(Base.CustomerBalance.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.openOrdersBalance>(Base.CustomerBalance.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.remainingCreditLimit>(Base.CustomerBalance.Cache, null, showCreditVerifications);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.oldInvoiceDate>(Base.CustomerBalance.Cache, null, showCreditVerifications);

			var showBalances = true;

			if (e.Row.BaseCuryID == null)
			{
				var availableBranches = CommonServiceLocator.ServiceLocator.Current.GetInstance<ICurrentUserInformationProvider>().GetAllBranches().ToList();
				var currencies = availableBranches.Select(b => PXAccess.GetBranch(b.Id).BaseCuryID).Distinct().ToList();
				showBalances = currencies.Count <= 1;
			}

			bool hasChildren = PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>() &&
				CustomerMaint.HasChildren<Override.BAccount.parentBAccountID>(Base, e.Row.BAccountID);

			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.balance>(Base.CustomerBalance.Cache, null, showBalances);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.consolidatedbalance>(Base.CustomerBalance.Cache, null, showBalances && hasChildren);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.signedDepositsBalance>(Base.CustomerBalance.Cache, null, showBalances && !hasChildren);
			PXUIFieldAttribute.SetVisible<CustomerMaint.CustomerBalanceSummary.retainageBalance>(Base.CustomerBalance.Cache, null, showBalances);

			PXUIFieldAttribute.SetVisible<ARBalancesByBaseCuryID.consolidatedBalance>(Base.Balances.Cache, null, hasChildren);
			Base.Balances.AllowSelect = !showBalances;

			if (Base.Balances.AllowSelect)
			{
				var arCurrenycBalances = SelectFrom<ARBalancesByBaseCuryID>
						.InnerJoin<Override.BAccount>
						.On<Override.BAccount.bAccountID.IsEqual<ARBalancesByBaseCuryID.customerID>>
						.Where<Override.BAccount.bAccountID.IsEqual<@P.AsInt>
							  .Or<Override.BAccount.parentBAccountID.IsEqual<@P.AsInt>.And<Override.BAccount.consolidateToParent.IsEqual<True>>>>
						.View.SelectMultiBound(Base, null, new object[] { customer.BAccountID, customer.BAccountID }).RowCast<ARBalancesByBaseCuryID>();

				var arCurrencyBalanceHistoryValues = PXSelectJoinGroupBy<CuryARHistory,
						InnerJoin<ARCustomerBalanceEnq.ARLatestHistory, On<ARCustomerBalanceEnq.ARLatestHistory.accountID, Equal<CuryARHistory.accountID>,
							And<ARCustomerBalanceEnq.ARLatestHistory.branchID, Equal<CuryARHistory.branchID>,
							And<ARCustomerBalanceEnq.ARLatestHistory.customerID, Equal<CuryARHistory.customerID>,
							And<ARCustomerBalanceEnq.ARLatestHistory.subID, Equal<CuryARHistory.subID>,
							And<ARCustomerBalanceEnq.ARLatestHistory.curyID, Equal<CuryARHistory.curyID>,
							And<ARCustomerBalanceEnq.ARLatestHistory.lastActivityPeriod, Equal<CuryARHistory.finPeriodID>>>>>>>>,
						Where<CuryARHistory.customerID, Equal<Current<Customer.bAccountID>>>,
						Aggregate<
							GroupBy<CuryARHistory.curyID,
							Sum<CuryARHistory.finYtdDeposits,
							Sum<CuryARHistory.finYtdRetainageWithheld,
							Sum<CuryARHistory.finYtdRetainageReleased>>>>>>
						.SelectMultiBound(Base, null).RowCast<CuryARHistory>();

				foreach (ARBalancesByBaseCuryID arBalance in Base.Balances.Select())
				{
					arBalance.ConsolidatedBalance = arCurrenycBalances.Where(a => a.BaseCuryID == arBalance.BaseCuryID)
						.Aggregate<ARBalancesByBaseCuryID, decimal?>(0m, (current, bal) => current + bal.CurrentBal) ?? 0m;

					arBalance.TotalPrepayments = arCurrencyBalanceHistoryValues.Where(a => a.CuryID == arBalance.BaseCuryID)
						.Aggregate<CuryARHistory, decimal?>(0m, (current, bal) => current - bal.FinYtdDeposits);

					arBalance.RetainageBalance = arCurrencyBalanceHistoryValues.Where(a => a.CuryID == arBalance.BaseCuryID)
						.Aggregate<CuryARHistory, decimal?>(0m, (current, bal) => current + bal.FinYtdRetainageWithheld ?? 0m - bal.FinYtdRetainageReleased ?? 0m);
				}
			}

			#endregion

			#region Restrict Visibility for Vendors, extended from Branch
			PXUIFieldAttribute.SetEnabled<Customer.cOrgBAccountID>(Base.CurrentCustomer.Cache, customer, true);

			if (customer.IsBranch == true)
			{
				using (new PXReadBranchRestrictedScope())
				{
					// searching for AR History with different base currencies
					ARHistory diffCurrencyHistory = SelectFrom<ARHistory>
						.InnerJoin<Branch>
							.On<Branch.branchID.IsEqual<ARHistory.branchID>>
						.InnerJoin<ARHistoryAlias>
							.On<ARHistoryAlias.customerID.IsEqual<ARHistory.customerID>>
						.InnerJoin<BranchAlias>
							.On<BranchAlias.branchID.IsEqual<ARHistoryAlias.branchID>
								.And<BranchAlias.baseCuryID.IsNotEqual<Branch.baseCuryID>>>
						.Where<ARHistory.customerID.IsEqual<@P.AsInt>>
						.View
						.SelectSingleBound(Base, null, new object[] { customer.BAccountID });

					if (diffCurrencyHistory != null)
					{
						PXUIFieldAttribute.SetEnabled<Customer.cOrgBAccountID>(Base.CurrentCustomer.Cache, customer, false);
					}
					else if (customer.Type == BAccountType.CombinedType)
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
							.SelectSingleBound(Base, null, new object[] { customer.BAccountID });

						if (diffCurrencyAPHistory != null)
						{
							PXUIFieldAttribute.SetEnabled<Customer.cOrgBAccountID>(Base.CurrentCustomer.Cache, customer, false);
						}
						else
						{
							// searching for combined AP/AR History with different base currencies
							ARHistory diffCurrencyAPARHistory =
								SelectFrom<ARHistory>
									.InnerJoin<Branch>
										.On<Branch.branchID.IsEqual<ARHistory.branchID>>
									.InnerJoin<APHistory>
										.On<APHistory.vendorID.IsEqual<ARHistory.customerID>>
									.InnerJoin<BranchAlias>
										.On<BranchAlias.branchID.IsEqual<APHistory.branchID>
											.And<BranchAlias.baseCuryID.IsNotEqual<Branch.baseCuryID>>>
									.Where<ARHistory.customerID.IsEqual<@P.AsInt>>
								.View
								.SelectSingleBound(Base, null, new object[] { customer.BAccountID });

							if (diffCurrencyAPARHistory != null)
							{
								PXUIFieldAttribute.SetEnabled<Customer.cOrgBAccountID>(Base.CurrentCustomer.Cache, customer, false);
							}
						}
					}
				}
			}
			#endregion
		}

		protected virtual void _(Events.RowPersisting<Customer> e)
		{
			if (e.Row == null)
				return;

			if (Base.CurrentCustomer.Cache.GetStatus(e.Row) == PXEntryStatus.Updated)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					Base.CurrentCustomer.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					Base.CurrentCustomer.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		protected virtual void _(Events.RowDeleted<Customer> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.Type == BAccountType.CustomerType && e.Row.IsBranch == true)
			{
				BAccountItself baccount = Base.CurrentBAccountItself.SelectSingle(e.Row.BAccountID);
				if (baccount != null)
				{
					baccount.BaseCuryID = null;
					baccount.COrgBAccountID = 0;
					Base.CurrentBAccountItself.Update(baccount);
				}
			}
		}

		protected virtual void _(Events.RowSelected<CustomerMaint.CustomerBalanceSummary> e)
		{
			if (e.Row == null)
				return;

			e.Cache
				.GetAttributesReadonly(null)
				.OfType<PX.Objects.CM.CurySymbolAttribute>()
				.ToList()
				.ForEach(attr => attr.SetSymbol(null));

			Customer customer = e.Cache.Graph.Caches[typeof(Customer)].Current as Customer;

			if (customer != null && customer.BaseCuryID == null)
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

		#region COrgBAccountID
		protected virtual void _(Events.FieldVerifying<Customer, Customer.cOrgBAccountID> e)
		{
			if (e.Row == null || (int?)e.NewValue == (int?)e.OldValue) return;
			Customer customer = e.Row;

			var newBaseCuryID = PXOrgAccess.GetBaseCuryID((int)e.NewValue);

			if (customer.BaseCuryID != newBaseCuryID)
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
						.SelectSingleBound(Base, null, new object[] { customer.BAccountID, newBaseCuryID });
				}

				if (arHist != null)
				{
					e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

					var branch = PXAccess.GetBranch(arHist.BranchID);

					throw new PXSetPropertyException(Messages.EntityCannotBeAssociated, PXErrorLevel.Error,
							branch.BaseCuryID,
							customer.AcctCD.Trim());
				}
				else if (customer.Type == BAccountType.CombinedType && (int?)e.NewValue != customer.VOrgBAccountID)
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
							.SelectSingleBound(Base, null, new object[] { customer.BAccountID, newBaseCuryID });
					}

					if (apHist != null)
					{
						e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

						var branch = PXAccess.GetBranch(apHist.BranchID);

						if (VisibilityRestriction.IsEmpty(customer.COrgBAccountID))
						{
							throw new PXSetPropertyException(Messages.EntityCannotBeAssociatedBecauseOfARHistoryNoRestrictForVendor, PXErrorLevel.Error,
									branch.BaseCuryID,
									customer.AcctCD.Trim());
						}
						else
						{
							var vendorBranch = PXAccess.GetBranchByBAccountID(customer.VOrgBAccountID);
							var vendorOrg = PXAccess.GetOrganizationByBAccountID(customer.VOrgBAccountID);

							throw new PXSetPropertyException(Messages.EntityCannotBeAssociatedBecauseOfAPHistory, PXErrorLevel.Error,
									branch.BaseCuryID,
									customer.AcctCD.Trim(),
									vendorBranch?.BranchCD?.Trim() ?? vendorOrg?.OrganizationCD?.Trim(),
									vendorBranch?.BaseCuryID ?? vendorOrg?.BaseCuryID);
						}
					}
					else if (VisibilityRestriction.IsNotEmpty(customer.VOrgBAccountID)
						&& (customer.IsBranch == true || VisibilityRestriction.IsNotEmpty((int?)e.NewValue)))
					{
						var vendorBranch = PXAccess.GetBranchByBAccountID(customer.VOrgBAccountID);
						var vendorOrg = PXAccess.GetOrganizationByBAccountID(customer.VOrgBAccountID);
						var localizedMsg = PXMessages.LocalizeFormatNoPrefix(Messages.ChangeVisibilityForVendor,
							customer.AcctCD.Trim(),
							vendorBranch?.BranchCD?.Trim() ?? vendorOrg?.OrganizationCD?.Trim(),
							vendorBranch?.BaseCuryID ?? vendorOrg?.BaseCuryID);
						if (Base.BAccount.View.Ask(localizedMsg, MessageButtons.YesNo) == WebDialogResult.Yes)
						{
							object newValue = e.NewValue;
							e.Cache.RaiseFieldVerifying<Customer.vOrgBAccountID>(e.Row, ref newValue);
							// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs
							// [This is almost independed field, related to the vendors. We have to either modify both fields,
							// or revert changing of the customer related one, because of complicated logic]
							customer.VOrgBAccountID = (int?)e.NewValue;
						}
						else
						{
							e.NewValue = e.OldValue;
							e.Cancel = true;
							return;
						}
					}
					else if (customer.IsBranch == true || VisibilityRestriction.IsNotEmpty((int?)e.NewValue))
					{
						var localizedMsg = PXMessages.LocalizeFormatNoPrefix(Messages.SetVisibilityForVendor,
							customer.AcctCD);
						if (Base.BAccount.View.Ask(localizedMsg, MessageButtons.YesNo) == WebDialogResult.Yes)
						{
							object newValue = e.NewValue;
							e.Cache.RaiseFieldVerifying<Customer.vOrgBAccountID>(e.Row, ref newValue);
							// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs
							// [This is almost independed field, related to the vendors. We have to either modify both fields,
							// or revert changing of the customer related one, because of complicated logic]
							customer.VOrgBAccountID = (int?)e.NewValue;
						}
						else
						{
							e.NewValue = e.OldValue;
							e.Cancel = true;
							return;
						}
					}
				}
			}

			if (e.NewValue != null && (int)e.NewValue != 0)
			{
				Customer childCustomer =
					SelectFrom<Customer>
					.Where<Customer.parentBAccountID.IsEqual<@P.AsInt>
					.And<Customer.consolidateToParent.IsEqual<True>
					.And<Customer.baseCuryID.IsNotEqual<@P.AsString>>>>
					.View.SelectSingleBound(Base, null, customer.BAccountID, newBaseCuryID);

				if (childCustomer != null)
				{
					e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

					throw new PXSetPropertyException(Messages.EntityWithBaseCurencyCannotBeAssociatedWithCustomer, PXErrorLevel.Error,
							e.Row.BaseCuryID,
							childCustomer.AcctCD,
							PXOrgAccess.GetCD(childCustomer.COrgBAccountID),
							PXOrgAccess.GetBaseCuryID(childCustomer.COrgBAccountID));
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<Customer, Customer.cOrgBAccountID> e)
		{
			if ((int?)e.OldValue == (int?)e.NewValue) return;

			if (!((int?)e.NewValue == VisibilityRestriction.EmptyBAccountID
				&& e.Row.Type == BAccountType.CombinedType
				&& e.Row.VOrgBAccountID != VisibilityRestriction.EmptyBAccountID))
			{
				e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.COrgBAccountID)
					?? ((e.Row.IsBranch == true) ? null : Base.Accessinfo.BaseCuryID);
			}

			if (e.Row.ParentBAccountID != null)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					e.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					e.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		#endregion

		protected virtual void _(Events.FieldVerifying<Customer, Customer.parentBAccountID> e)
		{
			if (e.NewValue == null)
				return;

			BAccountR bAccount = PXSelectorAttribute.Select<Customer.parentBAccountID>(e.Cache, e.Row, (int)e.NewValue) as BAccountR;

			if (bAccount != null
				&& bAccount.BaseCuryID != null
				&& bAccount.BaseCuryID != e.Row.BaseCuryID
				&& e.Row.ConsolidateToParent == true)
			{
				e.NewValue = bAccount.AcctCD;

				throw new PXSetPropertyException(Messages.CannotBeUsedAsParent, PXErrorLevel.Error,
							bAccount.AcctCD,
							PXOrgAccess.GetCD(e.Row.COrgBAccountID),
							e.Row.AcctCD);
			}
		}

		protected virtual void _(Events.RowUpdated<Customer> e)
		{
			if (e.OldRow.BaseCuryID != e.Row.BaseCuryID)
			{
				foreach (CustomerPaymentMethod paymentMethod in SelectFrom<CustomerPaymentMethod>
					.Where<CustomerPaymentMethod.bAccountID.IsEqual<Customer.bAccountID.FromCurrent>>.View.SelectMultiBound(Base, new object[] { e.Row }, null))
				{
					paymentMethod.CashAccountID = null;
					if (Base.Caches<CustomerPaymentMethod>().GetStatus(paymentMethod) == PXEntryStatus.Notchanged)
						Base.Caches<CustomerPaymentMethod>().MarkUpdated(paymentMethod);
				}

				foreach (CustomerPaymentMethodInfo paymentMethod in Base.GetExtension<CustomerMaint.PaymentDetailsExt>().PaymentMethods.Select()
					.Where(m => ((CustomerPaymentMethodInfo)m).BAccountID != null))
				{
					paymentMethod.CashAccountID = null;
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<Customer, Customer.consolidateToParent> e)
		{
			if ((bool)e.NewValue)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					Base.CurrentCustomer.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					Base.CurrentCustomer.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<BAccountR.baseCuryID.IsEqual<Customer.baseCuryID.FromCurrent>
			.Or<Customer.consolidateToParent.FromCurrent.IsEqual<False>>
			.Or<BAccountR.baseCuryID.IsNull>>),
			Messages.CannotBeUsedAsParentRestrictor)]
		protected void Customer_ParentBAccountID_CacheAttached(PXCache sender){}

		[CashAccount(null, typeof(Search2<
					CashAccount.cashAccountID,
				InnerJoin<PaymentMethodAccount,
					On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
					And<PaymentMethodAccount.useForAR, Equal<True>,
					And<PaymentMethodAccount.paymentMethodID, Equal<Current<CustomerPaymentMethod.paymentMethodID>>>>>>,
				Where2<
					Match<Current<AccessInfo.userName>>,
					And<Where<CashAccount.baseCuryID, Equal<Current<Customer.baseCuryID>>>>>>),
				DisplayName = "Cash Account",
				Visibility = PXUIVisibility.Visible,
				Enabled = false)]
		[PXDefault(typeof(Search<CA.PaymentMethod.defaultCashAccountID, Where<CA.PaymentMethod.paymentMethodID, Equal<Current<CustomerPaymentMethod.paymentMethodID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CustomerPaymentMethod.cashAccountID> e) { }

		public delegate IEnumerable CustomerStatementDelegate(PXAdapter adapter);
		[PXOverride]
		public IEnumerable CustomerStatement(PXAdapter adapter, CustomerStatementDelegate baseMethod)
		{
			Customer customer = Base.CurrentCustomer.Current;

			if (customer == null)
			{
				return adapter.Get();
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>()
					&& Base.ARSetup.Current.PrepareStatements.Equals(AR.ARSetup.prepareStatements.ConsolidatedForAllCompanies)
					&& customer.BaseCuryID == null)
			{
				throw new PXException(Messages.StatementsCannotBePrinted);
			}

			return baseMethod(adapter);
		}

		public delegate IEnumerable customerBalanceDelegate();
		[PXOverride]
		public IEnumerable customerBalance(customerBalanceDelegate baseMethod)
		{
			using(new Common.Scopes.ForceUseBranchRestrictionsScope())
			{
				return baseMethod.Invoke();
			}
		}

		protected virtual IEnumerable balances()
		{
			var list = new List<ARBalancesByBaseCuryID>();
			foreach(var item in PXSelect<ARBalancesByBaseCuryID,
				Where<ARBalancesByBaseCuryID.customerID, Equal<Current<Customer.bAccountID>>>>.Select(Base))
			{
				list.Add(item);
			}
			var foundCurrencies = list.Select(_ => _.BaseCuryID).Distinct().ToList();

			var availableBranches = CommonServiceLocator.ServiceLocator.Current.GetInstance<ICurrentUserInformationProvider>().GetAllBranches().ToList();
			var currencies = availableBranches.Select(b => PXAccess.GetBranch(b.Id).BaseCuryID).Distinct().ToList();

			var consolidatedBalances = new Dictionary<string, decimal>();
			if (PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>())
			{
				Customer customer = Base.BAccountAccessor.Current;

				consolidatedBalances =
						PXSelectJoinGroupBy<CustomerMaint.CustomerBalances,
						InnerJoin<Override.BAccount,
							On<Override.BAccount.bAccountID, Equal<CustomerMaint.CustomerBalances.customerID>>>,
						Where2<
							Where<Override.BAccount.consolidateToParent, Equal<True>,
								And<Override.BAccount.parentBAccountID, Equal<Required<Override.Customer.bAccountID>>>>
								, Or<Override.BAccount.bAccountID, Equal<Required<Customer.bAccountID>>>>
						, Aggregate<
							GroupBy<CustomerMaint.CustomerBalances.baseCuryID,
							Sum<CustomerMaint.CustomerBalances.balance,
							Sum<CustomerMaint.CustomerBalances.unreleasedBalance,
							Sum<CustomerMaint.CustomerBalances.openOrdersBalance,
							Min<CustomerMaint.CustomerBalances.oldInvoiceDate>>>>>>>
						.Select(Base, customer.BAccountID, customer.BAccountID)
						.RowCast<CustomerMaint.CustomerBalances>()
						.ToDictionary(_ => _.BaseCuryID, _ => _.Balance ?? 0m);
			}

			foreach (var currency in currencies.Where(c => !foundCurrencies.Contains(c)))
			{
				list.Add(new ARBalancesByBaseCuryID()
				{
					CustomerID = Base.BAccount.Current.BAccountID,
					BaseCuryID = currency, 
					ConsolidatedBalance = consolidatedBalances.ContainsKey(currency) ? consolidatedBalances[currency] : 0m,
					CurrentBal = 0,
					RetainageBalance = 0,
					TotalPrepayments = 0,
					UnreleasedBal = 0
				});
			}

			return list.OrderBy(_ => _.BaseCuryID);
		}

		protected virtual void _(Events.RowSelecting<CustomerMaint.ChildCustomerBalanceSummary> e)
		{
			CustomerMaint.ChildCustomerBalanceSummary sumarry = e.Row as CustomerMaint.ChildCustomerBalanceSummary;

			if (sumarry != null && sumarry.BaseCuryID == null)
			{
				sumarry.BaseCuryID = PXAccess.GetBranchByBAccountID(sumarry.CustomerID)?.BaseCuryID;
			}
		}
	}
}
