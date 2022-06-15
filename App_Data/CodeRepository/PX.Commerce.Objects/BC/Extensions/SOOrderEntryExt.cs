using System;
using System.Collections.Generic;
using System.Linq;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.SO;
using PX.Common;
using PX.Data.WorkflowAPI;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using PX.Objects.SO.Workflow.SalesOrder;
using System.Collections;
using PX.Objects.TX;

namespace PX.Commerce.Objects
{
	using static BoundedTo<SOOrderEntry, SOOrder>;
	using static PX.Commerce.Objects.BCSOOrderExt;
	using static SOOrder;
	public class BCSOOrderEntryExt : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public PXSelect<SOOrderRisks, Where<SOOrderRisks.orderNbr, Equal<Current<SOOrder.orderNbr>>, And<SOOrderRisks.orderType, Equal<Current<SOOrder.orderType>>>>,
			OrderBy<Asc<SOOrderRisks.lineNbr>>> OrderRisks;

		public PXWorkflowEventHandler<SOOrder> OnRiskHoldConditionSatisfied;

		public class SOOrderEntry_RiskWorkflow : PXGraphExtension<SOOrderEntry_ApprovalWorkflow, SOOrderEntry_Workflow, SOOrderEntry>
		{
			private class SOSetupApprovalWorkflow : IPrefetchable
			{
				public static bool IsActive => PXDatabase.GetSlot<SOSetupApprovalWorkflow>(nameof(SOSetupApprovalWorkflow), typeof(SOSetup)).OrderRequestApproval;

				private bool OrderRequestApproval;

				void IPrefetchable.Prefetch()
				{
					using (PXDataRecord soSetup = PXDatabase.SelectSingle<SOSetup>(new PXDataField<SOSetup.orderRequestApproval>()))
					{
						if (soSetup != null)
							OrderRequestApproval = (bool)soSetup.GetBoolean(0);
					}
				}
			}

			protected static bool ApprovalIsActive() { return (PXAccess.FeatureInstalled("PX.Objects.CS.FeaturesSet+approvalWorkflow") && SOSetupApprovalWorkflow.IsActive); }

			public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

			public override void Configure(PXScreenConfiguration config)
			{
				if (!IsActive()) return;

				var context = config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>();
				Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();

				var approvalCategory = PX.Objects.Common.CommonActionCategories.Get(context).Approval;

				var conditions = new
				{
					IsOnRiskHold
						= Bql<riskHold.IsEqual<True>>(),
					IsNotOnRiskHoldAndNotApproved
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<False>>>(),

					IsNotOnRiskHold
						= Bql<riskHold.IsEqual<False>>(),

					IsNotOnRiskAndHasPaymentsInPendingProcessing
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<True>.And<paymentsNeedValidationCntr.IsGreater<Zero>>>>(),

					IsNotOnRiskAndIsPaymentRequirementsViolated
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<True>.And<prepaymentReqSatisfied.IsEqual<False>>>>(),

				}.AutoNameConditions();

				var riskHold = context.ActionDefinitions
					.CreateExisting<SOOrderEntry_RiskWorkflow>(g => g.riskHold, a => a
					.WithCategory(approvalCategory, g => g.releaseFromCreditHold)
					.PlaceAfter(g => g.releaseFromCreditHold)
					.WithFieldAssignments(fass =>
					{
						fass.Add<riskHold>(v => v.SetFromValue(true));
					}
					));

				var removeRiskHold = context.ActionDefinitions
					.CreateExisting<SOOrderEntry_RiskWorkflow>(g => g.removeRiskHold, a => a
					.WithCategory(approvalCategory, riskHold)
					.PlaceAfter(riskHold)
					.WithFieldAssignments(fass =>
					{
						fass.Add<riskHold>(v => v.SetFromValue(false));
					}
					));

				context.UpdateScreenConfigurationFor(screen =>
				{
					return screen
					.WithFieldStates(fieldStates =>
						{
							fieldStates.Add<SOOrder.status>(fieldState =>
							fieldState.SetComboValue(SOOrderStatusExt.RiskHold, BCObjectsMessages.RiskHold));
						})
					.WithActions(actions =>
						{
							actions.Add(riskHold);
							actions.Add(removeRiskHold);
						})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler =>
							{
								return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<BCSOOrderExt.Events>(e => e.RiskHoldConditionStatisfied)
								.Is(e => e.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Risk Hold Required");
							});
						})
					.WithFlows(flows =>
						{
							flows.Update<SOBehavior.sO>(flow =>
							{
								const string initialState = "_";
								return flow
									.WithFlowStates(flowStates =>
									{
										flowStates.Add<SOOrderStatusExt.riskHold>(flowstate =>
										{
											return flowstate
											.WithActions(actions =>
											{
												actions.Add(removeRiskHold, g => g.IsDuplicatedInToolbar());
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.cancelOrder);
											});
										});

										flowStates.Update<SOOrderStatus.open>(flowState =>
										{
											return flowState
											.WithActions(actions =>
											{
												actions.Add(riskHold);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied);
											});
										});

										flowStates.Update<SOOrderStatus.hold>(flowState =>
										{
											return flowState
											.WithActions(actions =>
											{
												actions.Add(riskHold);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied);
											});
										});
									})

									.WithTransitions(transitions =>
									{
										transitions.UpdateGroupFrom(initialState, ts =>
										{

											ts.Add(t => t
											.To<SOOrderStatusExt.riskHold>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsOnRiskHold)
											.PlaceAfter(tr => tr.To<SOOrderStatus.hold>().IsTriggeredOn(g => g.initializeState))
											.WithFieldAssignments(fas =>
											{
												fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
											}));
										});

										transitions.UpdateGroupFrom<SOOrderStatus.hold>(ts =>
										{
											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>()
												.IsTriggeredOn(riskHold)
												.WithFieldAssignments(fas =>
												 {
													 fas.Add<hold>(e => e.SetFromValue(false));
												 }));

											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>()
												 .IsTriggeredOn(g => g.releaseFromHold)
												 .When(conditions.IsOnRiskHold)
												 .PlaceBefore(tr => ApprovalIsActive()
												  ? tr.To<SOOrderStatus.pendingApproval>()
													: tr.To<SOOrderStatus.pendingProcessing>()));
										});

										transitions.UpdateGroupFrom<SOOrderStatus.open>(ts =>
										{
											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>().IsTriggeredOn(riskHold)
												.WithFieldAssignments(fas =>
												{
													fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
												}));

											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>()
												 .IsTriggeredOn(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied)
												 .WithFieldAssignments(fas =>
												 {
													 fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
												 }));

										});

										transitions.AddGroupFrom<SOOrderStatusExt.riskHold>(ts =>
										{
											//put to hold on puttohold action
											ts.Add(t => t
													.To<SOOrderStatus.hold>()
													.IsTriggeredOn(g => g.putOnHold));

											//put to cancel on cancel action
											ts.Add(t => t
													.To<SOOrderStatus.cancelled>()
													.IsTriggeredOn(g => g.cancelOrder));

											if (SOOrderEntry_ApprovalWorkflow.ApprovalIsActive())
												ts.Add(t => t
															.To<SOOrderStatus.pendingApproval>()
															.IsTriggeredOn(removeRiskHold)
															.When(conditions.IsNotOnRiskHoldAndNotApproved)
															.WithFieldAssignments(fas =>
															{
																fas.Add<BCSOOrderExt.riskHold>(v => v.SetFromValue(false));

															}));

											ts.Add(t => t
														.To<SOOrderStatus.pendingProcessing>()
														.IsTriggeredOn(removeRiskHold)
														.When(conditions.IsNotOnRiskAndHasPaymentsInPendingProcessing));

											ts.Add(t => t
														.To<SOOrderStatus.awaitingPayment>()
														.IsTriggeredOn(removeRiskHold)
														.When(conditions.IsNotOnRiskAndIsPaymentRequirementsViolated));

											// put to open if releases from riskhold and let Open state decide where to go from  there
											ts.Add(t => t
													.To<SOOrderStatus.open>()
													.IsTriggeredOn(removeRiskHold)
													.WithFieldAssignments(fas =>
													{
														fas.Add<BCSOOrderExt.riskHold>(v => v.SetFromValue(false));
														fas.Add<inclCustOpenOrders>(e => e.SetFromValue(true));

													}));

										});
									});
							});
						});
				});
			}

			public PXAction<SOOrder> riskHold;
			[PXButton(CommitChanges = true), PXUIField(DisplayName = BCObjectsMessages.RiskHold, MapEnableRights = PXCacheRights.Select)]
			protected virtual IEnumerable RiskHold(PXAdapter adapter) => adapter.Get<SOOrder>();

			public PXAction<SOOrder> removeRiskHold;
			[PXButton(CommitChanges = true), PXUIField(DisplayName = BCObjectsMessages.RemoveRiskHold, MapEnableRights = PXCacheRights.Select)]
			protected virtual IEnumerable RemoveRiskHold(PXAdapter adapter) => adapter.Get<SOOrder>();
		}

		public class SOOrderStatusExt
		{
			public const string RiskHold = "X";
			public class riskHold : PX.Data.BQL.BqlString.Constant<riskHold>
			{
				public riskHold() : base(RiskHold) { }
			}

		}
		private bool _released;

		public delegate void PersistDelegate();
		[PXOverride]
		public void Persist(PersistDelegate handler)
		{
			SOOrder entry = Base.Document.Current;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();

			RemoveFromHold();

			AdjustAppliedtoOrderAmount(entry, context);
			SetEncryptionValue();
			handler();
		}

		protected void RemoveFromHold()
		{
			SOOrder entry = Base.Document.Current;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();

			if (entry != null && entry.GetExtension<BCSOOrderExt>().RiskHold == true)
			{
				if (context != null && !_released)
				{
					var store = BCBindingExt.PK.Find(Base, context.Binding);
					if (store.ImportOrderRisks == true)
					{
						SOOrderType orderType = PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>
						   >>.Select(Base, entry.OrderType);
						if (entry.Hold == false && orderType?.HoldEntry == false)
						{
							entry.Hold = true;
							_released = true;
							Base.Actions["releaseFromHold"].Press();

						}
					}
				}
			}

		}


		protected virtual void _(PX.Data.Events.RowSelected<SOOrder> e)
		{
			SOOrder row = e.Row;
			if (row == null)
				return;

			//To hide risks tab if not risks
			OrderRisks.AllowSelect = (row.GetExtension<BCSOOrderExt>().RiskStatus != BCCaptions.None && row.GetExtension<BCSOOrderExt>().RiskStatus != null);
			PXUIFieldAttribute.SetVisible<BCSOOrderExt.riskStatus>(e.Cache, row, OrderRisks.AllowSelect);
		}

		protected virtual void _(PX.Data.Events.RowDeleting<SOLine> e)
		{
			if (e.Row == null) return;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (e.ExternalCall && context == null)// allow deleting through connector or if order itself is deleted
			{
				SOLine currentLine = e.Row;
				if (currentLine.Operation == SOOperation.Issue)
				{
					SOLine associatedLine = Base.Transactions.Select().RowCast<SOLine>().FirstOrDefault(x => x.GetExtension<BCSOLineExt>().AssociatedOrderLineNbr == currentLine.LineNbr);
					if (associatedLine != null)
					{
						throw new PXSetPropertyException(BCObjectsMessages.CannotDeleteSOline, PXErrorLevel.RowError, associatedLine.LineNbr);
					}
				}

			}
		}

		protected virtual void _(PX.Data.Events.RowSelected<SOLine> e)
		{
			SOLine row = e.Row;
			if (row == null)
				return;

			var lineExt = row.GetExtension<BCSOLineExt>();

			if (lineExt.AssociatedOrderLineNbr != null)
			{
				PXUIFieldAttribute.SetEnabled<SOLine.siteID>(e.Cache, row, false);
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<BCSOOrderExt.riskStatus> e)
		{
			SOOrder order = Base.Document.Current;
			if (order != null)
			{
				if (e.NewValue != null)
				{
					string riskStatus = e.NewValue.ToString();
					BCBindingExt store = null;
					BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
					if (context != null)
						store = BCBindingExt.PK.Find(Base, context.Binding);
					else
					{
						BCSyncStatus syncStatus = PXSelect<BCSyncStatus, Where<BCSyncStatus.localID, Equal<Required<BCSyncStatus.localID>>, And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>>>>.Select(Base, order.NoteID, BCEntitiesAttribute.Order);
						if (syncStatus != null)
							store = BCBindingExt.PK.Find(Base, syncStatus?.BindingID);
					}
					if (store != null)
					{
						if ((store.HoldOnRiskStatus == BCRiskStatusAttribute.HighRisk && riskStatus == BCCaptions.High) ||
							(store.HoldOnRiskStatus == BCRiskStatusAttribute.MediumRiskorHighRisk && (riskStatus == BCCaptions.High || riskStatus == BCCaptions.Medium)))
						{
							Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, true);
							BCSOOrderExt.Events.Select(x => x.RiskHoldConditionStatisfied).FireOn(Base, order);
						}
						else
							Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, false);
					}
					else
						Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, false);
				}
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<SOOrderRisks.score> e)
		{
			if (e.NewValue != null)
			{
				decimal newValue = decimal.Parse(e.NewValue.ToString());
				if (Base.Document.Current.GetExtension<BCSOOrderExt>().MaxRiskScore == null || Base.Document.Current.GetExtension<BCSOOrderExt>().MaxRiskScore < newValue)
					Base.Document.Cache.SetValueExt<BCSOOrderExt.maxRiskScore>(Base.Document.Current, newValue);
			}
		}

		protected virtual void AdjustAppliedtoOrderAmount(SOOrder entry, BCAPISyncScope.BCSyncScopeContext context)
		{
			if (context != null)
			{
				//Adjust applied to order field to handle refunds flow
				var sOAdjusts = Base.Adjustments.Select();
				if (sOAdjusts.Count > 0 && entry.Cancelled != true && entry.Completed != true)
				{
					var appliedTotal = sOAdjusts?.ToList()?.Sum(x => x.GetItem<SOAdjust>().CuryAdjdAmt ?? 0m) ?? 0m;
					if (entry.CuryUnbilledOrderTotal < appliedTotal)
					{
						decimal? difference = appliedTotal - entry.CuryUnbilledOrderTotal;
						foreach (var soadjust in sOAdjusts)
						{
							var adjust = soadjust.GetItem<SOAdjust>();
							if (difference == 0m) break;
							if (adjust.CuryAdjdAmt > 0m)
							{
								decimal? newValue = adjust.CuryAdjdAmt;
								if (difference >= adjust.CuryAdjdAmt)
									newValue = difference = difference - adjust.CuryAdjdAmt;
								else if (difference < adjust.CuryAdjdAmt)
								{
									newValue = adjust.CuryAdjdAmt - difference;
									difference = 0m;
								}
								Base.Adjustments.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(adjust, newValue);
								Base.Adjustments.Cache.Update((SOAdjust)Base.Adjustments.Cache.CreateCopy(adjust));
							}
						}
					}
					else if ((entry.CuryOrderTotal - entry.CuryBilledPaymentTotal) > appliedTotal)
					{
						decimal? difference = entry.CuryOrderTotal - entry.CuryBilledPaymentTotal - appliedTotal;
						foreach (var soadjust in sOAdjusts)
						{
							var adjust = soadjust.GetItem<SOAdjust>();
							if (difference == 0m) break;
							decimal? balance = adjust.CuryOrigDocAmt - adjust.CuryAdjdAmt - (adjust.CuryAdjdBilledAmt ?? 0m);
							if (balance > 0m)
							{
								decimal? newValue = adjust.CuryAdjdAmt;
								if (difference <= balance)
								{
									newValue = adjust.CuryAdjdAmt + difference;
									difference = 0m;
								}
								else if (difference > balance)
								{
									difference = difference - balance;
									newValue = adjust.CuryAdjdAmt + balance;
								}
								Base.Adjustments.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(adjust, newValue);
								Base.Adjustments.Cache.Update((SOAdjust)Base.Adjustments.Cache.CreateCopy(adjust));
							}
						}
					}
				}
			}
		}

		public void SOOrder_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			SOOrder order = (SOOrder)e.Row;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();

			if (context != null && order != null)
			{
				if (Base.Transactions.Cache.IsInsertedUpdatedDeleted && e.Operation != PXDBOperation.Delete)
				{
					var soLines = Base.Transactions.Select().RowCast<SOLine>().ToList();
					if (soLines?.Count() > 0)
					{
						foreach (SOLine currentLine in soLines)
						{
							if (currentLine.Operation == SOOperation.Issue && currentLine.GetExtension<BCSOLineExt>().AssociatedOrderLineNbr != null && currentLine.GetExtension<BCSOLineExt>().AssociatedOrderLineNbr.ToString() == currentLine.GetExtension<BCSOLineExt>().ExternalRef)
							{
								SOLine line = soLines.FirstOrDefault(x => x.GetExtension<BCSOLineExt>().ExternalRef != null && x.GetExtension<BCSOLineExt>().ExternalRef == currentLine.GetExtension<BCSOLineExt>().AssociatedOrderLineNbr.ToString() && x.LineNbr != currentLine.LineNbr);
								if (line != null)
								{
									currentLine.SiteID = line.SiteID;
									currentLine.GetExtension<BCSOLineExt>().AssociatedOrderLineNbr = line.LineNbr;
									Base.Transactions.Cache.Update(currentLine);
								}
							}
						}
					}
				}

				if (String.IsNullOrEmpty(order.ShipVia)
					&& ((order.OverrideFreightAmount == true && order.CuryFreightAmt > 0)
						|| order.CuryPremiumFreightAmt > 0))
					throw new PXException(BCObjectsMessages.OrderMissingShipVia);

				//A validation made outside of taxZoneID updated event to prevent override of valid values in case of custom mappings 
				if (!String.IsNullOrEmpty(order.TaxCalcMode) && order.TaxCalcMode != TaxCalculationMode.TaxSetting && !String.IsNullOrEmpty(order.TaxZoneID))
				{
					TaxZone zone = PXSelect<TaxZone, Where<TaxZone.taxZoneID, Equal<Required<TaxZone.taxZoneID>>>>.Select(Base, order.TaxZoneID);
					if (zone.IsExternal == true)
						order.TaxCalcMode = TaxCalculationMode.TaxSetting;
				}
			}
		}

		private void SetEncryptionValue()
		{
			foreach (SOOrder doc in Base.Document.Cache.Inserted
				.Concat_(Base.Document.Cache.Updated)
				.Cast<SOOrder>())
			{
				SOOrderType orderType = (Base.soordertype.Current?.OrderType == doc.OrderType) ? Base.soordertype.Current : Base.soordertype.Select(doc.OrderType);
				if (orderType != null)
				{
					var encrypt = orderType.GetExtension<SOOrderTypeExt>().EncryptAndPseudonymizePII ?? false;
					SOShippingContact shippingContact = Base.Shipping_Contact.Select(doc.ShipContactID);
					if (shippingContact?.OverrideContact != null)
					{
						shippingContact.IsEncrypted = shippingContact.OverrideContact.Value && encrypt;
						Base.Shipping_Contact.Cache.Update((SOShippingContact)Base.Shipping_Contact.Cache.CreateCopy(shippingContact));
					}
					//if billing contact and shipping contact are new contacts or if they have different contactid then update each record else no need to update again
					if ((Base.Shipping_Contact.Cache.GetStatus(shippingContact) == PXEntryStatus.Inserted) || doc.BillContactID != doc.ShipContactID)
					{
						SOBillingContact billingContact = Base.Billing_Contact.Select(doc.BillContactID);
						if (billingContact?.OverrideContact != null)
						{
							billingContact.IsEncrypted = billingContact.OverrideContact.Value && encrypt;
							Base.Billing_Contact.Cache.Update((SOBillingContact)Base.Billing_Contact.Cache.CreateCopy(billingContact));
						}
					}
					SOBillingAddress billingAddress = Base.Billing_Address.Select(doc.BillAddressID);
					if (billingAddress?.OverrideAddress != null)
					{
						billingAddress.IsEncrypted = billingAddress.OverrideAddress.Value && encrypt;
						Base.Billing_Address.Cache.Update((SOBillingAddress)Base.Billing_Address.Cache.CreateCopy(billingAddress));
					}
					//if billing address and shipping address are new records or if they have different addressid then update each record else no need to update again

					if ((Base.Billing_Address.Cache.GetStatus(billingAddress) == PXEntryStatus.Inserted) ||doc.ShipAddressID != doc.BillAddressID)
					{
						SOShippingAddress shippingAddress = Base.Shipping_Address.Select(doc.ShipAddressID);
						if (shippingAddress?.OverrideAddress != null)
						{
							shippingAddress.IsEncrypted = shippingAddress.OverrideAddress.Value && encrypt;
							Base.Shipping_Address.Cache.Update((SOShippingAddress)Base.Shipping_Address.Cache.CreateCopy(shippingAddress));
						}
					}
				}
			}
		}

		protected void SOOrder_TaxZoneID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting baseHandler)
		{
			baseHandler?.Invoke(sender, e);

			if (e.NewValue == null)
			{
				BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
				if (context == null) return;

				BCBindingExt store = BCBindingExt.PK.Find(Base, context.Binding);
				if (store != null && store.TaxSynchronization == true)
					e.NewValue = store.DefaultTaxZoneID;
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdating<SOOrder, SOOrder.cancelled> eventArgs)
		{
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			bool value;
			bool.TryParse(eventArgs.NewValue?.ToString(), out value);
			if (value && context != null)
			{
				var sOAdjusts = Base.Adjustments.Select();
				if (sOAdjusts.Count > 0)
				{
					foreach (var soadjust in sOAdjusts)
					{
						var adjust = soadjust.GetItem<SOAdjust>();

						adjust.CuryAdjdAmt = 0;
						Base.Adjustments.Update(adjust);
					}
				}
			}
		}

		public bool? isTaxValid = null;
		protected virtual void _(PX.Data.Events.FieldUpdated<SOOrder, SOOrder.isTaxValid> e)
		{
			if (e.Row == null || e.NewValue == null) return;

			if (BCAPISyncScope.IsScoped() && (e.NewValue as Boolean?) == true)
			{
				isTaxValid = true;
			}
		}

		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void SOOrder_LastModifiedDateTime_CacheAttached(PXCache sender) { }


		protected virtual void _(PX.Data.Events.RowPersisting<SOOrder> e)
		{
			if (e.Row == null || (e.Operation & PXDBOperation.Command) != PXDBOperation.Update) return;
			Object oldRow = e.Cache.GetOriginal(e.Row);

			List<Type> monitoringTypes = new List<Type>();
			monitoringTypes.Add(typeof(SOOrder.customerID));
			monitoringTypes.Add(typeof(SOOrder.customerLocationID));
			monitoringTypes.Add(typeof(SOOrder.curyID));
			monitoringTypes.Add(typeof(SOOrder.orderQty));
			monitoringTypes.Add(typeof(SOOrder.curyDiscTot));
			monitoringTypes.Add(typeof(SOOrder.curyTaxTotal));
			monitoringTypes.Add(typeof(SOOrder.curyOrderTotal));
			monitoringTypes.Add(typeof(SOOrder.curyFreightTot));
			monitoringTypes.Add(typeof(SOOrder.shipVia));

			if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped() && ((bool?)e.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(e.Row) == true)
				&& monitoringTypes.Any(t => !object.Equals(e.Cache.GetValue(e.Row, t.Name), e.Cache.GetValue(oldRow, t.Name))))
			{
				e.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(e.Row, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowPersisting<SOLine> e)
		{
			if (e.Row == null || (e.Operation & PXDBOperation.Command) != PXDBOperation.Update) return;
			Object oldRow = e.Cache.GetOriginal(e.Row);

			List<Type> monitoringTypes = new List<Type>();
			monitoringTypes.Add(typeof(SOLine.inventoryID));
			monitoringTypes.Add(typeof(SOLine.curyDiscAmt));
			monitoringTypes.Add(typeof(SOLine.curyLineAmt));

			if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true)
				&& monitoringTypes.Any(t => !object.Equals(e.Cache.GetValue(e.Row, t.Name), e.Cache.GetValue(oldRow, t.Name))))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<SOLine.siteID> e)
		{
			if (e.NewValue != null && e.Row != null)
			{
				SOLine currentLine = e.Row as SOLine;
				var lines = Base.Transactions.Select().RowCast<SOLine>();

				foreach (var line in lines)
				{
					var lineExt = line.GetExtension<BCSOLineExt>();

					if (lineExt.AssociatedOrderLineNbr != null && lineExt.AssociatedOrderLineNbr == currentLine.LineNbr)
					{
						Base.Transactions.Cache.SetValueExt<SOLine.siteID>(line, currentLine.SiteID);
						Base.Transactions.Cache.IsDirty = true;
						Base.Transactions.Cache.Update(line);
						break;
					}
				}
			}
		}

		protected virtual void _(PX.Data.Events.RowUpdated<SOBillingAddress> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;


			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowUpdated<SOBillingContact> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;

			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}

		protected virtual void _(PX.Data.Events.RowUpdated<SOShippingAddress> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;


			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowUpdated<SOShippingContact> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;


			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}

		//to handle payments with Order, taxsyncMone= nosync, as applied to order amount will be greater due to taxes than ordertotal which is without taxes
		protected virtual void _(PX.Data.Events.RowPersisting<SOAdjust> e)
		{

			if (e.Row == null || Base.Document.Current == null || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update || Base.Document.Current.IsTaxValid != false) return;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null)
			{
				//Calculated Unpaid Balance
				decimal curyUnpaidBalance = Base.Document.Current.CuryOrderTotal ?? 0m;
				foreach (SOAdjust adj in Base.Adjustments.Select())
				{
					curyUnpaidBalance -= adj.CuryAdjdAmt ?? 0m;
				}

				decimal applicationAmount = (decimal)e.Row.CuryAdjdAmt > curyUnpaidBalance ? curyUnpaidBalance : (decimal)e.Row.CuryAdjdAmt;
				e.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(e.Row, applicationAmount);

			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<SOShippingAddress.isDefaultAddress> e)
		{
			if (e.NewValue != null && e.Row != null)
			{
				SOShippingAddress shippingAddress = e.Row as SOShippingAddress;
				if (Base?.soordertype?.Current != null && shippingAddress != null)
				{
					shippingAddress.IsEncrypted = !((bool)e.NewValue) && (Base.soordertype.Current.GetExtension<SOOrderTypeExt>().EncryptAndPseudonymizePII ?? false);

				}
				else
				{
					shippingAddress.IsEncrypted = false;
				}
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<SOBillingContact.isDefaultContact> e)
		{
			if (e.NewValue != null && e.Row != null)
			{
				SOBillingContact contact = e.Row as SOBillingContact;
				if (Base?.soordertype?.Current != null && contact != null)
				{
					contact.IsEncrypted = !((bool)e.NewValue) && (Base.soordertype.Current.GetExtension<SOOrderTypeExt>().EncryptAndPseudonymizePII ?? false);

				}
				else
				{
					contact.IsEncrypted = false;
				}
			}
		}


		protected virtual void _(PX.Data.Events.FieldUpdated<SOBillingAddress.isDefaultAddress> e)
		{
			if (e.NewValue != null && e.Row != null)
			{
				SOBillingAddress billingAddress = e.Row as SOBillingAddress;
				if (Base?.soordertype?.Current != null && billingAddress != null)
				{
					billingAddress.IsEncrypted = !((bool)e.NewValue) && (Base.soordertype.Current.GetExtension<SOOrderTypeExt>().EncryptAndPseudonymizePII ?? false);

				}
				else
				{
					billingAddress.IsEncrypted = false;
				}
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<SOShippingContact.isDefaultContact> e)
		{
			if (e.NewValue != null && e.Row != null)
			{
				SOShippingContact contact = e.Row as SOShippingContact;
				if (Base?.soordertype?.Current != null && contact != null)
				{
					contact.IsEncrypted = !((bool)e.NewValue) && (Base.soordertype.Current.GetExtension<SOOrderTypeExt>().EncryptAndPseudonymizePII ?? false);

				}
				else
				{
					contact.IsEncrypted = false;
				}
			}
		}

	}
}
